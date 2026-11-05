using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Audio.Retro;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>A sound made in code, a WAV file, music that starts and stops, volume and stereo pan.</summary>
public sealed class SoundExample : Scene
{
    #region register
    public override void Enter()
    {
        // Register each sound once by name (a game does this in Load); playing it later is cheap.
        if (!Audio.HasSound("laser"))
        {
            Audio.AddSound("laser", Synth.Render(new SfxParams { Wave = Wave.Saw, Frequency = 900, EndFrequency = 200, Decay = 0.25f }));
            Audio.AddSoundFile("chime", new ContentFolder().PathOf("pickup.wav"));
            Audio.AddMusic("loop", new Song(126)
                .Track(Instrument.Lead, "A4 - C5 - E5 - C5 - | G4 - B4 - D5 - B4 -")
                .Track(Instrument.Bass, "A2 . A2 . A2 . A2 . | G2 . G2 . G2 . G2 .")
                .Drums("k . h . s . h . k k h . s . h .")
                .Render());
        }
    }

    public override void Exit() => Audio.StopMusic();
    #endregion

    #region play
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("confirm")) Audio.Play("laser", pitchJitter: 0.1f);                    // a little variation
        if (Input.Pressed("action")) Audio.Play("chime", pan: Game.Random.NextSingle());        // 0 left .. 1 right
        if (Input.Pressed("cancel"))
        {
            if (Audio.CurrentMusic is null) Audio.PlayMusic("loop");
            else Audio.StopMusic(fadeSeconds: 1);
        }
        Audio.MusicVolume += Input.Axis("left", "right") * dt;                                  // clamped to 0..1
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        Font.Draw(r, "CONFIRM (Z): SYNTH SOUND", new Vector2(16, 30), Color.White);
        Font.Draw(r, "ACTION (C): WAV FILE, RANDOM PAN", new Vector2(16, 44), Color.White);
        Font.Draw(r, "CANCEL (X): MUSIC ON/OFF", new Vector2(16, 58), Color.White);
        Font.Draw(r, $"LEFT/RIGHT: MUSIC VOLUME {Audio.MusicVolume:0.00}", new Vector2(16, 72), Color.White);
        Font.Draw(r, $"MUSIC: {Audio.CurrentMusic ?? "NONE"}   DEVICE: {(Audio.Device.IsAvailable ? "OK" : "SILENT")}", new Vector2(16, 96), UiTheme.Default.Muted);
        Heading.Draw(r, Font, "SOUND AND MUSIC");
    }
}
