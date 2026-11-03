# ADR 0027: The loop owns time; backends keep only a monotonic clock

## Status

Accepted (2026-11-02). Amends [ADR 0023](0023-frame-pacing-and-measurement.md) (which runs open a sound device, and
fixed-step time chosen by the backend) and [ADR 0003](0003-scenes-entities-runtime.md) (five steps per frame as a
literal). Builds on [ADR 0026](0026-kernel-modules-and-schedule.md) (the kernel's schedule). Evidence:
[ARCH-1](../evaluations/arch-1.md) §3 (D4, S8, S9, S14), §6.4, and annex [A1](../evaluations/arch-1/A1-core.md) F4,
F5 and F8.

## Context

Each backend computed fixed-step time itself: SDL and raylib returned `frames / UpdateRate` as their clock when the
host asked for a fixed step, headless did the same unconditionally (and once hard-coded 60 Hz, D4). Every new backend
had to re-implement loop policy. The loop's limits were literals (five steps, a 0.25 s clamp). The launcher made a
hidden window imply both fixed-step time and silence (`FixedStep = script || hidden`, and `AudioOutput.Automatic`
opened no device for hidden runs): three independent concerns coupled.

## Decision

- **The loop owns the clock.** `IPlatform.Now` is a monotonic clock in seconds and nothing else on every backend
  (SDL ticks, raylib's timer, a stopwatch for headless). The kernel's `FrameClock` turns the elapsed time of each frame
  into fixed steps and the interpolation blend; the runner only measures, paces to the display (ADR 0023) and drives the
  schedule.
- **`TimeSettings` replaces the literals.** `FixedRate` (from `GameSettings.UpdateRate`), `MaxStepsPerFrame` (5),
  `MaxFrameDelta` (0.25 s) and `Mode` are explicit, validated values; a game changes them in `Game.Configure` with
  `builder.Configure<TimeSettings>(t => t with { ... })`.
- **Two time modes.** `RealTime`: each frame adds the measured time, at most `MaxFrameDelta`, and runs up to
  `MaxStepsPerFrame` steps, keeping at most one step pending when it hits the limit. `Deterministic`: each frame is
  exactly one step, whatever the wall clock says; the blend is 1 so captures show the last step exactly.
- **The mode is chosen explicitly.** `RunOptions.TimeMode` (the launcher's `--time realtime|deterministic`) wins over
  the game's `Configure`, which wins over the default. The default is the platform's `IPlatform.RealTime`: a declared
  preference, not a clock (headless prefers deterministic time; SDL and raylib prefer real time unless the request asked
  for deterministic time with `PlatformRequest.FixedStep`). A script implies deterministic time unless `--time` says
  otherwise. A hidden window implies nothing about time any more.
- **Audio follows the time mode, not the window.** `AudioOutput.Automatic` opens a physical device for real-time runs
  and none for deterministic-time runs (their sound would not follow the wall clock; checks stay silent). A hidden
  real-time run opens a device unless it passes `--audio silent` or `--mute`. `PlatformRequest.FixedStep` now only tells a
  backend that the run is deterministic, so it does not wait for the display or hold devices behind a start screen; it
  never changes the backend's clock.
- **The blend is exposed; engine interpolation comes later.** `FrameContext.Blend` and `Game.FrameBlend` carry the
  unsimulated part of a step; interpolating engine transforms (and teleports, world clocks) arrives with the world
  model (stage 3).
- **Frame-rate phases exist.** `Update` and `LateUpdate` run once per frame at the display's rate, after the frame's
  fixed steps, for camera smoothing, UI animation and interpolation.

## Alternatives considered

- **Fix the constant only (headless at the game's rate).** Rejected: the policy would still be duplicated in every
  backend.
- **Keep `IPlatform.RealTime` out of the decision entirely.** Rejected for now: tests and hosts that run a headless
  platform without options rely on deterministic time; the preference is explicit and documented, and the platform split
  replaces it with host options.
- **Silence every hidden run, as before.** Rejected: a hidden window is a presentation choice; checks that must be
  silent say so (`--audio silent`, `--mute`) or run in deterministic time.

## Consequences

- A new backend implements a monotonic clock and nothing about steps; scripted runs share one clock implementation on
  every backend.
- `HeadlessPlatform.UpdateRate` is gone and `HeadlessPlatform.Now` no longer counts frames.
- Hidden real-time runs (`--hidden` without a script) play sound on a machine with a sound device unless they pass
  `--audio silent` or `--mute`; package checks that need a device already pass `--audio device`.
- Touch controls are advanced by the frame's loop-owned delta, so their timing is the same at any update rate and on
  any backend.
