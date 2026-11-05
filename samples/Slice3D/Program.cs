// The 3D vertical slice on the tier-1 SDL_GPU device (docs/manual/rendering.md, "The 3D path").
//
//   dotnet run --project samples/Slice3D                          a window; Esc or closing it quits
//   dotnet run --project samples/Slice3D -- --frames 90 --capture out.png [--hidden] [--api vulkan|direct3d12]
//   dotnet run --project samples/Slice3D -- --measure-e12 [--hidden]   cost of a full-surface copy at Present
//   dotnet run --project samples/Slice3D -- --write-crate              rewrites Content/crate.glb
//
// The scene: a turntable (root entity) carrying four crates, one of which carries a small crate on an arm: a three-level
// transform hierarchy stepped at 30 Hz and drawn at the engine's interpolation (FrameClock's blend, SceneWorld's
// interpolated world matrices), lit by the one material; a 2D HUD (the batcher) drawn over the 3D in the same target.
using System.Diagnostics;
using System.Numerics;
using MicroEngine;
using MicroEngine.Content;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Graphics.SdlGpu;
using MicroEngine.Graphics3D;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using Slice3D;
using GpuDevice = MicroEngine.Graphics.Devices.IGraphicsDevice;

System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
var options = Options.Parse(args);
var content = Path.Combine(AppContext.BaseDirectory, "Content");
if (options.WriteCrate)
{
    var file = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "crate.glb"));
    File.WriteAllBytes(file, CrateModel.Glb());
    Console.WriteLine($"wrote {file}");
    return 0;
}

using var window = new SdlWindow("MicroEngine 3D slice", options.Width, options.Height, options.Hidden);
using var device = new SdlGpuBackendFactory().Create(window, new GraphicsDeviceOptions(options.MeasureE12 ? PresentMode.Immediate : PresentMode.VSync, options.Api));
var gpu = (SdlGpuDevice)device;
Console.WriteLine($"device: {device.Capabilities.Backend} {device.Capabilities.ApiName} on \"{device.Capabilities.Adapter}\" ({gpu.DriverName} {gpu.DriverVersion})");

// The mesh is an asset: the store's loader registry knows the "mesh" kind (glTF), the file is read from Content/.
using var store = new AssetStore(ContentSources.Open(content), new AssetLoaderRegistry().AddMeshes());
var crateData = store.Get(store.Root.Load<MeshData>(MeshAssets.Kind, "crate.glb"));
using var renderer = new Renderer3D(device) { Ambient = 0.3f, LightDirection = new Vector3(-0.5f, 1, 0.7f) };
using var crate = renderer.Upload(crateData);
using var batch = new Renderer2D(device);
var scene = new SliceScene(crate);
var camera = new Camera3D { Position = new Vector3(0, 3.2f, 7.5f), Target = new Vector3(0, 0.4f, 0), FieldOfView = MathF.PI / 3.2f };
var clock = new FrameClock(new TimeSettings { FixedRate = 30 });
RenderTarget? canvas = null;

if (options.MeasureE12) return MeasureE12.Run(device, gpu, window, renderer, batch, scene, camera);

long last = Stopwatch.GetTimestamp();
for (int frame = 0; options.Frames is not { } n || frame < n; frame++)
{
    if (!window.Pump()) break;
    long now = Stopwatch.GetTimestamp();
    // A capture run steps the clock deterministically (one step per frame) so its image is the same on every run.
    clock.BeginFrame(options.Frames is null ? clock.ClampElapsed(Stopwatch.GetElapsedTime(last, now).TotalSeconds) : 1.0 / 60);
    last = now;
    while (clock.TryStep(out var step)) scene.Step(step.FixedStep, (float)step.FixedDelta);
    clock.EndSteps();
    scene.World.BeginDraw(clock.FixedStep - 1, clock.Blend);

    var (w, h) = window.PixelSize;
    if (w <= 0 || h <= 0) continue;
    if (canvas is null || canvas.Width != w || canvas.Height != h) { canvas?.Dispose(); canvas = renderer.CreateTarget(w, h, "canvas"); }
    scene.Draw(renderer, batch, canvas, camera, clock.Blend);
    if (device.TryAcquireSurfaceTarget(out var surface))
    {
        batch.Begin(new BatchSettings(surface, Blend: BlendState.Opaque));
        batch.Draw(canvas.Color!, new RectF(0, 0, w, h), Matrix3x2.Identity, ColorF.White);
        batch.End();
    }
    device.Present();
}

if (options.Capture is { } path && canvas is not null)
{
    var request = device.ReadPixels(canvas, canvas.Bounds);
    request.Wait();
    File.WriteAllBytes(path, Png.Encode(canvas.Width, canvas.Height, request.Pixels.ToArray()));
    Console.WriteLine($"capture: {Path.GetFullPath(path)} ({canvas.Width}x{canvas.Height}), {renderer.Statistics.Meshes} meshes, {renderer.Statistics.Triangles} triangles, {gpu.SurfaceCopies} surface copies");
}
canvas?.Dispose();
return 0;

