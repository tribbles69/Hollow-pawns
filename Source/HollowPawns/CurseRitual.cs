using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    /// <summary>
    /// The curse ritual (4.2): a warden lays a curse on a prisoner that they carry home. Released or
    /// escaped, a carrier curses their faction, and the next raid it sends arrives with every raider
    /// cursed. Event-driven: one check when a carrier leaves the map, one when a raider spawns.
    /// </summary>
    public class WorldComponent_FactionCurses : WorldComponent
    {
        private List<Faction> cursed = new List<Faction>();
        private List<int> activeUntil = new List<int>();

        public WorldComponent_FactionCurses(World world) : base(world)
        {
        }

        public static WorldComponent_FactionCurses Get() => Find.World?.GetComponent<WorldComponent_FactionCurses>();

        public bool Any => cursed.Count > 0;

        public void Curse(Faction f)
        {
            if (f != null && !cursed.Contains(f))
            {
                cursed.Add(f);
                activeUntil.Add(-1);
            }
        }

        /// <summary>A raider of a cursed faction has arrived: curse them, and keep the curse live for six hours of arrivals.</summary>
        public void Notify_Arrived(Pawn p)
        {
            int i = cursed.IndexOf(p.Faction);
            if (i < 0)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (activeUntil[i] < 0)
            {
                activeUntil[i] = now + GenDate.TicksPerHour * 6;
                Messages.Message("The curse carried home to " + p.Faction.Name + " came back with their raiders.", p, MessageTypeDefOf.PositiveEvent);
            }
            if (now > activeUntil[i])
            {
                cursed.RemoveAt(i);
                activeUntil.RemoveAt(i);
                return;
            }
            CurseUtility.CurseRandom(p, "by the curse their people carried home");
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref cursed, "cursed", LookMode.Reference);
            Scribe_Collections.Look(ref activeUntil, "activeUntil", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                cursed = cursed ?? new List<Faction>();
                activeUntil = activeUntil ?? new List<int>();
                for (int i = cursed.Count - 1; i >= 0; i--)
                {
                    if (cursed[i] == null)
                    {
                        cursed.RemoveAt(i);
                        if (i < activeUntil.Count)
                        {
                            activeUntil.RemoveAt(i);
                        }
                    }
                }
                while (activeUntil.Count < cursed.Count)
                {
                    activeUntil.Add(-1);
                }
            }
        }
    }

    public static class CurseRitual
    {
        public static bool IsCarrier(Pawn p) => p?.health?.hediffSet?.HasHediff(HP_DefOf.HP_CurseCarrier) == true;

        public static void Perform(Pawn warden, Pawn prisoner)
        {
            prisoner.health.AddHediff(HP_DefOf.HP_CurseCarrier);
            CurseUtility.CurseRandom(prisoner, "by the curse ritual");
            Corruption.AddToPawn(warden, 0.03f);
            Corruption.Spill(prisoner.Position, prisoner.Map, 20, 1.9f);
            Messages.Message(warden.LabelShort + " laid a curse on " + prisoner.LabelShort + ". Release them and they'll carry it home to " + (prisoner.HomeFaction?.Name ?? "their people") + ".",
                prisoner, MessageTypeDefOf.NeutralEvent);
        }
    }

    public class WorkGiver_Warden_Curse : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.mapPawns.PrisonersOfColonySpawned;

        public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
            !pawn.Map.mapPawns.PrisonersOfColonySpawned.Any(p => p.guest.IsInteractionEnabled(HP_DefOf.HP_Curse) && !CurseRitual.IsCarrier(p));

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn prisoner) || !prisoner.IsPrisonerOfColony || prisoner.Dead || !prisoner.RaceProps.Humanlike
                || !prisoner.guest.IsInteractionEnabled(HP_DefOf.HP_Curse) || CurseRitual.IsCarrier(prisoner)
                || !pawn.CanReserveAndReach(prisoner, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            return JobMaker.MakeJob(HP_DefOf.HP_CursePrisoner, prisoner);
        }
    }

    public class JobDriver_CursePrisoner : JobDriver
    {
        private Pawn Prisoner => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Prisoner.IsPrisonerOfColony || !Prisoner.guest.IsInteractionEnabled(HP_DefOf.HP_Curse) || CurseRitual.IsCarrier(Prisoner));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil chant = Toils_General.Wait(1200, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            chant.WithEffect(EffecterDefOf.Clean, TargetIndex.A);
            yield return chant;
            Toil done = ToilMaker.MakeToil("HP_CurseDone");
            done.initAction = () => CurseRitual.Perform(pawn, Prisoner);
            done.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return done;
        }
    }

    /// <summary>A carrier leaving the map alive takes the curse home to their faction.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExitMap))]
    public static class Patch_Pawn_ExitMap_CurseCarrier
    {
        public static void Prefix(Pawn __instance)
        {
            Faction home = __instance.HomeFaction;
            if (!__instance.Dead && home != null && !home.IsPlayer && CurseRitual.IsCarrier(__instance))
            {
                WorldComponent_FactionCurses.Get()?.Curse(home);
                Messages.Message(__instance.LabelShort + " is carrying the curse home to " + home.Name + ". Their next raid will come cursed.",
                    MessageTypeDefOf.PositiveEvent);
            }
        }
    }

    /// <summary>Raiders of a cursed faction arriving on a player map (walking in or by drop pod) are cursed.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_Pawn_SpawnSetup_FactionCurse
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad || __instance.Faction == null || __instance.Faction.IsPlayer || !map.IsPlayerHome
                || !__instance.RaceProps.Humanlike || !__instance.HostileTo(Faction.OfPlayer))
            {
                return;
            }
            WorldComponent_FactionCurses c = WorldComponent_FactionCurses.Get();
            if (c != null && c.Any)
            {
                c.Notify_Arrived(__instance);
            }
        }
    }
}
