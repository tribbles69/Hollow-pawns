# Hollow Pawns v0.6

Every human has a soul. Take it from the dead at the soul crucible, or from the living on the operating table. They die without it.

## What's in v0.6

### Souls and essence
| Thing | What it does |
|---|---|
| **Soul** (body part) | Added to the Human body with 0 coverage, so damage can never hit it. Only adults' souls can be taken: children can't be operated on or reaped, and the crucible never takes their corpses. Animals, insects, mechanoids and alien races have no soul at all. |
| **Extract soul** (surgery) | Needs Medicine 6; uses no medicine. Drops 12 soul essence, marks the soul missing, kills the patient. Counts as a violation on prisoners and guests. |
| **Bottle soul** (surgery) | The same, but the soul goes whole into a **soul jar** instead of loose essence. |
| **Soul jar** | Comes out boxed; install it as decoration (beauty 20, dim cyan glow). Remembers who it was: "soul jar (Vex the Butcher)", with their title, faction and the date on inspect. *Smash jar* renders it into 12 essence, so bottling costs nothing. If a raid or fire breaks it, the essence spills out. Bottling records a tale sculptors can carve. |
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
| **Corrupted essence** | What a demon's soul gives, from surgery, scythe, crucible or a bottled demon's jar. Worth 20. Volatile, more so than clean essence. *Purge corrupted essence* at the crucible: 5 corrupted → 10 clean, but the worker has a 1-in-3 chance of being cursed. The soul broker buys it. |
| **Hell gate** (Misc, 3x3; research *Hell gate* after Soul weaponry) | 150 soulsteel, 40 essence, 6 components. Open it and a demon raid (110% normal points) comes through every 3 days; close it to stop. Dev mode adds *Wave now*. Rare tick only, idle when closed. |
| **Hell Unbound** (storyteller) | Cassandra Classic plus a demon wave roughly every 12 days from day 15. Generated from the game's own Cassandra by `Source/tools/gen_storyteller.py`. |

### The soul broker
An orbital trader (comms console) and an Outlander caravan that buy and sell soul essence, soulsteel and soul drugs at a markup. The orbital broker also sells soul weapons; the caravan only buys them. Both buy soul jars. Never pawns. Soul weapons and drugs have their own trade tags, so ordinary traders never stock them.

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
Know which bodies still hold a soul, take one in a click, and never by accident. Appears once *Soul extraction* is researched. Nothing ticks: it all runs when you look or click.
| Thing | What it does |
|---|---|
| **Mark soul** (button on prisoners) | Toggle: queues the extract soul surgery, or cancels it. Select several prisoners to mark them all. |
| **Mark all** (button on the crucible) | Marks every prisoner on the map whose soul is intact and who isn't Soul-Locked. |
| **Soul-Lock** (button on colonists, slaves and prisoners) | The safety catch. A locked pawn can't be marked, extract soul isn't offered on them, and locking cancels anything already queued. |
| **"Are you sure?"** | Queuing extract soul on one of your own (colonist or slave) asks first. |
| **Allow ensouled / allow soulless** | Corpse filters on stockpiles, graves and bills, like fresh / rotten. Send full corpses to a crucible stockpile and husks elsewhere. Children and soulless races count as soulless. |
| **Corpse hover line** | "Soul: intact (8 essence)" or "Soul: taken" on adult human corpses. |
| **Alerts** | *Ensouled corpse unclaimed*: non-colony corpses with a soul lying outside storage on a home map. *Soul generator low*: a generator with about a day of essence left. |

### Research
- **Soul extraction** (Industrial, after Electricity): crucible, generator, soulsteel, surgery, scythe.
- **Soul weaponry** (after Soul extraction and Gunsmithing): the gun family and soul ammo.

