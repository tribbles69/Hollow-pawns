using RimWorld;
using Verse;

namespace HollowPawns
{
    [DefOf]
    public static class HP_DefOf
    {
        public static ThingDef HP_SoulEssence;
        public static ThingDef HP_CorruptedEssence;
        public static ThingDef HP_SoulGenerator;
        public static ThingDef HP_SoulJar;
        public static BodyPartDef HP_Soul;
        public static RecipeDef HP_ExtractSoul;
        public static ResearchProjectDef HP_SoulExtraction;
        public static DamageDef HP_SoulBurst;
        public static TaleDef HP_BottledSoul;

        // Demons live in Mods/Biotech and only exist with Biotech. Null without it.
        [MayRequireBiotech] public static GeneDef HP_CorruptedSoul;
        [MayRequireBiotech] public static FactionDef HP_Demons;
        [MayRequireBiotech] public static IncidentDef HP_DemonWave;

        static HP_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HP_DefOf));
        }
    }
}
