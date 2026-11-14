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

    // ---- Several changes to one sound in one step

    /// <summary>
    /// Sequences that change one sound more than once in a step. Each plays a six-step ramp, so a sound that paused or
    /// resumed at another frame than its step's is at another point of the ramp for the rest of the run.
    /// </summary>
    private static readonly (string Name, Action<AudioEngine> Run)[] SameStep =
    [
        ("pause then volume", e => { var v = e.Play(Ramp(6 * StepFrames), new PlayParameters()); Step(e); e.Pause(v, true); e.Set(v, volume: 0.5f); Step(e, 2); e.Pause(v, false); Step(e, 7); }),
        ("volume then pause", e => { var v = e.Play(Ramp(6 * StepFrames), new PlayParameters()); Step(e); e.Set(v, volume: 0.5f); e.Pause(v, true); Step(e, 2); e.Pause(v, false); Step(e, 7); }),
        ("pause then resume", e => { var v = e.Play(Ramp(6 * StepFrames), new PlayParameters()); Step(e); e.Pause(v, true); e.Pause(v, false); Step(e, 7); }),
        ("resume then volume", e => { var v = e.Play(Ramp(6 * StepFrames), new PlayParameters()); Step(e); e.Pause(v, true); Step(e, 2); e.Pause(v, false); e.Set(v, volume: 0.3f); Step(e, 7); }),
        ("resume then pan and pitch", e => { var v = e.Play(Ramp(6 * StepFrames), new PlayParameters()); Step(e); e.Pause(v, true); Step(e, 2); e.Pause(v, false); e.Set(v, pan: 0.2f); e.SetPitch(v, 2f); Step(e, 7); }),
        ("pause, pitch and volume, then resume in a later step", e => { var v = e.Play(Ramp(6 * StepFrames), new PlayParameters()); Step(e); e.Pause(v, true); e.SetPitch(v, 0.5f); e.Set(v, volume: 0.25f); Step(e); e.Pause(v, false); Step(e, 12); }),
        ("volume twice", e => { var v = e.Play(Ramp(6 * StepFrames), new PlayParameters()); Step(e); e.Set(v, volume: 0.2f); e.Set(v, volume: 0.8f); Step(e, 7); }),
        ("resume then stop with a fade", e => { var v = e.Play(Ramp(6 * StepFrames), new PlayParameters()); Step(e); e.Pause(v, true); Step(e); e.Pause(v, false); e.Stop(v, 2.0 / Steps); Step(e, 5); }),
    ];

    [TestMethod]
    [DataRow(100)]
    [DataRow(512)]
    [DataRow(StepFrames)]
    public void Changes_to_one_sound_in_one_step_sound_the_same_whatever_the_delay(int delayFrames)
    {
        foreach (var (name, run) in SameStep)
            AssertSame(Delayed(Render(0, run), delayFrames), Render(delayFrames, run), $"{name}: the run without delay, {delayFrames} frames later");
    }

    [TestMethod]
    public void Pausing_and_changing_a_sound_in_one_step_pauses_it_at_the_steps_frame()
    {
        const int Delay = 300, Frames = 6 * StepFrames;
        var (engine, sink) = Open(delayFrames: Delay);
        var voice = engine.Play(Ramp(Frames), new PlayParameters());
        Step(engine);
        engine.Pause(voice, true);
        engine.Set(voice, volume: 0.5f);
        Step(engine, 3);
        engine.Pause(voice, false);
        Step(engine, 3);
        float At(int clipFrame) => 0.1f + 0.8f * clipFrame / Frames;
        // The sound started at the delay, so one step of it was heard before the frame of the step that paused it.
        int paused = StepFrames + Delay, resumed = 4 * StepFrames + Delay;
        Assert.AreEqual(At(StepFrames - 1), sink.Samples[(paused - 1) * 2], 1e-6f, "playing at full volume up to the step's frame");
        for (int f = paused; f < resumed; f += 97) Assert.AreEqual(0f, sink.Samples[f * 2], $"silent while paused (frame {f})");
        Assert.AreEqual(At(StepFrames) * 0.5f, sink.Samples[resumed * 2], 1e-6f, "it goes on from the frame it paused at, at the new volume");
        Assert.IsTrue(engine.IsPlaying(voice));
    }

    [TestMethod]
    public void Pausing_and_resuming_a_sound_in_one_step_changes_nothing()
    {
        void Run(AudioEngine e, bool call)
        {
            var voice = e.Play(Ramp(3 * StepFrames), new PlayParameters());
            Step(e);
            if (call)
            {
                e.Pause(voice, true);
                e.Pause(voice, false);
            }
            Step(e, 2);
            Assert.IsFalse(e.IsPlaying(voice), "three steps of sound ended after three steps");
            Step(e, 2);
        }
        var expected = Render(300, e => Run(e, false));
        var actual = Render(300, e => Run(e, true));
        AssertSame(expected, actual, "no gap and no shift");
        Assert.AreEqual(300 + 3 * StepFrames - 1, LastAudibleFrame(actual), "the sound ends where the mixer's timeline says");
    }

    [TestMethod]
    public void Resuming_and_changing_a_sound_in_one_step_resumes_it_at_the_steps_frame()
    {
        const int Delay = 300, Frames = 6 * StepFrames;
        var (engine, sink) = Open(delayFrames: Delay);
        var voice = engine.Play(Ramp(Frames), new PlayParameters());
        Step(engine);
        engine.Pause(voice, true);
        Step(engine, 2);
        engine.Pause(voice, false);
        engine.Set(voice, volume: 0.25f, pan: 1f);
        Step(engine, 6);
        float At(int clipFrame) => 0.1f + 0.8f * clipFrame / Frames;
        int resumed = 3 * StepFrames + Delay;
        Assert.AreEqual(0f, sink.Samples[(resumed - 1) * 2 + 1], "still paused just before the step's frame");
        Assert.AreEqual(0f, sink.Samples[resumed * 2], "panned right from that frame");
        Assert.AreEqual(At(StepFrames) * 0.25f, sink.Samples[resumed * 2 + 1], 1e-6f, "resumed where it paused, at the new volume");
        // One step was heard before the pause: five are left, so the sound ends five steps after it resumed.
        Assert.AreEqual(resumed + 5 * StepFrames - 1, LastAudibleFrame([.. sink.Samples]));
        Assert.IsFalse(engine.IsPlaying(voice), "and the mixer's timeline ended it there");
    }

    // ---- Handles whose voice another sound took

    /// <summary>Every call a handle takes; true when any of them did something.</summary>
    private static bool Poke(AudioEngine engine, VoiceHandle voice) =>
        engine.Set(voice, volume: 0f, pan: 0f) | engine.SetPitch(voice, 4f) | engine.Pause(voice, true) | engine.Stop(voice, 0.5) | engine.Stop(voice) | engine.IsPlaying(voice);

    [TestMethod]
    [DataRow("stopped")]
    [DataRow("stolen")]
    [DataRow("ended")]
    [DataRow("ended by a pitch change")]
    public void A_handle_whose_voice_another_sound_took_does_nothing_to_that_sound(string how)
    {
        void Run(AudioEngine e, bool poke)
        {
            var old = e.Play(Ramp(how == "ended" ? 2 * StepFrames : 10 * StepFrames), new PlayParameters());
            Step(e, 2);
            if (how == "stopped") e.Stop(old);
            if (how == "ended by a pitch change") e.SetPitch(old, 100f);
            if (how is "ended" or "ended by a pitch change") Step(e, 2);
            var taker = e.Play(Constant(0.5f, 6 * StepFrames), new PlayParameters { Loop = how == "ended" });
            Assert.AreEqual(old.Index, taker.Index, "the one voice, taken by the new sound");
            Assert.AreNotEqual(old.Generation, taker.Generation);
            if (poke) Assert.IsFalse(Poke(e, old), "in the step that took the voice");
            Step(e, 2);
            if (poke) Assert.IsFalse(Poke(e, old), "later");
            Assert.IsTrue(e.IsPlaying(taker));
            Step(e, 5);
        }
        foreach (int delay in new[] { 0, 300 })
            AssertSame(Render(delay, e => Run(e, false), maxVoices: 1), Render(delay, e => Run(e, true), maxVoices: 1), $"delay {delay}: the same sound as without the stale calls");
    }

    [TestMethod]
    public void Handles_that_never_were_a_voice_do_nothing()
    {
        var (engine, _) = Open(maxVoices: 2, delayFrames: 300);
        var voice = engine.Play(Ramp(8 * StepFrames), new PlayParameters());
        Step(engine);
        Assert.IsFalse(Poke(engine, VoiceHandle.None));
        Assert.IsFalse(Poke(engine, new VoiceHandle(voice.Index, voice.Generation + 1)), "a generation the voice has not reached");
        Assert.IsFalse(Poke(engine, new VoiceHandle(99, 1)), "a voice beyond the limit");
        Assert.IsFalse(Poke(engine, new VoiceHandle(-1, 1)));
        Assert.IsTrue(engine.IsPlaying(voice));
    }

    [TestMethod]
    public void The_games_mixer_ignores_a_handle_whose_voice_another_sound_took()
    {
        var sink = new OfflineAudioSink(Stereo, Steps, 256, latencyFrames: 300);
        var mixer = new AudioMixer(new MixerAudioDevice(sink, new AudioEngineSettings { UpdateRate = Steps, MaxVoices = 1 })) { Deterministic = true };
        mixer.AddSound("old", Ramp(10 * StepFrames));
        mixer.AddSound("new", Constant(0.5f, 6 * StepFrames));
        var old = mixer.PlayVoice("old", new PlayParameters());
        mixer.Update(1f / Steps);
        mixer.Pump();
        var taker = mixer.PlayVoice("new", new PlayParameters { Loop = true });
        Assert.IsFalse(mixer.Set(old, volume: 0, pan: 0, pitch: 3) | mixer.Stop(old) | mixer.Pause(old, true) | mixer.IsPlaying(old));
        for (int i = 0; i < 3; i++) { mixer.Update(1f / Steps); mixer.Pump(); }
        Assert.IsTrue(mixer.IsPlaying(taker));
        Assert.AreEqual(0.5f, sink.Samples[(3 * StepFrames + 5) * 2], "the new sound plays untouched");
    }

    // ---- Pitch in a clip's last frames

    [TestMethod]
    [DataRow(0.01f)]
    [DataRow(1f)]
    [DataRow(100f)]
    public void A_pitch_change_in_a_clips_last_frames_ends_the_sound_where_the_timeline_says(float pitch)
    {
        // Three frames of the clip are left when the pitch changes.
        var (engine, sink) = Open(maxVoices: 1, blockFrames: 5);
        var voice = engine.Play(Constant(0.5f, StepFrames + 3), new PlayParameters());
        Step(engine);
        Assert.IsTrue(engine.SetPitch(voice, pitch));
        int steps = 1;
        while (engine.IsPlaying(voice) && steps < 10) { Step(engine); steps++; }
        Step(engine, 2);
        var samples = sink.Samples.ToArray();
        int last = LastAudibleFrame(samples);
        // The clip's last three frames last 3 / pitch output frames, give or take the rounding of the pitch itself.
        int expected = StepFrames + (int)Math.Ceiling(3 / pitch) - 1;
        Assert.IsTrue(Math.Abs(last - expected) <= 1, $"the last audible frame is {last}, expected about {expected}");
        Assert.AreEqual(last / StepFrames + 1, steps, "the handle ended in the step in which the sound did");
        Assert.AreEqual(0.5f, samples.Max(Math.Abs), "no sample beyond the clip's");
        Assert.IsFalse(engine.SetPitch(voice, 1f), "the handle is stale once the sound ended");
    }

    [TestMethod]
    public void Pitch_changes_at_the_tail_of_short_and_looping_clips_stay_inside_the_clip()
    {
        foreach (int channels in new[] { 1, 2 })
            foreach (int length in new[] { 1, 2, 3, 801 })
                foreach (bool loop in new[] { false, true })
                {
                    var (engine, sink) = Open(blockFrames: 5, delayFrames: 7);
                    var clip = new AudioClip([.. Enumerable.Repeat(0.5f, length * channels)], 96000, channels);
                    var voice = engine.Play(clip, new PlayParameters { Loop = loop, Pitch = 0.5f });
                    for (int i = 0; i < 6; i++)
                    {
                        Step(engine);
                        engine.SetPitch(voice, i % 2 == 0 ? 100f : 0.01f);
                        engine.SetPitch(voice, 37.3f);
                    }
                    Step(engine, 3);
                    string where = $"{channels} channels, {length} frames, loop {loop}";
                    Assert.AreEqual(loop, engine.IsPlaying(voice), where);
                    Assert.IsTrue(sink.Samples.All(s => s is >= 0f and <= 0.5f), $"{where}: every sample is between silence and the clip's level");
                    Assert.AreEqual(loop ? 0.5f : 0f, sink.Samples[^1], $"{where}: a loop goes on, anything else ended");
                }
    }
}
