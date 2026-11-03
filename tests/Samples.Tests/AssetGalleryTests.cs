using AssetGallery;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace Samples.Tests;

/// <summary>The asset pipeline's consumer, played headlessly with its own content files (copied next to the tests).</summary>
[TestClass]
public sealed class AssetGalleryTests
{
    private static void Press(GalleryGame game, string action)
    {
        game.Input.SetForced(action, true);
        game.StepForTest();
        game.Input.SetForced(action, false);
        game.StepForTest();
    }

    [TestMethod]
    public void TheGalleryLoadsEveryKindOfAssetFromFiles()
    {
        var game = new GalleryGame();
        game.InitializeForTest(new RealTimePlatform(HeadlessPlatform.For(game.Settings)));
        Assert.AreEqual(TextureFilter.Linear, game.Players.Texture.Filter, "an HD game samples linearly by default");
        Assert.AreEqual(5, game.Players.Frames("playerBlue_walk").Count);
        Assert.AreEqual(12, game.Enemies.Animations.Count);
        Assert.IsTrue(game.Items.Contains("blueGem"));
        Assert.IsTrue(game.Ui.GlyphsRasterized > 0 && game.Bold.GlyphsRasterized > 0 && game.Serif.GlyphsRasterized > 0, "fonts prepared while loading");
        Assert.AreEqual(9, game.Assets.Statistics.Count, "three images, three atlases, three fonts");
        Assert.IsNull(game.LevelAhead, "an interactive run loads the level during play, in the background");
    }

    [TestMethod]
    public void PlayingAndDrawingLoadsNothingNew()
    {
        var game = new GalleryGame();
        var platform = HeadlessPlatform.For(game.Settings);
        // The profiler records the cache's numbers just before the game shuts down.
        var profiler = new FrameProfiler();
        var result = GameRunner.Run(game, platform, new RunOptions { Script = new InputScript().QuitAfter(30), Profiler = profiler });
        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.IsTrue(platform.Renderer.DrawCalls > 0);
        Assert.AreEqual("0", profiler.Environment["assets.loadsDuringPlay"], "everything the screen draws was loaded in Load");
        Assert.AreEqual("10", profiler.Environment["assets.loaded"], "nine for the screen and, in a fixed-step run, the level's atlas");
    }

    /// <summary>A headless platform, run in real time as a desktop window is (its clock is the wall clock).</summary>
    private sealed class RealTimePlatform(HeadlessPlatform inner) : IPlatform
    {
        public IGraphicsDevice Graphics => inner.Graphics;
        public MicroEngine.Audio.IAudioDevice Audio => inner.Audio;
        public MicroEngine.Input.IInputSource Input => inner.Input;
        public MicroEngine.Platform.IMonotonicClock Clock => inner.Clock;
        public bool PumpEvents(MicroEngine.Platform.IEngineEventSink sink) => inner.PumpEvents(sink);
        public MicroEngine.Platform.ISurface? MainSurface => inner.MainSurface;
        public bool CanBlock => inner.CanBlock;
        // A desktop window states no run preset: runs that choose nothing are real-time.
        public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class =>
            typeof(T) == typeof(RunPreset) ? (capability = (T)(object)new RunPreset()) is not null : inner.TryGetCapability(out capability);
        public MicroEngine.Color Letterbox { get => inner.Letterbox; set => inner.Letterbox = value; }
        public IRenderer BeginFrame() => inner.BeginFrame();
        public void EndFrame() => inner.EndFrame();
        public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
        public void ToggleFullscreen() { }
        public void Dispose() => inner.Dispose();
    }

