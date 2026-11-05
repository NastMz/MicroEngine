using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>PERF-2: the graphics options a game chooses, the launcher's overrides, the frame limit and <see cref="Game.Screen"/>.</summary>
[TestClass]
[DoNotParallelize] // redirects the console
public sealed class GraphicsSettingsTests
{
    private sealed class FrameGame(GraphicsSettings? graphics = null) : Game
    {
        public override GameSettings Settings { get; } = new("graphics", 8, 8) { Graphics = graphics ?? new() };
        public int Draws;
        public List<long> Steps { get; } = [];
        private long _last;
        protected override void Load() => Scenes.Push(new Counter(this));
        private sealed class Counter(FrameGame game) : Scene
        {
            public override void Update(float dt) { }
            public override void Draw(IRenderer renderer)
            {
                game.Draws++;
                game.Steps.Add(game.UpdateCount - game._last);
                game._last = game.UpdateCount;
            }
        }
    }

    /// <summary>
    /// A real-time platform on a simulated clock: each frame costs <c>work</c> seconds plus the jitter the test gives,
    /// and its <see cref="MicroEngine.Platform.IPreciseDelay"/> advances the clock; a platform that cannot block (a browser page)
    /// offers none, and <see cref="RunFor"/> moves its clock on to the next animation frame after a skipped one.
    /// </summary>
    private sealed class ClockPlatform : TestPlatform, IDisplayControl, MicroEngine.Platform.IDisplayPacing, MicroEngine.Platform.IPreciseDelay
    {
        private readonly double _work, _displayRate;
        private readonly bool _canWait;

        public ClockPlatform(double work, bool canWait = true, double displayRate = 0) : base(realTime: true)
        {
            (_work, _canWait, _displayRate) = (work, canWait, displayRate);
            _vsync = displayRate > 0;
            Now = 0;
            Capabilities.Add<IDisplayControl>(this);
            Capabilities.Add<MicroEngine.Platform.IDisplayPacing>(this);
            if (canWait) Capabilities.Add<MicroEngine.Platform.IPreciseDelay>(this);
        }

        public double Work => _work;
        public Func<int, double> Jitter { get; set; } = _ => 0;
        public int Frames;
        public double Waited;
        public double DisplayRate => _vsync ? _displayRate : 0;
        public override bool CanBlock => _canWait;
        public override void EndFrame() => Now += _work;
        public void Delay(double seconds)
        {
            Waited += seconds;
            Now += seconds + Jitter(++Frames);
        }

        private bool _vsync;
        public IReadOnlyList<DisplayInfo> Displays { get; } =
            [new(0, "main", 1920, 1080, 100, 1, true), new(1, "side", 1920, 1080, 75, 1.25f, false)];
        public int CurrentDisplay { get; private set; }
        public bool VSync => _vsync;
        public bool SetVSync(bool on) { _vsync = on; return true; }
        public WindowMode WindowMode { get; private set; }
        public bool SetWindowMode(WindowMode mode) { WindowMode = mode; return true; }
        public bool MoveToDisplay(int index)
        {
            if (index < 0 || index >= Displays.Count) return false;
            CurrentDisplay = index;
            return true;
        }
    }

    private static (FrameGame Game, ClockPlatform Platform, GameLoop Loop) Start(ClockPlatform platform, GraphicsSettings? graphics = null, bool pace = true)
    {
        var game = new FrameGame(graphics);
        var loop = new GameLoop(game, platform, new RunOptions { PaceToDisplay = pace });
        Assert.IsTrue(loop.Start());
        return (game, platform, loop);
    }

    private static void RunFor(GameLoop loop, ClockPlatform platform, double seconds)
    {
        double end = platform.Now + seconds;
        while (platform.Now < end)
        {
            double before = platform.Now;
            loop.Tick();
            // A page is called back at its next animation frame, after the skipped one.
            if (platform.Now == before) platform.Now += platform.Work;
        }
    }

    [TestMethod]
    public void The_frame_limit_caps_frames_per_second_without_changing_the_update_rate()
    {
        // Frames that take 2 ms could run at 500 per second; the limit holds them to 50.
        var (game, platform, loop) = Start(new ClockPlatform(work: 0.002), new GraphicsSettings { FrameRateLimit = 50 });
        RunFor(loop, platform, 10);
        Assert.AreEqual(500, game.Draws, 2, "50 frames per second for 10 s");
        Assert.AreEqual(600, game.UpdateCount, 2, "the simulation still runs 60 updates per second");
        Assert.IsTrue(platform.Waited > 8, "the loop waited instead of drawing");
    }

