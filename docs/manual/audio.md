# Audio

MicroEngine audio has two halves. The core synthesizes sound effects (`Synth`) and step-sequenced music (`Song`)
into `AudioClip`s, and decodes PCM WAV files. The game-facing `AudioMixer` (`Game.Audio`) keeps a bank of named
sounds and music, plays them by name, and fades music in and out. Playback itself is done by the backend's
`IAudioDevice`, which also streams compressed music: OGG on every platform, MP3 on raylib only.

## Pieces

| Type | Role |
| --- | --- |
| `AudioClip` | Interleaved float samples (-1..1), 1 or 2 channels; `FromWav(bytes)`, `ToWav()`, `Duration`, `Peak` |
| `SfxParams` + `Synth.Render` | Retro sound effects in the spirit of sfxr |
| `Synth.Layer`, `Synth.NoteFrequency` | Mix clips at time offsets; note name to Hz (`"C5"`, `"F#3"`) |
| `Instrument`, `Song` | Tracker-style voices and a step sequencer rendered to a looping stereo clip |
| `AudioMixer` | Named sound bank, music with fades, volumes |
| `IAudioDevice` | The platform's device: SDL audio streams on the SDL platform (the default on desktops and phones), raylib's audio on the raylib platform (desktops and browsers), `SilentAudioDevice` headless or without hardware |

Source: [`src/MicroEngine/Audio/`](../../src/MicroEngine/Audio/)

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

Rendering happens on the CPU at load time; long songs add to start-up time, so render them once in `Load`.

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

**Music changes are fade-out then fade-in, not an overlapping crossfade.** With a fade, the current track fades to
silence over `fadeSeconds`, then the new one starts and rises to full volume in about a quarter of a second. With
`fadeSeconds` 0, or when nothing is playing, the new track starts at once. Only one music track plays at a time.

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

## Sound and music files

```csharp
public static void Register(AudioMixer audio, ContentFolder content)
{
    foreach (var name in Effects) audio.AddSoundFile(name, content.PathOf($"sfx/{name}.wav"));
    foreach (var (name, file, loop) in Tracks) audio.AddMusicFile(name, content.PathOf(file), loop);
}
```

Source: [`games/NeonRift/Art/Sounds.cs`](../../games/NeonRift/Art/Sounds.cs)

| Format | Support |
| --- | --- |
| WAV | PCM 8, 16 or 24-bit, mono or stereo, decoded in managed code (`AudioClip.FromWav`); other WAV encodings throw `NotSupportedException` |
| OGG | Music only, through `AddMusicFile`; streamed by the SDL device (decoded in managed code with NVorbis) and by raylib |
| MP3 | Music only, and **raylib only**: the SDL device, the default on desktops and phones, throws `NotSupportedException` for it. Use OGG for music that plays everywhere |

Where content files live and how they are located is covered in [Assets](assets.md).

## Devices and silence

Both platforms fall back to `SilentAudioDevice` when no audio device is available (they print `audio: ...,
continuing silently`), so a machine without sound still runs the game. Each sound keeps several voices so rapid
repeats overlap instead of cutting each other off. `--mute` on desktop (or `RunOptions.Mute`) sets both volumes to zero and uses the silent device.
`HeadlessPlatform` exposes its `SilentAudioDevice`, whose `Played` list lets tests assert which cues fired.

## In the browser

Browsers do not start audio until the user interacts with the page. The web host waits for a click or key before
starting the game, so the audio context is created inside that gesture; if the browser still holds audio
suspended, the page shows a "Sound is blocked" button and any key or click resumes it. Development packages with
`autostart=1` skip the gesture and stay silent. See [The browser host](../examples/embedding.md).

## Limitations

- One music track at a time; no simultaneous layers or true crossfades.
- No positional audio beyond a per-play stereo `pan`, no effects buses, no runtime pitch or volume changes on a
  sound that is already playing.
- Compressed formats are supported for music only; sound effects must be WAV or synthesized.
