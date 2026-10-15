using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    /// <summary>
    /// Longevity (4.10): essence buys back years. The fountain of youth is a quick bath; the soul cradle a
    /// three-day cycle. Both run only when used (a job, or the cradle's rare tick while occupied).
    /// </summary>
    public static class Longevity
    {
        /// <summary>No one comes out of either younger than this. Souls are for adults.</summary>
        public const float MinAgeYears = 18f;

        public static bool CanTreat(Pawn p) => p != null && p.RaceProps.Humanlike && SoulUtility.HasSoul(p) && p.ageTracker.Adult;

        /// <summary>Takes years off, never below 18, and quiets an Ideology age-reversal demand.</summary>
        public static float Rejuvenate(Pawn p, float years)
        {
            float floor = Mathf.Max(MinAgeYears, p.ageTracker.AdultMinAge);
            float now = p.ageTracker.AgeBiologicalYearsFloat;
            float take = Mathf.Clamp(years, 0f, Mathf.Max(0f, now - floor));
            if (take > 0f)
            {
                p.ageTracker.AgeBiologicalTicks -= (long)(take * GenDate.TicksPerYear);
                if (ModsConfig.IdeologyActive)
                {
                    p.ageTracker.ResetAgeReversalDemand(Pawn_AgeTracker.AgeReversalReason.ViaTreatment);
                }
            }
            return take;
        }

        public static List<Hediff> Chronic(Pawn p) => p.health.hediffSet.hediffs.Where(h => h.def.chronic && h.Visible).ToList();

        public static string Cure(Pawn p, Hediff h)
        {
            string label = h.LabelBaseCap;
            p.health.RemoveHediff(h);
            return label;
        }
    }

    // ======================= Fountain of youth =======================

    public class CompProperties_Fountain : CompProperties
    {
        public float essencePerBath = 5f;
        public FloatRange yearsPerBath = new FloatRange(1f, 2f);
        public float cureChance = 0.25f;

        public CompProperties_Fountain()
        {
            compClass = typeof(CompFountain);
        }
    }

    /// <summary>
    /// The fountain of youth: right-click it with an adult colonist to bathe. Uses essence from the
    /// fountain's tank, takes a year or two, sometimes cures an old-age condition, and leaves a little
    /// corruption in the bather. One bath per pawn every five days (the "recently bathed" hediff).
    /// </summary>
    public class CompFountain : ThingComp
    {
        public CompProperties_Fountain Props => (CompProperties_Fountain)props;

        private CompRefuelable Fuel => parent.TryGetComp<CompRefuelable>();

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.IsColonistPlayerControlled)
            {
                yield break;
            }
            string label = "Bathe in the fountain";
            if (!Longevity.CanTreat(selPawn))
            {
                yield return new FloatMenuOption(label + " (adults with a soul only)", null);
            }
            else if (selPawn.health.hediffSet.HasHediff(HP_DefOf.HP_RecentlyBathed))
            {
                yield return new FloatMenuOption(label + " (bathed too recently)", null);
            }
            else if (Fuel != null && Fuel.Fuel < Props.essencePerBath)
            {
                yield return new FloatMenuOption(label + " (needs " + Props.essencePerBath + " essence)", null);
            }
            else if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption(label + " (no path)", null);
            }
            else
            {
                yield return new FloatMenuOption(label, () => selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(HP_DefOf.HP_BatheInFountain, parent), JobTag.Misc));
            }
        }

        public void Bathe(Pawn p)
        {
            CompRefuelable fuel = Fuel;
            if (fuel != null)
            {
                if (fuel.Fuel < Props.essencePerBath)
                {
                    return;
                }
                fuel.ConsumeFuel(Props.essencePerBath);
            }
            float took = Longevity.Rejuvenate(p, Props.yearsPerBath.RandomInRange);
            string cured = null;
            List<Hediff> chronic = Longevity.Chronic(p);
            if (chronic.Count > 0 && Rand.Chance(Props.cureChance))
            {
                cured = Longevity.Cure(p, chronic.RandomElement());
            }
            p.health.AddHediff(HP_DefOf.HP_RecentlyBathed);
            Corruption.AddToPawn(p, 0.02f);
            Messages.Message(p.LabelShort + " bathed in the fountain" + (took > 0.05f ? " and is " + took.ToString("0.#") + " years younger" : ", but can grow no younger")
                + (cured != null ? ". " + cured + " is gone." : "."), p, MessageTypeDefOf.PositiveEvent);
        }

        public override string CompInspectStringExtra() => Props.essencePerBath.ToString("0") + " essence a bath.";
    }

    public class JobDriver_BatheInFountain : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(600, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            Toil done = ToilMaker.MakeToil("HP_Bathe");
            done.initAction = () => job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompFountain>()?.Bathe(pawn);
            done.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return done;
        }
    }

    // ======================= Soul cradle =======================

    /// <summary>
    /// The soul cradle: a sealed pod. An adult climbs in; with 30 essence loaded and power on it runs a
    /// three-day cycle (the occupant isn't ticked, like cryptosleep), then takes 5-8 years off, removes
    /// every chronic condition and regrows one lost part. Rare tick on the cradle only.
    /// </summary>
    public class Building_SoulCradle : Building_Casket
    {
        public const int CycleTicks = GenDate.TicksPerDay * 3;
        public const float EssencePerCycle = 30f;

        private int ticksLeft = -1;

        public Pawn Occupant => ContainedThing as Pawn;

        public bool Running => ticksLeft > 0;

        private CompPowerTrader Power => GetComp<CompPowerTrader>();

        private CompRefuelable Fuel => GetComp<CompRefuelable>();

        public override bool Accepts(Thing thing) => thing is Pawn p && !HasAnyContents && Longevity.CanTreat(p) && base.Accepts(thing);

        public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!base.TryAcceptThing(thing, allowSpecialEffects))
            {
                return false;
            }
            ticksLeft = -1;
            TryStart();
            return true;
        }

        private void TryStart()
        {
            if (Occupant != null && !Running && Fuel != null && Fuel.Fuel >= EssencePerCycle)
            {
                Fuel.ConsumeFuel(EssencePerCycle);
                ticksLeft = CycleTicks;
            }
        }

        public override void TickRare()
        {
            base.TickRare();
            if (Occupant == null)
            {
                ticksLeft = -1;
                return;
            }
            if (!Running)
            {
                TryStart();
                return;
            }
            if (Power == null || Power.PowerOn)
            {
                ticksLeft -= GenTicks.TickRareInterval;
                if (ticksLeft <= 0)
                {
                    Finish();
                }
            }
        }

        private void Finish()
        {
            Pawn p = Occupant;
            ticksLeft = -1;
            float took = Longevity.Rejuvenate(p, Rand.Range(5f, 8f));
            List<string> cured = Longevity.Chronic(p).Select(h => Longevity.Cure(p, h)).ToList();
            string regrown = null;
            Hediff_MissingPart missing = p.health.hediffSet.GetMissingPartsCommonAncestors()
                .Where(h => h.Part != null && h.Part.def != HP_DefOf.HP_Soul).RandomElementWithFallback();
            if (missing != null)
            {
                regrown = missing.Part.LabelCap;
                p.health.RestorePart(missing.Part);
            }
            Corruption.AddToPawn(p, 0.05f);
            EjectContents();
            string msg = p.LabelShort + "'s cycle in the soul cradle is done: " + took.ToString("0.#") + " years younger";
            if (cured.Count > 0)
            {
                msg += ", cured of " + cured.ToCommaList(useAnd: true);
            }
            if (regrown != null)
            {
                msg += ", " + regrown.ToLower() + " regrown";
            }
            Messages.Message(msg + ".", p, MessageTypeDefOf.PositiveEvent);
        }

        public override void EjectContents()
        {
            ticksLeft = -1;
            base.EjectContents();
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(selPawn))
            {
                yield return o;
            }
            if (!selPawn.IsColonistPlayerControlled)
            {
                yield break;
            }
            string label = "Enter the soul cradle";
            if (HasAnyContents)
            {
                yield break;
            }
            if (!Longevity.CanTreat(selPawn))
            {
                yield return new FloatMenuOption(label + " (adults with a soul only)", null);
            }
            else if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption(label + " (no path)", null);
            }
            else
            {
                yield return new FloatMenuOption(label, () => selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(HP_DefOf.HP_EnterSoulCradle, this), JobTag.Misc));
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Faction != Faction.OfPlayer || Occupant == null)
            {
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = "Open",
                defaultDesc = "Let the occupant out now. A cycle in progress is lost, and so is its essence.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject"),
                action = EjectContents,
            };
            if (DebugSettings.ShowDevGizmos && Running)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: finish cycle",
                    action = () => ticksLeft = 1,
                };
            }
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            string line;
            if (Occupant == null)
            {
                line = "Empty.";
            }
            else if (Running)
            {
                line = "Cycle: " + ticksLeft.ToStringTicksToPeriod() + " left" + (Power != null && !Power.PowerOn ? " (paused, no power)." : ".");
            }
            else
            {
                line = "Waiting for " + EssencePerCycle.ToString("0") + " essence to start a cycle.";
            }
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
        }
    }

    public class JobDriver_EnterSoulCradle : JobDriver
    {
        private Building_SoulCradle Cradle => (Building_SoulCradle)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => Cradle.HasAnyContents);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(300, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            Toil enter = ToilMaker.MakeToil("HP_EnterCradle");
            enter.initAction = () =>
            {
                Building_SoulCradle cradle = Cradle;
                pawn.DeSpawnOrDeselect();
                if (!cradle.TryAcceptThing(pawn))
                {
                    GenSpawn.Spawn(pawn, cradle.InteractionCell, cradle.Map);
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }
}
