using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    public static class SoulUtility
    {
        /// <summary>Essence from a soul torn out of a living body (surgery, scythe).</summary>
        public const int EssenceFromLivingSoul = 12;

        /// <summary>Essence from the lingering soul of the dead (crucible, requiem kills).</summary>
        public const int EssenceFromDeadSoul = 8;

        private static readonly Color ReapTextColor = new Color(0.47f, 0.86f, 0.9f);

        /// <summary>
        /// The pawn's soul part, or null if it has none: already taken, a body with no soul,
        /// not humanlike, or not yet an adult. Children are never harvestable, by design:
        /// no surgery, no reaping, and their corpses are never taken by the crucible.
        /// Children still carry the part (it's on the Human body), so it's already there
        /// when they grow up; nothing can reach it before then.
        /// </summary>
        public static BodyPartRecord GetSoul(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || pawn.RaceProps?.Humanlike != true || !pawn.DevelopmentalStage.Adult())
            {
                return null;
            }
            foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (part.def == HP_DefOf.HP_Soul)
                {
                    return part;
                }
            }
            return null;
        }

        public static bool HasSoul(Pawn pawn) => GetSoul(pawn) != null;

        /// <summary>Drops soul essence where the pawn (or its corpse) is.</summary>
        public static void DropEssence(Pawn pawn, int count)
        {
            Map map = pawn.MapHeld;
            if (map == null || count <= 0)
            {
                return;
            }
            Thing essence = ThingMaker.MakeThing(HP_DefOf.HP_SoulEssence);
            essence.stackCount = count;
            GenPlace.TryPlaceThing(essence, pawn.PositionHeld, map, ThingPlaceMode.Near);
        }

        /// <summary>
        /// Tears the soul out of a living pawn: drops essence, marks the soul missing, kills them.
        /// Used by the extract soul surgery and the reaper scythe.
        /// </summary>
        public static void ReapLiving(Pawn pawn, DamageInfo? dinfo = null)
        {
            BodyPartRecord soul = GetSoul(pawn);
            if (soul == null || pawn.Dead)
            {
                return;
            }

            if (pawn.MapHeld != null)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.MapHeld, "HP_Reaped".Translate(), ReapTextColor);
            }
            DropEssence(pawn, EssenceFromLivingSoul);

            // Patch_SoulLossKills kills the pawn as soon as this is added; the Kill below is
            // only a fallback in case the patch didn't apply.
            Hediff missingSoul = pawn.health.AddHediff(HediffDefOf.MissingBodyPart, soul, dinfo);
            if (!pawn.Dead)
            {
                pawn.Kill(dinfo, missingSoul);
            }
        }

        /// <summary>
        /// Takes the lingering soul from someone who has just died (requiem kills).
        /// Marks the soul missing on the corpse so the crucible can't harvest it again.
        /// </summary>
        public static void ReapDead(Pawn pawn)
        {
            BodyPartRecord soul = GetSoul(pawn);
            if (soul == null || !pawn.Dead)
            {
                return;
            }
            DropEssence(pawn, EssenceFromDeadSoul);
            // AddDirect, because the normal AddHediff path is meant for the living.
            pawn.health.hediffSet.AddDirect(HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, soul));
        }
    }
}
