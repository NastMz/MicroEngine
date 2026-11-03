using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using MicroEngine.Graphics.Devices;
using MicroEngine.Platform;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Graphics.SdlGpu;

/// <summary>Factory of the tier-1 SDL_GPU backend ("sdl-gpu"): works with an SDL window surface or offscreen (no surface).</summary>
public sealed unsafe class SdlGpuBackendFactory : IGraphicsBackendFactory
{
    /// <inheritdoc />
    public string Name => SdlGpuDevice.BackendName;

    /// <inheritdoc />
    public string? UnsupportedReason(ISurface? surface)
    {
        // The same properties as creation: SDL's default would require hardware acceleration and refuse lavapipe.
        var props = SdlGpuDevice.CreationProperties(new GraphicsDeviceOptions());
        bool supported = SDL_GPUSupportsProperties(props);
        SDL_DestroyProperties(props);
        if (!supported) return $"no SDL_GPU driver for DXIL, SPIR-V or MSL on this system ({SDL_GetError()})";
        return surface is not null && surface.NativeWindow == 0 ? "SDL_GPU presents only to an SDL window surface (or renders offscreen without one)" : null;
    }

    /// <inheritdoc />
    public IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options)
    {
        if (UnsupportedReason(surface) is { } reason) throw new NotSupportedException(reason);
        return new SdlGpuDevice(surface, options);
    }
}

/// <summary>
/// The tier-1 graphics device on pure SDL_GPU (not SDL_Renderer's gpu driver): Direct3D 12, Vulkan or Metal, as SDL
/// chooses or <see cref="GraphicsDeviceOptions.Api"/> asks ("direct3d12", "vulkan", "metal"); the API in use is what
/// <see cref="GraphicsCapabilities.ApiName"/> reports after creation, because SDL's driver hint overrides a request.
/// <para>One SDL command buffer per submitted list: updates go to copy passes and passes to render passes, in recording
/// order. The built-in <see cref="BuiltInPipeline.Textured"/> pipeline is the device's own (offline-compiled sprite
/// shaders) and pushes the viewport-to-clip projection itself, restoring a custom pipeline's vertex uniform slot 0
/// afterwards, so uniform data persist for the rest of the list as the contract says. The surface target is a
/// device-owned texture in the swapchain format, copied to the swapchain at <see cref="IGraphicsDevice.Present"/>, so a
/// list that renders to the surface executes at submit like any other.</para>
/// <para>Declared features: render targets, repeat and mirror addressing, custom blend, shaders, depth, instancing and
/// separate filters; shader formats as SDL reports them; texture formats per usage from
/// <c>SDL_GPUTextureSupportsFormat</c> (24-bit depth is missing on some adapters). Limits are the minimums SDL_GPU and
/// the API guarantee. Software rasterizers (Mesa lavapipe) are accepted; <see cref="GraphicsCapabilities.Adapter"/>
/// names the adapter.</para>
/// </summary>
public sealed unsafe class SdlGpuDevice : GraphicsDeviceBase
{
    /// <summary>The backend name.</summary>
    public const string BackendName = "sdl-gpu";

    /// <summary>The optional features this device declares on every API.</summary>
    public const GraphicsFeatures DeclaredFeatures = GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat | GraphicsFeatures.AddressMirror
        | GraphicsFeatures.CustomBlend | GraphicsFeatures.Shaders | GraphicsFeatures.DepthBuffer | GraphicsFeatures.Instancing | GraphicsFeatures.SeparateFilters;

    internal const SDL_GPUShaderFormat AcceptedFormats =
        SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV | SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL | SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL;

    private static readonly (PresentMode Mode, SDL_GPUPresentMode Sdl)[] PresentModeMap =
    [
        (PresentMode.VSync, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC),
        (PresentMode.Immediate, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE),
        (PresentMode.Mailbox, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX),
    ];

    private readonly SDL_GPUDevice* _device;
    private readonly SDL_Window* _window;
    private readonly ISurface? _surface;
    private SDL_GPUShader* _spriteVertex, _spriteFragment;
    private SDL_GPUTexture* _backBuffer;
    private (int W, int H) _backBufferSize;
    private readonly TextureFormat _surfaceFormat;
    private readonly SDL_GPUTextureFormat _swapchainFormat;

