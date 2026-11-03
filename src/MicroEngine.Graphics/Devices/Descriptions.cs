namespace MicroEngine.Graphics.Devices;

/// <summary>
/// Texel formats. Whether a format can be used for a purpose is a per-format, per-usage capability:
/// <see cref="GraphicsCapabilities.Supports(TextureFormat, TextureUsage)"/> (depth formats in particular differ between
/// adapters: AMD Vulkan has no 24-bit depth). Using an unsupported combination throws
/// <see cref="GraphicsCapabilityException"/>.
/// </summary>
public enum TextureFormat
{
    /// <summary>8-bit RGBA, bytes in R, G, B, A order. Supported for sampling and rendering on every tier.</summary>
    Rgba8Unorm,
    /// <summary>8-bit RGBA with sRGB encoding (sampling decodes, rendering encodes).</summary>
    Rgba8UnormSrgb,
    /// <summary>8-bit BGRA, bytes in B, G, R, A order (the usual swapchain format).</summary>
    Bgra8Unorm,
    /// <summary>One 8-bit channel.</summary>
    R8Unorm,
    /// <summary>Two 8-bit channels.</summary>
    Rg8Unorm,
    /// <summary>16-bit float RGBA.</summary>
    Rgba16Float,
    /// <summary>BC7 block compression (16 bytes per 4×4 block).</summary>
    Bc7,
    /// <summary>ETC2 RGBA8 block compression (16 bytes per 4×4 block).</summary>
    Etc2Rgba8,
    /// <summary>ASTC 4×4 block compression (16 bytes per 4×4 block).</summary>
    Astc4x4,
    /// <summary>16-bit normalised depth.</summary>
    D16Unorm,
    /// <summary>24-bit normalised depth with 8-bit stencil.</summary>
    D24UnormS8,
    /// <summary>32-bit float depth.</summary>
    D32Float,
}

/// <summary>What a texture of a given format may be used for; queried per format.</summary>
[Flags]
public enum TextureUsage
{
    /// <summary>No usage.</summary>
    None = 0,
    /// <summary>Sampled by pipelines (<see cref="CommandList.SetTexture"/>).</summary>
    Sampled = 1,
    /// <summary>Colour attachment of a pass.</summary>
    RenderTarget = 2,
    /// <summary>Depth(-stencil) attachment of a pass.</summary>
    DepthStencil = 4,
}

/// <summary>How the colour channels of texel data relate to alpha.</summary>
public enum AlphaMode
{
    /// <summary>Colour already multiplied by alpha: the engine-wide default, what the built-in blend states assume.</summary>
    Premultiplied,
    /// <summary>Colour not multiplied by alpha. The device does not convert: content tools premultiply on import.</summary>
    Straight,
}

/// <summary>Byte-size rules of texture formats, shared by validation and backends.</summary>
public static class TextureFormats
{
    /// <summary>True for <see cref="TextureFormat.D16Unorm"/>, <see cref="TextureFormat.D24UnormS8"/>, <see cref="TextureFormat.D32Float"/>.</summary>
    public static bool IsDepth(TextureFormat format) => format is TextureFormat.D16Unorm or TextureFormat.D24UnormS8 or TextureFormat.D32Float;

    /// <summary>True for the 4×4 block-compressed formats.</summary>
    public static bool IsCompressed(TextureFormat format) => format is TextureFormat.Bc7 or TextureFormat.Etc2Rgba8 or TextureFormat.Astc4x4;

