using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HollowPawns
{
    /// <summary>What a marked prisoner is marked for (4.30).</summary>
    public enum SoulFate : byte
    {
        None,
        /// <summary>Plain essence: nameless, stacks, cheap.</summary>
        Render,
        /// <summary>The whole soul into a vessel: named, graded, kept.</summary>
        Bind,
        /// <summary>Locked in a prisoner battery and drained for power (4.7).</summary>
        Battery,
        /// <summary>A blood debtor's living soul bound into a wailing effigy (4.18).</summary>
        Effigy,
    }

    public class CompProperties_SoulRite : CompProperties
    {
        /// <summary>How long the rite takes once the victim is on the altar.</summary>
        public int riteTicks = 2500;

        /// <summary>Grade steps added to the soul (the press takes cleaner souls).</summary>
        public int gradeBonus;

        /// <summary>Chance the rite is botched: the soul comes out a grade worse.</summary>
        public float botchChance;

        /// <summary>Witnesses who can see it get a bad memory.</summary>
        public bool upsetsWitnesses;

        /// <summary>Corruption spilled around the building per rite, and how far.</summary>
        public int corruption = 100;
        public float corruptionRadius = 3.9f;

        public EffecterDef effecter;

        /// <summary>
        /// Load and leave (the soul press): the warden straps the victim in and goes; the building runs
        /// the rite itself on its rare tick, faster when staffed. Needs tickerType Rare on the def.
        /// </summary>
        public bool autonomous;

        /// <summary>Wisps: torn out unevenly (altar) or drawn up in a steady stream (press).</summary>
        public bool steadyStream;

        /// <summary>Each rite done here makes raids a little heavier for a while (the press, 4.27).</summary>
        public bool drawsAttention;

        public CompProperties_SoulRite()
        {
            compClass = typeof(CompSoulRite);
        }
    }

    /// <summary>
    /// The Bone Altar and the Soul Press (4.27). Both are load-and-leave: the warden straps the victim
    /// in and goes, and the building runs the rite on its own rare tick, faster with someone working it
    /// (Staffing.cs). A def without autonomous falls back to the warden doing the whole rite.
    /// </summary>
    public class CompSoulRite : ThingComp, IStaffable
    {
        public CompProperties_SoulRite Props => (CompProperties_SoulRite)props;

        private Pawn victim;
        private Thing vessel;
        private Pawn loadedBy;
        private int ticksLeft = -1;

        public Staffing Staffing { get; } = new Staffing(SkillDefOf.Medicine);

        /// <summary>A victim is strapped in and the rite is under way (autonomous buildings only).</summary>
        public bool Busy => victim != null;

        public bool Holds(Pawn p) => p != null && victim == p;

        public bool WantsOperator => Busy && Usable;

        /// <summary>The warden's part is done: the press takes it from here.</summary>
        public void Load(Pawn warden, Pawn v, Thing emptyVessel)
        {
            HP_DefOf.HP_RiteStart?.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            victim = v;
            vessel = emptyVessel;
            loadedBy = warden;
            ticksLeft = Props.riteTicks;
        }

        private void Clear()
        {
            victim = null;
            vessel = null;
            loadedBy = null;
            ticksLeft = -1;
        }

        private void Abandon(string why)
        {
            Messages.Message("The " + parent.LabelShort + " stopped: " + why, parent, MessageTypeDefOf.NeutralEvent);
            Clear();
        }

        public override void CompTickRare()
        {
            if (!Busy)
            {
                return;
            }
            if (victim.Destroyed || victim.Dead || !victim.Spawned || victim.Map != parent.Map
                || !victim.Position.InHorDistOf(parent.Position, 1.5f))
            {
                Abandon("the prisoner is no longer in it.");
                return;
            }
            if (!Rites.IsRiteFate(SoulUtility.GetFate(victim)) || !SoulUtility.CanExtract(victim))
            {
                Abandon(victim.LabelShort + "'s mark was cleared.");
                return;
            }
            // Keep them strapped down for as long as it takes (the restraint fades over a day on its own).
            Hediff r = victim.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_RiteRestrained);
            if (r == null)
            {
                victim.health.AddHediff(HP_DefOf.HP_RiteRestrained);
            }
            else
            {
                r.Severity = 1f;
            }
            if (!Usable)
            {
                return;
            }
            RiteFx.Wisps(victim, parent, Props.steadyStream);
            ticksLeft -= Mathf.RoundToInt(GenTicks.TickRareInterval * Staffing.Factor);
            if (ticksLeft > 0)
            {
                return;
            }
            Pawn v = victim;
            Pawn worker = Staffing.Operator ?? (loadedBy != null && !loadedBy.Destroyed ? loadedBy : null);
            Thing ves = vessel != null && !vessel.Destroyed ? vessel : null;
            Clear();
            RiteFx.Release(parent, Props.steadyStream);
            if (Props.drawsAttention)
            {
                PressAttention.Add(parent.Map);
            }
            Rites.Complete(worker, v, parent, ves);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            Clear();
        }

        public override void PostExposeData()
        {
            Scribe_References.Look(ref victim, "victim");
            Scribe_References.Look(ref vessel, "vessel");
            Scribe_References.Look(ref loadedBy, "loadedBy");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
        }

        public bool Usable
        {
            get
            {
                CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();
                return parent.Spawned && (power == null || power.PowerOn) && !parent.IsBrokenDown();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            var unmarked = parent.Map.mapPawns.PrisonersOfColonySpawned.Where(p => SoulUtility.CanMark(p) && !SoulUtility.IsMarked(p)).ToList();
            var all = new Command_Action
            {
                defaultLabel = "Mark all",
                defaultDesc = "Mark every prisoner here whose soul is intact and who isn't Soul-Locked to be rendered.",
                icon = ReapersMark.MarkIcon,
                action = () =>
                {
                    foreach (Pawn p in unmarked)
                    {
                        SoulUtility.SetFate(p, SoulFate.Render);
                    }
                    Messages.Message("Marked " + unmarked.Count + " prisoner" + (unmarked.Count == 1 ? "" : "s") + " to be rendered.",
                        parent, MessageTypeDefOf.TaskCompletion, false);
                },
            };
            if (unmarked.Count == 0)
            {
                all.Disable("No unmarked prisoners with a soul.");
            }
            yield return all;
        }

        public override string CompInspectStringExtra()
        {
            int marked = parent.Map.mapPawns.PrisonersOfColonySpawned.Count(SoulUtility.IsMarked);
            string line = marked > 0 ? marked + " prisoner" + (marked == 1 ? "" : "s") + " marked. Wardens will bring them." : null;
            if (Busy)
            {
                string run = "Taking the soul of " + victim.LabelShort + ": " + Mathf.Max(0, ticksLeft).ToStringTicksToPeriod() + " left"
                    + (Usable ? "." : " (paused, no power).") + "\n" + Staffing.InspectLine();
                line = line == null ? run : run + "\n" + line;
            }
            return line;
        }
    }

    /// <summary>Wardens bring marked prisoners to a free altar or press and take their souls.</summary>
    public class WorkGiver_SoulRite : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                if (b.TryGetComp<CompSoulRite>() != null)
                {
                    yield return b;
                }
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.mapPawns.PrisonersOfColonySpawned.Any(p => Rites.IsRiteFate(SoulUtility.GetFate(p)));
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => MakeJob(pawn, t, forced) != null;

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) => MakeJob(pawn, t, forced);

        private static Job MakeJob(Pawn pawn, Thing t, bool forced)
        {
            CompSoulRite rite = t.TryGetComp<CompSoulRite>();
            if (rite == null || !rite.Usable || rite.Busy || t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            foreach (Pawn victim in pawn.Map.mapPawns.PrisonersOfColonySpawned)
            {
                SoulFate fate = SoulUtility.GetFate(victim);
                if (!Rites.IsRiteFate(fate) || !SoulUtility.CanMark(victim) || Rites.BeingPressed(victim, pawn.Map)
                    || !pawn.CanReserveAndReach(victim, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
                {
                    continue;
                }
                Job job = JobMaker.MakeJob(HP_DefOf.HP_PerformSoulRite, victim, t);
                // One of each: one vessel, one victim. Picking up the vessel must not use up the count.
                job.count = 1;
                if (fate == SoulFate.Bind)
                {
                    Thing vessel = Rites.BestEmptyVessel(pawn, forced);
                    if (vessel == null)
                    {
                        JobFailReason.Is("No empty soul vessel to bind into.");
                        continue;
                    }
                    job.targetC = vessel;
                }
                return job;
            }
            return null;
        }
    }

    public static class Rites
    {
        /// <summary>Fates the altar and press carry out. A battery has its own warden job.</summary>
        public static bool IsRiteFate(SoulFate f) => f == SoulFate.Render || f == SoulFate.Bind || f == SoulFate.Effigy;

        /// <summary>Already strapped into a press and being worked on.</summary>
        public static bool BeingPressed(Pawn victim, Map map) =>
            map.listerBuildings.allBuildingsColonist.Any(b => b.TryGetComp<CompSoulRite>()?.Holds(victim) == true);

        /// <summary>The highest-tier empty vessel the pawn can reach and reserve.</summary>
        public static Thing BestEmptyVessel(Pawn pawn, bool forced)
        {
            Thing best = null;
            int bestTier = -1;
            foreach (Thing t in pawn.Map.listerThings.AllThings)
            {
                VesselExtension ext = t.def.GetModExtension<VesselExtension>();
                if (ext == null || ext.tier <= bestTier || t.IsForbidden(pawn) || !t.Spawned
                    || !pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly, 1, 1, null, forced))
                {
                    continue;
                }
                best = t;
                bestTier = ext.tier;
            }
            return best;
        }

        /// <summary>
        /// The end of the rite: take the soul, render or bind it, and kill the victim. warden is whoever
        /// worked it; null when a press finished on its own with nobody left to credit.
        /// </summary>
        public static void Complete(Pawn warden, Pawn victim, Thing building, Thing emptyVessel)
        {
            CompSoulRite rite = building.TryGetComp<CompSoulRite>();
            CompProperties_SoulRite props = rite.Props;
            Map map = building.Map;
            IntVec3 cell = building.Position;
            SoulFate fate = SoulUtility.GetFate(victim);

            SoulGrade grade = SoulGrades.Of(victim).Shift(props.gradeBonus);
            bool botched = Rand.Chance(props.botchChance);
            if (botched)
            {
                grade = grade.Shift(-1);
            }
            ThingDef essence = SoulUtility.EssenceDefFor(victim);

            if (fate == SoulFate.Bind && emptyVessel != null && !emptyVessel.Destroyed)
            {
                VesselExtension ext = emptyVessel.def.GetModExtension<VesselExtension>();
                Thing filled = ThingMaker.MakeThing(ext.filledDef);
                filled.TryGetComp<CompSoulVessel>()?.Fill(victim, grade, ext.gradeCap);
                if (emptyVessel.stackCount > 1)
                {
                    emptyVessel.SplitOff(1).Destroy();
                }
                else
                {
                    emptyVessel.Destroy();
                }
                if (warden != null)
                {
                    TaleRecorder.RecordTale(HP_DefOf.HP_SoulBound, warden, victim);
                }
                GenPlace.TryPlaceThing(filled, building.InteractionCell, map, ThingPlaceMode.Near);
            }
            else if (fate == SoulFate.Effigy)
            {
                GenPlace.TryPlaceThing(Effigies.MakeWailing(warden, victim, grade), building.InteractionCell, map, ThingPlaceMode.Near);
            }
            else
            {
                SoulUtility.DropEssence(building.InteractionCell, map, SoulGrades.RenderYield(grade), essence);
            }

            Messages.Message((warden != null ? warden.LabelShort : "The " + building.LabelShort) + " took the " + grade.Label() + " soul of " + victim.LabelShort
                + (fate == SoulFate.Bind ? " and bound it." : fate == SoulFate.Effigy ? " and sealed it, screaming, into an effigy." : " and rendered it.") + (botched ? " The rite was botched." : ""),
                new TargetInfo(cell, map), MessageTypeDefOf.NeutralEvent);

            Corruption.Spill(cell, map, props.corruption, props.corruptionRadius);
            Corruption.AddToPawn(warden, 0.06f);

            if (props.upsetsWitnesses)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (p != warden && p.Position.InHorDistOf(cell, 12f) && GenSight.LineOfSight(p.Position, cell, map)
                        && p.needs?.mood != null && !CorruptionCult.Attuned(p) && CorruptionCult.Stance(p) == CorruptionStance.None)
                    {
                        p.needs.mood.thoughts.memories.TryGainMemory(HP_DefOf.HP_WitnessedSoulRite);
                    }
                }
            }

            Faction home = victim.HomeFaction;
            if (home != null && home != Faction.OfPlayer && !home.HostileTo(Faction.OfPlayer))
            {
                home.TryAffectGoodwillWith(Faction.OfPlayer, -40, true, true, HistoryEventDefOf.MemberKilled);
            }

            CorruptionCult.Record(HP_DefOf.HP_SoulTaken, warden);
            SoulUtility.ClearFate(victim);
            SoulUtility.TakeSoulAndKill(victim);
        }
    }

    /// <summary>
    /// Fetch a vessel if binding, restrain and carry the victim to the altar or press, perform the rite.
    /// A = victim, B = altar/press, C = empty vessel (binding only).
    /// </summary>
    public class JobDriver_SoulRite : JobDriver
    {
        private Pawn Victim => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        private Thing Building => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) || !pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            return !job.targetC.IsValid || pawn.Reserve(job.targetC, job, 1, 1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOn(() => SoulUtility.GetFate(Victim) == SoulFate.None || !SoulUtility.CanExtract(Victim)
                || !Building.TryGetComp<CompSoulRite>().Usable);

            if (job.targetC.IsValid)
            {
                // Bring the vessel to the altar first.
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.C);
                yield return Toils_Haul.StartCarryThing(TargetIndex.C, false, false);
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
                Toil dropVessel = ToilMaker.MakeToil("HP_DropVessel");
                dropVessel.initAction = () =>
                {
                    if (pawn.carryTracker.TryDropCarriedThing(Building.InteractionCell, ThingPlaceMode.Near, out Thing dropped))
                    {
                        job.SetTarget(TargetIndex.C, dropped);
                    }
                };
                yield return dropVessel;
            }

            // Restrain the victim and carry them over.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil bind = ToilMaker.MakeToil("HP_Restrain");
            bind.initAction = () =>
            {
                if (!Victim.health.hediffSet.HasHediff(HP_DefOf.HP_RiteRestrained))
                {
                    Victim.health.AddHediff(HP_DefOf.HP_RiteRestrained);
                }
            };
            yield return bind;
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            Toil lay = ToilMaker.MakeToil("HP_LayOnAltar");
            lay.initAction = () => pawn.carryTracker.TryDropCarriedThing(Building.Position, ThingPlaceMode.Direct, out Thing _);
            yield return lay;

            // Load and leave: the press runs the rite itself.
            CompProperties_SoulRite props = Building.TryGetComp<CompSoulRite>().Props;
            if (props.autonomous)
            {
                Toil load = ToilMaker.MakeToil("HP_LoadPress");
                load.initAction = () =>
                {
                    Thing vessel = job.targetC.IsValid ? job.targetC.Thing : null;
                    Building.TryGetComp<CompSoulRite>().Load(pawn, Victim, vessel);
                };
                load.defaultCompleteMode = ToilCompleteMode.Instant;
                yield return load;
                yield break;
            }

            // The rite.
            Toil perform = Toils_General.Wait(props.riteTicks, TargetIndex.B);
            perform.FailOn(() => Victim.Position != Building.Position && !Victim.Position.InHorDistOf(Building.Position, 1.5f));
            perform.WithProgressBarToilDelay(TargetIndex.B);
            if (props.effecter != null)
            {
                perform.WithEffect(props.effecter, TargetIndex.B);
            }
            perform.PlaySustainerOrSound(SoundDefOf.Recipe_Surgery);
            yield return perform;

            Toil finish = ToilMaker.MakeToil("HP_FinishRite");
            finish.initAction = () =>
            {
                Thing vessel = job.targetC.IsValid ? job.targetC.Thing : null;
                Rites.Complete(pawn, Victim, Building, vessel);
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}

