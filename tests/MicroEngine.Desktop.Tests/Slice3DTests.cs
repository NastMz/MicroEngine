using System.Numerics;
using MicroEngine.Conformance.Graphics;
using MicroEngine.Conformance.Graphics3D;
using MicroEngine.Content;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Graphics.SdlGpu;
using MicroEngine.Graphics3D;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The 3D path on the tier-1 SDL_GPU device, by property: depth keeps the nearer mesh in either draw order; known world
/// points land on the pixels Camera3D projects them to; 2D drawn after 3D into the same target lands over it; a world's
/// transform hierarchy is drawn at the engine's interpolation between two steps; a glTF asset loaded through the store
/// draws its base colour texture. Ambient light 1 makes every lit colour its base colour exactly, so colours are compared
/// exactly; positions are compared at pixel centres well inside each shape.
/// </summary>
[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class Slice3DTests
{
    private const int W = 128, H = 96;
    private static readonly Vector4 Red = new(1, 0, 0, 1), Green = new(0, 1, 0, 1), Blue = new(0, 0, 1, 1);
    private MicroEngine.Graphics.Devices.IGraphicsDevice _device = null!;
    private Renderer3D _renderer = null!;

    [TestInitialize]
    public void Open()
    {
        try { _device = new SdlGpuBackendFactory().Create(null, new GraphicsDeviceOptions(PreferLowPower: Environment.GetEnvironmentVariable("MICROENGINE_GPU_LOW_POWER") == "1")); }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
        {
            Native.Require(false, $"SDL_GPU cannot start here: {e.Message}");
            throw;
        }
        if (Environment.GetEnvironmentVariable("MICROENGINE_GPU_ADAPTER") is { Length: > 0 } expected
            && _device.Capabilities.Adapter?.Contains(expected, StringComparison.OrdinalIgnoreCase) != true)
            Assert.Fail($"MICROENGINE_GPU_ADAPTER={expected}, but SDL_GPU chose \"{_device.Capabilities.Adapter}\"");
        _renderer = new Renderer3D(_device) { Ambient = 1 };
    }

    [TestCleanup]
    public void Close()
    {
        _renderer?.Dispose();
        _device?.Dispose();
    }

    private static Camera3D Camera() => new() { Position = new Vector3(0, 0, 6), Target = Vector3.Zero, FieldOfView = MathF.PI / 3, Near = 0.5f, Far = 50 };

    private Image Read(RenderTarget target)
    {
        var request = _device.ReadPixels(target, target.Bounds);
        request.Wait();
        return new Image(target.Width, target.Height, request.Pixels.ToArray());
    }

    private static uint Bytes(Vector4 c) => (uint)(c.X * 255) << 24 | (uint)(c.Y * 255) << 16 | (uint)(c.Z * 255) << 8 | 0xFF;

    private static (int X, int Y) PixelOf(Camera3D camera, Vector3 world)
    {
        var (pixel, _) = camera.Project(world, W, H) ?? throw new InvalidOperationException("behind the camera");
        return ((int)pixel.X, (int)pixel.Y);
    }

    [TestMethod]
    public void Depth_TheNearerMeshHidesTheFartherOne_InEitherDrawOrder()
    {
        using var near = _renderer.Upload(TestMeshes.Cube(1, Red));
        using var far = _renderer.Upload(TestMeshes.Cube(3, Green));
        var camera = Camera();
        var target = _renderer.CreateTarget(W, H);
        var nearWorld = Matrix4x4.CreateTranslation(0, 0, 1);
        var farWorld = Matrix4x4.CreateTranslation(0, 0, -2);
        foreach (var nearFirst in new[] { true, false })
        {
            _renderer.Begin(target, camera, clear: PremultipliedColor.FromBytes(0, 0, 0));
            if (nearFirst) { _renderer.Draw(near, nearWorld); _renderer.Draw(far, farWorld); }
            else { _renderer.Draw(far, farWorld); _renderer.Draw(near, nearWorld); }
            _renderer.End();
            var image = Read(target);
            var centre = PixelOf(camera, new Vector3(0, 0, 1.5f));
            Assert.AreEqual(Bytes(Red), image[centre.X, centre.Y], $"near first={nearFirst}: the near cube's front face is in front");
            var farOnly = PixelOf(camera, new Vector3(1.2f, 1.2f, -0.5f)); // on the far cube's front face, outside the near cube
            Assert.AreEqual(Bytes(Green), image[farOnly.X, farOnly.Y], $"near first={nearFirst}: the far cube around it");
            Assert.AreEqual(2, _renderer.Statistics.DrawCalls);
            Assert.AreEqual(24, _renderer.Statistics.Triangles);
        }
    }

    [TestMethod]
    public void Projection_KnownWorldPoints_LandOnTheirProjectedPixels()
    {
        using var cube = _renderer.Upload(TestMeshes.Cube(0.25f, Vector4.One));
        var camera = new Camera3D { Position = new Vector3(2, 1.5f, 5), Target = new Vector3(0, 0.2f, 0), FieldOfView = MathF.PI / 3.5f };
        var target = _renderer.CreateTarget(W, H);
        var points = new (Vector3 World, Vector4 Color)[]
        {
            (new(0, 0, 0), Red), (new(1.5f, 1, -1), Green), (new(-1.5f, -0.5f, 0.5f), Blue), (new(-1, 1.2f, -2), new(1, 1, 0, 1)),
        };
        _renderer.Begin(target, camera, clear: PremultipliedColor.FromBytes(0, 0, 0));
        foreach (var (world, color) in points) _renderer.Draw(cube, Matrix4x4.CreateTranslation(world), new ColorF(color.X, color.Y, color.Z, color.W));
        _renderer.End();
        var image = Read(target);
        foreach (var (world, color) in points)
        {
            var (x, y) = PixelOf(camera, world);
            Assert.AreEqual(Bytes(color), image[x, y], $"{world} projects to ({x},{y})");
            // The cube's whole silhouette lies around that pixel: every coloured pixel within the projection of its corners.
            var corners = from dx in new[] { -1, 1 } from dy in new[] { -1, 1 } from dz in new[] { -1, 1 } select camera.Project(world + new Vector3(dx, dy, dz) * 0.125f, W, H)!.Value.Pixel;
            var c = corners.ToArray();
            var bounds = image.BoundsOf(Bytes(color));
            Assert.IsTrue(bounds.X >= (int)c.Min(p => p.X) - 1 && bounds.Right <= (int)MathF.Ceiling(c.Max(p => p.X)) + 1
                && bounds.Y >= (int)c.Min(p => p.Y) - 1 && bounds.Bottom <= (int)MathF.Ceiling(c.Max(p => p.Y)) + 1,
                $"{world}: drawn at {bounds}, projected corners span x {c.Min(p => p.X):F1}..{c.Max(p => p.X):F1}, y {c.Min(p => p.Y):F1}..{c.Max(p => p.Y):F1} (±1 px: edge pixels by the coverage rule)");
        }
    }

    [TestMethod]
    public void TwoD_DrawnAfterThreeD_IntoTheSameTarget_LandsOverIt()
    {
        using var cube = _renderer.Upload(TestMeshes.Cube(2, Red));
        var camera = Camera();
        var target = _renderer.CreateTarget(W, H);
        _renderer.Begin(target, camera, clear: PremultipliedColor.FromBytes(0, 0, 255));
        _renderer.Draw(cube, Matrix4x4.Identity);
        _renderer.End();
        using var batch = new Renderer2D(_device);
        batch.Begin(new BatchSettings(target)); // no clear: keeps the 3D frame
        batch.Draw(batch.White, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(20, 12) * Matrix3x2.CreateTranslation(W / 2 - 10, H / 2 - 6), new ColorF(1, 1, 0, 1));
        batch.Draw(batch.White, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(8, 8), new ColorF(0, 0.5f, 0, 0.5f)); // half-alpha green over the blue clear
        batch.End();
        var image = Read(target);
        Assert.AreEqual(0xFFFF00FFu, image[W / 2, H / 2], "the opaque 2D quad covers the cube");
        Assert.AreEqual(new RectI(W / 2 - 10, H / 2 - 6, 20, 12), image.BoundsOf(0xFFFF00FFu), "exactly its pixels");
        Assert.AreEqual(0xFF0000FFu, image[W / 2 + 14, H / 2], "the cube around it");
        // Premultiplied over: (0, 0.5, 0, 0.5) + (0, 0, 1, 1) x 0.5 = (0, 0.5, 0.5, 1): ±1/255 (8-bit quantization of the blend).
        Assert.IsTrue(Math.Abs(image.Channel(2, 2, 1) - 127.5) <= 1 && Math.Abs(image.Channel(2, 2, 2) - 127.5) <= 1, $"blend over the 3D clear: {image[2, 2]:X8}");
        Assert.AreEqual(0xFF0000FFu, image[W / 2 + 14, H / 2]);
    }

    [TestMethod]
    public void Interpolation_DrawsTheHierarchyBetweenTwoSteps_AtTheEngineBlend()
    {
        // A parent moves from x = -1 to x = +1 in one step and turns a quarter about Z; its child sits at local (0.8, 0, 0).
        // Drawn at blend b, the child is where the interpolated parent puts it: parent at x = -1 + 2b, turned b x 90°.
        var world = new SceneWorld();
        using var cube = _renderer.Upload(TestMeshes.Cube(0.3f, Red));
        var parent = world.Create();
        var child = world.Create(parent);
        world.Local(parent).Position = new Vector3(-1, 0, 0);
        world.Local(child).Position = new Vector3(0.8f, 0, 0);
        world.Attach(child, new MeshRenderer(cube));
        world.BeginStep(1, 1 / 60f);
        world.Local(parent).Position = new Vector3(1, 0, 0);
        world.Local(parent).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        world.EndStep();
        var camera = Camera();
        var target = _renderer.CreateTarget(W, H);
        foreach (var blend in new[] { 0f, 0.5f, 1f })
        {
            world.BeginDraw(1, blend);
            _renderer.Begin(target, camera, clear: PremultipliedColor.FromBytes(0, 0, 0));
            _renderer.DrawWorld(world);
            _renderer.End();
            float angle = blend * MathF.PI / 2;
            var expected = new Vector3(-1 + 2 * blend + 0.8f * MathF.Cos(angle), 0.8f * MathF.Sin(angle), 0);
            var drawn = Vector3.Transform(Vector3.Zero, world.WorldMatrix(child));
            Assert.IsTrue(Vector3.Distance(expected, drawn) < 1e-5f, $"blend {blend}: the world puts the child at {drawn}, expected {expected}");
            var image = Read(target);
            var (x, y) = PixelOf(camera, expected);
            Assert.AreEqual(0xFF0000FFu, image[x, y], $"blend {blend}: the child is drawn at its interpolated place ({x},{y})");
            Assert.AreEqual(1, _renderer.Statistics.Meshes);
        }
        var start = PixelOf(camera, new Vector3(-0.2f, 0, 0));
        world.BeginDraw(1, 1);
        _renderer.Begin(target, camera, clear: PremultipliedColor.FromBytes(0, 0, 0));
        _renderer.DrawWorld(world);
        _renderer.End();
        Assert.AreEqual(0x000000FFu, Read(target)[start.X, start.Y], "at blend 1 nothing is left at the step's start");
    }

    [TestMethod]
    public void GltfAsset_LoadedThroughTheStore_DrawsItsBaseColourTexture()
    {
        // A cube whose base colour texture is 2 x 2 (red, green / blue, white); seen face on, the front face shows the
        // four texels as quadrants (linear filtering blends only near their borders).
        var texture = new ImageData(2, 2, [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255]);
        var files = new Dictionary<string, byte[]> { ["crate.glb"] = TestMeshes.ToGlb(TestMeshes.CubeGltf(2, Vector4.One, texture)) };
        using var store = new AssetStore(new MemoryFiles(files), new AssetLoaderRegistry().AddMeshes());
        var mesh = store.Get(store.Root.Load<MeshData>(MeshAssets.Kind, "crate.glb"));
        using var cube = _renderer.Upload(mesh);
        var camera = Camera();
        var target = _renderer.CreateTarget(W, H);
        _renderer.Begin(target, camera, clear: PremultipliedColor.FromBytes(0, 0, 0));
        _renderer.Draw(cube, Matrix4x4.Identity);
        _renderer.End();
        var image = Read(target);
        uint At(float x, float y) { var p = PixelOf(camera, new Vector3(x, y, 1)); return image[p.X, p.Y]; }
        Assert.AreEqual(0xFF0000FFu, At(-0.6f, 0.6f), "top-left texel (row 0 at the top, as glTF's uv)");
        Assert.AreEqual(0x00FF00FFu, At(0.6f, 0.6f), "top-right");
        Assert.AreEqual(0x0000FFFFu, At(-0.6f, -0.6f), "bottom-left");
        Assert.AreEqual(0xFFFFFFFFu, At(0.6f, -0.6f), "bottom-right");
    }

    [TestMethod]
    public void Canvas_Flush_PutsThe3DPassBetweenThe2DDrawnBeforeAndAfterIt()
    {
        // The game's canvas on the tier-1 device: clear, a red 2D rectangle, Flush, a 3D cube over it, Flush, a blue 2D
        // rectangle over the cube. The cube covers the red; the blue covers the cube.
        using var cube = _renderer.Upload(TestMeshes.Cube(2, Green));
        using var canvas = new DeviceCanvas(_device, W, H);
        var camera = Camera();
        var r = canvas.BeginFrame();
        r.Clear(new MicroEngine.Color(0, 0, 0));
        r.FillRect(new RectF(0, 0, W, H), new MicroEngine.Color(255, 0, 0));
        canvas.Renderer.Flush();
        _renderer.Begin(canvas.Renderer.Target, camera);
        _renderer.Draw(cube, Matrix4x4.Identity);
        _renderer.End();
        canvas.Renderer.Flush();
        r.FillRect(new RectF(W / 2 - 4, H / 2 - 4, 8, 8), new MicroEngine.Color(0, 0, 255));
        canvas.EndFrame();
        var image = Read(canvas.Target);
        Assert.AreEqual(0xFF0000FFu, image[1, 1], "the red outside the cube");
        var (cx, cy) = PixelOf(camera, new Vector3(0.7f, 0.7f, 1));
        Assert.AreEqual(Bytes(Green), image[cx, cy], "the 3D pass after the first flush covers the red");
        Assert.AreEqual(0x0000FFFFu, image[W / 2, H / 2], "2D recorded after the flush lands over the 3D");
    }

    [TestMethod]
    public void Renderer_RefusesATargetWithAnotherDepthFormat()
    {
        var camera = Camera();
        var other = new[] { TextureFormat.D16Unorm, TextureFormat.D32Float, TextureFormat.D24UnormS8 }
            .FirstOrDefault(f => f != _renderer.DepthFormat && _device.Capabilities.Supports(f, TextureUsage.DepthStencil));
        var mismatched = _device.CreateRenderTarget(new RenderTargetDescription(8, 8, Depth: other));
        Assert.ThrowsExactly<InvalidOperationException>(() => _renderer.Begin(mismatched, camera));
    }

    [TestMethod]
    public void Depth_OnATargetWithoutDepth_UsesTheRenderersOwn_AndKeepsTheNearerMeshInFront()
    {
        // A game's canvas has no depth texture: the renderer depth-tests against one of its own of the canvas's size.
        using var near = _renderer.Upload(TestMeshes.Cube(1, Red));
        using var far = _renderer.Upload(TestMeshes.Cube(3, Green));
        var camera = Camera();
        var canvas = _device.CreateRenderTarget(new RenderTargetDescription(W, H));
        _renderer.Begin(canvas, camera, clear: PremultipliedColor.FromBytes(0, 0, 0));
        _renderer.Draw(near, Matrix4x4.CreateTranslation(0, 0, 1));
        _renderer.Draw(far, Matrix4x4.CreateTranslation(0, 0, -2));
        _renderer.End();
        var image = Read(canvas);
        var centre = PixelOf(camera, new Vector3(0, 0, 1.5f));
        Assert.AreEqual(Bytes(Red), image[centre.X, centre.Y]);
        var farOnly = PixelOf(camera, new Vector3(1.2f, 1.2f, -0.5f));
        Assert.AreEqual(Bytes(Green), image[farOnly.X, farOnly.Y]);
    }

    private sealed class MemoryFiles(Dictionary<string, byte[]> files) : IContentSource
    {
        public string Location => "memory";
        public bool Exists(string path) => files.ContainsKey(path);
        public Stream Open(string path) => files.TryGetValue(path, out var b) ? new MemoryStream(b) : throw new FileNotFoundException(path);
        public IReadOnlyList<string> Files(string folder) => [.. files.Keys.Order(StringComparer.Ordinal)];
    }
}
