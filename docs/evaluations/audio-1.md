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

What the numbers say (points 2 and 3 are corrected in §5.2):

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

## 5. Review follow-up (2026-11-13)

The owner's review (record, L8) asked for five things. Every statement below is labelled **demonstrated failure**
(reproduced by a test or a run, named), **user-perceptible difference** (measured, and large enough to be heard or
felt) or **hypothesis** (read from code, not run). Scratch programs are outside the repository; their sections are
described so they can be rewritten.

### 5.1 The 34 differences of the same-update rule

An independent probe rendered 116 sequences offline (29 scenarios at delays of 0, 100, 512 and 1000 frames, and the
game-facing mixer at sink latencies of 0, 256 and 1024 frames) before this branch and on it; 34 files differ, all
with a delay above zero. Each was compared with the same sequence rendered **without delay and moved later by the
delay**: without delay both rules give the same samples, so that is what the sequence should sound like.

| Category (files) | Samples that differ, per file | Span of the difference | Difference, peak / RMS (dBFS) | Position shift for the rest of the sound | Frames off the delay-shifted run: before / now | Jumps above 0.02 in the whole file: before / now / ideal |
| --- | --- | --- | --- | --- | --- | --- |
| volume then pan (3) | 200 to 1600 | the delay: 2.1 to 16.7 ms | -18.7 / -19.0 to -21.4 | none | 100 to 800 / 0 | 2-4 / 2-3 / 2 |
| volume twice (3) | 200 to 1600 | 2.1 to 16.7 ms | -14.6 / -14.9 to -17.3 | none | 100 to 800 / 0 | 2-4 / 2-3 / 2 |
| volume and pan twice (3) | 200 to 1600 | 2.1 to 16.7 ms | -13.0 / -14.0 to -16.4 | none | 100 to 800 / 0 | 2-4 / 2-3 / 2 |
| volume then pause (3) | 200 to 1600 | 2.1 to 16.7 ms | -18.7 / -19.0 to -21.4 | none | 100 to 800 / 0 | 3-5 / 3-4 / 3 |
| changes then a steal in the same update (3) | 200 to 2000 | 2.1 to 20.8 ms | -14.8 / -15.0 to -16.6 | none | 100 to 1000 / 0 | 4 / 3 / 2 |
| pause then resume (3) | 8400 to 9600 | 87 to 100 ms (to the sound's end) | -0.9 to -12.6 / -12.8 to -17.0 | 2.1, 10.7, 16.7 ms later before | 4200 to 4800 / 0 | 1-4 / 1-2 / 1 |
| pause then volume (3) | 8400 to 9600 | 121 to 133 ms | -6.9 to -12.6 / -18.0 to -22.2 | 2.1, 10.7, 16.7 ms later before | 4200 to 4800 / 0 | 1-4 / 3-4 / 3 |
| resume then volume (3) | 8200 to 10000 | 85 to 104 ms | -8.0 to -11.4 / -14.7 to -25.0 | 2.1, 10.7, 20.8 ms earlier before | 4100 to 5000 / 0 | 5 / 4 / 3 |
| volume and pan in separate calls every update, 30 updates (3) | 5700 to 45799 | 469 to 483 ms | -41.6 / -46.2 | none | 2900, 14848, 23199 / 0, 0, 5600 | 1-3 / 1-2 / 1 |
| `MusicVolume` then `PauseMusic`, no fade (2) | 512, 2048 | 5.3, 21.3 ms | -12.0 / -15.2 | none | 256, 1024 / 0 | 3 / 2 / 2 |
| `PauseMusic` during a music fade (2) | 1024, 3200 | 89, 100 ms | -8.9 / -14.0 | none | 512, 3392 / 0, 2016 | 7-8 / 7 / 7 |
| `MusicVolume` during a music fade (2) | 1024, 3200 | 89, 100 ms | -13.4 / -17.8 | none | 512, 4064 / 0, 2912 | 6-8 / 7 / 7 |
| two streams, pause and two changes (1) | 400 | 4.2 ms | -15.2 / -19.9 | none | 4040 / 3840 (streams, see below) | 127 / 131 / 142 |

How to read it. "Frames off the delay-shifted run" is the count of frames that are not what the sequence sounds like
without delay: **0 on this branch for 30 of the 34 files**, and never 0 before. "Jumps" counts sample-to-sample steps
above 0.02 (the test signals move at most 0.015 per frame on their own), that is, the discontinuities a volume or
pause step makes: the branch has fewer than before in 21 files (the old rule applied the first change at the block's
start and the second at the frame: two steps where one was asked for), as many in 9 and more in 4.

Classification:

- **No demonstrated functional regression.** In every category the branch's output is the one the mixer's own
  timeline describes (the voice bookkeeping pauses, resumes and ends the sound at the update's frame; the render now
  does too). Before, the two disagreed: pause then resume in one update left a gap of the delay and played the rest of
  the sound that much late (**demonstrated failure of the previous rule**, now pinned by
  `Pausing_and_resuming_a_sound_in_one_step_changes_nothing`); pause or resume followed by a change moved the sound's
  position by the delay.
- **User-perceptible differences**, all in the direction of what was asked for: a paused-and-changed or
  resumed-and-changed sound continues from a position 2 to 21 ms away from before (up to the delay; by the same rule up to the
  70 ms delay of the SDL sink, which was not rendered), with differences up to -1 dBFS over the rest
  of the sound: audible on a long non-looping sound as a different alignment, not as an artefact. The gap of the delay
  that pause-then-resume used to leave (2 to 17 ms of silence in a playing sound) is gone.
- **Inaudible or at the threshold**: the first of two parameter changes now applies up to the delay later, at the
  frame of the second (2 to 21 ms; -13 to -19 dBFS for that span only); a source followed every update with separate
  volume and pan calls differs by -41.6 dBFS peak.
- **Not delay-invariant on either side (4 files; demonstrated limit, older than this branch)**: with a delay longer
  than one update (1000 or 1024 frames here), a change is still waiting for its frame when the next update's change
  for the same sound arrives, and a voice holds one waiting change: the earlier one applies at once, up to
  delay minus one update early (4.2 ms here). The old rule did the same and more (see the counts). It concerned every
  real device while the delay was 70 ms and the timeline ran a whole queue ahead; with the timeline of §5.2 a change
  is at most the timeline's lead (within one update in steady state) ahead of the render, so it only happens just
  after a slow frame, by a few milliseconds. That is 3 of the 4 files; the two-streams file differs from its delay-free run on both sides
  because streams are decoded ahead in whole pumps, not because of this rule.

Tests: `Changes_to_one_sound_in_one_step_sound_the_same_whatever_the_delay` (eight sequences at three delays against
the delay-free run), `Pausing_and_changing…`, `Pausing_and_resuming…`, `Resuming_and_changing…`; six of them fail when
the previous rule is put back (checked once by mutation).

### 5.2 Latency: 120 ms against 70 ms

**Cause (demonstrated).** `Advance` kept each step's frame at or ahead of `RenderedFrames`, which is already one sink
queue ahead of the device, and `DelayFrames` (the sink's latency) was added on top: the queue was waited for twice.

**Method.** A console program on the SDL sink of this machine (default output, `DelayFrames` 3360) runs the game
loop's model (fixed steps at 60 per second, at most 5 per frame) for 600 frames per row, plays a silent sound per
step and records, per step, `RenderedFrames` at the call and at `Advance`. (a) *Paired model*: every policy is then
evaluated on the same recording, so the rows of one loop share their timing noise. (b) *Engine as built*: the same
loop reading the engine's own `Now`, before and after the change. Output latency is the time from the update's call
to the sound's first frame being rendered, plus 57.5 ms (the feeder appends a block while 55 to 60 ms are queued) to
reach the device; the device's own buffer and the system mixer are on top and not measured. Timing precision is (i)
the error of the interval between the sounds of consecutive steps against one step (16.67 ms) and (ii) how late a
sound starts against its scheduled frame (a sound scheduled in frames already rendered starts at the next block).
The machine was running other builds during the runs, so absolute figures carry that load; the comparisons are
paired or back to back.

Engine as built, before and after (ms; update to device = update to render + 57.5):

| Loop | Update to device, before: median / p95 | after: median / p95 | Median in the last second: before → after | Late sounds: before / after | Interval error, before: median / p95 / max (steps moved) | after: median / p95 / max (steps moved) |
| --- | --- | --- | --- | --- | --- | --- |
| 60 fps steady | 117.5 / 123.5 | 64.8 / 71.5 | 118.8 → 65.5 | 0 / 0 | 0 / 0 / 14.7 (8 of 598) | 0 / 0 / 15.3 (7) |
| 144 fps steady | 120.2 / 127.5 | 67.5 / 74.2 | 120.8 → 68.2 | 0 / 0 | 0 / 0 / 20.7 (4 of 599) | 0 / 0 / 3.3 (6) |
| 30 fps steady (2 steps a frame) | 130.2 / 140.8 | 76.1 / 93.5 | 130.2 → 75.5 | 0 / 0 | 0 / 0 / 21.3 (6 of 597) | 0 / 0 / 10.0 (23) |
| 60 fps, a 40 ms frame every 2 s | 142.2 / 152.2 | 71.5 / 94.1 | 142.8 → 70.8 | 0 / 0 | 0 / 0 / 28.0 (5 of 604) | 0 / 1.0 / 42.7 (122) |
| 60 fps, a 120 ms frame every 2 s | 174.2 / 188.8 | 74.8 / 125.4 | 172.8 → 72.5 | 1 (38.7 ms late) / 0 | 0 / 0 / 53.3 (9 of 614) | 0 / 1.0 / 99.3 (230) |
| 60 fps steady, after those | 118.2 / 125.5 | 64.8 / 72.2 | 118.8 → 64.8 | 1 (8.0 ms late) / 0 | 0 / 0 / 53.3 (4 of 598) | 0 / 0 / 10.0 (6) |

Paired model, one recording per loop (update to device median, ms; late sounds; interval error p95 / steps moved):

| Policy | 60 fps | 144 fps | 30 fps | 40 ms frames | 120 ms frames |
| --- | --- | --- | --- | --- | --- |
| A. Before: timeline at or ahead of what is rendered | 136.2; 0; 0 / 6 | 144.2; 0; 0 / 7 | 146.8; 0; 0 / 6 | 144.8; 0; 0 / 7 | 190.2; 1; 0 / 9 |
| B. The proposal as written: device position, checked in `Advance` | 66.2; 6 late (4.0 ms median, 8.7 max); 0 / 6 | 74.2; 7 late (up to 36 ms); 0 / 7 | 76.8; 6 late (up to 15.3); 0 / 6 | 74.8; 7 late (up to 10); 0 / 7 | 120.2; 8 late (up to 92); 0 / 8 |
| C. B, and also checked at a step's first call | 66.2; 0; 0 / 6 | 74.2; 0; 0 / 7 | 76.8; 0; 0 / 6 | 74.8; 0; 0 / 7 | 120.2; 0; 0 / 8 |
| D. C, coming back 1 ms per frame beyond one step ahead (implemented) | 64.8; 0; 1.0 / 50 | 67.2; 0; 1.0 / 84 | 74.5; 0; 1.0 / 36 | 67.5; 0; 1.0 / 48 | 72.8; 0; 1.0 / 229 |
| D with half a step of slack instead of one | 64.2; 0; 1.3 / 144 | 63.5; 0; 2.7 / 215 | 74.2; 0; 1.0 / 104 | 63.5; 0; 1.0 / 162 | 67.5; 0; 1.0 / 295 |

What the numbers say:

1. **Output latency (user-perceptible difference).** A sound reaches the device a median 65 to 76 ms after its
   update instead of 118 to 130 ms: 53 to 54 ms sooner in every steady loop, by construction (the sink's 70 ms less
   the one step the timeline keeps). 50 ms is well above what players notice between an action and its sound.
2. **After slow frames (user-perceptible difference).** Before, the catch-up steps of a slow frame left the timeline
   ahead: 142 ms after 40 ms frames, 174 ms after 120 ms frames. Policy C alone keeps that fault (120 ms after 120 ms
   frames). With D the median in the last second is 71 to 73 ms.
3. **Timing precision.** The spacing of consecutive steps' sounds is exactly one step in all policies except where
   the timeline moves. A, B and C move it at the same steps (6 to 9 in about 600: the game ran later than the
   timeline). B, the proposal as written, starts 6 to 8 sounds in 600 late, by up to 36 ms (up to 92 ms after a
   120 ms frame), because a step's frame is fixed one frame before its sounds are played; checking at the step's
   first call (C) removes every late sound in the model, and the engine as built measured none in 3611 steps. D
   moves more steps (36 to 229 in 600), each by 1 ms (a sixteenth of a step): median error 0, p95 1.0 ms, during
   about a second after a slow frame. 1 ms is below the 5.3 ms block of the SDL feeder and far below what is heard
   as uneven rhythm; the maximum error is the slow frame itself in every policy.
4. **Half a step of slack** lowers latency by up to 5 ms more but moves two to three times as many steps in steady
   loops (the feeder's 5.3 ms blocks make the timeline hunt). One step was kept.

**Decision: implemented (D)**, because the change is local (`AudioEngine.Advance`, a check at a step's first sound or
change, a flag in `Pump`; the render function is untouched and allocates nothing, pinned by the existing tests),
deterministic output is byte-identical (the 116 offline renders of §5.1 rebuilt on the final code: 0 differ; and
`Deterministic_runs_follow_the_simulation_whatever_the_sink_rendered`), and precision does not degrade beyond 1 ms
steps while it removes the late sounds the old policy had after slow frames. Pinned by `AudioEngineTimelineTests`
(against a sink with a 70 ms queue; five of the first six failed before the change). §5.6 corrects two faults of
the first version of this change.

**Hypotheses, not run.** Browsers: `BrowserAudioSink` is pumped by the host after the frame's steps, so a step's
sounds start in the audio rendered by that frame's pump and nothing can be late; the expected figure is the 100 ms
queue less the frame, against about twice the queue before. No browser build was run. raylib's sink, Android and iOS
were not measured either. The system's own output latency (after SDL's queue) was not measured: that needs a
loopback recording.

### 5.3 Values that are not numbers

**Demonstrated failures** (tests in `AudioEngineControlTests`, red before the fix): `SetPitch(voice, NaN)` on a
non-looping sound returned `true`, then `IsPlaying` was `false` and `Stop` returned `false` while the sound played to
its end (`Math.Clamp` passes NaN through; the timeline's end became a huge negative number); `Play` with a NaN pitch
took a voice that was never busy; a NaN bus volume and a NaN `MusicVolume` were stored as given. Read from the code, not
observed (the tests fail earlier, at the stored value): a NaN volume or pan at `Play` and a NaN bus volume multiply
NaN into the samples of that sound or bus. `Set(voice, NaN)` was
already ignored, by accident. Fix: `Play` uses the default for such a value (full volume, original pitch, centred);
`Set`, `SetPitch`, `AudioBus.Volume`, `AudioMixer.MusicVolume`, `SfxVolume` and `SetBusVolume` ignore it. Infinities
were already clamped.

### 5.4 The command queue when the sink stops rendering

**Demonstrated failure** (`AudioEngineQueueTests`, five of six red before the fix). The queue holds 4096 commands and
`Post` dropped what did not fit, counting it in `DroppedCommands`, which nothing reads. With 32 voices changed every
update (`Game.Audio.Set(voice, volume, pan, pitch)` is two commands) it fills after 64 updates without a render (128
with volume and pan only). From then on: a dropped stop leaves a looping sound playing with `ActiveVoices` 0 and no
handle once the sink renders again; a dropped pause or resume, volume, pan, pitch or bus volume is lost; a dropped
start leaves a handle whose calls reach whatever the render function still has in that voice.

**When a sink stops rendering while the game goes on.** Read from the code, not run on devices (**hypothesis** as to
how often it happens):

- Browsers: the page's audio context can be held suspended by the browser until a key or click (the page shows a
  "Sound is blocked" button, `main.js`). The worklet then plays nothing, its queue stays full and
  `BrowserAudioSink.Pump` renders nothing, while the game runs. One looping voice followed with volume, pan and pitch
  fills the queue in 34 s; 32 voices in about a second. This is the realistic case. A hidden page is not one: the page
  stops its frames as well.
- SDL: the feeder renders while SDL's stream holds less than its target. It stops only if the device stops consuming
  (SDL keeps a lost device running as a silent one, so this should be rare). A debugger pause stops both threads. A
  minimized window does not stop the feeder.

**Fix, inside the existing mechanism.** The queue stays the only hand-off and the render function is unchanged. When
a change to a voice or a bus does not fit, the main thread, which already keeps each voice's state for the voice
policy, marks it as unsent; `Advance`, `Pump` and any later call send the unsent state first, as the latest values
(one command per voice for volume, pan and pitch, then its pause, then its stop), so a stalled sink costs at most
one waiting change per voice instead of a growing backlog. A sound that does not fit is refused before it takes a
voice (`VoiceHandle.None`; a stream is disposed), so no handle names a sound the render function never heard of.
`DroppedCommands` now counts the calls that had to wait or were refused. Game-thread calls with a full queue
allocate nothing (`Calls_made_while_the_queue_is_full_allocate_nothing`); a stress run of 80 million calls against a
rendering thread gave no exception and 0 bytes on the render thread.

**Known and left**: sounds started while the sink was stalled, before the queue filled, all start when it renders
again (at most one per voice survives, the last started). Not changed: it is bounded by the voice limit and needs a
browser run to judge (**hypothesis**).

### 5.5 Also pinned

Handles whose voice another sound took (stopped, stolen, ended, ended by a pitch change), `VoiceHandle.None` and
handles that never existed do nothing to the sound now in that voice, through the engine and through `Game.Audio`;
a pitch change in a clip's last three frames ends the sound where the timeline says, for pitches 0.01, 1 and 100;
pitch changes at the tail of clips of 1, 2, 3 and 801 frames, mono and stereo, looping or not, stay inside the clip.
These passed when written (no defect found).

### 5.6 Correction of the timeline change (2026-11-13, second review)

An independent verifier found two blockers in the first version of §5.2 (`3a66d01`); both are **demonstrated
failures of that version**, not of `main`, and are fixed.

1. **Catch-up steps could move the timeline back.** That version waited for the sink whenever the timeline was more
   than a quarter second ahead of the device, a fixed threshold that the legitimate catch-up steps of one slow frame
   cross when a step is long or the steps are many. Spacing of consecutive steps' `Now`, verifier's probe (`repro`):

   | Update rate, catch-up steps | `3a66d01` | Now |
   | --- | --- | --- |
   | 15, 5 | 3200 ×4, **-9600**, 3200, 3200 | 3200 ×6, 3000 |
   | 10, 5 | 4800 ×3, **-9600**, 4800 ×3 | 4800 ×6, 4500 |
   | 60, 20 | 800 ×16, **-12000**, 800 ×4, 750 | 800 ×21, 750 |
   | 60, 5; 30, 5; 20, 5; 60, 16 | all positive | unchanged |

   The threshold is gone. A stalled sink is told from catch-up by what distinguishes them: the sink's rendered
   position has not moved during a second of steps spread over at least two host frames. The catch-up steps of one
   frame are steps of a single host frame, whatever the update rate and the limit of steps per frame, so they never
   move the timeline back; recovery shortens a step by a sixteenth at most. Pinned by
   `Catch_up_steps_never_move_the_timeline_back_at_any_update_rate` (rates 2, 10, 15, 30, 60 and 144 with 5 to 40
   steps in a frame; six of eight rows failed on `3a66d01`) and
   `At_other_update_rates_a_sound_starts_within_a_step_of_what_the_sink_renders_next`.

2. **A stalled sink was waited for four times as often** (an undisclosed change of that version). Verifier's probe
   (`stall`: two seconds without rendering at 60 steps per second, a one-frame click per step):

   | | `main` (verifier's figures) | `3a66d01` | Now |
   | --- | --- | --- | --- |
   | `Now` ahead of what is rendered during the stall | 70 to 1054 ms | 17 to 253 ms | 17 to 948 ms |
   | Times `Now` moved back | 1 | 7 | 1 |
   | Clicks heard after resume span | (not given) | 971 ms | 967 ms |
   | Largest summed click amplitude on one frame | 0.50 | 1.00 | 1.00 |

   Decision: **the behaviour of `main` is restored** (wait for the sink once per second of steps). For a player the
   two differ only in how the sounds of a long stall are folded when the output resumes: either way they are all
   heard within about a second; the shorter window of `3a66d01` put four times as many of them on top of each other
   and moved the timeline back four times as often, for no gain a player hears, since a suspended output plays
   nothing meanwhile. One difference from `main` remains on the probe's last row: the timeline now sits within a step
   of the sink where `main` sat a whole queue ahead, so in this probe (exact 60 Hz frames) some clicks of the two
   folded seconds and of the first steps after resume land on one frame; the timeline waits half a step ahead of the
   sink so that two folded seconds interleave. With sounds longer than one frame the overlap is the same in all
   three. **User-perceptible difference: none demonstrated against `main`.** Pinned by
   `A_sink_that_does_not_render_is_waited_for_once_a_second`.

   The timeline still moves back once per second of a real stall, as on `main`. Voice bookkeeping across it is
   pinned by `Sounds_played_while_the_sink_did_not_render_play_whole_and_end_once_it_renders_again`: a half-second
   sound played and a loop stopped with a fade before the timeline goes back are still playing after it, the sound
   is heard whole and once when the sink renders again, the fade completes, and the handles end. Pausing or changing
   the pitch of a sound whose start frame is ahead of the timeline that went back no longer counts frames it has not
   played (read from the code and guarded; not reproduced as an audible fault). Music fades of `Game.Audio` are
   volume changes of a voice per update and go through the same path; they have no test of their own across a stall.

Also from that review:

- **(a) A call between a frame's last step and the next frame's first one used up that step's check
  (demonstrated failure of `3a66d01`).** The timeline was checked against the device at a step's first call only, so
  a call made while drawing fixed the next step's frame a frame early. Verifier's probe (`paced framecall`, real
  threads, 20 s at 60 fps with a 100 ms hitch every 4 s): 4 late sounds (max 94.7 ms) before, 0 now. The check now
  runs at every sound or change (one volatile read). Without frame-phase calls the same probe reported 0 late sounds
  before, and in two runs now 2 (by at most 0.7 ms) and 0: less than one render block, which the render thread can
  finish between the call and the probe's reading (hypothesis; not pursued). Pinned by
  `A_call_between_the_frames_steps_does_not_make_the_next_steps_sounds_late`.
- **(b) A resume kept while the queue was full was never sent if the sound ended on the timeline first
  (demonstrated failure of `08692db`**, reproduced by
  `A_resume_that_could_not_be_queued_arrives_even_if_the_sound_ended_on_the_timeline_meanwhile`, red before): the
  render function kept the sound paused until another sound took its voice, and what was left of it was never
  heard, unlike a resume that had been queued. The kept resume is now sent whether or not the sound is still on the
  timeline.

## 6. Left for the owner

Nothing here blocks the batch.

0. **Sounds played into a stalled output**: done on 2026-11-14 at the owner's request (§7.2).

1. **Measure in a browser** (and on raylib, Android and iOS): the latency after §5.2 and what is heard when a
   suspended page resumes are read from code only.
2. **Changes in consecutive updates while the timeline is ahead** (§5.1, last point): done on 2026-11-14 (§7.3);
   the deviation measured there is up to 67 ms, not a few milliseconds.

Also not done: gain ramps for volume and pan changes (they step at the update's frame; small per-update changes are
fine, large jumps can click), effects on buses, a pitch per bus, pitch for streamed music, and measurements in a
browser, on raylib, Android and iOS.

## 7. Suspension and change timing (2026-11-14)

The owner's third request (record L16). Measured with a program outside the repository, built three times against
`main` (`16af5af`), the batch as it stood (`a066d9b`) and the result: a simulated real-time sink (a 70 ms queue,
rendering as the device plays, able to stop), the engine's default time policy (60 updates per second, at most five
in a frame), no threads and no clock, so every figure is exact and repeatable. Nothing here was run in a browser, on
raylib, Android or iOS.

### 7.1 When an output stops, by backend

Verified: read in the code at the location given, or in the documentation or source linked. Assumed: reasoned from
those, not run.

| Backend | When the sink stops rendering | Does the game stop too | Sound in the background |
| --- | --- | --- | --- |
| SDL, desktop | Verified: never by the engine. A feeder thread renders while SDL's stream holds less than 60 ms (`src/MicroEngine.Sdl/SdlFeederSink.cs`); the engine never calls `SDL_PauseAudioDevice`, and SDL devices start unpaused and pause only through it ([SDL_PauseAudioDevice](https://wiki.libsdl.org/SDL3/SDL_PauseAudioDevice)). Assumed: it stops if the device stops taking audio (unplugged, the system asleep) or the feeder thread is starved. | No. Losing focus releases the input and nothing else (`src/MicroEngine/Core/Game.cs:401`); a minimized window skips drawing (`SdlGpuDevice.cs:449`). | Always on, mandatory today: there is no setting. A game can lower a bus on `Lifecycle.FocusChanged`. |
| raylib, desktop | Verified: a feeder thread refills each buffer raylib has played (`src/MicroEngine.Raylib/RaylibAudioDevice.cs`), nothing pauses it. Assumed: raylib does not pause its device when the window loses focus. | No. | As SDL. |
| Browser | Verified: the sink renders only what the worklet's queue lacks (`BrowserAudioSink.Pump`), and the queue empties only while the `AudioContext` runs. A hidden page: the host suspends the context (`Hosts/web/wwwroot/main.js:279`) and browsers stop animation frames ([Page Visibility](https://developer.mozilla.org/en-US/docs/Web/API/Page_Visibility_API)). A visible page whose context the browser holds `suspended` (the autoplay policy, [Autoplay guide](https://developer.mozilla.org/en-US/docs/Web/Media/Guides/Autoplay)) or `interrupted` (a call, a closed lid, [state](https://developer.mozilla.org/en-US/docs/Web/API/BaseAudioContext/state)): the game runs and the sink renders nothing. | Hidden: yes (`Hosts/web/Program.cs:85`, `SdlPlatform.cs:533`), the music track pauses (`Game.cs:585`). Suspended or interrupted while visible: **no**, this is the case of §7.2. | Mandatory: a hidden page gets no frames. Not configurable. |
| Android (SDL) | Verified in SDL's source: SDL pauses its audio devices on the way to the background only while it blocks its event loop ([SDL_androidevents.c](https://github.com/libsdl-org/SDL/blob/main/src/video/android/SDL_androidevents.c), `Android_OnPause`), and the engine turns that blocking off (`SdlPlatform.cs:160`). Assumed: the feeder therefore keeps rendering in the background. | Yes: no update runs while suspended (`SdlPlatform.cs:541`), and the music track pauses. | Assumed, see §7.5: voices other than the music track keep sounding. |
| iOS (SDL) | Verified in SDL's source: an audio session interruption stops SDL's queue ([SDL_coreaudio.m](https://github.com/libsdl-org/SDL/blob/main/src/audio/coreaudio/SDL_coreaudio.m), `InterruptionBegin`); the host declares no background audio (`Hosts/ios/Info.plist`). Assumed: the system suspends the process in the background. | Yes. | Mandatory off. |

So the engine has no background-audio setting anywhere; where the platform suspends the game, sound and game stop
together and nothing is skipped. The one case where the output stops and the game goes on is a visible page whose
context is not running, and a desktop device that stops taking audio.

### 7.2 What is heard when the output plays again

**Demonstrated failure.** A one-shot of 200 ms played ten times a second, a one-second loop, the sink stopped for
0.5, 2 and 30 s, then rendering again:

| The second after the output is back | `main` | Batch (`a066d9b`) | Now |
| --- | --- | --- | --- |
| 0.5 s stop: one-shots heard (10 in a normal second) / peak (0.50 normally) | 3.2 / 0.50 | 3.9 / 0.50 | 10.0 / 0.50 |
| 0.5 s stop: how late a sound played afterwards is | 553 ms, for good | 471 ms, back within a step after 7.3 s | 8 ms |
| 2 s stop: one-shots heard / peak | 3.2 / 0.50 | 15.3 / 1.50 | 10.0 / 0.50 |
| 2 s stop: how late a sound played afterwards is | 1037 ms, for good | 8 ms | 8 ms |
| 30 s stop: one-shots heard / peak | 35.1 / 7.25 | 61.7 / 7.75 | 10.0 / 0.50 |
| 30 s stop: voices busy at its end (of 32) | 31 | 31 | 2 |
| The timeline went back (2 s / 30 s) | 1 / 29 times | 2 / 30 times | never |
| The loop | resumes where it stopped | resumes where it stopped | is where the game is (within 21 ms of the stop's length) |

**User-perceptible difference.** Before, a stop of more than a second folded every second of its sounds onto the
second after it (the batch more densely than `main`: up to 15 sounds on one frame), and a shorter stop left every
later sound late by its length, for good on `main` and for seconds on the batch.

**The rule.** Once the sink has rendered nothing for a quarter of a second of updates, over at least two host
frames, the output has stopped: the timeline goes on and the frames the output did not play are skipped, at that
update and at each one until it renders again. The main thread only adds to a counter; the render function, when it
next runs, first takes everything queued (each command carries its frame) and then passes the skipped frames without
mixing: sounds start, change, stop and end at their frames, a clip moves on by the frames it would have played.

| Sound | Outcome | Why |
| --- | --- | --- |
| One-shot whose length has passed | Dropped, handle ended | It would have been heard whole had the output played |
| One-shot still within its length | Advanced: goes on from where the game is | Its remainder belongs to the present |
| Loop, music from a clip | Advanced: in step with the game | Same rule; a loop has no end to reach |
| Streamed music | Kept where it stopped, its end moved later | Passing frames of a stream means decoding them, on the main thread, for nobody |
| Sound paused by the game | Kept paused at its position | Pausing is the game's own request |
| Stop with a fade | The fade runs on the game's time | A stop is a point in the game |

Stale handles end by themselves: the mixer's own timeline was never moved, so a sound's end comes when its length
has passed. A quarter of a second, not the second the wait used: the wait moved the timeline back, which catch-up
updates must never trigger, and is gone; the skip covers the whole gap whenever it is noticed, so the threshold only
decides which gaps are stops, and a half-second stop left untreated is the 471 ms row above. The check that several
host frames went by is kept, so the catch-up updates of one slow frame never count, at any update rate.

**Alternative not taken**: hold everything that lasts (loops and clip music resume where they stopped, as streams
do) and drop only one-shots. It needs a rule for which clips are "music", and a fade-out asked for during the stop
would then be heard after it. Taking it instead is a change inside `Skip` in `AudioEngine.cs`.

### 7.3 When a change takes effect

**Demonstrated failure.** A voice held one waiting change and a bus one waiting volume, so a change still waiting
when the next update's arrived was applied at once. One change per update on one sound; error between the frame it
took effect and the frame it was issued for:

| Early by, median / p95 / max (ms); changes never heard | `main` | Batch | Now |
| --- | --- | --- | --- |
| Steady 30 fps | 53.3 / 53.3 / 53.3; 299 of 600 | 0 | 0 |
| Steady 60 fps | 36.7 / 36.7 / 36.7; 0 | 0 | 0 |
| Steady 144 fps | 39.4 / 42.2 / 42.2; 0 | 0 | 0 |
| 60 fps, a 40 ms frame every 2 s | 53.3 / 70.0 / 70.0; 5 of 605 | 0 / 0 / 16.7; 1, for 317 ms | 0 |
| 60 fps, a 120 ms frame every 2 s | 120 / 120 / 120; 19 of 620 | 0 / 61.5 / 66.7; 16, for 1167 ms | 0 |
| 60 fps, a 500 ms frame every 2 s | 120 / 120 / 120; 16 of 620 | 0 / 61.5 / 66.7; 16, for 1167 ms | 0 |

Volume, pan and pitch give the same figures (pitch does not exist on `main`). On `main` every change was early,
because every sound started more than an update after its call; the batch's latency change removed that in steady
play and left the second after each slow frame. **User-perceptible difference**: after a slow frame, three or four
of its updates' values were skipped and one applied at once (a fade of half a second jumps 13 % of its range instead
of 3 %), then each change came up to 67 ms before the sounds started in the same update, for about a second. A pitch
glide ran ahead by the same amount.

**Fix.** Each voice keeps eight waiting changes inside its render state, oldest first, each applied at its frame;
each bus keeps eight waiting volumes the same way. Nothing is allocated and nothing is locked. With more than eight
(a `DelayFrames` over eight updates), the two oldest become one at the later frame: late, never early. The start of
sounds and the timeline are untouched, so the latency of §5.2 is as it was.

Offline renders (the second review's program, 162 files): 138 are byte-identical to the batch. The 24 that differ
all have a delay and either changes to one sound or bus in consecutive updates with a delay over one update, or two
volumes of one bus in one update (the first was heard for the length of the delay; it no longer is). Of the 98
renders with a delay that have a twin without, 12 were not that twin moved by the delay before, and 4 are not now:
the same 4 as before (`steal_priorities` and `two_streams`), which no change here touches.

**Correction after the third review (2026-11-14).** The first overflow rule merged the two oldest waiting changes
at the later frame, which moved the oldest frame one update later at every arrival: with a change every update and
eight or more updates waiting, nothing was applied while changes kept arriving (**demonstrated failure**: a delay of
eight updates, 300 changes, 8 reached the sound and the value was stale for 4.9 s). The oldest waiting change now
always takes effect at its frame. When a ninth arrives, the waiting change whose two neighbours are closest (never
the oldest) joins the one after it, so the eight frames kept stay spread over the wait.

| One change per update (the review's program) | First rule | Now |
| --- | --- | --- |
| Delay of 8 updates, 300 changes | 8 reached the sound, stale for 4.9 s | at most 1 update (17 ms) behind, none early |
| Delay of 30 updates | never caught up | at most 5 updates (83 ms) behind |
| Real time, slow frames of 5 updates (120 ms) | exact, 619 of 619 | exact, 619 of 619 |
| Real time, slow frames of 10 updates (170 ms) | stale for up to 496 ms | at most 1 update (17 ms) behind |
| Real time, slow frames of 20 updates (340 ms) | stale for up to 7.2 s | at most 3 updates (50 ms) behind |
| Real time, slow frames of 40 updates (700 ms) | not measured | at most 7 updates (117 ms) behind |

The bound the tests hold, for voices and buses, at delays of 1, 8, 9, 16 and 64 updates and slow frames of 1 to 40:
with fewer than eight updates waiting every change is on its frame; with `w` waiting the value heard is at most
`(w + 3) / 4` updates behind, never ahead, and the last change arrives at its own frame. The capacity stays eight:
the default time policy leaves six waiting at most, and what a larger one would buy is now a bounded lag rather
than a stall. Offline renders against the previous commit: all 162 byte-identical (none of that program's
scenarios has eight changes waiting).

The stop detection of §7.2 counted a sink that renders in blocks longer than a quarter of a second (16384 frames)
as stopped between two blocks. No shipped backend does that; the threshold is now also at least twice the time of
the largest block the sink has asked for.

### 7.4 Tests

`AudioEngineChangeTimingTests` (15 cases, all failing before the fixes) and `AudioEngineSuspensionTests` (18 cases:
10 failed before the fix; the other 8 pin what must not change: pausing, muting, streams, short gaps, slow frames,
deterministic runs). Each asserts that the render function allocated nothing. Two tests of the wait that the skip
replaces were removed from `AudioEngineTimelineTests`.

### 7.5 For the owner

1. **Android in the background (hypothesis, from code and SDL's source, not run on a device).** The engine turns
   off SDL's blocking on pause so that it sees the suspension, and SDL pauses its audio devices only when it blocks.
   The game's suspension pauses the music track only (`Game.Suspend`), so looping voices, music voices and the
   tails of sounds would keep sounding with the app in the background. Scope if confirmed: either `Game.Suspend`
   and `Resume` pause and resume the output (a `Suspend`/`Resume` pair on `IAudioSink`, a contract change, SDL's
   being `SDL_PauseAudioDevice`), or they pause every voice through the mixer (no contract change, in
   `src/MicroEngine/Core/Game.cs`). Both are outside this work's files.
2. **A background-audio setting on desktop** does not exist. If wanted, it is the same pair on the sink driven by
   `FocusChanged`, with a setting; today a game does it with a bus volume.
3. **Clip music after a stop** is where the game is, streamed music where it stopped (§7.2). Making both hold is
   the alternative described there.
4. **Hypothesis, not measured**: at a page's start the worklet's module loads asynchronously while the game already
   renders into a pending list (`main.js`, `audio.write`), so the first sounds would be late by the load time.

