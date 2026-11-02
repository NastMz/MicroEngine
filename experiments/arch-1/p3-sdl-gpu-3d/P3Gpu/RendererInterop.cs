using System.Numerics;
using SDL;
using static P3Gpu.Gpu;
using static SDL.SDL3;

namespace P3Gpu;

/// <summary>
/// Variant 2: SDL_Renderer on its "gpu" driver draws the 2D; the 3D pass is recorded with raw SDL_GPU on the
/// renderer's own device into a GPU texture that is wrapped as an SDL_Texture
/// (SDL_PROP_TEXTURE_CREATE_GPU_TEXTURE_POINTER). One overlay quad uses a custom fragment shader through
/// SDL_CreateGPURenderState. Readback through SDL_RenderReadPixels.
/// </summary>
public static unsafe class RendererInterop
{
    public static int Run(Options options, bool deviceFirst)
    {
        var mode = deviceFirst ? "device-first" : "renderer-first";
        var c = new Checks($"renderer/{options.Driver}/{mode}");
        SDL_SetHint(SDL_HINT_GPU_DRIVER, options.Driver);
        var window = NotNull(SDL_CreateWindow($"P3 SDL_Renderer gpu {options.Driver} {mode}", Scene.Width, Scene.Height, options.Hidden ? SDL_WindowFlags.SDL_WINDOW_HIDDEN : 0), "SDL_CreateWindow");

        SDL_GPUDevice* device;
        SDL_Renderer* renderer;
        if (deviceFirst)
        {
            // The app creates the device (forcing the backend) and hands it to SDL_CreateGPURenderer.
            device = NotNull(SDL_CreateGPUDevice(AllFormats, options.Debug, options.Driver), $"SDL_CreateGPUDevice {options.Driver}");
            renderer = NotNull(SDL_CreateGPURenderer(device, window), "SDL_CreateGPURenderer");
        }
        else
        {
            // The renderer creates the device; the app borrows it with SDL_GetGPURendererDevice.
            if (options.Debug) SDL_SetHint(SDL_HINT_RENDER_GPU_DEBUG, "1");
            var props = SDL_CreateProperties();
            SDL_SetStringProperty(props, SDL_PROP_RENDERER_CREATE_NAME_STRING, "gpu");
            SDL_SetPointerProperty(props, SDL_PROP_RENDERER_CREATE_WINDOW_POINTER, (IntPtr)window);
            SDL_SetBooleanProperty(props, SDL_PROP_RENDERER_CREATE_GPU_SHADERS_SPIRV_BOOLEAN, true);
            SDL_SetBooleanProperty(props, SDL_PROP_RENDERER_CREATE_GPU_SHADERS_DXIL_BOOLEAN, true);
            SDL_SetBooleanProperty(props, SDL_PROP_RENDERER_CREATE_GPU_SHADERS_MSL_BOOLEAN, true);
            renderer = NotNull(SDL_CreateRendererWithProperties(props), "SDL_CreateRendererWithProperties gpu");
            SDL_DestroyProperties(props);
            device = NotNull(SDL_GetGPURendererDevice(renderer), "SDL_GetGPURendererDevice");
        }
        Console.WriteLine($"  renderer={SDL_GetRendererName(renderer)}");
        PureGpu.Report(device, 0);
        c.True("interop", "the renderer runs on the requested SDL_GPU backend", SDL_GetGPUDeviceDriver(device) == options.Driver, SDL_GetGPUDeviceDriver(device) ?? "null");

        var mesh = new Mesh3D(device);
        var scene = Wrap(renderer, mesh.Color);
        var opaque = CreateSdlTexture(renderer, SpriteTexture.Opaque);
        var half = CreateSdlTexture(renderer, SpriteTexture.Half);
        var invertShader = LoadShader(device, "invert.frag");
        var stateInfo = new SDL_GPURenderStateCreateInfo { fragment_shader = invertShader };
        var invert = NotNull(SDL_CreateGPURenderState(renderer, &stateInfo), "SDL_CreateGPURenderState");
        var amount = new Vector4(1, 0, 0, 0);
        Ok(SDL_SetGPURenderStateFragmentUniforms(invert, 0, (IntPtr)(&amount), 16), "SDL_SetGPURenderStateFragmentUniforms");
        var invertRect = new SDL_FRect { x = Scene.Width - 100, y = Scene.Height - 80, w = 40, h = 40 };

        float[] captures = [0, -30];
        var spin = options.Hidden ? 0 : options.Frames;
        for (var frame = 0; frame < spin + captures.Length; frame++)
        {
            if (PureGpu.Quit()) break;
            var capture = frame >= spin;
            var angle = capture ? captures[frame - spin] : frame * 360f / Math.Max(1, spin);

            // 1. 3D on the renderer's device, in the app's own command buffer, submitted before the renderer's work.
            var cmd = NotNull(SDL_AcquireGPUCommandBuffer(device), "SDL_AcquireGPUCommandBuffer");
            mesh.Record(cmd, angle);
            Ok(SDL_SubmitGPUCommandBuffer(cmd), "SDL_SubmitGPUCommandBuffer");

            // 2. 2D through SDL_Renderer: the 3D image, then the overlay.
            SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL_RenderClear(renderer);
            SDL_RenderTexture(renderer, scene, null, null);
            if (!capture || angle == 0)
            {
                foreach (var s in Scene.Sprites)
                {
                    var texture = s.Texture == SpriteTexture.Opaque ? opaque : half;
                    SDL_SetTextureColorModFloat(texture, s.Color.X, s.Color.Y, s.Color.Z);
                    var rect = new SDL_FRect { x = s.Position.X, y = s.Position.Y, w = s.Size.X, h = s.Size.Y };
                    SDL_RenderTexture(renderer, texture, null, &rect);
                }
                SDL_SetTextureColorModFloat(opaque, 1, 0.5f, 0);
                Ok(SDL_SetGPURenderState(renderer, invert), "SDL_SetGPURenderState");
                SDL_RenderTexture(renderer, opaque, null, &invertRect);
                Ok(SDL_SetGPURenderState(renderer, null), "SDL_SetGPURenderState null");
            }

            if (capture)
            {
                SDL_ClearError();
                var image = ReadPixels(renderer);
                var error = SDL_GetError();
                if (!string.IsNullOrEmpty(error)) Console.WriteLine($"  OBSERVE SDL_GetError after the frame: {error}");
                var path = Path.Combine(options.Out, $"renderer-{options.Driver}-{mode}{options.Suffix}-angle{angle:+0;-0;0}.png");
                SavePng(image, path);
                Console.WriteLine($"  captured {path}");
                Scene.Assert(c, image, angle, withOverlay: angle == 0);
                if (angle == 0)
                {
                    var p = Scene.Pixel(new Vector2(invertRect.x + 20, invertRect.y + 20));
                    c.Near("custom shader", $"SDL_GPURenderState invert shader turns (1,0.5,0) into (0,0.5,1) at {p}", image[p.X, p.Y],
                        Rgba.From(new Vector4(0, 0.5f, 1, 1)), 1, "8-bit quantization of 0.5");
                }
            }
            SDL_RenderPresent(renderer);
        }

        Ordering(renderer, device, mesh, scene);

        SDL_DestroyGPURenderState(invert);
        SDL_ReleaseGPUShader(device, invertShader);
        SDL_DestroyTexture(opaque);
        SDL_DestroyTexture(half);
        SDL_DestroyTexture(scene);
        SDL_WaitForGPUIdle(device);
        mesh.Dispose();
        SDL_DestroyRenderer(renderer);
        if (deviceFirst) SDL_DestroyGPUDevice(device);
        SDL_DestroyWindow(window);
        Console.WriteLine($"renderer/{options.Driver}/{mode}: {c.Count - c.Failed}/{c.Count} passed");
        return c.Failed;
    }

