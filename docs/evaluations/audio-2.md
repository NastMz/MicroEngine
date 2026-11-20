# AUDIO-2: bus effects, ramps and crossfades; what remains of position, formats and spatial sound

Issue NastMz/MicroEngine#177, part of [CONSOLIDATION-1](consolidation-1.md) (task T7, specs S4 and S7). Date:
2026-11-19. Decision: [ADR 0067](../decisions/0067-mix-graph-and-effects.md). The rule is the mission's: what must a
developer outside the repository write, not what the demos use or what a benchmark says.

## 1. What the code did (checked before designing)

| Finding of the audit | In the code |
| --- | --- |
| Voices are added straight into the output with the product of their bus volumes | `AudioEngine.Render`/`Mix`: `gain = voice.Volume * busGain`, `busGain = volume(bus) * busGain(parent)`; no per-bus buffer |
| No effect contract; `IAudioStream` is the only extension point | Confirmed: no type processes samples |
| No limiter although a bus goes to 4 | Confirmed; one existing test asserts a raw sum of 1.25 |
| Voice and bus calls do nothing on a device that is not the mixer | `AudioMixer` casts `_device as MixerAudioDevice`; otherwise `VoiceHandle.None`, and bus volumes kept in a dictionary |
| Changes are steps | `Op.Set` applies values at a frame; only `Stop` had a fade |
| `Play` drops a second play of a sound in one frame | Deliberate (`_lastPlayedFrame`): two hits in one update would double the volume. Left as it is; `PlayVoice` is the call without the rule |
| Decoders: `AddDecoder` protected, OGG music only, `.mp3` accepted by the loader on every backend | Confirmed (`MixerAudioDevice`, `AudioAssetLoaders`); not changed here (section 5) |
| Sounds cannot be unloaded | `LoadSound` appends to a list; not changed here (section 5) |

How the game thread already reached the render thread: one single-producer, single-consumer queue of commands, each
stamped with the output frame of its step; the render function drains it at the start of a block and applies each
command at its frame, splitting the block there. A full queue (a sink that stopped) keeps the latest values on the
main thread and resends them. Everything new uses that path.

## 2. Capabilities delivered

### 2.1 The mix graph

- **Need.** A pause menu that muffles the music, a cave, a radio voice, music under dialogue, a master that does not
  clip: any game with more than beeps. None could be written by a game: there was no place to process a group of
  sounds.
- **Today.** Impossible without replacing the mixer.
- **Reuse.** Nothing to adopt for the graph itself: it is forty lines around the existing loop. See 2.2 for DSP.
- **Target.** `Audio.Bus("music").Add(new LowPass { Cutoff = 800 })`; an effect is an object whose properties change
  while it plays.
- **Boundary.** The mixer must own the submix and the hand-off between threads; an effect's arithmetic can be a
  game's.
- **Formulation and bit-identical output.** A naive submix (every bus in its own buffer, then into its parent)
  changes the floats: `(sample * voiceGain) * busGain` summed per bus is not `sample * (voiceGain * busGain)` summed
  per voice. So a bus is mixed in its own buffer only when it has to be: it has effects, an effect listens to it, or
  its volume is fading. Every other bus is not mixed at all: its voices go straight into the nearest submix above it
  (or the output) with the product of the volumes on the way, computed by the same expression as before. A graph
  without effects therefore has no submix, and renders the same bits: the test recorded two hashes from the previous
  mixer (stereo `b0b529ac29b66037`, mono `4deb7b9708c708d7`, a 150-step scenario with nested buses, volume changes,
  pans, pitches, loops, fades, pauses and a stream, five block sizes) and the new mixer reproduces them, with the
  limiter on as well.
- **Order.** Buses are processed children first, and a bus after the buses its effects listen to; the main thread
  computes that order when a chain changes and refuses circles (`ArgumentException`).
- **Hand-off.** Chains travel as an immutable snapshot in one command; parameters and bypass as commands; all take
  effect at the frame of their step. No lock. A full queue keeps the latest and resends it. An effect that throws is
  removed from the mix on the render thread and reported (`Faulted`, `Error`, `FaultedEffects`).
- **Per voice?** No. A per-voice chain multiplies state by 32 voices and needs a second hand-off for effects that
  follow a voice's lifetime; the case it serves (one sound filtered alone) is a bus of its own, which costs one buffer.
  Revisit with spatial sound, where a low-pass with distance per emitter is the natural use.
- **Tail length.** Not in the contract: a submix is processed every block whether or not a voice plays, so tails ring
  out. Built-in effects flush values too small to hear so silence stays cheap.
- **Validation.** `AudioGraphBaselineTests`, `AudioGraphTests` (exact frames at four block sizes, order, sidechain,
  refusals, a throwing effect, mono, the queue full, allocation with every effect active).

### 2.2 Effects that ship

- **Reuse, verified on nuget.org on 2026-11-19.**

