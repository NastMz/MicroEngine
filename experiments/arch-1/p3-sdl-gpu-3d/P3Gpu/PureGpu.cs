using System.Diagnostics;
using SDL;
using static P3Gpu.Gpu;
using static SDL.SDL3;

namespace P3Gpu;

/// <summary>
/// Variant 1: one SDL_GPU device; a 3D pass (depth) then a 2D pass (sprite pipeline, alpha blend, LOAD) on the same
/// colour target and command buffer; readback through a download transfer buffer; blit to the swapchain to present.
/// </summary>
public static unsafe class PureGpu
{
    public static int Run(Options options)
    {
        var c = new Checks($"gpu/{options.Driver}");
        var window = NotNull(SDL_CreateWindow($"P3 SDL_GPU {options.Driver}", Scene.Width, Scene.Height, options.Hidden ? SDL_WindowFlags.SDL_WINDOW_HIDDEN : 0), "SDL_CreateWindow");
        // SDL_HINT_GPU_DRIVER, when set, wins over SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING; set both.
        SDL_SetHint(SDL_HINT_GPU_DRIVER, options.Driver);
        var props = SDL_CreateProperties();
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXIL_BOOLEAN, true);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_SPIRV_BOOLEAN, true);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_MSL_BOOLEAN, true);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_DEBUGMODE_BOOLEAN, options.Debug);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_PREFERLOWPOWER_BOOLEAN, options.LowPower);
        SDL_SetStringProperty(props, SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING, options.Driver);
        var started = Stopwatch.StartNew();
        var device = NotNull(SDL_CreateGPUDeviceWithProperties(props), $"SDL_CreateGPUDevice {options.Driver}");
        SDL_DestroyProperties(props);
        var createMs = started.Elapsed.TotalMilliseconds;
        Report(device, createMs);
        c.True("device", "SDL_GPU runs on the requested backend", SDL_GetGPUDeviceDriver(device) == options.Driver, SDL_GetGPUDeviceDriver(device) ?? "null");
        if (!options.Hidden) Ok(SDL_ClaimWindowForGPUDevice(device, window), "SDL_ClaimWindowForGPUDevice");

        var mesh = new Mesh3D(device);
        Console.WriteLine($"  depth formats: {mesh.DepthSupport}; using {mesh.DepthFormat}");
        var sprites = new Sprites2D(device);

        var frames = 0;
        var frameTimer = Stopwatch.StartNew();
        // Rotating camera on screen, then deterministic capture frames at known angles.
        float[] captures = [0, -30];
        var spin = options.Hidden ? 0 : options.Frames;
        for (var frame = 0; frame < spin + captures.Length; frame++)
        {
            if (Quit()) break;
            var capture = frame >= spin;
            var angle = capture ? captures[frame - spin] : frame * 360f / Math.Max(1, spin);
            var cmd = NotNull(SDL_AcquireGPUCommandBuffer(device), "SDL_AcquireGPUCommandBuffer");
            mesh.Record(cmd, angle);
            if (!capture || angle == 0) sprites.Record(cmd, mesh.Color);
            SDL_GPUTransferBuffer* download = capture ? RecordDownload(device, cmd, mesh.Color, Scene.Width, Scene.Height) : null;
            if (!options.Hidden) Present(cmd, window, mesh.Color);
            if (capture)
            {
                var fence = NotNull(SDL_SubmitGPUCommandBufferAndAcquireFence(cmd), "SDL_SubmitGPUCommandBufferAndAcquireFence");
                Ok(SDL_WaitForGPUFences(device, true, &fence, 1), "SDL_WaitForGPUFences");
                SDL_ReleaseGPUFence(device, fence);
                var image = Read(device, download, Scene.Width, Scene.Height);
                var path = Path.Combine(options.Out, $"gpu-{options.Driver}{options.Suffix}-angle{angle:+0;-0;0}.png");
                SavePng(image, path);
                Console.WriteLine($"  captured {path}");
                Scene.Assert(c, image, angle, withOverlay: angle == 0);
            }
            else
            {
                Ok(SDL_SubmitGPUCommandBuffer(cmd), "SDL_SubmitGPUCommandBuffer");
            }
            frames++;
        }
        var elapsed = frameTimer.Elapsed.TotalMilliseconds;
        if (spin > 0) Console.WriteLine($"  {frames} frames in {elapsed:F0} ms ({elapsed / frames:F2} ms/frame, vsync)");

        SDL_WaitForGPUIdle(device);
        sprites.Dispose();
        mesh.Dispose();
        if (!options.Hidden) SDL_ReleaseWindowFromGPUDevice(device, window);
        SDL_DestroyGPUDevice(device);
        SDL_DestroyWindow(window);
        Console.WriteLine($"gpu/{options.Driver}: {c.Count - c.Failed}/{c.Count} passed");
        return c.Failed;
    }

    /// <summary>Observes which wins when SDL_HINT_GPU_DRIVER and SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING disagree.</summary>
    public static void HintPrecedence()
    {
        SDL_SetHint(SDL_HINT_GPU_DRIVER, "direct3d12");
        var props = SDL_CreateProperties();
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXIL_BOOLEAN, true);
        SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_SPIRV_BOOLEAN, true);
        SDL_SetStringProperty(props, SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING, "vulkan");
        var device = SDL_CreateGPUDeviceWithProperties(props);
        SDL_DestroyProperties(props);
        Console.WriteLine($"  OBSERVE hint=direct3d12, name property=vulkan -> {(device == null ? $"no device ({SDL_GetError()})" : SDL_GetGPUDeviceDriver(device))}");
        if (device != null) SDL_DestroyGPUDevice(device);
        SDL_ResetHint(SDL_HINT_GPU_DRIVER);
    }

    public static void Report(SDL_GPUDevice* device, double createMs)
    {
        var p = SDL_GetGPUDeviceProperties(device);
        Console.WriteLine($"  device: driver={SDL_GetGPUDeviceDriver(device)} name=\"{SDL_GetStringProperty(p, SDL_PROP_GPU_DEVICE_NAME_STRING, "?")}\" " +
                          $"driverName=\"{SDL_GetStringProperty(p, SDL_PROP_GPU_DEVICE_DRIVER_NAME_STRING, "?")}\" " +
                          $"driverVersion=\"{SDL_GetStringProperty(p, SDL_PROP_GPU_DEVICE_DRIVER_VERSION_STRING, "?")}\" " +
                          $"info=\"{SDL_GetStringProperty(p, SDL_PROP_GPU_DEVICE_DRIVER_INFO_STRING, "?")}\" " +
                          $"shaderFormats={SDL_GetGPUShaderFormats(device)} created in {createMs:F0} ms");
    }

    private static void Present(SDL_GPUCommandBuffer* cmd, SDL_Window* window, SDL_GPUTexture* source)
    {
        SDL_GPUTexture* swapchain;
        uint w, h;
        Ok(SDL_WaitAndAcquireGPUSwapchainTexture(cmd, window, &swapchain, &w, &h), "SDL_WaitAndAcquireGPUSwapchainTexture");
        if (swapchain == null) return; // minimised or occluded: nothing to present this frame
        var blit = new SDL_GPUBlitInfo
        {
            source = new SDL_GPUBlitRegion { texture = source, w = Scene.Width, h = Scene.Height },
            destination = new SDL_GPUBlitRegion { texture = swapchain, w = w, h = h },
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
            filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
        };
        SDL_BlitGPUTexture(cmd, &blit);
    }

    public static bool Quit()
    {
        SDL_Event e;
        while (SDL_PollEvent(&e))
            if (e.Type == SDL_EventType.SDL_EVENT_QUIT) return true;
        return false;
    }
}

