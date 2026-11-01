# API reference

The public types of the MicroEngine SDK 0.4.0-alpha.1, one page per namespace and per type: signatures, parameters, default
values and the descriptions written in the engine's source. It covers the four engine packages (`MicroEngine`,
`MicroEngine.Sdl`, `MicroEngine.Raylib`, `MicroEngine.Desktop`); the `microengine` tool is documented in
[The microengine tool](../guide/cli.md).

This reference says what each member is. How the pieces work together, with examples, is in the
[engine manual](../manual/lifecycle.md); every page here links to its chapter under "Guide". New to MicroEngine? Start
with the [tutorial](../tutorial/README.md).

| Namespace | Contains | Guide |
| --- | --- | --- |
| [MicroEngine.Audio](MicroEngine.Audio.md) | Sound effects and music: synthesis, WAV decoding, the mixer games play sounds through, and the audio device contract. | [Audio](../manual/audio.md) |
| [MicroEngine.Core](MicroEngine.Core.md) | The game and its loop: `Game`, settings, runners and sessions, launchers and platform composition, scripted input, saves, content files, achievements, application identity, the headless platform. | [Game lifecycle and time](../manual/lifecycle.md) |
| [MicroEngine.Desktop](MicroEngine.Desktop.md) | The desktop launcher with the official desktop platforms (SDL 3, raylib, SDL software, headless). | [Backends and platforms](../manual/backends.md) |
| [MicroEngine.Graphics](MicroEngine.Graphics.md) | Drawing: the renderer contract, images in code and from PNG, sprites and atlases, animation, camera, parallax, particles, light, the pixel font. | [Graphics](../manual/rendering.md) |
| [MicroEngine.Input](MicroEngine.Input.md) | Input actions over keyboard, gamepad, mouse and touch: bindings, analog values, on-screen controls, custom sources. | [Input](../manual/input.md) |
| [MicroEngine](MicroEngine.md) | Basic value types: colours, rectangles, maths and easing helpers. | [Graphics](../manual/rendering.md) |
| [MicroEngine.Raylib](MicroEngine.Raylib.md) | The raylib platform: desktops and browsers (WebAssembly), with the browser launcher and browser storage. | [Backends and platforms](../manual/backends.md) |
| [MicroEngine.Scenes](MicroEngine.Scenes.md) | The scene stack: scenes, transitions, coroutines and tweens. | [Scenes](../manual/scenes.md) |
| [MicroEngine.Sdl](MicroEngine.Sdl.md) | The SDL 3 platform: Windows, Linux, macOS, Android and iOS. | [Backends and platforms](../manual/backends.md) |
| [MicroEngine.UI](MicroEngine.UI.md) | Pixel-art widgets: dialogue boxes, menus, typewriter text, themes, achievement banners and list. | [UI widgets](../manual/ui.md) |
| [MicroEngine.World](MicroEngine.World.md) | Tile layers, collision grids, swept movement against tiles, autotiling and plain entities. | [Tile maps and collision](../manual/world.md) |

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
