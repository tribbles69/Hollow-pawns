using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HollowPawns
{
    /// <summary>Marks a piece of a full-kit set (4.22). Written by Source/tools/gen_apparel_sets.py.</summary>
    public class ApparelSetExtension : DefModExtension
    {
        public string set;
        public string setLabel;
    }

    /// <summary>
    /// Full-kit bonus: wear every piece of one set (the CE-only webbing, backpack and shield don't
    /// count, so the bonus is the same with or without CE) and get the "full kit" hediff. Checked
    /// only when apparel goes on or comes off.
    /// </summary>
    public static class ApparelSets
    {
        private static Dictionary<string, HashSet<ThingDef>> required;

        public static Dictionary<string, HashSet<ThingDef>> Required
        {
            get
            {
                if (required == null)
                {
                    required = new Dictionary<string, HashSet<ThingDef>>();
                    foreach (ThingDef d in DefDatabase<ThingDef>.AllDefs)
                    {
                        ApparelSetExtension ext = d.GetModExtension<ApparelSetExtension>();
                        if (ext == null || d.apparel == null || IsCeOnly(d))
                        {
                            continue;
                        }
                        if (!required.TryGetValue(ext.set, out HashSet<ThingDef> defs))
                        {
                            required[ext.set] = defs = new HashSet<ThingDef>();
                        }
                        defs.Add(d);
                    }
                }
                return required;
            }
        }

        private static readonly HashSet<string> CeLayers = new HashSet<string> { "Webbing", "Backpack", "Shield" };

        private static bool IsCeOnly(ThingDef d) => d.apparel.layers.Any(l => CeLayers.Contains(l.defName));

        public static string CompleteSet(Pawn p)
        {
            List<Apparel> worn = p.apparel?.WornApparel;
            if (worn == null || worn.Count == 0)
            {
                return null;
            }
            foreach (KeyValuePair<string, HashSet<ThingDef>> kv in Required)
            {
                if (kv.Value.All(d => worn.Any(a => a.def == d)))
                {
                    return kv.Key;
                }
            }
            return null;
        }

        public static void Recheck(Pawn p)
        {
            if (p?.health == null || p.Dead)
            {
                return;
            }
            string set = CompleteSet(p);
            Hediff h = p.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_FullKit);
            if (set != null && h == null)
            {
                p.health.AddHediff(HP_DefOf.HP_FullKit);
            }
            else if (set == null && h != null)
            {
                p.health.RemoveHediff(h);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelAdded))]
    public static class Patch_ApparelAdded_FullKit
    {
        public static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            if (apparel.def.HasModExtension<ApparelSetExtension>())
            {
                ApparelSets.Recheck(__instance.pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelRemoved))]
    public static class Patch_ApparelRemoved_FullKit
    {
        public static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            if (apparel.def.HasModExtension<ApparelSetExtension>())
            {
                ApparelSets.Recheck(__instance.pawn);
            }
        }
    }
}