    /// <summary>
    /// Observes when SDL_Renderer's queued draws execute relative to the app's own submissions: the scene texture is
    /// cleared red and submitted, drawn by the renderer, then cleared blue and submitted, then read back.
    /// </summary>
    private static void Ordering(SDL_Renderer* renderer, SDL_GPUDevice* device, Mesh3D mesh, SDL_Texture* scene)
    {
        foreach (var (flush, cycle) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            Clear(device, mesh, Scene.Red, cycle: false);
            SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL_RenderClear(renderer);
            SDL_RenderTexture(renderer, scene, null, null);
            if (flush) Ok(SDL_FlushRenderer(renderer), "SDL_FlushRenderer");
            Clear(device, mesh, Scene.Blue, cycle);
            var image = ReadPixels(renderer);
            var seen = image[Scene.Width / 2, Scene.Height / 2];
            var which = seen == Rgba.From(Scene.Red) ? "red: the renderer sampled the texture before the later submission"
                : seen == Rgba.From(Scene.Blue) ? "blue: the renderer's draw ran after the app's later submission"
                : $"other {seen}";
            Console.WriteLine($"  OBSERVE ordering ({(flush ? "with" : "without")} SDL_FlushRenderer between the draw and the next app submission, cycle={cycle}): {which}");
            SDL_RenderPresent(renderer);
        }
    }

