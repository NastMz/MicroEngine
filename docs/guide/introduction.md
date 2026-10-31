# Introduction

MicroEngine is a compact, code-first 2D game framework for C# and .NET 10. A game is an ordinary .NET program
that subclasses `Game`, pushes scenes onto a stack and draws pixel art into a small virtual canvas; the engine
supplies the fixed-step loop, input actions, procedural art and audio tools, saves, achievements and a raylib
backend that runs on the desktop and, compiled to WebAssembly, in desktop browsers.

## Who it is for

- Developers who prefer writing games in code over working in a visual editor. There is no editor: sprites,
  tiles, sounds and music can all be authored as C# (or loaded from files when a game wants them).
- Small pixel-art projects: platformers, top-down adventures, turn-based RPGs, run & guns, at a scale of a few
  hundred objects per scene.
- Teams that want deterministic, testable games: every game built with it so far is played end to end by
  headless tests and scripted input.

MicroEngine is not distributed to external developers yet: there is no package or template to install today.
NuGet packages, project templates and versioned releases are planned; see
[Getting MicroEngine](getting-microengine.md) and the [roadmap](/roadmap/).

## The four games

Four complete games are built with the engine. Together they are the evidence for what it can build today, and
all four can be played as demos in a desktop browser on the [demos page](/demos/).

| Game | Genre | Content approach | What it exercises |
| --- | --- | --- | --- |
| [Meadow Run](../../games/MeadowRun/README.md) | Side-scrolling platformer | Fully procedural: every sprite, tile, sound and song is generated in code | Tile collision with one-way platforms, player feel, power-up forms, lighting, world map, boss |
| [Courtyard Key](../../games/CourtyardKey/README.md) | Top-down action adventure | Hybrid: procedural world and cast plus one imported CC0 enemy | Screen-by-screen rooms, combat, items, push blocks, dialogue, dungeon, boss |
| [Trail Duel](../../games/TrailDuel/README.md) | Creature-collecting, turn-based RPG | Hybrid: CC0 creature and trainer sprites, procedural world and music | Menus, turn-based battles, capture, evolution, trainers, shop, large save state |
| [Neon Rift](../../games/NeonRift/README.md) | Run & gun | External CC0 sprite sheets, tilesets, WAV effects and OGG music | File loading, streamed music, parallax, eight-way aiming, weapons, three stages |

Each game ships five achievements, saves its progress, runs headlessly in its own test project and has input
scripts used for tests and screenshots. All four also build into browser packages.

## Design principles

| Principle | In practice | Record |
| --- | --- | --- |
| Code-first content, files when wanted | `PixelImage`, palettes, noise and `Synth` build assets in code; `ContentFolder` loads PNG, WAV, OGG and MP3 | [ADR 0002](../decisions/0002-code-first-content.md), [ADR 0004](../decisions/0004-external-asset-files.md), [ADR 0005](../decisions/0005-hybrid-content.md) |
| Scenes on a stack | Pause menus, dialogue and inventories are overlay scenes; transitions swap scenes at their midpoint | [ADR 0003](../decisions/0003-scenes-entities-runtime.md) |
| Plain entities, not an ECS | Small classes with `Update`/`Draw` in an `EntityList<T>` with deferred add and remove | [ADR 0003](../decisions/0003-scenes-entities-runtime.md) |
| Actions, never keys | Games ask for `"jump"` or `"confirm"`; scripts and tests force the same actions | [Architecture](../architecture.md) |
| Fixed-step simulation | 60 updates per second, rendering once per frame; headless runs are deterministic | [Architecture](../architecture.md) |
| Small platform boundary | Everything platform-specific sits behind `IPlatform`; the core never references raylib | [ADR 0003](../decisions/0003-scenes-entities-runtime.md), [ADR 0007](../decisions/0007-browser-host.md) |
| The engine keeps score, the game decides | `AchievementTracker` owns progress and persistence; games call `Unlock`/`AddProgress`/`SetProgress` | [ADR 0006](../decisions/0006-achievements.md) |
| Genre-neutral engine | Game rules live in `games/<Name>`; code moves into the engine only when it is genre-neutral or used by two games | [Contributing](../../CONTRIBUTING.md) |

## What the engine provides

- Fixed-step runner, scene stack with overlays and transitions, coroutines and tweens.
- Action-based input (keyboard and gamepad bindings) and scripted input playback.
- Procedural pixel art (`PixelImage`, palettes, noise, dithering), sprite atlases, animation, particles,
  camera, parallax layers, a light map and a built-in pixel font.
- Managed PNG and WAV loading, grid sprite-sheet slicing, streamed OGG/MP3 music.
- Tile layers, swept tile collision with one-way platforms, autotiling, plain entities.
- A retro sound synthesizer, step-sequencer music and a mixer that fades between tracks.
- Dialogue boxes, selection menus, JSON saves, achievements with banners and a list screen.
- A headless platform for deterministic tests.

## Planned work

The [roadmap](/roadmap/) lists what is planned, without dates: distributing the SDK (NuGet packages, project
templates, versioned releases), research into mobile and touch input, engine robustness, and validation on more
platforms. A visual editor and 3D appear there only as a long-term vision; they are neither available nor
committed.

## Where to go next

| If you want to | Read |
| --- | --- |
| Know how the engine is distributed | [Getting MicroEngine](getting-microengine.md) |
| Write your first game | [Hello Engine walkthrough](../examples/hello-engine.md), then [Making a game](../making-a-game.md) |
| Learn each subsystem | [Engine manual](../manual/lifecycle.md) |
| Look up signatures | [Contracts](../reference/contracts.md) |
| Check where it runs | [Platforms](platforms.md) and [Status and limitations](status.md) |
| Understand the design | [Architecture](../architecture.md) |
