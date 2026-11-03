using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Kernel;

/// <summary>The launcher's time and sound options are explicit: a hidden window implies neither.</summary>
[TestClass]
[DoNotParallelize] // the launcher writes to the console
public sealed class LauncherTimeTests
{
    private sealed class Quiet : Game
    {
        public override GameSettings Settings { get; } = new("launch");
        protected override void Load() => Scenes.Push(new Idle());
        private sealed class Idle : Scene
        {
            public override void Update(float dt) { }
            public override void Draw(IRenderer renderer) { }
        }
    }

    /// <summary>Runs the launcher on a catalog whose only platform records the request (a headless platform under another name).</summary>
    private static (PlatformRequest Request, Game Game, int Code) Launch(params string[] args)
    {
        PlatformRequest? seen = null;
        var catalog = new PlatformCatalog().Add(new PlatformProfile("probe", "records the request", request =>
        {
            seen = request;
            return HeadlessPlatform.For(request);
        }));
        var game = new Quiet();
        var output = Console.Out;
        Console.SetOut(new StringWriter());
        try
        {
            int code = GameLauncher.Run(game, [.. args, "--platform", "probe", "--frames", "3"], catalog);
            return (seen!, game, code);
        }
        finally { Console.SetOut(output); }
    }

    private static string Script()
    {
        var path = Path.Combine(Path.GetTempPath(), $"microengine-launch-{Guid.NewGuid():N}.script");
        File.WriteAllText(path, "2 press confirm\n");
        return path;
    }

    [TestMethod]
    public void Time_parses_realtime_and_deterministic_and_rejects_anything_else()
    {
        Assert.AreEqual(TimeMode.RealTime, LaunchArguments.Parse(["--time", "realtime"]).Time);
        Assert.AreEqual(TimeMode.Deterministic, LaunchArguments.Parse(["--time", "deterministic"]).Time);
        Assert.IsNull(LaunchArguments.Parse([]).Time, "no choice: the game's, else real time");
        Assert.IsNull(LaunchArguments.Parse(["--hidden"]).Time, "a hidden window implies nothing about time");
        Assert.AreEqual("--time needs realtime or deterministic, not 'fast'.",
            Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(["--time", "fast"])).Message);
        StringAssert.Contains(GameLauncher.Usage, "[--time realtime|deterministic]");
    }

    [TestMethod]
    public void A_script_means_deterministic_time_unless_time_says_otherwise()
    {
        string script = Script();
        try
        {
            Assert.AreEqual(TimeMode.Deterministic, LaunchArguments.Parse(["--script", script]).Time);
            Assert.AreEqual(TimeMode.RealTime, LaunchArguments.Parse(["--script", script, "--time", "realtime"]).Time);
            Assert.AreEqual(TimeMode.RealTime, LaunchArguments.Parse(["--time", "realtime", "--script", script]).Time, "order does not matter");
        }
        finally { File.Delete(script); }
    }

    [TestMethod]
    public void A_hidden_run_is_real_time_and_may_open_a_sound_device()
    {
        var (request, game, code) = Launch("--hidden");
        Assert.AreEqual(0, code);
        Assert.IsTrue(request.Hidden);
        Assert.IsTrue(request.WaitForDisplay, "hidden implies nothing about time or pacing");
        Assert.IsTrue(request.OpensOwnAudio, "hidden no longer implies silence");
        Assert.AreEqual(TimeMode.Deterministic, game.TimeSettings.Mode, "no choice: this headless platform's preset decides");
    }

    [TestMethod]
    public void A_deterministic_run_asks_the_platform_explicitly_for_no_display_wait_and_no_sound()
    {
        var (request, game, code) = Launch("--hidden", "--time", "deterministic");
        Assert.AreEqual(0, code);
        Assert.IsFalse(request.WaitForDisplay, "the launcher's explicit option for deterministic runs");
        Assert.IsFalse(request.StartGamepadsAfterFirstFrame, "no start screen in deterministic time");
        Assert.AreEqual(AudioOutput.Silent, request.AudioOutput, "--audio auto: a deterministic run is not heard");
        Assert.IsFalse(request.OpensOwnAudio);
        Assert.AreEqual(TimeMode.Deterministic, game.TimeSettings.Mode, "the time mode goes to the loop only");

        var (real, realGame, _) = Launch("--time", "realtime");
        Assert.IsTrue(real.WaitForDisplay);
        Assert.AreEqual(AudioOutput.Automatic, real.AudioOutput);
        Assert.AreEqual(TimeMode.RealTime, realGame.TimeSettings.Mode);
    }

    [TestMethod]
    public void A_headless_run_is_deterministic_unless_time_says_otherwise()
    {
        var headless = new Quiet();
        var output = Console.Out;
        Console.SetOut(new StringWriter());
        try
        {
            Assert.AreEqual(0, GameLauncher.Run(headless, ["--headless", "--frames", "3"], new PlatformCatalog()));
            Assert.AreEqual(TimeMode.Deterministic, headless.TimeSettings.Mode, "--headless: one step per frame");
            Assert.AreEqual(3, headless.UpdateCount);
            var real = new Quiet();
            Assert.AreEqual(0, GameLauncher.Run(real, ["--headless", "--time", "realtime", "--frames", "3"], new PlatformCatalog()));
            Assert.AreEqual(TimeMode.RealTime, real.TimeSettings.Mode);
        }
        finally { Console.SetOut(output); }
    }

    [TestMethod]
    public void Silence_stays_explicit()
    {
        Assert.IsFalse(Launch("--hidden", "--audio", "silent").Request.OpensOwnAudio);
        Assert.IsFalse(Launch("--hidden", "--mute").Request.OpensOwnAudio);
        Assert.IsTrue(Launch("--time", "deterministic", "--audio", "device").Request.OpensOwnAudio);
    }
}
