using System.Numerics;
using MicroEngine.Conformance.Graphics;
using MicroEngine.Conformance.Graphics3D;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics3D;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// A game on the tier-1 device through the normal composition path: the desktop launcher with <c>--graphics sdl-gpu</c>,
/// the engine's loop and scene world, a 3D pass between two flushes of the canvas, 2D over it, and the UI view at the
/// window's resolution. The scene reads its own target back inside the frame (after flushing), so the test sees exactly
/// what the frame drew. Also: the refusals when SDL_GPU cannot start, or a platform does not offer it.
/// </summary>
[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class TierOneGameTests
{
    private const int W = 160, H = 90;

    private sealed class CubeGame : Game
    {
        public override GameSettings Settings { get; } = new("tier-1 test", W, H, 2)
        {
            Background = new MicroEngine.Color(0, 0, 0),
            Scaling = CanvasScaling.Smooth,
        };
        public CubeScene Scene { get; } = new();
        protected override void Load() => Scenes.Push(Scene);
    }

    private sealed class CubeScene : Scene
    {
        public readonly Camera3D Camera = new() { Position = new Vector3(0, 0, 6), Target = Vector3.Zero, Near = 0.5f, Far = 50 };
        private Renderer3D? _renderer;
        private GpuMesh? _cube;
        public Image? World, Ui;
        public (int Width, int Height) UiSize;
        public string? DeviceBackend;

        public override void Enter()
        {
            var device = ((DeviceGraphics)Game.Graphics).Device;
            DeviceBackend = device.Capabilities.Backend;
            _renderer = new Renderer3D(device) { Ambient = 1 };
            _cube = _renderer.Upload(TestMeshes.Cube(2, new Vector4(1, 0, 0, 1)));
            var parent = Objects.Create();
            var child = Objects.Create(parent);
            Objects.Local(parent).Position = new Vector3(0.5f, 0, 0);
            Objects.Local(child).Position = new Vector3(-0.5f, 0, 0); // the hierarchy puts the cube at the origin
            Objects.Attach(child, new MeshRenderer(_cube));
        }

        public override void Exit()
        {
            _cube?.Dispose();
            _renderer?.Dispose();
        }

        public override void Update(float dt) { }

        public override void Draw(IRenderer r)
        {
            var canvas = (DeviceRenderer)r;
            canvas.Flush();
            _renderer!.Begin(canvas.Target, Camera);
            _renderer.DrawWorld(Objects);
            _renderer.End();
            r.FillRect(new RectF(0, 0, 20, 20), new MicroEngine.Color(0, 0, 255));
            r.FillRect(new RectF(W / 2 - 3, H / 2 - 3, 6, 6), new MicroEngine.Color(0, 255, 0)); // 2D over the 3D
            canvas.Flush();
            World = ReadBack(canvas.Target);
        }

        public override void DrawUi(IRenderer ui)
        {
            ui.FillRect(new RectF(4, 4, 10, 10), new MicroEngine.Color(255, 255, 0));
            var view = (DeviceRenderer)ui;
            view.Flush();
            UiSize = (view.Target.Width, view.Target.Height);
            Ui = ReadBack(view.Target);
        }

        private Image ReadBack(Dev.RenderTarget target)
        {
            var request = ((DeviceGraphics)Game.Graphics).Device.ReadPixels(target, target.Bounds);
            request.Wait();
            return new Image(target.Width, target.Height, request.Pixels.ToArray());
        }
    }

    private static (int Code, string Errors) Launch(Game game, params string[] args)
    {
        var errors = new StringWriter();
        var (previousError, previousOut) = (Console.Error, Console.Out);
        Console.SetError(errors);
        Console.SetOut(new StringWriter());
        try { return (DesktopLauncher.Run(game, [.. args, "--hidden", "--frames", "3"]), errors.ToString()); }
        finally
        {
            Console.SetError(previousError);
            Console.SetOut(previousOut);
        }
    }

    [TestMethod]
    public void A_game_selects_the_tier_1_device_and_draws_3D_2D_and_its_UI_view_on_it()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
        var game = new CubeGame();
        var (code, errors) = Launch(game, "--platform", "sdl", "--graphics", "sdl-gpu");
        if (code != 0 && errors.Contains("cannot start here")) Native.Require(false, errors.Trim());
        Assert.AreEqual(0, code, errors);
        var scene = game.Scene;
        Assert.AreEqual("sdl-gpu", scene.DeviceBackend, "the game's textures and the 3D path share the tier-1 device");
        var world = scene.World!;
        Assert.AreEqual((W, H), (world.Width, world.Height), "the world view is the canvas");
        var (cx, cy) = scene.Camera.Project(new Vector3(0.7f, 0.7f, 1), W, H)!.Value.Pixel is var p ? ((int)p.X, (int)p.Y) : default;
        Assert.AreEqual(0xFF0000FFu, world[cx, cy], "the 3D cube, placed by the world's hierarchy, in the canvas");
        Assert.AreEqual(0x0000FFFFu, world[2, 2], "2D drawn after the 3D pass");
        Assert.AreEqual(0x00FF00FFu, world[W / 2, H / 2], "2D over the 3D cube");
        Assert.AreEqual(0x000000FFu, world[W - 2, H - 2], "the game's background where nothing is drawn");
        var ui = scene.Ui!;
        Assert.IsTrue(scene.UiSize.Width >= W && scene.UiSize.Height >= H, $"the UI view is at the window's resolution ({scene.UiSize}), not the canvas's");
        Assert.AreEqual(0xFFFF00FFu, ui[(int)(8 * ui.Width / (float)scene.UiSize.Width), 8], "the UI view's own drawing");
    }

    [TestMethod]
    public void The_tier_1_device_on_a_platform_that_does_not_offer_it_is_refused_with_its_reason()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
        var (code, errors) = Launch(new CubeGame(), "--platform", "sdl-software", "--graphics", "sdl-gpu");
        Assert.AreEqual(1, code);
        StringAssert.Contains(errors, "Platform 'sdl-software' has no graphics device 'sdl-gpu'; it offers: sdl-renderer.");
        (code, errors) = Launch(new CubeGame(), "--platform", "sdl", "--graphics", "vulkan9");
        Assert.AreEqual(1, code);
        StringAssert.Contains(errors, "has no graphics device 'vulkan9'; it offers: sdl-renderer, sdl-gpu.");
    }

    [TestMethod]
    public void SDL_GPU_that_cannot_start_here_is_a_clear_error_never_a_tier_0_fallback()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux()) Assert.Inconclusive("Metal exists on this system");
        // Asking SDL_GPU for Metal on a system without it: the platform says why and how to run, and does not start.
        var game = new CubeGameWithRenderers(["metal"]);
        var (code, errors) = Launch(game, "--platform", "sdl", "--graphics", "sdl-gpu");
        Assert.AreEqual(1, code, errors);
        StringAssert.Contains(errors, "SDL's graphics device 'sdl-gpu' cannot start here");
        StringAssert.Contains(errors, "--graphics sdl-renderer");
        Assert.IsNull(game.Scene.DeviceBackend, "the game never ran on another device");
    }

    private sealed class CubeGameWithRenderers(string[] renderers) : Game
    {
        public override GameSettings Settings { get; } = new("tier-1 test", W, H, 2) { Graphics = new GraphicsSettings { Renderers = renderers } };
        public CubeScene Scene { get; } = new();
        protected override void Load() => Scenes.Push(Scene);
    }
}
