# Introduction

MicroEngine is a game engine that orchestrates subsystems and integrates existing technologies to offer one coherent
way of making 2D games in C# and .NET 10. It does not reinvent graphics APIs, multimedia libraries or the runtime:
SDL 3 draws, plays sound and reads input on Windows, Linux, macOS, Android and iOS, raylib does the same in browsers
(compiled to WebAssembly), and .NET runs the code. MicroEngine's own work is choosing those technologies, coordinating
them behind one contract (`IPlatform`) and giving games a consistent model on top: the fixed-step loop, scenes,
input actions, procedural art and audio tools, saves and achievements.

A game is an ordinary .NET program that subclasses `Game`, pushes scenes onto a stack and draws pixel art into a small
virtual canvas. It never names a platform: the host picks one, so the same game assembly runs everywhere. MicroEngine is
code-first and has no visual editor; it is not a Godot or Unity, and not a thin wrapper over SDL or raylib either.

## Who it is for

- Developers who prefer writing games in code over working in a visual editor. There is no editor: sprites,
  tiles, sounds and music can all be authored as C# (or loaded from files when a game wants them).
- Small pixel-art projects: platformers, top-down adventures, turn-based RPGs, run & guns, at a scale of a few
  hundred objects per scene.
- Teams that want deterministic, testable games: every game built with it so far is played end to end by
  headless tests and scripted input.

MicroEngine is open source under the MIT License and in Alpha ([SDK status](sdk.md)): it works and is checked on six
platforms, but its APIs may still change between 0.x versions. A game is created, run and packaged with the
`microengine` tool and the SDK's NuGet packages, published on nuget.org (see
[Getting MicroEngine](getting-microengine.md) and the [roadmap](/roadmap/)).

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
| Integrate, do not reinvent | Platforms are profiles over existing libraries (SDL 3, raylib); the core references none of them, and a host can replace a subsystem or register its own platform | [ADR 0016](../decisions/0016-platform-profiles.md), [ADR 0017](../decisions/0017-sdl3-native-platform.md) |
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
- Platforms: SDL 3 (Windows, Linux, macOS, Android, iOS), raylib (browsers, desktop alternative), a headless
  platform for deterministic tests; named profiles, replaceable audio and input.
- Packaging of games as native downloads, as the four demos are ([Downloads](downloads.md)).

## Planned work

The [roadmap](/roadmap/) lists what is planned, without dates: distributing the SDK (NuGet packages, project
templates, versioned releases), validation on real devices, engine robustness and research into the mobile experience. A visual editor and 3D appear there only as a long-term vision; they are neither available nor
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
