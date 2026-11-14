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
        var platform = new HeadlessPlatform(320, 180) { Rasterize = true, PixelDensity = density, Presentation = game.Settings.Presentation };
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

    private static readonly Color Blue = new(0, 0, 255), Border = new(64, 64, 64);
    private static readonly Presentation Smooth = Presentation.PixelArt with { Rounding = ScaleRounding.Fractional, Upscale = Dev.SamplerDescription.LinearClamp };
    private static readonly Presentation Sharp = Smooth with { Prescale = Presentation.AutoPrescale };

    /// <summary>
    /// A canvas of four columns (red, blue, red, blue) and two rows composed by the device canvas into a window, a UI view
    /// with one white pixel at its corner over it: the frame the device presented and the canvas's two views.
    /// </summary>
    private static (PixelImage Presented, PixelImage World, PixelImage Ui, RectF Frame) Window(Presentation presentation, int width, int height)
    {
        var surface = new HostSurface(1, () => (width, height), () => 1f, () => false);
        using var device = new Dev.ReferenceGraphicsDevice(surface);
        using var canvas = new DeviceCanvas(device, 4, 2);
        var r = canvas.BeginFrame();
        for (int x = 0; x < 4; x++) r.FillRect(new RectF(x, 0, 1, 2), x % 2 == 0 ? Red : Blue);
        canvas.BeginUi(width, height, 1).FillRect(new RectF(0, 0, 1, 1), White);
        canvas.EndFrame();
        var frame = presentation.Fit(new Vector2(width, height), default, 4, 2);
        Assert.IsTrue(canvas.Compose(presentation, frame, Border));
        device.Present();
        var (w, h, rgba) = device.PresentedFrame!.Value;
        var presented = new PixelImage(w, h);
        for (int i = 0; i < w * h; i++) presented[i % w, i / w] = new Color(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], 255);
        return (presented, canvas.Capture(), canvas.CaptureUi()!, frame);
    }

    private static string Row(PixelImage image, int y) => string.Join(' ', Enumerable.Range(0, image.Width).Select(x => $"{image[x, y].R:X2}{image[x, y].G:X2}{image[x, y].B:X2}"));

    [TestMethod]
    public void Without_a_prescale_the_window_is_composed_as_it_always_was()
    {
        // 4x2 in 10x5 (2.5x). Nearest: canvas pixels of two and three window pixels; linear: blended. Both rows are the
        // frames the device canvas presented before presentations had a prescale.
        var nearest = Window(Presentation.PixelArt with { Rounding = ScaleRounding.Fractional }, 10, 5).Presented;
        Assert.AreEqual("FFFFFF FF0000 0000FF 0000FF 0000FF FF0000 FF0000 0000FF 0000FF 0000FF", Row(nearest, 0));
        Assert.AreEqual("FF0000 FF0000 0000FF 0000FF 0000FF FF0000 FF0000 0000FF 0000FF 0000FF", Row(nearest, 4));
        var smooth = Window(Smooth, 10, 5).Presented;
        Assert.AreEqual("FF0000 E6001A 800080 1A00E6 4C00B3 B3004C E60019 800080 1900E6 0000FF", Row(smooth, 2));
        Assert.AreEqual(Row(smooth, 2), Row(Window(Smooth with { Prescale = 1 }, 10, 5).Presented, 2));
        // With a letterbox: 4x2 in 11x5 is still 2.5x, ten pixels wide, the odd column on the right.
        var boxed = Window(Smooth, 11, 5).Presented;
        Assert.AreEqual(Row(smooth, 2) + " 404040", Row(boxed, 2));
    }

    [TestMethod]
    public void With_a_prescale_the_device_canvas_samples_the_enlarged_canvas()
    {
        // The same 2.5x: enlarged three times with nearest first, each canvas pixel keeps pure window pixels and the
        // blend stays on the pixel its edge crosses.
        var sharp = Window(Sharp, 10, 5).Presented;
        Assert.AreEqual(Red, sharp[0, 2]);
        Assert.AreEqual(Red, sharp[1, 2], "linear alone blends this pixel (E6001A)");
        Assert.AreEqual(new Color(128, 0, 128), sharp[2, 2], "the pixel the edge crosses in its middle");
        Assert.AreEqual(Blue, sharp[3, 2]);
        Assert.AreEqual(Blue, sharp[4, 2]);
        Assert.AreEqual(Blue, sharp[9, 2]);
        Assert.AreEqual(White, sharp[0, 0], "the UI view is still drawn over the world");

        // At a whole scale the prescaled picture is the nearest one, byte for byte.
        var nearest = Window(Presentation.PixelArt, 12, 6).Presented;
        var whole = Window(Sharp, 12, 6).Presented;
        for (int y = 0; y < 6; y++) Assert.AreEqual(Row(nearest, y), Row(whole, y), $"row {y}");

        // A fixed factor below the scale: softer edges than auto, sharper than linear alone.
        var soft = Window(Smooth with { Prescale = 2 }, 10, 5).Presented;
        Assert.AreEqual(Red, sharp[5, 2]);
        Assert.IsTrue(soft[5, 2].R > 0xB3 && soft[5, 2].R < 0xFF, $"between linear alone (B3004C) and auto (pure): {soft[5, 2]}");
    }

    [TestMethod]
    public void The_composite_of_a_prescaled_frame_matches_the_frame_the_device_presents()
    {
        foreach (var presentation in new[] { Sharp, Smooth with { Prescale = 2 }, Smooth, Sharp with { Aspect = AspectMode.Stretch } })
            foreach (var (width, height) in new[] { (10, 5), (11, 5), (13, 7), (12, 6), (3, 2) })
            {
                var (presented, world, ui, frame) = Window(presentation, width, height);
                var composite = ScreenCapture.Compose(world, ui, (width, height), frame, Border, presentation);
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        var (c, p) = (composite[x, y], presented[x, y]);
                        if (Math.Abs(c.R - p.R) > 1 || Math.Abs(c.G - p.G) > 1 || Math.Abs(c.B - p.B) > 1)
                            Assert.Fail($"prescale {presentation.Prescale}, {presentation.Aspect}, {width}x{height}, pixel {x},{y}: {c} captured, {p} presented");
                    }
            }
    }

    [TestMethod]
    public void Without_a_prescale_the_composite_is_the_one_of_the_upscale_sampler_alone()
    {
        var world = new PixelImage(5, 3);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 5; x++) world[x, y] = new Color((byte)(x * 60), (byte)(y * 120), (byte)((x + y) % 2 * 255));
        var ui = new PixelImage(17, 9);
        ui[3, 3] = new Color(100, 0, 0, 100);
        foreach (var presentation in new[] { Smooth, Presentation.PixelArt, Presentation.HighResolution2D, Sharp with { Prescale = 1 }, Sharp with { Upscale = Dev.SamplerDescription.PointClamp } })
            foreach (var frame in new[] { new RectF(0, 0, 17, 9), new RectF(1, 0, 15, 9), new RectF(3, 2, 10, 6), new RectF(6, 3, 5, 3), new RectF(7, 3, 2, 1) })
            {
                var before = ScreenCapture.Compose(world, ui, (17, 9), frame, Border, presentation.Upscale);
                var now = ScreenCapture.Compose(world, ui, (17, 9), frame, Border, presentation);
                CollectionAssert.AreEqual(before.ToRgba(), now.ToRgba(), $"{presentation.Upscale.Mag} in {frame}");
            }
    }

    [TestMethod]
    public void The_headless_platform_captures_a_prescaled_frame_as_a_window_shows_it()
    {
        // The game's 320x180 canvas in a 800x450 surface (2.5x): white ends at canvas pixel 4, window pixel 10.
        PixelImage Screen(Presentation presentation)
        {
            var game = new CaptureGame(new CaptureScene(ui: true));
            var platform = new HeadlessPlatform(320, 180) { Rasterize = true, Presentation = presentation };
            platform.Resize(800, 450);
            platform.PumpEvents(new IgnoredEvents());
            game.InitializeForTest(platform);
            game.StepForTest();
            game.Render(platform.BeginFrame());
            platform.EndFrame();
            return ((IViewCapture)platform).CaptureScreen();
        }
        var smooth = Screen(Smooth);
        var sharp = Screen(Sharp);
        Assert.AreEqual((800, 450), (sharp.Width, sharp.Height));
        Assert.AreNotEqual(White, smooth[9, 2] with { A = 255 }, "linear alone blends the window pixels on each side of the edge");
        Assert.AreEqual(White, sharp[9, 2] with { A = 255 }, "enlarged first, the pixels beside the edge stay pure");
        Assert.AreEqual(Color.Black, sharp[10, 2] with { A = 255 });
        Assert.AreEqual(Row(smooth, 20), Row(Screen(Smooth with { Prescale = 1 }), 20));
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
