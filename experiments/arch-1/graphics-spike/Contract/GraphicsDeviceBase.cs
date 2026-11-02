using System.Diagnostics.CodeAnalysis;

namespace Arch1.Graphics;

/// <summary>
/// Core half of every device: validates each call against the written rules and the declared capabilities, then hands
/// the backend a call it can execute. Backends derive from it and implement only the <c>*Core</c> members, so ownership,
/// disposal, ranges and capability errors behave identically on every tier.
/// </summary>
public abstract class GraphicsDeviceBase : IGraphicsDevice
{
    private readonly Dictionary<SamplerDescription, Sampler> _samplers = [];
    private readonly Dictionary<(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?), Pipeline> _builtIns = [];
    private RenderTarget? _surfaceTarget;

    /// <inheritdoc />
    public abstract GraphicsCapabilities Capabilities { get; }

    /// <inheritdoc />
    public event Action<DeviceLostReason>? DeviceLost;

    /// <summary>True after <see cref="Dispose"/>.</summary>
    public bool IsDisposed { get; private set; }

    private string Backend => Capabilities.Backend;

    /// <inheritdoc />
    public Texture CreateTexture(in TextureDescription description, ReadOnlySpan<byte> initialData = default)
    {
        Alive();
        Size(description.Width, description.Height, Capabilities.Limits.MaxTextureSize, nameof(description));
        if (TextureFormats.IsDepth(description.Format)) throw new ArgumentException("depth formats are created through RenderTargetDescription.Depth", nameof(description));
        RequireFormat(description.Format, TextureUsage.Sampled, "CreateTexture");
        if (description.MipLevels < 1) throw new ArgumentOutOfRangeException(nameof(description), "MipLevels must be at least 1");
        if (description.MipLevels > 1) Require(GraphicsFeatures.Mipmaps, "CreateTexture(MipLevels > 1)");
        if (!initialData.IsEmpty && initialData.Length != TextureFormats.ByteCount(description.Format, description.Width, description.Height))
            throw new ArgumentException($"initial data must be {TextureFormats.ByteCount(description.Format, description.Width, description.Height)} bytes (level 0, tightly packed) or empty", nameof(initialData));
        return CreateTextureCore(description, initialData);
    }

    /// <inheritdoc />
    public RenderTarget CreateRenderTarget(in RenderTargetDescription description)
    {
        Alive();
        Require(GraphicsFeatures.RenderTargets, "CreateRenderTarget");
        Size(description.Width, description.Height, Capabilities.Limits.MaxRenderTargetSize, nameof(description));
        if (TextureFormats.IsDepth(description.Format)) throw new ArgumentException("the colour format cannot be a depth format", nameof(description));
        RequireFormat(description.Format, TextureUsage.RenderTarget | TextureUsage.Sampled, "CreateRenderTarget");
        if (description.Depth is { } depth)
        {
            Require(GraphicsFeatures.DepthBuffer, "CreateRenderTarget(depth)");
            if (!TextureFormats.IsDepth(depth)) throw new ArgumentException($"{depth} is not a depth format", nameof(description));
            RequireFormat(depth, TextureUsage.DepthStencil, "CreateRenderTarget(depth)");
        }
        return CreateRenderTargetCore(description);
    }

    /// <inheritdoc />
    public Sampler GetSampler(in SamplerDescription description)
    {
        Alive();
        if (_samplers.TryGetValue(description, out var cached)) return cached;
        foreach (var mode in (ReadOnlySpan<AddressMode>)[description.U, description.V])
        {
            if (mode == AddressMode.Repeat) Require(GraphicsFeatures.AddressRepeat, "GetSampler(Repeat)");
            if (mode == AddressMode.Mirror) Require(GraphicsFeatures.AddressMirror, "GetSampler(Mirror)");
        }
        if (description.Min != description.Mag) Require(GraphicsFeatures.SeparateFilters, "GetSampler(Min != Mag)");
        var sampler = CreateSamplerCore(description);
        _samplers.Add(description, sampler);
        return sampler;
    }

    /// <inheritdoc />
    public GpuBuffer CreateBuffer(in BufferDescription description)
    {
        Alive();
        if (description.SizeInBytes < 1) throw new ArgumentOutOfRangeException(nameof(description), "SizeInBytes must be at least 1");
        if ((description.Usage & (BufferUsage.Vertex | BufferUsage.Index)) == 0 || (description.Usage & ~(BufferUsage.Vertex | BufferUsage.Index)) != 0)
            throw new ArgumentOutOfRangeException(nameof(description), "Usage must be Vertex, Index or both");
        return CreateBufferCore(description);
    }

