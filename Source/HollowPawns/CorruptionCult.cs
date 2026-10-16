using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    // ======================= Where a pawn stands on corruption (4.28) =======================

    public enum CorruptionStance : byte { None, Abhorrent, Disliked, Exalted }

    /// <summary>
    /// How a pawn feels about corruption. With Ideology, their ideoligion's corruption precept decides.
    /// Without it (or with no precept), a pawn deep in corruption themselves (soul corruption 50%+) is
    /// attuned: they stop minding corrupted ground and draw strength from it. Purists are everyone else.
    /// </summary>
    public static class CorruptionCult
    {
        private static PreceptDef abhorrent, disliked, exalted;
        private static bool looked;

        private static void Look()
        {
            if (looked)
            {
                return;
            }
            looked = true;
            abhorrent = DefDatabase<PreceptDef>.GetNamedSilentFail("HP_Corruption_Abhorrent");
            disliked = DefDatabase<PreceptDef>.GetNamedSilentFail("HP_Corruption_Disliked");
            exalted = DefDatabase<PreceptDef>.GetNamedSilentFail("HP_Corruption_Exalted");
        }

        public static CorruptionStance Stance(Pawn p)
        {
            Look();
            Ideo ideo = ModsConfig.IdeologyActive ? p?.Ideo : null;
            if (ideo != null)
            {
                if (exalted != null && ideo.HasPrecept(exalted)) return CorruptionStance.Exalted;
                if (abhorrent != null && ideo.HasPrecept(abhorrent)) return CorruptionStance.Abhorrent;
                if (disliked != null && ideo.HasPrecept(disliked)) return CorruptionStance.Disliked;
            }
            return CorruptionStance.None;
        }

        /// <summary>Cult-side: exalted by their ideoligion, or so corrupted themselves that it feels like home.</summary>
        public static bool Attuned(Pawn p)
        {
            if (p == null || SoulUtility.GetSoul(p) == null)
            {
                return false;
            }
            if (Stance(p) == CorruptionStance.Exalted)
            {
                return true;
            }
            Hediff h = p.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_SoulCorruption);
            return h != null && h.Severity >= 0.5f;
        }

        public static void Record(HistoryEventDef def, Pawn doer)
        {
            if (def == null || !ModsConfig.IdeologyActive)
            {
                return;
            }
            Find.HistoryEventsManager.RecordEvent(doer != null ? new HistoryEvent(def, doer.Named(HistoryEventArgsNames.Doer)) : new HistoryEvent(def));
        }

        /// <summary>Called on the corruption pawn pass: attuned pawns standing on corrupted ground draw on it.</summary>
        public static void AttunedPass(Pawn p, byte ground)
        {
            if (ground < 80 || !Attuned(p))
            {
                return;
            }
            Hediff h = p.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_CorruptionAttuned);
            if (h == null)
            {
                p.health.AddHediff(HP_DefOf.HP_CorruptionAttuned);
            }
            else
            {
                h.Severity = 1f;
            }
        }
    }

    /// <summary>Precept mood for corrupted ground: the stage by how bad the ground is, the sign by the precept's thought def.</summary>
    public class ThoughtWorker_Precept_CorruptedGround : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (!p.Spawned || SoulUtility.GetSoul(p) == null)
            {
                return ThoughtState.Inactive;
            }
            byte v = Corruption.At(p.Position, p.Map);
            return v >= 150 ? ThoughtState.ActiveAtStage(1) : v >= 50 ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }

    // ======================= Corrupted hits =======================

    /// <summary>
    /// A hit from a corrupted attacker (a demon, a Profaned) or a weapon with a corrupted soul bound in
    /// it stains the victim's soul and the ground they stand on. Runs only when damage lands.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_Pawn_PostApplyDamage_CorruptedHit
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (totalDamageDealt <= 0f || !__instance.Spawned || !(dinfo.Instigator is Pawn attacker) || attacker == __instance)
            {
                return;
            }
            bool corrupt = SoulUtility.IsCorrupted(attacker);
            if (!corrupt)
            {
                CompSoulBound bound = attacker.equipment?.Primary?.TryGetComp<CompSoulBound>();
                corrupt = bound?.soul != null && bound.soul.corrupted;
            }
            if (corrupt)
            {
                Corruption.AddToPawn(__instance, 0.015f);
                Corruption.Spill(__instance.Position, __instance.Map, 4, 0.9f);
            }
        }
    }

    // ======================= The cleansing rite =======================

    /// <summary>"Cleansing rite" order: one spot. A pawn brings five holy water and spends an hour cleansing everything around it.</summary>
    public class Designator_CleansingRite : Designator
    {
        protected override DesignationDef Designation => HP_DefOf.HP_CleansingRite;

        public Designator_CleansingRite()
        {
            defaultLabel = "Cleansing rite";
            defaultDesc = "Mark a spot for a cleansing rite. A pawn brings five holy water and spends an hour there: corruption within about 7 cells is scoured away, everyone in it has their soul corruption eased and any grave chill lifted, and any ghost inside is laid to rest.";
            icon = ContentFinder<Texture2D>.Get("UI/Designators/HP_Cleanse");
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Claim;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map) || c.Fogged(Map) || !c.Standable(Map))
            {
                return false;
            }
            return Map.designationManager.DesignationAt(c, Designation) == null;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Map.designationManager.AddDesignation(new Designation(c, Designation));
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
            Corruption.ShowOverlay = true;
            IntVec3 c = UI.MouseCell();
            if (c.InBounds(Map))
            {
                GenDraw.DrawRadiusRing(c, CleansingRite.Radius);
            }
        }
    }

    public static class CleansingRite
    {
        public const float Radius = 6.9f;
        public const int HolyWater = 5;
        public const int Ticks = GenDate.TicksPerHour;

        public static void Complete(Pawn priest, IntVec3 c, Map map)
        {
            Corruption.Spill(c, map, -150, Radius);
            int ghosts = 0;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned.ToList())
            {
                if (!p.Position.InHorDistOf(c, Radius))
                {
                    continue;
                }
                if (Ghosts.IsGhost(p))
                {
                    p.TryGetComp<CompGhost>()?.Fade("a cleansing rite laid it to rest");
                    ghosts++;
                    continue;
                }
                Corruption.Cleanse(p, 0.3f);
                Hediff chill = p.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_GraveChill);
                if (chill != null)
                {
                    p.health.RemoveHediff(chill);
                }
            }
            FleckMaker.Static(c.ToVector3Shifted(), map, FleckDefOf.PsycastAreaEffect, Radius);
            CorruptionCult.Record(HP_DefOf.HP_CleansingRiteDone, priest);
            Messages.Message(priest.LabelShort + " finished a cleansing rite." + (ghosts > 0 ? " A ghost was laid to rest." : ""), new TargetInfo(c, map), MessageTypeDefOf.PositiveEvent);
        }
    }

    public class WorkGiver_CleansingRite : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.OnCell;

        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            foreach (Designation d in pawn.Map.designationManager.SpawnedDesignationsOfDef(HP_DefOf.HP_CleansingRite))
            {
                yield return d.target.Cell;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false) => !pawn.Map.designationManager.AnySpawnedDesignationOfDef(HP_DefOf.HP_CleansingRite);

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false) => MakeJob(pawn, c, forced) != null;

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false) => MakeJob(pawn, c, forced);

        private static Job MakeJob(Pawn pawn, IntVec3 c, bool forced)
        {
            if (pawn.Map.designationManager.DesignationAt(c, HP_DefOf.HP_CleansingRite) == null
                || !pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            Thing water = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(HP_DefOf.HP_HolyWater),
                PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f,
                t => !t.IsForbidden(pawn) && t.stackCount >= CleansingRite.HolyWater && pawn.CanReserve(t, 1, CleansingRite.HolyWater, null, forced));
            if (water == null)
            {
                JobFailReason.Is("Needs a stack of " + CleansingRite.HolyWater + " holy water.");
                return null;
            }
            Job job = JobMaker.MakeJob(HP_DefOf.HP_PerformCleansingRite, c, water);
            job.count = CleansingRite.HolyWater;
            return job;
        }
    }

    public class JobDriver_CleansingRite : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) && pawn.Reserve(job.targetB, job, 1, CleansingRite.HolyWater, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Map.designationManager.DesignationAt(job.targetA.Cell, HP_DefOf.HP_CleansingRite) == null);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Toil chant = Toils_General.Wait(CleansingRite.Ticks, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            chant.WithEffect(EffecterDefOf.Clean, TargetIndex.A);
            yield return chant;
            Toil finish = ToilMaker.MakeToil("HP_FinishCleansingRite");
            finish.initAction = () =>
            {
                IntVec3 c = job.targetA.Cell;
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.stackCount < CleansingRite.HolyWater)
                {
                    return;
                }
                if (carried.stackCount > CleansingRite.HolyWater)
                {
                    carried.SplitOff(CleansingRite.HolyWater).Destroy();
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
                }
                else
                {
                    pawn.carryTracker.DestroyCarriedThing();
                }
                Designation d = Map.designationManager.DesignationAt(c, HP_DefOf.HP_CleansingRite);
                if (d != null)
                {
                    Map.designationManager.RemoveDesignation(d);
                }
                CleansingRite.Complete(pawn, c, Map);
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }

    // ======================= Zealots: the mirror enemy =======================

    /// <summary>
    /// Zealot raids (4.28): a hidden faction that comes only for a badly corrupted colony, with fire and
    /// holy water. Their aura cleanses the ground they walk on (on the corruption pawn pass).
    /// </summary>
    public class IncidentWorker_ZealotRaid : IncidentWorker_RaidEnemy
    {
        /// <summary>Cells at the spread level (80+) before the zealots notice a map.</summary>
        public const int CorruptedCellsToNotice = 400;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && Zealots.Faction != null && Corruption.For(map)?.CountAtLeast(MapComponent_Corruption.SpreadLevel) >= CorruptedCellsToNotice
                && base.CanFireNowSub(parms);
        }

        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            parms.faction = Zealots.Faction;
            return parms.faction != null;
        }
    }

    public static class Zealots
    {
        public static Faction Faction => HP_DefOf.HP_Zealots == null ? null : Find.FactionManager?.FirstFactionOfDef(HP_DefOf.HP_Zealots);

        public static bool IsZealot(Pawn p) => p.Faction != null && p.Faction.def == HP_DefOf.HP_Zealots;
    }

    /// <summary>Adds the zealots to saves made before they existed.</summary>
    public class GameComponent_Zealots : GameComponent
    {
        public GameComponent_Zealots(Game game)
        {
        }

        public override void FinalizeInit()
        {
            FactionDef def = HP_DefOf.HP_Zealots;
            if (def == null || Find.FactionManager == null || Find.FactionManager.FirstFactionOfDef(def) != null)
            {
                return;
            }
            Faction f = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
            Find.FactionManager.Add(f);
            Log.Message("[Hollow Pawns] Added the zealot faction to this save.");
        }
    }
}
