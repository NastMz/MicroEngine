using Examples;
using MicroEngine.Audio;

namespace Samples.Tests;

/// <summary>
/// The spatial sound example (audio manual), checked on what would be heard: each source is a tone at a known
/// frequency, so its level in each speaker can be measured as the listener walks.
/// </summary>
[TestClass]
public sealed class SpatialSoundExampleTests
{
    /// <summary>The amplitude of the tone at <paramref name="frequency"/> in one channel between two times, in seconds.</summary>
    private static double Tone(AudioClip heard, int frequency, double from, double to, int channel)
    {
        int first = (int)(from * heard.SampleRate), last = (int)(to * heard.SampleRate);
        double real = 0, imaginary = 0;
        for (int f = first; f < last; f++)
        {
            double angle = 2 * Math.PI * frequency * f / heard.SampleRate, sample = heard.Samples[f * 2 + channel];
            real += sample * Math.Cos(angle);
            imaginary += sample * Math.Sin(angle);
        }
        return 2 * Math.Sqrt(real * real + imaginary * imaginary) / (last - first);
    }

    [TestMethod]
    public void TheGeneratorIsLouderAsTheListenerWalksToItAndChangesSideAsItIsPassed()
    {
        CaptureAudioDevice? capture = null;
        var (game, platform) = SettingsExampleTests.Start(new SpatialSoundExample(), () => capture = new CaptureAudioDevice(60));
        var scene = (SpatialSoundExample)game.Scenes.Current!;
        void Until(double seconds)
        {
            while (capture!.Duration < seconds) SettingsExampleTests.Frame(game, platform);
        }

        // Standing at 60, the generator at 250: 190 away of 220, to the right.
        Until(0.5);
        Assert.AreEqual(190, 250 - scene.ListenerPosition.X, 0.01);
        Assert.AreEqual(1 - (190 - 16) / (220f - 16), scene.Generator.Gain, 1e-4f);
        Assert.AreEqual(1, scene.Generator.Pan, "past the pan distance: all on the right");
        double farRight = Tone(capture!.Output, SpatialSounds.Hum, 0.3, 0.5, 1), farLeft = Tone(capture.Output, SpatialSounds.Hum, 0.3, 0.5, 0);
        Assert.AreEqual(0.5 * 0.6 * scene.Generator.Gain, farRight, 0.01, "its level at that distance");
        Assert.IsTrue(farLeft < 0.005, $"nothing of it on the left ({farLeft:F4})");

        // Walk right for two seconds (180 units): to 240, ten short of it.
        game.Input.SetForced("right", true);
        Until(2.5);
        game.Input.SetForced("right", false);
        Until(3.0);
        Assert.AreEqual(240, scene.ListenerPosition.X, 2);
        Assert.AreEqual(1, scene.Generator.Gain, "within its minimum distance");
        double nearRight = Tone(capture.Output, SpatialSounds.Hum, 2.8, 3.0, 1), nearLeft = Tone(capture.Output, SpatialSounds.Hum, 2.8, 3.0, 0);
        Assert.AreEqual(0.5 * 0.6, nearRight, 0.01, "full volume beside it");
        Assert.IsTrue(nearLeft > 0.25, $"and nearly as much on the left ({nearLeft:F3})");
        Assert.IsTrue(nearRight > 5 * farRight);

        // On to the wall at 310: the generator is now 60 behind, on the left.
        game.Input.SetForced("right", true);
        Until(4.2);
        game.Input.SetForced("right", false);
        Until(4.6);
        Assert.AreEqual(310, scene.ListenerPosition.X, 0.01);
        Assert.IsTrue(scene.Generator.Pan < 0.5f);
        Assert.IsTrue(Tone(capture.Output, SpatialSounds.Hum, 4.4, 4.6, 0) > Tone(capture.Output, SpatialSounds.Hum, 4.4, 4.6, 1) + 0.05, "it is heard from the left once passed");
    }

    [TestMethod]
    public void TheDroneFliesPastAndAKnockIsHeardOnceToTheRight()
    {
        CaptureAudioDevice? capture = null;
        var (game, platform) = SettingsExampleTests.Start(new SpatialSoundExample(), () => capture = new CaptureAudioDevice(60));
        var scene = (SpatialSoundExample)game.Scenes.Current!;
        var levels = new List<(float Gain, float Pan)>();
        while (capture!.Duration < 5.0)
        {
            if (capture.Duration is >= 2.0 and < 2.03) SettingsExampleTests.Press(game, platform, "action");
            else SettingsExampleTests.Frame(game, platform);
            levels.Add((scene.Drone.Gain, scene.Drone.Pan));
        }
        Assert.IsTrue(levels.Max(l => l.Gain) > 0.75f, "the drone comes close, overhead");
        Assert.IsTrue(levels.First(l => l.Gain > 0.5f).Pan < 0.5f && levels.Last(l => l.Gain > 0.5f).Pan > 0.5f, "from the left to the right");
        Assert.IsTrue(levels[^1].Gain == 0 && !scene.Drone.IsAudible, "and leaves earshot, holding no voice");

        Assert.IsTrue(Tone(capture.Output, SpatialSounds.KnockTone, 2.02, 2.12, 1) > 0.02, "the knock, on the right");
        Assert.IsTrue(Tone(capture.Output, SpatialSounds.KnockTone, 2.02, 2.12, 0) < Tone(capture.Output, SpatialSounds.KnockTone, 2.02, 2.12, 1) / 2);
        Assert.IsTrue(Tone(capture.Output, SpatialSounds.KnockTone, 3.0, 3.2, 1) < 0.002, "once");
        Assert.AreEqual(2, scene.Sounds.Emitters.Count, "its emitter is gone with it");

        game.Scenes.Pop();
        for (int i = 0; i < 20; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(0, game.Audio.Statistics.ActiveVoices, "the scene's sounds stop with it");
    }

    [TestMethod]
    public void WithoutADeviceTheSceneRunsTheSame()
    {
        var (game, platform) = SettingsExampleTests.Start(new SpatialSoundExample());
        var scene = (SpatialSoundExample)game.Scenes.Current!;
        for (int i = 0; i < 120; i++) SettingsExampleTests.Frame(game, platform);
        Assert.IsTrue(scene.Drone.Gain > 0, "gains are computed all the same");
        Assert.IsFalse(scene.Drone.IsAudible);
    }
}