namespace HollowPawns
{
    /// <summary>
    /// A prisoner strapped into an altar or press is downed, and wardens carry downed prisoners to bed,
    /// which would pull them out mid-rite. Leave them where they are.
    /// </summary>
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.WorkGiver_Warden_TakeToBed), nameof(RimWorld.WorkGiver_Warden_TakeToBed.JobOnThing))]
    public static class Patch_TakeToBed_SoulPress
    {
        public static void Postfix(Verse.Thing t, ref Verse.AI.Job __result)
        {
            if (__result != null && t is Verse.Pawn p && p.Spawned && Rites.BeingPressed(p, p.Map))
            {
                __result = null;
            }
        }
    }

    /// <summary>
    /// The rite's look (4.27): wisps thrown on the building's rare tick while a rite runs, a flash when
    /// the soul comes free. Flecks are batch-drawn by vanilla, so this costs next to nothing.
    /// </summary>
    public static class RiteFx
    {
        public static void Wisps(Pawn victim, Thing building, bool steady)
        {
            Map map = building.Map;
            Vector3 from = victim.DrawPos;
            Vector3 to = building.TrueCenter();
            float angle = (to - from).AngleFlat();
            int n = steady ? 6 : Rand.RangeInclusive(2, 6);
            for (int i = 0; i < n; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, steady ? i / (float)n : Rand.Value * 0.6f) + new Vector3(Rand.Range(-0.2f, 0.2f), 0f, Rand.Range(-0.2f, 0.2f));
                FleckCreationData data = FleckMaker.GetDataStatic(at, map, HP_DefOf.HP_SoulWisp, steady ? 0.7f : Rand.Range(0.5f, 1.1f));
                data.velocityAngle = angle + (steady ? Rand.Range(-6f, 6f) : Rand.Range(-45f, 45f));
                data.velocitySpeed = steady ? 0.45f : Rand.Range(0.2f, 0.8f);
                data.rotationRate = Rand.Range(-40f, 40f);
                map.flecks.CreateFleck(data);
            }
            if (!steady && Rand.Chance(0.4f))
            {
                FleckMaker.ThrowMicroSparks(to, map);
            }
        }

        public static void Release(Thing building, bool steady)
        {
            Map map = building.Map;
            Vector3 c = building.TrueCenter();
            FleckMaker.Static(c, map, FleckDefOf.PsycastAreaEffect, steady ? 2.5f : 3.5f);
            if (!steady)
            {
                FleckMaker.ThrowFireGlow(c, map, 2.5f);
                FleckMaker.ThrowHeatGlow(building.Position, map, 2f);
            }
            HP_DefOf.HP_SoulRelease?.PlayOneShot(new TargetInfo(building.Position, map));
        }
    }

    /// <summary>
    /// The press draws attention (4.27): each rite done there adds to it, it halves every five days,
    /// and raids on that map get up to 30% more points while it's high. One number per map; read only
    /// when a raid is sized.
    /// </summary>
    public class MapComponent_PressAttention : MapComponent
    {
        public float attention;
        public int lastTick;

        public MapComponent_PressAttention(Map map) : base(map)
        {
        }

        public float Current
        {
            get
            {
                int now = Find.TickManager.TicksGame;
                float days = (now - lastTick) / (float)GenDate.TicksPerDay;
                attention *= Mathf.Pow(0.5f, days / 5f);
                lastTick = now;
                return attention;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref attention, "attention");
            Scribe_Values.Look(ref lastTick, "lastTick");
        }
    }

    public static class PressAttention
    {
        public static void Add(Map map)
        {
            MapComponent_PressAttention c = map?.GetComponent<MapComponent_PressAttention>();
            if (c == null)
            {
                return;
            }
            bool first = c.Current < 0.5f;
            c.attention += 1f;
            if (first)
            {
                Messages.Message("The soul press's hum carries a long way. Raids will come heavier while it keeps running.", MessageTypeDefOf.CautionInput);
            }
        }

        public static float Factor(Map map)
        {
            MapComponent_PressAttention c = map?.GetComponent<MapComponent_PressAttention>();
            return c == null ? 1f : 1f + Mathf.Min(c.Current * 0.03f, 0.3f);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(StorytellerUtility), nameof(StorytellerUtility.DefaultThreatPointsNow))]
    public static class Patch_DefaultThreatPointsNow_PressAttention
    {
        public static void Postfix(IIncidentTarget target, ref float __result)
        {
            if (target is Map map && map.IsPlayerHome)
            {
                __result *= PressAttention.Factor(map);
            }
        }
    }
}
