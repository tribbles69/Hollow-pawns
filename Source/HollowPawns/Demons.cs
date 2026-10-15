using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// Makes sure the demon faction exists. New games get it from world generation
    /// (requiredCountAtGameStart); saves made before demons existed get it added here, once, on load.
    /// </summary>
    public class GameComponent_Demons : GameComponent
    {
        public GameComponent_Demons(Game game)
        {
        }

        public override void FinalizeInit()
        {
            FactionDef def = HP_DefOf.HP_Demons;
            if (def == null || Find.FactionManager == null || Find.FactionManager.FirstFactionOfDef(def) != null)
            {
                return;
            }
            Faction demons = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
            // permanentEnemy: the generator already makes it hostile to everyone.
            Find.FactionManager.Add(demons);
            Log.Message("[Hollow Pawns] Added the demon faction to this save.");
        }
    }

    /// <summary>A raid that is always demons. Used by the hell gate and the Hell Unbound storyteller.</summary>
    public class IncidentWorker_DemonRaid : IncidentWorker_RaidEnemy
    {
        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            parms.faction = HP_DefOf.HP_Demons == null ? null : Find.FactionManager.FirstFactionOfDef(HP_DefOf.HP_Demons);
            return parms.faction != null;
        }
    }

    /// <summary>
    /// The hell gate: open it and it pulls a demon raid through every few days. A deliberate way to
    /// farm corrupted souls. Counts down on the rare tick only; does nothing while closed.
    /// </summary>
    public class Building_HellGate : Building
    {
        /// <summary>Days between waves while the gate is open.</summary>
        public const float DaysBetweenWaves = 3f;

        /// <summary>Gate waves hit a little harder than a normal raid at the same wealth.</summary>
        public const float PointsFactor = 1.1f;

        /// <summary>Average corruption over the gate and the ground around it needed to open it (4.28).</summary>
        public const int CorruptionToOpen = 100;

        private bool open;
        private int ticksToWave = -1;

        /// <summary>Average corruption over the gate's footprint and two cells around it.</summary>
        public int GroundCorruption
        {
            get
            {
                int sum = 0, n = 0;
                foreach (IntVec3 c in this.OccupiedRect().ExpandedBy(2))
                {
                    if (c.InBounds(Map))
                    {
                        sum += Corruption.At(c, Map);
                        n++;
                    }
                }
                return n == 0 ? 0 : sum / n;
            }
        }

        public override void TickRare()
        {
            base.TickRare();
            if (!open || !Spawned)
            {
                return;
            }
            ticksToWave -= GenTicks.TickRareInterval;
            if (ticksToWave > 0)
            {
                return;
            }
            ticksToWave = Mathf.RoundToInt(DaysBetweenWaves * GenDate.TicksPerDay);
            IncidentDef wave = HP_DefOf.HP_DemonWave;
            if (wave == null)
            {
                return;
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(wave.category, Map);
            parms.points *= PointsFactor;
            parms.forced = true;
            if (!wave.Worker.TryExecute(parms))
            {
                Messages.Message("The hell gate shudders, but nothing comes through.", this, MessageTypeDefOf.NeutralEvent);
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
            var toggle = new Command_Toggle
            {
                defaultLabel = open ? "Close gate" : "Open gate",
                defaultDesc = "While open, the gate pulls a demon raid through every " + DaysBetweenWaves
                    + " days. Their corrupted souls are worth taking. Closing it resets the count.",
                icon = ContentFinder<Texture2D>.Get("Things/Building/HP_HellGate"),
                isActive = () => open,
                toggleAction = () =>
                {
                    open = !open;
                    ticksToWave = open ? Mathf.RoundToInt(DaysBetweenWaves * GenDate.TicksPerDay) : -1;
                },
            };
            if (!open && GroundCorruption < CorruptionToOpen)
            {
                toggle.Disable("The gate needs corrupted ground to open (" + GroundCorruption + " / " + CorruptionToOpen + " around it).");
            }
            yield return toggle;
            if (DebugSettings.ShowDevGizmos && open)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Wave now",
                    action = () => ticksToWave = 0,
                };
            }
        }

        public override string GetInspectString()
        {
            var sb = new StringBuilder(base.GetInspectString());
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }
            sb.Append(open
                ? "Open. Next wave in " + Mathf.Max(0, ticksToWave).ToStringTicksToPeriod() + "."
                : "Closed. Ground corruption: " + GroundCorruption + " / " + CorruptionToOpen + " to open.");
            return sb.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref open, "open");
            Scribe_Values.Look(ref ticksToWave, "ticksToWave", -1);
        }
    }
}
