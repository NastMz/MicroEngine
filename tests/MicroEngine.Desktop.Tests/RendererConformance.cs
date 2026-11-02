using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The written rendering rules (docs/evaluations/arch-1/rendering-contract.md) as a conformance suite: every renderer
/// runs the same tests by subclassing this class and saying how it renders one frame. Each assertion names the property
/// it checks; it is exact unless the contract names a source of variation, which is written next to the tolerance.
/// The suite only uses <see cref="IRenderer"/>, <see cref="IGraphicsDevice"/> and <see cref="IPlatform"/>, so it can
/// move to a testing assembly that third-party backends reference.
/// </summary>
public abstract class RendererConformance
{
    /// <summary>The canvas every rule is drawn on (the game clears it to this colour first).</summary>
    protected static readonly Color Background = new(10, 20, 30);
    private static readonly Color Red = new(255, 0, 0);
    private static readonly Color Green = new(0, 255, 0);

    /// <summary>
    /// Sources of variation the contract allows on this backend (each one documented in the contract). A backend that
    /// declares none is held to the exact reference.
    /// </summary>
    [Flags]
    protected enum Variation
    {
        None = 0,
        /// <summary>Blending rounds results down instead of to nearest: ±1/255 per channel (8-bit quantization).</summary>
        Quantization = 1,
        /// <summary>Circles, lines and triangles are rasterized by a different algorithm (the software renderer).</summary>
        ShapeCoverage = 2,
    }

    /// <summary>The sources of variation this backend uses; the default is none.</summary>
    protected virtual Variation Allowed => Variation.None;

    /// <summary>
    /// Opens the backend for <paramref name="game"/> (hidden window, fixed step, mute, captures at scale 1), or returns
    /// null after marking the test skipped when it cannot open here.
    /// </summary>
    protected abstract IPlatform? Open(Game game, PlatformRequest request);

    /// <summary>Draws one frame with <paramref name="draw"/> on a <paramref name="width"/> × <paramref name="height"/> canvas and returns its capture.</summary>
    protected PixelImage Render(int width, int height, Action<IRenderer, IGraphicsDevice> draw)
    {
        var game = new RuleGame(width, height, draw);
        var request = new PlatformRequest(game.Settings) { Hidden = true, FixedStep = true, Mute = true, CaptureScale = 1 };
        var platform = Open(game, request) ?? throw new AssertInconclusiveException("the backend cannot open here");
        string dir = Path.Combine(Path.GetTempPath(), "microengine-conformance-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (platform)
            {
                var result = GameRunner.Run(game, platform, new RunOptions { Script = new InputScript().Capture(1, "rule.png").QuitAfter(2), CaptureDirectory = dir });
                Assert.IsNull(result.Error, result.Error?.ToString());
            }
            return PixelImage.Load(Path.Combine(dir, "rule.png"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    private sealed class RuleGame(int width, int height, Action<IRenderer, IGraphicsDevice> draw) : Game
    {
        public override GameSettings Settings { get; } = new("conformance", width, height, 1) { Background = Background };
        protected override void Load() => Scenes.Push(new RuleScene(this, draw));
    }

    private sealed class RuleScene(Game game, Action<IRenderer, IGraphicsDevice> draw) : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer r) => draw(r, game.Graphics);
    }

    private static string Rgb(Color c) => $"{c.R},{c.G},{c.B}";

    /// <summary>Position/coverage, exact: the set of pixels that are not the background.</summary>
    private static List<(int X, int Y)> Lit(PixelImage frame, int x0 = 0, int y0 = 0, int x1 = int.MaxValue, int y1 = int.MaxValue)
    {
        var lit = new List<(int, int)>();
        for (int y = Math.Max(0, y0); y < Math.Min(frame.Height, y1); y++)
            for (int x = Math.Max(0, x0); x < Math.Min(frame.Width, x1); x++)
                if (frame[x, y].R != Background.R || frame[x, y].G != Background.G || frame[x, y].B != Background.B) lit.Add((x, y));
        return lit;
    }

    private static string Describe(IEnumerable<(int X, int Y)> pixels) => string.Join(" ", pixels.Select(p => $"({p.X},{p.Y})"));

    /// <summary>Colour, exact or within the 8-bit quantization allowance when the backend declares it.</summary>
    private void AssertColour(PixelImage frame, int x, int y, Color expected, string what)
    {
        var c = frame[x, y];
        int tolerance = Allowed.HasFlag(Variation.Quantization) ? 1 : 0; // ±1/255: rounding down instead of to nearest
        bool ok = Math.Abs(c.R - expected.R) <= tolerance && Math.Abs(c.G - expected.G) <= tolerance && Math.Abs(c.B - expected.B) <= tolerance;
        Assert.IsTrue(ok, $"{what}: pixel ({x},{y}) is {Rgb(c)}, expected {Rgb(expected)} (±{tolerance})");
    }

    // ---- Clear -------------------------------------------------------------------------------------------------------

    [TestMethod]
    public void ClearFillsTheWholeCanvasWhateverTheClipAndBlendMode()
    {
        var frame = Render(16, 16, (r, _) =>
        {
            r.SetClip(new RectF(0, 0, 8, 8));
            r.SetBlendMode(BlendMode.Additive);
            r.Clear(Red);
            r.SetBlendMode(BlendMode.Alpha);
            r.SetClip(null);
        });
        var notRed = new List<(int, int)>();
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) if (frame[x, y] != new Color(255, 0, 0)) notRed.Add((x, y));
        Assert.IsEmpty(notRed, $"Clear must replace every pixel: {notRed.Count} are not red, first {Describe(notRed.Take(6))}");
    }

    [TestMethod]
    public void ClearReplacesWhatWasDrawnBeforeItAndTheClipStillAppliesAfterIt()
    {
        var frame = Render(16, 16, (r, _) =>
        {
            r.FillRect(new RectF(0, 0, 16, 16), Green);
            r.SetClip(new RectF(4, 4, 2, 2));
            r.Clear(Red);
            r.FillRect(new RectF(0, 0, 16, 16), Green);
            r.SetClip(null);
        });
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                var expected = x is 4 or 5 && y is 4 or 5 ? new Color(0, 255, 0) : new Color(255, 0, 0);
                Assert.AreEqual(expected, frame[x, y], $"pixel ({x},{y})");
            }
    }

