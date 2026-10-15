# Hollow Pawns v0.7

Every human has a soul. Take it from the dead at the soul crucible, or from the living on the operating table. They die without it.

## What's in v0.7

### Souls and essence
| Thing | What it does |
|---|---|
| **Soul** (body part) | Added to the Human body with 0 coverage, so damage can never hit it. Only adults' souls can be taken: children can't be operated on or reaped, and the crucible never takes their corpses. Animals, insects, mechanoids and alien races have no soul at all. |
| **Bone Altar** (Production, 2x1, stone, no research) | Mark a prisoner (render or bind) and a warden restrains them, carries them here and takes the soul. Slow (about 1.6 hours), 1 in 4 rites botched (a grade worse), witnesses are disturbed, heavy corruption close by. |
| **Soul Press** (Production, 2x2, 800W, research *Soul press*) | The same, faster (about 36 minutes), a grade better, never botched, nobody disturbed; vents less corruption per rite but over a wider area. |
| **Soul grades** | Every soul is wretched, poor, common, good, fine, masterwork or exalted, from the victim's best and top-three skills (passion counts). Rendering gives 6 to 20 essence by grade. |
| **Vessels** (bind) | A bound soul goes into the best empty vessel you have and keeps its name, grade, residue (a trait or top skill) and the day taken. **Reliquary urn** (crafting spot, stone): holds up to fine, fragile. **Soul canister** (machining table): up to masterwork; keep filled canisters on a powered **canister rack** or after a day without power they vent and the souls lose a grade. **Soul lattice** (fabrication bench, research *Soul lattice*): any grade, stable. |
| **Smashing a vessel** | Gives 1.5x what rendering would have. If a vessel is broken any other way, the soul gets out and corruption spills. Binding records a tale sculptors can carve. The old extract and bottle soul surgeries are gone. |
| **Soul crucible** (Production, 3x1, 250W, worked under **Crafting**) | *Harvest soul from corpse*: 1 human corpse → 8 essence fresh, 5 rotting, 2 desiccated, leaving a soulless husk behind for haulers. Husks are never hauled back to it. Colonist corpses are off by default in the bill filter. *Forge soulsteel*: 10 plasteel + 3 essence → 10 soulsteel. Also makes every soul weapon and soul ammo. |
| **Soulsteel** | Metallic stuff one tier above plasteel: tougher, sharper, lighter, non-flammable. Weapons, armour, walls, furniture. |
| **Soul generator** (Power, 2x2) | 1200W, burns 2 essence a day, holds 20. One fresh corpse runs it for 4 days. A breakdown vents a **soul burst** (radius ~3.9) at whoever is nearby. |
| **Volatile essence** | Fire or a heavy blow sets an essence stack off in a **soul burst**: barely a bruise, ignores armour, but enough soul shock to drop anyone in it, alive. Radius grows with stack size (1.5 + 0.03 per unit, ~3.8 for a full 75). Inspect warns "Volatile: spread your stock." |

### Drugs (drug lab, after Soul extraction and Drug production)
| Drug | What it does |
|---|---|
| **Soulfire** (2 essence, 1 neutroamine) | 12 hours: about 9 burning (+40% moving, +15% consciousness, +10% manipulation, no pain), then 3 crashing (-30% consciousness and moving). Highly addictive (about 3x go-juice). Withdrawal is **soul hunger**: pain, weakness, -25 mood, and a chance of berserk. |
| **Borrowed time** (4 essence, 2 neutroamine) | For a day no wound can kill the user: they go down instead. Each dose ages them a year. If the wounds are still lethal when it wears off, they die. |

### Curses
Debuffs that fade over 4 days (a fresh curse of the same kind resets the clock).
| Curse | Effect | Where it comes from |
|---|---|---|
| **Marked** | Takes 25% more damage | Demon hits (10% per damaging hit), purging corrupted essence |
| **Rotting luck** | Works 15% slower, breaks more easily | Same |
| **Soul-sick** | 30% hungrier, slower immunity, -5% consciousness | Same |
| **Hollowed** | -10% consciousness, breaks more easily | Wearing a **cursed idol** (no fade: lasts while worn) |

