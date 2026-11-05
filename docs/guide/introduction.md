# Introduction

MicroEngine is a game engine that orchestrates subsystems and integrates existing technologies to offer one coherent
way of making 2D games in C# and .NET 10. It does not reinvent graphics APIs, multimedia libraries or the runtime:
SDL 3 draws, plays sound and reads input on Windows, Linux, macOS, Android and iOS, raylib does the same in browsers
(compiled to WebAssembly; with the next SDK version SDL 3 runs there too), and .NET runs the code. MicroEngine's own work is choosing those technologies, coordinating
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
- Teams that want deterministic, testable games: a game can be played end to end by headless tests and scripted
  input.

MicroEngine is open source under the MIT License and in Alpha ([Versions](sdk.md)): games built with it run on six
platforms, and its APIs may still change between 0.x versions. A game is created, run and packaged with the
`microengine` tool and the SDK's NuGet packages, published on nuget.org (see
[Getting MicroEngine](getting-microengine.md) and the [roadmap](/roadmap/)).

## The four games

Four complete games are built with the engine, to show what it can build. All four can be played in the browser or
downloaded on the [demos page](/demos/).

| Game | Genre | Content approach | What it exercises |
| --- | --- | --- | --- |
| [Meadow Run](/demos/meadow-run/) | Side-scrolling platformer | Fully procedural: every sprite, tile, sound and song is generated in code | Tile collision with one-way platforms, player feel, power-up forms, lighting, world map, boss |
| [Courtyard Key](/demos/courtyard-key/) | Top-down action adventure | Hybrid: procedural world and cast plus one imported CC0 enemy | Screen-by-screen rooms, combat, items, push blocks, dialogue, dungeon, boss |
| [Trail Duel](/demos/trail-duel/) | Creature-collecting, turn-based RPG | Hybrid: CC0 creature and trainer sprites, procedural world and music | Menus, turn-based battles, capture, evolution, trainers, shop, large save state |
| [Neon Rift](/demos/neon-rift/) | Run & gun | External CC0 sprite sheets, tilesets, WAV effects and OGG music | File loading, streamed music, parallax, eight-way aiming, weapons, three stages |

Each game has five achievements and saves its progress, and is packaged for desktops, Android and the browser.

## Design principles

| Principle | In practice | More |
| --- | --- | --- |
| Code-first content, files when wanted | `PixelImage`, palettes, noise and `Synth` build assets in code; `ContentFolder` loads PNG and WAV, and music streams from OGG | [Assets](../manual/assets.md) |
| Scenes on a stack | Pause menus, dialogue and inventories are overlay scenes; transitions swap scenes at their midpoint | [Scenes](../manual/scenes.md) |
| Plain entities, not an ECS | Small classes with `Update`/`Draw` in an `EntityList<T>` with deferred add and remove; next SDK version: entities with ids, transforms and attachments in a scene's `IWorld`, and no `EntityList` | [Architecture](../architecture.md) |
| Actions, never keys | Games ask for `"jump"` or `"confirm"`; scripts and tests force the same actions | [Architecture](../architecture.md) |
| Fixed-step simulation | 60 updates per second, rendering once per frame; headless runs are deterministic | [Architecture](../architecture.md) |
| Integrate, do not reinvent | Platforms are profiles over existing libraries (SDL 3, raylib); the core references none of them, and a host can replace a subsystem or register its own platform | [Backends and platforms](../manual/backends.md) |
| The engine keeps score, the game decides | `AchievementTracker` owns progress and persistence; games call `Unlock`/`AddProgress`/`SetProgress` | [Achievements](../manual/achievements.md) |
| Genre-neutral engine | The engine holds no game rules: platformers, adventures and RPGs are built from the same parts | [Tutorial](../tutorial/README.md) |

## What the engine provides

- Fixed-step runner, scene stack with overlays and transitions, coroutines and tweens.
- Action-based input (keyboard, gamepad and on-screen touch controls, digital and analog) and scripted input playback.
- Procedural pixel art (`PixelImage`, palettes, noise, dithering), sprite atlases, animation, particles,
  camera, parallax layers, a light map and a built-in pixel font.
- Managed PNG and WAV loading, grid sprite-sheet slicing, streamed OGG music (and MP3 on raylib).
- Tile layers, swept tile collision with one-way platforms, autotiling, plain entities.
- A retro sound synthesizer, step-sequencer music and a mixer that fades between tracks.
- Dialogue boxes, selection menus, JSON saves, achievements with banners and a list screen.
- Platforms: SDL 3 (Windows, Linux, macOS, Android, iOS), raylib (browsers, desktop alternative), a headless
  platform for deterministic tests; named profiles, replaceable audio and input. Next SDK version: SDL 3 in browsers
  too, and raylib on desktops only.

Next SDK version (not published yet: arrives with the next SDK version): the engine as separate modules, a scene world
of entities with transform hierarchies, physics worlds behind one contract with a tile backend, HD canvases with smooth
scaling, TrueType and OpenType fonts, one audio mixer on every platform, and an opt-in programmable graphics device on
desktops ([migration notes](../migrations.md)). Two more parts live in the engine's repository and in no published
package: a minimal [3D path](../manual/rendering.md#the-3d-path) and a
[Box2D physics backend](../manual/world.md#the-box2d-backend).
- The `microengine` tool: a project template, and packages for players on every platform, with the game's own
  name and icon ([Packaging](packaging.md)).

## Planned work

The [roadmap](/roadmap/) lists what is planned, without dates. A visual editor and a 3D renderer appear there only as a
long-term vision; they are neither available nor committed.

## Where to go next

| If you want to | Read |
| --- | --- |
| Install the SDK | [Install the SDK](getting-microengine.md), then [Your first game](first-game.md) |
| Learn by building a game | [Tutorial: Gem Garden](../tutorial/README.md) |
| Find a quick answer | [How do I...?](../howto.md) and the [focused examples](../examples/focused.md) |
| Learn each subsystem | [Engine manual](../manual/lifecycle.md) |
| Look up a type or method | [API reference](../api/README.md) |
| Check where it runs | [Platforms](platforms.md) and [Requirements and limitations](../reference/limitations.md) |
| Understand the design | [Architecture](../architecture.md) |
