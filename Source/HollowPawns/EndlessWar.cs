using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HollowPawns
{
    // ======================= Thralls and husks: alive without a soul =======================

    /// <summary>
    /// Soulless pawns (vat-grown thralls, reanimated husks) only need food and rest: no mood, so no
    /// thoughts, mental breaks or recreation. Cheaper to simulate than colonists (4.26).
    /// </summary>
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Patch_NeedsTracker_ShouldHaveNeed
    {
        public static void Postfix(NeedDef nd, Pawn ___pawn, ref bool __result)
        {
            if (__result && nd != NeedDefOf.Food && nd != NeedDefOf.Rest && SoulUtility.IsSoulless(___pawn))
            {
                __result = false;
            }
        }
    }

    /// <summary>The soulless don't chat, so they form no relationships.</summary>
    [HarmonyPatch(typeof(SocialInteractionUtility), nameof(SocialInteractionUtility.CanInitiateInteraction))]
    public static class Patch_CanInitiateInteraction
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result && SoulUtility.IsSoulless(pawn))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(SocialInteractionUtility), nameof(SocialInteractionUtility.CanReceiveInteraction))]
    public static class Patch_CanReceiveInteraction
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result && SoulUtility.IsSoulless(pawn))
            {
                __result = false;
            }
        }
    }

    // ======================= Clone vat (Biotech) =======================

    /// <summary>
    /// The clone vat: a worker starts a culture (the bill eats meat, steel and essence quickly), then
    /// the vat grows it for a few days on its own and decants an adult thrall in basic gear. Rare tick
    /// while growing; idle otherwise. Takes no new bill while it's growing.
    /// </summary>
    public class Building_CloneVat : Building_WorkTable, IBillGiver
    {
        public const float GrowDays = 3f;

        private int growTicksLeft = -1;

        public bool Growing => growTicksLeft > 0;

        // Re-implements IBillGiver: Building_WorkTable's version isn't virtual, and work givers call it through the interface.
        bool IBillGiver.CurrentlyUsableForBills() => !Growing && base.CurrentlyUsableForBills();

        public void StartGrowing()
        {
            growTicksLeft = Mathf.RoundToInt(GrowDays * GenDate.TicksPerDay);
        }

        public override void TickRare()
        {
            base.TickRare();
            if (!Growing)
            {
                return;
            }
            CompPowerTrader power = GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                return;
            }
            growTicksLeft -= GenTicks.TickRareInterval;
            if (growTicksLeft <= 0)
            {
                growTicksLeft = -1;
                Decant();
            }
        }

        private void Decant()
        {
            PawnKindDef kind = HP_DefOf.HP_ThrallKind;
            if (kind == null)
            {
                return;
            }
            var request = new PawnGenerationRequest(kind, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false, mustBeCapableOfViolence: true,
                allowAddictions: false, forceNoIdeo: true, fixedBiologicalAge: 20f, fixedChronologicalAge: 0f,
                forcedXenotype: HP_DefOf.HP_ThrallXenotype, developmentalStages: DevelopmentalStage.Adult);
            Pawn thrall = PawnGenerator.GeneratePawn(request);
            thrall.Name = new NameSingle("Thrall " + Rand.RangeInclusive(100, 999));
            if (HP_DefOf.HP_ThrallDecay != null)
            {
                thrall.health.AddHediff(HP_DefOf.HP_ThrallDecay);
            }
            GenSpawn.Spawn(thrall, InteractionCell, Map);
            Messages.Message("A thrall has been decanted from the clone vat.", thrall, MessageTypeDefOf.PositiveEvent);
        }

        public override string GetInspectString()
        {
            var sb = new StringBuilder(base.GetInspectString());
            if (Growing)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append("Growing a thrall: " + growTicksLeft.ToStringTicksToPeriod() + " left.");
            }
            return sb.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (DebugSettings.ShowDevGizmos && Growing)
            {
                yield return new Command_Action { defaultLabel = "DEV: Decant now", action = () => growTicksLeft = 1 };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref growTicksLeft, "growTicksLeft", -1);
        }
    }

    /// <summary>The "start thrall culture" bill: when it finishes, the vat it was done at starts growing.</summary>
    public class RecipeWorker_StartThrall : RecipeWorker
    {
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            if (billDoer?.CurJob?.targetA.Thing is Building_CloneVat vat)
            {
                vat.StartGrowing();
            }
        }
    }

    // ======================= Reanimated husks (4.6) =======================

    /// <summary>
    /// The reanimation slab's bill: the soulless corpse is raised as a husk (the same body and gear),
    /// on the colony's side, with the husk condition: no soul, few needs, bad stats, a short life.
    /// The corpse isn't destroyed: resurrection turns it back into the pawn where it lies.
    /// </summary>
    public class RecipeWorker_Reanimate : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (!(ingredient is Corpse corpse))
            {
                base.ConsumeIngredient(ingredient, recipe, map);
                return;
            }
            Pawn husk = corpse.InnerPawn;
            if (husk == null || !husk.DevelopmentalStage.Adult() || !ResurrectionUtility.TryResurrect(husk))
            {
                return;
            }
            if (husk.Faction != Faction.OfPlayer)
            {
                husk.SetFaction(Faction.OfPlayer);
            }
            husk.guest?.SetGuestStatus(null);
            husk.health.AddHediff(HP_DefOf.HP_ReanimatedHusk);
            husk.needs?.AddOrRemoveNeedsAsAppropriate();
            husk.mindState?.mentalStateHandler?.Reset();
            Messages.Message(husk.LabelShort + " rises as a husk.", husk, MessageTypeDefOf.NeutralEvent);
        }
    }

    // ======================= Stand guard (4.26) =======================

    public static class StandGuard
    {
        /// <summary>Thralls and husks can be told to hold a post: drafted in place, hunger and rest slowed.</summary>
        public static Gizmo GizmoFor(Pawn pawn)
        {
            bool on = pawn.health.hediffSet.HasHediff(HP_DefOf.HP_StandingGuard);
            return new Command_Toggle
            {
                defaultLabel = "Stand guard",
                defaultDesc = "Hold this spot: stand still and fire on anything hostile in range. Hunger slows to a trickle and they don't tire. They stand down on their own when they get hungry.",
                icon = TexCommand.Draft,
                isActive = () => on,
                toggleAction = () => Set(pawn, !on),
            };
        }

        public static void Set(Pawn pawn, bool on)
        {
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_StandingGuard);
            if (on)
            {
                if (h == null)
                {
                    pawn.health.AddHediff(HP_DefOf.HP_StandingGuard);
                }
                if (pawn.drafter != null)
                {
                    pawn.drafter.Drafted = true;
                }
            }
            else
            {
                if (h != null)
                {
                    pawn.health.RemoveHediff(h);
                }
                if (pawn.drafter != null)
                {
                    pawn.drafter.Drafted = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_StandGuard
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (Gizmo g in gizmos)
            {
                yield return g;
            }
            if (__instance.Faction == Faction.OfPlayer && __instance.drafter != null && !__instance.Dead
                && SoulUtility.IsSoulless(__instance))
            {
                yield return StandGuard.GizmoFor(__instance);
            }
        }
    }

    // ======================= Map-level upkeep: carrion rot and guard posts =======================

    /// <summary>
    /// Carrion walls rot (lose hit points and leak rot stink above freezing), and standing guards
    /// stand down when hungry or undrafted. One sampled batch of walls per rare interval; guards are
    /// checked on the same interval. No per-wall ticking.
    /// </summary>
    public class MapComponent_EndlessWar : MapComponent
    {
        public const int WallsPerPass = 12;

        private int wallCursor;

        public MapComponent_EndlessWar(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if ((Find.TickManager.TicksGame + map.uniqueID * 7) % GenTicks.TickRareInterval != 0)
            {
                return;
            }
            RotWalls();
            CheckGuards();
        }

        private void RotWalls()
        {
            ThingDef def = HP_DefOf.HP_CarrionWall;
            List<Thing> walls = def == null ? null : map.listerThings.ThingsOfDef(def);
            if (walls == null || walls.Count == 0)
            {
                return;
            }
            for (int k = 0; k < Mathf.Min(WallsPerPass, walls.Count); k++)
            {
                wallCursor = (wallCursor + 1) % walls.Count;
                Thing wall = walls[wallCursor];
                if (wall.AmbientTemperature <= 0f)
                {
                    // Frozen carrion doesn't rot.
                    continue;
                }
                // Each wall is visited roughly every (walls/12) rare ticks; scale so a wall loses ~1% a day.
                float visitsPerDay = GenDate.TicksPerDay / (float)GenTicks.TickRareInterval * WallsPerPass / walls.Count;
                float loss = wall.MaxHitPoints * 0.01f / Mathf.Max(visitsPerDay, 0.01f);
                if (Rand.Chance(loss - Mathf.Floor(loss)))
                {
                    loss += 1f;
                }
                int dmg = Mathf.FloorToInt(loss);
                if (dmg > 0)
                {
                    wall.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, dmg));
                }
                if (Rand.Chance(0.25f) && !wall.Destroyed)
                {
                    IntVec3 out1 = wall.Position + GenAdj.CardinalDirections.RandomElement();
                    if (out1.InBounds(map) && !out1.Impassable(map))
                    {
                        GasUtility.AddGas(out1, map, GasType.RotStink, 40);
                    }
                }
            }
        }

        private void CheckGuards()
        {
            if (HP_DefOf.HP_StandingGuard == null)
            {
                return;
            }
            foreach (Pawn p in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                Hediff h = p.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_StandingGuard);
                if (h == null)
                {
                    continue;
                }
                bool hungry = p.needs?.food != null && p.needs.food.CurLevelPercentage < 0.15f;
                if (hungry || p.drafter == null || !p.drafter.Drafted)
                {
                    StandGuard.Set(p, false);
                    if (hungry)
                    {
                        Messages.Message(p.LabelShort + " stands down from guard to eat.", p, MessageTypeDefOf.NeutralEvent, false);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref wallCursor, "wallCursor");
        }
    }

    // ======================= Corpse grinder (4.31) =======================

    /// <summary>
    /// Pulls corpses from the cells around it (haulers fill a stockpile there) and grinds them: carrion
    /// bales, a little plain essence from adult human souls, meat and leather. No husk: the whole body
    /// goes. Never children. Rare tick, one corpse per cycle, only while powered.
    /// </summary>
    public class Building_CorpseGrinder : Building
    {
        public const int TicksPerCorpse = 1000;
        public const int EssencePerSoul = 4;

        private int cooldown;

        public override void TickRare()
        {
            base.TickRare();
            CompPowerTrader power = GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                return;
            }
            cooldown -= GenTicks.TickRareInterval;
            if (cooldown > 0)
            {
                return;
            }
            Corpse corpse = NextCorpse();
            if (corpse == null)
            {
                return;
            }
            cooldown = TicksPerCorpse;
            Grind(corpse);
        }

        private Corpse NextCorpse()
        {
            foreach (IntVec3 c in GenAdj.CellsAdjacent8Way(this))
            {
                if (!c.InBounds(Map))
                {
                    continue;
                }
                foreach (Thing t in c.GetThingList(Map))
                {
                    if (t is Corpse corpse && !corpse.IsForbidden(Faction.OfPlayer) && Grindable(corpse))
                    {
                        return corpse;
                    }
                }
            }
            return null;
        }

        public static bool Grindable(Corpse corpse)
        {
            Pawn p = corpse.InnerPawn;
            if (p == null || p.RaceProps.IsMechanoid)
            {
                return false;
            }
            return !p.RaceProps.Humanlike || p.DevelopmentalStage.Adult();
        }

        private void Grind(Corpse corpse)
        {
            IntVec3 output = InteractionCell;
            Map map = Map;
            Pawn inner = corpse.InnerPawn;
            if (SoulUtility.HasSoul(inner))
            {
                SoulUtility.DropEssence(output, map, EssencePerSoul, SoulUtility.EssenceDefAt(inner, Position, map));
            }
            Thing bale = ThingMaker.MakeThing(HP_DefOf.HP_CarrionBale);
            bale.stackCount = inner.RaceProps.Humanlike ? 2 : Mathf.Clamp(Mathf.RoundToInt(inner.BodySize * 2f), 1, 6);
            GenPlace.TryPlaceThing(bale, output, map, ThingPlaceMode.Near);
            foreach (Thing product in corpse.ButcherProducts(null, 0.6f).ToList())
            {
                GenPlace.TryPlaceThing(product, output, map, ThingPlaceMode.Near);
            }
            corpse.Destroy();
            Corruption.Spill(Position, map, 60, 4.9f);
        }
    }

    // ======================= Line forge (4.24) =======================

    /// <summary>
    /// Turns steel and components lying next to it into cheap conscript kit, with no worker and no
    /// essence. Pick what it makes; it pulls materials from adjacent cells and drops kit on its
    /// output cell. Rare tick, and only works while powered and fed.
    /// </summary>
    public class Building_LineForge : Building
    {
        public enum Product : byte { Off, Rifle, Armour, FullKit }

        private Product product = Product.Off;
        private int workLeft = -1;

        private static int SteelFor(Product p) => p == Product.Rifle ? 40 : p == Product.Armour ? 60 : p == Product.FullKit ? 100 : 0;
        private static int ComponentsFor(Product p) => p == Product.Rifle || p == Product.FullKit ? 1 : 0;
        private static int TicksFor(Product p) => p == Product.FullKit ? 5000 : 2500;

        public override void TickRare()
        {
            base.TickRare();
            CompPowerTrader power = GetComp<CompPowerTrader>();
            if (product == Product.Off || (power != null && !power.PowerOn))
            {
                return;
            }
            if (workLeft < 0)
            {
                if (!TryTakeMaterials(SteelFor(product), ComponentsFor(product)))
                {
                    return;
                }
                workLeft = TicksFor(product);
            }
            workLeft -= GenTicks.TickRareInterval;
            if (workLeft <= 0)
            {
                workLeft = -1;
                Produce();
            }
        }

        private bool TryTakeMaterials(int steel, int components)
        {
            var cells = GenAdj.CellsAdjacent8Way(this).Where(c => c.InBounds(Map)).ToList();
            int haveSteel = 0, haveComp = 0;
            foreach (IntVec3 c in cells)
            {
                foreach (Thing t in c.GetThingList(Map))
                {
                    if (t.def == ThingDefOf.Steel) haveSteel += t.stackCount;
                    else if (t.def == ThingDefOf.ComponentIndustrial) haveComp += t.stackCount;
                }
            }
            if (haveSteel < steel || haveComp < components)
            {
                return false;
            }
            Take(cells, ThingDefOf.Steel, steel);
            Take(cells, ThingDefOf.ComponentIndustrial, components);
            return true;
        }

        private void Take(List<IntVec3> cells, ThingDef def, int count)
        {
            foreach (IntVec3 c in cells)
            {
                foreach (Thing t in c.GetThingList(Map).ToList())
                {
                    if (count <= 0)
                    {
                        return;
                    }
                    if (t.def != def)
                    {
                        continue;
                    }
                    int n = Mathf.Min(count, t.stackCount);
                    t.SplitOff(n).Destroy();
                    count -= n;
                }
            }
        }

        private void Produce()
        {
            var items = new List<ThingDef>();
            if (product == Product.Rifle || product == Product.FullKit)
            {
                items.Add(HP_DefOf.HP_ConscriptRifle);
            }
            if (product == Product.Armour || product == Product.FullKit)
            {
                items.Add(HP_DefOf.HP_ConscriptHelmet);
                items.Add(HP_DefOf.HP_ConscriptVest);
            }
            foreach (ThingDef def in items)
            {
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(def), InteractionCell, Map, ThingPlaceMode.Near);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Faction != Faction.OfPlayer)
            {
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = "Make: " + ProductLabel(product),
                defaultDesc = "Choose what the line forge makes from the steel and components left next to it.\n\n"
                    + "Rifle: 40 steel, 1 component.\nArmour (helmet and vest): 60 steel.\nFull kit (all three): 100 steel, 1 component.",
                icon = ContentFinder<Texture2D>.Get("Things/Building/HP_LineForge"),
                action = () => Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption(ProductLabel(Product.Rifle), () => { product = Product.Rifle; workLeft = -1; }),
                    new FloatMenuOption(ProductLabel(Product.Armour), () => { product = Product.Armour; workLeft = -1; }),
                    new FloatMenuOption(ProductLabel(Product.FullKit), () => { product = Product.FullKit; workLeft = -1; }),
                    new FloatMenuOption(ProductLabel(Product.Off), () => { product = Product.Off; workLeft = -1; }),
                })),
            };
        }

        private static string ProductLabel(Product p) =>
            p == Product.Rifle ? "conscript rifle" : p == Product.Armour ? "conscript armour" : p == Product.FullKit ? "full conscript kit" : "nothing";

        public override string GetInspectString()
        {
            var sb = new StringBuilder(base.GetInspectString());
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }
            sb.Append("Making: " + ProductLabel(product));
            if (workLeft > 0)
            {
                sb.Append(" (" + workLeft.ToStringTicksToPeriod() + " left)");
            }
            else if (product != Product.Off)
            {
                sb.Append(" (waiting for " + SteelFor(product) + " steel" + (ComponentsFor(product) > 0 ? " and a component" : "") + " next to it)");
            }
            return sb.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref product, "product", Product.Off);
            Scribe_Values.Look(ref workLeft, "workLeft", -1);
        }
    }
}
