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
- [x] T4 S2, S5, S6 Position, seek, scheduled start, loop regions, the music's clock. Route: inline. Commit 2673b795.
- [x] T5 S3, S5, S6 Decoder registry, OGG sound effects, MP3, streamed or decoded music, unloading. Route: inline.
  Commit d2dce9be.
- [x] T6 S4, S5, S6 Listener and emitters as a scene system. Route: inline. Commit eddfd3e1.
- [x] T7 S5 Evaluation, ADR 0072, release notes, limitations, API reference. Route: inline. The commit after eddfd3e1.

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
- L4 (2026-11-22, T5) Measured before choosing how streams seek (a probe project outside the repository, a chirp so
  that positions cannot alias): NVorbis 0.10.5 `SamplePosition = n` lands off the place asked for (short mono file:
  +239 frames, and at frame 512 for `SamplePosition = 0` after reading; 20 s stereo file: -128 and +128 frames, exact
  only at 0); NVorbis 1.0.0-rc.2 lands on the frame but gives samples up to 1.3 away from a straight decode (louder
  than the signal) for the first 600 to 3600 samples; NLayer 3.0.1 gives silence and then differing samples after
  `Position = n`. None can serve a loop seam or a clock a game follows. Decoding speed, same probe: NVorbis decodes
  20 s of 44.1 kHz stereo in 117 to 219 ms and 98 s (Neon Rift's city.ogg) in 255 to 358 ms after the first run.
  Hence `ExactSeekStream`: forward by decoding and dropping, backward from the file's start, and a second decoder
  brought to a loop's start while the loop plays. Tested against the whole decode at 1e-6.
- L5 (2026-11-22, T5) NLayer 3.0.1 (MIT, no dependencies, net8.0 and netstandard2.0 builds): a console project
  referencing it was published trimmed (`TrimMode=full`, `TrimmerSingleWarn=false`, self-contained, win-x64) with no
  IL warning and decoded an MP3 file correctly when run. NativeAOT was not published: this machine has no platform
  linker. Not run in a browser.
- L6 (2026-11-22, T6) Decided: emitters are not in the state hash (a test compares a silent and a capture run). A
  loop waits for a free voice instead of stealing: the first version let equal-priority loops take voices from each
  other at every step when more were in range than there are voices (seen in the two-hundred-emitter test).
- L7 (2026-11-22) Looked at: captures `41-music-time.png` and `42-spatial-sound.png` of the examples' tour (hidden
  window, muted, so on a silent device). Not heard on a device. Outside the allowed surface and left for the owner:
  `src/MicroEngine.Cli/Content.cs` still warns that MP3 plays on raylib only (and `tests/MicroEngine.Cli.Tests`
  expects it); `src/MicroEngine.Raylib/RaylibAudioDevice.cs` registers an MP3 decoder that is no longer reached;
  `docs/guide/introduction.md`, `docs/guide/cli.md`, `docs/guide/licensing.md`, `docs/reference/components.md`,
  `docs/manual/scenes.md`, `README.md` and the site's data still describe MP3 as raylib-only, OGG as music-only or do
  not list NLayer and `Scene.Sounds`. Left open as found: `AudioEngine.CreateBus` and the audio thread.
