using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    public class CompProperties_WraithLantern : CompProperties
    {
        public float radius = 8.9f;
        public float fleeChance = 0.5f;
        public float soulShock = 0.1f;
        public int cooldownTicks = GenDate.TicksPerDay;

        public CompProperties_WraithLantern()
        {
            compClass = typeof(CompWraithLantern);
        }
    }

    /// <summary>
    /// The wraith lantern (4.13): worn on the belt, unshuttered on command. Enemies nearby see what's in
    /// it: a jolt of soul shock, and many break and run (vanilla PanicFlee). Once a day. Runs only when
    /// the button is pressed.
    /// </summary>
    public class CompWraithLantern : ThingComp
    {
        private int lastUsedTick = -999999;

        public CompProperties_WraithLantern Props => (CompProperties_WraithLantern)props;

        private int CooldownLeft => Mathf.Max(0, lastUsedTick + Props.cooldownTicks - Find.TickManager.TicksGame);

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            Pawn wearer = (parent as Apparel)?.Wearer;
            if (wearer == null || !wearer.IsColonistPlayerControlled || !wearer.Spawned)
            {
                yield break;
            }
            var cmd = new Command_Action
            {
                defaultLabel = "Unshutter lantern",
                defaultDesc = "Open the wraith lantern. Enemies within " + Props.radius.ToString("0.#") + " cells are soul-shocked and many break and run. Once a day.",
                icon = parent.def.uiIcon,
                action = () => Unshutter(wearer),
            };
            if (CooldownLeft > 0)
            {
                cmd.Disable("Recovering: " + CooldownLeft.ToStringTicksToPeriod() + ".");
            }
            yield return cmd;
        }

        private void Unshutter(Pawn wearer)
        {
            lastUsedTick = Find.TickManager.TicksGame;
            Map map = wearer.Map;
            FleckMaker.Static(wearer.DrawPos, map, FleckDefOf.PsycastAreaEffect, Props.radius);
            MoteMaker.ThrowText(wearer.DrawPos, map, "The lantern opens", new Color(0.8f, 0.85f, 1f));
            int fled = 0;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned.ToList())
            {
                if (p == wearer || p.Dead || !p.HostileTo(wearer) || !p.Position.InHorDistOf(wearer.Position, Props.radius)
                    || !GenSight.LineOfSight(wearer.Position, p.Position, map))
                {
                    continue;
                }
                HealthUtility.AdjustSeverity(p, HP_DefOf.HP_SoulShock, Props.soulShock);
                if (!p.Downed && !p.InMentalState && p.mindState?.mentalStateHandler != null && Rand.Chance(Props.fleeChance)
                    && p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, "wraith lantern", forced: true))
                {
                    fled++;
                }
            }
            Messages.Message(wearer.LabelShort + " unshuttered the wraith lantern" + (fled > 0 ? ": " + fled + " fled." : "."), wearer, MessageTypeDefOf.NeutralEvent);
        }

        public override string CompInspectStringExtra() => CooldownLeft > 0 ? "Recovering: " + CooldownLeft.ToStringTicksToPeriod() : "Ready.";

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", -999999);
        }
    }
}