    /// <summary>
    /// Bytes of tightly packed data for a <paramref name="width"/>×<paramref name="height"/> region (rows top to bottom,
    /// no padding; compressed formats count whole 4×4 blocks). Depth formats have no CPU layout and throw.
    /// </summary>
    public static int ByteCount(TextureFormat format, int width, int height) => format switch
    {
        TextureFormat.Rgba8Unorm or TextureFormat.Rgba8UnormSrgb or TextureFormat.Bgra8Unorm => width * height * 4,
        TextureFormat.R8Unorm => width * height,
        TextureFormat.Rg8Unorm => width * height * 2,
        TextureFormat.Rgba16Float => width * height * 8,
        TextureFormat.Bc7 or TextureFormat.Etc2Rgba8 or TextureFormat.Astc4x4 => ((width + 3) / 4) * ((height + 3) / 4) * 16,
        _ => throw new ArgumentException($"{format} has no CPU data layout", nameof(format)),
    };
}

/// <summary>A sampled texture. Texture data uploads go through <see cref="IGraphicsDevice.CreateTexture"/> and <see cref="CommandList.UpdateTexture"/>.</summary>
/// <param name="Width">Width in texels, 1..<see cref="GraphicsLimits.MaxTextureSize"/>.</param>
/// <param name="Height">Height in texels, 1..<see cref="GraphicsLimits.MaxTextureSize"/>.</param>
/// <param name="Format">Texel format; must support <see cref="TextureUsage.Sampled"/>.</param>
/// <param name="MipLevels">Mip levels; more than 1 requires <see cref="GraphicsFeatures.Mipmaps"/>.</param>
/// <param name="Alpha">Whether the data is premultiplied (default) or straight; informational for the device.</param>
/// <param name="Name">Debug name.</param>
public readonly record struct TextureDescription(int Width, int Height, TextureFormat Format = TextureFormat.Rgba8Unorm,
    int MipLevels = 1, AlphaMode Alpha = AlphaMode.Premultiplied, string? Name = null);

/// <summary>An offscreen target: one colour texture that can also be sampled, and an optional depth(-stencil) texture.</summary>
/// <param name="Width">Width in pixels, 1..<see cref="GraphicsLimits.MaxRenderTargetSize"/>.</param>
/// <param name="Height">Height in pixels, 1..<see cref="GraphicsLimits.MaxRenderTargetSize"/>.</param>
/// <param name="Format">Colour format; must support <see cref="TextureUsage.RenderTarget"/> and <see cref="TextureUsage.Sampled"/>.</param>
/// <param name="Depth">Depth format, or null; requires <see cref="GraphicsFeatures.DepthBuffer"/> and <see cref="TextureUsage.DepthStencil"/> support for the format.</param>
/// <param name="Name">Debug name.</param>
public readonly record struct RenderTargetDescription(int Width, int Height, TextureFormat Format = TextureFormat.Rgba8Unorm,
    TextureFormat? Depth = null, string? Name = null);

/// <summary>Texel filter.</summary>
public enum Filter
{
    /// <summary>The texel whose area contains the sample point.</summary>
    Nearest,
    /// <summary>Bilinear interpolation of the four nearest texel centres.</summary>
    Linear,
}

/// <summary>What sampling does outside 0..1.</summary>
public enum AddressMode
{
    /// <summary>Clamp to the edge texel. Every tier.</summary>
    Clamp,
    /// <summary>Wrap around; requires <see cref="GraphicsFeatures.AddressRepeat"/>.</summary>
    Repeat,
    /// <summary>Mirror every other repetition; requires <see cref="GraphicsFeatures.AddressMirror"/>.</summary>
    Mirror,
}

