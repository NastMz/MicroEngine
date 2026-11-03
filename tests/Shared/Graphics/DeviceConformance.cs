using System.Diagnostics;
using System.Numerics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics.Conformance;

/// <summary>
/// The conformance suite of the graphics device contract (version 1). It runs unchanged against every device: a concrete
/// class names how to create the device and what it must declare. Each test names the property it checks; exact
/// properties are checked exactly, and a tolerance appears only with its named source of variation. A test of a feature
/// the device does not declare asserts the typed <see cref="GraphicsCapabilityException"/> instead.
/// </summary>
public abstract class DeviceConformance
{
    private const uint Black = 0x000000FF; // opaque black, byte-exact

    private ITestSurface? _surface;
    private IGraphicsDevice? _device;
    private Painter? _painter;

    /// <summary>Creates the device under test over <paramref name="surface"/>.</summary>
    protected abstract IGraphicsDevice CreateDevice(ITestSurface surface);

    /// <summary>Creates a surface of <paramref name="width"/> × <paramref name="height"/> pixels (hidden windows on native backends).</summary>
    protected abstract ITestSurface CreateSurface(int width, int height);

    /// <summary>The backend name the device must report.</summary>
    protected abstract string ExpectedBackend { get; }

    /// <summary>The API the device must report after creation, or null when any API is accepted.</summary>
    protected virtual string? ExpectedApi => null;

    /// <summary>The tier the device must declare.</summary>
    protected virtual GraphicsTier ExpectedTier => GraphicsTier.Tier0;

    /// <summary>The exact optional features the device must declare.</summary>
    protected abstract GraphicsFeatures ExpectedFeatures { get; }

    /// <summary>
    /// Pixels of the rotated-quad coverage check allowed to differ from the written rule. Zero for conformant devices;
    /// a diagnostic device that does not claim conformance (SDL's software renderer) names its own.
    /// </summary>
    protected virtual int CoverageTolerance => 0;

    protected IGraphicsDevice Device => _device!;
    protected Painter Painter => _painter ??= new Painter(Device);
    protected GraphicsCapabilities Caps => Device.Capabilities;
    protected ITestSurface Surface => _surface!;

    [TestInitialize]
    public void Open()
    {
        _surface = CreateSurface(64, 48);
        _device = CreateDevice(_surface);
    }

    [TestCleanup]
    public void Close()
    {
        _painter?.Dispose();
        _device?.Dispose();
        _surface?.Dispose();
    }

    // ---------------------------------------------------------------- capabilities

    [TestMethod]
    public void Capabilities_DeclareTheExpectedTierFeaturesAndApi()
    {
        Console.WriteLine($"{Caps.Backend} api={Caps.ApiName} adapter=\"{Caps.Adapter}\" features={Caps.Features}");
        Assert.AreEqual(ExpectedBackend, Caps.Backend);
        Assert.AreEqual(ExpectedTier, Caps.Tier);
        Assert.AreEqual(ExpectedFeatures, Caps.Features, $"declared {Caps.Features}");
        if (ExpectedApi is { } api) Assert.AreEqual(api, Caps.ApiName, "backend identity is what the device reports after creation");
        Assert.IsFalse(string.IsNullOrEmpty(Caps.ApiName));
        Assert.IsTrue(Caps.Supports(TextureFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.RenderTarget), "Rgba8Unorm is mandatory");
        foreach (var preset in BlendState.Presets) Assert.Contains(preset, Caps.BuiltInBlendStates);
        Assert.AreEqual(ExpectedTier == GraphicsTier.Tier0, Caps.ShaderFormats == ShaderFormat.None, "tier 0 has no shader format");
        Assert.Contains(PresentMode.VSync, Caps.PresentModes);
    }

    // ---------------------------------------------------------------- clear and placement (exact)

    [TestMethod]
    public void Clear_FillsTheWholeTargetExactly()
    {
        var target = Target(16, 8);
        Painter.Paint(target, [], clear: PremultipliedColor.FromBytes(32, 64, 96, 255));
        var image = Read(target);
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
                Assert.AreEqual(0x204060FFu, image[x, y], $"pixel ({x},{y})");
    }

    [TestMethod]
    public void Sprite_AtAnIntegerPosition_CopiesTexelsExactly()
    {
        uint[] texels = [0xFF0000FF, 0x00FF00FF, 0x0000FFFF, 0xFFFF00FF, 0xFF00FFFF, 0x00FFFFFF];
        var texture = Device.CreateTexture(new TextureDescription(3, 2), Texels.Bytes(texels));
        var target = Target(16, 12);
        Painter.Paint(target, new Quad(texture, new RectF(0, 0, 3, 2), Matrix3x2.CreateTranslation(5, 7), ColorF.White), clear: Opaque(0, 0, 0));
        var image = Read(target);
        Assert.AreEqual(new RectI(5, 7, 3, 2), image.BoundsOfNot(Black), "position and size");
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                Assert.AreEqual(texels[y * 3 + x], image[5 + x, 7 + y], $"texel ({x},{y})");
    }

    [TestMethod]
    public void Sprite_AtAHalfPixelPosition_CoversThePixelsWhoseCentresItContains()
    {
        // Written rule: a pixel is covered when its centre lies inside; a centre on a left or top edge is inside, on a
        // right or bottom edge outside. A 2 × 2 quad from 2.5 to 4.5 has the centres of pixels 2 and 4 exactly on its
        // edges: pixel 2 is in (left and top edges), pixel 4 is out (right and bottom edges), pixel 3 is inside.
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var target = Target(8, 8);
        Painter.Paint(target, new Quad(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(2) * Matrix3x2.CreateTranslation(2.5f, 2.5f), ColorF.White), clear: Opaque(0, 0, 0));
        Assert.AreEqual(new RectI(2, 2, 2, 2), Read(target).BoundsOfNot(Black), "top-left rule at pixel centres");
    }

