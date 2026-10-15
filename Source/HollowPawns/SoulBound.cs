using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HollowPawns
{
    public class CompProperties_SoulBound : CompProperties
    {
        public CompProperties_SoulBound()
        {
            compClass = typeof(CompSoulBound);
        }
    }

    /// <summary>
    /// A soul bound into a weapon or a piece of apparel (4.35). Added to every weapon and apparel def at
    /// startup, so it works on anyone's gear; empty (and costless) until a soul is bound in. The soul's
    /// grade sets how strong the bonus is and its residue sets which stat. The bonus reaches the wielder
    /// through the game's own gear stat offsets (Patch_StatWorker_StatOffsetFromGear), so it is only
    /// worked out when a stat is read. Nothing ticks. Breaking it lets the soul out.
    /// </summary>
    public class CompSoulBound : ThingComp
    {
        public BoundSoul soul;

        public bool Bound => soul != null;

        public void Bind(BoundSoul s)
        {
            soul = s;
            SoulBoundUtility.Register(parent);
        }

        public override string TransformLabel(string label) => Bound ? label + " (soul-bound)" : label;

        public override string CompInspectStringExtra()
        {
            if (!Bound)
            {
                return null;
            }
            return "Bound: " + soul.Describe() + "\n" + SoulBoundUtility.BonusText(soul);
        }

        public override string GetDescriptionPart()
        {
            return Bound ? "Bound within is the soul of " + soul.ShortName + ", taken " + soul.dateTaken + ".\n" + SoulBoundUtility.BonusText(soul) : null;
        }

        public override bool AllowStackWith(Thing other) => !Bound && !(other.TryGetComp<CompSoulBound>()?.Bound ?? false);

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!Bound)
            {
                return;
            }
            SoulBoundUtility.Unregister(parent);
            if (mode == DestroyMode.KillFinalize && previousMap != null)
            {
                // Broken: the soul gets out. (Ghosts come with 4.8; for now, the stain it leaves.)
                Corruption.Spill(parent.Position, previousMap, 60, 2.9f);
                Messages.Message(parent.LabelNoCount.CapitalizeFirst() + " broke. The soul of " + soul.ShortName + " is loose.",
                    new TargetInfo(parent.Position, previousMap), MessageTypeDefOf.NegativeEvent);
            }
        }

        public override void PostExposeData()
        {
            Scribe_Deep.Look(ref soul, "boundSoul");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && soul != null)
            {
                SoulBoundUtility.Register(parent);
            }
        }
    }

    public static class SoulBoundUtility
    {
        /// <summary>Thing IDs of everything with a soul bound in, so the stat hook can skip the rest in O(1).</summary>
        private static readonly HashSet<int> bound = new HashSet<int>();

        public static void Register(Thing t) => bound.Add(t.thingIDNumber);

        public static void Unregister(Thing t) => bound.Remove(t.thingIDNumber);

        public static bool IsBound(Thing t) => t != null && bound.Contains(t.thingIDNumber);

        public static BoundSoul SoulOf(Thing t) => IsBound(t) ? t.TryGetComp<CompSoulBound>()?.soul : null;

        /// <summary>Grade sets the size: a wretched soul gives a seventh of the bonus, an exalted one all of it.</summary>
        public static float Scale(SoulGrade g) => ((int)g + 1) / 7f;

        // Skill residue → the stat it improves and the bonus at full (exalted) strength.
        private static readonly Dictionary<string, (string stat, float amount)> SkillBonuses = new Dictionary<string, (string, float)>
        {
            { "Shooting", ("ShootingAccuracyPawn", 4f) },
            { "Melee", ("MeleeHitChance", 4f) },
            { "Construction", ("ConstructionSpeed", 0.2f) },
            { "Mining", ("MiningSpeed", 0.2f) },
            { "Cooking", ("CookSpeed", 0.2f) },
            { "Plants", ("PlantWorkSpeed", 0.2f) },
            { "Animals", ("TrainAnimalChance", 0.1f) },
            { "Crafting", ("GeneralLaborSpeed", 0.15f) },
            { "Artistic", ("GlobalLearningFactor", 0.2f) },
            { "Medicine", ("MedicalTendQuality", 0.1f) },
            { "Social", ("SocialImpact", 0.15f) },
            { "Intellectual", ("ResearchSpeed", 0.2f) },
        };

        // Trait residue (by trait defName) → stat and full-strength bonus.
        private static readonly Dictionary<string, (string stat, float amount)> TraitBonuses = new Dictionary<string, (string, float)>
        {
            { "Pyromaniac", ("ArmorRating_Heat", 0.3f) },
            { "Tough", ("IncomingDamageFactor", -0.1f) },
            { "Brawler", ("MeleeHitChance", 4f) },
            { "ShootingAccuracy", ("ShootingAccuracyPawn", 3f) },
            { "Nimble", ("MeleeDodgeChance", 4f) },
            { "SpeedOffset", ("MoveSpeed", 0.3f) },
            { "Industriousness", ("WorkSpeedGlobal", 0.1f) },
            { "Kind", ("SocialImpact", 0.15f) },
            { "Beauty", ("SocialImpact", 0.15f) },
            { "Psychopath", ("MentalBreakThreshold", -0.04f) },
            { "Bloodlust", ("MentalBreakThreshold", -0.04f) },
            { "NaturalMood", ("MentalBreakThreshold", -0.04f) },
            { "Nerves", ("MentalBreakThreshold", -0.04f) },
            { "PsychicSensitivity", ("PsychicSensitivity", 0.3f) },
            { "GreatMemory", ("GlobalLearningFactor", 0.2f) },
            { "TooSmart", ("GlobalLearningFactor", 0.2f) },
            { "Immunity", ("ImmunityGainSpeed", 0.1f) },
        };

        /// <summary>Any soul without a recognised residue still sharpens its bearer's psychic sense.</summary>
        private static readonly (string stat, float amount) DefaultBonus = ("PsychicSensitivity", 0.2f);

        private static readonly Dictionary<string, (StatDef, float)> cache = new Dictionary<string, (StatDef, float)>();

        /// <summary>
        /// The stat a residue improves and by how much at full strength. Residue is stored as text
        /// ("Shooting 14", or a trait label like "Pyromaniac"), so it is matched back to a skill or trait.
        /// </summary>
        public static (StatDef stat, float amount) BonusFor(string residue)
        {
            string key = residue ?? "";
            if (cache.TryGetValue(key, out var hit))
            {
                return hit;
            }
            (string stat, float amount) pick = DefaultBonus;
            if (!residue.NullOrEmpty())
            {
                SkillDef skill = DefDatabase<SkillDef>.AllDefs.FirstOrDefault(s => residue.StartsWith(s.LabelCap.ToString()));
                if (skill != null && SkillBonuses.TryGetValue(skill.defName, out var sb))
                {
                    pick = sb;
                }
                else
                {
                    TraitDef trait = DefDatabase<TraitDef>.AllDefs.FirstOrDefault(t => t.degreeDatas.Any(d => string.Equals(d.label, residue, System.StringComparison.OrdinalIgnoreCase)));
                    if (trait != null && TraitBonuses.TryGetValue(trait.defName, out var tb))
                    {
                        pick = tb;
                    }
                }
            }
            StatDef def = DefDatabase<StatDef>.GetNamedSilentFail(pick.stat) ?? DefDatabase<StatDef>.GetNamedSilentFail(DefaultBonus.stat);
            var result = (def, def == null ? 0f : pick.amount);
            cache[key] = result;
            return result;
        }

        public static float OffsetFor(BoundSoul soul, StatDef stat)
        {
            var (s, amount) = BonusFor(soul.residue);
            return s == stat ? amount * Scale(soul.grade) : 0f;
        }

        public static string BonusText(BoundSoul soul)
        {
            var (s, amount) = BonusFor(soul.residue);
            if (s == null)
            {
                return "";
            }
            float v = amount * Scale(soul.grade);
            return "Gives its bearer " + s.ValueToString(v, ToStringNumberSense.Offset) + " " + s.label + ".";
        }
    }

    /// <summary>Adds the soul-bound comp to every weapon and apparel def, vanilla's and every mod's.</summary>
    [StaticConstructorOnStartup]
    public static class SoulBoundInjector
    {
        static SoulBoundInjector()
        {
            int n = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!(def.IsWeapon || def.IsApparel) || def.thingClass == null || !typeof(ThingWithComps).IsAssignableFrom(def.thingClass)
                    || def.HasComp(typeof(CompSoulBound)))
                {
                    continue;
                }
                if (def.comps == null)
                {
                    def.comps = new List<CompProperties>();
                }
                def.comps.Add(new CompProperties_SoulBound());
                n++;
            }
        }
    }

    /// <summary>A soul-bound weapon or garment gives its bonus to whoever holds or wears it.</summary>
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear))]
    public static class Patch_StatWorker_StatOffsetFromGear
    {
        public static void Postfix(Thing gear, StatDef stat, ref float __result)
        {
            BoundSoul soul = SoulBoundUtility.SoulOf(gear);
            if (soul != null)
            {
                __result += SoulBoundUtility.OffsetFor(soul, stat);
            }
        }
    }

    /// <summary>
    /// "Bind soul into item": one bill for any weapon or apparel. The item isn't consumed: the product
    /// patch below hands the same item back with the vessel's soul in it. The vessel is used up.
    /// </summary>
    public class RecipeWorker_BindSoulIntoItem : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient.TryGetComp<CompSoulBound>() != null && ingredient.TryGetComp<CompSoulVessel>() == null)
            {
                return;
            }
            base.ConsumeIngredient(ingredient, recipe, map);
        }
    }

    /// <summary>
    /// Products of recipes that use a filled vessel. The bind bill returns the very item it was given,
    /// now soul-bound. Any other recipe with a vessel ingredient (reliquary armour) binds that soul into
    /// whatever it makes, so a top-tier piece carries the residue of the soul that went into it (4.29).
    /// </summary>
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_GenRecipe_SoulBind
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients)
        {
            CompSoulVessel vessel = ingredients?.Select(t => t.TryGetComp<CompSoulVessel>()).FirstOrDefault(v => v != null && !v.victimName.NullOrEmpty());
            if (vessel == null)
            {
                foreach (Thing t in __result)
                {
                    yield return t;
                }
                yield break;
            }
            BoundSoul soul = BoundSoul.From(vessel);
            if (recipeDef.Worker is RecipeWorker_BindSoulIntoItem)
            {
                Thing item = ingredients.FirstOrDefault(t => t.TryGetComp<CompSoulBound>() != null && t.TryGetComp<CompSoulVessel>() == null);
                if (item != null)
                {
                    if (item.Spawned)
                    {
                        item.DeSpawn();
                    }
                    item.TryGetComp<CompSoulBound>().Bind(soul);
                    yield return item;
                }
                foreach (Thing t in __result)
                {
                    yield return t;
                }
                yield break;
            }
            foreach (Thing t in __result)
            {
                CompSoulBound sb = t.TryGetComp<CompSoulBound>();
                if (sb != null && !sb.Bound)
                {
                    sb.Bind(soul);
                }
                yield return t;
            }
        }
    }

    /// <summary>"Allow soul-bound": lets bills and stockpiles tell bound gear apart. The bind bill disallows it.</summary>
    public class SpecialThingFilterWorker_SoulBound : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t) => SoulBoundUtility.IsBound(t);

        public override bool CanEverMatch(ThingDef def) => def.IsWeapon || def.IsApparel;
    }
}
