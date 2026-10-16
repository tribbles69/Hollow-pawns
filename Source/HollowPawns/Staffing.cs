using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    /// <summary>
    /// A machine that runs on its own but goes faster with someone working it (corpse grinder, line
    /// forge). The operator's job pings the machine every second; the machine reads the speed on its
    /// own rare tick. Nothing new ticks.
    /// </summary>
    public interface IStaffable
    {
        /// <summary>Powered and with something to do, so an operator would make a difference.</summary>
        bool WantsOperator { get; }

        Staffing Staffing { get; }
    }

    /// <summary>Who's working a machine and how much faster that makes it. Not saved: it lapses within seconds.</summary>
    public class Staffing
    {
        public readonly SkillDef skill;
        private Pawn op;
        private int lastTick = -99999;
        private float factor = 1f;

        public Staffing(SkillDef skill)
        {
            this.skill = skill;
        }

        public Pawn Operator => Active ? op : null;

        private bool Active => op != null && op.Spawned && !op.Dead && Find.TickManager.TicksGame - lastTick < 150;

        /// <summary>
        /// Run speed: 1 on its own; with an operator, 1.5 at skill 0 up to 3 at skill 20, times their
        /// global work speed. Thralls with the skill count like anyone else.
        /// </summary>
        public float Factor => Active ? factor : 1f;

        public void Ping(Pawn p)
        {
            op = p;
            lastTick = Find.TickManager.TicksGame;
            int level = p.skills?.GetSkill(skill)?.Level ?? 0;
            factor = (1.5f + level * 0.075f) * Mathf.Max(0.1f, p.GetStatValue(StatDefOf.WorkSpeedGlobal));
        }

        public string InspectLine() => Operator == null
            ? "Unstaffed: running at base speed. Haulers (thralls too) work it when there's nothing to carry; " + skill.label + " skill makes it faster."
            : "Worked by " + Operator.LabelShort + ": x" + Factor.ToString("0.0") + " speed.";
    }

    /// <summary>Names the machine a staffing work giver sends pawns to.</summary>
    public class StaffingExtension : DefModExtension
    {
        public ThingDef building;
    }

    public class WorkGiver_StaffMachine : WorkGiver_Scanner
    {
        private ThingDef Building => def.GetModExtension<StaffingExtension>()?.building;

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) =>
            Building == null ? Enumerable.Empty<Thing>() : pawn.Map.listerBuildings.AllBuildingsColonistOfDef(Building).Cast<Thing>();

        public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
            Building == null || !pawn.Map.listerBuildings.AllBuildingsColonistOfDef(Building).Any(b => b is IStaffable s && s.WantsOperator);

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return t is IStaffable s && s.WantsOperator && s.Staffing.Operator == null && !t.IsForbidden(pawn) && !t.IsBurning()
                && pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Some, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) => JobMaker.MakeJob(HP_DefOf.HP_StaffMachine, t);
    }

    /// <summary>Stand at the machine and work it for half an hour, or until it runs out of work.</summary>
    public class JobDriver_StaffMachine : JobDriver
    {
        /// <summary>Half an hour, then back to the job list, so real hauling never waits long.</summary>
        private const int Shift = GenDate.TicksPerHour / 2;

        private IStaffable Machine => job.GetTarget(TargetIndex.A).Thing as IStaffable;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil work = ToilMaker.MakeToil("HP_StaffMachine");
            work.defaultCompleteMode = ToilCompleteMode.Delay;
            work.defaultDuration = Shift;
            work.FailOn(() => Machine == null || !Machine.WantsOperator);
            work.handlingFacing = true;
            work.tickAction = () =>
            {
                pawn.rotationTracker.FaceTarget(job.targetA);
                if (pawn.IsHashIntervalTick(60))
                {
                    Machine.Staffing.Ping(pawn);
                }
                pawn.skills?.Learn(Machine.Staffing.skill, 0.05f);
            };
            work.WithProgressBarToilDelay(TargetIndex.A);
            yield return work;
        }
    }
}