    [TestMethod]
    public void Sprite_ScaledByAnInteger_MapsEachTexelToABlockExactly()
    {
        uint[] texels = [0xFF0000FF, 0x00FF00FF, 0x0000FFFF, 0xFFFFFFFF];
        var texture = Device.CreateTexture(new TextureDescription(2, 2), Texels.Bytes(texels));
        var target = Target(16, 16);
        Painter.Paint(target, new Quad(texture, new RectF(0, 0, 2, 2), Matrix3x2.CreateScale(4) * Matrix3x2.CreateTranslation(3, 2), ColorF.White), clear: Opaque(0, 0, 0));
        var image = Read(target);
        Assert.AreEqual(new RectI(3, 2, 8, 8), image.BoundsOfNot(Black));
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
                Assert.AreEqual(texels[(y / 4) * 2 + x / 4], image[3 + x, 2 + y], $"pixel ({x},{y}) of the scaled sprite");
    }

    [TestMethod]
    public void Sprite_MirroredByANegativeScale_ReversesTexelsExactly()
    {
        uint[] texels = [0xFF0000FF, 0x00FF00FF, 0x0000FFFF];
        var texture = Device.CreateTexture(new TextureDescription(3, 1), Texels.Bytes(texels));
        var target = Target(16, 4);
        Painter.Paint(target, new Quad(texture, new RectF(0, 0, 3, 1), Matrix3x2.CreateScale(-1, 1) * Matrix3x2.CreateTranslation(10, 1), ColorF.White), clear: Opaque(0, 0, 0));
        var image = Read(target);
        Assert.AreEqual(new RectI(7, 1, 3, 1), image.BoundsOfNot(Black));
        for (var x = 0; x < 3; x++) Assert.AreEqual(texels[2 - x], image[7 + x, 1]);
    }

    [TestMethod]
    public void Geometry_TrianglesWithAWhiteTexel_CoverTheirPixelsWithTheVertexColour()
    {
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var target = Target(16, 16);
        var red = new ColorF(1, 0, 0, 1);
        Vertex2D[] quad = [new(new(2, 3), default, red), new(new(12, 3), default, red), new(new(12, 9), default, red), new(new(2, 9), default, red)];
        DrawGeometry(target, white, quad, [0, 1, 2, 0, 2, 3], Opaque(0, 0, 0));
        var image = Read(target);
        Assert.AreEqual(new RectI(2, 3, 10, 6), image.BoundsOfNot(Black), "pixels whose centres lie inside the quad");
        Assert.AreEqual(0xFF0000FFu, image[2, 3]);
        Assert.AreEqual(0xFF0000FFu, image[11, 8]);
    }

    [TestMethod]
    public void Coverage_OfARotatedQuad_FollowsThePixelCentreRule()
    {
        // A 30-degree rotated 20 x 12 quad: the expected coverage is computed here, independently, from the written rule
        // (pixel centre inside, top-left rule on ties). Pixels whose centre lies within 1/64 px of an edge are not
        // compared: named source of variation, the rasterizers' sub-pixel snapping of vertex positions (1/256 px).
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var target = Target(40, 40);
        var world = Matrix3x2.CreateTranslation(-10, -6) * Matrix3x2.CreateRotation(MathF.PI / 6) * Matrix3x2.CreateTranslation(20.3f, 19.7f);
        Painter.Paint(target, new Quad(white, new RectF(0, 0, 20, 12), world, ColorF.White), clear: Opaque(0, 0, 0));
        var image = Read(target);
        Vector2[] corners = [Vector2.Transform(new(0, 0), world), Vector2.Transform(new(20, 0), world), Vector2.Transform(new(20, 12), world), Vector2.Transform(new(0, 12), world)];
        int compared = 0, wrong = 0;
        for (var y = 0; y < 40; y++)
            for (var x = 0; x < 40; x++)
            {
                var (inside, margin) = InsideConvex(corners, new Vector2(x + 0.5f, y + 0.5f));
                if (margin < 1f / 64) continue;
                compared++;
                if ((image[x, y] == 0xFFFFFFFFu) != inside) wrong++;
            }
        Assert.IsTrue(compared > 1500, $"{compared} pixels compared");
        Assert.IsTrue(wrong <= CoverageTolerance, $"{wrong} of {compared} pixels differ from the pixel-centre rule (allowed {CoverageTolerance})");
    }

    // ---------------------------------------------------------------- blending and alpha (±1/255)

    [TestMethod]
    public void Blend_EveryPreset_MatchesItsFormulaWithin8BitQuantization()
    {
        var dst = (R: 51, G: 102, B: 153, A: 204);
        var tint = PremultipliedColor.FromBytes(102, 51, 26, 128); // premultiplied source, exact byte values
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        foreach (var preset in BlendState.Presets)
        {
            var target = Target(4, 4);
            Painter.Paint(target, [], clear: PremultipliedColor.FromBytes((byte)dst.R, (byte)dst.G, (byte)dst.B, (byte)dst.A));
            Painter.Paint(target, new Quad(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(4), tint), blend: preset);
            var image = Read(target);
            float[] s = [tint.R, tint.G, tint.B, tint.A];
            float[] d = [dst.R / 255f, dst.G / 255f, dst.B / 255f, dst.A / 255f];
            for (var ch = 0; ch < 4; ch++)
            {
                var expected = Blend(preset, s, d, ch);
                var actual = image.Channel(1, 1, ch);
                // Named source of variation: 8-bit quantization of the blended value (an exact .5 may round either way
                // between vendors and APIs): ±1/255.
                Assert.IsTrue(Math.Abs(actual - expected * 255f) <= 1f, $"{Name(preset)} channel {ch}: expected {expected * 255f:F2}, got {actual}");
            }
        }
    }

