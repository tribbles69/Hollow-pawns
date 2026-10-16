using HarmonyLib;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    public class HollowPawnsSettings : ModSettings
    {
        /// <summary>Chance a neglected, ensouled corpse rises as a ghost (4.8). Rolled once per corpse.</summary>
        public float ghostChance = 0.01f;

        /// <summary>Thralls use the lean HP_Thrall think tree (4.26). Off falls back to the full humanlike tree.</summary>
        public bool leanThrallBrain = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ghostChance, "ghostChance", 0.01f);
            Scribe_Values.Look(ref leanThrallBrain, "leanThrallBrain", true);
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
            list.Gap();
            list.CheckboxLabeled("Lean thrall brain", ref Settings.leanThrallBrain,
                "Thralls think with a cut-down think tree: no recreation, socialising or styling; idle thralls stand down at muster spots. Much cheaper for big thrall bases. Turn off if thralls misbehave.");
            list.End();
        }
    }
}
