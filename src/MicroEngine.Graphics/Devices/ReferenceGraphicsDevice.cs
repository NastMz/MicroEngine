using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine.Platform;

namespace MicroEngine.Graphics.Devices;

/// <summary>Factory of the CPU reference device ("reference"): offscreen, or over any surface (it never touches the window).</summary>
public sealed class ReferenceGraphicsBackendFactory : IGraphicsBackendFactory
{
    /// <inheritdoc />
    public string Name => ReferenceGraphicsDevice.BackendName;

    /// <summary>Always null: the reference device works with any surface and without one.</summary>
    public string? UnsupportedReason(ISurface? surface) => null;

    /// <inheritdoc />
    public IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options) => new ReferenceGraphicsDevice(surface, options);
}

/// <summary>
/// The CPU reference device: a tier-0 implementation of the graphics device contract that executes every written rule
/// literally, in managed code, the same on every machine. It is the oracle the device conformance suite holds backends
/// to and the device headless runs draw with. It favours clarity over speed.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Command lists execute completely inside <see cref="IGraphicsDevice.Submit"/>; readbacks complete at once.</item>
/// <item>Targets store 8-bit channels; every blended value is computed in floating point from the stored 8-bit values,
/// clamped to 0..1 and rounded to the nearest 8-bit value, as GPUs write UNORM targets.</item>
/// <item>Coverage follows the top-left rule at pixel centres, with vertex positions snapped to 1/256 pixel (the
/// sub-pixel precision of Direct3D and Vulkan rasterizers).</item>
/// <item>Texture coordinates and colours are interpolated linearly (2D: no perspective) at pixel centres. Nearest
/// sampling takes the texel containing the sample point; linear sampling mixes the four nearest texel centres with the
/// sampler's address mode. Minification (more than one texel per pixel along either axis) uses the sampler's
/// minification filter, magnification its magnification filter.</item>
/// <item>The surface target, when the device has a non-minimised surface, is a <see cref="TextureFormat.Bgra8Unorm"/>
/// target of the surface's pixel size; <see cref="IGraphicsDevice.Present"/> keeps its pixels as
/// <see cref="PresentedFrame"/>.</item>
/// </list>
/// Declared features: render targets, repeat and mirror addressing, custom blend states and separate filters; no
/// mipmaps, shaders, depth or instancing.
/// </remarks>
public sealed class ReferenceGraphicsDevice : GraphicsDeviceBase
{
    /// <summary>The backend name.</summary>
    public const string BackendName = "reference";

    /// <summary>The features the reference device declares.</summary>
    public const GraphicsFeatures DeclaredFeatures = GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat
        | GraphicsFeatures.AddressMirror | GraphicsFeatures.CustomBlend | GraphicsFeatures.SeparateFilters;

    private const float SubPixel = 256f;
    private readonly ISurface? _surface;

    /// <summary>Creates a reference device, with a surface or offscreen only (<paramref name="surface"/> null).</summary>
    /// <exception cref="GraphicsCapabilityException"><paramref name="options"/> asks for a present mode it lacks (Mailbox).</exception>
    public ReferenceGraphicsDevice(ISurface? surface = null, GraphicsDeviceOptions? options = null)
    {
        options ??= new GraphicsDeviceOptions();
        _surface = surface;
        var presentModes = new HashSet<PresentMode> { PresentMode.VSync, PresentMode.Immediate };
        if (!presentModes.Contains(options.PresentMode))
            throw new GraphicsCapabilityException("Create", BackendName, GraphicsFeatures.None, $"PresentMode.{options.PresentMode}");
        var both = TextureUsage.Sampled | TextureUsage.RenderTarget;
        Capabilities = new GraphicsCapabilities(BackendName, GraphicsTier.Tier0, DeclaredFeatures, ShaderFormat.None,
            new Dictionary<TextureFormat, TextureUsage> { [TextureFormat.Rgba8Unorm] = both, [TextureFormat.Bgra8Unorm] = both },
            new HashSet<BlendState>(BlendState.Presets), presentModes,
            new GraphicsLimits(MaxTextureSize: 16384, MaxRenderTargetSize: 16384, MaxTextureSlots: 1, MaxUniformBytes: 0, MaxVertexAttributes: 3),
            ApiName: "cpu", Adapter: null);
        PresentMode = options.PresentMode;
    }

