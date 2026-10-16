using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    public class CompProperties_DreadnoughtCore : CompProperties
    {
        public float capacity = 60f;
        public float burnPerDay = 8f;

        public CompProperties_DreadnoughtCore()
        {
            compClass = typeof(CompDreadnoughtCore);
        }
    }

    /// <summary>
    /// A soul dreadnought's core (4.9): it runs on essence, not mech energy. Burns about eight a day;
    /// haulers top it up; empty, it goes dark (shut down) until fed. Checked every in-game hour on the
    /// dreadnought itself.
    /// </summary>
    public class CompDreadnoughtCore : ThingComp
    {
        public float fuel;

        public CompProperties_DreadnoughtCore Props => (CompProperties_DreadnoughtCore)props;

        private Pawn Mech => parent as Pawn;

        public float Missing => Props.capacity - fuel;

        public bool WantsFuel => fuel < Props.capacity * 0.5f;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!respawningAfterLoad && fuel <= 0f)
            {
                fuel = Props.capacity;
            }
        }

        public override void CompTick()
        {
            Pawn p = Mech;
            if (p == null || p.Dead || !p.IsHashIntervalTick(GenDate.TicksPerHour))
            {
                return;
            }
            fuel = Mathf.Max(0f, fuel - Props.burnPerDay / 24f);
            Hediff dark = p.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_CoreDark);
            if (fuel <= 0f && dark == null)
            {
                p.health.AddHediff(HP_DefOf.HP_CoreDark);
                Messages.Message(p.LabelShort + "'s soul core has burned out. It has shut down until it's fed essence.", p, MessageTypeDefOf.NegativeEvent);
            }
            else if (fuel > 0f && dark != null)
            {
                p.health.RemoveHediff(dark);
            }
        }

        public void AddFuel(int essence)
        {
            fuel = Mathf.Min(Props.capacity, fuel + essence);
            Hediff dark = Mech?.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_CoreDark);
            if (dark != null && fuel > 0f)
            {
                Mech.health.RemoveHediff(dark);
            }
        }

        public override string CompInspectStringExtra() => "Soul core: " + fuel.ToString("0") + " / " + Props.capacity.ToString("0") + " essence (burns " + Props.burnPerDay.ToString("0") + " a day).";

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref fuel, "fuel");
        }
    }

    /// <summary>Dreadnoughts have no mech energy need: the core is their power.</summary>
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Patch_NeedsTracker_DreadnoughtEnergy
    {
        public static void Postfix(NeedDef nd, Pawn ___pawn, ref bool __result)
        {
            if (__result && nd.defName == "MechEnergy" && ___pawn.TryGetComp<CompDreadnoughtCore>() != null)
            {
                __result = false;
            }
        }
    }

    /// <summary>Haulers bring essence to a dreadnought whose core is under half.</summary>
    public class WorkGiver_FeedDreadnought : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) =>
            pawn.Map.mapPawns.SpawnedColonyMechs.Where(m => m.TryGetComp<CompDreadnoughtCore>()?.WantsFuel == true).Cast<Thing>();

        public override bool ShouldSkip(Pawn pawn, bool forced = false) => !PotentialWorkThingsGlobal(pawn).Any();

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompDreadnoughtCore core = t.TryGetComp<CompDreadnoughtCore>();
            if (core == null || (!core.WantsFuel && !forced) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Some, 1, -1, null, forced))
            {
                return null;
            }
            Thing essence = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(HP_DefOf.HP_SoulEssence), PathEndMode.ClosestTouch,
                TraverseParms.For(pawn), 9999f, x => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, forced));
            if (essence == null)
            {
                JobFailReason.Is("No soul essence.");
                return null;
            }
            Job job = JobMaker.MakeJob(HP_DefOf.HP_FeedDreadnought, t, essence);
            job.count = Mathf.Min(essence.stackCount, Mathf.CeilToInt(core.Missing));
            return job;
        }
    }

    public class JobDriver_FeedDreadnought : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) && pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(150, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            Toil feed = ToilMaker.MakeToil("HP_FeedCore");
            feed.initAction = () =>
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                CompDreadnoughtCore core = job.targetA.Thing.TryGetComp<CompDreadnoughtCore>();
                if (carried == null || core == null)
                {
                    return;
                }
                int n = Mathf.Min(carried.stackCount, Mathf.CeilToInt(core.Missing));
                core.AddFuel(n);
                if (n >= carried.stackCount)
                {
                    pawn.carryTracker.DestroyCarriedThing();
                }
                else
                {
                    carried.SplitOff(n).Destroy();
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
                }
            };
            feed.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return feed;
        }
    }
}
