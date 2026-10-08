using RimWorld;
using Verse;

namespace HollowPawns
{
    [DefOf]
    public static class HP_DefOf
    {
        public static ThingDef HP_SoulEssence;
        public static BodyPartDef HP_Soul;

        static HP_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HP_DefOf));
        }
    }
}
