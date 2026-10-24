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
                yield return Thralls.Generate();
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

    /// <summary>One key in a structure's legend: what a character in the layout stands for.</summary>
    public class StructureKey : IExposable
    {
        /// <summary>The single character used in the layout.</summary>
        public string key;

        /// <summary>A building or item placed with its anchor (its centre, by RimWorld's rule for even sizes) on this cell.</summary>
        public ThingDef thing;
        public ThingDef stuff;
        public Rot4 rotation = Rot4.North;
        public int count = 1;

        /// <summary>Floor laid on this cell, under whatever else goes there.</summary>
        public TerrainDef terrain;

        public bool roof;

        /// <summary>A bed placed by this key is set aside for prisoners.</summary>
        public bool forPrisoners;

        /// <summary>Left as the world's, not the player's (old graves, ruins someone else built).</summary>
        public bool unowned;

        public void ExposeData()
        {
            Scribe_Values.Look(ref key, "key");
            Scribe_Defs.Look(ref thing, "thing");
            Scribe_Defs.Look(ref stuff, "stuff");
            Scribe_Values.Look(ref rotation, "rotation", Rot4.North);
            Scribe_Values.Look(ref count, "count", 1);
            Scribe_Defs.Look(ref terrain, "terrain");
            Scribe_Values.Look(ref roof, "roof");
            Scribe_Values.Look(ref forPrisoners, "forPrisoners");
            Scribe_Values.Look(ref unowned, "unowned");
        }
    }

    /// <summary>
    /// A hand-drawn structure stamped at the landing site: rows of characters, north at the top, each
    /// character looked up in the legend (space and '.' are left alone). It runs with the other scenario
    /// parts at map generation, so it must be listed before the arrival part if the colonists are to
    /// start inside it. The footprint is cleared of plants, rubble and stray items first; it won't go on
    /// water, rock, a thick roof or anything that can't be destroyed. <c>ruin</c> leaves that share of plain
    /// walls out and knocks every building about. Fog is lifted over the footprint once generation finishes.
    /// </summary>
    public class ScenPart_HP_Structure : ScenPart
    {
        public string label = "a building";
        public List<string> layout = new List<string>();
        public List<StructureKey> legend = new List<StructureKey>();

        /// <summary>Centre the structure on the start spot (colonists start inside); otherwise anywhere near it.</summary>
        public bool atStart = true;

        public float ruin;

        [Unsaved]
        private CellRect placed = CellRect.Empty;

        [Unsaved]
        private Map placedOn;

        private int Width => layout.NullOrEmpty() ? 0 : layout.Max(r => r.Length);

        private int Height => layout?.Count ?? 0;

        public override string Summary(Scenario scen) => "Start in " + label + ".";

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            if (layout.NullOrEmpty())
            {
                yield return "structure '" + label + "' has no layout";
                yield break;
            }
            foreach (StructureKey k in legend)
            {
                if (k.key.NullOrEmpty() || k.key.Length != 1)
                {
                    yield return "structure '" + label + "' has a legend key that isn't one character: '" + k.key + "'";
                }
            }
            foreach (char c in layout.SelectMany(r => r).Distinct())
            {
                if (c != ' ' && c != '.' && Key(c) == null)
                {
                    yield return "structure '" + label + "' uses '" + c + "' but its legend doesn't say what that is";
                }
            }
        }

        public override bool HasNullDefs() => base.HasNullDefs() || legend.Any(k => k.thing == null && k.terrain == null && !k.roof);

        private StructureKey Key(char c) => legend.FirstOrDefault(k => k.key != null && k.key.Length == 1 && k.key[0] == c);

        /// <summary>The map cell for layout column x, row y (row 0 is the northern edge).</summary>
        private static IntVec3 CellAt(CellRect rect, int x, int y) => new IntVec3(rect.minX + x, 0, rect.maxZ - y);

        private static bool Fits(Map map, CellRect rect)
        {
            if (!rect.InBounds(map))
            {
                return false;
            }
            foreach (IntVec3 c in rect)
            {
                TerrainDef t = c.GetTerrain(map);
                if (t.IsWater || t.passability == Traversability.Impassable)
                {
                    return false;
                }
                RoofDef roof = c.GetRoof(map);
                if (roof != null && roof.isThickRoof)
                {
                    return false;
                }
                foreach (Thing th in c.GetThingList(map))
                {
                    if (!th.def.destroyable || (th.def.building != null && (th.def.building.isNaturalRock || th.def.building.isResourceRock)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private CellRect RectAt(IntVec3 centre) => new CellRect(centre.x - Width / 2, centre.z - Height / 2, Width, Height);

        private bool TryPlace(Map map, out CellRect rect)
        {
            IntVec3 start = MapGenerator.PlayerStartSpot.IsValid ? MapGenerator.PlayerStartSpot : map.Center;
            if (atStart && Fits(map, RectAt(start)))
            {
                rect = RectAt(start);
                return true;
            }
            foreach (int radius in new[] { 12, 25, 45 })
            {
                if (CellFinder.TryFindRandomCellNear(start, map, radius, c => Fits(map, RectAt(c)), out IntVec3 centre))
                {
                    rect = RectAt(centre);
                    return true;
                }
            }
            rect = CellRect.Empty;
            return false;
        }

        public override void GenerateIntoMap(Map map)
        {
            if (Find.GameInitData == null || layout.NullOrEmpty())
            {
                return;
            }
            if (!TryPlace(map, out CellRect rect))
            {
                Log.Warning("[Hollow Pawns] No room near the start for " + label + "; it was left out.");
                return;
            }
            placed = rect;
            placedOn = map;

            // The fog step floods outward from the start spot, and doors stop it, so with the colonists
            // starting inside the building only the room would be revealed. Give it roots outside too.
            int reach = Mathf.Max(Width, Height) + 8;
            for (int i = 0; i < 3; i++)
            {
                if (CellFinder.TryFindRandomCellNear(rect.CenterCell, map, reach,
                    c => !rect.ExpandedBy(1).Contains(c) && c.Standable(map) && !c.Roofed(map) && !c.GetTerrain(map).IsWater, out IntVec3 root))
                {
                    MapGenerator.rootsToUnfog.Add(root);
                }
            }

            // Clear the footprint: plants, chunks, filth, stray items and anything else that can go.
            foreach (IntVec3 c in rect)
            {
                foreach (Thing t in c.GetThingList(map).ToList())
                {
                    if (t is Pawn p)
                    {
                        // A wandering animal: step it outside rather than build a wall on it.
                        if (CellFinder.TryFindRandomCellNear(rect.CenterCell, map, Mathf.Max(Width, Height) + 6, o => o.Standable(map) && !rect.Contains(o), out IntVec3 outside))
                        {
                            p.DeSpawn();
                            GenSpawn.Spawn(p, outside, map);
                        }
                    }
                    else if (t.def.destroyable && !t.Destroyed)
                    {
                        t.Destroy();
                    }
                }
            }

            // Floors and roofs first, then everything that stands on them.
            var things = new List<(IntVec3 cell, StructureKey key)>();
            for (int y = 0; y < Height; y++)
            {
                string row = layout[y];
                for (int x = 0; x < row.Length; x++)
                {
                    StructureKey k = Key(row[x]);
                    if (k == null)
                    {
                        continue;
                    }
                    IntVec3 cell = CellAt(rect, x, y);
                    if (k.terrain != null)
                    {
                        map.terrainGrid.SetTerrain(cell, k.terrain);
                    }
                    if (k.roof)
                    {
                        map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                    }
                    if (k.thing != null)
                    {
                        things.Add((cell, k));
                    }
                }
            }
            foreach ((IntVec3 cell, StructureKey k) in things)
            {
                bool building = k.thing.category == ThingCategory.Building;
                if (k.thing == ThingDefOf.Wall && ruin > 0f && Rand.Chance(ruin))
                {
                    continue;
                }
                ThingDef stuff = k.thing.MadeFromStuff ? (k.stuff ?? GenStuff.DefaultStuffFor(k.thing)) : null;
                Thing t = ThingMaker.MakeThing(k.thing, stuff);
                if (!building)
                {
                    t.stackCount = Mathf.Clamp(k.count, 1, k.thing.stackLimit);
                }
                if (!k.unowned && t.def.CanHaveFaction)
                {
                    t.SetFaction(Faction.OfPlayer);
                }
                if (building && ruin > 0f && t.def.useHitPoints)
                {
                    t.HitPoints = Mathf.Max(1, Mathf.RoundToInt(t.MaxHitPoints * Rand.Range(1f - ruin, 1f)));
                }
                Thing spawned = GenSpawn.Spawn(t, cell, map, k.rotation, WipeMode.Vanish);
                if (k.forPrisoners && spawned is Building_Bed bed)
                {
                    bed.ForPrisoners = true;
                }
            }
        }

        public override void PostMapGenerate(Map map)
        {
            // Fog is laid after the scenario parts run, and doors stop it; lift it off our own building.
            if (map != placedOn || placed.IsEmpty)
            {
                return;
            }
            foreach (IntVec3 c in placed)
            {
                if (c.Fogged(map))
                {
                    map.fogGrid.Unfog(c);
                }
            }
            placedOn = null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref label, "label", "a building");
            Scribe_Collections.Look(ref layout, "layout", LookMode.Value);
            Scribe_Collections.Look(ref legend, "legend", LookMode.Deep);
            Scribe_Values.Look(ref atStart, "atStart", true);
            Scribe_Values.Look(ref ruin, "ruin");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                layout = layout ?? new List<string>();
                legend = legend ?? new List<StructureKey>();
            }
        }
    }
}