    [TestMethod]
    public void Premultiplied_LinearEdgeOverWhite_KeepsItsColourWithoutADarkFringe()
    {
        // Premultiplied colour (the contract's convention): an opaque red texel next to a transparent one, stretched
        // linearly over white, mixes to (1 - t, 0, 0, 1 - t); composited over white that is red 1 everywhere. Straight
        // alpha would darken the middle (red 191 measured on every backend before premultiplication).
        var edge = Device.CreateTexture(new TextureDescription(2, 1), Texels.Bytes(0xFF0000FF, 0x00000000));
        var target = Target(32, 4);
        Painter.Paint(target, new Quad(edge, new RectF(0, 0, 2, 1), Matrix3x2.CreateScale(16, 4), ColorF.White),
            clear: Opaque(255, 255, 255), sampler: SamplerDescription.LinearClamp);
        var image = Read(target);
        for (var x = 0; x < 32; x++)
        {
            // Named sources of variation: sub-texel weight precision plus 8-bit quantization (±2/255 on G and B).
            Assert.IsTrue(image.Channel(x, 1, 0) >= 254, $"red at x={x} is {image.Channel(x, 1, 0)}: no dark fringe");
            var t = Math.Clamp((x + 0.5f) / 16f - 0.5f, 0f, 1f);
            Assert.IsTrue(Math.Abs(image.Channel(x, 1, 1) - t * 255f) <= 2f, $"green at x={x}: expected {t * 255f:F1}, got {image.Channel(x, 1, 1)}");
        }
    }

    // ---------------------------------------------------------------- sampling

    [TestMethod]
    public void Sampler_NearestAndLinear_AreDistinguishable()
    {
        var pair = Device.CreateTexture(new TextureDescription(2, 1), Texels.Bytes(0x000000FF, 0xFFFFFFFF));
        var nearest = Stretch(pair, SamplerDescription.PointClamp);
        var linear = Stretch(pair, SamplerDescription.LinearClamp);
        for (var x = 0; x < 32; x++) Assert.AreEqual(x < 16 ? 0 : 255, nearest.Channel(x, 0, 0), $"nearest x={x}");
        // Linear: clamped ends exact, a monotonic ramp in between following the bilinear kernel. Named sources: sub-texel
        // weight precision plus output quantization, at most ±2/255 together.
        for (var x = 0; x < 32; x++)
        {
            var t = Math.Clamp((x + 0.5f) / 16f - 0.5f, 0f, 1f);
            var v = linear.Channel(x, 0, 0);
            if (x < 8) Assert.AreEqual(0, v, $"linear clamped x={x}");
            else if (x >= 24) Assert.AreEqual(255, v, $"linear clamped x={x}");
            else Assert.IsTrue(Math.Abs(v - t * 255f) <= 2f, $"linear x={x}: expected {t * 255f:F1}, got {v}");
            if (x > 0) Assert.IsTrue(v >= linear.Channel(x - 1, 0, 0), $"monotonic at x={x}");
        }
        Assert.IsTrue(Enumerable.Range(0, 32).Count(x => linear.Channel(x, 0, 0) is > 0 and < 255) >= 14, "linear produces intermediate values");
    }

    [TestMethod]
    public void Sampler_RepeatAndMirror_TileExactlyOrFailTyped()
    {
        uint[] texels = [0xFF0000FF, 0x00FF00FF, 0x0000FFFF, 0xFFFFFFFF];
        var texture = Device.CreateTexture(new TextureDescription(2, 2), Texels.Bytes(texels));
        foreach (var (mode, feature) in new[] { (AddressMode.Repeat, GraphicsFeatures.AddressRepeat), (AddressMode.Mirror, GraphicsFeatures.AddressMirror) })
        {
            var description = new SamplerDescription(U: mode, V: mode);
            if (!Caps.Has(feature))
            {
                var e = Assert.ThrowsExactly<GraphicsCapabilityException>(() => Device.GetSampler(description));
                Assert.AreEqual(feature, e.Missing);
                continue;
            }
            var target = Target(8, 8);
            Painter.Paint(target, new Quad(texture, new RectF(0, 0, 6, 4), Matrix3x2.Identity, ColorF.White), clear: Opaque(0, 0, 0), sampler: description);
            var image = Read(target);
            for (var y = 0; y < 4; y++)
                for (var x = 0; x < 6; x++)
                {
                    int tx = x % 2, ty = y % 2;
                    if (mode == AddressMode.Mirror) { tx = (x / 2) % 2 == 1 ? 1 - tx : tx; ty = (y / 2) % 2 == 1 ? 1 - ty : ty; }
                    Assert.AreEqual(texels[ty * 2 + tx], image[x, y], $"{mode} ({x},{y})");
                }
        }
    }

    [TestMethod]
    public void Sampler_SeparateFilters_MinifyAndMagnifyWithTheirOwnFilterOrFailTyped()
    {
        var description = new SamplerDescription(Min: Filter.Nearest, Mag: Filter.Linear);
        if (!Caps.Has(GraphicsFeatures.SeparateFilters))
        {
            Assert.AreEqual(GraphicsFeatures.SeparateFilters, Assert.ThrowsExactly<GraphicsCapabilityException>(() => Device.GetSampler(description)).Missing);
            return;
        }
        var pair = Device.CreateTexture(new TextureDescription(2, 1), Texels.Bytes(0x000000FF, 0xFFFFFFFF));
        var magnified = Stretch(pair, description);
        Assert.IsTrue(Enumerable.Range(0, 32).Count(x => magnified.Channel(x, 0, 0) is > 0 and < 255) >= 14, "magnification uses the linear filter");
        // Minified: 8 texels over 4 pixels, nearest: every pixel is exactly one of the texels.
        var stripes = Device.CreateTexture(new TextureDescription(8, 1), Texels.Bytes(0x000000FF, 0xFFFFFFFF, 0x000000FF, 0xFFFFFFFF, 0x000000FF, 0xFFFFFFFF, 0x000000FF, 0xFFFFFFFF));
        var target = Target(4, 1);
        Painter.Paint(target, new Quad(stripes, new RectF(0, 0, 8, 1), Matrix3x2.CreateScale(0.5f, 1), ColorF.White), clear: Opaque(0, 0, 255), blend: BlendState.Opaque, sampler: description);
        var minified = Read(target);
        for (var x = 0; x < 4; x++) Assert.IsTrue(minified.Channel(x, 0, 0) is 0 or 255, $"minification uses the nearest filter at x={x}: {minified.Channel(x, 0, 0)}");
    }