    // ---- Rasterization of axis-aligned rectangles and sprites --------------------------------------------------------

    [TestMethod]
    public void RectanglesCoverExactlyThePixelsWhoseCentresTheyContainTopLeftEdgesIncluded()
    {
        var frame = Render(32, 16, (r, _) =>
        {
            r.FillRect(new RectF(1, 1, 2, 2), Color.White);           // whole pixels: (1,1)..(2,2)
            r.FillRect(new RectF(6.5f, 2.5f, 1, 1), Color.White);     // centre (6.5,2.5) on the top-left corner: (6,2) only
            r.FillRect(new RectF(10.25f, 6.25f, 2, 1), Color.White);  // centres 10.5, 11.5 and 6.5 inside: (10,6), (11,6)
            r.FillRect(new RectF(16.6f, 2.1f, 0.3f, 0.3f), Color.White); // contains no centre: nothing
            r.FillRect(new RectF(20, 9.5f, 3, 2), Color.White);       // rows 9 and 10: centre 9.5 on the top edge is in, 11.5 on the bottom edge is out
        });
        (int, int)[] expected = [(1, 1), (2, 1), (1, 2), (2, 2), (6, 2), (10, 6), (11, 6), (20, 9), (21, 9), (22, 9), (20, 10), (21, 10), (22, 10)];
        var lit = Lit(frame);
        CollectionAssert.AreEquivalent(expected, lit, $"lit pixels: {Describe(lit)}");
    }

