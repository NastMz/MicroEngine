# ARCH-1 P5: PCM audio sinks on desktop, web and mobile

A throwaway prototype. Nothing here is built by `MicroEngine.slnx` or by CI (`experiments/` needs no checks), and the
engine is not changed: the probes carry their own small mixer and their own sinks.

## Question

The engine will have one managed mixer and backends become PCM sinks (dossier §6.4, §6.5, ADR 0033 in the plan).
Before `IAudioSink` is frozen (stage 2c), P5 asks which shape it must have:

- **Pull**: the backend asks the engine for N frames when it needs them (`Start(AudioRender render)` in the §6.4
  sketch, "pulled on the backend's audio thread").
- **Push**: the engine writes into the backend's queue from the game loop, keeping a target amount buffered.

on desktop SDL3 (an `SDL_AudioStream` get callback on SDL's device thread), on the web (SDL3 on Emscripten without
threads, as P4 runs it, and an AudioWorklet), and on Android/iOS (SDL3 audio under .NET for Android/iOS), compared on
latency, robustness to long frames (100 ms and 300 ms main-thread stalls), threading constraints for a managed mixer,
determinism for tests (offline rendering) and implementation cost on the web.

## Verdict

**Freeze one pull contract whose caller is chosen by the sink; do not freeze a push contract.** The mixer is always a
`AudioRender(Span<float>)` callback. A sink either calls it on a thread it owns (desktop, phones) or is pumped by the
host once per frame on the main thread (browsers, offline tests). The default sinks are:

| Host | Default sink | Measured here |
| --- | --- | --- |
| Desktop, Android, iOS (SDL3) | **Feeder**: a sink-owned managed thread keeps 60 ms queued in SDL's audio stream; SDL's device thread only reads the queue (native code) | Windows: nothing lost through 100 ms and 300 ms main-thread stalls and 53–60 ms full collections, sounds reach the device 64–71 ms after `Play`. Android emulator (40 and 150 ms targets): nothing lost beyond the measure's noise |
| Browser | **AudioWorklet queue**, pumped from `requestAnimationFrame` up to 100 ms ahead; SDL's audio is not used | Survives stalls up to about the target (100 ms target: 7–17 ms lost on a 100 ms stall; 150 ms: nothing); beyond it, loss = stall − target |
| Tests | **Offline**: one pump renders exactly one simulation step of frames | Bit-identical output on CoreCLR, Mono Android and the .NET wasm interpreter |

Why not the other shapes:

- **SDL's get callback (pull on the device thread)** has the lowest latency (13–18 ms on Windows) and ignores
  main-thread stalls, but it runs managed code on SDL's device thread, and every garbage collection suspends that
  thread: 50–90 ms forced collections lost 23–73 ms of audio on Windows. It stays possible as a low-latency option of
  the SDL sink (for games that keep collections short), not as the default.
- **Push from the main loop** loses about `stall − target` on every platform (100 ms stall, 50 ms target: 59–74 ms lost
  on Windows, 50–69 ms on Android; 300 ms stall, 150 ms target: 166–227 ms and 169–248 ms), so stall tolerance costs
  the same amount of sound-effect latency, and the game loop owns audio timing.
- **SDL audio on the web** cannot be made robust: SDL's Emscripten driver plays through a ScriptProcessorNode whose
  `onaudioprocess` runs on the page's only thread, so every main-thread stall is a dropout whatever is queued: a 400 ms
  SDL queue still dropped 85 ms on a 100 ms stall and 256 ms on a 300 ms stall. The managed mixer works there (pull
  runs, 0 errors, 13 ms in-graph latency with 512-frame buffers), it just cannot be protected.
- **A pull callback on the web audio thread** (what the dossier asked P5 to prove) is not possible on single-threaded
  .NET wasm: the AudioWorklet runs in its own global scope on Web Audio's thread, where there is no .NET runtime. The
  managed mixer can only run on the main thread; the worklet can only play what the main thread already rendered.

### Proposed contract (to freeze before stage 2c closes)

