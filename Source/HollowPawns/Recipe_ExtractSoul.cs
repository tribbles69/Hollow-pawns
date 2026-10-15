using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// Surgery that tears the soul out of a living pawn. Drops essence, marks the soul
    /// as missing (so the corpse can't be harvested again), and kills the patient.
    /// </summary>
    public class Recipe_ExtractSoul : Recipe_Surgery
    {
        /// <summary>True for bottle soul: the soul goes into a named soul jar instead of loose essence.</summary>
        protected virtual bool Bottle => false;

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            if (SoulUtility.CanExtract(pawn))
            {
                yield return SoulUtility.GetSoul(pawn);
            }
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (SoulUtility.IsSoulLocked(pawn))
            {
                // Locked after the bill was queued and the doctor had already started.
                Messages.Message(pawn.LabelShort + " is Soul-Locked: extraction stopped.", pawn, MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }

            bool violation = IsViolationOnPawn(pawn, part, Faction.OfPlayer);

            if (Bottle && billDoer != null)
            {
                // Recorded while the victim is still alive, so the tale has their name and face.
                TaleRecorder.RecordTale(HP_DefOf.HP_BottledSoul, billDoer, pawn);
            }
            SoulUtility.ReapLiving(pawn, null, Bottle);

            if (violation && billDoer != null)
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
            }
        }
    }

    /// <summary>
    /// Bottle soul: the same surgery, but the soul is kept whole in a soul jar that remembers
    /// who it was. Smash the jar later for the same essence, so bottling never costs anything.
    /// </summary>
    public class Recipe_BottleSoul : Recipe_ExtractSoul
    {
        protected override bool Bottle => true;
    }
}
