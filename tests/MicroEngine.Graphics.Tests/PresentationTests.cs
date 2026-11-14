using System.Numerics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics.Tests;

/// <summary>How the canvas reaches the window: each choice is its own field, and the presets only fill them in.</summary>
[TestClass]
public sealed class PresentationTests
{
    private static readonly RectF Window1080 = new(0, 0, 1920, 1080);

    [TestMethod]
    public void PixelArt_IsWholeFactorsWhenTheyFillEnough_NearestEverywhere_CapturesAtThree()
    {
        var p = Presentation.PixelArt;
        Assert.AreEqual(ScaleRounding.IntegerUnlessWasteful, p.Rounding);
        Assert.AreEqual(AspectMode.Keep, p.Aspect);
        Assert.AreEqual(SamplerDescription.PointClamp, p.Upscale);
        Assert.AreEqual(SamplerDescription.PointClamp, p.DefaultSampler);
        Assert.AreEqual(3, p.CaptureScale);
        Assert.AreEqual(0.8f, p.WastefulThreshold);
        Assert.IsTrue(p.WholeFactorWindows);
    }

    [TestMethod]
    public void HighResolution2D_FillsWhatFits_LinearEverywhere_CapturesAtOne()
    {
        var p = Presentation.HighResolution2D;
        Assert.AreEqual(ScaleRounding.Fractional, p.Rounding);
        Assert.AreEqual(AspectMode.Keep, p.Aspect);
        Assert.AreEqual(SamplerDescription.LinearClamp, p.Upscale);
        Assert.AreEqual(SamplerDescription.LinearClamp, p.DefaultSampler);
        Assert.AreEqual(1, p.CaptureScale);
        Assert.IsFalse(p.WholeFactorWindows);
    }

    [TestMethod]
    public void Fields_AreIndependent_APresetIsOnlyAStartingPoint()
    {
        // Integer scaling with a smooth present filter and linear textures: a combination the old single switch could not say.
        var p = Presentation.PixelArt with { Upscale = SamplerDescription.LinearClamp, DefaultSampler = SamplerDescription.LinearClamp, CaptureScale = 1 };
        Assert.AreEqual(ScaleRounding.IntegerUnlessWasteful, p.Rounding);
        Assert.AreEqual(SamplerDescription.LinearClamp, p.Upscale);
        Assert.AreEqual(new RectF(0, 0, 1920, 1080), p.Fit(Window1080, 320, 180), "6x fills 1920x1080 exactly");
    }

    [TestMethod]
    public void Fit_IntegerUnlessWasteful_TakesTheLargestWholeFactor_Centred()
    {
        // 320x180 in 1366x768: 4.27 fits, 4 fills more than 80 % of it: 1280x720 centred.
        Assert.AreEqual(new RectF(43, 24, 1280, 720), Presentation.PixelArt.Fit(new RectF(0, 0, 1366, 768), 320, 180));
    }

    [TestMethod]
    public void Fit_IntegerUnlessWasteful_GoesFractionalWhenTheWholeFactorWastesTooMuch()
    {
        // 480x270 in 732x412: 1.53 fits, 1 would fill 65 %: fractional.
        var frame = Presentation.PixelArt.Fit(new RectF(0, 0, 732, 412), 480, 270);
        Assert.AreEqual(732f, frame.Width);
        Assert.AreEqual(412f, frame.Height, "411.75 in whole pixels");
    }

    [TestMethod]
    public void Fit_Integer_NeverScalesFractionally_AndShrinksBelowOneOnlyWhenNothingFits()
    {
        var integer = Presentation.PixelArt with { Rounding = ScaleRounding.Integer };
        Assert.AreEqual(new RectF(46, 1, 640, 360), integer.Fit(new RectF(0, 0, 732, 362), 320, 180), "2x even though 2.01x would fit");
        var tiny = integer.Fit(new RectF(0, 0, 160, 90), 320, 180);
        Assert.AreEqual(160f, tiny.Width, "a window smaller than the canvas scales it down (never crops)");
    }