The **cursed idol** is belt-slot demon loot: +4 shooting and melee accuracy and +20% psychic sensitivity, at the price of being hollowed while it is worn. Arch-demons always carry one. The soul broker sometimes sells one.

### Demons (needs Biotech; lives in `Mods/Biotech`)
| Thing | What it does |
|---|---|
| **Demon** (xenotype) | Horned, deep red, red-eyed, tailed. Robust, fire-resistant, hyper-aggressive, strong in melee, reduced pain. Carries the **corrupted soul** gene. |
| **Demon host** (faction) | A permanent enemy built like the pirates, every member a demon: thralls, reavers, ravagers, brutes and arch-demons (vanilla raider gear). Raids like any enemy. Added to existing saves on load. |
| **Corrupted essence** | What a demon's soul gives (and any soul taken on deeply corrupted ground), from the altar, press, scythe, crucible or a demon's vessel. Worth 20. Volatile, more so than clean essence. *Purge corrupted essence* at the crucible: 5 corrupted → 10 clean, but the worker has a 1-in-3 chance of being cursed. The soul broker buys it. |
| **Hell gate** (Misc, 3x3; research *Hell gate* after Soul weaponry) | 150 soulsteel, 40 essence, 6 components. Open it and a demon raid (110% normal points) comes through every 3 days; close it to stop. Dev mode adds *Wave now*. Rare tick only, idle when closed. |
| **Hell Unbound** (storyteller) | Cassandra Classic plus a demon wave roughly every 12 days from day 15. Generated from the game's own Cassandra by `Source/tools/gen_storyteller.py`. |

### The soul broker
An orbital trader (comms console) and an Outlander caravan that buy and sell soul essence, soulsteel and soul drugs at a markup. The orbital broker also sells soul weapons; the caravan only buys them. Both buy filled soul vessels. Never pawns. Soul weapons and drugs have their own trade tags, so ordinary traders never stock them.

### Weapons
| Thing | What it does |
|---|---|
| **Reaper's scythe** (90 soulsteel, 20 essence, Crafting 8) | Blade hits roll to reap: 0.5% per Melee level (10% at Melee 20), ×3 against downed targets. A reaped target dies on the spot and drops 12 essence. |
| **Soul gun family** (see below) | Every shot ends in a **one-cell soul blast**: it takes the limb it lands on, craters the wall behind on a miss, and adds **soul shock**. Too small to splash the pawn next door. |
| **Soul shock** | Stacks with every soul blast hit, dragging consciousness down. Roughly three solid hits and the target collapses, alive, ready for capture and extraction. Fades in under a day. |

| Gun | Vanilla dmg / AP | Range | Burst | Cost (soulsteel / comps / essence) | CE mag |
|---|---|---|---|---|---|
| Vesper pistol | 20 / 0.35 | 26 | 1 | 40 / 2 / 10 | 12 |
| Whisper SMG | 16 / 0.30 | 22 | 4 | 55 / 3 / 15 | 30 |
| Dirge rifle | 22 / 0.40 | 31 | 3 | 65 / 4 / 20 | 30 |
| Requiem rifle | 30 / 0.55 | 39 | 1 | 70 / 5 / 25 | 10 |
| Last Rites sniper | 42 / 0.75 | 46 | 1 | 90 / 6 / 30 | 5 |
| Litany machine gun | 18 / 0.35 | 27 | 6 | 90 / 6 / 30 | 100 |
| Wake shotgun | 35 / 0.30 | 16 | 1 | 60 / 3 / 20 | 6 |

The table lives in `Source/tools/gen_soul_guns.py`. Change a number there and re-run it rather than editing the XML.

### The Reaper's Mark (quality of life)
Know which bodies still hold a soul, take one in a click, and never by accident. Nothing ticks: it all runs when you look or click.
| Thing | What it does |
|---|---|
| **Mark soul** (button on prisoners) | Render, bind, or clear. Wardens take marked prisoners to a free altar or press. Binding needs an empty vessel. |
| **Mark all** (button on the altar and press) | Marks every prisoner on the map whose soul is intact and who isn't Soul-Locked to be rendered. |
| **Soul-Lock** (button on prisoners) | The safety catch. A locked prisoner can't be marked, Mark all skips them, and locking clears any mark. |
| **Allow ensouled / allow soulless** | Corpse filters on stockpiles, graves and bills, like fresh / rotten. Send full corpses to a crucible stockpile and husks elsewhere. Children and soulless races count as soulless. |
| **Corpse hover line** | "Soul: intact (8 essence)" or "Soul: taken" on adult human corpses. |
| **Alerts** | *Ensouled corpse unclaimed*: non-colony corpses with a soul lying outside storage on a home map. *Soul generator low*: a generator with about a day of essence left. |

