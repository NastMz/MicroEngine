using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Platform;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests.Interface;

/// <summary>
/// Captures show what the player sees: the world view with the UI view composited over it when the frame drew a UI,
/// the world alone otherwise; the world-only capture stays available for tests that need it.
/// </summary>
[TestClass]
public sealed class ViewCaptureTests
{
    private static readonly Color Red = new(255, 0, 0), White = Color.White;

    private sealed class CaptureScene(bool ui) : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer)
        {
            renderer.Clear(Color.Black);
            renderer.FillRect(new RectF(0, 0, 4, 4), White);
        }
        public override void DrawUi(IRenderer r)
        {
            if (ui) r.FillRect(new RectF(100, 50, 10, 10), Red);
        }
    }

    private sealed class CaptureGame(Scene scene) : Game
    {
        public override GameSettings Settings { get; } = new("capture", 320, 180, 1);
        protected override void Load() => Scenes.Push(scene);
    }

    private static (HeadlessPlatform Platform, string Directory) Run(bool ui, float density = 1)
    {
        var game = new CaptureGame(new CaptureScene(ui));
        var platform = new HeadlessPlatform(320, 180) { Rasterize = true, PixelDensity = density, Presentation = game.Settings.EffectivePresentation };
        platform.Resize(640, 360);
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        game.StepForTest();
        game.Render(platform.BeginFrame());
        platform.EndFrame();
        var dir = Path.Combine(Path.GetTempPath(), "microengine-capture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return (platform, dir);
    }

    private static (int Width, int Height) PngSize(string path)
    {
        var bytes = File.ReadAllBytes(path);
        static int Be(byte[] b, int at) => b[at] << 24 | b[at + 1] << 16 | b[at + 2] << 8 | b[at + 3];
        return (Be(bytes, 16), Be(bytes, 20));
    }

    [TestMethod]
    public void A_frame_with_a_UI_is_captured_as_the_window_with_the_UI_over_the_world()
    {
        var (platform, dir) = Run(ui: true);
        try
        {
            var capture = (IViewCapture)platform;
            Assert.IsTrue(capture.FrameHasUi);
            var screen = capture.CaptureScreen();
            Assert.AreEqual((640, 360), (screen.Width, screen.Height), "the window's size");
            Assert.AreEqual(Red, screen[105, 55], "the UI at window pixels");
            Assert.AreEqual(White, screen[7, 7], "the world scaled into the window (4 canvas pixels are 8 window pixels)");
            Assert.AreEqual(Color.Black, screen[9, 9]);
            var file = Path.Combine(dir, "shot.png");
            platform.SaveScreenshot(file);
            Assert.AreEqual((640, 360), PngSize(file), "the run's capture is the composite");

            var world = capture.CaptureWorld();
            Assert.AreEqual((320, 180), (world.Width, world.Height));
            Assert.AreEqual(Color.Black, world[52, 27], "the world capture has no UI");
            var worldFile = Path.Combine(dir, "world.png");
            capture.SaveWorldScreenshot(worldFile);
            Assert.AreEqual((320, 180), PngSize(worldFile));
        }
        finally { Directory.Delete(dir, true); }
    }

    [TestMethod]
    public void On_a_density_2_surface_each_UI_unit_covers_two_window_pixels_in_the_capture()
    {
        var (platform, dir) = Run(ui: true, density: 2);
        try
        {
            var screen = ((IViewCapture)platform).CaptureScreen();
            Assert.AreEqual((640, 360), (screen.Width, screen.Height));
            Assert.AreEqual(Red, screen[201, 101], "UI unit 100, 50 is window pixel 200, 100");
            Assert.AreEqual(Red, screen[219, 119]);
            Assert.AreEqual(Color.Black, screen[221, 121], "the 10-unit square ends at window pixel 220");
        }
        finally { Directory.Delete(dir, true); }
    }

    [TestMethod]
    public void A_frame_without_a_UI_is_captured_as_before_the_canvas_alone()
    {
        var (platform, dir) = Run(ui: false);
        try
        {
            Assert.IsFalse(((IViewCapture)platform).FrameHasUi);
            Assert.IsNull(((IViewCapture)platform).CaptureUi());
            var file = Path.Combine(dir, "shot.png");
            platform.SaveScreenshot(file);
            Assert.AreEqual((320, 180), PngSize(file), "the canvas at scale 1, as headless captures always were");
        }
        finally { Directory.Delete(dir, true); }
    }

    [TestMethod]
    public void The_composite_matches_the_frame_the_device_presents()
    {
        var surface = new HostSurface(1, () => (640, 360), () => 1f, () => false);
        using var device = new Dev.ReferenceGraphicsDevice(surface);
        using var canvas = new DeviceCanvas(device, 320, 180);
        var presentation = Presentation.PixelArt;
        var r = canvas.BeginFrame();
        r.Clear(new Color(0, 0, 64));
        r.FillRect(new RectF(10, 10, 3, 2), White);
        canvas.BeginUi(640, 360, 1).FillRect(new RectF(101, 51, 7, 3), Red);
        canvas.EndFrame();
        var frame = presentation.Fit(new Vector2(640, 360), default, 320, 180);
        Assert.IsTrue(canvas.Compose(presentation, frame, Color.Black));
        device.Present();
        var (w, h, rgba) = device.PresentedFrame!.Value;

        var composite = ScreenCapture.Compose(canvas.Capture(), canvas.CaptureUi(), (640, 360), frame, Color.Black, presentation.Upscale);
        Assert.AreEqual((w, h), (composite.Width, composite.Height));
        for (int i = 0; i < w * h; i++)
        {
            var presented = new Color(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], 255);
            if (composite[i % w, i / w] != presented) Assert.Fail($"pixel {i % w},{i / w}: {composite[i % w, i / w]} captured, {presented} presented");
        }
    }

    [TestMethod]
    public void The_letterbox_surrounds_the_world_in_a_composite()
    {
        var world = new PixelImage(2, 2);
        for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++) world[x, y] = White;
        var ui = new PixelImage(8, 4);
        ui[7, 0] = new Color(255, 0, 0, 255);
        var composite = ScreenCapture.Compose(world, ui, (8, 4), new RectF(2, 0, 4, 4), new Color(9, 9, 9), Dev.SamplerDescription.PointClamp);
        Assert.AreEqual(new Color(9, 9, 9), composite[0, 0]);
        Assert.AreEqual(White, composite[2, 0]);
        Assert.AreEqual(White, composite[5, 3]);
        Assert.AreEqual(new Color(9, 9, 9), composite[6, 3]);
        Assert.AreEqual(Red, composite[7, 0], "the UI covers the letterbox too");
    }
}
