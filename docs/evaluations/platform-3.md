# PLATFORM-3: multiplatform backend architecture

Date: 2026-10-31. Branch `feat/platform-3` (from `main`). Task record: [`odd/tasks/platform-3.md`](../../odd/tasks/platform-3.md).
Decisions: [ADR 0016](../decisions/0016-platform-profiles.md) (contracts) and [ADR 0017](../decisions/0017-sdl3-native-platform.md)
(technology).

Evidence words are used strictly: **run** means executed here and its output read; **CI only** means the check exists
in a workflow but has not run yet (this branch has not been pushed); **desk** means judged from documentation or
package contents without a prototype.

## 1. Inventory before this work

| Piece | State | Coupling found |
| --- | --- | --- |
| `src/MicroEngine` (core) | No reference to raylib | `IPlatform` bundled graphics, audio, input, clock, frame and window calls; no way to replace one part, no lifecycle state |
| `src/MicroEngine.Raylib` | Desktop, browser and the experimental Android host | The desktop command line lived in the raylib assembly, so choosing another backend meant editing every game's `Program.cs` |
| `platforms/android` | raylib built with the NDK in a `NativeActivity` | raylib audio-teardown abort (3 of about 37 runs, [PLATFORM-2](platform-2.md#5-android)); no touch; content files refused |
| `platforms/ios` | The game on `HeadlessPlatform` | raylib 6.0 has no iOS platform: nothing drawn |
| macOS | Builds and headless tests | GLFW requires an accelerated OpenGL renderer; hosted runners have none |
| `ContentFolder` | Files next to the executable | No way for a phone host to point games at unpacked package files |

## 2. Candidates

| Candidate | What it is | Platforms (for .NET) | Evidence |
| --- | --- | --- | --- |
| raylib 6.0 + Raylib-cs 8.1.0 (zlib) | Window, GL renderer, input, miniaudio | Windows, Linux, macOS (GPU required), browser (our wasm build), Android (our NDK build); no iOS | Run in PLATFORM-1/2 and again here |
| SDL 3.5 + ppy.SDL3-CS 2026.1002.1 (zlib; bindings MIT) | Window, events, 2D renderer over Direct3D 11/12, Metal, OpenGL, OpenGL ES, Vulkan or a CPU renderer; audio streams; gamepads; touch; mobile lifecycle | Package ships natives for win-x64/x86/arm64, linux-x64/x86/arm64/arm, osx-x64/arm64, android (4 ABIs, with the `SDLActivity` Java classes) and an iOS xcframework (device and simulator) | Run here: Windows, Linux, Android; macOS and iOS CI only |
| MonoGame | Complete framework with its own `Game` loop and content pipeline | Desktop, Android, iOS; browser not official | Desk: adopting it would replace the engine's loop and identity rather than serve one subsystem |
| Silk.NET (GLFW/SDL windowing + raw GL/Vulkan) | Bindings, no 2D renderer | GLFW has no iOS; its SDL path is SDL itself | Desk: we would write and maintain a renderer per graphics API |
| Direct Metal / Vulkan / WebGPU renderer | Graphics APIs | One per system | Desk: rejected by the reuse rule; SDL's renderer already targets them |
| NVorbis 0.10.5 (MIT) | Managed OGG Vorbis decoder | Everywhere .NET runs | Run: Neon Rift's OGG music on Windows and Android |

No physics engine was evaluated: the engine has none and the games' tile motion is their own rule code.

## 3. Prototypes and results

### SDL3 renderer against raylib (Windows 11, NVIDIA GPU)

| Run | Result |
| --- | --- |
| 15 scenarios, 4 games, `--platform sdl` vs `--platform raylib` | 138 captures each. RGB byte-identical in 11 of 15 scenarios; the rest differ only where a game draws at half-pixel positions or rotates a translucent sprite (worst cell 7/255); alpha differs because raylib stores translucent alpha in its render target, SDL stores the frame as shown |
| SDL with `direct3d11`, `opengl` and `vulkan` renderers | All 138 captures identical across the three |
| SDL `software` renderer | Same frames visually; blending rounds one level differently and some edges move (worst cell 15/255): needs its own reference |
| Probe test (every `IRenderer` operation, `PlatformEquivalenceTests`) | SDL draws each operation as specified; SDL and raylib produce identical RGB for the probe frame |
| Duration of all 15 scenarios, hidden window | SDL 35 s, raylib 129 s |

The half-pixel case is legitimate rasterization: Courtyard Key centres a 5-pixel key on x.5, and the two APIs pick
different texels (raylib's version drops the key's ring). The engine does not round positions for one game.

### Linux (WSL2 Ubuntu 24.04, .NET 10.0.403, Mesa)

| Run | Result |
| --- | --- |
| Raylib on Mesa llvmpipe against `linux-x64.json` (the CI reference) | 138/138 pixel-identical: this setup reproduces the CI runner |
| SDL on llvmpipe against the GPU SDL reference | 112/138 within tolerance; the others differ in the same rotated, translucent sprites (one texel, one blend level) |
| SDL on llvmpipe, recorded as `sdl-linux-x64.json` | Native Linux now compares SDL against it |
| `PlatformEquivalenceTests` with native tests required | 4/5 pass (SDL conventions, every SDL GPU driver that starts draws the same frame, SDL = raylib, clear error for a missing driver); the audio-substitution test failed only because this WSL image has no PulseAudio client library (SDL found no audio backend); CI installs PulseAudio |

### Android (emulator, API 36.1, x86_64)

| Level | Meadow Run (code-first) | Neon Rift (PNG, WAV, OGG content) |
| --- | --- | --- |
| Installed, started | yes | yes |
| Rendered | 13/13 captures, pixel-identical to Windows SDL | 12/12 captures, pixel-identical to Windows SDL; within the Linux reference |
| Audio device | opened in every run | opened in every run (OGG music streamed through NVorbis) |
| Keys from the system | Title → Map → Level card → Play | Title → Stage card → Play |
| Touch (on-screen controls, `adb input tap`) | Title → Map | Title → Stage card → Play |
| Background and back | suspended, then resumed | suspended, then resumed |

Two Android-specific defects were found and fixed on the way: SDL turned the window to portrait (fixed with SDL's
orientation hint), and with SDL's default blocking pause the game never observed the background (fixed with an event
watch). The raylib audio-teardown abort no longer applies: raylib is not used on Android.

### macOS and iOS

Not run: there is no Mac here, and the branch has not been pushed. The iOS host compiles on Windows and restores the
SDL3 xcframework. The platform workflow will run both: SDL with Metal on the hosted macOS runner (whose GPU is a
paravirtual Metal device), and the two games in the iOS simulator with captures compared to the SDL reference.

### Browser

Raylib in WebAssembly stays. Meadow Run and Neon Rift development packages built from this branch played their 7
scripts in Chrome with all 25 captures byte-identical to desktop raylib. The packages grew by about 150 KB with Brotli
(SDL3-CS 103 KB, NVorbis 34 KB, the SDL and desktop launcher assemblies 15 KB) because the games' desktop entry point
now references the desktop platforms; that code is never called in a browser.

## 4. Decision

Official profiles (catalog names in brackets):

| System | Default | Alternatives |
| --- | --- | --- |
| Windows, Linux, macOS | SDL 3 [`sdl`] | raylib [`raylib`]; SDL's CPU renderer for machines without a GPU [`sdl-software`]; [`headless`] |
| Android, iOS | SDL 3 (the hosts in `platforms/`) | — |
| Browser | raylib in WebAssembly (`BrowserLauncher`) | — |

Reasons: SDL is the only candidate with maintained .NET packages for all five native systems; its renderer picks the
system's graphics API and produced identical frames on Direct3D, OpenGL, Vulkan and OpenGL ES; it removes our NDK
build and the raylib Android abort; it gives touch and the mobile lifecycle. raylib stays where it is proven (the
browser, where SDL3 has no package, and as a desktop alternative that also documents equivalence). Details:
[ADR 0017](../decisions/0017-sdl3-native-platform.md).

## 5. Compatibility matrix

| System | Profile | Build | Tests | Rendering | Input | Audio | Lifecycle | Real game | Real hardware |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Windows x64 | sdl | run | run | run (138 captures) | not exercised with real key events | SDL's device opened and played sounds (also under raylib's window) | n/a | four games scripted; two in real time | this machine (GPU); hand play pending |
| Windows x64 | raylib | run | run | run (138) | as before (PLATFORM-1) | as before | n/a | four games | yes (earlier) |
| Linux x64 | sdl | run (WSL) | run | run on llvmpipe (138); CI under Xvfb | not run | not run (no backend in WSL) | n/a | four games scripted | pending |
| Linux x64 | raylib | run | run | reference reproduced | CI (XTEST) | CI (PulseAudio) | n/a | four games | pending |
| macOS | sdl | CI only | CI only | CI only (Metal) | — | CI only | n/a | CI only | pending |
| Android x86_64 | sdl | run | — | run (25 captures, two games) | keys and touch run | device opens | run | two games, emulator | arm64 device pending |
| iOS | sdl | compiled (Windows) | — | CI only | — | CI only | CI only | CI only | pending |
| Browser | raylib (wasm) | run | Chrome scripts run | run (25 identical) | earlier suites | earlier suites | n/a | two games here, four earlier | earlier manual play |

## 6. Remaining limitations

- macOS and iOS have no run yet; their first evidence comes from the platform workflow on GitHub runners.
- No arm64 Android device and no iPhone were used; no person has played on Linux, macOS, Android or iOS.
- The on-screen controls are plain circles without labels, drawn over the frame; layouts per game do not exist.
- Text input, mouse and pointer input, haptics and screen safe areas (notches) are not handled.
- Gamepads on SDL were not tried with a physical controller.
- Browser packages carry about 150 KB of SDL code they never call.
- SDL's CPU renderer (`sdl-software`) has no recorded reference.
