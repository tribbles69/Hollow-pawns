using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// A named soul held by something other than a vessel: an item it was bound into (4.35), a shrine,
    /// an effigy (4.18). Copied from a vessel or a pawn; saved with whatever holds it. Nothing ticks.
    /// </summary>
    public class BoundSoul : IExposable
    {
        public string name;
        public string dateTaken;
        public SoulGrade grade = SoulGrade.Common;
        public string residue;
        public bool corrupted;

        /// <summary>Thing IDs of the living pawns who cared about them when they were taken.</summary>
        public List<int> lovedBy = new List<int>();

        public static BoundSoul From(CompSoulVessel v) => new BoundSoul
        {
            name = v.victimName,
            dateTaken = v.dateTaken,
            grade = v.grade,
            residue = v.residue,
            corrupted = v.corrupted,
            lovedBy = v.lovedBy?.ToList() ?? new List<int>(),
        };

        public static BoundSoul From(Pawn p, SoulGrade grade) => new BoundSoul
        {
            name = p.Name?.ToStringFull ?? p.LabelShort,
            dateTaken = GenDate.DateFullStringAt(GenTicks.TicksAbs, p.MapHeld == null ? default : Find.WorldGrid.LongLatOf(p.MapHeld.Tile)),
            grade = grade,
            residue = SoulGrades.ResidueOf(p),
            corrupted = SoulUtility.IsCorrupted(p),
            lovedBy = LovedBy(p),
        };

        public bool IsLovedBy(Pawn p) => p != null && lovedBy != null && lovedBy.Contains(p.thingIDNumber);

        public string ShortName => name.NullOrEmpty() ? "someone" : name;

        /// <summary>"Vex the Butcher (masterwork soul; residue: Pyromaniac)"</summary>
        public string Describe() => ShortName + " (" + (corrupted ? "corrupted " : "") + grade.Label() + " soul"
            + (residue.NullOrEmpty() ? "" : "; residue: " + residue) + ")";

        /// <summary>
        /// Everyone alive who cared about this pawn: family by blood, lovers and spouses, and anyone
        /// with a good opinion of them. Taken once, when the soul is taken, so it survives the pawn.
        /// </summary>
        public static List<int> LovedBy(Pawn victim)
        {
            var ids = new List<int>();
            if (victim?.relations == null)
            {
                return ids;
            }
            foreach (Pawn q in victim.relations.PotentiallyRelatedPawns.ToList())
            {
                if (q == null || q.Dead || q == victim || q.relations == null)
                {
                    continue;
                }
                bool close = q.GetRelations(victim).Any(r => r.familyByBloodRelation || r == PawnRelationDefOf.Lover
                    || r == PawnRelationDefOf.Spouse || r == PawnRelationDefOf.Fiance);
                if (close || (q.RaceProps.Humanlike && q.relations.OpinionOf(victim) >= 20))
                {
                    ids.Add(q.thingIDNumber);
                }
            }
            return ids;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref dateTaken, "dateTaken");
            Scribe_Values.Look(ref grade, "grade", SoulGrade.Common);
            Scribe_Values.Look(ref residue, "residue");
            Scribe_Values.Look(ref corrupted, "corrupted");
            Scribe_Collections.Look(ref lovedBy, "lovedBy", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && lovedBy == null)
            {
                lovedBy = new List<int>();
            }
        }
    }
}