    [TestMethod]
    public void Fit_Fractional_FillsTheLargestSizeOfTheCanvasShape()
    {
        var laptop = Presentation.HighResolution2D.Fit(new RectF(0, 0, 1366, 768), 1280, 720);
        Assert.AreEqual(768f, laptop.Height);
        Assert.AreEqual(1365f, laptop.Width, "1365.33 in whole pixels");
        Assert.AreEqual(new RectF(0, 0, 1920, 1080), Presentation.HighResolution2D.Fit(Window1080, 1280, 720));
    }

    private static readonly Presentation Fractional = Presentation.PixelArt with { Rounding = ScaleRounding.Fractional };
    private static readonly Presentation Integer = Presentation.PixelArt with { Rounding = ScaleRounding.Integer };

    private static readonly (int Width, int Height)[] OddWindows =
    [
        (1366, 768), (1365, 767), (1367, 769), (1366, 700), (1001, 563), (733, 411), (915, 412), (390, 844), (411, 731),
        (1919, 1079), (2561, 1441), (641, 359), (479, 269), (161, 90), (7, 5), (3, 2), (1, 1), (1366, 1), (1, 768),
    ];

    private static readonly (int Width, int Height)[] FitCanvases = [(480, 270), (320, 180), (256, 224), (1280, 720), (1920, 1080), (3, 2)];

    private static IEnumerable<(Presentation Presentation, string Name)> Roundings() =>
    [
        (Presentation.PixelArt, "pixel art"), (Integer, "integer"), (Fractional, "fractional"), (Presentation.HighResolution2D, "high resolution"),
        (Fractional with { Aspect = AspectMode.Expand }, "expand"),
    ];

    [TestMethod]
    public void Fit_AWholeWindow_GetsAWholePixelFrameInsideIt_WhateverItsSize()
    {
        foreach (var (window, height) in OddWindows)
            foreach (var (cw, ch) in FitCanvases)
                foreach (var (presentation, name) in Roundings())
                    foreach (bool portrait in new[] { false, true })
                    {
                        var area = new RectF(0, 0, window, height);
                        var frame = presentation.Fit(area, cw, ch, portrait);
                        string where = $"{cw}x{ch} in {window}x{height}, {name}{(portrait ? ", portrait" : "")}: {frame}";
                        Assert.AreEqual(MathF.Round(frame.X), frame.X, where);
                        Assert.AreEqual(MathF.Round(frame.Y), frame.Y, where);
                        Assert.AreEqual(MathF.Round(frame.Width), frame.Width, where);
                        Assert.AreEqual(MathF.Round(frame.Height), frame.Height, where);
                        Assert.IsTrue(frame.Width >= 1 && frame.Height >= 1, $"at least one pixel: {where}");
                        Assert.IsTrue(frame.X >= 0 && frame.Y >= 0 && frame.Right <= window && frame.Bottom <= height, $"never outside the window: {where}");
                    }
    }

    [TestMethod]
    public void Fit_CentresTheFrame_TheOddPixelOfLetterboxGoesToTheRightAndBottom()
    {
        foreach (var (window, height) in OddWindows)
            foreach (var (cw, ch) in FitCanvases)
                foreach (var (presentation, name) in Roundings())
                {
                    var frame = presentation.Fit(new RectF(0, 0, window, height), cw, ch);
                    float left = frame.X, right = window - frame.Right, top = frame.Y, bottom = height - frame.Bottom;
                    string where = $"{cw}x{ch} in {window}x{height}, {name}: {frame}";
                    Assert.IsTrue(right - left is 0 or 1, $"left {left}, right {right}: {where}");
                    Assert.IsTrue(bottom - top is 0 or 1, $"top {top}, bottom {bottom}: {where}");
                }
        // A safe area that does not start at the window's corner: centred in the area, not in the window.
        Assert.AreEqual(new RectF(121, 11, 1244, 700), Fractional.Fit(new RectF(60, 11, 1366, 700), 480, 270));
    }

