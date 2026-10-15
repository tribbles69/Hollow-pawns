using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// The Reaper's Mark: know which bodies still hold a soul, take one in a click, never by accident.
    /// Every patch here runs only when the player looks at or clicks something, never per tick.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ReapersMark
    {
        public static readonly Texture2D MarkIcon = ContentFinder<Texture2D>.Get("Things/Item/HP_SoulEssence");
        public static readonly Texture2D LockIcon = TexCommand.ForbidOn;

        /// <summary>The Mark toggle on a prisoner: queue or cancel extraction.</summary>
        public static Gizmo MarkGizmo(Pawn pawn)
        {
            var toggle = new Command_Toggle
            {
                defaultLabel = "Mark soul",
                defaultDesc = "Queue the extract soul surgery on this prisoner. They will die, and their soul becomes "
                    + SoulUtility.EssenceFromLivingSoul + " soul essence.\n\nToggle off to cancel.",
                icon = MarkIcon,
                isActive = () => SoulUtility.IsMarked(pawn),
                toggleAction = () =>
                {
                    if (SoulUtility.IsMarked(pawn))
                    {
                        SoulUtility.Unmark(pawn);
                    }
                    else
                    {
                        SoulUtility.Mark(pawn);
                    }
                },
            };
            if (SoulUtility.IsSoulLocked(pawn))
            {
                toggle.Disable("Soul-Locked.");
            }
            return toggle;
        }

        /// <summary>The Soul-Lock toggle: the safety catch.</summary>
        public static Gizmo LockGizmo(Pawn pawn)
        {
            return new Command_Toggle
            {
                defaultLabel = "Soul-Lock",
                defaultDesc = "While locked, this pawn's soul can't be marked or extracted, and locking cancels any extraction already queued.",
                icon = LockIcon,
                isActive = () => SoulUtility.IsSoulLocked(pawn),
                toggleAction = () => SoulUtility.SetSoulLocked(pawn, !SoulUtility.IsSoulLocked(pawn)),
            };
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (Gizmo g in gizmos)
            {
                yield return g;
            }
            Pawn pawn = __instance;
            if (!HP_DefOf.HP_SoulExtraction.IsFinished || pawn.Dead
                || SoulUtility.GetStatus(pawn) != SoulUtility.SoulStatus.Intact)
            {
                yield break;
            }
            bool prisoner = pawn.IsPrisonerOfColony;
            if (!prisoner && pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            if (prisoner)
            {
                yield return ReapersMark.MarkGizmo(pawn);
            }
            yield return ReapersMark.LockGizmo(pawn);
        }
    }

    /// <summary>
    /// Every way of queuing extract soul goes through here: refuse Soul-Locked pawns, and
    /// ask before queuing it on one of the colony's own.
    /// </summary>
    [HarmonyPatch(typeof(HealthCardUtility), nameof(HealthCardUtility.CreateSurgeryBill))]
    public static class Patch_HealthCardUtility_CreateSurgeryBill
    {
        private static bool confirmed;

        public static bool Prefix(Pawn medPawn, RecipeDef recipe, BodyPartRecord part, List<Thing> uniqueIngredients,
            bool sendMessages, ref Bill_Medical __result)
        {
            if (!SoulUtility.IsSoulSurgery(recipe) || confirmed)
            {
                return true;
            }
            if (SoulUtility.IsSoulLocked(medPawn))
            {
                Messages.Message(medPawn.LabelShort + " is Soul-Locked.", medPawn, MessageTypeDefOf.RejectInput, false);
                __result = null;
                return false;
            }
            if (medPawn.Faction != Faction.OfPlayer)
            {
                return true;
            }
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "Take " + medPawn.LabelShort + "'s soul (" + recipe.label + ")? " + medPawn.LabelShort + " is one of your own, and will die.",
                () =>
                {
                    confirmed = true;
                    try
                    {
                        HealthCardUtility.CreateSurgeryBill(medPawn, recipe, part, uniqueIngredients, sendMessages);
                    }
                    finally
                    {
                        confirmed = false;
                    }
                },
                destructive: true));
            __result = null;
            return false;
        }
    }

    /// <summary>"Soul: intact (8 essence)" or "Soul: taken" on adult human corpses.</summary>
    [HarmonyPatch(typeof(Corpse), nameof(Corpse.GetInspectString))]
    public static class Patch_Corpse_GetInspectString
    {
        public static void Postfix(Corpse __instance, ref string __result)
        {
            SoulUtility.SoulStatus status = SoulUtility.GetStatus(__instance.InnerPawn);
            if (status == SoulUtility.SoulStatus.None)
            {
                return;
            }
            string line;
            if (status == SoulUtility.SoulStatus.Taken)
            {
                line = "Soul: taken";
            }
            else if (SoulUtility.IsCorrupted(__instance.InnerPawn))
            {
                line = "Soul: corrupted (" + SoulUtility.DeadSoulYield(__instance) + " corrupted essence)";
            }
            else
            {
                line = "Soul: intact (" + SoulUtility.DeadSoulYield(__instance) + " essence)";
            }
            __result = __result.NullOrEmpty() ? line : __result.TrimEnd() + "\n" + line;
        }
    }
}
