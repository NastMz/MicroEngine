using MicroEngine.Graphics.Devices;
using MicroEngine.Platform;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>Factory of the tier-0 SDL_Renderer backend ("sdl-renderer").</summary>
public sealed class SdlRendererBackendFactory : IGraphicsBackendFactory
{
    /// <inheritdoc />
    public string Name => SdlRendererDevice.BackendName;

    /// <inheritdoc />
    public string? UnsupportedReason(ISurface? surface)
        => surface is null || surface.NativeWindow == 0 ? "SDL_Renderer renders only to an SDL window surface (it has no offscreen-only device)" : null;

    /// <inheritdoc />
    public IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options)
    {
        if (UnsupportedReason(surface) is { } reason) throw new NotSupportedException(reason);
        return new SdlRendererDevice(surface!, options);
    }
}

/// <summary>
/// The tier-0 graphics device on SDL_Renderer, with SDL's default render driver unless
/// <see cref="GraphicsDeviceOptions.Api"/> names one ("direct3d11", "direct3d12", "opengl", "vulkan", "metal", "gpu",
/// "software"...). Buffers are device-owned CPU memory submitted through <c>SDL_RenderGeometryRaw</c> (the
/// <see cref="Vertex2D"/> layout is passed with strides, without conversion); the built-in pipeline is an SDL blend mode;
/// samplers are SDL scale and address modes applied per draw; command lists execute inside
/// <see cref="IGraphicsDevice.Submit"/> into SDL's own queue, which SDL flushes at readback and present. Declared
/// features: render targets, repeat addressing and, where the driver composes every blend factor, custom blend states.
/// SDL's <c>software</c> driver is a diagnostic device: its triangle coverage differs from the contract's pixel-centre
/// rule along rotated edges, and it does not claim conformance.
/// </summary>
public sealed unsafe class SdlRendererDevice : GraphicsDeviceBase
{
    /// <summary>The backend name.</summary>
    public const string BackendName = "sdl-renderer";

    private readonly ISurface _surface;
    private readonly SDL_Renderer* _renderer;
    private readonly Dictionary<BlendState, SDL_BlendMode> _blendModes = [];

    /// <summary>Creates the device and its SDL renderer for an SDL window surface.</summary>
    /// <param name="surface">A surface whose <see cref="ISurface.NativeWindow"/> is an <c>SDL_Window*</c>.</param>
    /// <param name="options">The present mode and, optionally, the render driver to ask SDL for.</param>
    /// <exception cref="InvalidOperationException">SDL could not create the renderer (the message names SDL's error).</exception>
    /// <exception cref="GraphicsCapabilityException">The present mode is not VSync or Immediate.</exception>
    public SdlRendererDevice(ISurface surface, GraphicsDeviceOptions options)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(options);
        if (surface.NativeWindow == 0) throw new ArgumentException("SDL_Renderer renders only to an SDL window surface", nameof(surface));
        _surface = surface;
        _renderer = SDL_CreateRenderer((SDL_Window*)surface.NativeWindow, options.Api);
        if (_renderer == null) throw new InvalidOperationException($"SDL_CreateRenderer({options.Api ?? "default"}): {SDL_GetError()}");
        var api = SDL_GetRendererName(_renderer) ?? "unknown";
        var props = SDL_GetRendererProperties(_renderer);
        var maxTexture = (int)SDL_GetNumberProperty(props, SDL_PROP_RENDERER_MAX_TEXTURE_SIZE_NUMBER, 0);
        if (maxTexture <= 0) maxTexture = 2048; // SDL reports 0 when the renderer has no limit query; 2048 is SDL's own floor

