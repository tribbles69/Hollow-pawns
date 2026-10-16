using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace HollowPawns
{
    /// <summary>
    /// Demon arrivals (4.34): rift portals (scattered, the drop-pod version), burrowing (one crack,
    /// preferring corrupted ground, the infestation version) and the Great Rift (a building that sends
    /// waves for days). Demons only; everything runs when a raid arrives or on the rift's rare tick.
    /// </summary>
    public static class DemonArrival
    {
        public static bool IsDemonRaid(IncidentParms parms) =>
            HP_DefOf.HP_Demons != null && parms?.faction != null && parms.faction.def == HP_DefOf.HP_Demons;

        /// <summary>Steps a demon out of a tear at this cell: a flash, a stain of corruption, and there it is.</summary>
        public static void Emerge(Pawn p, IntVec3 c, Map map, int stain)
        {
            GenSpawn.Spawn(p, c, map, Rot4.Random);
            FleckMaker.Static(c.ToVector3Shifted(), map, FleckDefOf.PsycastAreaEffect, 1.5f);
            FleckMaker.ThrowSmoke(c.ToVector3Shifted(), map, 1.5f);
            Corruption.Spill(c, map, stain, 1.9f);
        }

        public static bool GoodCell(IntVec3 c, Map map) => c.InBounds(map) && c.Standable(map) && !c.Fogged(map) && c.GetFirstPawn(map) == null;
    }

    /// <summary>Rift portals: three or four tears open around the colony and the warband steps out of them.</summary>
    public class PawnsArrivalModeWorker_RiftPortals : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms) => DemonArrival.IsDemonRaid(parms) && base.CanUseWith(parms);

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int portals = Mathf.Clamp(pawns.Count / 3, 2, 4);
            var centres = new List<IntVec3>();
            for (int i = 0; i < portals; i++)
            {
                centres.Add(DropCellFinder.RandomDropSpot(map));
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                IntVec3 centre = centres[i % centres.Count];
                IntVec3 c = CellFinder.TryFindRandomCellNear(centre, map, 3, x => DemonArrival.GoodCell(x, map), out IntVec3 found) ? found : centre;
                DemonArrival.Emerge(pawns[i], c, map, 25);
            }
        }

        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            parms.spawnRotation = Rot4.Random;
            return true;
        }
    }

    /// <summary>Burrowing: one crack, on the most corrupted ground near the colony if there is any, and they crawl out.</summary>
    public class PawnsArrivalModeWorker_Burrow : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms) => DemonArrival.IsDemonRaid(parms) && base.CanUseWith(parms);

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 centre = parms.spawnCenter;
            foreach (Pawn p in pawns)
            {
                IntVec3 c = CellFinder.TryFindRandomCellNear(centre, map, 4, x => DemonArrival.GoodCell(x, map), out IntVec3 found) ? found : centre;
                DemonArrival.Emerge(p, c, map, 15);
            }
            FilthMaker.TryMakeFilth(centre, map, ThingDefOf.Filth_RubbleRock, 4);
            Corruption.Spill(centre, map, 80, 3.9f);
        }

        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!parms.spawnCenter.IsValid)
            {
                parms.spawnCenter = BestCrack(map);
            }
            parms.spawnRotation = Rot4.Random;
            return parms.spawnCenter.IsValid;
        }

        /// <summary>Samples cells near the colony and takes the most corrupted standable one.</summary>
        private static IntVec3 BestCrack(Map map)
        {
            IntVec3 home = map.mapPawns.FreeColonistsSpawned.Count > 0 ? map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.Center;
            IntVec3 best = IntVec3.Invalid;
            int bestV = -1;
            for (int i = 0; i < 60; i++)
            {
                if (!CellFinder.TryFindRandomCellNear(home, map, 30, x => DemonArrival.GoodCell(x, map) && !x.Roofed(map), out IntVec3 c))
                {
                    continue;
                }
                int v = Corruption.At(c, map) + Rand.Range(0, 10);
                if (v > bestV)
                {
                    bestV = v;
                    best = c;
                }
            }
            return best.IsValid ? best : DropCellFinder.RandomDropSpot(map);
        }
    }

    /// <summary>Out of a Great Rift: everyone steps out right beside it (the rift sets the spawn centre).</summary>
    public class PawnsArrivalModeWorker_RiftEmerge : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms) => false;

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            foreach (Pawn p in pawns)
            {
                IntVec3 c = CellFinder.TryFindRandomCellNear(parms.spawnCenter, map, 4, x => DemonArrival.GoodCell(x, map), out IntVec3 found) ? found : parms.spawnCenter;
                DemonArrival.Emerge(p, c, map, 10);
            }
        }

        public override bool TryResolveRaidSpawnCenter(IncidentParms parms) => parms.spawnCenter.IsValid;
    }

    // ======================= The Great Rift =======================

    /// <summary>
    /// The Great Rift: a hole torn in the world. It corrupts the ground around it, sends a demon wave
    /// out every 18 hours for three days, then collapses into a corrupted crater. A rift keeper guards
    /// it: kill the keeper, or finish a cleansing rite within reach of it, and it closes early.
    /// Rare tick on the rift only.
    /// </summary>
    public class Building_GreatRift : Building, ICorruptionSource
    {
        public const int LifeTicks = GenDate.TicksPerDay * 3;
        public const int WaveTicks = GenDate.TicksPerHour * 18;

        public Pawn keeper;
        private int ticksLeft = LifeTicks;
        private int nextWave = GenDate.TicksPerHour * 2;

        public void ApplyCorruption(MapComponent_Corruption c) => ShrineUtility.Touch(c, Position, 9.9f, 8, 10);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Corruption.For(map)?.Register(this);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Corruption.For(Map)?.Deregister(this);
            base.DeSpawn(mode);
        }

        public override void TickRare()
        {
            base.TickRare();
            if (keeper != null && (keeper.Dead || keeper.Destroyed))
            {
                Close("its keeper is dead", false);
                return;
            }
            ticksLeft -= GenTicks.TickRareInterval;
            nextWave -= GenTicks.TickRareInterval;
            if (ticksLeft <= 0)
            {
                Close("it has burned itself out", true);
                return;
            }
            if (nextWave <= 0)
            {
                nextWave = WaveTicks;
                SendWave();
            }
        }

        private void SendWave()
        {
            Faction demons = HP_DefOf.HP_Demons == null ? null : Find.FactionManager.FirstFactionOfDef(HP_DefOf.HP_Demons);
            if (demons == null || HP_DefOf.HP_DemonWave == null)
            {
                return;
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Map);
            parms.faction = demons;
            parms.points *= 0.6f;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = HP_DefOf.HP_RiftEmerge;
            parms.spawnCenter = Position;
            parms.forced = true;
            HP_DefOf.HP_DemonWave.Worker.TryExecute(parms);
        }

        /// <summary>Closes the rift. Burned out: a crater of corruption. Closed early: much less.</summary>
        public void Close(string why, bool collapse)
        {
            Map map = Map;
            IntVec3 pos = Position;
            Destroy(DestroyMode.Vanish);
            Corruption.Spill(pos, map, collapse ? 200 : 80, collapse ? 9.9f : 5.9f);
            FilthMaker.TryMakeFilth(pos, map, ThingDefOf.Filth_RubbleRock, 8);
            FleckMaker.Static(pos.ToVector3Shifted(), map, FleckDefOf.PsycastAreaEffect, 6f);
            Find.LetterStack.ReceiveLetter("Great Rift closed", "The Great Rift has closed: " + why + "."
                + (collapse ? " It left a crater of corruption behind." : ""), LetterDefOf.PositiveEvent, new TargetInfo(pos, map));
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            string line = "Closes in " + ticksLeft.ToStringTicksToPeriod() + ". Next wave in " + Mathf.Max(0, nextWave).ToStringTicksToPeriod() + "."
                + (keeper != null && !keeper.Dead ? " Kill its keeper (" + keeper.LabelShort + ") or perform a cleansing rite beside it to close it now." : "");
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref keeper, "keeper");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", LifeTicks);
            Scribe_Values.Look(ref nextWave, "nextWave", GenDate.TicksPerHour * 2);
        }
    }

    public class IncidentWorker_GreatRift : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return HP_DefOf.HP_Demons != null && HP_DefOf.HP_GreatRift != null && parms.target is Map map
                && Find.FactionManager.FirstFactionOfDef(HP_DefOf.HP_Demons) != null && TryFindCell(map, out IntVec3 _);
        }

        private static bool TryFindCell(Map map, out IntVec3 cell)
        {
            IntVec3 home = map.mapPawns.FreeColonistsSpawned.Count > 0 ? map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.Center;
            return CellFinder.TryFindRandomCellNear(home, map, 40,
                c => c.DistanceTo(home) > 18f && DemonArrival.GoodCell(c, map) && !c.Roofed(map) && c.GetEdifice(map) == null
                     && GenAdj.CellsAdjacent8Way(new TargetInfo(c, map)).All(a => a.InBounds(map) && a.Standable(map)), out cell);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!TryFindCell(map, out IntVec3 cell))
            {
                return false;
            }
            Faction demons = Find.FactionManager.FirstFactionOfDef(HP_DefOf.HP_Demons);
            var rift = (Building_GreatRift)ThingMaker.MakeThing(HP_DefOf.HP_GreatRift);
            rift.SetFaction(demons);
            GenSpawn.Spawn(rift, cell, map);
            Corruption.Spill(cell, map, 120, 5.9f);
            // The keeper: the strongest demon kind, holding the rift.
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("HP_DemonLord") ?? demons.def.pawnGroupMakers.SelectMany(g => g.options).Select(o => o.kind).MaxBy(k => k.combatPower);
            Pawn keeper = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, demons, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true));
            IntVec3 kc = CellFinder.TryFindRandomCellNear(cell, map, 2, x => DemonArrival.GoodCell(x, map), out IntVec3 found) ? found : cell;
            DemonArrival.Emerge(keeper, kc, map, 20);
            keeper.Name = new NameSingle("rift keeper");
            LordMaker.MakeNewLord(demons, new LordJob_DefendPoint(cell, 6f), map, new[] { keeper });
            rift.keeper = keeper;
            SendStandardLetter(def.letterLabel, def.letterText, LetterDefOf.ThreatBig, parms, new LookTargets(rift, keeper));
            return true;
        }
    }
}
