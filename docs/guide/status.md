# Status and limitations

**MicroEngine SDK — Alpha, 0.x.** MicroEngine is a working engine with four complete games, open source under the MIT
License and used from its public repository (no published packages yet). It runs on six official targets, each
validated to a stated level. Public APIs may still change incompatibly between Alpha versions
([SDK status](sdk.md)). This page states plainly what exists today, what does not, and what has and has not been
validated, so you can judge whether it would fit your project.

## What exists

| Area | State | Evidence |
| --- | --- | --- |
| Core engine (`src/MicroEngine`) | Loop, scenes, input (digital and analog actions, per-game touch controls), graphics model, tile world, audio synthesis, UI, saves, achievements | [Architecture](../architecture.md), the engine's test project |
| SDL 3 platform (`src/MicroEngine.Sdl`) | Window, 2D renderer (Direct3D, Metal, OpenGL, OpenGL ES, Vulkan or software), audio, keyboard, gamepad, on-screen touch controls, phone lifecycle; the default on Windows, Linux, macOS, Android and iOS | Scripted captures on real Windows hardware, Linux (Mesa), macOS (CI), the Android emulator and the iOS simulator ([Platforms](platforms.md)) |
| raylib platform (`src/MicroEngine.Raylib`) | raylib window, renderer, audio, keyboard and gamepad; the browser platform and a desktop alternative (`--platform raylib`) | Played on Windows; browsers ([Platforms](platforms.md)) |
| Platform selection | `PlatformCatalog` of named profiles, `--platform`, replaceable audio and extra input sources | [Backends](../manual/backends.md#choosing-a-platform) |
| Browser builds | raylib compiled to WebAssembly; `BrowserLauncher`, `localStorage` saves, one shared host | Automated browser tests and manual validation ([Platforms](platforms.md)) |
| Phone hosts (`platforms/`) | Android and iOS hosts running any of the games on SDL | Emulator and simulator runs ([Platforms](platforms.md#phones-and-tablets)) |
| Demo downloads (`packaging/`) | Windows ZIP, Linux archive, macOS app, Android APK for each demo, each checked as the downloaded file and published with a release | [Downloads](downloads.md), [packaging](../../packaging/README.md) |
| Games | Meadow Run, Courtyard Key, Trail Duel, Neon Rift, each with five achievements and saves | Each game's test project; playable on the [demos page](/demos/) |
| Sample | Hello Engine, the smallest complete game | [Hello Engine walkthrough](../examples/hello-engine.md) |
| Scripted runs and captures | `--script`, `--hidden`, `--captures` on every game; one visual reference per renderer kind | [Testing](../examples/testing.md) |

## What does not exist

These are not implemented. Do not plan around them.

| Missing | Notes |
| --- | --- |
| Published NuGet packages, project templates (`dotnet new`), an installer | Prepared for SDK-1: `dotnet pack` produces the packages, nothing is published ([SDK status](sdk.md)) |
| Visual editor, level editor or asset pipeline tool | MicroEngine is code-first: content is authored in code (`PixelImage.FromMap`, `TileLayer.Parse` over ASCII rows) or loaded through `ContentFolder`. A visual editor is only a long-term vision on the roadmap |
| An iOS download | Distributing to iPhones needs Apple signing ([Downloads](downloads.md#ios)) |
| An ECS, physics engine, networking, scripting language or 3D | Not part of the engine's design ([Architecture](../architecture.md)). 3D is only a long-term vision on the roadmap |
| Cloud saves | Saves stay on the machine (files) or in the browser (`localStorage`) |

## Platforms and validation

| Target | Status |
| --- | --- |
| Windows x64 | Played by hand with raylib; SDL (the default) checked on real hardware: 138 captures, audio device, the four downloads, also played by hand (keyboard and an Xbox controller) |
| Linux x64 | Checked automatically on every engine change (build, tests, rendering, keyboard and audio under a virtual display and sound server) and the download in Ubuntu 22.04, Debian 12 and Fedora 41 containers; the four downloads played by hand on Debian 13.7 and Ubuntu 24.04.4 LTS, with keyboard and an Xbox controller |
| macOS (Apple silicon) | Checked automatically on hosted Macs (tests, 138 captures on Metal, the downloadable apps); no person has played on a Mac |
| Android | All four demos checked on the x86_64 emulator from their release APKs (captures, audio device, keys, touch, background and back); the four APKs played by hand on a Redmi Note 14 Pro (Android 16), with touch and a Bluetooth PlayStation controller |
| iOS | Demos checked in Apple's simulator (captures, audio device, background and back, scripted touches); no iPhone, no download |
| Browsers | The four demos verified with automated tests in Chrome and Firefox on desktop, plus manual validation; on phones and tablets played with real multi-touch in emulated mobile Chrome ([INPUT-1](../evaluations/input-1.md)) and played by hand in Chrome on an Android 16 phone; Edge on the desktop played by hand; Safari not tested |
| Gamepads | Validated by hand in the browser (Edge on the desktop with an Xbox controller, Chrome on Android with a PlayStation controller over Bluetooth), with raylib on Windows, and with SDL: an Xbox controller on the desktop and a PlayStation controller over Bluetooth on Android |

Details per target: [Platforms](platforms.md) and the site's [compatibility matrix](/platforms/).

## Known open items

- Neon Rift's browser download is 11.9 MB with Brotli, mostly OGG music.
- raylib's web audio uses the deprecated `ScriptProcessorNode`; sound stops while the tab is hidden.
- One Firefox scripted-capture run under heavy machine load diverged from desktop; repeated alone it matched.
  The cause was not found.
- Browser saves live in `localStorage` (about 5 MB per origin, evictable, discarded by private windows).
- macOS downloads are not notarized and Windows downloads are not code-signed: both systems warn on first launch.

The full list with evidence is in [Known limitations](../reference/limitations.md).

## Planned work

The [roadmap](/roadmap/) lists planned work without dates: SDK-1 (NuGet packages, templates, versioned SDK releases),
validation on real devices, engine robustness and research into the mobile experience. A visual editor and 3D appear
there only as a long-term vision, not as available or committed features.

## Stability of the API

MicroEngine is in Alpha (0.x): there is no 1.0 and no backward-compatibility promise. Types documented in
[Contracts](../reference/contracts.md) are the ones the four games depend on; changes to them go through recorded
design decisions and are listed in the [migration notes](../migrations.md).