        // Present mode: chosen explicitly, never left to the driver.
        var presentModes = new HashSet<PresentMode> { PresentMode.VSync, PresentMode.Immediate };
        if (!presentModes.Contains(options.PresentMode))
            throw new GraphicsCapabilityException("Create", BackendName, GraphicsFeatures.None, $"PresentMode.{options.PresentMode}");
        if (!SDL_SetRenderVSync(_renderer, options.PresentMode == PresentMode.VSync ? 1 : 0))
            throw new InvalidOperationException($"SDL_SetRenderVSync: {SDL_GetError()}");

        // Formats: what SDL lists for the renderer; both 8-bit RGBA orders can be sampled and rendered.
        var formats = new Dictionary<TextureFormat, TextureUsage>();
        var listed = (SDL_PixelFormat*)SDL_GetPointerProperty(props, SDL_PROP_RENDERER_TEXTURE_FORMATS_POINTER, 0);
        for (var p = listed; p != null && *p != SDL_PixelFormat.SDL_PIXELFORMAT_UNKNOWN; p++)
        {
            if (*p == SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888) formats[TextureFormat.Rgba8Unorm] = TextureUsage.Sampled | TextureUsage.RenderTarget;
            if (*p == SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888) formats[TextureFormat.Bgra8Unorm] = TextureUsage.Sampled | TextureUsage.RenderTarget;
        }
        // Even when SDL lists only one order it converts the other on upload, but declare only what it lists natively,
        // plus Rgba8Unorm, which the contract makes mandatory (SDL converts it if needed).
        formats.TryAdd(TextureFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.RenderTarget);

        // Blend states: probe what this renderer accepts (SDL_SetTextureBlendMode refuses unsupported modes).
        var probe = SDL_CreateTexture(_renderer, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, 1, 1);
        var builtInBlends = new HashSet<BlendState>();
        foreach (var preset in BlendState.Presets)
            if (SDL_SetTextureBlendMode(probe, Compose(preset))) builtInBlends.Add(preset);
        // CustomBlend: every factor and operation the contract names, composed into modes no preset uses.
        var custom = Enum.GetValues<BlendFactor>().All(f => SDL_SetTextureBlendMode(probe, Compose(new BlendState(f, BlendFactor.InvSrcAlpha, BlendOp.Add, f, BlendFactor.One, BlendOp.Add))))
                     && Enum.GetValues<BlendOp>().All(o => SDL_SetTextureBlendMode(probe, Compose(new BlendState(BlendFactor.One, BlendFactor.One, o, BlendFactor.One, BlendFactor.One, o))));
        SDL_DestroyTexture(probe);
        if (builtInBlends.Count != BlendState.Presets.Count)
            throw new InvalidOperationException($"SDL renderer '{api}' does not support every blend preset; it cannot implement tier 0");

