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
        Assert.AreEqual(732f, frame.Width, 1e-3f);
        Assert.AreEqual(270f * 732 / 480, frame.Height, 1e-3f);
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
        Assert.AreEqual(768f, laptop.Height, 1e-3f);
        Assert.AreEqual(1280f * 768 / 720, laptop.Width, 1e-3f);
        Assert.AreEqual(new RectF(0, 0, 1920, 1080), Presentation.HighResolution2D.Fit(Window1080, 1280, 720));
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
