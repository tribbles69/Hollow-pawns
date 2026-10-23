using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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
        protected virtual int Yield(Corpse corpse) => SoulUtility.DeadSoulYield(corpse);

        protected virtual int CorruptionAmount => 20;

        protected virtual float CorruptionRadius => 2.9f;

        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient is Corpse corpse)
            {
                int yield = Yield(corpse);
                ThingDef essence = SoulUtility.EssenceDefFor(corpse.InnerPawn);
                SoulUtility.MarkSoulTaken(corpse.InnerPawn);
                SoulUtility.DropEssence(corpse.PositionHeld, map, yield, essence);
                // Rendering stains the ground around the bench (4.28).
                Corruption.Spill(corpse.PositionHeld, map, CorruptionAmount, CorruptionRadius);
                return;
            }
            base.ConsumeIngredient(ingredient, recipe, map);
        }
    }

    /// <summary>
    /// The bone altar's corpse harvest: no power, no research, done by hand. Half the essence of the
    /// crucible (rounded up: 4 fresh, 3 rotting, 1 desiccated) and three times the corruption.
    /// </summary>
    public class RecipeWorker_HarvestCorpseSoulCrude : RecipeWorker_HarvestCorpseSoul
    {
        protected override int Yield(Corpse corpse) => Mathf.CeilToInt(SoulUtility.DeadSoulYield(corpse) / 2f);

        protected override int CorruptionAmount => 60;

        protected override float CorruptionRadius => 3.9f;
    }

    /// <summary>Bills at the bone altar wait while a prisoner is strapped to it for a rite.</summary>
    public class WorkGiver_DoBillBoneAltar : WorkGiver_DoBill
    {
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            CompSoulRite rite = thing.TryGetComp<CompSoulRite>();
            if (rite != null && rite.Busy)
            {
                return null;
            }
            return base.JobOnThing(pawn, thing, forced);
        }
    }

    /// <summary>
    /// Bone altars built before the altar became a work table were saved as plain buildings, and the
    /// bills tab would throw on them. On load, swap each idle one for a fresh altar in the same spot.
    /// One with a prisoner strapped in is left alone until the next load.
    /// </summary>
    public class GameComponent_BoneAltarMigration : GameComponent
    {
        public GameComponent_BoneAltarMigration(Game game)
        {
        }

        public override void LoadedGame()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Thing old in map.listerThings.ThingsOfDef(HP_DefOf.HP_BoneAltar).ToList())
                {
                    if (old is Building_WorkTable || (old.TryGetComp<CompSoulRite>()?.Busy ?? false))
                    {
                        continue;
                    }
                    IntVec3 pos = old.Position;
                    Rot4 rot = old.Rotation;
                    Faction faction = old.Faction;
                    float health = (float)old.HitPoints / old.MaxHitPoints;
                    Thing altar = ThingMaker.MakeThing(old.def, old.Stuff);
                    old.Destroy(DestroyMode.Vanish);
                    altar.SetFaction(faction);
                    GenSpawn.Spawn(altar, pos, map, rot, WipeMode.Vanish);
                    altar.HitPoints = Mathf.Max(1, Mathf.RoundToInt(altar.MaxHitPoints * health));
                }
            }
        }
    }
}
