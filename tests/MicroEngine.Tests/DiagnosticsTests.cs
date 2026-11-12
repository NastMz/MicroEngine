using System.Diagnostics.Metrics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests;

/// <summary>DEBUG-1: the game's live diagnostics, per-system times, the engine meter and counters without casts.</summary>
[TestClass]
[DoNotParallelize] // the engine meter is process-wide
public sealed class DiagnosticsTests
{
    private sealed class SlowGame(bool enabled) : Game
    {
        public override GameSettings Settings { get; } = new("diagnostics") { UpdateRate = 60 };
        public FrameStatistics SeenInUpdate { get; private set; }

        public override void Configure(EngineBuilder builder) =>
            builder.Schedule.Add(FramePhase.Update, "test.slow", _ => Thread.Sleep(3));

        protected override void Load()
        {
            Diagnostics.Enabled = enabled;
            Scenes.Push(new Probe(this));
        }

        private sealed class Probe(SlowGame game) : Scene
        {
            public override void Update(float dt) => game.SeenInUpdate = game.Diagnostics.LastFrame;
            public override void Draw(MicroEngine.Graphics.IRenderer r) { }
        }
    }

    [TestMethod]
    public void Disabled_diagnostics_measure_nothing()
    {
        var game = new SlowGame(enabled: false);
        GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { MaxUpdates = 10 });
        Assert.IsFalse(game.Diagnostics.Enabled);
        Assert.AreEqual(default, game.Diagnostics.LastFrame);
        Assert.AreEqual(0, game.Diagnostics.Systems.Count);
    }

    [TestMethod]
    public void Enabled_diagnostics_report_the_last_frame_and_each_system()
    {
        var game = new SlowGame(enabled: true);
        var result = GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { MaxUpdates = 10 });
        var last = game.Diagnostics.LastFrame;
        Assert.AreEqual(result.Frames, last.Frame, "the last frame run is the one reported");
        Assert.IsTrue(last.Update >= 2.5, $"the update phase holds the 3 ms system: {last.Update} ms");
        Assert.IsTrue(last.Steps >= 1);
        Assert.IsTrue(last.Total >= last.Update);
        Assert.IsTrue(game.SeenInUpdate.Frame >= 1, "a game reads the previous frame while it updates");

        var slow = game.Diagnostics.Systems.Single(s => s.Name == "test.slow");
        Assert.AreEqual(FramePhase.Update, slow.Phase);
        Assert.IsTrue(slow.Milliseconds >= 2.5, $"{slow.Milliseconds} ms");
        var scenes = game.Diagnostics.Systems.Single(s => s.Name == "engine.scenes");
        Assert.AreEqual(FramePhase.FixedUpdate, scenes.Phase);
        Assert.IsTrue(game.InputSystem.Trace, "measuring traces input routing too");
        var phases = game.Diagnostics.Systems.Select(s => (int)s.Phase).ToArray();
        CollectionAssert.AreEqual(phases.Order().ToArray(), phases, "systems are listed in the order phases run");
    }

    [TestMethod]
    public void A_profiled_run_with_diagnostics_matches_one_without()
    {
        var plain = new SlowGame(enabled: false);
        var measured = new SlowGame(enabled: true);
        var a = GameRunner.Run(plain, new HeadlessPlatform(), new RunOptions { MaxUpdates = 12 });
        var b = GameRunner.Run(measured, new HeadlessPlatform(), new RunOptions { MaxUpdates = 12, Profiler = new FrameProfiler() });
        Assert.AreEqual((a.Updates, a.Frames), (b.Updates, b.Frames));
        Assert.AreEqual(b.Frames, measured.Diagnostics.LastFrame.Frame, "the profiler and the diagnostics measure the same frames");
    }

    [TestMethod]
    public void A_meter_listener_turns_measuring_on_and_reads_the_engine_instruments()
    {
        var values = new Dictionary<string, double>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) => { if (instrument.Meter.Name == GameDiagnostics.MeterName) l.EnableMeasurementEvents(instrument); };
        listener.SetMeasurementEventCallback<double>((i, v, _, _) => values[i.Name] = v);
        listener.SetMeasurementEventCallback<long>((i, v, _, _) => values[i.Name] = v);
        listener.SetMeasurementEventCallback<int>((i, v, _, _) => values[i.Name] = v);
        listener.Start();

        var game = new SlowGame(enabled: false);
        GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { MaxUpdates = 10 });
        Assert.IsTrue(game.Diagnostics.Active, "a listener makes the diagnostics measure without Enabled");
        listener.RecordObservableInstruments();
        CollectionAssert.IsSubsetOf(
            new[] { "microengine.frame.time", "microengine.frame.update", "microengine.frame.render", "microengine.frame.present",
                    "microengine.frame.allocated", "microengine.graphics.draw_calls", "microengine.audio.voices" },
            values.Keys.ToArray());
        Assert.IsTrue(values["microengine.frame.update"] >= 2.5, $"{values["microengine.frame.update"]} ms");
    }

    [TestMethod]
    public void Graphics_counters_are_read_through_the_device_interface()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        Dev.IGraphicsDevice contract = device;
        using var target = device.CreateRenderTarget(new Dev.RenderTargetDescription(4, 4));
        Assert.AreEqual(device.Counters, contract.Counters);
        Assert.IsTrue(contract.Counters.LiveResources > 0);
    }

    [TestMethod]
    public void Audio_statistics_come_from_the_mixer_without_casts()
    {
        var device = new CaptureAudioDevice();
        var mixer = new AudioMixer(device);
        mixer.AddSound("beep", new AudioClip(Enumerable.Repeat(0.5f, 4800).ToArray(), 48000));
        Assert.AreEqual(new AudioStatistics(0, device.Engine.MaxVoices, 0), mixer.Statistics);
        mixer.Play("beep");
        device.Update();
        Assert.AreEqual(1, mixer.Statistics.ActiveVoices);
        Assert.AreEqual(default, new AudioMixer(new SilentAudioDevice()).Statistics, "a device without a mixer reports nothing");
    }
}
