# ADR 0023: Frames paced to the display, measured by the engine, and checks that are never heard

## Status

Accepted. Builds on [ADR 0003](0003-scenes-entities-runtime.md) (the fixed-step loop), [ADR 0016](0016-platform-profiles.md)
(platform profiles) and [ADR 0017](0017-sdl3-native-platform.md) (SDL 3 native, raylib for browsers). Evidence:
[PERF-1](../evaluations/perf-1.md).

## Context

Players saw occasional stutter on a fast Windows machine at full frame rate. The engine had no way to measure a frame;
the loop advanced fixed 60 Hz steps by the time measured between frames and drew the last step; SDL's presentation
wakes the loop up irregularly around each refresh (p95 15.3 ms for a 13.34 ms period, every SDL driver), which turned
into frames with no step followed by frames with two when the display rate matched the update rate, and every display
not a multiple of 60 Hz repeated a frame regularly. Hidden and scripted runs (tests, package checks) played sound on
the developer's speakers. A failing default platform ended the launch.

## Decision

- **The engine measures itself, opt-in.** `FrameProfiler` and `--profile` record per-frame phases, steps, game time,
  allocations, collections, start phases and platform facts (`IPlatformDiagnostics`), exportable as JSON or CSV. Only
  what the engine sees is recorded; presentation times stay with the platforms' tools (PresentMon, Perfetto,
  Instruments, browser panels). Without `--profile` the loop measures nothing.
- **Time is counted in refresh periods when the display paces the frames.** `IPlatform.DisplayRate` (default 0) reports
  the rate VSync paces to (SDL: the window's display while VSync is on; raylib: the monitor rate its limiter targets on a
  visible desktop window). The loop rounds each frame's elapsed time to whole periods and carries the remainder, so the
  simulation still matches real time; frames shorter than half a period are taken as measured (VSync not honoured).
  `--pacing measured` (`RunOptions.PaceToDisplay = false`) restores the old timing for comparisons.
- **Interpolation is the game's choice.** `Game.FrameBlend` exposes the unsimulated part of a step; drawing between the
  previous and current state removes the repeated frames of 75/100/144 Hz displays. The engine does not interpolate on
  the game's behalf (it does not own the positions games draw), and existing games keep their rules; fixed-step runs
  see 1 (the last update, drawn exactly), so captures stay exact.
- **The fixed update rate stays.** Variable timesteps and update rates following the display were rejected: they make
  runs non-deterministic and change every game's tuning.
- **SDL_Renderer stays the native renderer, Direct3D 11 stays SDL's choice on Windows.** No driver was measurably better;
  SDL's `gpu` driver (SDL_GPU underneath, custom shaders since SDL 3.4) is the path for effects and future 3D, to be
  measured on phones and integrated GPUs first. A custom SDL_GPU renderer and a WebGPU browser backend were not adopted:
  no measured bottleneck needs them, and WebGPU does not cover enough mobile browsers yet.
- **The launcher falls back across platforms when none is named**, in catalog order without headless, reporting each
  failure; a named platform fails with its own error. SDL already falls back across its render drivers.
- **GPU preference is passed on only where it is real.** `GpuPreference` reaches SDL's Metal and GPU renderers;
  Direct3D 11 and OpenGL follow the system's per-application setting, which the engine does not change. The engine never
  forces the dedicated GPU.
- **Checks are never heard.** `PlatformRequest.AudioOutput`: `Automatic` opens a physical device only for visible
  real-time runs; `Device` is explicit (package checks, phone hosts); `Silent` never. `CaptureAudioDevice` records what
  would be heard, deterministically by game time, for tests and `--audio-capture`. It reproduces the engine's requests
  (volume, pitch, panning, music), not a sound card; SDL's own dummy and disk drivers remain available.

## Consequences

- Every performance claim can be reproduced with `--profile` and PerfBench, and compared run against run.
- Games on displays near the update rate lose their irregular double steps without any change; games that want smooth
  motion on other rates adopt `FrameBlend` themselves. The four demos have not yet.
- Hidden and scripted runs report `audio: unavailable` unless they pass `--audio device`; scripts that checked for a
  device were updated.
- A machine whose SDL cannot start now gets raylib (then SDL's software renderer) instead of an error, with the reason
  printed.
