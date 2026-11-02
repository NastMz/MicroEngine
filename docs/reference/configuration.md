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
starts through `DesktopLauncher` accepts them on its command line.

| Flag | Effect |
| --- | --- |
| `--script FILE` | Plays scripted input ([format](../examples/testing.md#level-3-input-scripts-and-captures)); implies a fixed step and seed 1 unless `--seed` is given |
| `--captures DIR` | Directory for script `capture` commands (default `captures`) |
| `--platform NAME` | Runs on that platform of the catalog (`sdl`, `raylib`, `sdl-software`, `headless`); default: `MICROENGINE_PLATFORM`, else the first that can run here (next SDK version: else every platform that can run here except `headless`, in order, until one starts, printing why each one failed) |
| `--list-platforms` | Prints the catalog, with the reason a platform cannot run here, and exits |
| `--hidden` | Renders in a hidden window with a fixed step; captures still work (next SDK version: opens no sound device unless `--audio device`) |
| `--headless` | Runs on `HeadlessPlatform`, without a window; no capture files are written; the same as `--platform headless` |
| `--frames N` | Stops after N updates |
| `--seed N` | Seed for `Game.Random` (otherwise random, or 1 under a script) |
| `--scale N` | Window scale |
| `--mute` | Sets sound-effect and music volume to zero |
| `--report` | Before the game starts, prints `platform: <name>` (with SDL's renderer, for example `sdl (SDL renderer metal)`) and `audio: available` or `unavailable`; used by the package checks |

Next SDK version (not published yet: arrives with the next SDK version):

| Flag | Effect |
| --- | --- |
| `--audio auto\|device\|silent` | Where sound may go. `auto` (default): a sound device for visible real-time runs only, none for `--hidden`, `--script` or `--headless` runs, so tests are never heard; `device`: always (checks that must prove a device opens); `silent`: never |
| `--audio-capture FILE` | Plays nothing aloud and writes what would have been heard as a WAV (stereo, 44.1 kHz), mixed step by step from game time: the same run gives the same file |
| `--profile FILE` | Measures every frame (time between frames, events, update, drawing, presenting, steps, allocations, collections) and the start phases; prints a summary and writes JSON, or CSV when FILE ends in `.csv`. The first 60 frames are left out of the summary |
| `--pacing display\|measured` | `display` (default): when VSync paces the frames, frame time is counted in whole refresh periods, which removes the stutter of irregular wake-ups; `measured`: frame time as measured |
| `--gpu auto\|high-performance\|power-saving` | The GPU to ask for on a machine with two. SDL passes it to its Metal and GPU renderers only; Direct3D 11 and OpenGL use the GPU the system assigns to the application (on Windows: Settings, Display, Graphics) |

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
desktop `Program.cs` ([Name, id, version and icon](../guide/app-identity.md)). The browser build uses invariant globalization,
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
| Entities | Subclass `Entity`; override `Update`, `Draw`, `Depth`, `Removed` | Courtyard Key's `Actor` |
| UI style | Pass a `UiTheme` to `UseAchievements`, `AchievementsScene` and widgets | Each game's theme |
| Achievements | `Achievement.Icon`; `AchievementToasts.Top` and `Heading`; `AchievementsScene` `Title`, `CloseActions`, `Moved`, `Closed` | Neon Rift closes its list with `fire` too |
| Lighting | `new LightMap(device, glowImage)` with your own glow image | Meadow Run's banded halos |
| Content location | `new ContentFolder(root)` (default `<exe dir>/Content`) | Tests can point at another folder |
| Save medium | Implement `ISaveStorage`; pass it to `SaveStore`, or set `SaveStore.DefaultStorage` before games are built | `BrowserSaveStorage` |
| Save location | `new SaveStore(gameId, directory)` | Every game's `*_SAVE_DIR` variable |
| Backend | Implement `IPlatform` and its device interfaces, offered as a `PlatformProfile` | `SdlPlatform`, `RaylibPlatform`, `HeadlessPlatform` ([backends](../manual/backends.md)) |
| Host loop | Drive `GameLoop` or `GameSession` yourself, one `Tick` per frame | The browser host |
