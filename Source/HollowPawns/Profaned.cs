using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// A shard of a Cursebearer's soul, planted in a body it raised (4.32). Always a wretched soul;
    /// its residue is the Cursebearer's, not the dead pawn's. When the Cursebearer dies, its shards go.
    /// </summary>
    public class Hediff_SoulShard : HediffWithComps
    {
        public int casterId = -1;
        public string casterName;
        public string residue;

        public override string LabelInBrackets => casterName;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref casterId, "casterId", -1);
            Scribe_Values.Look(ref casterName, "casterName");
            Scribe_Values.Look(ref residue, "residue");
        }
    }

    /// <summary>
    /// Compatibility with The Profaned (BotchJob.Profaned). Does nothing unless that mod is loaded:
    /// its genes are looked up by name, and its revival is patched by hand only if its type exists.
    /// - Profaned have souls, corrupted ones (SoulUtility.IsCorrupted).
    /// - A Cursebearer raising the dead plants a shard of its own soul in the body.
    /// - When a Cursebearer dies, everything it raised collapses (one check, on its death).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Profaned
    {
        public static readonly GeneDef Undead = DefDatabase<GeneDef>.GetNamedSilentFail("BotchJob_UndeadPsysiology");
        public static readonly GeneDef Cursebearer = DefDatabase<GeneDef>.GetNamedSilentFail("BotchJob_Cursebearer");

        static Profaned()
        {
            Type util = AccessTools.TypeByName("Profaned.Utility_Profaned");
            if (util == null)
            {
                return;
            }
            var bestow = AccessTools.Method(util, "BestowCurse");
            if (bestow == null)
            {
                Log.Warning("[Hollow Pawns] The Profaned is loaded but Utility_Profaned.BestowCurse wasn't found; soul shards are off.");
                return;
            }
            new Harmony("aaronclews.hollowpawns.profaned").Patch(bestow, postfix: new HarmonyMethod(typeof(Profaned), nameof(BestowCursePostfix)));
            Log.Message("[Hollow Pawns] The Profaned found: Cursebearers plant soul shards.");
        }

        public static bool IsProfaned(Pawn p) => Undead != null && p?.genes != null && p.genes.HasActiveGene(Undead);

        public static Hediff_SoulShard ShardOf(Pawn p) => p?.health?.hediffSet?.GetFirstHediffOfDef(HP_DefOf.HP_SoulShard) as Hediff_SoulShard;

        /// <summary>After the curse: the body gets a soul back, but it's a shard of the caster's.</summary>
        public static void BestowCursePostfix(Pawn caster, Pawn recipient)
        {
            if (caster == null || recipient == null || recipient.Dead || !recipient.RaceProps.Humanlike || !recipient.DevelopmentalStage.Adult())
            {
                return;
            }
            BodyPartRecord soulPart = recipient.RaceProps.body.AllParts.FirstOrDefault(p => p.def == HP_DefOf.HP_Soul);
            if (soulPart != null && recipient.health.hediffSet.PartIsMissing(soulPart))
            {
                recipient.health.RestorePart(soulPart);
            }
            Hediff_SoulShard old = ShardOf(recipient);
            if (old != null)
            {
                recipient.health.RemoveHediff(old);
            }
            var shard = (Hediff_SoulShard)HediffMaker.MakeHediff(HP_DefOf.HP_SoulShard, recipient);
            shard.casterId = caster.thingIDNumber;
            shard.casterName = caster.LabelShort;
            shard.residue = SoulGrades.ResidueOf(caster);
            recipient.health.AddHediff(shard);
        }

        /// <summary>A Cursebearer has died: every body it raised on a loaded map loses its shard and falls.</summary>
        public static void OnPawnDied(Pawn dead)
        {
            if (Cursebearer == null || dead?.genes == null || !dead.genes.HasActiveGene(Cursebearer))
            {
                return;
            }
            int fell = 0;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned.ToList())
                {
                    if (!p.Dead && ShardOf(p)?.casterId == dead.thingIDNumber)
                    {
                        HealthUtility.AdjustSeverity(p, HP_DefOf.HP_SoulShock, 1f);
                        SoulUtility.TakeSoulAndKill(p);
                        fell++;
                    }
                }
            }
            if (fell > 0)
            {
                Messages.Message("The Cursebearer " + dead.LabelShort + " is dead. " + fell + " of the bodies it raised collapsed.", MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
