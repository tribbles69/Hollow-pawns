using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    public static class SoulUtility
    {
        /// <summary>Essence from a soul torn out of a living body (surgery, scythe).</summary>
        public const int EssenceFromLivingSoul = 12;

        /// <summary>Essence from the lingering soul of the fresh dead (crucible).</summary>
        public const int EssenceFromDeadSoul = 8;

        /// <summary>A rotting corpse's soul has started to leak away.</summary>
        public const int EssenceFromRottingSoul = 5;

        /// <summary>Little is left in a dried-out husk of a corpse.</summary>
        public const int EssenceFromDessicatedSoul = 2;

        private static readonly Color ReapTextColor = new Color(0.47f, 0.86f, 0.9f);

        /// <summary>
        /// The pawn's soul part, or null if it has none: already taken, a body with no soul,
        /// or not yet an adult. Children are never harvestable, by design: no surgery,
        /// no reaping, and their corpses are never taken by the crucible.
        /// </summary>
        public static BodyPartRecord GetSoul(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || !pawn.DevelopmentalStage.Adult())
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

        /// <summary>
        /// Demons carry a corrupted soul (the Biotech gene HP_CorruptedSoul). Taking it gives corrupted
        /// essence instead of clean. Always false without Biotech.
        /// </summary>
        public static bool IsCorrupted(Pawn pawn) =>
            HP_DefOf.HP_CorruptedSoul != null && pawn?.genes != null && pawn.genes.HasActiveGene(HP_DefOf.HP_CorruptedSoul);

        /// <summary>The essence this pawn's soul turns into: corrupted for demons, clean for everyone else.</summary>
        public static ThingDef EssenceDefFor(Pawn pawn) => IsCorrupted(pawn) ? HP_DefOf.HP_CorruptedEssence : HP_DefOf.HP_SoulEssence;

        /// <summary>Drops this pawn's kind of essence where the pawn (or its corpse) is.</summary>
        public static void DropEssence(Pawn pawn, int count) => DropEssence(pawn.PositionHeld, pawn.MapHeld, count, EssenceDefFor(pawn));

        public static void DropEssence(IntVec3 cell, Map map, int count, ThingDef essenceDef = null)
        {
            if (map == null || count <= 0)
            {
                return;
            }
            Thing essence = ThingMaker.MakeThing(essenceDef ?? HP_DefOf.HP_SoulEssence);
            essence.stackCount = count;
            GenPlace.TryPlaceThing(essence, cell, map, ThingPlaceMode.Near);
        }

        /// <summary>
        /// What the crucible gets from this corpse: less the longer it has lain.
        /// 0 if there's no soul in it (taken, a child, a race with no soul).
        /// </summary>
        public static int DeadSoulYield(Corpse corpse)
        {
            if (corpse == null || !HasSoul(corpse.InnerPawn))
            {
                return 0;
            }
            switch (corpse.GetRotStage())
            {
                case RotStage.Rotting:
                    return EssenceFromRottingSoul;
                case RotStage.Dessicated:
                    return EssenceFromDessicatedSoul;
                default:
                    return EssenceFromDeadSoul;
            }
        }

        /// <summary>
        /// Tears the soul out of a living pawn: drops essence (or, with bottle, a soul jar holding
        /// them whole), marks the soul missing, kills them.
        /// Used by the extract soul and bottle soul surgeries and the reaper scythe.
        /// </summary>
        public static void ReapLiving(Pawn pawn, DamageInfo? dinfo = null, bool bottle = false)
        {
            BodyPartRecord soul = GetSoul(pawn);
            if (soul == null || pawn.Dead)
            {
                return;
            }

            if (pawn.MapHeld != null)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.MapHeld, "Reaped", ReapTextColor);
            }
            if (bottle)
            {
                CompSoulJar.SpawnJarFor(pawn);
            }
            else
            {
                DropEssence(pawn, EssenceFromLivingSoul);
            }

            Hediff missingSoul = pawn.health.AddHediff(HediffDefOf.MissingBodyPart, soul);
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
            if (pawn == null || !pawn.Dead || !HasSoul(pawn))
            {
                return;
            }
            DropEssence(pawn, EssenceFromDeadSoul);
            MarkSoulTaken(pawn);
        }

        /// <summary>
        /// Marks a dead pawn's soul as missing without dropping anything, leaving a husk.
        /// Used by the crucible, whose recipe already produces the essence.
        /// </summary>
        public static void MarkSoulTaken(Pawn pawn)
        {
            BodyPartRecord soul = GetSoul(pawn);
            if (soul == null || !pawn.Dead)
            {
                return;
            }
            // AddDirect, because the normal AddHediff path is meant for the living.
            pawn.health.hediffSet.AddDirect(HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, soul));
        }

        public enum SoulStatus
        {
            /// <summary>Never had one: a child, an animal, a race with no soul part.</summary>
            None,
            Intact,
            Taken,
        }

        /// <summary>
        /// Whether this pawn's soul is still there. Only adults of a race whose body has a soul
        /// part are Intact or Taken; everyone else is None, so children are never listed.
        /// </summary>
        public static SoulStatus GetStatus(Pawn pawn)
        {
            if (pawn?.RaceProps?.body == null || !pawn.DevelopmentalStage.Adult())
            {
                return SoulStatus.None;
            }
            if (HasSoul(pawn))
            {
                return SoulStatus.Intact;
            }
            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part.def == HP_DefOf.HP_Soul)
                {
                    return SoulStatus.Taken;
                }
            }
            return SoulStatus.None;
        }

        // ---------- The Reaper's Mark: Soul-Lock and marking for extraction ----------

        public static bool IsSoulLocked(Pawn pawn) => WorldComponent_SoulLocks.Get()?.IsLocked(pawn) ?? false;

        /// <summary>Locks or unlocks a pawn. Locking also cancels any extraction already queued on them.</summary>
        public static void SetSoulLocked(Pawn pawn, bool locked)
        {
            WorldComponent_SoulLocks.Get()?.SetLocked(pawn, locked);
            if (locked)
            {
                Unmark(pawn);
            }
        }

        /// <summary>Whether the extract soul surgery can be offered on this pawn at all.</summary>
        public static bool CanExtract(Pawn pawn) => HasSoul(pawn) && !IsSoulLocked(pawn);

        /// <summary>Prisoners the Mark buttons may act on: soul intact, not locked, extraction researched.</summary>
        public static bool CanMark(Pawn pawn) =>
            pawn.IsPrisonerOfColony && !pawn.Dead && CanExtract(pawn) && HP_DefOf.HP_ExtractSoul.AvailableNow;

        /// <summary>Extract soul or bottle soul: any surgery that takes the soul.</summary>
        public static bool IsSoulSurgery(RecipeDef recipe) =>
            recipe?.workerClass != null && typeof(Recipe_ExtractSoul).IsAssignableFrom(recipe.workerClass);

        /// <summary>Whether any soul surgery is queued on the pawn.</summary>
        public static bool IsMarked(Pawn pawn)
        {
            if (pawn.BillStack == null)
            {
                return false;
            }
            foreach (Bill bill in pawn.BillStack)
            {
                if (IsSoulSurgery(bill.recipe))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Queues the extract soul surgery. Returns false if it couldn't.</summary>
        public static bool Mark(Pawn pawn)
        {
            BodyPartRecord soul = GetSoul(pawn);
            if (soul == null || IsMarked(pawn) || !CanMark(pawn))
            {
                return false;
            }
            HealthCardUtility.CreateSurgeryBill(pawn, HP_DefOf.HP_ExtractSoul, soul, null, false);
            return true;
        }

        /// <summary>Removes any queued soul surgery (extract or bottle) from the pawn.</summary>
        public static void Unmark(Pawn pawn)
        {
            pawn.BillStack?.Bills.RemoveAll(b => IsSoulSurgery(b.recipe));
        }
    }
}