    // ---------------------------------------------------------------- scissor and viewport (exact)

    [TestMethod]
    public void Scissor_ClipsDrawsExactlyAndClearsIgnoreIt()
    {
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var target = Target(32, 24);
        var scissor = new RectI(3, 5, 10, 7);
        Painter.Paint(target, [new Quad(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(32, 24), ColorF.White)], clear: Opaque(0, 0, 0), scissor: scissor);
        var image = Read(target);
        Assert.AreEqual(scissor, image.BoundsOfNot(Black));
        Assert.AreEqual(Black, image[0, 0], "the clear covered the whole target despite the scissor");
        Assert.AreEqual(0xFFFFFFFFu, image[3, 5]);
        Assert.AreEqual(0xFFFFFFFFu, image[12, 11]);
    }

    [TestMethod]
    public void Viewport_OffsetsBuiltInPositionsAndClips_ScissorStaysAbsolute()
    {
        var target = Target(32, 32);
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var big = new Quad(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(20), ColorF.White);
        Painter.Paint(target, [big], clear: Opaque(0, 0, 0), viewport: new RectI(8, 6, 10, 10));
        Assert.AreEqual(new RectI(8, 6, 10, 10), Read(target).BoundsOfNot(Black), "positions relative to the viewport origin, clipped by it");
        Painter.Paint(target, [big], clear: Opaque(0, 0, 0), viewport: new RectI(8, 6, 10, 10), scissor: new RectI(10, 10, 4, 4));
        Assert.AreEqual(new RectI(10, 10, 4, 4), Read(target).BoundsOfNot(Black), "scissor in absolute target pixels");
    }

    // ---------------------------------------------------------------- targets, readback, ordering (exact)

    [TestMethod]
    public void RenderTarget_RoundTripIsExact()
    {
        uint[] texels = [0x11223344, 0xFF0000FF, 0x00FF00FF, 0x0000FFFF, 0x80808080, 0xFFFFFFFF];
        var texture = Device.CreateTexture(new TextureDescription(3, 2), Texels.Bytes(texels));
        var a = Target(8, 8);
        var b = Target(16, 16);
        Painter.Paint(a, new Quad(texture, new RectF(0, 0, 3, 2), Matrix3x2.CreateTranslation(1, 1), ColorF.White), clear: Opaque(0, 0, 0), blend: BlendState.Opaque);
        Painter.Paint(b, new Quad(a.Color!, new RectF(0, 0, 8, 8), Matrix3x2.CreateTranslation(4, 4), ColorF.White), clear: Opaque(0, 0, 0), blend: BlendState.Opaque);
        var first = Read(a);
        var second = Read(b);
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                Assert.AreEqual(texels[y * 3 + x], first[1 + x, 1 + y], "texture → target, including a translucent texel copied with Opaque");
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
                Assert.AreEqual(first[x, y], second[4 + x, 4 + y], $"target → target ({x},{y})");
    }