| Library | Latest | Licence | Dependencies | Verdict |
| --- | --- | --- | --- | --- |
| NAudio.Core | 3.4.0-preview.1 (2026-10-24) | MIT | System.Numerics.Tensors (net9.0) | Its DSP (`BiQuadFilter`, `SimpleCompressor`) is a few classes inside a device and file library; a dependency for two hundred lines, and its effects are sample-provider wrappers that pull, not blocks processed in place |
| NWaves | 0.9.6 (2021-10-06) | MIT | none | Broad DSP (filters, effects), no release in five years, written for offline signals (`DiscreteSignal` arrays); online filters exist but parameter changes are not smoothed and nothing is checked for trimming |
| Freeverb (Jezar at Dreampoint) | n/a | public domain | n/a | A design, not a package: adopted as the reverb's design |
| NLayer | 3.0.1 (2026-11-04) | MIT | none | Managed MP3 decoder; relevant to formats (section 5), not adopted here |
| NVorbis (in use) | pinned 0.10.5; 1.0.0-rc.2 exists | MIT | none on .NET 10 | Kept |

  The six effects are about 450 lines with their documentation. Adopting a library for them would add a dependency
  whose allocation behaviour in a render callback, trimming and browser behaviour would have to be proved anyway, to
  save code that is textbook (RBJ biquad, a feed-forward compressor, Freeverb). Written in the engine, against the
  same public class a game uses.
- **Delivered.** `LowPass`, `HighPass` (second order, coefficients glide), `Compressor` (threshold, ratio, attack,
  release, makeup, range, sidechain; `Compressor.Duck`), `Reverb`, `Delay`, `Limiter`.
- **Limiter default.** On in every device a game plays through; off in an `AudioEngine` created directly
  (`AudioEngineSettings.Limiter` null). "Unchanged" means: while the mix has not exceeded the threshold (and after a
  peak, once the gain has recovered) the limiter writes nothing, so every sample is bit for bit what the mixer
  produced; tested against the recorded hashes. It uses no look-ahead, because look-ahead is delay and delay would
  change every existing output. The one existing test that sums to 1.25 creates the mixer directly and is unchanged.
- **Validation.** `AudioEffectsTests`: filter responses against the analog prototype through the bilinear transform
  (0.5 dB), the compressor's curve (2 %), echoes of an impulse (1e-6), the room's decay between the bounds its comb
  lengths give. A mutation run (four effects broken on purpose) failed 12 of 18 tests.

### 2.3 Ramps, fades and crossfade

- **Need.** Every change of level in a game: fades, a sweep across the screen, a glide of pitch, track changes.
- **Today.** A step, or a value set again every update (zipper noise at 60 steps a second).
- **Target.** `Set(voice, volume:, pan:, pitch:, seconds:)`, `PlayParameters.FadeIn`, `bus.FadeTo(volume, seconds)`,
  `PlayMusic(next, crossfade: 1.5f)`.
- **Design.** Voice ramps are a step per output frame in the render function, computed from the end of the ramp
  backwards so no error accumulates; the main thread's timeline integrates a pitch ramp so it still knows when a clip
  ends without asking the sink. A fading bus becomes a submix for the length of the fade and goes back to being
  flattened at its last frame. Defaults are unchanged: `PlayMusic` without `crossfade` behaves as before.
- **Not done: snapshots of bus settings.** They did not fall out: a snapshot is a list of `FadeTo` calls plus effect
  properties, which have no common interpolation. A game writes the three lines.
- **Validation.** `AudioRampTests` (straight lines at 1e-5, exact end values, four block sizes, pause, pitch glide and
  the timeline's end, bus fades, crossfade through `Game.Audio`).

## 3. Decided and recorded

- **`IAudioDevice` and voices.** The seam for replacing audio output is `IAudioSink` (ADR 0033): a new output is a
  sink under the same mixer and gets voices, buses and effects for nothing. `IAudioDevice` remains the façade's seam
  for the silent and capture devices. Instead of moving twenty members into it, `Audio.SupportsVoices` says whether
  voices exist, and buses and effects work on any device (kept as state when nothing plays).
- **The retro song API** stays on the façade: it is three overloads of `AddMusic`, and moving them is a breaking
  change with no capability behind it.

## 4. State hash and determinism

Nothing new enters the `engine.audio` section: bus volumes were never in it, and effect parameters and fades are
presentation driven by game state that is hashed already. Hashes of existing games do not change. Rendering stays
deterministic and independent of block sizes with every effect active (tested).

## 5. What remains, and what this work changes about it

Slices 4 to 6 of the issue were not started; nothing of them is half in the code.

- **Time (position, seek, scheduled start, loop region).** The main thread now has `Settle` and `FramesLeft`, which
  give a clip's position at any frame including through pitch ramps: position is a read of that. A loop region needs
  the same two functions to wrap, and `SampleClip` to wrap at a region end. Streams need `IAudioStream` to grow a
  seek, and `StreamFeed` a generation so a seek can drop what is decoded ahead.
- **Formats and lifetime.** A decoder registry belongs on the device (`AddDecoder` made public plus a static list
  for built-ins); OGG for sound effects is `VorbisStream` read to the end at load. MP3: NLayer 3.0.1 is managed, MIT
  and without dependencies, so adoption is plausible, but its trimming and browser behaviour were not checked; until
  then the loader should refuse `.mp3` at load on devices that do not list it. Unloading needs sounds keyed by handle
  with a free list, since a playing voice holds its clip.
- **Spatial sound.** A scene system in the core (ADR 0060) that owns a listener and emitters and calls
  `Audio.Set(voice, volume, pan, seconds: one step)` each update; ramps make that click-free without new mixer code.
  Distance low-pass per emitter is the case for per-voice effects (2.1).

## 6. Not verified

Nothing was verified by ear. Browser, Android, iOS, Linux and macOS were not run. The cost of a submix was not
measured: the path without effects is the previous one, and no doubt needed a number.
