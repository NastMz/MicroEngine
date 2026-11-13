# AUDIO-1: exposed audio mixer controls and latency

Objective: assess, with measurements, what a game can control of the mixer through the public API, then implement the
public extensions the evidence shows are necessary. Issue NastMz/MicroEngine#124. Branch `feat/audio-1`.
Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/audio-1.md`](../../docs/evaluations/audio-1.md).

## Specs

- S1 Why: "The rebuilt audio engine has buses/voices and handles internally, while the convenient game-facing API may
  not expose all advanced capabilities. Assess the usability gap for 2D games without creating a sound-effect library."
- S2 Controls: "Public-API management of individual voices, buses, simultaneous music/streams, live volume/pitch and
  simple positional stereo control where needed."
- S3 Latency: "Playback latency and consistency of triggering audio from frame updates, especially native vs browser."
- S4 Reuse and profile: "Reuse the existing managed mixer and PCM sink; profile allocations and timing before changes."
- S5 Scope: "Distinguish generic mixer primitives from game-specific audio effects."
- S6 Done when: "Identify what already works and what public extensions, if any, are necessary, with a small game-side
  usage example and evidence for timing/latency claims."
- S7 Owner's standing rule for research issues (as TILE-1 S6): they end in implementation; the bar is whether an
  external developer could build that game today with the published SDK. Additive public API only; the hand-off between
  the game thread and the render function stays the one command queue; rendering stays allocation-free.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map the public audio surface against S2 | S1 S2 S5 | inline | pending | |
| T2 Baseline profile before any change: render time, allocations, trigger lead on the SDL sink | S3 S4 | inline | pending | |
| T3 `AudioEngine.SetPitch`: live pitch through the existing command queue | S2 S4 | inline | pending | |
| T4 `Game.Audio` (`AudioMixer`): voices with handles, voice control, buses, music layers | S2 S7 | inline | pending | |
| T5 Game-side example (sample region), manual, release notes, API reference | S6 | inline | pending | |
| T6 Profile after the change; evaluation `docs/evaluations/audio-1.md` | S3 S4 S6 | inline | pending | |

## Log

- L1 (2026-11-13) The issue, verbatim:

  > ## Why
  > The rebuilt audio engine has buses/voices and handles internally, while the convenient game-facing API may not
  > expose all advanced capabilities. Assess the usability gap for 2D games without creating a sound-effect library.
  >
  > ## Research
  > - Public-API management of individual voices, buses, simultaneous music/streams, live volume/pitch and simple
  >   positional stereo control where needed.
  > - Playback latency and consistency of triggering audio from frame updates, especially native vs browser.
  > - Reuse the existing managed mixer and PCM sink; profile allocations and timing before changes.
  > - Distinguish generic mixer primitives from game-specific audio effects.
  >
  > ## Done when
  > Identify what already works and what public extensions, if any, are necessary, with a small game-side usage example
  > and evidence for timing/latency claims.
  >
  > **Status:** Future candidate.