/// <summary>
/// Immutable sampler state, a value: <see cref="IGraphicsDevice.GetSampler"/> returns a cached device object for it.
/// Sampling is never texture state. <paramref name="Min"/> different from <paramref name="Mag"/> requires
/// <see cref="GraphicsFeatures.SeparateFilters"/>. <paramref name="Mip"/> applies only to textures with more than one
/// mip level and is otherwise ignored (so <see cref="LinearClamp"/> works on every tier).
/// </summary>
/// <param name="Min">Minification filter.</param>
/// <param name="Mag">Magnification filter.</param>
/// <param name="Mip">Filter between mip levels.</param>
/// <param name="U">Horizontal address mode.</param>
/// <param name="V">Vertical address mode.</param>
public readonly record struct SamplerDescription(Filter Min = Filter.Nearest, Filter Mag = Filter.Nearest,
    Filter Mip = Filter.Nearest, AddressMode U = AddressMode.Clamp, AddressMode V = AddressMode.Clamp)
{
    /// <summary>Nearest, clamped: pixel art and exact copies.</summary>
    public static readonly SamplerDescription PointClamp = new();

    /// <summary>Linear, clamped.</summary>
    public static readonly SamplerDescription LinearClamp = new(Filter.Linear, Filter.Linear, Filter.Linear);
}

/// <summary>Blend factor (the set every tier-0 and tier-1 backend can express).</summary>
public enum BlendFactor
{
    /// <summary>0.</summary>
    Zero,
    /// <summary>1.</summary>
    One,
    /// <summary>Source colour.</summary>
    SrcColor,
    /// <summary>1 − source colour.</summary>
    InvSrcColor,
    /// <summary>Source alpha.</summary>
    SrcAlpha,
    /// <summary>1 − source alpha.</summary>
    InvSrcAlpha,
    /// <summary>Destination colour.</summary>
    DstColor,
    /// <summary>1 − destination colour.</summary>
    InvDstColor,
    /// <summary>Destination alpha.</summary>
    DstAlpha,
    /// <summary>1 − destination alpha.</summary>
    InvDstAlpha,
}

/// <summary>Blend operation.</summary>
public enum BlendOp
{
    /// <summary>src·f + dst·g.</summary>
    Add,
    /// <summary>src·f − dst·g.</summary>
    Subtract,
    /// <summary>dst·g − src·f.</summary>
    ReverseSubtract,
    /// <summary>min(src, dst) (factors ignored).</summary>
    Min,
    /// <summary>max(src, dst) (factors ignored).</summary>
    Max,
}

/// <summary>
/// Fixed-function blending: <c>result = src·SrcFactor (op) dst·DstFactor</c>, separately for colour and alpha, computed
/// in float and quantised to the target format. The presets assume premultiplied colours.
/// </summary>
/// <param name="SrcColor">Factor applied to the source colour.</param>
/// <param name="DstColor">Factor applied to the destination colour.</param>
/// <param name="ColorOp">Colour operation.</param>
/// <param name="SrcAlpha">Factor applied to the source alpha.</param>
/// <param name="DstAlpha">Factor applied to the destination alpha.</param>
/// <param name="AlphaOp">Alpha operation.</param>
public sealed record BlendState(BlendFactor SrcColor, BlendFactor DstColor, BlendOp ColorOp,
    BlendFactor SrcAlpha, BlendFactor DstAlpha, BlendOp AlphaOp)
{
    /// <summary>Replace: src.</summary>
    public static readonly BlendState Opaque = new(BlendFactor.One, BlendFactor.Zero, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, BlendOp.Add);

    /// <summary>Premultiplied "over", the default: src + dst·(1 − srcA) for colour and alpha.</summary>
    public static readonly BlendState AlphaPremultiplied = new(BlendFactor.One, BlendFactor.InvSrcAlpha, BlendOp.Add, BlendFactor.One, BlendFactor.InvSrcAlpha, BlendOp.Add);

    /// <summary>Additive light: colour src + dst; destination alpha kept.</summary>
    public static readonly BlendState Additive = new(BlendFactor.One, BlendFactor.One, BlendOp.Add, BlendFactor.Zero, BlendFactor.One, BlendOp.Add);

    /// <summary>Premultiplied multiply: colour src·dst + dst·(1 − srcA); destination alpha kept.</summary>
    public static readonly BlendState Multiply = new(BlendFactor.DstColor, BlendFactor.InvSrcAlpha, BlendOp.Add, BlendFactor.Zero, BlendFactor.One, BlendOp.Add);

    /// <summary>The four presets. Every tier supports them with every built-in pipeline (they are the minimum of <see cref="GraphicsCapabilities.BuiltInBlendStates"/>).</summary>
    public static IReadOnlyList<BlendState> Presets { get; } = [Opaque, AlphaPremultiplied, Additive, Multiply];
}

