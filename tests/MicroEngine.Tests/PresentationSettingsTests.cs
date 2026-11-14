using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Input;

namespace MicroEngine.Tests;

/// <summary>
/// The game's presentation (<see cref="GameSettings.Presentation"/>) is the one way to say how the canvas reaches the
/// window, one choice per field; it replaced the old <c>Scaling</c> switch (pixel or smooth), whose two values are the
/// presets <see cref="Presentation.PixelArt"/> and <see cref="Presentation.HighResolution2D"/>, with the same results.
/// </summary>
[TestClass]
public sealed class PresentationSettingsTests
{
    [TestMethod]
    public void Pixel_Art_Is_The_Default_Presentation()
    {
        var settings = new GameSettings("pixel");
        Assert.AreSame(Presentation.PixelArt, settings.Presentation);
        Assert.AreEqual(TextureFilter.Nearest, settings.DefaultTextureFilter, "nearest textures, as the old pixel switch");
        Assert.AreEqual(TextureFilter.Linear, new GameSettings("hd", 1280, 720) { Presentation = Presentation.HighResolution2D }.DefaultTextureFilter, "linear, as the old smooth switch");
    }

    private sealed class SettingsGame(GameSettings settings) : Game
    {
        public override GameSettings Settings { get; } = settings;
        protected override void Load() { }
    }

    [TestMethod]
    public void Game_cameras_snap_for_pixel_art_and_move_smoothly_for_high_resolution_2D()
    {
        var pixel = new SettingsGame(new GameSettings("pixel")).CreateCamera();
        Assert.IsTrue(pixel.PixelSnap);
        Assert.AreEqual((320f, 180f), (pixel.ViewWidth, pixel.ViewHeight));
        var hd = new SettingsGame(new GameSettings("hd", 1280, 720) { Presentation = Presentation.HighResolution2D }).CreateCamera();
        Assert.IsFalse(hd.PixelSnap);
        var chosen = new SettingsGame(new GameSettings("hd", 1280, 720) { Presentation = Presentation.HighResolution2D with { CameraPixelSnap = true } }).CreateCamera();
        Assert.IsTrue(chosen.PixelSnap, "the presentation's field, not the preset's name, decides");
    }

    [TestMethod]
    public void The_Default_Texture_Filter_Comes_From_The_Default_Sampler_Not_From_The_Rounding()
    {
        // Whole-factor scaling with smooth textures: the old switch could not say it.
        var settings = new GameSettings("pixel") { Presentation = Presentation.PixelArt with { DefaultSampler = SamplerDescription.LinearClamp } };
        Assert.AreEqual(TextureFilter.Linear, settings.DefaultTextureFilter);
        Assert.AreEqual(ScaleRounding.IntegerUnlessWasteful, settings.Presentation.Rounding, "the rounding is untouched");
        Assert.AreEqual(TextureFilter.Nearest, (settings with { TextureFilter = TextureFilter.Nearest }).DefaultTextureFilter, "an explicit TextureFilter still wins");
    }

    [TestMethod]
    public void The_Capture_Scale_Comes_From_The_Presentation()
    {
        Assert.AreEqual(3, new PlatformRequest(new GameSettings("pixel")).CaptureScale, "pixel art, as before");
        Assert.AreEqual(1, new PlatformRequest(new GameSettings("hd", 1280, 720) { Presentation = Presentation.HighResolution2D }).CaptureScale, "high resolution, as the old smooth switch");
        var pixelArtAtOne = new GameSettings("pixel") { Presentation = Presentation.PixelArt with { CaptureScale = 1 } };
        Assert.AreEqual(1, new PlatformRequest(pixelArtAtOne).CaptureScale, "independent of the rounding");
        Assert.AreEqual(2, new PlatformRequest(pixelArtAtOne) { CaptureScale = 2 }.CaptureScale, "a request still chooses its own");
    }

    [TestMethod]
    public void Whole_Factor_Windows_Follow_Their_Own_Field()
    {
        var pixel = new GameSettings("pixel");
        Assert.AreEqual((960, 540), WindowSizing.Initial(pixel, 4, 1366, 728), "whole factors, as before");
        var fractional = pixel with { Presentation = Presentation.PixelArt with { WholeFactorWindows = false } };
        var (w, h) = WindowSizing.Initial(fractional, 4, 1366, 728);
        Assert.AreEqual(1164, w, "the canvas's shape scaled to 90 % of the display's height, no whole factor");
        Assert.AreEqual(655, h);
    }

    /// <summary>
    /// The letterbox rule of the removed <c>CanvasScaling</c> switch (<c>FrameLayout.Fit</c> before the presentation
    /// replaced it), kept here as the reference the presets must reproduce: pixel prefers whole factors unless they fill
    /// less than 80 % of what fits; smooth takes the largest size; portrait screens put the frame at the top.
    /// </summary>
    private static RectF OldFit(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight, bool pixel)
    {
        if (safeArea.Width <= 0 || safeArea.Height <= 0) safeArea = new RectF(0, 0, window.X, window.Y);
        float scale = MathF.Min(safeArea.Width / virtualWidth, safeArea.Height / virtualHeight);
        if (pixel && scale >= 1 && MathF.Floor(scale) >= scale * 0.8f) scale = MathF.Floor(scale);
        float w = virtualWidth * scale, h = virtualHeight * scale;
        float x = MathF.Floor(safeArea.X + (safeArea.Width - w) / 2);
        bool portrait = window.Y > window.X * 1.1f;
        float y = portrait ? safeArea.Y : MathF.Floor(safeArea.Y + (safeArea.Height - h) / 2);
        return new RectF(x, y, w, h);
    }