        var features = GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat | (custom ? GraphicsFeatures.CustomBlend : 0);
        Capabilities = new GraphicsCapabilities(
            BackendName, GraphicsTier.Tier0, features, ShaderFormat.None, formats, builtInBlends, presentModes,
            new GraphicsLimits(maxTexture, maxTexture, MaxTextureSlots: 1, MaxUniformBytes: 0, MaxVertexAttributes: 3),
            api, Adapter: null);
    }

    /// <inheritdoc />
    public override GraphicsCapabilities Capabilities { get; }

    /// <summary>The SDL renderer the device draws with (owned by the device; destroyed with it).</summary>
    public SDL_Renderer* Renderer => _renderer;

    /// <inheritdoc />
    protected override Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData)
    {
        var texture = NewTexture(description.Format, SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, description.Width, description.Height);
        if (!initialData.IsEmpty)
            fixed (byte* data = initialData)
                Check(SDL_UpdateTexture(texture, null, (nint)data, description.Width * 4), "SDL_UpdateTexture");
        return new SdlDeviceTexture(this, description, texture, owner: null);
    }

    /// <inheritdoc />
    protected override RenderTarget CreateRenderTargetCore(in RenderTargetDescription description)
        => new SdlDeviceTarget(this, description, NewTexture(description.Format, SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, description.Width, description.Height));

    /// <inheritdoc />
    protected override Sampler CreateSamplerCore(in SamplerDescription description) => new SdlDeviceSampler(this, description);

    /// <inheritdoc />
    protected override GpuBuffer CreateBufferCore(in BufferDescription description) => new SdlDeviceBuffer(this, description);

    /// <inheritdoc />
    protected override Shader CreateShaderCore(in ShaderDescription description)
        => throw new GraphicsCapabilityException("CreateShader", BackendName, GraphicsFeatures.Shaders); // unreachable: core refuses first

    /// <inheritdoc />
    protected override Pipeline CreatePipelineCore(PipelineDescription description)
        => throw new GraphicsCapabilityException("CreatePipeline", BackendName, GraphicsFeatures.Shaders); // unreachable: core refuses first

    /// <inheritdoc />
    protected override Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat)
    {
        if (!_blendModes.TryGetValue(blend, out var mode)) _blendModes[blend] = mode = Compose(blend);
        return new SdlDevicePipeline(this, kind, blend, colorFormat, mode);
    }

    /// <inheritdoc />
    protected override void SubmitCore(CommandList commands)
    {
        var state = new PassState();
        foreach (ref readonly var c in commands.Commands)
        {
            switch (c.Kind)
            {
                case CommandKind.BeginPass:
                    BeginPass(c.Pass, ref state);
                    break;
                case CommandKind.EndPass:
                    state = new PassState();
                    break;
                case CommandKind.SetViewport:
                    state.Viewport = c.Rect;
                    var vp = ToSdl(c.Rect);
                    Check(SDL_SetRenderViewport(_renderer, &vp), "SDL_SetRenderViewport");
                    ApplyScissor(ref state);
                    break;
                case CommandKind.SetScissor:
                    state.Scissor = c.Rect;
                    ApplyScissor(ref state);
                    break;
                case CommandKind.SetPipeline:
                    state.Pipeline = (SdlDevicePipeline)c.Resource!;
                    break;
                case CommandKind.SetTexture:
                    state.Texture = (SdlDeviceTexture)c.Resource!;
                    state.Sampler = (SdlDeviceSampler)c.Sampler!;
                    break;
                case CommandKind.SetVertices:
                    state.Vertices = (SdlDeviceBuffer)c.Resource!;
                    break;
                case CommandKind.SetIndices:
                    state.Indices = (SdlDeviceBuffer)c.Resource!;
                    state.IndexSize = (int)c.IndexSize;
                    break;
                case CommandKind.Draw:
                    Draw(ref state, c.First, c.Count, indexed: false, 0, 0);
                    break;
                case CommandKind.DrawIndexed:
                    Draw(ref state, c.BaseVertex, (state.Vertices!.SizeInBytes / Vertex2D.SizeInBytes) - c.BaseVertex, indexed: true, c.First, c.Count);
                    break;
                case CommandKind.UpdateBuffer:
                    commands.Payload(c).CopyTo(((SdlDeviceBuffer)c.Resource!).Memory.AsSpan(c.First));
                    break;
                case CommandKind.UpdateTexture:
                    var region = ToSdl(c.Rect);
                    fixed (byte* data = commands.Payload(c))
                        Check(SDL_UpdateTexture(((SdlDeviceTexture)c.Resource!).Native, &region, (nint)data, c.Rect.Width * 4), "SDL_UpdateTexture");
                    break;
                case CommandKind.SetUniforms:
                    throw new InvalidOperationException("tier 0 has no uniforms"); // unreachable: built-ins declare none
                default:
                    throw new NotSupportedException(c.Kind.ToString());
            }
        }
    }

    /// <inheritdoc />
    protected override ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region)
    {
        Check(SDL_SetRenderTarget(_renderer, ((SdlDeviceTarget)target).Native), "SDL_SetRenderTarget");
        Check(SDL_SetRenderViewport(_renderer, null), "SDL_SetRenderViewport");
        var rect = ToSdl(region);
        var surface = SDL_RenderReadPixels(_renderer, &rect); // flushes SDL's queue: everything submitted before is included
        if (surface == null) throw new InvalidOperationException($"SDL_RenderReadPixels: {SDL_GetError()}");
        var wanted = target.ColorFormat == TextureFormat.Bgra8Unorm ? SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888 : SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888;
        var converted = SDL_ConvertSurface(surface, wanted);
        SDL_DestroySurface(surface);
        if (converted == null) throw new InvalidOperationException($"SDL_ConvertSurface: {SDL_GetError()}");
        var pixels = new byte[region.Width * region.Height * 4];
        for (var y = 0; y < region.Height; y++)
            new ReadOnlySpan<byte>((byte*)converted->pixels + y * converted->pitch, region.Width * 4).CopyTo(pixels.AsSpan(y * region.Width * 4));
        SDL_DestroySurface(converted);
        return new CompletedReadback(region, target.ColorFormat, pixels);
    }

    /// <inheritdoc />
    protected override RenderTarget? TryAcquireSurfaceTargetCore()
    {
        if (_surface.IsMinimized) return null;
        int w, h;
        Check(SDL_SetRenderTarget(_renderer, null), "SDL_SetRenderTarget");
        Check(SDL_GetRenderOutputSize(_renderer, &w, &h), "SDL_GetRenderOutputSize");
        if (w <= 0 || h <= 0) return null;
        // SDL_Renderer does not expose its back buffer format; its D3D11, D3D12, Vulkan and GL back buffers are BGRA8.
        return new SdlDeviceTarget(this, new RenderTargetDescription(w, h, TextureFormat.Bgra8Unorm, Name: "surface"), null, isSurface: true);
    }

    /// <inheritdoc />
    protected override void PresentCore(RenderTarget surfaceTarget)
    {
        Check(SDL_SetRenderTarget(_renderer, null), "SDL_SetRenderTarget");
        Check(SDL_RenderPresent(_renderer), "SDL_RenderPresent");
        ((SdlDeviceTarget)surfaceTarget).ExpireSurface();
    }

    /// <inheritdoc />
    protected override void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers)
    {
        foreach (var p in builtIns) ((SdlDevicePipeline)p).Destroy();
        SDL_DestroyRenderer(_renderer); // destroys every texture it created
    }

    private void BeginPass(in PassDescription pass, ref PassState state)
    {
        var target = (SdlDeviceTarget)pass.Color.Target;
        Check(SDL_SetRenderTarget(_renderer, target.Native), "SDL_SetRenderTarget");
        Check(SDL_SetRenderViewport(_renderer, null), "SDL_SetRenderViewport");
        Check(SDL_SetRenderClipRect(_renderer, null), "SDL_SetRenderClipRect");
        state = new PassState { Target = target, Viewport = target.Bounds, Scissor = target.Bounds };
        if (pass.Color.Load == LoadOp.Clear)
        {
            var c = pass.Color.Clear;
            Check(SDL_SetRenderDrawColorFloat(_renderer, c.R, c.G, c.B, c.A), "SDL_SetRenderDrawColorFloat");
            Check(SDL_RenderClear(_renderer), "SDL_RenderClear"); // ignores viewport and clip, as the contract says
        }
    }

    private void ApplyScissor(ref PassState state)
    {
        // The contract's scissor is absolute in target pixels; SDL's clip rectangle is relative to the viewport.
        if (state.Scissor == state.Target!.Bounds) { Check(SDL_SetRenderClipRect(_renderer, null), "SDL_SetRenderClipRect"); return; }
        var clip = new SDL_Rect { x = state.Scissor.X - state.Viewport.X, y = state.Scissor.Y - state.Viewport.Y, w = state.Scissor.Width, h = state.Scissor.Height };
        if (clip.w == 0 || clip.h == 0) { clip.w = clip.h = 0; clip.x = -1; clip.y = -1; } // empty scissor: draw nothing
        Check(SDL_SetRenderClipRect(_renderer, &clip), "SDL_SetRenderClipRect");
    }

    private void Draw(ref PassState state, int firstVertex, int vertexCount, bool indexed, int firstIndex, int indexCount)
    {
        if (state.Scissor.IsEmpty) return;
        var texture = state.Texture!;
        var sampler = state.Sampler!.Description;
        Check(SDL_SetTextureBlendMode(texture.Native, state.Pipeline!.Mode), "SDL_SetTextureBlendMode");
        Check(SDL_SetTextureScaleMode(texture.Native, sampler.Mag == Filter.Linear ? SDL_ScaleMode.SDL_SCALEMODE_LINEAR : SDL_ScaleMode.SDL_SCALEMODE_NEAREST), "SDL_SetTextureScaleMode");
        Check(SDL_SetRenderTextureAddressMode(_renderer, Address(sampler.U), Address(sampler.V)), "SDL_SetRenderTextureAddressMode");
        fixed (byte* vertices = state.Vertices!.Memory)
        fixed (byte* indices = indexed ? state.Indices!.Memory : null)
        {
            var v = vertices + firstVertex * Vertex2D.SizeInBytes;
            Check(SDL_RenderGeometryRaw(_renderer, texture.Native,
                (float*)(v + 0), Vertex2D.SizeInBytes,
                (SDL_FColor*)(v + 16), Vertex2D.SizeInBytes,
                (float*)(v + 8), Vertex2D.SizeInBytes,
                vertexCount,
                indexed ? (nint)(indices + firstIndex * state.IndexSize) : 0, indexed ? indexCount : 0, indexed ? state.IndexSize : 0), "SDL_RenderGeometryRaw");
        }
    }

    private SDL_Texture* NewTexture(TextureFormat format, SDL_TextureAccess access, int width, int height)
    {
        var pixelFormat = format == TextureFormat.Bgra8Unorm ? SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888 : SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888;
        var texture = SDL_CreateTexture(_renderer, pixelFormat, access, width, height);
        if (texture == null) throw new InvalidOperationException($"SDL_CreateTexture: {SDL_GetError()}");
        return texture;
    }

    private static SDL_TextureAddressMode Address(AddressMode mode)
        => mode == AddressMode.Repeat ? SDL_TextureAddressMode.SDL_TEXTURE_ADDRESS_WRAP : SDL_TextureAddressMode.SDL_TEXTURE_ADDRESS_CLAMP;

    private static SDL_BlendMode Compose(BlendState b)
        => SDL_ComposeCustomBlendMode(Factor(b.SrcColor), Factor(b.DstColor), Op(b.ColorOp), Factor(b.SrcAlpha), Factor(b.DstAlpha), Op(b.AlphaOp));

    private static SDL_BlendFactor Factor(BlendFactor f) => f switch
    {
        BlendFactor.Zero => SDL_BlendFactor.SDL_BLENDFACTOR_ZERO,
        BlendFactor.One => SDL_BlendFactor.SDL_BLENDFACTOR_ONE,
        BlendFactor.SrcColor => SDL_BlendFactor.SDL_BLENDFACTOR_SRC_COLOR,
        BlendFactor.InvSrcColor => SDL_BlendFactor.SDL_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
        BlendFactor.SrcAlpha => SDL_BlendFactor.SDL_BLENDFACTOR_SRC_ALPHA,
        BlendFactor.InvSrcAlpha => SDL_BlendFactor.SDL_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
        BlendFactor.DstColor => SDL_BlendFactor.SDL_BLENDFACTOR_DST_COLOR,
        BlendFactor.InvDstColor => SDL_BlendFactor.SDL_BLENDFACTOR_ONE_MINUS_DST_COLOR,
        BlendFactor.DstAlpha => SDL_BlendFactor.SDL_BLENDFACTOR_DST_ALPHA,
        BlendFactor.InvDstAlpha => SDL_BlendFactor.SDL_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
        _ => throw new ArgumentOutOfRangeException(nameof(f)),
    };

    private static SDL_BlendOperation Op(BlendOp o) => o switch
    {
        BlendOp.Add => SDL_BlendOperation.SDL_BLENDOPERATION_ADD,
        BlendOp.Subtract => SDL_BlendOperation.SDL_BLENDOPERATION_SUBTRACT,
        BlendOp.ReverseSubtract => SDL_BlendOperation.SDL_BLENDOPERATION_REV_SUBTRACT,
        BlendOp.Min => SDL_BlendOperation.SDL_BLENDOPERATION_MINIMUM,
        BlendOp.Max => SDL_BlendOperation.SDL_BLENDOPERATION_MAXIMUM,
        _ => throw new ArgumentOutOfRangeException(nameof(o)),
    };

    private static SDL_Rect ToSdl(RectI r) => new() { x = r.X, y = r.Y, w = r.Width, h = r.Height };

    private static void Check(bool ok, string what)
    {
        if (!ok) throw new InvalidOperationException($"{what}: {SDL_GetError()}");
    }

    private struct PassState
    {
        public SdlDeviceTarget? Target;
        public RectI Viewport, Scissor;
        public SdlDevicePipeline? Pipeline;
        public SdlDeviceTexture? Texture;
        public SdlDeviceSampler? Sampler;
        public SdlDeviceBuffer? Vertices, Indices;
        public int IndexSize;
    }
}