### Research
- **Soul extraction** (Industrial, after Electricity): crucible, generator, soulsteel, scythe, soul canisters and the canister rack.
- **Soul press** (after Soul extraction and Microelectronics), **Soul lattice** (after Soul press and Fabrication), **Reanimation** (after Soul extraction), **Industrial harvest** (after Soul extraction and Machining), **Clone vats** (Biotech, after Soul extraction).
- **Soul weaponry** (after Soul extraction and Gunsmithing): the gun family and soul ammo.

### Corruption (the backbone)
Taking and spending souls stains the ground and the people who do it. Toggle the overlay with the purple button among the map controls (bottom right).
| Thing | What it does |
|---|---|
| **Ground corruption** | Spilled by soul rites (altar heavy and close, press thin and wide), crucible harvests, soul generators while they burn, soul blasts and bursts, broken vessels, demons walking about and demon corpses lying around. Above 80 it creeps into neighbouring cells; above 120 plants wither; above 160 any soul taken there comes out as corrupted essence. A sample of cells is checked every 250 ticks: nothing ticks per cell. |
| **Soul corruption** (pawns) | Builds while a soul stands on corrupted ground, and on wardens who perform rites. Stages: unease, dread, sickening, consumed; at *consumed* it draws a curse about every 4 days. Fades slowly. Children, thralls and husks can't carry it; demons are immune. |
| **Tainted / corrupted ground** | -3 / -8 mood standing on it. |
| **Fire** | Burning cleans corrupted ground, along with everything else. |
| **Holy water** (crafting spot or drug lab: 5 silver + 1 herbal medicine → 4) | The *Cleanse corruption* order (Orders tab): drag over ground; cleaners sprinkle one holy water per small patch. Or drink it: -0.15 soul corruption. |
| **Purge soul corruption** (surgery, Medicine 5, 2 medicine) | Halves soul corruption at a stroke, with two days of pain. |
| **Hell gate** | Now needs corrupted ground to open (average 100 over and around it). |

### The endless war
| Thing | What it does |
|---|---|
| **Thralls** (Biotech; research *Clone vats*) | A **clone vat** (3x2, 400W): a crafter starts a culture (60 meat, 25 steel, 5 essence), the vat grows an adult thrall for 3 days and decants it in basic gear. Thralls are soulless: no pain, only food and rest needs (no mood, no breaks), never chat, and can only haul, fight fires and fight. They fail after about 30 days. |
| **Husks** (research *Reanimation*) | The **reanimation slab** (2x1, 300W, doctors) raises a soulless adult human corpse (a husk from the crucible or a rite) as a husk on your side: same body and gear, -30% moving, -20% manipulation, no pain, food and rest only, haul / firefight / fight only, dead in about 4 days. |
| **Stand guard** (button on thralls and husks) | Drafted in place, hunger at a tenth, no tiredness. They stand down on their own when hungry. |
| **Carrion** | *Bind carrion* at a butcher table or spot turns an adult human or animal corpse (never a child) into carrion bales: 2 from a human, 1 to 6 from animals by body size (the grinder uses the same rule). *Render carrion* turns a bale back into 46 human meat (a human corpse unbutchered is about 92), and a clone vat can start a thrall from 2 bales instead of 60 meat. **Carrion walls** (Structure, 2 bales) rot about 1% a day above freezing, leak rot stink, burn well and look awful. |
| **Corpse grinder** (3x3, 600W; research *Industrial harvest*) | Grinds corpses left next to it (make a stockpile there): carrion bales, 4 essence from an adult human soul, meat and leather. No worker, no husk, never children, heavy corruption. One corpse per 1000 ticks while powered. |
| **Line forge** (4x2, 500W; research *Industrial harvest*) | Turns steel and components left next to it into conscript kit, no worker: a conscript rifle (bayonet, poor accuracy, 5.56 under CE), conscript helmet and vest, or the full kit. Near-worthless, no quality. A *Stop at* setting pauses it once that many of each piece are lying around the colony. |

