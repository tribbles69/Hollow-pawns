using System.Collections.Generic;
using RimWorld;
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
                + " essence each at the soul crucible, less as they rot. Nobody has stored them yet.";
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
            return "These soul generators have about a day of essence or less left. Refill them, or they go dark.";
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
