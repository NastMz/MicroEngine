using System.Diagnostics;
using System.Numerics;
using Arch1.Graphics.Batching;
using Arch1.Graphics.Sdl;

namespace Arch1.Graphics.Conformance;

/// <summary>
/// The conformance suite of the written graphics rules. It runs unchanged against every backend: a concrete class names
/// the factory and what the device must declare. Each test names the property it checks; exact properties are exact,
/// and a tolerance appears only with its named source of variation (dossier §8).
/// </summary>
public abstract class DeviceConformance
{
    private const uint Clear = 0x000000FF; // opaque black, byte-exact

    private SdlWindowSurface? _surface;
    private IGraphicsDevice? _device;
    private Renderer2D? _batcher;

    /// <summary>The factory under test.</summary>
    protected abstract IGraphicsBackendFactory Factory { get; }

    /// <summary>The API to request (null = backend default).</summary>
    protected abstract string? Api { get; }

    /// <summary>What the device must report as its API after creation.</summary>
    protected abstract string ExpectedApi { get; }

    /// <summary>The tier the device must declare.</summary>
    protected abstract GraphicsTier ExpectedTier { get; }

    /// <summary>The exact optional features the device must declare.</summary>
    protected abstract GraphicsFeatures ExpectedFeatures { get; }

    /// <summary>Device options; ARCH1_LOW_POWER=1 prefers the integrated adapter (the AMD runs).</summary>
    protected GraphicsDeviceOptions Options => new(Api: Api, PreferLowPower: Environment.GetEnvironmentVariable("ARCH1_LOW_POWER") == "1");

    protected IGraphicsDevice Device => _device!;
    protected Renderer2D Batcher => _batcher ??= new Renderer2D(Device);
    protected GraphicsCapabilities Caps => Device.Capabilities;

    [TestInitialize]
    public void Open()
    {
        _surface = new SdlWindowSurface($"conformance {GetType().Name}", 64, 48, hidden: true);
        _device = Factory.Create(_surface, Options);
    }

    [TestCleanup]
    public void Close()
    {
        _batcher?.Dispose();
        _device?.Dispose();
        _surface?.Dispose();
    }

    // ---------------------------------------------------------------- capabilities

