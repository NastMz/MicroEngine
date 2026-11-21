using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Music with an intro that plays once and a part that repeats, and a picture kept in step with it: a marker that
/// lands on a line at every beat the player hears.
/// </summary>
public sealed class MusicTimeExample : Scene
{
    /// <summary>The length of the intro, of the part that repeats and of one beat, in seconds.</summary>
    public const double IntroSeconds = 1, LoopSeconds = 2, BeatSeconds = 0.5;

    /// <summary>The beat of the loop being heard (0 to 3), or -1 during the intro.</summary>
    public int Beat { get; private set; } = -1;
    /// <summary>How far through that beat the music is, 0 to 1.</summary>
    public float Phase { get; private set; }

    #region time-setup
    public override void Enter()
    {
        // The clip names the part that repeats; the music plays its intro once and then stays in that part.
        if (!Audio.HasMusic("time-track"))
            Audio.AddMusic("time-track", TimeSounds.Track(), loop: true);
        Audio.PlayMusic("time-track", fadeSeconds: 0);
    }

    public override void Exit() => Audio.StopMusic(0.2f);
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

        #region time-follow
        // What the player hears at this update: the music's position less the time sound takes to reach the speaker.
        double heard = Audio.MusicHeardPosition;
        if (heard < IntroSeconds) (Beat, Phase) = (-1, 0);
        else
        {
            double beats = (heard - IntroSeconds) / BeatSeconds;
            Beat = (int)beats;
            Phase = (float)(beats - Beat);
        }
        #endregion

        #region time-seek
        if (Input.Pressed("action")) Audio.SeekMusic(0);                            // the intro again
        if (Input.Pressed("right")) Audio.SeekMusic(Audio.MusicPosition + BeatSeconds);   // one beat ahead
        #endregion
    }

    public override void Draw(IRenderer r)
    {
        var theme = UiTheme.Default;
        Font.Draw(r, "AN INTRO THAT PLAYS ONCE, THEN A LOOP", new Vector2(16, 30), Color.White);
        Font.Draw(r, "ACTION: THE INTRO AGAIN   RIGHT: ONE BEAT AHEAD", new Vector2(16, 44), Color.White);
        for (int beat = 0; beat < 4; beat++)
        {
            var box = new RectF(60 + beat * 54, 84, 40, 40);
            r.FillRect(box, beat == Beat ? theme.Accent.WithAlpha(1 - 0.7f * Phase) : theme.Muted.WithAlpha(0.3f));
        }
        Font.Draw(r, Beat < 0 ? "INTRO" : $"BEAT {Beat + 1} OF 4", new Vector2(16, 136), Beat < 0 ? theme.Muted : theme.Accent);
        Font.Draw(r, $"MUSIC AT {Audio.MusicPosition:0.00} S   HEARD AT {Audio.MusicHeardPosition:0.00} S", new Vector2(16, 150), theme.Muted);
        if (!Audio.SupportsVoices) Font.Draw(r, "(SILENT DEVICE: THE CLOCK RUNS, NOTHING PLAYS)", new Vector2(16, 164), theme.Muted);
        Heading.Draw(r, Font, "MUSIC TIME");
    }
}

/// <summary>The music of <see cref="MusicTimeExample"/>, synthesized: a rising intro, then four beats, each a tick and a tone of its own.</summary>
public static class TimeSounds
{
    /// <summary>The tone of the intro's end and of each of the loop's four beats, in hertz.</summary>
    public static readonly int[] BeatTones = [330, 440, 550, 660];

    /// <summary>The whole piece, with the part after the intro as its loop region.</summary>
    public static AudioClip Track()
    {
        const int rate = AudioClip.DefaultSampleRate;
        int intro = (int)(MusicTimeExample.IntroSeconds * rate), beat = (int)(MusicTimeExample.BeatSeconds * rate);
        var samples = new float[intro + 4 * beat];
        for (int i = 0; i < intro; i++)
        {
            // A tone that rises through the intro.
            double t = (double)i / intro;
            samples[i] = (float)(0.25 * t * Math.Sin(2 * Math.PI * (110 + 110 * t) * i / rate));
        }
        for (int b = 0; b < 4; b++)
            for (int i = 0; i < beat; i++)
            {
                double fade = Math.Exp(-6.0 * i / beat);
                samples[intro + b * beat + i] = (float)(0.4 * fade * Math.Sin(2 * Math.PI * BeatTones[b] * i / rate));
            }
        return new AudioClip(samples) { LoopRegion = LoopRegion.FromSeconds(MusicTimeExample.IntroSeconds, 0, rate) };
    }
}
