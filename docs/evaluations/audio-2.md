# AUDIO-2: bus effects, ramps and crossfades; then position, formats and spatial sound

Issue NastMz/MicroEngine#177, part of [CONSOLIDATION-1](consolidation-1.md) (task T7, specs S4 and S7). Date:
2026-11-19 (sections 1 to 6) and 2026-11-22 (sections 7 to 10). Decisions: [ADR 0067](../decisions/0067-mix-graph-and-effects.md) and
[ADR 0072](../decisions/0072-audio-time-formats-and-spatial-sound.md). The rule is the mission's: what must a
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

## 5. What remained after the first part

Slices 4 to 6 of the issue (time, formats and memory, spatial sound) were not part of the first pull request
(#199). They are sections 7 to 10 below and [ADR 0072](../decisions/0072-audio-time-formats-and-spatial-sound.md).

## 6. Not verified in the first part

Nothing was verified by ear. Browser, Android, iOS, Linux and macOS were not run. The cost of a submix was not
measured: the path without effects is the previous one, and no doubt needed a number.

## 7. Time: position, seek, scheduled start, loop regions (2026-11-20)

- **A. Need.** A rhythm game judges a key press against the music; a cutscene cues pictures from it; a game saved in
  the middle of a track wants it back there; nearly every soundtrack has an intro that must not repeat. Today's cost
  to a developer: impossible for position and seek (nothing exposes them), and for the intro two pieces of music and
  a timer that guesses when the first ends, off by the output latency and by every slow frame.
- **B. Before.** `l.Position` existed on the game's thread only to move a clip's end when its pitch changed.
- **C. Alternatives.** There is nothing to adopt: this is the mixer's own bookkeeping. Conventions adopted instead of
  invented: the sampler chunk of WAV (`smpl`, what audio editors write for loop points) and the `LOOPSTART`,
  `LOOPLENGTH`, `LOOPEND` comments of OGG files (what RPG Maker and the tools after it read and write).
- **D. Target, written first.** `Audio.AddMusicFile("field", "field.ogg"); Audio.PlayMusic("field");` loops over the
  file's region with nothing else; `Audio.MusicHeardPosition` in an update; `Audio.SeekMusic(saved)`;
  `new PlayParameters { Delay = beat }`.
- **E. Boundary.** Position and loop wrap must be the mixer's: only it knows frames. What a beat is, is the game's.
- **F. Integration.** Positions are computed from the timeline (the game's thread), not read from the audio thread:
  deterministic, equal in every run and on every sink, and in no hash (they are a function of steps that already
  are). A seek is one more command at a frame. Streams seek by a new feed carried in the command. The silent device
  gets a clock in the façade so headless runs behave alike.
- **G. Trade-offs.** A position moves once per step, not continuously: a game that wants it between steps
  interpolates. The heard position trusts the sink's declared latency; the device's own buffer beyond it is not known
  to the engine. A stream that falls behind its decoding is behind its position until sought.
- **H. Decision.** Extend.
- **I. Validation.** `AudioPositionTests` (24 tests): every clip and stream holds its frame numbers as samples, so
  each output frame says which source frame it played. Exact frames at four block sizes for start offsets, scheduled
  starts, seeks, seeks of several steps waiting, loop seams; position and heard position through pause, pitch and
  another sample rate; NaN, negative, zero, infinite and enormous times for every new argument; zero bytes allocated
  by the render function with seeks, regions and scheduled sounds; the WAV sampler chunk. The recorded baseline
  hashes pass unchanged.
- **Found on the way.** A stream that does not loop never played its last frame (an index kept across a buffer
  move). A stream's end on the timeline was `Start + Produced`, which ignores a pause made before decoding finished,
  so long music paused and resumed was reported as ended early (read in the code; the new test for it passes, the
  old code was not run against it).

## 8. Formats and memory (2026-11-22)

### 8.1 A list of decoders, MP3, OGG for effects

- **A. Need.** Any game with recorded sound. A developer with an MP3 or an OGG effect got "Not a WAV file", or music
  that loaded everywhere and played on one backend; a format of their own meant subclassing a device.
- **C. Alternatives, verified on nuget.org on 2026-11-20 and by running them.**

| Library | Version | Licence | Dependencies | Verdict |
| --- | --- | --- | --- | --- |
| NVorbis (in use) | 0.10.5 pinned | MIT | none on .NET 10 | Kept. Decodes 20 s of 44.1 kHz stereo in 117 to 219 ms. Its seek is not exact (8.2) |
| NVorbis | 1.0.0-rc.2 | MIT | none on .NET 10 | Not adopted: a prerelease, and after a seek its first 600 to 3600 samples differ from a straight decode by up to 1.3 (the signal was 0.3) |
| NLayer | 3.0.1 (2026-11-04) | MIT | none | Adopted for MP3. Managed, `Span` API, net8.0 build. A trimmed self-contained publish (`TrimMode=full`, every warning shown) was clean and decoded correctly when run. Its seek gives silence and then differing samples (8.2) |
| raylib's dr_mp3 (was in use on one backend) | bundled | public domain | native | Was the only MP3 path: one backend, whole file decoded at load and again at each play. Removed from the raylib device |
| NAudio.Core | 3.4.0-preview.1 | MIT | System.Numerics.Tensors | A device and file library; its MP3 path is NLayer or Windows codecs |
| Native decoders through SDL_mixer or SDL_sound | n/a | zlib | native, per platform | A native dependency per platform and none in browsers' managed runtime, to decode what two managed libraries already decode |

- **D. Target.** `Audio.AddSoundFile("hit", "hit.ogg")`; `Assets.Sound("sfx/hit.mp3")`;
  `AudioDecoders.Add("pcm16", bytes => new Pcm16Stream(bytes))`.
- **E. Boundary.** Decoding belongs beside the mixer, not in it and not in a device: the mixer consumes
  `IAudioStream` and never knew formats.
- **F. Integration.** One static list, read by the façade, the device and the asset loaders (the "sound" kind decodes
  by extension; which kind an extension chooses by default is unchanged, so `.ogg` named without a kind is still
  music). Static because the asset loaders run on worker threads and have no game object; copy-on-write, so reads
  take no lock.
- **G. Trade-offs.** A process-wide list: two games in one process share formats (tests remove what they add). A
  format in the list wins over a backend's own decoder: consistency over a native decoder's speed.
- **H. Decision.** Extend (the stream contract was the right seam; only its registration was closed).
- **I. Validation.** `AudioDecoderTests` (14 tests) on two small files made with ffmpeg and kept as base64: lengths,
  rates, the tone they were made from, garbage and wrong-format bytes, a custom format as effect and music, a decoder
  that throws, returns null or six channels; the sample's `Pcm16Stream`.

**NLayer's licence and upkeep, read from the package and from nuget.org on 2026-11-23.** The package's `nlayer.nuspec`
(NuGet cache, 3.0.1) declares `<license type="expression">MIT</license>`, authors "Mark Heath, Andrew Ward" and
copyright "© Mark Heath 2026"; it carries no licence file of its own, the expression is the statement. Its project
is `github.com/naudio/NLayer`, under the NAudio organisation. It is maintained: after years at 1.x it had six
releases in 2026 (2.0.0 on 2026-06-28, 2.0.1 on 2026-07-06, 3.0.0 on 2026-08-27, 3.0.1 on 2026-11-04, the last
one a decoding fix from a user's report). The version is pinned, so an abandoned or changed upstream changes
nothing that ships. If it stopped being maintained: the decoder is one entry of `AudioDecoders`, so the engine or
a game replaces it with one call (`AudioDecoders.Add("mp3", open)`), and OGG remains the format the manual
recommends for music.

### 8.2 Seeking a compressed stream

Measured in a probe project outside the repository, with a chirp so positions cannot alias:

| Decoder | `seek(n)` then read |
| --- | --- |
| NVorbis 0.10.5 | lands off: +239 frames in a short mono file, frame 512 for a seek to 0 after reading, -128 and +128 in a 20 s stereo file; exact only at 0 of the long file |
| NVorbis 1.0.0-rc.2 | lands on the frame, with samples up to 1.3 away from a straight decode for 600 to 3600 samples |
| NLayer 3.0.1 | silence, then samples that differ from a straight decode |

A loop seam some hundreds of samples off is a rhythm hiccup, and a position that assumes exact seeks drifts from the
sound by that much every loop. So seeks are made exact by reading (`ExactSeekStream`): forward by decoding and
dropping, backward from the file's beginning; a second decoder is brought to a loop's start a little at each read
while the loop plays, so the seam itself costs a swap. Cost of an interactive seek backward: decoding from the start,
3 to 11 ms per second of music at the speeds measured. This also fixed looping OGG music, which went back to where the
decoder's seek to 0 landed. Tested: after seeks to several frames a stream gives the samples of the whole decode at
1e-6, and streamed music loops over its file's region sample for sample across three seams.

### 8.3 Streamed or decoded, and releasing

- **Streaming or memory, per sound.** `AudioLoadMode.Streamed` (default for music) or `Decoded`; sound effects are
  decoded. Tested: the same file streamed and decoded renders the same samples at 1e-6.
- **Unloading. A/B.** A registered sound stayed in a list for good; a game with levels grew for ever, and a sound
  loaded by an asset scope was kept by the mixer after the scope released it.
- **D. Target.** Nothing to write for a level's sounds: `Audio.Play(level.Sound("owl.ogg"))`, and the scope is the
  only owner. For the bank: `Audio.RemoveSound(name)`.
- **F. Integration.** The asset store already counts holders of a clip; the audio side adds no second count. The
  device frees a handle's slot (`UnloadSound`, `UnloadMusic`, defaults that do nothing for other devices) and reuses
  it. A voice holds its clip by reference, so removing a sound that plays is safe.
- **I. Validation.** Weak references: a removed or replaced sound is collected; a clip played without a name is
  collected after its voices end; a registered one is not; removing the current music stops it, on a silent device
  too.

## 9. Spatial sound (2026-11-22)

- **A. Need.** Any game with a world larger than a screen: torches, machines, enemies off screen, a river. Each game
  wrote the same twenty lines (distance, a curve, pan, `Set` every update), kept the voice handles, stopped them on
  scene exit, and most forgot to free voices of sources out of range.
- **B. Before.** `VoicesExample` did it by hand for one sound.
- **C. Alternatives.** OpenAL Soft and Steam Audio are 3D spatializers with their own mixers: adopting one replaces
  the engine's mixer (ADR 0033) for a 2D need that is two multiplications per emitter. FMOD and Wwise are licensed
  middleware. Nothing managed and small exists to adopt; what is conventional was adopted as vocabulary: minimum and
  maximum distance and linear, inverse and custom rolloff are the terms of Unity, Godot and OpenAL.
- **D. Target, written first.** `Sounds.Follow(hero); Sounds.Emit("torch", torch, loop: true);` and
  `Sounds.Emit("explosion", position);`. No handle to keep, nothing in `Exit`.
- **E. Boundary.** The engine: distance, pan, lifetime, voices. The game: what makes a sound and where the listener
  is.
- **F. Integration.** A scene system (ADR 0060), `Scene.Sounds`, ordered after the others so it reads positions
  physics wrote. Entities through `IWorld`. Ramps of one step (ADR 0067) instead of new mixer code. The state hash
  gets nothing: emitters derive from hashed entities, and whether a voice plays depends on the device; a test runs
  the same scene on a silent and a capture device and compares the hashes.
- **G. Trade-offs.** Volume and pan only (no distance filter: effects are per bus). A loop waits for a free voice
  rather than stealing, so with more loops in range than voices the first keep theirs, not the nearest. A scene that
  is not updated keeps its emitters at their last values.
- **Doppler.** Asked to be included only if justified: fly-bys are common (racing, shooters), the cost is ten lines
  over the pitch ramp, and it is off by default. Included.
- **H. Decision.** Extend.
- **I. Validation.** `SceneAudioTests` (12 tests) on rendered samples and emitter values: levels in each speaker by
  distance and side, every rolloff monotonic with exact end points, a custom curve clamped, an emitter following an
  entity with no change between two samples larger than a ramp's, entity destroyed, scene left, two hundred emitters
  out of range with no voice and none refused, a loop back in range at the phase it would have, Doppler ratios, odd
  numbers everywhere, no allocation in a step with forty moving emitters. `SpatialSoundExampleTests` measures the
  example's tones as the listener walks.

## 9b. Corrections after an independent verification (2026-11-23)

A second verification confirmed the thread design and found a crash and several defects; each got a test that
failed first (`AudioCorrectionTests`, 20 tests, and two in `SceneAudioTests`).

| Found | Cause | Now |
| --- | --- | --- |
| D1. An MP3 damaged after its header passed loading, and `Pump` threw from the decoder every frame | Nothing caught a decoder's exception in the fill | The feed ends with what it decoded; reported once to the log and in `FailedStreams`. 400 damaged MP3 files and the OGG files that still open play without an exception (27 of the MP3 files end by their decoder) |
| D2. Sounds scheduled with `Delay` one after another shared a voice; only two of four sounded | A voice counted as free when its sound would be over at the new sound's start | Free only when over now; a scheduled sound that steals silences its victim at once |
| D3. A loop after a long intro decoded the intro at every seam; tiny regions decoded and reopened without end | The spare decoder was thrown away unless ready, and advanced only as fast as reads | The way back is found by the decoder's own seek and made exact against the sound kept from the loop's start; else decoded per `Pump`; stream regions under a tenth of a second are ignored |
| D4. An OGG whose last page claimed 2^40 frames allocated 2 GB or never returned; an ID3 tag larger than the file allocated 512 MB | Declared sizes were trusted | Bounded by what the bytes can hold, before allocating or decoding |
| D5. `Seek` or `SetPitch` before a scheduled start ended the sound early on the timeline | The end was counted from now, not from the start | Counted from where the sound is on the timeline |
| D6. `HeardPosition` jumped back on resume and on a pitch increase | It extrapolated backwards from the latest state | It evaluates the motion that held one delay ago, from the last eight changes |
| D7. A one-shot emitter's `IsActive` depended on the device | It followed the voice | It follows the sound's length on the scene's clock |
| D8. A sampler loop with start equal to end gave a one-frame loop | It was taken as a region | No region |
| D9. A WAV that is not PCM threw `InvalidDataException` through the decoder list | The list wrapped `NotSupportedException` | It passes through, as the documentation says |
| A far seek froze the game | Decode-and-drop inside the call | Spread over `Pump`s, a quarter of a second of sound each |

Measured with a probe outside the repository, a 120 s stereo 44.1 kHz OGG, offline sink, three rounds, on the machine
this was written on, with other builds running:

| Case | Before | After |
| --- | --- | --- |
| `Seek` forward to 90 s: the call | 102 to 550 ms | 0.6 to 4.3 ms; the place is reached 360 `Pump`s later, each at most 2.7 to 6.6 ms |
| `Seek` back from 100 s to 30 s: the call | 36 to 62 ms | 0.6 to 2.4 ms; reached 120 `Pump`s later, each at most 1.5 to 1.8 ms |
| `Play` with `StartAt` 54 s | 63 to 79 ms | 3.4 to 14 ms |
| A loop of 3.7 s after an intro of 55 s: the `Pump` at each seam | 64 to 77 ms, every seam | 2.0 to 5.4 ms; in the first round one seam took 10.8 ms and one 33.4 ms, in the later rounds none above 3.7 ms |

Not fixed, and why: nothing of the list. Left as limits and written in the manual: a far seek takes time to arrive
(four times faster than playing); an MP3 loop whose intro is more than thirty times the loop waits at its seam; the
heard position keeps eight changes. Not verified: the 33 ms `Pump` of the first round was not explained (first use
of the code, or a collection); nothing was heard on a device.

## 10. Not verified (second part)

- Nothing was heard on a device: every claim above is about rendered samples.
- NativeAOT with NLayer was not published (the machine has no native linker); the trimmed publish is the evidence.
  No browser, Android, iOS, Linux or macOS run.
- Decoding speed and seek cost were measured once, on one machine, with other builds running.
- The `sdk/tests` checks (`packages.mjs` was taught the new dependency) were not run: they need a packed feed.
- `AudioEngine.CreateBus` and the audio thread: argued, not measured (ADR 0072, consequences). The test added for
  it runs offline and checks the first frame of a bus used in the step that created it; it does not exercise two
  threads.
