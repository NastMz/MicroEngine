# ADR 0037: A platform host contract with capabilities, explicit options and no time coupling

## Status

Accepted (2026-11-03). Amends [ADR 0016](0016-platform-profiles.md) (how replaced parts are composed, and how optional
features are found) and [ADR 0027](0027-loop-owned-time.md) (which kept `IPlatform.RealTime` and
`PlatformRequest.FixedStep` until this split). Builds on [ADR 0026](0026-kernel-modules-and-schedule.md) (the kernel's
lifecycle events and capability registry). Numbered after the range 0028-0036 that [ARCH-1](../evaluations/arch-1.md)
§7 reserves. Evidence: ARCH-1 §3 (D1, S14), §6.3, §6.4 (platform part, P4's page-driven rules), §7, §9 (stage 1),
annex [A1](../evaluations/arch-1/A1-core.md) F1, F6, F8, F11, F12, F15; the frozen graphics contract v1
(`experiments/arch-1/graphics-spike/Contract`) for `ISurface`.

## Context

`IPlatform` mixed the host (clock, events, window) with the devices, and expressed optional features three ways:
default interface members (`DisplayRate`, `Wait`, `Touch`, `Suspended`), interfaces found by casting the platform
(`IDisplayControl`, `IPlatformDiagnostics`), and a wrapper (`ComposedPlatform`) that had to re-implement every one of
them whenever a host replaced audio or added input (D1: the wrapper silently dropped them once). Events were state
polled by the loop (`CloseRequested`, `Suspended`); no backend reported resizes or focus, although the kernel declared
the events. And three platform behaviours still followed the time mode through `PlatformRequest.FixedStep`: VSync and
display pacing, when the gamepad subsystem starts, and whether a sound device opens, plus `IPlatform.RealTime`, a
platform's preferred time mode.

## Decision

- **A `MicroEngine.Platform` assembly** that references only the kernel (the architecture test checks it) and ships
  inside the `MicroEngine` package like the kernel: `IPlatformHost` (`Clock`, `PumpEvents(IEngineEventSink)`,
  `MainSurface`, `TryGetCapability<T>`, `CanBlock`), `ISurface` exactly as frozen in graphics contract v1, `HostSurface`
  (a reusable surface that reports size changes the way the contract requires), `IDisplayPacing`, `IPreciseDelay`,
  `IStorage` (save slots) and `IContentSource` with `FolderContentSource`. `IPlatform` extends `IPlatformHost` with the
  renderer, devices and presentation, which stage 2 moves to their own contracts.
- **The contract is written down** in `IPlatformHost`'s documentation: one thread; events only from the pump, in order;
  `Suspending` and `Resumed` alternate, and a trip to the background between two pumps is delivered as both; one quit
  request per request; a resize raises the surface's event and the sink's in the same pump, once per change; a
  monotonic clock; capabilities found by query and fixed for the host's life; page-driven hosts (`CanBlock = false`)
  never block, read the surface size from the canvas after start and open audio inside the starting gesture (P4); and
  nothing a host does depends on the time mode.
- **Capabilities, not casts or defaults.** `IDisplayControl`, `IPlatformDiagnostics`, `ITouchScreen` (whose vibration
  is the haptics capability), `IDisplayPacing` and `IPreciseDelay` are found with `TryGetCapability<T>` everywhere;
  `Game.TryGetCapability<T>` falls back to the platform's. `IPlatform` and `IPlatformHost` have no default members, so a
  wrapper cannot silently miss one.
- **No wrapper for replaced parts.** `ComposedPlatform` is deleted. A backend builds its audio device with
  `PlatformRequest.CreateAudio` and its input with `CombineInput`; `PlatformProfile.Create` returns the backend itself
  and refuses one that ignored a part its profile allows.
- **Events are delivered.** SDL, raylib and headless raise `Resized` and `FocusChanged`; suspension and quit requests
  become events of the pump (`PollEvents`, `CloseRequested`, `Suspended` leave `IPlatform`). The loop turns them into
  the kernel's lifecycle events; events pumped before the game exists (during the start screen) reach it once it has
  loaded.
- **No hidden coupling with time.** `PlatformRequest.FixedStep`, `DesktopOptions.FixedStep` and `IPlatform.RealTime`
  are removed. `PlatformRequest.WaitForDisplay` (default true) and `StartGamepadsAfterFirstFrame` (default false) are
  explicit, and `AudioOutput.Automatic` opens a device on every platform. A run that chooses no time mode runs in real
  time whatever the platform. The launcher, a host, sets the options for the runs it starts: deterministic runs
  (`--script`, `--time deterministic`, and now `--headless` unless `--time` says otherwise) ask for no display wait and,
  with `--audio auto`, for silence; real-time runs that show a start screen ask for gamepads after the first frame.
- **A conformance suite** (`PlatformConformance`, beside `RendererConformance`) states the contract as tests; headless,
  SDL and raylib run it, and an event a host cannot be made to send in a test is reported as skipped, never passed.

## Alternatives considered

- **Forward the new members in `ComposedPlatform`.** Rejected: it fixes today's list and breaks at the next capability
  (A1 F1 alternative a).
- **Keep `IPlatform.RealTime` as a declared preference.** Rejected: it is the coupling ADR 0027 deferred; hosts say
  what they want (`RunOptions.TimeMode`, the launcher's `--headless`), the platform does not.
- **Move the window, devices and presentation out of `IPlatform` now.** Rejected for this step: the graphics device,
  input and audio contracts are later stages (2a, 2b, 2c); they stay reached through the host until then.
- **Remove `ContentFolder.DefaultRoot` and `SaveStore.DefaultStorage` now.** Rejected for this step: one composition
  path for every host (1C) replaces those statics; the contracts they will be replaced with exist now.

## Consequences

- Custom platforms implement the host members and declare their optional features as capabilities; wrappers forward
  `TryGetCapability`. Code that cast a platform asks for the capability instead.
- Tests and hosts that ran a headless platform through the loop and relied on one step per frame set
  `RunOptions.TimeMode` (or use `InitializeForTest(platform, seed, mode)`); the launcher's behaviour for `--headless`
  and `--script` is unchanged.
- A visible raylib window asked not to wait for the display no longer sets VSync, and visible deterministic raylib runs
  apply the game's window mode and display at start, as SDL already did.
- No host raises `ISurface.Lost` yet; device loss and surface recovery come with the graphics device (stage 2a).
- The mobile host logs suspension from the engine's lifecycle events instead of polling the platform.