### Reliquaries, soul-bound items and shrines
| Thing | What it does |
|---|---|
| **Reliquary** (Furniture, stone, wood or metal) | A display case for one filled vessel. Inspect shows whose soul it keeps. Anyone who cared about that person when they were taken (family, lovers, anyone with a good opinion of them) gets **Remembered** (+6) within 8 cells. Only vessels bound from v0.8 on remember who loved them. |
| **Bind soul into item** (soul crucible) | One bill for **any** weapon or apparel, vanilla's or any mod's: uses up a filled vessel and binds that soul into the item. The soul's **residue** picks the gift (a Shooting soul: accuracy; Melee: hit chance; Construction, Mining, Cooking, Plants, Crafting, Intellectual: that work speed; Medicine: tend quality; Social: social impact; traits such as Pyromaniac: heat armour, Tough: less damage taken, Jogger: speed; anything else: psychic sensitivity) and the **grade** its size, from a seventh of the bonus (wretched) to all of it (exalted). The bearer gets it like any gear bonus. Hover: *Bound: Vex the Butcher (masterwork soul; residue: Pyromaniac)*. If the item is destroyed the soul gets out and corrupts the ground. |
| **Bound shrine** (Misc, 120 stone) | Haulers bring it a filled vessel and the soul goes into the stone for good. Before that, toggle **consecrate** (cleans corrupted ground) or **desecrate** (corrupts it); fixed once bound. Radius 4.9 to 13.3 and strength by grade. Named for the soul: *shrine of Vex*. Breaking it frees the soul in a burst of the **opposite** effect. |
| **Essence shrine** (Misc, research *Soul extraction*) | Burns about 3 essence a day; toggle **cleansing** or **spreading** corruption within 6.9 cells. Works only while fuelled. |

Shrines work inside the corruption map pass (a few sampled cells each time), so they cost almost nothing.

### Combat Extended (only loads with CE)
Lives in `Mods/CombatExtended`, wired up in `LoadFolders.xml`.
- **Soul guns fire beams.** Like CE's own lasers: one shared **soul charge** ammo (100 from 6 soulsteel + 4 essence, so about 200 shots per corpse), and each gun maps it to its own beam with its own damage and penetration, plus a one-cell soul blast on impact.
- **Soul-charged ammo for normal guns.** .45 ACP, .44 Magnum, 5.56 and 7.62 NATO, .303 British, 5x50mm caseless, 12 gauge slug and the four charged calibres each get a soul-charged variant: ×1.25 damage, ×1.5 sharp / ×1.25 blunt penetration, soul blast damage and a one-cell blast. Made at the crucible from 100 standard rounds + 2 essence. Any gun on those calibres, modded ones included, can load it. Generated from CE's own defs by `Source/tools/gen_soul_ammo.py <path to CE>`: re-run it after a CE update.
- Soulsteel: armour Sharp 2.4 / Blunt 3.6 vs CE plasteel 2 / 3, mass ×0.75, melee penetration ×1.4, CE's weapon and building material tags.
- CE versions of the scythe (still reaps) and every soul gun.

Tick cost: nothing new ticks except the generator, which ticks like the vanilla wood-fired generator, and soul shock fading like any other condition.

## Building the DLL (one time, and after any C# change)
The C# is in `Source/HollowPawns`. Most of the mod needs it. Without the DLL the mod will throw errors on load.

1. Install the .NET SDK (8 or later): https://dotnet.microsoft.com/download
2. In a terminal:
   ```
   cd Source\HollowPawns
   dotnet build -c Release
   ```
3. That writes `Assemblies\HollowPawns.dll`. It pulls RimWorld reference assemblies from NuGet (`Krafs.Rimworld.Ref`), so you don't need to point it at your RimWorld install.

## Install
Needs **Harmony** (brrainz.harmony). Copy the whole `HollowPawns` folder into `RimWorld\Mods`, enable it in RimSort, load it after Harmony, Core and the DLCs (and after Combat Extended if you use it). `Assemblies/` is committed, so building is only needed after a C# change.

