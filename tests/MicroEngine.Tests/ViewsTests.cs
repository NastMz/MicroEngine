using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests;

/// <summary>
/// The world view (the canvas, at its resolution) and the UI view (the window's resolution) composited over it: what each
/// draws, the scene hook that draws into each, and the pointer mapped into each.
/// </summary>
[TestClass]
public sealed class ViewsTests
{
    private static readonly Color Red = new(255, 0, 0), White = Color.White, Bg = new(0, 0, 64);

    /// <summary>A 320 × 180 canvas presented in a 640 × 360 window: two window pixels per canvas pixel.</summary>
    private static (PixelImage Window, DeviceCanvas Canvas, Dev.ReferenceGraphicsDevice Device) Compose(Presentation presentation,
        Action<IRenderer, IGraphicsDevice> world, Action<IRenderer, IGraphicsDevice>? ui, int windowWidth = 640, int windowHeight = 360)
    {
        var surface = new HostSurface(1, () => (windowWidth, windowHeight), () => 1f, () => false);
        var device = new Dev.ReferenceGraphicsDevice(surface);
        var canvas = new DeviceCanvas(device, 320, 180);
        var r = canvas.BeginFrame();
        r.Clear(Bg);
        world(r, canvas.Graphics);
        if (ui is not null) ui(canvas.BeginUi(windowWidth, windowHeight, presentation.UiScaleFor(1)), canvas.Graphics);
        canvas.EndFrame();
        var frame = presentation.Fit(new Vector2(windowWidth, windowHeight), default, 320, 180);
        Assert.IsTrue(canvas.Compose(presentation, frame, Color.Black));
        device.Present();
        var (w, h, rgba) = device.PresentedFrame!.Value;
        var image = new PixelImage(w, h);
        for (int i = 0; i < w * h; i++) image[i % w, i / w] = new Color(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], 255);
        return (image, canvas, device);
    }

    private static RectF LitBounds(PixelImage image, Color color)
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
                if (image[x, y] == color) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
        return x1 < 0 ? default : new RectF(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    [TestMethod]
    public void The_UI_view_draws_at_window_resolution_over_the_world_drawn_at_canvas_resolution()
    {
        var (window, canvas, device) = Compose(Presentation.PixelArt,
            (world, _) => world.FillRect(new RectF(10, 10, 1, 1), White),
            (ui, _) => ui.FillRect(new RectF(101, 51, 1, 1), Red));
        using (device) using (canvas)
        {
            Assert.AreEqual(new RectF(20, 20, 2, 2), LitBounds(window, White), "one canvas pixel is a 2 × 2 block of window pixels");
            Assert.AreEqual(new RectF(101, 51, 1, 1), LitBounds(window, Red), "one UI unit is one window pixel");
            Assert.AreEqual(new Color(0, 0, 64), window[0, 0], "the transparent rest of the UI view shows the world under it");
            Assert.AreEqual(640, canvas.UiRenderer!.Width);
            Assert.AreEqual(360, canvas.UiRenderer.Height);
        }
    }

    [TestMethod]
    public void UI_text_has_the_window_resolution_while_world_text_has_the_canvas_resolution()
    {
        BitmapFont? font = null;
        var (window, canvas, device) = Compose(Presentation.PixelArt,
            (world, g) => (font = BitmapFont.CreateDefault(g)).Draw(world, "I", new Vector2(10, 10), White),
            (ui, _) => font!.Draw(ui, "I", new Vector2(200, 200), Red));
        using (device) using (canvas)
        {
            var worldInk = LitBounds(window, White);
            var uiInk = LitBounds(window, Red);
            Assert.IsTrue(uiInk.Width > 0 && worldInk.Width > 0);
            Assert.AreEqual(uiInk.Width * 2, worldInk.Width, "the same glyph covers twice the window pixels in the world view");
            Assert.AreEqual(uiInk.Height * 2, worldInk.Height);
        }
    }

    [TestMethod]
    public void A_UI_scale_makes_UI_units_larger_than_window_pixels()
    {
        var (window, canvas, device) = Compose(Presentation.PixelArt with { UiScale = 2 },
            (_, _) => { }, (ui, _) => { Assert.AreEqual((320, 180), (ui.Width, ui.Height)); ui.FillRect(new RectF(50, 50, 1, 1), Red); });
        using (device) using (canvas) Assert.AreEqual(new RectF(100, 100, 2, 2), LitBounds(window, Red));
    }

    [TestMethod]
    public void An_expanding_presentation_grows_the_canvas_with_a_wider_window()
    {
        var expand = Presentation.PixelArt with { Aspect = AspectMode.Expand };
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 320, 180);
        var (w, h) = expand.CanvasSize(new RectF(0, 0, 2560, 1080), 320, 180);
        Assert.IsTrue(canvas.EnsureSize(w, h));
        Assert.IsFalse(canvas.EnsureSize(w, h), "the same size twice changes nothing");
        var r = canvas.BeginFrame();
        Assert.AreEqual((426, 180), (r.Width, r.Height), "the renderer draws on the grown canvas");
        canvas.EndFrame();
    }

    /// <summary>One drawn frame, as the loop's render and present phases draw it.</summary>
    private static void Frame(Game game, IPlatform platform)
    {
        game.StepForTest();
        game.Render(platform.BeginFrame());
        platform.EndFrame();
    }

    private sealed class ViewsGame(Scene scene) : Game
    {
        public override GameSettings Settings { get; } = new("views", 320, 180, 1);
        protected override void Load() => Scenes.Push(scene);
    }

    private sealed class HookScene : Scene
    {
        public (int, int) UiSize;
        public int WorldDraws, UiDraws;
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer)
        {
            WorldDraws++;
            renderer.FillRect(new RectF(0, 0, 4, 4), White);
        }
        public override void DrawUi(IRenderer ui)
        {
            UiDraws++;
            UiSize = (ui.Width, ui.Height);
            ui.FillRect(new RectF(0, 0, 4, 4), Red);
        }
        public override bool UiContains(Vector2 uiPoint) => uiPoint.X < 100 && uiPoint.Y < 50;
    }

    [TestMethod]
    public void Scenes_draw_their_world_in_the_canvas_and_their_UI_in_the_UI_view()
    {
        var scene = new HookScene { WorldCamera = new Camera2D(320, 180) { Position = new Vector2(8, 0) } };
        var game = new ViewsGame(scene);
        var platform = HeadlessPlatform.For(game.Settings);
        platform.Resize(640, 360);
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        Frame(game, platform);
        Assert.AreEqual(1, scene.WorldDraws);
        Assert.AreEqual(1, scene.UiDraws);
        Assert.AreEqual((640, 360), scene.UiSize, "the UI view is the window's size in UI units");
        Assert.AreEqual(1, platform.UiRenderer.DrawCalls, "the UI went to the UI view");
        Assert.AreEqual(0, platform.Renderer.CameraDepth, "the world camera was pushed and popped around Draw");
        Assert.IsTrue(game.Views.HasScreenView);
    }

    [TestMethod]
    public void The_pointer_is_mapped_into_each_view_and_reaches_the_UI_first()
    {
        var scene = new HookScene();
        var game = new ViewsGame(scene);
        var platform = HeadlessPlatform.For(game.Settings);
        platform.Resize(640, 360);
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        var views = game.Views;
        Assert.AreEqual(new RectF(0, 0, 640, 360), views.WorldFrame);
        Assert.AreEqual(new Vector2(20, 30), views.CanvasToUi(new Vector2(10, 15)), "canvas pixel 10, 15 is window pixel 20, 30");
        Assert.AreEqual(new Vector2(10, 15), views.UiToCanvas(new Vector2(20, 30)));
        Assert.AreEqual(new Vector2(10, 15), views.ScreenToCanvas(new Vector2(20, 30)));

        platform.Keys.Pointer = new PointerState(true, new Vector2(30, 10), false, false, false); // canvas pixels
        game.StepForTest();
        Assert.AreEqual(new Vector2(60, 20), views.UiPointer.Position);
        Assert.IsTrue(views.PointerOnUi, "UI point 60, 20 lies on the scene's UI");
        Assert.IsFalse(views.WorldPointer().Present, "the UI view took the pointer");

        platform.Keys.Pointer = new PointerState(true, new Vector2(100, 100), false, false, false);
        game.StepForTest();
        Assert.IsFalse(views.PointerOnUi);
        var camera = new Camera2D(320, 180) { Position = new Vector2(50, 0) };
        Assert.AreEqual(new Vector2(150, 100), views.WorldPointer(camera).Position, "the world pointer in the camera's world");
    }

    [TestMethod]
    public void Without_a_UI_view_the_UI_is_drawn_in_the_canvas()
    {
        var scene = new HookScene();
        var game = new ViewsGame(scene);
        var platform = new NoViewPlatform(HeadlessPlatform.For(game.Settings));
        game.InitializeForTest(platform);
        Frame(game, platform);
        Assert.AreEqual((320, 180), scene.UiSize, "UI units are canvas pixels");
        Assert.IsFalse(game.Views.HasScreenView);
        Assert.AreEqual(new Vector2(10, 15), game.Views.CanvasToUi(new Vector2(10, 15)));
    }

    [TestMethod]
    public void The_UI_scale_follows_the_surfaces_pixel_density_unless_the_game_sets_one()
    {
        static GameViews Views(float density, float? uiScale)
        {
            var game = new ViewsGame(new HookScene());
            var platform = new HeadlessPlatform(320, 180)
            {
                PixelDensity = density,
                Presentation = Presentation.PixelArt with { UiScale = uiScale },
            };
            platform.Resize(640, 360);
            platform.PumpEvents(new IgnoredEvents());
            game.InitializeForTest(platform);
            return game.Views;
        }
        Assert.AreEqual(1f, Views(1, null).UiScale, "headless: density 1 by default");
        Assert.AreEqual(new Vector2(640, 360), Views(1, null).UiSize);
        Assert.AreEqual(2f, Views(2, null).UiScale, "a density-2 surface gets two window pixels per UI unit");
        Assert.AreEqual(new Vector2(320, 180), Views(2, null).UiSize);
        Assert.AreEqual(1f, Views(2, 1).UiScale, "an explicit UI scale overrides the density");
        Assert.AreEqual(new Vector2(640, 360), Views(2, 1).UiSize);
        Assert.AreEqual(1f, new HeadlessPlatform().MainSurface.PixelDensity, "the headless surface's density is 1 unless set");
    }

    [TestMethod]
    public void A_density_2_UI_view_draws_UI_units_as_two_window_pixels()
    {
        var scene = new HookScene();
        var game = new ViewsGame(scene);
        var platform = new HeadlessPlatform(320, 180) { PixelDensity = 2, Presentation = game.Settings.EffectivePresentation };
        platform.Resize(640, 360);
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        Frame(game, platform);
        Assert.AreEqual((320, 180), scene.UiSize, "the UI view is the window's size in UI units");
    }

    /// <summary>A platform that hides the headless platform's UI view, as a third-party platform without one would.</summary>
    private sealed class NoViewPlatform(HeadlessPlatform inner) : IPlatform
    {
        public IGraphicsDevice Graphics => inner.Graphics;
        public MicroEngine.Audio.IAudioDevice Audio => inner.Audio;
        public void ToggleFullscreen() => inner.ToggleFullscreen();
        public IInputSource Input => inner.Input;
        public IRenderer BeginFrame() => inner.BeginFrame();
        public void EndFrame() => inner.EndFrame();
        public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
        public Color Letterbox { get => inner.Letterbox; set => inner.Letterbox = value; }
        public IMonotonicClock Clock => ((IPlatformHost)inner).Clock;
        public bool PumpEvents(IEngineEventSink sink) => inner.PumpEvents(sink);
        public ISurface? MainSurface => ((IPlatformHost)inner).MainSurface;
        public bool CanBlock => inner.CanBlock;
        public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class
        {
            if (typeof(T) == typeof(IScreenView)) { capability = null; return false; }
            return inner.TryGetCapability(out capability);
        }
        public void Dispose() => inner.Dispose();
    }
}
