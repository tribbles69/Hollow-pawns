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

## 2. Built (v0.3.1)

| Feature | Notes |
|---|---|
| Soul body part | On the Human body, appended last in the body tree (save-safe), coverage 0. |
| Extract soul surgery | Medicine 6. 12 essence, soul marked missing, patient dies. Violation on prisoners. |
| Soul crucible | Corpse → 8 essence (corpse destroyed); soulless husks never hauled to it; colonist corpses off by default. Forges soulsteel; makes all soul weapons and ammo. |
| Soul essence, soulsteel | Soulsteel = 10 plasteel + 3 essence, one tier above plasteel. CE armour 2.4 / 3.6 vs plasteel 2 / 3. |
| Soul generator | 1200W on 2 essence/day. |
| Reaper's scythe | Reap chance 0.5% per Melee level, ×3 vs downed. Kills, drops 12 essence. |
| Soul gun family | Vesper pistol, Whisper SMG, Dirge AR, Requiem marksman, Last Rites sniper, Litany MG, Wake shotgun. One-cell soul blast on every shot: takes limbs, craters walls on a miss, no splash to neighbours. |
| Soul shock | Stacking consciousness debuff from soul blast damage; downs at high severity, fades in under a day. |
| Research | Soul extraction (after Electricity); Soul weaponry (after Soul extraction + Gunsmithing). |
| CE | Soul guns fire CE laser beams on shared soul charges (one ammo, per-gun ammo sets). Soul-charged variants of 11 CE calibres used by vanilla guns. CE stats for scythe, guns, soulsteel. |

---

## 3. Immediate to-do (before any new features)

1. **Build the DLL** (`dotnet build -c Release` in `Source/HollowPawns`). It couldn't be compiled where it was written (no NuGet access), so expect a few compile fixes against the 1.6 reference assemblies.
2. **First in-game test** using the checklist in README.
3. **Verify the uncertain defs** listed in README (vanilla parent names, sound and effect names, Bomb-style damage fields, `SurgeryFlesh`, `AllowCorpsesColonist`).
4. **CE check:** confirm CE's laser beam class runs `CompProperties_ExplosiveCE` on impact. If beams hit but never blast, switch the beam parent from `LaserBulletBlue` to `BaseBulletCE` in `gen_soul_guns.py`.
5. **Research tree positions:** `researchViewX/Y` are placeholders; move them so they don't overlap.

---

## 4. Planned features

Rough agreed order: **curses → demons → hell gate → thralls and clone vats → prisoner battery → ghosts → dreadnoughts**, with the smaller items slotted in wherever convenient. Each one sets up the next.

Added after the second pirate think tank: **soul jars → blood debts** (they build on each other), with the soul broker and volatile stockpiles as small fill-in items. The **Reaper's Mark** QoL package (4.21) can go in any time: it only needs what's already built.

### 4.1 Corpse wall (carrion wall)
Outdoor wall made from a pile of corpses, human and animal. Goes with the StarCrete/hemocrete idea.
- **Building it:** wall costs can't reference "any corpse" (every species has its own corpse def). So: a **"bind carrion"** bill at the butcher spot/table turns any corpse into a **carrion bale** (stackable resource); walls cost bales. XML. **Exclude child corpses** (needs a special filter, like `SpecialThingFilterWorker_Soulless`).
- **Constant rot:** very negative beauty; continuously emits rot stink gas; loses HP over time when above freezing; burns well.
- **Tick cost:** one MapComponent working through a sampled batch of carrion walls, not a comp per wall.
- **Open decisions:**
  - Should frozen walls **stop** rotting entirely, or only **slow** down?
  - **Husk change:** should the crucible leave a soulless corpse behind instead of destroying it? Then: harvest the soul → reanimate or bale the husk → nothing wasted. Small C# change (recipe worker marks the corpse soulless instead of consuming it). Strongly suggested, because it also feeds reanimated husks (4.6).

### 4.2 Curses
Debuff conditions: e.g. *Hollowed*, *Marked*, *Rotting luck*, *Soul-sick*.
- Hediffs are pure XML.
- Sources: demon attacks (4.3), cursed loot (a relic that curses its wearer), a curse ritual cast on a prisoner.
- Delivering them as an ability needs Royalty or a little C#.

### 4.3 Demons
Same soul mechanics, corrupted.
- **Demon xenotype** (Biotech genes: horns, skin colour, toughness, fire immunity). XML.
- **Hostile demon faction** using the xenotype. XML.
- **Corrupted soul:** extracting from a demon gives **corrupted essence**, a second resource: stronger but dangerous to use (side effects, curses). Small branch in `SoulUtility` keyed off the demon gene.
- Custom bodies would need Humanoid Alien Races as a dependency, so start with the xenotype.

### 4.4 Hell gate
Demons arriving in waves. Two flavours, both wanted:
- **Storyteller mode:** a custom storyteller that sends demon waves. Mostly XML.
- **A gate you build:** a structure that pulls demon waves on a timer. A risk-for-reward way to farm corrupted souls. Small C#, rare-tick only.

### 4.5 Thralls and clone vats (endless war)
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
Emergency meat shields. Chosen over true Biotech mechs, because true mechs must be mechanoid flesh type and need new art from every direction.
- **Reanimation slab:** takes a **soulless corpse** + steel and resurrects that exact body as a husk. Same Soulless behaviour as thralls, plus bad stats, slow movement and a short lifespan (a few days) before they collapse.
- No new art: keeps the body's own look and gear.
- Never child corpses.
- C# to spawn/resurrect from the bill.

### 4.7 Prisoner battery
A casket-style pod (like a cryptosleep casket). Lock a prisoner in: it generates power while slowly draining their soul. When the soul runs out they die and drop essence. Adults only. Small C# on top of vanilla's casket.