    /// <inheritdoc />
    public override GraphicsCapabilities Capabilities { get; }

    /// <summary>The present mode chosen at creation (presenting never waits: there is no display).</summary>
    public PresentMode PresentMode { get; }

    /// <summary>
    /// The pixels of the last presented surface target: width, height and tightly packed RGBA bytes (opaque or not, as
    /// drawn); null before the first present. Not part of the device contract (surfaces are not read back): headless
    /// hosts use it to capture what a window would have shown.
    /// </summary>
    public (int Width, int Height, byte[] Rgba)? PresentedFrame { get; private set; }

    /// <inheritdoc />
    protected override Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData)
    {
        var texture = new ReferenceTexture(this, description, owner: null);
        if (!initialData.IsEmpty) texture.Write(new RectI(0, 0, description.Width, description.Height), initialData);
        return texture;
    }

    /// <inheritdoc />
    protected override RenderTarget CreateRenderTargetCore(in RenderTargetDescription description) => new ReferenceTarget(this, description, isSurface: false);

    /// <inheritdoc />
    protected override Sampler CreateSamplerCore(in SamplerDescription description) => new ReferenceSampler(this, description);

    /// <inheritdoc />
    protected override GpuBuffer CreateBufferCore(in BufferDescription description) => new ReferenceBuffer(this, description);

    /// <inheritdoc />
    protected override Shader CreateShaderCore(in ShaderDescription description)
        => throw new GraphicsCapabilityException("CreateShader", BackendName, GraphicsFeatures.Shaders); // unreachable: core refuses first

    /// <inheritdoc />
    protected override Pipeline CreatePipelineCore(PipelineDescription description)
        => throw new GraphicsCapabilityException("CreatePipeline", BackendName, GraphicsFeatures.Shaders); // unreachable: core refuses first

    /// <inheritdoc />
    protected override Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat)
        => new ReferencePipeline(this, kind, blend, colorFormat);

    /// <inheritdoc />
    protected override void SubmitCore(CommandList commands)
    {
        var state = new PassState();
        foreach (ref readonly var c in commands.Commands)
        {
            switch (c.Kind)
            {
                case CommandKind.BeginPass:
                    var target = (ReferenceTarget)c.Pass.Color.Target;
                    state = new PassState { Target = target, Viewport = target.Bounds, Scissor = target.Bounds };
                    if (c.Pass.Color.Load == LoadOp.Clear) target.Pixels.Fill(c.Pass.Color.Clear); // ignores viewport and scissor
                    break;
                case CommandKind.EndPass:
                    state = new PassState();
                    break;
                case CommandKind.SetViewport:
                    state.Viewport = c.Rect;
                    break;
                case CommandKind.SetScissor:
                    state.Scissor = c.Rect;
                    break;
                case CommandKind.SetPipeline:
                    state.Pipeline = (ReferencePipeline)c.Resource!;
                    break;
                case CommandKind.SetTexture:
                    state.Texture = (ReferenceTexture)c.Resource!;
                    state.Sampler = c.Sampler!.Description;
                    break;
                case CommandKind.SetVertices:
                    state.Vertices = (ReferenceBuffer)c.Resource!;
                    break;
                case CommandKind.SetIndices:
                    state.Indices = (ReferenceBuffer)c.Resource!;
                    state.IndexSize = c.IndexSize;
                    break;
                case CommandKind.Draw:
                    for (int i = 0; i + 2 < c.Count; i += 3)
                        Triangle(ref state, c.First + i, c.First + i + 1, c.First + i + 2);
                    break;
                case CommandKind.DrawIndexed:
                    for (int i = 0; i + 2 < c.Count; i += 3)
                        Triangle(ref state, Index(state, c.First + i) + c.BaseVertex, Index(state, c.First + i + 1) + c.BaseVertex, Index(state, c.First + i + 2) + c.BaseVertex);
                    break;
                case CommandKind.UpdateBuffer:
                    commands.Payload(c).CopyTo(((ReferenceBuffer)c.Resource!).Memory.AsSpan(c.First));
                    break;
                case CommandKind.UpdateTexture:
                    ((ReferenceTexture)c.Resource!).Write(c.Rect, commands.Payload(c));
                    break;
                case CommandKind.SetUniforms:
                    throw new InvalidOperationException("the reference device has no uniforms"); // unreachable: built-ins declare none
                default:
                    throw new NotSupportedException(c.Kind.ToString());
            }
        }
    }

    /// <inheritdoc />
    protected override ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region)
        => new CompletedReadback(region, target.ColorFormat, ((ReferenceTarget)target).Pixels.Read(region, target.ColorFormat));

    /// <inheritdoc />
    protected override RenderTarget? TryAcquireSurfaceTargetCore()
    {
        if (_surface is null || _surface.IsMinimized) return null;
        var (width, height) = _surface.PixelSize;
        if (width <= 0 || height <= 0) return null;
        return new ReferenceTarget(this, new RenderTargetDescription(width, height, TextureFormat.Bgra8Unorm, Name: "surface"), isSurface: true);
    }

    /// <inheritdoc />
    protected override void PresentCore(RenderTarget surfaceTarget)
    {
        var target = (ReferenceTarget)surfaceTarget;
        PresentedFrame = (target.Width, target.Height, target.Pixels.Read(target.Bounds, TextureFormat.Rgba8Unorm));
        target.ExpireSurface();
    }

    /// <inheritdoc />
    protected override void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers) { }

    private static int Index(in PassState state, int position)
    {
        var bytes = state.Indices!.Memory.AsSpan();
        return state.IndexSize == IndexSize.Bits16
            ? MemoryMarshal.Read<ushort>(bytes[(position * 2)..])
            : (int)MemoryMarshal.Read<uint>(bytes[(position * 4)..]);
    }

    private static Vertex2D ReadVertex(in PassState state, int index)
    {
        var memory = state.Vertices!.Memory;
        long offset = (long)index * Vertex2D.SizeInBytes;
        if (index < 0 || offset + Vertex2D.SizeInBytes > memory.Length)
            throw new InvalidOperationException($"vertex {index} lies outside the bound vertex buffer ({memory.Length} bytes); index values are not validated when recorded");
        return MemoryMarshal.Read<Vertex2D>(memory.AsSpan((int)offset));
    }

    /// <summary>Rasterizes one triangle of the built-in pipeline with the pass state.</summary>
    private static void Triangle(ref PassState state, int i0, int i1, int i2)
    {
        var a = ReadVertex(state, i0);
        var b = ReadVertex(state, i1);
        var c = ReadVertex(state, i2);
        var clip = state.Viewport.Intersect(state.Scissor);
        if (clip.IsEmpty) return;
        // Positions: relative to the viewport origin, snapped to the rasterizer's sub-pixel grid.
        Vector2 origin = new(state.Viewport.X, state.Viewport.Y);
        double ax = Snap(a.Position.X + origin.X), ay = Snap(a.Position.Y + origin.Y);
        double bx = Snap(b.Position.X + origin.X), by = Snap(b.Position.Y + origin.Y);
        double cx = Snap(c.Position.X + origin.X), cy = Snap(c.Position.Y + origin.Y);
        double area = Edge(ax, ay, bx, by, cx, cy);
        if (area == 0) return;
        if (area < 0)
        {
            (bx, by, cx, cy) = (cx, cy, bx, by);
            (b, c) = (c, b);
            area = -area;
        }
        int x0 = Math.Max(clip.X, (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx)))), x1 = Math.Min(clip.Right, (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx))));
        int y0 = Math.Max(clip.Y, (int)Math.Floor(Math.Min(ay, Math.Min(by, cy)))), y1 = Math.Min(clip.Bottom, (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy))));
        if (x0 >= x1 || y0 >= y1) return;

        var texture = state.Texture!;
        var sampler = state.Sampler;
        var blend = state.Pipeline!.Blend;
        var pixels = state.Target!.Pixels;
        // Affine interpolation: texture coordinates as functions of the pixel position, constant derivatives per triangle.
        bool topLeftBC = TopLeft(bx, by, cx, cy), topLeftCA = TopLeft(cx, cy, ax, ay), topLeftAB = TopLeft(ax, ay, bx, by);
        var filter = Minifies(a, b, c, ax, ay, bx, by, cx, cy, area, texture) ? sampler.Min : sampler.Mag;
        for (int y = y0; y < y1; y++)
        {
            double py = y + 0.5;
            for (int x = x0; x < x1; x++)
            {
                double px = x + 0.5;
                double w0 = Edge(bx, by, cx, cy, px, py), w1 = Edge(cx, cy, ax, ay, px, py), w2 = Edge(ax, ay, bx, by, px, py);
                if (!Inside(w0, topLeftBC) || !Inside(w1, topLeftCA) || !Inside(w2, topLeftAB)) continue;
                float l0 = (float)(w0 / area), l1 = (float)(w1 / area), l2 = 1f - l0 - l1;
                var uv = a.TexCoord * l0 + b.TexCoord * l1 + c.TexCoord * l2;
                var color = new Vector4(
                    a.Color.R * l0 + b.Color.R * l1 + c.Color.R * l2,
                    a.Color.G * l0 + b.Color.G * l1 + c.Color.G * l2,
                    a.Color.B * l0 + b.Color.B * l1 + c.Color.B * l2,
                    a.Color.A * l0 + b.Color.A * l1 + c.Color.A * l2);
                var source = texture.Sample(uv, filter, sampler.U, sampler.V) * color;
                pixels.Blend(x, y, source, blend);
            }
        }
    }

    private static double Snap(float value) => Math.Round(value * (double)SubPixel) / SubPixel;

    /// <summary>Twice the signed area of (a, b, p): positive when p lies to the right of a→b on a y-down screen.</summary>
    private static double Edge(double ax, double ay, double bx, double by, double px, double py) => (bx - ax) * (py - ay) - (by - ay) * (px - ax);

    /// <summary>For the orientation with positive area: a top edge is horizontal going right, a left edge goes up.</summary>
    private static bool TopLeft(double ax, double ay, double bx, double by) => (ay == by && bx > ax) || by < ay;

    private static bool Inside(double w, bool topLeft) => w > 0 || (w == 0 && topLeft);

    /// <summary>Whether the triangle maps more than one texel to a pixel along either screen axis.</summary>
    private static bool Minifies(in Vertex2D a, in Vertex2D b, in Vertex2D c, double ax, double ay, double bx, double by, double cx, double cy, double area, ReferenceTexture texture)
    {
        // Gradients of u and v (in texels) with respect to screen x and y, from the plane through the three vertices.
        double du1 = (b.TexCoord.X - a.TexCoord.X) * texture.Width, du2 = (c.TexCoord.X - a.TexCoord.X) * texture.Width;
        double dv1 = (b.TexCoord.Y - a.TexCoord.Y) * texture.Height, dv2 = (c.TexCoord.Y - a.TexCoord.Y) * texture.Height;
        double ex1 = bx - ax, ey1 = by - ay, ex2 = cx - ax, ey2 = cy - ay;
        double det = ex1 * ey2 - ex2 * ey1; // = area
        if (det == 0) return false;
        double dudx = (du1 * ey2 - du2 * ey1) / det, dudy = (du2 * ex1 - du1 * ex2) / det;
        double dvdx = (dv1 * ey2 - dv2 * ey1) / det, dvdy = (dv2 * ex1 - dv1 * ex2) / det;
        double rho2 = Math.Max(dudx * dudx + dvdx * dvdx, dudy * dudy + dvdy * dvdy);
        _ = area;
        return rho2 > 1.0 + 1e-9;
    }

    private struct PassState
    {
        public ReferenceTarget? Target;
        public RectI Viewport, Scissor;
        public ReferencePipeline? Pipeline;
        public ReferenceTexture? Texture;
        public SamplerDescription Sampler;
        public ReferenceBuffer? Vertices, Indices;
        public IndexSize IndexSize;
    }
}

