using System.Diagnostics;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Graphics.SdlGpu;
using MicroEngine.Graphics3D;

namespace Slice3D;

/// <summary>
/// The cost of the tier-1 design's full-surface copy at Present (the device renders the surface into its own texture
/// and copies it to the swapchain), measured outside the engine loop on a 1920 × 1080 window. SDL_GPU has no timestamp
/// queries, so the cost is measured on the CPU with the GPU synchronised every frame (a 1-pixel readback waits for the
/// frame's work): frame time with and without presentation, and the cost of one extra full-size textured copy (the
/// same bytes the swapchain copy moves), from the difference between frames with 0 and 8 such copies. Present mode
/// Immediate, so no display wait is included.
/// </summary>
internal static class MeasureE12
{
    private const int Warmup = 30, Frames = 240, ExtraCopies = 8;

    public static int Run(string? api)
    {
        using var window = new SdlWindow("MicroEngine 3D slice: E12", 1920, 1080, hidden: false);
        using var device = new SdlGpuBackendFactory().Create(window, new GraphicsDeviceOptions(PresentMode.Immediate, api));
        var gpu = (SdlGpuDevice)device;
        using var renderer = new Renderer3D(device) { Ambient = 0.3f };
        using var crate = renderer.Upload(GltfReader.Read(CrateModel.Glb(), "crate.glb"));
        using var batch = new Renderer2D(device);
        var camera = new Camera3D { Position = new Vector3(0, 3.2f, 7.5f), Target = new Vector3(0, 0.4f, 0) };
        var (w, h) = window.PixelSize;
        using var canvas = renderer.CreateTarget(w, h, "canvas");
        using var scratch = device.CreateRenderTarget(new RenderTargetDescription(w, h, Name: "copy"));

        void Scene(int frame)
        {
            renderer.Begin(canvas, camera, clear: PremultipliedColor.FromBytes(24, 28, 40));
            for (int i = 0; i < 5; i++)
                renderer.Draw(crate, Matrix4x4.CreateRotationY(frame * 0.02f + i) * Matrix4x4.CreateTranslation(i * 1.6f - 3.2f, 0, 0));
            renderer.End();
        }

        double Measure(bool present, int copies)
        {
            long copiesBefore = gpu.SurfaceCopies;
            var clock = Stopwatch.StartNew();
            for (int i = 0; i < Warmup + Frames; i++)
            {
                if (i == Warmup) { clock.Restart(); copiesBefore = gpu.SurfaceCopies; }
                window.Pump();
                Scene(i);
                for (int c = 0; c < copies; c++)
                {
                    batch.Begin(new BatchSettings(scratch, Blend: BlendState.Opaque));
                    batch.Draw(canvas.Color!, new RectF(0, 0, w, h), Matrix3x2.Identity, ColorF.White);
                    batch.End();
                }
                if (present && device.TryAcquireSurfaceTarget(out var surface))
                {
                    batch.Begin(new BatchSettings(surface, Blend: BlendState.Opaque));
                    batch.Draw(canvas.Color!, new RectF(0, 0, w, h), Matrix3x2.Identity, ColorF.White);
                    batch.End();
                    device.Present();
                }
                device.ReadPixels(canvas, new RectI(0, 0, 1, 1)).Wait();
            }
            double ms = clock.Elapsed.TotalMilliseconds / Frames;
            if (present && gpu.SurfaceCopies - copiesBefore != Frames) Console.WriteLine($"  note: {gpu.SurfaceCopies - copiesBefore} of {Frames} frames had a swapchain texture");
            return ms;
        }

        var offscreen = Measure(present: false, copies: 0);
        var presented = Measure(present: true, copies: 0);
        var withCopies = Measure(present: true, copies: ExtraCopies);
        var perCopy = (withCopies - presented) / ExtraCopies;
        Console.WriteLine($"E12 on {device.Capabilities.ApiName} \"{device.Capabilities.Adapter}\", {w}x{h}, {Frames} frames, GPU synchronised per frame:");
        Console.WriteLine($"  offscreen frame (3D, no surface)               {offscreen:F3} ms");
        Console.WriteLine($"  presented frame (+ surface pass, copy, present) {presented:F3} ms  (+{presented - offscreen:F3} ms)");
        Console.WriteLine($"  one extra full-surface textured copy           {perCopy:F3} ms  ({ExtraCopies} copies: {withCopies:F3} ms per frame)");
        return 0;
    }
}
