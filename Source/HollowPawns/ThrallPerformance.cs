using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    /// <summary>
    /// Thrall performance (4.26): a lean think tree, muster spots to stand down at, and racks that hold
    /// thralls in suspended animation. The aim is a big base full of thralls that costs far less to
    /// simulate than the same number of colonists.
    /// </summary>
    public static class Thralls
    {
        /// <summary>A soulless pawn of the player's: a thrall or a husk.</summary>
        public static bool IsThrall(Pawn p) => p?.Faction != null && p.Faction.IsPlayer && p.RaceProps.Humanlike && SoulUtility.IsSoulless(p);
    }

    /// <summary>
    /// Thralls think with HP_Thrall instead of the full humanlike tree: no recreation, social, styling,
    /// inventory stocking or wandering for joy. Downed, burning, drafted, lord duties, food, rest, work
    /// and dressing stay; idle thralls stand down at a muster spot. Off in mod settings if it misbehaves.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_Thinker), nameof(Pawn_Thinker.MainThinkTree), MethodType.Getter)]
    public static class Patch_Pawn_Thinker_ThrallTree
    {
        public static void Postfix(Pawn_Thinker __instance, ref ThinkTreeDef __result)
        {
            if (HollowPawnsMod.Settings.leanThrallBrain && HP_DefOf.HP_Thrall != null && __instance.pawn?.mutant == null && Thralls.IsThrall(__instance.pawn))
            {
                __result = HP_DefOf.HP_Thrall;
            }
        }
    }

    /// <summary>Idle thralls go and stand at the nearest muster spot, and stay put there: no wandering, no pathing.</summary>
    public class JobGiver_ThrallMuster : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Building spot = null;
            float best = float.MaxValue;
            foreach (Building b in pawn.Map.listerBuildings.AllBuildingsColonistOfDef(HP_DefOf.HP_ThrallMuster))
            {
                float d = b.Position.DistanceToSquared(pawn.Position);
                if (d < best && pawn.CanReach(b, PathEndMode.OnCell, Danger.Some))
                {
                    best = d;
                    spot = b;
                }
            }
            if (spot == null)
            {
                return null;
            }
            if (pawn.Position.InHorDistOf(spot.Position, 4f))
            {
                Job wait = JobMaker.MakeJob(JobDefOf.Wait, 1500);
                wait.checkOverrideOnExpire = true;
                return wait;
            }
            IntVec3 c = CellFinder.TryFindRandomCellNear(spot.Position, pawn.Map, 3, x => x.Standable(pawn.Map) && pawn.CanReach(x, PathEndMode.OnCell, Danger.Some), out IntVec3 found)
                ? found : spot.Position;
            return JobMaker.MakeJob(JobDefOf.Goto, c);
        }
    }

    // ======================= Thrall racks =======================

    /// <summary>
    /// A rack that holds six thralls in suspended animation: like a cryptosleep casket, nothing inside
    /// is ticked, so a hundred racked thralls cost about as much as a hundred crates (and their decay
    /// pauses). Rack idle thralls with one button, release them all with another (drafted, at the rack),
    /// or let the rack release them when a raid arrives.
    /// </summary>
    public class Building_ThrallRack : Building_Casket
    {
        public const int Capacity = 6;

        public bool releaseOnRaid = true;

        public int Count => innerContainer.Count;

        public override bool Accepts(Thing thing) => thing is Pawn p && Thralls.IsThrall(p) && innerContainer.Count < Capacity && base.Accepts(thing);

        public void ReleaseAll(bool draft)
        {
            List<Pawn> out_ = innerContainer.OfType<Pawn>().ToList();
            EjectContents();
            foreach (Pawn p in out_)
            {
                if (draft && p.Spawned && p.drafter != null && !p.Downed)
                {
                    p.drafter.Drafted = true;
                }
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
            var rack = new Command_Action
            {
                defaultLabel = "Rack idle thralls",
                defaultDesc = "Send undrafted thralls on this map that aren't busy fighting to climb into this rack, up to six.",
                icon = ContentFinder<Texture2D>.Get("Things/Building/HP_ThrallRack"),
                action = RackIdle,
            };
            if (Count >= Capacity)
            {
                rack.Disable("The rack is full.");
            }
            yield return rack;
            var release = new Command_Action
            {
                defaultLabel = "Release and draft",
                defaultDesc = "Open the rack: every thrall steps out, drafted, ready to fight.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject"),
                action = () => ReleaseAll(true),
            };
            if (Count == 0)
            {
                release.Disable("The rack is empty.");
            }
            yield return release;
            yield return new Command_Toggle
            {
                defaultLabel = "Release on raid",
                defaultDesc = "Open the rack and draft everyone in it when a raid arrives on this map.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject"),
                isActive = () => releaseOnRaid,
                toggleAction = () => releaseOnRaid = !releaseOnRaid,
            };
        }

        private void RackIdle()
        {
            int room = Capacity - Count, sent = 0;
            foreach (Pawn p in Map.mapPawns.FreeColonistsSpawned.Where(Thralls.IsThrall).ToList())
            {
                if (sent >= room)
                {
                    break;
                }
                if (p.Drafted || p.Downed || p.InMentalState || !p.CanReach(this, PathEndMode.InteractionCell, Danger.Some))
                {
                    continue;
                }
                p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(HP_DefOf.HP_EnterThrallRack, this), JobTag.Misc);
                sent++;
            }
            Messages.Message(sent == 0 ? "No idle thralls to rack." : sent + " thrall" + (sent == 1 ? "" : "s") + " sent to the rack.", this, MessageTypeDefOf.TaskCompletion, false);
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            string line = "Racked: " + Count + " / " + Capacity + (releaseOnRaid ? ". Opens when a raid arrives." : ".");
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref releaseOnRaid, "releaseOnRaid", true);
        }
    }

    public class JobDriver_EnterThrallRack : JobDriver
    {
        private Building_ThrallRack Rack => (Building_ThrallRack)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Rack.Accepts(pawn));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(120, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            Toil enter = ToilMaker.MakeToil("HP_EnterRack");
            enter.initAction = () =>
            {
                Building_ThrallRack rack = Rack;
                bool wasSelected = pawn.DeSpawnOrDeselect();
                if (!rack.TryAcceptThing(pawn))
                {
                    GenSpawn.Spawn(pawn, rack.InteractionCell, rack.Map);
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }

    /// <summary>Racks set to release on raid open when a raid lands on their map.</summary>
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_IncidentWorker_RaidEnemy_ThrallRacks
    {
        public static void Postfix(IncidentParms parms, bool __result)
        {
            if (!__result || !(parms?.target is Map map))
            {
                return;
            }
            int released = 0;
            foreach (Building_ThrallRack rack in map.listerBuildings.AllBuildingsColonistOfClass<Building_ThrallRack>().ToList())
            {
                if (rack.releaseOnRaid && rack.Count > 0)
                {
                    released += rack.Count;
                    rack.ReleaseAll(true);
                }
            }
            if (released > 0)
            {
                Messages.Message("The thrall racks opened: " + released + " thralls stepped out, drafted.", MessageTypeDefOf.ThreatBig);
            }
        }
    }

    /// <summary>For the benchmark in ROADMAP 4.26: spawn 20 thralls at the mouse (Biotech).</summary>
    public static class ThrallBenchmark
    {
        [DebugAction("Hollow Pawns", "Spawn 20 thralls (benchmark)", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Spawn20()
        {
            if (HP_DefOf.HP_ThrallKind == null)
            {
                Messages.Message("Thralls need Biotech.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            Map map = Find.CurrentMap;
            IntVec3 at = UI.MouseCell();
            for (int i = 0; i < 20; i++)
            {
                var request = new PawnGenerationRequest(HP_DefOf.HP_ThrallKind, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, allowAddictions: false,
                    forceNoIdeo: true, fixedBiologicalAge: 20f, fixedChronologicalAge: 0f, forcedXenotype: HP_DefOf.HP_ThrallXenotype,
                    developmentalStages: DevelopmentalStage.Adult);
                Pawn t = PawnGenerator.GeneratePawn(request);
                t.Name = new NameSingle("Thrall " + Rand.RangeInclusive(100, 999));
                IntVec3 c = CellFinder.TryFindRandomCellNear(at, map, 6, x => x.Standable(map), out IntVec3 found) ? found : at;
                GenSpawn.Spawn(t, c, map);
            }
        }
    }
}
