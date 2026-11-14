using System.Numerics;
using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Platform;

namespace MicroEngine.Graphics.Tests;

/// <summary>
/// The canvas presented into the window with a prescale (<see cref="Presentation.Prescale"/>): enlarged by a whole factor
/// with nearest sampling, then sampled by the presentation's upscale sampler, so a fractional scale keeps even pixels with
/// one blended window pixel at their edges.
/// </summary>
[TestClass]
public sealed class CanvasPresenterTests
{
    private const uint Red = 0xFF0000FF, Blue = 0x0000FFFF, Black = 0x000000FF, Grey = 0x404040FF;
    private static readonly Presentation Sharp = Presentation.PixelArt with
    {
        Rounding = ScaleRounding.Fractional,
        Upscale = SamplerDescription.LinearClamp,
        Prescale = Presentation.AutoPrescale,
    };

    [TestMethod]
    public void Prescale_IsOffInBothPresets_AndRefusesNegativeFactors()
    {
        Assert.AreEqual(1, Presentation.PixelArt.Prescale);
        Assert.AreEqual(1, Presentation.HighResolution2D.Prescale);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Presentation.PixelArt with { Prescale = -1 });
    }

    [TestMethod]
    public void PrescaleFor_Auto_IsTheSmallestWholeFactorThatCoversTheFrame()
    {
        Assert.AreEqual((3, 3), Sharp.PrescaleFor(Sharp.Fit(new RectF(0, 0, 1366, 768), 480, 270), 480, 270), "2.84x: 3");
        Assert.AreEqual((4, 4), Sharp.PrescaleFor(new RectF(0, 0, 1920, 1080), 480, 270), "exactly 4x: 4");
        Assert.AreEqual((1, 1), Sharp.PrescaleFor(new RectF(0, 0, 240, 135), 480, 270), "a canvas drawn smaller is never enlarged first");
        var stretched = Sharp with { Aspect = AspectMode.Stretch };
        Assert.AreEqual((3, 2), stretched.PrescaleFor(new RectF(0, 0, 1000, 500), 480, 270), "each axis covers its own scale (2.08x, 1.85x)");
    }

    [TestMethod]
    public void PrescaleFor_AFixedFactorIsKept_OneMeansNone_AndNearestUpscalingNeedsNone()
    {
        var frame = new RectF(0, 0, 1366, 768);
        Assert.AreEqual((2, 2), (Sharp with { Prescale = 2 }).PrescaleFor(frame, 480, 270));
        Assert.AreEqual((1, 1), (Sharp with { Prescale = 1 }).PrescaleFor(frame, 480, 270));
        Assert.AreEqual((1, 1), (Sharp with { Upscale = SamplerDescription.PointClamp }).PrescaleFor(frame, 480, 270),
            "nearest of a nearest enlargement is nearest: nothing to gain");
    }

    [TestMethod]
    public void PrescaleFor_NeverExceedsTheLargestTargetOfTheDevice()
    {
        Assert.AreEqual((2, 2), (Sharp with { Prescale = 8 }).PrescaleFor(new RectF(0, 0, 3840, 2160), 480, 270, maxSize: 1000), "480 x 2 = 960 fits in 1000, 480 x 3 does not");
        Assert.AreEqual((1, 1), Sharp.PrescaleFor(new RectF(0, 0, 3840, 2160), 480, 270, maxSize: 480));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Sharp.PrescaleFor(new RectF(0, 0, 10, 10), 0, 270));
    }

    /// <summary>A canvas of one row presented into a window of <paramref name="windowWidth"/> × 1, as RGBA rows.</summary>
    private static uint[] PresentRow(Presentation presentation, uint[] canvasRow, int windowWidth, RectF? frame = null, uint? ui = null)
    {
        using var device = new ReferenceGraphicsDevice(new HostSurface(1, () => (windowWidth, 1), () => 1f, () => false));
        using var batcher = new Renderer2D(device);
        using var presenter = new CanvasPresenter(device);
        var canvas = device.CreateRenderTarget(new RenderTargetDescription(canvasRow.Length, 1));
        var texels = device.CreateTexture(new TextureDescription(canvasRow.Length, 1), Texels.Bytes(canvasRow));
        batcher.Begin(new BatchSettings(canvas, Blend: BlendState.Opaque));
        batcher.Draw(texels, new RectF(0, 0, canvasRow.Length, 1), Matrix3x2.Identity, ColorF.White);
        batcher.End();
        RenderTarget? overlay = null;
        if (ui is { } colour)
        {
            // A UI view of the window's size whose first pixel is lit, the rest transparent.
            overlay = device.CreateRenderTarget(new RenderTargetDescription(windowWidth, 1));
            var dot = device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(colour));
            batcher.Begin(new BatchSettings(overlay, Blend: BlendState.Opaque, Clear: default(ColorF)));
            batcher.Draw(dot, new RectF(0, 0, 1, 1), Matrix3x2.Identity, ColorF.White);
            batcher.End();
        }
        Assert.IsTrue(presenter.Present(presentation, canvas, overlay, frame ?? new RectF(0, 0, windowWidth, 1), new Color(0x40, 0x40, 0x40)));
        device.Present();
        var (w, _, rgba) = device.PresentedFrame!.Value;
        var row = new uint[w];
        for (int i = 0; i < w; i++) row[i] = (uint)(rgba[i * 4] << 24 | rgba[i * 4 + 1] << 16 | rgba[i * 4 + 2] << 8 | 0xFF);
        return row;
    }

    [TestMethod]
    public void AtAFractionalScale_PixelsStayEven_WithOneBlendedWindowPixelAtTheirEdge()
    {
        // Two canvas pixels in five window pixels (2.5x). Nearest gives them 2 and 3 window pixels; linear blurs four of
        // the five; prescaled 3x and then sampled linearly, each keeps two pure pixels and they share the middle one.
        var sharp = PresentRow(Sharp, [Red, Blue], 5);
        Assert.AreEqual(Red, sharp[0]);
        Assert.AreEqual(Red, sharp[1]);
        Assert.AreEqual(Blue, sharp[3]);
        Assert.AreEqual(Blue, sharp[4]);
        Assert.IsTrue(sharp[2] is not Red and not Blue, $"the shared pixel is a blend, was {sharp[2]:X8}");
        Assert.AreEqual((sharp[2] >> 24) & 0xFF, (sharp[2] >> 8) & 0xFF, 1u, "an even blend of the two");

        CollectionAssert.AreEqual(new[] { Red, Red, Blue, Blue, Blue }, PresentRow(Sharp with { Upscale = SamplerDescription.PointClamp }, [Red, Blue], 5), "nearest: uneven pixels");
        var linear = PresentRow(Sharp with { Prescale = 1 }, [Red, Blue], 5);
        Assert.AreEqual(3, linear.Count(p => p is not Red and not Blue), "linear alone blurs three of the five");
    }

    [TestMethod]
    public void AtAWholeScale_ThePictureIsExactlyTheNearestOne()
    {
        var nearest = PresentRow(Presentation.PixelArt, [Red, Blue, Red], 12);
        CollectionAssert.AreEqual(nearest, PresentRow(Sharp, [Red, Blue, Red], 12));
        CollectionAssert.AreEqual(new[] { Red, Red, Red, Red, Blue, Blue, Blue, Blue, Red, Red, Red, Red }, nearest);
    }

    [TestMethod]
    public void WithoutAPrescale_ThePictureIsThePresentationsUpscaleAlone()
    {
        CollectionAssert.AreEqual(new[] { Red, Red, Blue, Blue, Blue }, PresentRow(Presentation.PixelArt with { Rounding = ScaleRounding.Fractional }, [Red, Blue], 5));
    }

    [TestMethod]
    public void TheLetterboxSurroundsTheFrame_AndTheUiViewCoversBoth()
    {
        var row = PresentRow(Sharp, [Red, Blue], 9, frame: new RectF(2, 0, 5, 1), ui: 0x00FF00FF);
        Assert.AreEqual(0x00FF00FFu, row[0], "the UI view is drawn over the window, letterbox included");
        Assert.AreEqual(Grey, row[1]);
        Assert.AreEqual(Red, row[2]);
        Assert.AreEqual(Red, row[3]);
        Assert.AreEqual(Blue, row[5]);
        Assert.AreEqual(Blue, row[6]);
        Assert.AreEqual(Grey, row[7]);
        Assert.AreEqual(Grey, row[8]);
    }

    [TestMethod]
    public void TheFrameOfAWindow_IsPaintedPixelForPixel_WithEvenLetterbox()
    {
        // 480x270 in 1366x700 scales by 2.59: 1244.44 pixels wide. A device paints the pixels whose centres a quad covers,
        // so a frame of that width starting at 60 painted 1244 columns with 60 of letterbox on its left and 62 on its
        // right; the frame Fit gives is whole, and what is painted is exactly it.
        var canvas = Enumerable.Repeat(Red, 480).ToArray();
        var nearest = Presentation.PixelArt with { Rounding = ScaleRounding.Fractional };
        static (int Left, int Painted, int Right) Columns(uint[] row) =>
            (Array.FindIndex(row, p => p == Red), row.Count(p => p == Red), row.Length - 1 - Array.FindLastIndex(row, p => p == Red));

        Assert.AreEqual((60, 1244, 62), Columns(PresentRow(nearest, canvas, 1366, frame: new RectF(60, 0, 1244.4445f, 1))), "a fraction of a pixel: uneven letterbox");

        var fit = nearest.Fit(new RectF(0, 0, 1366, 700), 480, 270);
        var row = PresentRow(nearest, canvas, 1366, frame: new RectF(fit.X, 0, fit.Width, 1));
        Assert.AreEqual(((int)fit.X, (int)fit.Width, 1366 - (int)fit.Right), Columns(row), "painted exactly where Fit says");
        Assert.AreEqual((61, 1244, 61), Columns(row));
        Assert.IsTrue(row.All(p => p is Red or Grey), "no blended pixel at the frame's edges");
    }

    [TestMethod]
    public void PresentsNothing_WhenTheWindowHasNoTarget()
    {
        using var device = new ReferenceGraphicsDevice(); // no surface
        using var presenter = new CanvasPresenter(device);
        var canvas = device.CreateRenderTarget(new RenderTargetDescription(2, 1));
        Assert.IsFalse(presenter.Present(Sharp, canvas, null, new RectF(0, 0, 5, 1), Color.Black));
    }
}
