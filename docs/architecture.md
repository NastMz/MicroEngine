# MicroEngine architecture

MicroEngine is a small, code-first 2D framework for C#/.NET 10, for pixel-art and HD games. It is
deliberately compact: a kernel that knows no domain, one backend-independent core library on top of it, two platform
backends (SDL 3 for Windows, Linux, macOS,
Android and iOS; raylib for browsers and as a desktop alternative), and games that are ordinary .NET programs. This
document explains how the pieces fit and why.

## Layers

```mermaid
flowchart TB
  accTitle: Layers
  accDescr: Games and samples start through the hosts, which compose them through hosting and offer the SDL and raylib backends; both implement the platform contract, which builds on the kernel.
  games["<b> games/* </b> <br/> Game rules, content, art and music <br/> (MeadowRun, CourtyardKey, TrailDuel, NeonRift)"]
  samples["<b> samples/* </b> <br/> Minimal examples (HelloEngine)"]
  hosts["<b> src/MicroEngine.Desktop, src/MicroEngine.Cli/Hosts </b> <br/> Hosts: choose a platform profile, run the game"]
  sdl["<b> src/MicroEngine.Sdl </b> <br/> SDL 3: window, 2D renderer, input, touch, audio, lifecycle"]
  raylib["<b> src/MicroEngine.Raylib </b> <br/> raylib: window, renderer, audio, input; WebAssembly"]
  core["<b> src/MicroEngine </b> <br/> Core: loop, scenes, input, graphics model, world, audio synthesis, UI, platform contract"]
  platform["<b> src/MicroEngine.Platform </b> <br/> Platform contracts: host, surface, capabilities, storage, content sources"]
  graphics["<b> src/MicroEngine.Graphics </b> <br/> Graphics device contract: capabilities, resources, command lists, the reference device"]
  input["<b> src/MicroEngine.Input </b> <br/> Input: device events, players, layers, text, actions and saved bindings"]
  hosting["<b> src/MicroEngine.Hosting </b> <br/> Hosting: launch requests and their rules, platform selection, the composition a game configures once"]
  content["<b> src/MicroEngine.Content </b> <br/> Content: asset store, typed handles, scopes, loader registry, zip and listed sources"]
  audio["<b> src/MicroEngine.Audio </b> <br/> Audio: the one mixer over PCM sinks, sound bank and music, synthesis"]
  scene["<b> src/MicroEngine.Scene </b> <br/> Scene world: entity ids, transforms, attachments, interpolation, world clocks"]
  physics["<b> src/MicroEngine.Physics, src/MicroEngine.Physics.Tiles </b> <br/> Physics: the 2D contract and capabilities; tile grids and the swept tile backend"]
  saves["<b> src/MicroEngine.Saves </b> <br/> Saves: JSON save slots over the platform storage, the optional saves module"]
  text["<b> src/MicroEngine.Text </b> <br/> Text: the built-in pixel font, scalable TTF/OTF fonts, measurement and wrapping, the font loader"]
  achievements["<b> src/MicroEngine.Achievements </b> <br/> Achievements: catalog, tracker, the optional achievements module"]
  ui["<b> src/MicroEngine.UI </b> <br/> UI: widgets, layout in UI units, focus and navigation, themes, the UI input layer"]
  kernel["<b> src/MicroEngine.Kernel </b> <br/> Kernel: builder, services, frame schedule, time, events, log, capabilities, handles, math and colour"]
  games -- started by --> hosts
  samples -- started by --> hosts
  hosts -- choose --> sdl
  hosts -- choose --> raylib
  hosts -- compose through --> hosting
  sdl -- implements --> core
  raylib -- implements --> core
  sdl -- implements --> platform
  raylib -- implements --> platform
  games -- use --> core
  core -- builds on --> platform
  core -- builds on --> kernel
  platform -- builds on --> kernel
  core -- builds on --> scene
  scene -- builds on --> kernel
  core -- builds on --> physics
  physics -- builds on --> scene
  graphics -- builds on --> platform
  graphics -- builds on --> kernel
  core -- builds on --> hosting
  hosting -- builds on --> platform
  core -- builds on --> input
  input -- builds on --> platform
  core -- builds on --> content
  content -- builds on --> platform
  core -- builds on --> audio
  audio -- builds on --> content
  core -- builds on --> saves
  saves -- builds on --> platform
  core -- builds on --> text
  text -- builds on --> content
  core -- builds on --> achievements
  achievements -- builds on --> saves
  core -- builds on --> ui
  ui -- builds on --> text
  ui -- builds on --> input
```