    [TestMethod]
    public void Fit_AFractionalScaleKeepsTheCanvasShapeToThePixel_AndTouchesTheWindowsEdges()
    {
        foreach (var (window, height) in OddWindows)
            foreach (var (cw, ch) in FitCanvases)
                foreach (var presentation in new[] { Fractional, Presentation.HighResolution2D })
                {
                    var frame = presentation.Fit(new RectF(0, 0, window, height), cw, ch);
                    float scale = MathF.Min((float)window / cw, (float)height / ch);
                    string where = $"{cw}x{ch} in {window}x{height}: {frame}";
                    Assert.AreEqual(MathF.Max(1, cw * scale), frame.Width, 0.5f, $"the nearest whole width: {where}");
                    Assert.AreEqual(MathF.Max(1, ch * scale), frame.Height, 0.5f, $"the nearest whole height: {where}");
                    Assert.IsTrue(frame.Width == window || frame.Height == height, $"the limiting side is filled exactly: {where}");
                }
        // 480x270 in 1366x768: 2.844x. The height fills the window; the width is 1365 of the 1365.33 that would fit.
        Assert.AreEqual(new RectF(0, 0, 1365, 768), Fractional.Fit(new RectF(0, 0, 1366, 768), 480, 270));
        // In 1366x700 the letterbox is 61 pixels on each side.
        Assert.AreEqual(new RectF(61, 0, 1244, 700), Fractional.Fit(new RectF(0, 0, 1366, 700), 480, 270));
        // A portrait phone: the frame fills the width at the top.
        Assert.AreEqual(new RectF(0, 0, 390, 219), Fractional.Fit(new RectF(0, 0, 390, 844), 480, 270, portrait: true));
    }

    [TestMethod]
    public void Fit_AtWholeFactors_TheFrameIsExactlyTheCanvasTimesTheFactor()
    {
        foreach (var (cw, ch) in FitCanvases)
            for (int factor = 1; factor <= 8; factor++)
                foreach (var (presentation, name) in Roundings())
                {
                    string where = $"{cw}x{ch} at {factor}x, {name}";
                    Assert.AreEqual(new RectF(0, 0, cw * factor, ch * factor), presentation.Fit(new RectF(0, 0, cw * factor, ch * factor), cw, ch), where);
                    // The same window with room to spare on one side: a centred frame of the same size.
                    var keep = presentation with { Aspect = AspectMode.Keep };
                    Assert.AreEqual(new RectF(3, 0, cw * factor, ch * factor), keep.Fit(new RectF(0, 0, cw * factor + 7, ch * factor), cw, ch), where);
                    Assert.AreEqual(new RectF(0, 2, cw * factor, ch * factor), keep.Fit(new RectF(0, 0, cw * factor, ch * factor + 5), cw, ch), where);
                }
        Assert.AreEqual(new RectF(80, 45, 2400, 1350), Presentation.PixelArt.Fit(new RectF(0, 0, 2560, 1440), 480, 270), "5x with a border");
        Assert.AreEqual(new RectF(43, 24, 1280, 720), Presentation.PixelArt.Fit(new RectF(0, 0, 1366, 768), 320, 180), "4x with a border");
    }

    [TestMethod]
    public void Fit_AtTheEdges_AWindowSmallerThanTheCanvasShrinksIt_AndAnEmptyAreaGivesAnEmptyFrame()
    {
        Assert.AreEqual(new RectF(0, 0, 160, 90), Presentation.PixelArt.Fit(new RectF(0, 0, 160, 90), 320, 180), "half the canvas");
        Assert.AreEqual(new RectF(0, 0, 160, 90), Integer.Fit(new RectF(0, 0, 161, 90), 320, 180), "the odd column is letterbox");
        Assert.AreEqual(new RectF(0, 0, 1, 1), Presentation.PixelArt.Fit(new RectF(0, 0, 1, 1), 480, 270), "a window of one pixel");
        Assert.AreEqual(new RectF(682, 0, 2, 1), Fractional.Fit(new RectF(0, 0, 1366, 1), 480, 270), "one row: 1.78 columns round to 2");
        Assert.AreEqual(new RectF(0, 383, 1, 1), Fractional.Fit(new RectF(0, 0, 1, 768), 480, 270), "one column");
        Assert.AreEqual(new RectF(5, 7, 0, 0), Presentation.PixelArt.Fit(new RectF(5, 7, 0, 0), 320, 180), "nothing to draw into");
        Assert.AreEqual(new RectF(0, 0, 1366, 768), Presentation.PixelArt.Fit(new Vector2(1366, 768), new RectF(0, 0, -4, 10), 1366, 768), "an impossible safe area means the whole window");
    }

