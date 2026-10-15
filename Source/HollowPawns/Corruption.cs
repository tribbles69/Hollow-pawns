using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// Corruption (4.28), the backbone. Terrain corruption is a byte per cell in one MapComponent.
    /// Additions are one-off events (Spill). Spread, withering and fire cleansing run on a sampled
    /// batch of cells every rare-tick interval; pawn corruption and demon auras on the same interval
    /// over spawned humanlikes. Nothing ticks per cell or per thing.
    /// </summary>
    public class MapComponent_Corruption : MapComponent
    {
        /// <summary>Cells sampled per pass (every 250 ticks).</summary>
        public const int CellsPerPass = 200;

        /// <summary>Plants on ground this corrupted wither.</summary>
        public const byte WitherLevel = 120;

        /// <summary>Ground this corrupted creeps into its neighbours.</summary>
        public const byte SpreadLevel = 80;

        /// <summary>Ground this corrupted turns harvested and rendered essence corrupted.</summary>
        public const byte CorruptsEssenceLevel = 160;

        private byte[] grid;
        private CellBoolDrawer drawer;

        /// <summary>Shrines and anything else that pushes corruption around itself. Rebuilt as they spawn; not saved.</summary>
        private readonly List<ICorruptionSource> sources = new List<ICorruptionSource>();
        private bool drawerDirty = true;

        public MapComponent_Corruption(Map map) : base(map)
        {
        }

        private byte[] Grid => grid ?? (grid = new byte[map.cellIndices.NumGridCells]);

        public byte At(IntVec3 c) => c.InBounds(map) ? Grid[map.cellIndices.CellToIndex(c)] : (byte)0;

        public void Add(IntVec3 c, int amount)
        {
            if (!c.InBounds(map) || amount == 0)
            {
                return;
            }
            int i = map.cellIndices.CellToIndex(c);
            Grid[i] = (byte)Mathf.Clamp(Grid[i] + amount, 0, 255);
            drawerDirty = true;
        }

        public void Register(ICorruptionSource s)
        {
            if (!sources.Contains(s))
            {
                sources.Add(s);
            }
        }

        public void Deregister(ICorruptionSource s) => sources.Remove(s);

        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;
            if ((tick + map.uniqueID) % GenTicks.TickRareInterval != 0)
            {
                return;
            }
            SampleCells();
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                sources[i].ApplyCorruption(this);
            }
            PawnPass();
            if ((tick + map.uniqueID) % GenTicks.TickLongInterval == 0)
            {
                CorpsePass();
            }
        }

        private void SampleCells()
        {
            int n = Grid.Length;
            for (int k = 0; k < CellsPerPass; k++)
            {
                int i = Rand.Range(0, n);
                byte v = Grid[i];
                if (v == 0)
                {
                    continue;
                }
                IntVec3 c = map.cellIndices.IndexToCell(i);
                List<Thing> things = c.GetThingList(map);
                for (int t = things.Count - 1; t >= 0; t--)
                {
                    Thing thing = things[t];
                    if (thing is Fire)
                    {
                        // Fire cleanses: cheap, fast, destroys everything else with it.
                        Add(c, -60);
                        v = At(c);
                    }
                    else if (thing is Plant plant && v >= WitherLevel && Rand.Chance(0.5f))
                    {
                        plant.Kill();
                    }
                }
                if (v >= SpreadLevel && Rand.Chance(0.3f))
                {
                    IntVec3 to = c + GenAdj.AdjacentCells.RandomElement();
                    if (to.InBounds(map) && !to.Impassable(map))
                    {
                        // Creep moves corruption outward rather than making more of it.
                        int moved = v / 8;
                        Add(to, moved);
                        Add(c, -moved);
                    }
                }
            }
        }

        private void PawnPass()
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (!p.RaceProps.Humanlike || p.Dead)
                {
                    continue;
                }
                if (SoulUtility.IsCorrupted(p))
                {
                    // Demons carry a corruption aura: they stain the ground they walk on.
                    if (p.Faction != Faction.OfPlayer)
                    {
                        Corruption.Spill(p.Position, map, 6, 1.5f);
                    }
                    continue;
                }
                byte v = At(p.Position);
                if (v > 40)
                {
                    Corruption.AddToPawn(p, v / 255f * 0.0015f);
                }
                Hediff h = p.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_SoulCorruption);
                if (h != null && h.Severity >= 0.85f && Rand.MTBEventOccurs(4f, GenDate.TicksPerDay, GenTicks.TickRareInterval))
                {
                    CurseUtility.CurseRandom(p, "by the corruption in their soul");
                }
            }
        }

        private void CorpsePass()
        {
            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                if (t is Corpse corpse && corpse.Spawned && SoulUtility.IsCorrupted(corpse.InnerPawn))
                {
                    Corruption.Spill(corpse.Position, map, 8, 1.5f);
                }
            }
        }

        // ---------- overlay ----------

        public override void MapComponentUpdate()
        {
            if (!Corruption.ShowOverlay || Find.CurrentMap != map)
            {
                return;
            }
            if (drawer == null)
            {
                drawer = new CellBoolDrawer(i => Grid[i] > 0, () => Color.white, CellColor, map.Size.x, map.Size.z, 0.45f);
            }
            if (drawerDirty && Time.frameCount % 30 == 0)
            {
                drawer.SetDirty();
                drawerDirty = false;
            }
            drawer.MarkForDraw();
            drawer.CellBoolDrawerUpdate();
        }

        private Color CellColor(int i)
        {
            float f = Grid[i] / 255f;
            return new Color(0.35f + 0.35f * f, 0.05f, 0.25f + 0.2f * f, 0.25f + 0.6f * f);
        }

        public override void ExposeData()
        {
            byte[] g = Grid;
            DataExposeUtility.LookByteArray(ref g, "corruption");
            grid = g;
            if (Scribe.mode == LoadSaveMode.PostLoadInit && (grid == null || grid.Length != map.cellIndices.NumGridCells))
            {
                grid = new byte[map.cellIndices.NumGridCells];
            }
        }
    }

    public static class Corruption
    {
        public static bool ShowOverlay;

        public static MapComponent_Corruption For(Map map) => map?.GetComponent<MapComponent_Corruption>();

        public static byte At(IntVec3 c, Map map) => For(map)?.At(c) ?? 0;

        /// <summary>Spills corruption around a cell, strongest at the centre. Negative cleanses.</summary>
        public static void Spill(IntVec3 center, Map map, int amount, float radius)
        {
            MapComponent_Corruption comp = For(map);
            if (comp == null || amount == 0)
            {
                return;
            }
            foreach (IntVec3 c in GenRadial.RadialCellsAround(center, radius, true))
            {
                float falloff = 1f - 0.6f * (c.DistanceTo(center) / Mathf.Max(radius, 1f));
                comp.Add(c, Mathf.RoundToInt(amount * falloff));
            }
        }

        /// <summary>
        /// Adds soul corruption to a pawn. Only souls can be corrupted: children, thralls, husks and
        /// anything else without a soul are never affected, and demons are immune (it's their nature).
        /// </summary>
        public static void AddToPawn(Pawn pawn, float amount)
        {
            if (amount <= 0f || SoulUtility.GetSoul(pawn) == null || SoulUtility.IsCorrupted(pawn))
            {
                return;
            }
            HealthUtility.AdjustSeverity(pawn, HP_DefOf.HP_SoulCorruption, amount);
        }

        public static void Cleanse(Pawn pawn, float amount)
        {
            Hediff h = pawn?.health?.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_SoulCorruption);
            if (h != null)
            {
                h.Severity -= amount;
            }
        }
    }

    /// <summary>Soul blasts and soul bursts stain the ground they hit.</summary>
    [HarmonyPatch(typeof(GenExplosion), nameof(GenExplosion.DoExplosion))]
    public static class Patch_GenExplosion_DoExplosion
    {
        public static void Postfix(IntVec3 center, Map map, DamageDef damType)
        {
            if (map != null && (damType == HP_DefOf.HP_SoulBlast || damType == HP_DefOf.HP_SoulBurst))
            {
                Corruption.Spill(center, map, 12, 0.9f);
            }
        }
    }

    /// <summary>The corruption overlay toggle, among the map's bottom-right play settings.</summary>
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_PlaySettings
    {
        private static Texture2D icon;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            icon = icon ?? ContentFinder<Texture2D>.Get("UI/Icons/HP_CorruptionOverlay");
            row.ToggleableIcon(ref Corruption.ShowOverlay, icon, "Show soul corruption on the ground.");
        }
    }

    /// <summary>Mood penalty for standing or working on corrupted ground. Only for souls that aren't corrupt.</summary>
    public class ThoughtWorker_OnCorruptedGround : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned || SoulUtility.GetSoul(p) == null || SoulUtility.IsCorrupted(p))
            {
                return ThoughtState.Inactive;
            }
            byte v = Corruption.At(p.Position, p.Map);
            if (v >= 150)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            return v >= 50 ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }

    /// <summary>Holy water, drunk: lowers the drinker's soul corruption.</summary>
    public class IngestionOutcomeDoer_Cleanse : IngestionOutcomeDoer
    {
        public float amount = 0.15f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            Corruption.Cleanse(pawn, amount * ingestedCount);
        }
    }

    /// <summary>Purgation: a painful operation that burns corruption out of a soul. Adults only.</summary>
    public class Recipe_Purgation : Recipe_Surgery
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            if (SoulUtility.GetSoul(pawn) != null && pawn.health.hediffSet.HasHediff(HP_DefOf.HP_SoulCorruption))
            {
                yield return null;
            }
        }

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return thing is Pawn p && SoulUtility.GetSoul(p) != null && p.health.hediffSet.HasHediff(HP_DefOf.HP_SoulCorruption);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            Corruption.Cleanse(pawn, 0.5f);
            pawn.health.AddHediff(HP_DefOf.HP_PurgationPain);
        }
    }
}
