using System.Linq;
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
            if (pawn?.health?.hediffSet == null || !pawn.DevelopmentalStage.Adult() || IsSoulless(pawn))
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

        /// <summary>An adult human, alive or dead: the only kind whose face can be taken (4.23).</summary>
        public static bool IsAdultHuman(Pawn pawn) => pawn?.RaceProps != null && pawn.RaceProps.Humanlike && pawn.DevelopmentalStage.Adult();

        /// <summary>Whether this living or dead adult still has a face to take.</summary>
        public static bool HasFace(Pawn pawn) => IsAdultHuman(pawn) && !pawn.health.hediffSet.HasHediff(HP_DefOf.HP_FaceTaken);

        /// <summary>Marks a corpse's face taken without consuming the corpse (like MarkSoulTaken).</summary>
        public static void MarkFaceTaken(Pawn pawn)
        {
            if (!HasFace(pawn))
            {
                return;
            }
            BodyPartRecord head = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def == BodyPartDefOf.Head);
            Hediff h = HediffMaker.MakeHediff(HP_DefOf.HP_FaceTaken, pawn, head);
            if (pawn.Dead)
            {
                pawn.health.hediffSet.AddDirect(h);
            }
            else
            {
                pawn.health.AddHediff(h);
            }
        }

        /// <summary>
        /// Alive without a soul: vat-grown thralls (the Soulless gene) and reanimated husks (the husk
        /// condition). The only things alive without one (4.30). They're limited to food and rest needs.
        /// </summary>
        public static bool IsSoulless(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return false;
            }
            if (HP_DefOf.HP_ReanimatedHusk != null && pawn.health.hediffSet.HasHediff(HP_DefOf.HP_ReanimatedHusk))
            {
                return true;
            }
            return HP_DefOf.HP_Soulless != null && pawn.genes != null && pawn.genes.HasActiveGene(HP_DefOf.HP_Soulless);
        }

        /// <summary>
        /// Demons carry a corrupted soul (the Biotech gene HP_CorruptedSoul). Taking it gives corrupted
        /// essence instead of clean. Always false without Biotech.
        /// </summary>
        public static bool IsCorrupted(Pawn pawn) =>
            (HP_DefOf.HP_CorruptedSoul != null && pawn?.genes != null && pawn.genes.HasActiveGene(HP_DefOf.HP_CorruptedSoul))
            || Profaned.IsProfaned(pawn) || Profaned.ShardOf(pawn) != null;

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
        /// The reaper scythe's reap: tears the soul out of a living pawn, renders it on the spot into
        /// plain essence, and kills them. Leaves a husk.
        /// </summary>
        public static void ReapLiving(Pawn pawn, DamageInfo? dinfo = null)
        {
            if (GetSoul(pawn) == null || pawn.Dead)
            {
                return;
            }
            if (pawn.MapHeld != null)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.MapHeld, "Reaped", ReapTextColor);
                DropEssence(pawn.PositionHeld, pawn.MapHeld, EssenceFromLivingSoul, EssenceDefFor(pawn));
            }
            TakeSoulAndKill(pawn, dinfo);
        }

        /// <summary>Marks a living pawn's soul missing and kills them. No soul is no life.</summary>
        public static void TakeSoulAndKill(Pawn pawn, DamageInfo? dinfo = null)
        {
            BodyPartRecord soul = GetSoul(pawn);
            if (soul == null || pawn.Dead)
            {
                return;
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
            if (pawn?.RaceProps?.body == null || !pawn.DevelopmentalStage.Adult() || IsSoulless(pawn))
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

        // ---------- The Reaper's Mark: Soul-Lock and marking for the altar or press ----------

        public static bool IsSoulLocked(Pawn pawn) => WorldComponent_SoulLocks.Get()?.IsLocked(pawn) ?? false;

        /// <summary>Locks or unlocks a pawn. Locking also clears any mark on them.</summary>
        public static void SetSoulLocked(Pawn pawn, bool locked)
        {
            WorldComponent_SoulLocks.Get()?.SetLocked(pawn, locked);
            if (locked)
            {
                ClearFate(pawn);
            }
        }

        /// <summary>Whether this pawn's soul can be taken at all.</summary>
        public static bool CanExtract(Pawn pawn) => HasSoul(pawn) && !IsSoulLocked(pawn);

        /// <summary>Prisoners the Mark buttons may act on: soul intact and not Soul-Locked.</summary>
        public static bool CanMark(Pawn pawn) => pawn.IsPrisonerOfColony && !pawn.Dead && CanExtract(pawn);

        public static SoulFate GetFate(Pawn pawn) => WorldComponent_SoulLocks.Get()?.GetFate(pawn) ?? SoulFate.None;

        public static bool IsMarked(Pawn pawn) => GetFate(pawn) != SoulFate.None;

        /// <summary>Marks a prisoner to be rendered or bound. Wardens bring them to an altar or press.</summary>
        public static void SetFate(Pawn pawn, SoulFate fate)
        {
            if (fate != SoulFate.None && !CanMark(pawn))
            {
                return;
            }
            WorldComponent_SoulLocks.Get()?.SetFate(pawn, fate);
        }

        public static void ClearFate(Pawn pawn) => WorldComponent_SoulLocks.Get()?.SetFate(pawn, SoulFate.None);
    }
}
