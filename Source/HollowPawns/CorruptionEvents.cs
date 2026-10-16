using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    // ======================= Black rain (4.28) =======================

    /// <summary>
    /// Black rain: a dark, filthy rain that taints a few patches of the map. Uses vanilla's steady
    /// cell effects (the same sampling as toxic fallout), so it adds no ticking of its own; pawns
    /// caught outside pick up a little soul corruption every in-game hour.
    /// </summary>
    public class GameCondition_BlackRain : GameCondition
    {
        private List<IntVec3> centres = new List<IntVec3>();
        private List<float> radii = new List<float>();

        private static readonly SkyColorSet Colors = new SkyColorSet(
            new Color(0.32f, 0.26f, 0.34f), new Color(0.2f, 0.16f, 0.22f), new Color(0.55f, 0.45f, 0.55f), 0.6f);

        public override int TransitionTicks => 3000;

        public override void Init()
        {
            base.Init();
            foreach (Map map in AffectedMaps)
            {
                int n = Rand.RangeInclusive(3, 5);
                for (int i = 0; i < n; i++)
                {
                    centres.Add(CellFinder.RandomCell(map));
                    radii.Add(Rand.Range(14f, 24f));
                }
                WeatherDef rain = DefDatabase<WeatherDef>.GetNamedSilentFail("Rain");
                if (rain != null)
                {
                    map.weatherManager.TransitionTo(rain);
                }
            }
        }

        private bool InPatch(IntVec3 c)
        {
            for (int i = 0; i < centres.Count; i++)
            {
                if (c.InHorDistOf(centres[i], radii[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public override void DoCellSteadyEffects(IntVec3 c, Map map)
        {
            if (c.Roofed(map) || !InPatch(c) || !Rand.Chance(0.4f))
            {
                return;
            }
            MapComponent_Corruption corruption = Corruption.For(map);
            if (corruption != null && corruption.At(c) < 140)
            {
                corruption.Add(c, 5);
            }
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (Find.TickManager.TicksGame % GenDate.TicksPerHour != 0)
            {
                return;
            }
            foreach (Map map in AffectedMaps)
            {
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p.RaceProps.Humanlike && !p.Position.Roofed(map) && InPatch(p.Position))
                    {
                        Corruption.AddToPawn(p, 0.01f);
                    }
                }
            }
        }

        public override SkyTarget? SkyTarget(Map map) => new SkyTarget(0.55f, Colors, 1f, 1f);

        public override float SkyTargetLerpFactor(Map map) => GameConditionUtility.LerpInOutValue(this, TransitionTicks, 0.7f);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref centres, "centres", LookMode.Value);
            Scribe_Collections.Look(ref radii, "radii", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                centres = centres ?? new List<IntVec3>();
                radii = radii ?? new List<float>();
            }
        }
    }

    // ======================= Fallen idol (4.28) =======================

    /// <summary>
    /// A fallen idol: comes down like a meteorite and leaks corruption around it, on the corruption map
    /// pass, until it's destroyed. Smashed, it bursts in a wave of loose souls; claimed and
    /// deconstructed, it's quieter but still stains the ground. Either way it leaves corrupted essence.
    /// </summary>
    public class Building_FallenIdol : Building, ICorruptionSource
    {
        public const float Radius = 7.9f;
        public const int Strength = 6;

        public void ApplyCorruption(MapComponent_Corruption c)
        {
            ShrineUtility.Touch(c, Position, Radius, Strength, 8);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Corruption.For(map)?.Register(this);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            IntVec3 pos = Position;
            Corruption.For(map)?.Deregister(this);
            base.DeSpawn(mode);
            if (mode == DestroyMode.KillFinalize)
            {
                GenExplosion.DoExplosion(pos, map, 4.9f, HP_DefOf.HP_SoulBurst, null);
                SoulUtility.DropEssence(pos, map, 15, HP_DefOf.HP_CorruptedEssence);
                Messages.Message("The fallen idol shattered and the souls in it tore loose.", new TargetInfo(pos, map), MessageTypeDefOf.PositiveEvent);
            }
            else if (mode == DestroyMode.Deconstruct)
            {
                Corruption.Spill(pos, map, 80, 3.9f);
            }
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            string line = "Leaks corruption within " + Radius.ToString("0.#") + " cells until it's destroyed. Smash it, or claim and deconstruct it.";
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }
    }

    /// <summary>The meteorite incident, with an idol inside instead of rock.</summary>
    public class IncidentWorker_FallenIdol : IncidentWorker_MeteoriteImpact
    {
        protected override List<Thing> GenerateMeteorContents(IncidentParms parms)
        {
            return new List<Thing> { ThingMaker.MakeThing(HP_DefOf.HP_FallenIdol) };
        }

        protected override Letter MakeLetter(Skyfaller meteorite, List<Thing> contents)
        {
            return LetterMaker.MakeLetter(def.letterLabel, def.letterText, LetterDefOf.ThreatSmall, new TargetInfo(meteorite.Position, meteorite.Map));
        }
    }
}
