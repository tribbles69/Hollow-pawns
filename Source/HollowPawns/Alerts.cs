using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// Ensouled corpses lying around outside storage on a home map. Your own dead are left out:
    /// that call is yours to make. Vanilla's alert readout throttles how often this runs.
    /// </summary>
    public class Alert_EnsouledCorpseUnclaimed : Alert
    {
        private readonly List<Thing> culprits = new List<Thing>();

        public Alert_EnsouledCorpseUnclaimed()
        {
            defaultLabel = "Ensouled corpse unclaimed";
            defaultPriority = AlertPriority.Medium;
        }

        public override string GetLabel()
        {
            return culprits.Count == 1 ? defaultLabel : "Ensouled corpses unclaimed (" + culprits.Count + ")";
        }

        public override TaggedString GetExplanation()
        {
            return "These corpses still hold a soul, worth up to " + SoulUtility.EssenceFromDeadSoul
                + " essence each at the soul crucible (half that at a bone altar), less as they rot. Nobody has stored them yet.";
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            if (!HP_DefOf.HP_SoulExtraction.IsFinished)
            {
                return false;
            }
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                {
                    continue;
                }
                foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
                {
                    if (t is Corpse corpse && corpse.InnerPawn.Faction != Faction.OfPlayer
                        && !corpse.IsInValidStorage() && SoulUtility.HasSoul(corpse.InnerPawn))
                    {
                        culprits.Add(corpse);
                    }
                }
            }
            return AlertReport.CulpritsAre(culprits);
        }
    }

    /// <summary>
    /// Ensouled corpses that will start to rot within a day where they lie, with the essence that's
    /// about to be lost. Frozen corpses never show. Vanilla's alert readout throttles how often this runs.
    /// </summary>
    public class Alert_EnsouledCorpsesSpoiling : Alert
    {
        private readonly List<Thing> culprits = new List<Thing>();
        private int atRisk;

        public Alert_EnsouledCorpsesSpoiling()
        {
            defaultLabel = "Souls spoiling";
            defaultPriority = AlertPriority.Medium;
        }

        public override string GetLabel() => "Souls spoiling (~" + atRisk + " essence)";

        public override TaggedString GetExplanation()
        {
            return culprits.Count + " ensouled corpse" + (culprits.Count == 1 ? " is" : "s are") + " going to rot within a day. A rotting soul gives "
                + SoulUtility.EssenceFromRottingSoul + " essence instead of " + SoulUtility.EssenceFromDeadSoul
                + ", so about " + atRisk + " essence will be lost.\n\nTake their souls now, or get them somewhere cold.";
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            atRisk = 0;
            if (!HP_DefOf.HP_SoulExtraction.IsFinished)
            {
                return false;
            }
            int loss = SoulUtility.EssenceFromDeadSoul - SoulUtility.EssenceFromRottingSoul;
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                {
                    continue;
                }
                foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
                {
                    if (!(t is Corpse corpse) || corpse.InnerPawn.Faction == Faction.OfPlayer || !SoulUtility.HasSoul(corpse.InnerPawn))
                    {
                        continue;
                    }
                    CompRottable rot = corpse.GetComp<CompRottable>();
                    if (rot == null || rot.Stage != RotStage.Fresh || rot.TicksUntilRotAtCurrentTemp > GenDate.TicksPerDay)
                    {
                        continue;
                    }
                    culprits.Add(corpse);
                    atRisk += loss;
                }
            }
            return AlertReport.CulpritsAre(culprits);
        }
    }

    /// <summary>Soul generators with about a day of essence or less left.</summary>
    public class Alert_SoulGeneratorLow : Alert
    {
        /// <summary>The generator burns 2 essence a day, so this is roughly a day left.</summary>
        public const float LowFuel = 2f;

        private readonly List<Thing> culprits = new List<Thing>();

        public Alert_SoulGeneratorLow()
        {
            defaultLabel = "Soul generator low";
            defaultPriority = AlertPriority.Medium;
        }

        public override TaggedString GetExplanation()
        {
            var lines = new List<string> { "These soul generators have about a day of essence or less left. Refill them, or they go dark:" };
            foreach (Thing t in culprits)
            {
                CompRefuelable fuel = t.TryGetComp<CompRefuelable>();
                float perDay = fuel?.Props.fuelConsumptionRate ?? 0f;
                lines.Add("  - " + t.LabelCap + ": dark in about " + (perDay > 0f ? Mathf.CeilToInt(fuel.Fuel / perDay * 24f) + " hours" : "a while"));
            }
            lines.Add("");
            lines.Add(Forecast());
            return string.Join("\n", lines);
        }

        /// <summary>How long the essence on the home maps would keep every running generator going.</summary>
        private static string Forecast()
        {
            float stock = 0f, burn = 0f;
            foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
            {
                stock += map.resourceCounter.GetCount(HP_DefOf.HP_SoulEssence);
                foreach (Thing t in map.listerThings.ThingsOfDef(HP_DefOf.HP_SoulGenerator))
                {
                    CompRefuelable fuel = t.TryGetComp<CompRefuelable>();
                    CompFlickable flick = t.TryGetComp<CompFlickable>();
                    if (t.Faction == Faction.OfPlayer && fuel != null && (flick == null || flick.SwitchIsOn))
                    {
                        burn += fuel.Props.fuelConsumptionRate;
                    }
                }
            }
            if (burn <= 0f)
            {
                return "No generators are running.";
            }
            return "Stored essence (" + Mathf.FloorToInt(stock) + ") would keep every running generator going for about "
                + (stock / burn).ToString("0.#") + " days.";
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            foreach (Map map in Find.Maps)
            {
                foreach (Thing t in map.listerThings.ThingsOfDef(HP_DefOf.HP_SoulGenerator))
                {
                    CompRefuelable fuel = t.TryGetComp<CompRefuelable>();
                    if (t.Faction == Faction.OfPlayer && fuel != null && fuel.Fuel < LowFuel)
                    {
                        culprits.Add(t);
                    }
                }
            }
            return AlertReport.CulpritsAre(culprits);
        }
    }
}
