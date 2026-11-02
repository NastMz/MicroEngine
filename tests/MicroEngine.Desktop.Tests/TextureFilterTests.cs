using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Raylib;
using MicroEngine.Scenes;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// Both desktop platforms sample textures as each texture asks (nearest or linear) and accept region updates: what HD art
/// and fonts need. Real windows, hidden; skipped without a display (failed under MICROENGINE_NATIVE_TESTS=1).
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class TextureFilterTests
{
    [TestInitialize]
    public void NotOnMacOS()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS; see web/tests/desktop.mjs");
    }

    /// <summary>A black and a white texel stretched 16 times, once per filter, then a texture whose middle is updated.</summary>
    private sealed class FilterGame : Game
    {
        public override GameSettings Settings { get; } = new("filters", 64, 24, 1) { Background = new Color(0, 0, 255) };
        protected override void Load() => Scenes.Push(new FilterScene(this));
    }

    private sealed class FilterScene(Game game) : Scene
    {
        private Texture? _nearest, _linear, _updated;
        public override void Update(float dt) { }
        public override void Draw(IRenderer r)
        {
            if (_nearest is null)
            {
                var pair = new PixelImage(2, 1);
                pair[0, 0] = Color.Black;
                pair[1, 0] = Color.White;
                _nearest = game.Graphics.CreateTexture(pair, TextureFilter.Nearest);
                _linear = game.Graphics.CreateTexture(pair, TextureFilter.Nearest);
                _linear.Filter = TextureFilter.Linear;
                _updated = game.Graphics.CreateTexture(new PixelImage(4, 4).Clear(new Color(255, 0, 0)));
                _updated.Update(1, 1, 2, 2, [Color.White, Color.White, Color.White, Color.White]);
            }
            var stretch = new DrawOptions(Scale: new Vector2(16, 8));
            r.Draw(_nearest, _nearest.Bounds, new Vector2(0, 0), Color.White, stretch);
            r.Draw(_linear!, _linear!.Bounds, new Vector2(0, 8), Color.White, stretch);
            r.Draw(_updated!, _updated!.Bounds, new Vector2(40, 0), Color.White);
        }
    }

    private static PixelImage Capture(PlatformProfile profile)
    {
        var game = new FilterGame();
        IPlatform platform;
        try { platform = profile.Create(new PlatformRequest(game.Settings) { Hidden = true, FixedStep = true, Mute = true, CaptureScale = 1 }); }
        catch (InvalidOperationException e)
        {
            Native.Require(false, $"{profile.Name} cannot open a window here: {e.Message}");
            throw;
        }
        string dir = Path.Combine(Path.GetTempPath(), "microengine-filter-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (platform)
            {
                var result = GameRunner.Run(game, platform, new RunOptions { Script = new InputScript().Capture(1, "frame.png").QuitAfter(2), CaptureDirectory = dir });
                Assert.IsNull(result.Error, result.Error?.ToString());
            }
            return PixelImage.Load(Path.Combine(dir, "frame.png"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    private static void Check(PixelImage frame)
    {
        // Nearest: the left half is the black texel, the right half the white one, with a hard edge in the middle.
        Assert.AreEqual(0, frame[14, 4].R);
        Assert.AreEqual(255, frame[17, 4].R);
        // Linear: values climb across the middle instead of jumping.
        int left = frame[12, 12].R, middle = frame[16, 12].R, right = frame[20, 12].R;
        Assert.IsTrue(left < middle && middle < right, $"linear sampling should blend: {left}, {middle}, {right}");
        Assert.IsTrue(middle is > 60 and < 200, $"the middle should be grey, got {middle}");
        // The updated region is white inside a red texture.
        Assert.AreEqual(new Color(255, 0, 0), frame[40, 0]);
        Assert.AreEqual(Color.White, frame[41, 1]);
        Assert.AreEqual(Color.White, frame[42, 2]);
        Assert.AreEqual(new Color(255, 0, 0), frame[43, 3]);
    }

    [TestMethod]
    public void SdlSamplesEachTextureWithItsFilterAndUpdatesRegions() => Check(Capture(SdlPlatform.Profile));

    [TestMethod]
    public void RaylibSamplesEachTextureWithItsFilterAndUpdatesRegions() => Check(Capture(RaylibPlatform.Profile));
}