/// <summary>8-bit RGBA storage shared by textures and targets: R, G, B, A bytes per texel, premultiplied as written.</summary>
internal sealed class ReferencePixels(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Rgba { get; } = new byte[width * height * 4];

    public void Fill(ColorF color)
    {
        byte r = Quantize(color.R), g = Quantize(color.G), b = Quantize(color.B), a = Quantize(color.A);
        for (int i = 0; i < Rgba.Length; i += 4) (Rgba[i], Rgba[i + 1], Rgba[i + 2], Rgba[i + 3]) = (r, g, b, a);
    }

    /// <summary>Writes tightly packed data of <paramref name="format"/> into <paramref name="region"/>.</summary>
    public void Write(RectI region, ReadOnlySpan<byte> data, TextureFormat format)
    {
        bool bgra = format == TextureFormat.Bgra8Unorm;
        for (int y = 0; y < region.Height; y++)
            for (int x = 0; x < region.Width; x++)
            {
                int s = (y * region.Width + x) * 4, d = ((region.Y + y) * Width + region.X + x) * 4;
                Rgba[d] = data[s + (bgra ? 2 : 0)];
                Rgba[d + 1] = data[s + 1];
                Rgba[d + 2] = data[s + (bgra ? 0 : 2)];
                Rgba[d + 3] = data[s + 3];
            }
    }

    /// <summary>Reads <paramref name="region"/> as tightly packed data of <paramref name="format"/>.</summary>
    public byte[] Read(RectI region, TextureFormat format)
    {
        bool bgra = format == TextureFormat.Bgra8Unorm;
        var result = new byte[region.Width * region.Height * 4];
        for (int y = 0; y < region.Height; y++)
            for (int x = 0; x < region.Width; x++)
            {
                int s = ((region.Y + y) * Width + region.X + x) * 4, d = (y * region.Width + x) * 4;
                result[d] = Rgba[s + (bgra ? 2 : 0)];
                result[d + 1] = Rgba[s + 1];
                result[d + 2] = Rgba[s + (bgra ? 0 : 2)];
                result[d + 3] = Rgba[s + 3];
            }
        return result;
    }

    public Vector4 Texel(int x, int y)
    {
        int i = (y * Width + x) * 4;
        return new Vector4(Rgba[i], Rgba[i + 1], Rgba[i + 2], Rgba[i + 3]) / 255f;
    }

    /// <summary>Blends a premultiplied source colour into pixel (x, y) with <paramref name="state"/>, rounding to 8 bits.</summary>
    public void Blend(int x, int y, Vector4 source, BlendState state)
    {
        int i = (y * Width + x) * 4;
        var dst = new Vector4(Rgba[i], Rgba[i + 1], Rgba[i + 2], Rgba[i + 3]) / 255f;
        source = Vector4.Clamp(source, Vector4.Zero, Vector4.One);
        Rgba[i] = Quantize(Channel(state, source, dst, 0));
        Rgba[i + 1] = Quantize(Channel(state, source, dst, 1));
        Rgba[i + 2] = Quantize(Channel(state, source, dst, 2));
        Rgba[i + 3] = Quantize(Channel(state, source, dst, 3));
    }

    internal static float Channel(BlendState state, Vector4 s, Vector4 d, int channel)
    {
        bool alpha = channel == 3;
        var (sf, df, op) = alpha ? (state.SrcAlpha, state.DstAlpha, state.AlphaOp) : (state.SrcColor, state.DstColor, state.ColorOp);
        float sc = s[channel], dc = d[channel];
        float F(BlendFactor f) => f switch
        {
            BlendFactor.Zero => 0,
            BlendFactor.One => 1,
            BlendFactor.SrcColor => sc,
            BlendFactor.InvSrcColor => 1 - sc,
            BlendFactor.SrcAlpha => s.W,
            BlendFactor.InvSrcAlpha => 1 - s.W,
            BlendFactor.DstColor => dc,
            BlendFactor.InvDstColor => 1 - dc,
            BlendFactor.DstAlpha => d.W,
            _ => 1 - d.W,
        };
        float result = op switch
        {
            BlendOp.Add => sc * F(sf) + dc * F(df),
            BlendOp.Subtract => sc * F(sf) - dc * F(df),
            BlendOp.ReverseSubtract => dc * F(df) - sc * F(sf),
            BlendOp.Min => MathF.Min(sc, dc),
            _ => MathF.Max(sc, dc),
        };
        return Math.Clamp(result, 0f, 1f);
    }

    internal static byte Quantize(float value) => (byte)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f, MidpointRounding.AwayFromZero);
}

