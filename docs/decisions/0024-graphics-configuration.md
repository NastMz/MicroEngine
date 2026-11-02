# ADR 0024: Graphics options chosen by the game, changed at run time, and a separate mission for a capability-based renderer

## Status

Accepted. Builds on [ADR 0016](0016-platform-profiles.md) (platform profiles), [ADR 0017](0017-sdl3-native-platform.md)
(SDL 3 native, raylib for browsers) and [ADR 0023](0023-frame-pacing-and-measurement.md) (frame pacing). Evidence:
[PERF-2](../evaluations/perf-2.md).

## Context

A game could not choose its graphics options: VSync was always on, there was no frame limit, the window opened where the
system put it, full screen was a toggle only, and the platform, graphics API and GPU could be chosen only on the command
line (`--platform`, `SDL_RENDER_DRIVER`, `--gpu`). A shipped game has no command line for its players, and a graphics
menu needs to change these while running. The reference machine has three displays (100, 75 and 60 Hz) and two GPUs.

## Decision

- **The game declares its graphics options in `GameSettings.Graphics` (`GraphicsSettings`)**: preferred platform
  (`Backend`), graphics APIs in order of preference (`Renderers`), `VSync`, `FrameRateLimit`, `Gpu`, `WindowMode` and
  `Display`. Every option is automatic by default and the defaults keep the previous behaviour. The launcher's options
  override them one by one for a run.
- **Preferences fall back, demands do not.** A preferred platform or graphics API that cannot start here is reported and
  the next one is tried; a platform named on the command line or in `MICROENGINE_PLATFORM` fails with its error.
- **VSync, the frame limit and the update rate are independent.** VSync waits for the display; the frame limit is the
  engine loop's own wait on an absolute schedule (the backend's precise timer: `SDL_DelayPrecise`, raylib's
  `WaitTime`), and with VSync off the loop paces time to the limit's periods as it does to refresh periods; the update
  rate stays the game's fixed simulation rate. A page-driven host that cannot block skips early frames instead.
- **Run-time changes go through `Game.Screen` (`GameScreen`) and an optional platform interface (`IDisplayControl`).**
  Platforms implement what they can (SDL and raylib desktop windows: VSync, window mode, display); browsers and phones
  keep their host's choices and say so. The platform, graphics APIs and GPU are start-time choices; a game saves
  `GameScreen.Current` and returns it from `Settings` at the next start.
- **Full screen means borderless by default.** On Windows the borderless window is presented directly to the display
  (PresentMon: independent flip, one refresh less latency than a window); SDL's exclusive mode measured no better and is
  offered only as an option. F11 and Alt+Enter switch to borderless.
- **Displays are reported and chosen by index; the pacing trusts the display's rate only while frames arrive at it.**
  On the reference machine a window composed by the desktop runs at the 100 Hz primary display's rate on the 60 Hz
  display, and a window moved between displays keeps its first rate, while full screen and windows the desktop
  promotes to independent flip run at their display's rate. The loop paces to the reported rate only while the median
  of the recent frames matches it, and counts time as measured otherwise.
- **Windows gamepads keep SDL's default, GameInput.** Its start waits about a second for its service on the reference
  machine (measured), once, behind the start screen; turning it off would save that second but fall back to drivers
  that do not handle every device (raw GIP devices such as Xbox One guitars need GameInput). Device support comes
  before start time.
- **A capability-based renderer is a separate mission (GRAPHICS-1).** Selecting graphics paths by capability (shaders,
  render targets, compute, 3D) means a renderer on SDL_GPU (with SDL_shadercross or precompiled shaders per backend)
  next to SDL_Renderer and raylib's WebGL, an interface for capabilities games can query, and measurements on phones,
  integrated GPUs and browsers. That is a considerable implementation with its own risks; PERF-2 keeps the existing
  renderers and their automatic choice, and adds explicit configuration that GRAPHICS-1 can extend (`Renderers`
  already accepts SDL's `gpu` driver, which supports custom shaders since SDL 3.4).

## Consequences

- Games can offer a graphics menu without platform code, and players without a command line get the options too.
- The launcher's options become overrides of the game's choices; `PlatformRequest.GpuPreference` (never published) moved
  to `GraphicsSettings.Gpu`.
- A platform that cannot change something reports it instead of failing; games read properties back.
- Connection, recognition, presses and hand-over between pads are tested with SDL's virtual pads; no physical
  controller was connected on the reference machine.