/// <summary>Buffer usage. Uniform data is not a buffer: it is pushed with <see cref="CommandList.SetUniforms(ShaderStage, int, ReadOnlySpan{byte})"/>.</summary>
[Flags]
public enum BufferUsage
{
    /// <summary>Bound with <see cref="CommandList.SetVertices"/>.</summary>
    Vertex = 1,
    /// <summary>Bound with <see cref="CommandList.SetIndices"/>.</summary>
    Index = 2,
}

/// <summary>
/// A buffer, available on every tier (tier 0: device-owned CPU memory submitted to the backend's geometry call; tier 1:
/// GPU memory). Contents are written only by <see cref="CommandList.UpdateBuffer"/> and are undefined until written.
/// </summary>
/// <param name="SizeInBytes">Size, at least 1.</param>
/// <param name="Usage">Vertex and/or index.</param>
/// <param name="Name">Debug name.</param>
public readonly record struct BufferDescription(int SizeInBytes, BufferUsage Usage, string? Name = null);

/// <summary>Index element size.</summary>
public enum IndexSize
{
    /// <summary>16-bit indices.</summary>
    Bits16 = 2,
    /// <summary>32-bit indices.</summary>
    Bits32 = 4,
}

/// <summary>Programmable stage.</summary>
public enum ShaderStage
{
    /// <summary>Vertex stage.</summary>
    Vertex,
    /// <summary>Fragment stage.</summary>
    Fragment,
}

/// <summary>Offline-compiled shader bytecode formats (produced by the tool from one HLSL source).</summary>
[Flags]
public enum ShaderFormat
{
    /// <summary>No shader format (tier 0).</summary>
    None = 0,
    /// <summary>SPIR-V (Vulkan).</summary>
    SpirV = 1,
    /// <summary>DXIL (Direct3D 12).</summary>
    Dxil = 2,
    /// <summary>Metal Shading Language source (Metal).</summary>
    Msl = 4,
}

/// <summary>
/// Resource counts of a shader, produced by the offline tool's reflection and validated at creation. Bindings follow
/// the SDL_GPU register-space rules (vertex uniforms space1; fragment samplers space2, fragment uniforms space3).
/// </summary>
/// <param name="Samplers">Texture-sampler pairs, slots 0..n-1.</param>
/// <param name="UniformBuffers">Uniform slots, 0..n-1.</param>
/// <param name="StorageTextures">Storage textures (reserved; must be 0 in this contract version).</param>
/// <param name="StorageBuffers">Storage buffers (reserved; must be 0 in this contract version).</param>
public readonly record struct ShaderResources(int Samplers = 0, int UniformBuffers = 0, int StorageTextures = 0, int StorageBuffers = 0);

/// <summary>A custom shader; requires <see cref="GraphicsFeatures.Shaders"/> and a <see cref="Format"/> in <see cref="GraphicsCapabilities.ShaderFormats"/>.</summary>
/// <param name="Stage">Stage.</param>
/// <param name="Format">One bytecode format the device declares.</param>
/// <param name="Code">Bytecode (or MSL source) produced offline.</param>
/// <param name="EntryPoint">Entry point name ("main"; "main0" for MSL generated by SPIRV-Cross).</param>
/// <param name="Resources">Reflected resource counts.</param>
public readonly record struct ShaderDescription(ShaderStage Stage, ShaderFormat Format, ReadOnlyMemory<byte> Code,
    string EntryPoint, ShaderResources Resources);

