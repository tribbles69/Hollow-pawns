using HarmonyLib;
using Verse;

namespace HollowPawns
{
    /// <summary>Applies every Harmony patch in this assembly once defs have loaded.</summary>
    [StaticConstructorOnStartup]
    public static class HollowPawnsStartup
    {
        public const string HarmonyId = "tribbles69.hollowpawns";

        static HollowPawnsStartup()
        {
            new Harmony(HarmonyId).PatchAll();
        }
    }
}
