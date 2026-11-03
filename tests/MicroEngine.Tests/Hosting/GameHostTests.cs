using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Hosting;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Hosting;

/// <summary>
/// One composition path for every host: the launch rules (a script means deterministic time and seed 1), the game's
/// <c>Configure</c> entry point reaching desktop, browser-style sessions and tests alike, and per-host defaults instead of
/// process-wide statics.
/// </summary>
[TestClass]
[DoNotParallelize] // the desktop launcher writes to the console
public sealed class GameHostTests
{
    private const string Script = "2 press confirm\n3 quit\n";

    private sealed class Idle : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    /// <summary>Records what a run showed the game: its content folder (or why there was none) and its time mode.</summary>
    private sealed class Probe(string? contentRoot = null) : Game
    {
        public override GameSettings Settings { get; } = new("probe");
        protected override string? ContentRoot => contentRoot;
        public string? Content { get; private set; }
        public Marker? Marker { get; private set; }
        protected override void Load()
        {
            try { Content = Assets.Content.Root; }
            catch (AssetLoadException e) { Content = e.Message; }
            Services.TryGet<Marker>(out var marker);
            Marker = marker;
            Scenes.Push(new Idle());
        }
    }

    private sealed class Marker(string host) { public string Host { get; } = host; }

    /// <summary>A platform of a real-time host (no deterministic preset), like a browser page or a phone.</summary>
    private sealed class RealTimePlatform(HeadlessPlatform inner) : TestPlatform(inner, realTime: true);

    private static PlatformCatalog RealTimeCatalog(List<PlatformRequest>? requests = null) => new PlatformCatalog().Add(new PlatformProfile("realtime", "a real-time host", request =>
    {
        requests?.Add(request);
        return new RealTimePlatform(HeadlessPlatform.For(request));
    }));

    private static HostEnvironment Environment(HostKind kind, Func<string, string?>? variables = null) =>
        new(kind) { Variables = variables ?? (_ => null), Out = new StringWriter(), Error = new StringWriter(), ClockSeed = () => 77 };

