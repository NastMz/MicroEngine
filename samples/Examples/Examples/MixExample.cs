using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Audio.Effects;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Effects on buses: a room around the sound effects (a reverb), music that drops under a voice line (ducking) and
/// goes muffled behind a pause (a low-pass filter), and a chime that crosses the screen on a pan ramp.
/// </summary>
public sealed class MixExample : Scene
{
    private const float Width = 320, ChimeSeconds = 0.9f;
    private readonly Reverb _room = new() { RoomSize = 0.7f, Wet = 0.35f };
    private readonly LowPass _muffle = new() { Cutoff = 600, Bypass = true };
    private readonly Compressor _duck = Compressor.Duck("voice");
    private VoiceHandle _music, _line;
    private float _chimeIn = 0.5f, _chimeAge = ChimeSeconds;

    /// <summary>Whether the game counts as paused: the music is muffled and the sound effects are turned down.</summary>
    public bool Paused { get; private set; }

    #region mix-setup
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
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("down")) SetPaused(true);
        if (Input.Pressed("up")) SetPaused(false);

        #region mix-duck
        // A voice line on the "voice" bus: the compressor on the music bus listens to that bus and turns the music
        // down while the line plays. Nothing else to do here.
        if (Input.Pressed("action") && !Audio.IsPlaying(_line))
            _line = Audio.PlayVoice("mix-line", new PlayParameters { Bus = "voice", Volume = 0.8f });
        #endregion

        #region mix-chime
        _chimeAge += dt;
        _chimeIn -= dt;
        if (_chimeIn <= 0)
        {
            (_chimeIn, _chimeAge) = (2f, 0);
            // It starts on the left and the mixer moves it to the right over its length, frame by frame.
            var chime = Audio.PlayVoice("mix-chime", new PlayParameters { Pan = 0, Volume = 0.7f });
            Audio.Set(chime, pan: 1, seconds: ChimeSeconds);
        }
        #endregion
    }

    #region mix-pause
    private void SetPaused(bool paused)
    {
        Paused = paused;
        _muffle.Bypass = !paused;                                  // the filter is switched in, not added again
        Audio.Bus("sfx").FadeTo(paused ? 0.3f : 1f, seconds: 0.3f); // and the sound effects fade down, without a step
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        var theme = UiTheme.Default;
        Font.Draw(r, "SFX BUS: REVERB   MUSIC BUS: LOW-PASS + DUCKING", new Vector2(16, 30), Color.White);
        Font.Draw(r, "ACTION: A VOICE LINE (THE MUSIC DROPS UNDER IT)", new Vector2(16, 44), Color.White);
        Font.Draw(r, "DOWN / UP: PAUSE ON / OFF (MUFFLED MUSIC)", new Vector2(16, 58), Color.White);
        float t = Math.Clamp(_chimeAge / ChimeSeconds, 0, 1);
        r.FillRect(new RectF(20, 100, Width - 40, 1), theme.Muted);
        if (_chimeAge < ChimeSeconds) r.FillCircle(new Vector2(20 + (Width - 40) * t, 100), 5, theme.Accent);
        Font.Draw(r, "A CHIME CROSSES FROM LEFT TO RIGHT", new Vector2(16, 112), theme.Muted);
        Font.Draw(r, $"MUSIC: {(Paused ? "MUFFLED (PAUSED)" : "CLEAR")}", new Vector2(16, 132), Paused ? theme.Accent : theme.Muted);
        Font.Draw(r, $"VOICE LINE: {(Audio.IsPlaying(_line) ? "SPEAKING, MUSIC DUCKED" : "SILENT")}", new Vector2(16, 146), Audio.IsPlaying(_line) ? theme.Accent : theme.Muted);
        if (!Audio.SupportsVoices) Font.Draw(r, "(SILENT DEVICE: NOTHING PLAYS)", new Vector2(16, 160), theme.Muted);
        Heading.Draw(r, Font, "EFFECTS ON BUSES");
    }
}

/// <summary>The sounds of <see cref="MixExample"/>, synthesized: tones at known frequencies, so a test can find each in the mix.</summary>
public static class MixSounds
{
    /// <summary>The music's low tone and its bright one, the voice line's tone and the chime's, in hertz.</summary>
    public const int MusicLow = 220, MusicBright = 3520, Voice = 700, ChimeTone = 1500;

    private static float Tone(int i, int frequency) => (float)Math.Sin(2 * Math.PI * frequency * i / AudioClip.DefaultSampleRate);

    /// <summary>One second that loops without a click: a low tone, its octave and a bright one.</summary>
    public static AudioClip Loop()
    {
        var samples = new float[AudioClip.DefaultSampleRate];
        for (int i = 0; i < samples.Length; i++) samples[i] = 0.3f * Tone(i, MusicLow) + 0.15f * Tone(i, MusicLow * 2) + 0.15f * Tone(i, MusicBright);
        return new AudioClip(samples);
    }

    /// <summary>A second of a steady tone that rises and falls at its ends: the "voice".</summary>
    public static AudioClip Line()
    {
        var samples = new float[AudioClip.DefaultSampleRate];
        for (int i = 0; i < samples.Length; i++)
        {
            float edge = Math.Min(1, Math.Min(i, samples.Length - 1 - i) / 2000f);
            samples[i] = 0.5f * edge * Tone(i, Voice);
        }
        return new AudioClip(samples);
    }

    /// <summary>A tone that dies away.</summary>
    public static AudioClip Chime()
    {
        var samples = new float[(int)(AudioClip.DefaultSampleRate * 0.9f)];
        for (int i = 0; i < samples.Length; i++) samples[i] = 0.5f * MathF.Exp(-4f * i / samples.Length) * Tone(i, ChimeTone);
        return new AudioClip(samples);
    }
}