    [TestMethod]
    public void SpritesCoverExactlyThePixelsWhoseCentresTheyContainAndSampleTheTexelUnderEachCentre()
    {
        Texture? dot = null, pair = null;
        var frame = Render(32, 16, (r, g) =>
        {
            dot ??= g.CreateTexture(new PixelImage(1, 1).Clear(Color.White), TextureFilter.Nearest);
            if (pair is null)
            {
                var image = new PixelImage(2, 1);
                image[0, 0] = Red;
                image[1, 0] = Green;
                pair = g.CreateTexture(image, TextureFilter.Nearest);
            }
            r.Draw(dot, dot.Bounds, new Vector2(6.5f, 2.5f), Color.White);         // (6,2) only, as the rectangle
            r.DrawStretched(dot, dot.Bounds, new RectF(10.25f, 6.25f, 2, 1), Color.White); // (10,6), (11,6)
            r.Draw(pair, pair.Bounds, new Vector2(20.5f, 10), Color.White, new DrawOptions(Scale: new Vector2(2, 1))); // 20..23
        });
        var lit = Lit(frame);
        (int, int)[] expected = [(6, 2), (10, 6), (11, 6), (20, 10), (21, 10), (22, 10), (23, 10)];
        CollectionAssert.AreEquivalent(expected, lit, $"lit pixels: {Describe(lit)}");
        // Nearest sampling at the centres 20.5 and 21.5 (texel 0, from 20.5 to 22.5) and 22.5, 23.5 (texel 1).
        Assert.AreEqual(new Color(255, 0, 0), frame[20, 10]);
        Assert.AreEqual(new Color(255, 0, 0), frame[21, 10]);
        Assert.AreEqual(new Color(0, 255, 0), frame[22, 10]);
        Assert.AreEqual(new Color(0, 255, 0), frame[23, 10]);
    }