namespace Slice3D
{
    /// <summary>The world and its step: the hierarchy the slice draws.</summary>
    internal sealed class SliceScene
    {
        private readonly EntityId _turntable, _carrier, _arm;
        private float _turn, _spin, _orbit;

        public SliceScene(GpuMesh crate)
        {
            World = new SceneWorld();
            _turntable = World.Create();
            var colors = new[] { new ColorF(1, 0.55f, 0.35f, 1), new ColorF(0.45f, 0.8f, 1, 1), new ColorF(0.7f, 1, 0.5f, 1), new ColorF(1, 0.9f, 0.45f, 1) };
            for (int i = 0; i < 4; i++)
            {
                var box = World.Create(_turntable);
                float a = i * MathF.PI / 2;
                ref var t = ref World.Local(box);
                t.Position = new Vector3(2.2f * MathF.Cos(a), -0.05f, 2.2f * MathF.Sin(a));
                t.Scale = new Vector3(0.9f);
                World.Attach(box, new MeshRenderer(crate) { Tint = colors[i] });
                if (i == 0) _carrier = box;
            }
            // A third level: an arm on the first crate carrying a small crate.
            _arm = World.Create(_carrier);
            World.Local(_arm).Position = new Vector3(0, 1.1f, 0);
            var small = World.Create(_arm);
            ref var s = ref World.Local(small);
            s.Position = new Vector3(0.9f, 0, 0);
            s.Scale = new Vector3(0.35f);
            World.Attach(small, new MeshRenderer(crate) { Tint = new ColorF(1, 1, 1, 1) });
            // The floor: a flattened crate under everything, not a child of the turntable.
            var floor = World.Create();
            ref var f = ref World.Local(floor);
            f.Position = new Vector3(0, -0.6f, 0);
            f.Scale = new Vector3(7, 0.2f, 7);
            World.Attach(floor, new MeshRenderer(crate) { Tint = new ColorF(0.35f, 0.35f, 0.4f, 1) });
        }

        public SceneWorld World { get; }

        public long Steps { get; private set; }

        public void Step(long engineStep, float dt)
        {
            World.BeginStep(engineStep, dt);
            _turn += dt * 0.6f;
            _spin += dt * 1.7f;
            _orbit += dt * 3.1f;
            World.Local(_turntable).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _turn);
            World.Local(_carrier).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _spin);
            World.Local(_arm).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _orbit);
            World.EndStep();
            Steps++;
        }

        /// <summary>The 3D world into <paramref name="canvas"/>, then the 2D HUD over it in the same target.</summary>
        public void Draw(Renderer3D renderer, Renderer2D batch, RenderTarget canvas, Camera3D camera, float blend)
        {
            renderer.Begin(canvas, camera, clear: PremultipliedColor.FromBytes(24, 28, 40));
            renderer.DrawWorld(World);
            renderer.End();
            batch.Begin(new BatchSettings(canvas));
            // A translucent panel with one bar per step of the last 30 (the step rate) and the blend as a moving marker.
            batch.Draw(batch.White, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(196, 34) * Matrix3x2.CreateTranslation(12, 12), new ColorF(0, 0, 0, 0.55f));
            for (int i = 0; i < 30; i++)
            {
                bool lit = i <= Steps % 30;
                batch.Draw(batch.White, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(4, 12) * Matrix3x2.CreateTranslation(20 + i * 6, 18),
                    lit ? new ColorF(0.4f, 0.85f, 1, 1) : new ColorF(0.2f, 0.2f, 0.25f, 1));
            }
            batch.Draw(batch.White, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(180 * blend + 2, 4) * Matrix3x2.CreateTranslation(20, 36), new ColorF(1, 0.8f, 0.3f, 1));
            batch.End();
        }
    }

    internal sealed record Options(int Width, int Height, bool Hidden, int? Frames, string? Capture, string? Api, bool MeasureE12, bool WriteCrate)
    {
        public static Options Parse(string[] args)
        {
            string? Value(string name) => Array.IndexOf(args, name) is >= 0 and var i && i + 1 < args.Length ? args[i + 1] : null;
            bool measure = args.Contains("--measure-e12");
            return new Options(
                int.Parse(Value("--width") ?? (measure ? "1920" : "960"), System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(Value("--height") ?? (measure ? "1080" : "540"), System.Globalization.CultureInfo.InvariantCulture),
                args.Contains("--hidden"),
                Value("--frames") is { } f ? int.Parse(f, System.Globalization.CultureInfo.InvariantCulture) : null,
                Value("--capture"), Value("--api"), measure, args.Contains("--write-crate"));
        }
    }
}
