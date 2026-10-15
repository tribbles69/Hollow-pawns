using Verse;

namespace HollowPawns
{
    /// <summary>Matches corpses with no soul left in them.</summary>
    public class SpecialThingFilterWorker_Soulless : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return t is Corpse corpse && !SoulUtility.HasSoul(corpse.InnerPawn);
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def.IsCorpse;
        }
    }

    /// <summary>Matches corpses whose soul is still in them: the ones worth taking to the crucible.</summary>
    public class SpecialThingFilterWorker_Ensouled : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return t is Corpse corpse && SoulUtility.HasSoul(corpse.InnerPawn);
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def.IsCorpse;
        }
    }
}
