using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Kernel;

/// <summary>
/// The loop runs the kernel's schedule and owns time; <see cref="Game"/> is a module façade whose services come from the
/// registry and whose extension point is <see cref="Game.Configure"/>.
/// </summary>
[TestClass]
public sealed class GameKernelTests
{
    /// <summary>A platform whose monotonic clock the test advances by hand, with a chosen time-mode preference.</summary>
    private sealed class ClockPlatform(bool realTime) : IPlatform
    {
        private readonly HeadlessPlatform _inner = new();
        public IGraphicsDevice Graphics => _inner.Graphics;
        public IAudioDevice Audio => _inner.Audio;
        public IInputSource Input => _inner.Input;
        public bool CloseRequested { get; set; }
        public bool Suspended { get; set; }
        public double Now { get; set; }
        public bool RealTime => realTime;
        public void PollEvents() { }
        public IRenderer BeginFrame() => _inner.BeginFrame();
        public void EndFrame() { }
        public void SaveScreenshot(string path) { }
        public void ToggleFullscreen() { }
        public void Dispose() { }
    }

    private sealed class Probe : Scene
    {
        public List<string> Log { get; } = [];
        public override void Update(float dt) => Log.Add("scene");
        public override void Draw(IRenderer renderer) => Log.Add("draw");
    }

    private sealed class ModularGame(Action<EngineBuilder>? configure = null) : Game
    {
        public Probe Scene { get; } = new();
        public List<string> Seen => Scene.Log;
        public List<string> Events { get; } = [];
        public override GameSettings Settings { get; } = new("kernel");
        public override void Configure(EngineBuilder builder)
        {
            builder.Schedule.Add(FramePhase.Update, "test.update", _ => Seen.Add("update"));
            builder.Schedule.Add(FramePhase.LateUpdate, "test.late", _ => Seen.Add("late"));
            builder.Schedule.Add(FramePhase.FixedUpdate, "test.after-scenes", c => Seen.Add($"fixed {c.FixedStep}"), after: "engine.scenes");
            builder.Schedule.Add(FramePhase.EndFrame, "test.end", c => Seen.Add($"end {c.Frame}"));
            builder.Lifecycle.Suspending += () => Events.Add("suspending");
            builder.Lifecycle.Resumed += () => Events.Add("resumed");
            configure?.Invoke(builder);
        }
        protected override void Load() => Scenes.Push(Scene);
        protected override void OnSuspended() => Events.Add("game suspended");
        protected override void OnResumed() => Events.Add("game resumed");
    }

    [TestMethod]
    public void The_loop_runs_the_game_s_systems_in_phase_order()
    {
        var game = new ModularGame();
        var loop = new GameLoop(game, new HeadlessPlatform());
        Assert.IsTrue(loop.Start());
        loop.Tick();
        loop.Tick();
        CollectionAssert.AreEqual(new[]
        {
            "scene", "fixed 0", "update", "late", "draw", "end 1",
            "scene", "fixed 1", "update", "late", "draw", "end 2",
        }, game.Seen);
        loop.Finish();
    }

    [TestMethod]
    public void The_built_in_systems_keep_the_engine_s_order()
    {
        var game = new ModularGame();
        new GameLoop(game, new HeadlessPlatform()).Start();
        CollectionAssert.AreEqual(new[] { "engine.input" }, game.Schedule.Order(FramePhase.PreFixed).ToArray());
        CollectionAssert.AreEqual(new[] { "engine.time", "engine.scenes", "test.after-scenes" }, game.Schedule.Order(FramePhase.FixedUpdate).ToArray());
        CollectionAssert.AreEqual(new[] { "engine.audio", "engine.achievements", "engine.input.end", "engine.assets.playing" }, game.Schedule.Order(FramePhase.PostFixed).ToArray());
        CollectionAssert.AreEqual(new[] { "engine.assets" }, game.Schedule.Order(FramePhase.BeginFrame).ToArray());
        CollectionAssert.AreEqual(new[] { "engine.touch" }, game.Schedule.Order(FramePhase.Input).ToArray());
        CollectionAssert.AreEqual(new[] { "engine.render" }, game.Schedule.Order(FramePhase.Render).ToArray());
        CollectionAssert.AreEqual(new[] { "engine.present" }, game.Schedule.Order(FramePhase.Present).ToArray());
    }

    [TestMethod]
    public void Services_come_from_the_registry_and_can_be_replaced()
    {
        var log = new ConsoleLog(new StringWriter());
        var game = new ModularGame(builder => builder.Services.Replace<ILog>(log));
        game.InitializeForTest(new HeadlessPlatform());
        Assert.AreSame(game.Input, game.Services.Get<InputMap>());
        Assert.AreSame(game.Audio, game.Services.Get<AudioMixer>());
        Assert.AreSame(game.Scenes, game.Services.Get<SceneManager>());
        Assert.AreSame(game.Assets, game.Services.Get<AssetCache>());
        Assert.AreSame(game.Touch, game.Services.Get<VirtualControls>());
        Assert.AreSame(game.Font, game.Services.Get<BitmapFont>());
        Assert.AreSame(game, game.Services.Get<Game>());
        Assert.AreSame(log, game.Log);
        Assert.IsTrue(game.TryGetCapability<ITouchScreen>(out var touch), "the headless touch screen is declared");
        Assert.IsNotNull(touch);
    }

