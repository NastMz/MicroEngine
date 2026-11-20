using MicroEngine.Audio;
using MicroEngine.Audio.Effects;
using MicroEngine.Platform;
using MicroEngine.Testing;

namespace MicroEngine.Tests.Audio;

/// <summary>
/// The mix graph: a bus with effects is mixed in a buffer of its own, processed and added to the bus above it; effects
/// are written against a small public contract, and every change to them (added, removed, bypassed, a parameter) takes
/// effect at the exact frame of the game step that made it, whatever blocks the sink renders.
/// </summary>
[TestClass]
public sealed class AudioGraphTests
{
    private const int Rate = 48000, Steps = 60, StepFrames = Rate / Steps;

    private static AudioClip Constant(float value, int frames) => new([.. Enumerable.Repeat(value, frames)], Rate);

    private static (AudioEngine Engine, OfflineAudioSink Sink) Open(int channels = 2, int blockFrames = 1024, int delayFrames = 0, bool? limiter = null)
    {
        var sink = new OfflineAudioSink(new AudioFormat(Rate, channels), Steps, blockFrames);
        return (new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = delayFrames, Limiter = limiter }), sink);
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

    /// <summary>A game's own effect: a gain with one parameter, written against the public contract only.</summary>
    private sealed class Gain : AudioEffect
    {
        private float _live = 1;
        public Gain(float amount = 1) : base(parameters: 1) => SetParameter(0, amount);
        public float Amount { get => GetParameter(0); set => SetParameter(0, value); }
        public int Resets, Channels;
        protected override void Prepare(int sampleRate, int channels) => Channels = channels;
        protected override void OnParameter(int index, float value) => _live = value;
        protected override void Reset() => Resets++;
        protected override void Process(Span<float> buffer)
        {
            for (int i = 0; i < buffer.Length; i++) buffer[i] *= _live;
        }
    }

    /// <summary>Adds a constant: with <see cref="Gain"/>, the order of a chain shows in the output.</summary>
    private sealed class Offset(float amount) : AudioEffect
    {
        protected override void Process(Span<float> buffer)
        {
            for (int i = 0; i < buffer.Length; i++) buffer[i] += amount;
        }
    }

    /// <summary>Replaces its bus's mix by the mix of the bus it listens to.</summary>
    private sealed class Follow : AudioEffect
    {
        public Follow(string? bus) => SidechainBus = bus;
        public string? Listens { get => SidechainBus; set => SidechainBus = value; }
        protected override void Process(Span<float> buffer) => buffer.Clear();
        protected override void Process(Span<float> buffer, ReadOnlySpan<float> sidechain) => sidechain.CopyTo(buffer);
    }

    private sealed class Broken : AudioEffect
    {
        public int Calls;
        protected override void Process(Span<float> buffer)
        {
            Calls++;
            throw new InvalidOperationException("broken effect");
        }
    }

    [TestMethod]
    public void An_effect_processes_the_sounds_of_its_bus_and_of_the_buses_under_it_only()
    {
        var (engine, sink) = Open();
        var ui = engine.AddBus("ui", AudioEngine.SfxBus);
        ui.Volume = 0.5f;
        engine.Bus(AudioEngine.SfxBus).Add(new Gain(0.5f));
        engine.Play(Constant(0.4f, 10), new PlayParameters());                                          // sfx: 0.4 * 0.5
        engine.Play(Constant(0.4f, 10), new PlayParameters { Bus = "ui", Pan = 0 });                    // ui, left: 0.4 * 0.5 (ui) * 0.5
        engine.Play(Constant(0.25f, 10), new PlayParameters { Bus = AudioEngine.MusicBus, Pan = 1 });   // music, right: untouched
        Step(engine);
        Assert.AreEqual(0.2f + 0.1f, Left(sink, 3), 1e-7f, "the effect on sfx halves sfx and the bus under it");
        Assert.AreEqual(0.2f + 0.25f, Right(sink, 3), 1e-7f, "and leaves the music alone");
        Assert.AreEqual(0f, Left(sink, 12));
    }

    [TestMethod]
    public void A_chain_runs_in_order_before_the_bus_volume()
    {
        var (engine, sink) = Open();
        var bus = engine.Bus(AudioEngine.SfxBus);
        var gain = new Gain(2);
        bus.Add(new Offset(0.1f)).Add(gain);
        bus.Volume = 0.5f;
        engine.Play(Constant(0.2f, StepFrames * 4), new PlayParameters());
        Step(engine);
        Assert.AreEqual((0.2f + 0.1f) * 2 * 0.5f, Left(sink, 5), 1e-7f, "offset, then gain, then the bus's volume");
        bus.Remove(gain);
        bus.Insert(0, gain);
        Step(engine);
        Assert.AreEqual((0.2f * 2 + 0.1f) * 0.5f, Left(sink, StepFrames + 5), 1e-7f, "gain first, then offset");
        CollectionAssert.AreEqual(new AudioEffect[] { gain, bus.Effects[1] }, bus.Effects.ToArray());
        Assert.ThrowsExactly<InvalidOperationException>(() => engine.Bus(AudioEngine.MusicBus).Add(gain), "an effect is on one bus at a time");
        bus.ClearEffects();
        Step(engine);
        Assert.AreEqual(0.2f * 0.5f, Left(sink, 2 * StepFrames + 5), "without effects the sound is as before");
    }

    [TestMethod]
    public void Changes_to_effects_take_effect_at_the_frame_of_their_step_whatever_blocks_the_sink_renders()
    {
        string? reference = null;
        foreach (int block in new[] { 64, 441, 1024, 4096 })
        {
            var (engine, sink) = Open(blockFrames: block, delayFrames: 300);
            var gain = new Gain(0.5f);
            var bus = engine.Bus(AudioEngine.SfxBus);
            engine.Play(Constant(0.4f, StepFrames * 20), new PlayParameters());
            Step(engine, 2);
            bus.Add(gain);                       // step 2
            Step(engine, 2);
            gain.Amount = 0.25f;                 // step 4
            Step(engine, 2);
            gain.Bypass = true;                  // step 6
            Step(engine, 2);
            gain.Bypass = false;                 // step 8
            Step(engine, 2);
            bus.Remove(gain);                    // step 10
            Step(engine, 3);
            float At(int step, int offset) => Left(sink, step * StepFrames + 300 + offset);
            Assert.AreEqual(0.4f, At(2, -1), $"block {block}: untouched before the effect's frame");
            Assert.AreEqual(0.2f, At(2, 0), $"block {block}: added at the frame of its step");
            Assert.AreEqual(0.2f, At(4, -1));
            Assert.AreEqual(0.1f, At(4, 0), $"block {block}: a parameter changes at the frame of its step");
            Assert.AreEqual(0.1f, At(6, -1));
            Assert.AreEqual(0.4f, At(6, 0), $"block {block}: bypassed");
            Assert.AreEqual(0.1f, At(8, 0), $"block {block}: back in");
            Assert.AreEqual(0.4f, At(10, 0), $"block {block}: removed");
            Assert.AreEqual(2, gain.Resets, "cleared when it entered the chain and when it left bypass");
            string hash = AudioGraphBaselineTests.Hash(sink.Samples);
            reference ??= hash;
            Assert.AreEqual(reference, hash, $"block {block}");
        }
    }

    [TestMethod]
    public void An_effect_can_listen_to_another_bus()
    {
        var (engine, sink) = Open();
        engine.AddBus("voice");
        engine.Bus("voice").Volume = 0.5f;
        engine.Bus(AudioEngine.MusicBus).Add(new Follow("voice"));
        engine.Play(Constant(0.8f, 10), new PlayParameters { Bus = "voice", Pan = 0 });
        engine.Play(Constant(0.3f, 10), new PlayParameters { Bus = AudioEngine.MusicBus, Pan = 1 });
        Step(engine);
        Assert.AreEqual(0.8f, Left(sink, 3), 1e-7f, "the voice bus itself (0.4) and what the music bus copied from it, after its volume (0.4)");
        Assert.AreEqual(0f, Right(sink, 3), "the music's own sound was replaced");
    }

    [TestMethod]
    public void An_effect_cannot_listen_to_a_bus_whose_mix_depends_on_its_own()
    {
        var (engine, _) = Open();
        engine.AddBus("voice", AudioEngine.SfxBus);
        engine.AddBus("ambience");
        var sfx = engine.Bus(AudioEngine.SfxBus);
        Assert.ThrowsExactly<ArgumentException>(() => sfx.Add(new Follow("none")), "the bus must exist");
        Assert.ThrowsExactly<ArgumentException>(() => sfx.Add(new Follow(AudioEngine.SfxBus)), "its own bus");
        Assert.ThrowsExactly<ArgumentException>(() => engine.Bus("voice").Add(new Follow(AudioEngine.SfxBus)), "a bus it plays into");
        Assert.ThrowsExactly<ArgumentException>(() => sfx.Add(new Follow(AudioEngine.MasterBus)), "everything plays into master");
        Assert.AreEqual(0, sfx.Effects.Count, "a refused effect is not left on the bus");
        var follow = new Follow("ambience");
        sfx.Add(follow);
        Assert.ThrowsExactly<ArgumentException>(() => engine.Bus("ambience").Add(new Follow(AudioEngine.SfxBus)), "a circle: each would wait for the other");
        engine.Bus("ambience").Add(new Follow("voice"));
        Assert.ThrowsExactly<ArgumentException>(() => follow.Listens = "voices", "checked when it changes too");
        Assert.AreEqual("ambience", follow.Listens, "and the refused name is not kept");
        sfx.Add(new Follow("voice"));
        Assert.AreEqual(2, sfx.Effects.Count, "a bus can listen to one under it: that one is mixed first");
    }

    [TestMethod]
    public void An_effect_that_throws_leaves_the_mix_and_nothing_escapes_the_render_function()
    {
        var (engine, sink) = Open();
        var broken = new Broken();
        engine.Bus(AudioEngine.SfxBus).Add(broken).Add(new Gain(0.5f));
        engine.Play(Constant(0.4f, StepFrames * 3), new PlayParameters());
        Step(engine, 3);
        Assert.IsTrue(broken.Faulted);
        Assert.AreEqual(1, broken.Calls, "it is not called again");
        Assert.AreEqual(1, engine.FaultedEffects);
        Assert.AreEqual(0.2f, Left(sink, StepFrames + 5), "the rest of the chain goes on");
    }

    [TestMethod]
    public void A_mono_mixer_gives_effects_one_channel()
    {
        var (engine, sink) = Open(channels: 1);
        var gain = new Gain(0.5f);
        engine.Bus(AudioEngine.MasterBus).Add(gain);
        engine.Play(Constant(0.4f, 10), new PlayParameters());
        Step(engine);
        Assert.AreEqual(1, gain.Channels);
        Assert.AreEqual(0.2f, sink.Samples[3], 1e-7f, "an effect on master processes the output itself");
    }

    [TestMethod]
    public void The_same_calls_give_the_same_samples_with_every_built_in_effect_whatever_blocks_the_sink_renders()
    {
        string? reference = null;
        foreach (int block in new[] { 64, 441, 1024, 4096 })
        {
            var (engine, sink) = Open(blockFrames: block, delayFrames: 200, limiter: true);
            engine.AddBus("voice");
            var lowPass = new LowPass { Cutoff = 4000 };
            var reverb = new Reverb();
            var delay = new Delay { Time = 0.11f };
            engine.Bus(AudioEngine.SfxBus).Add(reverb).Add(delay);
            engine.Bus(AudioEngine.MusicBus).Add(lowPass).Add(Compressor.Duck("voice"));
            engine.Bus(AudioEngine.MasterBus).Add(new HighPass { Cutoff = 60 }).Add(new Compressor());
            var rng = new Random(5);
            for (int step = 0; step < 90; step++)
            {
                if (step % 5 == 0)
                    engine.Play(Noise(rng, 2000 + rng.Next(6000)), new PlayParameters { Pan = (float)rng.NextDouble(), Bus = rng.Next(3) switch { 0 => "voice", 1 => AudioEngine.MusicBus, _ => AudioEngine.SfxBus } });
                if (step % 9 == 0) lowPass.Cutoff = 300 + rng.Next(5000);
                if (step % 13 == 0) { reverb.Wet = (float)rng.NextDouble(); delay.Time = 0.05f + (float)rng.NextDouble() * 0.2f; }
                if (step == 40) engine.Bus(AudioEngine.SfxBus).FadeTo(0.3f, 0.2f);
                Step(engine);
            }
            Assert.IsTrue(sink.Samples.Any(s => s != 0));
            string hash = AudioGraphBaselineTests.Hash(sink.Samples);
            reference ??= hash;
            Assert.AreEqual(reference, hash, $"block {block}");
        }
    }

    private static AudioClip Noise(Random rng, int frames)
    {
        var samples = new float[frames];
        for (int i = 0; i < frames; i++) samples[i] = (float)(rng.NextDouble() - 0.5) * 0.6f;
        return new AudioClip(samples, Rate);
    }

    /// <summary>A sink the test renders itself, a block at a time, on the test's thread.</summary>
    private sealed class ManualSink(int channels = 2) : IAudioSink
    {
        private AudioRender? _render;
        public readonly float[] Block = new float[StepFrames * channels];
        public AudioFormat Format => new(Rate, channels);
        public AudioSinkSchedule Schedule => AudioSinkSchedule.HostPump;
        public int LatencyFrames => 0;
        public void Start(AudioRender render) => _render = render;
        public void Pump() { }
        public void Dispose() => _render = null;
        public void Render() => _render!(Block);
    }

    [TestMethod]
    public void Rendering_with_every_effect_active_allocates_nothing()
    {
        var sink = new ManualSink();
        using var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, Limiter = true }) { Deterministic = true };
        engine.AddBus("voice");
        var lowPass = new LowPass();
        engine.Bus(AudioEngine.SfxBus).Add(new Reverb()).Add(new Delay());
        engine.Bus(AudioEngine.MusicBus).Add(lowPass).Add(new HighPass()).Add(Compressor.Duck("voice"));
        engine.Bus(AudioEngine.MasterBus).Add(new Compressor { Threshold = -30 }).Add(new Gain(1.5f));
        var rng = new Random(2);
        foreach (string bus in new[] { "voice", AudioEngine.MusicBus, AudioEngine.SfxBus })
            engine.Play(Noise(rng, 5000), new PlayParameters { Loop = true, Bus = bus, Volume = 0.9f });
        int step = 0;
        AllocationAssert.None(() =>
        {
            // What a game does while it plays: parameters and fades every step, then the sink renders.
            lowPass.Cutoff = 500 + step++ % 50 * 40;
            if (step % 30 == 0) engine.Bus(AudioEngine.SfxBus).FadeTo(step % 60 == 0 ? 1 : 0.4f, 0.3f);
            engine.Advance();
            sink.Render();
        }, because: "the render function and the calls that feed it");
        Assert.IsTrue(sink.Block.Any(s => s != 0), "and it did render sound");
        foreach (var bus in engine.Buses) foreach (var effect in bus.Effects) Assert.IsNull(effect.Error, $"{effect.GetType().Name}: {effect.Error}");
    }

    [TestMethod]
    public void Changes_to_effects_made_while_the_sink_does_not_read_arrive_when_it_reads_again()
    {
        var sink = new ManualSink();
        using var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps }) { Deterministic = true };
        var gain = new Gain(0.5f);
        var bus = engine.Bus(AudioEngine.SfxBus);
        bus.Add(gain);
        engine.Play(Constant(0.4f, Rate * 60), new PlayParameters { Loop = true });
        engine.Advance();
        sink.Render();
        Assert.AreEqual(0.2f, sink.Block[10]);
        // The sink stops reading: the queue fills with changes the render function never takes.
        var filler = new Gain();
        engine.Bus(AudioEngine.MusicBus).Add(filler);
        for (int i = 0; i < 5000; i++) filler.Amount = i;
        Assert.IsTrue(engine.DroppedCommands > 0, "the queue is full");
        gain.Amount = 0.25f;
        var late = new Gain(0.5f);
        bus.Add(late);
        gain.Bypass = true;
        gain.Bypass = false;
        // It reads again: what the game last asked for is what plays.
        for (int i = 0; i < 4; i++)
        {
            engine.Advance();
            sink.Render();
        }
        Assert.AreEqual(0.4f * 0.25f * 0.5f, sink.Block[10], 1e-7f, "the latest parameter and the chain as it is now");
        Assert.AreEqual(0, engine.FaultedEffects);
    }

    [TestMethod]
    public void The_limiter_leaves_a_mix_within_full_scale_bit_for_bit_as_it_is()
    {
        foreach (int channels in new[] { 2, 1 })
        {
            var sink = new OfflineAudioSink(new AudioFormat(Rate, channels), Steps);
            using var engine = new AudioEngine(sink, new AudioEngineSettings { MaxVoices = 12, UpdateRate = Steps, DelayFrames = 300, Limiter = true });
            Assert.IsFalse(engine.Limiter.Bypass);
            AudioGraphBaselineTests.Scenario(engine);
            Assert.IsTrue(sink.Samples.Max(MathF.Abs) < 1f, "the scenario stays within full scale");
            Assert.AreEqual(channels == 2 ? AudioGraphBaselineTests.StereoReference : AudioGraphBaselineTests.MonoReference, AudioGraphBaselineTests.Hash(sink.Samples),
                "the same bits as the mixer without a limiter");
        }
    }

    [TestMethod]
    public void The_limiter_turns_a_mix_over_full_scale_down_at_once_and_lets_go_afterwards()
    {
        var (engine, sink) = Open(limiter: true);
        engine.Limiter.Release = 0.05f;
        engine.Play(Constant(0.5f, StepFrames * 30), new PlayParameters());
        Step(engine, 2);
        engine.Play(Constant(1f, StepFrames * 2), new PlayParameters());   // 1.5 for two steps
        Step(engine, 28);
        Assert.AreEqual(0.5f, Left(sink, 2 * StepFrames - 1), "untouched before");
        for (int f = 2 * StepFrames; f < 4 * StepFrames; f++)
            Assert.AreEqual(1f, Left(sink, f), 1e-6f, $"frame {f}: held at full scale from the first frame over it, not clipped later");
        Assert.IsTrue(Left(sink, 4 * StepFrames) < 0.5f, "the gain comes back over the release, not at once");
        int recovered = 4 * StepFrames + (int)(0.05f * Rate) + 1;
        for (int f = recovered; f < recovered + 100; f++) Assert.AreEqual(0.5f, Left(sink, f), $"frame {f}: exactly as mixed once the gain is back");
        Assert.IsTrue(sink.Samples.Max(MathF.Abs) <= 1f + 1e-6f);
    }

    [TestMethod]
    public void A_games_device_has_the_limiter_on_and_a_mixer_made_directly_has_it_off()
    {
        var (engine, sink) = Open();
        Assert.IsTrue(engine.Limiter.Bypass, "a bare mixer renders exactly what it is told");
        engine.Play(Constant(1f, 10), new PlayParameters());
        engine.Play(Constant(1f, 10), new PlayParameters());
        Step(engine);
        Assert.AreEqual(2f, Left(sink, 3));

        using var capture = new CaptureAudioDevice(Steps, Rate);
        var mixer = new AudioMixer(capture);
        Assert.IsFalse(mixer.Limiter.Bypass, "a game's mix is kept within full scale");
        Assert.AreSame(capture.Engine.Limiter, mixer.Limiter);
        mixer.Limiter.Bypass = true;
        mixer.Limiter.Threshold = 0.5f;
        Assert.AreEqual(0.5f, capture.Engine.Limiter.Threshold);
    }

    [TestMethod]
    public void A_game_sets_up_buses_and_effects_the_same_way_without_a_device_that_plays()
    {
        var audio = new AudioMixer(new SilentAudioDevice());
        Assert.IsFalse(audio.SupportsVoices, "a silent device says so instead of returning no voice without a word");
        var voice = audio.AddBus("voice");
        Assert.AreSame(voice, audio.Bus("voice"));
        var lowPass = new LowPass { Cutoff = 800 };
        audio.Bus(AudioEngine.MusicBus).Add(lowPass).Add(Compressor.Duck("voice"));
        Assert.AreEqual(2, audio.Bus(AudioEngine.MusicBus).Effects.Count);
        lowPass.Cutoff = 500;
        lowPass.Bypass = true;
        Assert.AreEqual(500f, lowPass.Cutoff);
        Assert.ThrowsExactly<ArgumentException>(() => audio.Bus(AudioEngine.SfxBus).Add(Compressor.Duck("nobody")), "the same checks as on a device that plays");
        Assert.ThrowsExactly<KeyNotFoundException>(() => audio.Bus("nobody"));
        audio.SetBusVolume("voice", 0.5f, fadeSeconds: 1);
        Assert.AreEqual(0.5f, audio.BusVolume("voice"));
        Assert.IsTrue(new AudioMixer(new CaptureAudioDevice()).SupportsVoices);
    }

    /// <summary>Counts what each thread does to it, and notes a <c>Prepare</c> that arrives after it started processing.</summary>
    private sealed class Watched : AudioEffect
    {
        public int Prepares, Resets, Blocks, PreparedWhileRendering;
        protected override void Prepare(int sampleRate, int channels)
        {
            Prepares++;
            if (Blocks > 0) PreparedWhileRendering++;
        }
        protected override void Reset() => Resets++;
        protected override void Process(Span<float> buffer) => Blocks++;
    }

    [TestMethod]
    public void An_effect_is_prepared_once_in_its_life_so_adding_it_again_never_touches_what_the_render_thread_still_reads()
    {
        // A delay of several blocks: a change to the chains waits that long, and the render function goes on with the
        // chain it has. The sink renders only when the test says so, as another thread would at any moment.
        var sink = new ManualSink();
        using var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps, DelayFrames = StepFrames * 4 }) { Deterministic = true };
        var effect = new Watched();
        var sfx = engine.Bus(AudioEngine.SfxBus);
        sfx.Add(effect);
        for (int i = 0; i < 6; i++) { engine.Advance(); sink.Render(); }
        Assert.AreEqual(1, effect.Prepares);
        Assert.AreEqual(1, effect.Resets);
        int rendered = effect.Blocks;
        Assert.IsTrue(rendered > 0, "the render function is processing it");

        sfx.Remove(effect);
        sfx.Add(effect);
        // Between the two calls and after them the old chain is still what renders: this block processes the effect.
        sink.Render();
        Assert.IsTrue(effect.Blocks > rendered, "the chain without it has not reached its frame: the effect is still rendered");
        Assert.AreEqual(1, effect.Prepares, "it is not prepared again: Prepare runs on the game's thread and would write what the render function reads");
        Assert.AreEqual(0, effect.PreparedWhileRendering);

        for (int i = 0; i < 6; i++) { engine.Advance(); sink.Render(); }
        Assert.AreEqual(2, effect.Resets, "it starts clean when it comes back, on the render thread, at its frame");
        engine.Bus(AudioEngine.MusicBus).Add(new Watched());
        sfx.Remove(effect);
        engine.Bus(AudioEngine.MusicBus).Add(effect);
        for (int i = 0; i < 6; i++) { engine.Advance(); sink.Render(); }
        Assert.AreEqual(1, effect.Prepares, "moved to another bus of the same mixer: still prepared once");
        Assert.AreEqual(3, effect.Resets);
        Assert.AreEqual(0, engine.FaultedEffects);
    }

    [TestMethod]
    public void An_effect_belongs_to_the_first_mixer_that_plays_it()
    {
        var (first, _) = Open();
        var (second, _) = Open();
        var effect = new Watched();
        var silent = new AudioMixer(new SilentAudioDevice());
        silent.Bus(AudioEngine.SfxBus).Add(effect);
        silent.Bus(AudioEngine.SfxBus).Remove(effect);
        Assert.AreEqual(0, effect.Prepares, "a device without sound renders nothing: nothing to prepare");
        first.Bus(AudioEngine.SfxBus).Add(effect);
        first.Bus(AudioEngine.SfxBus).Remove(effect);
        var refused = Assert.ThrowsExactly<InvalidOperationException>(() => second.Bus(AudioEngine.SfxBus).Add(effect),
            "the first mixer's render thread may still be processing it");
        StringAssert.Contains(refused.Message, "another mixer");
        Assert.AreEqual(0, second.Bus(AudioEngine.SfxBus).Effects.Count);
        first.Bus(AudioEngine.MusicBus).Add(effect);
        Assert.AreEqual(1, effect.Prepares);
    }

    [TestMethod]
    public void Clearing_a_bus_of_its_effects_in_the_middle_of_a_fade_leaves_the_fade_running()
    {
        var (engine, sink) = Open();
        var bus = engine.AddBus("late");
        bus.Add(new Gain(0.5f));
        engine.Play(Constant(0.8f, StepFrames * 8), new PlayParameters { Bus = "late" });
        Step(engine);
        bus.FadeTo(0.5f, 0.05f);          // 2400 frames from step 1
        Step(engine);
        bus.ClearEffects();               // step 2: 800 frames into the fade
        Step(engine, 4);
        Assert.AreEqual(0.8f * 0.5f * (1 - 0.5f * 800 / 2400), Left(sink, 2 * StepFrames - 1), 1e-5f, "with the effect, fading");
        Assert.AreEqual(0.8f * (1 - 0.5f * 801 / 2400), Left(sink, 2 * StepFrames), 1e-5f, "without it from its frame, the fade where it was");
        Assert.AreEqual(0.8f * 0.5f, Left(sink, StepFrames + 2400 + 10), 1e-6f, "and it ends on its volume");
        Assert.AreEqual(0, engine.FaultedEffects);
    }

    [TestMethod]
    public void An_effect_moved_to_another_bus_while_the_sink_does_not_read_still_starts_clean_there()
    {
        var sink = new ManualSink();
        using var engine = new AudioEngine(sink, new AudioEngineSettings { UpdateRate = Steps }) { Deterministic = true };
        var effect = new Watched();
        engine.Bus(AudioEngine.SfxBus).Add(effect);
        engine.Advance();
        sink.Render();
        Assert.AreEqual(1, effect.Resets);
        // The queue fills: the chain without the effect cannot be queued, nor the one that has it on the other bus.
        var filler = new Gain();
        engine.Bus(AudioEngine.MasterBus).Add(filler);
        for (int i = 0; i < 5000; i++) filler.Amount = i;
        engine.Bus(AudioEngine.SfxBus).Remove(effect);
        engine.Bus(AudioEngine.MusicBus).Add(effect);
        for (int i = 0; i < 4; i++) { engine.Advance(); sink.Render(); }
        Assert.AreEqual(2, effect.Resets, "the render function never saw it leave, but it is cleared where it enters the other bus's chain");
        Assert.AreEqual(1, effect.Prepares);
    }

    private sealed class Meddler : AudioEffect
    {
        public Meddler() : base(parameters: 1) { }
        protected override void Process(Span<float> buffer) => SetParameter(0, 2);
    }

    [TestMethod]
    public void An_effect_that_sets_its_own_property_while_processing_is_taken_out_of_the_mix()
    {
        var (engine, sink) = Open();
        var meddler = new Meddler();
        engine.Bus(AudioEngine.SfxBus).Add(meddler);
        engine.Play(Constant(0.4f, StepFrames * 3), new PlayParameters());
        Step(engine, 3);
        Assert.IsTrue(meddler.Faulted, "a property is set from the game's thread only");
        Assert.AreEqual(0.4f, Left(sink, StepFrames + 5), "the mix goes on without it");
    }
}