    private static string Folder()
    {
        string root = Path.Combine(Path.GetTempPath(), "microengine-host-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    [TestMethod]
    public void A_script_means_deterministic_time_seed_one_no_display_wait_and_no_sound_device_on_every_host()
    {
        foreach (var kind in Enum.GetValues<HostKind>())
        {
            var host = GameHost.Create(Environment(kind), new LaunchRequest { ScriptText = Script, ScriptName = "s", CaptureDirectory = "/captures" },
                h => h.UseGame(() => new Probe()), h => h.UsePlatforms(RealTimeCatalog()));
            var game = host.CreateGame();
            var options = host.CreateRunOptions(game);
            Assert.AreEqual(TimeMode.Deterministic, options.TimeMode, $"{kind}: a scripted run is deterministic");
            Assert.AreEqual(1, options.Seed, kind.ToString());
            Assert.AreEqual("/captures", options.CaptureDirectory, kind.ToString());
            Assert.IsNotNull(options.Script, kind.ToString());
            var request = host.CreateRequest(game, options);
            Assert.IsFalse(request.WaitForDisplay, kind.ToString());
            Assert.AreEqual(AudioOutput.Silent, request.AudioOutput, kind.ToString());
            Assert.IsFalse(request.StartGamepadsAfterFirstFrame, kind.ToString());
        }

        var plain = GameHost.Create(Environment(HostKind.Browser), new LaunchRequest(), h => h.UseGame(() => new Probe()));
        var unscripted = plain.CreateRunOptions(plain.CreateGame());
        Assert.IsNull(unscripted.TimeMode, "no script: the game's choice, then the platform's preset, then real time");
        Assert.AreEqual(77, unscripted.Seed, "no script: the host's clock seed");
        Assert.IsTrue(plain.CreateRequest(plain.CreateGame(), unscripted).WaitForDisplay);

        var chosen = GameHost.Create(Environment(HostKind.Browser), new LaunchRequest { ScriptText = Script, Time = TimeMode.RealTime, Seed = 5 }, h => h.UseGame(() => new Probe()));
        var options2 = chosen.CreateRunOptions(chosen.CreateGame());
        Assert.AreEqual(TimeMode.RealTime, options2.TimeMode, "an explicit time mode wins over the script's");
        Assert.AreEqual(5, options2.Seed, "an explicit seed wins over the script's");
    }

    [TestMethod]
    public void A_launch_that_asks_for_a_sound_device_keeps_it_in_deterministic_time()
    {
        // Phones ask for a device on every run: their package checks prove one opens.
        var host = GameHost.Create(Environment(HostKind.Android), new LaunchRequest { ScriptText = Script, Audio = "device" }, h => h.UseGame(() => new Probe()));
        var game = host.CreateGame();
        Assert.AreEqual(AudioOutput.Device, host.CreateRequest(game, host.CreateRunOptions(game)).AudioOutput);
    }

    [TestMethod]
    public void A_page_driven_session_runs_a_script_one_fixed_step_per_frame_on_a_real_time_platform()
    {
        // The browser host's model: a session composed per launch, on a platform that states no deterministic preset.
        var session = GameHost.CreateSession(request => GameHost.Create(Environment(HostKind.Browser), request,
            h => h.UseGame(() => new Probe()), h => h.UsePlatforms(RealTimeCatalog())));
        Assert.IsTrue(session.Start(new LaunchRequest { ScriptText = "40 quit\n" }), session.Result?.Error?.ToString());
        var game = session.Game!;
        Assert.AreEqual(TimeMode.Deterministic, game.TimeSettings.Mode, "a scripted run in a page is deterministic, as on desktop");
        int frames = 0;
        while (session.Tick())
        {
            frames++;
            Assert.AreEqual(1f, game.FrameBlend, "every frame shows exactly the last update");
            Assert.AreEqual(frames, game.UpdateCount, "one step per frame, whatever the wall clock says");
        }
        Assert.IsNull(session.Result!.Error);
        Assert.AreEqual(40, session.Result.Updates);

        Assert.IsTrue(session.Start(new LaunchRequest()));
        Assert.AreEqual(TimeMode.RealTime, session.Game!.TimeSettings.Mode, "a run without a script on a real-time host keeps real time");
        session.Stop();
    }

    [TestMethod]
    public void The_composition_a_game_configures_reaches_the_desktop_launcher_page_sessions_and_tests()
    {
        var games = new List<Probe>();
        var requests = new List<PlatformRequest>();
        var extra = new HeadlessInput();
        // One Configure, as a game writes it once: its game, an engine service and a request change.
        void Configure(HostBuilder host)
        {
            host.UseGame(() => { var g = new Probe(); games.Add(g); return g; });
            host.ConfigureEngine(engine => engine.Services.Add(new Marker(host.Kind.ToString())));
            host.ConfigureRequest(request => request with { ExtraInput = [extra] });
        }

        var output = Console.Out;
        Console.SetOut(new StringWriter());
        try { Assert.AreEqual(0, GameLauncher.Run(["--platform", "realtime", "--frames", "3"], Configure, h => h.UsePlatforms(RealTimeCatalog(requests)))); }
        finally { Console.SetOut(output); }

        var session = GameHost.CreateSession(request => GameHost.Create(Environment(HostKind.Browser), request, Configure, h => h.UsePlatforms(RealTimeCatalog(requests))));
        Assert.IsTrue(session.Start(new LaunchRequest { ScriptText = "3 quit\n" }));
        while (session.Tick()) { }
        Assert.IsNull(session.Result!.Error);

        var test = GameHost.Create(Environment(HostKind.Test), new LaunchRequest { Frames = 3 }, Configure, h => h.UsePlatforms(RealTimeCatalog(requests)));
        Assert.IsNull(test.Play().Error);

        CollectionAssert.AreEqual(new[] { "Desktop", "Browser", "Test" }, games.Select(g => g.Marker?.Host).ToArray(), "each host's engine got the composition's service");
        Assert.AreEqual(3, requests.Count);
        Assert.IsTrue(requests.All(r => r.ExtraInput.Contains(extra)), "each host's platform request got the composition's change");
    }

    [TestMethod]
    public void Two_hosts_in_one_process_keep_their_own_content_folders()
    {
        string first = Folder(), second = Folder();
        try
        {
            Probe Play(string? content)
            {
                Probe? game = null;
                var host = GameHost.Create(Environment(HostKind.Test), new LaunchRequest { Frames = 1 }, h =>
                {
                    h.UseGame(() => game = new Probe());
                    if (content is not null) h.Content = new FolderContentSource(content);
                }, h => h.UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile)));
                Assert.IsNull(host.Play().Error);
                return game!;
            }
            Assert.AreEqual(Path.GetFullPath(first), Play(first).Content);
            Assert.AreEqual(Path.GetFullPath(second), Play(second).Content);
            var plain = Play(null).Content!;
            Assert.IsTrue(plain == Path.GetFullPath(ContentFolder.DefaultRoot) || plain.Contains(ContentFolder.DefaultRoot), $"a host without content keeps the default folder: {plain}");
        }
        finally
        {
            Directory.Delete(first, recursive: true);
            Directory.Delete(second, recursive: true);
        }
    }