/// <summary>Depth comparison.</summary>
public enum CompareOp
{
    /// <summary>Never passes.</summary>
    Never,
    /// <summary>Passes when the new depth is less.</summary>
    Less,
    /// <summary>Passes when equal.</summary>
    Equal,
    /// <summary>Passes when less or equal.</summary>
    LessOrEqual,
    /// <summary>Passes when greater.</summary>
    Greater,
    /// <summary>Passes when not equal.</summary>
    NotEqual,
    /// <summary>Passes when greater or equal.</summary>
    GreaterOrEqual,
    /// <summary>Always passes.</summary>
    Always,
}

/// <summary>Depth test and write. Depth is 0 (near) .. 1 (far); anything but <see cref="None"/> requires <see cref="GraphicsFeatures.DepthBuffer"/>.</summary>
/// <param name="Test">Compare against the depth attachment.</param>
/// <param name="Write">Write passing depths.</param>
/// <param name="Compare">The comparison when <paramref name="Test"/> is set.</param>
public readonly record struct DepthState(bool Test, bool Write, CompareOp Compare)
{
    /// <summary>No depth test, no depth write.</summary>
    public static readonly DepthState None = new(false, false, CompareOp.Always);

    /// <summary>Test less, write: the usual opaque 3D state.</summary>
    public static readonly DepthState LessWrite = new(true, true, CompareOp.Less);
}

/// <summary>Face culling.</summary>
public enum CullMode
{
    /// <summary>Draw both faces.</summary>
    None,
    /// <summary>Cull front faces.</summary>
    Front,
    /// <summary>Cull back faces.</summary>
    Back,
}

/// <summary>Front-face winding in framebuffer space (y up in clip space). The engine convention is counter-clockwise.</summary>
public enum FrontFace
{
    /// <summary>Counter-clockwise triangles are front faces (the engine convention).</summary>
    CounterClockwise,
    /// <summary>Clockwise triangles are front faces.</summary>
    Clockwise,
}

/// <summary>Rasterizer state. Scissor is not here: it is dynamic pass state, always on (<see cref="CommandList.SetScissor"/>).</summary>
/// <param name="Cull">Culling.</param>
/// <param name="Front">Front-face winding.</param>
public readonly record struct RasterState(CullMode Cull = CullMode.None, FrontFace Front = FrontFace.CounterClockwise);

/// <summary>Vertex attribute element format.</summary>
public enum VertexFormat
{
    /// <summary>One float.</summary>
    Float1,
    /// <summary>Two floats.</summary>
    Float2,
    /// <summary>Three floats.</summary>
    Float3,
    /// <summary>Four floats.</summary>
    Float4,
    /// <summary>Four normalised bytes.</summary>
    Byte4Norm,
}

/// <summary>One vertex attribute.</summary>
/// <param name="Location">Shader input location (HLSL <c>TEXCOORD&lt;location&gt;</c>).</param>
/// <param name="Format">Element format.</param>
/// <param name="Offset">Byte offset in the vertex.</param>
public readonly record struct VertexAttribute(int Location, VertexFormat Format, int Offset);

/// <summary>Interleaved vertex layout of one vertex buffer (slot 0).</summary>
/// <param name="Stride">Bytes per vertex.</param>
/// <param name="Attributes">Attributes, at most <see cref="GraphicsLimits.MaxVertexAttributes"/>.</param>
public sealed record VertexLayout(int Stride, IReadOnlyList<VertexAttribute> Attributes)
{
    /// <summary>The layout of <see cref="Devices.Vertex2D"/>: position Float2 @0, texcoord Float2 @8, colour Float4 @16, stride 32.</summary>
    public static readonly VertexLayout Vertex2D = new(Devices.Vertex2D.SizeInBytes,
        [new(0, VertexFormat.Float2, 0), new(1, VertexFormat.Float2, 8), new(2, VertexFormat.Float4, 16)]);
}

