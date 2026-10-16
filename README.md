# Hollow Pawns v0.8

Every human has a soul. Take it from the dead at the soul crucible, or from the living on the operating table. They die without it.

## What's in v0.8

### Souls and essence
| Thing | What it does |
|---|---|
| **Soul** (body part) | Added to the Human body with 0 coverage, so damage can never hit it. Only adults' souls can be taken: children can't be operated on or reaped, and the crucible never takes their corpses. Animals, insects, mechanoids and alien races have no soul at all. |
| **Bone Altar** (Production, 2x1, stone, no research) | Mark a prisoner (render or bind) and a warden restrains them, carries them here and binds them to it. **Load and leave:** the altar takes the soul on its own while the warden goes back to work, up to 3x faster while someone works it (hauling work, thralls too; Medicine sets the speed). Slow (about 1.6 hours unstaffed), 1 in 4 rites botched (a grade worse), witnesses are disturbed, heavy corruption close by. |
| **Soul Press** (Production, 2x2, 800W, research *Soul press*) | The same, faster (about 36 minutes), a grade better, never botched, nobody disturbed; vents less corruption per rite but over a wider area. **Load and leave** like the altar: the press runs the rite itself (paused without power), up to 3x faster while someone works it (hauling work, thralls too; Medicine sets the speed). Moving the prisoner or clearing the mark stops it. |
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
| **Corpse grinder** (3x3, 600W; research *Industrial harvest*) | Grinds corpses left next to it (make a stockpile there): carrion bales, 4 essence from an adult human soul, meat and leather. No husk, never children, heavy corruption. One corpse per 1000 ticks while powered, faster when staffed (below). |
| **Line forge** (4x2, 500W; research *Industrial harvest*) | Turns steel and components left next to it into conscript kit: a conscript rifle (bayonet, poor accuracy, 5.56 under CE), conscript helmet and vest, or the full kit. Near-worthless, no quality. A *Stop at* setting pauses it once that many of each piece are lying around the colony. Faster when staffed. |

**Staffing.** The grinder, the forge and the clone vat (while growing) run on their own, but a pawn working one speeds it up: x1.5 at skill 0 up to x3 at skill 20 (Cooking for the grinder, Crafting for the forge, Medicine for the vat), times their global work speed. It's **hauling** work, so thralls can do it, and it comes last among hauling jobs, in half-hour shifts. Nobody is sent while the machine is unpowered or has nothing to do. Inspect shows who's working it and the speed.

### Reliquaries, soul-bound items and shrines
| Thing | What it does |
|---|---|
| **Reliquary** (Furniture, stone, wood or metal) | A display case for one filled vessel. Inspect shows whose soul it keeps. Anyone who cared about that person when they were taken (family, lovers, anyone with a good opinion of them) gets **Remembered** (+6) within 8 cells. Only vessels bound from v0.8 on remember who loved them. |
| **Bind soul into item** (soul crucible) | One bill for **any** weapon or apparel, vanilla's or any mod's: uses up a filled vessel and binds that soul into the item. The soul's **residue** picks the gift (a Shooting soul: accuracy; Melee: hit chance; Construction, Mining, Cooking, Plants, Crafting, Intellectual: that work speed; Medicine: tend quality; Social: social impact; traits such as Pyromaniac: heat armour, Tough: less damage taken, Jogger: speed; anything else: psychic sensitivity) and the **grade** its size, from a seventh of the bonus (wretched) to all of it (exalted). The bearer gets it like any gear bonus. Hover: *Bound: Vex the Butcher (masterwork soul; residue: Pyromaniac)*. If the item is destroyed the soul gets out and corrupts the ground. |
| **Bound shrine** (Misc, 120 stone) | Haulers bring it a filled vessel and the soul goes into the stone for good. Before that, toggle **consecrate** (cleans corrupted ground) or **desecrate** (corrupts it); fixed once bound. Radius 4.9 to 13.3 and strength by grade. Named for the soul: *shrine of Vex*. Breaking it frees the soul in a burst of the **opposite** effect. |
| **Essence shrine** (Misc, research *Soul extraction*) | Burns about 3 essence a day; toggle **cleansing** or **spreading** corruption within 6.9 cells. Works only while fuelled. |