    /// <inheritdoc />
    public Shader CreateShader(in ShaderDescription description)
    {
        Alive();
        Require(GraphicsFeatures.Shaders, "CreateShader");
        if (description.Format is not (ShaderFormat.SpirV or ShaderFormat.Dxil or ShaderFormat.Msl))
            throw new ArgumentException("Format must be exactly one shader format", nameof(description));
        if ((Capabilities.ShaderFormats & description.Format) == 0)
            throw new GraphicsCapabilityException("CreateShader", Backend, GraphicsFeatures.None, $"ShaderFormat.{description.Format} (declared: {Capabilities.ShaderFormats})");
        if (description.Code.IsEmpty) throw new ArgumentException("shader code is empty", nameof(description));
        if (string.IsNullOrEmpty(description.EntryPoint)) throw new ArgumentException("entry point is empty", nameof(description));
        var r = description.Resources;
        if (r.Samplers < 0 || r.UniformBuffers < 0 || r.StorageTextures != 0 || r.StorageBuffers != 0)
            throw new ArgumentException("resource counts must be non-negative; storage resources are not part of this contract version", nameof(description));
        if (description.Stage == ShaderStage.Fragment && r.Samplers > Capabilities.Limits.MaxTextureSlots)
            throw new ArgumentOutOfRangeException(nameof(description), $"{r.Samplers} samplers exceed MaxTextureSlots {Capabilities.Limits.MaxTextureSlots}");
        return CreateShaderCore(description);
    }

    /// <inheritdoc />
    public Pipeline CreatePipeline(PipelineDescription description)
    {
        Alive();
        ArgumentNullException.ThrowIfNull(description);
        Require(GraphicsFeatures.Shaders, "CreatePipeline");
        Own(description.Vertex, nameof(description));
        Own(description.Fragment, nameof(description));
        if (description.Vertex.Stage != ShaderStage.Vertex || description.Fragment.Stage != ShaderStage.Fragment)
            throw new ArgumentException("Vertex must be a vertex shader and Fragment a fragment shader", nameof(description));
        if (description.DepthFormat is not null || description.Depth.Test || description.Depth.Write)
        {
            Require(GraphicsFeatures.DepthBuffer, "CreatePipeline(depth)");
            if (description.DepthFormat is not { } depth || !TextureFormats.IsDepth(depth))
                throw new ArgumentException("a depth state needs a depth DepthFormat", nameof(description));
            RequireFormat(depth, TextureUsage.DepthStencil, "CreatePipeline(depth)");
        }
        RequireFormat(description.ColorFormat, TextureUsage.RenderTarget, "CreatePipeline");
        if (!Capabilities.BuiltInBlendStates.Contains(description.Blend)) Require(GraphicsFeatures.CustomBlend, "CreatePipeline(blend)");
        if (description.Layout.Stride <= 0 || description.Layout.Attributes.Count == 0 || description.Layout.Attributes.Count > Capabilities.Limits.MaxVertexAttributes)
            throw new ArgumentOutOfRangeException(nameof(description), "the vertex layout needs a positive stride and 1..MaxVertexAttributes attributes");
        return CreatePipelineCore(description);
    }

    /// <inheritdoc />
    public Pipeline GetBuiltInPipeline(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat = null)
    {
        Alive();
        ArgumentNullException.ThrowIfNull(blend);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var key = (kind, blend, colorFormat, depthFormat);
        if (_builtIns.TryGetValue(key, out var cached)) return cached;
        if (!Capabilities.BuiltInBlendStates.Contains(blend)) Require(GraphicsFeatures.CustomBlend, $"GetBuiltInPipeline({kind}, custom blend)");
        RequireFormat(colorFormat, TextureUsage.RenderTarget, $"GetBuiltInPipeline({kind})");
        if (depthFormat is { } depth)
        {
            Require(GraphicsFeatures.DepthBuffer, $"GetBuiltInPipeline({kind}, depth)");
            RequireFormat(depth, TextureUsage.DepthStencil, $"GetBuiltInPipeline({kind}, depth)");
        }
        var pipeline = CreateBuiltInPipelineCore(kind, blend, colorFormat, depthFormat);
        _builtIns.Add(key, pipeline);
        return pipeline;
    }

    /// <inheritdoc />
    public CommandList BeginCommands()
    {
        Alive();
        return new CommandList(this);
    }

