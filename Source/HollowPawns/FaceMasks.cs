using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// A flayed face, worn as a mask (4.23). Keeps whose face it was, whether it came off the living,
    /// who loved them (seeing it worn is a horror) and, for a blood debtor, who it avenges.
    /// </summary>
    public class CompFace : ThingComp
    {
        public string name;
        public bool living;
        public string dateTaken;
        public List<int> lovedBy = new List<int>();
        public List<int> avengers = new List<int>();

        public void Fill(Pawn victim)
        {
            name = victim.Name?.ToStringFull ?? victim.LabelShort;
            living = !victim.Dead;
            dateTaken = GenDate.DateFullStringAt(GenTicks.TicksAbs, victim.MapHeld == null ? Vector2.zero : Find.WorldGrid.LongLatOf(victim.MapHeld.Tile));
            lovedBy = BoundSoul.LovedBy(victim);
            avengers = BloodDebts.DebtOn(victim)?.avengers?.ToList() ?? new List<int>();
        }

        private string Short => name == null ? null : name.Split(' ').Last().Trim('\'');

        public override string TransformLabel(string label) => name == null ? label : "face of " + Short;

        public override string CompInspectStringExtra() => Line;

        public override string GetDescriptionPart() => Line;

        private string Line => name == null ? null
            : "The face of " + name + ", taken " + (living ? "from the living" : "from the dead") + " on " + dateTaken + "."
              + (avengers.Count > 0 ? " They carried a blood debt." : "");

        public override bool AllowStackWith(Thing other) => false;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref living, "living");
            Scribe_Values.Look(ref dateTaken, "dateTaken");
            Scribe_Collections.Look(ref lovedBy, "lovedBy", LookMode.Value);
            Scribe_Collections.Look(ref avengers, "avengers", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                lovedBy = lovedBy ?? new List<int>();
                avengers = avengers ?? new List<int>();
            }
        }
    }

    public static class FaceMasks
    {
        /// <summary>A mask of this face. Faces taken from the living come out a grade better.</summary>
        public static Thing Make(Pawn victim, Pawn maker, SkillDef skill)
        {
            Thing mask = ThingMaker.MakeThing(HP_DefOf.HP_FaceMask);
            mask.TryGetComp<CompFace>()?.Fill(victim);
            // The mask takes the victim's own skin colour (the sprites are drawn pale so the tint reads true).
            if (victim.story != null)
            {
                mask.TryGetComp<CompColorable>()?.SetColor(victim.story.SkinColor);
            }
            QualityCategory q = maker != null ? QualityUtility.GenerateQualityCreatedByPawn(maker, skill) : QualityCategory.Normal;
            q = victim.Dead ? (QualityCategory)Mathf.Min((int)q, (int)QualityCategory.Normal) : (QualityCategory)Mathf.Min((int)q + 1, (int)QualityCategory.Legendary);
            mask.TryGetComp<CompQuality>()?.SetQuality(q, ArtGenerationContext.Colony);
            CorruptionCult.Record(HP_DefOf.HP_FaceTakenEvent, maker);
            return mask;
        }

        public static IEnumerable<CompFace> WornBy(Pawn p)
        {
            List<Apparel> worn = p.apparel?.WornApparel;
            if (worn == null)
            {
                yield break;
            }
            foreach (Apparel a in worn)
            {
                CompFace f = a.TryGetComp<CompFace>();
                if (f?.name != null)
                {
                    yield return f;
                }
            }
        }

        /// <summary>Someone in sight, within 8 cells, wearing a face matching the test.</summary>
        public static bool SeesMask(Pawn p, System.Func<CompFace, bool> test)
        {
            if (!p.Spawned)
            {
                return false;
            }
            foreach (Pawn other in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == p || other.apparel == null || !other.Position.InHorDistOf(p.Position, 8f))
                {
                    continue;
                }
                foreach (CompFace f in WornBy(other))
                {
                    if (test(f) && GenSight.LineOfSight(p.Position, other.Position, p.Map))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Blood-debt tie-in: an avenger seeing their loved one's killer worn as a face.</summary>
        public static bool AvengedBySeenMask(Pawn p) => SeesMask(p, f => f.avengers.Contains(p.thingIDNumber));

        public static bool LikesTrophies(Pawn p) => p.story?.traits != null
            && (p.story.traits.HasTrait(TraitDefOf.Psychopath) || p.story.traits.HasTrait(TraitDefOf.Bloodlust));
    }

    // ======================= Taking a face =======================

    /// <summary>The take-face bill: the corpse is marked, not used up, so it can still go to the crucible or the slab.</summary>
    public class RecipeWorker_TakeFace : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient is Corpse c)
            {
                SoulUtility.MarkFaceTaken(c.InnerPawn);
                return;
            }
            base.ConsumeIngredient(ingredient, recipe, map);
        }
    }

    /// <summary>
    /// Makes the mask. Runs before the corpse is marked (products are made before ingredients are
    /// consumed), so the face is read off an unmarked corpse.
    /// </summary>
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_GenRecipe_TakeFace
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients)
        {
            if (recipeDef.Worker is RecipeWorker_TakeFace)
            {
                Corpse corpse = ingredients?.OfType<Corpse>().FirstOrDefault();
                if (corpse != null && SoulUtility.HasFace(corpse.InnerPawn))
                {
                    yield return FaceMasks.Make(corpse.InnerPawn, worker, SkillDefOf.Crafting);
                }
            }
            foreach (Thing t in __result)
            {
                yield return t;
            }
        }
    }

    /// <summary>Corpses with no face to take: not an adult human, or already taken. The bill excludes them.</summary>
    public class SpecialThingFilterWorker_NoFace : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t) => t is Corpse c && !SoulUtility.HasFace(c.InnerPawn);

        public override bool CanEverMatch(ThingDef def) => def.IsCorpse;
    }

    /// <summary>Flaying a living prisoner or slave: they live, disfigured, and their faction won't forget.</summary>
    public class Recipe_FlayFace : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return thing is Pawn p && SoulUtility.HasFace(p) && (p.IsPrisonerOfColony || p.IsSlaveOfColony) && base.AvailableOnNow(thing, part);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            Thing mask = FaceMasks.Make(pawn, billDoer, SkillDefOf.Medicine);
            SoulUtility.MarkFaceTaken(pawn);
            HealthUtility.AdjustSeverity(pawn, HediffDefOf.BloodLoss, 0.2f);
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(HP_DefOf.HP_FaceFlayed);
            GenPlace.TryPlaceThing(mask, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
            if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -30);
            }
        }
    }

    // ======================= Thoughts =======================

    /// <summary>Social: wearing a human face. Psychopaths and the bloodlusty admire it; everyone else recoils.</summary>
    public class ThoughtWorker_WearsFace : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !FaceMasks.WornBy(other).Any())
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(FaceMasks.LikesTrophies(p) ? 1 : 0);
        }
    }

    /// <summary>Mood: someone nearby is wearing the face of a person this pawn loved.</summary>
    public class ThoughtWorker_LovedFaceWorn : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return FaceMasks.SeesMask(p, f => f.lovedBy.Contains(p.thingIDNumber)) ? ThoughtState.ActiveAtStage(0) : ThoughtState.Inactive;
        }
    }
}
