# ADR 0033: One managed mixer over PCM sinks; a feeder thread is the default SDL sink

## Status

Accepted (2026-11-03). Builds on [ADR 0037](0037-platform-host-contract.md) (the platform contracts, which now hold
`IAudioSink` v1) and [ADR 0034](0034-content-handles-loaders-sources.md) (sounds and music as asset kinds). Evidence:
ARCH-1 §3 (D8 three mixers, S12 audio API), §6.4 (`IAudioSink` v1), §6.5 (audio: now, prepare, later), §9 (stage 2c,
the P5 verdict), the P5 report ([`experiments/arch-1/p5-audio-sinks`](../../experiments/arch-1/p5-audio-sinks/README.md))
and the browser determinism report ([`experiments/arch-1/browser-determinism`](../../experiments/arch-1/browser-determinism/README.md), R2).

## Context

Each backend mixed for itself: SDL bound sixteen round-robin audio streams, raylib kept four aliases per sound, and the
capture device used by tests mixed with no voice limit at all, so the reference could not detect voice stealing and a
game sounded different per backend (D8). `Play` returned nothing, only one music track existed, and music was fed from
the simulation step on the main thread; in browsers songs rendered in wall-clock slices inside the step, so the music
state Courtyard Key's rules read depended on CPU speed (R2). P5 measured the sink shapes on Windows, an Android
emulator and headless Chrome and Firefox: a pull callback on SDL's device thread loses audio on every long garbage
collection; pushing from the main loop loses about the stall minus the target everywhere; SDL's audio in browsers
drops on every main-thread stall; a sink-owned feeder thread with 60 ms queued lost nothing; an AudioWorklet queue
filled once per frame survives stalls up to its target; offline rendering was bit-identical on CoreCLR, Mono and the
.NET wasm interpreter.

## Decision

- **`IAudioSink` v1 in the platform contracts** (`MicroEngine.Platform`), exactly as frozen by P5: `AudioFormat`
  (interleaved float32), `AudioRender(Span<float>)` (one thread at a time; never blocks, allocates, locks or throws),
  `AudioSinkSchedule { SinkThread, HostPump }`, `LatencyFrames`, `Start(render)` (page hosts: inside the user gesture),
  `Pump()` (main thread, once per frame). The six threading rules are part of its documentation.
- **One managed mixer** (`AudioEngine` in the new `MicroEngine.Audio` assembly, which references the kernel, the
  platform contracts and content): voices reached through generational `VoiceHandle`s, one voice policy (a limit,
  32 by default; a new sound steals the oldest sound of the lowest priority if that priority is not higher than its own,
  otherwise it is refused), acyclic named buses under `master` (`sfx`, `music`, and any a game adds under an existing
  parent), stop with fade, volume, pan and pause changes, any number of music streams, linear resampling and balance
  panning. The policy runs on the main thread against the simulation's timeline, never against what the sink has
  rendered, so it is the same on every backend and in every run.
- **Exact-frame scheduling.** A sound starts at its step's output frame plus a fixed delay of at least the sink's
  latency; commands reach the render function through a single-producer, single-consumer queue and take effect at
  their frame (a stolen voice keeps playing until the new sound's frame; bus volumes change at theirs), so output does
  not depend on block sizes. In real time the timeline catches up with the sink when the game fell behind it; in
  deterministic runs (`Game.Audio.Deterministic`, set from the time mode) it follows the simulation only.
- **Streaming off the simulation step.** Music streams (OGG Vorbis, decoded in managed code with NVorbis on every
  backend; formats a backend adds, such as raylib's MP3) are decoded ahead by `Pump`, once per host frame in the
  schedule's `EndFrame` phase, into a ring the render function reads. Songs that render in slices (browsers) render
  there too, and `CurrentMusic` names a song from the moment it is asked for: logical music state no longer depends on
  rendering speed (R2). In deterministic runs a song is rendered only when the game waits for it, all of it.
- **Default sinks.** SDL3 platforms: a feeder thread keeping 60 ms queued in an SDL audio stream (`SinkThread`),
  configurable (`SdlAudioDevice.TryCreate(driver, latencyMilliseconds)`); **the default SDL sink is a feeder thread,
  not SDL's audio callback**. Browsers: an AudioWorklet queue the game fills up to 100 ms ahead once per frame
  (`HostPump`), with SDL and raylib audio not used; the page creates the AudioContext inside the starting gesture and
  suspends it while the page is hidden. raylib desktops: a raylib audio stream refilled by a feeder thread. Tests: an
  offline sink rendering exactly one simulation step per pump (`OfflineAudioSink`, `CaptureAudioDevice`).
- **Façades kept.** `IAudioDevice` stays the platforms' device contract; every backend device is a
  `MixerAudioDevice` (the mixer plus its sink), so `AudioMixer` (`Game.Audio`) and games are unchanged. The audio
  types move into `MicroEngine.Audio.dll` with their namespace, and `MicroEngine.dll` forwards them.

## Alternatives considered

- **Mixing per backend** (today's): three voice policies and a reference that cannot see stealing (D8).
- **miniaudio as the engine mixer**: a native dependency in the browser; kept as a possible future sink.
- **SDL's get callback as the default sink**: managed code on SDL's device thread loses 23-73 ms on 50-90 ms
  collections (P5); it can come back as a low-latency option for games that keep collections short.
- **Push from the main loop**: loses about stall minus target on every platform, and the loop owns audio timing.
- **SDL audio on the web**: dropouts on every main-thread stall whatever is queued (P5).

## Consequences

- Sounds behave the same on every backend, and tests hear what players hear, voice stealing included.
- The `MicroEngine` package depends on NVorbis (managed, MIT), which only the SDL package carried before.
- Sound starts about 60 ms (SDL), 85 ms (raylib desktop) or 100 ms plus the AudioContext's own latency (browsers) after
  the update that played it; SDL's device thread no longer runs managed code.
- In browsers a frame longer than about 100 ms is heard as a gap; streamed music is decoded on the page's thread once
  per frame and runs dry after a stall longer than half a second.
- Not done here: the low-latency SDL callback sink, listener and emitters, effects on buses, the SDL browser host's sink
  (stage 5), multithreaded wasm.
- Risks kept from P5: iOS is unverified, Android measured only on an emulator, browsers only headless.
