using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// The mixer's timeline in real time, against a sink that keeps audio queued ahead of what is heard (as the SDL feeder
/// and the browser's worklet do): how soon after its update a sound is rendered, how evenly the sounds of consecutive
/// steps are spaced, and what a slow frame leaves behind. The sink here renders when the test says so, which stands for
/// the time that passed; deterministic runs follow the simulation only and are not affected.
/// </summary>
[TestClass]
public sealed class AudioEngineTimelineTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps, Queue = 3360;
    private static readonly AudioFormat Stereo = new(Rate, 2);

    /// <summary>A sink with <see cref="Queue"/> frames between what it renders and what is heard.</summary>
    private sealed class QueuedSink(int latencyFrames) : IAudioSink
    {
        private AudioRender? _render;
        private readonly float[] _block = new float[256 * 2];
        public AudioFormat Format => Stereo;
        public AudioSinkSchedule Schedule => AudioSinkSchedule.SinkThread;
        public int LatencyFrames => latencyFrames;
        public void Start(AudioRender render) => _render = render;
        public void Pump() { }
        public void Dispose() => _render = null;

        /// <summary>Time passes: the device plays <paramref name="frames"/> and the sink renders as many to keep its queue.</summary>
        public void Play(int frames)
        {
            for (; frames > 0; frames -= 256) _render!(_block.AsSpan(0, Math.Min(256, frames) * 2));
        }
    }

    private static (AudioEngine Engine, QueuedSink Sink) Open(bool deterministic = false)
    {
        var sink = new QueuedSink(Queue);
        var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps }) { Deterministic = deterministic };
        sink.Play(Queue); // the sink fills its queue as soon as it starts
        return (engine, sink);
    }

    private static readonly AudioClip Click = new(new float[16], Rate);

    /// <summary>
    /// One host frame: <paramref name="elapsedFrames"/> of audio went by, then the game runs <paramref name="steps"/>
    /// updates, each playing a sound. Returns, for each step, the frame its sound starts at and how far ahead of what the
    /// sink had rendered that was.
    /// </summary>
    private static List<(long Start, long Ahead)> Frame(AudioEngine engine, QueuedSink sink, int elapsedFrames = StepFrames, int steps = 1)
    {
        sink.Play(elapsedFrames);
        var played = new List<(long, long)>();
        for (int i = 0; i < steps; i++)
        {
            Assert.IsFalse(engine.Play(Click, new PlayParameters()).IsNone);
            played.Add((engine.Now, engine.Now - engine.RenderedFrames));
            engine.Advance();
        }
        engine.Pump();
        return played;
    }

    [TestMethod]
    public void In_real_time_a_sound_starts_within_a_step_of_what_the_sink_renders_next()
    {
        var (engine, sink) = Open();
        for (int i = 0; i < 120; i++) Frame(engine, sink);
        long previous = Frame(engine, sink)[0].Start;
        for (int i = 0; i < 120; i++)
        {
            var (start, ahead) = Frame(engine, sink)[0];
            Assert.IsTrue(ahead >= 0, $"frame {i}: never behind what is already rendered ({ahead})");
            Assert.IsTrue(ahead <= StepFrames, $"frame {i}: {ahead} frames after what the sink renders next; the sink's queue is not added twice");
            Assert.AreEqual(StepFrames, start - previous, $"frame {i}: consecutive steps are exactly one step apart");
            previous = start;
        }
    }

    [TestMethod]
    public void Two_steps_in_one_frame_start_their_sounds_one_step_apart()
    {
        var (engine, sink) = Open();
        for (int i = 0; i < 120; i++) Frame(engine, sink, 2 * StepFrames, steps: 2);
        long previous = Frame(engine, sink, 2 * StepFrames, steps: 2)[1].Start;
        for (int i = 0; i < 60; i++)
        {
            var played = Frame(engine, sink, 2 * StepFrames, steps: 2);
            Assert.IsTrue(played[0].Ahead is >= 0 and <= StepFrames, $"frame {i}: the first step's sound is {played[0].Ahead} frames ahead");
            Assert.AreEqual(StepFrames, played[0].Start - previous, $"frame {i}");
            Assert.AreEqual(StepFrames, played[1].Start - played[0].Start, $"frame {i}: the second step's sound is a step later, not at the same instant");
            previous = played[1].Start;
        }
    }

    [TestMethod]
    public void A_game_that_fell_behind_starts_its_sounds_at_what_the_sink_renders_next()
    {
        var (engine, sink) = Open();
        for (int i = 0; i < 60; i++) Frame(engine, sink);
        // A frame of 200 ms: the sink went on rendering; the step's first call finds its frame already rendered.
        var late = Frame(engine, sink, elapsedFrames: 12 * StepFrames)[0];
        Assert.AreEqual(0, late.Ahead, "it starts at the first frame the sink has not rendered");
    }

    [TestMethod]
    public void After_a_slow_frame_the_timeline_comes_back_a_little_each_frame()
    {
        var (engine, sink) = Open();
        for (int i = 0; i < 120; i++) Frame(engine, sink);
        // A frame of 100 ms, made up with five steps at once: their sounds are one step apart, the last four steps ahead.
        var burst = Frame(engine, sink, elapsedFrames: 6 * StepFrames, steps: 5);
        for (int i = 1; i < burst.Count; i++) Assert.AreEqual(StepFrames, burst[i].Start - burst[i - 1].Start, "the catch-up steps keep their spacing");
        long previous = burst[^1].Start, worst = 0;
        int back = -1;
        for (int i = 0; i < 240; i++)
        {
            var (start, ahead) = Frame(engine, sink)[0];
            long spacing = start - previous;
            Assert.IsTrue(spacing > 0, "the timeline never goes back");
            Assert.IsTrue(spacing <= StepFrames && spacing >= StepFrames - StepFrames / 16, $"frame {i}: a step is shortened by at most a sixteenth ({spacing} frames)");
            worst = Math.Max(worst, ahead);
            if (back < 0 && ahead <= StepFrames) back = i;
            if (back >= 0) Assert.IsTrue(ahead <= StepFrames, $"frame {i}: it stays back ({ahead})");
            previous = start;
        }
        Assert.IsTrue(worst > 2 * StepFrames, $"the slow frame did leave the timeline ahead ({worst} frames)");
        Assert.IsTrue(back is >= 0 and <= 120, $"and it is back within two seconds (after {back} frames)");
    }

    [TestMethod]
    public void A_sink_that_does_not_render_holds_the_timeline_within_a_quarter_second()
    {
        var (engine, sink) = Open();
        for (int i = 0; i < 60; i++) Frame(engine, sink);
        for (int i = 0; i < 300; i++)
        {
            var (_, ahead) = Frame(engine, sink, elapsedFrames: 0)[0];
            Assert.IsTrue(ahead <= Rate / 4 + StepFrames, $"step {i}: {ahead} frames ahead of a sink that stopped");
        }
    }

    [TestMethod]
    public void Deterministic_runs_follow_the_simulation_whatever_the_sink_rendered()
    {
        var (engine, sink) = Open(deterministic: true);
        for (int i = 0; i < 50; i++)
        {
            var played = Frame(engine, sink, elapsedFrames: i % 7 * 500, steps: 1 + i % 3);
            foreach (var (start, _) in played) Assert.AreEqual(0, (start - Queue) % StepFrames);
        }
        Assert.AreEqual(engine.StepFrame(engine.Steps) + Queue, engine.Now, "each step's frame plus the delay, nothing else");
    }
}