### Combat Extended (only loads with CE)
Lives in `Mods/CombatExtended`, wired up in `LoadFolders.xml`.
- **Soul guns fire beams.** Like CE's own lasers: one shared **soul charge** ammo (100 from 6 soulsteel + 4 essence, so about 200 shots per corpse), and each gun maps it to its own beam with its own damage and penetration, plus a one-cell soul blast on impact.
- **Soul-charged ammo for normal guns.** .45 ACP, .44 Magnum, 5.56 and 7.62 NATO, .303 British, 5x50mm caseless, 12 gauge slug and the four charged calibres each get a soul-charged variant: ×1.25 damage, ×1.5 sharp / ×1.25 blunt penetration, soul blast damage and a one-cell blast. Made at the crucible from 100 standard rounds + 2 essence. Any gun on those calibres, modded ones included, can load it. Generated from CE's own defs by `Source/tools/gen_soul_ammo.py <path to CE>`: re-run it after a CE update.
- Soulsteel: armour Sharp 2.4 / Blunt 3.6 vs CE plasteel 2 / 3, mass ×0.75, melee penetration ×1.4, CE's weapon and building material tags.
- CE versions of the scythe (still reaps) and every soul gun.

Tick cost: nothing new ticks except the generator, which ticks like the vanilla wood-fired generator, and soul shock fading like any other condition.

## Building the DLL (one time, and after any C# change)
The C# is in `Source/HollowPawns`. It is needed for the surgery (the kill), the corpse filter and the scythe's reap. Without the DLL the mod will throw errors on load.

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
- [ ] Check a child prisoner: no *extract soul* option, the scythe never reaps them, and the crucible won't take a child's corpse.
- [ ] Spawn a prisoner, queue *extract soul*. They die, 12 essence drops, the death message mentions the soul.
- [ ] Put that corpse and a normal raider corpse near the crucible. Only the raider gets harvested.
- [ ] Forge soulsteel, then make a soulsteel longsword and a soulsteel wall.
- [ ] Give a Melee 20 colonist the scythe, fight a few downed prisoners: roughly 1 in 3 hits should reap ("Reaped" text, 12 essence).
- [ ] Shoot a raider with the Requiem and Last Rites: limbs should come off, and soul shock should show in their health tab and down them after a few hits.
- [ ] Miss on purpose into a wall: it should take a big chunk of damage, with some rubble. A colonist standing next to the target should be untouched.
- [ ] Build a generator, fill it, check 1200W on the power tab.
- [x] Load an old save: injuries and bionics on existing pawns should be where they were. (Passed, October 2026.)
- [ ] Reaper's Mark: *Mark soul* on a prisoner adds the surgery bill; toggling off removes it. *Mark all* on the crucible marks every eligible prisoner and skips a child and a Soul-Locked one.
- [ ] Soul-Lock a prisoner who is already marked: the bill disappears and extract soul is no longer in their operations list.
- [ ] Queue extract soul on a colonist from the health tab: a confirmation appears; cancel adds nothing, confirm adds the bill.
- [ ] Harvest at the crucible: the corpse stays as a husk, hovering it says "Soul: taken", and nobody hauls it back to the crucible.
- [ ] Stockpile filters: a stockpile with *allow soulless* off rejects the husk.
- [ ] Leave a raider corpse outside storage: *Ensouled corpse unclaimed* appears. Let a generator run below 2 essence: *Soul generator low* appears.

**v0.5 batch** (dev mode makes most of these quick: spawn things, set rot, start fires, trigger breakdowns)
- [ ] Freshness: hover a fresh, a rotting and a desiccated raider corpse: "Soul: intact (8 / 5 / 2 essence)". Harvest each: that much essence drops by the crucible.
- [ ] Bottle soul on a prisoner: they die, a boxed soul jar drops, named after them. Install it: it glows, inspect shows name, title, faction and date. Save, reload: the name is still there.
- [ ] *Smash jar*: confirmation, then 12 essence. Damage another jar until it breaks: 12 essence spills out.
- [ ] Have a sculptor make art after a bottling: the tale can show up ("bottling the soul of ...").
- [ ] Volatile essence: set a stack of 75 on fire (dev: start fire) with a prisoner next to it: it bursts cyan, the prisoner drops from soul shock with only bruises. A single essence makes a small burst.
- [ ] Generator vent: dev "break down" a soul generator with a pawn next to it: soul burst, message, pawn shocked.
- [ ] Soulfire: craft at the drug lab, inject: "burning" stage with no pain and faster movement, then "crashing", then gone after ~12 hours. Dose a few times: addiction, then soul hunger when it lapses.
- [ ] Borrowed time: inject a colonist, then shoot them to death with a dev tool (or let a raid do it): they go down instead of dying. They are a year older. Check what happens when it wears off with lethal wounds (expected: they die).
- [ ] Soul broker: dev "trader arrives" with the soul broker: stocks essence, soulsteel, drugs and a few soul guns; offers to buy a soul jar. Check vanilla traders no longer stock soul guns.

