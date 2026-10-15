using HarmonyLib;
using Verse;

namespace HollowPawns
{
    public class HollowPawnsMod : Mod
    {
        public HollowPawnsMod(ModContentPack content) : base(content)
        {
            new Harmony("aaronclews.hollowpawns").PatchAll();
        }
    }
}