Next SDK version (not published yet: arrives with the next SDK version): the kernel is its own assembly,
`MicroEngine.Kernel`, shipped inside the `MicroEngine` package. It references no other engine assembly; modules
register services, frame systems and capabilities on an `EngineBuilder`, and `Game` is one of them. The platform
contracts are a second assembly, `MicroEngine.Platform`, also shipped inside the `MicroEngine` package, which references
only the kernel: `IPlatformHost` (a monotonic clock, an event pump whose events become the engine's lifecycle events,
the main surface, optional features found with `TryGetCapability<T>`, and whether the host may block), `ISurface`,
`IStorage` and `IContentSource`. `IPlatform` extends `IPlatformHost` with the devices and frame presentation that
later move to their own contracts. The engine never casts a platform to find an optional feature, never wraps one, and
nothing a platform does depends on the time mode ([The platform host](manual/backends.md#the-platform-host)).
Input is a third assembly, `MicroEngine.Input` (kernel and platform contracts only, shipped inside the `MicroEngine`
package): device events with ids, the polled state derived from the events no input layer took, players with device
pairing, input layers, the text channel and `InputMap` ([Devices, players and layers](manual/input.md#devices-players-and-layers)).

Next SDK version (not published yet: arrives with the next SDK version): the graphics device contract is an assembly
of the `MicroEngine` package too, `MicroEngine.Graphics`, which references only the kernel and the platform contracts.
It holds one device contract for every backend (capabilities with a typed error for anything undeclared, resources
owned by their device with generational handles and deferred release, samplers as values, buffers, the built-in
`Textured` pipeline, command lists with passes, fenced readback of render targets and the window's target), the core
validation every backend inherits (`GraphicsDeviceBase`) and the CPU reference device that executes the written rules
([The graphics device](manual/rendering.md#the-graphics-device)).
The tier-1 device is `MicroEngine.Graphics.SdlGpu` (in the repository, not in a package yet), which references only the
graphics and platform contracts and SDL3: the contract on pure SDL_GPU with custom shaders compiled ahead of time and
depth, checked by the same conformance suite as every other device. The 3D path over it is `MicroEngine.Graphics3D`
(also in the repository only), which references the graphics contract, content and the scene world: glTF meshes as a
content asset kind, `Camera3D`, one material compiled ahead of time and a renderer that draws a world's mesh attachments
at their interpolated transforms ([The 3D path](manual/rendering.md#the-3d-path)).
The 2D layer is `MicroEngine.Graphics2D`, also carried by the `MicroEngine` package, which references the graphics
contract only: the batcher (`Renderer2D`) and `Camera2D`, and the 2D drawing model games use (`IRenderer`, textures,
`PixelImage`, sprites and atlases, animation, particles, light, parallax), moved out of `MicroEngine.dll` with its
namespace unchanged (`MicroEngine.dll` forwards each type). The SDL and raylib platforms draw every frame into a canvas
render target through it, with `IRenderer` as its façade, and compose the canvas into the window as the game's
`Presentation` says ([The 2D batcher and the camera](manual/rendering.md#the-2d-batcher-and-the-camera)).

Next SDK version (not published yet: arrives with the next SDK version): text is `MicroEngine.Text`, another assembly
of the `MicroEngine` package, which references only the 2D layer, the graphics contract, content, the kernel and the
platform contracts. It holds the fonts with their `MicroEngine.Graphics` namespace (`IFont` and `FontExtensions`,
`BitmapFont`, `ScalableFont`, `SizedFont`, `FontOptions`; `MicroEngine.dll` forwards each type), FontStashSharp, which
rasterizes scalable fonts, and the font loader the asset store registers under the kind "font"
([Text](manual/text.md)).

Next SDK version (not published yet: arrives with the next SDK version): the UI is `MicroEngine.UI`, another assembly
of the `MicroEngine` package, which references only the 2D layer, text, input, the graphics contract, content, the
kernel and the platform contracts. It holds the widgets that need nothing else (`Typewriter`, `SelectionList` and
`UiTheme`, which `MicroEngine.dll` forwards, and the widget tree: `Widget`, `Label`, `Button`, `Panel`, `Stack`,
`TextField`, `Toasts`) and `UiRoot`, which lays a tree out in UI units in the UI view, keeps the focus, moves it with
the engine's actions under the input map's names and, as the highest input layer, takes the pointer presses and keys
it handles from gameplay. Scenes stay in the core, which hosts one root per scene (`Scene.Widgets`), and so do the
widgets that read core types: `DialogueBox` and `PromptText` (action prompts) and the achievement views
([UI widgets](manual/ui.md)).

Next SDK version (not published yet: arrives with the next SDK version): hosting is another assembly of the
`MicroEngine` package, `MicroEngine.Hosting`, which references only the kernel and the platform contracts. It holds the
launcher model every host shares: what a launch asks for (`LaunchRequest`, from a command line, a page or a phone) and
its rules (a script means deterministic time and seed 1 on every host), how a host chooses a platform, the host's
environment, and the composition a game configures once (`HostBuilder`): a `Configure` entry point that the desktop
`Program.cs` hands to the launcher and that the browser, Android and iOS hosts call too. Content folders and save media
are parts of that composition, never process-wide settings
([One composition for every platform](manual/backends.md#one-composition-for-every-platform)).

Next SDK version (not published yet: arrives with the next SDK version): content is a fourth assembly of the
`MicroEngine` package, `MicroEngine.Content`, which references only the kernel and the platform contracts. It holds
the asset store under `Game.Assets`: typed handles (`Asset<T>`) whose generation goes stale once every scope released
the asset, scopes that share one object and dispose it with its last holder, a loader registry with one loader per
kind of asset (images, atlases and fonts register theirs from the core for now; sounds and music too), a reserved
build-time import step (`IAssetImporter`), and content sources beyond folders: zip archives and files a host lists
and opens itself ([Handles, loaders and content sources](manual/assets.md#handles-loaders-and-content-sources)).

Next SDK version (not published yet: arrives with the next SDK version): audio is a fifth assembly of the `MicroEngine`
package, `MicroEngine.Audio`, which references the kernel, the platform contracts and content. It holds the one mixer
every backend plays through (`AudioEngine`: voices with handles and priorities, one voice policy, buses, music streams
decoded ahead outside the simulation step, sounds started at exact output frames), the audio device contract and the
`AudioMixer` games use, offline rendering for tests, and the synthesizer. The platform contracts gain `IAudioSink`, the
PCM output a backend implements: an SDL audio stream fed by a thread, a raylib audio stream, a browser AudioWorklet
([One mixer on every platform](manual/audio.md#one-mixer-on-every-platform)).

Next SDK version (not published yet: arrives with the next SDK version): the scene world is a sixth assembly of the
`MicroEngine` package, `MicroEngine.Scene`, which references only the kernel. Its types are in the `MicroEngine.Scenes`
namespace, beside the scene stack: `IWorld` and its implementation `SceneWorld` (entities with generational `EntityId`s,
a `Transform` hierarchy stored in 3D with 2D helpers, typed attachments walked in attach order, interpolation for
drawing with teleports, per-world step tracking, and a `WorldClock` with time scale and freezes), and the world's part of
the per-step state hash.

Next SDK version (not published yet: arrives with the next SDK version): physics is two more assemblies of the
`MicroEngine` package. `MicroEngine.Physics` (scene world and kernel only) is the 2D contract, `IPhysicsWorld2D`: bodies
owned by entities, box shapes with collision layers, collide-and-slide moves that return their contacts, exact ray and
overlap queries, sensors with buffered events, declared `PhysicsCapabilities` with a typed error for anything a backend
does not declare, and `PhysicsModule`, which steps worlds in the `Physics` phase and writes body poses to transforms.
`MicroEngine.Physics.Tiles` is its tile backend, `TilePhysicsWorld2D`, and now holds the tile types (`TileLayer`,
`TileGrid`, `TileFlags`, `ICollisionGrid`, `Autotile`, `TileMotion`) with their `MicroEngine.World` namespace;
`TileRenderer` stays in the core because it draws ([Physics worlds](manual/world.md#physics-worlds)).
`MicroEngine.Physics.Box2D` (physics contract, scene world and kernel only; optional and not published yet, outside the
SDK packages) is the second backend, `Box2DPhysicsWorld2D`: native Box2D 3.2 behind a flat C layer of the engine's own,
across which only integers, floats and pointers pass and no game code runs, with the native library for each platform in
its package ([The Box2D backend](manual/world.md#the-box2d-backend)).

Next SDK version (not published yet: arrives with the next SDK version): saves and achievements are optional engine
modules. `MicroEngine.Saves` (kernel and platform contracts only, shipped inside the `MicroEngine` package) holds
`SaveStore`, `ISaveStorage` and `FileSaveStorage` with their `MicroEngine.Core` namespace, and `SavesModule`, which
registers a store as the engine's `SaveStore` service. `MicroEngine.Achievements` (kernel, platform contracts, saves and 2D
graphics, also shipped inside the `MicroEngine` package) holds the catalog and tracker types with their `MicroEngine.Core`
namespace and `AchievementsModule`, which registers a game's `AchievementTracker`
and schedules `engine.achievements`, which writes progress. `Game` adds both when the game declares achievements
(`UseAchievements` from its constructor or `Configure`); a game that declares none has no achievement or save service,
no `engine.achievements` system and no achievement file ([Achievements](manual/achievements.md)).

The core references no backend library. Everything platform-specific sits behind `MicroEngine.Core.IPlatform` and
four device interfaces:

| Interface | Responsibility | Implementations |
| --- | --- | --- |
| `IRenderer` | Immediate-mode drawing into the virtual canvas | `SdlRenderer`, `RaylibRenderer`, `HeadlessRenderer` |
| `IGraphicsDevice` | Turning `PixelImage`s into textures with a filter (nearest, linear); textures take region updates | SDL, raylib, headless |
| `IAudioDevice` | Loading/playing PCM sounds and looping music | SDL, raylib, `SilentAudioDevice` |
| `IInputBackend` | Device events: keys, text, pointers, wheel, gamepads, devices connecting (a capability) | `HeadlessInputBackend` |
| `IInputSource` | Raw key, gamepad and mouse state, polled; read as one device | SDL, raylib, `HeadlessInput` |
| `ITouchScreen` (optional, `IPlatform.Touch`; next SDK version: a capability) | Touches, safe area, density, vibration and an overlay drawn over the frame | SDL, raylib (from the browser page), `HeadlessTouchScreen` |

Hosts choose a platform through a `PlatformCatalog` of named `PlatformProfile`s (`sdl`, `raylib`, `sdl-software`,
`headless`, or one of the developer's own) and may supply an audio device or extra input sources of their own; window,
renderer and graphics device stay together because they share a graphics context ([Backends](manual/backends.md)).
SDL is the default on desktops and phones. The raylib backend also runs in a browser (raylib compiled to
WebAssembly): `BrowserLauncher` returns a `GameSession` that the page starts, ticks from `requestAnimationFrame` and
restarts, and keeps saves in `localStorage`. Next SDK version: every host composes the game with its `Configure` entry
point through the same launcher model (`GameHost`), so a page's scripted run is deterministic like a desktop one.
`microengine package web` builds any game into a static site
([The browser host](examples/embedding.md)).

## The frame

`GameLoop` runs one frame per `Tick()`; `GameRunner.Run(game, platform, options)` ticks it until the game
ends. Next SDK version (not published yet: arrives with the next SDK version): the loop owns time; the platform only
supplies a monotonic clock,
and the engine's `FrameClock` turns each frame's time into fixed steps following `TimeSettings` (fixed rate, at most
`MaxStepsPerFrame` steps, frames of at most `MaxFrameDelta`, real or deterministic time). A frame runs the engine's
frame schedule: `BeginFrame` and `Input` once, `PreFixed`, `FixedUpdate`, `Physics` and `PostFixed` once per step,
then `Update`, `LateUpdate`, `Extract`, `Render`, `Present` and `EndFrame`. `Game` registers the built-in systems
(`engine.assets`, `engine.input.events`, `engine.touch`, `engine.input`, `engine.time`, `engine.scenes`, `engine.audio`,
`engine.achievements` (only in games that declare achievements), `engine.input.end`, `engine.render`, `engine.present`...) in the order listed below, and a game
adds its own from `Game.Configure`. Hosts that outlive a run (a browser page) use `GameSession`, which builds the game and platform, ticks the
loop from `requestAnimationFrame` and releases the platform when the run ends. Each frame:

1. Poll platform events. While the platform is suspended (a phone app in the background) the frame stops here:
   no simulation and no rendering; on the way out of suspension the clock restarts, so the time away is not simulated.
2. Accumulate wall-clock time in real time, exactly one step in deterministic time (headless and scripted runs by default).
3. Hand the frame's touches to the game's on-screen controls (`VirtualControls`), which update their state and
   drawing immediately.
4. For each fixed step (60 Hz, at most five per frame): apply scripted input, sample each player's devices and action
   sources (touch controls among them) into their `InputMap` (the frame's device events were routed through the input
   layers before the steps), update scenes, update the audio mixer and achievement banners (flushing achievement
   progress about once a second), advance input edge detection.
5. Render once: clear, draw the scene stack, draw any transition, draw achievement banners; the backend then draws
   the touch controls over the letterboxed frame (never into captures).
6. Optionally write a capture.

Fixed-step simulation keeps platformer physics and combat deterministic, which is what makes
scripted playthrough tests and reproducible screenshots possible.

## Core modules

| Namespace | Contents |
| --- | --- |
| `MicroEngine` | `Color`, `RectF`, `MathX` (approach, damp, waves), `Ease`; next SDK version: `ColorF` (linear, premultiplied helpers), all in the kernel assembly |
| `MicroEngine.Kernel` (next SDK version) | `EngineBuilder`, `Engine`, `IEngineModule`, `ServiceRegistry`, `CapabilityRegistry`, `FrameSchedule`, `FramePhase`, `FrameContext`, `TimeSettings`, `FrameClock`, `IEngineLifecycle`, `ILog`, `Handle<T>`, `HandlePool<T>` |
| `MicroEngine.Platform` (next SDK version) | `IPlatformHost`, `IMonotonicClock`, `IEngineEventSink`, `ISurface`, `HostSurface`, `IDisplayPacing`, `IPreciseDelay`, `IStorage`, `IContentSource`, `FolderContentSource` |
| `MicroEngine.Graphics` in `MicroEngine.Graphics2D.dll` (next SDK version) | `IRenderer2D`, `Renderer2D` (the batcher the `IRenderer` façade draws through), `BatchSettings`, `RenderState`, `SortMode`, `Camera2D`, and the 2D model moved from `MicroEngine.dll`: `IRenderer`, `Texture`, `IGraphicsDevice`, `TextureFilter`, `BlendMode`, `DrawOptions`, `PixelImage`, `Art`, `Sprite`, `SpriteAtlas`, `SpriteAtlasBuilder`, `AtlasData`, `Animator`, `ParticleSystem`, `LightMap`, `ParallaxLayer`, `RendererExtensions` |
| `MicroEngine.Graphics.Devices` (next SDK version) | `IGraphicsDevice`, `GraphicsDeviceBase`, `GraphicsCapabilities`, `GraphicsCapabilityException`, `Texture`, `RenderTarget`, `Sampler`, `GpuBuffer`, `Pipeline`, `CommandList`, `ReadbackRequest`, `Vertex2D`, `GraphicsCounters`, `ReferenceGraphicsDevice` |
| `MicroEngine.Core` | `Game`, `GameSettings`, `GameRunner`, `GameLoop`, `GameSession`, `InputScript`, `HeadlessPlatform`, `SaveStore` (over `ISaveStorage`), `ContentFolder`, `AssetCache` (`Game.Assets`), `Achievement`, `AchievementTracker` |
| `MicroEngine.Scenes` in `MicroEngine.Scene.dll` (next SDK version) | `IWorld`, `SceneWorld`, `EntityId`, `Transform`, `Attachments<T>`, `EntityChildren`, `IDestroyedCallback`, `WorldClock` |
| `MicroEngine.Scenes` | `Scene`, `SceneManager` (stack, overlays), `Transition` (fade, iris, diamonds, wipe), `CoroutineRunner`, `Tweener` |
| `MicroEngine.Input` | In the `MicroEngine.Input` assembly (kernel and platform contracts only): device ids and events (`InputEvent`, `IInputBackend`), the state derived from them (`InputDevices`), players and pairing (`Players`), input layers (`InputLayers`), the text channel, `InputSystem` over them, and `InputMap`: named actions, digital and analog, bound to keys, pad buttons, sticks and triggers, plus action sources, with saved bindings. In the core assembly (they draw): `VirtualControls` and `TouchLayout` (per-game on-screen d-pads, joysticks and buttons, an input layer), `ActionPrompts`, `ITouchScreen`, `FrameLayout` |
| `MicroEngine.Graphics` | `PixelImage` (CPU art, image decoding, crop/rotate), `Art` (noise, dither, ramps), `SpriteAtlasBuilder` (incl. grid sheets), `AtlasData` (packing tools' files), `Sprite`, `Animator`, `Camera2D`, `ParallaxLayer`, `ParticleSystem`, `LightMap`, `IFont` (`BitmapFont`, `ScalableFont`), `TextureFilter`, `IRenderer`; next SDK version: all but the fonts in `MicroEngine.Graphics2D.dll` |
| `MicroEngine.Graphics` in `MicroEngine.Text.dll` (next SDK version) | `IFont`, `FontExtensions`, `BitmapFont`, `ScalableFont`, `SizedFont`, `FontOptions` |
| `MicroEngine.UI` in `MicroEngine.UI.dll` (next SDK version) | `UiRoot`, `UiActions`, `UiContext`, `Widget`, `Label`, `Button`, `Panel`, `Stack`, `TextField`, `Toasts`, `Toast`, `Typewriter`, `SelectionList`, `UiTheme`; `DialogueBox`, `PromptText` and the achievement views stay in `MicroEngine.dll` |
| `MicroEngine.World` | `TileLayer`, `TileGrid`, `TileMotion` (swept AABB with one-way tiles), `Autotile`, `TileRenderer`; `Entity` and `EntityList<T>` in the published SDK only (removed in the next SDK version); next SDK version: the tile types and `TileMotion` are in `MicroEngine.Physics.Tiles.dll` |
| `MicroEngine.Physics` in `MicroEngine.Physics.dll` (next SDK version) | `IPhysicsWorld2D`, `PhysicsCapabilities`, `PhysicsCapabilityException`, `BodyId`, `ShapeId`, `BodyDescription`, `ShapeDescription`, `QueryFilter`, `MoveResult`, `RayHit`, `SensorEvent`, `ContactEvent`, `PhysicsModule`, `PhysicsSystem`, `PhysicsSync` |
| `MicroEngine.Physics.Tiles` (next SDK version) | `TilePhysicsWorld2D`: tile grids as static geometry, the swept mover, one-way platforms, moving solids |
| `MicroEngine.Saves` (next SDK version) | `SavesModule`; `SaveStore`, `ISaveStorage` and `FileSaveStorage` live in `MicroEngine.Saves.dll` with the `MicroEngine.Core` namespace |
| `MicroEngine.Achievements` (next SDK version) | `AchievementsModule` (the tracker as a service, the `engine.achievements` system); `Achievement`, `AchievementStatus`, `AchievementTracker`, `AchievementSaveData` and `AchievementRecord` live in `MicroEngine.Achievements.dll` with the `MicroEngine.Core` namespace |
| `MicroEngine.Audio` | `Synth` (retro SFX), `Song` (step sequencer), `AudioClip` (incl. WAV decoding), `AudioMixer` (incl. sound/music files) |
| `MicroEngine.UI` | `Typewriter`, `DialogueBox`, `SelectionList`, `UiTheme`, `AchievementToasts`, `AchievementsScene`; next SDK version: the widget tree and `Typewriter`, `SelectionList`, `UiTheme` in `MicroEngine.UI.dll` (row above) |

## Key design decisions

In short:

- **Files ship as they are; the engine decodes, caches and releases them.** `Content/` is copied with every game;
  images (StbImageSharp), atlases from packing tools and TTF/OTF fonts (FontStashSharp) are decoded in managed code at
  load, the same on every platform. `Game.Assets` returns one object per file and options, releases each scope's assets
  with it, and reports loads that would hitch during play. The platform locates files and makes textures; the game
  decides what to load and when.
- **Code-first content by default, files when a game wants them.** Sprites, tiles and fonts can be
  authored as code (`PixelImage`, char maps, palettes, noise) and sounds synthesized, so a game can ship
  no binary assets. Games that use existing packs load PNG and WAV in managed code and stream OGG (or MP3 on raylib)
  through the audio device; Neon Rift does this with CC0 assets listed in its `CREDITS.md`.
  Games can also mix both per element: Courtyard Key and Trail Duel load a few crafted
  pieces and keep the rest procedural, repainting imported sprites with their own palette at load.
- **Scenes on a stack, not a global state machine.** Pause menus, dialogue and inventories are overlay
  scenes that freeze (or not) the scene below; transitions swap scenes at their midpoint.
- **Plain entities, not an ECS.** A game of this kind has at most a few hundred objects. Small classes with
  `Update`/`Draw` and an `EntityList<T>` with deferred add/remove are easier to read and debug than
  component plumbing at this scale. Games that outgrow it can add their own systems on top.
- **Actions, never keys.** Games query `"jump"` or `"confirm"` (and `Value`/`Vector` for analog strength); keys,
  gamepads, touch controls and custom sources all press the same actions, and scripts and tests force the same
  actions or touch the same controls, so automated runs exercise exactly the code players do.
- **Touch controls belong to the engine, layouts to the game.** Hit testing, finger tracking, feedback and drawing
  are core code; backends report touches, the safe area and density, and draw primitives. One `TouchLayout` per game
  works on phones, in mobile browsers and in headless tests.
- **Achievements: the engine keeps score, the game decides.** `AchievementTracker` owns the catalog,
  progress, one-time unlocks and persistence (through `SaveStore`, so each game keeps its own file); games
  only call `Unlock`/`AddProgress`/`SetProgress` where their events already happen.
- **Virtual canvas.** Games draw at 320×180 (configurable, up to HD sizes such as 1920×1080). With pixel scaling, the
  default, the backend scales by the largest integer factor that fits the safe area and letterboxes, keeping pixels
  square at any window size (fractionally only when whole factors would waste a fifth of a small screen); with smooth
  scaling it fills the window and samples linearly. Texture filtering is per texture. F11 toggles fullscreen.

## Testing a game

- `HeadlessPlatform` runs a game without a window or sound; runs on it default to deterministic time (one step per
  frame), so tests are reproducible. Next SDK version: that default is the headless platform's stated preset
  (`RunPreset.Testing`), which a run or a game overrides; nothing the platform does depends on it.
- Any game accepts `--script file --hidden --captures dir` to replay input and save 3× PNG captures, for visual
  review and screenshots ([Testing](examples/testing.md)).
