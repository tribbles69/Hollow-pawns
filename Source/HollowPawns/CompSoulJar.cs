using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HollowPawns
{
    public class CompProperties_SoulJar : CompProperties
    {
        public CompProperties_SoulJar()
        {
            compClass = typeof(CompSoulJar);
        }
    }

    /// <summary>
    /// A soul kept whole: remembers who it was, shows it on inspect and in the label, and can be
    /// smashed for the same essence the soul would have rendered into. Saved with the jar
    /// (and inside a minified jar), so it survives save/load. Nothing ticks.
    /// </summary>
    public class CompSoulJar : ThingComp
    {
        public string victimName;
        public string victimTitle;
        public string victimFaction;
        public string dateTaken;

        /// <summary>A demon's soul: smashing or breaking the jar gives corrupted essence.</summary>
        public bool corrupted;

        private ThingDef EssenceDef => corrupted ? HP_DefOf.HP_CorruptedEssence : HP_DefOf.HP_SoulEssence;

        public static void SpawnJarFor(Pawn victim)
        {
            Map map = victim.MapHeld;
            if (map == null)
            {
                return;
            }
            Thing jar = ThingMaker.MakeThing(HP_DefOf.HP_SoulJar);
            CompSoulJar comp = jar.TryGetComp<CompSoulJar>();
            if (comp != null)
            {
                comp.victimName = victim.Name?.ToStringFull ?? victim.LabelShort;
                comp.victimTitle = victim.story?.TitleShortCap;
                comp.victimFaction = victim.HomeFaction?.Name;
                comp.dateTaken = GenDate.DateFullStringAt(GenTicks.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile));
                comp.corrupted = SoulUtility.IsCorrupted(victim);
            }
            // Comes out boxed, ready to be installed on a shelf of the damned.
            GenPlace.TryPlaceThing(MinifyUtility.MakeMinified(jar), victim.PositionHeld, map, ThingPlaceMode.Near);
        }

        public override string TransformLabel(string label)
        {
            return victimName.NullOrEmpty() ? label : label + " (" + victimName + ")";
        }

        public override string CompInspectStringExtra()
        {
            if (victimName.NullOrEmpty())
            {
                return null;
            }
            string who = victimName;
            if (!victimTitle.NullOrEmpty())
            {
                who += ", " + victimTitle.UncapitalizeFirst();
            }
            if (!victimFaction.NullOrEmpty())
            {
                who += " of " + victimFaction;
            }
            return "Holds the " + (corrupted ? "corrupted " : "") + "soul of " + who + ".\nBottled " + dateTaken + ".";
        }

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
            yield return new Command_Action
            {
                defaultLabel = "Smash jar",
                defaultDesc = "Break the jar and render the soul inside into " + SoulUtility.EssenceFromLivingSoul + " soul essence.",
                icon = ReapersMark.MarkIcon,
                action = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "Smash the jar holding " + (victimName ?? "this soul") + "? The soul inside becomes essence, and the jar is gone.",
                    Smash, destructive: true)),
            };
        }

        private void Smash()
        {
            if (parent.Destroyed || !parent.Spawned)
            {
                return;
            }
            IntVec3 cell = parent.Position;
            Map map = parent.Map;
            parent.Destroy(DestroyMode.Vanish);
            SoulUtility.DropEssence(cell, map, SoulUtility.EssenceFromLivingSoul, EssenceDef);
        }

        /// <summary>Broken by a raid, fire or anything else: the soul spills out as essence instead of being lost.</summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (mode == DestroyMode.KillFinalize)
            {
                SoulUtility.DropEssence(parent.Position, previousMap, SoulUtility.EssenceFromLivingSoul, EssenceDef);
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref victimName, "victimName");
            Scribe_Values.Look(ref victimTitle, "victimTitle");
            Scribe_Values.Look(ref victimFaction, "victimFaction");
            Scribe_Values.Look(ref dateTaken, "dateTaken");
            Scribe_Values.Look(ref corrupted, "corrupted");
        }
    }
}
