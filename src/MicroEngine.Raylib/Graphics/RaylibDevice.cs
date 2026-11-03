using System.Numerics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Platform;
using Rl = Raylib_cs.Raylib;
using Rlgl = Raylib_cs.Rlgl;

namespace MicroEngine.Raylib;

/// <summary>
/// Factory of the tier-0 raylib device ("raylib"). raylib owns one window and one OpenGL context per process, so the
/// device accepts only raylib's own window: the surface whose native window is raylib's, once raylib has opened it.
/// </summary>
public sealed class RaylibDeviceFactory : IGraphicsBackendFactory
{
    /// <inheritdoc />
    public string Name => RaylibDevice.BackendName;

    /// <inheritdoc />
    public unsafe string? UnsupportedReason(ISurface? surface)
    {
        if (surface is null) return "raylib draws only into the window it opened (it has no offscreen-only device)";
        if (!Rl.IsWindowReady()) return "raylib has not opened its window (InitWindow)";
        if (surface.NativeWindow != (nint)Rl.GetWindowHandle()) return "raylib draws only into its own window, not into another library's surface";
        return null;
    }

    /// <inheritdoc />
    public IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options)
    {
        if (UnsupportedReason(surface) is { } reason) throw new NotSupportedException(reason);
        return new RaylibDevice(surface!, options);
    }
}

/// <summary>
/// The tier-0 graphics device on raylib's OpenGL layer (rlgl): textures and render targets are OpenGL textures and
/// framebuffers, buffers are device-owned CPU memory whose triangles are sent through rlgl's batch, the built-in pipeline
/// is rlgl's default shader with the pass's blend factors, and samplers are texture parameters set before each draw.
/// Offscreen targets are rendered with row 0 at the top of the texture's memory, so uploads, sampling and readback need no
/// flipping and OpenGL's tie rule gives the contract's top-left rule; the window's target (bottom-up in OpenGL) moves
/// geometry up by 1/128 px for the same rule. Desktop OpenGL declares render targets, repeat and mirror addressing,
/// separate filters and custom blend states; in browsers (WebGL) only render targets are declared.
/// </summary>
public sealed unsafe class RaylibDevice : GraphicsDeviceBase
{
    /// <summary>The backend name.</summary>
    public const string BackendName = "raylib";

    /// <summary>How far geometry drawn into the window moves up so pixel-centre ties follow the top-left rule.</summary>
    internal const float SurfaceTieShift = 1f / 128;

    // OpenGL enumerations rlgl passes through unchanged.
    private const int GlTextureMagFilter = 0x2800, GlTextureMinFilter = 0x2801, GlTextureWrapS = 0x2802, GlTextureWrapT = 0x2803;
    private const int GlNearest = 0x2600, GlLinear = 0x2601, GlRepeat = 0x2901, GlClampToEdge = 0x812F, GlMirroredRepeat = 0x8370;
    private const int GlTriangles = 0x0004;

    private readonly ISurface _surface;