## First test (dev mode on)
- [ ] No red errors on load. Check the log for anything mentioning `HP_`.
- [ ] Research both projects (dev mode: finish instantly). Check neither overlaps anything on the Main tab. If they do, change `researchViewX/Y` in `Defs/ResearchProjectDefs/HP_Research.xml`.
- [ ] Check a child prisoner: no *Mark soul* button, the scythe never reaps them, and the crucible, grinder and carrion bills never take a child's corpse.
- [x] ~~Extract soul surgery~~ (removed in v0.7; replaced by the altar and press).
- [ ] Put that corpse and a normal raider corpse near the crucible. Only the raider gets harvested.
- [ ] Forge soulsteel, then make a soulsteel longsword and a soulsteel wall.
- [ ] Give a Melee 20 colonist the scythe, fight a few downed prisoners: roughly 1 in 3 hits should reap ("Reaped" text, 12 essence).
- [ ] Shoot a raider with the Requiem and Last Rites: limbs should come off, and soul shock should show in their health tab and down them after a few hits.
- [ ] Miss on purpose into a wall: it should take a big chunk of damage, with some rubble. A colonist standing next to the target should be untouched.
- [ ] Build a generator, fill it, check 1200W on the power tab.
- [x] Load an old save: injuries and bionics on existing pawns should be where they were. (Passed, October 2026.)
- [ ] Reaper's Mark: *Mark soul* → render on a prisoner; a warden takes them to the altar. *Mark all* on the altar marks every eligible prisoner and skips a child and a Soul-Locked one.
- [ ] Soul-Lock a prisoner who is already marked: the mark clears and nobody comes for them.
- [ ] Harvest at the crucible: the corpse stays as a husk, hovering it says "Soul: taken", and nobody hauls it back to the crucible.
- [ ] Stockpile filters: a stockpile with *allow soulless* off rejects the husk.
- [ ] Leave a raider corpse outside storage: *Ensouled corpse unclaimed* appears. Let a generator run below 2 essence: *Soul generator low* appears.

**v0.5 batch** (dev mode makes most of these quick: spawn things, set rot, start fires, trigger breakdowns)
- [ ] Freshness: hover a fresh, a rotting and a desiccated raider corpse: "Soul: intact (8 / 5 / 2 essence)". Harvest each: that much essence drops by the crucible.
- [x] ~~Bottle soul and soul jars~~ (replaced by vessels in v0.7; see the v0.7 checks).
- [ ] Have a sculptor make art after a bottling: the tale can show up ("bottling the soul of ...").
- [ ] Volatile essence: set a stack of 75 on fire (dev: start fire) with a prisoner next to it: it bursts cyan, the prisoner drops from soul shock with only bruises. A single essence makes a small burst.
- [ ] Generator vent: dev "break down" a soul generator with a pawn next to it: soul burst, message, pawn shocked.
- [ ] Soulfire: craft at the drug lab, inject: "burning" stage with no pain and faster movement, then "crashing", then gone after ~12 hours. Dose a few times: addiction, then soul hunger when it lapses.
- [ ] Borrowed time: inject a colonist, then shoot them to death with a dev tool (or let a raid do it): they go down instead of dying. They are a year older. Check what happens when it wears off with lethal wounds (expected: they die).
- [ ] Soul broker: dev "trader arrives" with the soul broker: stocks essence, soulsteel, drugs and a few soul guns; offers to buy a filled vessel. Check vanilla traders no longer stock soul guns.

