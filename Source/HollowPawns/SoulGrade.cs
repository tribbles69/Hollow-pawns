using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>The Weight of Names (4.29): how good a soul is. Like art quality.</summary>
    public enum SoulGrade : byte
    {
        Wretched,
        Poor,
        Common,
        Good,
        Fine,
        Masterwork,
        Exalted,
    }

    public static class SoulGrades
    {
        public static string Label(this SoulGrade g)
        {
            switch (g)
            {
                case SoulGrade.Wretched: return "wretched";
                case SoulGrade.Poor: return "poor";
                case SoulGrade.Common: return "common";
                case SoulGrade.Good: return "good";
                case SoulGrade.Fine: return "fine";
                case SoulGrade.Masterwork: return "masterwork";
                default: return "exalted";
            }
        }

        /// <summary>
        /// A living pawn's soul grade, from who they were: their best skill and their top three,
        /// with a nudge for passion. 0-20 score, banded into seven grades.
        /// </summary>
        public static SoulGrade Of(Pawn pawn)
        {
            // A Cursebearer's shard is always a wretched soul (4.32).
            if (Profaned.ShardOf(pawn) != null)
            {
                return SoulGrade.Wretched;
            }
            if (pawn?.skills == null)
            {
                return SoulGrade.Common;
            }
            var levels = pawn.skills.skills.Where(s => !s.TotallyDisabled)
                .Select(s => s.Level + (s.passion == Passion.Major ? 2f : s.passion == Passion.Minor ? 1f : 0f))
                .OrderByDescending(l => l).ToList();
            if (levels.Count == 0)
            {
                return SoulGrade.Wretched;
            }
            float best = levels[0];
            float top3 = levels.Take(3).Average();
            float score = best * 0.5f + top3 * 0.5f;
            return FromScore(score);
        }

        private static SoulGrade FromScore(float score)
        {
            if (score < 4f) return SoulGrade.Wretched;
            if (score < 7f) return SoulGrade.Poor;
            if (score < 10f) return SoulGrade.Common;
            if (score < 13f) return SoulGrade.Good;
            if (score < 16f) return SoulGrade.Fine;
            if (score < 19f) return SoulGrade.Masterwork;
            return SoulGrade.Exalted;
        }

        public static SoulGrade Shift(this SoulGrade g, int steps) =>
            (SoulGrade)Mathf.Clamp((int)g + steps, (int)SoulGrade.Wretched, (int)SoulGrade.Exalted);

        /// <summary>Essence from rendering a living soul of this grade (bulk, nameless).</summary>
        public static int RenderYield(SoulGrade g)
        {
            switch (g)
            {
                case SoulGrade.Wretched: return 6;
                case SoulGrade.Poor: return 8;
                case SoulGrade.Common: return 10;
                case SoulGrade.Good: return 12;
                case SoulGrade.Fine: return 14;
                case SoulGrade.Masterwork: return 17;
                default: return 20;
            }
        }

        /// <summary>Smashing a bound vessel gives more than rendering would have (4.30).</summary>
        public static int SmashYield(SoulGrade g) => Mathf.RoundToInt(RenderYield(g) * 1.5f);

        /// <summary>
        /// Residue (4.29): one thing the soul keeps from its owner. A trait if they had a notable one
        /// (half the time), otherwise their best skill.
        /// </summary>
        public static string ResidueOf(Pawn pawn)
        {
            Hediff_SoulShard shard = Profaned.ShardOf(pawn);
            if (shard != null)
            {
                return shard.residue;
            }
            var traits = pawn.story?.traits?.allTraits;
            if (traits != null && traits.Count > 0 && Rand.Chance(0.5f))
            {
                return traits.RandomElement().LabelCap;
            }
            SkillRecord best = pawn.skills?.skills.Where(s => !s.TotallyDisabled).OrderByDescending(s => s.Level).FirstOrDefault();
            return best == null ? null : best.def.LabelCap + " " + best.Level;
        }
    }
}
