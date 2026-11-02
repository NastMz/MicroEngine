using System.Text.Json;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>PERF-1: frame pacing, interpolation, profiling, background music rendering and headless audio.</summary>
[TestClass]
public class PerformanceTests
{
    private sealed class StepGame : Game
    {
        public override GameSettings Settings { get; } = new("pacing") { UpdateRate = 60 };
        public List<float> Blends { get; } = [];
        protected override void Load() => Scenes.Push(new BlendScene(this));
        private sealed class BlendScene(StepGame game) : Scene
        {
            public override void Update(float dt) { }
            public override void Draw(IRenderer renderer) => game.Blends.Add(game.FrameBlend);
        }
    }

    /// <summary>A real-time platform whose clock the test sets, optionally paced to a display.</summary>
    private sealed class DisplayPlatform(double displayRate) : IPlatform
    {
        private readonly HeadlessPlatform _inner = new();
        public IGraphicsDevice Graphics => _inner.Graphics;
        public IAudioDevice Audio => _inner.Audio;
        public IInputSource Input => _inner.Input;
        public bool CloseRequested => false;
        public double Now { get; set; }
        public bool RealTime => true;
        public double DisplayRate => displayRate;
        public void PollEvents() { }
        public IRenderer BeginFrame() => _inner.BeginFrame();
        public void EndFrame() { }
        public void SaveScreenshot(string path) { }
        public void ToggleFullscreen() { }
        public void Dispose() => _inner.Dispose();
    }

    /// <summary>
    /// Runs 600 frames of a 60 Hz display whose frames reach the loop up to ±3 ms late or early (as measured with SDL's
    /// Direct3D renderer on Windows), and returns the steps of every frame.
    /// </summary>
    private static List<long> JitteredRun(bool paceToDisplay, double displayRate)
    {
        var game = new StepGame();
        var platform = new DisplayPlatform(displayRate);
        var loop = new GameLoop(game, platform, new RunOptions { PaceToDisplay = paceToDisplay });
        Assert.IsTrue(loop.Start());
        var random = new Random(11);
        var steps = new List<long>();
        // The game's phase sits on a step boundary, where jitter does the most harm.
        for (int frame = 1; frame <= 600; frame++)
        {
            long before = game.UpdateCount;
            platform.Now = frame / 60.0 + (random.NextDouble() * 2 - 1) * 0.003;
            loop.Tick();
            steps.Add(game.UpdateCount - before);
        }
        return steps;
    }

    [TestMethod]
    public void Pacing_to_the_display_removes_the_stutter_of_irregular_wake_ups()
    {
        var measured = JitteredRun(paceToDisplay: false, displayRate: 60);
        var paced = JitteredRun(paceToDisplay: true, displayRate: 60);
        Assert.IsTrue(measured.Count(s => s != 1) > 50, $"measured time gives frames with 0 or 2 steps ({measured.Count(s => s != 1)})");
        Assert.AreEqual(0, paced.Skip(1).Count(s => s != 1), "paced to the display, every frame runs exactly one step");
        Assert.AreEqual(measured.Sum(), paced.Sum(), 2, "both simulate the same time");
    }

    [TestMethod]
    public void Pacing_is_off_without_a_known_display_rate()
    {
        Assert.IsTrue(JitteredRun(paceToDisplay: true, displayRate: 0).Count(s => s != 1) > 50, "no display rate: time as measured");
    }

    [TestMethod]
    public void Pacing_still_adds_up_to_real_time_when_vsync_is_not_honoured()
    {
        // The display says 60 Hz but frames arrive at 500 per second (VSync forced off by a driver): the game must not
        // run eight times too fast.
        var game = new StepGame();
        var platform = new DisplayPlatform(60);
        var loop = new GameLoop(game, platform);
        loop.Start();
        for (int frame = 1; frame <= 1000; frame++)
        {
            platform.Now = frame / 500.0;
            loop.Tick();
        }
        Assert.AreEqual(120, game.UpdateCount, 2, "two seconds at 60 updates per second");

        // The display says 75 Hz but frames come at 60 Hz: still real time.
        game = new StepGame();
        platform = new DisplayPlatform(75);
        loop = new GameLoop(game, platform);
        loop.Start();
        for (int frame = 1; frame <= 120; frame++)
        {
            platform.Now = frame / 60.0;
            loop.Tick();
        }
        Assert.AreEqual(120, game.UpdateCount, 2);
    }

