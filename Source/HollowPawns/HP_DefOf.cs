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
        public static ThingDef HP_CarrionBale;
        public static RecipeDef HP_BindCarrion;
        public static ThingDef HP_CarrionWall;
        public static ThingDef HP_ConscriptRifle;
        public static ThingDef HP_ConscriptHelmet;
        public static ThingDef HP_ConscriptVest;
        public static BodyPartDef HP_Soul;
        public static ResearchProjectDef HP_SoulExtraction;
        public static DamageDef HP_SoulBlast;
        public static DamageDef HP_SoulBurst;
        public static TaleDef HP_SoulBound;
        public static JobDef HP_PerformSoulRite;
        public static JobDef HP_LoadSoulBattery;
        public static JobDef HP_StaffMachine;
        public static HediffDef HP_SoulShock;
        public static JobDef HP_CleanseCorruption;
        public static DesignationDef HP_Cleanse;
        public static ThingDef HP_HolyWater;
        public static ThoughtDef HP_WitnessedSoulRite;
        public static HediffDef HP_SoulCorruption;
        public static HediffDef HP_PurgationPain;
        public static HediffDef HP_RiteRestrained;
        public static HediffDef HP_ReanimatedHusk;
        public static HediffDef HP_StandingGuard;
        public static HediffDef HP_Shackled;
        public static ThingDef HP_FallenIdol;
        public static ThingDef HP_GhostRace;
        public static PawnKindDef HP_GhostLingering;
        public static PawnKindDef HP_GhostWrathful;
        public static JobDef HP_GhostDrift;
        public static HediffDef HP_GraveChill;
        public static DamageDef HP_SoulArrow;
        public static ThingDef HP_ReaperScythe;
        public static ThingDef HP_Soulsteel;
        public static HediffDef HP_SoulShard;
        public static HediffDef HP_FaceTaken;
        public static ThingDef HP_FaceMask;
        public static ThoughtDef HP_FaceFlayed;
        public static HediffDef HP_FullKit;
        public static HediffDef HP_ReliquaryWard;
        public static HediffDef HP_RecentlyBathed;
        public static JobDef HP_BatheInFountain;
        public static JobDef HP_EnterSoulCradle;
        public static HediffDef HP_BloodDebt;
        public static HediffDef HP_Tormented;
        public static PrisonerInteractionModeDef HP_Torment;
        public static JobDef HP_TormentPrisoner;
        public static ThoughtDef HP_Avenging;
        public static ThoughtDef HP_AvengedMemory;
        public static ThoughtDef HP_TormentDisturbed;
        public static ThoughtDef HP_HeardEffigyScream;
        public static ThingDef HP_WailingEffigy;
        public static SoundDef HP_EffigyWail;

        // Biotech-only (Mods/Biotech). Null without Biotech; every use checks.
        [MayRequireBiotech] public static GeneDef HP_CorruptedSoul;
        [MayRequireBiotech] public static GeneDef HP_Soulless;
        [MayRequireBiotech] public static XenotypeDef HP_ThrallXenotype;
        [MayRequireBiotech] public static PawnKindDef HP_ThrallKind;
        [MayRequireBiotech] public static HediffDef HP_ThrallDecay;
        [MayRequireBiotech] public static FactionDef HP_Demons;
        [MayRequireBiotech] public static IncidentDef HP_DemonWave;

        static HP_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HP_DefOf));
        }
    }
}
