using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// Scenario parts for the Hollow Pawns starts (4.33). Each runs once, at map generation or when the
    /// starting things are made; none of them tick.
    /// </summary>
    public static class ScenarioUtility
    {
        /// <summary>A clear, unroofed, standable spot for a building of this size near the player's start.</summary>
        public static bool TryFindBuildingSpot(Map map, ThingDef def, out IntVec3 cell)
        {
            IntVec3 start = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : map.Center;
            return CellFinder.TryFindRandomCellNear(start, map, 14, c =>
            {
                foreach (IntVec3 o in GenAdj.OccupiedRect(c, Rot4.North, def.size).ExpandedBy(1))
                {
                    if (!o.InBounds(map) || !o.Standable(map) || o.Roofed(map) || o.GetEdifice(map) != null
                        || o.GetFirstItem(map) != null || o.GetTerrain(map).passability != Traversability.Standable)
                    {
                        return false;
                    }
                }
                return true;
            }, out cell);
        }

        public static IntVec3 NearStart(Map map)
        {
            IntVec3 start = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : map.Center;
            return CellFinder.TryFindRandomCellNear(start, map, 8, c => c.Standable(map) && !c.Fogged(map), out IntVec3 cell) ? cell : start;
        }
    }

    /// <summary>A finished building of the player's, placed near the start (the Coven's altar, the war's machines).</summary>
    public class ScenPart_HP_StartingBuilding : ScenPart
    {
        public ThingDef thingDef;
        public ThingDef stuff;

        public override string Summary(Scenario scen) => "Start with a " + (stuff != null ? stuff.label + " " : "") + thingDef?.label + " already built.";

        public override void GenerateIntoMap(Map map)
        {
            if (Find.GameInitData == null || thingDef == null)
            {
                return;
            }
            if (!ScenarioUtility.TryFindBuildingSpot(map, thingDef, out IntVec3 cell))
            {
                Log.Warning("[Hollow Pawns] No room near the start for " + thingDef.defName + "; it was left out.");
                return;
            }
            Thing b = ThingMaker.MakeThing(thingDef, thingDef.MadeFromStuff ? (stuff ?? GenStuff.DefaultStuffFor(thingDef)) : null);
            b.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(b, cell, map, Rot4.North);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Defs.Look(ref stuff, "stuff");
        }

        public override bool HasNullDefs() => base.HasNullDefs() || thingDef == null;
    }

    /// <summary>
    /// Captives taken before landing: adult enemies, already prisoners, lying shackled near the start
    /// (downed for about two days, so there's time to build a cell before they can walk off).
    /// </summary>
    public class ScenPart_HP_StartingPrisoners : ScenPart
    {
        public int count = 1;

        public override string Summary(Scenario scen) => "Start with " + count + " captive" + (count == 1 ? "" : "s") + ", shackled.";

        public override void GenerateIntoMap(Map map)
        {
            if (Find.GameInitData == null)
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Faction enemy = Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: false);
                PawnKindDef kind = enemy?.def.basicMemberKind ?? PawnKindDefOf.Villager;
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, enemy, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, developmentalStages: DevelopmentalStage.Adult));
                GenSpawn.Spawn(p, ScenarioUtility.NearStart(map), map);
                p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                p.health.AddHediff(HP_DefOf.HP_Shackled);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count", 1);
        }
    }

    /// <summary>
    /// An old battlefield (the Gravedigger): adult dead from enemy factions scattered around the start,
    /// half of them stripped of their weapons. Every one still has its soul, so every one rolls for a
    /// ghost after a day, at this scenario's higher chance.
    /// </summary>
    public class ScenPart_HP_Battlefield : ScenPart
    {
        public int count = 16;
        public float radius = 26f;

        /// <summary>Ghost chance per neglected corpse while this scenario runs (the mod setting if that's higher).</summary>
        public float ghostChance = 0.08f;

        public override string Summary(Scenario scen) => "The landing site is an old battlefield: " + count + " unburied dead, any of whom may rise.";

        public override void PostMapGenerate(Map map)
        {
            if (Find.GameInitData == null)
            {
                return;
            }
            IntVec3 start = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : map.Center;
            for (int i = 0; i < count; i++)
            {
                if (!CellFinder.TryFindRandomCellNear(start, map, Mathf.RoundToInt(radius), c => c.Standable(map) && !c.Roofed(map) && c.DistanceTo(start) > 6f, out IntVec3 cell))
                {
                    continue;
                }
                Faction enemy = Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: false);
                PawnKindDef kind = enemy?.RandomPawnKind() ?? PawnKindDefOf.Villager;
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, enemy, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, developmentalStages: DevelopmentalStage.Adult));
                GenSpawn.Spawn(p, cell, map);
                if (Rand.Chance(0.5f))
                {
                    p.equipment?.DestroyAllEquipment();
                }
                p.Kill(null);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count", 16);
            Scribe_Values.Look(ref radius, "radius", 26f);
            Scribe_Values.Look(ref ghostChance, "ghostChance", 0.08f);
        }
    }

    /// <summary>The ground around the start is already corrupted: full strength in the middle, fading out.</summary>
    public class ScenPart_HP_CorruptedGround : ScenPart
    {
        public int amount = 140;
        public float radius = 20f;

        public override string Summary(Scenario scen) => "The ground around the landing site is corrupted.";

        public override void PostMapGenerate(Map map)
        {
            if (Find.GameInitData == null)
            {
                return;
            }
            MapComponent_Corruption c = Corruption.For(map);
            if (c == null)
            {
                return;
            }
            IntVec3 center = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : map.Center;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                float fall = Mathf.Clamp01(1.2f - cell.DistanceTo(center) / radius);
                int want = Mathf.RoundToInt(amount * fall * Rand.Range(0.8f, 1.1f));
                if (want > c.At(cell))
                {
                    c.Add(cell, want - c.At(cell));
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref amount, "amount", 140);
            Scribe_Values.Look(ref radius, "radius", 20f);
        }
    }

    /// <summary>Thralls who arrive with the colonists (Endless War). Biotech only; does nothing without it.</summary>
    public class ScenPart_HP_StartingThralls : ScenPart
    {
        public int count = 3;

        public override string Summary(Scenario scen) => "Start with " + count + " thralls.";

        public override IEnumerable<Thing> PlayerStartingThings()
        {
            if (HP_DefOf.HP_ThrallKind == null)
            {
                yield break;
            }
            for (int i = 0; i < count; i++)
            {
                var request = new PawnGenerationRequest(HP_DefOf.HP_ThrallKind, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false, mustBeCapableOfViolence: true,
                    allowAddictions: false, forceNoIdeo: true, fixedBiologicalAge: 20f, fixedChronologicalAge: 0f,
                    forcedXenotype: HP_DefOf.HP_ThrallXenotype, developmentalStages: DevelopmentalStage.Adult);
                Pawn thrall = PawnGenerator.GeneratePawn(request);
                thrall.Name = new NameSingle("Thrall " + Rand.RangeInclusive(100, 999));
                if (HP_DefOf.HP_ThrallDecay != null)
                {
                    thrall.health.AddHediff(HP_DefOf.HP_ThrallDecay);
                }
                yield return thrall;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count", 3);
        }
    }

    /// <summary>
    /// The Escapee's lattice: someone the first colonist loved, taken by the soul-press facility. A
    /// pawn is made, tied to the colonist (spouse, or lover if they're already married), bound into a
    /// lattice and left dead in the world. The lattice remembers that the colonist loved them.
    /// </summary>
    public class ScenPart_HP_LovedOneLattice : ScenPart
    {
        public override string Summary(Scenario scen) => "Start with a soul lattice holding someone the first colonist loved.";

        public override IEnumerable<Thing> PlayerStartingThings()
        {
            Pawn colonist = Find.GameInitData?.startingAndOptionalPawns?.FirstOrDefault();
            if (colonist == null)
            {
                yield break;
            }
            Pawn loved = PawnGenerator.GeneratePawn(new PawnGenerationRequest(colonist.kindDef, null, PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false, developmentalStages: DevelopmentalStage.Adult,
                fixedBiologicalAge: Mathf.Max(20f, colonist.ageTracker.AgeBiologicalYearsFloat + Rand.Range(-6f, 6f))));
            PawnRelationDef rel = colonist.relations.DirectRelations.Any(r => r.def == PawnRelationDefOf.Spouse) ? PawnRelationDefOf.Lover : PawnRelationDefOf.Spouse;
            colonist.relations.AddDirectRelation(rel, loved);

            Thing lattice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("HP_VesselLattice"));
            CompSoulVessel v = lattice.TryGetComp<CompSoulVessel>();
            SoulGrade grade = SoulGrades.Of(loved);
            v?.Fill(loved, grade, SoulGrade.Exalted);

            Find.WorldPawns.PassToWorld(loved, PawnDiscardDecideMode.KeepForever);
            SoulUtility.TakeSoulAndKill(loved);
            yield return lattice;
        }
    }
}