    /// <summary>Plays the gallery's tour on the CPU reference rasterizer and returns each capture's bytes, by name.</summary>
    private static Dictionary<string, byte[]> PlayTour()
    {
        var directory = Directory.CreateTempSubdirectory("asset-gallery-tour-");
        try
        {
            var game = new GalleryGame();
            var platform = HeadlessPlatform.For(game.Settings, rasterize: true);
            var script = InputScript.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "tour.script")));
            var result = GameRunner.Run(game, platform, new RunOptions { Script = script, CaptureDirectory = directory.FullName });
            Assert.IsNull(result.Error, result.Error?.ToString());
            Assert.HasCount(3, result.Captures);
            return result.Captures.ToDictionary(path => Path.GetFileName(path), File.ReadAllBytes);
        }
        finally { directory.Delete(recursive: true); }
    }

    [TestMethod]
    public void TheScriptedTourReportsNoHitch()
    {
        // The engine reports a slow load during play on the error output; the tour loads and releases the level scope.
        var errors = new StringWriter();
        var previous = Console.Error;
        Console.SetError(errors);
        try { PlayTour(); }
        finally { Console.SetError(previous); }
        StringAssert.DoesNotMatch(errors.ToString(), new System.Text.RegularExpressions.Regex("during play"), errors.ToString());
    }

    [TestMethod]
    public void EveryScriptedRunOfTheTourCapturesTheSameFrames()
    {
        var first = PlayTour();
        var second = PlayTour();
        CollectionAssert.AreEquivalent(new[] { "gallery.png", "nearest-level.png", "back.png" }, first.Keys.ToArray());
        foreach (var (name, bytes) in first)
            CollectionAssert.AreEqual(bytes, second[name], $"{name} differs between two scripted runs");
    }

    [TestMethod]
    public void AScriptedRunShowsNoMeasuredTime()
    {
        var game = new GalleryGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        var scene = (GalleryScene)game.Scenes.Current!;
        game.StepForTest();
        Assert.IsFalse(game.RealTime);
        StringAssert.Matches(scene.StatusLines[0], new System.Text.RegularExpressions.Regex(@"^10 assets, \d+\.\d MB of textures, loaded \(not timed\)$"));
        StringAssert.DoesNotMatch(string.Join("\n", scene.StatusLines), new System.Text.RegularExpressions.Regex(@"\d+ ms"));
    }

    [TestMethod]
    public void AScriptedRunShowsTheLevelScopeLoadedOnTheUpdateItIsAskedFor()
    {
        var game = new GalleryGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        var scene = (GalleryScene)game.Scenes.Current!;
        Assert.IsNotNull(game.LevelAhead, "a fixed-step run loads the level's atlas before play");
        int before = game.Assets.Statistics.Count;
        game.Input.SetForced("action", true);
        game.StepForTest();
        game.Input.SetForced("action", false);
        Assert.IsTrue(scene.LevelLoaded, "a fixed-step run does not depend on how long the workers take");
        Assert.AreEqual("Level scope: loaded (action switches)", scene.StatusLines[2]);
        Assert.IsNull(game.LevelAhead, "the level scope took the atlas over");
        Assert.AreEqual(before, game.Assets.Statistics.Count, "nothing loaded again");
        Assert.AreEqual(0, game.Assets.Statistics.LoadsDuringPlay);
        game.StepForTest();
        Press(game, "action");
        Assert.IsFalse(scene.LevelLoaded);
        Assert.AreEqual(before - 1, game.Assets.Statistics.Count, "disposing the scope released the tiles");
    }

    [TestMethod]
    public void AnInteractiveRunShowsTheMeasuredLoadTime()
    {
        var game = new GalleryGame();
        game.InitializeForTest(new RealTimePlatform(HeadlessPlatform.For(game.Settings)));
        var scene = (GalleryScene)game.Scenes.Current!;
        game.StepForTest();
        Assert.IsTrue(game.RealTime);
        StringAssert.Matches(scene.StatusLines[0], new System.Text.RegularExpressions.Regex(@"^9 assets, \d+\.\d MB of textures, loaded in \d+ ms$"));
    }

    [TestMethod]
    public void ConfirmSwitchesSamplingAndActionLoadsAndReleasesTheLevelScope()
    {
        // The interactive path: the level loads during play, on workers.
        var game = new GalleryGame();
        game.InitializeForTest(new RealTimePlatform(HeadlessPlatform.For(game.Settings)));
        var scene = (GalleryScene)game.Scenes.Current!;
        Press(game, "confirm");
        Assert.AreEqual(TextureFilter.Nearest, scene.CharacterFilter);
        Assert.AreEqual(TextureFilter.Nearest, game.Enemies.Texture.Filter);
        int before = game.Assets.Statistics.Count;
        Press(game, "action");
        // Workers may start late on a busy runner: wait by time, not by a count of frames.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!scene.LevelLoaded && DateTime.UtcNow < deadline) { game.StepForTest(); Thread.Sleep(1); }
        Assert.IsTrue(scene.LevelLoaded, "the preloaded tile atlas arrives within a few frames");
        Assert.AreEqual(before + 1, game.Assets.Statistics.Count);
        Press(game, "action");
        Assert.IsFalse(scene.LevelLoaded);
        Assert.AreEqual(before, game.Assets.Statistics.Count, "disposing the scope released the tiles");
    }
}
