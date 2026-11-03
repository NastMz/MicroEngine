# API reference

The public types of the MicroEngine SDK 0.4.0-alpha.2, one page per namespace and per type: signatures, parameters, default
values and the descriptions written in the engine's source. It covers the four engine packages (`MicroEngine`,
`MicroEngine.Sdl`, `MicroEngine.Raylib`, `MicroEngine.Desktop`); the `microengine` tool is documented in
[The microengine tool](../guide/cli.md).

This reference says what each member is. How the pieces work together, with examples, is in the
[engine manual](../manual/lifecycle.md); every page here links to its chapter under "Guide". New to MicroEngine? Start
with the [tutorial](../tutorial/README.md).

| Namespace | Contains | Guide |
| --- | --- | --- |
| [MicroEngine.Audio](MicroEngine.Audio.md) | Sound effects and music: the one mixer every backend plays through (voices with handles and priorities, buses, music streams, exact-frame scheduling), the sound bank and music games play through, the audio device contract, offline rendering for tests, WAV and OGG decoding, synthesis. | [Audio](../manual/audio.md) |
| [MicroEngine.Content](MicroEngine.Content.md) | Content: the asset store under Game.Assets (typed handles, scopes that share and release, the loader registry, the reserved import step) and content sources beyond folders (zip archives, files a host lists). | [Assets](../manual/assets.md#handles-loaders-and-content-sources) |
| [MicroEngine.Core](MicroEngine.Core.md) | The game and its loop: `Game`, settings, runners and sessions, launchers and platform composition, scripted input, saves, content files, achievements, application identity, the headless platform. | [Game lifecycle and time](../manual/lifecycle.md) |
| [MicroEngine.Desktop](MicroEngine.Desktop.md) | The desktop launcher with the official desktop platforms (SDL 3, raylib, SDL software, headless). | [Backends and platforms](../manual/backends.md) |
| [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md) | The graphics device contract every backend implements: capabilities and the typed capability error, textures, render targets, samplers, buffers, pipelines, command lists with passes, readback and the surface target; the core validation backends inherit and the CPU reference device. | [The graphics device](../manual/rendering.md#the-graphics-device) |
| [MicroEngine.Graphics](MicroEngine.Graphics.md) | Drawing: the renderer contract, images in code and from PNG, sprites and atlases, animation, camera, parallax, particles, light, the pixel font. | [Graphics](../manual/rendering.md) |
| [MicroEngine.Hosting](MicroEngine.Hosting.md) | Hosting: the launcher model every host shares (launch requests and their rules, platform selection, the host environment) and the composition a game configures once with its Configure entry point. | [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform) |
| [MicroEngine.Input](MicroEngine.Input.md) | Input: device events with ids (keys, text, pointers and touches, wheel, gamepads), players and device pairing, input layers, and actions over them: bindings and their persistence, analog values, on-screen controls, prompts, custom sources. | [Input](../manual/input.md) |
| [MicroEngine.Kernel](MicroEngine.Kernel.md) | The kernel: engine composition (builder, services, modules, capabilities), the frame schedule and its phases, loop-owned time, engine events, logging and generational handles. | [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule) |
| [MicroEngine.Physics.Tiles](MicroEngine.Physics.Tiles.md) | The tile backend of the physics contract: tile grids as static geometry, the exact swept mover, one-way platforms and moving solids that push and carry bodies. | [Physics worlds](../manual/world.md#physics-worlds) |
| [MicroEngine.Physics](MicroEngine.Physics.md) | The 2D physics contract: worlds of bodies owned by entities, box shapes with collision layers, collide-and-slide moves with their contacts, ray and overlap queries, sensors with buffered events, declared capabilities and the typed capability error, and the module that steps worlds and writes body poses to transforms. | [Physics worlds](../manual/world.md#physics-worlds) |
| [MicroEngine.Platform](MicroEngine.Platform.md) | The platform contracts: the host (clock, event pump, main surface, capabilities, whether it may block), the surface, display pacing and precise waits, storage for saves and content sources. | [The platform host](../manual/backends.md#the-platform-host) |
| [MicroEngine.Raylib](MicroEngine.Raylib.md) | The raylib platform: desktops and browsers (WebAssembly), with the browser launcher and the browser page it reads. | [Backends and platforms](../manual/backends.md) |
| [MicroEngine.Scenes](MicroEngine.Scenes.md) | The scene stack (scenes, transitions, coroutines and tweens) and the scene world: entity ids, transforms with a hierarchy, typed attachments, interpolation for drawing and world clocks. | [Scenes](../manual/scenes.md) |
| [MicroEngine.Sdl](MicroEngine.Sdl.md) | The SDL 3 platform: Windows, Linux, macOS, Android and iOS. | [Backends and platforms](../manual/backends.md) |
| [MicroEngine.UI](MicroEngine.UI.md) | Pixel-art widgets: dialogue boxes, menus, typewriter text, themes, achievement banners and list. | [UI widgets](../manual/ui.md) |
| [MicroEngine.World](MicroEngine.World.md) | Tile layers, collision grids, swept movement against tiles (`TileMotion`), autotiling, drawing tiles and plain entities. | [Tile maps and collision](../manual/world.md) |
| [MicroEngine](MicroEngine.md) | Basic value types: colours, rectangles, maths and easing helpers. | [Graphics](../manual/rendering.md) |

## Reading a page

- **Signatures** are C#. Optional parameters show their default value (`float fadeSeconds = 0.4`); `?` marks a value
  that may be null.
- **Records** (`GameSettings`, `SfxParams`, `TouchLayout`...) are configured with object initialisers and copied with
  `with`: `new GameSettings("My Game") { Background = Color.Hex("1a1c2c") }`.
- **Examples** on type pages are short fragments that name your own classes (`TitleScene`, `player`); complete programs
  that compile and run are in the [focused examples](../examples/focused.md) and the [tutorial](../tutorial/README.md).
- **Platform types** (`SdlPlatform`, `RaylibPlatform`, `HeadlessPlatform`) implement the same `IPlatform` contract; a game
  never names them, its launcher does ([Backends and platforms](../manual/backends.md)).
- The SDK is in Alpha: these APIs may change between 0.x versions, and every such change is listed in the
  [migration notes](../migrations.md).
