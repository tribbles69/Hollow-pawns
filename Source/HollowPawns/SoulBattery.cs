using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    /// <summary>
    /// The prisoner battery (4.7): a casket that holds one living adult prisoner and drains their soul
    /// for power. Like cryptosleep, nothing inside is ticked, so they need no food. After the drain
    /// runs out they die as a husk and leave essence. Rare tick on the battery only.
    /// </summary>
    public class Building_SoulBattery : Building_Casket
    {
        /// <summary>How long a soul lasts in the battery, whatever its grade.</summary>
        public const int DrainTicks = GenDate.TicksPerDay * 5;

        private int drainTicksLeft;
        private SoulGrade grade = SoulGrade.Common;

        public Pawn Occupant => ContainedThing as Pawn;

        /// <summary>Power while draining: 300 W from a wretched soul up to 900 W from an exalted one.</summary>
        public float Watts => Occupant == null ? 0f : 300f + 100f * (int)grade;

        public override bool Accepts(Thing thing)
        {
            return thing is Pawn p && !HasAnyContents && p.IsPrisonerOfColony && SoulUtility.CanExtract(p) && base.Accepts(thing);
        }

        public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!base.TryAcceptThing(thing, allowSpecialEffects))
            {
                return false;
            }
            Pawn p = (Pawn)thing;
            grade = SoulGrades.Of(p);
            drainTicksLeft = DrainTicks;
            SoulUtility.ClearFate(p);
            UpdatePower();
            return true;
        }

        public override void TickRare()
        {
            base.TickRare();
            Pawn p = Occupant;
            if (p == null)
            {
                return;
            }
            int before = drainTicksLeft;
            drainTicksLeft -= GenTicks.TickRareInterval;
            if (before / GenDate.TicksPerDay != drainTicksLeft / GenDate.TicksPerDay)
            {
                // A little corruption leaks out every day it runs.
                Corruption.Spill(Position, Map, 25, 2.9f);
            }
            if (drainTicksLeft <= 0)
            {
                Finish(p);
            }
            UpdatePower();
        }

        private void UpdatePower()
        {
            GetComp<CompPowerPlant_SoulBattery>()?.UpdateDesiredPowerOutput();
        }

        /// <summary>The soul is spent: out they come, dead, a husk, with what's left of the soul as essence.</summary>
        private void Finish(Pawn p)
        {
            Map map = Map;
            IntVec3 cell = InteractionCell;
            innerContainer.TryDrop(p, cell, map, ThingPlaceMode.Near, out Thing _);
            SoulUtility.DropEssence(cell, map, Mathf.Max(1, SoulGrades.RenderYield(grade) / 2), SoulUtility.EssenceDefAt(p, cell, map));
            Faction home = p.HomeFaction;
            if (home != null && home != Faction.OfPlayer && !home.HostileTo(Faction.OfPlayer))
            {
                home.TryAffectGoodwillWith(Faction.OfPlayer, -40, true, true, HistoryEventDefOf.MemberKilled);
            }
            Messages.Message("The prisoner battery has drained " + p.LabelShort + " dry.", new TargetInfo(cell, map), MessageTypeDefOf.NeutralEvent);
            SoulUtility.TakeSoulAndKill(p);
        }

        public override void EjectContents()
        {
            Pawn p = Occupant;
            base.EjectContents();
            if (p != null && !p.Dead)
            {
                // Pulled out early: alive, shaken, and the soul isn't what it was.
                HealthUtility.AdjustSeverity(p, HP_DefOf.HP_SoulShock, 0.6f);
            }
            UpdatePower();
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
                defaultLabel = "Release",
                defaultDesc = "Open the battery and let the prisoner out alive, badly soul-shocked.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject"),
                action = EjectContents,
            };
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: drain now",
                    action = () => drainTicksLeft = 1,
                };
            }
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            Pawn p = Occupant;
            string line = p == null
                ? "Empty. Mark a prisoner \"battery\" and a warden will lock them in."
                : "Draining " + p.LabelShort + " (" + grade.Label() + " soul): " + Watts.ToString("0") + " W, "
                    + Mathf.Max(0, drainTicksLeft).ToStringTicksToPeriod() + " left.";
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref drainTicksLeft, "drainTicksLeft");
            Scribe_Values.Look(ref grade, "grade", SoulGrade.Common);
        }
    }

    /// <summary>Power from the battery: its output follows the soul inside, updated on the battery's rare tick.</summary>
    public class CompPowerPlant_SoulBattery : CompPowerPlant
    {
        protected override float DesiredPowerOutput => (parent as Building_SoulBattery)?.Watts ?? 0f;
    }

    /// <summary>Wardens lock prisoners marked "battery" into an empty prisoner battery.</summary>
    public class WorkGiver_LoadSoulBattery : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_SoulBattery>().Where(b => !b.HasAnyContents).Cast<Thing>();
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.mapPawns.PrisonersOfColonySpawned.Any(p => SoulUtility.GetFate(p) == SoulFate.Battery);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_SoulBattery battery) || battery.HasAnyContents || t.IsForbidden(pawn)
                || !pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            foreach (Pawn victim in pawn.Map.mapPawns.PrisonersOfColonySpawned)
            {
                if (SoulUtility.GetFate(victim) == SoulFate.Battery && SoulUtility.CanMark(victim)
                    && pawn.CanReserveAndReach(victim, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
                {
                    Job job = JobMaker.MakeJob(HP_DefOf.HP_LoadSoulBattery, victim, t);
                    job.count = 1;
                    return job;
                }
            }
            return null;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => JobOnThing(pawn, t, forced) != null;
    }

    /// <summary>A = prisoner, B = battery. Restrain, carry over, lock in.</summary>
    public class JobDriver_LoadSoulBattery : JobDriver
    {
        private Pawn Victim => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        private Building_SoulBattery Battery => (Building_SoulBattery)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) && pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOn(() => SoulUtility.GetFate(Victim) != SoulFate.Battery || Battery.HasAnyContents);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil restrain = ToilMaker.MakeToil("HP_RestrainForBattery");
            restrain.initAction = () =>
            {
                if (!Victim.health.hediffSet.HasHediff(HP_DefOf.HP_RiteRestrained))
                {
                    Victim.health.AddHediff(HP_DefOf.HP_RiteRestrained);
                }
            };
            yield return restrain;
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            Toil lockIn = Toils_General.Wait(300, TargetIndex.B).WithProgressBarToilDelay(TargetIndex.B);
            yield return lockIn;
            Toil finish = ToilMaker.MakeToil("HP_LockInBattery");
            finish.initAction = () =>
            {
                Pawn victim = Victim;
                if (Battery.TryAcceptThing(victim))
                {
                    Messages.Message(pawn.LabelShort + " locked " + victim.LabelShort + " into the prisoner battery.", Battery, MessageTypeDefOf.NeutralEvent);
                }
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}
