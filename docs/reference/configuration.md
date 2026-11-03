# Configuration and extensibility

This page collects every setting MicroEngine and its games read: the `GameSettings` a game declares, the
desktop launcher's command-line flags, the environment variables the four games use as developer shortcuts, the
browser packages' options, and the points where a game or a backend plugs its own behaviour into the engine.

## GameSettings

Declared by every game in `Settings`
([`src/MicroEngine/Core/Platform.cs`](../../src/MicroEngine/Core/Platform.cs)):

```csharp
public sealed record GameSettings(string Title, int VirtualWidth = 320, int VirtualHeight = 180, int Scale = 4)
{
    /// <summary>Fixed simulation rate in updates per second.</summary>
    public int UpdateRate { get; init; } = 60;
    public Color Background { get; init; } = Color.Black;
}
```

| Option | Default | Meaning |
| --- | --- | --- |
| `Title` | required | Window and page title |
| `VirtualWidth`, `VirtualHeight` | 320, 180 | The pixel-art canvas every scene draws into; the backend scales it by whole factors and letterboxes it |
| `Scale` | 4 | Initial window scale on the desktop (overridden by `--scale N`) |
| `UpdateRate` | 60 | Fixed simulation steps per second; `dt` is `1 / UpdateRate` |
| `Background` | black | Clear colour of every frame |

Neon Rift, for example, draws at 480x270.

## Command-line flags

Parsed by `GameLauncher.Run`, which `DesktopLauncher.Run` calls
([`src/MicroEngine/Core/GameLauncher.cs`](../../src/MicroEngine/Core/GameLauncher.cs)). A game that
starts through `DesktopLauncher` accepts them on its command line. Next SDK version (not published yet: arrives with the
next SDK version): the flags are parsed into a `LaunchRequest` (namespace `MicroEngine.Hosting`), the launch model
every host shares: a browser development package's `script=` and a phone app's script follow the same rules as
`--script` (deterministic time, seed 1, no sound device unless asked for), and a script that cannot be parsed exits
with code 2.