    private static void Clear(SDL_GPUDevice* device, Mesh3D mesh, Vector4 color, bool cycle)
    {
        var cmd = NotNull(SDL_AcquireGPUCommandBuffer(device), "SDL_AcquireGPUCommandBuffer");
        mesh.Record(cmd, 0, color, drawScene: false, cycle);
        Ok(SDL_SubmitGPUCommandBuffer(cmd), "SDL_SubmitGPUCommandBuffer");
    }

    /// <summary>Wraps an existing SDL_GPU texture (R8G8B8A8) as an SDL_Texture of this renderer, without copying.</summary>
    private static SDL_Texture* Wrap(SDL_Renderer* renderer, SDL_GPUTexture* texture)
    {
        var props = SDL_CreateProperties();
        SDL_SetNumberProperty(props, SDL_PROP_TEXTURE_CREATE_FORMAT_NUMBER, (long)SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
        SDL_SetNumberProperty(props, SDL_PROP_TEXTURE_CREATE_ACCESS_NUMBER, (long)SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC);
        SDL_SetNumberProperty(props, SDL_PROP_TEXTURE_CREATE_WIDTH_NUMBER, Scene.Width);
        SDL_SetNumberProperty(props, SDL_PROP_TEXTURE_CREATE_HEIGHT_NUMBER, Scene.Height);
        SDL_SetPointerProperty(props, SDL_PROP_TEXTURE_CREATE_GPU_TEXTURE_POINTER, (IntPtr)texture);
        var wrapped = NotNull(SDL_CreateTextureWithProperties(renderer, props), "SDL_CreateTextureWithProperties (wrap GPU texture)");
        SDL_DestroyProperties(props);
        Ok(SDL_SetTextureBlendMode(wrapped, SDL_BlendMode.SDL_BLENDMODE_NONE), "SDL_SetTextureBlendMode");
        Ok(SDL_SetTextureScaleMode(wrapped, SDL_ScaleMode.SDL_SCALEMODE_NEAREST), "SDL_SetTextureScaleMode");
        return wrapped;
    }

    private static SDL_Texture* CreateSdlTexture(SDL_Renderer* renderer, SpriteTexture kind)
    {
        var texture = NotNull(SDL_CreateTexture(renderer, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, 4, 4), "SDL_CreateTexture");
        var pixels = Scene.TexturePixels(kind);
        fixed (byte* p = pixels) Ok(SDL_UpdateTexture(texture, null, (IntPtr)p, 16), "SDL_UpdateTexture");
        Ok(SDL_SetTextureBlendMode(texture, SDL_BlendMode.SDL_BLENDMODE_BLEND), "SDL_SetTextureBlendMode");
        Ok(SDL_SetTextureScaleMode(texture, SDL_ScaleMode.SDL_SCALEMODE_NEAREST), "SDL_SetTextureScaleMode");
        return texture;
    }

    private static Image ReadPixels(SDL_Renderer* renderer)
    {
        var surface = NotNull(SDL_RenderReadPixels(renderer, null), "SDL_RenderReadPixels");
        var rgba = NotNull(SDL_ConvertSurface(surface, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888), "SDL_ConvertSurface");
        SDL_DestroySurface(surface);
        var bytes = new byte[rgba->w * rgba->h * 4];
        for (var y = 0; y < rgba->h; y++)
            new ReadOnlySpan<byte>((byte*)rgba->pixels + y * rgba->pitch, rgba->w * 4).CopyTo(bytes.AsSpan(y * rgba->w * 4));
        var image = new Image(rgba->w, rgba->h, bytes);
        SDL_DestroySurface(rgba);
        return image;
    }
}
