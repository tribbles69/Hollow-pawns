using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// The Reaper's Mark state: which pawns are Soul-Locked, and which prisoners are marked to be
    /// rendered or bound. Stored by thing ID rather than by reference, so a pawn who dies or leaves
    /// the world never leaves a broken reference in the save. Nothing ticks.
    /// </summary>
    public class WorldComponent_SoulLocks : WorldComponent
    {
        private HashSet<int> locked = new HashSet<int>();
        private Dictionary<int, SoulFate> fates = new Dictionary<int, SoulFate>();

        public WorldComponent_SoulLocks(World world) : base(world)
        {
        }

        public static WorldComponent_SoulLocks Get() => Find.World?.GetComponent<WorldComponent_SoulLocks>();

        public bool IsLocked(Pawn pawn) => pawn != null && locked.Contains(pawn.thingIDNumber);

        public void SetLocked(Pawn pawn, bool value)
        {
            if (value)
            {
                locked.Add(pawn.thingIDNumber);
            }
            else
            {
                locked.Remove(pawn.thingIDNumber);
            }
        }

        public SoulFate GetFate(Pawn pawn) =>
            pawn != null && fates.TryGetValue(pawn.thingIDNumber, out SoulFate f) ? f : SoulFate.None;

        public void SetFate(Pawn pawn, SoulFate fate)
        {
            if (fate == SoulFate.None)
            {
                fates.Remove(pawn.thingIDNumber);
            }
            else
            {
                fates[pawn.thingIDNumber] = fate;
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref locked, "locked", LookMode.Value);
            Scribe_Collections.Look(ref fates, "fates", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                locked = locked ?? new HashSet<int>();
                fates = fates ?? new Dictionary<int, SoulFate>();
            }
        }
    }
}
