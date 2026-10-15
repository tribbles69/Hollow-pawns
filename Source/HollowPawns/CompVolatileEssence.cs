using RimWorld;
using Verse;

namespace HollowPawns
{
    public class CompProperties_VolatileEssence : CompProperties
    {
        public float explosiveRadius = 1.5f;
        public float explosiveExpandPerStackcount = 0.03f;
        /// <summary>The stack bursts once its hit points fall to this fraction (or on any fire damage).</summary>
        public float burstHitPointsPercent = 0.5f;
        public string extraInspectStringKey;

        public CompProperties_VolatileEssence()
        {
            compClass = typeof(CompVolatileEssence);
        }
    }

    /// <summary>
    /// Volatile essence stacks. Vanilla CompExplosive needs every stack to tick for its wick, which the mod
    /// won't do, so this bursts at once on the damage event instead: fire, heavy damage or destruction.
    /// Same radius rule as chemfuel (base plus a little per item). Never ticks.
    /// </summary>
    public class CompVolatileEssence : ThingComp
    {
        bool bursting;

        public CompProperties_VolatileEssence Props => (CompProperties_VolatileEssence)props;

        float Radius => Props.explosiveRadius + parent.stackCount * Props.explosiveExpandPerStackcount;

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (bursting || !parent.Spawned)
            {
                return;
            }
            if (dinfo.Def == DamageDefOf.Flame || parent.HitPoints <= parent.MaxHitPoints * Props.burstHitPointsPercent)
            {
                Burst(parent.Position, parent.Map);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (!bursting && mode == DestroyMode.KillFinalize && previousMap != null)
            {
                Burst(parent.Position, previousMap);
            }
        }

        void Burst(IntVec3 cell, Map map)
        {
            bursting = true;
            float radius = Radius;
            if (!parent.Destroyed)
            {
                parent.Destroy(DestroyMode.Vanish);
            }
            GenExplosion.DoExplosion(cell, map, radius, HP_DefOf.HP_SoulBurst, null);
        }

        public override string CompInspectStringExtra()
        {
            return Props.extraInspectStringKey.NullOrEmpty() ? null : Props.extraInspectStringKey.Translate().ToString();
        }
    }
}