    [TestMethod]
    public void A_run_s_time_mode_overrides_the_platform_s_preference()
    {
        // A real-time platform run deterministically: one step per frame whatever its clock says.
        var game = new ModularGame();
        var platform = new ClockPlatform(realTime: true);
        var loop = new GameLoop(game, platform, new RunOptions { TimeMode = TimeMode.Deterministic });
        Assert.IsTrue(loop.Start());
        Assert.AreEqual(TimeMode.Deterministic, game.TimeSettings.Mode);
        for (int i = 0; i < 10; i++) { platform.Now += i % 2 == 0 ? 0.2 : 0; loop.Tick(); }
        Assert.AreEqual(10, game.UpdateCount);
        Assert.AreEqual(1f, game.FrameBlend);
        loop.Finish();

        // A platform that prefers deterministic time run in real time: steps follow its clock.
        var real = new ModularGame();
        var clock = new ClockPlatform(realTime: false);
        loop = new GameLoop(real, clock, new RunOptions { TimeMode = TimeMode.RealTime });
        Assert.IsTrue(loop.Start());
        for (int i = 0; i < 10; i++) loop.Tick();
        Assert.AreEqual(0, real.UpdateCount, "the clock did not move");
        clock.Now += 0.5;
        loop.Tick();
        Assert.AreEqual(5, real.UpdateCount, "half a second, at most five steps per frame");
        loop.Finish();
    }

    [TestMethod]
    public void Without_a_run_choice_the_platform_s_preference_decides()
    {
        var headless = new ModularGame();
        headless.InitializeForTest(new HeadlessPlatform());
        Assert.AreEqual(TimeMode.Deterministic, headless.TimeSettings.Mode);
        var real = new ModularGame();
        real.InitializeForTest(new ClockPlatform(realTime: true));
        Assert.AreEqual(TimeMode.RealTime, real.TimeSettings.Mode);
        Assert.AreEqual(60, real.TimeSettings.FixedRate, "from GameSettings.UpdateRate");
    }

    [TestMethod]
    public void The_game_s_time_settings_drive_the_loop()
    {
        var game = new ModularGame(builder => builder.Configure<TimeSettings>(t => t with { MaxStepsPerFrame = 2, MaxFrameDelta = 0.1 }));
        var platform = new ClockPlatform(realTime: true);
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        platform.Now += 1;
        loop.Tick();
        Assert.AreEqual(2, game.UpdateCount, "MaxStepsPerFrame");
        platform.Now += 1;
        loop.Tick();
        loop.Tick();
        // The second second counted as 0.1 s (MaxFrameDelta) plus the step kept pending: 6 + 1 steps, two per frame.
        Assert.AreEqual(5, game.UpdateCount);
        loop.Finish();
    }

    [TestMethod]
    public void Run_options_win_over_the_game_s_time_mode()
    {
        var game = new ModularGame(builder => builder.Configure<TimeSettings>(t => t with { Mode = TimeMode.RealTime }));
        var loop = new GameLoop(game, new HeadlessPlatform(), new RunOptions { TimeMode = TimeMode.Deterministic });
        Assert.IsTrue(loop.Start());
        Assert.AreEqual(TimeMode.Deterministic, game.TimeSettings.Mode);
        var chosen = new ModularGame(builder => builder.Configure<TimeSettings>(t => t with { Mode = TimeMode.RealTime }));
        chosen.InitializeForTest(new HeadlessPlatform());
        Assert.AreEqual(TimeMode.RealTime, chosen.TimeSettings.Mode, "the game's choice wins over the platform's preference");
    }

    [TestMethod]
    public void Suspension_raises_lifecycle_events_after_the_game_s_own_handling()
    {
        var game = new ModularGame();
        var platform = new ClockPlatform(realTime: true);
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        platform.Now += 1.0 / 60;
        loop.Tick();
        platform.Suspended = true;
        platform.Now += 10;
        loop.Tick();
        platform.Suspended = false;
        long before = game.UpdateCount;
        loop.Tick();
        Assert.AreEqual(before, game.UpdateCount, "the time away is not simulated");
        CollectionAssert.AreEqual(new[] { "game suspended", "suspending", "game resumed", "resumed" }, game.Events);
        loop.Finish();
    }

    [TestMethod]
    public void A_vetoed_close_keeps_the_game_running()
    {
        bool veto = true;
        int asked = 0;
        var game = new ModularGame(builder => builder.Lifecycle.QuitRequested += request => { asked++; if (veto) request.Veto(); });
        var platform = new HeadlessPlatform();
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        platform.CloseRequested = true;
        Assert.IsTrue(loop.Tick(), "vetoed: the frame runs");
        Assert.IsTrue(loop.Tick(), "still vetoed: asked once per request");
        Assert.AreEqual(1, asked);
        platform.CloseRequested = false;
        loop.Tick();
        veto = false;
        platform.CloseRequested = true;
        Assert.IsFalse(loop.Tick(), "a new request nobody vetoes ends the run");
        Assert.AreEqual(2, asked);
        loop.Finish();
    }

    [TestMethod]
    public void Headless_time_is_a_monotonic_clock_not_a_frame_count()
    {
        var platform = new HeadlessPlatform();
        double start = platform.Now;
        for (int i = 0; i < 600; i++) platform.PollEvents();
        Assert.IsTrue(platform.Now - start < 1, "polling ten seconds' worth of frames does not move the clock ten seconds");
        Assert.IsTrue(platform.Now >= start);
        Assert.IsFalse(platform.RealTime, "headless runs prefer deterministic time");
    }
}
