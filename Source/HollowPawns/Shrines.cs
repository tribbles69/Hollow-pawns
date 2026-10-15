using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    /// <summary>Anything that pushes the ground's corruption up or down around itself, on the corruption pass.</summary>
    public interface ICorruptionSource
    {
        void ApplyCorruption(MapComponent_Corruption corruption);
    }

    public static class ShrineUtility
    {
        /// <summary>
        /// Nudges a few random cells within the radius, the same sampled way the rest of corruption works:
        /// negative cleanses, positive corrupts. Desecration never pushes ground past 200.
        /// </summary>
        public static void Touch(MapComponent_Corruption c, IntVec3 center, float radius, int amount, int cells = 6)
        {
            int n = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < cells; i++)
            {
                IntVec3 cell = center + GenRadial.RadialPattern[Rand.Range(0, n)];
                if (amount > 0 && c.At(cell) >= 200)
                {
                    continue;
                }
                c.Add(cell, amount);
            }
        }
    }

    // ======================= Reliquary =======================

    /// <summary>
    /// A display case for one filled vessel (4.35). Shows who's inside; anyone who cared about them is
    /// "Remembered" while near it. Plain storage: haulers bring a vessel, nothing ticks.
    /// </summary>
    public class Building_Reliquary : Building_Storage
    {
        public CompSoulVessel HeldVessel
        {
            get
            {
                if (!Spawned)
                {
                    return null;
                }
                foreach (Thing t in Position.GetThingList(Map))
                {
                    CompSoulVessel v = t.TryGetComp<CompSoulVessel>();
                    if (v != null && !v.victimName.NullOrEmpty())
                    {
                        return v;
                    }
                }
                return null;
            }
        }

        public override string GetInspectString()
        {
            CompSoulVessel v = HeldVessel;
            string line = v == null ? "Empty. Haulers will bring a filled vessel." : "Keeps the soul of " + v.victimName + " (" + v.grade.Label() + ").";
            string b = base.GetInspectString();
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }
    }

    /// <summary>"Remembered": near a reliquary holding someone this pawn cared about.</summary>
    public class ThoughtWorker_Remembered : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned || p.Map == null)
            {
                return ThoughtState.Inactive;
            }
            foreach (Building_Reliquary b in p.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Reliquary>())
            {
                if (!p.Position.InHorDistOf(b.Position, 8f))
                {
                    continue;
                }
                CompSoulVessel v = b.HeldVessel;
                if (v?.lovedBy != null && v.lovedBy.Contains(p.thingIDNumber) && GenSight.LineOfSight(p.Position, b.Position, p.Map))
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            return ThoughtState.Inactive;
        }
    }

    // ======================= Bound shrine =======================

    /// <summary>
    /// The bound shrine (4.35). Haulers bring one filled vessel; the shrine takes the soul in and keeps it
    /// for good. Before that, choose consecrated (cleans the ground) or desecrated (corrupts it); once
    /// bound the choice is fixed. Radius and strength scale with the soul's grade. Breaking it frees
    /// the soul in a burst of the opposite effect.
    /// </summary>
    public class Building_BoundShrine : Building_Storage, ICorruptionSource
    {
        public BoundSoul soul;
        public bool desecrated;

        public bool Bound => soul != null;

        private float Radius => soul == null ? 0f : 4.9f + (int)soul.grade * 1.4f;

        private int Strength => soul == null ? 0 : 3 + (int)soul.grade;

        /// <summary>Takes in a delivered vessel on the rare tick (not mid-haul, when the item is still spawning).</summary>
        public override void TickRare()
        {
            base.TickRare();
            if (Bound || !Spawned)
            {
                return;
            }
            Thing vesselThing = Position.GetThingList(Map).FirstOrDefault(t => !(t.TryGetComp<CompSoulVessel>()?.victimName.NullOrEmpty() ?? true));
            if (vesselThing == null)
            {
                return;
            }
            soul = BoundSoul.From(vesselThing.TryGetComp<CompSoulVessel>());
            vesselThing.Destroy(DestroyMode.Vanish);
            GetStoreSettings().filter.SetDisallowAll();
            Messages.Message("The soul of " + soul.ShortName + " is bound into the shrine. It will " + (desecrated ? "foul" : "cleanse") + " the ground for good.",
                this, MessageTypeDefOf.NeutralEvent);
        }

        public void ApplyCorruption(MapComponent_Corruption c)
        {
            if (Bound)
            {
                ShrineUtility.Touch(c, Position, Radius, desecrated ? Strength : -Strength);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Corruption.For(map)?.Register(this);
            if (Bound)
            {
                GetStoreSettings().filter.SetDisallowAll();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            IntVec3 pos = Position;
            Corruption.For(map)?.Deregister(this);
            base.DeSpawn(mode);
            if (Bound && mode == DestroyMode.KillFinalize)
            {
                // The soul breaks free in a burst of the opposite of what it was bound to do.
                Corruption.Spill(pos, map, desecrated ? -150 : 150, 5.9f);
                Messages.Message("The shrine of " + soul.ShortName + " broke and the soul tore free.", new TargetInfo(pos, map), MessageTypeDefOf.NegativeEvent);
            }
        }

        public override string LabelNoCount => Bound ? "shrine of " + soul.ShortName + (desecrated ? " (desecrated)" : " (consecrated)") : base.LabelNoCount;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Bound || Faction != Faction.OfPlayer)
            {
                yield break;
            }
            yield return new Command_Toggle
            {
                defaultLabel = desecrated ? "Desecrate" : "Consecrate",
                defaultDesc = "What the shrine does with the soul bound into it. Fixed once a soul is bound.\n\n"
                    + "Consecrated: slowly cleans corrupted ground around it.\nDesecrated: slowly corrupts it.",
                icon = ReapersMark.MarkIcon,
                isActive = () => desecrated,
                toggleAction = () => desecrated = !desecrated,
            };
        }

        public override string GetInspectString()
        {
            string line = Bound
                ? "Holds the " + soul.grade.Label() + " soul of " + soul.ShortName + ". " + (desecrated ? "Corrupts" : "Cleanses") + " the ground within " + Radius.ToString("0.#") + " cells."
                : "Unbound. Haulers will bring a filled vessel; its soul goes into the shrine for good. Will " + (desecrated ? "desecrate." : "consecrate.");
            string b = base.GetInspectString();
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref soul, "soul");
            Scribe_Values.Look(ref desecrated, "desecrated");
        }
    }

    // ======================= Essence shrine =======================

    public class CompProperties_EssenceShrine : CompProperties
    {
        public float radius = 6.9f;
        public int strength = 5;

        /// <summary>Essence burned per day while it works.</summary>
        public float fuelPerDay = 3f;

        public CompProperties_EssenceShrine()
        {
            compClass = typeof(CompEssenceShrine);
        }
    }

    /// <summary>
    /// The essence shrine (4.35): burns essence like fuel and either cleans or spreads corruption, the
    /// player's choice, on a fixed radius. Fuel is burned on the shrine's rare tick (one building).
    /// </summary>
    public class CompEssenceShrine : ThingComp, ICorruptionSource
    {
        public bool spread;

        public CompProperties_EssenceShrine Props => (CompProperties_EssenceShrine)props;

        private CompRefuelable Fuel => parent.TryGetComp<CompRefuelable>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            Corruption.For(parent.Map)?.Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            Corruption.For(map)?.Deregister(this);
        }

        public override void CompTickRare()
        {
            CompRefuelable fuel = Fuel;
            if (fuel != null && fuel.HasFuel)
            {
                fuel.ConsumeFuel(Props.fuelPerDay * GenTicks.TickRareInterval / GenDate.TicksPerDay);
            }
        }

        public void ApplyCorruption(MapComponent_Corruption c)
        {
            if (Fuel?.HasFuel ?? true)
            {
                ShrineUtility.Touch(c, parent.Position, Props.radius, spread ? Props.strength : -Props.strength);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            yield return new Command_Toggle
            {
                defaultLabel = spread ? "Spreading" : "Cleansing",
                defaultDesc = "Cleansing: burns essence to clean corrupted ground around it.\nSpreading: burns essence to corrupt it.",
                icon = ReapersMark.MarkIcon,
                isActive = () => spread,
                toggleAction = () => spread = !spread,
            };
        }

        public override string CompInspectStringExtra()
        {
            return (spread ? "Spreading corruption" : "Cleansing corruption") + " within " + Props.radius.ToString("0.#") + " cells"
                + (Fuel?.HasFuel ?? true ? "." : " (no essence).");
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref spread, "spread");
        }
    }
}
