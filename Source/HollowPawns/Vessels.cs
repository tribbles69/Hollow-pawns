using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>On an empty vessel's def: what it becomes when a soul is bound into it.</summary>
    public class VesselExtension : DefModExtension
    {
        public ThingDef filledDef;

        /// <summary>The best grade it holds without damage; a better soul is cut down to this.</summary>
        public SoulGrade gradeCap = SoulGrade.Exalted;

        /// <summary>Preference when the rite picks a vessel: the highest tier available is used.</summary>
        public int tier;
    }

    public class CompProperties_SoulVessel : CompProperties
    {
        /// <summary>Corruption spilled if the vessel is broken and the soul gets out.</summary>
        public int corruptionOnBreak = 120;

        public CompProperties_SoulVessel()
        {
            compClass = typeof(CompSoulVessel);
        }
    }

    /// <summary>
    /// A bound soul (4.30): who they were, how good a soul, what it kept, when it was taken.
    /// Saved with the vessel. Smash it for more essence than rendering gives; if it is broken by
    /// anything else, the soul gets out and the ground is corrupted. Nothing ticks.
    /// </summary>
    public class CompSoulVessel : ThingComp
    {
        public string victimName;
        public string victimTitle;
        public string victimFaction;
        public string dateTaken;
        public SoulGrade grade = SoulGrade.Common;
        public string residue;
        public bool corrupted;
        public bool damagedOnBinding;

        /// <summary>Thing IDs of the living pawns who cared about them (reliquary "Remembered", 4.35). Empty on vessels bound before v0.8.</summary>
        public List<int> lovedBy = new List<int>();

        public CompProperties_SoulVessel Props => (CompProperties_SoulVessel)props;

        private ThingDef EssenceDef => corrupted ? HP_DefOf.HP_CorruptedEssence : HP_DefOf.HP_SoulEssence;

        public void Fill(Pawn victim, SoulGrade rawGrade, SoulGrade cap)
        {
            victimName = victim.Name?.ToStringFull ?? victim.LabelShort;
            victimTitle = victim.story?.TitleShortCap;
            victimFaction = victim.HomeFaction?.Name;
            Map map = victim.MapHeld;
            dateTaken = map == null ? GenDate.DateFullStringAt(GenTicks.TicksAbs, Vector2.zero)
                : GenDate.DateFullStringAt(GenTicks.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile));
            corrupted = SoulUtility.IsCorrupted(victim);
            residue = SoulGrades.ResidueOf(victim);
            damagedOnBinding = rawGrade > cap;
            grade = damagedOnBinding ? cap : rawGrade;
            lovedBy = BoundSoul.LovedBy(victim);
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
            string s = "Holds the " + (corrupted ? "corrupted " : "") + "soul of " + who + ".\n"
                + "Grade: " + grade.Label() + (damagedOnBinding ? " (damaged: the vessel couldn't hold more)" : "") + ".";
            if (!residue.NullOrEmpty())
            {
                s += "\nResidue: " + residue + ".";
            }
            return s + "\nBound " + dateTaken + ".";
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (!parent.Spawned || parent.MapHeld?.IsPlayerHome != true)
            {
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = "Smash vessel",
                defaultDesc = "Break the vessel and render the soul inside into " + SoulGrades.SmashYield(grade)
                    + (corrupted ? " corrupted" : "") + " essence. Who they were is lost.",
                icon = ReapersMark.MarkIcon,
                action = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "Smash the vessel holding " + (victimName ?? "this soul") + "?", Smash, destructive: true)),
            };
        }

        private void Smash()
        {
            if (!parent.Spawned)
            {
                return;
            }
            IntVec3 cell = parent.Position;
            Map map = parent.Map;
            int yield = SoulGrades.SmashYield(grade);
            parent.Destroy(DestroyMode.Vanish);
            SoulUtility.DropEssence(cell, map, yield, EssenceDef);
            Corruption.Spill(cell, map, 30, 1.9f);
        }

        /// <summary>Broken by damage or fire: the soul gets out, and corruption spills where it broke.</summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (mode == DestroyMode.KillFinalize && previousMap != null)
            {
                Corruption.Spill(parent.Position, previousMap, Props.corruptionOnBreak, 3.9f);
                Messages.Message("A soul vessel broke. The soul of " + (victimName ?? "someone") + " is loose.",
                    new TargetInfo(parent.Position, previousMap), MessageTypeDefOf.NegativeEvent);
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref victimName, "victimName");
            Scribe_Values.Look(ref victimTitle, "victimTitle");
            Scribe_Values.Look(ref victimFaction, "victimFaction");
            Scribe_Values.Look(ref dateTaken, "dateTaken");
            Scribe_Values.Look(ref grade, "grade", SoulGrade.Common);
            Scribe_Values.Look(ref residue, "residue");
            Scribe_Values.Look(ref corrupted, "corrupted");
            Scribe_Values.Look(ref damagedOnBinding, "damagedOnBinding");
            Scribe_Collections.Look(ref lovedBy, "lovedBy", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && lovedBy == null)
            {
                lovedBy = new List<int>();
            }
        }
    }

    public class CompProperties_CanisterRack : CompProperties
    {
        /// <summary>How long a rack can be unpowered before its canisters start to vent.</summary>
        public int graceTicks = GenDate.TicksPerDay;

        public CompProperties_CanisterRack()
        {
            compClass = typeof(CompCanisterRack);
        }
    }

    /// <summary>
    /// Soul canisters need a powered rack. If the rack is unpowered for more than a day, every filled
    /// canister on it vents: corruption spills and the soul inside loses a grade. Rare tick on the
    /// rack only (one building), never on the canisters.
    /// </summary>
    public class CompCanisterRack : ThingComp
    {
        private int unpoweredTicks;

        public CompProperties_CanisterRack Props => (CompProperties_CanisterRack)props;

        public override void CompTickRare()
        {
            CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();
            if (power == null || power.PowerOn)
            {
                unpoweredTicks = 0;
                return;
            }
            unpoweredTicks += GenTicks.TickRareInterval;
            if (unpoweredTicks < Props.graceTicks)
            {
                return;
            }
            unpoweredTicks = 0;
            int vented = 0;
            foreach (IntVec3 c in parent.OccupiedRect())
            {
                foreach (Thing t in c.GetThingList(parent.Map))
                {
                    CompSoulVessel v = t.TryGetComp<CompSoulVessel>();
                    if (v != null && t.def.defName == "HP_VesselCanister")
                    {
                        v.grade = v.grade.Shift(-1);
                        vented++;
                    }
                }
            }
            if (vented > 0)
            {
                Corruption.Spill(parent.Position, parent.Map, 40 * vented, 3.9f);
                Messages.Message("A canister rack has been without power too long: " + vented + " soul canister"
                    + (vented == 1 ? "" : "s") + " vented, and the souls inside lost a grade.", parent, MessageTypeDefOf.NegativeEvent);
            }
        }

        public override string CompInspectStringExtra()
        {
            return unpoweredTicks > 0 ? "Unpowered: canisters vent in " + (Props.graceTicks - unpoweredTicks).ToStringTicksToPeriod() + "." : null;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref unpoweredTicks, "unpoweredTicks");
        }
    }
}
