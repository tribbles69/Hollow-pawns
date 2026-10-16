using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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
    /// while growing; idle otherwise. Takes no new bill while it's growing. Grows faster with someone
    /// tending it (Staffing.cs, Medicine skill).
    /// </summary>
    public class Building_CloneVat : Building_WorkTable, IBillGiver, IStaffable
    {
        public Staffing Staffing { get; } = new Staffing(SkillDefOf.Medicine);

        public bool WantsOperator => Spawned && Growing && (GetComp<CompPowerTrader>()?.PowerOn ?? true);

        public const float GrowDays = 3f;

        private int growTicksLeft = -1;

        public bool Growing => growTicksLeft > 0;

        // Re-implements IBillGiver: Building_WorkTable's versions aren't virtual, and callers go through the interface.
        // WorkGiver_DoBill only checks UsableForBillsAfterFueling while JobDriver_DoBill checks CurrentlyUsableForBills,
        // so both must refuse while growing, or workers take the bill and drop it in a loop.
        bool IBillGiver.CurrentlyUsableForBills() => !Growing && base.CurrentlyUsableForBills();
        bool IBillGiver.UsableForBillsAfterFueling() => !Growing && base.UsableForBillsAfterFueling();

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
            growTicksLeft -= Mathf.RoundToInt(GenTicks.TickRareInterval * Staffing.Factor);
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
                sb.AppendLine();
                sb.Append(Staffing.InspectLine());
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
    /// goes. Never children. Rare tick, one corpse per cycle, only while powered. Runs faster with
    /// someone on cooking duty working it (Staffing.cs).
    /// </summary>
    public class Building_CorpseGrinder : Building, IStaffable
    {
        public const int TicksPerCorpse = 1000;
        public const int EssencePerSoul = 4;

        private int cooldown;

        public Staffing Staffing { get; } = new Staffing(SkillDefOf.Cooking);

        private bool Powered => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        public bool WantsOperator => Spawned && Powered && NextCorpse() != null;

        public override void TickRare()
        {
            base.TickRare();
            if (!Powered)
            {
                return;
            }
            cooldown -= Mathf.RoundToInt(GenTicks.TickRareInterval * Staffing.Factor);
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

        /// <summary>Bales from one body: two for a human, by body size for animals (1 to 6). A bale renders to 46 meat.</summary>
        public static int CarrionBalesFor(Pawn inner) =>
            inner.RaceProps.Humanlike ? 2 : Mathf.Clamp(Mathf.RoundToInt(inner.BodySize * 2f), 1, 6);

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
            bale.stackCount = CarrionBalesFor(inner);
            GenPlace.TryPlaceThing(bale, output, map, ThingPlaceMode.Near);
            // Corpse.ButcherProducts needs a butcher pawn (blood filth at their feet, butchered-human
            // history event), so with no pawn it throws. Take the inner pawn's products directly.
            if (inner.RaceProps.BloodDef != null)
            {
                FilthMaker.TryMakeFilth(output, map, inner.RaceProps.BloodDef, inner.LabelIndefinite());
            }
            foreach (Thing product in inner.ButcherProducts(null, 0.6f).ToList())
            {
                GenPlace.TryPlaceThing(product, output, map, ThingPlaceMode.Near);
            }
            corpse.Destroy();
            Corruption.Spill(Position, map, 60, 4.9f);
        }

        public override string GetInspectString()
        {
            string b = base.GetInspectString();
            string line = Staffing.InspectLine();
            return b.NullOrEmpty() ? line : b + "\n" + line;
        }
    }

    /// <summary>Bind carrion yields bales by body size (as the grinder does), not a flat two, so small animals can't be turned into a human's worth of meat.</summary>
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_GenRecipe_BindCarrion
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients)
        {
            Pawn inner = recipeDef == HP_DefOf.HP_BindCarrion ? ingredients?.OfType<Corpse>().FirstOrDefault()?.InnerPawn : null;
            foreach (Thing t in __result)
            {
                if (inner != null && t.def == HP_DefOf.HP_CarrionBale)
                {
                    t.stackCount = Building_CorpseGrinder.CarrionBalesFor(inner);
                }
                yield return t;
            }
        }
    }

    // ======================= Line forge (4.24) =======================

    /// <summary>
    /// Turns steel and components lying next to it into cheap conscript kit, with no worker and no
    /// essence. Pick what it makes; it pulls materials from adjacent cells and drops kit on its
    /// output cell. Rare tick, and only works while powered and fed.
    /// </summary>
    public class Building_LineForge : Building, IStaffable
    {
        public Staffing Staffing { get; } = new Staffing(SkillDefOf.Crafting);

        private bool Powered => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        /// <summary>Mid-piece, or ready to start one: an operator would speed it up.</summary>
        public bool WantsOperator => Spawned && Powered && product != Product.Off
            && (workLeft > 0 || (!StockFull() && HasMaterials(SteelFor(product), ComponentsFor(product))));

        public enum Product : byte { Off, Rifle, Armour, FullKit }

        private Product product = Product.Off;
        private int workLeft = -1;
        /// <summary>Stop starting new pieces once this many of each item lie on the map (0 = no limit). Worn or carried kit isn't counted.</summary>
        private int stopAt = 0;
        private static readonly int[] StopAtOptions = { 0, 5, 10, 20, 30, 50, 100 };

        private static int SteelFor(Product p) => p == Product.Rifle ? 40 : p == Product.Armour ? 60 : p == Product.FullKit ? 140 : 0;
        private static int ComponentsFor(Product p) => p == Product.Rifle || p == Product.FullKit ? 1 : 0;
        private static int TicksFor(Product p) => p == Product.FullKit ? 6000 : 2500;

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
                if (StockFull() || !TryTakeMaterials(SteelFor(product), ComponentsFor(product)))
                {
                    return;
                }
                workLeft = TicksFor(product);
            }
            workLeft -= Mathf.RoundToInt(GenTicks.TickRareInterval * Staffing.Factor);
            if (workLeft <= 0)
            {
                workLeft = -1;
                Produce();
            }
        }

        private IEnumerable<ThingDef> Items(Product p)
        {
            if (p == Product.Rifle || p == Product.FullKit)
            {
                yield return HP_DefOf.HP_ConscriptRifle;
            }
            if (p == Product.Armour)
            {
                yield return HP_DefOf.HP_ConscriptHelmet;
                yield return HP_DefOf.HP_ConscriptVest;
            }
            if (p == Product.FullKit)
            {
                // Every piece of the conscript kit (4.22 generator), so a full kit fills every slot.
                foreach (ThingDef d in ConscriptKit.Pieces)
                {
                    yield return d;
                }
            }
        }

        /// <summary>Fewest of any item this product makes lying spawned on the map (stockpiles and shelves included).</summary>
        private int OnHand() => Items(product).Select(d => Map.listerThings.ThingsOfDef(d).Count).DefaultIfEmpty(0).Min();

        private bool StockFull() => stopAt > 0 && OnHand() >= stopAt;

        private bool HasMaterials(int steel, int components)
        {
            int haveSteel = 0, haveComp = 0;
            foreach (IntVec3 c in GenAdj.CellsAdjacent8Way(this))
            {
                if (!c.InBounds(Map))
                {
                    continue;
                }
                foreach (Thing t in c.GetThingList(Map))
                {
                    if (t.def == ThingDefOf.Steel) haveSteel += t.stackCount;
                    else if (t.def == ThingDefOf.ComponentIndustrial) haveComp += t.stackCount;
                }
            }
            return haveSteel >= steel && haveComp >= components;
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
            foreach (ThingDef def in Items(product))
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
                    + "Rifle: 40 steel, 1 component.\nArmour (helmet and vest): 60 steel.\nFull kit (rifle and the whole conscript kit: fatigues, vest, greatcoat, helmet, belt, goggles): 140 steel, 1 component.",
                icon = ContentFinder<Texture2D>.Get("Things/Building/HP_LineForge"),
                action = () => Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption(ProductLabel(Product.Rifle), () => { product = Product.Rifle; workLeft = -1; }),
                    new FloatMenuOption(ProductLabel(Product.Armour), () => { product = Product.Armour; workLeft = -1; }),
                    new FloatMenuOption(ProductLabel(Product.FullKit), () => { product = Product.FullKit; workLeft = -1; }),
                    new FloatMenuOption(ProductLabel(Product.Off), () => { product = Product.Off; workLeft = -1; }),
                })),
            };
            yield return new Command_Action
            {
                defaultLabel = "Stop at: " + StopAtLabel(stopAt),
                defaultDesc = "Pause once this many of each piece it makes are lying around the colony (stockpiles and shelves count; kit being worn or carried doesn't). It starts again when the stock drops below.",
                icon = ContentFinder<Texture2D>.Get("Things/Building/HP_LineForge"),
                action = () => Find.WindowStack.Add(new FloatMenu(StopAtOptions
                    .Select(n => new FloatMenuOption(StopAtLabel(n), () => stopAt = n)).ToList())),
            };
            yield return new Command_Action
            {
                defaultLabel = "Kit out thralls",
                defaultDesc = "Put every thrall on this map on the Conscript kit outfit, so they dress themselves in conscript kit, and send every thrall not already carrying a conscript rifle to swap for one (they drop what they had).",
                icon = ContentFinder<Texture2D>.Get("Things/Building/HP_LineForge"),
                action = () => ConscriptKit.KitOutThralls(Map),
            };
        }

        private static string StopAtLabel(int n) => n == 0 ? "no limit" : n.ToString();

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
            if (stopAt > 0)
            {
                sb.Append(", stop at " + stopAt);
            }
            if (workLeft > 0)
            {
                sb.Append(" (" + workLeft.ToStringTicksToPeriod() + " left)");
            }
            else if (product != Product.Off && Spawned && StockFull())
            {
                sb.Append(" (paused: " + OnHand() + " on hand)");
            }
            else if (product != Product.Off)
            {
                sb.Append(" (waiting for " + SteelFor(product) + " steel" + (ComponentsFor(product) > 0 ? " and a component" : "") + " in hoppers next to it)");
            }
            if (product != Product.Off)
            {
                sb.AppendLine();
                sb.Append(Staffing.InspectLine());
            }
            return sb.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref product, "product", Product.Off);
            Scribe_Values.Look(ref workLeft, "workLeft", -1);
            Scribe_Values.Look(ref stopAt, "stopAt", 0);
        }
    }

    /// <summary>
    /// The conscript kit (4.24): its pieces (read from the apparel-set extension the generator writes),
    /// the Conscript kit outfit policy, and one-click kitting for thralls.
    /// </summary>
    public static class ConscriptKit
    {
        public const string Set = "Conscript";
        public const string PolicyLabel = "Conscript kit";

        public static IEnumerable<ThingDef> Pieces =>
            ApparelSets.Required.TryGetValue(Set, out HashSet<ThingDef> defs) ? defs.OrderBy(d => d.defName) : Enumerable.Empty<ThingDef>();

        /// <summary>Makes sure the outfit policy exists, allowing exactly the kit's pieces. Called on every game load.</summary>
        public static ApparelPolicy EnsurePolicy()
        {
            OutfitDatabase db = Current.Game?.outfitDatabase;
            if (db == null)
            {
                return null;
            }
            ApparelPolicy policy = db.AllOutfits.FirstOrDefault(o => o.label == PolicyLabel);
            if (policy == null)
            {
                policy = db.MakeNewOutfit();
                policy.label = PolicyLabel;
                policy.filter.SetDisallowAll();
                foreach (ThingDef d in Pieces)
                {
                    policy.filter.SetAllow(d, true);
                }
            }
            return policy;
        }

        public static bool IsThrall(Pawn p) => p.Faction == Faction.OfPlayer && SoulUtility.IsSoulless(p) && p.RaceProps.Humanlike;

        public static void KitOutThralls(Map map)
        {
            ApparelPolicy policy = EnsurePolicy();
            int dressed = 0, armed = 0;
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned.ToList())
            {
                if (!IsThrall(p) || p.outfits == null)
                {
                    continue;
                }
                if (policy != null)
                {
                    p.outfits.CurrentApparelPolicy = policy;
                    dressed++;
                }
                // Whatever they came with (vat-grown thralls arrive with a random gun) goes for a conscript rifle; Equip drops the old one.
                if (p.equipment != null && p.equipment.Primary?.def != HP_DefOf.HP_ConscriptRifle && !p.Downed && !p.Drafted)
                {
                    Thing rifle = GenClosest.ClosestThingReachable(p.Position, map, ThingRequest.ForDef(HP_DefOf.HP_ConscriptRifle), PathEndMode.ClosestTouch,
                        TraverseParms.For(p), 9999f, t => !t.IsForbidden(p) && p.CanReserve(t));
                    if (rifle != null)
                    {
                        p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, rifle), JobTag.Misc);
                        armed++;
                    }
                }
            }
            Messages.Message(dressed == 0 ? "No thralls here to kit out."
                : dressed + " thrall" + (dressed == 1 ? "" : "s") + " put on the Conscript kit outfit; " + armed + " sent for a rifle.",
                MessageTypeDefOf.TaskCompletion, false);
        }
    }

    public class GameComponent_ConscriptKit : GameComponent
    {
        public GameComponent_ConscriptKit(Game game)
        {
        }

        public override void FinalizeInit()
        {
            ConscriptKit.EnsurePolicy();
        }
    }
}