    /// <summary>The device creation properties (the factory's support check uses the same ones); the caller destroys them.</summary>
    internal static SDL_PropertiesID CreationProperties(GraphicsDeviceOptions options)
    {
        var props = SDL_CreateProperties();
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXIL_BOOLEAN, true);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_SPIRV_BOOLEAN, true);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_MSL_BOOLEAN, true);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_DEBUGMODE_BOOLEAN, options.Debug);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_PREFERLOWPOWER_BOOLEAN, options.PreferLowPower);
        // Software Vulkan (Mesa lavapipe) is a valid adapter: CI renders on it.
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_VULKAN_REQUIRE_HARDWARE_ACCELERATION_BOOLEAN, false);
        if (options.Api is not null) SDL_SetStringProperty(props, SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING, options.Api);
        return props;
    }

    /// <summary>Creates the device, offscreen or for an SDL window surface.</summary>
    /// <param name="surface">A surface whose <see cref="ISurface.NativeWindow"/> is an <c>SDL_Window*</c> with no renderer, or null for an offscreen device.</param>
    /// <param name="options">Present mode (set explicitly on the swapchain), preferred API, validation and adapter preference.</param>
    /// <exception cref="InvalidOperationException">SDL could not create the device or claim the window (the message names SDL's error).</exception>
    /// <exception cref="GraphicsCapabilityException">The window does not support the requested present mode.</exception>
    public SdlGpuDevice(ISurface? surface, GraphicsDeviceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (surface is not null && surface.NativeWindow == 0) throw new ArgumentException("SDL_GPU presents only to an SDL window surface", nameof(surface));
        _surface = surface;
        // SDL's driver hint wins over the name property: set both for the duration of the call, then check what SDL chose.
        if (options.Api is not null) SDL_SetHint(SDL_HINT_GPU_DRIVER, options.Api);
        var props = CreationProperties(options);
        _device = SDL_CreateGPUDeviceWithProperties(props);
        SDL_DestroyProperties(props);
        if (options.Api is not null) SDL_ResetHint(SDL_HINT_GPU_DRIVER);
        if (_device == null) throw new InvalidOperationException($"SDL_CreateGPUDevice({options.Api ?? "default"}): {SDL_GetError()}");

        try
        {
            var api = SDL_GetGPUDeviceDriver(_device) ?? "unknown";
            var deviceProps = SDL_GetGPUDeviceProperties(_device);
            var adapter = SDL_GetStringProperty(deviceProps, SDL_PROP_GPU_DEVICE_NAME_STRING, "");
            DriverName = SDL_GetStringProperty(deviceProps, SDL_PROP_GPU_DEVICE_DRIVER_NAME_STRING, "") ?? "";
            DriverVersion = SDL_GetStringProperty(deviceProps, SDL_PROP_GPU_DEVICE_DRIVER_VERSION_STRING, "") ?? "";
            DriverInfo = SDL_GetStringProperty(deviceProps, SDL_PROP_GPU_DEVICE_DRIVER_INFO_STRING, "") ?? "";

            var sdlFormats = SDL_GetGPUShaderFormats(_device);
            var shaderFormats = ShaderFormat.None;
            if ((sdlFormats & SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV) != 0) shaderFormats |= ShaderFormat.SpirV;
            if ((sdlFormats & SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL) != 0) shaderFormats |= ShaderFormat.Dxil;
            if ((sdlFormats & SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL) != 0) shaderFormats |= ShaderFormat.Msl;

            var formats = new Dictionary<TextureFormat, TextureUsage>();
            foreach (var format in Enum.GetValues<TextureFormat>())
            {
                if (Native(format) is not { } native) continue;
                var usage = TextureUsage.None;
                bool depth = TextureFormats.IsDepth(format);
                // Depth sampling is not part of this contract version: depth formats are attachments only.
                if (!depth && Supports(native, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER)) usage |= TextureUsage.Sampled;
                if (!depth && !TextureFormats.IsCompressed(format) && Supports(native, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET)) usage |= TextureUsage.RenderTarget;
                if (depth && Supports(native, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET)) usage |= TextureUsage.DepthStencil;
                if (usage != TextureUsage.None) formats[format] = usage;
            }

            var presentModes = new HashSet<PresentMode>();
            _surfaceFormat = TextureFormat.Bgra8Unorm;
            if (surface is not null)
            {
                _window = (SDL_Window*)surface.NativeWindow;
                Check(SDL_ClaimWindowForGPUDevice(_device, _window), "SDL_ClaimWindowForGPUDevice");
                foreach (var (mode, sdl) in PresentModeMap)
                    if (SDL_WindowSupportsGPUPresentMode(_device, _window, sdl)) presentModes.Add(mode);
                // The present mode is always set explicitly, never left to the driver.
                if (!presentModes.Contains(options.PresentMode))
                    throw new GraphicsCapabilityException("Create", BackendName, GraphicsFeatures.None, $"PresentMode.{options.PresentMode}");
                Check(SDL_SetGPUSwapchainParameters(_device, _window, SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR,
                    PresentModeMap.First(m => m.Mode == options.PresentMode).Sdl), "SDL_SetGPUSwapchainParameters");
                _swapchainFormat = SDL_GetGPUSwapchainTextureFormat(_device, _window);
                _surfaceFormat = FromNative(_swapchainFormat) ?? throw new InvalidOperationException($"swapchain format {_swapchainFormat} has no contract format");
                SwapchainPresentMode = options.PresentMode;
            }

            // SDL_GPU reports no limits: these are guaranteed minimums (SDL_GPU's slot, attribute and uniform constants; the
            // API specification's minimum texture size).
            var maxTexture = api switch { "direct3d12" => 16384, "metal" => 8192, _ => 4096 };
            var limits = new GraphicsLimits(maxTexture, maxTexture, MaxTextureSlots: 16, MaxUniformBytes: 32768, MaxVertexAttributes: 16);
            Capabilities = new GraphicsCapabilities(BackendName, GraphicsTier.Tier1, DeclaredFeatures, shaderFormats, formats,
                new HashSet<BlendState>(BlendState.Presets), presentModes, limits, api, adapter);
        }
        catch
        {
            if (_window != null) SDL_ReleaseWindowFromGPUDevice(_device, _window);
            SDL_DestroyGPUDevice(_device);
            throw;
        }
    }

    /// <inheritdoc />
    public override GraphicsCapabilities Capabilities { get; }

    /// <summary>The driver's name as SDL reports it (Vulkan: "llvmpipe" adapters report Mesa's driver name), or empty.</summary>
    public string DriverName { get; }

    /// <summary>The driver's version as SDL reports it, or empty.</summary>
    public string DriverVersion { get; }

    /// <summary>Further driver information as SDL reports it, or empty.</summary>
    public string DriverInfo { get; }

    /// <summary>The present mode set on the swapchain, or null for an offscreen device.</summary>
    public PresentMode? SwapchainPresentMode { get; }

    /// <summary>Copies of the surface texture to the swapchain made by <see cref="IGraphicsDevice.Present"/> (one per presented frame that had a swapchain texture).</summary>
    public long SurfaceCopies { get; private set; }

    internal SDL_GPUDevice* Handle => _device;

    /// <inheritdoc />
    protected override Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData)
    {
        var texture = NewTexture(Native(description.Format)!.Value, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER, description.Width, description.Height, description.MipLevels);
        if (!initialData.IsEmpty)
        {
            var cmd = Acquire();
            var copy = SDL_BeginGPUCopyPass(cmd);
            UploadTexture(copy, texture, new RectI(0, 0, description.Width, description.Height), initialData);
            SDL_EndGPUCopyPass(copy);
            Check(SDL_SubmitGPUCommandBuffer(cmd), "SDL_SubmitGPUCommandBuffer");
        }
        return new GpuTexture(this, description, texture, owner: null);
    }

    /// <inheritdoc />
    protected override RenderTarget CreateRenderTargetCore(in RenderTargetDescription description)
    {
        var color = NewTexture(Native(description.Format)!.Value, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET, description.Width, description.Height, 1);
        SDL_GPUTexture* depth = null;
        if (description.Depth is { } d)
            depth = NewTexture(Native(d)!.Value, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET, description.Width, description.Height, 1);
        return new GpuRenderTarget(this, description, color, depth, isSurface: false);
    }

    /// <inheritdoc />
    protected override Sampler CreateSamplerCore(in SamplerDescription description)
    {
        var info = new SDL_GPUSamplerCreateInfo
        {
            min_filter = NativeFilter(description.Min), mag_filter = NativeFilter(description.Mag),
            mipmap_mode = description.Mip == Filter.Linear ? SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR : SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
            address_mode_u = Address(description.U), address_mode_v = Address(description.V), address_mode_w = Address(AddressMode.Clamp),
            max_lod = 1000,
        };
        var sampler = SDL_CreateGPUSampler(_device, &info);
        if (sampler == null) throw new InvalidOperationException($"SDL_CreateGPUSampler: {SDL_GetError()}");
        return new GpuSampler(this, description, sampler);
    }

    /// <inheritdoc />
    protected override GpuBuffer CreateBufferCore(in BufferDescription description)
    {
        var usage = (SDL_GPUBufferUsageFlags)0;
        if ((description.Usage & BufferUsage.Vertex) != 0) usage |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX;
        if ((description.Usage & BufferUsage.Index) != 0) usage |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX;
        var info = new SDL_GPUBufferCreateInfo { usage = usage, size = (uint)description.SizeInBytes };
        var buffer = SDL_CreateGPUBuffer(_device, &info);
        if (buffer == null) throw new InvalidOperationException($"SDL_CreateGPUBuffer: {SDL_GetError()}");
        return new GpuBufferObject(this, description, buffer);
    }

    /// <inheritdoc />
    protected override Shader CreateShaderCore(in ShaderDescription description)
        => new GpuShader(this, description, LoadShader(description.Stage, description.Format, description.Code.Span, description.EntryPoint, description.Resources));

    /// <inheritdoc />
    protected override Pipeline CreatePipelineCore(PipelineDescription d)
    {
        var vertex = (GpuShader)d.Vertex;
        var fragment = (GpuShader)d.Fragment;
        var pipeline = NewPipeline(vertex.Native, fragment.Native, d.Layout, d.Blend, d.Depth, d.Raster, d.ColorFormat, d.DepthFormat);
        return new GpuPipeline(this, null, d.Layout, d.Blend, d.ColorFormat, d.DepthFormat, vertex.Resources, fragment.Resources, pipeline);
    }

    /// <inheritdoc />
    protected override Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat)
    {
        if (_spriteVertex == null)
        {
            _spriteVertex = LoadBuiltInShader("sprite.vert", ShaderStage.Vertex, new ShaderResources(UniformBuffers: 1));
            _spriteFragment = LoadBuiltInShader("sprite.frag", ShaderStage.Fragment, new ShaderResources(Samplers: 1));
        }
        var pipeline = NewPipeline(_spriteVertex, _spriteFragment, VertexLayout.Vertex2D, blend, DepthState.None, new RasterState(), colorFormat, depthFormat);
        // Callers see no uniforms: the device pushes the viewport projection itself.
        return new GpuPipeline(this, kind, VertexLayout.Vertex2D, blend, colorFormat, depthFormat, default, new ShaderResources(Samplers: 1), pipeline);
    }

    /// <inheritdoc />
    protected override void SubmitCore(CommandList commands)
    {
        var cmd = Acquire();
        SDL_GPUCopyPass* copy = null;
        SDL_GPURenderPass* pass = null;
        GpuPipeline? pipeline = null;
        var viewport = default(RectI);
        var scissor = default(RectI);
        var projectionPushed = false;
        (int Offset, int Length)? userVertexSlot0 = null;
        var builtInClobbered = false;

        foreach (ref readonly var c in commands.Commands)
        {
            switch (c.Kind)
            {
                case CommandKind.UpdateBuffer:
                case CommandKind.UpdateTexture:
                    if (copy == null) copy = SDL_BeginGPUCopyPass(cmd);
                    if (c.Kind == CommandKind.UpdateBuffer) UploadBuffer(copy, ((GpuBufferObject)c.Resource!).Native, c.First, commands.Payload(c));
                    else UploadTexture(copy, ((GpuTexture)c.Resource!).Native, c.Rect, commands.Payload(c));
                    break;
                case CommandKind.BeginPass:
                {
                    if (copy != null) { SDL_EndGPUCopyPass(copy); copy = null; }
                    var target = (GpuRenderTarget)c.Pass.Color.Target;
                    var clear = c.Pass.Color.Clear;
                    var colorInfo = new SDL_GPUColorTargetInfo
                    {
                        texture = target.NativeColor,
                        load_op = Load(c.Pass.Color.Load),
                        store_op = Store(c.Pass.Color.Store),
                        clear_color = new SDL_FColor { r = clear.R, g = clear.G, b = clear.B, a = clear.A },
                    };
                    if (c.Pass.Depth is { } depth)
                    {
                        var depthInfo = new SDL_GPUDepthStencilTargetInfo
                        {
                            texture = ((GpuRenderTarget)depth.Target).NativeDepth,
                            clear_depth = depth.ClearDepth,
                            load_op = Load(depth.Load), store_op = Store(depth.Store),
                            stencil_load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE, stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
                        };
                        pass = SDL_BeginGPURenderPass(cmd, &colorInfo, 1, &depthInfo);
                    }
                    else pass = SDL_BeginGPURenderPass(cmd, &colorInfo, 1, null);
                    if (pass == null) throw new InvalidOperationException($"SDL_BeginGPURenderPass: {SDL_GetError()}");
                    viewport = scissor = target.Bounds;
                    pipeline = null;
                    projectionPushed = false;
                    break;
                }
                case CommandKind.EndPass:
                    SDL_EndGPURenderPass(pass);
                    pass = null;
                    break;
                case CommandKind.SetViewport:
                {
                    viewport = c.Rect;
                    var vp = new SDL_GPUViewport { x = c.Rect.X, y = c.Rect.Y, w = c.Rect.Width, h = c.Rect.Height, min_depth = 0, max_depth = 1 };
                    SDL_SetGPUViewport(pass, &vp);
                    projectionPushed = false;
                    break;
                }
                case CommandKind.SetScissor:
                {
                    scissor = c.Rect;
                    if (!scissor.IsEmpty) // an empty scissor draws nothing: draws are skipped below (some drivers reject a zero-size scissor)
                    {
                        var rect = new SDL_Rect { x = c.Rect.X, y = c.Rect.Y, w = c.Rect.Width, h = c.Rect.Height };
                        SDL_SetGPUScissor(pass, &rect);
                    }
                    break;
                }
                case CommandKind.SetPipeline:
                    pipeline = (GpuPipeline)c.Resource!;
                    SDL_BindGPUGraphicsPipeline(pass, pipeline.Native);
                    if (pipeline.BuiltIn is null && builtInClobbered && userVertexSlot0 is { } user)
                    {
                        // The built-in projection overwrote vertex uniform slot 0 (SDL_GPU uniforms belong to the command
                        // buffer): restore the caller's data, which the contract says persists for the rest of the list.
                        fixed (byte* data = commands.Payload(new Command(CommandKind.SetUniforms, PayloadOffset: user.Offset, PayloadLength: user.Length)))
                            SDL_PushGPUVertexUniformData(cmd, 0, (nint)data, (uint)user.Length);
                        builtInClobbered = false;
                    }
                    if (pipeline.BuiltIn is not null) projectionPushed = false;
                    break;
                case CommandKind.SetTexture:
                {
                    var binding = new SDL_GPUTextureSamplerBinding { texture = ((GpuTexture)c.Resource!).Native, sampler = ((GpuSampler)c.Sampler!).Native };
                    SDL_BindGPUFragmentSamplers(pass, (uint)c.Slot, &binding, 1);
                    break;
                }
                case CommandKind.SetVertices:
                {
                    var binding = new SDL_GPUBufferBinding { buffer = ((GpuBufferObject)c.Resource!).Native };
                    SDL_BindGPUVertexBuffers(pass, 0, &binding, 1);
                    break;
                }
                case CommandKind.SetIndices:
                {
                    var binding = new SDL_GPUBufferBinding { buffer = ((GpuBufferObject)c.Resource!).Native };
                    SDL_BindGPUIndexBuffer(pass, &binding, c.IndexSize == IndexSize.Bits32 ? SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT : SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);
                    break;
                }
                case CommandKind.SetUniforms:
                {
                    var data = commands.Payload(c);
                    fixed (byte* p = data)
                    {
                        if (c.Stage == ShaderStage.Vertex) SDL_PushGPUVertexUniformData(cmd, (uint)c.Slot, (nint)p, (uint)data.Length);
                        else SDL_PushGPUFragmentUniformData(cmd, (uint)c.Slot, (nint)p, (uint)data.Length);
                    }
                    if (c.Stage == ShaderStage.Vertex && c.Slot == 0) { userVertexSlot0 = (c.PayloadOffset, c.PayloadLength); builtInClobbered = false; }
                    break;
                }
                case CommandKind.Draw:
                case CommandKind.DrawIndexed:
                    if (scissor.IsEmpty) break;
                    if (pipeline!.BuiltIn is not null && !projectionPushed)
                    {
                        // Built-in rule: positions are pixels relative to the viewport origin, y down.
                        var projection = Matrix4x4.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
                        SDL_PushGPUVertexUniformData(cmd, 0, (nint)(&projection), 64);
                        projectionPushed = true;
                        builtInClobbered = true;
                    }
                    if (c.Kind == CommandKind.Draw) SDL_DrawGPUPrimitives(pass, (uint)c.Count, (uint)c.Instances, (uint)c.First, 0);
                    else SDL_DrawGPUIndexedPrimitives(pass, (uint)c.Count, (uint)c.Instances, (uint)c.First, c.BaseVertex, 0);
                    break;
                default:
                    throw new NotSupportedException(c.Kind.ToString());
            }
        }
        if (copy != null) SDL_EndGPUCopyPass(copy);
        Check(SDL_SubmitGPUCommandBuffer(cmd), "SDL_SubmitGPUCommandBuffer");
    }

    /// <inheritdoc />
    protected override ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region)
    {
        var size = (uint)TextureFormats.ByteCount(target.ColorFormat, region.Width, region.Height);
        var info = new SDL_GPUTransferBufferCreateInfo { usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD, size = size };
        var transfer = SDL_CreateGPUTransferBuffer(_device, &info);
        if (transfer == null) throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer: {SDL_GetError()}");
        var cmd = Acquire();
        var copy = SDL_BeginGPUCopyPass(cmd);
        var source = new SDL_GPUTextureRegion
        {
            texture = ((GpuRenderTarget)target).NativeColor,
            x = (uint)region.X, y = (uint)region.Y, w = (uint)region.Width, h = (uint)region.Height, d = 1,
        };
        var destination = new SDL_GPUTextureTransferInfo { transfer_buffer = transfer };
        SDL_DownloadFromGPUTexture(copy, &source, &destination);
        SDL_EndGPUCopyPass(copy);
        var fence = SDL_SubmitGPUCommandBufferAndAcquireFence(cmd);
        if (fence == null) throw new InvalidOperationException($"SDL_SubmitGPUCommandBufferAndAcquireFence: {SDL_GetError()}");
        return new GpuReadback(this, region, target.ColorFormat, fence, transfer, (int)size);
    }

    /// <inheritdoc />
    protected override RenderTarget? TryAcquireSurfaceTargetCore()
    {
        if (_surface is null || _surface.IsMinimized) return null;
        var (w, h) = _surface.PixelSize;
        if (w <= 0 || h <= 0) return null;
        if (_backBuffer == null || _backBufferSize != (w, h))
        {
            if (_backBuffer != null) SDL_ReleaseGPUTexture(_device, _backBuffer);
            _backBuffer = NewTexture(_swapchainFormat, SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER, w, h, 1);
            _backBufferSize = (w, h);
        }
        return new GpuRenderTarget(this, new RenderTargetDescription(w, h, _surfaceFormat, Name: "surface"), _backBuffer, null, isSurface: true);
    }

    /// <inheritdoc />
    protected override void PresentCore(RenderTarget surfaceTarget)
    {
        var cmd = Acquire();
        SDL_GPUTexture* swapchain;
        uint w, h;
        Check(SDL_WaitAndAcquireGPUSwapchainTexture(cmd, _window, &swapchain, &w, &h), "SDL_WaitAndAcquireGPUSwapchainTexture");
        if (swapchain != null) // null: occluded or too many frames in flight; this frame is not shown
        {
            // The one full-surface copy per frame of this design: surface passes render into the device's own texture so
            // they execute at Submit like every other list (the swapchain texture belongs to the acquiring command buffer).
            var blit = new SDL_GPUBlitInfo
            {
                source = new SDL_GPUBlitRegion { texture = _backBuffer, w = (uint)_backBufferSize.W, h = (uint)_backBufferSize.H },
                destination = new SDL_GPUBlitRegion { texture = swapchain, w = w, h = h },
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
                filter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
            };
            SDL_BlitGPUTexture(cmd, &blit);
            SurfaceCopies++;
        }
        Check(SDL_SubmitGPUCommandBuffer(cmd), "SDL_SubmitGPUCommandBuffer");
        ((GpuRenderTarget)surfaceTarget).ExpireSurface();
    }

    /// <inheritdoc />
    protected override void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers)
    {
        SDL_WaitForGPUIdle(_device);
        foreach (var p in builtIns) ((GpuPipeline)p).Destroy();
        foreach (var s in samplers) SDL_ReleaseGPUSampler(_device, ((GpuSampler)s).Native);
        if (_spriteVertex != null) SDL_ReleaseGPUShader(_device, _spriteVertex);
        if (_spriteFragment != null) SDL_ReleaseGPUShader(_device, _spriteFragment);
        if (_backBuffer != null) SDL_ReleaseGPUTexture(_device, _backBuffer);
        if (_window != null) SDL_ReleaseWindowFromGPUDevice(_device, _window);
        SDL_DestroyGPUDevice(_device);
    }

    private SDL_GPUCommandBuffer* Acquire()
    {
        var cmd = SDL_AcquireGPUCommandBuffer(_device);
        return cmd != null ? cmd : throw new InvalidOperationException($"SDL_AcquireGPUCommandBuffer: {SDL_GetError()}");
    }

    private void UploadBuffer(SDL_GPUCopyPass* copy, SDL_GPUBuffer* buffer, int offset, ReadOnlySpan<byte> data)
    {
        var transfer = Staging(data);
        var source = new SDL_GPUTransferBufferLocation { transfer_buffer = transfer };
        var destination = new SDL_GPUBufferRegion { buffer = buffer, offset = (uint)offset, size = (uint)data.Length };
        SDL_UploadToGPUBuffer(copy, &source, &destination, false); // no cycling: the rest of the buffer is preserved
        SDL_ReleaseGPUTransferBuffer(_device, transfer); // SDL releases it once the copy executed
    }

    private void UploadTexture(SDL_GPUCopyPass* copy, SDL_GPUTexture* texture, RectI region, ReadOnlySpan<byte> data)
    {
        var transfer = Staging(data);
        var source = new SDL_GPUTextureTransferInfo { transfer_buffer = transfer };
        var destination = new SDL_GPUTextureRegion { texture = texture, x = (uint)region.X, y = (uint)region.Y, w = (uint)region.Width, h = (uint)region.Height, d = 1 };
        SDL_UploadToGPUTexture(copy, &source, &destination, false);
        SDL_ReleaseGPUTransferBuffer(_device, transfer);
    }

    private SDL_GPUTransferBuffer* Staging(ReadOnlySpan<byte> data)
    {
        var info = new SDL_GPUTransferBufferCreateInfo { usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD, size = (uint)data.Length };
        var transfer = SDL_CreateGPUTransferBuffer(_device, &info);
        if (transfer == null) throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer: {SDL_GetError()}");
        var mapped = SDL_MapGPUTransferBuffer(_device, transfer, false);
        if (mapped == 0) throw new InvalidOperationException($"SDL_MapGPUTransferBuffer: {SDL_GetError()}");
        data.CopyTo(new Span<byte>((void*)mapped, data.Length));
        SDL_UnmapGPUTransferBuffer(_device, transfer);
        return transfer;
    }

    private SDL_GPUTexture* NewTexture(SDL_GPUTextureFormat format, SDL_GPUTextureUsageFlags usage, int width, int height, int levels)
    {
        var info = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D, format = format, usage = usage,
            width = (uint)width, height = (uint)height, layer_count_or_depth = 1, num_levels = (uint)levels,
        };
        var texture = SDL_CreateGPUTexture(_device, &info);
        return texture != null ? texture : throw new InvalidOperationException($"SDL_CreateGPUTexture({format}): {SDL_GetError()}");
    }

    private SDL_GPUShader* LoadBuiltInShader(string name, ShaderStage stage, ShaderResources resources)
    {
        var (extension, format, entry) = Capabilities.ShaderFormats switch
        {
            var f when f.HasFlag(ShaderFormat.Dxil) && Capabilities.ApiName == "direct3d12" => ("dxil", ShaderFormat.Dxil, "main"),
            var f when f.HasFlag(ShaderFormat.SpirV) && Capabilities.ApiName != "metal" => ("spv", ShaderFormat.SpirV, "main"),
            var f when f.HasFlag(ShaderFormat.Msl) => ("msl", ShaderFormat.Msl, "main0"),
            _ => throw new InvalidOperationException($"no built-in shader blob for {Capabilities.ShaderFormats} on {Capabilities.ApiName}"),
        };
        using var stream = typeof(SdlGpuDevice).Assembly.GetManifestResourceStream($"builtin/{name}.{extension}")
                           ?? throw new InvalidOperationException($"missing embedded shader builtin/{name}.{extension}");
        var code = new byte[stream.Length];
        stream.ReadExactly(code);
        return LoadShader(stage, format, code, entry, resources);
    }

    private SDL_GPUShader* LoadShader(ShaderStage stage, ShaderFormat format, ReadOnlySpan<byte> code, string entryPoint, ShaderResources resources)
    {
        byte[]? terminated = null;
        if (format == ShaderFormat.Msl && code[^1] != 0) // MSL source is NUL-terminated
        {
            terminated = new byte[code.Length + 1];
            code.CopyTo(terminated);
            code = terminated;
        }
        var entry = Encoding.UTF8.GetBytes(entryPoint + "\0");
        fixed (byte* codePointer = code)
        fixed (byte* entryPointer = entry)
        {
            var info = new SDL_GPUShaderCreateInfo
            {
                code_size = (nuint)code.Length, code = codePointer, entrypoint = entryPointer,
                format = format switch
                {
                    ShaderFormat.Dxil => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
                    ShaderFormat.SpirV => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                    _ => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                },
                stage = stage == ShaderStage.Vertex ? SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX : SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
                num_samplers = (uint)resources.Samplers,
                num_uniform_buffers = (uint)resources.UniformBuffers,
            };
            var shader = SDL_CreateGPUShader(_device, &info);
            return shader != null ? shader : throw new InvalidOperationException($"SDL_CreateGPUShader: {SDL_GetError()}");
        }
    }

    private SDL_GPUGraphicsPipeline* NewPipeline(SDL_GPUShader* vertex, SDL_GPUShader* fragment, VertexLayout layout, BlendState blend,
        DepthState depth, RasterState raster, TextureFormat colorFormat, TextureFormat? depthFormat)
    {
        var bufferDescription = new SDL_GPUVertexBufferDescription { slot = 0, pitch = (uint)layout.Stride, input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX };
        var attributes = stackalloc SDL_GPUVertexAttribute[layout.Attributes.Count];
        for (var i = 0; i < layout.Attributes.Count; i++)
        {
            var a = layout.Attributes[i];
            attributes[i] = new SDL_GPUVertexAttribute { location = (uint)a.Location, buffer_slot = 0, format = Element(a.Format), offset = (uint)a.Offset };
        }
        var colorTarget = new SDL_GPUColorTargetDescription
        {
            format = Native(colorFormat)!.Value,
            blend_state = new SDL_GPUColorTargetBlendState
            {
                enable_blend = blend != BlendState.Opaque,
                src_color_blendfactor = Factor(blend.SrcColor), dst_color_blendfactor = Factor(blend.DstColor), color_blend_op = Op(blend.ColorOp),
                src_alpha_blendfactor = Factor(blend.SrcAlpha), dst_alpha_blendfactor = Factor(blend.DstAlpha), alpha_blend_op = Op(blend.AlphaOp),
            },
        };
        var info = new SDL_GPUGraphicsPipelineCreateInfo
        {
            vertex_shader = vertex,
            fragment_shader = fragment,
            vertex_input_state = new SDL_GPUVertexInputState
            {
                vertex_buffer_descriptions = &bufferDescription, num_vertex_buffers = 1,
                vertex_attributes = attributes, num_vertex_attributes = (uint)layout.Attributes.Count,
            },
            primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
            rasterizer_state = new SDL_GPURasterizerState
            {
                cull_mode = raster.Cull switch { CullMode.Back => SDL_GPUCullMode.SDL_GPU_CULLMODE_BACK, CullMode.Front => SDL_GPUCullMode.SDL_GPU_CULLMODE_FRONT, _ => SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE },
                front_face = raster.Front == FrontFace.CounterClockwise ? SDL_GPUFrontFace.SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE : SDL_GPUFrontFace.SDL_GPU_FRONTFACE_CLOCKWISE,
                enable_depth_clip = true,
            },
            depth_stencil_state = new SDL_GPUDepthStencilState
            {
                enable_depth_test = depth.Test, enable_depth_write = depth.Write, compare_op = Compare(depth.Compare),
            },
            target_info = new SDL_GPUGraphicsPipelineTargetInfo
            {
                color_target_descriptions = &colorTarget, num_color_targets = 1,
                has_depth_stencil_target = depthFormat is not null,
                depth_stencil_format = depthFormat is { } df ? Native(df)!.Value : SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID,
            },
        };
        var pipeline = SDL_CreateGPUGraphicsPipeline(_device, &info);
        // Pipeline creation validates the shaders against each other and the layout: a mismatch fails here, never at draw.
        return pipeline != null ? pipeline : throw new InvalidOperationException($"SDL_CreateGPUGraphicsPipeline: {SDL_GetError()}");
    }

    private bool Supports(SDL_GPUTextureFormat format, SDL_GPUTextureUsageFlags usage)
        => SDL_GPUTextureSupportsFormat(_device, format, SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D, usage);

    internal static SDL_GPUTextureFormat? Native(TextureFormat format) => format switch
    {
        TextureFormat.Rgba8Unorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
        TextureFormat.Rgba8UnormSrgb => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB,
        TextureFormat.Bgra8Unorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM,
        TextureFormat.R8Unorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM,
        TextureFormat.Rg8Unorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UNORM,
        TextureFormat.Rgba16Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT,
        TextureFormat.Bc7 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM,
        TextureFormat.Astc4x4 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_UNORM,
        TextureFormat.D16Unorm => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM,
        TextureFormat.D24UnormS8 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
        TextureFormat.D32Float => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT,
        _ => null, // Etc2Rgba8: SDL_GPU 3.5 has no ETC2 format
    };

    private static TextureFormat? FromNative(SDL_GPUTextureFormat format) => format switch
    {
        SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM => TextureFormat.Bgra8Unorm,
        SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM => TextureFormat.Rgba8Unorm,
        _ => null,
    };

    private static SDL_GPUFilter NativeFilter(Filter f) => f == Filter.Linear ? SDL_GPUFilter.SDL_GPU_FILTER_LINEAR : SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;

    private static SDL_GPUSamplerAddressMode Address(AddressMode m) => m switch
    {
        AddressMode.Repeat => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
        AddressMode.Mirror => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
        _ => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
    };

    private static SDL_GPULoadOp Load(LoadOp op) => op switch
    {
        LoadOp.Clear => SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
        LoadOp.DontCare => SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
        _ => SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
    };

    private static SDL_GPUStoreOp Store(StoreOp op) => op == StoreOp.DontCare ? SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE : SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE;

    private static SDL_GPUVertexElementFormat Element(VertexFormat f) => f switch
    {
        VertexFormat.Float1 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,
        VertexFormat.Float2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
        VertexFormat.Float3 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
        VertexFormat.Float4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
        _ => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
    };

    private static SDL_GPUCompareOp Compare(CompareOp op) => op switch
    {
        CompareOp.Never => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NEVER,
        CompareOp.Less => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS,
        CompareOp.Equal => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_EQUAL,
        CompareOp.LessOrEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS_OR_EQUAL,
        CompareOp.Greater => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_GREATER,
        CompareOp.NotEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NOT_EQUAL,
        CompareOp.GreaterOrEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_GREATER_OR_EQUAL,
        _ => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_ALWAYS,
    };

    private static SDL_GPUBlendFactor Factor(BlendFactor f) => f switch
    {
        BlendFactor.Zero => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
        BlendFactor.One => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
        BlendFactor.SrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
        BlendFactor.InvSrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
        BlendFactor.SrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
        BlendFactor.InvSrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
        BlendFactor.DstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
        BlendFactor.InvDstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
        BlendFactor.DstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
        _ => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
    };

    private static SDL_GPUBlendOp Op(BlendOp o) => o switch
    {
        BlendOp.Add => SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
        BlendOp.Subtract => SDL_GPUBlendOp.SDL_GPU_BLENDOP_SUBTRACT,
        BlendOp.ReverseSubtract => SDL_GPUBlendOp.SDL_GPU_BLENDOP_REVERSE_SUBTRACT,
        BlendOp.Min => SDL_GPUBlendOp.SDL_GPU_BLENDOP_MIN,
        _ => SDL_GPUBlendOp.SDL_GPU_BLENDOP_MAX,
    };

    private static void Check(bool ok, string what)
    {
        if (!ok) throw new InvalidOperationException($"{what}: {SDL_GetError()}");
    }

    internal void ReleaseTexture(SDL_GPUTexture* texture) { if (!IsDisposed) SDL_ReleaseGPUTexture(_device, texture); }
    internal void ReleaseBuffer(SDL_GPUBuffer* buffer) { if (!IsDisposed) SDL_ReleaseGPUBuffer(_device, buffer); }
    internal void ReleaseShader(SDL_GPUShader* shader) { if (!IsDisposed) SDL_ReleaseGPUShader(_device, shader); }
    internal void ReleasePipeline(SDL_GPUGraphicsPipeline* pipeline) { if (!IsDisposed) SDL_ReleaseGPUGraphicsPipeline(_device, pipeline); }
}