```csharp
namespace MicroEngine.Platform;

/// <summary>Interleaved 32-bit float PCM: <see cref="Channels"/> samples per frame at <see cref="SampleRate"/>.</summary>
public readonly record struct AudioFormat(int SampleRate, int Channels);

/// <summary>
/// Fills all of <paramref name="buffer"/> (<c>buffer.Length / Channels</c> frames). Called by one thread at a time; which
/// thread depends on <see cref="IAudioSink.Schedule"/>. Must not block, allocate, take a lock the main thread holds, or throw.
/// </summary>
public delegate void AudioRender(Span<float> buffer);

public enum AudioSinkSchedule
{
    /// <summary>The sink calls <see cref="AudioRender"/> on a thread it owns; <see cref="IAudioSink.Pump"/> does nothing.</summary>
    SinkThread,
    /// <summary>The host calls <see cref="IAudioSink.Pump"/> once per frame on the main thread, which calls <see cref="AudioRender"/>.</summary>
    HostPump,
}

public interface IAudioSink : IDisposable
{
    AudioFormat Format { get; }
    AudioSinkSchedule Schedule { get; }
    /// <summary>Frames rendered ahead of the device (queue target + device period): the mixer's minimum scheduling delay.</summary>
    int LatencyFrames { get; }
    /// <summary>Starts calling <paramref name="render"/>. Page-driven hosts call it inside the user gesture that starts the game.</summary>
    void Start(AudioRender render);
    /// <summary>HostPump sinks: render until the queue holds its target. SinkThread sinks: no-op. Main thread, once per host frame.</summary>
    void Pump();
}
```

The prototype's `IAudioSink` (`Shared/AudioSink.cs`) carries more members (`StarvedFrames`, `PlayedFrames`,
`DevicePeriodFrames`, an `aheadFrames` argument): they exist only to measure and are not proposed.

### Threading rules

1. `AudioRender` runs on exactly one thread at a time, but which thread is the sink's choice (a feeder thread, SDL's
   device thread, the browser's main thread, a test thread). The mixer is written once, thread-agnostic.
2. Game code talks to the mixer only through a single-producer/single-consumer command queue (the prototype's
   `ProbeMixer.Play`); render never takes a lock shared with the main thread.
3. Render does not allocate (a collection suspends whichever thread renders) and never touches files: clips and music
   are decoded from memory already loaded by the content system; streaming reads run on a loader thread (AU-6).
4. `Pump` is called by the host once per frame after the simulation steps, on every host; it costs nothing on
   `SinkThread` sinks, so the loop does not branch on the platform.
5. `Dispose` returns only after the sink has stopped calling `render`.
6. Page-driven hosts start the sink inside the user gesture (P4 rule), create the AudioContext there and stop pumping
   when the page is hidden (requestAnimationFrame stops anyway; the worklet then plays silence).

### Buffer and latency policy

- The sink owns its target; the engine exposes one latency setting that sinks read (milliseconds, clamped per sink).
- Desktop and phones: 48 kHz stereo F32, feeder target **60 ms**, refilled every 2 ms. The rule is "longer than the
  longest collection": 40 ms lost 21–42 ms when collections took 50–90 ms, 60 ms lost nothing with 53–60 ms ones, 20 ms
  lost up to 659 ms. The device period is SDL's (10 ms WASAPI, 20 ms AAudio). A game that keeps collections short can
  lower it, or use the SDL sink's device-callback mode (13–18 ms).
- Browser: AudioWorklet target **100 ms** by default (a hitch shorter than about target − one frame is inaudible;
  150 ms covered the 100 ms stall completely), configurable by the game; render quantum 128 frames; the AudioContext
  adds its own `baseLatency` + `outputLatency` (10 + 56–64 ms in headless Chrome).
- Offline: exactly one simulation step of frames per pump, in blocks of at most `LatencyFrames`.
- Sounds are scheduled, not just queued: the mixer stamps each command with the output frame of its simulation step
  plus a delay of at least `LatencyFrames`, and starts a late command at the next block. Stamped commands give the same
  output whatever block sizes the sink asks for (measured below); "start at the next block" does not.

## Environment