internal sealed class ReferenceTexture : Texture
{
    private readonly ReferenceTarget? _owner;

    public ReferenceTexture(ReferenceGraphicsDevice device, in TextureDescription description, ReferenceTarget? owner, ReferencePixels? pixels = null)
        : base(device, description)
    {
        _owner = owner;
        Pixels = pixels ?? new ReferencePixels(description.Width, description.Height);
    }

    public ReferencePixels Pixels { get; }

    public override RenderTarget? OwnerTarget => _owner;

    protected override string? OwnedBy => _owner is null ? null : "its render target";

    public void Write(RectI region, ReadOnlySpan<byte> data) => Pixels.Write(region, data, Format);

    /// <summary>Samples at a normalised coordinate with a filter and address modes; the result is premultiplied 0..1.</summary>
    public Vector4 Sample(Vector2 uv, Filter filter, AddressMode u, AddressMode v)
    {
        if (filter == Filter.Nearest)
        {
            int x = Address((int)MathF.Floor(uv.X * Width), Width, u), y = Address((int)MathF.Floor(uv.Y * Height), Height, v);
            return Pixels.Texel(x, y);
        }
        float tx = uv.X * Width - 0.5f, ty = uv.Y * Height - 0.5f;
        int x0 = (int)MathF.Floor(tx), y0 = (int)MathF.Floor(ty);
        float fx = tx - x0, fy = ty - y0;
        int xa = Address(x0, Width, u), xb = Address(x0 + 1, Width, u), ya = Address(y0, Height, v), yb = Address(y0 + 1, Height, v);
        var top = Vector4.Lerp(Pixels.Texel(xa, ya), Pixels.Texel(xb, ya), fx);
        var bottom = Vector4.Lerp(Pixels.Texel(xa, yb), Pixels.Texel(xb, yb), fx);
        return Vector4.Lerp(top, bottom, fy);
    }