**v0.6 batch: curses, demons, the hell gate**
- [ ] Load the old save: the log says "[Hollow Pawns] Added the demon faction to this save", and the demon host shows in the factions tab, hostile to everyone.
- [ ] Dev: spawn a demon raid (Execute incident → *demon wave*, or a raid with the demon host). Demons have horns, red skin and tails, and carry vanilla raider gear; the arch-demon wears a cursed idol.
- [ ] Let a demon hit a colonist a few times: roughly 1 hit in 10 lays a curse, with a message. Curses show in the health tab and fade over 4 days.
- [ ] Capture a demon and render them at the altar: corrupted essence. Harvest a demon corpse: hover says "Soul: corrupted (8 corrupted essence)", and the crucible drops corrupted essence.
- [ ] Purge 5 corrupted essence at the crucible: 10 clean essence; roughly one batch in three curses the worker.
- [ ] Put the cursed idol on a colonist: *curse: hollowed* appears; take it off: gone. Accuracy goes up while worn.
- [ ] Set corrupted essence on fire: a bigger burst than clean essence.
- [ ] Research *Hell gate*, build one, open it: inspect shows the countdown. Dev *Wave now*: a demon raid arrives. Close it: "Closed." Save and reload with it open: the countdown survives.
- [ ] New game: *Hell Unbound* is in the storyteller list with its portrait; the demon host is in world generation.
- [ ] Research tab: *Hell gate* sits at (9, 6.1) under the soul projects without overlapping.

**v0.7 batch: extraction, corruption, the endless war**
- [ ] Build a Bone Altar. Mark a prisoner → render. A warden restrains and carries them to the altar, a rite runs (about 1.6 hours with a progress bar), they die, essence drops by grade, a message names the grade. Colonists watching get "watched a soul torn out".
- [ ] Craft an empty reliquary urn. Mark a skilled prisoner → bind. The warden fetches the urn first. A filled urn drops: inspect shows name, grade, residue and date; a soul better than fine says it was damaged. Save and reload: still there.
- [ ] Smash a filled vessel: 1.5x essence. Damage another until it breaks: "A soul vessel broke", and corruption appears there.
- [ ] Soul press: research it, power it, mark → render: faster, a grade better, no witness thought.
- [ ] Canister rack: put a filled canister on a rack, cut the power for over a day: canisters vent, grade drops, corruption spills. Lattice: research and craft one at the fabrication bench.
- [ ] Corruption overlay: the purple button in the bottom-right map controls shows corruption around the altar after a rite. Leave it a while: it creeps; plants wither on heavy patches.
- [ ] A colonist standing on heavy corruption picks up *soul corruption* and the ground mood penalty. Holy water: make some, use *Cleanse corruption* (Orders) over the patch: cleaners sprinkle it and it fades. Drink one: corruption goes down. Purge surgery halves it.
- [ ] Set fire to a corrupted patch: the corruption drops.
- [ ] Hell gate: on clean ground the Open button is disabled with the corruption figure; on corrupted ground it opens.
- [ ] Clone vat (Biotech): research, build, power, add *start thrall culture*: a crafter loads it, then the vat grows (inspect countdown; dev *Decant now*). A bald grey thrall appears in gear: only food and rest needs, no mood tab, works only hauling and firefighting, can be drafted. *Thrall lifespan* in health.
- [ ] Reanimation slab: harvest a raider at the crucible (husk), then *reanimate husk*: the husk stands up on your side, slow, with *reanimated husk* in health and only food and rest needs.
- [ ] *Stand guard* on a thrall: drafted in place; it shoots raiders in range; when hungry it stands down with a message.
- [ ] Bind carrion from a human (2 bales) and a chicken (1); render a bale into 46 human meat and cook it; start a thrall from bales at a clone vat.
- [ ] Bind carrion at the butcher table, build carrion walls: they lose hit points slowly when warm and leak rot stink; frozen ones don't.
- [ ] Corpse grinder: stockpile corpses next to it, power it: bales, meat, leather and a little essence appear at its front, corpses vanish one at a time, heavy corruption. A child corpse is left alone.
- [ ] Line forge: choose *full conscript kit*, drop 100 steel and a component next to it: after a while a conscript rifle, helmet and vest appear. Under CE the rifle takes 5.56.
- [ ] Line forge *Stop at 5*: it pauses (inspect says *paused*) once 5 of each piece are on the map, and starts again when colonists equip some.
- [ ] With CE: compare a soulsteel and a plasteel armour piece; make soul charges and check each soul gun fires cyan beams with blasts; soul-charge some 5.56 and load it into an assault rifle.