    [TestMethod]
    public void Capabilities_DeclareTheExpectedTierFeaturesAndApi()
    {
        Console.WriteLine($"{Caps.Backend} api={Caps.ApiName} adapter=\"{Caps.Adapter}\" features={Caps.Features}");
        Assert.AreEqual(Factory.Name, Caps.Backend);
        Assert.AreEqual(ExpectedTier, Caps.Tier);
        Assert.AreEqual(ExpectedFeatures, Caps.Features, $"declared {Caps.Features}");
        Assert.AreEqual(ExpectedApi, Caps.ApiName, "backend identity is what the device reports after creation");
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
        Batcher.Begin(new BatchSettings(target, Clear: ColorF.FromBytes(32, 64, 96, 255)));
        Batcher.End();
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
        Batcher.Begin(new BatchSettings(target, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.Draw(texture, new RectF(0, 0, 3, 2), Matrix3x2.CreateTranslation(5, 7), ColorF.White);
        Batcher.End();
        var image = Read(target);
        Assert.AreEqual(new RectI(5, 7, 3, 2), image.BoundsOfNot(Clear), "position and size");
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                Assert.AreEqual(texels[y * 3 + x], image[5 + x, 7 + y], $"texel ({x},{y})");
    }

    [TestMethod]
    public void Sprite_ScaledByAnInteger_MapsEachTexelToABlockExactly()
    {
        uint[] texels = [0xFF0000FF, 0x00FF00FF, 0x0000FFFF, 0xFFFFFFFF];
        var texture = Device.CreateTexture(new TextureDescription(2, 2), Texels.Bytes(texels));
        var target = Target(16, 16);
        Batcher.Begin(new BatchSettings(target, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.Draw(texture, new RectF(0, 0, 2, 2), Matrix3x2.CreateScale(4) * Matrix3x2.CreateTranslation(3, 2), ColorF.White);
        Batcher.End();
        var image = Read(target);
        Assert.AreEqual(new RectI(3, 2, 8, 8), image.BoundsOfNot(Clear));
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
        Batcher.Begin(new BatchSettings(target, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.Draw(texture, new RectF(0, 0, 3, 1), Matrix3x2.CreateScale(-1, 1) * Matrix3x2.CreateTranslation(10, 1), ColorF.White);
        Batcher.End();
        var image = Read(target);
        Assert.AreEqual(new RectI(7, 1, 3, 1), image.BoundsOfNot(Clear));
        for (var x = 0; x < 3; x++) Assert.AreEqual(texels[2 - x], image[7 + x, 1]);
    }

    [TestMethod]
    public void Geometry_UntexturedTriangles_CoverTheirPixelsWithTheVertexColour()
    {
        var target = Target(16, 16);
        var red = new ColorF(1, 0, 0, 1);
        Vertex2D[] quad = [new(new(2, 3), default, red), new(new(12, 3), default, red), new(new(12, 9), default, red), new(new(2, 9), default, red)];
        Batcher.Begin(new BatchSettings(target, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.DrawGeometry(null, quad, [0, 1, 2, 0, 2, 3]);
        Batcher.End();
        var image = Read(target);
        Assert.AreEqual(new RectI(2, 3, 10, 6), image.BoundsOfNot(Clear), "pixels whose centres lie inside the quad");
        Assert.AreEqual(0xFF0000FFu, image[2, 3]);
        Assert.AreEqual(0xFF0000FFu, image[11, 8]);
    }

    // ---------------------------------------------------------------- blending (±1/255)

    [TestMethod]
    public void Blend_EveryPreset_MatchesItsFormulaWithin8BitQuantization()
    {
        var dst = (R: 51, G: 102, B: 153, A: 204);
        var tint = ColorF.FromBytes(102, 51, 26, 128); // premultiplied source, exact byte values
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        foreach (var preset in BlendState.Presets)
        {
            var target = Target(4, 4);
            Batcher.Begin(new BatchSettings(target, Clear: ColorF.FromBytes((byte)dst.R, (byte)dst.G, (byte)dst.B, (byte)dst.A)));
            Batcher.End();
            Batcher.Begin(new BatchSettings(target, Blend: preset));
            Batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(4), tint);
            Batcher.End();
            var image = Read(target);
            float[] s = [tint.R, tint.G, tint.B, tint.A];
            float[] d = [dst.R / 255f, dst.G / 255f, dst.B / 255f, dst.A / 255f];
            for (var ch = 0; ch < 4; ch++)
            {
                var expected = Blend(preset, s, d, ch);
                var actual = image.Channel(1, 1, ch);
                // Named source of variation: 8-bit quantization of the blended value (an exact .5 may round either
                // way, as P3 observed between vendors and APIs): ±1/255.
                Assert.IsTrue(Math.Abs(actual - expected * 255f) <= 1f, $"{Name(preset)} channel {ch}: expected {expected * 255f:F2}, got {actual}");
            }
        }
    }

    // ---------------------------------------------------------------- sampling

    [TestMethod]
    public void Sampler_NearestAndLinear_AreDistinguishable()
    {
        var pair = Device.CreateTexture(new TextureDescription(2, 1), Texels.Bytes(0x000000FF, 0xFFFFFFFF));
        var nearest = Stretch(pair, SamplerDescription.PointClamp);
        var linear = Stretch(pair, SamplerDescription.LinearClamp);
        // Nearest: exact, a hard edge in the middle.
        for (var x = 0; x < 32; x++) Assert.AreEqual(x < 16 ? 0 : 255, nearest.Channel(x, 0, 0), $"nearest x={x}");
        // Linear: clamped ends exact (sample points beyond the outer texel centres), a monotonic ramp in between whose
        // values follow the bilinear kernel. Named sources: the APIs' 8-bit sub-texel weight precision plus output
        // quantization, at most ±2/255 together.
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
            Batcher.Begin(new BatchSettings(target, Sampler: description, Clear: ColorF.FromBytes(0, 0, 0)));
            Batcher.Draw(texture, new RectF(0, 0, 6, 4), Matrix3x2.Identity, ColorF.White);
            Batcher.End();
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

    // ---------------------------------------------------------------- scissor and viewport (exact)

    [TestMethod]
    public void Scissor_ClipsDrawsExactlyAndClearsIgnoreIt()
    {
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var target = Target(32, 24);
        var scissor = new RectI(3, 5, 10, 7);
        Batcher.Begin(new BatchSettings(target, Scissor: scissor, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(32, 24), ColorF.White);
        Batcher.End();
        var image = Read(target);
        Assert.AreEqual(scissor, image.BoundsOfNot(Clear));
        Assert.AreEqual(Clear, image[0, 0], "the clear covered the whole target despite the scissor");
        Assert.AreEqual(0xFFFFFFFFu, image[3, 5]);
        Assert.AreEqual(0xFFFFFFFFu, image[12, 11]);
    }

    [TestMethod]
    public void Viewport_OffsetsBuiltInPositionsAndClips_ScissorStaysAbsolute()
    {
        var target = Target(32, 32);
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var vertices = Device.CreateBuffer(new BufferDescription(4 * Vertex2D.SizeInBytes, BufferUsage.Vertex));
        var indices = Device.CreateBuffer(new BufferDescription(12, BufferUsage.Index));
        var list = Device.BeginCommands();
        list.UpdateBuffer<Vertex2D>(vertices, 0, Quad(0, 0, 20, 20, ColorF.White));
        list.UpdateBuffer<ushort>(indices, 0, [0, 1, 2, 0, 2, 3]);
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Clear, Clear: ColorF.FromBytes(0, 0, 0))));
        list.SetViewport(new RectI(8, 6, 10, 10));
        BindBuiltIn(list, target, white, vertices, indices);
        list.DrawIndexed(6);
        list.EndPass();
        Device.Submit(list);
        Assert.AreEqual(new RectI(8, 6, 10, 10), Read(target).BoundsOfNot(Clear), "positions relative to the viewport origin, clipped by it");

        var scissored = Device.BeginCommands();
        scissored.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Clear, Clear: ColorF.FromBytes(0, 0, 0))));
        scissored.SetViewport(new RectI(8, 6, 10, 10));
        scissored.SetScissor(new RectI(10, 10, 4, 4));
        BindBuiltIn(scissored, target, white, vertices, indices);
        scissored.DrawIndexed(6);
        scissored.EndPass();
        Device.Submit(scissored);
        Assert.AreEqual(new RectI(10, 10, 4, 4), Read(target).BoundsOfNot(Clear), "scissor in absolute target pixels");
    }

    // ---------------------------------------------------------------- targets, readback, ordering (exact)

    [TestMethod]
    public void RenderTarget_RoundTripIsExact()
    {
        uint[] texels = [0x11223344, 0xFF0000FF, 0x00FF00FF, 0x0000FFFF, 0x80808080, 0xFFFFFFFF];
        var texture = Device.CreateTexture(new TextureDescription(3, 2), Texels.Bytes(texels));
        var a = Target(8, 8);
        var b = Target(16, 16);
        Batcher.Begin(new BatchSettings(a, Blend: BlendState.Opaque, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.Draw(texture, new RectF(0, 0, 3, 2), Matrix3x2.CreateTranslation(1, 1), ColorF.White);
        Batcher.End();
        Batcher.Begin(new BatchSettings(b, Blend: BlendState.Opaque, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.Draw(a.Color!, new RectF(0, 0, 8, 8), Matrix3x2.CreateTranslation(4, 4), ColorF.White);
        Batcher.End();
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
    public void Updates_TakeEffectInRecordingOrder()
    {
        var target = Target(16, 8);
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var vertices = Device.CreateBuffer(new BufferDescription(4 * Vertex2D.SizeInBytes, BufferUsage.Vertex));
        var indices = Device.CreateBuffer(new BufferDescription(12, BufferUsage.Index));
        var list = Device.BeginCommands();
        list.UpdateBuffer<ushort>(indices, 0, [0, 1, 2, 0, 2, 3]);
        list.UpdateBuffer<Vertex2D>(vertices, 0, Quad(0, 0, 4, 4, new ColorF(1, 0, 0, 1)));
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Clear, Clear: ColorF.FromBytes(0, 0, 0))));
        BindBuiltIn(list, target, white, vertices, indices);
        list.DrawIndexed(6);
        list.EndPass();
        list.UpdateBuffer<Vertex2D>(vertices, 0, Quad(8, 2, 4, 4, new ColorF(0, 1, 0, 1)));
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Load)));
        BindBuiltIn(list, target, white, vertices, indices);
        list.DrawIndexed(6);
        list.EndPass();
        Device.Submit(list);
        var image = Read(target);
        Assert.AreEqual(0xFF0000FFu, image[1, 1], "the first draw saw the first update");
        Assert.AreEqual(0x00FF00FFu, image[9, 3], "the second draw saw the second update");
        Assert.AreEqual(Clear, image[1, 6]);
        Assert.AreEqual(new RectI(0, 0, 12, 6), image.BoundsOfNot(Clear), "LoadOp.Load kept the first pass");
    }

    [TestMethod]
    public void UpdateTexture_ReplacesExactlyTheRegion()
    {
        var texture = Device.CreateTexture(new TextureDescription(4, 4), Texels.Bytes(Enumerable.Repeat(0xFF0000FFu, 16).ToArray()));
        var target = Target(4, 4);
        var list = Device.BeginCommands();
        list.UpdateTexture(texture, new RectI(1, 2, 2, 1), Texels.Bytes(0x00FF00FF, 0x0000FFFF));
        Device.Submit(list);
        Batcher.Begin(new BatchSettings(target, Blend: BlendState.Opaque));
        Batcher.Draw(texture, new RectF(0, 0, 4, 4), Matrix3x2.Identity, ColorF.White);
        Batcher.End();
        var image = Read(target);
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                var expected = (x, y) switch { (1, 2) => 0x00FF00FFu, (2, 2) => 0x0000FFFFu, _ => 0xFF0000FFu };
                Assert.AreEqual(expected, image[x, y], $"({x},{y})");
            }
    }

    [TestMethod]
    public void Batcher_IssuesOneDrawPerRunOfTheSameTexture()
    {
        var a = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFF0000FF));
        var b = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0x00FF00FF));
        var target = Target(8, 8);
        Batcher.Begin(new BatchSettings(target, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.Draw(a, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(0, 0), ColorF.White);
        Batcher.Draw(a, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(1, 0), ColorF.White);
        Batcher.Draw(b, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(2, 0), ColorF.White);
        Batcher.Draw(a, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(3, 0), ColorF.White);
        Batcher.End();
        Assert.AreEqual(3, Batcher.LastDrawCalls);
        var image = Read(target);
        Assert.AreEqual(0x00FF00FFu, image[2, 0], "submission order kept");
        Assert.AreEqual(0xFF0000FFu, image[3, 0]);
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
            (GraphicsFeatures.CustomBlend, () => Device.GetBuiltInPipeline(BuiltInPipeline.Sprite, new BlendState(BlendFactor.SrcColor, BlendFactor.Zero, BlendOp.Max, BlendFactor.One, BlendFactor.Zero, BlendOp.Add), TextureFormat.Rgba8Unorm)),
            (GraphicsFeatures.DepthBuffer, () => Device.CreateRenderTarget(new RenderTargetDescription(4, 4, Depth: FirstDepthFormat() ?? TextureFormat.D32Float))),
            (GraphicsFeatures.Shaders, () => Device.CreateShader(ShaderFiles.Load("color.frag", ShaderFiles.For(Caps)))),
            (GraphicsFeatures.Instancing, () => InstancedDraw(target)),
        };
        foreach (var (feature, attempt) in checks)
        {
            if (Caps.Has(feature)) { attempt(); continue; } // declared: the same request succeeds
            var e = Assert.ThrowsExactly<GraphicsCapabilityException>(attempt, $"{feature} is undeclared");
            Assert.AreEqual(feature, e.Missing, e.Message);
            Assert.AreEqual(Caps.Backend, e.Device);
        }

        // Non-feature requirements: an unsupported format, an undeclared shader format.
        var unsupported = Enum.GetValues<TextureFormat>().First(f => !TextureFormats.IsDepth(f) && !Caps.Supports(f, TextureUsage.Sampled));
        var formatError = Assert.ThrowsExactly<GraphicsCapabilityException>(() => Device.CreateTexture(new TextureDescription(4, 4, unsupported)));
        Assert.AreEqual(GraphicsFeatures.None, formatError.Missing);
        Assert.Contains(unsupported.ToString(), formatError.Requirement!);
        if (Caps.Has(GraphicsFeatures.Shaders))
        {
            var undeclared = new[] { ShaderFormat.SpirV, ShaderFormat.Dxil, ShaderFormat.Msl }.First(f => (Caps.ShaderFormats & f) == 0);
            var shaderError = Assert.ThrowsExactly<GraphicsCapabilityException>(() => Device.CreateShader(new ShaderDescription(ShaderStage.Fragment, undeclared, new byte[] { 1, 2, 3, 4 }, "main", default)));
            Assert.Contains(undeclared.ToString(), shaderError.Requirement!);
        }
    }

    // ---------------------------------------------------------------- depth (tier 1)

    [TestMethod]
    public void Depth_TestKeepsTheNearerSurfaceRegardlessOfDrawOrder()
    {
        if (!Caps.Has(GraphicsFeatures.DepthBuffer))
        {
            Assert.ThrowsExactly<GraphicsCapabilityException>(() => Device.CreateRenderTarget(new RenderTargetDescription(8, 8, Depth: TextureFormat.D32Float)));
            return;
        }
        var depthFormat = FirstDepthFormat()!.Value;
        var format = ShaderFiles.For(Caps);
        using var vertex = Device.CreateShader(ShaderFiles.Load("mesh.vert", format));
        using var fragment = Device.CreateShader(ShaderFiles.Load("color.frag", format));
        using var pipeline = Device.CreatePipeline(new PipelineDescription(vertex, fragment, MeshVertex.Layout, BlendState.Opaque,
            DepthState.LessWrite, new RasterState(CullMode.Back, FrontFace.CounterClockwise), TextureFormat.Rgba8Unorm, depthFormat));
        var target = Device.CreateRenderTarget(new RenderTargetDescription(16, 16, Depth: depthFormat));
        // Clip-space quads (identity matrix), counter-clockwise: near (z 0.25, red, centre) drawn BEFORE far (z 0.75, green, everywhere).
        MeshVertex[] mesh = [.. ClipQuad(-0.5f, -0.5f, 0.5f, 0.5f, 0.25f, new Vector4(1, 0, 0, 1)), .. ClipQuad(-1, -1, 1, 1, 0.75f, new Vector4(0, 1, 0, 1))];
        using var buffer = Device.CreateBuffer(new BufferDescription(mesh.Length * 28, BufferUsage.Vertex));
        var list = Device.BeginCommands();
        list.UpdateBuffer<MeshVertex>(buffer, 0, mesh);
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Clear, Clear: ColorF.FromBytes(0, 0, 0)), new DepthAttachment(target, LoadOp.Clear, StoreOp.DontCare, 1)));
        list.SetPipeline(pipeline);
        list.SetVertices(buffer);
        list.SetUniforms(ShaderStage.Vertex, 0, Matrix4x4.Identity);
        list.Draw(12);
        list.EndPass();
        Device.Submit(list);
        var image = Read(target);
        Assert.AreEqual(0xFF0000FFu, image[8, 8], "the near quad survives although drawn first");
        Assert.AreEqual(0x00FF00FFu, image[1, 1], "the far quad fills the rest");
        Assert.AreEqual(new RectI(4, 4, 8, 8), Bounds(image, 0xFF0000FFu), "near quad covers clip -0.5..0.5 exactly");
    }

    [TestMethod]
    public void Uniforms_PersistForTheRestOfTheList_AcrossBuiltInDraws()
    {
        if (!Caps.Has(GraphicsFeatures.Shaders))
        {
            Assert.ThrowsExactly<GraphicsCapabilityException>(() => Device.CreateShader(ShaderFiles.Load("mesh.vert", ShaderFormat.SpirV)));
            return;
        }
        var format = ShaderFiles.For(Caps);
        using var vertex = Device.CreateShader(ShaderFiles.Load("mesh.vert", format));
        using var fragment = Device.CreateShader(ShaderFiles.Load("color.frag", format));
        using var custom = Device.CreatePipeline(new PipelineDescription(vertex, fragment, MeshVertex.Layout, BlendState.Opaque,
            DepthState.None, new RasterState(), TextureFormat.Rgba8Unorm, null));
        var target = Target(16, 16);
        MeshVertex[] mesh = [.. ClipQuad(-1, 0, 0, 1, 0.5f, new Vector4(1, 0, 0, 1)), .. ClipQuad(0, -1, 1, 0, 0.5f, new Vector4(0, 1, 0, 1))];
        using var meshBuffer = Device.CreateBuffer(new BufferDescription(mesh.Length * 28, BufferUsage.Vertex));
        var white = Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
        var vertices = Device.CreateBuffer(new BufferDescription(4 * Vertex2D.SizeInBytes, BufferUsage.Vertex));
        var indices = Device.CreateBuffer(new BufferDescription(12, BufferUsage.Index));
        var list = Device.BeginCommands();
        list.UpdateBuffer<MeshVertex>(meshBuffer, 0, mesh);
        list.UpdateBuffer<Vertex2D>(vertices, 0, Quad(12, 0, 4, 4, new ColorF(0, 0, 1, 1)));
        list.UpdateBuffer<ushort>(indices, 0, [0, 1, 2, 0, 2, 3]);
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Clear, Clear: ColorF.FromBytes(0, 0, 0))));
        list.SetPipeline(custom);
        list.SetVertices(meshBuffer);
        list.SetUniforms(ShaderStage.Vertex, 0, Matrix4x4.Identity);
        list.Draw(6, 0);                     // top-left quadrant, red
        BindBuiltIn(list, target, white, vertices, indices);
        list.DrawIndexed(6);                 // a built-in draw in between (blue, top-right corner)
        list.SetPipeline(custom);
        list.SetVertices(meshBuffer);
        list.Draw(6, 6);                     // bottom-right quadrant, green: still the identity set above
        list.EndPass();
        Device.Submit(list);
        var image = Read(target);
        Assert.AreEqual(new RectI(0, 0, 8, 8), Bounds(image, 0xFF0000FFu));
        Assert.AreEqual(new RectI(12, 0, 4, 4), Bounds(image, 0x0000FFFFu));
        Assert.AreEqual(new RectI(8, 8, 8, 8), Bounds(image, 0x00FF00FFu), "the caller's uniform data survived the built-in draw");
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
        using var otherSurface = new SdlWindowSurface("other", 16, 16, hidden: true);
        using var other = Factory.Create(otherSurface, Options);
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
        var other = Caps.Supports(TextureFormat.Bgra8Unorm, TextureUsage.RenderTarget) ? TextureFormat.Bgra8Unorm : (TextureFormat?)null;
        var builtIn = Device.GetBuiltInPipeline(BuiltInPipeline.Sprite, BlendState.AlphaPremultiplied, TextureFormat.Rgba8Unorm);
        Assert.AreSame(builtIn, Device.GetBuiltInPipeline(BuiltInPipeline.Sprite, BlendState.AlphaPremultiplied, TextureFormat.Rgba8Unorm), "cached");
        Assert.ThrowsExactly<InvalidOperationException>(builtIn.Dispose, "built-in pipelines belong to the device");
        Assert.ThrowsExactly<InvalidOperationException>(target.Color!.Dispose, "a target's colour texture belongs to the target");
        var list = Device.BeginCommands();
        list.BeginPass(new PassDescription(new ColorAttachment(target)));
        if (other is { } f)
            Assert.ThrowsExactly<InvalidOperationException>(() => list.SetPipeline(Device.GetBuiltInPipeline(BuiltInPipeline.Sprite, BlendState.AlphaPremultiplied, f)), "colour format mismatch");
        Assert.ThrowsExactly<InvalidOperationException>(() => list.SetTexture(0, target.Color!, Device.GetSampler(SamplerDescription.PointClamp)), "sampling the pass's own target");
        list.EndPass();
        Assert.IsNotNull(other, "the surface format (Bgra8Unorm) is renderable on every backend in the spike");
    }

    [TestMethod]
    public void SurfaceTarget_IsAcquiredDrawnPresentedAndExpires()
    {
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out var surface), "a shown or hidden, non-minimised window has a surface target");
        Assert.IsTrue(surface.IsSurface);
        Assert.IsNull(surface.Color, "the surface cannot be sampled");
        Assert.AreEqual((64, 48), (surface.Width, surface.Height));
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out var again) && ReferenceEquals(surface, again), "one surface target per frame");
        Assert.ThrowsExactly<InvalidOperationException>(() => Device.ReadPixels(surface, surface.Bounds), "surface targets are not read back");
        Batcher.Begin(new BatchSettings(surface, Clear: ColorF.FromBytes(10, 20, 30)));
        Batcher.Draw(Device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF)), new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(8), ColorF.White);
        Batcher.End();
        Device.Present();
        Assert.IsTrue(surface.IsDisposed, "the surface target expires at Present");
        Batcher.Begin(new BatchSettings(surface));
        Assert.ThrowsExactly<ObjectDisposedException>(Batcher.End, "an expired surface target cannot be drawn to");
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out var next) && !ReferenceEquals(surface, next), "the next frame acquires a new one");
        Device.Present();
    }

    [TestMethod]
    [TestCategory("Visible")]
    public void SurfaceTarget_IsNotAvailableWhileMinimised()
    {
        using var surface = new SdlWindowSurface("minimised", 64, 48, hidden: false);
        using var device = Factory.Create(surface, Options);
        surface.Show();
        Assert.IsTrue(device.TryAcquireSurfaceTarget(out _));
        device.Present();
        surface.SetMinimized(true);
        Assert.IsTrue(surface.IsMinimized, "the window manager minimised the window");
        Assert.IsFalse(device.TryAcquireSurfaceTarget(out var none));
        Assert.IsNull(none);
        device.Present(); // no-op without a target
        surface.SetMinimized(false);
    }

    // ---------------------------------------------------------------- helpers

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
        Batcher.Begin(new BatchSettings(target, Blend: BlendState.Opaque, Clear: ColorF.FromBytes(0, 0, 0)));
        Batcher.Draw(texture, new RectF(0, 0, width, height), Matrix3x2.Identity, ColorF.White);
        Batcher.End();
        return target;
    }

    private Image Stretch(Texture pair, SamplerDescription sampler)
    {
        var target = Target(32, 4);
        Batcher.Begin(new BatchSettings(target, Sampler: sampler, Blend: BlendState.Opaque, Clear: ColorF.FromBytes(0, 0, 255)));
        Batcher.Draw(pair, new RectF(0, 0, 2, 1), Matrix3x2.CreateScale(16, 4), ColorF.White);
        Batcher.End();
        return Read(target);
    }

    private void BindBuiltIn(CommandList list, RenderTarget target, Texture texture, GpuBuffer vertices, GpuBuffer indices)
    {
        list.SetPipeline(Device.GetBuiltInPipeline(BuiltInPipeline.TexturedGeometry, BlendState.AlphaPremultiplied, target.ColorFormat));
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
        list.UpdateBuffer<Vertex2D>(vertices, 0, Quad(0, 0, 1, 1, ColorF.White));
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

    private static Vertex2D[] Quad(float x, float y, float w, float h, ColorF color) =>
    [
        new(new(x, y), new(0, 0), color), new(new(x + w, y), new(1, 0), color),
        new(new(x + w, y + h), new(1, 1), color), new(new(x, y + h), new(0, 1), color),
    ];

    private static MeshVertex[] ClipQuad(float x0, float y0, float x1, float y1, float z, Vector4 color) =>
    [
        new(new(x0, y0, z), color), new(new(x1, y0, z), color), new(new(x1, y1, z), color),
        new(new(x0, y0, z), color), new(new(x1, y1, z), color), new(new(x0, y1, z), color),
    ];

    private static RectI Bounds(Image image, uint color)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
                if (image[x, y] == color) { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
        return maxX < 0 ? default : new RectI(minX, minY, maxX - minX + 1, maxY - minY + 1);
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