    private static int Address(int i, int size, AddressMode mode)
    {
        switch (mode)
        {
            case AddressMode.Repeat:
                return ((i % size) + size) % size;
            case AddressMode.Mirror:
                int period = size * 2, m = ((i % period) + period) % period;
                return m < size ? m : period - 1 - m;
            default:
                return Math.Clamp(i, 0, size - 1);
        }
    }

    protected override void Release() { }

    internal void ExpireOwned() => Expire();
}

internal sealed class ReferenceTarget : RenderTarget
{
    private readonly ReferenceTexture? _color;

    public ReferenceTarget(ReferenceGraphicsDevice device, in RenderTargetDescription description, bool isSurface) : base(device, description, isSurface)
    {
        Pixels = new ReferencePixels(description.Width, description.Height);
        if (!isSurface) _color = new ReferenceTexture(device, new TextureDescription(description.Width, description.Height, description.Format, Name: description.Name), this, Pixels);
    }

    public ReferencePixels Pixels { get; }

    public override Texture? Color => _color;

    protected override string? OwnedBy => IsSurface ? "its device until Present" : null;

    protected override void Release() => _color?.ExpireOwned();

    internal void ExpireSurface() => Expire();
}

internal sealed class ReferenceSampler(ReferenceGraphicsDevice device, in SamplerDescription description) : Sampler(device, description);

