using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HollowPawns
{
    public class CompProperties_SoulCrucible : CompProperties
    {
        public CompProperties_SoulCrucible()
        {
            compClass = typeof(CompSoulCrucible);
        }
    }

    /// <summary>The crucible's "Mark all" button. No ticking: it only runs when clicked.</summary>
    public class CompSoulCrucible : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            var mark = new Command_Action
            {
                defaultLabel = "Mark all",
                defaultDesc = "Queue the extract soul surgery on every prisoner here whose soul is intact and who isn't Soul-Locked.",
                icon = ReapersMark.MarkIcon,
                action = () =>
                {
                    int marked = 0;
                    foreach (Pawn p in Unmarked())
                    {
                        if (SoulUtility.Mark(p))
                        {
                            marked++;
                        }
                    }
                    Messages.Message("Marked " + marked + " prisoner" + (marked == 1 ? "" : "s") + " for soul extraction.",
                        parent, MessageTypeDefOf.TaskCompletion, false);
                },
            };
            if (!HP_DefOf.HP_ExtractSoul.AvailableNow)
            {
                mark.Disable("Needs " + HP_DefOf.HP_SoulExtraction.LabelCap + " research.");
            }
            else if (Unmarked().Count == 0)
            {
                mark.Disable("No unmarked prisoners with a soul.");
            }
            yield return mark;
        }

        private List<Pawn> Unmarked()
        {
            var list = new List<Pawn>();
            foreach (Pawn p in parent.Map.mapPawns.PrisonersOfColonySpawned)
            {
                if (SoulUtility.CanMark(p) && !SoulUtility.IsMarked(p))
                {
                    list.Add(p);
                }
            }
            return list;
        }
    }
}