    /// <summary>Creates the device on raylib's window (raylib must have opened it).</summary>
    /// <param name="surface">raylib's window surface.</param>
    /// <param name="options">The present mode (raylib's VSync is a window flag chosen when the window opens; the device records the mode).</param>
    /// <exception cref="GraphicsCapabilityException">The present mode is Mailbox.</exception>
    public RaylibDevice(ISurface surface, GraphicsDeviceOptions options)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(options);
        if (!Rl.IsWindowReady()) throw new InvalidOperationException("raylib has not opened its window");
        _surface = surface;
        var presentModes = new HashSet<PresentMode> { PresentMode.VSync, PresentMode.Immediate };
        if (!presentModes.Contains(options.PresentMode))
            throw new GraphicsCapabilityException("Create", BackendName, GraphicsFeatures.None, $"PresentMode.{options.PresentMode}");
        PresentMode = options.PresentMode;
        bool web = OperatingSystem.IsBrowser();
        var features = GraphicsFeatures.RenderTargets
            | (web ? GraphicsFeatures.None : GraphicsFeatures.AddressRepeat | GraphicsFeatures.AddressMirror | GraphicsFeatures.SeparateFilters | GraphicsFeatures.CustomBlend);
        var both = TextureUsage.Sampled | TextureUsage.RenderTarget;
        Capabilities = new GraphicsCapabilities(BackendName, GraphicsTier.Tier0, features, ShaderFormat.None,
            new Dictionary<TextureFormat, TextureUsage> { [TextureFormat.Rgba8Unorm] = both, [TextureFormat.Bgra8Unorm] = both },
            new HashSet<BlendState>(BlendState.Presets), presentModes,
            // OpenGL ES 2 guarantees 64; every desktop GL 3.3 and WebGL implementation in use reports at least 2048.
            new GraphicsLimits(MaxTextureSize: 2048, MaxRenderTargetSize: 2048, MaxTextureSlots: 1, MaxUniformBytes: 0, MaxVertexAttributes: 3),
            ApiName: web ? "webgl" : "opengl", Adapter: null);
    }

    /// <inheritdoc />
    public override GraphicsCapabilities Capabilities { get; }

    /// <summary>The present mode chosen at creation.</summary>
    public PresentMode PresentMode { get; }

    /// <summary>
    /// Whether <see cref="IGraphicsDevice.Present"/> swaps the window's buffers (true by default). A host whose frame
    /// loop ends with raylib's <c>EndDrawing</c>, which swaps them itself (and polls input), sets it to false: Present
    /// then only finishes the frame's drawing and expires the surface target.
    /// </summary>
    public bool SwapOnPresent { get; init; } = true;

    /// <inheritdoc />
    protected override Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData)
    {
        uint id;
        if (initialData.IsEmpty) id = Rlgl.LoadTexture(null, description.Width, description.Height, Raylib_cs.PixelFormat.UncompressedR8G8B8A8, 1);
        else
        {
            var rgba = AsRgba(initialData, description.Format);
            fixed (byte* data = rgba) id = Rlgl.LoadTexture(data, description.Width, description.Height, Raylib_cs.PixelFormat.UncompressedR8G8B8A8, 1);
        }
        if (id == 0) throw new InvalidOperationException($"raylib could not create a {description.Width}x{description.Height} texture");
        return new RaylibDeviceTexture(this, description, id, owner: null);
    }

    /// <inheritdoc />
    protected override RenderTarget CreateRenderTargetCore(in RenderTargetDescription description)
    {
        var target = Rl.LoadRenderTexture(description.Width, description.Height);
        if (target.Id == 0) throw new InvalidOperationException($"raylib could not create a {description.Width}x{description.Height} render target");
        return new RaylibDeviceTarget(this, description, target);
    }

    /// <inheritdoc />
    protected override Sampler CreateSamplerCore(in SamplerDescription description) => new RaylibDeviceSampler(this, description);

    /// <inheritdoc />
    protected override GpuBuffer CreateBufferCore(in BufferDescription description) => new RaylibDeviceBuffer(this, description);

    /// <inheritdoc />
    protected override Shader CreateShaderCore(in ShaderDescription description)
        => throw new GraphicsCapabilityException("CreateShader", BackendName, GraphicsFeatures.Shaders); // unreachable: core refuses first

    /// <inheritdoc />
    protected override Pipeline CreatePipelineCore(PipelineDescription description)
        => throw new GraphicsCapabilityException("CreatePipeline", BackendName, GraphicsFeatures.Shaders); // unreachable: core refuses first

    /// <inheritdoc />
    protected override Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat)
        => new RaylibDevicePipeline(this, kind, blend, colorFormat);

    /// <inheritdoc />
    protected override void SubmitCore(CommandList commands)
    {
        var projection = Rlgl.GetMatrixProjection();
        var modelview = Rlgl.GetMatrixModelview();
        Rlgl.DrawRenderBatchActive(); // whatever raylib queued before belongs before this list
        var state = new PassState();
        try
        {
            foreach (ref readonly var c in commands.Commands)
            {
                switch (c.Kind)
                {
                    case CommandKind.BeginPass:
                        BeginPass(c.Pass, ref state);
                        break;
                    case CommandKind.EndPass:
                        Rlgl.DrawRenderBatchActive();
                        state = new PassState();
                        break;
                    case CommandKind.SetViewport:
                        Rlgl.DrawRenderBatchActive();
                        state.Viewport = c.Rect;
                        ApplyViewport(state);
                        break;
                    case CommandKind.SetScissor:
                        Rlgl.DrawRenderBatchActive();
                        state.Scissor = c.Rect;
                        ApplyScissor(state);
                        break;
                    case CommandKind.SetPipeline:
                        var pipeline = (RaylibDevicePipeline)c.Resource!;
                        if (!ReferenceEquals(pipeline, state.Pipeline))
                        {
                            Rlgl.DrawRenderBatchActive();
                            ApplyBlend(pipeline.Blend);
                            state.Pipeline = pipeline;
                        }
                        break;
                    case CommandKind.SetTexture:
                        state.Texture = (RaylibDeviceTexture)c.Resource!;
                        state.Sampler = c.Sampler!.Description;
                        break;
                    case CommandKind.SetVertices:
                        state.Vertices = (RaylibDeviceBuffer)c.Resource!;
                        break;
                    case CommandKind.SetIndices:
                        state.Indices = (RaylibDeviceBuffer)c.Resource!;
                        state.IndexSize = c.IndexSize;
                        break;
                    case CommandKind.Draw:
                        Draw(ref state, c.First, c.Count, indexed: false, baseVertex: 0);
                        break;
                    case CommandKind.DrawIndexed:
                        Draw(ref state, c.First, c.Count, indexed: true, c.BaseVertex);
                        break;
                    case CommandKind.UpdateBuffer:
                        commands.Payload(c).CopyTo(((RaylibDeviceBuffer)c.Resource!).Memory.AsSpan(c.First));
                        break;
                    case CommandKind.UpdateTexture:
                        var texture = (RaylibDeviceTexture)c.Resource!;
                        var rgba = AsRgba(commands.Payload(c), texture.Format);
                        fixed (byte* data = rgba)
                            Rlgl.UpdateTexture(texture.Id, c.Rect.X, c.Rect.Y, c.Rect.Width, c.Rect.Height, Raylib_cs.PixelFormat.UncompressedR8G8B8A8, data);
                        break;
                    case CommandKind.SetUniforms:
                        throw new InvalidOperationException("tier 0 has no uniforms"); // unreachable: the built-in pipeline declares none
                    default:
                        throw new NotSupportedException(c.Kind.ToString());
                }
            }
        }
        finally
        {
            // Leave raylib as its own drawing expects it: default framebuffer, alpha blending, no scissor, back faces culled.
            Rlgl.DrawRenderBatchActive();
            Rlgl.DisableScissorTest();
            Rlgl.DisableFramebuffer();
            Rlgl.SetBlendMode(Raylib_cs.BlendMode.Alpha);
            Rlgl.EnableBackfaceCulling();
            Rlgl.Viewport(0, 0, Rl.GetRenderWidth(), Rl.GetRenderHeight());
            Rlgl.SetMatrixProjection(projection);
            Rlgl.SetMatrixModelView(modelview);
        }
    }

    /// <inheritdoc />
    protected override ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region)
    {
        Rlgl.DrawRenderBatchActive();
        var native = (RaylibDeviceTarget)target;
        var pixels = (byte*)Rlgl.ReadTexturePixels(native.Native.Texture.Id, target.Width, target.Height, Raylib_cs.PixelFormat.UncompressedR8G8B8A8);
        if (pixels == null) throw new InvalidOperationException("raylib could not read the render target back");
        try
        {
            bool bgra = target.ColorFormat == TextureFormat.Bgra8Unorm;
            var result = new byte[region.Width * region.Height * 4];
            for (int y = 0; y < region.Height; y++)
                for (int x = 0; x < region.Width; x++)
                {
                    int s = ((region.Y + y) * target.Width + region.X + x) * 4, d = (y * region.Width + x) * 4;
                    result[d] = pixels[s + (bgra ? 2 : 0)];
                    result[d + 1] = pixels[s + 1];
                    result[d + 2] = pixels[s + (bgra ? 0 : 2)];
                    result[d + 3] = pixels[s + 3];
                }
            return new CompletedReadback(region, target.ColorFormat, result);
        }
        finally { Rl.MemFree(pixels); }
    }

    /// <inheritdoc />
    protected override RenderTarget? TryAcquireSurfaceTargetCore()
    {
        if (_surface.IsMinimized) return null;
        int width = Rl.GetRenderWidth(), height = Rl.GetRenderHeight();
        if (width <= 0 || height <= 0) return null;
        return new RaylibDeviceTarget(this, new RenderTargetDescription(width, height, TextureFormat.Bgra8Unorm, Name: "surface"), default, isSurface: true);
    }

    /// <inheritdoc />
    protected override void PresentCore(RenderTarget surfaceTarget)
    {
        Rlgl.DrawRenderBatchActive();
        if (SwapOnPresent) Rl.SwapScreenBuffer();
        ((RaylibDeviceTarget)surfaceTarget).ExpireSurface();
    }

    /// <inheritdoc />
    protected override void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers)
    {
        foreach (var texture in _textures.ToArray()) texture.Destroy();
        foreach (var target in _targets.ToArray()) target.Destroy();
        _textures.Clear();
        _targets.Clear();
    }

    private readonly HashSet<RaylibDeviceTexture> _textures = [];
    private readonly HashSet<RaylibDeviceTarget> _targets = [];

    internal void Track(RaylibDeviceTexture texture) => _textures.Add(texture);
    internal void Forget(RaylibDeviceTexture texture) => _textures.Remove(texture);
    internal void Track(RaylibDeviceTarget target) => _targets.Add(target);
    internal void Forget(RaylibDeviceTarget target) => _targets.Remove(target);

    private void BeginPass(in PassDescription pass, ref PassState state)
    {
        Rlgl.DrawRenderBatchActive();
        var target = (RaylibDeviceTarget)pass.Color.Target;
        if (target.IsSurface) Rlgl.DisableFramebuffer();
        else Rlgl.EnableFramebuffer(target.Native.Id);
        Rlgl.DisableBackfaceCulling();
        Rlgl.DisableScissorTest();
        state = new PassState { Target = target, Viewport = target.Bounds, Scissor = target.Bounds };
        ApplyViewport(state);
        if (pass.Color.Load == LoadOp.Clear)
        {
            // OpenGL clears ignore the viewport but not the scissor: the scissor is off until the pass sets one.
            var c = pass.Color.Clear;
            Rlgl.ClearColor(Byte(c.R), Byte(c.G), Byte(c.B), Byte(c.A));
            Rlgl.ClearScreenBuffers();
        }
        state.Pipeline = null;
    }

    private static void ApplyViewport(in PassState state)
    {
        var target = state.Target!;
        var v = state.Viewport;
        // Offscreen: target row y is OpenGL row y (row 0 at the bottom of the framebuffer = first row in memory).
        // The window: target row 0 is the top, OpenGL row height - 1.
        int glY = target.IsSurface ? target.Height - v.Bottom : v.Y;
        Rlgl.Viewport(v.X, glY, v.Width, v.Height);
        var projection = target.IsSurface
            ? Raylib_cs.Raymath.MatrixOrtho(0, v.Width, v.Height, 0, 0, 1)
            : Raylib_cs.Raymath.MatrixOrtho(0, v.Width, 0, v.Height, 0, 1);
        Rlgl.SetMatrixProjection(projection);
        Rlgl.SetMatrixModelView(Matrix4x4.Identity);
        ApplyScissor(state);
    }

    private static void ApplyScissor(in PassState state)
    {
        var target = state.Target!;
        var s = state.Scissor;
        if (s == target.Bounds) { Rlgl.DisableScissorTest(); return; }
        Rlgl.EnableScissorTest();
        int glY = target.IsSurface ? target.Height - s.Bottom : s.Y;
        Rlgl.Scissor(s.X, glY, Math.Max(0, s.Width), Math.Max(0, s.Height));
    }

    private static void ApplyBlend(BlendState blend)
    {
        Rlgl.SetBlendFactorsSeparate(Factor(blend.SrcColor), Factor(blend.DstColor), Factor(blend.SrcAlpha), Factor(blend.DstAlpha), Op(blend.ColorOp), Op(blend.AlphaOp));
        Rlgl.SetBlendMode(Raylib_cs.BlendMode.CustomSeparate);
    }

    private void Draw(ref PassState state, int first, int count, bool indexed, int baseVertex)
    {
        var target = state.Target!;
        if (state.Viewport.Intersect(state.Scissor).IsEmpty) return;
        var texture = state.Texture!;
        if (!ReferenceEquals(state.BoundTexture, texture) || state.BoundSampler != state.Sampler)
        {
            Rlgl.DrawRenderBatchActive(); // texture parameters apply when the batch is drawn
            var s = state.Sampler;
            Rlgl.TextureParameters(texture.Id, GlTextureMagFilter, s.Mag == Filter.Linear ? GlLinear : GlNearest);
            Rlgl.TextureParameters(texture.Id, GlTextureMinFilter, s.Min == Filter.Linear ? GlLinear : GlNearest);
            Rlgl.TextureParameters(texture.Id, GlTextureWrapS, Wrap(s.U));
            Rlgl.TextureParameters(texture.Id, GlTextureWrapT, Wrap(s.V));
            state.BoundTexture = texture;
            state.BoundSampler = s;
        }
        float shift = target.IsSurface ? SurfaceTieShift : 0;
        var vertices = state.Vertices!.Memory;
        var indices = indexed ? state.Indices!.Memory : null;
        int vertexCapacity = vertices.Length / Vertex2D.SizeInBytes;
        const int Chunk = 3 * 1024; // whole triangles per rlgl batch check
        for (int start = 0; start + 2 < count; start += Chunk)
        {
            int n = Math.Min(Chunk, count - start) / 3 * 3;
            Rlgl.CheckRenderBatchLimit(n);
            Rlgl.SetTexture(texture.Id);
            Rlgl.Begin(GlTriangles);
            for (int i = 0; i < n; i++)
            {
                int index = indexed ? Index(indices!, state.IndexSize, first + start + i) + baseVertex : first + start + i;
                if ((uint)index >= (uint)vertexCapacity)
                {
                    Rlgl.End();
                    throw new InvalidOperationException($"vertex {index} lies outside the bound vertex buffer; index values are not validated when recorded");
                }
                var v = System.Runtime.InteropServices.MemoryMarshal.Read<Vertex2D>(vertices.AsSpan(index * Vertex2D.SizeInBytes));
                Rlgl.Color4ub(Byte(v.Color.R), Byte(v.Color.G), Byte(v.Color.B), Byte(v.Color.A));
                Rlgl.TexCoord2f(v.TexCoord.X, v.TexCoord.Y);
                Rlgl.Vertex2f(v.Position.X, v.Position.Y - shift);
            }
            Rlgl.End();
            Rlgl.SetTexture(0);
        }
    }

    private static int Index(byte[] indices, IndexSize size, int position) => size == IndexSize.Bits16
        ? BitConverter.ToUInt16(indices, position * 2)
        : (int)BitConverter.ToUInt32(indices, position * 4);

    private static byte Byte(float value) => (byte)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);

    private static int Wrap(AddressMode mode) => mode switch
    {
        AddressMode.Repeat => GlRepeat,
        AddressMode.Mirror => GlMirroredRepeat,
        _ => GlClampToEdge,
    };

    private static int Factor(BlendFactor f) => f switch
    {
        BlendFactor.Zero => 0,
        BlendFactor.One => 1,
        BlendFactor.SrcColor => 0x0300,
        BlendFactor.InvSrcColor => 0x0301,
        BlendFactor.SrcAlpha => 0x0302,
        BlendFactor.InvSrcAlpha => 0x0303,
        BlendFactor.DstAlpha => 0x0304,
        BlendFactor.InvDstAlpha => 0x0305,
        BlendFactor.DstColor => 0x0306,
        BlendFactor.InvDstColor => 0x0307,
        _ => throw new ArgumentOutOfRangeException(nameof(f)),
    };

    private static int Op(BlendOp o) => o switch
    {
        BlendOp.Add => 0x8006,
        BlendOp.Min => 0x8007,
        BlendOp.Max => 0x8008,
        BlendOp.Subtract => 0x800A,
        BlendOp.ReverseSubtract => 0x800B,
        _ => throw new ArgumentOutOfRangeException(nameof(o)),
    };

    /// <summary>Texel data as R, G, B, A bytes (BGRA data swizzled; RGBA returned as is).</summary>
    private static byte[] AsRgba(ReadOnlySpan<byte> data, TextureFormat format)
    {
        var rgba = data.ToArray();
        if (format != TextureFormat.Bgra8Unorm) return rgba;
        for (int i = 0; i + 3 < rgba.Length; i += 4) (rgba[i], rgba[i + 2]) = (rgba[i + 2], rgba[i]);
        return rgba;
    }

    private struct PassState
    {
        public RaylibDeviceTarget? Target;
        public RectI Viewport, Scissor;
        public RaylibDevicePipeline? Pipeline;
        public RaylibDeviceTexture? Texture, BoundTexture;
        public SamplerDescription Sampler, BoundSampler;
        public RaylibDeviceBuffer? Vertices, Indices;
        public IndexSize IndexSize;
    }
}

