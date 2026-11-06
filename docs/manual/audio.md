# Audio

MicroEngine audio has two halves. The core synthesizes sound effects (`Synth`) and step-sequenced music (`Song`)
into `AudioClip`s, and decodes PCM WAV files. The game-facing `AudioMixer` (`Game.Audio`) keeps a bank of named
sounds and music, plays them by name, and fades music in and out. Playback itself is done by the backend's
`IAudioDevice`, which also streams compressed music: OGG on every platform, MP3 on raylib only. Every
device is the engine's one mixer playing through the platform's output
([One mixer on every platform](#one-mixer-on-every-platform)).

## Pieces

| Type | Role |
| --- | --- |
| `AudioClip` | Interleaved float samples (-1..1), 1 or 2 channels; `FromWav(bytes)`, `ToWav()`, `Duration`, `Peak` |
| `SfxParams` + `Synth.Render` | Retro sound effects in the spirit of sfxr |
| `Synth.Layer`, `Synth.NoteFrequency` | Mix clips at time offsets; note name to Hz (`"C5"`, `"F#3"`) |
| `Instrument`, `Song` | Tracker-style voices and a step sequencer rendered to a looping stereo clip |
| `AudioMixer` | Named sound bank, music with fades, volumes |
| `IAudioDevice` | The platform's device. Every backend device is the engine's own mixer (`MixerAudioDevice`, `AudioEngine`) playing through the backend's PCM sink (an SDL audio stream on the SDL platform, the default on desktops and phones; a raylib audio stream on raylib desktops; an AudioWorklet in browsers); `SilentAudioDevice` headless or without hardware |

The synthesizer (`Synth`, `SfxParams`, `Wave`, `Song`, `Instrument`) is in the `MicroEngine.Audio.Retro` namespace, so
a file that makes sounds in code adds `using MicroEngine.Audio.Retro;`.

Source: [`src/MicroEngine.Audio/`](../../src/MicroEngine.Audio/)

## Synthesized sound effects

`SfxParams` is a record of waveform (`Square`, `Triangle`, `Saw`, `Sine`, `Noise`, `Pulse25`, `Pulse12`),
`Frequency` with optional `EndFrequency` slide, attack/sustain/decay times, volume, vibrato, arpeggio, low-pass
filter and bit-crush. `Synth.Render` is deterministic: the same parameters always give the same samples.

```csharp
Audio.AddSound("ding", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 880, Arpeggio = [1f, 1.5f], Sustain = 0.05f, Decay = 0.2f }));
```

Source: [`samples/HelloEngine/Program.cs`](../../samples/HelloEngine/Program.cs)

Meadow Run builds its whole sound bank this way, layering clips for richer hits:

```csharp
yield return ("stomp", Synth.Layer((R(Tone(Wave.Square, 260, 90, 0.02f, 0.1f, 0.35f, 2500)), 0), (R(Tone(Wave.Noise, 700, 200, 0.01f, 0.08f, 0.35f, 2000)), 0)));
```

Source: [`games/MeadowRun/Audio/SoundBank.cs`](../../games/MeadowRun/Audio/SoundBank.cs)

## Music with `Song`

`new Song(bpm, stepsPerBeat = 4)` holds tracks of whitespace-separated steps: a note (`C4`, `F#3`, `Bb5`), `-` to
hold, `.` to rest, `|` as an ignored bar line. `Drums(...)` uses `k` kick, `s` snare, `h` hat, `o` open hat.
Shorter tracks loop to the length of the longest one. `Render()` produces a stereo clip sized to loop seamlessly
(echo tails wrap around). Built-in voices: `Instrument.Lead`, `Bass`, `Pad`, `Pluck`, `Bell`; `Echo`,
`EchoDelayBeats` and `DrumVolume` tune the mix.

```csharp
Audio.AddMusic("theme", new Song(120).Track(Instrument.Lead, "C5 - E5 - G5 - E5 - | C5 - - - . . . .").Track(Instrument.Bass, "C3 . C3 . G2 . G2 .").Drums("k . h . s . h .").Render());
```

Source: [`samples/HelloEngine/Program.cs`](../../samples/HelloEngine/Program.cs)

Rendering happens on the CPU at load time; long songs add to start-up time, so render them once in `Load`, or pass
the `Song` itself (no `Render()`) to `Audio.AddMusic(name, song)`, which renders it in the background, songs side by
side, and hands it to the device the first time it plays, waiting then only for that song;
in browsers, which have no background threads, songs render a few milliseconds per frame, outside the game's updates,
in the order they were registered; a song asked to play before it is ready goes first and starts once rendered, so the
game never freezes for it. `CurrentMusic` names it from the moment it is asked for, so game rules that read it do not
depend on how fast the machine renders (in deterministic runs such as scripted ones, a song waited for is finished
before the next frame). A song with an invalid note or drum fails in `AddMusic`. Do not change a song after
registering it.

## The mixer

| Member | Behaviour |
| --- | --- |
| `AddSound(name, clip)`, `AddMusic(name, clip, loop)` | Register clips |
| `AddSoundFile(name, path)` | Decode a PCM WAV file as a sound |
| `AddMusicFile(name, path, loop)` | WAV is decoded in managed code; any other extension is streamed by the device |
| `Play(name, volume, pitch, pitchJitter, pan)` | Plays a sound; unknown names throw. The same sound is not retriggered twice in one step |
| `TryPlay(...)` | Same, but returns `false` for unregistered names |
| `PlayMusic(name, fadeSeconds = 0.4f)` | Switches track (see below); playing the current track again does nothing |
| `StopMusic(fadeSeconds = 0.4f)`, `PauseMusic(bool)` | Stop with a fade, pause or resume |
| `SfxVolume` (default 0.8), `MusicVolume` (0..1) | Global volumes |
| `HasSound`, `HasMusic`, `CurrentMusic` | Queries |

<!-- sdk-version: history -->
**Music changes are fade-out then fade-in, not an overlapping crossfade.** With a fade, the current track fades to
silence over `fadeSeconds`, then the new one starts and rises to full volume in about a quarter of a second. When
nothing is playing, the new track starts at once and rises in about a quarter of a second. With `fadeSeconds` 0 it starts
immediately; in 0.4.0-alpha.1 it still rises over that quarter of a second, and starts at full volume from
0.4.0-alpha.2. Only one music track plays at a time.

Neon Rift wraps the mixer so missing cues are recorded for its tests instead of crashing:

```csharp
public void Music(string name)
{
    if (name.Length == 0) Audio.StopMusic(0.4f);
    else if (Audio.HasMusic(name)) Audio.PlayMusic(name, 0.6f);
    else MissingSounds.Add(name);
}
```

Source: [`games/NeonRift/RiftGame.cs`](../../games/NeonRift/RiftGame.cs)

## One mixer on every platform

Every audio device of the engine is the same managed mixer,
`AudioEngine`, rendering into the platform's PCM output: on the SDL platform a thread keeps 60 ms queued in an SDL
audio stream; on raylib desktops a thread keeps a raylib audio stream full; in browsers the page plays through an
AudioWorklet that the game fills 100 ms ahead once per frame. So a sound plays the same everywhere: the same voice
limit, priorities, panning, resampling, looping and fades, and `CaptureAudioDevice` records exactly what a device would
play.

| `AudioEngine` member | Behaviour |
| --- | --- |
| `Play(clip, parameters)` | Plays a clip; returns a `VoiceHandle`, or `VoiceHandle.None` when the voice policy refused it |
| `Play(stream, parameters)` | Plays an `IAudioStream` (music decoded while it plays); several streams play side by side |
| `Stop(voice, fadeSeconds)`, `Set(voice, volume, pan)`, `Pause(voice, paused)` | Control a playing sound; a stale handle does nothing and returns `false` |
| `IsPlaying(voice)`, `ActiveVoices`, `RefusedSounds` | What plays now |
| `Bus(name)`, `AddBus(name, parent)`, `AudioBus.Volume` | Named buses under `master` (`sfx` and `music` exist); a bus is created under a parent that exists, so buses never form a cycle |
| `MaxVoices` (32 by default), `DelayFrames` | The voice limit; the delay between a step and the sounds it starts |

`PlayParameters` holds `Volume`, `Pitch`, `Pan`, `Loop`, `Priority` and `Bus` (`sfx` by default). When every voice is
busy, a new sound steals the oldest sound of the lowest priority, if that priority is not higher than its own;
otherwise the new sound is refused. `Game.Audio` plays music with the highest priority, so sound effects never steal
it. A device's mixer is `MixerAudioDevice.Engine`.

Sounds start at an exact output frame: the frame of the game update that played them plus a fixed delay at least as
long as the output's queue, so what is heard does not depend on how the platform asks for audio, and an offline
render (`OfflineAudioSink`, `CaptureAudioDevice`) gives the same samples on every machine. Music streams are decoded
ahead once per frame, outside the game's updates. `SdlAudioDevice.TryCreate(driver, latencyMilliseconds)` changes the
SDL queue (lower plays sounds sooner; higher survives longer pauses, such as long garbage collections).

## Sound and music files

| Format | Support |
| --- | --- |
| WAV | PCM 8, 16 or 24-bit, mono or stereo, decoded in managed code (`AudioClip.FromWav`); other WAV encodings throw `NotSupportedException` |
| OGG | Music only, through `AddMusicFile`; streamed and decoded in managed code (NVorbis) on every platform, ahead of playback and outside the simulation step |
| MP3 | Music only, and **raylib only** (decoded by raylib, whole, when it is loaded): the SDL device, the default on desktops and phones, throws `NotSupportedException` for it. Use OGG for music that plays everywhere |

Sound and music files can also come through `Game.Assets`, from any content source (a zip
archive too), shared and released by scopes like any asset:

```csharp
Audio.AddSound("jump", Assets.Sound("sfx/jump.wav"));
Audio.AddMusic("city", Assets.Music("music/city.ogg"));
```

Neon Rift registers every effect and track that way:

```csharp
public static void Register(AudioMixer audio, AssetCache assets)
{
    foreach (var name in Effects) audio.AddSound(name, assets.Sound($"sfx/{name}.wav"));
    foreach (var (name, file, loop) in Tracks) audio.AddMusic(name, assets.Music(file), loop);
}
```

Source: [`games/NeonRift/Art/Sounds.cs`](../../games/NeonRift/Art/Sounds.cs)

Where content files live and how they are located is covered in [Assets](assets.md).

## Devices and silence

Both platforms fall back to `SilentAudioDevice` when no audio device is available (they print `audio: ...,
continuing silently`), so a machine without sound still runs the game. Rapid repeats of a sound overlap (up to the voice limit, 32 by default) instead of cutting each other off.
`--mute` on desktop (or `RunOptions.Mute`) sets both volumes to zero and uses the silent device.
`HeadlessPlatform` exposes its `SilentAudioDevice`, whose `Played` list lets tests assert which cues fired.

- Runs nobody listens to are silent: by default `PlatformRequest.AudioOutput` (`--audio`) follows the loop's time
  mode. A real-time run opens a device (a hidden one too, unless it passes `--audio silent` or `--mute`); a
  deterministic-time run (`--script`, `--time deterministic`, headless) does not. `AudioOutput.Device`
  (`--audio device`) asks for one anyway; `AudioOutput.Silent` never opens one.
- `CaptureAudioDevice` records what would have been heard, one game step per update, so the same run always gives the
  same samples. Tests read its `Output` clip (waves, durations, silences, amplitudes) and `Played` list;
  `--audio-capture FILE` writes it as a WAV. It is the engine's own mixer rendering offline, with the same voice limit,
  priorities, panning, resampling, looping, pause and fades as every device, and it decodes OGG music too.
- `SdlAudioDevice.TryCreate("dummy")` runs SDL's whole audio path with no device or sound; `"disk"` writes SDL's mix
  to a file.

## In the browser

Browsers do not start audio until the user interacts with the page. The web host waits for a click or key before
starting the game, so the audio context is created inside that gesture; if the browser still holds audio
suspended, the page shows a "Sound is blocked" button and any key or click resumes it. Development packages with
`autostart=1` skip the gesture and stay silent. See [The browser host](../examples/embedding.md).

The game renders its sound once per frame, 100 ms ahead, and the page plays it through an
AudioWorklet. A frame longer than that is heard as a gap. A hidden page (another tab, a locked phone) suspends its
sound and resumes it when shown again.

## Limitations

- `Game.Audio` plays one music track at a time, with fade-out then fade-in; several streams at once, buses and
  per-sound handles are on the engine's mixer (`MixerAudioDevice.Engine`), not on `Game.Audio` yet.
- No positional audio beyond a per-play stereo `pan`, no effects on buses, no runtime pitch changes on a sound that is
  already playing.
- Compressed formats are supported for music only; sound effects must be WAV or synthesized. MP3 plays on raylib only.
- Sound starts 60 ms (SDL) to about 100 ms (browsers) after the update that played it: the queue that keeps pauses
  from being heard.
