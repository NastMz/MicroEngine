using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>DEBUG-1: the crash report of a failed run, and platform messages reaching the game's log.</summary>
[TestClass]
public sealed class CrashReportTests
{
    private sealed class FailingGame : Game
    {
        public override GameSettings Settings { get; } = new("crash test");
        protected override void Load()
        {
            Diagnostics.Enabled = true;
            Scenes.Push(new Failing());
        }

        private sealed class Failing : Scene
        {
            private int _updates;
            public override void Update(float dt)
            {
                if (++_updates == 3) Explode();
            }
            private static void Explode() => throw new InvalidOperationException("the boss fell out of the world");
            public override void Draw(Graphics.IRenderer r) { }
        }
    }

    [TestMethod]
    public void A_failed_run_writes_a_report_with_the_error_its_stack_and_the_last_frame_and_names_it()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"me-crash-{Guid.NewGuid():N}");
        var errors = new StringWriter();
        try
        {
            var environment = new HostEnvironment(HostKind.Test) { Out = new StringWriter(), Error = errors, CrashDirectory = directory };
            var host = GameHost.Create(environment, new LaunchRequest { Headless = true, Frames = 10 }, h => h.UseGame(() => new FailingGame()));
            Assert.AreEqual(1, host.Run());
            var line = errors.ToString().Split('\n').Single(l => l.StartsWith("crash report: ", StringComparison.Ordinal)).Trim();
            var path = line["crash report: ".Length..];
            Assert.AreEqual(directory, Path.GetDirectoryName(path));
            var report = File.ReadAllText(path);
            StringAssert.StartsWith(report, "MicroEngine crash report\n");
            StringAssert.Contains(report, "\ngame: ");
            StringAssert.Contains(report, "the boss fell out of the world");
            StringAssert.Contains(report, "Explode", "the stack trace names the method that threw");
            StringAssert.Contains(report, "last frame: #", "diagnostics were on, so the last frame is described");
            StringAssert.Contains(report, "engine.scenes");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Without_diagnostics_the_report_says_the_frame_was_not_measured()
    {
        var game = new HeadlessGame();
        game.InitializeForTest(new HeadlessPlatform());
        var report = CrashReport.Describe(game, new InvalidOperationException("boom"));
        StringAssert.Contains(report, "last frame: not measured");
        StringAssert.Contains(report, "System.InvalidOperationException: boom");
    }

    private sealed class HeadlessGame : Game
    {
        public override GameSettings Settings { get; } = new("plain");
        protected override void Load() { }
    }

    private sealed class Lines : ILog
    {
        public List<string> Written { get; } = [];
        public void Write(LogLevel level, string category, string message) => Written.Add($"{level} {category}: {message}");
    }

    private sealed class LoggedGame : Game
    {
        public Lines Lines { get; } = new();
        public override GameSettings Settings { get; } = new("logged");
        public override void Configure(EngineBuilder builder) => builder.Services.Replace<ILog>(Lines);
        protected override void Load() { }
    }

    [TestMethod]
    public void What_the_platform_reports_before_the_game_starts_reaches_the_game_log()
    {
        var game = new LoggedGame();
        var environment = new HostEnvironment(HostKind.Test) { Out = new StringWriter(), Error = new StringWriter() };
        var host = GameHost.Create(environment, new LaunchRequest { Headless = true, Frames = 1 }, h => h.UseGame(() => game)
            .ConfigureRequest(r => { r.Log.Write(LogLevel.Warning, "audio", "no device available, continuing silently"); return r; }));
        Assert.AreEqual(0, host.Run());
        CollectionAssert.Contains(game.Lines.Written, "Warning audio: no device available, continuing silently");
        Assert.AreEqual("", environment.Error.ToString(), "nothing went around the game's log");
    }
}