/// <summary>The 2D half on raw SDL_GPU: a textured, alpha-blended sprite pipeline drawn in a LOAD pass over the 3D result.</summary>
public sealed unsafe class Sprites2D : IDisposable
{
    private readonly SDL_GPUDevice* _device;
    private readonly SDL_GPUGraphicsPipeline* _pipeline;
    private readonly SDL_GPUBuffer* _vertices;
    private readonly SDL_GPUTexture* _opaque;
    private readonly SDL_GPUTexture* _half;
    private readonly SDL_GPUSampler* _sampler;
    private readonly Sprite[] _sprites = Scene.Sprites;

    public Sprites2D(SDL_GPUDevice* device)
    {
        _device = device;
        var vertex = LoadShader(device, "sprite.vert");
        var fragment = LoadShader(device, "sprite.frag");
        var bufferDescription = new SDL_GPUVertexBufferDescription { slot = 0, pitch = 32, input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX };
        var attributes = stackalloc SDL_GPUVertexAttribute[3];
        attributes[0] = new SDL_GPUVertexAttribute { location = 0, format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2, offset = 0 };
        attributes[1] = new SDL_GPUVertexAttribute { location = 1, format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2, offset = 8 };
        attributes[2] = new SDL_GPUVertexAttribute { location = 2, format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4, offset = 16 };
        var colorTarget = new SDL_GPUColorTargetDescription
        {
            format = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
            blend_state = new SDL_GPUColorTargetBlendState
            {
                enable_blend = true,
                src_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                dst_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                src_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                dst_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
            },
        };
        var info = new SDL_GPUGraphicsPipelineCreateInfo
        {
            vertex_shader = vertex,
            fragment_shader = fragment,
            vertex_input_state = new SDL_GPUVertexInputState
            {
                vertex_buffer_descriptions = &bufferDescription, num_vertex_buffers = 1,
                vertex_attributes = attributes, num_vertex_attributes = 3,
            },
            primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
            rasterizer_state = new SDL_GPURasterizerState { cull_mode = SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE },
            target_info = new SDL_GPUGraphicsPipelineTargetInfo { color_target_descriptions = &colorTarget, num_color_targets = 1 },
        };
        _pipeline = NotNull(SDL_CreateGPUGraphicsPipeline(device, &info), "SDL_CreateGPUGraphicsPipeline sprite");
        SDL_ReleaseGPUShader(device, vertex);
        SDL_ReleaseGPUShader(device, fragment);
        _vertices = CreateBuffer<SpriteVertex>(device, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX, Scene.SpriteVertices(_sprites));
        _opaque = CreateTexture(device, 4, 4, Scene.TexturePixels(SpriteTexture.Opaque));
        _half = CreateTexture(device, 4, 4, Scene.TexturePixels(SpriteTexture.Half));
        _sampler = PointClamp(device);
    }

