using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HollowPawns
{
    // ======================= Grim industry (4.37): battery farming =======================
    //
    // Three kinds of pod share one base: battery cages and pens (animals), the rendering pod (human
    // prisoners into nutrient slurry) and the mech dynamo (mechanoids into power). Every pod is a casket:
    // the occupant is held, not spawned, so nothing inside ticks. The pod does the bookkeeping on its own
    // rare tick (feeding, ageing, production), the same as the prisoner battery. No per-pawn ticking.

    [DefOf]
    public static class HP_GrimDefOf
    {
        public static ThingDef HP_FeedHopper;
        public static ThingDef HP_NutrientSlurry;
        public static HediffDef HP_Fattened;
        public static JobDef HP_LoadIntoPod;
        public static SoundDef HP_CageCollect;
        public static SoundDef HP_RenderSquelch;
        public static SoundDef HP_MechBreakout;

        static HP_GrimDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HP_GrimDefOf));
        }
    }

    /// <summary>Per-pod numbers, set in XML so cage and pen can share one class.</summary>
    public class GrimPodExtension : DefModExtension
    {
        /// <summary>Largest body size the pod takes.</summary>
        public float maxBodySize = 1f;
        /// <summary>Products come this much faster than out in the field.</summary>
        public float productionFactor = 1.5f;
        /// <summary>Juveniles grow up this much faster.</summary>
        public float growthFactor = 5f;
        /// <summary>Adults age this much faster: a shorter life.</summary>
        public float agingFactor = 1.25f;
        /// <summary>Hoppers within this range feed the pod.</summary>
        public float hopperRange = 4.9f;
    }

    /// <summary>
    /// Base for every grim-industry pod: holds one pawn, knows who should go in next and which work
    /// type loads it, and has the Eject and Assign buttons.
    /// </summary>
    public abstract class Building_GrimPod : Building_Casket
    {
        protected Pawn assigned;

        public Pawn Occupant => ContainedThing as Pawn;

        public GrimPodExtension Ext => def.GetModExtension<GrimPodExtension>() ?? new GrimPodExtension();

        /// <summary>Who loads this pod: Handling for cages, Warden for the rendering pod, Hauling for the dynamo.</summary>
        public abstract WorkTypeDef LoaderWork { get; }

        /// <summary>Whether this pawn may go in (ignoring whether the pod is full).</summary>
        public abstract bool CanHold(Pawn p);

        /// <summary>Who a loader should bring here, if anyone.</summary>
        public virtual Pawn NextCandidate(Pawn worker)
        {
            if (assigned == null || assigned.Destroyed || !assigned.Spawned || assigned.Map != Map || !CanHold(assigned))
            {
                return null;
            }
            return assigned;
        }

        /// <summary>Candidates shown on the Assign button.</summary>
        protected virtual IEnumerable<Pawn> AssignCandidates() => Enumerable.Empty<Pawn>();

        protected virtual string AssignLabel => "Assign";

        public override bool Accepts(Thing thing)
        {
            return thing is Pawn p && !HasAnyContents && CanHold(p) && base.Accepts(thing);
        }

        public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!base.TryAcceptThing(thing, allowSpecialEffects))
            {
                return false;
            }
            Pawn p = (Pawn)thing;
            Hediff restrained = p.health?.hediffSet?.GetFirstHediffOfDef(HP_DefOf.HP_RiteRestrained);
            if (restrained != null)
            {
                p.health.RemoveHediff(restrained);
            }
            assigned = null;
            OnLoaded(p);
            return true;
        }

        protected virtual void OnLoaded(Pawn p)
        {
        }

        /// <summary>Takes the occupant out and puts them down next to the pod. Returns them (null if empty).</summary>
        protected Pawn DropOccupant()
        {
            Pawn p = Occupant;
            if (p == null)
            {
                return null;
            }
            innerContainer.TryDrop(p, InteractionCellOrPosition, Map, ThingPlaceMode.Near, out Thing _);
            return p;
        }

        protected IntVec3 InteractionCellOrPosition => def.hasInteractionCell ? InteractionCell : Position;

        /// <summary>Drops a product next to the pod.</summary>
        protected void Spawn(ThingDef thingDef, int count)
        {
            if (thingDef == null || count <= 0 || Map == null)
            {
                return;
            }
            while (count > 0)
            {
                Thing t = ThingMaker.MakeThing(thingDef);
                t.stackCount = Mathf.Min(count, thingDef.stackLimit);
                count -= t.stackCount;
                GenPlace.TryPlaceThing(t, InteractionCellOrPosition, Map, ThingPlaceMode.Near);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Faction != Faction.OfPlayer)
            {
                yield break;
            }
            if (Occupant != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Eject",
                    defaultDesc = "Open the pod and let the occupant out.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject"),
                    action = EjectContents,
                };
            }
            else
            {
                List<Pawn> candidates = AssignCandidates().ToList();
                var cmd = new Command_Action
                {
                    defaultLabel = assigned == null ? AssignLabel : AssignLabel + ": " + assigned.LabelShort,
                    defaultDesc = "Choose who goes in. A " + LoaderWork.labelShort + " worker brings them.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter"),
                    action = () =>
                    {
                        var options = candidates.Select(c => new FloatMenuOption(c.LabelCap, () => assigned = c)).ToList();
                        if (assigned != null)
                        {
                            options.Add(new FloatMenuOption("Clear", () => assigned = null));
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    },
                };
                if (candidates.Count == 0 && assigned == null)
                {
                    cmd.Disable("Nothing here fits.");
                }
                yield return cmd;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref assigned, "assigned");
        }
    }

    // ----------------------- Feed hopper -----------------------

    /// <summary>The feed hopper: plain storage for food and medicine. Pods nearby take what they need from it.</summary>
    public static class FeedHoppers
    {
        public static IEnumerable<Building> Near(Building pod, float range)
        {
            return pod.Map.listerBuildings.AllBuildingsColonistOfDef(HP_GrimDefOf.HP_FeedHopper)
                .Where(h => h.Position.InHorDistOf(pod.Position, range));
        }

        /// <summary>Things stored on the hopper's cells.</summary>
        public static IEnumerable<Thing> Contents(Building hopper)
        {
            foreach (IntVec3 c in hopper.OccupiedRect())
            {
                List<Thing> things = c.GetThingList(hopper.Map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (things[i].def.category == ThingCategory.Item)
                    {
                        yield return things[i];
                    }
                }
            }
        }
    }

    // ----------------------- Battery cage and pen -----------------------

    /// <summary>
    /// The battery cage (small animals) and pen (big ones). Whatever the animal gives in the field it
    /// gives here, faster: eggs, milk, wool. An animal that gives nothing (or a male) is grown fast if
    /// young, fattened for meat if grown. Fed from feed hoppers nearby (or the paste pipes with VNPE);
    /// medicine keeps it healthy, without it it sickens and dies young.
    /// </summary>
    public class Building_BatteryCage : Building_GrimPod
    {
        private const float DayPerRare = GenTicks.TickRareInterval / (float)GenDate.TicksPerDay;
        private const int MedicineDays = 4;

        private float storedNutrition;
        private int storedMedicine;
        private float medicatedDays;
        private float condition = 1f;
        private float eggProgress;
        private float milkProgress;
        private float woolProgress;

        public override WorkTypeDef LoaderWork => WorkTypeDefOf.Handling;

        protected override string AssignLabel => "Assign animal";

        public override bool CanHold(Pawn p)
        {
            if (p == null || p.Dead || p.RaceProps == null || !p.RaceProps.Animal || p.Faction != Faction.OfPlayer)
            {
                return false;
            }
            if (p.def.defName.Contains("Dryad") || p.RaceProps.IsMechanoid || p.RaceProps.Humanlike)
            {
                return false;
            }
            return p.BodySize <= Ext.maxBodySize;
        }

        protected override IEnumerable<Pawn> AssignCandidates() => Map.mapPawns.SpawnedColonyAnimals.Where(CanHold);

        protected override void OnLoaded(Pawn p)
        {
            condition = Mathf.Clamp01(p.health.summaryHealth.SummaryHealthPercent);
        }

        private float DailyNutrition(Pawn p)
        {
            return 1.6f * p.RaceProps.baseHungerRate * p.ageTracker.CurLifeStage.hungerRateFactor;
        }

        private bool Juvenile(Pawn p) => p.ageTracker.CurLifeStageIndex < p.RaceProps.lifeStageAges.Count - 1;

        private CompProperties_EggLayer Eggs(Pawn p)
        {
            var props = p.def.GetCompProperties<CompProperties_EggLayer>();
            if (props == null || props.eggUnfertilizedDef == null || !p.ageTracker.CurLifeStage.reproductive
                || (props.eggLayFemaleOnly && p.gender != Gender.Female))
            {
                return null;
            }
            return props;
        }

        private CompProperties_Milkable Milk(Pawn p)
        {
            var props = p.def.GetCompProperties<CompProperties_Milkable>();
            if (props == null || props.milkDef == null || !p.ageTracker.CurLifeStage.milkable
                || (props.milkFemaleOnly && p.gender != Gender.Female))
            {
                return null;
            }
            return props;
        }

        private CompProperties_Shearable Wool(Pawn p)
        {
            var props = p.def.GetCompProperties<CompProperties_Shearable>();
            if (props == null || props.woolDef == null || !p.ageTracker.CurLifeStage.shearable)
            {
                return null;
            }
            return props;
        }

        private bool Produces(Pawn p) => Eggs(p) != null || Milk(p) != null || Wool(p) != null;

        public override void TickRare()
        {
            base.TickRare();
            Pawn p = Occupant;
            if (p == null)
            {
                return;
            }
            float daily = DailyNutrition(p);
            if (storedNutrition < daily || storedMedicine == 0)
            {
                Restock(p, daily);
            }

            float need = daily * DayPerRare;
            if (storedNutrition < need)
            {
                // Starving: nothing grows, nothing comes out, and it won't last long.
                condition -= 0.35f * DayPerRare;
                CheckDeath(p, "starved");
                return;
            }
            storedNutrition -= need;

            if (medicatedDays <= 0f && storedMedicine > 0)
            {
                storedMedicine--;
                medicatedDays = MedicineDays;
            }
            if (medicatedDays > 0f)
            {
                medicatedDays -= DayPerRare;
                condition = Mathf.Min(1f, condition + 0.1f * DayPerRare);
            }
            else
            {
                condition -= 0.04f * DayPerRare;
                if (CheckDeath(p, "sickened and died"))
                {
                    return;
                }
            }

            float output = Ext.productionFactor * (condition < 0.5f ? condition * 2f : 1f);
            bool juvenile = Juvenile(p);
            p.ageTracker.AgeTickMothballed(Mathf.RoundToInt(GenTicks.TickRareInterval * (juvenile ? Ext.growthFactor : Ext.agingFactor)));

            if (Produces(p))
            {
                Produce(p, output);
            }
            else if (!juvenile)
            {
                Fatten(p);
            }
        }

        private void Produce(Pawn p, float output)
        {
            bool made = false;
            var eggs = Eggs(p);
            if (eggs != null && Step(ref eggProgress, eggs.eggLayIntervalDays, output))
            {
                Spawn(eggs.eggUnfertilizedDef, eggs.eggCountRange.RandomInRange);
                made = true;
            }
            var milk = Milk(p);
            if (milk != null && Step(ref milkProgress, milk.milkIntervalDays, output))
            {
                Spawn(milk.milkDef, milk.milkAmount);
                made = true;
            }
            var wool = Wool(p);
            if (wool != null && Step(ref woolProgress, wool.shearIntervalDays, output))
            {
                Spawn(wool.woolDef, wool.woolAmount);
                made = true;
            }
            if (made)
            {
                HP_GrimDefOf.HP_CageCollect?.PlayOneShot(new TargetInfo(Position, Map));
            }
        }

        private static bool Step(ref float progress, float intervalDays, float output)
        {
            progress += DayPerRare * output / Mathf.Max(0.1f, intervalDays);
            if (progress < 1f)
            {
                return false;
            }
            progress -= 1f;
            return true;
        }

        private static void Fatten(Pawn p)
        {
            Hediff h = p.health.hediffSet.GetFirstHediffOfDef(HP_GrimDefOf.HP_Fattened) ?? p.health.AddHediff(HP_GrimDefOf.HP_Fattened);
            h.Severity = Mathf.Min(1f, h.Severity + 0.1f * DayPerRare);
        }

        private bool CheckDeath(Pawn p, string how)
        {
            if (condition > 0f)
            {
                return false;
            }
            DropOccupant();
            Messages.Message("The caged " + p.LabelShort + " " + how + ".", new TargetInfo(Position, Map), MessageTypeDefOf.NegativeEvent);
            if (!p.Dead)
            {
                p.Kill(null);
            }
            ResetState();
            return true;
        }

        private void ResetState()
        {
            condition = 1f;
            eggProgress = milkProgress = woolProgress = 0f;
            medicatedDays = 0f;
        }

        /// <summary>Tops up food (two days' worth) and medicine (two doses) from hoppers in range, then the paste pipes.</summary>
        private void Restock(Pawn p, float daily)
        {
            float want = daily * 2f - storedNutrition;
            foreach (Building hopper in FeedHoppers.Near(this, Ext.hopperRange))
            {
                foreach (Thing t in FeedHoppers.Contents(hopper).ToList())
                {
                    if (want > 0f && t.def.IsNutritionGivingIngestible && !t.def.IsDrug && p.RaceProps.CanEverEat(t))
                    {
                        float per = t.GetStatValue(StatDefOf.Nutrition);
                        if (per <= 0f)
                        {
                            continue;
                        }
                        int take = Mathf.Min(t.stackCount, Mathf.CeilToInt(want / per));
                        t.SplitOff(take).Destroy();
                        storedNutrition += take * per;
                        want -= take * per;
                    }
                    else if (storedMedicine < 2 && t.def.IsMedicine)
                    {
                        int take = Mathf.Min(t.stackCount, 2 - storedMedicine);
                        t.SplitOff(take).Destroy();
                        storedMedicine += take;
                    }
                }
            }
            // Vanilla Nutrient Paste Expanded: one meal's worth of paste at a time, only when the hoppers are dry.
            while (want > 0f && PipeNets.TryDraw(this, PipeNets.Paste, 1f))
            {
                storedNutrition += 0.9f;
                want -= 0.9f;
            }
        }

        public override void EjectContents()
        {
            base.EjectContents();
            ResetState();
            storedNutrition = 0f;
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            Pawn p = Occupant;
            string line;
            if (p == null)
            {
                line = assigned != null ? "Waiting for " + assigned.LabelShort + "." : "Empty. Assign an animal and a handler will bring it.";
            }
            else
            {
                string mode = Produces(p) ? "producing" : Juvenile(p) ? "growing fast" : "fattening";
                float daily = DailyNutrition(p);
                line = p.LabelCap + ": " + mode + ", condition " + condition.ToStringPercent()
                    + "\nFeed: " + (daily > 0f ? (storedNutrition / daily).ToString("0.0") : "-") + " days, medicine: " + storedMedicine
                    + (medicatedDays > 0f ? " (dosed)" : " (undosed: sickening)");
            }
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref storedNutrition, "storedNutrition");
            Scribe_Values.Look(ref storedMedicine, "storedMedicine");
            Scribe_Values.Look(ref medicatedDays, "medicatedDays");
            Scribe_Values.Look(ref condition, "condition", 1f);
            Scribe_Values.Look(ref eggProgress, "eggProgress");
            Scribe_Values.Look(ref milkProgress, "milkProgress");
            Scribe_Values.Look(ref woolProgress, "woolProgress");
        }
    }

    // ----------------------- Rendering pod -----------------------

    /// <summary>
    /// The rendering pod: a living adult prisoner marked "Slurry" is sealed in and slowly broken down into
    /// nutrient slurry, a share every day, over four days per unit of body size. At the end nothing is
    /// left: the soul comes loose as a little essence, the body is gone. Leaks corruption like the battery.
    /// </summary>
    public class Building_RenderingPod : Building_GrimPod
    {
        public const float DaysPerBodySize = 4f;
        public const int SlurryPerBodySize = 100;

        private float daysLeft;
        private float totalDays;
        private int slurryLeft;

        public override WorkTypeDef LoaderWork => WorkTypeDefOf.Warden;

        public override bool CanHold(Pawn p) => p != null && !p.Dead && p.IsPrisonerOfColony && SoulUtility.CanExtract(p);

        public override Pawn NextCandidate(Pawn worker)
        {
            return Map.mapPawns.PrisonersOfColonySpawned
                .Where(p => SoulUtility.GetFate(p) == SoulFate.Slurry && SoulUtility.CanMark(p) && CanHold(p))
                .OrderBy(p => p.Position.DistanceToSquared(Position))
                .FirstOrDefault();
        }

        protected override void OnLoaded(Pawn p)
        {
            SoulUtility.ClearFate(p);
            totalDays = daysLeft = DaysPerBodySize * p.BodySize;
            slurryLeft = Mathf.RoundToInt(SlurryPerBodySize * p.BodySize);
        }

        public override void TickRare()
        {
            base.TickRare();
            Pawn p = Occupant;
            if (p == null || totalDays <= 0f)
            {
                return;
            }
            if (!(GetComp<CompPowerTrader>()?.PowerOn ?? true))
            {
                // No power, no enzymes: the pod just holds them.
                return;
            }
            float before = daysLeft;
            daysLeft -= GenTicks.TickRareInterval / (float)GenDate.TicksPerDay;
            if (Mathf.FloorToInt(before) != Mathf.FloorToInt(Mathf.Max(0f, daysLeft)) || daysLeft <= 0f)
            {
                // A day's share of slurry, and a little corruption.
                int share = daysLeft <= 0f ? slurryLeft : Mathf.Min(slurryLeft, Mathf.RoundToInt(SlurryPerBodySize * p.BodySize / totalDays));
                slurryLeft -= share;
                Spawn(HP_GrimDefOf.HP_NutrientSlurry, share);
                HP_GrimDefOf.HP_RenderSquelch?.PlayOneShot(new TargetInfo(Position, Map));
                Corruption.Spill(Position, Map, 20, 2.9f);
            }
            if (daysLeft <= 0f)
            {
                Finish(p);
            }
        }

        private void Finish(Pawn p)
        {
            Map map = Map;
            IntVec3 cell = InteractionCellOrPosition;
            DropOccupant();
            SoulGrade grade = SoulGrades.Of(p);
            SoulUtility.DropEssence(cell, map, Mathf.Max(1, SoulGrades.RenderYield(grade) / 3), SoulUtility.EssenceDefAt(p, cell, map));
            Faction home = p.HomeFaction;
            if (home != null && home != Faction.OfPlayer && !home.HostileTo(Faction.OfPlayer))
            {
                home.TryAffectGoodwillWith(Faction.OfPlayer, -40, true, true, HistoryEventDefOf.MemberKilled);
            }
            Messages.Message(p.LabelShort + " has been rendered down. Nothing is left but slurry.", new TargetInfo(cell, map), MessageTypeDefOf.NeutralEvent);
            p.Strip();
            SoulUtility.TakeSoulAndKill(p);
            if (!p.Dead)
            {
                p.Kill(null);
            }
            p.Corpse?.Destroy();
            totalDays = daysLeft = 0f;
            slurryLeft = 0;
        }

        public override void EjectContents()
        {
            Pawn p = Occupant;
            base.EjectContents();
            if (p != null && !p.Dead)
            {
                // Pulled out part-way: alive, and not all there.
                float done = totalDays > 0f ? 1f - daysLeft / totalDays : 0f;
                HealthUtility.AdjustSeverity(p, HP_DefOf.HP_SoulShock, 0.3f + 0.5f * done);
                HealthUtility.DamageUntilDowned(p, false);
            }
            totalDays = daysLeft = 0f;
            slurryLeft = 0;
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            Pawn p = Occupant;
            string line = p == null
                ? "Empty. Mark a prisoner \"slurry\" and a warden will seal them in."
                : "Rendering " + p.LabelShort + ": " + Mathf.Max(0f, daysLeft).ToString("0.0") + " days left, " + slurryLeft + " slurry to come.";
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref daysLeft, "daysLeft");
            Scribe_Values.Look(ref totalDays, "totalDays");
            Scribe_Values.Look(ref slurryLeft, "slurryLeft");
        }
    }

    // ----------------------- Mech dynamo -----------------------

    /// <summary>
    /// The mech dynamo. A colony mech plugged in runs backwards and feeds the grid; it wears as it goes and
    /// comes out battered after ten days. A downed hostile mech locked in is drained harder and slowly
    /// stripped for parts, but it might wake up and tear its way out. Both leave toxic waste (Biotech).
    /// </summary>
    public class Building_MechDynamo : Building_GrimPod
    {
        public const float WearDays = 10f;
        public const float StripDaysPerBodySize = 6f;

        private float wear;
        private float stripProgress;
        private float wasteProgress;

        public override WorkTypeDef LoaderWork => WorkTypeDefOf.Hauling;

        protected override string AssignLabel => "Assign mech";

        private bool Captive => Occupant != null && Occupant.Faction != Faction.OfPlayer;

        public float Watts
        {
            get
            {
                Pawn p = Occupant;
                if (p == null)
                {
                    return 0f;
                }
                return (Captive ? 600f : 300f) * p.BodySize;
            }
        }

        public override bool CanHold(Pawn p)
        {
            if (p == null || p.Dead || p.RaceProps == null || !p.RaceProps.IsMechanoid || p.BodySize > Ext.maxBodySize)
            {
                return false;
            }
            if (p.kindDef != null && p.kindDef.isBoss)
            {
                return false;
            }
            return p.Faction == Faction.OfPlayer || (p.Downed && (p.Faction == null || p.HostileTo(Faction.OfPlayer)));
        }

        protected override IEnumerable<Pawn> AssignCandidates() => Map.mapPawns.AllPawnsSpawned.Where(CanHold);

        protected override void OnLoaded(Pawn p)
        {
            wear = stripProgress = 0f;
            UpdatePower();
        }

        public override void TickRare()
        {
            base.TickRare();
            Pawn p = Occupant;
            if (p == null)
            {
                return;
            }
            float dt = GenTicks.TickRareInterval / (float)GenDate.TicksPerDay;
            wasteProgress += dt / 2f;
            if (wasteProgress >= 1f)
            {
                wasteProgress -= 1f;
                Spawn(DefDatabase<ThingDef>.GetNamedSilentFail("Wastepack"), 1);
            }
            if (Captive)
            {
                if (Rand.Chance(0.03f * p.BodySize * dt))
                {
                    Breakout(p);
                    return;
                }
                stripProgress += dt / (StripDaysPerBodySize * p.BodySize);
                if (stripProgress >= 1f)
                {
                    Strip(p);
                }
            }
            else
            {
                wear += dt / WearDays;
                if (wear >= 1f)
                {
                    Messages.Message(p.LabelShort + " is worn out and the dynamo has let it go.", new TargetInfo(Position, Map), MessageTypeDefOf.NeutralEvent);
                    EjectContents();
                }
            }
            UpdatePower();
        }

        private void UpdatePower() => GetComp<CompPowerPlant_MechDynamo>()?.UpdateDesiredPowerOutput();

        /// <summary>The captive mech is stripped down to nothing: its parts drop, the shell is gone.</summary>
        private void Strip(Pawn p)
        {
            Map map = Map;
            IntVec3 cell = InteractionCellOrPosition;
            DropOccupant();
            foreach (Thing t in p.ButcherProducts(null, 1f).ToList())
            {
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
            }
            Messages.Message("The dynamo has stripped " + p.LabelShort + " for parts.", new TargetInfo(cell, map), MessageTypeDefOf.PositiveEvent);
            if (!p.Dead)
            {
                p.Kill(null);
            }
            p.Corpse?.Destroy();
            stripProgress = 0f;
            UpdatePower();
        }

        /// <summary>The captive wakes, mends and tears its way out.</summary>
        private void Breakout(Pawn p)
        {
            DropOccupant();
            foreach (Hediff_Injury injury in p.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToList())
            {
                p.health.RemoveHediff(injury);
            }
            HP_GrimDefOf.HP_MechBreakout?.PlayOneShot(new TargetInfo(p.Position, p.Map));
            Find.LetterStack.ReceiveLetter("Mech breakout", p.LabelCap + " has woken up and torn its way out of the mech dynamo.", LetterDefOf.ThreatBig, p);
            stripProgress = 0f;
            UpdatePower();
        }

        public override void EjectContents()
        {
            Pawn p = Occupant;
            float w = wear;
            base.EjectContents();
            if (p != null && p.Spawned && !p.Dead && p.Faction == Faction.OfPlayer && w > 0.05f)
            {
                // The wear shows: a few dents for every few days plugged in.
                int hits = Mathf.CeilToInt(w * 6f);
                for (int i = 0; i < hits && !p.Dead; i++)
                {
                    p.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 4f, 999f));
                }
            }
            wear = stripProgress = 0f;
            UpdatePower();
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            Pawn p = Occupant;
            string line;
            if (p == null)
            {
                line = assigned != null ? "Waiting for " + assigned.LabelShort + "." : "Empty. Assign a colony mech, or a downed enemy one.";
            }
            else if (Captive)
            {
                line = "Draining captive " + p.LabelShort + ": " + Watts.ToString("0") + " W, stripped " + stripProgress.ToStringPercent() + ". It may wake.";
            }
            else
            {
                line = "Running " + p.LabelShort + " backwards: " + Watts.ToString("0") + " W, wear " + wear.ToStringPercent() + ".";
            }
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref wear, "wear");
            Scribe_Values.Look(ref stripProgress, "stripProgress");
            Scribe_Values.Look(ref wasteProgress, "wasteProgress");
        }
    }

    public class CompPowerPlant_MechDynamo : CompPowerPlant
    {
        protected override float DesiredPowerOutput => (parent as Building_MechDynamo)?.Watts ?? 0f;
    }

    // ----------------------- Loading -----------------------

    /// <summary>
    /// One work giver class for every pod; each WorkGiverDef only looks at pods loaded by its own work type
    /// (cages: Handling, rendering pod: Warden, dynamo: Hauling).
    /// </summary>
    public class WorkGiver_LoadGrimPod : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_GrimPod>()
                .Where(b => !b.HasAnyContents && b.LoaderWork == def.workType).Cast<Thing>();
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_GrimPod pod) || pod.HasAnyContents || pod.LoaderWork != def.workType || t.IsForbidden(pawn)
                || !pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            Pawn target = pod.NextCandidate(pawn);
            if (target == null || target == pawn || !pawn.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(HP_GrimDefOf.HP_LoadIntoPod, target, t);
            job.count = 1;
            return job;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => JobOnThing(pawn, t, forced) != null;
    }

    /// <summary>A = the pawn going in, B = the pod. Restrain (if they're up), carry over, seal in.</summary>
    public class JobDriver_LoadIntoPod : JobDriver
    {
        private Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        private Building_GrimPod Pod => (Building_GrimPod)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) && pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOn(() => Pod.HasAnyContents || !Pod.CanHold(Target));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil restrain = ToilMaker.MakeToil("HP_RestrainForPod");
            restrain.initAction = () =>
            {
                Pawn t = Target;
                if (!t.Downed && !t.health.hediffSet.HasHediff(HP_DefOf.HP_RiteRestrained))
                {
                    t.health.AddHediff(HP_DefOf.HP_RiteRestrained);
                }
                t.jobs?.StopAll();
            };
            yield return restrain;
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, Pod.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
            yield return Toils_General.Wait(240, TargetIndex.B).WithProgressBarToilDelay(TargetIndex.B);
            Toil seal = ToilMaker.MakeToil("HP_SealPod");
            seal.initAction = () =>
            {
                Pawn t = Target;
                if (Pod.TryAcceptThing(t))
                {
                    Messages.Message(pawn.LabelShort + " sealed " + t.LabelShort + " into the " + Pod.Label + ".", Pod, MessageTypeDefOf.NeutralEvent);
                }
            };
            seal.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return seal;
        }
    }
}