internal sealed unsafe class GpuTexture(SdlGpuDevice device, in TextureDescription description, SDL_GPUTexture* native, GpuRenderTarget? owner)
    : Texture(device, description)
{
    public SDL_GPUTexture* Native { get; } = native;
    public override RenderTarget? OwnerTarget => owner;
    protected override string? OwnedBy => owner is null ? null : "its render target";
    protected override void Release() { if (owner is null) ((SdlGpuDevice)Device).ReleaseTexture(Native); }
    internal void ExpireOwned() => Expire();
}

internal sealed unsafe class GpuRenderTarget : RenderTarget
{
    private readonly GpuTexture? _color;

    public GpuRenderTarget(SdlGpuDevice device, in RenderTargetDescription description, SDL_GPUTexture* color, SDL_GPUTexture* depth, bool isSurface)
        : base(device, description, isSurface)
    {
        NativeColor = color;
        NativeDepth = depth;
        if (!isSurface) _color = new GpuTexture(device, new TextureDescription(description.Width, description.Height, description.Format, Name: description.Name), color, this);
    }

    public SDL_GPUTexture* NativeColor { get; }
    public SDL_GPUTexture* NativeDepth { get; }
    public override Texture? Color => _color;
    protected override string? OwnedBy => IsSurface ? "its device until Present" : null;

