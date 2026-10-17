using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace HollowPawns
{
    /// <summary>Matches corpses with no soul left in them.</summary>
    public class SpecialThingFilterWorker_Soulless : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return t is Corpse corpse && !SoulUtility.HasSoul(corpse.InnerPawn);
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def.IsCorpse;
        }
    }

    /// <summary>Matches corpses whose soul is still in them: the ones worth taking to the crucible.</summary>
    public class SpecialThingFilterWorker_Ensouled : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return t is Corpse corpse && SoulUtility.HasSoul(corpse.InnerPawn);
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def.IsCorpse;
        }
    }

    /// <summary>
    /// Saves from before 17 Oct 2026 had three corpse filters (not a debtor, no face, child) switched off in
    /// every storage and bill filter, because they were off by default; between them they refused nearly
    /// every corpse, dumping zones and hoppers included. They're on by default now (the recipes that need
    /// them disallow them explicitly), and this takes them back out of filters loaded from an old save.
    /// None of the three can be set by hand, so nothing the player chose is undone.
    /// </summary>
    [HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.ExposeData))]
    public static class Patch_ThingFilter_ExposeData_CorpseFilterFix
    {
        private static readonly AccessTools.FieldRef<ThingFilter, List<SpecialThingFilterDef>> Disallowed =
            AccessTools.FieldRefAccess<ThingFilter, List<SpecialThingFilterDef>>("disallowedSpecialFilters");

        private static readonly string[] Fixed = { "HP_NotDebtorCorpse", "HP_NoFace", "HP_AllowChildCorpses" };

        public static void Postfix(ThingFilter __instance)
        {
            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                return;
            }
            List<SpecialThingFilterDef> list = Disallowed(__instance);
            if (list == null || list.Count == 0)
            {
                return;
            }
            list.RemoveAll(sf => sf != null && System.Array.IndexOf(Fixed, sf.defName) >= 0);
        }
    }
}
