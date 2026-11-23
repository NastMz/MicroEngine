using Examples;
using MicroEngine.Audio;

namespace Samples.Tests;

/// <summary>
/// The music time example (audio manual), checked on what would be heard: its intro plays once, its four beats repeat,
/// and the beat the scene shows is the one in the output at that moment.
/// </summary>
[TestClass]
public sealed class MusicTimeExampleTests
{
    /// <summary>The amplitude of the tone at <paramref name="frequency"/> in the left channel between two times, in seconds.</summary>
    private static double Tone(AudioClip heard, int frequency, double from, double to)
    {
        int first = (int)(from * heard.SampleRate), last = (int)(to * heard.SampleRate);
        double real = 0, imaginary = 0;
        for (int f = first; f < last; f++)
        {
            double angle = 2 * Math.PI * frequency * f / heard.SampleRate, sample = heard.Samples[f * 2];
            real += sample * Math.Cos(angle);
            imaginary += sample * Math.Sin(angle);
        }
        return 2 * Math.Sqrt(real * real + imaginary * imaginary) / (last - first);
    }

    /// <summary>Which of the four beat tones is loudest between two times, or -1 when none is heard.</summary>
    private static int BeatHeard(AudioClip heard, double from, double to)
    {
        var levels = TimeSounds.BeatTones.Select(tone => Tone(heard, tone, from, to)).ToArray();
        int loudest = Array.IndexOf(levels, levels.Max());
        return levels[loudest] > 0.02 ? loudest : -1;
    }

    [TestMethod]
    public void TheIntroPlaysOnceTheBeatsRepeatAndTheSceneShowsTheBeatHeard()
    {
        CaptureAudioDevice? capture = null;
        var (game, platform) = SettingsExampleTests.Start(new MusicTimeExample(), () => capture = new CaptureAudioDevice(60));
        var scene = (MusicTimeExample)game.Scenes.Current!;
        double start = capture!.Duration - game.Audio.MusicPosition;   // when the music's first frame was played
        var shown = new List<(double Time, int Beat)>();
        while (capture.Duration < start + 7.2)
        {
            SettingsExampleTests.Frame(game, platform);
            shown.Add((capture.Duration - start, scene.Beat));
        }
        var heard = capture.Output;

        Assert.AreEqual(-1, BeatHeard(heard, start + 0.1, start + 0.9), "the intro has none of the beats' tones");
        // Seven seconds: the intro, then the four beats three times over.
        for (int pass = 0; pass < 3; pass++)
            for (int beat = 0; beat < 4; beat++)
            {
                double at = 1 + pass * 2 + beat * 0.5;
                Assert.AreEqual(beat, BeatHeard(heard, start + at + 0.02, start + at + 0.2), $"pass {pass + 1}, beat {beat + 1}, at {at} s");
            }
        Assert.IsTrue(Tone(heard, 165, start + 3.05, start + 3.45) < 0.01, "the intro's rising tone does not come back");

        // What the scene shows at each update is the beat in the output at that time (the device here has no latency).
        foreach (var (time, beat) in shown)
        {
            if (time < 1) { Assert.AreEqual(-1, beat, $"intro at {time:F3}"); continue; }
            double inLoop = (time - 1) % 2 / 0.5;
            if (inLoop - Math.Floor(inLoop) is < 0.05 or > 0.95) continue;   // an update on the line between two beats
            Assert.AreEqual((int)inLoop, beat, $"at {time:F3} s");
        }
        Assert.AreEqual(1 + (7.2 - 1) % 2, game.Audio.MusicPosition, 0.02, "the position is inside the loop");
    }

    [TestMethod]
    public void ASeekPlaysTheIntroAgainAndAStepAheadMovesOneBeat()
    {
        CaptureAudioDevice? capture = null;
        var (game, platform) = SettingsExampleTests.Start(new MusicTimeExample(), () => capture = new CaptureAudioDevice(60));
        while (game.Audio.MusicPosition < 1.6) SettingsExampleTests.Frame(game, platform);
        double before = game.Audio.MusicPosition;
        SettingsExampleTests.Press(game, platform, "right");
        Assert.AreEqual(before + 0.5 + 2 / 60.0, game.Audio.MusicPosition, 1e-6, "one beat ahead, and the two updates of the key press");
        SettingsExampleTests.Press(game, platform, "action");
        Assert.AreEqual(2 / 60.0, game.Audio.MusicPosition, 1e-6, "back in the intro");
        double sought = capture!.Duration - 2 / 60.0;
        while (capture.Duration < sought + 1.3) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(-1, BeatHeard(capture.Output, sought + 0.1, sought + 0.9), "the intro is heard again");
        Assert.AreEqual(0, BeatHeard(capture.Output, sought + 1.02, sought + 1.2), "then the first beat");
    }

    [TestMethod]
    public void WithoutADeviceTheClockStillRuns()
    {
        var (game, platform) = SettingsExampleTests.Start(new MusicTimeExample());
        var scene = (MusicTimeExample)game.Scenes.Current!;
        for (int i = 0; i < 80; i++) SettingsExampleTests.Frame(game, platform);
        Assert.IsTrue(game.Audio.MusicPosition > 1.2, "the music's time is counted on a silent device");
        Assert.AreEqual(0, scene.Beat, "and the scene follows it");
    }
}
