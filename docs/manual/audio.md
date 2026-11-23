# Audio

MicroEngine audio has two halves. The core synthesizes sound effects (`Synth`) and step-sequenced music (`Song`)
into `AudioClip`s, and decodes WAV, OGG and MP3 files. The game-facing `AudioMixer` (`Game.Audio`) keeps a bank of named
sounds and music, plays them by name, fades and crossfades music, and groups sounds in buses that carry effects (a
reverb, a filter, music that drops under a voice line). Playback itself is done by the backend's
`IAudioDevice`, which also streams music while it plays. Every
device is the engine's one mixer playing through the platform's output
([One mixer on every platform](#one-mixer-on-every-platform)).

## Pieces

| Type | Role |
| --- | --- |
| `AudioClip` | Interleaved float samples (-1..1), 1 or 2 channels; `FromWav(bytes)`, `ToWav()`, `Duration`, `Peak` |
| `SfxParams` + `Synth.Render` | Retro sound effects in the spirit of sfxr |
| `Synth.Layer`, `Synth.NoteFrequency` | Mix clips at time offsets; note name to Hz (`"C5"`, `"F#3"`) |
| `Instrument`, `Song` | Tracker-style voices and a step sequencer rendered to a looping stereo clip |
| `AudioMixer` | Named sound bank, music with fades and crossfades, voices, buses |
| `AudioBus` | A group of sounds: one volume (`Volume`, `FadeTo`) and one chain of effects (`Add`, `Insert`, `Remove`) |
| `AudioEffect` | A processor on a bus. Built in (`MicroEngine.Audio.Effects`): `LowPass`, `HighPass`, `Compressor`, `Reverb`, `Delay`, `Limiter`; a game writes its own |
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
| `AddSoundFile(name, path)` | Decode a WAV, OGG or MP3 file as a sound |
| `AddMusicFile(name, path, loop, mode)` | A WAV, OGG or MP3 file as music, decoded while it plays or whole ([Sound and music files](#sound-and-music-files)) |
| `RemoveSound(name)`, `RemoveMusic(name)` | Unregister, releasing what the mixer holds |
| `Play(clip, ...)`, `PlayVoice(clip, parameters)` | Play a clip that is not registered (one held by an asset scope) |
| `Play(name, volume, pitch, pitchJitter, pan)` | Plays a sound; unknown names throw. The same sound is not retriggered twice in one step |
| `TryPlay(...)` | Same, but returns `false` for unregistered names |
| `PlayMusic(name, fadeSeconds = 0.4f)` | Switches track (see below); playing the current track again does nothing |
| `StopMusic(fadeSeconds = 0.4f)`, `PauseMusic(bool)` | Stop with a fade, pause or resume |
| `SfxVolume` (default 0.8), `MusicVolume` (0..1) | Volumes of `Play` and of the music track |
| `HasSound`, `HasMusic`, `CurrentMusic` | Queries |
| `PlayVoice(name, parameters)` | Plays a sound as a voice and returns its `VoiceHandle` ([Sounds that keep playing](#sounds-that-keep-playing)) |
| `PlayMusicVoice(name, parameters)` | Plays registered music as one more voice, beside the music track |
| `Set(voice, volume, pan, pitch)`, `Pause(voice, paused)`, `Stop(voice, fadeSeconds)`, `IsPlaying(voice)` | Change, pause, stop or ask about a voice while it plays |
| `AddBus(name, parent)`, `SetBusVolume(name, volume)`, `BusVolume(name)`, `HasBus(name)` | Groups of sounds with one volume ([Buses and effects](#buses-and-effects)) |

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

### Music beside the music

`PlayMusicVoice(name, parameters)` plays registered music as one more voice without replacing the track `PlayMusic`
plays: rain under the theme, a second layer of the same piece that fades in when the fight starts. It returns a
`VoiceHandle` like any voice, so `Set` and `Stop(voice, fadeSeconds)` fade it. Name the `music` bus in its parameters
to keep it with the music; it loops when the parameters say so. `MusicVolume`, `CurrentMusic` and the fades of
`PlayMusic` are those of the music track only. Streamed music keeps its own pitch.

## Buses and effects

Every sound plays through a bus. A bus mixes its sounds and the buses under it, passes the mix through its effects and
scales it by its volume. Three exist: `master`, and under it `sfx` (where sounds play unless their parameters name
another) and `music` (the music track). `AddBus(name, parent)` adds one under a bus that exists (`master` by default),
up to 32 in all: `ambience`, `voice`, `ui`. `Audio.Bus(name)` is the bus itself.

| On a bus | What it does |
| --- | --- |
| `Volume`, `Audio.SetBusVolume(name, volume)` | Scales everything on the bus, 0..4, from the current update on; `Audio.BusVolume(name)` reads it back |
| `FadeTo(volume, seconds)`, `Audio.SetBusVolume(name, volume, fadeSeconds)` | Moves the volume there over a time, without a step in the sound |
| `Add(effect)`, `Insert(index, effect)` | Puts an effect in the chain; `Add` returns the bus, to add another |
| `Remove(effect)`, `ClearEffects()`, `Effects` | Takes effects out; lists them in order |

A settings screen with a slider per group sets bus volumes: `sfx` reaches the sounds of `Play` too (on top of
`SfxVolume`), `music` the music track (on top of `MusicVolume`), `master` everything.

### Effects

An effect is an object the game keeps: it is added to a bus once and its properties are changed while it plays. This
scene puts a room around every sound effect, and two effects on the music. Source:
[`MixExample.cs`](../../samples/Examples/Examples/MixExample.cs).

<!-- code: samples/Examples/Examples/MixExample.cs#mix-setup -->
```csharp
public override void Enter()
{
    if (!Audio.HasBus("voice"))
    {
        Audio.AddBus("voice");                                 // the bus of spoken lines, beside "sfx" and "music"
        Audio.AddMusic("mix-loop", MixSounds.Loop());
        Audio.AddSound("mix-line", MixSounds.Line());
        Audio.AddSound("mix-chime", MixSounds.Chime());
    }
    Audio.Bus("sfx").Add(_room);                               // every sound effect is heard in a room
    Audio.Bus("music").Add(_muffle).Add(_duck);                // the music: muffled on demand, and under the voice
    _music = Audio.PlayMusicVoice("mix-loop", new PlayParameters { Loop = true, Bus = "music", Volume = 0.6f, FadeIn = 0.5f });
}

public override void Exit()
{
    Audio.Stop(_music, fadeSeconds: 0.3f);
    Audio.Bus("sfx").Remove(_room);                            // the buses go back to what they were
    Audio.Bus("music").Remove(_muffle);
    Audio.Bus("music").Remove(_duck);
    Audio.Bus("sfx").Volume = 1;
}
```

| Effect | Properties | Use |
| --- | --- | --- |
| `LowPass` | `Cutoff` (Hz), `Resonance` | Muffled: music behind a pause menu, under water, behind a wall |
| `HighPass` | `Cutoff` (Hz), `Resonance` | Thin: a radio, a telephone; a rumble removed |
| `Compressor` | `Threshold` and `Makeup` (dB), `Ratio`, `Attack`, `Release`, `Range`, `Sidechain` | Evens out a bus, or turns it down while another bus sounds (`Compressor.Duck`) |
| `Reverb` | `RoomSize`, `Damping`, `Wet`, `Dry`, `Width` | A space around the sounds, from a small room to a hall |
| `Delay` | `Time` (up to 2 s), `Feedback`, `Wet`, `Dry` | An echo |
| `Limiter` | `Threshold`, `Release` | Keeps a bus within a ceiling |

Effects run in the order of the chain, before the bus's volume. A property set on an effect takes effect at the
update that set it and glides to its new value over a fiftieth of a second, so a filter can be swept every update
without clicks. `effect.Bypass = true` lets the sound through unchanged without taking the effect out; an effect taken
out of a chain (`Remove`) stops at once, tail included. Values are clamped to each property's range, and a value that is
not a number is ignored. All of it works on a device without sound: the buses keep their effects and volumes, so the
same code runs headless.

### A muffled pause

A low-pass filter added once and switched in while the game is paused, and the sound effects faded down:

<!-- code: samples/Examples/Examples/MixExample.cs#mix-pause -->
```csharp
private void SetPaused(bool paused)
{
    Paused = paused;
    _muffle.Bypass = !paused;                                  // the filter is switched in, not added again
    Audio.Bus("sfx").FadeTo(paused ? 0.3f : 1f, seconds: 0.3f); // and the sound effects fade down, without a step
}
```

### Ducking: music under a voice line

`Compressor.Duck(bus)` makes a compressor that listens to another bus: while that bus sounds, the bus it is on drops
(12 dB unless told otherwise), and it comes back when the other falls silent. It is added once, in the scene's `Enter`
above (`Audio.Bus("music").Add(Compressor.Duck("voice"))`); from then on a line played on the `voice` bus is all it
takes:

<!-- code: samples/Examples/Examples/MixExample.cs#mix-duck -->
```csharp
// A voice line on the "voice" bus: the compressor on the music bus listens to that bus and turns the music
// down while the line plays. Nothing else to do here.
if (Input.Pressed("action") && !Audio.IsPlaying(_line))
    _line = Audio.PlayVoice("mix-line", new PlayParameters { Bus = "voice", Volume = 0.8f });
```

Any `Compressor` can listen to another bus through its `Sidechain` property. The bus it listens to cannot be its own
bus or one its bus plays into (`music` cannot listen to `master`), and two buses cannot listen to each other: `Add`
and the property throw `ArgumentException` for those, and for a bus that does not exist.

### The limiter after master

When many loud sounds play at once the mix can go over full scale, which a sound card clips. The mixer has a
`Limiter` after `master` (`Audio.Limiter`), switched on in every game: it turns the mix down the moment it would go
over and lets go over a tenth of a second, with no delay. While the mix stays within full scale it does nothing at
all: every sample leaves the mixer exactly as it was mixed. `Audio.Limiter.Bypass = true` switches it off;
`Threshold` lowers the ceiling.

### Effects of your own

An effect is a class that derives from `AudioEffect` and processes a block of samples in place; a game or a package
adds it to a bus like the built-in ones. See [an audio effect](extending.md#an-audio-effect).

The game's thread sets an effect's properties and the audio thread runs its `Process`; the values cross between them
through `SetParameter` and `OnParameter`. An effect that sets one of its own properties inside `Process` is taken out
of the mix and reported by `Faulted`, like one that throws.

## Fades and crossfades

Volume, pan and pitch can move to a new value over a time instead of jumping to it. The mixer moves them a little at
every output sample, from the update that asked, and lands exactly on the value.

| Call | What moves |
| --- | --- |
| `PlayVoice(name, new PlayParameters { FadeIn = 0.5f })` | The sound rises from silence to its volume |
| `Audio.Set(voice, volume:, pan:, pitch:, seconds: 0.3f)` | A voice's volume, pan and pitch move there over the time |
| `Audio.Stop(voice, fadeSeconds: 0.3f)` | The voice falls to silence, then ends |
| `Audio.Bus("sfx").FadeTo(0.3f, seconds: 0.3f)` | A bus's volume |
| `Audio.PlayMusic("battle", crossfade: 1.5f)` | The new track rises while the current one falls, both over the time |

A sound that crosses the screen once is one call after it starts:

<!-- code: samples/Examples/Examples/MixExample.cs#mix-chime -->
```csharp
_chimeAge += dt;
_chimeIn -= dt;
if (_chimeIn <= 0)
{
    (_chimeIn, _chimeAge) = (2f, 0);
    // It starts on the left and the mixer moves it to the right over its length, frame by frame.
    var chime = Audio.PlayVoice("mix-chime", new PlayParameters { Pan = 0, Volume = 0.7f });
    Audio.Set(chime, pan: 1, seconds: ChimeSeconds);
}
```

Without `seconds`, `Set` changes the values at once, which is what a value set again every update needs (a sound that
follows something that moves, as in [Sounds that keep playing](#sounds-that-keep-playing)). A change without a time
ends a ramp that was running; a ramp stands still while its sound is paused.

`PlayMusic(name)` keeps its behaviour: the current track fades out, then the new one starts. With `crossfade`, the two
overlap: the new track is `CurrentMusic` at once and rises in a straight line while the old one falls. On a device
without sound, and for a `Song` that is still being rendered in a browser, the music switches without the overlap.

## Time: position, seeking and loops

A game can read where a sound is, move it, start it from the middle or a moment later, and name the part of it that
repeats.

| Call | What it does |
| --- | --- |
| `Audio.MusicPosition`, `Audio.Position(voice)` | Seconds from the sound's beginning, at this update. A loop comes back to the start of its region |
| `Audio.MusicHeardPosition`, `Audio.HeardPosition(voice)` | The same, less `Audio.OutputLatency`: what the player hears at this update |
| `Audio.SeekMusic(seconds)`, `Audio.Seek(voice, seconds)` | Moves the sound there from this update on, exactly at the update's point in the output |
| `Audio.MusicDuration`, `Audio.Duration(voice)` | The sound's length in seconds |
| `new PlayParameters { StartAt = 12.5 }` | The sound starts that far into itself |
| `new PlayParameters { Delay = 0.25 }` | The sound starts that long after this update, exact to the output sample |
| `new PlayParameters { Loop = true, LoopRegion = ... }` | The part that repeats, for this play |

### An intro, then a loop

Music often has an opening that must play once and a body that repeats. That is one piece of music with a loop
region: it plays from its beginning to the region's end, then goes back to the region's start every time.

<!-- code: samples/Examples/Examples/MusicTimeExample.cs#time-setup -->
```csharp
public override void Enter()
{
    // The clip names the part that repeats; the music plays its intro once and then stays in that part.
    if (!Audio.HasMusic("time-track"))
        Audio.AddMusic("time-track", TimeSounds.Track(), loop: true);
    Audio.PlayMusic("time-track", fadeSeconds: 0);
}

public override void Exit() => Audio.StopMusic(0.2f);
```

The region comes from the sound itself:

- a WAV file's sampler chunk (`smpl`), which audio editors write when a loop is marked in them;
- an OGG file's comments `LOOPSTART` with `LOOPLENGTH` or `LOOPEND`, in samples, as game audio tools write them;
- `AudioClip.LoopRegion` for a clip made in code: `new AudioClip(samples) { LoopRegion = LoopRegion.FromSeconds(1, 0, rate) }`
  (an end of 0 is the sound's end).

`PlayParameters.LoopRegion` replaces it for one voice. A region is in samples of the sound, so it is exact whatever
the output's rate; one that makes no sense (it starts past the end, or is empty) is the whole sound. MP3 files carry
no loop points and are padded at both ends by their encoder: music that must loop without a gap is OGG or WAV.

### Following the music

Sound reaches the speaker later than the update that plays it (see `Audio.OutputLatency`). `MusicPosition` is where
the music is in the mixer; `MusicHeardPosition` is where it is in the player's ears, and is the one to compare with
what is on screen or with a key press:

<!-- code: samples/Examples/Examples/MusicTimeExample.cs#time-follow -->
```csharp
// What the player hears at this update: the music's position less the time sound takes to reach the speaker.
double heard = Audio.MusicHeardPosition;
if (heard < IntroSeconds) (Beat, Phase) = (-1, 0);
else
{
    double beats = (heard - IntroSeconds) / BeatSeconds;
    Beat = (int)beats;
    Phase = (float)(beats - Beat);
}
```

Both follow the game's updates, not a clock of their own: an update later, the position is one update further (at
the sound's pitch), and a scripted run reads the same values every time. A heard position is negative while a
sound's first sample is still on its way, by the time left; so is the position of a sound waiting for its `Delay`.
On a device without sound the music's position is the time it has played, so a game that follows its music behaves
the same in a headless run until the music loops.

Seeking is one call. A paused sound stays paused at the new place; a sound that does not loop ends as much sooner or
later; a time past the end ends it, and past a loop's region lands inside the region:

<!-- code: samples/Examples/Examples/MusicTimeExample.cs#time-seek -->
```csharp
if (Input.Pressed("action")) Audio.SeekMusic(0);                            // the intro again
if (Input.Pressed("right")) Audio.SeekMusic(Audio.MusicPosition + BeatSeconds);   // one beat ahead
```

To bring the music back where a saved game left it, save `MusicPosition` and call `SeekMusic` after `PlayMusic`, or
play it as a voice with `StartAt`.


## Sounds with a place

A scene has a listener and emitters, in the units of its world (`Scene.Sounds`). After every update each emitter's
volume follows its distance to the listener and its pan the side it is on; an emitter that follows an entity goes
where the entity goes. Source:
[`SpatialSoundExample.cs`](../../samples/Examples/Examples/SpatialSoundExample.cs).

<!-- code: samples/Examples/Examples/SpatialSoundExample.cs#spatial-setup -->
```csharp
public override void Enter()
{
    if (!Audio.HasSound("spatial-hum"))
    {
        Audio.AddSound("spatial-hum", SpatialSounds.Tone(SpatialSounds.Hum, 1f));
        Audio.AddSound("spatial-drone", SpatialSounds.Tone(SpatialSounds.DroneTone, 1f));
        Audio.AddSound("spatial-knock", SpatialSounds.Knock());
    }
    _listener = Objects.Create();
    Objects.Local(_listener).Position2D = new Vector2(60, Ground);
    _generator = Objects.Create();
    Objects.Local(_generator).Position2D = new Vector2(250, Ground);
    _drone = Objects.Create();

    Sounds.Follow(_listener);                 // the listener is this entity
    Sounds.MaxDistance = 220;                 // silent from here on, in the world's units
    Sounds.PanDistance = 160;                 // half the view: a sound at the screen's edge is all on that side
    Generator = Sounds.Emit("spatial-hum", _generator, loop: true, volume: 0.6f);   // this entity makes this sound
    Drone = Sounds.Emit("spatial-drone", _drone, loop: true, volume: 0.5f);
    Drone.Rolloff = SoundRolloff.Smooth;
}
```

<!-- code: samples/Examples/Examples/SpatialSoundExample.cs#spatial-move -->
```csharp
// Entities move as they always do; their sounds follow, and stop when the scene leaves.
ref var listener = ref Objects.Local(_listener);
listener.Position2D = new Vector2(Math.Clamp(listener.Position2D.X + Input.Axis("left", "right") * Speed * dt, 10, 310), Ground);
_droneX = _droneX > 360 ? -40 : _droneX + 70 * dt;
Objects.Local(_drone).Position2D = new Vector2(_droneX, 50);

// A sound at a place, once: nothing to keep, it is gone when it ends.
if (Input.Pressed("action")) Sounds.Emit("spatial-knock", listener.Position2D + new Vector2(120, 0));
```

| Member | What it does |
| --- | --- |
| `Sounds.Emit(sound, entity, loop, volume)` | The entity is the source of a registered sound; the emitter stops when the entity is destroyed |
| `Sounds.Emit(sound, position, loop, volume)` | A sound at a place; move it through the emitter's `Position` |
| `Sounds.Emit(clip, ...)` | The same for a clip that is not registered (one held by an asset scope) |
| `Sounds.Follow(entity)`, `Sounds.Listener = point` | Who listens. With neither, the centre of the scene's `WorldCamera`; without a camera, the world's origin |
| `Sounds.MinDistance`, `MaxDistance`, `Rolloff`, `Bus` | What new emitters start with (16, 320, `Linear`, "sfx") |
| `Sounds.PanDistance` | The horizontal distance at which a sound is all on one side (by default each emitter's `MaxDistance`) |
| `Sounds.Doppler`, `Sounds.SpeedOfSound` | A pitch shift by how fast a sound comes closer or goes away (off by default) |
| `Sounds.StopAll(fade)`, `emitter.Stop(fade)` | Stop them |

An emitter (`SoundEmitter`) can be changed while it plays: `Position`, `Volume`, `Pitch`, `MinDistance`,
`MaxDistance`, `Rolloff`, `Curve`, and `Bus` and `Priority` for the next time its sound starts. It reports what the
last update gave it (`Gain`, `Pan`, `PlayedPitch`), whether it is still in the scene (`IsActive`) and whether a
voice plays it (`IsAudible`).

**Distance.** Within `MinDistance` a sound plays at its volume; from `MaxDistance` on it is silent; between the two
`Rolloff` shapes the fall:

| `SoundRolloff` | Shape | Use it for |
| --- | --- | --- |
| `Linear` | a straight line | small levels, where "half way is half as loud" is easy to tune |
| `Inverse` | one over the distance, brought to silence at the maximum: fast near the source, slow far away | open spaces, sources heard from far |
| `Smooth` | an S: nearly full for a while, then a fall, then a gentle end | ambiences that should fill a room and end at its door |

`emitter.Curve = t => ...` replaces it with a curve of the game's own: `t` is 0 at the minimum distance and 1 at the
maximum, the result is the gain.

**Voices.** An emitter out of range holds no voice, so a level can have hundreds of emitters and only those within
earshot count against the 32 voices. A looping emitter that comes into range starts where it would be had it played
all along. A sound that plays once and is out of range when it happens is not heard at all. When more loops are in
range than there are voices, the ones that came first keep theirs and the others wait for one to be free; sounds
that play once take voices by the usual priority rule.

**Lifetime.** Emitters stop when their entity is destroyed and when the scene leaves the stack, fading out over
`Sounds.StopFade` (a tenth of a second). A sound that plays once removes its emitter when it ends.

**Scripted runs.** Emitters are sound, not state: nothing of them is in the scene's state hash, and a run on a
silent device computes the same gains and pans and plays nothing. Do not decide anything in the game by `IsAudible`,
which is false on a silent device.

**Doppler.** With `Sounds.Doppler = 1` a sound's pitch rises as its distance to the listener shrinks and falls as it
grows, by the ratio of that speed to `Sounds.SpeedOfSound` (world units per second, 3400 by default), kept between
half and twice the pitch. It needs sounds that can change pitch: clips, not streamed music.

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
away. The mixer takes it as stopped once it has rendered nothing for a quarter of a second of updates (or twice the length
of the largest block the output asks for, if that is longer), and from then on skips the frames that were not played (`AudioEngine.SkippedFrames` counts them), so the game's audio time never
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

Every format works for sound effects and for music, and is decoded by the same managed code on every platform, so a
file sounds the same everywhere.

| Format | Support |
| --- | --- |
| WAV | PCM 8, 16 or 24-bit, mono or stereo; other WAV encodings throw `NotSupportedException`. Always decoded whole. Loop points from its sampler chunk |
| OGG | Vorbis. Loop points from its `LOOPSTART` and `LOOPLENGTH` (or `LOOPEND`) comments. The format for music that loops |
| MP3 | MPEG layers 1 to 3. No loop points, and its encoder pads both ends with a few milliseconds of silence, so a loop has a gap |
| your own | `AudioDecoders.Add("ext", bytes => stream)` ([a decoder of your own](extending.md#an-audio-format)) |

```csharp
Audio.AddSoundFile("jump", "Content/sfx/jump.ogg");                 // a sound effect: decoded whole, now
Audio.AddMusicFile("city", "Content/music/city.ogg");               // music: decoded while it plays
Audio.AddMusicFile("sting", "Content/music/sting.mp3", loop: false, AudioLoadMode.Decoded);
```

Music is kept one of two ways, chosen per piece:

| `AudioLoadMode` | Memory | While it plays | Use it for |
| --- | --- | --- | --- |
| `Streamed` (the default) | the file's bytes and half a second of sound | decodes a little every frame, outside the simulation step; plays at its own pitch; a seek decodes up to the place asked for | long tracks |
| `Decoded` | the whole sound: about ten megabytes per minute of stereo at 44.1 kHz | nothing to decode; changes pitch; seeks at once | short pieces, stings, anything played many times at once |

Sound effects are always decoded whole. A long ambience is better registered as music and played beside the track
with `PlayMusicVoice`, which streams it.

A seek in streamed OGG or MP3 lands on the exact sample, by decoding and dropping what lies between: forward from
where the music is, or from the file's beginning when it goes back, at a few milliseconds per second of music
skipped. A loop that goes back to the same place again and again does not pay for it: the place is prepared while
the loop plays.

### Files from content, and releasing them

Sound and music files also come through `Game.Assets`, from any content source (a zip archive too), shared and
released by scopes like any asset:

```csharp
Audio.AddSound("jump", Assets.Sound("sfx/jump.ogg"));
Audio.AddMusic("city", Assets.Music("music/city.ogg"));
```

A registered sound stays in memory until it is removed: `Audio.RemoveSound(name)` and `Audio.RemoveMusic(name)`
release what the mixer holds (music that was playing stops; a sound that was playing plays on to its end), and
registering a name again releases what had it.

For the sounds of one level there is nothing to register or remove: play the clip itself, and the level's scope is
its only owner.

```csharp
_level = Assets.CreateScope("forest");
_owl = _level.Sound("forest/owl.ogg");        // decoded once, held by the scope

Audio.Play(_owl, volume: 0.6f);               // or Audio.PlayVoice(_owl, parameters) for a handle
_level.Dispose();                             // the clip is freed once its last voice has ended
```

`Assets.Statistics.AudioBytes` tells how much decoded sound and encoded music the assets hold.


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

- Spatial sound is 2D and by volume and pan only: no filtering with distance, no sound blocked by walls, no
  reverberation by place. Pan is by horizontal offset; a sound above or below the listener is in the middle.
- Emitters of a scene that is not updated (under a pause overlay) keep sounding at their last volume and pan; to
  silence them, lower or fade their bus.
- With more looping emitters in range than voices, the ones in range first keep their voices, not the nearest.

- Effects are on buses, not on single voices: a sound that needs processing of its own plays on a bus of its own, and
  there are 32 buses. A bus has a volume and effects but no pitch.
- A crossfade and every ramp are straight lines in gain, so two unrelated tracks dip slightly in loudness half way
  through a crossfade.
- The limiter looks at nothing ahead: a mix that jumps over full scale is turned down from that sample on, which
  bends the first peak instead of clipping the whole passage. An effect taken out of a chain is cut, tail included;
  bypass keeps it in place.
- Without a time, volume, pan and pitch change in one step at the update that sets them: a large jump on a loud sound
  can click, so give `Set` a time or change them a little every update.
- A position follows the game's updates: it is exact at each update and does not move between two of them. Streamed
  music that fell behind its decoding (a frame of several hundred milliseconds) is that much behind its position
  until it is sought. A stream whose decoder does not know its length has no duration, cannot be sought and counts
  its position on through its loops.
- A loop wraps at a whole sample of the sound; played at another pitch or rate, the fraction of a sample across the
  seam holds the last sample before it.
- Streamed music keeps its own pitch; register it with `AudioLoadMode.Decoded` to change it.
- A seek backward in streamed OGG or MP3 decodes from the file's beginning to the place asked for, and a seek forward
  from where the music is: a few milliseconds per second of music skipped, on the game's thread. Decoded music
  seeks at once.
- MP3 has no loop points, and an MP3 file that loops has a short gap at the seam (the padding of its encoder).
- A sound registered by name stays in memory until `RemoveSound`; a clip played without a name is held only while
  it plays.

- Sound is not instant: it is heard after the queue that keeps pauses from being heard, 60 ms on the SDL platform
  and 100 ms in browsers. On the SDL platform a sound reaches the device about 65 ms after the update that played it
  at 60 or more frames per second and about 75 ms at 30, before the device's own buffer; a slow frame adds up to its
  catch-up updates to that for about a second.
- A sound or a bus keeps the changes of eight updates waiting for their points in the sound, which covers a slow
  frame made up with the default five updates. When more wait at once (an `AudioEngineSettings.DelayFrames` of
  eight updates or more, or a `TimeSettings.MaxStepsPerFrame` raised to eight or more), eight of them still take
  effect at their own points, spread over the wait, and each of the others takes effect together with the next one
  kept. The value heard is then never ahead of the one asked for and never more than about a quarter of the wait
  behind it (4 updates with 16 waiting, 16 with 64); the oldest waiting change and the last one asked for always
  keep their points.
- There is no setting for sound in the background: a suspended game is silent, a desktop game behind another window
  sounds unless the game lowers its buses.