internal sealed unsafe class SdlDeviceTexture(SdlRendererDevice device, in TextureDescription description, SDL_Texture* native, SdlDeviceTarget? owner)
    : Texture(device, description)
{
    public SDL_Texture* Native { get; } = native;

    public override RenderTarget? OwnerTarget => owner;

    protected override string? OwnedBy => owner is null ? null : "its render target";

    protected override void Release()
    {
        if (owner is null && !((SdlRendererDevice)Device).IsDisposed) SDL_DestroyTexture(Native);
    }

    internal void ExpireOwned() => Expire();
}

internal sealed unsafe class SdlDeviceTarget : RenderTarget
{
    private readonly SdlDeviceTexture? _color;

    public SdlDeviceTarget(SdlRendererDevice device, in RenderTargetDescription description, SDL_Texture* native, bool isSurface = false)
        : base(device, description, isSurface)
    {
        Native = native;
        if (!isSurface) _color = new SdlDeviceTexture(device, new TextureDescription(description.Width, description.Height, description.Format, Name: description.Name), native, this);
    }

    /// <summary>The SDL target texture; null for the surface (SDL's default target).</summary>
    public SDL_Texture* Native { get; }

    public override Texture? Color => _color;

    protected override string? OwnedBy => IsSurface ? "its device until Present" : null;

    protected override void Release()
    {
        _color?.ExpireOwned();
        if (!IsSurface && !((SdlRendererDevice)Device).IsDisposed) SDL_DestroyTexture(Native);
    }

    internal void ExpireSurface() => Expire();
}

internal sealed class SdlDeviceSampler(SdlRendererDevice device, in SamplerDescription description) : Sampler(device, description);

internal sealed class SdlDeviceBuffer(SdlRendererDevice device, in BufferDescription description) : GpuBuffer(device, description)
{
    /// <summary>Device-owned CPU memory: the tier-0 buffer.</summary>
    public byte[] Memory { get; } = new byte[description.SizeInBytes];

    protected override void Release() { }
}

internal sealed class SdlDevicePipeline(SdlRendererDevice device, BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, SDL_BlendMode mode)
    : Pipeline(device, kind, VertexLayout.Vertex2D, blend, colorFormat, null, default, new ShaderResources(Samplers: 1))
{
    public SDL_BlendMode Mode { get; } = mode;

    protected override void Release() { }

    internal void Destroy() => Expire();
}