**v0.8 batch**
- [ ] Reliquary: bind a prisoner who has a friend or relative in the colony, put the vessel in a reliquary: inspect names the soul; the friend standing near it gets *remembered*. Someone who didn't know them doesn't.
- [ ] Bind soul into item: at the crucible, bind a vessel with a Shooting residue into a rifle: label says *soul-bound*, hover names the soul and the bonus, and the wielder's shooting accuracy shows the extra line from gear. Try a piece of apparel and a modded weapon too. Destroy a bound item with a dev tool: message and corruption. The bill never offers an already bound item.
- [ ] Bound shrine: build one on corrupted ground, leave it consecrated, let a vessel be hauled in: the vessel disappears, the shrine is renamed *shrine of ...*, the overlay shows the corruption fading around it over a few days. Build a desecrated one elsewhere: corruption grows. Break one: a burst of the opposite.
- [ ] Essence shrine: refuel with essence, toggle cleansing / spreading, watch the overlay; with no fuel it stops.

## Things I'm least sure of (from memory, not checked against 1.6)
- The vanilla (non-CE) soulsteel numbers are set from memory of plasteel's. The CE numbers are taken from CE's own patch file.
- Research positions were checked against Core, Ideology and Biotech only. Royalty, Anomaly or Odyssey might put something at (7, 5.5) or (8, 5.5) on the Main tab.

