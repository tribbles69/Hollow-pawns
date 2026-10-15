using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// Which pawns are Soul-Locked: the Mark buttons skip them and extract soul can't be queued on them.
    /// Stored by thing ID rather than by reference, so a locked pawn who dies or leaves the world
    /// never leaves a broken reference in the save. Nothing ticks.
    /// </summary>
    public class WorldComponent_SoulLocks : WorldComponent
    {
        private HashSet<int> locked = new HashSet<int>();

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

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref locked, "locked", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && locked == null)
            {
                locked = new HashSet<int>();
            }
        }
    }
}