| Flag | Effect |
| --- | --- |
| `--script FILE` | Plays scripted input ([format](../examples/testing.md#level-3-input-scripts-and-captures)); implies a fixed step and seed 1 unless `--seed` is given (next SDK version: deterministic time unless `--time realtime`) |
| `--captures DIR` | Directory for script `capture` commands (default `captures`) |
| `--platform NAME` | Runs on that platform of the catalog (`sdl`, `raylib`, `sdl-software`, `headless`); default: `MICROENGINE_PLATFORM`, else the first that can run here (next SDK version: else every platform that can run here except `headless`, in order, until one starts, printing why each one failed) |
| `--list-platforms` | Prints the catalog, with the reason a platform cannot run here, and exits |
| `--hidden` | Renders in a hidden window with a fixed step; captures still work (next SDK version: a hidden window implies nothing about time or sound: it runs in real time and may open a sound device unless the run passes `--time deterministic`, `--script`, `--audio silent` or `--mute`) |
| `--time realtime\|deterministic` | Next SDK version: how the loop advances time. `realtime` follows the wall clock; `deterministic` runs exactly one fixed step per frame. Default: deterministic with `--script`, else the game's choice, else the platform's stated preset (headless: deterministic), else real time. A deterministic run also asks the platform not to wait for the display and, with `--audio auto`, for no sound device ([Time](../manual/lifecycle.md#time)) |
| `--headless` | Runs on `HeadlessPlatform`, without a window; no capture files are written; the same as `--platform headless` |
| `--frames N` | Stops after N updates |
| `--seed N` | Seed for `Game.Random` (otherwise random, or 1 under a script) |
| `--scale N` | Window scale |
| `--mute` | Sets sound-effect and music volume to zero |
| `--report` | Before the game starts, prints `platform: <name>` (with SDL's renderer, for example `sdl (SDL renderer metal)`) and `audio: available` or `unavailable`; used by the package checks |

Next SDK version (not published yet: arrives with the next SDK version):

| Flag | Effect |
| --- | --- |
| `--audio auto\|device\|silent` | Where sound may go. `auto` (default): a sound device for visible real-time runs only, none for `--hidden`, `--script` or `--headless` runs, so tests are never heard (next SDK version: a device for every real-time run, hidden ones included, none for deterministic-time runs: `--script`, `--time deterministic`, `--headless`); `device`: always (checks that must prove a device opens); `silent`: never |
| `--audio-capture FILE` | Plays nothing aloud and writes what would have been heard as a WAV (stereo, 44.1 kHz), mixed step by step from game time: the same run gives the same file |
| `--profile FILE` | Measures every frame (time between frames, events, update, drawing, presenting, steps, allocations, collections) and the start phases; prints a summary and writes JSON, or CSV when FILE ends in `.csv`. The first 60 frames are left out of the summary |
| `--state-trace FILE` | Hashes the simulation state after every fixed step and writes one line per step (update, hash, then each section's hash) when the run ends; two runs of the same build, script and seed write the same file ([State traces](../examples/testing.md#recording-and-comparing-traces)) |
| `--pacing display\|measured` | `display` (default): when VSync paces the frames, frame time is counted in whole refresh periods, which removes the stutter of irregular wake-ups; `measured`: frame time as measured |
| `--gpu auto\|high-performance\|power-saving` | The GPU to ask for on a machine with two. SDL passes it to its Metal and GPU renderers only; Direct3D 11 and OpenGL use the GPU the system assigns to the application (on Windows: Settings, Display, Graphics) |
| `--renderer NAME[,NAME...]` | Graphics APIs to try first, in order (SDL: `direct3d11`, `direct3d12`, `vulkan`, `opengl`, `metal`, `gpu`...); when none can start, SDL chooses and the reason is printed |
| `--vsync on\|off` | Whether frames wait for the display's refresh. Off: frames run as fast as they can, or at `--fps-limit` |
| `--fps-limit N` | Draws at most N frames per second, with or without VSync; `0` removes a limit the game sets. Scripted and hidden runs are never limited (next SDK version: deterministic-time runs are never limited) |
| `--window windowed\|fullscreen\|exclusive` | Window mode: a window, borderless full screen on its display, or exclusive full screen at the display's own mode |
| `--display N` | Opens on display N, counted from 0 (the order the system lists them in) |

Each of these replaces the matching `GameSettings.Graphics` option for one run; options not given keep the game's
choice ([Graphics options](../manual/backends.md#graphics-options)).

Exit codes: 0 success, 1 game or platform error (unknown or unavailable platform, no display, a native library the
system cannot load, such as SDL on a Linux with glibc older than 2.34), 2 bad arguments. An unknown argument is an error, which is why games take
developer options from environment variables instead.

## Environment variables

### Games

Read by each game's `Program.cs` or launch options.

| Game | Variable | Effect |
| --- | --- | --- |
| Meadow Run | `MEADOW_START` | `title`, `map`, `level`, `credits`, `gameover` or `sheet` |
| | `MEADOW_LEVEL` | Level number (1 to 5) |
| | `MEADOW_CHECKPOINT` | Checkpoint to start from |
| | `MEADOW_FORM` | `sprout`, `bloom` or `ember` |
| | `MEADOW_SAVE_DIR` | Separate save folder |
| Courtyard Key | `COURTYARD_START` | Checkpoint: `village`, `town`, `road`, `woods`, `bridge`, `ruins`, `dungeon`, `blocks`, `trial`, `channel`, `boss`, `ending` |
| | `COURTYARD_SAVE_DIR` | Separate save folder |
| Trail Duel | `TRAIL_DEBUG` | Jump to a screen or situation (`trainer`, `battle`, `evolve`, `credits` and others) |
| | `TRAIL_EVOLVE` | Species id that evolves under `TRAIL_DEBUG=evolve` |
| | `TRAIL_SAVE_DIR` | Separate folder for saves and achievements |
| Neon Rift | `RIFT_START` | `title`, `stage`, `card`, `gameover`, `ending` |
| | `RIFT_STAGE` | Stage number (1 to 3) for `stage`/`card` |
| | `RIFT_CHECKPOINT` | Start at checkpoint N |
| | `RIFT_WEAPON` | `Pulse`, `Spread`, `Lance` or `Rapid` |
| | `RIFT_GOD=1` | Shots and contact do not kill |
| | `RIFT_AUTO=1` | The test autopilot plays |
| | `RIFT_SAVE_DIR` | Separate folder for achievements |

The engine itself reads no environment variables.

## Browser packages

`microengine package web` builds them ([Publishing on the web](../examples/embedding.md)); `--dev` builds a development
package. What a package exposes to the page that serves or embeds it:

| Setting | Effect |
| --- | --- |
| URL parameters | Ignored by release packages. Development packages (`--dev`) read `UPPER_CASE=value` environment variables, `script=<name>` and `autostart=1` from the URL |
| iframe `allow="fullscreen; autoplay"` | Enables the package's Fullscreen button and Alt+Enter inside an iframe ([Embedding](../examples/embedding.md#the-iframe)) |

The browser host builds the game with the project's `MicroEngineGameFactory` expression, which should match its
desktop `Program.cs` ([Name, id, version and icon](../guide/app-identity.md)). Next SDK version (not published yet:
arrives with the next SDK version): the browser, Android and iOS hosts call the game's `Configure` entry point, the same
method its desktop `Program.cs` passes to `DesktopLauncher.Run`, so they run exactly what the desktop build runs
([One composition for every platform](../manual/backends.md#one-composition-for-every-platform));
`MicroEngineGameFactory` is deprecated and used only by a game without one. A scripted run in a development package
follows the desktop rules: deterministic time, seed 1, no sound device. The browser build uses invariant globalization,
partial trimming and reflection-based JSON for `SaveStore`.

## Default input actions

From `InputMap.CreateDefault()` in [`src/MicroEngine/Input/Input.cs`](../../src/MicroEngine/Input/Input.cs):

| Action | Keys | Pad |
| --- | --- | --- |
| `left` / `right` / `up` / `down` | Arrows, A / D / W / S | D-pad, left stick |
| `confirm` | Z, Space, Enter, J | A |
| `cancel` | X, Backspace, K | B |
| `action` | C, L | X |
| `pause` | Escape, P | Start |

## Extensibility points

| Point | How | Example |
| --- | --- | --- |
| Input bindings | Override `Game.CreateInput()` and call `Bind` | Meadow Run adds `jump` and `run` ([patterns](../examples/patterns.md#actions-never-keys)) |
| Scenes and overlays | Subclass `Scene`; override `IsOverlay`, `BlocksUpdateBelow`, `Enter`, `Exit`, `Resume` | Pause menus in every game |
| Transitions | Subclass `Transition(float duration)` and implement `Draw` | Built-ins: `Fade`, `Iris`, `Diamonds`, `Wipe` |
| Entities | Subclass `Entity`; override `Update`, `Draw`, `Depth`, `Removed` (removed in the next SDK version: keep a scene's objects in `Scene.Objects`, an `IWorld`; [The scene world](../manual/scenes.md#the-scene-world)) | Courtyard Key's `Actor` |
| UI style | Pass a `UiTheme` to `UseAchievements`, `AchievementsScene` and widgets | Each game's theme |
| Achievements | `Achievement.Icon`; `AchievementToasts.Top` and `Heading`; `AchievementsScene` `Title`, `CloseActions`, `Moved`, `Closed` | Neon Rift closes its list with `fire` too |
| Lighting | `new LightMap(device, glowImage)` with your own glow image | Meadow Run's banded halos |
| Content location | `new ContentFolder(root)` (default `<exe dir>/Content`); next SDK version: `host.Content` in the composition, for every host | Tests can point at another folder |
| Save medium | Implement `ISaveStorage`; pass it to `SaveStore`, or set `SaveStore.DefaultStorage` before games are built (next SDK version: `host.Storage` in the composition and `host.SaveStore(gameId)`) | `BrowserSaveStorage` (next SDK version: `BrowserStorage`) |
| Save location | `new SaveStore(gameId, directory)` | Every game's `*_SAVE_DIR` variable |
| Backend | Implement `IPlatform` and its device interfaces, offered as a `PlatformProfile` (next SDK version: `IPlatform` extends the host contract, `IPlatformHost`, and optional features are capabilities) | `SdlPlatform`, `RaylibPlatform`, `HeadlessPlatform` ([backends](../manual/backends.md)) |
| Host loop | Drive `GameLoop` or `GameSession` yourself, one `Tick` per frame (next SDK version: `GameHost` gives a host of your own the shared launch rules and the game's composition) | The browser host |

### Platform request options

Next SDK version (not published yet: arrives with the next SDK version). A host asks a platform for what it needs with
a `PlatformRequest`; every option is explicit, and none depends on how the loop advances time:

| Option | Default | Effect |
| --- | --- | --- |
| `Scale` | null (`GameSettings.Scale`) | The window's initial scale |
| `Hidden` | false | A hidden window; captures still work, and it never waits for the display |
| `WaitForDisplay` | true | Whether a visible window's frames may wait for the display (VSync as the game asks, and frames paced to it); false never waits. The launcher sets false for deterministic runs |
| `StartGamepadsAfterFirstFrame` | false | Starts gamepads only after the first presented frame, so a slow gamepad start never delays the window. The launcher sets it for real-time runs whose game shows a start screen |
| `AudioOutput` | `Automatic` | `Automatic` and `Device` open a sound device, `Silent` opens none. The launcher's `--audio auto` asks for `Silent` in deterministic runs |
| `Mute` | false | No sound device |
| `CaptureScale` | 3 for pixel art, 1 for smooth scaling | Size factor of screenshots |
| `Audio` | null | An audio device the platform uses instead of its own (where its profile allows it) |
| `ExtraInput` | none | Input sources read together with the platform's (where its profile allows it) |
| `Identity` | null | The game's application identity (title, id, icon, orientation) |
