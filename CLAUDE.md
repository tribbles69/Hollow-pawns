# Hollow Pawns: notes for Claude Code

RimWorld 1.6 mod. Read `ROADMAP.md` first: it holds the ground rules, what's built, and the full plan with design decisions. `README.md` has the player-facing summary, the test checklist and the list of things written from memory that still need verifying.

## Layout
- `Defs/`, `Patches/`, `Textures/`: always loaded.
- `Mods/CombatExtended/`: only loaded when Combat Extended is active (see `LoadFolders.xml`).
- `Source/HollowPawns/`: C# (net48). Build with `dotnet build -c Release`; output goes to `Assemblies/`. References come from the `Krafs.Rimworld.Ref` NuGet package.
- `Source/tools/`: generators. `gen_soul_guns.py` owns the soul gun family (vanilla and CE); `gen_soul_ammo.py <CE path>` builds soul-charged ammo from Combat Extended's own defs. Never hand-edit their output files.

## Rules that must hold
- Souls: adult humans only. Every soul or corpse check goes through `SoulUtility`.
- No per-thing ticking. Event-driven, rare tick, or one sampled MapComponent.
- `HP_` prefix on every def; namespace `HollowPawns`.
- CE: follow CE's own patterns and check numbers against its repo.
- Anything unverified gets added to README's "Things I'm least sure of".
- When a roadmap item ships, update `ROADMAP.md` (move it to Built) and README.
