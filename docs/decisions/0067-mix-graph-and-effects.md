# ADR 0067: A bus is a submix when it needs to be; effects are a public class fed through the command queue

## Status

Accepted (2026-11-19). The SDK is in Alpha. Evidence: [AUDIO-2](../evaluations/audio-2.md). Issue
NastMz/MicroEngine#177, part of [CONSOLIDATION-1](../evaluations/consolidation-1.md). Builds on
[ADR 0033](0033-one-mixer-over-pcm-sinks.md), whose mixer, command queue and `IAudioSink` v1 are unchanged, and amends
its "buses are volumes" consequence.

## Context

A bus was a gain: voices were added straight into the output with the product of their bus volumes, so nothing could
process a group of sounds. Reverb, filters, compression and ducking were impossible for a game, not merely manual.
Changes to volume, pan and pitch were steps, and music changed track by fading out and then in. The earlier reason
for leaving this out ("nothing measured asks for it") is not accepted by CONSOLIDATION-1.

Two constraints shaped the design: a mix without effects must render the same bits as before (existing games and
their rendered-audio tests), and the render function must stay free of allocation, locks and exceptions on every
sink, including the browser's, which renders on the main thread.

## Decision

- **Submix on demand.** A bus is mixed in a buffer of its own only while it has effects, an effect listens to it, or
  its volume is fading. Any other bus is flattened as before: its voices go into the nearest submix above (or the
  output) with the product of the volumes on the way, by the same expression. A graph without effects has no submix
  and is bit-identical to the previous mixer (recorded hashes, tested). A submix runs its chain, then its volume, then
  is added to the submix above. Blocks are mixed in segments of at most 1024 frames, the length of a submix buffer.
- **`AudioEffect`** is the public contract, an abstract class: `Prepare` (game thread, allocate), `OnParameter`,
  `Reset`, `Process(Span<float>)` and `Process(buffer, sidechain)` (render thread). A class, not an interface, because
  the hand-off lives in it: a property setter calls `SetParameter`, which sends the value through the mixer's
  existing single-producer, single-consumer queue, stamped with the frame of the step; the render thread delivers it
  at that frame. Chains travel as one immutable snapshot per change; bypass as a command. No lock; a full queue keeps
  the latest and resends it.
- **Sidechain** is a bus name on an effect. The main thread orders buses (children first, listened-to buses before
  their listeners) and refuses circles.
- **Faults.** An effect that throws on the render thread is taken out of the mix and reported; nothing escapes
  `Render`.
- **Built-in effects** (`MicroEngine.Audio.Effects`): `LowPass`, `HighPass`, `Compressor` (with sidechain and
  `Compressor.Duck`), `Reverb` (the Freeverb design, public domain), `Delay`, `Limiter`. Written in the engine against
  the public class; no dependency added.
- **A limiter after `master`**, with no look-ahead, that writes nothing while the mix is within its threshold. On in
  the devices games play through (`MixerAudioDevice`), off in an `AudioEngine` created directly unless
  `AudioEngineSettings.Limiter` says otherwise.
- **Ramps.** `Set`/`SetPitch` take a time, `PlayParameters.FadeIn`, `AudioBus.FadeTo`, `PlayMusic(name, crossfade:)`.
  One step per output frame, computed from the end of the ramp backwards; defaults unchanged.
- **Effects per bus, not per voice.** A sound that needs its own processing plays on its own bus.
- **`AudioBus` exists without a mixer**: on a silent device buses keep their volumes and effects, with the same
  checks, and `Audio.SupportsVoices` says whether voices play.
- **The state hash** gains nothing: effect parameters and fades are presentation.

## Alternatives rejected

- **Every bus a submix.** Simpler, but changes the floats of every existing mix (the order of multiplication), and
  costs a buffer and a pass per bus for games that use none.
- **Parameters as shared volatile fields.** No queue, but a change would take effect at whatever block the render
  thread is on: not at the frame of its step, and dependent on block sizes, which breaks deterministic rendering.
- **A lock around the chain.** Forbidden by `IAudioSink` v1.
- **A DSP library** (NAudio's DSP, NWaves): a dependency to prove under trimming, NativeAOT and the browser for a few
  hundred lines of textbook code; neither processes blocks in place with smoothed parameters.
- **A look-ahead limiter.** Cleaner peaks, but it delays the whole mix and so changes every existing output.
- **Limiter on in `AudioEngine` by default.** One existing engine test asserts a sum of 1.25, which no device can
  play; the mixer created directly stays literal and the device adds the policy, as it already does for music
  priority.

## Consequences

- A game adds effects, ducks music and fades anything with one call each; a package can ship effects.
- Switching a bus between flattened and submix (when a fade starts or ends) changes its samples by rounding only
  (about one unit in the last place), at an exact frame.
- An effect removed from a chain is cut, tail included; bypass keeps it.
- Binary-breaking signature changes are listed in the migration guide; source compiles unchanged.
- Playback position and loop regions, a public decoder registry, MP3, unloading and spatial sound are not part of
  this record; AUDIO-2 section 5 says what each needs.
