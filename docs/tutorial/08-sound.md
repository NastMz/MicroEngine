# 8. Sound and music

Gem Garden plays a chime when a gem is picked up, a thud when a slime touches the gardener, a click in menus, and a
looping tune. The chime comes from a file; everything else is made in code.

## Registering sounds

Download [`pickup.wav`](../../samples/GemGarden/part3-garden/Content/pickup.wav) into `Content/`. Then add a
`LoadSounds` method to `MainGame` and call it from `Load` (step 6 already shows the call):

<!-- code: samples/GemGarden/part3-garden/MainGame.cs#sounds -->
```csharp title="MainGame.cs"
private void LoadSounds(ContentFolder content)
{
    // A sound file from Content, and sounds and music made in code.
    Audio.AddSoundFile("pickup", content.PathOf("pickup.wav"));
    Audio.AddSound("hit", Synth.Render(new SfxParams { Wave = Wave.Noise, Frequency = 600, EndFrequency = 120, Sustain = 0.05f, Decay = 0.2f }));
    Audio.AddSound("select", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 660, Sustain = 0.03f, Decay = 0.08f, Volume = 0.5f }));
    Audio.AddMusic("garden", new Song(112)
        .Track(Instrument.Pluck, "C5 . E5 . G5 . E5 . | D5 . F5 . A5 . F5 . | E5 . G5 . C6 . G5 . | D5 . B4 . G4 . . .")
        .Track(Instrument.Bass, "C3 - - . G2 - - . | D3 - - . A2 - - . | C3 - - . E3 - - . | G2 - - . G2 - - .")
        .Drums("k . h . s . h .")
        .Render());
}
```

(`using MicroEngine.Audio;` at the top; next SDK version (not published yet: arrives with the next SDK version):
also `using MicroEngine.Audio.Retro;`, the namespace of `Synth`, `SfxParams`, `Wave`, `Song` and `Instrument`.)

`Audio` is the game's **mixer**, a bank of named sounds and music. Each sound is registered once, by name, and played
later by that name from anywhere (`Audio.Play("pickup")`). Registering decodes or synthesizes the sound, which takes a
moment, so it belongs in `Load`, never in `Update`.

| Call | Registers |
| --- | --- |
| `AddSoundFile(name, path)` | a WAV file (PCM, 8, 16 or 24-bit, mono or stereo) as a sound effect |
| `AddSound(name, clip)` | a sound made in code: `Synth.Render(new SfxParams { ... })` |
| `AddMusic(name, clip)` | music made in code: `new Song(bpm).Track(...).Render()` |
| `AddMusicFile(name, path)` | a music file: WAV everywhere, OGG on SDL and raylib, MP3 on raylib only |

`content.PathOf("pickup.wav")` gives the full path of a file in `Content`, and fails clearly if it is missing.

## Sounds made in code

`Synth.Render` is a small retro synthesizer: a waveform (`Square`, `Triangle`, `Saw`, `Sine`, `Noise`, two pulse
widths), a frequency that can slide (`EndFrequency`), an envelope (`Attack`, `Sustain`, `Decay`, in seconds), and
optional vibrato, arpeggio, low-pass filter and bit-crush. The same parameters always give the same sound, so a game can
ship without a single audio file. The "hit" above is noise sliding down from 600 to 120 Hz in a quarter of a second.

## Music made in code

`Song` is a step sequencer. Each `Track` is an instrument (`Lead`, `Bass`, `Pad`, `Pluck`, `Bell`) and a pattern of
steps: a note (`C5`, `F#3`, `Bb4`), `-` to hold the previous note, `.` for silence, `|` as a bar line the sequencer
ignores. `Drums` uses `k` (kick), `s` (snare), `h` (hat) and `o` (open hat). Shorter patterns repeat to the length of
the longest. `Render()` produces a stereo clip that loops seamlessly.

## Playing them

Sounds play from the scenes. The gem and slime code of step 7 already calls them:

```csharp
Audio.Play("pickup", pitchJitter: 0.05f);
Audio.Play("hit");
```

`pitchJitter` varies the pitch randomly by up to 5 %, so repeated pickups do not sound mechanical. Other parameters:
`volume`, `pitch` and `pan` (0 left, 1 right). An unregistered name throws; `Audio.TryPlay` returns false instead, for
optional sounds. The same sound is not started twice in one step, so ten gems collected at once do not make ten times
the noise.

The title screen starts the music when it appears, in its `Enter` method:

<!-- code: samples/GemGarden/part3-garden/Scenes/TitleScene.cs#music -->
```csharp title="Scenes/TitleScene.cs"
public override void Enter() => Audio.PlayMusic("garden");
```

Only one track plays at a time. `PlayMusic` with the track already playing does nothing, so coming back to the title
does not restart the tune; with another track, the current one fades out first. `StopMusic(fadeSeconds)` and
`PauseMusic(true/false)` stop and pause it ([step 10](10-states.md) pauses it under the pause menu).
`Audio.SfxVolume` and `Audio.MusicVolume` (0 to 1) are the global volumes, for an options screen.

## On each platform

- **No audio device** (a server, a CI machine): the game runs silently; nothing to handle.
- **Browsers** start audio only after the player interacts with the page; the page MicroEngine builds waits for a
  click or key before starting the game.
- `--mute` on the command line silences a desktop run (`microengine run -- --mute`).

The [sound example](../examples/focused.md#sound-and-music) plays every kind of sound with volume and pan controls;
[Audio](../manual/audio.md) covers formats and limits.

## Next

Part 3 ends with the HUD: [step 9](09-hud.md).
