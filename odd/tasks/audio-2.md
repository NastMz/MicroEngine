# AUDIO-2: bus effects, ramps, playback position, decoders and 2D spatial audio

Objective: issue NastMz/MicroEngine#177, part of CONSOLIDATION-1 (`odd/tasks/consolidation-1.md`, task T7). Slices 1 to 3
(mix graph, effects, ramps) merged with PR #199 from `feat/audio-2`; slices 4 to 6 on branch `feat/audio-3`.
Delivery: one commit per slice, no push and no pull request without the owner. Test runner:
`dotnet test tests/MicroEngine.Tests -c Release --filter "FullyQualifiedName~Tests.Audio"`, `dotnet test tests/Samples.Tests -c Release`.
Evaluation: `docs/evaluations/audio-2.md`. Decisions: ADR 0067 (graph, effects, ramps), ADR 0072 (time, formats, spatial sound).

## Specs

- S1 (issue, Target) "Per-bus submix with an effect chain and a public effect contract; built-in low-pass, high-pass,
  reverb, compressor with sidechain, and a master limiter; ramped parameters; music crossfade". Done in PR #199.
- S2 (issue, Target) "position, seek, scheduled start and loop region". From the brief: "the playback position of a
  voice and of music (read it, seek to it), loop regions (an intro that plays once and a loop that repeats: loop
  start/end in samples or seconds, read from the file's metadata where the format has it), and what a rhythm or
  cutscene game needs to stay in step with the music (a position that accounts for the output latency the engine
  already measures)".
- S3 (issue, Target) "a public decoder registry with consistent formats across backends; unloading through asset
  scopes". From the brief: "a decoder registry so a format is added without touching the mixer; OGG Vorbis for sound
  effects [...], `.mp3` if a mature managed decoder with a compatible licence exists [...] the SDK supports trimmed
  and NativeAOT builds and browsers, so a decoder must work there or be declared), streaming vs decoded-in-memory as a
  choice per sound, and unloading sounds through the asset system".
- S4 (issue, Target) "a listener and emitters helper for 2D". From the brief: "a listener and emitters in world units:
  attenuation by distance with a curve, pan by horizontal offset, optional pitch by relative speed only if the method
  justifies it; as a scene system (ADR 0060) so a scene's emitters follow its entities, stop with it and are part of
  nothing a scripted run compares unless they should be (decide and say). The developer experience first: \"this
  entity makes this sound\" in one line."
- S5 (issue, Acceptance) "Same output on SDL, raylib and browser sinks (all mix in managed code); deterministic
  rendering preserved." "Real-time safety of the render path checked (no allocation per block)." "Manual, sample,
  release notes, ADR."
- S6 (brief, constraints kept true) Everything crosses the existing single-producer queue as value commands or
  immutable snapshots; no lock and no allocation on the audio thread in the steady state; every time-to-frames
  conversion goes through `Frames()`; NaN, negative, zero, infinite and enormous arguments are tested for everything
  added; the bit-exact baseline (`AudioGraphBaselineTests`) passes unchanged; nothing was heard on a device.

## Tasks

- [x] T1 S1 Mix graph, effects, limiter, ramps, crossfade. PR #199 (`feat/audio-2`).
- [ ] T4 S2, S5, S6 Position, seek, scheduled start, loop regions, the music's clock. Route: inline. Commit: see Log.
- [ ] T5 S3, S5, S6 Decoder registry, OGG sound effects, MP3, streamed or decoded music, unloading. Route: inline.
- [ ] T6 S4, S5, S6 Listener and emitters as a scene system. Route: inline.
- [ ] T7 S5 Evaluation, ADR 0072, release notes, limitations, API reference. Route: inline.

## Log

- L1 (2026-11-18, the issue, verbatim) "## Problem Every voice is added straight into the output with flattened bus
  gains: a bus is only a volume. No effect, filter or limiter can be inserted by a game or a package, which makes
  reverb, low-pass (pause menu, underwater), compression and sidechain ducking impossible, not merely manual. The
  recorded reason (AUDIO-1 #124) is \"nothing measured asks for them\". Also: the voice and bus API silently does
  nothing on any device that is not the built-in mixer; no parameter ramps (clicks), no fade-in or crossfade, no
  playback position, seek or loop points, no public decoder registration, `.mp3` music loads everywhere and decodes
  on one backend, sounds cannot be unloaded, and spatial pan and attenuation are rewritten per game. ## Target
  Per-bus submix with an effect chain and a public effect contract; built-in low-pass, high-pass, reverb, compressor
  with sidechain, and a master limiter; ramped parameters; music crossfade; position, seek, scheduled start and loop
  region; a public decoder registry with consistent formats across backends; unloading through asset scopes; a
  listener and emitters helper for 2D. ## Acceptance - [ ] A game adds a low-pass to the music bus and ducks music
  under dialogue with no engine internals. - [ ] A custom effect written by a game is inserted through the public
  contract. - [ ] Same output on SDL, raylib and browser sinks (all mix in managed code); deterministic rendering
  preserved. - [ ] Real-time safety of the render path checked (no allocation per block). - [ ] Manual, sample,
  release notes, ADR."
- L2 (2026-11-20) This record did not exist for slices 1 to 3; their evidence is in `docs/evaluations/audio-2.md`
  sections 1 to 4 and ADR 0067.
- L3 (2026-11-20, T4) Found while testing: a stream that does not loop never played its last frame (`StreamFeed.Fill`
  kept an index from before its buffer moved). Fixed with a test. Also found: a stream's end on the timeline ignored
  the time it had been paused (`End = Start + Produced`), so music paused and resumed was "over" early for
  `IsPlaying` and `CurrentMusic`; the end now comes from the position, which pauses and seeks move. The baseline hashes
  are unchanged by both.
