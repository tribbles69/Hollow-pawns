# Hollow Pawns: notes for Claude Code

RimWorld 1.6 mod. Read `ROADMAP.md` first: it holds the ground rules, what's built, and the full plan with design decisions. `README.md` has the player-facing summary, the test checklist and the list of things written from memory that still need verifying.

## Layout
- `Defs/`, `Patches/`, `Textures/`: always loaded.
- `Mods/CombatExtended/`: only loaded when Combat Extended is active (see `LoadFolders.xml`).
- `Mods/Biotech/`: demons, the hell gate and the Hell Unbound storyteller; only loaded with Biotech. C# reaching these defs uses `[MayRequireBiotech]` DefOf fields and must cope with them being null.
- `Source/HollowPawns/`: C# (net48). Build with `dotnet build -c Release`; output goes to `Assemblies/`. References come from the `Krafs.Rimworld.Ref` NuGet package.
- `Source/tools/`: generators. `gen_soul_guns.py` owns the soul gun family (vanilla and CE); `gen_soul_ammo.py <CE path>` builds soul-charged ammo from Combat Extended's own defs (Aaron's CE is workshop item 2890901044); `gen_textures.py` draws the placeholder sprites; `gen_storyteller.py <RimWorld Data>` builds Hell Unbound from the game's Cassandra. Never hand-edit their output files.
- Harmony is a dependency (brrainz.harmony); patches live next to the feature they serve and are applied in `HollowPawnsMod`.
- Before asking Aaron to load the game, verify offline: build, then run `Source/tools/check_defs.py` (game Data dir, mod dir, CE Defs and Patches) to cross-check every def reference, parent, field name and bench work giver against the game's own XML (RimWorld is installed locally) and CE's. In-game loads take him ~10 minutes, so batch features between tests.

## Rules that must hold
- Souls: adult humans only. Every soul or corpse check goes through `SoulUtility`.
- No per-thing ticking. Event-driven, rare tick, or one sampled MapComponent.
- `HP_` prefix on every def; namespace `HollowPawns`.
- CE: follow CE's own patterns and check numbers against its repo.
- Anything unverified gets added to README's "Things I'm least sure of".
- When a roadmap item ships, update `ROADMAP.md` (move it to Built) and README.
