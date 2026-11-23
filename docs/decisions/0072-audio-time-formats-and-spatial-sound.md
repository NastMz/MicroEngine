# ADR 0072: Time in a sound, one list of decoders, and a scene's listener and emitters

## Status

Accepted (2026-11-22). The SDK is in Alpha. Evidence: [AUDIO-2](../evaluations/audio-2.md), sections 7 to 10. Issue
NastMz/MicroEngine#177, part of [CONSOLIDATION-1](../evaluations/consolidation-1.md). Completes
[ADR 0067](0067-mix-graph-and-effects.md), whose last consequence left these out; builds on
[ADR 0033](0033-one-mixer-over-pcm-sinks.md) (the mixer, its queue and `IAudioSink` v1 are unchanged),
[ADR 0060](0060-scene-systems-and-module-lifecycle.md) (scene systems) and [ADR 0066](0066-content-production-as-data-and-optional-tools.md) (asset scopes).

## Context

A game could not read where its music was, start a sound from the middle, or loop a part of a piece: an intro and a
loop were two pieces of music joined by hand. Compressed files were music only, MP3 played on one backend, formats
were a protected method of a device class, a registered sound stayed in memory for good, and every game wrote its
own pan and volume by distance. The earlier reasons ("no demo needs it", "the game can write it") are not accepted
by CONSOLIDATION-1.

## Decision

**Time.**

- A voice has a position on the mixer's timeline (`Position`, in seconds of the sound) and a heard position
  (`HeardPosition`): the same, less the output delay the mixer already schedules by (`DelayFrames`, at least the
  sink's latency). Both are computed on the game's thread from the timeline's view of the voice, never read from the
  audio thread: they follow the simulation's steps and are the same in every run. Before a sound's start or seek is
  heard, the heard position counts down to it (negative before a sound's first frame).
- `Seek` is a command at the step's frame, like every other. For a clip it carries the frame to go on from. For a
  stream the game's thread seeks the decoder, makes a new feed, fills it, and the command carries the feed: the audio
  thread swaps one reference at the frame. The old feed is never filled again; nothing is locked.
- `PlayParameters.StartAt` and `Delay` (a start exact to the output frame, after the step).
- A loop region (`LoopRegion`, in frames of the sound) belongs to the sound and can be replaced per play. It is read
  from a WAV sampler chunk and from the `LOOPSTART`, `LOOPLENGTH` and `LOOPEND` comments of an OGG file. The whole
  sound is the region when none is given, by the same arithmetic as before: a mix without regions renders the same
  bits (the recorded baseline passes unchanged).
- On a device without voices the façade counts the time the music has played, so a game that follows its music runs
  headless.

**Formats and memory.**

- `AudioDecoders` is one static list of formats by extension, for sound effects (decoded whole), streamed music and
  the asset kinds. It is static because a decoder is a function over bytes with no state of a game, and three entry
  points need it, one of them on worker threads. A format in the list is decoded by it on every device; a backend's
  own decoder (`MixerAudioDevice.AddDecoder`) serves only formats the list lacks, so a file sounds the same
  everywhere.
- MP3 is decoded by NLayer 3.0.1 (MIT, managed, no dependencies), pinned. OGG stays on NVorbis 0.10.5.
- Seeks are exact by decoding, not by the decoders' own seeks (`ExactSeekStream`): forward by decoding and dropping,
  backward from the file's beginning, and a second decoder brought to a loop's start while the loop plays, so a loop
  seam costs nothing when it comes. Measured: both libraries' seeks land off the frame or give wrong samples after
  it (evaluation, 8.2).
- Music is streamed or decoded whole per piece (`AudioLoadMode`); sound effects are always decoded.
- Memory: `RemoveSound` and `RemoveMusic` free what the mixer and device hold (`IAudioDevice.UnloadSound` and
  `UnloadMusic`, with defaults that do nothing); a name registered again frees what had it. A clip can be played
  without registering it (`Audio.Play(clip)`), and then nothing but its voice holds it: a sound loaded by an asset
  scope is freed with the scope. No reference counting was added to the audio side: the asset store already counts
  holders.

**Spatial sound.**

- `SceneAudio` (`Scene.Sounds`) is a scene system in the core. Each step, after the scene's update and its other
  systems, it gives every emitter a gain from its distance (minimum and maximum distance, a `SoundRolloff` or a curve
  of the game's own) and a pan from its horizontal offset, and sends them with a ramp of one step: the ramps of ADR
  0067 make it click-free with no new mixer code.
- An emitter out of range holds no voice; a loop that comes into range is sought to where it would be. A loop waits
  for a free voice instead of stealing one.
- A Doppler shift exists and is off by default: it is ten lines over `SetPitch` and its ramp, and fly-bys are a
  common need (racing, shooters).
- Nothing of it enters the state hash. Emitters are presentation derived from entities that are hashed already, and
  whether a voice plays differs between a device that plays and a silent one.

## Alternatives rejected

- **Reading the position from the audio thread** (a volatile counter per voice): it moves in blocks, differs between
  sinks and runs, and would make a game that follows its music non-deterministic.
- **A heard position that follows the device's own clock between steps**: smoother than a step, but not
  reproducible; a game interpolates between two steps if it needs to.
- **Seeking a stream by resetting its ring**: the ring is single-producer, single-consumer; resetting it from the
  producer races the consumer. A new feed swapped by command needs no new rule.
- **The decoders' own seeks**: measured wrong (above). **NVorbis 1.0.0-rc.2**: a prerelease whose seek gives samples
  louder than the signal for hundreds of frames.
- **Upgrading to exact seeking through Ogg page parsing of our own**: a second container parser to own.
- **A decoder registry per device or per `AudioMixer`**: the asset loaders have neither.
- **MP3 through each backend's native decoder**: one backend had it; the others would each need one, and the samples
  would differ between them.
- **Unloading by reference counts on sounds**: the asset scopes already do it for clips, and a registered name is
  an explicit hold that an explicit call releases.
- **Spatial sound in the mixer** (positions as voice parameters): it would put world units, listeners and curves
  under the audio thread for something a scene computes once a step.
- **Per-emitter low-pass with distance**: needs effects per voice, which ADR 0067 left out; a bus per group of far
  sounds does it today.
- **Hashing emitters**: their count depends on whether voices exist on the device.

## Consequences

- An intro with a loop is one file; a rhythm or cutscene game follows `MusicHeardPosition`.
- The `MicroEngine` package depends on NLayer. NativeAOT with it was not published on the machine this was written
  on (no native linker); a trimmed, self-contained publish with every trim warning shown was clean and ran.
- A seek backward in a long streamed track costs a few milliseconds per second of music before the place asked for,
  on the game's thread. Decoded music seeks at once.
- MP3 on raylib is now the managed decoder; the raylib device's own MP3 decoder is never reached.
- Binary-breaking signature changes and behaviour changes are in the migration guide.
- Still open: `AudioEngine.CreateBus` writes the parent and volume of a new bus where the audio thread reads them
  without a hand-over (found by the verification of ADR 0067; not touched here).
- Not part of this record: sound blocked by walls, reverberation by place, effects per voice, nearest-first voice
  allocation for emitters, loop points in MP3.
