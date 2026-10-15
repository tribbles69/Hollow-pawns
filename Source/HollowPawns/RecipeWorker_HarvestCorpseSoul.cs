using Verse;

namespace HollowPawns
{
    /// <summary>
    /// The crucible's corpse harvest. Instead of destroying the corpse, it takes the soul and
    /// leaves a soulless husk behind for haulers. The essence depends on how fresh the corpse is
    /// (see SoulUtility.DeadSoulYield), so the recipe has no fixed products: it's dropped here.
    /// The husk can later feed the carrion wall, the reanimation slab or face masks.
    /// </summary>
    public class RecipeWorker_HarvestCorpseSoul : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient is Corpse corpse)
            {
                int yield = SoulUtility.DeadSoulYield(corpse);
                ThingDef essence = SoulUtility.EssenceDefFor(corpse.InnerPawn);
                SoulUtility.MarkSoulTaken(corpse.InnerPawn);
                SoulUtility.DropEssence(corpse.PositionHeld, map, yield, essence);
                return;
            }
            base.ConsumeIngredient(ingredient, recipe, map);
        }
    }
}