    [TestMethod]
    public void IntegerAlignedSpritesCopyTexelsExactlyWithFlipsAndWholeScales()
    {
        var texel = Texel();
        Texture? texture = null;
        var frame = Render(32, 16, (r, g) =>
        {
            texture ??= g.CreateTexture(texel, TextureFilter.Nearest);
            r.Draw(texture, texture.Bounds, new Vector2(1, 1), Color.White);
            r.Draw(texture, texture.Bounds, new Vector2(6, 1), Color.White, new DrawOptions(FlipX: true));
            r.Draw(texture, texture.Bounds, new Vector2(11, 1), Color.White, new DrawOptions(FlipY: true));
            r.Draw(texture, texture.Bounds, new Vector2(16, 1), Color.White, new DrawOptions(Scale: new Vector2(2)));
        });
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                Assert.AreEqual(texel[x, y], frame[1 + x, 1 + y], $"plain texel ({x},{y})");
                Assert.AreEqual(texel[3 - x, y], frame[6 + x, 1 + y], $"flipped X texel ({x},{y})");
                Assert.AreEqual(texel[x, 3 - y], frame[11 + x, 1 + y], $"flipped Y texel ({x},{y})");
                for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 2; dx++)
                    Assert.AreEqual(texel[x, y], frame[16 + 2 * x + dx, 1 + 2 * y + dy], $"scaled texel ({x},{y})");
            }
    }

    /// <summary>4 × 4 texture whose every texel differs: (x·60, y·60, 200).</summary>
    private static PixelImage Texel()
    {
        var image = new PixelImage(4, 4);
        for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) image[x, y] = new Color((byte)(x * 60), (byte)(y * 60), 200);
        return image;
    }

    // ---- Resources ---------------------------------------------------------------------------------------------------

    [TestMethod]
    public void DrawingADisposedTextureThrowsObjectDisposedExceptionAndDrawsNothing()
    {
        var errors = new List<Exception>();
        var frame = Render(8, 8, (r, g) =>
        {
            var texture = g.CreateTexture(new PixelImage(2, 2).Clear(Color.White));
            texture.Dispose();
            try { r.Draw(texture, texture.Bounds, new Vector2(1, 1), Color.White); } catch (Exception e) { errors.Add(e); }
            try { r.DrawStretched(texture, texture.Bounds, new RectF(4, 4, 2, 2), Color.White); } catch (Exception e) { errors.Add(e); }
        });
        Assert.IsNotEmpty(errors, "drawing a disposed texture must throw");
        foreach (var e in errors) Assert.IsInstanceOfType<ObjectDisposedException>(e, e.ToString());
        Assert.AreEqual(0, errors.Count % 2, "both Draw and DrawStretched must throw on every frame");
        Assert.IsEmpty(Lit(frame), "nothing may be drawn");
    }

    [TestMethod]
    public void DrawingATextureOfAnotherDeviceThrowsArgumentExceptionAndDrawsNothing()
    {
        var other = new HeadlessGraphicsDevice();
        var errors = new List<Exception>();
        var frame = Render(8, 8, (r, _) =>
        {
            var texture = other.CreateTexture(new PixelImage(2, 2).Clear(Color.White));
            try { r.Draw(texture, texture.Bounds, new Vector2(1, 1), Color.White); } catch (Exception e) { errors.Add(e); }
            try { r.DrawStretched(texture, texture.Bounds, new RectF(4, 4, 2, 2), Color.White); } catch (Exception e) { errors.Add(e); }
        });
        Assert.IsNotEmpty(errors, "drawing another device's texture must throw");
        foreach (var e in errors)
        {
            Assert.IsInstanceOfType<ArgumentException>(e, e.ToString());
            StringAssert.Contains(e.Message, "another graphics device");
        }
        Assert.IsEmpty(Lit(frame), "nothing may be drawn");
    }

    // ---- Canvas alpha and captures -----------------------------------------------------------------------------------

    [TestMethod]
    public void CapturesAreOpaqueWhateverWasDrawn()
    {
        var frame = Render(16, 8, (r, _) =>
        {
            r.Clear(new Color(200, 100, 50, 128));
            r.FillRect(new RectF(0, 0, 8, 8), new Color(0, 0, 255, 128));
            r.SetBlendMode(BlendMode.Additive);
            r.FillRect(new RectF(8, 0, 8, 8), new Color(0, 255, 0, 64));
            r.SetBlendMode(BlendMode.Alpha);
        });
        var translucent = new List<(int, int)>();
        for (int y = 0; y < 8; y++) for (int x = 0; x < 16; x++) if (frame[x, y].A != 255) translucent.Add((x, y));
        Assert.IsEmpty(translucent, $"captures must be opaque: {translucent.Count} pixels are not, first {Describe(translucent.Take(4))} alpha {frame[0, 0].A}");
        // A translucent Clear stores its colour channels as given.
        AssertColour(frame, 15, 7, BlendReference.Additive(new Color(200, 100, 50), new Color(0, 255, 0, 64)), "additive over the translucent clear");
    }

    // ---- Blending ----------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BlendModesFollowTheWrittenEquations()
    {
        Color grey = new(128, 128, 128);
        Color alpha = new(255, 0, 0, 128), add = new(100, 0, 0, 128), mul = new(255, 0, 0, 128), tint = new(255, 255, 255, 128);
        Texture? white = null;
        var frame = Render(40, 8, (r, g) =>
        {
            white ??= g.CreateTexture(new PixelImage(4, 4).Clear(new Color(0, 200, 100)));
            r.FillRect(new RectF(0, 0, 40, 8), grey);
            r.FillRect(new RectF(0, 0, 8, 8), alpha);
            r.SetBlendMode(BlendMode.Additive);
            r.FillRect(new RectF(8, 0, 8, 8), add);
            r.SetBlendMode(BlendMode.Multiply);
            r.FillRect(new RectF(16, 0, 8, 8), mul);
            r.SetBlendMode(BlendMode.Alpha);
            r.Draw(white, white.Bounds, new Vector2(24, 2), tint);
        });
        // Colour, exact up to the rounding of 8-bit results (see AssertColour).
        AssertColour(frame, 4, 4, BlendReference.Alpha(grey, alpha), "alpha");
        AssertColour(frame, 12, 4, BlendReference.Additive(grey, add), "additive");
        AssertColour(frame, 20, 4, BlendReference.Multiply(grey, mul), "multiply");
        AssertColour(frame, 25, 3, BlendReference.Alpha(grey, new Color(0, 200, 100, 128)), "tinted sprite");
    }

    // ---- Clip and camera ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void TheClipIsInScreenPixelsTruncatedAndIgnoresTheCamera()
    {
        var frame = Render(32, 16, (r, _) =>
        {
            r.PushCamera(new Camera2D(32, 16) { Position = new Vector2(10, 0) });
            r.SetClip(new RectF(2.9f, 3.9f, 2.9f, 1.9f)); // whole pixels: x 2..3, y 3
            r.FillRect(new RectF(0, 0, 32, 16), Color.White);
            r.SetClip(null);
            r.PopCamera();
        });
        var lit = Lit(frame);
        CollectionAssert.AreEquivalent(new[] { (2, 3), (3, 3) }, lit, $"lit pixels: {Describe(lit)}");
    }

    [TestMethod]
    public void TheCameraMovesDrawsByItsRenderOffset()
    {
        var camera = new Camera2D(32, 16) { Position = new Vector2(-5, -3) };
        var frame = Render(32, 16, (r, _) =>
        {
            r.PushCamera(camera);
            r.FillRect(new RectF(1, 1, 2, 1), Color.White);
            r.PopCamera();
            r.FillRect(new RectF(20, 10, 1, 1), Color.White);
        });
        var offset = camera.RenderOffset;
        var lit = Lit(frame);
        (int, int)[] expected = [((int)(1 + offset.X), (int)(1 + offset.Y)), ((int)(2 + offset.X), (int)(1 + offset.Y)), (20, 10)];
        CollectionAssert.AreEquivalent(expected, lit, $"offset {offset}, lit pixels: {Describe(lit)}");
    }

    // ---- Shapes: coverage within declared bounds ---------------------------------------------------------------------

    [TestMethod]
    public void CirclesCoverTheirDiscWithinTheDeclaredCoverageBand()
    {
        const float radius = 5;
        var frame = Render(32, 32, (r, _) => r.FillCircle(new Vector2(16, 16), radius, Color.White));
        var lit = Lit(frame);
        // Geometry, exact: the centre is covered and nothing lies beyond the radius plus the edge pixel.
        Assert.IsTrue(lit.Contains((15, 15)) && lit.Contains((16, 16)), "the centre must be covered");
        var outside = lit.Where(p => Vector2.Distance(new Vector2(p.X + 0.5f, p.Y + 0.5f), new Vector2(16, 16)) > radius + 1).ToList();
        Assert.IsEmpty(outside, $"pixels beyond the radius: {Describe(outside)}");
        // Coverage: the disc's area within ±15 % for the polygon approximation of GPU backends; the software renderer
        // (source of variation: its own rasterizer, measured up to +30 %) gets [0.85, 1.35].
        double area = Math.PI * radius * radius;
        (double low, double high) = Allowed.HasFlag(Variation.ShapeCoverage) ? (0.85, 1.35) : (0.85, 1.15);
        Assert.IsTrue(lit.Count >= area * low && lit.Count <= area * high, $"{lit.Count} pixels lit for a disc of {area:0.#} (band {low}..{high})");
    }

    [TestMethod]
    public void LinesCoverTheirQuadWithinTheDeclaredCoverageBand()
    {
        var from = new Vector2(4, 4);
        var to = new Vector2(28, 12);
        var frame = Render(32, 16, (r, _) => r.DrawLine(from, to, 2, Color.White));
        var lit = Lit(frame);
        // Geometry: both ends and the middle are covered, exactly; with the software renderer's own line algorithm (its
        // declared source of variation) within one pixel. Nothing lies farther than the half thickness plus one edge
        // pixel from the segment.
        int near = Allowed.HasFlag(Variation.ShapeCoverage) ? 1 : 0;
        foreach (var (px, py) in new[] { (5, 4), (16, 8), (27, 11) })
            Assert.IsTrue(lit.Any(q => Math.Abs(q.X - px) <= near && Math.Abs(q.Y - py) <= near), $"({px},{py}) must be covered (±{near}): {Describe(lit)}");
        var far = lit.Where(p => DistanceToSegment(new Vector2(p.X + 0.5f, p.Y + 0.5f), from, to) > 1 + 1).ToList();
        Assert.IsEmpty(far, $"pixels away from the line: {Describe(far)}");
        // Coverage: length × thickness within ±25 % (edge pixels of a slanted quad). The software renderer's own line
        // algorithm (its declared source of variation; measured 0.63× for this 2-px line, 2× for a 1-px diagonal) gets [0.5, 2].
        double area = Vector2.Distance(from, to) * 2;
        (double low, double high) = Allowed.HasFlag(Variation.ShapeCoverage) ? (0.5, 2.0) : (0.75, 1.25);
        Assert.IsTrue(lit.Count >= area * low && lit.Count <= area * high, $"{lit.Count} pixels lit for a line of {area:0.#} (band {low}..{high})");
    }

    [TestMethod]
    public void TrianglesCoverTheirInteriorWithEitherWinding()
    {
        var frame = Render(32, 16, (r, _) =>
        {
            r.FillTriangle(new Vector2(1, 1), new Vector2(11, 1), new Vector2(1, 11), Color.White);
            r.FillTriangle(new Vector2(16, 1), new Vector2(16, 11), new Vector2(26, 1), Color.White);
        });
        foreach (var (x0, label) in new[] { (1, "clockwise"), (16, "counter-clockwise") })
        {
            var lit = Lit(frame, x0 - 1, 0, x0 + 12, 16);
            Assert.IsTrue(lit.Contains((x0 + 2, 3)), $"{label}: an interior pixel must be covered");
            Assert.IsFalse(lit.Contains((x0 + 8, 8)), $"{label}: a pixel past the hypotenuse must not be covered");
            // Coverage: the triangle's 50 px² within ±1 edge pixel per pixel of perimeter (≈34 px), here ±20 %.
            Assert.IsTrue(lit.Count is >= 40 and <= 60, $"{label}: {lit.Count} pixels lit for a 50 px² triangle");
        }
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0, 1);
        return Vector2.Distance(p, a + ab * t);
    }

    // ---- Sampling ----------------------------------------------------------------------------------------------------

    [TestMethod]
    public void LinearSamplingBlendsNeighboursMonotonicallyWithinTheKernelAndNearestKeepsHardEdges()
    {
        Texture? nearest = null, linear = null;
        var frame = Render(64, 16, (r, g) =>
        {
            if (nearest is null)
            {
                var pair = new PixelImage(2, 1);
                pair[0, 0] = Color.Black;
                pair[1, 0] = Color.White;
                nearest = g.CreateTexture(pair, TextureFilter.Nearest);
                linear = g.CreateTexture(pair, TextureFilter.Linear);
            }
            var stretch = new DrawOptions(Scale: new Vector2(16, 8));
            r.Draw(nearest, nearest.Bounds, new Vector2(0, 0), Color.White, stretch);
            r.Draw(linear!, linear!.Bounds, new Vector2(0, 8), Color.White, stretch);
        });
        // Filtering, exact for nearest: the edge falls between pixels 15 and 16.
        for (int x = 0; x < 32; x++) Assert.AreEqual(x < 16 ? 0 : 255, frame[x, 4].R, $"nearest pixel {x}");
        // Filtering, bounded by the bilinear kernel: flat outside the middle texel span, a non-decreasing ramp across it,
        // and each value within ±2/255 of the kernel (source of variation: interpolation precision of the sampler).
        for (int x = 0; x < 32; x++)
        {
            double u = (x + 0.5) / 16 - 0.5; // texel space, centres at 0 and 1
            int expected = (int)Math.Round(Math.Clamp(u, 0, 1) * 255);
            Assert.IsTrue(Math.Abs(frame[x, 12].R - expected) <= 2, $"linear pixel {x} is {frame[x, 12].R}, kernel {expected}");
            if (x > 0) Assert.IsTrue(frame[x, 12].R >= frame[x - 1, 12].R, $"linear ramp must not decrease at {x}");
        }
    }
}