- v0.8, unverified until tested in game: adding the soul-bound comp to every weapon and apparel def at startup (`SoulBoundInjector`); the bind bill handing back the same item (the item is despawned inside `GenRecipe.MakeRecipeProducts` and skipped by `ConsumeIngredient`); the bound shrine's storage filter being cleared after binding; `Building_Storage` subclasses with `maxItemsInCell` 1 as single-vessel holders.
- v0.7, unverified until tested in game: the rite job (carrying a restrained prisoner and laying them on the altar cell; vanilla `Toils_Haul.StartCarryThing` on a pawn), `Building_CloneVat` re-implementing `IBillGiver.CurrentlyUsableForBills` (vanilla's isn't virtual), `ResurrectionUtility.TryResurrect` on rotted husks, the corruption overlay (`CellBoolDrawer`) and its toggle among the play settings, `DataExposeUtility.LookByteArray` for the corruption grid, thralls generated with a forced xenotype that has `canGenerateAsCombatant` false, CE armour values for the conscript vest and helmet (set by eye below CE's steel flak vest).
- v0.6, unverified until tested in game: adding a faction to an existing save with `FactionGenerator.NewGeneratedFaction` (it will have no world bases in that save, and Ideology must give it an ideoligion); the storyteller firing an incident whose `baseChance` is 0 through an on/off cycle; demon pawn kinds inheriting from non-abstract vanilla kinds (`ThrasherBase`, `PirateKindBase`, `MercenaryHeavyBase`); `CompProperties_CauseHediff_Apparel` with no body part.
- v0.5, unverified until tested in game: `HediffDef.preventsDeath` (no vanilla XML uses it; the game code reads it in `ShouldBeDead`/`CheckForStateChange`), the soul jar surviving minify and save/load, and the soul broker caravan patch on the abstract `OutlanderFactionBase`.

If any of these is wrong, the log will name it.

### Fixed from the v0.7 load log
- **Clone vat work loop.** While a vat was growing, workers took its bill and dropped it ten times a tick. `WorkGiver_DoBill` only asks `UsableForBillsAfterFueling`, and only the job driver asks `CurrentlyUsableForBills`. The vat now refuses on both while growing.
- Demon faction: on old saves it was added and then forced hostile with `SetRelationDirect`, which errors for goodwill factions (one error per faction). It's `permanentEnemy`, so the generator already makes it hostile; the loop is gone.
- **Line forge sat idle**: nothing brought it steel unless you made a stockpile beside it. Added a **material hopper** (steel and components) and a **corpse hopper** (for the grinder), vanilla-style storage at Important priority, so haulers feed both machines. The first version was never filled: a storage building that isn't standable needs `surfaceType` Item, or the game treats its cell as blocked.
- **Corpse grinder threw on every corpse.** It asked `Corpse.ButcherProducts` for meat and leather with no butcher, and that method puts blood at the butcher's feet and records a butchered-human event for them. It now takes the products from the inner pawn and leaves the blood at its output. Side effects: grinding a human doesn't count as butchering one (no ideology reaction), and mods that hook `Corpse.ButcherProducts` (e.g. the Mechanitor one) don't add to the grinder's output.
- Clone vat, corpse grinder and line forge are pass-through (slow), not impassable. Conscript rifle, helmet and vest aren't smeltable. Thralls have a will and resistance range (the game requires one for humanlike kinds).

### Fixed from the v0.6 load log
- **Hell Unbound broke world generation.** The generated storyteller copied Cassandra's comps but not the population and adaptation curves she inherits from `BaseStoryteller`, so every pawn generated with it selected threw. It now has that parent. The demon wave's `minSpacingDays` (4) was longer than its 3-day window and is gone.
- Essence stacks: vanilla `CompExplosive` needs every stack to tick every frame. Replaced by `CompVolatileEssence`, which bursts at once on fire damage, at half hit points (60% for corrupted) or when destroyed. There is no wick any more, so a fire bursts a stack the moment it reaches it. Unverified in game.
- Hell gate is now pass-through (slow), not impassable; cursed idol isn't smeltable; the arch-demon has a starting resistance (40-60).

### Verified (October 2026, against RimWorld 1.6.4871 and CE `3b38e8d`)
Every def reference, abstract parent and field name in the mod was cross-checked against the game's own Core/Ideology/Biotech XML and CE's Defs and Patches, and every API the C# calls was checked by reflection against the 1.6 `Assembly-CSharp.dll`.
- `Krafs.Rimworld.Ref` 1.6.4871 restores from NuGet and the DLL builds with 0 warnings.
- All of these exist in Core: `SurgeryFlesh`, `Cremate` / `Smelt` effects, `Recipe_Cremate` / `Recipe_Smelt` sounds, `AllowCorpsesColonist`, `BaseMeleeWeapon_Sharp_Quality`, `BaseHumanMakeableGun`, `BaseBullet`, `Shot_ChargeRifle`, `Interact_Rifle`, `buildingDamageFactorImpassable`, `explosionCellFleck`. No hidden DLC dependency.
- Fixed: soulsteel used `soundImpactStuff`, which isn't a field. Now `soundImpactBullet`, as plasteel does.
- Fixed (found on first load): 4 soul-charged bullets (12 gauge slug and three charged calibres) had no graphic, and some lost CE's own speed, casings and mass, because the generator made siblings of CE's rounds instead of copies. `gen_soul_ammo.py` now clones CE's ammo and bullet defs and only overrides the soul fields. Soul rounds also cook off as soul rounds now. Generated from CE workshop item 2890901044.
- CE laser beams do blast: `LaserBeamCE.Impact` → `BulletCE.Impact` → `ProjectileCE.Impact`, which runs `CompExplosiveCE` on both hits and misses. No need for the `BaseBulletCE` fallback.

## Roadmap
**Weapons**
- Lower-tech soul weapons (to brainstorm): neolithic and medieval tiers.
- Unique soul armour: reliquary armour (burns stored essence to cancel a killing blow), reaper's shroud (raises the scythe's reap chance).

**Longevity**
- Medieval: fountain of youth, fed with essence.
- Industrial/spacer: soul cradle, a bio-pod style machine.

**Infrastructure and later**
- Soul vats and pipes via Vanilla Expanded Framework PipeSystem (Thing to resource, Resource storage, Resource to power).
- Soul mechs (gestator recipes using essence).
- Ideology reactions, soul-eating xenotype.
- Blood debts (torment, vengeance effigies and the wailing effigy), building on vessels.
- Curse ritual on a prisoner (the third curse source); demon-specific art.
- Full-kit apparel sets (light and heavy for medieval, industrial and spacer) that fill every slot, with CE webbing/backpack/shield pieces when CE is loaded.
- Flayed face masks, from corpses or living prisoners, named like vessels.
- Trenches that give real cover (all-round in vanilla, lower profile under CE); check the existing Frontline – Trenches mod first.
- Thrall performance for big bases: a lean thrall think tree and thrall racks that store them in suspension at near-zero cost.
- Ghosts, the prisoner battery, reliquaries and shrines, scenarios, demon portals and siege shells, Ideology corruption precepts and the cleansing rite.

The full plan, with design decisions, is in `ROADMAP.md`.
