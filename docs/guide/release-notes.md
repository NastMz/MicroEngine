# SDK release notes

One section per SDK version, newest first. The SDK is in Alpha: a version may change public APIs incompatibly, and
every such change is in the [migration notes](../migrations.md). Releases are tagged `sdk-v<version>`; the demo
releases (`v0.1.0`, `v0.2.0`...) are separate.

## Next release

Not published yet: merged on `main` after 0.4.0-alpha.2, in the next SDK version.

**New**

- [Action prompts](../manual/input.md#showing-controls-to-the-player): `Game.Prompts` names the control that presses an
  action on the device being played (keyboard, Xbox, PlayStation or Nintendo pad, touch layout), follows remapping and
  device changes; `Format("Press {confirm}")`, `TextAll`, `Directions`; `PromptText` draws them as glyphs;
  `DialogueBox.Prompts` resolves them in dialogue; the `prompts` script command.
- [Touch control looks](../manual/input.md#look-styles-skins-and-images): `TouchStyle` (shape, colours, outline,
  press feedback, images) per kind or per control, `ITouchSkin` for a look of your own, `IOverlayCanvas.Image`.
- [Start screen](app-identity.md#start-screen): Android and iOS launch screens, the browser loading page and the
  engine's screen while the game loads, in the game's colour and image, with optional "Made with MicroEngine"
  (`MicroEngineSplash*` properties, `AppIdentity.Splash*`, `StartScreen`, `RunOptions.FirstFrame`).
- Android adaptive and round icons at every density (`MicroEngineIconBackground`); a web app manifest for browser
  packages.
- `GameSettings.Letterbox` (the colour around the frame), `AchievementToasts.Bottom`, `PixelImage.Resized`,
  `PixelImage.FitInside`, `○ □ △` in the built-in font.
- [Frame pacing](../manual/lifecycle.md#fixed-step-and-frame-order): with VSync, frame time is counted in refresh
  periods (`IPlatform.DisplayRate`, `RunOptions.PaceToDisplay`, `--pacing`), so irregular wake-ups no longer give
  frames with no update followed by frames with two; `Game.FrameBlend` for drawing between updates on 75, 100 or
  144 Hz displays.
- [Measuring a game](../manual/lifecycle.md#measuring-a-game): `--profile FILE` (`FrameProfiler`, `FrameSummary`,
  `IPlatformDiagnostics`), per-frame times, allocations and start phases as JSON or CSV.
- [Audio for tests](../manual/audio.md#devices-and-silence): `--audio auto|device|silent` (`AudioOutput`),
  `--audio-capture FILE` and `CaptureAudioDevice`, `SdlAudioDevice.TryCreate(driver)`.
- `AudioMixer.AddMusic(name, Song)` renders songs in the background, out of `Load`.
- [Choosing a platform](../manual/backends.md#choosing-a-platform): the launcher tries the next platform when the
  default one cannot start; `--gpu` (`GpuPreference`).

**Changed**: `DialogueBox` continues a page that does not fit instead of cutting it; `BitmapFont.Wrap` breaks words
wider than a line; hidden and scripted runs open no sound device; see the [migration notes](../migrations.md#next-release).

**Faster**: the SDL window appears about a second sooner on some Windows machines (gamepads start behind the start
screen); the SDL renderer skips unchanged state; input sampling and touch controls allocate nothing per frame; hidden
raylib windows no longer wait for the display.

## 0.4.0-alpha.2

Published 2026-11-01 on nuget.org (GitHub release `sdk-v0.4.0-alpha.2`): fixes found while writing the developer manual. **Fixed**: scene changes requested during a
transition keep their order; `CoroutineRunner.StopAll` stops coroutines that had not run yet; a tap shorter than a
frame reports `Released`; `PlayMusic` with no fade starts at full volume; `Animator.Speed` refuses negative values;
`Synth.Render` gives no NaN samples for a sound of length 0; `Synth.Layer` refuses clips it would mix wrongly;
`ContentFolder.Exists` stays inside its folder and `PathOf` no longer accepts a sibling folder with the same prefix;
`SaveStore.Delete` lets an unreadable slot be saved again; launcher errors name the option with the wrong number;
`SelectionList` keeps its index valid; the template's first scene describes `CreateInput` correctly. Behaviour changes
are in the [migration notes](../migrations.md).

## 0.4.0-alpha.1

Published 2026-11-01 on nuget.org (GitHub release `sdk-v0.4.0-alpha.1`). The first public SDK: MicroEngine can be
installed, and a game created, run and distributed, with public tools.

```bash
dotnet tool install -g MicroEngine.Cli --prerelease
microengine new MyGame
```

**Packages** (all at 0.4.0-alpha.1, MIT, .NET 10):

| Package | What it is |
| --- | --- |
| `MicroEngine` | the backend-independent core, and the build targets for the game's identity |
| `MicroEngine.Sdl` | the SDL 3 platform (Windows, Linux, macOS, Android, iOS) |
| `MicroEngine.Raylib` | the raylib platform (desktop alternative, browsers), with raylib for WebAssembly |
| `MicroEngine.Desktop` | the desktop launcher with SDL 3, raylib, SDL's CPU renderer and headless |
| `MicroEngine.Cli` | `microengine`: new, run, build, package, info, doctor |
| `MicroEngine.Templates` | `dotnet new microengine-game` |

**New**

- `microengine package windows | linux | macos | android | ios | ios-simulator | web` ([Packaging](packaging.md)).
- [Application identity](app-identity.md) declared once in the project: name, id, version, description, icon,
  orientation; turned into each system's metadata and used at run time (`AppIdentity`, `PlatformRequest.Identity`).
- Signing with the developer's own keys from environment variables ([Signing](signing.md)).
- `microengine doctor` checks a machine for every target and explains how to install what is missing.

**Changed**: hosts and demo packaging; see the [migration notes](../migrations.md#040-alpha1).

**Known limitations**: Android App Bundles (`.aab`) are not built; iOS device builds and macOS notarization need the
developer's Apple credentials; Windows executables are not code-signed by the tool; desktop packages are x64 (Windows, Linux) and arm64 (macOS) only. See
[Requirements and limitations](../reference/limitations.md).

**Upgrading** from a game that referenced the engine's projects: replace the `ProjectReference` with
`<PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.1" />`, add the identity properties, and
package with `microengine package` instead of copying build output.
