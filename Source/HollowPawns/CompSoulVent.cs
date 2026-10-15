using RimWorld;
using Verse;

namespace HollowPawns
{
    public class CompProperties_SoulVent : CompProperties
    {
        /// <summary>Radius of the soul shock pulse when the generator breaks down.</summary>
        public float radius = 3.9f;

        public CompProperties_SoulVent()
        {
            compClass = typeof(CompSoulVent);
        }
    }

    /// <summary>
    /// When a soul generator breaks down it vents: a soul burst that shocks whoever is nearby.
    /// Runs only on the breakdown signal, never per tick.
    /// </summary>
    public class CompSoulVent : ThingComp
    {
        public CompProperties_SoulVent Props => (CompProperties_SoulVent)props;

        /// <summary>Burning essence leaves exhaust: a little corruption around the generator every hour it runs.</summary>
        public override void CompTick()
        {
            if (!parent.IsHashIntervalTick(GenDate.TicksPerHour) || !parent.Spawned)
            {
                return;
            }
            CompRefuelable fuel = parent.TryGetComp<CompRefuelable>();
            CompFlickable flick = parent.TryGetComp<CompFlickable>();
            if (fuel != null && fuel.HasFuel && (flick == null || flick.SwitchIsOn))
            {
                Corruption.Spill(parent.Position, parent.Map, 4, 2.9f);
            }
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (signal != CompBreakdownable.BreakdownSignal || !parent.Spawned)
            {
                return;
            }
            GenExplosion.DoExplosion(parent.Position, parent.Map, Props.radius, HP_DefOf.HP_SoulBurst, parent);
            Messages.Message(parent.LabelCap + " broke down and vented soul essence.", parent, MessageTypeDefOf.NegativeEvent);
        }
    }
}
