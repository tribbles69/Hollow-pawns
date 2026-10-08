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
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            BodyPartRecord soul = SoulUtility.GetSoul(pawn);
            if (soul != null)
            {
                yield return soul;
            }
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }

            bool violation = IsViolationOnPawn(pawn, part, Faction.OfPlayer);

            SoulUtility.ReapLiving(pawn);

            if (violation && billDoer != null)
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
            }
        }
    }
}
