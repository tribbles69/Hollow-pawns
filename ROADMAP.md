# Hollow Pawns: roadmap

The design record for the mod: what's built, what's decided, and everything planned, with enough detail to implement from. Written for working through with Claude Code. Keep it updated as things ship: move items into **Built** and record decisions as they're made.

---

## 1. Ground rules (decided, apply to everything)

- **Only adult humans have souls.** Animals, insects, mechanoids and alien races have none. Children are never harvestable: no surgery, no reaping, their corpses are never taken, never baled into walls, never reanimated. This is a firm moral line, not a balance choice. It lives in one place: `SoulUtility.GetSoul()` returns null for non-adults. Every new feature that touches souls or corpses must go through `SoulUtility` and inherit it.
- **No soul = death.** A pawn whose soul is taken dies. (Going rogue instead was considered and rejected.)
- **Tick cost matters.** Nothing should tick per-thing if it can be avoided. Prefer, in order: event-driven (runs only when something happens) → rare-tick → a single MapComponent working through a sampled batch (like vanilla's steady environment effects). Never a per-wall or per-item normal tick. Ideas have been rejected for being tick-hungry for little payoff.
- **Combat Extended compatible.** Everything CE-specific goes in `Mods/CombatExtended/` (loaded only when CE is active, via `LoadFolders.xml`). Follow CE's own patterns: `CombatExtended.PatchOperationMakeGunCECompatible`, `ToolCE`, ammo sets. Check numbers against CE's repo (`CombatExtended-Continued/CombatExtended`) instead of guessing.
- **Grimdark tone.** The mod leans dark on purpose.
- **Generate, don't hand-write, repetitive defs.** Gun stats live in `Source/tools/gen_soul_guns.py`; CE soul-charged ammo is generated from CE's defs by `Source/tools/gen_soul_ammo.py`. Edit the table, re-run.
- **Def prefix `HP_`** for everything. C# namespace `HollowPawns`.
- **Mark guesses.** Anything written from memory rather than checked against 1.6 or CE goes in README's "Things I'm least sure of".

---

## 2. Built (v0.8)

| Feature | Notes |
|---|---|
| Soul body part | On the Human body, appended last in the body tree (save-safe), coverage 0. |
| ~~Extract soul surgery~~ | **Removed in v0.7** with the Bone Altar (4.27), as decided. Bottle soul went with it (vessels replace jars). |
| Soul crucible | Corpse → 8 / 5 / 2 essence by freshness, leaving a soulless husk (see 4.1); husks never hauled to it; colonist corpses off by default. Forges soulsteel; makes all soul weapons and ammo. |
| Soul essence, soulsteel | Soulsteel = 10 plasteel + 3 essence, one tier above plasteel. CE armour 2.4 / 3.6 vs plasteel 2 / 3. |
| Soul generator | 1200W on 2 essence/day. |
| Reaper's scythe | Reap chance 0.5% per Melee level, ×3 vs downed. Kills, drops 12 essence. |
| Soul gun family | Vesper pistol, Whisper SMG, Dirge AR, Requiem marksman, Last Rites sniper, Litany MG, Wake shotgun. One-cell soul blast on every shot: takes limbs, craters walls on a miss, no splash to neighbours. |
| Soul shock | Stacking consciousness debuff from soul blast damage; downs at high severity, fades in under a day. |
| Research | Soul extraction (after Electricity); Soul weaponry (after Soul extraction + Gunsmithing). |
| The Reaper's Mark v1 (4.21) | Mark soul on prisoners (render / bind / clear: wardens take them to an altar or press), Mark all on the altar and press, Soul-Lock on prisoners, ensouled/soulless corpse filters, corpse hover line, two alerts. Uses Harmony. Marks and locks stored in a WorldComponent by thing ID. The colonist confirmation went with the surgeries (only prisoners can be marked now). |
| Essence by freshness (4.16) | Crucible yield 8 fresh / 5 rotting / 2 desiccated (`SoulUtility.DeadSoulYield`); the recipe has no fixed products, the worker drops the essence. |
| ~~Soul jars (4.17)~~ | Replaced in v0.7 by vessels (4.30). |
| Soul broker (4.19) | Orbital + Outlander caravan trader kinds, Expensive prices, never pawns. Soul weapons and drugs got their own trade tags (they used to inherit WeaponRanged/WeaponMelee, so any arms dealer could stock them). Market values unchanged: essence 12, soulsteel 14. Vanilla settlements likely already buy essence via BuyExpensiveSimple. Trade requests for essence left for later. |
| Volatile stockpiles (4.20) | Essence stacks explode (vanilla `CompExplosive`, chemfuel's pattern) in `HP_SoulBurst`: armour-ignoring, near-harmless, heavy soul shock. Generator breakdown vents a burst (`CompSoulVent`, on the breakdown signal). CE cook-off of soul rounds was done in v0.4. |
| Drugs (4.11) | Soulfire (high + crash in one hediff, addiction "soul hunger") and Borrowed time (`preventsDeath` for a day, +1 year per dose). |
| Curses (4.2) | Marked, rotting luck, soul-sick (rolled; fade over 4 days) and hollowed (cursed idol, while worn). Sources: demon hits (10%, Harmony postfix on `Pawn.PostApplyDamage`), purging corrupted essence (33%), the idol. |
| Demons (4.3) | Biotech-only (`Mods/Biotech`). `HP_Demon` xenotype of vanilla genes plus `HP_CorruptedSoul`; `HP_Demons` faction (pirate-style, permanent enemy, added to old saves by `GameComponent_Demons`); five pawn kinds inheriting vanilla raider kinds. Corrupted essence via `SoulUtility.IsCorrupted`, purged at the crucible 5 → 10 clean. Cursed idol as demon loot. |
| Hell gate (4.4) | Buildable gate (`Building_HellGate`, rare tick, demon raid every 3 days while open, 1.1x points) and the Hell Unbound storyteller (Cassandra + `HP_DemonWave` on/off cycle, generated by `gen_storyteller.py`). |
| Extraction buildings (4.27) | Bone Altar (stone, no research: 4000-tick rite, 25% botch for a grade less, witnesses -6, 120 corruption r3.9) and Soul Press (800W, research: 1500 ticks, +1 grade, no botch, no witness thought, 50 corruption r6.9). `JobDriver_SoulRite`: the warden fetches a vessel (bind), restrains the prisoner (`HP_RiteRestrained` sets moving to 0), carries them to the building, performs the rite. Vanilla effecters for now. |
| Grades and vessels (4.29, 4.30) | Seven grades from skills (best and top three, passion counts). Render 6-20 essence by grade; smash a vessel for 1.5x. Urn (crafting spot, up to fine), canister (machining table, up to masterwork, powered rack), lattice (fabrication bench, any grade). Residue (a trait or top skill) is stored and shown; nothing uses it yet. |
| Corruption core (4.28) | `MapComponent_Corruption`: a byte per cell; spills from rites, crucible, generator exhaust, soul blasts/bursts, broken vessels, demon auras and demon corpses; 200 sampled cells per 250 ticks for spread (above 80), withering (above 120) and fire cleansing; above 160 souls come out corrupted. Pawn corruption hediff (4 stages, curse at the top). Holy water (order + drink), purgation surgery. Overlay toggle in the play settings. Hell gate needs average 100 to open. |
| Endless war (4.1, 4.5, 4.6, 4.24, 4.26, 4.31) | Thralls and clone vats (Biotech), reanimated husks (slab), stand guard, carrion bales and walls, corpse grinder, line forge with conscript rifle, helmet and vest. Grinder, forge, clone vat, bone altar and soul press (the last two load and leave) can be staffed (hauling work, thralls included) for up to x3 speed by skill. Soulless pawns keep only food and rest needs (Harmony on `ShouldHaveNeed`) and never chat. |
| Reliquary, soul-bound items, shrines (4.35) | Reliquary case (Remembered thought for those who loved the soul), *bind soul into item* for any weapon or apparel (bonus by residue, size by grade, via a postfix on `StatWorker.StatOffsetFromGear`), bound shrine (consecrate or desecrate, fixed once bound) and essence shrine (fuelled, toggle). Shrines feed the corruption map pass. Not yet: shrines as ghost wards (with 4.8), soul-bound sculpture tales. |
| Prisoner battery (4.7) | Casket-style generator: mark a prisoner *Battery*, a warden locks them in, 300-900 W by soul grade for five days, then a husk and some essence. Release early for soul shock. Rare tick only. |
| Blood debts (4.18) | Killers of colonists carry a blood debt (kept as world pawns if they escape). Torment prisoner mode (pain, soul shock, Avenging for loved ones, kind colonists disturbed, goodwill hit). Vengeance effigy cast from a debtor's corpse; wailing effigy from the *Effigy* fate, screaming at night, at passers-by and at raids (soul shock, panic). Placeholder scream sound. Not yet: Ideology precept reactions, vengeance TaleDef. |
| Scenarios (4.33) | The Coven (altar, shackled captive, corrupted ground), Endless War (Biotech: thralls, forge, grinder, generator, frequent raids) the Escapee (lattice holding a loved one, hunted) and the Gravedigger (lone undertaker with the scythe on an unburied battlefield, 8% ghost chance). Custom scenario parts for buildings, captives, corrupted ground, thralls, the lattice and the battlefield. Not yet: the Inquisitors (needs salt), precepts. |
| Longevity (4.10) | Fountain of youth (essence bath, 1-2 years, chance to cure a chronic condition, five-day cooldown) and soul cradle (spacer casket, three-day powered cycle on 30 essence: 5-8 years, all chronic conditions, one part regrown). |
| Unique armour (4.12) | Reliquary armour (two essence charges that each cancel a death and leave the wearer downed for a day; recharged at the crucible; made with a filled vessel, so it is soul-bound) and the reaper's shroud (+5% reap chance through a new stat). |
| Lower-tech soul gear (4.13) | Soul bow (soul shock on every hit; CE soul arrows in its own ammo set), bone fetish (halves reap chance against the wearer) and wraith lantern (once a day: soul shock and panic on enemies within 9 cells). Not yet: javelins. |
| Soul pipes (4.14) | Optional `Mods/VEF` folder, loaded only with Vanilla Expanded Framework: soul pipe, valve, drain, vat (vents a soul burst when wrecked), tap. Auto-refuels anything that burns essence. XML only. No soulsteel processor. |
| Full-kit apparel sets (4.22) | `gen_apparel_sets.py`: six sets (gravecloth, ossuary plate, mourner's kit, soulsteel kit, wraithweave, reliquary kit) built from vanilla originals, checked clash-free and complete against the game's Human body with and without CE; CE adds webbing, backpack and shield per set and mirrors CE's patches. Full-kit hediff on wearing every piece. Art borrowed and tinted. |
| Flayed faces (4.23) | Take-face butcher bill (corpse marked, not used up) and flay-face surgery (violation, lasting scar and mood). Named masks with social thoughts, a horror for whoever loved the face, and Avenged for blood-debt avengers. |
| The Profaned (4.32) | No hard dependency: genes looked up by name, revival patched by hand only if its type exists. Profaned souls count as corrupted; Cursebearers plant wretched soul shards (caster's residue) in what they raise; a dead Cursebearer collapses everything it raised. Not yet: Profaned blood and bones as cult inputs. |
| Ghosts (4.8) | One 1% roll (mod setting) per neglected ensouled corpse, in the corruption corpse pass. Lingering (drifts, cold spot, dread) or wrathful (corrupted ground or corpse; hunts souls through walls inside its tether with grave chill, armour-ignoring, lethal). Ends when the body is rendered, bound, ground, burned, buried or taken away. Only soul weapons, soulsteel or the scythe, and fire hurt it. Shrines ward. Target filtering and bullet pass-through (vanilla and CE) built too. Not yet: salt lines, smoother drift art. |
| Corruption events (4.28) | Black rain (a game condition; taints 3-5 patches through vanilla's steady cell sampling, corrupts pawns caught outside) and the fallen idol (meteorite-style; leaks corruption on the map pass until smashed, with a soul burst, or claimed and deconstructed). |
| Curse ritual (4.2) | *Curse* prisoner mode: the prisoner is cursed and carries it home when released or escaped; their faction's next raid arrives cursed. Also CE soul great arrows and soul javelins. |
| Conscript kit (4.24) | Full slot-filling conscript kit from the apparel generator (fatigues, greatcoat, belt, goggles around the existing vest and helmet; forge-only, no bench recipe). The forge's full kit makes all of it plus the rifle (140 steel). A *Conscript kit* outfit policy in every game, and *Kit out thralls* on the forge. |
| Extraction polish (4.27) | Wisps while a rite runs (torn out in fits at the altar, a steady stream at the press), a flare and a release tone at the end, a drone at the start (placeholder sounds in `Sounds/HP`). The press draws attention: each rite adds to it, raids get up to +30% points, halving every five days. |
| Corruption, the rest (4.28) | Cleansing rite (5 holy water, scours ~7 cells, eases pawns, lays ghosts to rest), Ideology precepts (abhorrent / disliked / exalted; `Mods/Ideology`), attuned cult perk without DLC, corrupting hits (demons, Profaned, corrupted-soul weapons), zealot raids for badly corrupted maps (purifying aura), cursed idol in trade and rewards. |
| Demon raids and sieges (4.34) | Rift portals and burrowing (demon-only arrival modes), the Great Rift (waves every 18 hours for three days, a keeper to kill or a cleansing rite to seal it, a crater if left), sieges for demons and zealots. |
| CE | Soul guns fire CE laser beams on shared soul charges (one ammo, per-gun ammo sets). Soul-charged variants of 11 CE calibres used by vanilla guns. CE stats for scythe, guns, soulsteel. |

---

## 3. Immediate to-do (before any new features)

1. ~~Build the DLL~~ Done: builds clean against `Krafs.Rimworld.Ref` 1.6.4871.
2. **First in-game test** using the checklist in README.
3. ~~Verify the uncertain defs~~ Done: all exist in 1.6 Core; one bad field fixed (`soundImpactStuff`). See README "Verified".
4. ~~CE laser blast check~~ Done: CE's laser beams run `CompExplosiveCE` on impact via `BulletCE` → `ProjectileCE.Impact`.
5. ~~Research tree positions~~ Done: moved to (7, 5.5) and (8, 5.5), directly under the vanilla industrial columns.

---

## 4. Planned features

Rough agreed order: **curses → demons → hell gate → thralls and clone vats → prisoner battery → ghosts → dreadnoughts**, with the smaller items slotted in wherever convenient. Each one sets up the next.

Added after the second pirate think tank: **soul jars → blood debts** (they build on each other), with the soul broker and volatile stockpiles as small fill-in items. The **Reaper's Mark** QoL package (4.21) can go in any time: it only needs what's already built.

**Backbone (decided 13 Oct 2026): corruption (4.28) and the Weight of Names (4.29).** Corruption is the spine the dark features hang off: ghosts, curses, demons, the hell gate, corrupted essence and the cleansing rite all read from it instead of being separate systems. The Weight of Names makes each harvest a choice. Build order after the first in-game test and the extraction buildings (4.27): **soul grades and jars → corruption core (terrain + pawn) → purification → hostile and event sources → cult-side benefits**, then the existing feature order, now wired into corruption.

### 4.1 Corpse wall (carrion wall)
**Built in v0.7.** Decided: *bind carrion* gives 2 bales per human, 1-6 by body size for animals (Harmony on `GenRecipe.MakeRecipeProducts`); a bale renders back into 46 human meat (half an unbutchered human) and feeds the clone vat (2 bales for 60 meat); walls are unlinked lumps (no linked atlas art yet); frozen walls **stop** rotting; warm walls lose about 1% hit points a day and leak rot stink. Corruption leak from walls not built.
Outdoor wall made from a pile of corpses, human and animal. Goes with the StarCrete/hemocrete idea.
- **Building it:** wall costs can't reference "any corpse" (every species has its own corpse def). So: a **"bind carrion"** bill at the butcher spot/table turns any corpse into a **carrion bale** (stackable resource); walls cost bales. XML. **Exclude child corpses** (needs a special filter, like `SpecialThingFilterWorker_Soulless`).
- **Constant rot:** very negative beauty; continuously emits rot stink gas; loses HP over time when above freezing; burns well.
- **Tick cost:** one MapComponent working through a sampled batch of carrion walls, not a comp per wall.
- **Open decisions:**
  - Should frozen walls **stop** rotting entirely, or only **slow** down?
  - ~~Husk change~~ **Decided (13 Oct 2026):** rendering a corpse at the crucible and binding a living pawn (4.30) both leave a **husk**: the body stays, marked soulless. Harvest the soul → reanimate, bale or take the face of the husk → nothing wasted. **Built for the crucible in v0.4** (`RecipeWorker_HarvestCorpseSoul`); binding comes with 4.30.

### 4.2 Curses
**Curse ritual built in v0.8:** a prisoner interaction, not an ability; the point is to release them, so the curse reaches their faction's next raid.
**Built in v0.6.** Decided: four curses, three rolled (fade in 4 days) and hollowed from the idol. The curse ritual on a prisoner is not built yet: still open.
Debuff conditions: e.g. *Hollowed*, *Marked*, *Rotting luck*, *Soul-sick*.
- Hediffs are pure XML.
- Sources: demon attacks (4.3), cursed loot (a relic that curses its wearer), a curse ritual cast on a prisoner.
- Delivering them as an ability needs Royalty or a little C#.

### 4.3 Demons
**Built in v0.6.** Decided: demons are a normal (not hidden) pirate-style faction with a few world bases, so raids and AI need nothing custom; pawn kinds inherit vanilla raider gear; corrupted essence is used by purging at the crucible (5 → 10 clean, 33% curse risk). Curse on hit: 10% per damaging hit.
Same soul mechanics, corrupted.
- **Demon xenotype** (Biotech genes: horns, skin colour, toughness, fire immunity). XML.
- **Hostile demon faction** using the xenotype. XML.
- **Corrupted soul:** extracting from a demon gives **corrupted essence**, a second resource: stronger but dangerous to use (side effects, curses). Small branch in `SoulUtility` keyed off the demon gene.
- Custom bodies would need Humanoid Alien Races as a dependency, so start with the xenotype.

### 4.4 Hell gate
**Built in v0.6.** Decided: the gate pulls a wave every 3 days while open at 1.1x points; the storyteller is Cassandra plus a demon wave roughly every 12 days from day 15.
Demons arriving in waves. Two flavours, both wanted:
- **Storyteller mode:** a custom storyteller that sends demon waves. Mostly XML.
- **A gate you build:** a structure that pulls demon waves on a timer. A risk-for-reward way to farm corrupted souls. Small C#, rare-tick only.

### 4.5 Thralls and clone vats (endless war)
**Built in v0.7 (Biotech).** Decided: the 30-day lifespan is on; the vat takes a 1500-work bill (60 meat, 25 steel, 5 essence), then grows for 3 days on the rare tick and pauses without power; thralls get vanilla basic gear from their pawn kind and a vat-grown backstory.
The loop: kill a wave → corpses into walls, souls into ammo → vats grow new soldiers armed with basic gear → take the next wave. Raids scale with colony size, so it escalates by itself.
- **Thrall xenotype** with a **Soulless** gene:
  - No soul, but alive: `SoulUtility` treats the gene as "no soul".
  - **Work: hauling and firefighting only**, plus combat (drafting, not a work type, so the gene must not block violence). Disable everything else via the gene's disabled work tags. (Rescue was considered and dropped: leave it out.)
  - **Needs: food and sleep only.** Vanilla genes can't remove needs, so: Harmony patch on `Pawn_NeedsTracker.ShouldHaveNeed` returning false for Soulless pawns except Food and Rest. No mood need means no mood, no mental breaks, no recreation.
  - **No pain** (gene pain factor 0).
  - Block social interactions so they don't form relationships.
- **Clone vat:** large powered building taking meat or nutrient paste + steel + a little essence. After a few days it decants an **adult** thrall wearing basic gear (flak vest, helmet, cheap rifle or melee weapon, set by the thrall pawn kind). Needs C# to spawn the pawn.
- **Optional lifespan** (~30 days, then they degrade and die) to keep the vats running.
- **Performance angle:** thralls with most needs and thoughts stripped are cheaper to simulate than colonists. Worth benchmarking.

### 4.6 Reanimated husks (the "mech" version of thralls)
**Built in v0.7.** The slab is a doctors' bill (25 steel + a soulless adult corpse); husks last about 4 days.
Emergency meat shields. Chosen over true Biotech mechs, because true mechs must be mechanoid flesh type and need new art from every direction.
- **Reanimation slab:** takes a **soulless corpse** + steel and resurrects that exact body as a husk. Same Soulless behaviour as thralls, plus bad stats, slow movement and a short lifespan (a few days) before they collapse.
- No new art: keeps the body's own look and gear.
- Never child corpses.
- C# to spawn/resurrect from the bill.

### 4.7 Prisoner battery
**Built in v0.8.** Decided while building: a new fate on the Reaper's Mark (*Battery*) rather than a bill; the drain is five days whatever the grade, and grade sets the wattage.
A casket-style pod (like a cryptosleep casket). Lock a prisoner in: it generates power while slowly draining their soul. When the soul runs out they die and drop essence. Adults only. Small C# on top of vanilla's casket.

### 4.8 Ghosts (reworked 13 Oct 2026)
**Built in v0.8.** Decided while building: a ghost is an animal-race pawn with its own think tree and its own step-by-step movement (1.6's flying animals still use the pathfinder, so no help there); burying the body counts as dealing with it; killing a ghost leaves nothing and its corpse doesn't roll again; grave chill thaws slowly on its own (0.004 per 250 ticks) and fast in a warm warded room. Gravedigger scenario now possible.
A ghost is a soul still clinging to its body. Ghosts are **rare and local**: tied to one corpse, dealt with by dealing with the dead.

**Spawning**
- **1 in 100 chance per corpse**, rolled **once**, when an ensouled adult corpse has lain unharvested past a threshold (about a day; tune in testing). Mod setting so players can change the odds.
- Only ensouled corpses: never children, animals, husks (soul already gone) or anything `SoulUtility` says has no soul.
- Check: the corruption MapComponent (4.28) walks a sampled batch of corpses on the rare tick and rolls only for corpses that haven't rolled yet (a flag on the corpse). No per-corpse ticking.

**Two kinds**
- **Lingering ghost (harmless):** from an ordinary corpse. **Tethered** within a few cells of its body. Drifts and wails, nothing more. Dread: a mood penalty for anyone who sees it; maybe a cold spot lowering the temperature around it.
- **Wrathful ghost (hostile):** from a corpse lying on **corrupted ground**, or a **corrupted corpse** (demons 4.3, Profaned 4.32). Still tethered, but its radius is wider and grows with corruption. Inside that radius it passes through walls and hunts. One haunted corner of the map to avoid, ward or clear, not a ghost roaming the whole base.

**Ending a ghost:** render, bind (if somehow still possible), grind or burn its corpse, and the ghost goes with it. The real counterplay is "deal with your dead". It can also be destroyed directly (below).

**No body, so no physical attack: grave chill**
- Its touch attacks the soul directly and **ignores armour** (custom damage def with no armour category that applies a hediff).
- **Grave chill:** stacking, like toxic buildup. Cold and slow movement → lost consciousness → downed → death at full severity if untreated. Also adds pawn corruption (4.28).
- **Cures:** drinking holy water, a cleansing rite, or time in a warm, warded room.
- **Only touches souls,** so ghosts ignore children entirely, consistent with the ground rules.

**Passing through walls**
- Leave the 1.6 pathfinder alone (Burst-compiled and multithreaded). Ghosts get their own movement: a custom job drifting them cell by cell in a straight line, ignoring walls and doors, drawn smoothly between cells.
- **Warding:** salt lines, shrines and consecrated walls (4.28 purification) **stop** ghosts. Ordinary walls are air to them; warded rooms are safe. Gives purification a combat use.
- **Check first:** how 1.6's flying animals move; it may offer a ready-made route.

**Immune to most weapons**
- **Step one (simple):** a comp on the ghost discards all damage except allowed types: soul blast (soul weapons), fire, thrown holy water, soulsteel or blessed melee, psychic. All damage passes through the same pre-apply check, so this works for vanilla and CE alike.
- **Step two (later):** true pass-through, so bullets fly on to whatever's behind. Needs a projectile collision patch, and a separate one for CE's 3D projectiles (`ProjectileCE`).
- **Targeting:** filter ghosts out as targets for weapons that can't hurt them, so colonists don't waste ammo. Small extra patch.

**Performance:** ghost count is capped by neglected corpses at 1 in 100, so there are only ever a handful. Lingering ghosts idle near their body. Wrathful ghosts' leash and movement run only on themselves.

**Art:** a translucent pawn-style graphic (all facings); lingering and wrathful can share it with a tint.

### 4.9 Soul dreadnoughts
Huge Biotech-style mechs grown at the gestator from soulsteel and essence. They run on essence instead of power; possibly need a soul core made from a captured prisoner. Code is moderate; **the real cost is art** (all directions).

### 4.10 Longevity
**Built in v0.8.** Decided while building: the bath is a float-menu order, not recreation; nobody goes below 18; bathing and the cradle add a little soul corruption to the pawn.
- **Fountain of youth** (medieval, unpowered): stone font fed with essence. A pawn bathes (job or recreation) and loses a year or two of biological age per dose, with a chance to cure old-age conditions (bad back, frailty, cataracts). One C# hook to set the age.
- **Soul cradle** (spacer): sealed bio-pod running multi-day cycles on essence. Bigger age reversal, guaranteed chronic-condition removal, maybe regrowing lost parts. Model the cycle and occupancy on Ideology's biosculpter.

### 4.11 Drugs (overcharge)
**Built in v0.5.** See Built for what shipped.
Mostly XML (vanilla drug system handles highs, tolerance and addiction).
- **Soulfire:** short burst of speed, consciousness and pain immunity, then a hard crash. High addiction; withdrawal called "soul hunger" (sets up the soul-eating xenotype).
- **Borrowed time:** for a day the pawn can't die from injuries (downed instead), but each use ages them a year.

### 4.12 Unique armour
**Built in v0.8.** Decided while building: reap chance is two stats (*reap chance* on the reaper, *reap ward* on the target) so gear moves it like any gear bonus; the armour is a stand-alone piece for now and can become the spacer heavy chest in 4.22.
- **Reliquary armour:** soulsteel set with an essence reservoir. When a hit would kill the wearer, it burns stored essence to cancel the death and leave them downed; then it needs refilling. Small C#, only checked on a lethal hit.
- **Reaper's shroud:** hooded cloak that raises the scythe's reap chance; stacks with Melee skill.

### 4.13 Lower-tech soul weapons (to brainstorm further)
**Built in v0.8.** Decided while building: javelins left out (vanilla has none to copy); the lantern is a worn-gizmo button rather than a reloadable verb; under CE the soul bow fires ordinary arrows too, and only soul arrows shock.
- Neolithic: soul-dipped arrows and javelins (vanilla bows have no ammo, so a soulsteel bow; under CE a real arrow type). A bone fetish charm that makes the wearer harder to reap.
- Medieval: soulsteel blades already work via stuff. Unique: a **wraith lantern**, a carried light that frightens nearby enemies into fleeing (vanilla flee mechanic).

### 4.14 Soul vats and pipes
**Built in v0.8.** Decided while building: optional, not a hard dependency (only loads with VEF); no soulsteel processor (VEF's processor can't do it); the vat vents a soul burst instead of exploding in fire.
Like Dubwise's Rimefeller / Dubs Bad Hygiene. Use the **Vanilla Expanded Framework PipeSystem** (hard dependency on VEF): Thing to resource (extractor), Resource storage (vats), Resource to power (generator by pipe), Resource processor (soulsteel), Refill building with pipes (weapons or turrets). Writing a pipe network from scratch would be the biggest C# job in the mod: don't.

### 4.15 Soul-eating xenotype (left for later)
Sanguophage-style, eats souls. Needs Biotech.
- Gene and xenotype defs: XML. A **soul hunger** resource bar like hemogen: small C# gene class modelled on vanilla hemogen (check VEF's genes module first). Eating soul essence tops it up (small ingestion hook). A soul-drain ability like the bloodfeed bite.
- Design clash to resolve: a full drain kills (no soul = death), so a pure version must keep hunting. Suggested middle ground: a drain takes part of the soul, which slowly regrows; only a full drain kills. Gives a prisoner-farming loop and a reason not to get greedy.

### 4.16 Smaller ideas
- ~~Essence yield by corpse freshness~~ **Built in v0.5** (8 / 5 / 2).
- Ideology reactions: treat soul harvesting like organ harvesting (horrified vs approving precepts).

---

### 4.17 Soul jars (from the pirate think tank)
**Built in v0.5** as a second surgery (bottle soul). See Built for what shipped.
A second option at the extract-soul surgery: **render** the soul into 12 essence as now, or **bottle it whole** as a named soul jar. (The surgery is being removed, so this choice moves to the extraction buildings in 4.27.)
- The jar stores the victim's name, faction, backstory and the day taken, shown on inspect. Must survive save/load.
- Dim glowing decoration with good beauty: a shelf of the damned.
- Smash it later for the same 12 essence, so bottling never costs anything.
- Each bottling records a Tale that sculptors can carve.
- Build: one ThingDef with a single-facing texture, a ThingComp storing the name (`PostExposeData`), a recipe worker for the second surgery option, a TaleDef. No ticking. Adults only via `SoulUtility`.
- Conditions: search the Workshop for an existing soul-jar mod first; prove the name survives save/load before making art.
- Mutiny clause: if playtests show everyone smashes their jars, cut the decoration and keep only the named Tale. (The blood-debt line below gives jars a stronger reason to exist.)

### 4.18 Blood debts: revenge on whoever killed your colonists
**Built in v0.8.** Decided while building: the debt is a hediff on the killer (not a WorldComponent), so it travels with them; the wailing effigy is a fate on the Reaper's Mark, taken by the rite; the vengeance effigy is cast at the sculptor's table from 25 soulsteel and the corpse; avengers are worked out when the colonist dies (the same family, lover or good-opinion test as the reliquary).
**1. The blood debt.** When an enemy kills a colonist, record it: the killer gets a visible **"Blood debt"** mark naming who they killed. Event-driven (hook on colonist death with a pawn instigator); store debts in a WorldComponent so they persist if the killer escapes.
- Escaped killers are world pawns and can return in later raids, still marked. Seeing the mark in combat tells you to down them, not kill them.

**2. Torment** (if captured): a new prisoner interaction mode next to recruit, enslave and execute. A warden runs sessions, modelled on vanilla's resistance-reduction job.
- Each session adds soul shock and pain to the prisoner.
- Colonists close to the victim get a growing **"Avenging {victim}"** mood buff.
- Kind-hearted colonists and some Ideology precepts are disturbed; it's a violation and the prisoner's faction hates you for it.
- Build: `PrisonerInteractionModeDef` (XML) + a warden JobDriver/WorkGiver (C#). Adults only.

**3. The endings: art.** Both use the vanilla art system (quality from the sculptor's Artistic skill; description generated from a vengeance TaleDef).
- **If they die → the effigy.** Their soul goes into a vengeance sculpture: *"A soulsteel figure of Vex the Butcher, cut down at the wall on Day 61 for the murder of Jeb."* Colonists who loved the victim get **"Avenged"** whenever they see it. Otherwise a normal art piece.
- **If they live → the wailing effigy.** After torment, their living soul is bound into the sculpture, and it **screams** at moments that matter: when a raid arrives, at night, when someone walks past. Event-driven or a slow sampled check, never a per-tick comp.
  - Avengers get a much stronger, lasting "Avenged".
  - Everyone else gets **dread**, so placement matters.
  - Raiders who hear it on approach get soul shock or panic: a grim defensive totem by the gate.
  - **Smashing it** releases the soul for a big essence payout.
- **Audio:** the scream needs a custom wail sound. Aaron has an audio-engineering background and will likely make this himself; leave a placeholder SoundDef pointing at a clearly named file.
- Soul jars (4.17) remain the everyday version for anyone bottled; blood-debt enemies get the art.

### 4.19 Soul broker (small, XML)
**Built in v0.5.** See Built for what shipped.
- First, a **market-value pass** on essence (currently 12), soulsteel, jars and soul weapons.
- Then traders: an orbital **soul broker** (comms console) and an outlander caravan variant that buy and sell essence, soulsteel and soul weapons at a markup. **Never pawns.** Settlement trade requests for essence via the vanilla quest system.
- Check first whether existing traders already buy essence (it sits in the Manufactured category).

### 4.20 Volatile stockpiles (small)
**Built in v0.5.** See Built for what shipped.
Hoarded essence is dangerous cargo, so players spread their stock out.
- Essence stacks get vanilla `CompExplosive` (like chemfuel): fire or a hit sets a stack off, radius scaling with stack size up to a cap, using **soul blast** damage (already applies soul shock), so pawns drop alive.
- A soul generator breakdown vents a short soul shock pulse at whoever's nearby.
- Under CE: soul-charged rounds cook off in fires, if CE's cook-off system accepts custom ammo (unverified).
- Inspect text warns "Volatile: spread your stock."
- Tick cost: none. Item explosions only run when triggered, and the vent fires on the breakdown event.

### 4.21 The Reaper's Mark (quality of life, from the QoL think tank)
**v1 built in v0.4.** Decisions made while building: the Mark button is a toggle (click again to cancel); Soul-Lock shows on colonists, slaves and prisoners once *Soul extraction* is researched; the confirmation covers slaves as well as colonists; the unclaimed-corpse alert skips your own dead and corpses already in storage; "low" means under 2 essence (about a day).

*Know which bodies still hold a soul, take one in a click, and never by accident.* Everything in v1 works with what's already built. Nothing ticks per item. (Once the surgery is removed, "queue extraction" means queueing the prisoner for the altar or press in 4.27.)

**v1**
1. **Mark button** (gizmo) on adult human prisoners that queues the extract-soul surgery in one click, and a **Mark all** gizmo on the soul crucible that marks every eligible prisoner. Never shown for children or the soulless (check via `SoulUtility`). Harmony postfix on the pawn's gizmos; runs only on click.
2. **Soul-Lock** toggle on any pawn: the mark buttons skip them and the surgery can't be queued on them. Plus an **"Are you sure?"** confirmation before any extraction is queued on a colonist (one-click killing needs a safety catch).
3. **Ensouled / Soulless corpse stockpile filters.** Nearly free: `HP_AllowSoulless` and `SpecialThingFilterWorker_Soulless` already exist for the crucible. Add a configurable stockpile-facing filter def (and an "ensouled" counterpart) so haulers can route full corpses to the crucible and husks elsewhere.
4. **Corpse hover line:** "Soul: intact (8 essence)" or "Soul: taken" on the corpse's inspect string.
5. **Two alerts** (vanilla `Alert` subclasses, vanilla's own throttling): "Ensouled corpse unclaimed" and "Soul generator low" (a simple fuel threshold, not a forecast).

Already done: essence shows in the top-left resource readout (it's a resource with a readout priority).

**Later, once their features exist**
- Rot alert ("corpses spoiling, ~N essence lost") → after essence-by-freshness.
- Stockpile size and fire warning with a blast-radius overlay → after volatile stockpiles (4.20).
- Thrall and husk status lines → after thralls and husks.
- Essence forecast ("generators dark in ~9 hours") → once burn rates are stable.
- A full soul management **tab** (Barnacle's Register) and a **soul sight map overlay** (Mossbeard's Pale Sight: souls glow, the soulless show as cold outlines). Both are nice but costly UI.

**Mutiny clause:** if the gizmo or dialog patches clash with CE or a heavy modlist, or v1 grows past these five items, cut back to Mark + Soul-Lock and ship that.

### 4.22 Full-kit apparel sets
**Built in v0.8.** Decided while building: pieces copy a vanilla original rather than inventing stats, so no piece outclasses its vanilla equivalent (armour x0.9 light, x0.95 heavy) and CE's own patches can be mirrored onto them; the reliquary armour is the spacer heavy chest; CE-only pieces don't count toward the bonus.
Aaron likes filling every clothing slot and hates working out what fits together. So: six sets that fill **every** slot, guaranteed conflict-free.
- **Six sets:** light and heavy for each of medieval, industrial and spacer. Working names: *gravecloth* (medieval light: gambeson, hood and so on; mobile, cheap, good cold insulation), *ossuary plate* (medieval heavy, stuff-based), *mourner's kit* (industrial light, flak-style, fast), a soulsteel heavy set (industrial heavy), and spacer light/heavy at marine tier and above. The planned reliquary armour (4.12) can become the spacer heavy chest piece.
- **A generator, like the guns** (`Source/tools/gen_apparel_sets.py`): each set is a table of pieces, each with one apparel layer and its covered body-part groups. The script **refuses to output a set where two pieces share a layer and any body part** (that's what makes items clash in-game) and **checks the set covers every slot**. Pull the exact slot list from the game's own apparel layer defs rather than guessing.
- **Shared stat budget per tier**, so light vs heavy is a clear trade (armour against move speed and work speed) and no single piece outclasses the vanilla equivalent at its tier.
- **Full-set bonus** (optional, small C#): a small buff for wearing every piece.
- **Combat Extended extension** (CE-only folder): CE adds five layers: *webbing* (tactical vests, ammo and carry capacity), *backpack* (carry capacity), *shield* (off-hand melee/ballistic shields), *skin (head)* (headwraps and balaclavas under helmets) and *strapped (head)* (headsets and goggles). Each set gets its webbing, backpack and shield pieces (plus head pieces if they fit the theme) only when CE is loaded, so nobody gets half a set either way. Layers checked against CE's `Defs/ApparelLayerDefs/ApparelLayerDefs.xml`.
- **Art is the real cost:** worn apparel needs every facing and body type. First pass borrows vanilla worn textures tinted by material, so stats and slot logic can be tested before custom art exists.

### 4.23 Face masks (flayed)
**Built in v0.8.** Decided while building: the mask is on the eye slot (EyeCover) in both vanilla and CE, because vanilla only loads bodyless head art for Overhead and EyeCover; so it stacks with helmets but not with goggles. No Ideology precept reactions yet.
Like Ideology's skull extraction, but the face, worn as a mask.
- **From corpses:** a **"take face"** bill at the butcher table on an adult human corpse. The corpse is marked rather than consumed (same mark-instead-of-consume code as the crucible husk change in 4.1), so it can still go to the crucible, the reanimation slab or the carrion wall.
- **From the living:** a **"flay face"** surgery on an adult prisoner. They survive, permanently disfigured: a lasting condition with a heavy beauty and social penalty and a strong mood penalty for them. It counts as a violation; their faction hates you for it. A living face makes a better-quality mask than a corpse's.
- **The mask:** sits in the face slot so it stacks with helmets (under CE, the *skin (head)* layer). It **keeps the name**, like soul jars: *"The face of Vex the Butcher."* Stats are about intimidation, not armour: slave suppression up, social penalty with most people, social bonus with psychopaths and bloodlust. With Ideology, precepts decide whether wearers are revered or reviled. Deteriorates like leather, so they're trophies you keep earning.
- **Blood-debt tie-in (4.18):** flay your colonist's killer and wear them; colonists who loved the victim get "Avenged" when they see the wearer. Anyone who sees a **friend's** face being worn gets a heavy mood penalty.
- Adults only, corpse or living, via `SoulUtility`'s age rule.
- Build: one apparel def, a butcher bill and a surgery (C# workers), name storage shared with soul jars. Small worn art (face only).

### 4.24 Line forge: cheap conscript kit for the endless war
**Finished in v0.8:** the full kit, the outfit policy (added on game load if missing) and one-click kitting (a forge button that puts every thrall on the policy and sends the unarmed for a rifle). The conscript kit is vanilla-only in the generator: under CE it's checked for clashes but gets no webbing, pack or shield.
**Built in v0.7**, partly. Built: the machine (pick rifle / armour / full kit, pulls steel and components from adjacent cells, rare tick), conscript rifle with bayonet (CE: 5.56 and a ToolCE stab), conscript helmet and vest on vanilla textures. Not yet: the full slot-filling conscript set (needs the apparel set generator, 4.22), the outfit policy and one-click kitting.
Arm ten thralls between waves without spending souls; save the expensive gear for real colonists.
- **The line forge:** a 2x4 powered machine. **Steel, components and power only: no essence.** No pawn works it: materials are pulled from an input area next to it, finished kit is dropped on an output tile for haulers or thralls. Progress on the rare tick, and only while a bill has materials (no cost when idle).
- **It only makes its own conscript kit** (its own recipes; it does not patch into other benches' recipes):
  - **Conscript armour set:** helmet, body armour and enough pieces to fill every slot. Built with the apparel-set generator (4.22) as the cheapest tier, so it's guaranteed clash-free.
  - **Conscript rifle with bayonet:** a basic, unreliable rifle (poor accuracy, short range) whose melee tools include a **bayonet stab**, so it doubles as a spear when the fighting reaches the wall. The bayonet is just a stab tool on the gun (XML). Under CE it uses standard 5.56 NATO, so it can load the soul-charged rounds (and CE's `ToolCE` stab for the bayonet).
- **Deliberately rubbish, fixed stats:** no quality roll, poor stats, and near-zero market value so it can't be farmed for silver or compete with real gear.
- **One-click kitting:** a **"conscript kit"** bill makes a whole loadout in one go, and a matching **outfit policy** means anyone assigned to it walks over and dresses themselves.
- The loop: clone vats grow the bodies (4.5), the line forge arms them, they hold the wall; real colonists keep the soulsteel.

### 4.25 Trenches
Dug trench cells that actually protect whoever stands in them. Pairs with the carrion wall as a parapet (4.1) and thralls holding a line (4.5).
- **Step one: check *Frontline – Trenches*.** It already exists and CE ships patches for its guns (`ModPatches/Frontline - Trenches` in CE's repo). Find out how its trenches work. If it covers this, make Hollow Pawns' content work alongside it instead of building our own.
- **Why it needs code (vanilla):** vanilla cover is directional. When a pawn is shot, only cover in the adjacent cells on the side facing the shooter counts. What a pawn stands on (terrain, or a building in its own cell) never counts, so a plain dug terrain would give no protection.
- **Vanilla design:** trench as a terrain, dug by a designator. Small Harmony patch to the cover calculation: a pawn standing on trench terrain gets solid cover (~50–60%) from **every** direction, except from enemies adjacent to the trench or shooting down into it from its edge. Explosions and mortars still land. Trench cells are slow to move through and can't hold furniture.
- **CE design:** CE models cover as **height** (`Source/CombatExtended/CombatExtended/CollisionVertical.cs`): projectiles fly in 3D, crouching pawns duck to the height of adjacent partial cover, and the pawn's height already gets adjusted (e.g. `heightAdjust += 1` when flying). A trench is the opposite: patch that height calculation so a pawn on trench terrain sits lower, and flat incoming fire passes over unless the shooter is close or elevated. One small patch, and more realistic than the vanilla version.
- Tick cost: none; both patches only run when a shot is resolved.

### 4.26 Thrall performance: cheap enough for a big base
**Partly built in v0.7.** Stand guard is a toggle that drafts the pawn in place with a hediff (hunger 0.1x, no rest fall); drafted pawns already fire on hostiles, so no separate wake check was needed. They stand down when hungry. Not yet: the lean think tree, area-restricted hauling, thrall racks, the benchmark.
Goal: Aaron wants a big base full of thralls. Thralls must cost far less than colonists to simulate.
- **What the Soulless gene already cuts (4.5):** mood and situational thoughts (a notable per-pawn idle cost), social interactions and relationships, most needs, mental break checks, recreation jobs, most work scanning.
- **What it doesn't:** pathfinding (the biggest cost whenever anyone moves), combat, health ticking, and hauling (map-wide haul searches are expensive, and hauling is one of their two jobs). So unmanaged, thralls in a big fight cost close to normal pawns.
- **Custom think tree for thralls** (the real lever): fight if drafted → eat → sleep → haul within an assigned area → otherwise go to a muster point and stand down. Cuts per-decision cost to a fraction of a colonist's.
- **Area-restricted hauling:** thralls only search a small assigned zone (e.g. the crucible yard), never the whole map.
- **Lifespan** (4.5) keeps the population from creeping; husks (4.6) are cheaper still.

**Stand guard** (on the map)
- Toggle on a thrall or a group: they walk to their post and **stop**. No job searching, no wandering.
- Hunger slows to a trickle (~0.1×) and rest doesn't drain (hediff with hunger/rest rate factors), so a guard can stand for days.
- **Auto-wake:** when hostiles come within range they draft themselves and hold position. The wake check is one cheap "hostiles nearby?" look on the rare tick (use the map's existing attack-target cache), not per tick.
- Still costs something (health ticks, rendering) but decision-making is near zero.

**Thrall racks** (stored, near-zero cost)
- A grim rack building holding several thralls in **suspended animation**, like a cryptosleep casket (model on vanilla's casket: contained pawns don't tick needs or health). A hundred racked thralls cost about the same as a hundred crates.
- **Release** with one button or automatically when a raid arrives; they step out kitted and auto-draft at the rack.
- The loop: clone vats grow them (4.5), the line forge arms them (4.24), racks store them, released when the wave hits.

**Benchmark, don't guess:** use Dubs Performance Analyzer. Spawn 20 colonists and 20 thralls on a test map doing the same thing (idle, hauling, fighting, standing guard, racked) and compare per-pawn cost. Record results here. Worth a write-up for the RimWorld performance community.

### 4.27 Extraction buildings and animated extraction
**Polish built in v0.8:** wisp flecks thrown on the building's rare tick (no effecter to tick), flare on release, placeholder `RiteStart` and `SoulRelease` sounds for Aaron to replace, and press attention (raid points). Ideology: witnesses' reactions follow the corruption precepts (4.28) rather than a full ritual. Not done: render-node shudder and the hollow overlay.
**Built in v0.7** (see Built). Not yet: Ideology ritual integration, custom flecks/render-node animation and Aaron's sounds (vanilla effecters and the surgery sound stand in), the press drawing attention.
The extract-soul surgery is a stopgap: it's convenient, but taking a soul deserves a proper place to do it. Two tiers, from cult to industry. **Decided: the surgery will be removed once these buildings exist.** Anything that currently hangs off the surgery (soul jar bottling in 4.17, Reaper's Mark queueing in 4.21) moves to the buildings.

**Bone Altar (early, no power, no research)**
- Built from stone, bone and blood. The victim is bound on it and a pawn performs the rite.
- Slow, painful, risky: a chance of a partial or damaged soul (less essence), and mood penalties for witnesses unless their ideology approves.
- With Ideology: hook into the ritual system so the colony gathers to watch.
- Animation: blood channels in the altar glow as the rite goes on, wisps tear out of the body unevenly, a guttering flame flares when the soul comes free.

**Soul Press (late, powered, research-gated)**
- Soulsteel and advanced components; heavy power draw while running.
- Faster, cleaner, higher-quality souls than the altar. Downside: draws attention (raids, quests, or a faction that hates it).
- Animation: the pawn is held in a field, the machine's rings or arms spin up, the soul is drawn up in a steady pale stream into a containment chamber that glows while full.

**How to animate (keep it cheap)**
- **Flecks** (XML particles: fade, drift, rotate, scale) for the wisps, sparks and flash. Vanilla batch-draws them.
- **Effecters** chaining flecks and sounds over the job's duration, attached to the extraction job/toil, like vanilla surgery and psycasts.
- **Pawn render node** overlays (1.5+ render tree): the victim's shudder, and a lingering "hollow" overlay afterwards so harvested pawns read at a glance.
- **Custom shaders** (glow, distortion, dissolve) are optional extras. On 1.6 they come via Unity asset bundles, which will work differently on 1.7's Waylith engine: check the dev Discord before building any.
- **Sound:** extraction tone, altar flame, press spin-up. Aaron will likely make these himself; leave placeholder SoundDefs with clearly named files.

**Tick cost:** neither building ticks when idle. Animation runs from the job via effecters, only while someone is being harvested. Adults only via `SoulUtility`.

**Order:** altar first (players hit it earliest), then the press. Remove the surgery in the same release the altar ships, so there's never a gap with no way to extract.

### 4.28 Corruption: the backbone
**Rest built in v0.8:** cleansing rite (a designation and a cleaning job, not an Ideology ritual), precepts, the no-DLC attuned perk (exalted or 50%+ soul corruption), corrupting hits, zealot raids (hidden faction, own incident, only when 400+ cells are at 80+), and the cursed idol as cursed loot.
**Events built in v0.8:** black rain and the fallen idol. Still not built from this section: cursed loot, corrupted weapons, the cleansing rite, Ideology precepts, cult-side benefits, zealot raids, the purist mood penalty.
**Core built in v0.7** (see Built). Decided while building: ground corruption never decays by itself, it only creeps (moved, not created) or is purified; pawn corruption fades slowly (-0.02 a day); the altar spills more per cell, the press less over a wider area. Not yet: events (black rain, fallen idol), corrupted weapons, the cleansing rite, shrines, Ideology precepts, cult-side benefits, zealot raids.
Works like Biotech pollution, but its own system. Taking and spending souls stains the ground and the people who do it. **Bad on a purist run, a resource on a demon-cult run.** Aaron's idea (13 Oct 2026); it replaces the global "soul tally" meter the pirate council pitched, because corruption is visible on the map, so cause and effect stay clear.

**Two layers**
- **Terrain corruption:** its own map grid (one byte per cell, a `MapComponent`), separate from Biotech pollution so the mod needs no DLC and the two never get confused. Shown as a map overlay/section layer. Creeps outward slowly: the MapComponent processes a **sampled batch of cells on the rare tick**, like vanilla's steady environment effects. Never per-cell ticking.
  - Effects: **plants wither and nothing grows on corrupted ground** (decided 13 Oct 2026: no custom corrupted plants; a deep cult run has made its bed and lives on **meat and leather**: hunting, butchering, the corpse grinder 4.31); beauty drops; ghosts (4.8) rise more easily on corrupted ground; mood penalty for purists living or working on it.
- **Pawn corruption:** a staged hediff, a bit like toxic buildup. Gained from working on corrupted ground, handling essence, performing extractions, being hit by corrupted weapons, or standing in a demon's aura.
  - It is **soul corruption**, so it only exists on pawns with a soul: `SoulUtility` returns null for children, so they are never affected. No special case needed; the firm rule holds by itself.
  - Stages: unease → dread and mood penalties → health effects and mental breaks → (top stage) a hook for curses (4.2) and, on cult runs, demonic changes.

**Sources: the colony's own (all event-driven)**
- **Extraction:** the Bone Altar spills heavily onto nearby cells; the Soul Press vents less per extraction but over a wider radius. (Which tier corrupts more overall is a balance call, still open.)
- **Spending essence:** soul generator exhaust, the crucible, soulsteel forging.
- **Soul blast impacts:** the cell a soul round craters gets a small stain.

**Sources: how corruption reaches a purist colony**
- **Events:** a black rain that taints a patch of the map (like toxic fallout); a fallen idol / cursed meteorite / demonic relic that keeps leaking corruption until destroyed; cursed loot from traders and quests.
- **Hostiles:** demon raiders (4.3) carry a **corruption aura** that stains the ground they walk on and nearby pawns. Only checked on the rare tick, and only on the few hostile pawns that have it. **Corrupted weapons** add pawn corruption on hit and taint the struck cell.
- **Aftermath:** demon corpses leak corruption while they lie there. The choice after a raid: harvest them (more corruption, plus corrupted essence) or burn them.

**Purification: each method has a different cost**
- **Fire:** burning corrupted cells and corpses cleans them. Cheap and fast, but destroys crops, buildings and anything else caught in it.
- **Holy water:** a crafted consumable from a blessed well or shrine. Pawns sprinkle it on tainted cells (designator + job) or drink it to lower their own corruption. Gentle, but a steady resource drain.
- **Cleansing rite:** clears an area or a pawn. Costs time and gathers the colony; with Ideology it plugs into the ritual system. (This absorbs the "Rite of Laying" idea from the pirate council.)
- **Salt lines:** see 4.36: a conduit-style floor network that stops corruption, ghosts and demon portals.
- **Shrines:** see 4.35: a permanent bound shrine (consumes a vessel) and a fuelled essence shrine; both can cleanse or corrupt.
- **Purgation surgery:** a painful operation that burns corruption out of a pawn. Adults only.

**The two runs**
- **Purist:** corruption is a slow poison. Keep extraction away from living space, cleanse regularly, fight off demons who bring it to you.
- **Demon cult:** corruption is the resource. Cultist genes or precepts thrive on it; corrupted ground boosts demonic buildings; the hell gate (4.4) needs it to open; essence harvested on corrupted ground comes out as stronger **corrupted essence** (4.3).
- **The mirror enemy:** on a purist run, demons spread corruption at you. On a cult run, **zealot / inquisitor raids** come to purify your land with fire and holy water, burning away what you've cultivated. Same system, opposite threat.
- **Ideology:** a precept from *Corruption: abhorrent* to *Corruption: exalted* decides how colonists feel about it.

**What it ties together:** ghosts (4.8) rise more on corrupted ground; curses (4.2) come from high pawn corruption; demons (4.3) and the hell gate (4.4) feed on it; carrion walls (4.1) and volatile stockpiles (4.20) can leak it; the soul-eating xenotype (4.15) could be the cult-side endpoint.

**Tick cost:** additions are all one-off events (extraction, spending, impact, hit). Spread and shrine cleaning are one sampled batch per rare tick in a single MapComponent. Auras run on the rare tick only on hostile pawns that carry them. Benchmark with Dubs Performance Analyzer once it exists.

**Open decisions**
- Altar vs Press: which corrupts more overall?
- Does corruption ever decay on its own, or only through purification?
- Should the cult benefits need Ideology/Biotech, or have a no-DLC version?
- Mutiny clause: if spread is visually noisy or makes purist runs a chore, cut spread to only around sources and keep purification simple (fire + holy water).

### 4.29 The Weight of Names: soul grades and residue (from the third pirate think tank)
**v1 built in v0.7** except residue on top-tier recipes (none of those recipes exist yet).
*Every soul has a name, a grade and a cost. You choose who to take.* The pirate captain's pick; built alongside corruption so the depth happens at the altar, face to face with a named pawn.
- **Soul grades:** like art quality, from *Wretched* to *Exalted*. Set by the victim's skills and traits and by how it was taken (Bone Altar vs Soul Press, partial souls from a botched rite). A better grade gives more essence.
- **Residue:** each bound soul keeps one trait or top skill from its owner. It lives on the **vessel**, and passes to the **finished item** crafted from that vessel (weapons and armour never stack, so it costs nothing). A master smith's soul makes a finer blade; a pyromaniac's sparks. **Never on essence or soulsteel**: unique data on a resource stops it stacking, which floods the map with single-unit piles and costs hauling and rendering performance.
- **Vessels (4.30)** are the other road: bind a named, graded soul instead of rendering it. **Top-tier recipes** (dreadnoughts 4.9, reliquary armour 4.12) need a high-grade vessel as an ingredient.
- **v1:** grades, vessels, residue on one or two top-tier recipes. **Later:** residue on more crafts, a Ledger tab listing every name taken.
- **Build:** a ThingComp stamping grade and residue at extraction (stored with `PostExposeData`), quality-style display, a recipe check for high-grade jars. No ticking.
- **Worth building if:** playtesters actually stop and think at the altar about which pawn to take.
- **Mutiny clause:** if players render everything regardless of grade, scrap grades and let corruption carry the depth alone.

### 4.30 Render or bind: two ways to take a soul (decided 13 Oct 2026)
**Built in v0.7.** Decided: a soul better than a vessel's cap is cut down **to** the cap (not one grade); a broken vessel spills corruption (the ghost comes with 4.8).
Two paths with different jobs. Keeps essence fungible and puts names only where items are already unique.

**Render: cheap and nasty**
- Corpses at the crucible, the scythe's reap, battlefield harvesting.
- Gives plain **soul essence** that always stacks. No name, no grade, no residue: who they were is lost.
- Fast and in bulk. The dirtier route for corruption (4.28) per unit of essence.
- The corpse is left behind as a **husk** (4.1).

**Bind: the whole soul**
- **Living adult pawns only**, at the Bone Altar or Soul Press (4.27).
- The whole soul goes into a **vessel** that keeps the name, grade (4.29), residue and day taken. Survives save/load.
- **Binding kills.** The no-soul-equals-death rule holds; the body is left as a **husk** for the reanimation slab (4.6), carrion wall (4.1) or face bill (4.23).
- A vessel can be used in top-tier recipes, kept on display, or **smashed** for a large one-off lump of essence (more than rendering would have given).
- Replaces soul jars (4.17): the jar becomes the medieval vessel. Its named Tale, beauty and inspect text carry over.

**Three kinds of thing, kept separate**
- **Vessels** are who they were (named, graded).
- **Husks** are what's left (a soulless corpse; dead).
- **Thralls** (4.5) were never anyone (vat-grown, born without a soul; the only things alive without one).

**Why it adds depth**
- Capture is worth the risk: killing someone gets plain essence; downing and capturing them can get an Exalted soul. Reasons to hold the scythe back, fire carefully and take prisoners.
- Blood debts (4.18): a marked killer bound alive is the best vessel you'll ever get.
- Grades only matter where they're meaningful: at the moment you decide about a living, named person. Corpses are fuel.

**Performance:** essence stacks forever. Vessels are unique but rare (a living adult captive plus a ritual or machine each), so a colony holds dozens at most. A vessel stores a name, a grade and one trait or skill; nothing ticks; the description is built only on hover.

**Vessel tiers (decided 13 Oct 2026).** They differ in the highest grade they hold without damaging the soul, how safe they are, and what they're for.

| Tier | Vessel | Made at / unlocked by | Grade cap | Risk | Uses |
|---|---|---|---|---|---|
| Medieval | **Reliquary urn** (clay, bone or stone, sealed with wax and lead) | Crafting spot, no research | Up to *Fine*; anything better drops one grade when bound | Fragile: if damaged it cracks, the soul escapes as a ghost (4.8) and a burst of corruption (4.28) spills out | Dim, eerie decoration; carries the named Tale; smash for essence |
| Industrial | **Soul canister** (steel and glass pressure cell, gauges, faint glow) | Machining table, after Soul extraction | Up to *Masterwork*, no loss | Sturdy, but needs a **powered canister rack**; if power is out too long it vents corruption (one check on the rack when power state changes, no ticking) | Plugs into machines: prisoner battery (4.7), dreadnought cores (4.9) |
| Spacer | **Soul lattice** (grown soulsteel/plasteel crystal) | Fabrication bench, late research | **Any grade, including *Exalted*** (the only vessel that can) | Fully stable: no leaks, no power | Reliquary armour cheat-death core (4.12), dreadnought soul cores, later maybe residue into implants |

- **The progression:** early on you lose your best souls unless you wait; mid-game you keep them but depend on power; late game you hold anything safely. An Exalted prisoner early is a real dilemma: bind into an urn now and lose a grade, or keep them captive until lattices are researched.
- **Any vessel at either building** (Bone Altar or Soul Press); the altar's botched-rite risk still applies whatever the vessel.
- Naming: the spacer vessel is deliberately *not* a "soul gem", because The Profaned already uses that name (4.32).

### 4.31 Corpse grinder (industrial)
**Built in v0.7.** One corpse per 1000 ticks; 4 essence from an adult human soul (the crucible gives up to 8), 2 bales a human, meat and leather at 60%.
Bulk, automated, crude. The industrial-scale answer to a pile of raid corpses; the crucible stays the careful option.
- **Building:** large (about 3x3) powered machine, steel and components, after Soul extraction research.
- **No pawn works it** (same pattern as the line forge 4.24): haulers drop corpses in an input zone, it pulls them in, products drop on an output tile. Progress on the rare tick only while the hopper has something in it; nothing when idle.
- **Output:**
  - **Carrion bales** for corpse walls (4.1), skipping the "bind carrion" bill.
  - **Soul essence**, rendered and plain (4.30), **less per body than the crucible**.
  - **Meat and leather** (and bone if a mod adds it).
- **Animals accepted** for bales, meat and leather; no essence (no soul).
- **No husk:** the grinder eats the whole body. The one exception to the husk rule (4.1), on purpose: you trade reanimation and face-taking for throughput.
- **Corruption:** vents heavily around itself (4.28): the dirtiest way to take a soul.
- **Never children:** input filter uses `SoulUtility`'s age rule (the same special filter carrion bales need).
- **Grim:** awful beauty, mood penalty for pawns who see it running, constant grinding sound (Aaron to make; placeholder SoundDef).
- **Performance:** a net gain. Clears corpses that would otherwise lie around rotting, being hauled and rendered after every raid.
- The loop it closes: raid → grinder → bales into walls, essence into ammo, line forge arms the thralls → next raid.

### 4.32 Compatibility: The Profaned
**Built in v0.8.** Checked against the mod's own 1.6 files: revival runs through `Profaned.Utility_Profaned.BestowCurse`, the undead gene is `BotchJob_UndeadPsysiology`, the Cursebearer gene `BotchJob_Cursebearer`. Decided while building: no `Mods/Profaned` folder needed (all C#, nothing loads without it); the collapse kills the raised again rather than just weakening them.
Optional patch for *The Profaned* (Botch_Job; Biotech xenotype of partly decayed undead humans, hostile faction, Cursebearers who raise the dead, its own soul gems, wraiths and corrupted blood). Lives in `Mods/Profaned/`, loaded only when that mod is active via `LoadFolders.xml`, like the CE folder. Does nothing without it.
- **Profaned have souls, corrupted ones (decided 13 Oct 2026).** `SoulUtility` treats them as ensouled adults: they can be rendered or bound. Their essence comes out as **corrupted essence** (4.3) and they're comfortable on corrupted ground (4.28). A ready-made cult-side faction and a natural enemy for purist runs.
- **Cursebearers share their soul.** Raising a husk doesn't bring the dead person back; their soul is gone. The Cursebearer plants a shard of its own corrupted soul in the empty body (like reimplanting a xenogerm in Biotech). So the rule holds: the risen body has a soul, just not its own.
  - A shard is weak: always *Wretched*, a small amount of corrupted essence, residue taken from the **Cursebearer**, not the dead pawn.
  - **Optional twist:** binding or killing a Cursebearer weakens or collapses everything it raised. One check when that pawn dies; no ticking. A reason to hunt Cursebearers first in a Profaned raid.
- **Undead creatures** are animals: no souls, no change needed.
- **Optional extra:** Profaned corrupted blood and tainted bones as valid inputs for cult-side recipes.
- **Verify before building:** the xenotype def name, and how the Cursebearer revive actually works (Aaron remembers it working like gene implanting). Check the mod's own files; this section was written from its description page. CE support for The Profaned is unconfirmed by its author.
- Source: [The Profaned on TOP-Mods](https://top-mods.com/mods/rimworld/factions/12268-the-profaned.html), [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/comments/3202008037).

### 4.33 Scenarios (decided 13 Oct 2026)
**Built in v0.8 (four of five).** The Coven, Endless War, the Escapee and the Gravedigger. The Inquisitors wait for salt (the fallen idol is built); no precepts yet. The Gravedigger's battlefield part raises the ghost chance to 8% for that scenario only, so a handful of bodies actually threatens to rise. Decided while building: the Coven's captive is shackled (downed) for two days rather than starting in a cell; the Escapee's loved one is a spouse, or a lover if the colonist is already married.
Five small starts, each pointing at a different way to play.
1. **The Inquisitors (purist):** three zealots, holy water, salt, the purist precept. They land near a fallen idol (4.28 event) already leaking corruption; destroying it is the first job.
2. **The Coven (cult):** three cultists, a Bone Altar (4.27), one captive, the corruption-exalted precept. Start on corrupted ground, so no farming from day one: meat and leather only.
3. **The Gravedigger (solo):** one undertaker with the Reaper's scythe on an old battlefield littered with corpses. Every body is a ghost roll (4.8), so the clock is ticking.
4. **Endless War (industrial):** two overseers, three thralls (4.5), a line forge (4.24) and a corpse grinder (4.31). Frequent raids; you start partway into the loop.
5. **The Escapee (spacer):** a lone survivor of a soul-press facility carrying a soul lattice (4.30) holding someone they loved, hunted by the faction that ran it. Ties straight into the Weight of Names (4.29).
- Build: `ScenarioDef`s are XML. Starting buildings, captives and corrupted ground need small custom scenario parts (C#). The Escapee's starting lattice needs a pre-filled vessel with a generated name and relationship to the colonist.

### 4.34 Demon raids and sieges (decided 13 Oct 2026)
**Built in v0.8.** Decided while building: the Great Rift is its own incident (from day 40), not an arrival mode; burrowing picks the most corrupted of 60 sampled cells near the colony; sieges reuse vanilla's siege (canSiege on).
Arrival modes for demons (4.3) and, mirrored, zealots (4.28):
- **Standard:** walk in from the map edge.
- **Rift portals (the drop-pod version):** small portals tear open in or around the base; demons step out. Each leaves a scorch of corruption.
- **Burrow (the infestation version):** a crack opens, preferring corrupted ground; demons crawl out.
- **The Great Rift:** a huge hole opens and sends several waves over a few days, then collapses, leaving a crater and heavy corruption. Close it early with a cleansing rite or by killing its **rift keeper**. Similar in spirit to Anomaly's pit gate, but our own so it needs no DLC.
- **Sieges:** a camp with mortars.

**Mortar shells** (player can craft all three):
| Shell | Effect | Used by |
|---|---|---|
| **Soul shell** ("someone's tormented soul") | Soul shock (existing) plus a corruption splash (4.28) | Demon sieges, cult players |
| **Holy shell** ("a big ol' jug of holy water") | Purifies corruption in the blast and burns corrupted pawns, ghosts and corrupted thralls | Zealot sieges against cult runs, purist players |
| **Plain explosive** | Standard | Both |
- **CE:** mortars use CE's own shell ammo; generate soul and holy shell variants from CE's shells the same way `gen_soul_ammo.py` makes soul bullets.

**Performance:** waves and portals are one-off scheduled events; the Great Rift is a single building checking itself on the rare tick.

### 4.35 Reliquary, soul-bound items and shrines (decided 13 Oct 2026)
**Built in v0.8.** Decided while building: the bind bill is at the soul crucible; residue is matched back from its stored text (skill label or trait label) to a stat; a bound shrine takes its vessel on its rare tick. Not yet: ghost warding (needs 4.8) and the sculpture tale.
What to do with a bound vessel (4.30) besides recipes and smashing.

**Reliquary (display for a vessel)**
- A shrine-like case holding **one vessel**, usually a soul lattice. Shows the soul's name and grade on inspect; good beauty.
- Pawns who had a relationship with that person get a **"Remembered"** mood boost when near it.
- The Escapee scenario (4.33) starts with one: their loved one is the first thing they build a home around.
- No ticking: mood via a vanilla room/proximity thought, checked when vanilla checks thoughts.

**Soul-bound items: one bill, not variants**
- **No separate soul-bonded variants of each weapon or armour.** That doubles the defs and needs new art for every one.
- Instead, one **"bind soul into item"** bill that works on **any** weapon or apparel: Hollow Pawns', vanilla's, Odyssey's unique weapons, other mods' gear. Consumes a vessel and adds a ThingComp holding the soul (name, grade, residue, day taken).
- **Bonus:** grade sets the size, residue sets the kind (e.g. a pyromaniac's soul adds burn damage or fire resistance; a master shooter's adds accuracy). Applied through a StatPart, so it only does work when a stat is read; nothing ticks, unbound items cost nothing. Works under CE the same way.
- **Hover text:** *"Bound: Vex the Butcher (Masterwork soul; residue: Pyromaniac)"*.
- **Art:** binding a soul into a sculpture (or carving one with a vessel as an ingredient) uses a special TaleDef, so the generated description names them: *"A soulsteel figure of a woman weeping. Bound within is the soul of Vex the Butcher, taken at the Bone Altar on Day 61."*
- **Breaking a bound item** (destroyed, or deteriorated to nothing) releases the soul: a ghost roll (4.8) and a small corruption burst.

**Shrines: both can purify or corrupt**
| Shrine | Runs on | Mode | Strength |
|---|---|---|---|
| **Bound shrine** (permanent) | Consumes a whole vessel when built | Chosen once at building and fixed: **consecrated** (cleans) or **desecrated** (corrupts) | Radius and strength scale with the soul's grade. Named for the soul: "Shrine of Vex" |
| **Essence shrine** (fuelled) | Burns soul essence like fuel (refuelable comp) | Toggle: cleanse or spread | Fixed radius; works only while fuelled |
- **Destroying a bound shrine** frees the soul as a ghost with a burst of the **opposite** effect (a consecrated shrine's fall spills corruption).
- **The moral sting:** the strongest purist protection runs on a bound soul. The Inquisitors keep their land clean by doing the very thing they hunt. Ideology precepts could react to it.
- Both are also wards (4.8): ghosts can't cross a consecrated shrine's radius.
- **Tick cost:** both feed the corruption MapComponent's sampled-cell pass on the rare tick (4.28); no per-shrine ticking beyond the fuel comp's vanilla behaviour.
- This replaces the plain "shrine" line in 4.28's purification list.

### 4.36 Salt: lines, guns, and Medieval Overhaul (OPTIONAL, not decided)
**Status (13 Oct 2026): Aaron isn't fully sold. Keep as optional; nothing else in the mod should depend on salt.** Purification works without it (fire, holy water, rites, shrines). If it goes in, it needs the anti-cheese rules at the end of this section.

**Salt the resource**
- Our own salt item, from salt deposits or evaporating water at a salt pan (exact source to design).
- **Medieval Overhaul** already has salt. A `Mods/MedievalOverhaul/` patch folder (loaded only when MO is active, like the CE and Profaned folders) **swaps MO's salt into every Hollow Pawns recipe and hides ours**, so there's one salt in the game and MO players use their existing sources. **Check MO's salt def name in its files** before writing the patch.

**Salt lines: built like power conduits, floor only**
- Cheap building on the floor, links visually to neighbours like a conduit, walkable, blocks nothing, never ticks.
- **Ghosts** (4.8) can't cross them at all (their custom movement checks the cell).
- **Corruption** (4.28) won't spread past them.
- **Demons** (4.3): the pathfinder isn't taught to avoid salt for one faction (awkward with the 1.6 pathfinder). Instead a demon stepping on a line is **burned and breaks that section**: a ward that holds once, then needs relaying. Usable as a trap line on approaches.
- **Wear:** a line only wears while it's doing work (holding back heavy corruption, or broken by a demon). No weather decay, so no busywork.

**Why a purist actually needs them**
1. **Portals and burrows can't open near salt.** Rift portals and burrows (4.34) open inside or around the base, like drop pods and infestations. They **can't open within a few cells of a salt line or a consecrated shrine** (4.35). To keep demons arriving at the edge instead of in the bedroom, you run salt through the base: the same "network everywhere" feel as power conduits. Checked once, when the incident picks its spot.
2. **Corruption creeps.** Black rain patches, rift craters, demon corpses and fallen idols spread corruption outward. Farms and bedrooms without a salt border get eaten slowly. Lines are the firebreak.
3. **Wrathful ghosts** pass ordinary walls. Salted rooms are where people sleep safely.
4. **Demon assaults:** a salt line across the killbox burns the front rank and breaks, so it's cheap attrition you relay between waves.

**Cult mirror (optional): blood channels**
- The cult version of a conduit network: channels that carry corruption from sources (altar, grinder, desecrated shrine) to demonic buildings that need to be "connected", like power. Cultists get their own reason to lay lines everywhere. Design later; flagged as optional.

**Salt weapons**
- **Rock salt** is painful but not very lethal against ordinary people, and devastating against the unholy.
- **Against demons, insectoids and ghosts:** high damage plus a burning effect. Salt is one of the damage types allowed to hurt ghosts (4.8).
- **Against humans:** low damage, heavy pain, so it downs rather than kills: **the capture gun**, the way to take living prisoners for binding (4.30). Purists use it on demons, cultists use it to catch souls.
- **Tiers:** medieval salt arrows and a thrown salt pouch; industrial rock-salt shotgun; **CE:** a salt shell for CE's 12 gauge, generated from CE's own shells like the soul rounds.
- **Build:** one custom DamageDef with a worker that checks the target (demon gene, insectoid flesh type, ghost race) and scales damage. Runs only when a hit lands.

**Anti-cheese rules (required if salt goes in)**
- **Portal grid cheese:** a cheap salt grid over the whole map would stop portals and burrows opening anywhere, so the raid either always lands at the edge or fails entirely. Fix: if no valid spot exists, the portal **burns through**: it opens at the best spot anyway and destroys all salt in a radius. Salt can push demons around, never cancel them.
- **Containing the altar:** a salt ring around the Bone Altar or grinder would delete the corruption cost of harvesting. Fix: salt stops **spread**, never the **splash** a source drops on its own cells, and lines holding back a source **wear fast**, so containment costs a steady salt supply.
- **Free attrition:** a line across the approach burning every wave forever. Fix: modest burn, one burn per section before it breaks, relaying costs real salt and work.
- **Free salt:** if salt is effectively infinite, none of the costs above bite. Its source must be slow or limited.
- **Ghost-proofing everything:** fine to allow (ghosts are 1 in 100); don't balance around it.
- **Capture gun:** non-lethal downing is close to vanilla blunt weapons, so not a new exploit; pain-immune targets (demons, thralls, Profaned) shrug off the pain part.

## 5. Considered and dropped
- **Soul-drop on the requiem rifle** (kills dropped essence): dropped. The requiem is just a hard-hitting rifle; only the scythe reaps.
- **Soulless pawns going rogue instead of dying:** dropped. No soul = death.
- **Rescue work for thralls:** dropped.
- **Acoustics/noise mod:** rejected as tick-hungry for little payoff.
- **Woodpile/chopping block:** already covered by Medieval Overhaul.
- **Pickle mod:** already exists on the Workshop ([KD] Pickled Vegetables).
- **Three separate soul calibres under CE:** replaced by one shared soul charge (laser-style ammo sets).
- **Burning or plain piercing beams:** chosen instead: punch + one-cell blast + soul shock.
- **Ledger of the Unremembered** (bound-name weapons, a lectern reading the names, a mood hit if it burns): only the carved Tales were kept, inside soul jars.
