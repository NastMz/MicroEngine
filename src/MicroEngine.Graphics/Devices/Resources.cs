namespace MicroEngine.Graphics.Devices;

/// <summary>
/// Base of every device object. A resource belongs to the device that created it: using it with another device is an
/// <see cref="ArgumentException"/>, using it after <see cref="Dispose"/> an <see cref="ObjectDisposedException"/>, both
/// raised by core validation before a backend sees the call. Every resource has a <see cref="Handle"/> with a
/// generation: once the resource is disposed the handle no longer resolves
/// (<see cref="GraphicsDeviceBase.TryGetResource{T}"/>), even if its slot is reused. Destruction is deferred: the
/// backend object is released only when no submitted work uses it any more (at once on devices that execute command
/// lists at <see cref="IGraphicsDevice.Submit"/>).
/// </summary>
public abstract class GraphicsResource : IDisposable
{
    /// <summary>Creates a resource owned by <paramref name="device"/>.</summary>
    protected GraphicsResource(IGraphicsDevice device, string? name)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        Name = name;
    }

    /// <summary>The owning device.</summary>
    public IGraphicsDevice Device { get; }

    /// <summary>Debug name.</summary>
    public string? Name { get; }

    /// <summary>
    /// The resource's handle on its device: a slot and a generation. <see cref="Kernel.Handle{T}.None"/> until the device
    /// registers the resource (core does it when the create call returns) and after a device that is not a
    /// <see cref="GraphicsDeviceBase"/> created it.
    /// </summary>
    public Kernel.Handle<GraphicsResource> Handle { get; internal set; }

    /// <summary>True after <see cref="Dispose"/> (or, for a surface target, after <see cref="IGraphicsDevice.Present"/>).</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>True when a backend that detects device loss marked the resource lost; restorable resources are re-uploaded by their owner.</summary>
    public bool IsLost { get; private set; }

    /// <summary>
    /// When not null, <see cref="Dispose"/> throws <see cref="InvalidOperationException"/> with this text: device-owned
    /// objects (built-in pipelines, a render target's colour texture, the surface target) are not disposed by callers.
    /// </summary>
    protected virtual string? OwnedBy => null;

    /// <summary>Releases the resource (deferred until submitted work no longer uses it). Idempotent.</summary>
    /// <exception cref="InvalidOperationException">The resource is owned by its device or its render target.</exception>
    public void Dispose()
    {
        if (IsDisposed) return;
        if (OwnedBy is { } owner) throw new InvalidOperationException($"{GetType().Name} '{Name}' is owned by {owner} and cannot be disposed by callers");
        End();
        GC.SuppressFinalize(this);
    }

    /// <summary>Backend release: called once, when no submitted work uses the resource any more.</summary>
    protected abstract void Release();

    /// <summary>Lets a backend end the life of an owned object (a surface target at present, a target's colour texture with its target).</summary>
    protected void Expire()
    {
        if (IsDisposed) return;
        End();
    }

    /// <summary>Lets a backend that detects device loss mark the resource lost.</summary>
    protected void MarkLost() => IsLost = true;

    private void End()
    {
        IsDisposed = true;
        if (Device is GraphicsDeviceBase core) core.Retire(this);
        else Release();
    }

    /// <summary>Core: runs the backend release once the device says no submitted work uses the resource.</summary>
    internal void ReleaseNow() => Release();
}

/// <summary>A texture that pipelines sample.</summary>
public abstract class Texture : GraphicsResource
{
    /// <summary>Creates the texture object.</summary>
    protected Texture(IGraphicsDevice device, in TextureDescription description) : base(device, description.Name) => Description = description;

    /// <summary>The creation description (for a render target's colour texture: its size and format).</summary>
    public TextureDescription Description { get; }

    /// <summary>Width in texels.</summary>
    public int Width => Description.Width;

    /// <summary>Height in texels.</summary>
    public int Height => Description.Height;

    /// <summary>Texel format.</summary>
    public TextureFormat Format => Description.Format;

    /// <summary>The render target this texture is the colour of, or null for an ordinary texture.</summary>
    public virtual RenderTarget? OwnerTarget => null;
}

/// <summary>
/// A colour attachment (plus optional depth) that passes render to. Offscreen targets come from
/// <see cref="IGraphicsDevice.CreateRenderTarget"/> and can be sampled through <see cref="Color"/> and read back. The
/// surface target comes from <see cref="IGraphicsDevice.TryAcquireSurfaceTarget"/>, is valid until the next
/// <see cref="IGraphicsDevice.Present"/>, cannot be sampled and cannot be read back.
/// </summary>
public abstract class RenderTarget : GraphicsResource
{
    /// <summary>Creates the target object.</summary>
    protected RenderTarget(IGraphicsDevice device, in RenderTargetDescription description, bool isSurface)
        : base(device, description.Name)
    {
        Description = description;
        IsSurface = isSurface;
    }

    /// <summary>Size and formats.</summary>
    public RenderTargetDescription Description { get; }

    /// <summary>True for the surface (back buffer) target.</summary>
    public bool IsSurface { get; }

    /// <summary>Width in pixels.</summary>
    public int Width => Description.Width;

    /// <summary>Height in pixels.</summary>
    public int Height => Description.Height;

    /// <summary>Colour format: what a pipeline drawing into it must declare.</summary>
    public TextureFormat ColorFormat => Description.Format;

