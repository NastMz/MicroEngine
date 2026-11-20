using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// Ramps: a volume, pan or pitch of a voice and the volume of a bus move to a new value over a time the caller gives,
/// one equal step per output frame from the frame of the game step that asked, ending on the value exactly; a sound
/// can fade in; music can crossfade. Rendered offline and compared with the straight line computed here.
/// </summary>
/// <remarks>Declared tolerance: 1e-5 on a ramp (the mixer adds a step per frame; the reference multiplies).</remarks>
[TestClass]
public sealed class AudioRampTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;
    private const float Tolerance = 1e-5f;

    private static AudioClip Constant(float value, double seconds) => new([.. Enumerable.Repeat(value, (int)(seconds * Rate))], Rate);

    private static (AudioEngine Engine, OfflineAudioSink Sink) Open(int blockFrames = 1024, int delayFrames = 0)
    {
        var sink = new OfflineAudioSink(new AudioFormat(Rate, 2), Steps, blockFrames);
        return (new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = delayFrames }), sink);
    }

    private static void Step(AudioEngine engine, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            engine.Advance();
            engine.Pump();
        }
    }

    private static float Left(OfflineAudioSink sink, long frame) => sink.Samples[(int)frame * 2];
    private static float Right(OfflineAudioSink sink, long frame) => sink.Samples[(int)frame * 2 + 1];

    [TestMethod]
    public void A_sound_fades_in_from_silence_to_its_volume_frame_by_frame()
    {
        var (engine, sink) = Open(delayFrames: 100);
        Step(engine);
        engine.Play(Constant(1f, 1), new PlayParameters { Volume = 0.8f, FadeIn = 0.01f });
        Step(engine, 3);
        int start = StepFrames + 100, fade = 480;
        Assert.AreEqual(0f, Left(sink, start - 1));
        for (int k = 0; k < fade; k++) Assert.AreEqual(0.8f * (k + 1) / fade, Left(sink, start + k), Tolerance, $"frame {k} of the fade");
        Assert.AreEqual(0.8f, Left(sink, start + fade - 1), "the last step lands on the volume exactly");
        Assert.AreEqual(0.8f, Left(sink, start + fade + 500));
    }

    [TestMethod]
    public void A_voice_volume_and_pan_move_over_the_time_asked_from_the_frame_of_their_step()
    {
        string? reference = null;
        foreach (int block in new[] { 64, 441, 1024, 4096 })
        {
            var (engine, sink) = Open(block, delayFrames: 300);
            var voice = engine.Play(Constant(1f, 2), new PlayParameters { Volume = 1, Pan = 0.5f });
            Step(engine, 2);
            Assert.IsTrue(engine.Set(voice, volume: 0.25f, pan: 1f, seconds: 0.02));
            Step(engine, 6);
            int start = 2 * StepFrames + 300, ramp = 960;
            Assert.AreEqual(1f, Left(sink, start - 1), $"block {block}: untouched before its frame");
            for (int k = 0; k < ramp; k += 37)
            {
                float t = (k + 1f) / ramp, volume = 1 + (0.25f - 1) * t;
                Assert.AreEqual(volume * (1 - t), Left(sink, start + k), Tolerance, $"block {block}, frame {k}: the left side falls to nothing as the pan goes right");
                Assert.AreEqual(volume, Right(sink, start + k), Tolerance, $"block {block}, frame {k}");
            }
            Assert.AreEqual(0f, Left(sink, start + ramp - 1), "exactly on the values at the last frame");
            Assert.AreEqual(0.25f, Right(sink, start + ramp - 1));
            Assert.AreEqual(0.25f, Right(sink, start + ramp + 1000), "and it stays there");
            string hash = AudioGraphBaselineTests.Hash(sink.Samples);
            reference ??= hash;
            Assert.AreEqual(reference, hash, $"block {block}");
        }
    }

    [TestMethod]
    public void A_change_without_a_time_ends_a_ramp_at_once()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(Constant(1f, 1), new PlayParameters());
        engine.Set(voice, volume: 0f, seconds: 1);
        Step(engine, 2);
        engine.Set(voice, volume: 0.5f);
        Step(engine, 2);
        Assert.AreEqual(1 - (2f * StepFrames) / Rate, Left(sink, 2 * StepFrames - 1), Tolerance, "the ramp so far");
        Assert.AreEqual(0.5f, Left(sink, 2 * StepFrames), "then the value asked for, at once");
        Assert.AreEqual(0.5f, Left(sink, 3 * StepFrames));
    }

    [TestMethod]
    public void A_ramp_stands_still_while_its_sound_is_paused()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(Constant(1f, 2), new PlayParameters());
        engine.Set(voice, volume: 0f, seconds: 0.1);
        Step(engine, 2);
        engine.Pause(voice, true);
        Step(engine, 10);
        engine.Pause(voice, false);
        Step(engine, 2);
        float before = Left(sink, 2 * StepFrames - 1);
        Assert.AreEqual(0f, Left(sink, 5 * StepFrames), "paused: silent");
        Assert.AreEqual(before - 1f / (0.1f * Rate), Left(sink, 12 * StepFrames), Tolerance, "it goes on from where it was");
    }

    /// <summary>A clip whose every sample is its own index over its length: the output shows where the sound is.</summary>
    private static AudioClip Counter(int frames)
    {
        var samples = new float[frames];
        for (int i = 0; i < frames; i++) samples[i] = (float)i / frames;
        return new AudioClip(samples, Rate);
    }

    [TestMethod]
    public void A_pitch_glides_over_the_time_asked_and_the_timeline_knows_when_the_sound_ends()
    {
        var (engine, sink) = Open();
        int frames = Rate;
        var voice = engine.Play(Counter(frames), new PlayParameters());
        Step(engine, 6);
        Assert.IsTrue(engine.SetPitch(voice, 2f, seconds: 0.1));
        int start = 6 * StepFrames, ramp = (int)(0.1 * Rate);
        int steps = 0;
        while (engine.IsPlaying(voice)) { Step(engine); steps++; }
        Step(engine, 2);
        // Frame k of the glide plays at 1 + (k + 1) / ramp, so after k frames the sound has moved the sum of those speeds.
        for (int k = 0; k < ramp; k += 97)
        {
            double position = start + k + (double)k * (k + 1) / (2 * ramp);
            Assert.AreEqual(position / frames, Left(sink, start + k), 1e-4, $"frame {k} of the glide");
        }
        double afterRamp = start + ramp + (double)ramp * (ramp + 1) / (2 * ramp);
        Assert.AreEqual((afterRamp + 2 * 1000) / frames, Left(sink, start + ramp + 1000), 1e-4, "then twice the speed");
        long last = sink.Samples.Count / 2 - 1;
        while (Left(sink, last) == 0) last--;
        long expectedEnd = start + ramp + (long)Math.Ceiling((frames - afterRamp) / 2);
        Assert.AreEqual(expectedEnd, last + 1, 2, "where the glide and the doubled speed put its end");
        Assert.AreEqual((double)expectedEnd / StepFrames, 6 + steps, 1.01, "and the timeline frees the voice at that step, without asking the sink");
    }

    [TestMethod]
    public void A_bus_volume_fades_frame_by_frame_and_a_plain_volume_ends_the_fade()
    {
        string? reference = null;
        foreach (int block in new[] { 64, 441, 1024, 4096 })
        {
            var (engine, sink) = Open(block, delayFrames: 200);
            var ambience = engine.AddBus("ambience", AudioEngine.SfxBus);
            engine.Bus(AudioEngine.SfxBus).Volume = 0.5f;
            engine.Play(Constant(0.8f, 3), new PlayParameters { Bus = "ambience" });
            Step(engine, 3);
            ambience.FadeTo(0.25f, 0.05f);
            Assert.AreEqual(0.25f, ambience.Volume, "the bus reads its new volume at once");
            Step(engine, 6);
            int start = 3 * StepFrames + 200, fade = 2400;
            Assert.AreEqual(0.4f, Left(sink, start - 1), $"block {block}: untouched before");
            for (int k = 0; k < fade; k += 53)
                Assert.AreEqual(0.8f * (1 + (0.25f - 1) * (k + 1) / fade) * 0.5f, Left(sink, start + k), Tolerance, $"block {block}, frame {k} of the fade");
            Assert.AreEqual(0.8f * 0.25f * 0.5f, Left(sink, start + fade + 10), 1e-7f, "it ends on the volume");
            ambience.FadeTo(1f, 1f);          // step 9
            Step(engine, 3);
            ambience.Volume = 0.5f;           // step 12: at once, and the fade is over
            Step(engine, 3);
            int cut = 12 * StepFrames + 200;
            Assert.AreEqual(0.8f * (0.25f + 0.75f * 3 * StepFrames / Rate) * 0.5f, Left(sink, cut - 1), Tolerance, "the second fade so far");
            Assert.AreEqual(0.8f * 0.5f * 0.5f, Left(sink, cut), 1e-7f, "then the plain volume");
            Assert.AreEqual(0.8f * 0.5f * 0.5f, Left(sink, cut + 2000), 1e-7f);
            string hash = AudioGraphBaselineTests.Hash(sink.Samples);
            reference ??= hash;
            Assert.AreEqual(reference, hash, $"block {block}");
        }
    }

    [TestMethod]
    public void Fading_master_fades_everything()
    {
        var (engine, sink) = Open();
        engine.Play(Constant(0.5f, 1), new PlayParameters());
        engine.Play(Constant(0.25f, 1), new PlayParameters { Bus = AudioEngine.MusicBus });
        Step(engine);
        engine.Bus(AudioEngine.MasterBus).FadeTo(0, 0.1f);
        Step(engine, 12);
        Assert.AreEqual(0.75f, Left(sink, StepFrames - 1));
        Assert.AreEqual(0.75f * 0.5f, Left(sink, StepFrames + 2400 - 1), Tolerance, "half way");
        Assert.AreEqual(0f, Left(sink, StepFrames + 4800 + 5));
    }

    private static (AudioMixer Audio, CaptureAudioDevice Device) Game()
    {
        var device = new CaptureAudioDevice(Steps, Rate);
        return (new AudioMixer(device) { Deterministic = true }, device);
    }

    [TestMethod]
    public void Music_crossfades_the_new_track_rising_while_the_old_one_falls()
    {
        var (audio, device) = Game();
        using var _ = device;
        audio.AddMusic("town", Constant(0.4f, 10));
        audio.AddMusic("battle", Constant(0.2f, 10));
        audio.PlayMusic("town", fadeSeconds: 0);
        for (int i = 0; i < 30; i++) audio.Update(1f / Steps);
        audio.PlayMusic("battle", crossfade: 0.5f);
        Assert.AreEqual("battle", audio.CurrentMusic, "the new track is the music from that update on");
        for (int i = 0; i < 60; i++) audio.Update(1f / Steps);
        var samples = device.Output.Samples;
        int start = 30 * StepFrames, fade = Rate / 2;
        Assert.AreEqual(0.4f, samples[(start - 1) * 2], "only the old track before");
        foreach (int k in new[] { 0, 1000, fade / 2, fade - 2 })
        {
            float t = (k + 1f) / fade;
            Assert.AreEqual(0.4f * (1 - (float)k / fade) + 0.2f * t, samples[(start + k) * 2], 1e-4f, $"frame {k}: both tracks, one falling and one rising");
        }
        Assert.AreEqual(0.2f, samples[(start + fade + 100) * 2], "only the new track after");
        Assert.AreEqual(1, device.Engine.ActiveVoices, "the old track's voice is free again");
    }

    [TestMethod]
    public void Without_a_crossfade_music_switches_as_it_always_did_and_a_silent_device_just_switches()
    {
        var (audio, device) = Game();
        using var _ = device;
        audio.AddMusic("town", Constant(0.4f, 10));
        audio.AddMusic("battle", Constant(0.2f, 10));
        audio.PlayMusic("town", fadeSeconds: 0);
        audio.Update(1f / Steps);
        audio.PlayMusic("battle");
        Assert.AreEqual("town", audio.CurrentMusic, "by default the old track fades out first");
        for (int i = 0; i < 40; i++) audio.Update(1f / Steps);
        Assert.AreEqual("battle", audio.CurrentMusic);

        var silent = new AudioMixer(new SilentAudioDevice());
        silent.AddMusic("town", Constant(0.4f, 1));
        silent.AddMusic("battle", Constant(0.2f, 1));
        silent.PlayMusic("town", fadeSeconds: 0);
        silent.PlayMusic("battle", crossfade: 1);
        for (int i = 0; i < 40; i++) silent.Update(1f / Steps);
        Assert.AreEqual("battle", silent.CurrentMusic, "no voices to overlap: the music still changes");
    }

    [TestMethod]
    public void A_game_ramps_a_voice_through_its_mixer()
    {
        var (audio, device) = Game();
        using var _ = device;
        audio.AddSound("hum", Constant(1f, 2));
        var voice = audio.PlayVoice("hum", new PlayParameters { Volume = 0.5f, FadeIn = 0.1f });
        for (int i = 0; i < 12; i++) audio.Update(1f / Steps);
        Assert.IsTrue(audio.Set(voice, volume: 1f, pan: 0f, seconds: 0.1f));
        for (int i = 0; i < 12; i++) audio.Update(1f / Steps);
        var samples = device.Output.Samples;
        Assert.AreEqual(0.25f, samples[(2400 - 1) * 2], 1e-4f, "half way up the fade-in");
        Assert.AreEqual(0.5f, samples[(4800 + 100) * 2], "at its volume");
        Assert.AreEqual(0.75f, samples[(12 * StepFrames + 2400 - 1) * 2], 1e-4f, "half way up the ramp, on the left");
        Assert.AreEqual(0.75f * 0.5f, samples[(12 * StepFrames + 2400 - 1) * 2 + 1], 1e-4f, "while the right side falls as the pan goes left");
        Assert.AreEqual(0f, samples[(24 * StepFrames - 1) * 2 + 1]);
    }
}