    [TestMethod]
    public void A_game_that_names_its_content_folder_wins_over_the_composition()
    {
        string own = Folder(), composed = Folder();
        try
        {
            Probe? game = null;
            var host = GameHost.Create(Environment(HostKind.Test), new LaunchRequest { Frames = 1 }, h =>
            {
                h.UseGame(() => game = new Probe(own));
                h.Content = new FolderContentSource(composed);
            }, h => h.UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile)));
            Assert.IsNull(host.Play().Error);
            Assert.AreEqual(Path.GetFullPath(own), game!.Content);
        }
        finally
        {
            Directory.Delete(own, recursive: true);
            Directory.Delete(composed, recursive: true);
        }
    }

    private sealed class Archive : IContentSource
    {
        public string Location => "archive.zip";
        public bool Exists(string path) => false;
        public Stream Open(string path) => throw new FileNotFoundException(path);
        public IReadOnlyList<string> Files(string folder) => [];
    }

    [TestMethod]
    public void Content_that_is_not_a_folder_is_refused_with_its_reason()
    {
        var host = GameHost.Create(Environment(HostKind.Test), new LaunchRequest { Frames = 1 }, h =>
        {
            h.UseGame(() => new Probe());
            h.Content = new Archive();
        }, h => h.UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile)));
        var error = host.Play().Error;
        Assert.IsInstanceOfType<NotSupportedException>(error);
        StringAssert.Contains(error.Message, "archive.zip");
    }

    [TestMethod]
    public void A_composition_that_chooses_no_game_is_an_error_and_the_launcher_exits_with_1()
    {
        var e = Assert.ThrowsExactly<InvalidOperationException>(() => GameHost.Create(Environment(HostKind.Test), new LaunchRequest(), _ => { }));
        StringAssert.Contains(e.Message, "host.UseGame");
        var errors = Console.Error;
        var captured = new StringWriter();
        Console.SetError(captured);
        try { Assert.AreEqual(1, GameLauncher.Run(["--frames", "1"], _ => { })); }
        finally { Console.SetError(errors); }
        StringAssert.Contains(captured.ToString(), "error: The composition chose no game");
    }

    [TestMethod]
    public void The_platform_variable_comes_from_the_host_environment_not_the_process()
    {
        var catalog = new PlatformCatalog().Add(new PlatformProfile("a", "", HeadlessPlatform.For)).Add(new PlatformProfile("b", "", HeadlessPlatform.For));
        var named = ProfileSelection.Choose(catalog.Profiles, new LaunchRequest(), Environment(HostKind.Test, n => n == ProfileSelection.PlatformVariable ? "b" : null), null);
        CollectionAssert.AreEqual(new[] { "b" }, named.Candidates.Select(p => p.Name).ToArray());
        var unnamed = ProfileSelection.Choose(catalog.Profiles, new LaunchRequest(), Environment(HostKind.Test), null);
        CollectionAssert.AreEqual(new[] { "a", "b" }, unnamed.Candidates.Select(p => p.Name).ToArray());
        var unknown = ProfileSelection.Choose(catalog.Profiles, new LaunchRequest { Platform = "c" }, Environment(HostKind.Test), null);
        Assert.AreEqual("Unknown platform 'c'. Available platforms: a, b.", unknown.Error);
        var preferred = ProfileSelection.Choose(catalog.Profiles, new LaunchRequest(), Environment(HostKind.Test), "z");
        CollectionAssert.AreEqual(new[] { "platform: the game prefers 'z', which is not in this build; choosing automatically" }, preferred.Notes.ToArray());
    }

    [TestMethod]
    public void A_launch_request_parses_the_command_line_into_words_and_names_its_script()
    {
        string script = Path.Combine(Path.GetTempPath(), $"microengine-host-{Guid.NewGuid():N}.script");
        File.WriteAllText(script, Script);
        try
        {
            var request = LaunchRequest.Parse(["--script", script, "--gpu", "power-saving", "--window", "fullscreen", "--audio", "silent"]);
            Assert.AreEqual(Path.GetFileName(script), request.ScriptName);
            Assert.AreEqual(("power-saving", "fullscreen", "silent"), (request.Gpu, request.Window, request.Audio));
            Assert.AreEqual(TimeMode.Deterministic, request.EffectiveTime);
            Assert.AreEqual(1, request.ResolveSeed(77));
            var typed = LaunchArguments.From(request, 77);
            Assert.AreEqual((GpuPreference.PowerSaving, WindowMode.Fullscreen, AudioOutput.Silent, 1), (typed.Gpu, typed.WindowMode, typed.Audio, typed.Seed));
        }
        finally { File.Delete(script); }
        Assert.AreEqual(77, LaunchRequest.Parse([]).ResolveSeed(77));
        Assert.AreEqual("--window needs windowed, fullscreen or exclusive, not 'tiny'.", Assert.ThrowsExactly<FormatException>(() => LaunchRequest.Parse(["--window", "tiny"])).Message);
        Assert.AreEqual("Script 'x.script' not found.", Assert.ThrowsExactly<FormatException>(() => LaunchRequest.Parse(["--script", "x.script"], _ => false)).Message);
    }
}