Shrines work inside the corruption map pass (a few sampled cells each time), so they cost almost nothing.

### Prisoner battery
| Thing | What it does |
|---|---|
| **Prisoner battery** (Power, after Soul extraction) | A sealed 2x1 casket. Mark a prisoner *Battery* and a warden locks them in. Like cryptosleep they need nothing while inside. Makes 300 W from a wretched soul up to 900 W from an exalted one for five days, then the prisoner dies as a husk and drops half a render's worth of essence. Leaks a little corruption each day. *Release* lets them out alive but badly soul-shocked. Their faction takes it hard. |

### Blood debts
When an enemy kills one of your colonists, the killer carries a **blood debt** naming who they killed (it shows on their inspect pane and health tab). If they escape they are kept as a world pawn and can come back, still marked. Down them; don't kill them.

| Thing | What it does |
|---|---|
| **Torment** (prisoner interaction) | A warden gives the prisoner a session at most twice a day: rising pain (*tormented*, fades slowly), soul shock, and -15 goodwill with their faction. Anyone who loved someone the prisoner killed gets **Avenging** (+4, stacks to 5); kind colonists get *prisoner tormented* (-5). Works on any prisoner, but only debtors have avengers. |
| **Vengeance effigy** (sculptor's table, 25 soulsteel and the debtor's corpse, soul still in it) | Art. *A soulsteel figure of Vex, cut down on ... for the murder of Jeb.* Avengers get **Avenged** (+8 when cast) and *effigy of the killer* (+5) whenever they see it. |
| **Wailing effigy** (Reaper's Mark: *Effigy*, debtors only) | At an altar or press their living soul is sealed into a soulsteel figure, quality from the warden's Artistic skill. Avengers who see it get +10; everyone else gets *dread* (-4). It **screams** once a night, sometimes when someone walks past, and always when a raid arrives: enemies within 20 cells (30 for a raid) take soul shock and a quarter break and run; colonists nearby who aren't avengers get -3. **Shatter** it for twice a smashed vessel's essence and a burst of corruption. |

The scream is a generated placeholder (`Sounds/HP/EffigyWail.wav`, made by `Source/tools/gen_sounds.py`); drop a real recording over it with the same name.

### Scenarios
| Scenario | Start |
|---|---|
| **The Coven** | Three cultists, a granite **bone altar** already built, one **captive** in irons (shackled for two days: build a cell), two empty urns, *Soul extraction* researched, and the ground for 20 cells around **corrupted**. Pemmican, a knife, a bow. |
| **Endless War** (Biotech) | Two overseers and three **thralls**, a **line forge**, a **corpse grinder** and a **soul generator** with 60 essence (wire them up), three conscript rifles. *Electricity*, *Soul extraction* and *Industrial harvest* researched. A raid every 4 to 7 days. |
| **The Gravedigger** | One undertaker with the **reaper's scythe**, 4 holy water and 2 empty urns, on an **old battlefield**: 16 enemy dead lying around the start, half still armed. Every body still has its soul, and while this scenario runs the ghost chance is **8%** per body (or your setting, if higher), so expect one or two to rise after the first day. Bury, burn or harvest them fast. |
| **The Escapee** | One survivor in a drop pod with a **soul lattice** holding their spouse (or lover), who loved them back: put it in a reliquary and they're *Remembered*. A Whisper soul pistol, 30 essence, *Soul extraction* researched. A raid every 8 to 14 days. |

The Inquisitors wait for salt (the fallen idol is built). The precepts (purist, corruption-exalted) don't exist yet.

### Longevity
| Thing | What it does |
|---|---|
| **Fountain of youth** (Misc, 150 stone and 10 essence) | Right-click with an adult colonist to bathe: 5 essence from its tank, 1 to 2 years younger (never below 18), a 1 in 4 chance to wash away one chronic condition (bad back, frailty, cataracts and the like), and a touch of soul corruption. Once every five days per pawn. |
| **Soul cradle** (Misc, research *Soul cradle* after *Soul lattice*; 400 W) | Right-click to climb in. With 30 essence loaded it runs a three-day cycle (paused without power; the occupant needs nothing while sealed): 5 to 8 years younger, every chronic condition gone, one lost body part regrown. *Open* lets them out early and wastes the essence. |

### Unique armour
| Thing | What it does |
|---|---|
| **Reliquary armour** (fabrication bench, research *Soul lattice*; 120 soulsteel, 2 advanced components and a **filled vessel**) | Full plate, a notch under marine armour and slower. Holds **2 essence charges**: when the wearer would die, it burns one and they drop instead, *held back from death* for a day (downed, can't die). Get them to a doctor; if what would have killed them is still there when it fades, it will. Recharge at the soul crucible (15 essence; the bill skips full suits). The vessel's soul is bound into the armour, so it also carries that soul's gift. Borrows the marine armour art, tinted. |
| **Reaper's shroud** (tailoring bench, research *Soul extraction*; cloth or leather and 10 essence) | A black duster. **+5% reap chance** for the scythe (a new *reap chance* stat, shown in the stats panel), on top of 0.5% per Melee level. |

### Lower-tech soul gear
| Thing | What it does |
|---|---|
| **Soul bow** (crafting spot, research *Recurve bow*; 40 wood and 5 essence) | A recurve bow whose arrows carry soul shock (about 0.16 a hit) on top of the wound. Under CE it takes ordinary streamlined arrows or **soul arrows** (crafting spot or smithy: wood, steel and essence, 10 at a time), and only soul arrows shock. Soul arrows also load into the **short bow and recurve bow** (and any modded bow using CE's arrow sets); the greatbow takes great arrows, as in CE. Borrows the recurve bow art, tinted. |
| **Bone fetish** (crafting spot or tailoring bench; wood and 3 essence) | Worn at the throat (neck, on the overhead layer, so it fits under any shirt). **Halves** a reaper's chance against the wearer (a new *reap ward* stat). |
| **Wraith lantern** (smithy, research *Soul extraction*; 30 steel and 15 essence) | Worn on the belt. **Unshutter lantern**: every enemy within 9 cells in line of sight takes soul shock, and about half break and run. Once a day. |

### Soul pipes (only with Vanilla Expanded Framework)
If VEF is loaded, a **Soul pipes** build tab appears (research *Industrial harvest*). All on VEF's pipe system, nothing new in C#.

| Thing | What it does |
|---|---|
| **Soul pipe** | Carries essence. Any building on the network that burns essence as fuel (soul generator, essence shrine, fountain of youth, soul cradle) **tops itself up** from it. |
| **Essence drain** | Haulers bring essence; it goes into the pipes. |
| **Soul vat** (2x2) | Holds 500 essence. *Draw essence* gives 25 as a stack. If it's wrecked it vents a soul burst sized by what's in it. |
| **Essence tap** | Fills with stacks from the network for haulers. |
| **Soul valve** | Shuts the flow. |

No soulsteel processor: VEF's processor can't make soulsteel the way the crucible does. The pipes borrow vanilla's power conduit art, tinted.

### Full-kit apparel sets
Six sets that fill every apparel slot (skin, middle, shell, belt, head, eyes) and never clash. Wear every piece of one set for **full kit** (mental break threshold -3%, pain shock threshold +5%). Most pieces are renamed copies of a vanilla piece (same coverage, recipe, bench and research, same art tinted, armour 90% of the original for light sets and 95% for heavy) plus a little essence; the belts (+3% global work speed) and eye pieces (-5% aiming time) are new.

| Set | Tier | Pieces |
|---|---|---|
| **Gravecloth** | medieval light | tunic, leggings, gambeson, mourning cloak, hood, corpse-pouch belt, grave blinkers |
| **Ossuary plate** | medieval heavy | arming shirt, arming hose, ossuary plate, ossuary helm, sword belt, bone visor |
| **Mourner's kit** | industrial light | shirt, trousers, vest, coat, hat, belt, smoked goggles |
| **Soulsteel kit** | industrial heavy | undershirt, greaves, vest, jacket, helmet, belt, goggles |
| **Wraithweave** | spacer light | undershirt, leggings, armour, helmet, belt, lenses |
| **Reliquary kit** | spacer heavy | undershirt, leggings, **reliquary armour** (4.12), helm, belt, lenses |

Under **Combat Extended** each set adds a webbing, a backpack and a shield (copies of CE's own), and every CE patch on a vanilla original is applied to its copy too, so the CE numbers are CE's. Those three don't count toward the full-kit bonus, so it works the same either way.

### Flayed faces
| Thing | What it does |
|---|---|
| **Take face** (butcher spot or table) | On an adult human corpse. Makes a **flayed face** mask; the corpse is marked, not used up, so it can still go to the crucible, the slab or the wall. Up to *normal* quality. |
| **Flay face** (surgery, prisoners and slaves) | They live, with *face taken* (beauty -3) and *my face was taken* (-25 for 60 days). A violation: -30 goodwill. The mask comes out a grade better than a corpse's. |
| **Flayed face** (eye slot) | *face of Vex*: keeps the name. Social impact -15%, slave suppression +15% (Ideology). Others think less of the wearer (-15), unless they're psychopaths or bloodlust (+10). Anyone who loved that person and sees it worn gets -20. If the face carried a **blood debt**, the avengers who see it get *effigy of the killer* (+5). Wears out like leather. |

### The Profaned (only does anything with The Profaned loaded)
- **Profaned have souls, corrupted ones.** They can be rendered or bound; their essence comes out corrupted; corrupted ground doesn't bother them.
- **Cursebearers share their soul.** When one raises the dead, the body gets a **soul shard**: a splinter of the Cursebearer's own soul. A shard is always *wretched*, and its residue is the Cursebearer's.
- **Kill the Cursebearer first.** When a Cursebearer dies, every body it raised on a loaded map loses its shard and drops dead again.

### Ghosts
A corpse of an adult who still has their soul, left lying for about a day, rolls **once**: a 1% chance (mod settings: *Hollow Pawns*) that the soul rises as a ghost, named for the dead and **tethered** to its body.

| Kind | What it does |
|---|---|
| **Lingering ghost** | From an ordinary corpse. Drifts within about 4 cells of the body and chills the air around it. Harmless, but anyone who sees it gets *ghost nearby* (-4). |
| **Wrathful ghost** | From a corpse on corrupted ground (60+) or a corrupted corpse (demons, Profaned). Hunts anyone with a soul within 7+ cells of the body (wider on worse ground), **through walls**. Its touch is **grave chill**: no armour helps, it builds with every touch (cold, numb, freezing, then failing) and kills at full. Holy water thaws it at once; a warm room on warded ground thaws it fast; otherwise it fades slowly. Seeing one: -10. Children have no soul to touch. |

- **Ending one:** render, bind, grind, burn or bury its body (or carry it off the map), and the ghost fades.
- **Fighting one:** only soul weapons (blasts, bursts, soul arrows), soulsteel melee weapons, the reaper's scythe and fire hurt it; everything else passes through. Shooters and turrets whose weapon can't hurt it don't pick it as a target (you can still order an attack), and bullets that can't hurt it fly straight through to whatever's behind (vanilla and CE). Killing it leaves nothing.
- **Warding:** ghosts can't enter the reach of a consecrated bound shrine or a lit, cleansing essence shrine, and won't hunt anyone standing in it.
- Dev tools: *Hollow Pawns → Raise lingering ghost / Raise wrathful ghost*, then click a corpse.

### Corruption events
How corruption reaches a colony that never touches a soul.

| Event | What happens |
|---|---|
| **Black rain** (from day 20; lasts half a day to a day and a half) | Dark sky, rain, and 3 to 5 patches of the map (15 to 25 cells across) get corrupted wherever the rain lands unroofed, up to 140. Colonists caught outside in a patch pick up a little soul corruption each hour. |
| **Fallen idol** (from day 15) | Comes down like a meteorite. Leaks corruption within about 8 cells until destroyed. **Smash it** (draft and attack; it bursts in a soul burst, so stand back) or **claim and deconstruct** it (quieter, fouls the ground under it). Either way it leaves corrupted essence. |

Fire and holy water clean up after both, as usual.

### The curse ritual
A **Curse** prisoner mode: a warden spends a while chanting over the prisoner. The prisoner gets a random curse and becomes a **curse carrier** for 20 days. Release them, or let them escape, and they take it home: the next raid their faction sends arrives with **every raider cursed** (for six hours after the first one turns up, so drop pods count too).

Under CE there are also **soul great arrows** for the greatbow and **soul javelins** (thrown by hand or from an atlatl), both made at the crafting spot or smithy with a little essence. They shock like soul arrows and can hurt ghosts.

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
- [ ] Soul press: research it, power it, mark → render: faster, a grade better, no witness thought. The warden leaves once the prisoner is strapped in; inspect counts down; a hauler or thrall comes to work it and the countdown speeds up. Clear the mark mid-rite: it stops with a message. Wardens don't carry the strapped-in prisoner back to bed. Same at the bone altar (no power needed; witnesses still disturbed when it finishes).
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
- [ ] Staffing: a thrall with hauling on and nothing to carry walks to the grinder's front and works it; inspect says *Worked by Thrall 123: x1.5 speed* and corpses go faster. Turn the power off: they leave. Same at the forge with a colonist with good Crafting (higher speed). Start a clone vat culture: once it's growing someone tends it and the days left drop faster.
- [ ] Line forge *Stop at 5*: it pauses (inspect says *paused*) once 5 of each piece are on the map, and starts again when colonists equip some.
- [ ] With CE: compare a soulsteel and a plasteel armour piece; make soul charges and check each soul gun fires cyan beams with blasts; soul-charge some 5.56 and load it into an assault rifle.

**v0.8 batch**
- [ ] Reliquary: bind a prisoner who has a friend or relative in the colony, put the vessel in a reliquary: inspect names the soul; the friend standing near it gets *remembered*. Someone who didn't know them doesn't.
- [ ] Bind soul into item: at the crucible, bind a vessel with a Shooting residue into a rifle: label says *soul-bound*, hover names the soul and the bonus, and the wielder's shooting accuracy shows the extra line from gear. Try a piece of apparel and a modded weapon too. Destroy a bound item with a dev tool: message and corruption. The bill never offers an already bound item.
- [ ] Bound shrine: build one on corrupted ground, leave it consecrated, let a vessel be hauled in: the vessel disappears, the shrine is renamed *shrine of ...*, the overlay shows the corruption fading around it over a few days. Build a desecrated one elsewhere: corruption grows. Break one: a burst of the opposite.
- [ ] Essence shrine: refuel with essence, toggle cleansing / spreading, watch the overlay; with no fuel it stops.
- [ ] Prisoner battery: build one, mark a prisoner *Battery*: a warden carries them over and locks them in; the power tab shows 300-900 W by grade; inspect counts down five days. DEV *drain now*: they come out dead, essence drops. Try *Release* on another: alive, soul shock. Unpowered net and flick off behave like any generator.
- [ ] Blood debts: let a raider kill a colonist (dev: damage until dead): the killer gets *blood debt (Jeb)* and a message; inspect shows it. Capture them. Set *Torment*: a warden runs a session, pain and soul shock rise, the victim's friend gets *avenging*, a kind colonist is disturbed, goodwill drops. Twice a day at most.
- [ ] Wailing effigy: mark the debtor *Effigy* (option only appears for debtors), rite at the altar: a minified *wailing effigy (Vex)* drops. Place it: friend gets +10 nearby, others *dread*. Wait for 1am: it screams (sound, text, -3 for others nearby). Trigger a raid: it screams and some raiders flee. Shatter: essence and corruption.
- [ ] Vengeance effigy: kill a debtor without taking the soul, bring the corpse and 25 soulsteel to a sculptor's table: the bill only accepts that corpse; the effigy names the victim; avengers get *avenged*.
- [ ] Blood debt survives escape: let a debtor flee the map, save, reload, check `Find.WorldPawns` (dev) still has them.
- [ ] The Gravedigger: start it: about 16 corpses around the landing site, some armed; after a day one or two rise as ghosts (letters). The scythe hurts them.
- [ ] Scenarios: start each of the three others. Coven: altar placed near the start, captive lying shackled (can't move, two days), corruption overlay shows a blot around the start. Endless War: three thralls arrive with the colonists, forge, grinder and generator placed, raids come every few days. Escapee: the lattice is labelled with a name, the colonist's social tab shows a dead spouse, a reliquary gives them *remembered*.
- [ ] Fountain of youth: fuel it, right-click with an old colonist: they walk over, bathe, a message says how much younger; the health tab shows *bathed in the fountain* and a second bath is refused. A child or a husk gets a greyed-out option.
- [ ] Soul cradle: research, build, power, fuel 30+; right-click a colonist with a bad back and a missing finger: they go in, inspect counts three days, DEV *finish cycle*: out younger, back fixed, finger back. Cut power mid-cycle: inspect says paused.
- [ ] Reliquary armour: make one with a filled vessel: label shows *(2/2)* and *soul-bound*. Wear it, kill the wearer with dev damage: they go down, *held back from death*, label *(1/2)*. Tend them; the hediff fades after a day. Kill again twice: the second time with no charges they die. Recharge bill at the crucible ignores a full suit.
- [ ] Reaper's shroud: wear it with the scythe: the stats panel shows reap chance +5%; reaps come noticeably more often.
- [ ] Soul bow: shoot a raider a few times: soul shock appears in their health tab. Under CE: the bow's ammo list shows soul arrows next to stone, steel and plasteel, and so do the short bow's and recurve bow's; the soul arrow bill is at the crafting spot and smithy.
- [ ] Bone fetish: wear it: stats show reap ward 50%. Wraith lantern: wear it in a fight, press *Unshutter*: message counts who fled, button greys out for a day.
- [ ] Soul pipes (with VEF): the *Soul pipes* tab appears after *Industrial harvest*. Drain to vat to soul generator: essence hauled to the drain fills the vat, the generator refuels itself. *Draw essence* on the vat drops 25. A tap fills with stacks. Shut a valve: the far side stops filling. Without VEF: no tab, no errors.
- [ ] Apparel sets: open the tailoring, smithy, machining and fabrication benches: each set's pieces are there next to the vanilla originals. Make and wear a full mourner's kit: no piece pushes another off, and *full kit* appears in health; take the hat off and it goes. With CE: the rig, pack and shield go on too, and the vest's armour reads in mm like CE's flak vest.
- [ ] Faces: *take face* at a butcher table on a raider's corpse: a *face of X* mask appears and the corpse is still there (inspect its health: face taken); the bill won't take it again. Wear it: social tab shows *wears a human face*. Flay a prisoner: they live, mood -25, goodwill drops. Flay a blood debtor: the victim's friend near the wearer gets *effigy of the killer*.
- [ ] The Profaned (if you run it): the log says *The Profaned found*. Mark a Profaned prisoner *render*: corrupted essence. Let a Cursebearer raise someone, capture them: health shows *soul shard (caster)*, grade is wretched. Kill that Cursebearer: the raised drop.
- [ ] Ghosts (dev tool *Raise lingering ghost* on a raider's corpse): a letter, a translucent *ghost of X* drifting near the body; mood *ghost nearby*. Bury the body: it fades with a message.
- [ ] Wrathful ghost (dev tool): it drifts through a wall to a colonist, touches them: *grave chill* rises, armour doesn't matter. Drafted colonists with normal rifles don't fire at it on their own; order a shot: it goes straight through and hits the wall behind. Hit it with a soul gun or the scythe: it takes damage and leaves no corpse when killed. Drink holy water: chill drops. Put a consecrated shrine near: the ghost won't come in. Render the corpse at the altar: it fades.
- [ ] Mod settings: *Hollow Pawns* shows the ghost chance slider.
- [ ] Black rain (dev: *Execute incident → black rain*): the sky darkens, it rains, the corruption overlay shows a few blots growing over the day, a colonist left outside in one picks up soul corruption. Under a roof: nothing.
- [ ] Fallen idol (dev: *Execute incident → fallen idol*): a meteorite-style impact, a letter, corruption spreads around it on the overlay. Attack it: it breaks in a soul burst and drops corrupted essence. Another one: claim, deconstruct: no burst, a stain under it.
- [ ] Curse ritual: set a prisoner to *Curse*: a warden chants, the prisoner gets a curse and *curse carrier*. Release them: a message says they carry it home. Trigger a raid from that faction (dev): the raiders arrive cursed, with a message.
- [ ] CE: the greatbow's ammo list has soul great arrows; the atlatl's has soul javelins; soul javelins can also be thrown by hand.

## Things I'm least sure of (from memory, not checked against 1.6)
- The vanilla (non-CE) soulsteel numbers are set from memory of plasteel's. The CE numbers are taken from CE's own patch file.
- Research positions were checked against Core, Ideology and Biotech only. Royalty, Anomaly or Odyssey might put something at (7, 5.5) or (8, 5.5) on the Main tab.

- v0.8, unverified until tested in game: machine staffing and the load-and-leave altar and press (an interface-based work giver for the grinder, forge, clone vat, altar and press; the prisoner left lying on it, kept out of the warden take-to-bed job by a postfix in the Hauling work type, the operator's speed read by the machine's rare tick).
- v0.8, unverified until tested in game: adding the soul-bound comp to every weapon and apparel def at startup (`SoulBoundInjector`); the bind bill handing back the same item (the item is despawned inside `GenRecipe.MakeRecipeProducts` and skipped by `ConsumeIngredient`); the bound shrine's storage filter being cleared after binding; `Building_Storage` subclasses with `maxItemsInCell` 1 as single-vessel holders.
- v0.8, unverified until tested in game: the prisoner battery as a `Building_Casket` subclass holding a prisoner (whether they stay a prisoner and come back out right, and whether `CompPowerPlant.DesiredPowerOutput` updated only on rare tick is enough for the power net).
- v0.8, unverified until tested in game: blood debts (the `Pawn.Kill` prefix/postfix pair seeing the instigator, the postfix on the private `WorldPawnGC.GetCriticalPawnReason` keeping escaped debtors, a non-exclusive `PrisonerInteractionModeDef` showing up as a checkbox next to vanilla's, `MakeRecipeProducts` minifying the effigy before my postfix sees it, `AudioGrain_Clip` loading a mod wav from `Sounds/`, effigies' `CompTickRare` running on a minified-then-placed `Building_Art`).
- v0.8, unverified until tested in game: scenario parts (`MapGenerator.PlayerStartSpot` being set when `GenerateIntoMap` runs, vanilla's internal `ScenPart_CreateIncident` loading from XML with its private `minDays`/`maxDays`/`repeat`, a pawn killed as a world pawn inside `PlayerStartingThings`, a downed prisoner set by `Moving` `setMax` 0 staying a prisoner on the ground).
- v0.8, unverified until tested in game: setting `AgeBiologicalTicks` directly (and `ResetAgeReversalDemand` with Ideology), `Pawn_HealthTracker.RestorePart` on a missing part, a pawn entering a `Building_Casket` subclass through my own job (copied from vanilla's cryptosleep job), the soul cradle's position on the research tab.
- v0.8, unverified until tested in game: a `Pawn.Kill` prefix returning false (and whether anything else calls `Kill` past `ShouldBeDead` while the ward is up), `preventsDeath` on a hediff with a `Consciousness` cap, new `StatDef`s picking up apparel `equippedStatOffsets` on pawns.
- v0.8, unverified until tested in game: `graphicData` `color` tinting borrowed vanilla textures (soul bow, reliquary armour, soul arrow projectile), CE's `AmmoArrowBase` with its trade tags replaced, and `CompGetWornGizmosExtra` showing the lantern button.
- v0.8, unverified until tested in game: the VEF soul pipes (VEF's `PipeSystem` field names are copied from Vanilla Chemfuel Expanded, not checked against VEF's code; `Custom3` link flag shared with some other mod; `UI/Designators/Deconstruct` as the deconstruct icon; `CompProperties_ExplosiveContent` with `HP_SoulBurst`; tinting the vanilla conduit atlas).
- v0.8, unverified until tested in game: the apparel sets' borrowed worn art tinted by `colorGenerator`, `Inherit="False"` on empty `apparel/tags` and `thingSetMakerTags`, CE shields and backpacks copied from CE's own defs (render nodes and all), and whether every mirrored CE patch lands (the generator mirrors only patches that name the original by defName; patches on shared parents are inherited).
- v0.8, unverified until tested in game: the flayed face's worn head art (three facings, EyeCover drawn over helmets), a `Thought_SituationalSocial` worker on every pair of pawns, `AddDirect` of a hediff on a corpse's head, `wearPerDay` on a mask.
- v0.8, unverified until tested in game: the Profaned patch (checked against its 1.6 `Profaned.dll`: `Utility_Profaned.BestowCurse(Pawn caster, Pawn recipient, RevivalCurseExtension)` and the genes `BotchJob_UndeadPsysiology` and `BotchJob_Cursebearer`; not yet seen running), `RestorePart` on the soul part of a freshly revived husk.
- v0.8, unverified until tested in game: ghosts (an animal-race pawn with its own think tree, moved by setting `Position` cell by cell through walls; `ManhunterPermanent` only to make the wrathful one count as hostile; the `Transparent` shader with a tinted `Graphic_Multi`; a one-part body tagged with every capacity source; `PreApplyDamage` postfix absorbing most damage; the corpse removed in a `Kill` postfix). The ghost target filter (a `Pawn.ThreatDisabled` postfix reading the attacker's verb `Projectile` by reflection, so CE's ammo-aware verbs work too) and pass-through (`Projectile.CanHit`, and CE's `ProjectileCE.CanCollideWith` patched by hand).
- v0.8, unverified until tested in game: black rain's `SkyTarget` colours and forcing the `Rain` weather from `Init`; `DoCellSteadyEffects` being called for a custom game condition; the fallen idol riding a `MeteoriteIncoming` skyfaller (a 1x1 building as contents) and being claimable.
- v0.8, unverified until tested in game: the curse ritual's `Pawn.ExitMap` prefix (released and escaping prisoners both go through it?) and the `Pawn.SpawnSetup` postfix cursing arriving raiders; CE soul javelins copied from CE's pila (a weapon that is also ammo).
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