internal sealed class RaylibDeviceTexture : Texture
{
    private readonly RaylibDeviceTarget? _owner;
    private bool _destroyed;

    public RaylibDeviceTexture(RaylibDevice device, in TextureDescription description, uint id, RaylibDeviceTarget? owner) : base(device, description)
    {
        Id = id;
        _owner = owner;
        if (owner is null) device.Track(this);
    }

    public uint Id { get; }

    public override RenderTarget? OwnerTarget => _owner;

    protected override string? OwnedBy => _owner is null ? null : "its render target";

    protected override void Release()
    {
        if (_owner is not null) return; // the target's framebuffer owns its colour texture
        ((RaylibDevice)Device).Forget(this);
        Destroy();
    }

    internal void Destroy()
    {
        if (_destroyed || _owner is not null) return;
        _destroyed = true;
        Rlgl.UnloadTexture(Id);
    }

    internal void ExpireOwned() => Expire();
}

internal sealed class RaylibDeviceTarget : RenderTarget
{
    private readonly RaylibDeviceTexture? _color;
    private bool _destroyed;

    public RaylibDeviceTarget(RaylibDevice device, in RenderTargetDescription description, Raylib_cs.RenderTexture2D native, bool isSurface = false)
        : base(device, description, isSurface)
    {
        Native = native;
        if (isSurface) return;
        _color = new RaylibDeviceTexture(device, new TextureDescription(description.Width, description.Height, description.Format, Name: description.Name), native.Texture.Id, this);
        device.Track(this);
    }