    /// <summary>Depth format, or null when the target has no depth texture.</summary>
    public TextureFormat? DepthFormat => Description.Depth;

    /// <summary>The colour texture for sampling; null for the surface target. Owned by the target.</summary>
    public abstract Texture? Color { get; }

    /// <summary>The whole target as a rectangle.</summary>
    public RectI Bounds => new(0, 0, Width, Height);
}

/// <summary>Device-owned, cached sampler object for one <see cref="SamplerDescription"/> (never disposed by callers).</summary>
public abstract class Sampler
{
    /// <summary>Creates the sampler object.</summary>
    protected Sampler(IGraphicsDevice device, in SamplerDescription description)
    {
        Device = device;
        Description = description;
    }

    /// <summary>The owning device.</summary>
    public IGraphicsDevice Device { get; }

    /// <summary>The sampler state.</summary>
    public SamplerDescription Description { get; }
}

/// <summary>A vertex and/or index buffer.</summary>
public abstract class GpuBuffer : GraphicsResource
{
    /// <summary>Creates the buffer object.</summary>
    protected GpuBuffer(IGraphicsDevice device, in BufferDescription description) : base(device, description.Name) => Description = description;

    /// <summary>Size and usage.</summary>
    public BufferDescription Description { get; }

    /// <summary>Size in bytes.</summary>
    public int SizeInBytes => Description.SizeInBytes;

    /// <summary>Usage flags.</summary>
    public BufferUsage Usage => Description.Usage;
}

/// <summary>A custom shader stage.</summary>
public abstract class Shader : GraphicsResource
{
    /// <summary>Creates the shader object.</summary>
    protected Shader(IGraphicsDevice device, in ShaderDescription description) : base(device, null)
    {
        Stage = description.Stage;
        Format = description.Format;
        Resources = description.Resources;
    }

    /// <summary>Stage.</summary>
    public ShaderStage Stage { get; }

    /// <summary>Bytecode format.</summary>
    public ShaderFormat Format { get; }

    /// <summary>Reflected resource counts (validated against bindings at draw recording).</summary>
    public ShaderResources Resources { get; }
}

/// <summary>A graphics pipeline: built-in (device-owned) or custom.</summary>
public abstract class Pipeline : GraphicsResource
{
    /// <summary>Creates the pipeline object.</summary>
    protected Pipeline(IGraphicsDevice device, BuiltInPipeline? builtIn, VertexLayout layout, BlendState blend,
        TextureFormat colorFormat, TextureFormat? depthFormat, ShaderResources vertexResources, ShaderResources fragmentResources)
        : base(device, builtIn?.ToString())
    {
        BuiltIn = builtIn;
        Layout = layout;
        Blend = blend;
        ColorFormat = colorFormat;
        DepthFormat = depthFormat;
        VertexResources = vertexResources;
        FragmentResources = fragmentResources;
    }

    /// <summary>The built-in kind, or null for a custom pipeline.</summary>
    public BuiltInPipeline? BuiltIn { get; }

    /// <summary>Vertex layout (<see cref="VertexLayout.Vertex2D"/> for built-ins).</summary>
    public VertexLayout Layout { get; }

    /// <summary>Blend state.</summary>
    public BlendState Blend { get; }

    /// <summary>Colour attachment format the pipeline renders to.</summary>
    public TextureFormat ColorFormat { get; }

    /// <summary>Depth attachment format, or null for passes without depth.</summary>
    public TextureFormat? DepthFormat { get; }

    /// <summary>Vertex-stage resources (built-ins: none visible to callers).</summary>
    public ShaderResources VertexResources { get; }

    /// <summary>Fragment-stage resources (built-ins: one sampler).</summary>
    public ShaderResources FragmentResources { get; }

    /// <inheritdoc />
    protected override string? OwnedBy => BuiltIn is null ? null : "its device (built-in pipeline)";
}

/// <summary>
/// An asynchronous read of a render target region, completed by a fence. The data are the pixels of every command
/// list submitted before <see cref="IGraphicsDevice.ReadPixels"/>: rows top to bottom, tightly packed, in the target's
/// colour format.
/// </summary>
public abstract class ReadbackRequest
{
    private byte[]? _pixels;

    /// <summary>Creates the request.</summary>
    protected ReadbackRequest(RectI region, TextureFormat format)
    {
        Region = region;
        Format = format;
    }

    /// <summary>The region read.</summary>
    public RectI Region { get; }

    /// <summary>The pixel format of <see cref="Pixels"/>.</summary>
    public TextureFormat Format { get; }

    /// <summary>True once the GPU finished the copy (polls the fence; never blocks).</summary>
    public abstract bool IsCompleted { get; }

    /// <summary>Blocks until the copy finished.</summary>
    public abstract void Wait();

    /// <summary>The pixels; <see cref="InvalidOperationException"/> before <see cref="IsCompleted"/>.</summary>
    public ReadOnlySpan<byte> Pixels
    {
        get
        {
            if (_pixels is null)
            {
                if (!IsCompleted) throw new InvalidOperationException("the readback has not completed; call Wait() or poll IsCompleted");
                _pixels = Collect();
            }
            return _pixels;
        }
    }

    /// <summary>Backend: copies the completed data out and releases the staging memory (called once).</summary>
    protected abstract byte[] Collect();
}