    [TestMethod]
    public void A_limited_frame_rate_equal_to_the_update_rate_runs_one_step_per_frame_despite_jitter()
    {
        // VSync off, limit 60, updates 60: the timer wakes up to 1 ms late or early around each period.
        static double Jitter(int f) => (f * 7919 % 3 - 1) * 0.001;
        int Uneven(bool pace)
        {
            var platform = new ClockPlatform(work: 0.001) { Jitter = Jitter };
            var (game, _, loop) = Start(platform, new GraphicsSettings { FrameRateLimit = 60 }, pace);
            RunFor(loop, platform, 10);
            Assert.AreEqual(600, game.UpdateCount, 2, "real time either way");
            return game.Steps.Skip(5).Count(s => s != 1);
        }
        Assert.IsTrue(Uneven(pace: false) > 0, "measured time turns the wake-up jitter into frames of 0 and 2 updates");
        Assert.AreEqual(0, Uneven(pace: true), "paced to the limit, every frame stands for one update");
    }

    [TestMethod]
    public void A_page_that_cannot_wait_skips_early_frames_instead()
    {
        var (game, platform, loop) = Start(new ClockPlatform(work: 0.004, canWait: false), new GraphicsSettings { FrameRateLimit = 30 });
        RunFor(loop, platform, 10);
        Assert.AreEqual(300, game.Draws, 30, "about 30 frames drawn per second of 250 offered");
        Assert.AreEqual(0, platform.Waited);
    }