    /// <inheritdoc />
    public void Submit(CommandList commands)
    {
        Alive();
        ArgumentNullException.ThrowIfNull(commands);
        if (!ReferenceEquals(commands.Device, this)) throw new ArgumentException("the command list belongs to another device", nameof(commands));
        commands.Seal();
        SubmitCore(commands);
    }

    /// <inheritdoc />
    public ReadbackRequest ReadPixels(RenderTarget target, RectI region)
    {
        Alive();
        Own(target, nameof(target));
        if (target.IsSurface) throw new InvalidOperationException("surface targets cannot be read back; render offscreen, read back, then draw the target to the surface");
        if (region.IsEmpty || !target.Bounds.Contains(region))
            throw new ArgumentOutOfRangeException(nameof(region), $"region {region} must be non-empty and inside {target.Bounds}");
        return ReadPixelsCore(target, region);
    }

    /// <inheritdoc />
    public bool TryAcquireSurfaceTarget([NotNullWhen(true)] out RenderTarget? target)
    {
        Alive();
        if (_surfaceTarget is { IsDisposed: false }) { target = _surfaceTarget; return true; }
        _surfaceTarget = target = TryAcquireSurfaceTargetCore();
        return target is not null;
    }

    /// <inheritdoc />
    public void Present()
    {
        Alive();
        var target = _surfaceTarget;
        _surfaceTarget = null;
        if (target is null || target.IsDisposed) return;
        PresentCore(target);
    }

    /// <summary>Releases backend objects (waiting for the GPU first). Resources disposed later are ignored by the backend.</summary>
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        DisposeCore([.. _builtIns.Values], [.. _samplers.Values]);
        _builtIns.Clear();
        _samplers.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>For backends that detect loss.</summary>
    protected void RaiseDeviceLost(DeviceLostReason reason) => DeviceLost?.Invoke(reason);

    /// <summary>Backend: create a validated texture.</summary>
    protected abstract Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData);
    /// <summary>Backend: create a validated render target.</summary>
    protected abstract RenderTarget CreateRenderTargetCore(in RenderTargetDescription description);
    /// <summary>Backend: create a validated sampler (core caches it).</summary>
    protected abstract Sampler CreateSamplerCore(in SamplerDescription description);
    /// <summary>Backend: create a validated buffer.</summary>
    protected abstract GpuBuffer CreateBufferCore(in BufferDescription description);
    /// <summary>Backend: create a validated shader (tier 0 is never called: core refuses first).</summary>
    protected abstract Shader CreateShaderCore(in ShaderDescription description);
    /// <summary>Backend: create a validated custom pipeline (tier 0 is never called: core refuses first).</summary>
    protected abstract Pipeline CreatePipelineCore(PipelineDescription description);
    /// <summary>Backend: create a built-in pipeline variant (core caches it).</summary>
    protected abstract Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat);
    /// <summary>Backend: execute a validated, sealed list after everything submitted before it.</summary>
    protected abstract void SubmitCore(CommandList commands);
    /// <summary>Backend: start a validated readback.</summary>
    protected abstract ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region);
    /// <summary>Backend: acquire this frame's surface target or return null.</summary>
    protected abstract RenderTarget? TryAcquireSurfaceTargetCore();
    /// <summary>Backend: present and expire the surface target.</summary>
    protected abstract void PresentCore(RenderTarget surfaceTarget);
    /// <summary>Backend: release everything, the device-owned built-ins and samplers included.</summary>
    protected abstract void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers);

    private void Alive() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    private void Own(GraphicsResource resource, string parameter)
    {
        ArgumentNullException.ThrowIfNull(resource, parameter);
        if (!ReferenceEquals(resource.Device, this)) throw new ArgumentException($"'{resource.Name}' belongs to another device", parameter);
        if (resource.IsDisposed) throw new ObjectDisposedException(resource.GetType().Name, $"'{resource.Name}' is disposed");
    }

    private void Require(GraphicsFeatures feature, string operation)
    {
        if (!Capabilities.Has(feature)) throw new GraphicsCapabilityException(operation, Backend, feature);
    }

    private void RequireFormat(TextureFormat format, TextureUsage usage, string operation)
    {
        if (!Capabilities.Supports(format, usage))
            throw new GraphicsCapabilityException(operation, Backend, GraphicsFeatures.None, $"TextureFormat.{format} as {usage}");
    }

    private static void Size(int width, int height, int max, string parameter)
    {
        if (width < 1 || height < 1 || width > max || height > max)
            throw new ArgumentOutOfRangeException(parameter, $"size {width}x{height} must be within 1..{max}");
    }
}