    public void Record(SDL_GPUCommandBuffer* cmd, SDL_GPUTexture* target)
    {
        var colorInfo = new SDL_GPUColorTargetInfo { texture = target, load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD, store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE };
        var pass = SDL_BeginGPURenderPass(cmd, &colorInfo, 1, null);
        SDL_BindGPUGraphicsPipeline(pass, _pipeline);
        var ortho = Scene.Ortho;
        SDL_PushGPUVertexUniformData(cmd, 0, (IntPtr)(&ortho), 64);
        var binding = new SDL_GPUBufferBinding { buffer = _vertices };
        SDL_BindGPUVertexBuffers(pass, 0, &binding, 1);
        for (var i = 0; i < _sprites.Length; i++)
        {
            var textureBinding = new SDL_GPUTextureSamplerBinding { texture = _sprites[i].Texture == SpriteTexture.Opaque ? _opaque : _half, sampler = _sampler };
            SDL_BindGPUFragmentSamplers(pass, 0, &textureBinding, 1);
            SDL_DrawGPUPrimitives(pass, 6, 1, (uint)(i * 6), 0);
        }
        SDL_EndGPURenderPass(pass);
    }

    public void Dispose()
    {
        SDL_ReleaseGPUSampler(_device, _sampler);
        SDL_ReleaseGPUTexture(_device, _opaque);
        SDL_ReleaseGPUTexture(_device, _half);
        SDL_ReleaseGPUBuffer(_device, _vertices);
        SDL_ReleaseGPUGraphicsPipeline(_device, _pipeline);
    }
}