| Item | Version |
| --- | --- |
| Machine | Windows 11 Pro 10.0.26300, Git Bash |
| .NET | SDK 10.0.403, runtime 10.0.14 (desktop CoreCLR, workstation concurrent GC) |
| SDL | 3.5.0 at `ea7a2da` (ppy.SDL3-CS 2026.1002.1 natives on desktop and Android; `SDL3.a` built with P4's `build-sdl3-wasm.sh` for the web, no threads) |
| Web toolchain | `web/setup.sh`: browser-wasm runtime pack 10.0.14, Emscripten 3.1.56 |
| Browsers | Chrome 156.0.8078.76 headless (puppeteer-core 25.13.0), Firefox 150 headless (one run per sink) |
| Android | Emulator AVD `Medium_Phone_API_36.1` (Android 16, x86_64 image, software GPU), .NET for Android 36.1.118 (Mono), SDL's AAudio driver, low-latency mode |
| Desktop audio | Default WASAPI playback device, 48 kHz, SDL device period 480 frames |

## Method

One scenario (`Shared/Scenario.cs`), driven one frame at a time so a blocking 60 Hz loop and `requestAnimationFrame`
run it the same way:

| Time | Event |
| --- | --- |
| 0–2 s | steady play: a continuous bed tone plus a short tone posted every 250 ms (the "sound effect" whose latency is measured) |
| 2 s | the main thread busy-waits 100 ms (a long frame) |
| 4 s | the main thread busy-waits 300 ms |
| 5.5 s | allocation burst + full GC: 1.5 million live small objects and 100 MB of short-lived arrays, then a blocking compacting gen-2 collection while the graph is alive |
| 7 s | end |

The mixer (`Shared/ProbeMixer.cs`) is the shape the engine mixer will have: SPSC command ring, voices with
oldest-first stealing, no allocation or lock in `Render`. Sinks (`Shared/SdlSinks.cs`, `Shared/FeederSink.cs`,
`Web/Program.cs` + `Web/wwwroot/worklets.js`, `Shared/OfflineSink.cs`):

| Sink | Who renders | Queue |
| --- | --- | --- |
| `sdl-pull` | SDL's device thread, through `SDL_OpenAudioDeviceStream` with a get callback (exactly the bytes SDL is missing) | none beyond the device period |
| `sdl-push` | the main loop, topping the stream up to the target | SDL stream |
| `sdl-feeder` | a sink-owned managed thread, topping up every 2 ms | SDL stream |
| `worklet` | the main loop (rAF); chunks posted to an AudioWorklet that plays them in order | worklet queue |

What was measured, per phase:

- **Lost audio.** Native: wall time minus audio the device took from the sink (`put − queued`), so starvation and a
  device thread that did not run both count; resolution about one device period (±10 ms Windows, ±20–50 ms on the
  emulator, whose AAudio consumption is bursty). Cross-checked on the push sink against SDL's own report of missing
  bytes (`P5_OBSERVE=1`): stall 300 ms, 150 ms target → 160–170 ms by both measures. Web: a tap AudioWorklet at the
  output reads a frame counter the mixer writes into the right channel, so it sees inserted silence, repeated buffers
  and skipped audio in what is actually played (a level detector missed them: Chrome repeats the ScriptProcessorNode's
  last buffers rather than playing silence).
- **Sound latency.** From `Play` on the main thread to the render that contains the sound's first frame, plus the
  frames queued in front of it and the device period (the part the sink knows; the OS or the AudioContext adds its own).
- **Render threads**, gaps between render calls, render cost, garbage collections.
- **Determinism** (`Shared/Determinism.cs`): the same scripted session (120 steps at 60 Hz, sounds on some steps)
  rendered with no device, asked for as a pulled device would (blocks of B frames, independent of steps) and as a
  pumped sink would (one step per pump), for B = 128, 441, 800, 1024, 4000; commands either start at the next block or
  are stamped at `step × 800 + 1600` frames. Output hashed (FNV-1a over the float bits).

## Results

### Desktop (Windows, WASAPI, 10 ms device period)

Three runs per row (ranges); "lost" in ms per phase, ±10 ms is the measure's resolution (negative values are noise).
In this batch the forced collection took 49–91 ms and the whole burst 116–212 ms of GC pause (14–15 collections).

| Sink | Target | Sound latency ms (median / max) | Steady | Stall 100 ms | Stall 300 ms | Allocation burst + GC |
| --- | --- | --- | --- | --- | --- | --- |
| SDL pull (device thread) | — | 13–18 / 17–19 | −5..0 | 0..11 | −7..3 | **41–73** |
| SDL push (main loop) | 50 ms | 40 / 57–65 | −6..−1 | **59–74** | **263–450** | **177–247** |
| SDL push (main loop) | 150 ms | 140 / 165–167 | 0..4 | −1..4 | **166–227** | **81–166** |
| SDL push (main loop) | 400 ms | 390–400 / 407–413 | −5..2 | −5..4 | 4..9 | −4..1 |
| Feeder | 40 ms | 43–47 / 49–53 | −2..−1 | 5..7 | −5..0 | **21–42** |
| Feeder | 60 ms | 64–68 / 70–71 | −5..5 | −5..5 | 0..5 | −6..1 |
| Feeder | 80 ms | 84–88 / 90–91 | 0..5 | −9..6 | −6..0 | −3..7 |
| Feeder | 100 ms | 105–109 / 109–112 | 0..2 | −9..0 | −4..6 | −3..1 |
| Feeder | 150 ms | 154–157 / 159–162 | 2..6 | −2..0 | −10..0 | 5..11 |

- The pull callback ran on one non-main managed thread (SDL's device thread); only garbage collection reached it: the
  longest gap between its calls was 62–84 ms in the GC phase against 11–14 ms elsewhere. An earlier batch with
  46–62 ms forced collections gave pull 23–47 ms lost and feeder 40 ms 1–7 ms lost.
- A feeder target of 20 ms lost 137–659 ms in the GC phase and 10 ms lost 71–106 ms (`results/desktop-feeder-small-targets.jsonl`):
  the feeder's target must exceed the longest single pause.
- Cross-check on the push sink with SDL's own count of missing bytes (`P5_OBSERVE=1`, 150 ms target): 300 ms stall
  170 / 170 / 171 ms by both measures. In the GC phase the observer counted less (30–80 ms) than the deficit (68–182 ms):
  the observer is managed code on SDL's device thread, so the collection suspends the very thread that would count.
- Mixer cost per 1024-frame call (21.3 ms of audio): 0.04 ms bed only, 0.15 ms with 16 extra voices, 0.60 ms with 64
  (0.2 %, 0.7 %, 2.8 % of real time).

### Web (Chrome 156 headless, .NET 10 wasm, single thread)

Two runs per row. "Dropout" is audio that was not the sink's own continuous stream at the output (tap worklet);
"latency" excludes the AudioContext's own 66–74 ms (`baseLatency` + `outputLatency` reported by headless Chrome).

| Sink | Device buffer | Target | Sound latency ms (median / max) | Steady | Stall 100 ms | Stall 300 ms | Allocation burst + GC |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SDL pull (ScriptProcessorNode) | 2048 frames (43 ms) | — | 55–61 / 87 | 0–3 | 85 | 256 | 171–256 |
| SDL pull, `SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES=256` | 512 frames (11 ms) | — | 13 / 23 | 0 | 85 | 299 | 192 |
| SDL push | 2048 frames | 60 ms | 70 / 100 | 0–5 | 111–153 | 256–259 | 171 |
| SDL push | 2048 frames | 150 ms | 160–170 / 190 | 0–3 | 85 | 256 | 234–259 |
| SDL push | 2048 frames | 400 ms | 410 / 440 | 3 | 85 | 256 | 171 |
| AudioWorklet | 128-frame quantum | 40 ms | 32 / 35–40 | 0–8 | 67 | 267 | 179–237 |
| AudioWorklet | 128-frame quantum | 60 ms | 52 / 111 | 0 | 47–49 | 247–249 | 188 |
| AudioWorklet | 128-frame quantum | 100 ms | 92 / 95–168 | 0 | 7–17 | 207–209 | 127–169 |
| AudioWorklet | 128-frame quantum | 150 ms | 142 / 145 | 0 | 0 | 157–159 | 127–138 |
| AudioWorklet | 128-frame quantum | 400 ms | 392 / 400 | 0 | 0 | 0 | 0 |

- SDL's driver (`SDL_emscriptenaudio.c`) creates a ScriptProcessorNode of `2 × SDL_GetDefaultSampleFramesFromFreq`
  frames and calls `SDL_PlaybackAudioThreadIterate` from its `onaudioprocess` (`dynCall`); this SDL has no
  AudioWorklet path. The managed get callback therefore runs on the main thread (`renderOnMainThread: true`, the same
  managed thread as `Frame`), between frames, never inside one. During a stall Chrome replays the node's last buffers
  (+85 ms per 2048-frame buffer pair), then skips what was rendered late.
- The "allocation burst + GC" frame lasted 184–300 ms on wasm (1.5 million objects under the interpreter, 35
  collections, the forced one 15–38 ms); the worklet loses what exceeds its target, as with a stall.
- Firefox 150 (one run each): SDL pull 59 / 195 / 0 ms lost (stall 100 / stall 300 / GC), worklet 150 ms 0 / 159 / 77:
  the same pattern.
- Mixer cost under the interpreter, 1024 frames: 0.1 ms with the bed only, 0.3–0.5 ms with 16 extra voices (2.3 % of
  the 21.3 ms it renders), on the main thread.

### Android (emulator, AAudio, 20 ms device period at 44.1 kHz)

Three runs per row; the emulator's measure is ±20–50 ms per phase (negative values are measurement noise).

| Sink | Target | Sound latency ms (median / max) | Steady | Stall 100 ms | Stall 300 ms | Allocation burst + GC |
| --- | --- | --- | --- | --- | --- | --- |
| SDL pull (SDL's audio thread) | — | 28–33 / 40–59 | −22..−2 | −18..0 | −2..1 | −5..−3 |
| SDL push (main loop) | 50 ms | 50 / 70 | −36..0 | 50–69 | 271–313 | 245–727 |
| SDL push (main loop) | 150 ms | 150 / 167–178 | −22..−6 | 0–2 | 169–248 | 268–889 |
| Feeder | 40 ms | 45–52 / 58–63 | −23..35 | −16..2 | −2..19 | −4..−1 |
| Feeder | 150 ms | 160–162 / 169–172 | −4..31 | −54..2 | 0..19 | −5..−3 |

- SDL's Android driver is AAudio (low-latency mode, `freq / 50` frames per callback, capacity two callbacks). SDL
  renders on its own audio thread into a ring that the AAudio data callback copies from, so the managed get callback
  runs on SDL's thread (a native pthread, attached to Mono on first call: thread 4, main thread 3), never on AAudio's
  real-time callback thread. No error in any run.
- The allocation burst made the main thread stall 290–1040 ms on the emulator (forced collection 26–82 ms); the pull
  and feeder sinks lost nothing measurable there. In an earlier batch of two runs, one feeder run with a 150 ms target
  lost 155 ms in that phase (a 597 ms frame); it did not reproduce in the three runs above.
  `GC.GetTotalPauseDuration` returns 0 on Mono, so the pause itself is not reported.
- Mixer cost per 1024-frame call: 0.1 ms bed only, 0.44 ms with 16 extra voices (emulated x86_64).

### Determinism (offline rendering)

| Commands | Distinct outputs over 10 renderings (pulled and pumped, B = 128 … 4000) |
| --- | --- |
| Start at the next block | 5: every block size gives a different mix (pumped renderings agree with each other only because each pump starts at a step) |
| Stamped at step × 800 + 1600 | 1 for every block size up to the delay (1600 frames); B = 4000 > delay differs, as expected |

The stamped hash (`5917aa7d91dbbaf9`) and all others were identical on desktop CoreCLR, the browser's wasm
interpreter and Mono on Android, so a reference rendering made on one runtime checks the others (for this signal; a
mixer using other math functions must check it again).

### iOS (not run: no Mac or device here)

From SDL's source (`src/audio/coreaudio/SDL_coreaudio.m`): playback uses an AudioQueue created on a thread SDL starts for
it (`AudioQueueThreadEntry`, its own CFRunLoop), with three buffers or more (at least 15 ms); `PlaybackBufferReadyCallback`
calls `SDL_PlaybackAudioThreadIterate` on that thread, so a get callback would run managed code on an SDL-created
thread, as on Android. The feeder needs only a managed thread and `SDL_PutAudioStreamData`, which .NET for iOS supports;
the reverse P/Invoke (`[UnmanagedCallersOnly]` under AOT) is only needed for the optional pull mode. **Unverified.**

## Comparison

| | Pull on the device thread (SDL get callback) | Push from the main loop | Feeder (sink-owned thread) | AudioWorklet queue (web) |
| --- | --- | --- | --- | --- |
| Sound latency | lowest: 13–18 ms Windows, 28–33 ms Android emulator | = target | target + period: 64–71 ms at 60 ms | target + quantum + AudioContext latency |
| 100 / 300 ms main-thread stall | nothing lost (native); on the web everything lost (main thread) | lost: stall − target | nothing lost | lost: stall − target |
| Garbage collection | pauses the render thread: 23–73 ms lost on 50–90 ms collections (Windows) | lost while the GC frame exceeds the target | absorbed when the pause is shorter than the target | lost: frame − target |
| Threading for the mixer | managed code on a native device thread; reverse P/Invoke | main thread only | managed thread the engine owns | main thread only |
| Tests | offline sink (same mixer, no device) | same | same | same |
| Web cost | free (SDL), but unfixable dropouts | free (SDL), same dropouts | not available (no threads) | about 50 lines of JS (worklet + glue), about 40 lines of C#, one `JSImport` with a `MemoryView`; SDL audio not needed |

## What was not verified

- Real phones and tablets: Android only on an emulator (its audio timing is coarse and its CPU is emulated), iOS not at
  all. Bluetooth output, audio focus/interruptions (calls, other apps), the app going to the background.
- Safari, and any browser with a real audio device: headless browsers report their own output latency; nothing was
  heard. Tabs in the background (rAF stops, so a pumped sink runs dry: the host should suspend the AudioContext).
- The multithreaded .NET wasm runtime (`WasmEnableThreads`): it would allow a feeder in a worker writing a
  SharedArrayBuffer ring the worklet reads, at the cost of cross-origin isolation headers on every host.
- Long sessions, device changes (headphones plugged in), sample-rate changes, more than stereo.
- Real decoding work in render (Vorbis), and the CPU cost of the real mixer; the probe mixes sine tones.
- Actual xruns as counted by the OS (AAudio `getXRunCount`, WASAPI): the lost-audio measure is derived from what the
  device took from the sink.

## Steps to reproduce

```bash
P=experiments/arch-1/p5-audio-sinks
W=$(mktemp -d)                                          # scratch, outside the repository
# Desktop: offline check, mixer cost, then the sink matrix on the default playback device.
$P/run-desktop.sh "$W/desktop.jsonl" 3
# Web: SDL3.a with P4's script, publish, serve, measure.
web/setup.sh
pip install --target "$W/ninja" ninja
NINJA="$W/ninja/bin/ninja" experiments/arch-1/p4-sdl3-web/build-sdl3-wasm.sh ~/.microengine/web-tools/dotnet "$W/sdl"
$P/build-web.sh "$W/sdl/out/SDL3.a" "$W/web"
node web/serve.mjs "$W/web/wwwroot" 8095 &
npm install --prefix "$W/node" puppeteer-core@25.13.0
PUPPETEER_DIR="$W/node" node $P/measure-web.mjs http://127.0.0.1:8095/ 2 > "$W/web.jsonl"     # add --firefox for Firefox
node $P/summarize.mjs < "$W/web.jsonl"
# Android: one device or emulator on adb (here: emulator -avd Medium_Phone_API_36.1 -no-window).
$P/run-android.sh "$W/android.jsonl" 3
```

The raw JSON lines of the runs above are in `results/` (the web runs were made before render-gap statistics started
skipping the first 0.5 s; no web figure above uses those gaps). The projects write `bin/` and `obj/` next to themselves (ignored
by Git); delete them after use.
