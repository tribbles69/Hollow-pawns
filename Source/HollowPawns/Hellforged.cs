using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// On a weapon ThingDef or a DamageDef: every hit that lands stains the victim's soul and the ground
    /// under them, like a demon's own hits (Patch_Pawn_PostApplyDamage_CorruptedHit). The hellforged
    /// blades and hellfire blasts carry it.
    /// </summary>
    public class CorruptingExtension : DefModExtension
    {
        public float pawnCorruption = 0.015f;
        public int groundSpill = 4;
    }

    /// <summary>
    /// On a weapon: every kill made with it feeds the wielder's bloodrage (HP_Bloodrage), and a little of
    /// the killing stays on their soul. The hellforged blades carry it.
    /// </summary>
    public class BloodTitheExtension : DefModExtension
    {
        /// <summary>Bloodrage per humanlike kill; animals and mechs give a third.</summary>
        public float rage = 0.25f;
        public float corruptionPerKill = 0.02f;
    }

    public static class Hellforged
    {
        private static readonly Color BloodText = new Color(0.85f, 0.15f, 0.1f);

        /// <summary>The corrupting extension on this hit's weapon or damage type, if either has one.</summary>
        public static CorruptingExtension CorruptingOf(DamageInfo dinfo) =>
            dinfo.Weapon?.GetModExtension<CorruptingExtension>() ?? dinfo.Def?.GetModExtension<CorruptingExtension>();

        public static void Tithe(Pawn killer, Pawn victim, BloodTitheExtension tithe)
        {
            if (killer.health == null || killer.Dead)
            {
                return;
            }
            float rage = victim.RaceProps.Humanlike ? tithe.rage : tithe.rage / 3f;
            Hediff h = killer.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_Bloodrage);
            if (h == null)
            {
                h = killer.health.AddHediff(HP_DefOf.HP_Bloodrage);
                h.Severity = rage;
            }
            else
            {
                h.Severity = Mathf.Min(h.def.maxSeverity, h.Severity + rage);
            }
            Corruption.AddToPawn(killer, tithe.corruptionPerKill);
            if (killer.Spawned)
            {
                MoteMaker.ThrowText(killer.DrawPos, killer.Map, h.Severity >= 0.9f ? "BLOOD!" : "Blood", BloodText);
            }
        }
    }

    /// <summary>
    /// Kills made with a blood-tithe weapon feed the wielder's bloodrage. The weapon is read from the
    /// killing blow before the death, when the damage info still names it.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_BloodTithe
    {
        public static void Prefix(Pawn __instance, DamageInfo? dinfo, out Pawn __state)
        {
            __state = null;
            if (!__instance.Dead && dinfo?.Instigator is Pawn killer && killer != __instance
                && dinfo.Value.Weapon?.GetModExtension<BloodTitheExtension>() != null)
            {
                __state = killer;
            }
        }

        public static void Postfix(Pawn __instance, DamageInfo? dinfo, Pawn __state)
        {
            if (__state != null && __instance.Dead)
            {
                Hellforged.Tithe(__state, __instance, dinfo.Value.Weapon.GetModExtension<BloodTitheExtension>());
            }
        }
    }

    /// <summary>Black ichor, drunk: the dose stains the drinker's soul.</summary>
    public class IngestionOutcomeDoer_Corrupt : IngestionOutcomeDoer
    {
        public float amount = 0.04f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            Corruption.AddToPawn(pawn, amount * ingestedCount);
        }
    }
}
