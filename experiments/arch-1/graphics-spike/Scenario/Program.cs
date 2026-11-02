// ARCH-1 graphics contract spike — scenario.
//
//   dotnet run --project Scenario -c Release -- scene [--out dir] [--low-power] [--tier0-api name]   2D scene on every device, 3D under 2D on tier 1
//   dotnet run --project Scenario -c Release -- compare a.png b.png                pixel diff statistics of two captures
//   dotnet run --project Scenario -c Release -- present [--frames n] [--low-power] present-mode timing (K-P1-1), visible windows
//
// The exit code is the number of failed checks.
using System.Diagnostics;
using System.Numerics;
using Arch1.Graphics;
using Arch1.Graphics.Batching;
using Arch1.Graphics.Scenario;
using Arch1.Graphics.Sdl;
using Arch1.Graphics.Tier0Sdl;
using Arch1.Graphics.Tier1SdlGpu;
using SDL;
using static SDL.SDL3;

var command = args.FirstOrDefault() ?? "scene";
var outDir = Option("--out") ?? "out";
var lowPower = args.Contains("--low-power");
var frames = int.TryParse(Option("--frames"), out var n) ? n : 120;

SdlRuntime.EnsureVideo();
Console.WriteLine($"SDL {SdlRuntime.Version}; .NET {Environment.Version}; {Environment.OSVersion}");

(string Name, IGraphicsBackendFactory Factory, string? Api)[] devices =
[
    ("tier0-sdl-renderer" + (Option("--tier0-api") is { } t0 ? "-" + t0 : ""), new SdlRendererBackendFactory(), Option("--tier0-api")),
    ("tier1-sdl-gpu-direct3d12", new SdlGpuBackendFactory(), "direct3d12"),
    ("tier1-sdl-gpu-vulkan", new SdlGpuBackendFactory(), "vulkan"),
];

return command switch
{
    "scene" => RunScene(),
    "present" => RunPresent(),
    "compare" when args.Length >= 3 => Compare(args[1], args[2]),
    _ => Usage(),
};

int RunScene()
{
    var failed = 0;
    var frames2D = new List<(string Name, Frame Frame)>();
    var frames3D = new List<(string Name, Frame Frame)>();
    foreach (var (name, factory, api) in devices)
    {
        var c = new Checks(name);
        using var surface = new SdlWindowSurface(name, Scene.Width, Scene.Height, hidden: true);
        var started = Stopwatch.StartNew();
        using var device = factory.Create(surface, new GraphicsDeviceOptions(Api: api, PreferLowPower: lowPower));
        var createMs = started.Elapsed.TotalMilliseconds;
        var caps = device.Capabilities;
        Console.WriteLine($"{name}: api={caps.ApiName} adapter=\"{caps.Adapter}\" tier={caps.Tier} features={caps.Features} shaders={caps.ShaderFormats} " +
                          $"present={string.Join("/", caps.PresentModes)} depth={string.Join("/", new[] { TextureFormat.D16Unorm, TextureFormat.D24UnormS8, TextureFormat.D32Float }.Where(f => caps.Supports(f, TextureUsage.DepthStencil)))} created in {createMs:F0} ms");
        using var batcher = new Renderer2D(device);

        // The same batcher code on every device: 2D scene to an offscreen target, read back.
        var target = device.CreateRenderTarget(new RenderTargetDescription(Scene.Width, Scene.Height, Name: "scene"));
        Scene.Draw2D(device, batcher, target, clear: true);
        var frame = Frame.Read(device, target);
        frame.SavePng(Path.Combine(outDir, $"{name}-2d.png"));
        Scene.Check(c, frame, with3D: false);
        frames2D.Add((name, frame));

        // Captures reach the surface by drawing the read-back target (directive 3): exercised once per device.
        if (device.TryAcquireSurfaceTarget(out var surfaceTarget))
        {
            batcher.Begin(new BatchSettings(surfaceTarget, Blend: BlendState.Opaque, Clear: ColorF.Transparent));
            batcher.Draw(target.Color!, new RectF(0, 0, Scene.Width, Scene.Height), Matrix3x2.Identity, ColorF.White);
            batcher.End();
            device.Present();
            c.True("state", $"capture drawn to the surface target ({surfaceTarget.ColorFormat}) and presented", surfaceTarget.IsDisposed);
        }

        if (caps.Has(GraphicsFeatures.DepthBuffer))
        {
            var depth = new[] { TextureFormat.D24UnormS8, TextureFormat.D32Float, TextureFormat.D16Unorm }.First(f => caps.Supports(f, TextureUsage.DepthStencil));
            var target3D = device.CreateRenderTarget(new RenderTargetDescription(Scene.Width, Scene.Height, Depth: depth, Name: "scene-3d"));
            Scene.Draw3D(device, target3D);
            Scene.Draw2D(device, batcher, target3D, clear: false); // the same 2D layer, composited over the mesh
            var frame3D = Frame.Read(device, target3D);
            frame3D.SavePng(Path.Combine(outDir, $"{name}-3d.png"));
            Scene.Check(c, frame3D, with3D: true);
            frames3D.Add((name, frame3D));
        }
        else
        {
            try
            {
                device.CreateRenderTarget(new RenderTargetDescription(Scene.Width, Scene.Height, Depth: TextureFormat.D32Float));
                c.True("state", "tier 0 refuses depth", false);
            }
            catch (GraphicsCapabilityException e)
            {
                c.True("state", "tier 0 refuses the 3D half with the typed error", e.Missing == GraphicsFeatures.DepthBuffer, e.Message);
            }
        }
        Console.WriteLine($"{name}: {c.Count - c.Failed}/{c.Count} checks passed");
        failed += c.Failed;
    }

    Console.WriteLine("pixel comparison (2D scene, same batcher code):");
    for (var i = 0; i < frames2D.Count; i++)
        for (var j = i + 1; j < frames2D.Count; j++)
            Diff(frames2D[i], frames2D[j]);
    Console.WriteLine("pixel comparison (3D + 2D, tier 1):");
    for (var i = 0; i < frames3D.Count; i++)
        for (var j = i + 1; j < frames3D.Count; j++)
            Diff(frames3D[i], frames3D[j]);
    Console.WriteLine($"scene: {failed} failed check(s)");
    return failed;
}

