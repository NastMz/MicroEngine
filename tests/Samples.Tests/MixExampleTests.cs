using Examples;
using MicroEngine.Audio;

namespace Samples.Tests;

/// <summary>
/// The effects example (audio manual), checked on what would be heard: the example plays through a capture device, and
/// each of its sounds is a tone at a known frequency, so the level of each in the mix can be measured.
/// </summary>
[TestClass]
public sealed class MixExampleTests
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

    private static double Decibels(double ratio) => 20 * Math.Log10(ratio);

    [TestMethod]
    public void TheMusicDropsUnderTheVoiceLineAndIsMuffledBehindThePause()
    {
        CaptureAudioDevice? capture = null;
        var (game, platform) = SettingsExampleTests.Start(new MixExample(), () => capture = new CaptureAudioDevice(60));
        var scene = (MixExample)game.Scenes.Current!;
        Assert.AreEqual(60, game.Settings.UpdateRate, "the times below count 60 updates a second");
        void Until(double seconds)
        {
            while (capture!.Duration < seconds) SettingsExampleTests.Frame(game, platform);
        }

        Until(2.0);
        SettingsExampleTests.Press(game, platform, "action");      // a voice line of one second
        Until(4.1);
        SettingsExampleTests.Press(game, platform, "down");        // pause: the low-pass is switched in
        Assert.IsTrue(scene.Paused);
        Until(4.65);
        SettingsExampleTests.Press(game, platform, "up");
        Until(5.4);
        var heard = capture!.Output;

        // The music alone, before anything: its low tone and its bright one.
        double low = Tone(heard, MixSounds.MusicLow, 1.5, 1.7), bright = Tone(heard, MixSounds.MusicBright, 1.5, 1.7);
        Assert.IsTrue(low > 0.1 && bright > 0.04, $"the music plays: {low:F3}, {bright:F3}");

        // Ducking: while the voice line plays, the music is 12 dB down; afterwards it is back.
        Assert.IsTrue(Tone(heard, MixSounds.Voice, 2.5, 2.7) > 0.3, "the voice line is heard");
        Assert.AreEqual(-12, Decibels(Tone(heard, MixSounds.MusicLow, 2.5, 2.7) / low), 1.5, "the music under the voice line");
        Assert.AreEqual(0, Decibels(Tone(heard, MixSounds.MusicLow, 3.9, 4.1) / low), 0.5, "the music after it");

        // The pause: the low-pass at 600 Hz takes the bright tone away and leaves the low one.
        Assert.IsTrue(Decibels(Tone(heard, MixSounds.MusicBright, 4.4, 4.6) / bright) < -25, "muffled: the bright tone is gone");
        Assert.AreEqual(0, Decibels(Tone(heard, MixSounds.MusicLow, 4.4, 4.6) / low), 1.0, "and the low one stays");
        Assert.AreEqual(0, Decibels(Tone(heard, MixSounds.MusicBright, 5.2, 5.4) / bright), 1.0, "resumed: clear again");

        // The room: the chime played from 0.5 s to 1.4 s; what is heard of it afterwards is the reverb's tail.
        Assert.IsTrue(Tone(heard, MixSounds.ChimeTone, 1.5, 1.7) > 1e-4, $"the reverb rings after the chime ended: {Tone(heard, MixSounds.ChimeTone, 1.5, 1.7):E2}");
        // And it moves to the right: it starts on the left side only, and by its middle the right side has as much.
        // (Later the room's reflections, which come from both sides, cover what is left of the chime itself.)
        double Right(double from, double to)
        {
            var right = new AudioClip([.. heard.Samples.Skip(1).Append(0f)], heard.SampleRate, 2);
            return Tone(right, MixSounds.ChimeTone, from, to);
        }
        double startLeft = Tone(heard, MixSounds.ChimeTone, 0.52, 0.6), startRight = Right(0.52, 0.6);
        double middleLeft = Tone(heard, MixSounds.ChimeTone, 0.9, 1.0), middleRight = Right(0.9, 1.0);
        Assert.IsTrue(startLeft > 2 * startRight, $"the chime starts on the left: {startLeft:F4} against {startRight:F4}");
        Assert.IsTrue(middleRight / middleLeft > 2 * (startRight / startLeft), $"and moves right: {middleLeft:F4} against {middleRight:F4}");

        SettingsExampleTests.Press(game, platform, "pause");
        Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current, "pause goes back to the list");
        Assert.AreEqual(0, game.Audio.Bus("sfx").Effects.Count + game.Audio.Bus("music").Effects.Count, "leaving takes the effects off the buses");
        Assert.AreEqual(0, capture.Engine.FaultedEffects);
    }

    [TestMethod]
    public void TheExampleRunsTheSameOnADeviceWithoutVoices()
    {
        var (game, platform) = SettingsExampleTests.Start(new MixExample());
        Assert.IsFalse(game.Audio.SupportsVoices);
        Assert.AreEqual(2, game.Audio.Bus("music").Effects.Count, "the buses are set up all the same");
        SettingsExampleTests.Press(game, platform, "action");
        SettingsExampleTests.Press(game, platform, "down");
        Assert.IsTrue(((MixExample)game.Scenes.Current!).Paused);
        SettingsExampleTests.Press(game, platform, "pause");
        Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current);
    }

    [TestMethod]
    public void TheExtensionExamplesEffectWobblesItsBusAtItsRate()
    {
        using var capture = new CaptureAudioDevice(60);
        var audio = new AudioMixer(capture) { Deterministic = true };
        audio.AddSound("steady", new AudioClip([.. Enumerable.Repeat(0.5f, capture.SampleRate * 2)], capture.SampleRate));
        var tremolo = new Tremolo { Rate = 4, Depth = 1 };
        audio.Bus("sfx").Add(tremolo);
        audio.PlayVoice("steady", new PlayParameters());
        for (int i = 0; i < 60; i++) audio.Update(1f / 60);
        var heard = capture.Output.Samples;
        int period = capture.SampleRate / 4;
        Assert.AreEqual(0.5f, heard[0], 1e-4f, "full volume at the start of a wobble");
        Assert.AreEqual(0f, heard[period / 2 * 2], 1e-3f, "silence half way through it, at full depth");
        Assert.AreEqual(0.5f, heard[period * 2], 1e-3f, "and back after one period: four times a second");
        Assert.IsFalse(tremolo.Faulted);
    }
}
