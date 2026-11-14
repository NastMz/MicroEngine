using MicroEngine.Audio;
using MicroEngine.Platform;
using MicroEngine.Testing;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// The command queue between the game and the render function when the sink stops rendering while the game goes on (a
/// page whose audio output is suspended, a device that stopped taking audio): the queue fills, and what the game asked
/// for must still be true when the sink renders again. Nothing is lost silently: stops, pauses and the latest values of
/// what changed arrive, and a sound that cannot be queued is refused.
/// </summary>
[TestClass]
public sealed class AudioEngineQueueTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;
    private static readonly AudioFormat Stereo = new(Rate, 2);

    private static AudioClip Constant(float value, int frames) => new([.. Enumerable.Repeat(value, frames)], Rate);

    /// <summary>A sink that renders only when the test says so, as a device that stopped taking audio does not.</summary>
    private sealed class StalledSink : IAudioSink
    {
        private AudioRender? _render;
        private readonly float[] _block = new float[StepFrames * 2];
        public AudioFormat Format => Stereo;
        public AudioSinkSchedule Schedule => AudioSinkSchedule.HostPump;
        public int LatencyFrames => 0;
        public void Start(AudioRender render) => _render = render;
        public void Pump() { }
        public void Dispose() => _render = null;

        /// <summary>Renders one step of frames and returns them (valid until the next call).</summary>
        public float[] RenderStep()
        {
            _render!(_block);
            return _block;
        }
    }

    private static (AudioEngine Engine, StalledSink Sink) Open(int maxVoices = 32)
    {
        var sink = new StalledSink();
        return (new AudioEngine(sink, new AudioEngineSettings { MaxVoices = maxVoices, UpdateRate = Steps }), sink);
    }

    /// <summary>One step in which the sink renders.</summary>
    private static float[] Step(AudioEngine engine, StalledSink sink)
    {
        engine.Advance();
        engine.Pump();
        return sink.RenderStep();
    }

    /// <summary>
    /// The sink renders again. While it did not, the mixer's timeline went up to a second ahead of it, so what the game
    /// asked for in those steps is due within the next second of sound: this renders past it and returns the last step.
    /// </summary>
    private static float[] Resume(AudioEngine engine, StalledSink sink)
    {
        for (int i = 0; i < Steps + 10; i++) Step(engine, sink);
        return Step(engine, sink);
    }

    /// <summary>Steps in which the sink renders nothing while every voice is changed, until the queue has filled.</summary>
    private static void Stall(AudioEngine engine, VoiceHandle[] voices, int steps = 200)
    {
        for (int s = 0; s < steps; s++)
        {
            foreach (var voice in voices)
            {
                engine.Set(voice, volume: 0.03f, pan: 0.5f);
                engine.SetPitch(voice, 1f);
            }
            engine.Advance();
            engine.Pump();
        }
        Assert.IsTrue(engine.DroppedCommands > 0, "the queue filled while the sink did not render");
    }

    private static VoiceHandle[] Loops(AudioEngine engine, int count, float level = 0.03f)
    {
        var clip = Constant(level, StepFrames);
        var voices = new VoiceHandle[count];
        for (int i = 0; i < count; i++) voices[i] = engine.Play(clip, new PlayParameters { Loop = true, Volume = 0.03f });
        return voices;
    }

    [TestMethod]
    public void Sounds_stopped_while_the_sink_does_not_render_are_silent_when_it_renders_again()
    {
        var (engine, sink) = Open();
        var voices = Loops(engine, 32, level: 1f);
        Assert.IsTrue(Step(engine, sink).Any(s => s != 0), "32 loops are heard");
        Stall(engine, voices);
        foreach (var voice in voices) Assert.IsTrue(engine.Stop(voice));
        Assert.AreEqual(0, engine.ActiveVoices);
        Assert.AreEqual(0f, Resume(engine, sink).Max(Math.Abs), "no loop plays on without a handle");
    }

    [TestMethod]
    public void A_fade_out_asked_for_while_the_sink_does_not_render_still_ends_the_sound()
    {
        var (engine, sink) = Open();
        var voices = Loops(engine, 32, level: 1f);
        Step(engine, sink);
        Stall(engine, voices);
        foreach (var voice in voices) engine.Stop(voice, fadeSeconds: 2.0 / Steps);
        var heard = Resume(engine, sink);
        Assert.AreEqual(0, engine.ActiveVoices);
        Assert.AreEqual(0f, heard.Max(Math.Abs));
    }

    [TestMethod]
    public void Changes_made_while_the_sink_does_not_render_arrive_as_their_latest_values()
    {
        var (engine, sink) = Open(maxVoices: 35);
        var filler = Loops(engine, 32, level: 0f);
        var changed = engine.Play(Constant(1f, StepFrames), new PlayParameters { Loop = true });
        var paused = engine.Play(Constant(1f, StepFrames), new PlayParameters { Loop = true, Pan = 0f });
        var resumed = engine.Play(Constant(0.125f, StepFrames), new PlayParameters { Loop = true, Pan = 0f });
        engine.Pause(resumed, true);
        Step(engine, sink);
        Stall(engine, filler);
        // The queue is full: none of these can be queued now.
        int dropped = engine.DroppedCommands;
        engine.Set(changed, volume: 0.9f, pan: 0f);
        engine.Set(changed, volume: 0.25f, pan: 1f);
        engine.Pause(paused, true);
        engine.Pause(resumed, false);
        engine.Bus(AudioEngine.SfxBus).Volume = 0.5f;
        Assert.IsTrue(engine.DroppedCommands >= dropped + 5, "each call that could not be queued is counted");
        Assert.IsTrue(engine.IsPlaying(changed) && engine.IsPlaying(paused) && engine.IsPlaying(resumed));
        var heard = Resume(engine, sink);
        Assert.AreEqual(0.125f * 0.5f, heard[10], 1e-6f, "left: only the resumed sound, through the bus at half volume");
        Assert.AreEqual(0.25f * 0.5f, heard[11], 1e-6f, "right: the changed sound at its last volume and pan");
    }

    [TestMethod]
    public void A_sound_that_cannot_be_queued_is_refused_and_plays_once_the_sink_renders()
    {
        var (engine, sink) = Open(maxVoices: 34);
        var filler = Loops(engine, 32, level: 0f);
        Step(engine, sink);
        Stall(engine, filler);
        int dropped = engine.DroppedCommands, active = engine.ActiveVoices;
        var refused = engine.Play(Constant(0.5f, Rate), new PlayParameters());
        Assert.IsTrue(refused.IsNone, "refused, as when the voice policy refuses it");
        Assert.AreEqual(dropped + 1, engine.DroppedCommands);
        Assert.AreEqual(active, engine.ActiveVoices, "it took no voice");
        Step(engine, sink);
        var played = engine.Play(Constant(0.5f, Rate), new PlayParameters());
        Assert.IsFalse(played.IsNone, "the sink reads again: sounds play");
        Assert.AreEqual(0.5f, Resume(engine, sink)[10]);
    }

    [TestMethod]
    public void A_stream_that_cannot_be_queued_is_disposed()
    {
        var (engine, sink) = Open(maxVoices: 34);
        var filler = Loops(engine, 32, level: 0f);
        Step(engine, sink);
        Stall(engine, filler);
        var stream = new Silence();
        Assert.IsTrue(engine.Play(stream, new PlayParameters { Bus = AudioEngine.MusicBus }).IsNone);
        Assert.IsTrue(stream.Disposed);
    }

    private sealed class Silence : IAudioStream
    {
        public bool Disposed;
        public int Channels => 1;
        public int SampleRate => Rate;
        public int Read(Span<float> buffer) { buffer.Clear(); return buffer.Length; }
        public void Rewind() { }
        public void Dispose() => Disposed = true;
    }

    [TestMethod]
    public void Calls_made_while_the_queue_is_full_allocate_nothing()
    {
        var (engine, sink) = Open();
        var voices = Loops(engine, 32);
        Step(engine, sink);
        Stall(engine, voices);
        int n = 0;
        AllocationAssert.None(() =>
        {
            var voice = voices[n++ % voices.Length];
            engine.Set(voice, volume: 0.02f, pan: 0.4f);
            engine.SetPitch(voice, 1.5f);
            engine.Pause(voice, n % 2 == 0);
            engine.Advance();
            engine.Pump();
        });
    }
}
