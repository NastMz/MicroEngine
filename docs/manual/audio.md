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
| `SfxVolume` (default 0.8), `MusicVolume` (0..1) | Volumes of `Play` and of the music track |
| `HasSound`, `HasMusic`, `CurrentMusic` | Queries |
| `PlayVoice(name, parameters)` | Plays a sound as a voice and returns its `VoiceHandle` ([Sounds that keep playing](#sounds-that-keep-playing)) |
| `PlayMusicVoice(name, parameters)` | Plays registered music as one more voice, beside the music track |
| `Set(voice, volume, pan, pitch)`, `Pause(voice, paused)`, `Stop(voice, fadeSeconds)`, `IsPlaying(voice)` | Change, pause, stop or ask about a voice while it plays |
| `AddBus(name, parent)`, `SetBusVolume(name, volume)`, `BusVolume(name)`, `HasBus(name)` | Groups of sounds with one volume ([Buses](#buses)) |

<!-- sdk-version: history -->
**Music changes are fade-out then fade-in, not an overlapping crossfade.** With a fade, the current track fades to
silence over `fadeSeconds`, then the new one starts and rises to full volume in about a quarter of a second. When
nothing is playing, the new track starts at once and rises in about a quarter of a second. With `fadeSeconds` 0 it starts
immediately; in 0.4.0-alpha.1 it still rises over that quarter of a second, and starts at full volume from
0.4.0-alpha.2. `PlayMusic` plays one track at a time; music that plays beside it is a
[music voice](#music-beside-the-music).

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

## Sounds that keep playing

`Play` starts a sound and forgets it. A sound the game goes on changing (an engine that revs, a siren that passes, a
loop that stops when its source dies) is a **voice**: `PlayVoice` starts it and returns a `VoiceHandle`, and the handle
changes it while it plays.

<!-- code: samples/Examples/Examples/VoicesExample.cs#voice-start -->
```csharp
public override void Enter()
{
    if (!Audio.HasSound("hum"))
    {
        // One second of a 110 Hz tone: a whole number of cycles, so it loops without a click.
        var samples = new float[AudioClip.DefaultSampleRate];
        for (int i = 0; i < samples.Length; i++) samples[i] = MathF.Sin(i * MathF.Tau * 110 / samples.Length);
        Audio.AddSound("hum", new AudioClip(samples));
        Audio.AddBus("ambience");            // a group of sounds with one volume, beside "sfx" and "music"
    }
    // A voice: the sound plays until it is stopped, and the handle changes it while it plays.
    _hum = Audio.PlayVoice("hum", new PlayParameters { Loop = true, Volume = 0, Bus = "ambience" });
}

public override void Exit() => Audio.Stop(_hum, fadeSeconds: 0.2f);
```

`PlayParameters` says how it plays: `Volume`, `Pitch`, `Pan`, `Loop`, `Priority` and `Bus`. A voice plays exactly what
its parameters say: `SfxVolume` and the once-per-step rule of `Play` do not apply to it, so two voices of the same
sound overlap.

`Set` changes the volume, the stereo position or the pitch from the current update on; a value left out keeps what
it was. Called every update it makes a sound follow something that moves. The engine has no listener or emitters:
where a sound is heard is a `pan` and a `volume` the game computes from its own positions, in a few lines.

<!-- code: samples/Examples/Examples/VoicesExample.cs#voice-follow -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    _x += _speed * dt;
    if (_x < 20 || _x > Width - 20) _speed = -_speed;

    // The listener stands in the middle of the screen. Where the source is gives the stereo position; how far it
    // is, the volume; whether it comes or goes, a slightly higher or lower pitch.
    float pan = _x / Width;                                  // 0 left .. 1 right
    float distance = MathF.Abs(pan - 0.5f) * 2;              // 0 at the listener .. 1 at an edge
    bool approaching = (pan < 0.5f) == (_speed > 0);
    Audio.Set(_hum, volume: Loudest * (1 - 0.8f * distance), pan: pan, pitch: approaching ? 1.06f : 0.94f);

    // The bus scales every sound played through it: what a settings screen changes.
    float change = Input.Axis("down", "up") * dt;
    if (change != 0) Audio.SetBusVolume("ambience", Math.Clamp(Audio.BusVolume("ambience") + change, 0, 1));
}
```

| Call | Behaviour |
| --- | --- |
| `Set(voice, volume, pan, pitch)` | Volume 0..1, pan 0 (left) to 1 (right), pitch as a speed factor (0.01..100; 2 is an octave up and half the length). Changes made in one update take effect together |
| `Pause(voice, paused)` | A paused sound keeps its voice and goes on from where it was |
| `Stop(voice, fadeSeconds)` | Stops at once, or fades out over `fadeSeconds` |
| `IsPlaying(voice)` | False once the sound ended, was stopped or lost its voice |

A handle is safe to keep: once its sound ended, was stopped or was stolen by the voice limit, every call with it does
nothing and returns `false`. `PlayVoice` returns `VoiceHandle.None` when every voice is busy with sounds of a higher
`Priority`, and on a device without sound (headless runs, `--mute`, a machine without audio hardware); calls with
`None` do nothing too, so game code needs no special case. A headless test still sees the sound's name in
`SilentAudioDevice.Played`.

### Buses

Every sound plays through a bus, and a bus has a volume that scales everything on it. Three exist: `master`, and under
it `sfx` (where sounds play unless their parameters name another) and `music` (the music track). `AddBus(name, parent)`
adds one under a bus that exists (`master` by default), up to 32 in all: `ambience`, `voices`, `ui`.
`SetBusVolume(name, volume)` takes 0..4 and changes what is already playing, from the current update on;
`BusVolume(name)` reads it back, also on a device without sound. A settings screen with a slider per group sets bus
volumes: `sfx` reaches the sounds of `Play` too (on top of `SfxVolume`), `music` the music track (on top of
`MusicVolume`), `master` everything.

### Music beside the music

`PlayMusicVoice(name, parameters)` plays registered music as one more voice without replacing the track `PlayMusic`
plays: rain under the theme, a second layer of the same piece that fades in when the fight starts. It returns a
`VoiceHandle` like any voice, so `Set` and `Stop(voice, fadeSeconds)` fade it. Name the `music` bus in its parameters
to keep it with the music; it loops when the parameters say so. `MusicVolume`, `CurrentMusic` and the fades of
`PlayMusic` are those of the music track only. Streamed music (OGG) keeps its own pitch.

## One mixer on every platform

Every audio device of the engine is the same managed mixer,
`AudioEngine`, rendering into the platform's PCM output: on the SDL platform a thread keeps 60 ms queued in an SDL
audio stream; on raylib desktops a thread keeps a raylib audio stream full; in browsers the page plays through an
AudioWorklet that the game fills 100 ms ahead once per frame. So a sound plays the same everywhere: the same voice
limit, priorities, panning, resampling, looping and fades, and `CaptureAudioDevice` records exactly what a device would
play.

| `AudioEngine` member | Behaviour |
| --- | --- |
| `Play(clip, parameters)` | Plays a clip; returns a `VoiceHandle`, or `VoiceHandle.None` when the voice policy refused it (or the output stopped taking audio, see below) |
| `Play(stream, parameters)` | Plays an `IAudioStream` (music decoded while it plays); several streams play side by side |
| `Stop(voice, fadeSeconds)`, `Set(voice, volume, pan)`, `SetPitch(voice, pitch)`, `Pause(voice, paused)` | Control a playing sound; a stale handle does nothing and returns `false` |
| `IsPlaying(voice)`, `ActiveVoices`, `RefusedSounds`, `DroppedCommands`, `SkippedFrames` | What plays now; calls made while the output took no audio; frames the output did not play |
| `Bus(name)`, `AddBus(name, parent)`, `AudioBus.Volume` | Named buses under `master` (`sfx` and `music` exist); a bus is created under a parent that exists, so buses never form a cycle |
| `MaxVoices` (32 by default), `DelayFrames` | The voice limit; the delay between a step and the sounds it starts |

`PlayParameters` holds `Volume`, `Pitch`, `Pan`, `Loop`, `Priority` and `Bus` (`sfx` by default). When every voice is
busy, a new sound steals the oldest sound of the lowest priority, if that priority is not higher than its own;
otherwise the new sound is refused. `Game.Audio` plays music with the highest priority, so sound effects never steal
it. A device's mixer is `MixerAudioDevice.Engine`; `Game.Audio` reaches its voices and buses by name
([Sounds that keep playing](#sounds-that-keep-playing)), so a game needs the engine itself only to play a clip it did
not register or an `IAudioStream` of its own.

Sounds start at an exact output frame: the frame of the game update that played them plus a fixed delay at least as
long as the output's queue, so what is heard does not depend on how the platform asks for audio, and an offline
render (`OfflineAudioSink`, `CaptureAudioDevice`) gives the same samples on every machine. Sounds played in
consecutive updates start exactly one update apart, also when a frame runs two updates at once. In real time the
updates' frames follow what the device is playing, so a sound is heard one output queue after its update; an update
that runs late starts its sounds in the next audio to be rendered, and after a slow frame (whose catch-up updates
run ahead of the device) the timing comes back within about a second, a millisecond per frame. Music streams are
decoded ahead once per frame, outside the game's updates. `SdlAudioDevice.TryCreate(driver, latencyMilliseconds)`
changes the SDL queue (lower plays sounds sooner; higher survives longer pauses, such as long garbage collections).

A value that is not a number (`float.NaN`, the result of dividing zero by zero in a distance or a ratio) never reaches
the sound: `Play` uses the default instead (full volume, original pitch, centred), and `Set`, `SetPitch`, bus volumes,
`MusicVolume` and `SfxVolume` ignore it and keep their value.

When the output stops taking audio while the game goes on (a page whose sound the browser holds suspended until a key
or click), nothing the game asks for is lost: stops, pauses, and the latest volume, pan and pitch of each sound and
the volume of each bus apply when the output plays again. Once the calls waiting for it fill the mixer's queue
(4096), a new sound is refused (`VoiceHandle.None`) until then; `AudioEngine.DroppedCommands` counts the calls that
had to wait or were refused.

### Muting, pausing and an output that stops

Three things silence a sound, and they are not the same:

| | Who asks | The sound meanwhile | Afterwards |
| --- | --- | --- | --- |
| **Muting**: a volume of 0 (`Set`, `SfxVolume`, `MusicVolume`, a bus's `Volume`) | The game | Goes on, unheard | Is heard from where it has got to |
| **Pausing**: `Pause(voice, true)`, `PauseMusic` | The game | Holds its position and keeps its voice | Resumes at the frame it was paused at |
| **An output that stops**: the device or the page takes no audio | Nobody | Goes on with the game, unheard | The output plays what the game is at |

The output stops when a browser holds the page's sound suspended while the page is visible, or when a device goes
away. The mixer takes it as stopped once it has rendered nothing for a quarter of a second of updates, and from then
on skips the frames that were not played (`AudioEngine.SkippedFrames` counts them), so the game's audio time never
waits. When the output plays again:

- a sound that ended meanwhile is not heard, and its handle has ended (`IsPlaying` is false, `Stop` returns false):
  the shots and hits of a silent stretch are never played late or together;
- a sound still within its length goes on from where the game is, not from its start; a loop and music from a clip
  (a `Song`, a WAV) are where they would be had the output played;
- a fade-out asked for meanwhile has run its time; a sound the game paused is still paused, at the same position;
- streamed music (OGG, MP3), which is not decoded while nobody hears it, goes on from where it stopped;
- a sound played from then on starts in the next audio to be rendered, as always.

A gap shorter than a quarter of a second is not a stop: nothing is skipped, and the sounds played during it are heard
after it, late by its length. Deterministic runs and recordings never skip.

A game that goes to the background is a different case. On a phone and in a hidden browser page the game itself is
suspended: no update runs, the music track pauses, and everything resumes where it was. On a desktop a game whose
window is minimized or behind another keeps running and keeps sounding; a game that wants silence there lowers a
bus in its handler of `Lifecycle.FocusChanged`.

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

- `PlayMusic` changes track with fade-out then fade-in, not an overlapping crossfade; a crossfade is two music voices
  whose volumes the game sets.
- No listener or emitters: a sound's position is the `pan` and `volume` the game sets. No effects on buses (reverb,
  filters), and a bus has a volume but no pitch.
- Volume, pan and pitch change in one step at the update that sets them, not as a ramp: large jumps on a loud sound
  can click, so change them a little every update or fade with `Stop`.
- Streamed music keeps its own pitch.
- Compressed formats are supported for music only; sound effects must be WAV or synthesized. MP3 plays on raylib only.
- Sound is not instant: it is heard after the queue that keeps pauses from being heard, 60 ms on the SDL platform
  and 100 ms in browsers. On the SDL platform a sound reaches the device about 65 ms after the update that played it
  at 60 or more frames per second and about 75 ms at 30, before the device's own buffer; a slow frame adds up to its
  catch-up updates to that for about a second.
- A sound or a bus keeps the changes of eight updates waiting for their points in the sound. That is more than a
  slow frame leaves behind; with an `AudioEngineSettings.DelayFrames` longer than eight updates, the oldest waiting
  change takes effect together with the next one, later than its own point and never earlier.
- There is no setting for sound in the background: a suspended game is silent, a desktop game behind another window
  sounds unless the game lowers its buses.