int RunPresent()
{
    Console.WriteLine(DisplayMode());
    foreach (var (name, factory, api) in devices)
        foreach (var present in new[] { PresentMode.VSync, PresentMode.Immediate })
            foreach (var fullscreen in new[] { false, true })
            {
                using var surface = new SdlWindowSurface($"{name} {present}", 640, 480, hidden: false);
                if (fullscreen) surface.SetFullscreen(true);
                IGraphicsDevice device;
                try { device = factory.Create(surface, new GraphicsDeviceOptions(present, api, PreferLowPower: lowPower)); }
                catch (GraphicsCapabilityException e) { Console.WriteLine($"  {name} {present} {(fullscreen ? "fullscreen" : "windowed")}: {e.Message}"); continue; }
                using (device)
                using (var batcher = new Renderer2D(device))
                {
                    var white = device.CreateTexture(new TextureDescription(1, 1), [255, 255, 255, 255]);
                    var clock = new Stopwatch();
                    var shown = 0;
                    for (var f = 0; f < frames + 10; f++)
                    {
                        if (f == 10) clock.Start();
                        SdlRuntime.Pump();
                        if (!device.TryAcquireSurfaceTarget(out var target)) continue;
                        batcher.Begin(new BatchSettings(target, Clear: ColorF.FromBytes(32, 64, 96)));
                        batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(40) * Matrix3x2.CreateTranslation(f % 600, 100), ColorF.White);
                        batcher.End();
                        device.Present();
                        if (f >= 10) shown++;
                    }
                    var ms = clock.Elapsed.TotalMilliseconds / Math.Max(1, shown);
                    Console.WriteLine($"  {name} {present,-9} {(fullscreen ? "fullscreen" : "windowed  ")}: {ms,6:F2} ms/frame over {shown} frames ({surface.PixelSize.Width}x{surface.PixelSize.Height})");
                }
            }
    return 0;
}

void Diff((string Name, Frame Frame) a, (string Name, Frame Frame) b)
{
    int differing = 0, maxDelta = 0;
    var histogram = new int[256];
    var minX = int.MaxValue; var minY = int.MaxValue; var maxX = -1; var maxY = -1;
    for (var y = 0; y < a.Frame.Height; y++)
        for (var x = 0; x < a.Frame.Width; x++)
        {
            var delta = 0;
            for (var ch = 0; ch < 4; ch++) delta = Math.Max(delta, Math.Abs(a.Frame.Channel(x, y, ch) - b.Frame.Channel(x, y, ch)));
            if (delta == 0) continue;
            differing++;
            histogram[delta]++;
            maxDelta = Math.Max(maxDelta, delta);
            minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
        }
    var spread = string.Join(", ", Enumerable.Range(1, 255).Where(d => histogram[d] > 0).Select(d => $"Δ{d}:{histogram[d]}"));
    Console.WriteLine($"  {a.Name} vs {b.Name}: {differing} of {a.Frame.Width * a.Frame.Height} pixels differ, max channel delta {maxDelta}" +
                      (differing > 0 ? $" [{spread}] within ({minX},{minY})-({maxX},{maxY})" : ""));
}

int Compare(string first, string second)
{
    Diff((Path.GetFileName(first), Frame.LoadPng(first)), (Path.GetFileName(second), Frame.LoadPng(second)));
    return 0;
}

string? Option(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static unsafe string DisplayMode()
{
    var mode = SDL_GetCurrentDisplayMode(SDL_GetPrimaryDisplay());
    return $"display: {mode->w}x{mode->h} @ {mode->refresh_rate:F2} Hz ({1000 / mode->refresh_rate:F2} ms per refresh)";
}

static int Usage()
{
    Console.Error.WriteLine("usage: Scenario scene [--out dir] [--low-power] | present [--frames n] [--low-power]");
    return 2;
}
