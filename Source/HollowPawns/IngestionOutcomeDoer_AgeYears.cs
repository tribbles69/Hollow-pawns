using RimWorld;
using Verse;

namespace HollowPawns
{
    /// <summary>Borrowed time's price: each dose ages the user. Runs once, on ingestion.</summary>
    public class IngestionOutcomeDoer_AgeYears : IngestionOutcomeDoer
    {
        public float years = 1f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (pawn.ageTracker == null)
            {
                return;
            }
            pawn.ageTracker.AgeBiologicalTicks += (long)(years * GenDate.TicksPerYear);
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message(pawn.LabelShort + " pays for borrowed time: " + years.ToString("0.#")
                    + " year" + (years == 1f ? "" : "s") + " older.", pawn, MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}
