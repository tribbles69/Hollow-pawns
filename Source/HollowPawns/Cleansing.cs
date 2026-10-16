using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    /// <summary>"Cleanse corruption" order: drag over corrupted ground for pawns to sprinkle holy water on it.</summary>
    public class Designator_Cleanse : Designator
    {
        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

        protected override DesignationDef Designation => HP_DefOf.HP_Cleanse;

        public Designator_Cleanse()
        {
            defaultLabel = "Cleanse corruption";
            defaultDesc = "Mark corrupted ground to be cleansed with holy water. Each sprinkle uses one holy water and cleans a small patch.";
            icon = ContentFinder<Texture2D>.Get("UI/Designators/HP_Cleanse");
            useMouseIcon = true;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            soundSucceeded = SoundDefOf.Designate_Claim;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map) || c.Fogged(Map))
            {
                return false;
            }
            if (Map.designationManager.DesignationAt(c, Designation) != null)
            {
                return false;
            }
            return Corruption.At(c, Map) > 0 ? AcceptanceReport.WasAccepted : "Not corrupted.";
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Map.designationManager.AddDesignation(new Designation(c, Designation));
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
            Corruption.PeekOverlay();
        }
    }

    /// <summary>Finds cells marked for cleansing and sends a pawn with holy water.</summary>
    public class WorkGiver_Cleanse : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            foreach (Designation d in pawn.Map.designationManager.SpawnedDesignationsOfDef(HP_DefOf.HP_Cleanse))
            {
                yield return d.target.Cell;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
            !pawn.Map.designationManager.AnySpawnedDesignationOfDef(HP_DefOf.HP_Cleanse);

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false) => MakeJob(pawn, c, forced) != null;

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false) => MakeJob(pawn, c, forced);

        private static Job MakeJob(Pawn pawn, IntVec3 c, bool forced)
        {
            if (pawn.Map.designationManager.DesignationAt(c, HP_DefOf.HP_Cleanse) == null
                || !pawn.CanReserveAndReach(c, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            Thing water = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(HP_DefOf.HP_HolyWater),
                PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, t => !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, 1, null, forced));
            if (water == null)
            {
                JobFailReason.Is("No holy water.");
                return null;
            }
            Job job = JobMaker.MakeJob(HP_DefOf.HP_CleanseCorruption, c, water);
            job.count = 1;
            return job;
        }
    }

    /// <summary>Fetch one holy water, walk to the cell, sprinkle: cleans a small patch and clears the order there.</summary>
    public class JobDriver_Cleanse : JobDriver
    {
        public const int SprinkleTicks = 180;
        public const int CleanseAmount = -90;
        public const float Radius = 1.9f;

        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) && pawn.Reserve(job.targetB, job, 1, 1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Map.designationManager.DesignationAt(job.targetA.Cell, HP_DefOf.HP_Cleanse) == null);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
            Toil sprinkle = Toils_General.Wait(SprinkleTicks, TargetIndex.A);
            sprinkle.WithProgressBarToilDelay(TargetIndex.A);
            yield return sprinkle;
            Toil finish = ToilMaker.MakeToil("HP_FinishCleanse");
            finish.initAction = () =>
            {
                IntVec3 c = job.targetA.Cell;
                Corruption.Spill(c, Map, CleanseAmount, Radius);
                foreach (IntVec3 near in GenRadial.RadialCellsAround(c, Radius, true))
                {
                    if (near.InBounds(Map) && Corruption.At(near, Map) == 0)
                    {
                        Designation d = Map.designationManager.DesignationAt(near, HP_DefOf.HP_Cleanse);
                        if (d != null)
                        {
                            Map.designationManager.RemoveDesignation(d);
                        }
                    }
                }
                Designation here = Map.designationManager.DesignationAt(c, HP_DefOf.HP_Cleanse);
                if (here != null && Corruption.At(c, Map) == 0)
                {
                    Map.designationManager.RemoveDesignation(here);
                }
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried != null)
                {
                    if (carried.stackCount > 1)
                    {
                        carried.SplitOff(1).Destroy();
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
                    }
                    else
                    {
                        pawn.carryTracker.DestroyCarriedThing();
                    }
                }
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }

    /// <summary>Matches the corpses of children. Disallowed in every recipe that uses corpses: children are never harvested.</summary>
    public class SpecialThingFilterWorker_ChildCorpse : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t) =>
            t is Corpse corpse && corpse.InnerPawn != null && corpse.InnerPawn.RaceProps.Humanlike && !corpse.InnerPawn.DevelopmentalStage.Adult();

        public override bool CanEverMatch(ThingDef def) => def.IsCorpse;
    }
}
