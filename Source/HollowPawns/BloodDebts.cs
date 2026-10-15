using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HollowPawns
{
    // ======================= 1. The blood debt =======================

    /// <summary>
    /// The mark on an enemy who killed one of your colonists (4.18). Names who they killed and
    /// remembers everyone who cared about those victims (the avengers). Lives on the killer, so it
    /// goes wherever they go, including back to the world if they escape. No effect on its own.
    /// </summary>
    public class Hediff_BloodDebt : HediffWithComps
    {
        public List<string> victims = new List<string>();
        public List<int> avengers = new List<int>();

        public override string LabelInBrackets => string.Join(", ", victims);

        public override bool ShouldRemove => false;

        public void Add(string victim, IEnumerable<int> mourners)
        {
            if (!victims.Contains(victim))
            {
                victims.Add(victim);
            }
            foreach (int id in mourners)
            {
                if (!avengers.Contains(id))
                {
                    avengers.Add(id);
                }
            }
        }

        public bool IsAvenger(Pawn p) => p != null && avengers.Contains(p.thingIDNumber);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref victims, "victims", LookMode.Value);
            Scribe_Collections.Look(ref avengers, "avengers", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                victims = victims ?? new List<string>();
                avengers = avengers ?? new List<int>();
            }
        }
    }

    public static class BloodDebts
    {
        public static HediffDef DebtDef => HP_DefOf.HP_BloodDebt;

        public static Hediff_BloodDebt DebtOn(Pawn p) => p?.health?.hediffSet?.GetFirstHediffOfDef(DebtDef) as Hediff_BloodDebt;

        public static bool IsDebtor(Pawn p) => DebtOn(p) != null;

        /// <summary>Records the debt on the killer. Called when an enemy kills a colonist.</summary>
        public static void Record(Pawn killer, Pawn victim, List<int> mourners)
        {
            Hediff_BloodDebt debt = DebtOn(killer);
            if (debt == null)
            {
                debt = (Hediff_BloodDebt)HediffMaker.MakeHediff(DebtDef, killer);
                killer.health.AddHediff(debt);
            }
            debt.Add(victim.LabelShort, mourners);
            Messages.Message(killer.LabelShort + " killed " + victim.LabelShort + " and now carries a blood debt. Down them; don't kill them.",
                killer, MessageTypeDefOf.NegativeEvent, false);
        }

        /// <summary>Avengers of anyone tormented or avenged: their mood follows the debtor's fate.</summary>
        public static IEnumerable<Pawn> AvengersOnMap(Hediff_BloodDebt debt, Map map)
        {
            if (debt == null || map == null)
            {
                yield break;
            }
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (debt.IsAvenger(p))
                {
                    yield return p;
                }
            }
        }
    }

    /// <summary>When an enemy kills a colonist, the killer takes on a blood debt.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_BloodDebt
    {
        public static void Prefix(Pawn __instance, DamageInfo? dinfo, out List<int> __state)
        {
            __state = null;
            if (!__instance.Dead && __instance.IsColonist && dinfo?.Instigator is Pawn killer && killer.RaceProps.Humanlike
                && killer.Faction != Faction.OfPlayer && killer != __instance)
            {
                // Taken before death, while opinions of the victim still mean something.
                __state = BoundSoul.LovedBy(__instance);
            }
        }

        public static void Postfix(Pawn __instance, DamageInfo? dinfo, List<int> __state)
        {
            if (__state != null && __instance.Dead && dinfo?.Instigator is Pawn killer && !killer.Dead)
            {
                BloodDebts.Record(killer, __instance, __state);
            }
            if (__instance.Dead)
            {
                Profaned.OnPawnDied(__instance);
            }
        }
    }

    /// <summary>A marked killer who escapes is kept in the world, so they can be met again.</summary>
    [HarmonyPatch(typeof(WorldPawnGC), "GetCriticalPawnReason")]
    public static class Patch_WorldPawnGC_BloodDebt
    {
        public static void Postfix(Pawn pawn, ref string __result)
        {
            if (__result == null && pawn != null && !pawn.Dead && BloodDebts.IsDebtor(pawn))
            {
                __result = "HollowPawnsBloodDebt";
            }
        }
    }

    /// <summary>"Blood debt: Jeb, Ann" on a debtor's inspect pane, so it shows in a fight.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public static class Patch_Pawn_GetInspectString_BloodDebt
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            Hediff_BloodDebt debt = BloodDebts.DebtOn(__instance);
            if (debt != null)
            {
                string line = "Blood debt: killed " + string.Join(", ", debt.victims) + ".";
                __result = __result.NullOrEmpty() ? line : __result.TrimEnd() + "\n" + line;
            }
        }
    }

    // ======================= 2. Torment =======================

    /// <summary>How tormented a prisoner is, and when the last session was. Pain rises with severity.</summary>
    public class Hediff_Tormented : HediffWithComps
    {
        public int lastSessionTick = -999999;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastSessionTick, "lastSessionTick", -999999);
        }
    }

    public static class Torment
    {
        /// <summary>At most one session every half day per prisoner.</summary>
        public const int Cooldown = GenDate.TicksPerDay / 2;

        public static bool Due(Pawn prisoner)
        {
            var h = prisoner.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_Tormented) as Hediff_Tormented;
            return h == null || Find.TickManager.TicksGame - h.lastSessionTick >= Cooldown;
        }

        /// <summary>The end of a session: pain and soul shock for the prisoner, mood for everyone else.</summary>
        public static void Session(Pawn warden, Pawn prisoner)
        {
            var h = prisoner.health.hediffSet.GetFirstHediffOfDef(HP_DefOf.HP_Tormented) as Hediff_Tormented;
            if (h == null)
            {
                h = (Hediff_Tormented)HediffMaker.MakeHediff(HP_DefOf.HP_Tormented, prisoner);
                h.Severity = 0f;
                prisoner.health.AddHediff(h);
            }
            h.Severity = Mathf.Min(1f, h.Severity + 0.15f);
            h.lastSessionTick = Find.TickManager.TicksGame;
            HealthUtility.AdjustSeverity(prisoner, HP_DefOf.HP_SoulShock, 0.25f);

            Hediff_BloodDebt debt = BloodDebts.DebtOn(prisoner);
            foreach (Pawn avenger in BloodDebts.AvengersOnMap(debt, prisoner.MapHeld))
            {
                avenger.needs?.mood?.thoughts?.memories?.TryGainMemory(HP_DefOf.HP_Avenging);
            }
            foreach (Pawn p in prisoner.MapHeld.mapPawns.FreeColonistsSpawned)
            {
                if (p != warden && p.story?.traits?.HasTrait(TraitDefOf.Kind) == true)
                {
                    p.needs?.mood?.thoughts?.memories?.TryGainMemory(HP_DefOf.HP_TormentDisturbed);
                }
            }
            Faction home = prisoner.HomeFaction;
            if (home != null && home != Faction.OfPlayer && !home.IsPlayer)
            {
                home.TryAffectGoodwillWith(Faction.OfPlayer, -15, true, !home.HostileTo(Faction.OfPlayer), HistoryEventDefOf.UsedHarmfulAbility);
            }
            Corruption.AddToPawn(warden, 0.02f);
        }
    }

    /// <summary>Wardens give prisoners set to Torment a session, at most every half day.</summary>
    public class WorkGiver_Warden_Torment : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.mapPawns.PrisonersOfColonySpawned;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.mapPawns.PrisonersOfColonySpawned.Any(p => p.guest.IsInteractionEnabled(HP_DefOf.HP_Torment));
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn prisoner) || !prisoner.IsPrisonerOfColony || prisoner.Dead || !prisoner.guest.IsInteractionEnabled(HP_DefOf.HP_Torment)
                || !Torment.Due(prisoner) || !pawn.CanReserveAndReach(prisoner, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
            {
                return null;
            }
            return JobMaker.MakeJob(HP_DefOf.HP_TormentPrisoner, prisoner);
        }
    }

    public class JobDriver_Torment : JobDriver
    {
        private Pawn Prisoner => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Prisoner.IsPrisonerOfColony || !Prisoner.guest.IsInteractionEnabled(HP_DefOf.HP_Torment));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil work = Toils_General.Wait(900, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            work.PlaySustainerOrSound(SoundDefOf.Recipe_Surgery);
            yield return work;
            Toil done = ToilMaker.MakeToil("HP_TormentDone");
            done.initAction = () => Torment.Session(pawn, Prisoner);
            done.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return done;
        }
    }

    // ======================= 3. The effigies =======================

    public class CompProperties_Effigy : CompProperties
    {
        /// <summary>The wailing effigy holds a living soul and screams.</summary>
        public bool wailing;

        public CompProperties_Effigy()
        {
            compClass = typeof(CompEffigy);
        }
    }

    /// <summary>
    /// A vengeance effigy (4.18). Holds the debtor's soul (its lovedBy list holds the avengers) and the
    /// names of the colonists they killed. Avengers who see it are Avenged; a wailing one also unsettles
    /// everyone else and screams when it matters. Its screams are event-driven (raid arrival) or a slow
    /// check on its own rare tick. Nothing else ticks.
    /// </summary>
    public class CompEffigy : ThingComp
    {
        public BoundSoul soul;
        public List<string> victims = new List<string>();
        private int lastScreamTick = -999999;
        private int lastNightScreamDay = -1;

        public CompProperties_Effigy Props => (CompProperties_Effigy)props;

        public bool Wailing => Props.wailing;

        public void Fill(BoundSoul s, IEnumerable<string> killed)
        {
            soul = s;
            victims = killed?.ToList() ?? new List<string>();
        }

        public bool IsAvenger(Pawn p) => soul?.IsLovedBy(p) ?? false;

        private string Line => soul == null ? null
            : "A soulsteel figure of " + soul.ShortName + ", " + (Wailing ? "bound alive" : "cut down") + " on " + soul.dateTaken
              + (victims.Count > 0 ? " for the murder of " + victims.ToCommaList(useAnd: true) : "") + "."
              + (Wailing ? " The soul is still in it, and it screams." : "");

        public override string TransformLabel(string label) => soul == null ? label : label + " (" + soul.ShortName + ")";

        public override string CompInspectStringExtra() => Line;

        public override string GetDescriptionPart() => Line;

        public override void CompTickRare()
        {
            if (!Wailing || soul == null || !parent.Spawned)
            {
                return;
            }
            Map map = parent.Map;
            int now = Find.TickManager.TicksGame;
            // At night, once.
            int hour = GenLocalDate.HourOfDay(map);
            int day = GenLocalDate.DayOfYear(map) + GenLocalDate.Year(map) * 60;
            if (hour == 1 && day != lastNightScreamDay)
            {
                lastNightScreamDay = day;
                Scream(false);
                return;
            }
            // Someone walking past, now and then.
            if (now - lastScreamTick > GenDate.TicksPerHour * 2 && Rand.Chance(0.15f))
            {
                foreach (IntVec3 c in GenRadial.RadialCellsAround(parent.Position, 2.9f, false))
                {
                    if (c.InBounds(map) && c.GetFirstPawn(map) != null)
                    {
                        Scream(false);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The wail: enemies nearby are shocked and may break and run; colonists who aren't avengers
        /// are unsettled. Raiders hear it as they come in.
        /// </summary>
        public void Scream(bool raid)
        {
            Map map = parent.Map;
            lastScreamTick = Find.TickManager.TicksGame;
            HP_DefOf.HP_EffigyWail?.PlayOneShot(new TargetInfo(parent.Position, map));
            MoteMaker.ThrowText(parent.DrawPos, map, "screams", new Color(0.8f, 0.85f, 1f));
            float reach = raid ? 30f : 20f;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned.ToList())
            {
                if (p.Dead || !p.RaceProps.Humanlike || !p.Position.InHorDistOf(parent.Position, reach))
                {
                    continue;
                }
                if (p.HostileTo(Faction.OfPlayer))
                {
                    HealthUtility.AdjustSeverity(p, HP_DefOf.HP_SoulShock, 0.3f);
                    if (!p.Downed && !p.InMentalState && Rand.Chance(0.25f))
                    {
                        p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, "screaming effigy", forced: true);
                    }
                }
                else if (p.IsColonist && !IsAvenger(p) && p.Position.InHorDistOf(parent.Position, 15f))
                {
                    p.needs?.mood?.thoughts?.memories?.TryGainMemory(HP_DefOf.HP_HeardEffigyScream);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!Wailing || soul == null || parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = "Shatter",
                defaultDesc = "Break the effigy and release the soul of " + soul.ShortName + " as " + ShatterYield + " essence. The avengers lose their monument.",
                icon = ReapersMark.MarkIcon,
                action = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("Shatter the effigy of " + soul.ShortName + "?", Shatter, destructive: true)),
            };
        }

        private int ShatterYield => SoulGrades.SmashYield(soul.grade) * 2;

        private void Shatter()
        {
            if (!parent.Spawned)
            {
                return;
            }
            IntVec3 cell = parent.Position;
            Map map = parent.Map;
            parent.Destroy(DestroyMode.Vanish);
            SoulUtility.DropEssence(cell, map, ShatterYield, soul.corrupted ? HP_DefOf.HP_CorruptedEssence : HP_DefOf.HP_SoulEssence);
            Corruption.Spill(cell, map, 120, 3.9f);
        }

        public override void PostExposeData()
        {
            Scribe_Deep.Look(ref soul, "soul");
            Scribe_Collections.Look(ref victims, "victims", LookMode.Value);
            Scribe_Values.Look(ref lastScreamTick, "lastScreamTick", -999999);
            Scribe_Values.Look(ref lastNightScreamDay, "lastNightScreamDay", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && victims == null)
            {
                victims = new List<string>();
            }
        }
    }

    public static class Effigies
    {
        /// <summary>The rite's ending for a debtor marked "effigy": their living soul goes into a wailing effigy.</summary>
        public static Thing MakeWailing(Pawn warden, Pawn victim, SoulGrade grade)
        {
            Thing effigy = ThingMaker.MakeThing(HP_DefOf.HP_WailingEffigy);
            Hediff_BloodDebt debt = BloodDebts.DebtOn(victim);
            BoundSoul soul = BoundSoul.From(victim, grade);
            soul.lovedBy = debt?.avengers?.ToList() ?? new List<int>();
            effigy.TryGetComp<CompEffigy>()?.Fill(soul, debt?.victims);
            effigy.TryGetComp<CompQuality>()?.SetQuality(QualityUtility.GenerateQualityCreatedByPawn(warden, SkillDefOf.Artistic), ArtGenerationContext.Colony);
            foreach (Pawn avenger in BloodDebts.AvengersOnMap(debt, victim.MapHeld))
            {
                avenger.needs?.mood?.thoughts?.memories?.TryGainMemory(HP_DefOf.HP_AvengedMemory);
            }
            return effigy.MakeMinified();
        }

        public static IEnumerable<CompEffigy> On(Map map)
        {
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                CompEffigy e = b.TryGetComp<CompEffigy>();
                if (e?.soul != null)
                {
                    yield return e;
                }
            }
        }
    }

    /// <summary>Carving the dead debtor: the effigy takes the corpse's soul and the debt's names.</summary>
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_GenRecipe_VengeanceEffigy
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients)
        {
            Corpse corpse = recipeDef.defName == "HP_CarveVengeanceEffigy" ? ingredients?.OfType<Corpse>().FirstOrDefault() : null;
            foreach (Thing t in __result)
            {
                if (corpse != null)
                {
                    Thing inner = t is MinifiedThing m ? m.InnerThing : t;
                    CompEffigy e = inner.TryGetComp<CompEffigy>();
                    if (e != null)
                    {
                        Pawn dead = corpse.InnerPawn;
                        Hediff_BloodDebt debt = BloodDebts.DebtOn(dead);
                        BoundSoul soul = BoundSoul.From(dead, SoulGrades.Of(dead));
                        soul.lovedBy = debt?.avengers?.ToList() ?? new List<int>();
                        e.Fill(soul, debt?.victims);
                        foreach (Pawn avenger in BloodDebts.AvengersOnMap(debt, corpse.MapHeld))
                        {
                            avenger.needs?.mood?.thoughts?.memories?.TryGainMemory(HP_DefOf.HP_AvengedMemory);
                        }
                    }
                }
                yield return t;
            }
        }
    }

    /// <summary>A debtor's corpse with its soul still in it: the only thing the vengeance effigy bill takes.</summary>
    public class SpecialThingFilterWorker_NotDebtorCorpse : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t) => t is Corpse c && !(BloodDebts.IsDebtor(c.InnerPawn) && SoulUtility.HasSoul(c.InnerPawn));

        public override bool CanEverMatch(ThingDef def) => def.IsCorpse;
    }

    /// <summary>Avenged: an avenger near an effigy of their loved one's killer (stronger for a wailing one).</summary>
    public class ThoughtWorker_Avenged : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned)
            {
                return ThoughtState.Inactive;
            }
            int best = -1;
            foreach (CompEffigy e in Effigies.On(p.Map))
            {
                if (e.IsAvenger(p) && p.Position.InHorDistOf(e.parent.Position, 8f) && GenSight.LineOfSight(p.Position, e.parent.Position, p.Map))
                {
                    best = Mathf.Max(best, e.Wailing ? 1 : 0);
                }
            }
            // A face mask of the killer, worn by someone in sight (4.23).
            if (best < 0 && FaceMasks.AvengedBySeenMask(p))
            {
                best = 0;
            }
            return best < 0 ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(best);
        }
    }

    /// <summary>Dread: anyone but an avenger near a wailing effigy.</summary>
    public class ThoughtWorker_EffigyDread : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.Spawned)
            {
                return ThoughtState.Inactive;
            }
            foreach (CompEffigy e in Effigies.On(p.Map))
            {
                if (e.Wailing && !e.IsAvenger(p) && p.Position.InHorDistOf(e.parent.Position, 8f) && GenSight.LineOfSight(p.Position, e.parent.Position, p.Map))
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            return ThoughtState.Inactive;
        }
    }

    /// <summary>Wailing effigies scream when a raid arrives on their map.</summary>
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_IncidentWorker_RaidEnemy_Effigy
    {
        public static void Postfix(IncidentParms parms, bool __result)
        {
            if (__result && parms?.target is Map map)
            {
                foreach (CompEffigy e in Effigies.On(map).Where(e => e.Wailing).ToList())
                {
                    e.Scream(true);
                }
            }
        }
    }
}
