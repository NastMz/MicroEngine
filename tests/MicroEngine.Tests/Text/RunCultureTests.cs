using System.Globalization;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>
/// A run nobody plays takes nothing from the machine, the way numbers are written included: its game runs with the
/// invariant culture, so a capture shows "3.5" on every machine. A run somebody plays keeps the player's culture.
/// </summary>
[TestClass]
[DoNotParallelize] // sets the thread's culture
public sealed class RunCultureTests
{
    private sealed class Probe : Scene
    {
        public string Updated = "", Drawn = "", Entered = "";
        public bool Throw;
        public override void Enter() => Entered = Number();
        public override void Update(float dt)
        {
            Updated = Number();
            if (Throw) throw new InvalidOperationException("the game failed");
        }
        public override void Draw(IRenderer renderer) => Drawn = Number();
    }

    private sealed class ProbeGame(Probe scene) : Game
    {
        public string Loaded = "", Unloaded = "";
        public override GameSettings Settings { get; } = new("run-culture");
        protected override void Load()
        {
            Loaded = Number();
            Scenes.Push(scene);
        }
        protected override void Unload() => Unloaded = Number();
    }

    /// <summary>What a game shows for three and a half, as text: a string and a text drawn without one agree.</summary>
    private static string Number()
    {
        float value = 3.5f;
        string text = $"{value}";
        Assert.AreEqual(text, Read($"{value}"), "the interpolated text follows the same culture as the string");
        return text;
    }

    private static string Read(InterpolatedText text)
    {
        string s = text.ToString();
        text.Dispose();
        return s;
    }

    /// <summary>The headless platform without what marks it as a platform nobody plays on.</summary>
    private sealed class PlayedPlatform(HeadlessPlatform inner) : IPlatform
    {
        public IRenderer BeginFrame() => inner.BeginFrame();
        public void EndFrame() => inner.EndFrame();
        public IGraphicsDevice Graphics => inner.Graphics;
        public MicroEngine.Audio.IAudioDevice Audio => inner.Audio;
        public MicroEngine.Input.IInputSource Input => inner.Input;
        public IMonotonicClock Clock => inner.Clock;
        public bool PumpEvents(IEngineEventSink sink) => inner.PumpEvents(sink);
        public ISurface? MainSurface => inner.MainSurface;
        public bool CanBlock => inner.CanBlock;
        public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class
        {
            capability = null;
            return typeof(T) != typeof(RunPreset) && inner.TryGetCapability(out capability);
        }
        public Color Letterbox { get => inner.Letterbox; set => inner.Letterbox = value; }
        public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
        public void ToggleFullscreen() => inner.ToggleFullscreen();
        public void Dispose() => inner.Dispose();
    }

    private static void UnderGerman(Action test)
    {
        var culture = CultureInfo.CurrentCulture;
        var ui = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.AreEqual("3,5", Number(), "the machine writes a decimal comma");
            test();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = ui;
        }
    }

    private static void AssertGerman(string when)
    {
        Assert.AreEqual("de-DE", CultureInfo.CurrentCulture.Name, $"the thread's culture {when}");
        Assert.AreEqual("de-DE", CultureInfo.CurrentUICulture.Name, $"the thread's interface culture {when}");
        Assert.AreEqual("3,5", Number(), $"numbers {when}");
    }

    [TestMethod]
    public void A_stepped_game_on_the_headless_platform_writes_numbers_the_same_on_every_machine() => UnderGerman(() =>
    {
        var scene = new Probe();
        var game = new ProbeGame(scene);
        game.InitializeForTest(new HeadlessPlatform());
        Assert.AreEqual("3.5", game.Loaded, "Load");
        Assert.AreEqual("3.5", scene.Entered, "Enter");

        game.StepForTest();
        Assert.AreEqual("3.5", scene.Updated, "Update");
        Assert.AreEqual("3.5", Number(), "a stepped game has no end: the thread formats as the game does while the test goes on");
        Assert.AreEqual("de-DE", CultureInfo.CurrentUICulture.Name, "the interface culture, which localization may read, is left alone");
    });

    [TestMethod]
    public void A_run_nobody_plays_writes_numbers_the_same_on_every_machine_and_gives_the_culture_back() => UnderGerman(() =>
    {
        var scene = new Probe();
        var game = new ProbeGame(scene);
        using var platform = new PlayedPlatform(new HeadlessPlatform());
        // A window in real time that ends by itself after three updates: a check, not somebody playing.
        var result = GameRunner.Run(game, platform, new RunOptions { TimeMode = TimeMode.RealTime, MaxUpdates = 3, Mute = true });
        Assert.IsNull(result.Error, result.Error?.ToString());
        AssertGerman("after the run");
        Assert.AreEqual("3.5", game.Loaded, "Load");
        Assert.AreEqual("3.5", scene.Updated, "Update");
        Assert.AreEqual("3.5", scene.Drawn, "Draw");
        Assert.AreEqual("3.5", game.Unloaded, "Unload");
    });

    [TestMethod]
    public void A_run_somebody_plays_keeps_the_players_culture() => UnderGerman(() =>
    {
        var scene = new Probe();
        var game = new ProbeGame(scene);
        using var platform = new PlayedPlatform(new HeadlessPlatform());
        game.InitializeForTest(platform);
        game.StepForTest();
        Assert.AreEqual("3,5", game.Loaded, "Load");
        Assert.AreEqual("3,5", scene.Updated, "Update");
        AssertGerman("during a played run");
    });

    [TestMethod]
    public void A_run_that_fails_still_gives_the_culture_back() => UnderGerman(() =>
    {
        var scene = new Probe { Throw = true };
        var game = new ProbeGame(scene);
        using var platform = new PlayedPlatform(new HeadlessPlatform());
        var result = GameRunner.Run(game, platform, new RunOptions { TimeMode = TimeMode.Deterministic, MaxUpdates = 3, Mute = true });
        Assert.IsNotNull(result.Error);
        Assert.AreEqual("3.5", scene.Updated);
        AssertGerman("after a run that failed");
    });
}
