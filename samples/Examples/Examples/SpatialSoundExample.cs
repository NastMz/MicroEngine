using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Sounds with a place: a listener that walks, a generator that hums where it stands, a drone that flies past, and a
/// knock wherever the listener asks for one. Each is louder the closer it is and comes from the side it is on.
/// </summary>
public sealed class SpatialSoundExample : Scene
{
    private const float Ground = 120, Speed = 90;
    private EntityId _listener, _generator, _drone;
    private float _droneX = -40;

    /// <summary>The generator's and the drone's emitters, for what the scene draws (and for tests).</summary>
    public SoundEmitter Generator { get; private set; } = null!;
    /// <inheritdoc cref="Generator"/>
    public SoundEmitter Drone { get; private set; } = null!;
    /// <summary>Where the listener stands.</summary>
    public Vector2 ListenerPosition => Objects.Local(_listener).Position2D;

    #region spatial-setup
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
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

        #region spatial-move
        // Entities move as they always do; their sounds follow, and stop when the scene leaves.
        ref var listener = ref Objects.Local(_listener);
        listener.Position2D = new Vector2(Math.Clamp(listener.Position2D.X + Input.Axis("left", "right") * Speed * dt, 10, 310), Ground);
        _droneX = _droneX > 360 ? -40 : _droneX + 70 * dt;
        Objects.Local(_drone).Position2D = new Vector2(_droneX, 50);

        // A sound at a place, once: nothing to keep, it is gone when it ends.
        if (Input.Pressed("action")) Sounds.Emit("spatial-knock", listener.Position2D + new Vector2(120, 0));
        #endregion
    }

    public override void Draw(IRenderer r)
    {
        var theme = UiTheme.Default;
        r.FillRect(new RectF(0, Ground + 8, r.Width, 1), theme.Muted);
        var listener = Objects.DrawPosition2D(_listener);
        foreach (var emitter in Sounds.Emitters)
        {
            // Each source: a ring that grows with how loud it is heard, on the side it is heard from.
            r.FillCircle(emitter.Position, 3 + 9 * emitter.Gain, (emitter.IsAudible ? theme.Accent : theme.Muted).WithAlpha(0.35f + 0.65f * emitter.Gain));
            r.FillRect(new RectF(emitter.Position.X - 12, emitter.Position.Y + 14, 24, 2), theme.Muted.WithAlpha(0.4f));
            r.FillRect(new RectF(emitter.Position.X - 12 + 24 * emitter.Pan - 1, emitter.Position.Y + 12, 2, 6), Color.White);
        }
        r.FillRect(new RectF(listener.X - 4, listener.Y - 12, 8, 20), Color.White);
        Font.Draw(r, "LEFT / RIGHT: WALK   ACTION: A KNOCK TO THE RIGHT", new Vector2(16, 30), Color.White);
        Font.Draw(r, $"GENERATOR {Generator.Gain * 100:0}%   DRONE {Drone.Gain * 100:0}%", new Vector2(16, 150), theme.Muted);
        if (!Audio.SupportsVoices) Font.Draw(r, "(SILENT DEVICE: NOTHING PLAYS)", new Vector2(16, 164), theme.Muted);
        Heading.Draw(r, Font, "SOUNDS WITH A PLACE");
    }
}

/// <summary>The sounds of <see cref="SpatialSoundExample"/>, synthesized: tones at known frequencies, so a test can find each in the mix.</summary>
public static class SpatialSounds
{
    /// <summary>The generator's tone, the drone's and the knock's, in hertz.</summary>
    public const int Hum = 220, DroneTone = 990, KnockTone = 1760;

    /// <summary>A steady tone that loops without a click.</summary>
    public static AudioClip Tone(int frequency, float seconds)
    {
        var samples = new float[(int)(AudioClip.DefaultSampleRate * seconds)];
        for (int i = 0; i < samples.Length; i++) samples[i] = 0.5f * (float)Math.Sin(2 * Math.PI * frequency * i / AudioClip.DefaultSampleRate);
        return new AudioClip(samples);
    }

    /// <summary>A short tone that dies away.</summary>
    public static AudioClip Knock()
    {
        var samples = new float[AudioClip.DefaultSampleRate / 4];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = 0.6f * MathF.Exp(-8f * i / samples.Length) * (float)Math.Sin(2 * Math.PI * KnockTone * i / AudioClip.DefaultSampleRate);
        return new AudioClip(samples);
    }
}
