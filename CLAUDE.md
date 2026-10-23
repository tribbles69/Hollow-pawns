# Hollow Pawns: notes for Claude Code

RimWorld 1.6 and 1.7 mod (the installed game is now the 1.7 unstable branch, so `check_defs.py` checks against 1.7 data). Read `ROADMAP.md` first: it holds the ground rules, what's built, and the full plan with design decisions. `README.md` has the player-facing summary, the test checklist and the list of things written from memory that still need verifying.

## Layout
- `Defs/`, `Patches/`, `Textures/`: always loaded.
- `Mods/CombatExtended/`: only loaded when Combat Extended is active (see `LoadFolders.xml`).
- `Mods/Biotech/`: demons, the hell gate and the Hell Unbound storyteller; only loaded with Biotech. C# reaching these defs uses `[MayRequireBiotech]` DefOf fields and must cope with them being null.
- `Source/HollowPawns/`: C#, built twice by one `dotnet build -c Release`: net48 against RimWorld 1.6 into `1.6/Assemblies/`, and net10.0 against RimWorld 1.7 (Waylith, Harmony 3) into `1.7/Assemblies/`. Needs the .NET 10 SDK. References come from `Krafs.Rimworld.Ref` (1.6.* and 1.7.4985-unstable). C# 14: `field` is a keyword inside property accessors, don't use it as a name. Harmony 3 rejects patches whose `__instance`, `__result` or named arguments don't match the target's types.
- `Source/tools/`: generators. `gen_soul_guns.py` owns the soul gun family (vanilla and CE); `gen_soul_ammo.py <CE path>` builds soul-charged ammo from Combat Extended's own defs (Aaron's CE is workshop item 2890901044); `gen_textures.py` draws the placeholder sprites; `gen_sounds.py` writes placeholder sounds; `gen_apparel_sets.py <RimWorld Data> <CE path>` builds the full-kit apparel sets (and refuses any set that clashes or leaves a slot empty, with and without CE); `gen_storyteller.py <RimWorld Data>` builds Hell Unbound from the game's Cassandra. Never hand-edit their output files.
- Harmony is a dependency (brrainz.harmony); patches live next to the feature they serve and are applied in `HollowPawnsMod`.
- Systems: souls are taken by the rite job (`Rites.cs`) at the altar or press, graded (`SoulGrade.cs`) and rendered or bound into vessels (`Vessels.cs`). Corruption is one MapComponent (`Corruption.cs`); every new soul source or sink should spill or cleanse through `Corruption.Spill`. Soulless pawns (thralls, husks) go through `SoulUtility.IsSoulless`.
- Before asking Aaron to load the game, verify offline: build, then run `Source/tools/check_defs.py` (game Data dir, mod dir, CE Defs and Patches) to cross-check every def reference, parent, field name and bench work giver against the game's own XML (RimWorld is installed locally) and CE's. In-game loads take him ~10 minutes, so batch features between tests.

## Rules that must hold
- Souls: adult humans only. Every soul or corpse check goes through `SoulUtility`.
- No per-thing ticking. Event-driven, rare tick, or one sampled MapComponent.
- `HP_` prefix on every def; namespace `HollowPawns`.
- CE: follow CE's own patterns and check numbers against its repo.
- Anything unverified gets added to README's "Things I'm least sure of".
- When a roadmap item ships, update `ROADMAP.md` (move it to Built) and README.