/// <summary>
/// A custom pipeline: requires <see cref="GraphicsFeatures.Shaders"/>. The pipeline is bound to the attachment formats
/// it renders to: a pass accepts it only when its colour format equals <paramref name="ColorFormat"/> and its depth
/// attachment format equals <paramref name="DepthFormat"/> (null = the pass has no depth attachment).
/// </summary>
/// <param name="Vertex">Vertex shader.</param>
/// <param name="Fragment">Fragment shader.</param>
/// <param name="Layout">Vertex layout.</param>
/// <param name="Blend">Blend state (any state: tier 1 declares <see cref="GraphicsFeatures.CustomBlend"/>).</param>
/// <param name="Depth">Depth state; anything but <see cref="DepthState.None"/> requires a <paramref name="DepthFormat"/>.</param>
/// <param name="Raster">Rasterizer state.</param>
/// <param name="ColorFormat">Format of the colour attachment.</param>
/// <param name="DepthFormat">Format of the depth attachment, or null.</param>
public sealed record PipelineDescription(Shader Vertex, Shader Fragment, VertexLayout Layout, BlendState Blend,
    DepthState Depth, RasterState Raster, TextureFormat ColorFormat, TextureFormat? DepthFormat);

/// <summary>
/// The built-in pipeline every tier implements with every blend state of
/// <see cref="GraphicsCapabilities.BuiltInBlendStates"/>: <see cref="Vertex2D"/> vertices, one texture in slot 0, no
/// uniforms, no depth and no culling; colour = sample(texture) × vertex colour.
/// </summary>
public enum BuiltInPipeline
{
    /// <summary>Textured triangles: sprites, quads and arbitrary geometry (an untextured draw binds a white texel).</summary>
    Textured,
}

/// <summary>What a pass does with an attachment's previous contents.</summary>
public enum LoadOp
{
    /// <summary>Keep the previous contents.</summary>
    Load,
    /// <summary>Fill with the clear value; ignores viewport and scissor.</summary>
    Clear,
    /// <summary>Contents undefined at pass start.</summary>
    DontCare,
}

/// <summary>What a pass does with an attachment's contents at its end.</summary>
public enum StoreOp
{
    /// <summary>Keep the rendered contents.</summary>
    Store,
    /// <summary>Contents undefined after the pass (transient depth).</summary>
    DontCare,
}

/// <summary>The colour attachment of a pass.</summary>
/// <param name="Target">The target; its <see cref="RenderTarget.ColorFormat"/> must match the bound pipeline.</param>
/// <param name="Load">Load operation.</param>
/// <param name="Store">Store operation.</param>
/// <param name="Clear">Clear value when <paramref name="Load"/> is <see cref="LoadOp.Clear"/> (premultiplied).</param>
public readonly record struct ColorAttachment(RenderTarget Target, LoadOp Load = LoadOp.Load, StoreOp Store = StoreOp.Store, ColorF Clear = default);

/// <summary>The depth attachment of a pass: the depth texture of a target created with <see cref="RenderTargetDescription.Depth"/>.</summary>
/// <param name="Target">The target owning the depth texture (same size as the colour attachment).</param>
/// <param name="Load">Load operation.</param>
/// <param name="Store">Store operation.</param>
/// <param name="ClearDepth">Clear depth when <paramref name="Load"/> is <see cref="LoadOp.Clear"/> (0..1).</param>
public readonly record struct DepthAttachment(RenderTarget Target, LoadOp Load = LoadOp.Clear, StoreOp Store = StoreOp.DontCare, float ClearDepth = 1);

/// <summary>
/// A render pass: one colour attachment and an optional depth attachment (several colour attachments are a later,
/// additive extension). Viewport and scissor start as the whole colour target; bindings start empty.
/// </summary>
/// <param name="Color">Colour attachment.</param>
/// <param name="Depth">Depth attachment, or null.</param>
public readonly record struct PassDescription(ColorAttachment Color, DepthAttachment? Depth = null);