internal sealed class ReferenceBuffer(ReferenceGraphicsDevice device, in BufferDescription description) : GpuBuffer(device, description)
{
    public byte[] Memory { get; } = new byte[description.SizeInBytes];

    protected override void Release() { }
}

internal sealed class ReferencePipeline(ReferenceGraphicsDevice device, BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat)
    : Pipeline(device, kind, VertexLayout.Vertex2D, blend, colorFormat, null, default, new ShaderResources(Samplers: 1))
{
    protected override void Release() { }
}

/// <summary>A readback whose pixels were copied when it was requested (devices that execute at submit).</summary>
public sealed class CompletedReadback : ReadbackRequest
{
    private readonly byte[] _pixels;

    /// <summary>A completed request over pixels already read.</summary>
    /// <param name="region">The region read.</param>
    /// <param name="format">The format of <paramref name="pixels"/>.</param>
    /// <param name="pixels">Tightly packed pixels of the region.</param>
    public CompletedReadback(RectI region, TextureFormat format, byte[] pixels) : base(region, format) => _pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));

    /// <summary>Always true.</summary>
    public override bool IsCompleted => true;

    /// <summary>Returns at once.</summary>
    public override void Wait() { }

    /// <inheritdoc />
    protected override byte[] Collect() => _pixels;
}
