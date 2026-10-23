using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HollowPawns
{
    /// <summary>
    /// Ghosts (4.8): a soul still clinging to its body. Rare (one roll per neglected corpse), local
    /// (tethered to that corpse) and ended by dealing with the dead. Lingering ghosts drift and chill
    /// the air; wrathful ones (corrupted ground or a corrupted corpse) hunt souls inside their tether,
    /// through walls but never into warded ground.
    /// </summary>
    public static class Ghosts
    {
        /// <summary>How long a corpse lies unharvested before it rolls for a ghost: about a day.</summary>
        public const int NeglectTicks = GenDate.TicksPerDay;

        /// <summary>Ground at or above this makes the ghost wrathful.</summary>
        public const byte WrathLevel = 60;

        public static bool IsGhost(Pawn p) => p?.def == HP_DefOf.HP_GhostRace;

        /// <summary>The mod setting, or a scenario's own chance if it sets a higher one (the Gravedigger's battlefield).</summary>
        public static float Chance
        {
            get
            {
                float c = HollowPawnsMod.Settings.ghostChance;
                ScenPart_HP_Battlefield field = Find.Scenario?.AllParts.OfType<ScenPart_HP_Battlefield>().FirstOrDefault();
                return field != null ? Mathf.Max(c, field.ghostChance) : c;
            }
        }

        /// <summary>The one roll a neglected, ensouled adult corpse gets. Called from the corruption corpse pass.</summary>
        public static void MaybeRise(Corpse corpse, MapComponent_Corruption c)
        {
            Pawn inner = corpse.InnerPawn;
            if (inner == null || c.ghostRolled.Contains(corpse.thingIDNumber) || corpse.Age < NeglectTicks)
            {
                return;
            }
            if (!SoulUtility.HasSoul(inner))
            {
                // Children, animals, husks: never a ghost, and no point checking again.
                if (!inner.RaceProps.Humanlike || inner.DevelopmentalStage.Adult())
                {
                    c.ghostRolled.Add(corpse.thingIDNumber);
                }
                return;
            }
            c.ghostRolled.Add(corpse.thingIDNumber);
            if (Rand.Chance(Chance))
            {
                Raise(corpse, c);
            }
        }

        public static Pawn Raise(Corpse corpse, MapComponent_Corruption c, bool? forceWrath = null)
        {
            Map map = corpse.Map;
            bool wrathful = forceWrath ?? (SoulUtility.IsCorrupted(corpse.InnerPawn) || c.At(corpse.Position) >= WrathLevel);
            PawnKindDef kind = wrathful ? HP_DefOf.HP_GhostWrathful : HP_DefOf.HP_GhostLingering;
            Pawn ghost = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true));
            string who = corpse.InnerPawn.Name?.ToStringShort ?? corpse.InnerPawn.LabelShort;
            ghost.Name = new NameSingle("ghost of " + who);
            CompGhost g = ghost.TryGetComp<CompGhost>();
            g.corpse = corpse;
            g.wrathful = wrathful;
            g.tether = wrathful ? 6.9f + c.At(corpse.Position) / 40f : 3.9f;
            GenSpawn.Spawn(ghost, corpse.Position, map);
            if (wrathful)
            {
                ghost.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, forced: true, transitionSilently: true);
            }
            Find.LetterStack.ReceiveLetter(wrathful ? "Wrathful ghost" : "Ghost",
                (wrathful
                    ? "The soul of " + who + " has risen from its corpse, and it is angry. It hunts anyone with a soul within about " + Mathf.RoundToInt(g.tether) + " cells of the body, through walls. Its touch chills the soul and ignores armour.\n\nOnly soul weapons, soulsteel blades, the reaper's scythe and fire can hurt it. Better: render, bind, grind, burn or bury the corpse, and the ghost goes with it. Shrines ward the ground against it."
                    : "The soul of " + who + " has risen from its neglected corpse. It drifts near the body, cold and wailing, and does no harm, but nobody likes to see it.\n\nRender, bind, grind, burn or bury the corpse and it goes."),
                wrathful ? LetterDefOf.ThreatSmall : LetterDefOf.NeutralEvent, new LookTargets(ghost, corpse));
            return ghost;
        }

        [DebugAction("Hollow Pawns", "Raise lingering ghost", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DebugRaiseLingering() => DebugRaise(false);

        [DebugAction("Hollow Pawns", "Raise wrathful ghost", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DebugRaiseWrathful() => DebugRaise(true);

        private static void DebugRaise(bool wrathful)
        {
            Map map = Find.CurrentMap;
            Corpse corpse = UI.MouseCell().GetThingList(map).OfType<Corpse>().FirstOrDefault();
            if (corpse == null || !SoulUtility.HasSoul(corpse.InnerPawn))
            {
                Messages.Message("Click an adult human corpse that still has its soul.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            MapComponent_Corruption c = Corruption.For(map);
            c.ghostRolled.Add(corpse.thingIDNumber);
            Raise(corpse, c, wrathful);
        }

        /// <summary>Ground ghosts can't enter: inside a consecrated bound shrine's reach, or a lit, cleansing essence shrine's.</summary>
        public static bool Warded(IntVec3 c, Map map)
        {
            foreach (Building_BoundShrine s in map.listerBuildings.AllBuildingsColonistOfClass<Building_BoundShrine>())
            {
                if (s.Bound && !s.desecrated && c.InHorDistOf(s.Position, s.Radius))
                {
                    return true;
                }
            }
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                CompEssenceShrine e = b.TryGetComp<CompEssenceShrine>();
                if (e != null && !e.spread && (e.Fuel?.HasFuel ?? true) && c.InHorDistOf(b.Position, e.Props.radius))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// What can hurt a ghost: soul weapons, fire, and blades of soulsteel, hellforged blades or the reaper's scythe.
        /// Everything else passes through.
        /// </summary>
        public static bool CanHurt(DamageInfo dinfo) => CanHurt(dinfo.Def, dinfo.Instigator, dinfo.Def != null && !dinfo.Def.isRanged);

        public static bool HurtsGhosts(DamageDef d) =>
            d != null && (d == HP_DefOf.HP_SoulBlast || d == HP_DefOf.HP_HellfireBlast || d == HP_DefOf.HP_SoulBurst || d == HP_DefOf.HP_SoulArrow || d == DamageDefOf.Flame || d == DamageDefOf.Burn);

        public static bool CanHurt(DamageDef d, Thing instigator, bool melee)
        {
            if (HurtsGhosts(d))
            {
                return true;
            }
            if (melee && instigator is Pawn attacker)
            {
                ThingWithComps weapon = attacker.equipment?.Primary;
                if (weapon != null && (weapon.def == HP_DefOf.HP_ReaperScythe || weapon.Stuff == HP_DefOf.HP_Soulsteel || weapon.def.HasModExtension<CorruptingExtension>()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Whether whatever this searcher would attack with can hurt a ghost. Reads the verb's projectile
        /// (vanilla's and CE's launch verbs both have a Projectile property, CE's following the loaded ammo).
        /// Unknown verbs count as able, so nothing is hidden by mistake.
        /// </summary>
        public static bool SearcherCanHurt(IAttackTargetSearcher searcher)
        {
            Verb verb = searcher?.CurrentEffectiveVerb;
            if (verb == null)
            {
                return true;
            }
            if (verb.IsMeleeAttack)
            {
                return CanHurt(null, searcher.Thing, true);
            }
            ThingDef projectile = null;
            try
            {
                projectile = Traverse.Create(verb).Property("Projectile").GetValue<ThingDef>();
            }
            catch
            {
                return true;
            }
            return projectile?.projectile == null || HurtsGhosts(projectile.projectile.damageDef);
        }
    }

    /// <summary>The ghost's tie to its body. Checked every 250 ticks on the ghost itself; nothing else ticks.</summary>
    public class CompGhost : ThingComp
    {
        public Corpse corpse;
        public bool wrathful;
        public float tether = 3.9f;

        private Pawn Ghost => (Pawn)parent;

        /// <summary>Where the body lies (or is held, if someone's carrying it).</summary>
        public IntVec3 Anchor => corpse != null && !corpse.Destroyed ? corpse.PositionHeld : Ghost.Position;

        public bool InTether(IntVec3 c) => c.InHorDistOf(Anchor, tether);

        public override void CompTick()
        {
            Pawn g = Ghost;
            if (!g.Spawned || !g.IsHashIntervalTick(250))
            {
                return;
            }
            string why = null;
            if (corpse == null || corpse.Destroyed)
            {
                why = "its body is gone";
            }
            else if (!SoulUtility.HasSoul(corpse.InnerPawn))
            {
                why = "its soul has been taken";
            }
            else if (corpse.ParentHolder is Building_Grave)
            {
                why = "its body has been laid to rest";
            }
            else if (corpse.MapHeld != g.Map)
            {
                why = "its body was taken away";
            }
            if (why != null)
            {
                Fade(why);
                return;
            }
            // A cold spot: the air around a ghost loses heat.
            GenTemperature.PushHeat(g.Position, g.Map, wrathful ? -12f : -6f);
        }

        public void Fade(string why)
        {
            Pawn g = Ghost;
            Messages.Message("The " + g.LabelShort + " fades: " + why + ".", new TargetInfo(g.Position, g.Map), MessageTypeDefOf.PositiveEvent);
            FleckMaker.ThrowSmoke(g.DrawPos, g.Map, 1.5f);
            g.Destroy();
        }

        public override string CompInspectStringExtra() => (wrathful ? "Wrathful. Hunts souls" : "Lingering. Drifts") + " within " + tether.ToString("0") + " cells of its body.";

        public override void PostExposeData()
        {
            Scribe_References.Look(ref corpse, "corpse");
            Scribe_Values.Look(ref wrathful, "wrathful");
            Scribe_Values.Look(ref tether, "tether", 3.9f);
        }
    }

    /// <summary>The ghost's only brain: hunt a soul in reach if wrathful, otherwise drift about the body.</summary>
    public class JobGiver_Ghost : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            CompGhost g = pawn.TryGetComp<CompGhost>();
            if (g == null)
            {
                return null;
            }
            if (g.wrathful)
            {
                Pawn prey = null;
                float best = float.MaxValue;
                foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
                {
                    if (p == pawn || p.Dead || Ghosts.IsGhost(p) || !SoulUtility.HasSoul(p) || !g.InTether(p.Position) || Ghosts.Warded(p.Position, p.Map))
                    {
                        continue;
                    }
                    float d = p.Position.DistanceToSquared(pawn.Position);
                    if (d < best)
                    {
                        best = d;
                        prey = p;
                    }
                }
                if (prey != null)
                {
                    Job hunt = JobMaker.MakeJob(HP_DefOf.HP_GhostDrift, prey);
                    hunt.expiryInterval = 600;
                    return hunt;
                }
            }
            IntVec3 anchor = g.Anchor;
            for (int i = 0; i < 8; i++)
            {
                IntVec3 c = anchor + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(Mathf.Min(g.tether, 3.9f)))];
                if (c.InBounds(pawn.Map) && !Ghosts.Warded(c, pawn.Map))
                {
                    Job drift = JobMaker.MakeJob(HP_DefOf.HP_GhostDrift, c);
                    drift.expiryInterval = 600;
                    return drift;
                }
            }
            return JobMaker.MakeJob(JobDefOf.Wait, 120);
        }
    }

    /// <summary>
    /// A ghost's movement: one cell at a time in a straight line, through walls and doors, never into
    /// warded ground or past its tether. The vanilla pathfinder is never asked. Adjacent to its prey, it touches.
    /// </summary>
    public class JobDriver_GhostDrift : JobDriver
    {
        private int ticks;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil move = ToilMaker.MakeToil("HP_GhostDrift");
            move.defaultCompleteMode = ToilCompleteMode.Never;
            move.tickAction = () =>
            {
                ticks++;
                CompGhost g = pawn.TryGetComp<CompGhost>();
                LocalTargetInfo t = job.targetA;
                if (g == null || !t.IsValid || ticks > 600 || (t.HasThing && (!t.Thing.Spawned || t.Thing.Map != pawn.Map)))
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                IntVec3 goal = t.Cell;
                if (t.Thing is Pawn prey)
                {
                    if (prey.Dead || !g.InTether(prey.Position) || Ghosts.Warded(prey.Position, pawn.Map))
                    {
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }
                    if (pawn.Position.AdjacentTo8Way(prey.Position))
                    {
                        pawn.rotationTracker.FaceTarget(prey);
                        if (ticks % 90 == 0)
                        {
                            pawn.meleeVerbs.TryMeleeAttack(prey);
                        }
                        return;
                    }
                }
                else if (pawn.Position == goal)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                if (ticks % (g.wrathful ? 18 : 40) != 0)
                {
                    return;
                }
                IntVec3 d = goal - pawn.Position;
                IntVec3 next = pawn.Position + new IntVec3(System.Math.Sign(d.x), 0, System.Math.Sign(d.z));
                if (!next.InBounds(pawn.Map) || !g.InTether(next) || Ghosts.Warded(next, pawn.Map) || next.GetFirstPawn(pawn.Map) != null)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                pawn.rotationTracker.FaceCell(next);
                pawn.Position = next;
            };
            yield return move;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticks, "ticks");
        }
    }

    /// <summary>Grave chill: a ghost's touch, on the soul and not the body. Armour doesn't help; the soulless don't feel it.</summary>
    public class DamageWorker_GraveChill : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            var result = new DamageResult();
            if (victim is Pawn p && !p.Dead && SoulUtility.HasSoul(p))
            {
                HealthUtility.AdjustSeverity(p, HP_DefOf.HP_GraveChill, dinfo.Amount * 0.01f);
                Corruption.AddToPawn(p, 0.01f);
                FleckMaker.ThrowDustPuffThick(p.DrawPos, p.Map, 0.8f, new Color(0.7f, 0.85f, 1f));
            }
            return result;
        }
    }

    /// <summary>Grave chill thaws slowly on its own, and much faster in a warm, warded place.</summary>
    public class Hediff_GraveChill : HediffWithComps
    {
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!pawn.IsHashIntervalTick(250, delta) || !pawn.Spawned)
            {
                return;
            }
            float thaw = 0.004f;
            if (pawn.AmbientTemperature > 15f && Ghosts.Warded(pawn.Position, pawn.Map))
            {
                thaw = 0.03f;
            }
            Severity -= thaw;
        }
    }

    /// <summary>Most damage passes straight through a ghost.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class Patch_Pawn_PreApplyDamage_Ghost
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, ref bool absorbed)
        {
            if (!absorbed && Ghosts.IsGhost(__instance) && !Ghosts.CanHurt(dinfo))
            {
                absorbed = true;
            }
        }
    }

    /// <summary>
    /// Shooters and turrets don't pick a ghost as a target if their weapon can't hurt it (no wasted
    /// ammo). The player can still order an attack by hand.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ThreatDisabled))]
    public static class Patch_Pawn_ThreatDisabled_Ghost
    {
        public static void Postfix(Pawn __instance, IAttackTargetSearcher disabledFor, ref bool __result)
        {
            if (!__result && disabledFor != null && Ghosts.IsGhost(__instance) && !Ghosts.SearcherCanHurt(disabledFor))
            {
                __result = true;
            }
        }
    }

    /// <summary>Vanilla projectiles that can't hurt a ghost fly straight through it, on to whatever's behind.</summary>
    [HarmonyPatch(typeof(Projectile), "CanHit")]
    public static class Patch_Projectile_CanHit_Ghost
    {
        public static void Postfix(Projectile __instance, Thing thing, ref bool __result)
        {
            if (__result && thing is Pawn p && Ghosts.IsGhost(p) && !Ghosts.HurtsGhosts(__instance.def.projectile?.damageDef))
            {
                __result = false;
            }
        }
    }

    /// <summary>The same for Combat Extended's 3D projectiles, patched by hand only when CE is loaded.</summary>
    [StaticConstructorOnStartup]
    public static class GhostPassThroughCE
    {
        static GhostPassThroughCE()
        {
            System.Type ce = AccessTools.TypeByName("CombatExtended.ProjectileCE");
            var m = ce == null ? null : AccessTools.Method(ce, "CanCollideWith");
            if (m != null)
            {
                new Harmony("aaronclews.hollowpawns.ghostce").Patch(m, postfix: new HarmonyMethod(typeof(GhostPassThroughCE), nameof(Postfix)));
            }
        }

        public static void Postfix(Thing __instance, Thing thing, ref bool __result)
        {
            if (__result && thing is Pawn p && Ghosts.IsGhost(p))
            {
                DamageDef d = Traverse.Create(__instance).Property("DamageDef").GetValue<DamageDef>();
                if (!Ghosts.HurtsGhosts(d))
                {
                    __result = false;
                }
            }
        }
    }

    /// <summary>A destroyed ghost leaves nothing behind.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_Ghost
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.Dead && Ghosts.IsGhost(__instance))
            {
                Corpse c = __instance.Corpse;
                if (c != null && !c.Destroyed)
                {
                    if (c.Spawned)
                    {
                        FleckMaker.ThrowSmoke(c.DrawPos, c.Map, 2f);
                    }
                    c.Destroy();
                }
            }
        }
    }

    /// <summary>Dread: a ghost in sight.</summary>
    public class ThoughtWorker_SawGhost : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned)
            {
                return ThoughtState.Inactive;
            }
            foreach (Pawn g in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (Ghosts.IsGhost(g) && p.Position.InHorDistOf(g.Position, 10f) && GenSight.LineOfSight(p.Position, g.Position, p.Map))
                {
                    return ThoughtState.ActiveAtStage(g.TryGetComp<CompGhost>()?.wrathful == true ? 1 : 0);
                }
            }
            return ThoughtState.Inactive;
        }
    }
}
