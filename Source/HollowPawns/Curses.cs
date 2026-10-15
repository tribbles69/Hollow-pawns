using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// Curses: debuff conditions that fade on their own. Delivered by demon attacks (a chance per
    /// damaging hit), by purging corrupted essence, and (Hollowed) by wearing a cursed idol.
    /// Everything here is event-driven: it runs when a pawn is hurt or a bill finishes.
    /// </summary>
    public static class CurseUtility
    {
        /// <summary>Chance that a damaging hit from a demon curses the victim.</summary>
        public const float DemonCurseChance = 0.10f;

        private static List<HediffDef> rollable;

        /// <summary>The curses a demon hit or a purge can roll. Hollowed only comes from the idol.</summary>
        public static List<HediffDef> Rollable
        {
            get
            {
                if (rollable == null)
                {
                    rollable = new List<HediffDef>();
                    foreach (string name in new[] { "HP_Curse_Marked", "HP_Curse_RottingLuck", "HP_Curse_SoulSick" })
                    {
                        HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(name);
                        if (def != null)
                        {
                            rollable.Add(def);
                        }
                    }
                }
                return rollable;
            }
        }

        /// <summary>Lays a random curse on the pawn, or refreshes it if they already carry that one.</summary>
        public static void CurseRandom(Pawn pawn, string reason)
        {
            if (pawn?.health == null || pawn.Dead || !pawn.RaceProps.Humanlike || Rollable.Count == 0)
            {
                return;
            }
            HediffDef curse = Rollable.RandomElement();
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(curse);
            if (existing != null)
            {
                existing.Severity = 1f;
            }
            else
            {
                pawn.health.AddHediff(curse);
            }
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message(pawn.LabelShort + " has been cursed (" + curse.label + ") " + reason + ".",
                    pawn, MessageTypeDefOf.NegativeHealthEvent);
            }
        }
    }

    /// <summary>A demon's hit can carry a curse. Runs only when a pawn takes damage.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_Pawn_PostApplyDamage
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (totalDamageDealt <= 0f || __instance.Dead || !(dinfo.Instigator is Pawn attacker)
                || attacker == __instance || !SoulUtility.IsCorrupted(attacker) || SoulUtility.IsCorrupted(__instance))
            {
                return;
            }
            if (Rand.Chance(CurseUtility.DemonCurseChance))
            {
                CurseUtility.CurseRandom(__instance, "by " + attacker.LabelShort);
            }
        }
    }

    /// <summary>
    /// Purge corruption at the crucible: corrupted essence comes out as twice the clean essence
    /// (the bill's products), but whoever did the work may be cursed for it.
    /// </summary>
    public class RecipeWorker_PurgeCorruption : RecipeWorker
    {
        public const float CurseChance = 0.33f;

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            if (billDoer != null && Rand.Chance(CurseChance))
            {
                CurseUtility.CurseRandom(billDoer, "while purging corrupted essence");
            }
        }
    }
}
