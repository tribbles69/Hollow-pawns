using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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

        public CompProperties_SoulRite()
        {
            compClass = typeof(CompSoulRite);
        }
    }

    /// <summary>The Bone Altar and the Soul Press (4.27). Holds no state; the warden's job does the work.</summary>
    public class CompSoulRite : ThingComp
    {
        public CompProperties_SoulRite Props => (CompProperties_SoulRite)props;

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
            return marked > 0 ? marked + " prisoner" + (marked == 1 ? "" : "s") + " marked. Wardens will bring them." : null;
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
            return !pawn.Map.mapPawns.PrisonersOfColonySpawned.Any(SoulUtility.IsMarked);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => MakeJob(pawn, t, forced) != null;

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) => MakeJob(pawn, t, forced);

        private static Job MakeJob(Pawn pawn, Thing t, bool forced)
        {
            CompSoulRite rite = t.TryGetComp<CompSoulRite>();
            if (rite == null || !rite.Usable || t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            foreach (Pawn victim in pawn.Map.mapPawns.PrisonersOfColonySpawned)
            {
                SoulFate fate = SoulUtility.GetFate(victim);
                if (fate == SoulFate.None || !SoulUtility.CanMark(victim)
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

        /// <summary>The end of the rite: take the soul, render or bind it, and kill the victim.</summary>
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
            ThingDef essence = SoulUtility.EssenceDefAt(victim, cell, map);

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
                TaleRecorder.RecordTale(HP_DefOf.HP_SoulBound, warden, victim);
                GenPlace.TryPlaceThing(filled, building.InteractionCell, map, ThingPlaceMode.Near);
            }
            else
            {
                SoulUtility.DropEssence(building.InteractionCell, map, SoulGrades.RenderYield(grade), essence);
            }

            Messages.Message(warden.LabelShort + " took the " + grade.Label() + " soul of " + victim.LabelShort
                + (fate == SoulFate.Bind ? " and bound it." : " and rendered it.") + (botched ? " The rite was botched." : ""),
                new TargetInfo(cell, map), MessageTypeDefOf.NeutralEvent);

            Corruption.Spill(cell, map, props.corruption, props.corruptionRadius);
            Corruption.AddToPawn(warden, 0.06f);

            if (props.upsetsWitnesses)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (p != warden && p.Position.InHorDistOf(cell, 12f) && GenSight.LineOfSight(p.Position, cell, map)
                        && p.needs?.mood != null)
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

            // The rite.
            CompProperties_SoulRite props = Building.TryGetComp<CompSoulRite>().Props;
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