/// <summary>The blend equations of the contract on 8-bit colours (straight alpha, results rounded to nearest).</summary>
public static class BlendReference
{
    private static byte Q(double v) => (byte)Math.Round(Math.Clamp(v, 0, 1) * 255);

    /// <summary>rgb = src·a + dst·(1 − a).</summary>
    public static Color Alpha(Color dst, Color src)
    {
        double a = src.A / 255.0;
        return new Color(Q(src.R / 255.0 * a + dst.R / 255.0 * (1 - a)), Q(src.G / 255.0 * a + dst.G / 255.0 * (1 - a)), Q(src.B / 255.0 * a + dst.B / 255.0 * (1 - a)));
    }

    /// <summary>rgb = src·a + dst.</summary>
    public static Color Additive(Color dst, Color src)
    {
        double a = src.A / 255.0;
        return new Color(Q(src.R / 255.0 * a + dst.R / 255.0), Q(src.G / 255.0 * a + dst.G / 255.0), Q(src.B / 255.0 * a + dst.B / 255.0));
    }

    /// <summary>rgb = src·dst + dst·(1 − a).</summary>
    public static Color Multiply(Color dst, Color src)
    {
        double a = src.A / 255.0;
        static double M(byte s, byte d, double a) => s / 255.0 * (d / 255.0) + d / 255.0 * (1 - a);
        return new Color(Q(M(src.R, dst.R, a)), Q(M(src.G, dst.G, a)), Q(M(src.B, dst.B, a)));
    }
}
