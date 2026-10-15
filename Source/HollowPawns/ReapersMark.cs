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

        /// <summary>The Mark button on a prisoner: render, bind, or clear. Wardens take marked prisoners to an altar or press.</summary>
        public static Gizmo MarkGizmo(Pawn pawn)
        {
            SoulFate fate = SoulUtility.GetFate(pawn);
            var cmd = new Command_Action
            {
                defaultLabel = FateLabel(fate),
                defaultDesc = "Mark this prisoner. Wardens carry it out.\n\n"
                    + "Render: at a Bone Altar or Soul Press, plain soul essence, more for a better soul. Who they were is lost.\n"
                    + "Bind: at an altar or press, the whole soul goes into an empty vessel (urn, canister or lattice), named and graded. "
                    + "Needs an empty vessel.\n"
                    + "Battery: locked in a prisoner battery and drained for power over days, then they die and leave essence.\n"
                    + "Effigy (blood debtors only): at an altar or press, their living soul is bound into a wailing effigy.\n\nEvery fate ends in death.",
                icon = MarkIcon,
                action = () =>
                {
                    var options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("Render (essence)", () => SoulUtility.SetFate(pawn, SoulFate.Render)),
                        new FloatMenuOption("Bind (into a vessel)", () => SoulUtility.SetFate(pawn, SoulFate.Bind)),
                        new FloatMenuOption("Battery (drain for power)", () => SoulUtility.SetFate(pawn, SoulFate.Battery)),
                    };
                    if (BloodDebts.IsDebtor(pawn))
                    {
                        options.Add(new FloatMenuOption("Effigy (bind into a wailing effigy)", () => SoulUtility.SetFate(pawn, SoulFate.Effigy)));
                    }
                    if (SoulUtility.IsMarked(pawn))
                    {
                        options.Add(new FloatMenuOption("Clear mark", () => SoulUtility.ClearFate(pawn)));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                },
            };
            if (SoulUtility.IsSoulLocked(pawn))
            {
                cmd.Disable("Soul-Locked.");
            }
            return cmd;
        }

        private static string FateLabel(SoulFate fate)
        {
            switch (fate)
            {
                case SoulFate.Render: return "Marked: render";
                case SoulFate.Bind: return "Marked: bind";
                case SoulFate.Battery: return "Marked: battery";
                case SoulFate.Effigy: return "Marked: effigy";
                default: return "Mark soul";
            }
        }

        /// <summary>The Soul-Lock toggle: the safety catch.</summary>
        public static Gizmo LockGizmo(Pawn pawn)
        {
            return new Command_Toggle
            {
                defaultLabel = "Soul-Lock",
                defaultDesc = "While locked, this prisoner can't be marked, Mark all skips them, and locking clears any mark already on them.",
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
            if (pawn.Dead || !pawn.IsPrisonerOfColony || SoulUtility.GetStatus(pawn) != SoulUtility.SoulStatus.Intact)
            {
                yield break;
            }
            yield return ReapersMark.MarkGizmo(pawn);
            yield return ReapersMark.LockGizmo(pawn);
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