    protected override void Release()
    {
        if (IsSurface) return; // the back buffer belongs to the device
        _color?.ExpireOwned();
        var device = (SdlGpuDevice)Device;
        device.ReleaseTexture(NativeColor);
        if (NativeDepth != null) device.ReleaseTexture(NativeDepth);
    }

    internal void ExpireSurface() => Expire();
}

internal sealed unsafe class GpuSampler(SdlGpuDevice device, in SamplerDescription description, SDL_GPUSampler* native) : Sampler(device, description)
{
    public SDL_GPUSampler* Native { get; } = native;
}

internal sealed unsafe class GpuBufferObject(SdlGpuDevice device, in BufferDescription description, SDL_GPUBuffer* native) : GpuBuffer(device, description)
{
    public SDL_GPUBuffer* Native { get; } = native;
    protected override void Release() => ((SdlGpuDevice)Device).ReleaseBuffer(Native);
}

internal sealed unsafe class GpuShader(SdlGpuDevice device, in ShaderDescription description, SDL_GPUShader* native) : Shader(device, description)
{
    public SDL_GPUShader* Native { get; } = native;
    protected override void Release() => ((SdlGpuDevice)Device).ReleaseShader(Native);
}

internal sealed unsafe class GpuPipeline(SdlGpuDevice device, BuiltInPipeline? kind, VertexLayout layout, BlendState blend, TextureFormat color,
    TextureFormat? depth, ShaderResources vertexResources, ShaderResources fragmentResources, SDL_GPUGraphicsPipeline* native)
    : Pipeline(device, kind, layout, blend, color, depth, vertexResources, fragmentResources)
{
    public SDL_GPUGraphicsPipeline* Native { get; } = native;
    protected override void Release() => ((SdlGpuDevice)Device).ReleasePipeline(Native);
    internal void Destroy() => Expire();
}

internal sealed unsafe class GpuReadback(SdlGpuDevice device, RectI region, TextureFormat format, SDL_GPUFence* fence, SDL_GPUTransferBuffer* transfer, int size)
    : ReadbackRequest(region, format)
{
    public override bool IsCompleted => SDL_QueryGPUFence(device.Handle, fence);

    public override void Wait()
    {
        var f = fence;
        if (!SDL_WaitForGPUFences(device.Handle, true, &f, 1)) throw new InvalidOperationException($"SDL_WaitForGPUFences: {SDL_GetError()}");
    }

    protected override byte[] Collect()
    {
        var pixels = new byte[size];
        var mapped = SDL_MapGPUTransferBuffer(device.Handle, transfer, false);
        if (mapped == 0) throw new InvalidOperationException($"SDL_MapGPUTransferBuffer: {SDL_GetError()}");
        Marshal.Copy(mapped, pixels, 0, size);
        SDL_UnmapGPUTransferBuffer(device.Handle, transfer);
        SDL_ReleaseGPUTransferBuffer(device.Handle, transfer);
        SDL_ReleaseGPUFence(device.Handle, fence);
        return pixels;
    }
}