    [TestMethod]
    public void Fixed_step_runs_ignore_the_frame_limit()
    {
        var game = new FrameGame(new GraphicsSettings { FrameRateLimit = 10 });
        var result = GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { MaxUpdates = 100 });
        Assert.IsNull(result.Error);
        Assert.AreEqual(100, game.Draws, "one frame per update, as fast as possible");
    }

    [TestMethod]
    public void The_screen_changes_vsync_window_display_and_limit_while_running_and_reports_them()
    {
        var (game, platform, loop) = Start(new ClockPlatform(work: 0.002, displayRate: 100),
            new GraphicsSettings { Backend = "probe", Renderers = ["vulkan"], Gpu = GpuPreference.PowerSaving });
        var screen = game.Screen;
        Assert.IsTrue(screen.CanChange);
        Assert.AreEqual(2, screen.Displays.Count);
        Assert.AreEqual("main", screen.Display?.Name);
        Assert.IsTrue(screen.VSync);
        Assert.AreEqual(100, screen.RefreshRate);

        screen.VSync = false;
        screen.WindowMode = WindowMode.Fullscreen;
        Assert.IsTrue(screen.MoveToDisplay(1));
        Assert.IsFalse(screen.MoveToDisplay(5), "a display the system does not have");
        screen.FrameRateLimit = 75;
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => screen.FrameRateLimit = -1);
        RunFor(loop, platform, 2);

        Assert.IsFalse(platform.VSync);
        Assert.AreEqual(0, screen.RefreshRate, "nothing paces frames to the display with VSync off");
        Assert.AreEqual(WindowMode.Fullscreen, platform.WindowMode);
        Assert.AreEqual("side", screen.Display?.Name);
        Assert.AreEqual(150, game.Draws, 3, "the limit applies at once");
        var saved = screen.Current;
        Assert.IsFalse(saved.VSync);
        Assert.AreEqual(75, saved.FrameRateLimit);
        Assert.AreEqual(WindowMode.Fullscreen, saved.WindowMode);
        Assert.AreEqual(1, saved.Display);
        Assert.AreEqual("probe", saved.Backend, "start-only options as the game started");
        CollectionAssert.AreEqual(new[] { "vulkan" }, saved.Renderers.ToArray());
        Assert.AreEqual(GpuPreference.PowerSaving, saved.Gpu);
    }

    [TestMethod]
    public void A_headless_screen_cannot_change_and_says_so()
    {
        var game = new FrameGame(new GraphicsSettings { WindowMode = WindowMode.Fullscreen, Display = 2 });
        game.InitializeForTest(new HeadlessPlatform());
        var screen = game.Screen;
        Assert.IsFalse(screen.CanChange);
        Assert.AreEqual(0, screen.Displays.Count);
        Assert.IsNull(screen.Display);
        Assert.IsFalse(screen.MoveToDisplay(0));
        screen.VSync = true;
        Assert.IsFalse(screen.VSync);
        Assert.IsNull(screen.Renderer);
        Assert.AreEqual(WindowMode.Fullscreen, screen.Current.WindowMode, "kept as the game asked");
        Assert.AreEqual(2, screen.Current.Display);
    }

    [TestMethod]
    public void Launcher_options_parse_and_override_only_what_they_name()
    {
        var parsed = LaunchArguments.Parse(["--renderer", "vulkan, direct3d11", "--vsync", "off", "--fps-limit", "0",
            "--window", "exclusive", "--display", "1", "--gpu", "high-performance", "--platform", "raylib"]);
        var game = new GraphicsSettings { VSync = true, FrameRateLimit = 30, WindowMode = WindowMode.Fullscreen };
        var merged = parsed.ApplyTo(game);
        CollectionAssert.AreEqual(new[] { "vulkan", "direct3d11" }, merged.Renderers.ToArray());
        Assert.IsFalse(merged.VSync);
        Assert.AreEqual(0, merged.FrameRateLimit, "0 removes the game's limit");
        Assert.AreEqual(WindowMode.ExclusiveFullscreen, merged.WindowMode);
        Assert.AreEqual(1, merged.Display);
        Assert.AreEqual(GpuPreference.HighPerformance, merged.Gpu);
        Assert.AreEqual("raylib", merged.Backend);

        var kept = LaunchArguments.Parse([]).ApplyTo(game);
        Assert.AreEqual(game, kept, "no option changes nothing");

        foreach (var bad in new[] { new[] { "--vsync", "maybe" }, ["--fps-limit", "-5"], ["--window", "big"], ["--display", "x"], ["--renderer", ","] })
            Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(bad), string.Join(' ', bad));
    }

    private static (int Code, string Errors, PlatformRequest? Request, List<string> Started) Launch(GraphicsSettings graphics, params string[] args)
    {
        var started = new List<string>();
        PlatformRequest? seen = null;
        PlatformProfile Working(string name) => new(name, "", request => { started.Add(name); seen = request; return new HeadlessPlatform(); });
        var catalog = new PlatformCatalog()
            .Add(Working("first"))
            .Add(new PlatformProfile("broken", "", _ => throw new InvalidOperationException("broken has no display")))
            .Add(Working("second"));
        var errors = new StringWriter();
        var previousError = Console.Error;
        var previousOut = Console.Out;
        Console.SetError(errors);
        Console.SetOut(new StringWriter());
        try
        {
            int code = GameLauncher.Run(new FrameGame(graphics), [.. args, "--frames", "1"], catalog);
            return (code, errors.ToString(), seen, started);
        }
        finally
        {
            Console.SetError(previousError);
            Console.SetOut(previousOut);
        }
    }

    [TestMethod]
    public void The_game_s_graphics_options_reach_the_platform_with_the_command_line_on_top()
    {
        var graphics = new GraphicsSettings { VSync = false, Renderers = ["opengl"], Display = 1 };
        var (code, errors, request, _) = Launch(graphics, "--window", "fullscreen");
        Assert.AreEqual(0, code, errors);
        Assert.IsFalse(request!.Settings.Graphics.VSync);
        CollectionAssert.AreEqual(new[] { "opengl" }, request.Settings.Graphics.Renderers.ToArray());
        Assert.AreEqual(1, request.Settings.Graphics.Display);
        Assert.AreEqual(WindowMode.Fullscreen, request.Settings.Graphics.WindowMode);
    }

    [TestMethod]
    public void A_preferred_backend_starts_first_and_falls_back_when_it_cannot()
    {
        var (code, errors, _, started) = Launch(new GraphicsSettings { Backend = "second" });
        Assert.AreEqual(0, code, errors);
        CollectionAssert.AreEqual(new[] { "second" }, started);

        (code, errors, _, started) = Launch(new GraphicsSettings { Backend = "broken" });
        Assert.AreEqual(0, code, errors);
        CollectionAssert.AreEqual(new[] { "first" }, started);
        StringAssert.Contains(errors, "platform: broken could not start (broken has no display); trying first");

        (code, errors, _, started) = Launch(new GraphicsSettings { Backend = "vulkan9" });
        Assert.AreEqual(0, code, errors);
        CollectionAssert.AreEqual(new[] { "first" }, started);
        StringAssert.Contains(errors, "the game prefers 'vulkan9', which is not in this build; choosing automatically");

        // Named on the command line, a platform does not fall back.
        (code, errors, _, started) = Launch(new GraphicsSettings { Backend = "second" }, "--platform", "broken");
        Assert.AreEqual(1, code);
        Assert.AreEqual(0, started.Count);
        StringAssert.Contains(errors, "broken has no display");
    }

    [TestMethod]
    public void The_graphics_option_names_the_device_and_overrides_the_game_s()
    {
        var merged = LaunchArguments.Parse(["--graphics", "sdl-gpu"]).ApplyTo(new GraphicsSettings { Device = "sdl-renderer" });
        Assert.AreEqual("sdl-gpu", merged.Device);
        Assert.AreEqual("sdl-renderer", LaunchArguments.Parse([]).ApplyTo(new GraphicsSettings { Device = "sdl-renderer" }).Device, "kept without the option");
        Assert.IsNull(LaunchArguments.Parse([]).ApplyTo(new GraphicsSettings()).Device, "the platform's default by default");
        Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(["--graphics"]), "needs a value");
        StringAssert.Contains(MicroEngine.Hosting.LaunchRequest.Usage, "[--graphics NAME]");
    }

    private static (int Code, string Errors, PlatformRequest? Request) LaunchWithDevices(GraphicsSettings graphics, PlatformProfile[] profiles, params string[] args)
    {
        var catalog = new PlatformCatalog();
        foreach (var profile in profiles) catalog.Add(profile);
        var errors = new StringWriter();
        var (previousError, previousOut) = (Console.Error, Console.Out);
        Console.SetError(errors);
        Console.SetOut(new StringWriter());
        try { return (GameLauncher.Run(new FrameGame(graphics), [.. args, "--frames", "1"], catalog), errors.ToString(), Seen); }
        finally
        {
            Console.SetError(previousError);
            Console.SetOut(previousOut);
        }
    }

    private static PlatformRequest? Seen;

    private static PlatformProfile Offering(string name, params string[] devices) =>
        new(name, "", request => { Seen = request; return new HeadlessPlatform(); }) { GraphicsDevices = devices };

    [TestMethod]
    public void A_graphics_device_reaches_only_a_platform_that_offers_it()
    {
        // The first platform offers only its own device; the second offers "sdl-gpu": a game asking for it starts on the
        // second, with the device in its request.
        Seen = null;
        var (code, errors, request) = LaunchWithDevices(new GraphicsSettings { Device = "sdl-gpu" }, [Offering("plain"), Offering("gpu", "sdl-renderer", "sdl-gpu")]);
        Assert.AreEqual(0, code, errors);
        Assert.AreEqual("sdl-gpu", request!.Settings.Graphics.Device);
        StringAssert.Contains(errors, "Platform 'plain' has no graphics device 'sdl-gpu'");
    }

    [TestMethod]
    public void A_graphics_device_no_platform_offers_is_a_clear_error_never_another_device()
    {
        Seen = null;
        var (code, errors, request) = LaunchWithDevices(new GraphicsSettings(), [Offering("plain"), Offering("other", "sdl-renderer")], "--graphics", "sdl-gpu");
        Assert.AreEqual(1, code);
        Assert.IsNull(request, "no platform started on another device");
        StringAssert.Contains(errors, "has no graphics device 'sdl-gpu'");

        // Named with --platform, the refusal names what the platform offers.
        (code, errors, _) = LaunchWithDevices(new GraphicsSettings(), [Offering("other", "sdl-renderer")], "--platform", "other", "--graphics", "sdl-gpu");
        Assert.AreEqual(1, code);
        StringAssert.Contains(errors, "Platform 'other' has no graphics device 'sdl-gpu'; it offers: sdl-renderer.");
    }

    [TestMethod]
    public void Without_a_graphics_device_every_platform_starts_as_before()
    {
        Seen = null;
        var (code, errors, request) = LaunchWithDevices(new GraphicsSettings(), [Offering("plain")]);
        Assert.AreEqual(0, code, errors);
        Assert.IsNull(request!.Settings.Graphics.Device);
    }
}
