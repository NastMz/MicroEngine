using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// An output that stops playing while the game goes on (a browser holding its audio suspended, a device that went
/// away), and plays again: what is heard when it does. The sink here renders when the test says so; while it is
/// stalled it renders nothing and the game keeps stepping. Muting, pausing and this suspension are three things: a
/// muted sound goes on unheard, a paused one holds its position, and an output that stopped is something nobody asked
/// for, so the sounds go on with the game and the output joins them where they are.
/// </summary>
[TestClass]
public sealed class AudioEngineSuspensionTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps, Queue = 3360;
    private static readonly AudioFormat Stereo = new(Rate, 2);

    /// <summary>A sink with <see cref="Queue"/> frames between what it renders and what is heard, which can stop.</summary>
    private sealed class StallingSink : IAudioSink
    {
        private AudioRender? _render;
        private readonly float[] _block = new float[Rate * 2];
        public readonly List<float> Left = [], Right = [];
        public bool Stalled;
        /// <summary>The frames it asks the mixer for at once.</summary>
        public int Block = 256;
        public AudioFormat Format => Stereo;
        // Rendered on the test's thread: counted as a pumped sink, so that the allocation count is the render function's
        // alone and not what the test allocates between two renders.
        public AudioSinkSchedule Schedule => AudioSinkSchedule.HostPump;
        public int LatencyFrames => Queue;
        public void Start(AudioRender render) => _render = render;
        public void Pump() { }
        public void Dispose() => _render = null;

        /// <summary>Time passes: unless it is stalled, the device plays <paramref name="frames"/> and the sink renders as many.</summary>
        public void Play(int frames)
        {
            if (Stalled) return;
            for (; frames > 0; frames -= Block)
            {
                var block = _block.AsSpan(0, Math.Min(Block, frames) * 2);
                _render!(block);
                for (int i = 0; i < block.Length; i += 2) { Left.Add(block[i]); Right.Add(block[i + 1]); }
            }
        }
    }

    /// <summary>A game loop at one step per frame over a <see cref="StallingSink"/>.</summary>
    private sealed class Host
    {
        public readonly StallingSink Sink = new();
        public readonly AudioEngine Engine;
        public long Step;

        public Host(bool deterministic = false, int updateRate = Steps)
        {
            Engine = new AudioEngine(Sink, new AudioEngineSettings { UpdateRate = updateRate }) { Deterministic = deterministic };
            StepFrames = Rate / updateRate;
            Sink.Play(Queue);
        }

        public int StepFrames { get; }
        /// <summary>The frames heard so far: the index of the next one.</summary>
        public int Heard => Sink.Left.Count;
        /// <summary>How far the current step's sounds are from what the output plays next.</summary>
        public long Ahead => Engine.Now - (Engine.RenderedFrames + Engine.SkippedFrames);

        /// <summary>One host frame: the time of its steps goes by, the game runs them, then the host pumps the audio.</summary>
        public void Frame(Action? step = null, int steps = 1)
        {
            Sink.Play(steps * StepFrames);
            for (int i = 0; i < steps; i++)
            {
                step?.Invoke();
                Engine.Advance();
                Step++;
            }
            Engine.Pump();
        }

        public void Frames(int count, Action? step = null) { for (int i = 0; i < count; i++) Frame(step); }

        /// <summary>The output stops for <paramref name="seconds"/> while the game goes on; returns the first frame heard after it.</summary>
        public int Stall(double seconds, Action? step = null)
        {
            Sink.Stalled = true;
            Frames((int)Math.Round(seconds * Steps), step);
            Sink.Stalled = false;
            return Heard;
        }
    }

    private static AudioClip Constant(float value, double seconds) => new([.. Enumerable.Repeat(value, (int)(seconds * Rate))], Rate);
    /// <summary>Rises from 0.1 to 0.9: every frame says which frame of the clip it is.</summary>
    private static AudioClip Ramp(double seconds)
    {
        int frames = (int)(seconds * Rate);
        return new([.. Enumerable.Range(0, frames).Select(i => 0.1f + 0.8f * i / frames)], Rate);
    }
    /// <summary>The frame of a <see cref="Ramp"/> of <paramref name="seconds"/> that <paramref name="sample"/> is.</summary>
    private static double RampFrame(float sample, double seconds) => (sample - 0.1) / 0.8 * seconds * Rate;

    private static readonly AudioClip Shot = Constant(0.25f, 0.2);

    // ---- Transient sounds

    [TestMethod]
    [DataRow(0.5)]
    [DataRow(2.0)]
    [DataRow(30.0)]
    public void One_shots_played_while_the_output_had_stopped_are_not_heard_together_when_it_plays_again(double seconds)
    {
        var host = new Host();
        var shots = new List<(long Step, VoiceHandle Voice)>();
        long previousNow = 0;
        bool back = false;
        void Game()
        {
            // A shot every tenth of a second, each a fifth of a second long: two overlap, 0.5 at most.
            if (host.Step % 6 == 0) shots.Add((host.Step, host.Engine.Play(Shot, new PlayParameters { Pan = 0f })));
            back |= host.Engine.Now < previousNow;
            previousNow = host.Engine.Now;
        }
        host.Frames(120, Game);
        int resumed = host.Stall(seconds, Game);
        long resumedStep = host.Step;
        Assert.IsFalse(back, "the timeline never went back while the output had stopped");
        foreach (var (step, voice) in shots)
        {
            Assert.IsFalse(voice.IsNone, "every shot was accepted");
            // A fifth of a second and the timeline's step of margin: older shots are over.
            if (resumedStep - step < 15) continue;
            Assert.IsFalse(host.Engine.IsPlaying(voice), $"the shot of step {step} ended on the timeline ({resumedStep - step} steps ago)");
            Assert.IsFalse(host.Engine.Stop(voice), "and its handle ended with it");
        }
        Assert.IsTrue(host.Engine.ActiveVoices <= 3, $"only the shots still within their fifth of a second hold a voice ({host.Engine.ActiveVoices})");
        long ahead = 0;
        host.Frame(() => { Game(); ahead = host.Ahead; });
        Assert.IsTrue(ahead >= 0 && ahead <= 2 * StepFrames, $"a sound played as the output is back starts in the frames it plays next, not {ahead * 1000.0 / Rate:F0} ms later");
        host.Frames(120, Game);
        float peak = 0;
        double sum = 0;
        for (int f = resumed; f < resumed + Rate; f++)
        {
            peak = Math.Max(peak, host.Sink.Left[f]);
            sum += host.Sink.Left[f];
        }
        double heard = sum / (0.25 * Shot.Samples.Length);
        Assert.IsTrue(peak <= 0.5001f, $"no more than the two overlapping shots of any moment are heard at once (peak {peak:F2}, 0.50 in a normal second)");
        Assert.IsTrue(heard > 8.5 && heard < 10.5, $"the second after it holds the shots of one second, not those of the stall ({heard:F1}, 10 in a normal second)");
        Assert.AreEqual(0, host.Engine.RefusedSounds + host.Engine.DroppedCommands, "nothing was refused or dropped");
    }

    [TestMethod]
    public void A_one_shot_still_within_its_length_goes_on_from_where_the_game_is()
    {
        var host = new Host();
        host.Frames(60);
        // Three seconds long, played one second before the output is back: it is heard from its second second.
        var voice = VoiceHandle.None;
        long played = 0;
        int resumed = host.Stall(2, () =>
        {
            if (host.Step != 120) return;
            voice = host.Engine.Play(Ramp(3), new PlayParameters());
            played = host.Engine.Now;
        });
        Assert.IsTrue(host.Engine.IsPlaying(voice), "it has two seconds left");
        host.Frames(180);
        double first = RampFrame(host.Sink.Left[resumed], 3);
        long timeline = resumed + host.Engine.SkippedFrames - played;
        Assert.AreEqual(timeline, first, 2, "the first frame heard is the one the timeline is at, not the sound's first");
        Assert.IsTrue(first > 0.9 * Rate && first < 1.1 * Rate, $"about a second into the sound ({first / Rate:F2} s)");
        int last = resumed;
        while (last < host.Heard && host.Sink.Left[last] != 0) last++;
        Assert.AreEqual(3 * Rate - first, last - resumed, 2, "the rest of it plays, once");
        Assert.IsFalse(host.Engine.IsPlaying(voice));
    }

    // ---- Sounds that last

    [TestMethod]
    [DataRow(0.5)]
    [DataRow(2.0)]
    [DataRow(30.0)]
    public void A_loop_and_a_long_sound_go_on_in_step_with_the_game(double seconds)
    {
        var host = new Host();
        host.Frames(30);
        // Right: a loop of one second (music from a clip plays the same way). Left: a sound of five seconds.
        var loop = host.Engine.Play(Ramp(1), new PlayParameters { Loop = true, Pan = 1f, Bus = AudioEngine.MusicBus });
        var longSound = host.Engine.Play(Ramp(5), new PlayParameters { Pan = 0f });
        host.Frames(30);
        int stopped = host.Heard;
        int resumed = host.Stall(seconds);
        host.Frames(60);
        long skipped = host.Engine.SkippedFrames;
        Assert.AreEqual(seconds * Rate, skipped, 2 * StepFrames, "the frames the output did not play are skipped");
        Assert.IsTrue(host.Engine.IsPlaying(loop));
        double before = RampFrame(host.Sink.Right[stopped - 1], 1), after = RampFrame(host.Sink.Right[resumed], 1);
        Assert.AreEqual((before + 1 + skipped) % Rate, after, 2, "the loop is where it would be had the output played");
        double longBefore = RampFrame(host.Sink.Left[stopped - 1], 5);
        if (longBefore + skipped < 5 * Rate)
        {
            Assert.IsTrue(host.Engine.IsPlaying(longSound), "the long sound has not ended");
            Assert.AreEqual(longBefore + 1 + skipped, RampFrame(host.Sink.Left[resumed], 5), 2, "and goes on from where the game is, not from where it was cut");
        }
        else
        {
            Assert.IsFalse(host.Engine.IsPlaying(longSound), "the long sound ended while the output had stopped");
            Assert.IsFalse(host.Engine.Stop(longSound));
            for (int f = resumed; f < host.Heard; f++) Assert.AreEqual(0f, host.Sink.Left[f], "and none of it is heard afterwards");
        }
    }

    /// <summary>A stream of <see cref="Ramp"/> samples.</summary>
    private sealed class RampStream(double seconds) : IAudioStream
    {
        private readonly int _frames = (int)(seconds * Rate);
        private int _position;
        public int Channels => 1;
        public int SampleRate => Rate;
        public int Read(Span<float> buffer)
        {
            int n = Math.Min(buffer.Length, _frames - _position);
            for (int i = 0; i < n; i++) buffer[i] = 0.1f + 0.8f * (_position + i) / _frames;
            _position += n;
            return n;
        }
        public void Rewind() => _position = 0;
        public void Dispose() { }
    }

    [TestMethod]
    public void Streamed_music_goes_on_from_where_it_stopped_and_plays_whole()
    {
        var host = new Host();
        host.Frames(30);
        var music = host.Engine.Play(new RampStream(4), new PlayParameters { Bus = AudioEngine.MusicBus, Pan = 0f });
        host.Frames(60);
        int stopped = host.Heard;
        int resumed = host.Stall(2);
        // It is not decoded while nobody hears it: the next frame heard is the one after the last.
        host.Frame();
        Assert.AreEqual(RampFrame(host.Sink.Left[stopped - 1], 4) + 1, RampFrame(host.Sink.Left[resumed], 4), 0.5, "the stream goes on from its next frame");
        // Two seconds after the output is back, the stream has a second left although four seconds went by since it started.
        host.Frames(120);
        Assert.IsTrue(host.Engine.IsPlaying(music), "its handle lasts as long as the music does");
        host.Frames(120);
        Assert.IsFalse(host.Engine.IsPlaying(music), "and ends with it");
        int heard = 0;
        for (int f = 0; f < host.Heard; f++) if (host.Sink.Left[f] != 0) heard++;
        Assert.AreEqual(4 * Rate, heard, 2, "all of it was heard");
    }

    // ---- Pausing, stopping and muting keep their meaning

    [TestMethod]
    public void A_sound_the_game_paused_is_still_paused_afterwards_and_resumes_where_it_was()
    {
        var host = new Host();
        var voice = host.Engine.Play(Ramp(5), new PlayParameters { Pan = 0f });
        host.Frames(30);
        Assert.IsTrue(host.Engine.Pause(voice, true));
        host.Frames(10);
        int paused = host.Heard - 1;
        while (host.Sink.Left[paused] == 0) paused--;
        int resumed = host.Stall(2);
        host.Frames(30);
        Assert.IsTrue(host.Engine.IsPlaying(voice), "a paused sound keeps its voice");
        for (int f = resumed; f < host.Heard; f++) Assert.AreEqual(0f, host.Sink.Left[f], "and stays silent until the game resumes it");
        Assert.IsTrue(host.Engine.Pause(voice, false));
        int from = host.Heard;
        host.Frames(10);
        int first = from;
        while (host.Sink.Left[first] == 0) first++;
        Assert.AreEqual(RampFrame(host.Sink.Left[paused], 5) + 1, RampFrame(host.Sink.Left[first], 5), 0.5, "it resumes at the frame after the one it was paused at");
    }

    [TestMethod]
    public void A_fade_out_asked_for_while_the_output_had_stopped_runs_on_the_games_time()
    {
        var host = new Host();
        // Left: stopped with a tenth of a second of fade early in the stall. Right: with a second of fade, a quarter of
        // a second before the output is back.
        var early = host.Engine.Play(Constant(0.5f, 1), new PlayParameters { Loop = true, Pan = 0f });
        var late = host.Engine.Play(Constant(0.5f, 1), new PlayParameters { Loop = true, Pan = 1f });
        host.Frames(60);
        int resumed = host.Stall(2, () =>
        {
            if (host.Step == 60 + 30) Assert.IsTrue(host.Engine.Stop(early, fadeSeconds: 0.1));
            if (host.Step == 60 + 105) Assert.IsTrue(host.Engine.Stop(late, fadeSeconds: 1));
        });
        Assert.IsFalse(host.Engine.IsPlaying(early), "the short fade is over");
        Assert.IsFalse(host.Engine.Stop(early));
        Assert.IsTrue(host.Engine.IsPlaying(late), "the long one has three quarters of a second left");
        host.Frames(90);
        for (int f = resumed; f < host.Heard; f++) Assert.AreEqual(0f, host.Sink.Left[f], "the sound whose fade ended is not heard again");
        float first = host.Sink.Right[resumed];
        Assert.IsTrue(first > 0.5f * 0.70f && first < 0.5f * 0.80f, $"the other is a quarter into its fade ({first / 0.5f:F2} of its volume)");
        int silent = resumed;
        while (host.Sink.Right[silent] != 0) silent++;
        Assert.AreEqual(0.75 * Rate, silent - resumed, 2 * StepFrames, "and fades out over what is left of its second");
        Assert.IsFalse(host.Engine.IsPlaying(late));
    }

    [TestMethod]
    public void A_muted_sound_goes_on_unheard_and_a_paused_one_holds_its_position()
    {
        var host = new Host();
        var voice = host.Engine.Play(Ramp(5), new PlayParameters { Pan = 0f });
        host.Frames(30);
        float last = host.Sink.Left[^1];

        // Muted for half a second, by its own volume and then by its bus: it is half a second further on.
        host.Engine.Set(voice, volume: 0);
        host.Frames(15);
        host.Engine.Set(voice, volume: 1);
        host.Engine.Bus(AudioEngine.SfxBus).Volume = 0;
        host.Frames(15);
        host.Engine.Bus(AudioEngine.SfxBus).Volume = 1;
        int from = host.Heard;
        host.Frames(5);
        int first = from;
        while (host.Sink.Left[first] == 0) first++;
        Assert.AreEqual(RampFrame(last, 5) + 30 * StepFrames + (first - from) + 1, RampFrame(host.Sink.Left[first], 5), 2, "muted, the sound went on");

        // Paused for half a second: it is where it was.
        last = host.Sink.Left[^1];
        int before = host.Heard;
        Assert.IsTrue(host.Engine.Pause(voice, true));
        host.Frames(30);
        Assert.IsTrue(host.Engine.Pause(voice, false));
        from = host.Heard;
        host.Frames(5);
        first = from;
        while (host.Sink.Left[first] == 0) first++;
        int played = 0;
        for (int f = before; f < from; f++) if (host.Sink.Left[f] != 0) played++;
        Assert.AreEqual(RampFrame(last, 5) + played + 1, RampFrame(host.Sink.Left[first], 5), 2, "paused, the sound held its position");
        Assert.AreEqual(0, host.Engine.SkippedFrames, "and none of this skips anything: the output never stopped");
    }

    // ---- What is and is not an output that stopped

    [TestMethod]
    public void While_the_output_has_stopped_the_timeline_goes_on_and_the_missing_frames_are_skipped()
    {
        var host = new Host();
        host.Frames(60);
        long now = host.Engine.Now, rendered = host.Engine.RenderedFrames;
        host.Sink.Stalled = true;
        for (int i = 0; i < 150; i++)
        {
            host.Frame();
            long moved = host.Engine.Now - now;
            now = host.Engine.Now;
            // Ahead of a sink that does not render, a step is shortened by a sixteenth at most, as after a slow frame.
            Assert.IsTrue(moved <= StepFrames && moved >= StepFrames - StepFrames / 16, $"step {i}: the timeline goes on, a step at a time ({moved} frames)");
            // A quarter of a second of steps without a render, and from then on the output is taken to have stopped.
            if (i < 14) Assert.AreEqual(0, host.Engine.SkippedFrames, $"step {i}: not yet");
            if (i >= 15) Assert.IsTrue(host.Ahead > 0 && host.Ahead <= 2 * StepFrames, $"step {i}: the output's place stays within a step of the timeline ({host.Ahead})");
        }
        Assert.AreEqual(rendered, host.Engine.RenderedFrames, "the sink rendered nothing");
    }

    [TestMethod]
    public void A_gap_shorter_than_a_quarter_of_a_second_skips_nothing()
    {
        var host = new Host();
        int shots = 0;
        void Game() { if (host.Step % 6 == 0) { host.Engine.Play(Shot, new PlayParameters { Pan = 0f }); shots++; } }
        host.Frames(60, Game);
        host.Stall(0.2, Game);
        host.Frames(60, Game);
        while (host.Step % 6 != 1) host.Frame(Game);
        host.Frames(40);
        Assert.AreEqual(0, host.Engine.SkippedFrames);
        double heard = host.Sink.Left.Sum(v => (double)v) / (0.25 * Shot.Samples.Length);
        Assert.AreEqual(shots, heard, 0.001, "every shot is heard whole");
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(60)]
    [DataRow(144)]
    public void Slow_frames_and_sinks_that_render_in_large_blocks_skip_nothing(int updateRate)
    {
        var host = new Host(updateRate: updateRate);
        for (int cycle = 0; cycle < 20; cycle++)
        {
            host.Frames(7, () => host.Engine.Play(Shot, new PlayParameters()));
            // A slow frame made up with five steps at once.
            host.Frame(() => host.Engine.Play(Shot, new PlayParameters()), steps: 5);
        }
        // A sink that renders 100 ms at a time (several host frames go by between two renders).
        int owed = 0;
        for (int i = 0; i < 4 * updateRate; i++)
        {
            owed += host.StepFrames;
            host.Sink.Stalled = owed < Rate / 10;
            if (!host.Sink.Stalled) { host.Sink.Play(owed - host.StepFrames); owed = 0; }
            host.Frame(() => host.Engine.Play(Shot, new PlayParameters()));
        }
        Assert.AreEqual(0, host.Engine.SkippedFrames);
    }

    [TestMethod]
    [DataRow(4096)]
    [DataRow(16384)]
    [DataRow(48000)]
    public void A_sink_that_renders_in_blocks_longer_than_a_quarter_of_a_second_has_not_stopped(int blockFrames)
    {
        var host = new Host();
        host.Sink.Block = blockFrames;
        host.Sink.Play(blockFrames); // its first block, as it starts
        var voice = host.Engine.Play(Ramp(1), new PlayParameters { Loop = true, Pan = 0f });
        // The sink asks for a whole block each time the device has played one.
        int owed = 0;
        for (int i = 0; i < 600; i++)
        {
            owed += StepFrames;
            host.Sink.Stalled = true;
            if (owed >= blockFrames) { host.Sink.Stalled = false; host.Sink.Play(blockFrames); owed -= blockFrames; host.Sink.Stalled = true; }
            host.Frame();
        }
        Assert.AreEqual(0, host.Engine.SkippedFrames, "nothing is skipped while the sink keeps rendering, however large its blocks");
        // And when it does stop, it is noticed.
        for (int i = 0; i < 600; i++) host.Frame();
        Assert.IsTrue(host.Engine.SkippedFrames > 5 * Rate, $"a real stop is still skipped ({host.Engine.SkippedFrames} frames)");
        Assert.IsTrue(host.Engine.IsPlaying(voice));
    }

    [TestMethod]
    public void Deterministic_runs_skip_nothing_whatever_the_sink_does()
    {
        var host = new Host(deterministic: true);
        var voice = host.Engine.Play(Ramp(5), new PlayParameters { Pan = 0f });
        host.Frames(30);
        int stopped = host.Heard;
        int resumed = host.Stall(2);
        host.Frames(30);
        Assert.AreEqual(0, host.Engine.SkippedFrames);
        Assert.AreEqual(RampFrame(host.Sink.Left[stopped - 1], 5) + 1, RampFrame(host.Sink.Left[resumed], 5), 0.5, "the sound is rendered frame after frame");
        Assert.IsTrue(host.Engine.IsPlaying(voice) || host.Engine.Steps * StepFrames >= 5 * Rate);
    }

    [TestMethod]
    public void Skipping_what_the_output_did_not_play_allocates_nothing_while_rendering()
    {
        var host = new Host();
        var loop = host.Engine.Play(Ramp(1), new PlayParameters { Loop = true });
        void Game()
        {
            if (host.Step % 6 == 0) host.Engine.Play(Shot, new PlayParameters { Pan = 0f });
            host.Engine.Set(loop, volume: 0.5f + host.Step % 7 * 0.05f);
            host.Engine.SetPitch(loop, 1f + host.Step % 5 * 0.1f);
        }
        void Round()
        {
            host.Frames(30, Game);
            host.Stall(0.5, Game);
            host.Frames(30, Game);
        }
        Round();
        var bytes = new long[5];
        for (int window = 0; window < bytes.Length; window++)
        {
            long before = host.Engine.RenderAllocations.Bytes;
            Round();
            bytes[window] = host.Engine.RenderAllocations.Bytes - before;
        }
        Assert.IsTrue(host.Engine.SkippedFrames > 5 * Rate / 4, "each round skipped the frames of its stall");
        // A background collection can count once against any thread: one clean window of five is the mixer's own count.
        Assert.AreEqual(0, bytes.Min(), $"rendering allocated in every window (bytes per window: {string.Join(", ", bytes)})");
    }
}