### 4.8 Ghosts
Corpses left unharvested too long can rise as a hostile ghost: hard to hit, with a dread aura. Ties into the essence loop: harvest your dead or they come back. Needs a custom pawn and art (bigger job).

### 4.9 Soul dreadnoughts
Huge Biotech-style mechs grown at the gestator from soulsteel and essence. They run on essence instead of power; possibly need a soul core made from a captured prisoner. Code is moderate; **the real cost is art** (all directions).

### 4.10 Longevity
- **Fountain of youth** (medieval, unpowered): stone font fed with essence. A pawn bathes (job or recreation) and loses a year or two of biological age per dose, with a chance to cure old-age conditions (bad back, frailty, cataracts). One C# hook to set the age.
- **Soul cradle** (spacer): sealed bio-pod running multi-day cycles on essence. Bigger age reversal, guaranteed chronic-condition removal, maybe regrowing lost parts. Model the cycle and occupancy on Ideology's biosculpter.

### 4.11 Drugs (overcharge)
Mostly XML (vanilla drug system handles highs, tolerance and addiction).
- **Soulfire:** short burst of speed, consciousness and pain immunity, then a hard crash. High addiction; withdrawal called "soul hunger" (sets up the soul-eating xenotype).
- **Borrowed time:** for a day the pawn can't die from injuries (downed instead), but each use ages them a year.

### 4.12 Unique armour
- **Reliquary armour:** soulsteel set with an essence reservoir. When a hit would kill the wearer, it burns stored essence to cancel the death and leave them downed; then it needs refilling. Small C#, only checked on a lethal hit.
- **Reaper's shroud:** hooded cloak that raises the scythe's reap chance; stacks with Melee skill.

### 4.13 Lower-tech soul weapons (to brainstorm further)
- Neolithic: soul-dipped arrows and javelins (vanilla bows have no ammo, so a soulsteel bow; under CE a real arrow type). A bone fetish charm that makes the wearer harder to reap.
- Medieval: soulsteel blades already work via stuff. Unique: a **wraith lantern**, a carried light that frightens nearby enemies into fleeing (vanilla flee mechanic).

### 4.14 Soul vats and pipes
Like Dubwise's Rimefeller / Dubs Bad Hygiene. Use the **Vanilla Expanded Framework PipeSystem** (hard dependency on VEF): Thing to resource (extractor), Resource storage (vats), Resource to power (generator by pipe), Resource processor (soulsteel), Refill building with pipes (weapons or turrets). Writing a pipe network from scratch would be the biggest C# job in the mod: don't.

### 4.15 Soul-eating xenotype (left for later)
Sanguophage-style, eats souls. Needs Biotech.
- Gene and xenotype defs: XML. A **soul hunger** resource bar like hemogen: small C# gene class modelled on vanilla hemogen (check VEF's genes module first). Eating soul essence tops it up (small ingestion hook). A soul-drain ability like the bloodfeed bite.
- Design clash to resolve: a full drain kills (no soul = death), so a pure version must keep hunting. Suggested middle ground: a drain takes part of the soul, which slowly regrows; only a full drain kills. Gives a prisoner-farming loop and a reason not to get greedy.

### 4.16 Smaller ideas
- Essence yield by corpse freshness (rotten and desiccated corpses give less). Small C#.
- Ideology reactions: treat soul harvesting like organ harvesting (horrified vs approving precepts).

---

### 4.17 Soul jars (from the pirate think tank)
A second option at the extract-soul surgery: **render** the soul into 12 essence as now, or **bottle it whole** as a named soul jar.
- The jar stores the victim's name, faction, backstory and the day taken, shown on inspect. Must survive save/load.
- Dim glowing decoration with good beauty: a shelf of the damned.
- Smash it later for the same 12 essence, so bottling never costs anything.
- Each bottling records a Tale that sculptors can carve.
- Build: one ThingDef with a single-facing texture, a ThingComp storing the name (`PostExposeData`), a recipe worker for the second surgery option, a TaleDef. No ticking. Adults only via `SoulUtility`.
- Conditions: search the Workshop for an existing soul-jar mod first; prove the name survives save/load before making art.
- Mutiny clause: if playtests show everyone smashes their jars, cut the decoration and keep only the named Tale. (The blood-debt line below gives jars a stronger reason to exist.)

### 4.18 Blood debts: revenge on whoever killed your colonists
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
- First, a **market-value pass** on essence (currently 12), soulsteel, jars and soul weapons.
- Then traders: an orbital **soul broker** (comms console) and an outlander caravan variant that buy and sell essence, soulsteel and soul weapons at a markup. **Never pawns.** Settlement trade requests for essence via the vanilla quest system.
- Check first whether existing traders already buy essence (it sits in the Manufactured category).

### 4.20 Volatile stockpiles (small)
Hoarded essence is dangerous cargo, so players spread their stock out.
- Essence stacks get vanilla `CompExplosive` (like chemfuel): fire or a hit sets a stack off, radius scaling with stack size up to a cap, using **soul blast** damage (already applies soul shock), so pawns drop alive.
- A soul generator breakdown vents a short soul shock pulse at whoever's nearby.
- Under CE: soul-charged rounds cook off in fires, if CE's cook-off system accepts custom ammo (unverified).
- Inspect text warns "Volatile: spread your stock."
- Tick cost: none. Item explosions only run when triggered, and the vent fires on the breakdown event.

### 4.21 The Reaper's Mark (quality of life, from the QoL think tank)
*Know which bodies still hold a soul, take one in a click, and never by accident.* Everything in v1 works with what's already built. Nothing ticks per item.

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
