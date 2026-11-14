using MicroEngine.Audio;
using MicroEngine.Platform;
using MicroEngine.Testing;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// The one managed mixer over <see cref="IAudioSink"/>: exact-frame scheduling, one voice policy (limits, priorities,
/// stealing) whatever the sink, buses, fades, music streams decoded ahead outside the simulation step, and output that
/// does not depend on the block sizes a sink asks for. Rendered offline, compared with references computed here.
/// </summary>
/// <remarks>
/// Declared tolerances: 0 (bit-exact) where the mixer copies samples times exact gains; 1e-6 where it interpolates
/// (linear resampling computes the same expression in another order than the reference), and for fades.
/// </remarks>
[TestClass]
public sealed class AudioEngineTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;
    private static readonly AudioFormat Stereo = new(Rate, 2);

    private static AudioClip Constant(float value, int frames, int rate = Rate) => new([.. Enumerable.Repeat(value, frames)], rate);

    private static (AudioEngine Engine, OfflineAudioSink Sink) Open(int maxVoices = 32, int blockFrames = 1024, int delayFrames = 0)
    {
        var sink = new OfflineAudioSink(Stereo, Steps, blockFrames);
        var engine = new AudioEngine(sink, new AudioEngineSettings { MaxVoices = maxVoices, UpdateRate = Steps, DelayFrames = delayFrames });
        return (engine, sink);
    }

    /// <summary>One simulation step: the game's plays happened, the step ends, the sink renders it.</summary>
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
    public void A_sound_starts_at_the_exact_frame_of_its_step_plus_the_delay()
    {
        var (engine, sink) = Open(delayFrames: 100);
        Step(engine, 3);
        var voice = engine.Play(Constant(0.5f, 10), new PlayParameters());
        Assert.IsFalse(voice.IsNone);
        Step(engine, 2);
        long start = 3 * StepFrames + 100;
        Assert.AreEqual(5 * StepFrames, sink.Frames, "one step per pump");
        Assert.AreEqual(0f, Left(sink, start - 1));
        for (int f = 0; f < 10; f++)
        {
            Assert.AreEqual(0.5f, Left(sink, start + f), $"frame {f}: copied at full volume, centred");
            Assert.AreEqual(0.5f, Right(sink, start + f));
        }
        Assert.AreEqual(0f, Left(sink, start + 10), "and nothing after its end");
        Assert.IsFalse(engine.IsPlaying(voice), "the mixer knows when it ended, without asking the sink");
    }

    [TestMethod]
    public void A_delay_shorter_than_the_sinks_latency_is_raised_to_it()
    {
        var sink = new OfflineAudioSink(Stereo, Steps, latencyFrames: 512);
        var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = 10 });
        Assert.AreEqual(512, engine.DelayFrames);
    }

    [TestMethod]
    public void Output_is_the_same_whatever_block_sizes_the_sink_asks_for()
    {
        string? reference = null;
        foreach (int block in new[] { 64, 128, 441, 800, 1024, 4096 })
        {
            var (engine, sink) = Open(maxVoices: 4, blockFrames: block, delayFrames: StepFrames);
            var rng = new Random(3);
            for (int step = 0; step < 90; step++)
            {
                if (step % 4 == 0)
                    engine.Play(Constant((float)rng.NextDouble() - 0.5f, 300 + rng.Next(4000), Rate / 2), new PlayParameters { Pitch = 0.8f + (float)rng.NextDouble() * 0.6f, Pan = (float)rng.NextDouble() });
                Step(engine);
            }
            var hash = Hash(sink.Samples);
            reference ??= hash;
            Assert.AreEqual(reference, hash, $"block {block}");
        }
    }

    private static string Hash(IReadOnlyList<float> samples)
    {
        ulong h = 14695981039346656037;
        foreach (var s in samples) h = (h ^ BitConverter.SingleToUInt32Bits(s)) * 1099511628211;
        return h.ToString("x16");
    }

    [TestMethod]
    public void The_voice_limit_steals_the_oldest_sound_of_the_lowest_priority()
    {
        var (engine, sink) = Open(maxVoices: 3);
        var first = engine.Play(Constant(0.1f, Rate), new PlayParameters());
        Step(engine);
        var second = engine.Play(Constant(0.01f, Rate), new PlayParameters());
        Step(engine);
        var third = engine.Play(Constant(0.001f, Rate), new PlayParameters());
        Step(engine);
        Assert.AreEqual(3, engine.ActiveVoices);
        var fourth = engine.Play(Constant(0.0001f, Rate), new PlayParameters());
        Assert.IsFalse(fourth.IsNone);
        Assert.IsFalse(engine.IsPlaying(first), "the oldest was stolen");
        Assert.IsTrue(engine.IsPlaying(second) && engine.IsPlaying(third) && engine.IsPlaying(fourth));
        Assert.AreEqual(3, engine.ActiveVoices);
        Assert.IsFalse(engine.Stop(first), "a stolen sound's handle is stale");
        Step(engine);
        Assert.AreEqual(0.0111f, Left(sink, 3 * StepFrames + 5), 1e-7f, "the first sound is gone from the output from that step on");
        Assert.AreEqual(0.111f, Left(sink, 2 * StepFrames + 5), 1e-7f, "it played until then");
    }

    [TestMethod]
    public void A_lower_priority_sound_is_refused_and_a_higher_one_steals()
    {
        var (engine, _) = Open(maxVoices: 2);
        var music = engine.Play(Constant(0.1f, Rate), new PlayParameters { Priority = 10 });
        var jingle = engine.Play(Constant(0.1f, Rate), new PlayParameters { Priority = 5 });
        Step(engine);
        Assert.IsTrue(engine.Play(Constant(0.1f, 10), new PlayParameters { Priority = 1 }).IsNone, "nothing lower to steal: refused");
        Assert.AreEqual(1, engine.RefusedSounds);
        var alarm = engine.Play(Constant(0.1f, 10), new PlayParameters { Priority = 7 });
        Assert.IsFalse(alarm.IsNone);
        Assert.IsTrue(engine.IsPlaying(music), "the highest priority stays");
        Assert.IsFalse(engine.IsPlaying(jingle), "the lowest was stolen");
        var equal = engine.Play(Constant(0.1f, 10), new PlayParameters { Priority = 7 });
        Assert.IsFalse(equal.IsNone, "an equal priority steals the oldest of that priority");
        Assert.IsFalse(engine.IsPlaying(alarm));
    }

    [TestMethod]
    public void A_finished_sound_frees_its_voice_at_its_logical_end()
    {
        var (engine, _) = Open(maxVoices: 1);
        var shortSound = engine.Play(Constant(0.1f, StepFrames / 2), new PlayParameters());
        Step(engine);
        Assert.IsFalse(engine.IsPlaying(shortSound));
        Assert.AreEqual(0, engine.ActiveVoices);
        var slow = engine.Play(Constant(0.1f, StepFrames), new PlayParameters { Pitch = 0.5f });
        Step(engine);
        Assert.IsTrue(engine.IsPlaying(slow), "half speed lasts two steps");
        Step(engine);
        Assert.IsFalse(engine.IsPlaying(slow));
        var looping = engine.Play(Constant(0.1f, 10), new PlayParameters { Loop = true });
        Step(engine, 10);
        Assert.IsTrue(engine.IsPlaying(looping), "a loop never ends by itself");
    }

    [TestMethod]
    public void Pan_volume_and_resampling_follow_the_reference()
    {
        var (engine, sink) = Open();
        var ramp = new AudioClip([.. Enumerable.Range(0, 200).Select(i => i / 200f)], Rate / 2);
        engine.Play(ramp, new PlayParameters { Volume = 0.5f, Pan = 0.25f });
        Step(engine);
        float left = 1, right = 0.5f; // balance pan at 0.25: left full, right halved
        for (int f = 0; f < 398; f++)
        {
            double position = f * 0.5;
            int i = (int)position;
            float t = (float)(position - i);
            float reference = ramp.Samples[i] + (ramp.Samples[Math.Min(i + 1, 199)] - ramp.Samples[i]) * t;
            Assert.AreEqual(reference * 0.5f * left, Left(sink, f), 1e-6f, $"left {f}");
            Assert.AreEqual(reference * 0.5f * right, Right(sink, f), 1e-6f, $"right {f}");
        }
    }

    [TestMethod]
    public void Buses_scale_their_sounds_and_nest_without_cycles()
    {
        var (engine, sink) = Open();
        var ui = engine.AddBus("ui", "sfx");
        engine.Bus("sfx").Volume = 0.5f;
        ui.Volume = 0.5f;
        Assert.AreEqual("sfx", ui.Parent!.Name);
        engine.Play(Constant(1f, 10), new PlayParameters { Bus = "ui" });
        engine.Play(Constant(1f, 10), new PlayParameters { Bus = "music" });
        Step(engine);
        Assert.AreEqual(1.25f, Left(sink, 0), "0.25 through ui and sfx, 1 through music");
        engine.Bus("master").Volume = 0.5f;
        engine.Play(Constant(1f, 10), new PlayParameters());
        Step(engine);
        Assert.AreEqual(0.25f, Left(sink, StepFrames), "the master scales everything; sounds go to sfx by default");
        Assert.ThrowsExactly<ArgumentException>(() => engine.AddBus("ui", "master"), "names are unique");
        Assert.ThrowsExactly<KeyNotFoundException>(() => engine.AddBus("x", "none"), "a parent must exist first, so buses never form a cycle");
        Assert.ThrowsExactly<KeyNotFoundException>(() => engine.Play(Constant(1, 1), new PlayParameters { Bus = "none" }));
        CollectionAssert.AreEqual(new[] { "master", "music", "sfx", "ui" }, engine.Buses.Select(b => b.Name).Order(StringComparer.Ordinal).ToArray());
    }

    [TestMethod]
    public void Stopping_with_a_fade_ramps_to_silence_then_frees_the_voice()
    {
        var (engine, sink) = Open(maxVoices: 1);
        var voice = engine.Play(Constant(1f, Rate), new PlayParameters());
        Step(engine);
        Assert.IsTrue(engine.Stop(voice, fadeSeconds: (double)StepFrames / Rate));
        Assert.IsTrue(engine.IsPlaying(voice), "still fading");
        Step(engine);
        Assert.IsFalse(engine.IsPlaying(voice));
        for (int f = 0; f < StepFrames; f++)
            Assert.AreEqual(1f - (float)f / StepFrames, Left(sink, StepFrames + f), 1e-6f, $"frame {f}");
        Step(engine);
        Assert.AreEqual(0f, Left(sink, 2 * StepFrames));
        Assert.IsFalse(engine.Stop(voice), "already ended");
    }

    [TestMethod]
    public void Volume_pan_and_pause_change_at_the_frame_of_their_step()
    {
        var (engine, sink) = Open();
        var voice = engine.Play(Constant(1f, Rate), new PlayParameters { Loop = true });
        Step(engine);
        Assert.IsTrue(engine.Set(voice, volume: 0.5f, pan: 1f));
        Step(engine);
        Assert.AreEqual((1f, 1f), (Left(sink, StepFrames - 1), Right(sink, StepFrames - 1)));
        Assert.AreEqual((0f, 0.5f), (Left(sink, StepFrames), Right(sink, StepFrames)));
        engine.Pause(voice, true);
        Step(engine);
        Assert.AreEqual(0f, Right(sink, 2 * StepFrames + 3), "paused");
        engine.Pause(voice, false);
        Step(engine);
        Assert.AreEqual(0.5f, Right(sink, 3 * StepFrames + 3), "resumed");
    }

    private static AudioClip Ramp(int frames) => new([.. Enumerable.Range(0, frames).Select(i => i / (float)frames)], Rate);

    [TestMethod]
    public void Pitch_changes_at_the_frame_of_its_step_and_moves_the_sounds_end()
    {
        var (engine, sink) = Open(maxVoices: 1);
        int frames = 4 * StepFrames;
        var voice = engine.Play(Ramp(frames), new PlayParameters());
        Step(engine);
        Assert.IsTrue(engine.SetPitch(voice, 2f));
        Step(engine);
        Assert.AreEqual((StepFrames - 1) / (float)frames, Left(sink, StepFrames - 1), 1e-6f, "the original pitch until the step that changed it");
        for (int k = 0; k < StepFrames; k += 37)
            Assert.AreEqual((StepFrames + 2 * k) / (float)frames, Left(sink, StepFrames + k), 1e-6f, $"twice as fast from that frame on ({k})");
        // One step at pitch 1 and one at pitch 2 played three of its four steps; the last one takes half a step.
        Assert.IsTrue(engine.IsPlaying(voice));
        Step(engine);
        Assert.IsFalse(engine.IsPlaying(voice), "the mixer's timeline knows the new end");
        Assert.AreEqual(0f, Left(sink, 2 * StepFrames + StepFrames / 2 + 1), "and the sound ended there");
        Assert.IsFalse(engine.SetPitch(voice, 1f), "a stale handle does nothing");
    }

    [TestMethod]
    public void Slowing_a_sound_down_keeps_its_voice_until_its_later_end()
    {
        var (engine, _) = Open(maxVoices: 1);
        var voice = engine.Play(Constant(0.1f, 2 * StepFrames), new PlayParameters());
        Step(engine);
        engine.SetPitch(voice, 0.5f);
        Step(engine);
        Assert.IsTrue(engine.IsPlaying(voice), "the remaining step lasts two at half speed");
        Assert.IsTrue(engine.Play(Constant(0.1f, 10), new PlayParameters { Priority = -1 }).IsNone, "so its voice is still taken");
        Step(engine);
        Assert.IsFalse(engine.IsPlaying(voice));
    }

    [TestMethod]
    public void Pitch_changes_while_paused_apply_to_what_is_left()
    {
        var (engine, sink) = Open();
        int frames = 4 * StepFrames;
        var voice = engine.Play(Ramp(frames), new PlayParameters());
        Step(engine);
        engine.Pause(voice, true);
        Assert.IsTrue(engine.SetPitch(voice, 3f));
        Step(engine);
        Assert.IsTrue(engine.IsPlaying(voice), "paused sounds keep their voice");
        engine.Pause(voice, false);
        Step(engine);
        Assert.AreEqual((StepFrames + 3 * 10) / (float)frames, Left(sink, 2 * StepFrames + 10), 1e-6f, "resumed where it was, three times as fast");
        Assert.IsFalse(engine.IsPlaying(voice), "three steps of sound were left: one step at three times the speed");
    }

    [TestMethod]
    public void A_pitch_change_never_outlives_a_stop_that_is_fading()
    {
        var (engine, _) = Open(maxVoices: 1);
        var voice = engine.Play(Constant(0.1f, 10 * StepFrames), new PlayParameters());
        Step(engine);
        engine.Stop(voice, fadeSeconds: 2.0 / Steps);
        engine.SetPitch(voice, 0.5f);
        Step(engine);
        Assert.IsTrue(engine.IsPlaying(voice), "still fading");
        Step(engine);
        Assert.IsFalse(engine.IsPlaying(voice), "the fade ends the sound, however slow it plays");
    }

    [TestMethod]
    public void Pitch_is_clamped_and_loops_and_streams_keep_playing()
    {
        var (engine, sink) = Open();
        var loop = engine.Play(Constant(0.25f, 10), new PlayParameters { Loop = true });
        Assert.IsTrue(engine.SetPitch(loop, 0f), "clamped to 0.01, as when a sound starts");
        Step(engine, 3);
        Assert.IsTrue(engine.IsPlaying(loop));
        Assert.AreEqual(0.25f, Left(sink, 3 * StepFrames - 1));
        var stream = engine.Play(new CountingStream(Rate, 0.5f), new PlayParameters { Bus = "music" });
        Assert.IsFalse(engine.SetPitch(stream, 2f), "streams play at their own pitch");
        Assert.IsTrue(engine.IsPlaying(stream));
    }

    [TestMethod]
    public void Changes_of_one_step_take_effect_together_at_its_frame()
    {
        var (engine, sink) = Open(delayFrames: 100);
        int frames = 4 * StepFrames;
        var voice = engine.Play(Ramp(frames), new PlayParameters());
        Step(engine);
        engine.Set(voice, volume: 0.5f);
        engine.SetPitch(voice, 2f);
        engine.Set(voice, pan: 1f);
        Step(engine, 2);
        // The step's frame is one step plus the delay; the sound itself started at the delay.
        int at = StepFrames + 100, played = StepFrames;
        Assert.AreEqual((played - 1) / (float)frames, Left(sink, at - 1), 1e-6f, "unchanged until that frame: full volume, centred, original pitch");
        Assert.AreEqual((played - 1) / (float)frames, Right(sink, at - 1), 1e-6f);
        Assert.AreEqual(0f, Left(sink, at + 5), "panned right from it");
        Assert.AreEqual((played + 2 * 5) / (float)frames * 0.5f, Right(sink, at + 5), 1e-6f, "at half volume and twice the speed");
    }

    [TestMethod]
    public void Live_control_commands_render_without_garbage()
    {
        var (engine, _) = Open(maxVoices: 4);
        var voice = engine.Play(Constant(0.01f, Rate * 4), new PlayParameters { Loop = true });
        Step(engine, 5);
        var buffer = new float[512];
        int n = 0;
        AllocationAssert.None(() =>
        {
            engine.SetPitch(voice, 0.5f + n++ % 10 * 0.1f);
            engine.Set(voice, volume: 0.5f, pan: n % 3 * 0.5f);
            engine.Render(buffer);
        }, calls: 50, because: "neither the game thread's calls nor the render that applies them make garbage");
    }

    /// <summary>A stream that records when it is read, to prove decoding never runs inside a step.</summary>
    private sealed class CountingStream(int frames, float value) : IAudioStream
    {
        private int _position;
        public int Channels => 1;
        public int SampleRate => Rate;
        public List<string> Reads { get; } = [];
        public string Phase { get; set; } = "";
        public int Read(Span<float> buffer)
        {
            Reads.Add(Phase);
            int n = Math.Min(buffer.Length, frames - _position);
            buffer[..n].Fill(value);
            _position += n;
            return n;
        }
        public void Rewind() => _position = 0;
        public void Dispose() { }
    }

    [TestMethod]
    public void Music_streams_play_side_by_side_and_decode_only_while_pumping()
    {
        var (engine, sink) = Open();
        var city = new CountingStream(Rate * 10, 0.25f);
        var drums = new CountingStream(Rate * 10, 0.125f);
        city.Phase = drums.Phase = "step";
        var a = engine.Play(city, new PlayParameters { Bus = "music", Loop = true });
        var b = engine.Play(drums, new PlayParameters { Bus = "music", Loop = true });
        engine.Advance();
        city.Phase = drums.Phase = "pump";
        engine.Pump();
        city.Phase = drums.Phase = "step";
        for (int i = 0; i < 5; i++)
        {
            engine.Advance();
            city.Phase = drums.Phase = "pump";
            engine.Pump();
            city.Phase = drums.Phase = "step";
        }
        Assert.IsTrue(city.Reads.Count > 0 && city.Reads.All(p => p == "pump"), string.Join(",", city.Reads));
        Assert.IsTrue(drums.Reads.All(p => p == "pump"));
        Assert.AreEqual(0.375f, Left(sink, 10), "both streams mixed");
        Assert.IsTrue(engine.IsPlaying(a) && engine.IsPlaying(b));
        engine.Stop(a);
        Step(engine);
        Assert.AreEqual(0.125f, Left(sink, sink.Frames - 1));
    }

    [TestMethod]
    public void A_stream_that_ends_frees_its_voice_and_a_looping_one_rewinds()
    {
        var (engine, sink) = Open();
        var once = new CountingStream(StepFrames / 2, 0.5f);
        var voice = engine.Play(once, new PlayParameters { Bus = "music" });
        Step(engine, 3);
        Assert.AreEqual(0.5f, Left(sink, 0));
        Assert.AreEqual(0f, Left(sink, StepFrames / 2 + 1));
        Assert.IsFalse(engine.IsPlaying(voice), "its end was seen while decoding ahead");
        var loop = new CountingStream(StepFrames / 2, 0.5f);
        engine.Play(loop, new PlayParameters { Bus = "music", Loop = true });
        Step(engine, 2);
        Assert.AreEqual(0.5f, Left(sink, 3 * StepFrames + StepFrames - 1), "rewound and played again");
    }

    [TestMethod]
    public void Rendering_allocates_nothing()
    {
        var (engine, sink) = Open(maxVoices: 16);
        for (int i = 0; i < 16; i++) engine.Play(Constant(0.01f, Rate * 4, Rate / 2), new PlayParameters { Pitch = 1.1f, Loop = true });
        engine.Play(new CountingStream(Rate * 100, 0.1f), new PlayParameters { Bus = "music", Loop = true });
        Step(engine, 30);
        var buffer = new float[2048];
        // Rendered on the test thread, as the sink's thread renders it: the count is this thread's alone.
        AllocationAssert.None(() => engine.Render(buffer), calls: 20, because: "the audio thread renders without garbage");
    }

    [TestMethod]
    public void Two_runs_of_the_same_steps_render_the_same_samples()
    {
        string Run()
        {
            var (engine, sink) = Open(maxVoices: 2, delayFrames: 300);
            for (int step = 0; step < 40; step++)
            {
                if (step % 3 == 0) engine.Play(Constant(0.1f * (step % 5), 2000), new PlayParameters { Priority = step % 2 });
                Step(engine);
            }
            return Hash(sink.Samples);
        }
        Assert.AreEqual(Run(), Run());
    }

    /// <summary>
    /// A sink with a thread of its own, as the SDL feeder: each <see cref="Render"/> call has that thread render
    /// <c>count</c> blocks in a loop, allocating between blocks when asked to, and returns when it is done.
    /// </summary>
    private sealed class ThreadSink(bool allocate) : IAudioSink
    {
        private readonly float[] _buffer = new float[512];
        private Thread? _thread;
        private int _pending;
        private volatile bool _stop;
        public object? Kept;
        public AudioFormat Format => Stereo;
        public AudioSinkSchedule Schedule => AudioSinkSchedule.SinkThread;
        public int LatencyFrames => 0;

        public void Start(AudioRender render)
        {
            _thread = new Thread(() =>
            {
                while (!_stop)
                {
                    int count = Volatile.Read(ref _pending);
                    if (count == 0) { Thread.Yield(); continue; }
                    for (int i = 0; i < count; i++)
                    {
                        render(_buffer);
                        if (allocate) Kept = new byte[100];
                    }
                    Volatile.Write(ref _pending, 0);
                }
            }) { IsBackground = true };
            _thread.Start();
        }

        public void Render(int count)
        {
            Volatile.Write(ref _pending, count);
            while (Volatile.Read(ref _pending) != 0) Thread.Yield();
        }

        public void Pump() { }

        public void Dispose()
        {
            _stop = true;
            _thread?.Join();
        }
    }

    /// <summary>The bytes the rendering thread allocated in each of five rounds of <paramref name="round"/>, after one warm-up round.</summary>
    private static long[] CountedRounds(AudioEngine engine, Action round)
    {
        round();
        var bytes = new long[5];
        for (int i = 0; i < bytes.Length; i++)
        {
            long before = engine.RenderAllocations.Bytes;
            round();
            bytes[i] = engine.RenderAllocations.Bytes - before;
        }
        return bytes;
    }

    [TestMethod]
    public void The_render_counter_counts_what_a_sink_thread_allocates_between_renders()
    {
        var sink = new ThreadSink(allocate: true);
        using var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps });
        sink.Render(100);
        Assert.AreEqual(100, engine.RenderAllocations.Renders);
        var bytes = CountedRounds(engine, () => sink.Render(50));
        Assert.AreEqual(100 + 6 * 50, engine.RenderAllocations.Renders, "every render is counted");
        // A byte[100] is 124 bytes on a 64-bit runtime; the 50 of a round are counted when the next render starts.
        Assert.IsTrue(bytes.All(b => b >= 49 * 120), $"the sink loop's garbage is counted (bytes per round: {string.Join(", ", bytes)})");
    }

    [TestMethod]
    public void A_sink_thread_that_renders_without_garbage_counts_no_bytes()
    {
        var sink = new ThreadSink(allocate: false);
        using var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps });
        for (int i = 0; i < 16; i++) engine.Play(Constant(0.01f, Rate, Rate / 2), new PlayParameters { Pitch = 1.1f, Loop = true });
        engine.Advance();
        // A background collection can count up to one allocation quantum against a thread once (see AllocationAssert).
        var bytes = CountedRounds(engine, () => sink.Render(100));
        Assert.AreEqual(0, bytes.Min(), $"bytes per round: {string.Join(", ", bytes)}");
    }

    [TestMethod]
    public void A_host_pumped_sink_counts_only_its_renders()
    {
        var (engine, sink) = Open();
        object? kept = null;
        var bytes = CountedRounds(engine, () =>
        {
            for (int i = 0; i < 10; i++)
            {
                Step(engine);
                kept = new byte[100]; // the host's frame, between renders on the same thread
            }
        });
        GC.KeepAlive(kept);
        Assert.AreEqual(0, bytes.Min(), $"the host's own garbage is not the mixer's (bytes per round: {string.Join(", ", bytes)})");
        Assert.AreEqual(60, engine.RenderAllocations.Renders, "one block of 800 frames per step");
    }
}