    /// <summary>
    /// Where the old rule gave a frame of whole pixels, <paramref name="now"/> is exactly that frame; where it gave a
    /// fraction of a pixel (a scale that is not whole), the same frame with its size in whole pixels. Returns whether the
    /// frame is exactly the old one.
    /// </summary>
    private static bool AssertAsTheOldRule(RectF old, RectF now, string where)
    {
        if (old.Width == MathF.Round(old.Width) && old.Height == MathF.Round(old.Height))
        {
            Assert.AreEqual(old, now, where);
            return true;
        }
        Assert.AreEqual(MathF.Round(now.Width), now.Width, $"a whole width: {where}");
        Assert.AreEqual(MathF.Round(now.Height), now.Height, $"a whole height: {where}");
        Assert.IsTrue(MathF.Abs(now.Width - old.Width) < 1 && MathF.Abs(now.Height - old.Height) < 1, $"less than a pixel from {old}: {now}, {where}");
        Assert.IsTrue(MathF.Abs(now.X - old.X) <= 1 && MathF.Abs(now.Y - old.Y) <= 1, $"placed within a pixel of {old}: {now}, {where}");
        return false;
    }

    private static readonly Vector2[] Windows =
    [
        new(1366, 768), new(1920, 1080), new(732, 412), new(915, 412), new(1080, 1920), new(1080, 2400), new(2400, 1080),
        new(800, 600), new(320, 180), new(200, 150), new(1000, 560), new(1280, 800), new(390, 844),
    ];

    private static readonly (int Width, int Height)[] Canvases = [(320, 180), (480, 270), (256, 224), (1280, 720), (1920, 1080)];

    private static IEnumerable<RectF> SafeAreas(Vector2 window) =>
    [
        new RectF(0, 0, 0, 0), // none reported: the whole window
        new RectF(0, 0, window.X, window.Y),
        new RectF(window.X * 0.05f, 24, window.X * 0.9f, window.Y - 48), // a notch and a home indicator
    ];

    [TestMethod]
    public void The_Presets_Place_The_Frame_Exactly_As_The_Old_Switch_Did()
    {
        int compared = 0, exact = 0;
        foreach (var window in Windows)
            foreach (var safe in SafeAreas(window))
                foreach (var (w, h) in Canvases)
                {
                    string where = $"{w}x{h} in {window} safe {safe}";
                    var pixel = OldFit(window, safe, w, h, pixel: true);
                    var smooth = OldFit(window, safe, w, h, pixel: false);
                    if (AssertAsTheOldRule(pixel, Presentation.PixelArt.Fit(window, safe, w, h), $"pixel art: {where}")) exact++;
                    Assert.AreEqual(Presentation.PixelArt.Fit(window, safe, w, h), FrameLayout.Fit(window, safe, w, h, Presentation.PixelArt), $"FrameLayout, pixel art: {where}");
                    Assert.AreEqual(Presentation.PixelArt.Fit(window, safe, w, h), FrameLayout.Fit(window, safe, w, h), $"FrameLayout's default is pixel art: {where}");
                    if (AssertAsTheOldRule(smooth, Presentation.HighResolution2D.Fit(window, safe, w, h), $"high resolution: {where}")) exact++;
                    Assert.AreEqual(Presentation.HighResolution2D.Fit(window, safe, w, h), FrameLayout.Fit(window, safe, w, h, Presentation.HighResolution2D), $"FrameLayout, high resolution: {where}");
                    compared++;
                }
        Assert.AreEqual(Windows.Length * 3 * Canvases.Length, compared);
        Assert.AreEqual(164, exact, "the frames the old rule already gave in whole pixels are untouched");
    }

    [TestMethod]
    public void On_Screen_Controls_Without_Shown_Controls_Leave_The_Frame_Where_The_Old_Switch_Put_It()
    {
        var controls = new VirtualControls(TouchLayout.CreateDefault());
        foreach (var window in Windows)
            foreach (var safe in SafeAreas(window))
            {
                AssertAsTheOldRule(OldFit(window, safe, 320, 180, pixel: true), controls.Frame(window, safe, 320, 180, Presentation.PixelArt), $"pixel art in {window}");
                AssertAsTheOldRule(OldFit(window, safe, 320, 180, pixel: true), ((ITouchOverlay)controls).Frame(window, safe, 320, 180), $"default in {window}");
                AssertAsTheOldRule(OldFit(window, safe, 1280, 720, pixel: false), controls.Frame(window, safe, 1280, 720, Presentation.HighResolution2D), $"high resolution in {window}");
            }
    }

    [TestMethod]
    public void The_Overlay_Frame_Follows_Every_Field_Of_The_Presentation()
    {
        // The on-screen controls and the plain letterbox now agree for any presentation, not only the two presets.
        var window = new Vector2(732, 362);
        var none = new RectF(0, 0, 0, 0);
        var integer = Presentation.PixelArt with { Rounding = ScaleRounding.Integer };
        Assert.AreEqual(new RectF(46, 1, 640, 360), integer.Fit(window, none, 320, 180), "never fractional when a whole factor fits");
        Assert.AreEqual(integer.Fit(window, none, 320, 180), FrameLayout.Fit(window, none, 320, 180, integer));
        var stretch = Presentation.HighResolution2D with { Aspect = AspectMode.Stretch };
        Assert.AreEqual(new RectF(0, 0, 732, 362), FrameLayout.Fit(window, none, 320, 180, stretch));
        var portrait = new Vector2(1080, 1920);
        var safe = new RectF(0, 80, 1080, 1800);
        Assert.AreEqual(Presentation.PixelArt.Fit(portrait, safe, 320, 180), FrameLayout.Fit(portrait, safe, 320, 180), "portrait screens put the frame at the top of the safe area");
        Assert.AreEqual(80f, FrameLayout.Fit(portrait, safe, 320, 180).Y);
    }
}
