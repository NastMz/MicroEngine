using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A sound that keeps playing and follows something on screen: a looping voice whose stereo position, volume and pitch
/// change every update, on a bus with a volume of its own.
/// </summary>
public sealed class VoicesExample : Scene
{
    private const float Width = 320, Loudest = 0.5f;
    private VoiceHandle _hum;
    private float _x = 40, _speed = 90;

    /// <summary>Where the sound source is, in canvas pixels from the left edge.</summary>
    public float SourceX => _x;

    #region voice-start
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
    #endregion

    #region voice-follow
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
    #endregion

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(Width / 2 - 1, 84, 2, 24), UiTheme.Default.Muted);
        Font.Draw(r, "LISTENER", new Vector2(Width / 2 - Font.MeasureWidth("LISTENER") / 2, 112), UiTheme.Default.Muted);
        r.FillCircle(new Vector2(_x, 96), 5, UiTheme.Default.Accent);
        Font.Draw(r, "A LOOPING SOUND FOLLOWS THE DOT: PAN, VOLUME, PITCH", new Vector2(16, 30), Color.White);
        Font.Draw(r, $"UP/DOWN: AMBIENCE BUS VOLUME {Audio.BusVolume("ambience"):0.00}", new Vector2(16, 44), Color.White);
        Font.Draw(r, $"VOICE: {(Audio.IsPlaying(_hum) ? "PLAYING" : "NONE (SILENT DEVICE)")}", new Vector2(16, 140), UiTheme.Default.Muted);
        Heading.Draw(r, Font, "A SOUND THAT FOLLOWS");
    }
}
