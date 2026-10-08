using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// The reaper scythe's "crit". Rides along on every blade hit as an extra melee damage
    /// that does no damage itself; it just rolls to tear the soul out.
    /// Chance scales with the wielder's Melee skill, and is higher against the downed.
    /// Runs only when a hit lands, so it costs nothing per tick.
    /// </summary>
    public class DamageWorker_SoulReap : DamageWorker
    {
        /// <summary>0.5% per Melee level: 5% at Melee 10, 10% at Melee 20.</summary>
        public const float ChancePerMeleeLevel = 0.005f;

        /// <summary>A downed target is far easier to reap: x3.</summary>
        public const float DownedMultiplier = 3f;

        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = new DamageResult();
            if (victim is Pawn pawn && !pawn.Dead && dinfo.Instigator is Pawn reaper)
            {
                if (Rand.Chance(ReapChance(reaper, pawn)))
                {
                    SoulUtility.ReapLiving(pawn, dinfo);
                }
            }
            return result;
        }

        public static float ReapChance(Pawn reaper, Pawn target)
        {
            if (!SoulUtility.HasSoul(target))
            {
                return 0f;
            }
            int melee = reaper.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            float chance = melee * ChancePerMeleeLevel;
            if (target.Downed)
            {
                chance *= DownedMultiplier;
            }
            return Mathf.Clamp01(chance);
        }
    }
}
