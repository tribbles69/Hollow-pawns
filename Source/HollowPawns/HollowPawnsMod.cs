using HarmonyLib;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    public class HollowPawnsSettings : ModSettings
    {
        /// <summary>Chance a neglected, ensouled corpse rises as a ghost (4.8). Rolled once per corpse.</summary>
        public float ghostChance = 0.01f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ghostChance, "ghostChance", 0.01f);
        }
    }

    public class HollowPawnsMod : Mod
    {
        public static HollowPawnsSettings Settings;

        public HollowPawnsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HollowPawnsSettings>();
            new Harmony("aaronclews.hollowpawns").PatchAll();
        }

        public override string SettingsCategory() => "Hollow Pawns";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            list.Label("Ghost chance per neglected corpse: " + (Settings.ghostChance * 100f).ToString("0.#") + "%",
                tooltip: "Each adult corpse that still has its soul and has lain unharvested for about a day rolls this once. Default 1%.");
            Settings.ghostChance = Mathf.Round(list.Slider(Settings.ghostChance, 0f, 0.2f) * 1000f) / 1000f;
            list.End();
        }
    }
}