    [TestMethod]
    public void Readback_FenceCompletes_AndRegionsAgreeWithTheWholeTarget()
    {
        var target = PatternTarget(12, 10);
        var whole = Device.ReadPixels(target, target.Bounds);
        whole.Wait();
        Assert.IsTrue(whole.IsCompleted, "Wait returns only after the fence signalled");
        Assert.AreEqual(12 * 10 * 4, whole.Pixels.Length);
        var region = new RectI(2, 3, 5, 4);
        var part = Device.ReadPixels(target, region);
        var clock = Stopwatch.StartNew();
        while (!part.IsCompleted) // poll without blocking: the fence must complete on its own
        {
            Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(5), "the readback fence never completed");
            Thread.Yield();
        }
        var full = new Image(12, 10, whole.Pixels.ToArray());
        var sub = new Image(5, 4, part.Pixels.ToArray());
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 5; x++)
                Assert.AreEqual(full[2 + x, 3 + y], sub[x, y], $"region pixel ({x},{y})");
    }

    [TestMethod]
    public void Readback_OfABgraTarget_ReturnsBytesInBgraOrder()
    {
        if (!Caps.Supports(TextureFormat.Bgra8Unorm, TextureUsage.RenderTarget | TextureUsage.Sampled)) Assert.Inconclusive("this device has no Bgra8Unorm targets");
        var target = Device.CreateRenderTarget(new RenderTargetDescription(2, 2, TextureFormat.Bgra8Unorm));
        Painter.Paint(target, [], clear: PremultipliedColor.FromBytes(255, 128, 0));
        var request = Device.ReadPixels(target, target.Bounds);
        request.Wait();
        Assert.AreEqual(TextureFormat.Bgra8Unorm, request.Format);
        CollectionAssert.AreEqual(new byte[] { 0, 128, 255, 255 }, request.Pixels[..4].ToArray(), "B, G, R, A");
    }

    [TestMethod]
    public void Updates_TakeEffectInRecordingOrder()
    {
        var target = Target(16, 8);
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var vertices = Device.CreateBuffer(new BufferDescription(4 * Vertex2D.SizeInBytes, BufferUsage.Vertex));
        var indices = Device.CreateBuffer(new BufferDescription(12, BufferUsage.Index));
        var list = Device.BeginCommands();
        list.UpdateBuffer<ushort>(indices, 0, [0, 1, 2, 0, 2, 3]);
        list.UpdateBuffer<Vertex2D>(vertices, 0, QuadVertices(0, 0, 4, 4, new ColorF(1, 0, 0, 1)));
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Clear, Clear: Opaque(0, 0, 0))));
        BindBuiltIn(list, target, white, vertices, indices);
        list.DrawIndexed(6);
        list.EndPass();
        list.UpdateBuffer<Vertex2D>(vertices, 0, QuadVertices(8, 2, 4, 4, new ColorF(0, 1, 0, 1)));
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Load)));
        BindBuiltIn(list, target, white, vertices, indices);
        list.DrawIndexed(6);
        list.EndPass();
        Device.Submit(list);
        var image = Read(target);
        Assert.AreEqual(0xFF0000FFu, image[1, 1], "the first draw saw the first update");
        Assert.AreEqual(0x00FF00FFu, image[9, 3], "the second draw saw the second update");
        Assert.AreEqual(Black, image[1, 6]);
        Assert.AreEqual(new RectI(0, 0, 12, 6), image.BoundsOfNot(Black), "LoadOp.Load kept the first pass");
    }

    [TestMethod]
    public void UpdateTexture_ReplacesExactlyTheRegion()
    {
        var texture = Device.CreateTexture(new TextureDescription(4, 4), Texels.Bytes(Enumerable.Repeat(0xFF0000FFu, 16).ToArray()));
        var target = Target(4, 4);
        var list = Device.BeginCommands();
        list.UpdateTexture(texture, new RectI(1, 2, 2, 1), Texels.Bytes(0x00FF00FF, 0x0000FFFF));
        Device.Submit(list);
        Painter.Paint(target, new Quad(texture, new RectF(0, 0, 4, 4), Matrix3x2.Identity, ColorF.White), blend: BlendState.Opaque);
        var image = Read(target);
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                var expected = (x, y) switch { (1, 2) => 0x00FF00FFu, (2, 2) => 0x0000FFFFu, _ => 0xFF0000FFu };
                Assert.AreEqual(expected, image[x, y], $"({x},{y})");
            }
    }

    [TestMethod]
    public void Counters_CountTheSubmittedWorkExactly()
    {
        if (Device is not GraphicsDeviceBase core) { Assert.Inconclusive("the device does not derive from GraphicsDeviceBase"); return; }
        var a = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFF0000FF));
        var b = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0x00FF00FF));
        var target = Target(8, 8);
        var before = core.Counters;
        Painter.Paint(target, [new Quad(a, new RectF(0, 0, 1, 1), Matrix3x2.Identity, ColorF.White), new Quad(b, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(1, 0), ColorF.White),
            new Quad(a, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(2, 0), ColorF.White)], clear: Opaque(0, 0, 0));
        var spent = core.Counters - before;
        Assert.AreEqual(1, spent.Submissions);
        Assert.AreEqual(1, spent.Passes);
        Assert.AreEqual(3, spent.DrawCalls, "one indexed draw per quad, as recorded");
        Assert.AreEqual(1, spent.PipelineChanges);
        Assert.AreEqual(3, spent.TextureBindings);
        Assert.AreEqual(2, spent.BufferUpdates);
        Assert.AreEqual(3 * 4 * Vertex2D.SizeInBytes + 3 * 6 * sizeof(ushort), spent.BytesUploaded);
        Assert.AreEqual(0, spent.Presents);
    }

    // ---------------------------------------------------------------- resources: generations and deferred destruction

    [TestMethod]
    public void Handles_StopResolvingWhenTheirResourceIsDisposed_EvenWhenTheSlotIsReused()
    {
        if (Device is not GraphicsDeviceBase core) { Assert.Inconclusive("the device does not derive from GraphicsDeviceBase"); return; }
        var texture = Device.CreateTexture(new TextureDescription(1, 1));
        var handle = texture.Handle;
        Assert.IsFalse(handle.IsNone);
        Assert.IsTrue(core.TryGetResource<Texture>(handle, out var found) && ReferenceEquals(found, texture));
        Assert.IsFalse(core.TryGetResource<GpuBuffer>(handle, out _), "a handle resolves only to its own kind");
        int live = core.Counters.LiveResources;
        texture.Dispose();
        Assert.AreEqual(live - 1, core.Counters.LiveResources);
        Assert.IsFalse(core.TryGetResource<Texture>(handle, out _), "a disposed resource's handle no longer resolves");
        var next = Device.CreateTexture(new TextureDescription(1, 1));
        Assert.AreEqual(handle.Index, next.Handle.Index, "the slot is reused");
        Assert.AreNotEqual(handle.Generation, next.Handle.Generation, "with a new generation");
        Assert.IsFalse(core.TryGetResource<Texture>(handle, out _), "the old handle does not find the new resource");
        Assert.AreEqual(0, core.PendingReleases, "nothing waits: this device executes lists at Submit");
    }

    // ---------------------------------------------------------------- capabilities (typed errors)

    [TestMethod]
    public void UndeclaredCapabilities_FailWithTheTypedError()
    {
        var target = Target(4, 4);
        var checks = new (GraphicsFeatures Feature, Action Attempt)[]
        {
            (GraphicsFeatures.Mipmaps, () => Device.CreateTexture(new TextureDescription(4, 4, MipLevels: 2))),
            (GraphicsFeatures.SeparateFilters, () => Device.GetSampler(new SamplerDescription(Min: Filter.Linear, Mag: Filter.Nearest))),
            (GraphicsFeatures.AddressMirror, () => Device.GetSampler(new SamplerDescription(U: AddressMode.Mirror))),
            (GraphicsFeatures.AddressRepeat, () => Device.GetSampler(new SamplerDescription(V: AddressMode.Repeat))),
            (GraphicsFeatures.CustomBlend, () => Device.GetBuiltInPipeline(BuiltInPipeline.Textured, new BlendState(BlendFactor.SrcColor, BlendFactor.Zero, BlendOp.Max, BlendFactor.One, BlendFactor.Zero, BlendOp.Add), TextureFormat.Rgba8Unorm)),
            (GraphicsFeatures.DepthBuffer, () => Device.CreateRenderTarget(new RenderTargetDescription(4, 4, Depth: FirstDepthFormat() ?? TextureFormat.D32Float))),
            (GraphicsFeatures.Shaders, () => Device.CreateShader(new ShaderDescription(ShaderStage.Fragment, ShaderFormat.SpirV, new byte[] { 3, 2, 35, 7 }, "main", default))),
            (GraphicsFeatures.Instancing, () => InstancedDraw(target)),
        };
        foreach (var (feature, attempt) in checks)
        {
            if (Caps.Has(feature))
            {
                if (feature is not GraphicsFeatures.Shaders) attempt(); // declared: the same request succeeds
                continue;
            }
            var e = Assert.ThrowsExactly<GraphicsCapabilityException>(attempt, $"{feature} is undeclared");
            Assert.AreEqual(feature, e.Missing, e.Message);
            Assert.AreEqual(Caps.Backend, e.Device);
            Assert.IsInstanceOfType<NotSupportedException>(e, "the typed error is a NotSupportedException");
        }

        // Non-feature requirements: an unsupported format.
        var unsupported = Enum.GetValues<TextureFormat>().First(f => !TextureFormats.IsDepth(f) && !Caps.Supports(f, TextureUsage.Sampled));
        var formatError = Assert.ThrowsExactly<GraphicsCapabilityException>(() => Device.CreateTexture(new TextureDescription(4, 4, unsupported)));
        Assert.AreEqual(GraphicsFeatures.None, formatError.Missing);
        Assert.Contains(unsupported.ToString(), formatError.Requirement!);
    }

    [TestMethod]
    public void Depth_IsRefusedWithTheTypedErrorWhereUndeclared()
    {
        if (Caps.Has(GraphicsFeatures.DepthBuffer)) Assert.Inconclusive("depth-declaring devices run the depth tests with their own shaders (not in this suite yet)");
        var e = Assert.ThrowsExactly<GraphicsCapabilityException>(() => Device.CreateRenderTarget(new RenderTargetDescription(8, 8, Depth: TextureFormat.D32Float)));
        Assert.AreEqual(GraphicsFeatures.DepthBuffer, e.Missing);
        Assert.AreEqual(GraphicsFeatures.DepthBuffer, Assert.ThrowsExactly<GraphicsCapabilityException>(
            () => Device.GetBuiltInPipeline(BuiltInPipeline.Textured, BlendState.Opaque, TextureFormat.Rgba8Unorm, TextureFormat.D32Float)).Missing);
    }

    [TestMethod]
    public void Uniforms_HaveNothingToBindOnTheBuiltInPipeline()
    {
        var target = Target(4, 4);
        var list = Device.BeginCommands();
        list.BeginPass(new PassDescription(new ColorAttachment(target)));
        list.SetPipeline(Device.GetBuiltInPipeline(BuiltInPipeline.Textured, BlendState.AlphaPremultiplied, target.ColorFormat));
        Assert.ThrowsExactly<InvalidOperationException>(() => list.SetUniforms(ShaderStage.Vertex, 0, new byte[16]), "the built-in pipeline declares no uniform slot");
        list.EndPass();
        Device.Submit(list);
    }

    // ---------------------------------------------------------------- core validation (identical on every tier)

    [TestMethod]
    public void Recording_OutOfOrder_IsRejectedAtTheCall()
    {
        var target = Target(4, 4);
        var buffer = Device.CreateBuffer(new BufferDescription(64, BufferUsage.Vertex));
        var list = Device.BeginCommands();
        Assert.ThrowsExactly<InvalidOperationException>(() => list.Draw(3), "draw outside a pass");
        Assert.ThrowsExactly<InvalidOperationException>(() => list.SetVertices(buffer), "binding outside a pass");
        list.BeginPass(new PassDescription(new ColorAttachment(target)));
        Assert.ThrowsExactly<InvalidOperationException>(() => list.BeginPass(new PassDescription(new ColorAttachment(target))), "nested pass");
        Assert.ThrowsExactly<InvalidOperationException>(() => list.UpdateBuffer(buffer, 0, new byte[4]), "update inside a pass");
        Assert.ThrowsExactly<InvalidOperationException>(() => list.Draw(3), "draw without a pipeline");
        Assert.ThrowsExactly<InvalidOperationException>(() => Device.Submit(list), "submit with an open pass");
        list.EndPass();
        Device.Submit(list);
        Assert.IsTrue(list.IsSubmitted);
        Assert.ThrowsExactly<InvalidOperationException>(() => Device.Submit(list), "submitted twice");
        Assert.ThrowsExactly<InvalidOperationException>(() => list.BeginPass(new PassDescription(new ColorAttachment(target))), "recording after submit");
    }

    [TestMethod]
    public void Ranges_OutsideBuffersAndTargets_AreRejected()
    {
        var target = Target(8, 8);
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var vertices = Device.CreateBuffer(new BufferDescription(3 * Vertex2D.SizeInBytes, BufferUsage.Vertex));
        var indices = Device.CreateBuffer(new BufferDescription(6, BufferUsage.Index));
        var list = Device.BeginCommands();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.UpdateBuffer(vertices, 90, new byte[8]));
        list.BeginPass(new PassDescription(new ColorAttachment(target)));
        BindBuiltIn(list, target, white, vertices, indices);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.Draw(4), "4 vertices in a 3-vertex buffer");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.DrawIndexed(4), "4 indices in a 3-index buffer");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => list.SetViewport(new RectI(4, 4, 8, 8)), "viewport outside the target");
        list.EndPass();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Device.ReadPixels(target, new RectI(0, 0, 9, 8)));
    }

    [TestMethod]
    public void Resources_OfAnotherDeviceOrDisposed_AreRejected()
    {
        using var otherSurface = CreateSurface(16, 16);
        using var other = CreateDevice(otherSurface);
        var foreign = other.CreateTexture(new TextureDescription(1, 1));
        var target = Target(4, 4);
        var list = Device.BeginCommands();
        list.BeginPass(new PassDescription(new ColorAttachment(target)));
        Assert.ThrowsExactly<ArgumentException>(() => list.SetTexture(0, foreign, Device.GetSampler(SamplerDescription.PointClamp)), "texture of another device");
        Assert.ThrowsExactly<ArgumentException>(() => list.SetTexture(0, Device.CreateTexture(new TextureDescription(1, 1)), other.GetSampler(SamplerDescription.PointClamp)), "sampler of another device");
        list.EndPass();
        Assert.ThrowsExactly<ArgumentException>(() => other.Submit(list), "list of another device");

        var texture = Device.CreateTexture(new TextureDescription(1, 1));
        var recorded = Device.BeginCommands();
        recorded.BeginPass(new PassDescription(new ColorAttachment(target)));
        recorded.SetTexture(0, texture, Device.GetSampler(SamplerDescription.PointClamp));
        recorded.EndPass();
        texture.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => Device.Submit(recorded), "disposed after recording");
        var late = Device.BeginCommands();
        late.BeginPass(new PassDescription(new ColorAttachment(target)));
        Assert.ThrowsExactly<ObjectDisposedException>(() => late.SetTexture(0, texture, Device.GetSampler(SamplerDescription.PointClamp)), "disposed before recording");
        late.EndPass();
    }

    [TestMethod]
    public void Pipelines_MustMatchThePassFormats_AndBuiltInsAreDeviceOwned()
    {
        var target = Target(4, 4);
        var builtIn = Device.GetBuiltInPipeline(BuiltInPipeline.Textured, BlendState.AlphaPremultiplied, TextureFormat.Rgba8Unorm);
        Assert.AreSame(builtIn, Device.GetBuiltInPipeline(BuiltInPipeline.Textured, BlendState.AlphaPremultiplied, TextureFormat.Rgba8Unorm), "cached");
        Assert.AreEqual(BuiltInPipeline.Textured, builtIn.BuiltIn);
        Assert.AreSame(VertexLayout.Vertex2D, builtIn.Layout);
        Assert.ThrowsExactly<InvalidOperationException>(builtIn.Dispose, "built-in pipelines belong to the device");
        Assert.ThrowsExactly<InvalidOperationException>(target.Color!.Dispose, "a target's colour texture belongs to the target");
        var list = Device.BeginCommands();
        list.BeginPass(new PassDescription(new ColorAttachment(target)));
        Assert.IsTrue(Caps.Supports(TextureFormat.Bgra8Unorm, TextureUsage.RenderTarget), "the surface format (Bgra8Unorm) is renderable");
        Assert.ThrowsExactly<InvalidOperationException>(() => list.SetPipeline(Device.GetBuiltInPipeline(BuiltInPipeline.Textured, BlendState.AlphaPremultiplied, TextureFormat.Bgra8Unorm)), "colour format mismatch");
        Assert.ThrowsExactly<InvalidOperationException>(() => list.SetTexture(0, target.Color!, Device.GetSampler(SamplerDescription.PointClamp)), "sampling the pass's own target");
        list.EndPass();
    }

    [TestMethod]
    public void SurfaceTarget_IsAcquiredDrawnPresentedAndExpires()
    {
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out var surface), "a non-minimised surface has a surface target");
        Assert.IsTrue(surface.IsSurface);
        Assert.IsNull(surface.Color, "the surface cannot be sampled");
        Assert.AreEqual((64, 48), (surface.Width, surface.Height));
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out var again) && ReferenceEquals(surface, again), "one surface target per frame");
        Assert.ThrowsExactly<InvalidOperationException>(() => Device.ReadPixels(surface, surface.Bounds), "surface targets are not read back");
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        Painter.Paint(surface, new Quad(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(8), ColorF.White), clear: Opaque(10, 20, 30));
        Device.Present();
        Assert.IsTrue(surface.IsDisposed, "the surface target expires at Present");
        Assert.ThrowsExactly<ObjectDisposedException>(() => Painter.Paint(surface, [], clear: Opaque(0, 0, 0)), "an expired surface target cannot be drawn to");
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out var next) && !ReferenceEquals(surface, next), "the next frame acquires a new one");
        Device.Present();
    }

    [TestMethod]
    public void SurfaceTarget_IsNotAvailableWhileMinimised()
    {
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out _));
        Device.Present();
        if (!Surface.TrySetMinimized(true)) Assert.Inconclusive("this surface cannot be minimised here");
        try
        {
            Assert.IsTrue(Surface.IsMinimized, "the surface reports itself minimised");
            Assert.IsFalse(Device.TryAcquireSurfaceTarget(out var none));
            Assert.IsNull(none);
            Device.Present(); // no-op without a target
        }
        finally { Surface.TrySetMinimized(false); }
    }

    // ---------------------------------------------------------------- helpers

    protected static ColorF Opaque(byte r, byte g, byte b) => PremultipliedColor.FromBytes(r, g, b, 255);

    protected RenderTarget Target(int width, int height) => Device.CreateRenderTarget(new RenderTargetDescription(width, height));

    protected Image Read(RenderTarget target)
    {
        var request = Device.ReadPixels(target, target.Bounds);
        request.Wait();
        return new Image(target.Width, target.Height, request.Pixels.ToArray());
    }

    private RenderTarget PatternTarget(int width, int height)
    {
        var texels = Enumerable.Range(0, width * height).Select(i => (uint)(i * 0x01020304u) | 0xFF).ToArray();
        var texture = Device.CreateTexture(new TextureDescription(width, height), Texels.Bytes(texels));
        var target = Target(width, height);
        Painter.Paint(target, new Quad(texture, new RectF(0, 0, width, height), Matrix3x2.Identity, ColorF.White), clear: Opaque(0, 0, 0), blend: BlendState.Opaque);
        return target;
    }

    private Image Stretch(Texture pair, SamplerDescription sampler)
    {
        var target = Target(32, 4);
        Painter.Paint(target, new Quad(pair, new RectF(0, 0, 2, 1), Matrix3x2.CreateScale(16, 4), ColorF.White), clear: Opaque(0, 0, 255), blend: BlendState.Opaque, sampler: sampler);
        return Read(target);
    }

    private void DrawGeometry(RenderTarget target, Texture texture, Vertex2D[] vertices, ushort[] indices, ColorF clear)
    {
        using var vb = Device.CreateBuffer(new BufferDescription(vertices.Length * Vertex2D.SizeInBytes, BufferUsage.Vertex));
        using var ib = Device.CreateBuffer(new BufferDescription(indices.Length * sizeof(ushort), BufferUsage.Index));
        var list = Device.BeginCommands();
        list.UpdateBuffer<Vertex2D>(vb, 0, vertices);
        list.UpdateBuffer<ushort>(ib, 0, indices);
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Clear, Clear: clear)));
        BindBuiltIn(list, target, texture, vb, ib);
        list.DrawIndexed(indices.Length);
        list.EndPass();
        Device.Submit(list);
    }

    private void BindBuiltIn(CommandList list, RenderTarget target, Texture texture, GpuBuffer vertices, GpuBuffer indices)
    {
        list.SetPipeline(Device.GetBuiltInPipeline(BuiltInPipeline.Textured, BlendState.AlphaPremultiplied, target.ColorFormat));
        list.SetTexture(0, texture, Device.GetSampler(SamplerDescription.PointClamp));
        list.SetVertices(vertices);
        list.SetIndices(indices);
    }

    private void InstancedDraw(RenderTarget target)
    {
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var vertices = Device.CreateBuffer(new BufferDescription(4 * Vertex2D.SizeInBytes, BufferUsage.Vertex));
        var indices = Device.CreateBuffer(new BufferDescription(12, BufferUsage.Index));
        var list = Device.BeginCommands();
        list.UpdateBuffer<Vertex2D>(vertices, 0, QuadVertices(0, 0, 1, 1, ColorF.White));
        list.UpdateBuffer<ushort>(indices, 0, [0, 1, 2, 0, 2, 3]);
        list.BeginPass(new PassDescription(new ColorAttachment(target)));
        BindBuiltIn(list, target, white, vertices, indices);
        list.DrawIndexed(6, instances: 2);
        list.EndPass();
        Device.Submit(list);
    }

    private TextureFormat? FirstDepthFormat()
        => new[] { TextureFormat.D24UnormS8, TextureFormat.D32Float, TextureFormat.D16Unorm }.Cast<TextureFormat?>()
            .FirstOrDefault(f => Caps.Supports(f!.Value, TextureUsage.DepthStencil));

    private static Vertex2D[] QuadVertices(float x, float y, float w, float h, ColorF color) =>
    [
        new(new(x, y), new(0, 0), color), new(new(x + w, y), new(1, 0), color),
        new(new(x + w, y + h), new(1, 1), color), new(new(x, y + h), new(0, 1), color),
    ];

    /// <summary>Whether a point lies inside a convex polygon (either winding), and its distance to the nearest edge.</summary>
    private static (bool Inside, float Margin) InsideConvex(Vector2[] corners, Vector2 p)
    {
        int sign = 0;
        float margin = float.MaxValue;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 a = corners[i], b = corners[(i + 1) % corners.Length];
            var edge = b - a;
            float cross = edge.X * (p.Y - a.Y) - edge.Y * (p.X - a.X);
            margin = MathF.Min(margin, MathF.Abs(cross) / edge.Length());
            int s = MathF.Sign(cross);
            if (s == 0) continue;
            if (sign == 0) sign = s;
            else if (s != sign) return (false, margin);
        }
        return (true, margin);
    }

    private static float Blend(BlendState b, float[] s, float[] d, int channel)
    {
        var alpha = channel == 3;
        var (sf, df, op) = alpha ? (b.SrcAlpha, b.DstAlpha, b.AlphaOp) : (b.SrcColor, b.DstColor, b.ColorOp);
        float F(BlendFactor f) => f switch
        {
            BlendFactor.Zero => 0, BlendFactor.One => 1,
            BlendFactor.SrcColor => s[channel], BlendFactor.InvSrcColor => 1 - s[channel],
            BlendFactor.SrcAlpha => s[3], BlendFactor.InvSrcAlpha => 1 - s[3],
            BlendFactor.DstColor => d[channel], BlendFactor.InvDstColor => 1 - d[channel],
            BlendFactor.DstAlpha => d[3], _ => 1 - d[3],
        };
        var result = op switch
        {
            BlendOp.Add => s[channel] * F(sf) + d[channel] * F(df),
            BlendOp.Subtract => s[channel] * F(sf) - d[channel] * F(df),
            BlendOp.ReverseSubtract => d[channel] * F(df) - s[channel] * F(sf),
            BlendOp.Min => Math.Min(s[channel], d[channel]),
            _ => Math.Max(s[channel], d[channel]),
        };
        return Math.Clamp(result, 0, 1);
    }

    private static string Name(BlendState b) => ReferenceEquals(b, BlendState.Opaque) ? "Opaque" : ReferenceEquals(b, BlendState.AlphaPremultiplied) ? "AlphaPremultiplied" : ReferenceEquals(b, BlendState.Additive) ? "Additive" : "Multiply";
}
