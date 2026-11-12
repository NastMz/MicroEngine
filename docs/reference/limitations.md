# Requirements and limitations

What a MicroEngine game needs, and the functional limits to plan around. For what each platform runs on, see
[Platforms](../guide/platforms.md).

## Requirements

| To | You need |
| --- | --- |
| Develop | The .NET 10 SDK on Windows, Linux or macOS |
| Package for Windows | A Windows machine |
| Package for Linux | Any system |
| Package for macOS, iOS | A Mac with Xcode; for iOS, the .NET `ios` workload |
| Package for Android | The .NET `android` workload, an Android SDK, Java 17 or later |
| Package for the web | The .NET `wasm-tools` workload |

| Players need | |
| --- | --- |
| Windows | Windows 10 or 11, 64-bit, a GPU with Direct3D 11, OpenGL or Vulkan (or `--platform sdl-software`) |
| Linux | x86_64, glibc 2.34 or later (2.38 for `--platform raylib`), X11 or Wayland with OpenGL, PulseAudio or PipeWire |
| macOS | macOS 14 or later on Apple silicon |
| Android | Android 7.0 (API 24) or later, 64-bit (arm64-v8a or x86_64) |
| iOS | iOS 15 or later |
| Web | A browser with WebAssembly and WebGL |

## Engine

| Limitation | More |
| --- | --- |
| 2D only in the SDK packages. A minimal 3D path (glTF meshes, a perspective camera, depth) is in the engine's repository only, in no package; there is no 3D renderer | [The 3D path](../manual/rendering.md#the-3d-path) |
| No networking or scripting language | [Architecture](../architecture.md) |
| No visual editor, level editor or asset tool: content is code, or files loaded through `Game.Assets` | [Assets](../manual/assets.md) |
| No ECS: plain entities, sized for a few hundred objects per scene | [Architecture](../architecture.md) |
| Text uses the built-in pixel font (`Game.Font`) or TrueType and OpenType fonts at any size (`Assets.Font`). Bitmap font files (AngelCode BMFont) are not read | [Text](../manual/text.md) |
| Below 12 drawn frames per second the game slows down instead of skipping updates (at most five steps per frame) | [Architecture](../architecture.md#the-frame) |

## Input

| Limitation | More |
| --- | --- |
| One gamepad at a time (the first connected; the next takes over when it is unplugged); no rumble | [Input](../manual/input.md#limitations) |
| The mouse can be read (`Input.Pointer`) but not bound to actions | [Input](../manual/input.md#limitations) |
| No in-game rebinding screen or layout editor: bindings and touch layouts are code | [Input](../manual/input.md#limitations) |
| No haptics on iOS | [Input](../manual/input.md#limitations) |
| On screens near 16:9 the game frame shrinks while touch controls are shown, so none covers it | [Input](../manual/input.md#keeping-the-game-visible) |

## Saves

| Limitation | More |
| --- | --- |
| No cloud saves: files on desktops and phones, `localStorage` in browsers | [Persistence](../manual/persistence.md) |
| `localStorage` holds about 5 MB per site, shared with everything on that site; browsers may evict it under storage pressure, private windows discard it, and Safari may delete it for sites not visited for seven days | [Persistence](../manual/persistence.md) |

When storage is full or blocked, the game keeps running and the page says progress could not be saved;
`SaveStore` never overwrites a slot it could not read.

## Browsers

| Limitation | More |
| --- | --- |
| Sound starts only after the player's first click, tap or key press, and stops while the tab is hidden (the game pauses with it) | [Platforms](../guide/platforms.md#web-browsers) |
| F11 is the browser's own fullscreen; the game's fullscreen is Alt+Enter or the page's button | [The browser host](../examples/embedding.md) |
| The download includes the .NET runtime: about 2 MB compressed for a small game, more with large music or art | [Platforms](../guide/platforms.md#web-browsers) |

## Packaging and distribution

| Limitation | More |
| --- | --- |
| Android packages are APKs; Google Play asks for Android App Bundles (`.aab`), which the tool does not build yet | [Packaging](../guide/packaging.md#android) |
| Packages are x64 on Windows and Linux and arm64 on macOS: no Intel Mac, 32-bit or ARM Linux packages | [Packaging](../guide/packaging.md) |
| Windows executables are not code-signed by the tool, so SmartScreen may warn until you sign them | [Signing](../guide/signing.md#windows) |
| macOS apps without a Developer ID signature and notarization need Open Anyway in Privacy & Security on first launch | [Signing](../guide/signing.md#macos) |
| iOS device builds and App Store distribution need an Apple Developer account | [Signing](../guide/signing.md#ios) |
| Desktop packages include raylib next to SDL (for `--platform raylib`); a game that references only `MicroEngine.Sdl` ships SDL alone | [Install the SDK](../guide/getting-microengine.md#adding-microengine-to-an-existing-project) |

## API stability

MicroEngine is in Alpha (0.x): public APIs may change incompatibly between versions, and every change is listed in
the [migration notes](../migrations.md) ([Versions](../guide/sdk.md)).
