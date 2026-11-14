using MicroEngine.Audio;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// When a change to a playing sound is heard, against the frame it was issued for (<see cref="AudioEngine.Now"/> at the
/// call): with sounds starting more than one step after their update (a delay longer than a step, or the steps after a
/// slow frame, which are ahead of the device), the changes of several steps wait at once, each for its own frame.
/// </summary>
[TestClass]
public sealed class AudioEngineChangeTimingTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps, Queue = 3360;
    private static readonly AudioFormat Stereo = new(Rate, 2);

    /// <summary>A sink that renders when the test says so and keeps the left channel.</summary>
    private sealed class RecordingSink(int latencyFrames) : IAudioSink
    {
        private AudioRender? _render;
        private readonly float[] _block = new float[256 * 2];
        public readonly List<float> Left = [];
        public AudioFormat Format => Stereo;
        // Rendered on the test's thread: counted as a pumped sink, so that the allocation count is the render function's
        // alone and not what the test allocates between two renders.
        public AudioSinkSchedule Schedule => AudioSinkSchedule.HostPump;
        public int LatencyFrames => latencyFrames;
        public void Start(AudioRender render) => _render = render;
        public void Pump() { }
        public void Dispose() => _render = null;

        public void Play(int frames)
        {
            for (; frames > 0; frames -= 256)
            {
                var block = _block.AsSpan(0, Math.Min(256, frames) * 2);
                _render!(block);
                for (int i = 0; i < block.Length; i += 2) Left.Add(block[i]);
            }
        }
    }

    private static AudioClip Constant(float value, int frames) => new([.. Enumerable.Repeat(value, frames)], Rate);
    /// <summary>A sawtooth of 1000 frames: the difference of two frames is the pitch over 1000.</summary>
    private static readonly AudioClip Saw = new([.. Enumerable.Range(0, 48000).Select(i => i % 1000 / 1000f)], Rate);

    /// <summary>One change of a kind to <paramref name="voice"/>, different at every step; returns the value the left channel then shows.</summary>
    private static float Change(AudioEngine engine, VoiceHandle voice, string kind, int k)
    {
        switch (kind)
        {
            case "volume":
                float volume = 0.25f + k % 97 * 0.005f;
                Assert.IsTrue(engine.Set(voice, volume: volume));
                return volume;
            case "bus":
                float gain = 0.25f + k % 97 * 0.005f;
                engine.Bus(AudioEngine.SfxBus).Volume = gain;
                return gain;
            case "pan":
                float pan = 0.5f + k % 97 * 0.004f;
                Assert.IsTrue(engine.Set(voice, pan: pan));
                return Math.Min(1, 2 * (1 - pan));
            default:
                float pitch = 1f + k % 47 * 0.01f;
                Assert.IsTrue(engine.SetPitch(voice, pitch));
                return pitch;
        }
    }

    private static VoiceHandle PlayFor(AudioEngine engine, string kind) =>
        engine.Play(kind == "pitch" ? Saw : Constant(1f, 4800), new PlayParameters { Loop = true, Pan = kind == "pan" ? 0.5f : 0f });

    /// <summary>
    /// The value the left channel shows at <paramref name="frame"/>: the level, or for pitch the slope of the sawtooth
    /// from that frame to the next (looking <paramref name="direction"/> frames further at a time while the sawtooth
    /// wraps there).
    /// </summary>
    private static float Shown(List<float> left, long frame, string kind, int direction)
    {
        if (kind != "pitch") return left[(int)frame];
        for (long f = frame; Math.Abs(f - frame) < 12; f += direction)
        {
            float d = left[(int)f + 1] - left[(int)f];
            if (d > 0 && left[(int)f + 1] < 0.995f) return MathF.Round(d * 1000 * 100) / 100;
        }
        return float.NaN;
    }

    /// <summary>Five more windows of <paramref name="step"/>: the render function allocated nothing in at least one (a background collection can count once against any thread).</summary>
    private static void AssertRendersWithoutAllocating(AudioEngine engine, Action step)
    {
        var bytes = new long[5];
        for (int window = 0; window < bytes.Length; window++)
        {
            long before = engine.RenderAllocations.Bytes;
            for (int i = 0; i < 20; i++) step();
            bytes[window] = engine.RenderAllocations.Bytes - before;
        }
        Assert.AreEqual(0, bytes.Min(), $"rendering allocated in every window (bytes per window: {string.Join(", ", bytes)})");
    }

    /// <summary>Every change is heard from its frame on and not a frame before.</summary>
    private static void AssertHeardAtTheirFrames(List<float> left, List<(long Frame, float Value)> changes, string kind)
    {
        for (int i = 1; i < changes.Count - 1; i++)
        {
            var (frame, value) = changes[i];
            Assert.IsTrue(frame + 4 < left.Count, "the test rendered past every change");
            // A pitch shows in the difference of two frames: one frame of room on either side.
            long before = frame - (kind == "pitch" ? 2 : 1), at = frame + (kind == "pitch" ? 1 : 0);
            Assert.AreEqual(changes[i - 1].Value, Shown(left, before, kind, -1), 0.0005f, $"change {i} ({kind}) for frame {frame}, after {changes[i - 1].Frame}: the frame before it still has the previous value; it was not applied early");
            Assert.AreEqual(value, Shown(left, at, kind, 1), 0.0005f, $"change {i} ({kind}): heard at its frame {frame}");
        }
    }

    [TestMethod]
    [DataRow("volume", 1000)]
    [DataRow("pan", 1000)]
    [DataRow("pitch", 1000)]
    [DataRow("volume", 2000)]
    [DataRow("pitch", 4000)]
    [DataRow("bus", 1000)]
    [DataRow("bus", 4000)]
    public void With_a_delay_longer_than_a_step_each_steps_change_is_heard_at_its_frame(string kind, int delayFrames)
    {
        var sink = new OfflineAudioSink(Stereo, Steps, blockFrames: 256);
        var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = delayFrames }) { Deterministic = true };
        var voice = PlayFor(engine, kind);
        var changes = new List<(long, float)>();
        for (int k = 0; k < 90; k++)
        {
            changes.Add((engine.Now, Change(engine, voice, kind, k)));
            engine.Advance();
            engine.Pump();
        }
        for (int i = 0; i < 12; i++) { engine.Advance(); engine.Pump(); }
        var left = new List<float>();
        for (int i = 0; i < sink.Samples.Count; i += 2) left.Add(sink.Samples[i]);
        AssertHeardAtTheirFrames(left, changes, kind);
        int more = 0;
        AssertRendersWithoutAllocating(engine, () =>
        {
            Change(engine, voice, kind, more++);
            engine.Advance();
            engine.Pump();
        });
    }

    [TestMethod]
    [DataRow("volume", 40)]
    [DataRow("volume", 120)]
    [DataRow("pan", 120)]
    [DataRow("pitch", 120)]
    [DataRow("pitch", 500)]
    [DataRow("bus", 120)]
    public void After_a_slow_frame_each_steps_change_is_heard_at_its_frame(string kind, int slowMilliseconds)
    {
        var sink = new RecordingSink(Queue);
        var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps });
        sink.Play(Queue);
        var voice = PlayFor(engine, kind);
        var changes = new List<(long, float)>();
        int k = 0;
        void Frame(int steps, bool change)
        {
            // The device played that many steps of audio; the game makes it up with at most five steps, as the default
            // time policy does.
            sink.Play(steps * StepFrames);
            steps = Math.Min(steps, 5);
            for (int s = 0; s < steps; s++)
            {
                // The frame is read after the call: a step that comes late moves the timeline to what can still be rendered.
                if (change)
                {
                    float value = Change(engine, voice, kind, k++);
                    changes.Add((engine.Now, value));
                }
                engine.Advance();
            }
            engine.Pump();
        }
        for (int i = 0; i < 60; i++) Frame(1, change: false);
        for (int i = 0; i < 360; i++) Frame(i % 120 == 30 ? slowMilliseconds * Steps / 1000 : 1, change: true);
        for (int i = 0; i < 30; i++) Frame(1, change: false);
        for (int i = 1; i < changes.Count; i++) Assert.IsTrue(changes[i].Item1 > changes[i - 1].Item1, "each step's changes are for a later frame");
        AssertHeardAtTheirFrames(sink.Left, changes, kind);
        AssertRendersWithoutAllocating(engine, () => Frame(k % 20 == 0 ? 5 : 1, change: true));
    }

    [TestMethod]
    public void A_pause_and_a_resume_of_consecutive_steps_silence_exactly_one_step_whatever_the_delay()
    {
        const int delay = 3000;
        var sink = new OfflineAudioSink(Stereo, Steps, blockFrames: 256);
        var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = delay }) { Deterministic = true };
        var voice = engine.Play(Constant(0.5f, 4800), new PlayParameters { Loop = true });
        void Step() { engine.Advance(); engine.Pump(); }
        for (int i = 0; i < 5; i++) Step();
        long paused = engine.Now;
        Assert.IsTrue(engine.Pause(voice, true));
        Step();
        long resumed = engine.Now;
        Assert.IsTrue(engine.Pause(voice, false));
        for (int i = 0; i < 10; i++) Step();
        for (long f = delay; f < sink.Frames; f++)
            Assert.AreEqual(f >= paused && f < resumed ? 0f : 0.5f, sink.Samples[(int)f * 2], $"frame {f} (paused from {paused} to {resumed})");
    }

    [TestMethod]
    public void More_waiting_changes_than_a_voice_holds_are_never_early_and_end_at_the_latest_value()
    {
        // Twenty steps of delay: twenty changes wait at once, more than a voice keeps apart.
        const int delay = 20 * StepFrames;
        var sink = new OfflineAudioSink(Stereo, Steps, blockFrames: 256);
        var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = delay }) { Deterministic = true };
        var voice = engine.Play(Constant(1f, 4800), new PlayParameters { Loop = true, Pan = 0f });
        var changes = new List<(long Frame, float Value)>();
        for (int k = 0; k < 60; k++)
        {
            changes.Add((engine.Now, Change(engine, voice, "volume", k)));
            engine.Advance();
            engine.Pump();
        }
        for (int i = 0; i < 30; i++) { engine.Advance(); engine.Pump(); }
        // Each frame shows a value that was due by then: never one whose frame has not come.
        int due = -1, late = 0;
        for (long f = delay; f < sink.Frames; f++)
        {
            while (due + 1 < changes.Count && changes[due + 1].Frame <= f) due++;
            float heard = sink.Samples[(int)f * 2];
            int shown = heard == 1f ? -1 : changes.FindIndex(c => c.Value == heard);
            Assert.IsTrue(heard == 1f || shown >= 0, $"frame {f}: {heard} is one of the values asked for");
            Assert.IsTrue(shown <= due, $"frame {f}: the value of change {shown} is heard before its frame {(shown >= 0 ? changes[shown].Frame : 0)}");
            if (shown < due) late++;
        }
        Assert.IsTrue(late > 0, "some changes did wait for a later one, as the voice holds only so many");
        Assert.AreEqual(changes[^1].Value, sink.Samples[(int)(sink.Frames - 1) * 2], "the latest value is the one that stays");
        int more = 0;
        AssertRendersWithoutAllocating(engine, () =>
        {
            Change(engine, voice, "volume", more++);
            engine.Advance();
            engine.Pump();
        });
    }
}
