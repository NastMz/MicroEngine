using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.Tests;

/// <summary>The start screen: settings read from the game's identity, drawn once before loading, never headless.</summary>
[TestClass]
public sealed class StartScreenTests
{
    /// <summary>A headless platform that runs "in real time" and records what each frame drew, for the start screen.</summary>
    private sealed class RealTimePlatform() : TestPlatform(realTime: true)
    {
        public List<string> Log { get; } = [];
        public override IRenderer BeginFrame() { Log.Add("frame"); return Inner.BeginFrame(); }
        public override void EndFrame() { Log.Add($"draws {Inner.Renderer.DrawCalls}"); Inner.EndFrame(); }
    }

    private sealed class LoggingGame(List<string> log) : Game
    {
        public override GameSettings Settings { get; } = new("Logging", 320, 180);
        protected override void Load() => log.Add("load");
    }

    [TestMethod]
    public void TheIdentityCarriesTheStartScreenSettings()
    {
        var identity = AppIdentity.Of(typeof(StartScreenTests).Assembly)!;
        Assert.IsTrue(identity.Splash, "on unless the project turns it off");
        Assert.AreEqual(Color.Hex("123456"), identity.SplashColor);
        Assert.IsTrue(identity.SplashBranding);
        Assert.IsNull(identity.SplashImage, "no icon in the test assembly");
    }

    [TestMethod]
    public void TheBackgroundContinuesTheImagesEdge()
    {
        var image = new PixelImage(16, 16).Clear(Color.Hex("204080")).FillRect(4, 4, 8, 8, Color.White);
        Assert.AreEqual(Color.Hex("204080"), AppIdentity.EdgeColor(image));
        Assert.AreEqual(AppIdentity.DefaultBackground, AppIdentity.EdgeColor(new PixelImage(8, 8)), "a transparent border gives the default");
        var identity = new AppIdentity("Game", null, "1.0", AppOrientation.Landscape) { Icon = image };
        Assert.AreEqual(Color.Hex("204080"), identity.SplashColor);
        Assert.AreSame(image, identity.SplashImage);
    }

    [TestMethod]
    public void ItIsDrawnOnceBeforeTheGameLoads()
    {
        var platform = new RealTimePlatform();
        var game = new LoggingGame(platform.Log);
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        // The test assembly's identity has branding but no image: a clear and the text.
        Assert.AreEqual("frame", platform.Log[0]);
        Assert.IsTrue(platform.Log[1].StartsWith("draws ") && int.Parse(platform.Log[1][6..]) > 1, string.Join(",", platform.Log));
        Assert.AreEqual("load", platform.Log[2], "loading starts right after: no delay, one frame");
        loop.Finish();
    }

    [TestMethod]
    public void HeadlessRunsAndOptedOutRunsSkipIt()
    {
        var log = new List<string>();
        var headless = new GameLoop(new LoggingGame(log), new HeadlessPlatform());
        headless.Start();
        CollectionAssert.AreEqual(new[] { "load" }, log, "captures and tests are unchanged");
        var platform = new RealTimePlatform();
        var off = new GameLoop(new LoggingGame(platform.Log), platform, new RunOptions { StartScreen = false });
        off.Start();
        CollectionAssert.AreEqual(new[] { "load" }, platform.Log);
    }

    [TestMethod]
    public void TheImageFillsHalfTheShorterSideAndKeepsItsShape()
    {
        Assert.AreEqual((90, 90), StartScreen.ImageSize(new PixelImage(1024, 1024), 320, 180));
        Assert.AreEqual((90, 45), StartScreen.ImageSize(new PixelImage(200, 100), 320, 180));
        var big = new PixelImage(4, 4).Clear(Color.White).Resized(16, 16);
        Assert.AreEqual(Color.White, big[15, 15], "whole-number enlargements repeat pixels");
        var half = new PixelImage(2, 2).FillRect(0, 0, 1, 2, Color.Hex("ff0000")).FillRect(1, 0, 1, 2, Color.Hex("0000ff")).Resized(1, 1)[0, 0];
        Assert.AreEqual(new Color(127, 0, 127), half, "reductions average the area");
    }
}

/// <summary>Where achievement banners appear.</summary>
[TestClass]
public sealed class AchievementToastPlacementTests
{
    /// <summary>Records the rectangles filled, to find a panel's place.</summary>
    private sealed class RectRecorder(int width, int height) : IRenderer
    {
        public List<RectF> Rects { get; } = [];
        public int Width { get; } = width;
        public int Height { get; } = height;
        public void Clear(Color color) { }
        public void Draw(Texture texture, RectF source, System.Numerics.Vector2 position, Color tint, in DrawOptions options = default) { }
        public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) { }
        public void FillRect(RectF rect, Color color) => Rects.Add(rect);
        public void FillCircle(System.Numerics.Vector2 center, float radius, Color color) { }
        public void FillTriangle(System.Numerics.Vector2 a, System.Numerics.Vector2 b, System.Numerics.Vector2 c, Color color) { }
        public void DrawLine(System.Numerics.Vector2 from, System.Numerics.Vector2 to, float thickness, Color color) { }
        public void PushCamera(Camera2D camera) { }
        public void PopCamera() { }
        public void SetBlendMode(BlendMode mode) { }
        public void SetClip(RectF? screenRect) { }
    }

    private static RectF Shown(MicroEngine.UI.AchievementToasts toasts)
    {
        toasts.Show(new Achievement("first", "First steps", "Walk a little."));
        toasts.Update(1f); // fully slid in
        var r = new RectRecorder(320, 180);
        toasts.Draw(r, BitmapFont.CreateDefault(new HeadlessGraphicsDevice()));
        return r.Rects[1]; // the panel's border (the first rectangle is its drop shadow)
    }

    [TestMethod]
    public void BannersSitAtTheTopByDefaultOrAtTheBottom()
    {
        var top = Shown(new MicroEngine.UI.AchievementToasts { Top = 23 });
        Assert.AreEqual(23, top.Y, 0.5f);
        var bottom = Shown(new MicroEngine.UI.AchievementToasts { Bottom = 4 });
        Assert.AreEqual(180 - 4, bottom.Bottom, 0.5f, "fully shown, its bottom edge is 4 pixels above the screen's");
    }
}
