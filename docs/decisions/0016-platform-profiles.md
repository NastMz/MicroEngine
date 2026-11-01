# ADR 0016: Platform profiles, replaceable subsystems and suspension

## Status

Accepted. Builds on [ADR 0007](0007-browser-host.md) (pluggable save storage). Evidence: [PLATFORM-3](../evaluations/platform-3.md).

## Context

`IPlatform` is the boundary between the engine and every backend, and it works: the four games never touch a
backend type. But a host could only take a backend whole. The desktop command line lived in the raylib assembly, a
host could not swap one subsystem (another audio device, an extra input source), and nothing expressed that some
parts cannot be swapped because they share the window and graphics context. Phones also send apps to the background,
which the loop did not know about.

## Decision

- **`IPlatform` stays.** It gains one member with a default, `bool Suspended => false`, so existing implementations
  keep compiling. While true, `GameLoop` neither simulates nor renders, pauses music, saves achievement progress and
  calls `Game.OnSuspended`/`OnResumed`; time spent away is not simulated.
- **Profiles.** A `PlatformProfile` is a named factory (`sdl`, `raylib`, `headless`, or a developer's own) with a
  support check and the parts a host may replace (`PlatformParts.Audio`, `PlatformParts.Input`). A
  `PlatformCatalog` lists profiles in order of preference; its default is the first that can run here.
- **Requests.** A `PlatformRequest` carries the window options and, optionally, an audio device factory and extra
  input sources. A backend opens no audio device of its own when one is supplied (`OpensOwnAudio`); the profile wraps
  the backend so the replacement is used and disposed. Window, renderer, graphics device and frame presentation are
  one group: they share a context, so they are replaced only by choosing another profile.
- **Errors, not surprises.** An unknown profile, a profile that cannot run here (raylib on iOS, SDL in a browser), a
  replacement the profile does not allow, or a render driver the system lacks all raise
  `PlatformConfigurationException` or `InvalidOperationException` with the reason; the launcher prints it and exits 1.
- **Selection belongs to the host.** `GameLauncher` (core) reads `--platform NAME`, `MICROENGINE_PLATFORM` and
  `--list-platforms`; `MicroEngine.Desktop.DesktopLauncher` passes the official desktop catalog. Games keep a
  one-line `Program.cs`. No dependency-injection container: a catalog and a request cover every case found.
- **Content and touch for every host.** `ContentFolder.DefaultRoot` lets a host point games at unpacked package files,
  as `SaveStore.DefaultStorage` does for saves. `TouchPad` (core) lays out an on-screen gamepad that presses the
  buttons games already bind, with quick taps latched like keys.

## Alternatives considered

- **Splitting `IPlatform` into independent interfaces chosen one by one.** Rejected: it would promise combinations
  that cannot work (an SDL renderer on a raylib window) and break every implementation.
- **A dependency-injection container.** Nothing needed more than a catalog and a request.
- **Hot swapping platforms during a run.** Not needed; a run uses one platform.

## Consequences

- The same game runs on any profile, and a host can replace audio or add input without touching game code
  (`PlatformEquivalenceTests` runs raylib with SDL's audio device).
- `LaunchArguments` moved from `MicroEngine.Raylib` to `MicroEngine.Core`; `MicroEngine.Raylib.DesktopLauncher.Run`
  keeps its signature and now also accepts `--platform raylib|headless`.
- A backend that has no background state keeps the default and needs no change.
