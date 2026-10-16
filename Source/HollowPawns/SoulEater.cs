using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>
    /// The soul eater (4.15, Biotech): a xenotype that lives on souls the way sanguophages live on
    /// hemogen. The gene is a resource that drains every day; a soul drain (touch) takes the whole
    /// soul and kills, as Aaron chose; soul essence can be devoured to top up. At zero the eater starves
    /// and, given long enough, dies.
    /// </summary>
    public class Gene_SoulHunger : Gene_Resource, IGeneResourceDrain
    {
        public Gene_Resource Resource => this;

        public Pawn Pawn => pawn;

        public bool CanOffset => Active;

        public string DisplayLabel => Label + " (" + "Gene".Translate() + ")";

        public float ResourceLossPerDay => def.resourceLossPerDay;

        public override float InitialResourceMax => 1f;

        public override float MinLevelForAlert => 0.15f;

        public override float MaxLevelOffset => 0.1f;

        protected override Color BarColor => new Color(0.35f, 0.75f, 0.82f);

        protected override Color BarHighlightColor => new Color(0.55f, 0.88f, 0.94f);

        public override void PostAdd()
        {
            base.PostAdd();
            Reset();
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            GeneResourceDrainUtility.TickResourceDrainInterval(this, delta);
            if (!pawn.IsHashIntervalTick(GenDate.TicksPerHour, delta) || !Active)
            {
                return;
            }
            Hediff starved = pawn.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_SoulStarved);
            if (Value <= 0.001f)
            {
                // Two days or so at nothing, and the eater's own soul gives out.
                HealthUtility.AdjustSeverity(pawn, HP_DefOf.HP_SoulStarved, 0.02f);
            }
            else if (starved != null)
            {
                starved.Severity -= 0.05f;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!Active)
            {
                yield break;
            }
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            foreach (Gizmo g in GeneResourceDrainUtility.GetResourceDrainGizmos(this))
            {
                yield return g;
            }
        }
    }

    public static class SoulEater
    {
        public static Gene_SoulHunger GeneOf(Pawn p) => p?.genes?.GetFirstGeneOfType<Gene_SoulHunger>();

        public static void Feed(Pawn p, float amount)
        {
            Gene_SoulHunger g = GeneOf(p);
            if (g != null)
            {
                GeneResourceDrainUtility.OffsetResource(g, amount);
            }
        }
    }

    public class CompProperties_AbilitySoulDrain : CompProperties_AbilityEffect
    {
        public float baseGain = 0.3f;
        public float gainPerGrade = 0.08f;

        public CompProperties_AbilitySoulDrain()
        {
            compClass = typeof(CompAbilityEffect_SoulDrain);
        }
    }

    /// <summary>A soul drain: the whole soul, so the victim dies. A better soul feeds more.</summary>
    public class CompAbilityEffect_SoulDrain : CompAbilityEffect
    {
        public new CompProperties_AbilitySoulDrain Props => (CompProperties_AbilitySoulDrain)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!(target.Pawn is Pawn victim) || !SoulUtility.CanExtract(victim))
            {
                if (throwMessages)
                {
                    Messages.Message("Only an adult with a soul (and not Soul-Locked) can be drained.", MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (!(target.Pawn is Pawn victim) || !SoulUtility.CanExtract(victim))
            {
                return;
            }
            SoulGrade grade = SoulGrades.Of(victim);
            SoulEater.Feed(caster, Props.baseGain + Props.gainPerGrade * (int)grade);
            Corruption.Spill(victim.Position, victim.Map, 30, 1.9f);
            Corruption.AddToPawn(caster, 0.03f);
            FleckMaker.Static(victim.DrawPos, victim.Map, FleckDefOf.PsycastAreaEffect, 1.5f);
            Messages.Message(caster.LabelShort + " drained the " + grade.Label() + " soul of " + victim.LabelShort + ".", caster, MessageTypeDefOf.NeutralEvent);
            CorruptionCult.Record(HP_DefOf.HP_SoulTaken, caster);
            SoulUtility.TakeSoulAndKill(victim);
        }
    }

    public class CompProperties_AbilityDevourEssence : CompProperties_AbilityEffect
    {
        public int maxEssence = 5;
        public float gainPerEssence = 0.06f;

        public CompProperties_AbilityDevourEssence()
        {
            compClass = typeof(CompAbilityEffect_DevourEssence);
        }
    }

    /// <summary>Devour essence: eat up to five essence from a stack within reach. A poor meal, but no one has to die.</summary>
    public class CompAbilityEffect_DevourEssence : CompAbilityEffect
    {
        public new CompProperties_AbilityDevourEssence Props => (CompProperties_AbilityDevourEssence)props;

        private static bool IsEssence(Thing t) => t != null && (t.def == HP_DefOf.HP_SoulEssence || t.def == HP_DefOf.HP_CorruptedEssence);

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!IsEssence(target.Thing))
            {
                if (throwMessages)
                {
                    Messages.Message("Target a stack of soul essence.", MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Thing stack = target.Thing;
            if (!IsEssence(stack) || stack.Destroyed)
            {
                return;
            }
            int n = Mathf.Min(Props.maxEssence, stack.stackCount);
            bool corrupt = stack.def == HP_DefOf.HP_CorruptedEssence;
            stack.SplitOff(n).Destroy();
            SoulEater.Feed(parent.pawn, n * Props.gainPerEssence);
            if (corrupt)
            {
                Corruption.AddToPawn(parent.pawn, 0.02f * n);
            }
        }
    }
}
