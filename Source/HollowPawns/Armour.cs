using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HollowPawns
{
    [DefOf]
    public static class HP_StatDefOf
    {
        /// <summary>Added to the reaper's chance to tear a soul out (the shroud).</summary>
        public static StatDef HP_ReapPower;

        /// <summary>Cuts the chance of having your soul reaped, as a fraction (the bone fetish).</summary>
        public static StatDef HP_ReapWard;

        static HP_StatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HP_StatDefOf));
        }
    }

    // ======================= Reliquary armour (4.12) =======================

    public class CompProperties_ReliquaryArmour : CompProperties
    {
        public int maxCharges = 2;

        public CompProperties_ReliquaryArmour()
        {
            compClass = typeof(CompReliquaryArmour);
        }
    }

    /// <summary>
    /// Reliquary armour: holds essence charges. When the wearer would die, one charge is burned and they
    /// are held at the edge instead, downed, for a day (HP_ReliquaryWard, which prevents death). Checked
    /// only when Pawn.Kill is called; recharged at the soul crucible.
    /// </summary>
    public class CompReliquaryArmour : ThingComp
    {
        public int charges;

        public CompProperties_ReliquaryArmour Props => (CompProperties_ReliquaryArmour)props;

        public bool Full => charges >= Props.maxCharges;

        public override void PostPostMake()
        {
            charges = Props.maxCharges;
        }

        public void Refill() => charges = Props.maxCharges;

        public override string CompInspectStringExtra() => "Essence charges: " + charges + " / " + Props.maxCharges;

        public override string TransformLabel(string label) => label + " (" + charges + "/" + Props.maxCharges + ")";

        public override bool AllowStackWith(Thing other) => false;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref charges, "charges");
        }
    }

    public static class ReliquaryArmour
    {
        public static CompReliquaryArmour ChargedOn(Pawn p)
        {
            List<Apparel> worn = p.apparel?.WornApparel;
            if (worn == null)
            {
                return null;
            }
            foreach (Apparel a in worn)
            {
                CompReliquaryArmour c = a.TryGetComp<CompReliquaryArmour>();
                if (c != null && c.charges > 0)
                {
                    return c;
                }
            }
            return null;
        }
    }

    /// <summary>The death that reliquary armour cancels. Runs first, so nothing else sees a death that didn't happen.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_ReliquaryArmour
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Pawn __instance, Hediff exactCulprit)
        {
            Pawn p = __instance;
            if (p.Dead || !p.Spawned || p.health == null)
            {
                return true;
            }
            // Taking the soul is death the armour can't stop.
            if (exactCulprit?.Part?.def == HP_DefOf.HP_Soul)
            {
                return true;
            }
            if (p.health.hediffSet.HasHediff(HP_DefOf.HP_ReliquaryWard))
            {
                return false;
            }
            CompReliquaryArmour armour = ReliquaryArmour.ChargedOn(p);
            if (armour == null)
            {
                return true;
            }
            armour.charges--;
            p.health.AddHediff(HP_DefOf.HP_ReliquaryWard);
            MoteMaker.ThrowText(p.DrawPos, p.Map, "Held back", new UnityEngine.Color(0.6f, 0.9f, 1f));
            Messages.Message(p.LabelShort + "'s reliquary armour burned a charge of essence and held them back from death. They have a day to be saved.",
                p, MessageTypeDefOf.NegativeHealthEvent);
            return false;
        }
    }

    /// <summary>Recharging keeps the armour: the bill consumes only the essence.</summary>
    public class RecipeWorker_RechargeReliquary : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient.TryGetComp<CompReliquaryArmour>() != null)
            {
                return;
            }
            base.ConsumeIngredient(ingredient, recipe, map);
        }
    }

    /// <summary>Hands the recharged armour back as the bill's product.</summary>
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_GenRecipe_RechargeReliquary
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients)
        {
            if (recipeDef.Worker is RecipeWorker_RechargeReliquary)
            {
                Thing armour = ingredients?.FirstOrDefault(t => t.TryGetComp<CompReliquaryArmour>() != null);
                if (armour != null)
                {
                    if (armour.Spawned)
                    {
                        armour.DeSpawn();
                    }
                    armour.TryGetComp<CompReliquaryArmour>().Refill();
                    yield return armour;
                }
            }
            foreach (Thing t in __result)
            {
                yield return t;
            }
        }
    }

    /// <summary>Lets the recharge bill skip armour that's already full.</summary>
    public class SpecialThingFilterWorker_FullReliquary : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t) => t.TryGetComp<CompReliquaryArmour>()?.Full ?? false;

        public override bool CanEverMatch(ThingDef def) => def.HasComp(typeof(CompReliquaryArmour));
    }
}