    [TestMethod]
    public void Frame_blend_is_the_unsimulated_part_of_a_step()
    {
        var game = new StepGame();
        var platform = new DisplayPlatform(0);
        var loop = new GameLoop(game, platform);
        loop.Start();
        // 100 Hz frames, 60 Hz updates: 10 ms per frame.
        for (int frame = 1; frame <= 6; frame++)
        {
            platform.Now = frame / 100.0;
            loop.Tick();
        }
        float[] expected = [0.6f, 0.2f, 0.8f, 0.4f, 0.0f, 0.6f];
        for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], game.Blends[i], 1e-3, $"frame {i + 1}");

        var headless = new StepGame();
        GameRunner.Run(headless, new HeadlessPlatform(), new RunOptions { MaxUpdates = 10 });
        Assert.IsTrue(headless.Blends.All(b => b == 0), "one step per frame: nothing left to blend");
    }

    [TestMethod]
    public void Profiler_measures_frames_and_start_phases_and_writes_json_and_csv()
    {
        var profiler = new FrameProfiler(capacity: 50);
        var game = new StepGame();
        var result = GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { MaxUpdates = 80, Profiler = profiler });
        Assert.IsNull(result.Error);
        Assert.AreEqual(80, profiler.FrameCount);
        Assert.AreEqual(50, profiler.Samples.Count, "only the last frames are kept");
        CollectionAssert.AreEqual(new[] { "start screen", "load", "first frame" }, profiler.Phases.Select(p => p.Phase).ToArray());
        Assert.IsTrue(profiler.Samples.All(s => s.Steps == 1));
        Assert.AreEqual(80 / 60.0, profiler.Samples[^1].SimulatedTime, 1e-4, "the newest frame is last");

        var summary = profiler.Summarize();
        Assert.AreEqual(50, summary.Frames);
        CollectionAssert.AreEqual(new[] { 0, 50 }, summary.StepsPerFrame.ToArray());

        profiler.Environment["platform"] = "headless \"test\"";
        using var json = JsonDocument.Parse(profiler.ToJson());
        Assert.AreEqual("headless \"test\"", json.RootElement.GetProperty("environment").GetProperty("platform").GetString());
        Assert.AreEqual(50, json.RootElement.GetProperty("frames").GetArrayLength());
        Assert.AreEqual(50, json.RootElement.GetProperty("summary").GetProperty("frames").GetInt32());
        var csv = profiler.ToCsv().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(51, csv.Length, "a header and one row per kept frame");
        Assert.AreEqual(json.RootElement.GetProperty("frameColumns").GetArrayLength(), csv[0].Split(',').Length);
    }

    [TestMethod]
    public void Percentiles_and_hitches_of_a_summary()
    {
        var profiler = new FrameProfiler();
        for (int i = 0; i < 1000; i++)
            profiler.Add(new FrameSample(i == 500 ? 50 : 10, 0, 1, 1, 8, 1, i / 100.0, i / 100.0, 100, i == 500 ? 1 : 0));
        var s = profiler.Summarize();
        Assert.AreEqual(10, s.Interval.P50);
        Assert.AreEqual(10, s.Interval.P99);
        Assert.AreEqual(50, s.Interval.Max);
        Assert.AreEqual(1, s.Hitches);
        Assert.AreEqual(1, s.Collections);
        Assert.AreEqual(100, s.AllocatedPerFrame);
        Assert.AreEqual(2, s.Work.P50, "events, update and render");
    }

    [TestMethod]
    public void A_profiled_run_matches_an_unprofiled_one()
    {
        var plain = new StepGame();
        var profiled = new StepGame();
        var a = GameRunner.Run(plain, new HeadlessPlatform(), new RunOptions { MaxUpdates = 30 });
        var b = GameRunner.Run(profiled, new HeadlessPlatform(), new RunOptions { MaxUpdates = 30, Profiler = new FrameProfiler() });
        Assert.AreEqual(a.Updates, b.Updates);
        Assert.AreEqual(a.Frames, b.Frames);
        CollectionAssert.AreEqual(plain.Blends, profiled.Blends);
    }

    [TestMethod]
    public void Capture_device_records_what_would_be_heard()
    {
        var device = new CaptureAudioDevice(updateRate: 60, sampleRate: 48000);
        var beep = new AudioClip(Enumerable.Repeat(0.5f, 4800).ToArray(), 48000); // 0.1 s, mono
        var sound = device.LoadSound("beep", beep);
        for (int i = 0; i < 6; i++) device.Update(); // 0.1 s of silence
        device.Play(sound, volume: 0.5f, pan: 0);     // hard left
        for (int i = 0; i < 12; i++) device.Update(); // 0.2 s
        var output = device.Output;
        Assert.AreEqual(2, output.Channels);
        Assert.AreEqual(0.3, output.Duration, 1e-6);
        Assert.AreEqual(("beep", 0.1), (device.Played[0].Name, Math.Round(device.Played[0].Time, 3)));
        float[] left = [.. output.Samples.Where((_, i) => i % 2 == 0)], right = [.. output.Samples.Where((_, i) => i % 2 == 1)];
        Assert.IsTrue(left.Take(4800).All(s => s == 0), "silence before the sound");
        Assert.IsTrue(left.Skip(4800).Take(4800).All(s => Math.Abs(s - 0.25f) < 1e-6), "the sound at half volume");
        Assert.IsTrue(left.Skip(9600).All(s => s == 0), "silence after it ends");
        Assert.IsTrue(right.All(s => s == 0), "panned hard left");
        Assert.IsFalse(device.IsAvailable, "nothing reaches a speaker");

        var tone = new AudioClip(Enumerable.Range(0, 2400).Select(i => i < 1200 ? 0.2f : -0.2f).ToArray(), 48000);
        device.PlayMusic(device.LoadMusic("loop", tone, loop: true), volume: 1);
        for (int i = 0; i < 6; i++) device.Update(); // 0.1 s = two loops
        var music = device.Output.Samples.Skip(28800).Where((_, i) => i % 2 == 0).ToArray();
        Assert.AreEqual(4800, music.Length);
        Assert.IsTrue(music.Skip(2400).Take(1200).All(s => Math.Abs(s - 0.2f) < 1e-6), "looping music starts over");
        device.PauseMusic(true);
        device.Update();
        Assert.IsTrue(device.Output.Samples.Skip(38400).All(s => s == 0), "paused music is silent");
    }

    [TestMethod]
    public void Hidden_and_scripted_runs_open_no_physical_audio_unless_asked()
    {
        var settings = new GameSettings("audio");
        Assert.IsTrue(new PlatformRequest(settings).OpensOwnAudio, "a window a player watches");
        Assert.IsFalse(new PlatformRequest(settings) { Hidden = true }.OpensOwnAudio, "hidden window");
        Assert.IsFalse(new PlatformRequest(settings) { FixedStep = true }.OpensOwnAudio, "scripted run");
        Assert.IsTrue(new PlatformRequest(settings) { Hidden = true, AudioOutput = AudioOutput.Device }.OpensOwnAudio, "asked for a device");
        Assert.IsFalse(new PlatformRequest(settings) { AudioOutput = AudioOutput.Silent }.OpensOwnAudio);
        Assert.IsFalse(new PlatformRequest(settings) { AudioOutput = AudioOutput.Device, Mute = true }.OpensOwnAudio, "mute wins");
        Assert.IsFalse(new PlatformRequest(settings) { AudioOutput = AudioOutput.Device, Audio = () => new SilentAudioDevice() }.OpensOwnAudio, "the host's device wins");
    }

    [TestMethod]
    public void Launcher_parses_audio_pacing_and_profile_options()
    {
        var parsed = LaunchArguments.Parse(["--audio", "device", "--audio-capture", "out.wav", "--pacing", "measured", "--profile", "run.json"]);
        Assert.AreEqual(AudioOutput.Device, parsed.Audio);
        Assert.AreEqual("out.wav", parsed.AudioCapturePath);
        Assert.IsFalse(parsed.PaceToDisplay);
        Assert.AreEqual("run.json", parsed.ProfilePath);
        var defaults = LaunchArguments.Parse([]);
        Assert.AreEqual(AudioOutput.Automatic, defaults.Audio);
        Assert.IsTrue(defaults.PaceToDisplay);
        Assert.IsNull(defaults.ProfilePath);
        Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(["--audio", "loud"]));
        Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(["--pacing", "fast"]));
        Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(["--profile"]));
    }
}
