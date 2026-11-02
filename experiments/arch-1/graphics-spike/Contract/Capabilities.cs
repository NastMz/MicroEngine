namespace Arch1.Graphics;

/// <summary>Capability tier. Tiers differ only in declared capabilities, never in API shape.</summary>
public enum GraphicsTier
{
    /// <summary>Fixed function: built-in pipelines only (SDL_Renderer, raylib, the CPU reference rasterizer).</summary>
    Tier0,
    /// <summary>Programmable: custom shaders, depth (SDL_GPU on native).</summary>
    Tier1,
}

/// <summary>
/// Optional features. Everything not listed here is mandatory on every tier: textures (Rgba8Unorm sampled and
/// rendered), samplers with clamp addressing, buffers, both built-in pipelines with the four blend presets, scissor,
/// viewport, readback and the surface target. A request that needs an undeclared feature fails with
/// <see cref="GraphicsCapabilityException"/> naming it; there is no silent degradation.
/// </summary>
[Flags]
public enum GraphicsFeatures
{
    /// <summary>No optional feature.</summary>
    None = 0,
    /// <summary>Offscreen render targets (<see cref="IGraphicsDevice.CreateRenderTarget"/>).</summary>
    RenderTargets = 1 << 0,
    /// <summary><see cref="AddressMode.Repeat"/>.</summary>
    AddressRepeat = 1 << 1,
    /// <summary><see cref="AddressMode.Mirror"/>.</summary>
    AddressMirror = 1 << 2,
    /// <summary>Textures with more than one mip level.</summary>
    Mipmaps = 1 << 3,
    /// <summary>Any <see cref="BlendState"/>, not only those in <see cref="GraphicsCapabilities.BuiltInBlendStates"/>.</summary>
    CustomBlend = 1 << 4,
    /// <summary>Custom shaders and pipelines.</summary>
    Shaders = 1 << 5,
    /// <summary>Depth attachments, depth test and write.</summary>
    DepthBuffer = 1 << 6,
    /// <summary>Draws with more than one instance.</summary>
    Instancing = 1 << 7,
    /// <summary>Compute shaders (no operation in this contract version uses it).</summary>
    ComputeShaders = 1 << 8,
    /// <summary>Samplers whose minification and magnification filters differ.</summary>
    SeparateFilters = 1 << 9,
}

/// <summary>When <see cref="IGraphicsDevice.Present"/> waits for the display.</summary>
public enum PresentMode
{
    /// <summary>Wait for vertical blank: no tearing.</summary>
    VSync,
    /// <summary>Do not wait: may tear.</summary>
    Immediate,
    /// <summary>Do not wait, replace the queued frame: no tearing, lowest latency where available.</summary>
    Mailbox,
}

/// <summary>
/// Limits. Each value is a guaranteed minimum: a backend that cannot query its API reports the minimum the API
/// specification guarantees, so a limit may be conservative but never optimistic.
/// </summary>
/// <param name="MaxTextureSize">Largest texture width or height.</param>
/// <param name="MaxRenderTargetSize">Largest render target width or height.</param>
/// <param name="MaxTextureSlots">Texture-sampler slots per fragment stage.</param>
/// <param name="MaxUniformBytes">Bytes per uniform slot (0 on tier 0).</param>
/// <param name="MaxVertexAttributes">Attributes per vertex layout.</param>
public sealed record GraphicsLimits(int MaxTextureSize, int MaxRenderTargetSize, int MaxTextureSlots,
    int MaxUniformBytes, int MaxVertexAttributes);

/// <summary>What a device declares. Read after creation: the backend identity is what the device reports, not what was requested.</summary>
/// <param name="Backend">The factory name ("sdl-renderer", "sdl-gpu", ...); <see cref="GraphicsCapabilityException.Device"/>.</param>
/// <param name="Tier">Tier.</param>
/// <param name="Features">Declared optional features.</param>
/// <param name="ShaderFormats">Accepted shader formats; <see cref="ShaderFormat.None"/> on tier 0.</param>
/// <param name="Formats">Usages each texture format supports; a format absent from the map supports nothing.</param>
/// <param name="BuiltInBlendStates">Blend states the built-in pipelines support; always contains <see cref="BlendState.Presets"/>.</param>
/// <param name="PresentModes">Present modes the surface supports (empty without a surface).</param>
/// <param name="Limits">Limits.</param>
/// <param name="ApiName">Underlying API as reported after creation ("direct3d11", "direct3d12", "vulkan", ...).</param>
/// <param name="Adapter">Adapter name when the API reports one.</param>
public sealed record GraphicsCapabilities(
    string Backend,
    GraphicsTier Tier,
    GraphicsFeatures Features,
    ShaderFormat ShaderFormats,
    IReadOnlyDictionary<TextureFormat, TextureUsage> Formats,
    IReadOnlySet<BlendState> BuiltInBlendStates,
    IReadOnlySet<PresentMode> PresentModes,
    GraphicsLimits Limits,
    string ApiName,
    string? Adapter)
{
    /// <summary>True when every flag of <paramref name="features"/> is declared.</summary>
    public bool Has(GraphicsFeatures features) => (Features & features) == features;

    /// <summary>True when <paramref name="format"/> supports every flag of <paramref name="usage"/>.</summary>
    public bool Supports(TextureFormat format, TextureUsage usage)
        => Formats.TryGetValue(format, out var supported) && (supported & usage) == usage;
}

/// <summary>
/// The one typed error for a missing capability: names the operation, the device and what it lacks. Thrown at
/// creation or recording time, never deferred to a draw.
/// </summary>
public sealed class GraphicsCapabilityException : NotSupportedException
{
    /// <summary>Creates the error.</summary>
    /// <param name="operation">The operation, e.g. "CreatePipeline(depth)".</param>
    /// <param name="device">The backend name (<see cref="GraphicsCapabilities.Backend"/>).</param>
    /// <param name="missing">The undeclared feature, or <see cref="GraphicsFeatures.None"/> when the requirement is not a feature (a format, shader format or present mode: see <paramref name="requirement"/>).</param>
    /// <param name="requirement">What exactly is missing, e.g. "TextureFormat.D24UnormS8 as DepthStencil".</param>
    public GraphicsCapabilityException(string operation, string device, GraphicsFeatures missing, string? requirement = null)
        : base($"{operation} on '{device}' needs {(missing != GraphicsFeatures.None ? missing.ToString() : requirement)}"
               + (missing != GraphicsFeatures.None && requirement is not null ? $" ({requirement})" : ""))
    {
        Operation = operation;
        Device = device;
        Missing = missing;
        Requirement = requirement;
    }

    /// <summary>The refused operation.</summary>
    public string Operation { get; }

    /// <summary>The backend name.</summary>
    public string Device { get; }

    /// <summary>The undeclared feature, or <see cref="GraphicsFeatures.None"/> for a non-feature requirement.</summary>
    public GraphicsFeatures Missing { get; }

    /// <summary>The non-feature requirement (format and usage, shader format, present mode), when there is one.</summary>
    public string? Requirement { get; }
}