    [TestMethod]
    public void Fit_Stretch_FillsTheWholeArea_IgnoringTheCanvasShape()
    {
        var stretched = Presentation.HighResolution2D with { Aspect = AspectMode.Stretch };
        Assert.AreEqual(new RectF(0, 0, 1000, 500), stretched.Fit(new RectF(0, 0, 1000, 500), 320, 180));
    }

    [TestMethod]
    public void Fit_StaysInsideTheSafeArea_AndAPortraitScreenPutsTheFrameAtTheTop()
    {
        var area = new RectF(0, 50, 1080, 1800); // a portrait phone, below its notch
        var frame = Presentation.PixelArt.Fit(area, 320, 180, portrait: true);
        Assert.AreEqual(50f, frame.Y, "the top of the safe area, leaving the space below to on-screen controls");
        Assert.AreEqual(960f, frame.Width, "3x: 1080 / 320 = 3.375, 3 fills 89 %");
    }

    [TestMethod]
    public void Validation_RefusesImpossibleValues()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Presentation.PixelArt with { CaptureScale = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Presentation.PixelArt with { WastefulThreshold = 1.5f });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Presentation.PixelArt.Fit(Window1080, 0, 180));
    }

    [TestMethod]
    public void ToCanvas_MapsAWindowPositionBackIntoCanvasPixels()
    {
        var p = Presentation.PixelArt;
        var frame = p.Fit(new RectF(0, 0, 1366, 768), 320, 180);
        Assert.AreEqual(new Vector2(0, 0), Presentation.ToCanvas(new Vector2(43, 24), frame, 320, 180));
        Assert.AreEqual(new Vector2(160, 90), Presentation.ToCanvas(new Vector2(43 + 640, 24 + 360), frame, 320, 180));
    }

    [TestMethod]
    public void Expand_GrowsTheCanvasAlongTheFreeSide_KeepingTheScaleAndSquarePixels()
    {
        var expand = Presentation.PixelArt with { Aspect = AspectMode.Expand };
        // 320x180 in 2560x1080 (ultrawide): 6x fits the height; the canvas grows to 2560 / 6 = 426 wide.
        var area = new RectF(0, 0, 2560, 1080);
        Assert.AreEqual((426, 180), expand.CanvasSize(area, 320, 180));
        var frame = expand.Fit(area, 320, 180);
        Assert.AreEqual(new RectF(2, 0, 2556, 1080), frame, "426 x 6 = 2556 pixels wide, centred; no letterbox beyond the rounding");
        Assert.AreEqual((320, 180), expand.CanvasSize(new RectF(0, 0, 1920, 1080), 320, 180), "the canvas's own shape: unchanged");
        Assert.AreEqual((320, 240), expand.CanvasSize(new RectF(0, 0, 1280, 960), 320, 180), "a taller window grows the height");
        Assert.AreEqual((320, 180), Presentation.PixelArt.CanvasSize(area, 320, 180), "Keep never changes the canvas");
    }

    [TestMethod]
    public void UiScale_IsUnsetByDefault_SoTheSurfacesPixelDensityDecides_AndRefusesNonPositiveValues()
    {
        Assert.IsNull(Presentation.PixelArt.UiScale);
        Assert.IsNull(Presentation.HighResolution2D.UiScale);
        Assert.AreEqual(1f, Presentation.PixelArt.UiScaleFor(1), "density 1: one UI unit per window pixel");
        Assert.AreEqual(2f, Presentation.PixelArt.UiScaleFor(2), "density 2: the UI keeps its physical size on a high-density screen");
        Assert.AreEqual(1f, Presentation.PixelArt.UiScaleFor(0), "a surface that reports no density counts as 1");
        Assert.AreEqual(1f, Presentation.PixelArt.UiScaleFor(float.NaN));
        var explicitScale = Presentation.PixelArt with { UiScale = 1.5f };
        Assert.AreEqual(1.5f, explicitScale.UiScale);
        Assert.AreEqual(1.5f, explicitScale.UiScaleFor(1), "an explicit value overrides the density");
        Assert.AreEqual(1.5f, explicitScale.UiScaleFor(2));
        Assert.IsNull((explicitScale with { UiScale = null }).UiScale, "null goes back to the density");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Presentation.PixelArt with { UiScale = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Presentation.PixelArt with { UiScale = float.PositiveInfinity });
    }
}
