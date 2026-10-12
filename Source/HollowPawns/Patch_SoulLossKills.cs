using HarmonyLib;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// No soul = death, whatever removed it. Vanilla only kills for a missing part through a
    /// lethal capacity or the core part, so a missing soul alone would leave the pawn alive.
    /// Our own paths (surgery, scythe) kill directly; this catches everything else: dev tools,
    /// other mods' surgeries, anything that adds a missing-part hediff to the soul.
    /// Runs only when a hediff is added, so it costs nothing per tick.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff),
        new[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Patch_SoulLossKills
    {
        public static void Postfix(Pawn ___pawn, Hediff hediff, DamageInfo? dinfo)
        {
            if (___pawn == null || ___pawn.Dead || !(hediff is Hediff_MissingPart))
            {
                return;
            }
            if (hediff.Part?.def != HP_DefOf.HP_Soul || !___pawn.health.hediffSet.PartIsMissing(hediff.Part))
            {
                return;
            }
            ___pawn.Kill(dinfo, hediff);
        }
    }
}
