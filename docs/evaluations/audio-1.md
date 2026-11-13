# AUDIO-1: exposed audio mixer controls and latency

Issue NastMz/MicroEngine#124. Record: [`odd/tasks/audio-1.md`](../../odd/tasks/audio-1.md). Date: 2026-11-13.
The question is what a game can control of the mixer through the public API, how late and how evenly sounds start,
and what must be added. The rule is that of every recent evaluation: could an external developer build that game
today with the published SDK? Builds on [ADR 0033](../decisions/0033-one-mixer-over-pcm-sinks.md) (one managed mixer
over PCM sinks), which this work keeps: one mixer, one command queue between the game thread and the render function,
the `IAudioSink` v1 contract unchanged.

## 1. What existed

Two public layers, both shipped in the `MicroEngine` package:

- **The engine's mixer**, `AudioEngine` (`src/MicroEngine.Audio/Mixing/AudioEngine.cs`): `Play(clip | stream,
  PlayParameters)` returning a generational `VoiceHandle`; `Stop(voice, fade)`, `Set(voice, volume, pan)`,
  `Pause`, `IsPlaying`; named buses under `master` with a volume (`Bus`, `AddBus`, `AudioBus.Volume`); priorities and
  one stealing policy; any number of streams side by side. Reached through `MixerAudioDevice.Engine`.
- **The game-facing mixer**, `AudioMixer` (`Game.Audio`, `src/MicroEngine.Audio/Audio.cs`): a bank of named sounds
  and music; `Play(name, volume, pitch, pitchJitter, pan)` returning nothing; one music track with fades;
  `SfxVolume`, `MusicVolume`.

| Need (issue) | `AudioEngine` | `Game.Audio` |
| --- | --- | --- |
| Individual voices (handle, stop, pause, loop) | yes | no: `Play` returned `void`, no loop, no priority |
| Buses | yes | no |
| Simultaneous music or streams | yes (`Play(stream)`) | no: one track |
| Live volume and pan | yes (`Set`) | only the music's volume |
| Live pitch | **no**: fixed when the sound starts (the manual listed it as a limitation) | no |
| Simple positional stereo | pan and volume per voice, live | pan at the moment of `Play` only |

**Answer to the rule before this change.** A game that only fires one-shot sounds and one music track was served. A
game with a looping engine or siren, ambience under the music, a sound cut when its source dies or a volume slider per
group had to leave `Game.Audio`: cast `Audio.Device` to `MixerAudioDevice`, keep its own `AudioClip` references
(`AudioMixer` kept only device handles, so a sound registered by name could not be played on the engine), branch for
the devices without a mixer (`SilentAudioDevice`: headless tests, `--mute`, no hardware), and lose the capture
device's `Played` list. No demo did it, and nothing in the manual showed how. A sound whose pitch follows something
(an engine, a wind-up) could not be built at all, short of restarting the sound. The gap was real, and it was a gap
of exposure plus one missing primitive, not of mixing.

## 2. Measurements before any change

Machine: the maintainer's Windows 11 desktop, .NET 10.0.404, Release. Harness: a console program outside the
repository referencing `src/MicroEngine.Sdl` (kept in the session's scratch folder; its three sections are described
here so it can be rewritten). Each number was read from a run made for this evaluation.

**Rendering** (the function a sink calls; 256-frame stereo blocks at 48 kHz, the SDL feeder's chunk, 5.33 ms of audio;
looping 44.1 kHz mono clips resampled at pitch 1.1; 20 000 blocks after warm-up):

| Voices | Before | After, with a pitch, volume and pan change per block |
| --- | --- | --- |
| 0 | 1.3 µs per block | 1.3 µs |
| 1 | 7.5 µs | 7.3 µs |
| 8 | 10.7 µs | 9.7 µs |
| 32 (the limit) | 40.6 µs: 0.76 % of real time | 40.2 µs: 0.75 % |

Bytes allocated by the rendering thread over the 20 000 blocks: 0 before and 0 after, in every row. The differences
between the columns are within the run-to-run spread (a second baseline run gave 54.9 µs for 32 voices).

**Game-thread calls** (200 000 calls each, the queue drained between batches of 1 000; 0 bytes allocated and 0
commands dropped in every row):

| Call | Cost |
| --- | --- |
| `AudioMixer.Play(name)` plus the step's `Update` | 753 ns before, 731 ns after |
| `AudioEngine.Play(clip)` plus `Advance` | 761 ns before, 716 ns after |
| `AudioBus.Volume` set | 27 ns before, 25 ns after |
| `AudioEngine.Set(volume, pan)`, `Pause` | 63 ns, 55 ns (after only: the baseline run measured them on a voice the test's own sounds had stolen, which returns early; their code did not change) |
| `AudioEngine.SetPitch` (new) | 42 ns |
| `AudioMixer.PlayVoice` plus `Stop` and `Update` (new) | 264 ns |
| `AudioMixer.Set(volume, pan, pitch)` (new) | 173 ns |
| `AudioMixer.SetBusVolume` (new) | 48 ns |

Mixing and control are not a cost worth optimizing: 32 voices take under 1 % of one core, nothing allocates.

**Triggering from frame updates, SDL sink, real time.** The harness models `GameRunner`'s loop (frames of a given
length, a fixed-step accumulator at 60 steps per second, at most 5 steps a frame), plays one sound per step on
`SdlAudioDevice.TryCreate()` (this machine's default output; `DelayFrames` 3360 = 70.0 ms: the 60 ms queue plus the
10 ms device period SDL reports), and a second thread watches `RenderedFrames` to time when each sound's first frame
is rendered. 600 frames per row:

| Loop | Play call to its frame being rendered (ms): min / median / p95 / max | Median in the first and the last second | Late | Steps moved by catch-up |
| --- | --- | --- | --- | --- |
| 60 fps, steady | 41.8 / 60.9 / 67.2 / 82.4 | 59.5 → 61.1 | 0 | 10 of 598 |
| 144 fps, steady | 39.4 / 62.7 / 69.5 / 74.0 | 60.0 → 64.4 | 0 | 8 of 249 |
| 30 fps, steady (2 steps a frame) | 26.1 / 71.9 / 83.4 / 99.7 | 72.7 → 71.9 | 0 | 8 of 1198 |
| 60 fps, one 40 ms frame every 2 s | 36.6 / 87.0 / 96.6 / 101.3 | 59.8 → 87.0 | 0 | 8 of 604 |
| 60 fps, one 120 ms frame every 2 s | 0.0 / 120.0 / 128.1 / 142.7 | 62.9 → 122.8 | 1 | 9 of 614 |

The same run after the change gave the same picture (medians 60.3, 61.9, 70.8, 80.5, 121.4): the change does not
touch scheduling.

What the numbers say:

1. **Consistency.** A sound does not start when `Play` is called but at its step's frame on the output timeline, so
   sounds played in consecutive steps are exactly one step apart (800 frames, 16.67 ms) however uneven the frames
   are; only the steps the timeline moved (8 to 10 in about 600, when the game fell behind the sink) differ. Nothing
   was late in steady loops; one sound in 614 was late after 120 ms frames, longer than the 60 ms queue.
2. **Latency is about twice what the documents say.** The frame a sound starts at is rendered a median 60 ms after
   the call. The feeder renders a block only while less than 60 ms is queued in SDL's stream
   (`src/MicroEngine.Sdl/SdlFeederSink.cs`), so that block reaches the device 55 to 60 ms after it is rendered. A
   sound is therefore handed to the device about **120 ms** after the update that played it in a steady loop, not the
   "about 60 ms" of ADR 0033 and of the manual; the device's own buffer and the system mixer come on top and were not
   measured (that needs a loopback recording). The cause is in `AudioEngine.Advance`: in real time the timeline is
   kept at or ahead of `RenderedFrames`, which is already a queue ahead of the device, and `DelayFrames` (the queue
   again) is added to it.
3. **A slow frame leaves latency higher for good.** After a 40 ms frame the median stays 20 ms higher; after 120 ms
   frames it settles 60 ms higher (about 180 ms to the device). The steps a frame runs to catch up each move the
   timeline one step ahead, and nothing brings it back unless it is more than a second ahead.

**Browser: not measured here.** This machine has no browser build of a game ready and the session did not build
one. Read from the code only: `BrowserAudioSink` renders once per frame until the worklet holds 100 ms, and the same
`Advance` rule schedules a step's sounds `DelayFrames` (100 ms plus 128 frames) after what is already rendered, so
the same doubling applies in principle; the figure and the behaviour after slow frames need a run in a browser.
Earlier browser measurements of the sinks themselves are in
[`experiments/arch-1/p5-audio-sinks`](../../experiments/arch-1/p5-audio-sinks/README.md). raylib's sink, Android and
iOS were not measured either.

## 3. Generic primitives and game effects

| Generic mixer primitive (engine) | Game-specific (stays in games) |
| --- | --- |
| A voice with a handle: start, stop with a fade, pause, loop, priority | Which sounds loop, when they stop |
| Live volume, pan and **pitch** of a voice | A listener, emitters, distance curves, Doppler: a few lines over pan, volume and pitch (the example) |
| Buses with a volume, nested | The groups a game has (ambience, voices, UI) and its settings screen |
| Several music voices at once | Crossfades, layered or adaptive music, ducking under dialogue |
| | Sound design: footsteps by surface, randomized variations, effect libraries (the issue excludes them) |

Reverb, filters and other effects on buses are generic too but need DSP in the render function; nothing measured asks
for them, so they stay out.

## 4. What changed

All additive; every existing call compiles and sounds as before (pinned by `Play_by_name_sounds_as_before` and the
existing engine tests).

- **`AudioEngine.SetPitch(voice, pitch)`.** The one missing primitive. It travels as the existing `Set` command with
  one more field, through the same single-producer, single-consumer queue; the render function applies it at the
  step's frame. The main thread's timeline tracks where the sound is, so its end (and therefore the voice policy)
  moves with the pitch, also while paused or fading out. Streams keep their own pitch (they are decoded ahead at one
  rate).
- **Changes of one step apply together.** Before, a second change of the same voice in one step (volume, then pause)
  made the first apply at the start of the block being rendered instead of at its frame; with pitch as a third kind
  of change this became common (`Set(volume, pan, pitch)`). Changes for one frame now wait for it together.
- **`Game.Audio` reaches voices and buses by name.** `PlayVoice(name, parameters)` and
  `PlayMusicVoice(name, parameters)` return a `VoiceHandle`; `Set(voice, volume, pan, pitch)`, `Pause`, `Stop` and
  `IsPlaying` control it; `AddBus`, `SetBusVolume`, `BusVolume`, `HasBus`. On a device without the mixer the calls do
  nothing, return `VoiceHandle.None` or `false`, still tell a `SilentAudioDevice` the sound's name, and bus volumes
  read back, so game code and headless tests need no special case. `MixerAudioDevice.PlayVoice` and `PlayMusicVoice`
  are the device-level entry points; `CaptureAudioDevice` lists voices in `Played`.
- **Decisions.** A voice plays exactly its `PlayParameters`: `SfxVolume`, `MusicVolume` and the once-per-step rule
  belong to `Play` and `PlayMusic`, whose behaviour is unchanged; groups are scaled with buses. Bus access is by name
  (`SetBusVolume`) rather than by `AudioBus` object, because a device without a mixer has no bus objects. No listener
  or emitter type: positional stereo is three lines of game code over `Set`.
- **Example.** `samples/Examples/Examples/VoicesExample.cs`: a looping tone on an `ambience` bus whose pan, volume
  and pitch follow a point crossing the screen. Its capture and an audio capture of it were inspected: the left and
  right levels cross as the point passes the listener (left 0.22 / right 0.12 RMS on the left side, 0.03 / 0.08 on
  the right), the tone reads 117 Hz approaching and 103 Hz receding (110 Hz × 1.06 and × 0.94), and the levels match
  the bus at 0.5.

Not an architectural change (no new assembly, contract or threading rule), so no decision record.

## 5. Left for the owner

Two findings about real-time scheduling are defects or tuning of ADR 0033's policy rather than public extensions. They
change when existing sounds are heard, the right fix trades exact-frame starts against latency, and neither can be
judged without listening on a device, so nothing was changed:

1. **Halve the latency.** Keep the timeline at the device's position instead of the rendered position (subtract the
   sink's `LatencyFrames` when comparing with `RenderedFrames` in `Advance`), so a sound starts in the next block to
   be rendered: about 70 ms to the device on SDL instead of about 120 ms. On a host-pumped sink (browsers) rendering
   happens after the steps on the same thread, so nothing can be late; on a sink thread a step that runs a few
   milliseconds late would start its sounds late by that much instead of at their exact frame, unless a small margin
   (one feeder chunk, 5 ms) is kept. Deterministic runs are not affected either way.
2. **Recover after a slow frame.** Bring the timeline back once per frame (in `Pump`) when it is more than a step
   ahead of the sink. It makes the timeline go backwards by up to four steps, which the voice bookkeeping (`End`
   against `Now`, the first free voice being reused while the old sound's last milliseconds still play) was not
   written for; it needs its own tests.

Also not done: gain ramps for volume and pan changes (they step at the update's frame; small per-update changes are
fine, large jumps can click), effects on buses, a pitch per bus, pitch for streamed music, and measurements in a
browser, on raylib, Android and iOS.