**v0.6 batch: curses, demons, the hell gate**
- [ ] Load the old save: the log says "[Hollow Pawns] Added the demon faction to this save", and the demon host shows in the factions tab, hostile to everyone.
- [ ] Dev: spawn a demon raid (Execute incident → *demon wave*, or a raid with the demon host). Demons have horns, red skin and tails, and carry vanilla raider gear; the arch-demon wears a cursed idol.
- [ ] Let a demon hit a colonist a few times: roughly 1 hit in 10 lays a curse, with a message. Curses show in the health tab and fade over 4 days.
- [ ] Capture a demon. Extract or bottle their soul: corrupted essence (or a jar that says "corrupted soul"). Harvest a demon corpse: hover says "Soul: corrupted (8 corrupted essence)", and the crucible drops corrupted essence.
- [ ] Purge 5 corrupted essence at the crucible: 10 clean essence; roughly one batch in three curses the worker.
- [ ] Put the cursed idol on a colonist: *curse: hollowed* appears; take it off: gone. Accuracy goes up while worn.
- [ ] Set corrupted essence on fire: a bigger burst than clean essence.
- [ ] Research *Hell gate*, build one, open it: inspect shows the countdown. Dev *Wave now*: a demon raid arrives. Close it: "Closed." Save and reload with it open: the countdown survives.
- [ ] New game: *Hell Unbound* is in the storyteller list with its portrait; the demon host is in world generation.
- [ ] Research tab: *Hell gate* sits at (9, 6.1) under the soul projects without overlapping.
- [ ] With CE: compare a soulsteel and a plasteel armour piece; make soul charges and check each soul gun fires cyan beams with blasts; soul-charge some 5.56 and load it into an assault rifle.

## Things I'm least sure of (from memory, not checked against 1.6)
- The vanilla (non-CE) soulsteel numbers are set from memory of plasteel's. The CE numbers are taken from CE's own patch file.
- Research positions were checked against Core, Ideology and Biotech only. Royalty, Anomaly or Odyssey might put something at (7, 5.5) or (8, 5.5) on the Main tab.

- v0.6, unverified until tested in game: adding a faction to an existing save with `FactionGenerator.NewGeneratedFaction` (it will have no world bases in that save, and Ideology must give it an ideoligion); the storyteller firing an incident whose `baseChance` is 0 through an on/off cycle; demon pawn kinds inheriting from non-abstract vanilla kinds (`ThrasherBase`, `PirateKindBase`, `MercenaryHeavyBase`); `CompProperties_CauseHediff_Apparel` with no body part.
- v0.5, unverified until tested in game: `HediffDef.preventsDeath` (no vanilla XML uses it; the game code reads it in `ShouldBeDead`/`CheckForStateChange`), the soul jar surviving minify and save/load, vanilla `CompExplosive` on essence still firing with CE loaded, and the soul broker caravan patch on the abstract `OutlanderFactionBase`.

If any of these is wrong, the log will name it.

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
- Blood debts (torment, vengeance effigies and the wailing effigy), building on soul jars.
- Curse ritual on a prisoner (the third curse source); demon-specific art.
- Full-kit apparel sets (light and heavy for medieval, industrial and spacer) that fill every slot, with CE webbing/backpack/shield pieces when CE is loaded.
- Flayed face masks, from corpses or living prisoners, named like soul jars.
- Line forge: a soul-free 2x4 machine that turns steel and components into cheap conscript kit (full armour set, rifle with bayonet) for arming thralls between waves.
- Trenches that give real cover (all-round in vanilla, lower profile under CE); check the existing Frontline – Trenches mod first.
- Thrall performance for big bases: a lean thrall think tree, stand-guard mode with auto-wake, and thrall racks that store them in suspension at near-zero cost.

The full plan, with design decisions, is in `ROADMAP.md`.
