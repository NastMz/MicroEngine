using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// Controlling sounds while they play, at the edges: several changes to one sound in one step when sounds start a delay
/// after their step, handles whose voice was taken by another sound, values that are not numbers, and pitch changes in a
/// clip's last frames. Rendered offline; a run with the calls under test is compared with a run without them, or with
/// the same calls on a mixer without delay, shifted by the delay.
/// </summary>
[TestClass]
public sealed class AudioEngineControlTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;
    private static readonly AudioFormat Stereo = new(Rate, 2);

    private static AudioClip Constant(float value, int frames, int rate = Rate) => new([.. Enumerable.Repeat(value, frames)], rate);
    /// <summary>Rises from 0.1 to 0.9, so every frame says which frame of the clip it is and none is silent.</summary>
    private static AudioClip Ramp(int frames) => new([.. Enumerable.Range(0, frames).Select(i => 0.1f + 0.8f * i / frames)], Rate);

    private static (AudioEngine Engine, OfflineAudioSink Sink) Open(int maxVoices = 32, int blockFrames = 256, int delayFrames = 0)
    {
        var sink = new OfflineAudioSink(Stereo, Steps, blockFrames);
        var engine = new AudioEngine(sink, new AudioEngineSettings { MaxVoices = maxVoices, UpdateRate = Steps, DelayFrames = delayFrames });
        return (engine, sink);
    }

    private static void Step(AudioEngine engine, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            engine.Advance();
            engine.Pump();
        }
    }

    private static float[] Render(int delayFrames, Action<AudioEngine> body, int maxVoices = 32)
    {
        var (engine, sink) = Open(maxVoices, delayFrames: delayFrames);
        body(engine);
        return [.. sink.Samples];
    }

    /// <summary>The samples of a run without delay, moved <paramref name="delayFrames"/> later and cut to the same length.</summary>
    private static float[] Delayed(float[] samples, int delayFrames)
    {
        var moved = new float[samples.Length];
        Array.Copy(samples, 0, moved, delayFrames * 2, samples.Length - delayFrames * 2);
        return moved;
    }

    private static void AssertSame(float[] expected, float[] actual, string message)
    {
        Assert.AreEqual(expected.Length, actual.Length, message);
        for (int i = 0; i < expected.Length; i++)
            if (expected[i] != actual[i]) Assert.Fail($"{message}: frame {i / 2}, channel {i % 2}: expected {expected[i]}, was {actual[i]}");
    }

    private static int LastAudibleFrame(float[] samples)
    {
        for (int f = samples.Length / 2 - 1; f >= 0; f--) if (samples[f * 2] != 0 || samples[f * 2 + 1] != 0) return f;
        return -1;
    }

    // ---- Values that are not numbers

    [TestMethod]
    public void A_sound_played_with_values_that_are_not_numbers_plays_with_the_defaults()
    {
        var expected = Render(0, e => { e.Play(Ramp(2 * StepFrames), new PlayParameters()); Step(e, 4); });
        var actual = Render(0, e =>
        {
            var voice = e.Play(Ramp(2 * StepFrames), new PlayParameters { Pitch = float.NaN, Volume = float.NaN, Pan = float.NaN });
            Assert.IsTrue(e.IsPlaying(voice));
            Step(e, 2);
            Assert.IsFalse(e.IsPlaying(voice), "it ended when the clip did, at the original pitch");
            Step(e, 2);
        });
        Assert.IsFalse(actual.Any(float.IsNaN), "no sample is not a number");
        AssertSame(expected, actual, "full volume, original pitch, centred");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void A_pitch_that_is_not_a_number_changes_nothing(bool loop)
    {
        void Run(AudioEngine e, bool call)
        {
            var voice = e.Play(Ramp(4 * StepFrames), new PlayParameters { Loop = loop });
            Step(e);
            if (call)
            {
                Assert.IsTrue(e.SetPitch(voice, float.NaN), "the sound is playing");
                Assert.IsTrue(e.IsPlaying(voice), "and goes on playing with its handle");
            }
            Step(e, 2);
            Assert.IsTrue(e.IsPlaying(voice));
            Assert.IsTrue(e.Stop(voice), "the handle still stops it");
            Step(e, 2);
        }
        var expected = Render(100, e => Run(e, false));
        var actual = Render(100, e => Run(e, true));
        AssertSame(expected, actual, "the same sound as without the call");
        Assert.AreEqual(3 * StepFrames + 100 - 1, LastAudibleFrame(actual), "stopped at the step that stopped it");
    }

    [TestMethod]
    public void Volume_pan_and_bus_volume_that_are_not_numbers_keep_their_values()
    {
        void Run(AudioEngine e, bool call)
        {
            var voice = e.Play(Ramp(4 * StepFrames), new PlayParameters { Volume = 0.5f, Pan = 0.25f });
            e.Bus("sfx").Volume = 0.5f;
            Step(e);
            if (call)
            {
                Assert.IsTrue(e.Set(voice, volume: float.NaN, pan: float.NaN));
                e.Bus("sfx").Volume = float.NaN;
                e.Bus("master").Volume = float.NaN;
                Assert.AreEqual(0.5f, e.Bus("sfx").Volume);
                Assert.AreEqual(1f, e.Bus("master").Volume);
            }
            Step(e, 4);
        }
        var expected = Render(0, e => Run(e, false));
        var actual = Render(0, e => Run(e, true));
        Assert.IsFalse(actual.Any(float.IsNaN), "no sample is not a number");
        AssertSame(expected, actual, "the same sound as without the calls");
    }

    [TestMethod]
    public void The_games_mixer_keeps_its_volumes_when_given_values_that_are_not_numbers()
    {
        var device = new CaptureAudioDevice(Steps, Rate);
        var mixer = new AudioMixer(device);
        mixer.AddMusic("song", Constant(0.5f, Rate));
        mixer.MusicVolume = 0.5f;
        mixer.SetBusVolume("music", 0.5f);
        mixer.PlayMusic("song", fadeSeconds: 0);
        mixer.MusicVolume = float.NaN;
        mixer.SetBusVolume("music", float.NaN);
        mixer.SfxVolume = float.NaN;
        Assert.AreEqual(0.5f, mixer.MusicVolume);
        Assert.AreEqual(0.5f, mixer.BusVolume("music"));
        Assert.AreEqual(0.8f, mixer.SfxVolume);
        mixer.Update(1f / Steps);
        mixer.Update(1f / Steps);
        var samples = device.Output.Samples;
        Assert.IsFalse(samples.Any(float.IsNaN));
        Assert.AreEqual(0.125f, samples[(StepFrames + 5) * 2], "the music at its volume, through its bus");

        var silent = new AudioMixer(new SilentAudioDevice());
        silent.SetBusVolume("sfx", 0.5f);
        silent.SetBusVolume("sfx", float.NaN);
        Assert.AreEqual(0.5f, silent.BusVolume("sfx"), "the same without the engine's mixer");
    }
}
