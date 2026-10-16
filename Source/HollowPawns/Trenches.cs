using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// Trenches (4.25): dug terrain that protects whoever stands in it. Vanilla cover is a block chance
    /// from cover in neighbouring cells, so a trench adds its own: about 55% from every direction, except
    /// against a shooter within three cells (on the lip, firing down into it). Under CE, cover is height:
    /// a pawn in a trench shows only head and shoulders, the same way CE crouches pawns behind cover.
    /// All of it runs only when a shot or a cover search is worked out.
    /// </summary>
    public static class Trenches
    {
        public const float BlockChance = 0.55f;
        public const float CloseRange = 2.9f;

        /// <summary>Under CE: the top of a pawn's exposed height while in a trench.</summary>
        public const float CEExposedHeight = 0.55f;

        public static bool IsTrench(IntVec3 c, Map map) => map != null && c.InBounds(map) && HP_DefOf.HP_Trench != null && c.GetTerrain(map) == HP_DefOf.HP_Trench;

        public static bool InTrench(Pawn p) => p != null && p.Spawned && !p.Flying && IsTrench(p.Position, p.Map);
    }

    [HarmonyPatch(typeof(CoverUtility), nameof(CoverUtility.CalculateOverallBlockChance))]
    public static class Patch_CoverUtility_BlockChance_Trench
    {
        public static void Postfix(LocalTargetInfo target, IntVec3 shooterLoc, Map map, ref float __result)
        {
            if (target.Thing is Pawn p && Trenches.InTrench(p) && !shooterLoc.InHorDistOf(p.Position, Trenches.CloseRange))
            {
                // Combine with any cover around the trench rather than replacing it.
                __result = 1f - (1f - __result) * (1f - Trenches.BlockChance);
            }
        }
    }

    /// <summary>The AI's cover search counts a trench cell as good cover, so raiders and drafted pawns use it.</summary>
    [HarmonyPatch(typeof(CoverUtility), nameof(CoverUtility.TotalSurroundingCoverScore))]
    public static class Patch_CoverUtility_CoverScore_Trench
    {
        public static void Postfix(IntVec3 c, Map map, ref float __result)
        {
            if (Trenches.IsTrench(c, map))
            {
                __result += 1.5f;
            }
        }
    }

    /// <summary>
    /// CE: CollisionVertical.CalculateHeightRange is private, so it's patched by hand when CE is loaded.
    /// A pawn in a trench has its collision top cut to head height, and shoots from just below it.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class TrenchesCE
    {
        static TrenchesCE()
        {
            System.Type cv = AccessTools.TypeByName("CombatExtended.CollisionVertical");
            var m = cv == null ? null : AccessTools.Method(cv, "CalculateHeightRange");
            if (m != null)
            {
                new Harmony("aaronclews.hollowpawns.trenchce").Patch(m, postfix: new HarmonyMethod(typeof(TrenchesCE), nameof(Postfix)));
            }
        }

        public static void Postfix(Thing thing, ref FloatRange heightRange, ref float shotHeight)
        {
            if (thing is Pawn p && Trenches.InTrench(p) && heightRange.max > Trenches.CEExposedHeight)
            {
                heightRange = new FloatRange(heightRange.min, Mathf.Max(heightRange.min, Trenches.CEExposedHeight));
                shotHeight = Mathf.Min(shotHeight, Trenches.CEExposedHeight - 0.05f);
            }
        }
    }
}