    /// <summary>raylib's framebuffer and colour texture; default for the window's target.</summary>
    public Raylib_cs.RenderTexture2D Native { get; }

    public override Texture? Color => _color;

    protected override string? OwnedBy => IsSurface ? "its device until Present" : null;

    protected override void Release()
    {
        _color?.ExpireOwned();
        if (IsSurface) return;
        ((RaylibDevice)Device).Forget(this);
        Destroy();
    }

    internal void Destroy()
    {
        if (_destroyed || IsSurface) return;
        _destroyed = true;
        Rl.UnloadRenderTexture(Native);
    }

    internal void ExpireSurface() => Expire();
}

internal sealed class RaylibDeviceSampler(RaylibDevice device, in SamplerDescription description) : Sampler(device, description);

internal sealed class RaylibDeviceBuffer(RaylibDevice device, in BufferDescription description) : GpuBuffer(device, description)
{
    /// <summary>Device-owned CPU memory: the tier-0 buffer.</summary>
    public byte[] Memory { get; } = new byte[description.SizeInBytes];

    protected override void Release() { }
}

internal sealed class RaylibDevicePipeline(RaylibDevice device, BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat)
    : Pipeline(device, kind, VertexLayout.Vertex2D, blend, colorFormat, null, default, new ShaderResources(Samplers: 1))
{
    protected override void Release() { }
}
