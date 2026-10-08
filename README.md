# Hollow Pawns v0.3

Every human has a soul. Take it from the dead at the soul crucible, or from the living on the operating table. They die without it.

## What's in v0.3

### Souls and essence
| Thing | What it does |
|---|---|
| **Soul** (body part) | Added to the Human body with 0 coverage, so damage can never hit it. Only adults' souls can be taken: children can't be operated on or reaped, and the crucible never takes their corpses. Animals, insects, mechanoids and alien races have no soul at all. |
| **Extract soul** (surgery) | Needs Medicine 6 and 1 medicine. Drops 12 soul essence, marks the soul missing, kills the patient. Counts as a violation on prisoners and guests. |
| **Soul crucible** (Production, 3x1, 250W) | *Harvest soul from corpse*: 1 human corpse → 8 essence, corpse destroyed. Husks with no soul left are never hauled to it. Colonist corpses are off by default in the bill filter. *Forge soulsteel*: 10 plasteel + 3 essence → 10 soulsteel. Also makes every soul weapon and soul ammo. |
| **Soulsteel** | Metallic stuff one tier above plasteel: tougher, sharper, lighter, non-flammable. Weapons, armour, walls, furniture. |
| **Soul generator** (Power, 2x2) | 1200W, burns 2 essence a day, holds 20. One fresh corpse runs it for 4 days. |

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
Copy the whole `HollowPawns` folder into `RimWorld\Mods`, enable it in RimSort, load it anywhere after Core and the DLCs (and after Combat Extended if you use it).

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
- [ ] Load an old save: injuries and bionics on existing pawns should be where they were.
- [ ] With CE: compare a soulsteel and a plasteel armour piece; make soul charges and check each soul gun fires cyan beams with blasts; soul-charge some 5.56 and load it into an assault rifle.

## Things I'm least sure of (from memory, not checked against 1.6)
- `SurgeryFlesh` as the surgery parent, and the `Cremate` / `Smelt` effect and sound names.
- The `AllowCorpsesColonist` special filter name.
- `Krafs.Rimworld.Ref` having a 1.6 version on NuGet.
- The vanilla (non-CE) soulsteel numbers are set from memory of plasteel's. The CE numbers are taken from CE's own patch file.
- The vanilla parents `BaseMeleeWeapon_Sharp_Quality`, `BaseHumanMakeableGun` and `BaseBullet`, the `Shot_ChargeRifle` / `Interact_Rifle` sounds, and the explosion fields copied from memory of vanilla's Bomb damage (`buildingDamageFactorImpassable`, `explosionCellFleck` and so on).
- Under CE: whether CE's laser beam class runs an explosive comp on impact the way its bullets do. If the beams hit but never blast, the fallback is to switch the beams' parent from `LaserBulletBlue` to `BaseBulletCE` in the generator.

If any of these is wrong, the log will name it.

## Roadmap
**Weapons**
- Lower-tech soul weapons (to brainstorm): neolithic and medieval tiers.
- Unique soul armour: reliquary armour (burns stored essence to cancel a killing blow), reaper's shroud (raises the scythe's reap chance).

**Longevity**
- Medieval: fountain of youth, fed with essence.
- Industrial/spacer: soul cradle, a bio-pod style machine.

**Drugs**
- Soul-based drugs that overcharge a pawn, with a cost.

**Infrastructure and later**
- Soul vats and pipes via Vanilla Expanded Framework PipeSystem (Thing to resource, Resource storage, Resource to power).
- Soul mechs (gestator recipes using essence).
- Essence yield by corpse freshness, Ideology reactions, soul-eating xenotype.
- Soul jars, blood debts (torment, vengeance effigies and the wailing effigy), the soul broker, volatile stockpiles.
- Quality of life (the Reaper's Mark): one-click extraction with a Soul-Lock safety catch, ensouled/soulless corpse stockpile filters, corpse soul status on hover, alerts.

The full plan, with design decisions, is in `ROADMAP.md`.
