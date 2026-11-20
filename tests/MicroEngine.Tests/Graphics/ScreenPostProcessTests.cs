using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests;

/// <summary>
/// The window stage: a post-processing chain over the finished frame at the window's resolution
/// (<see cref="Game.ScreenPostProcess"/>, <see cref="IScreenEffects"/>), the letterbox and the UI view included. On a
/// graphics device (the CPU reference device with a surface) and on the headless platform's captures.
/// </summary>
[TestClass]
public sealed class ScreenPostProcessTests
{
    private static readonly Color Red = new(255, 0, 0), White = Color.White, Bg = new(0, 0, 64), Box = new(20, 20, 20);

    /// <summary>
    /// A 320 × 180 canvas (a white world square, a red UI square) presented in a 640 × 400 window through
    /// <paramref name="effects"/>: two window pixels per canvas pixel and twenty rows of letterbox above and below.
    /// </summary>
    private static (PixelImage Window, PixelImage? Captured) Present(PostProcess? effects, Presentation? presentation = null)
    {
        presentation ??= Presentation.PixelArt;
        var surface = new HostSurface(1, () => (640, 400), () => 1f, () => false);
        using var device = new Dev.ReferenceGraphicsDevice(surface);
        using var canvas = new DeviceCanvas(device, 320, 180);
        var r = canvas.BeginFrame();
        r.Clear(Bg);
        r.FillRect(new RectF(10, 10, 20, 20), White);
        canvas.BeginUi(640, 400, 1).FillRect(new RectF(100, 100, 40, 40), Red);
        canvas.EndFrame();
        var frame = presentation.Fit(new Vector2(640, 400), default, 320, 180);
        Assert.AreEqual(new RectF(0, 20, 640, 360), frame);
        Assert.IsTrue(canvas.Compose(presentation, frame, Box, effects));
        device.Present();
        var (w, h, rgba) = device.PresentedFrame!.Value;
        var window = new PixelImage(w, h);
        for (int i = 0; i < w * h; i++) window[i % w, i / w] = new Color(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], 255);
        return (window, canvas.CapturePresented());
    }

    [TestMethod]
    public void The_screens_chain_covers_the_world_the_interface_and_the_letterbox_at_the_windows_resolution()
    {
        // Two hundred lines in four hundred window rows: every other window row is black, half a canvas pixel high.
        var (window, captured) = Present(new PostProcess().Add(new Scanlines { Strength = 1, Lines = 200 }));
        Assert.AreEqual(White, window[30, 50], "the world, on a bright row");
        Assert.AreEqual(Color.Black, window[30, 51], "and on a dark row: half of a canvas pixel, so at the window's resolution");
        Assert.AreEqual(Red, window[110, 110], "the interface, on a bright row");
        Assert.AreEqual(Color.Black, window[110, 111], "the interface is covered too");
        Assert.AreEqual(Box, window[5, 4], "the letterbox, on a bright row");
        Assert.AreEqual(Color.Black, window[5, 5], "and the letterbox is part of the picture");
        Assert.IsNotNull(captured, "a frame presented through a chain can be captured as it was shown");
        CollectionAssert.AreEqual(window.Pixels.ToArray(), captured.Pixels.ToArray());
    }

    [TestMethod]
    public void Without_an_active_chain_the_window_is_composed_as_always()
    {
        var (plain, none) = Present(null);
        Assert.IsNull(none, "nothing was presented through a chain");
        var (inactive, alsoNone) = Present(new PostProcess().Add(new Vignette { Enabled = false }));
        Assert.IsNull(alsoNone);
        CollectionAssert.AreEqual(plain.Pixels.ToArray(), inactive.Pixels.ToArray(), "an inactive chain changes nothing");
        // A chain that changes no pixel gives the same window as none: the stage itself adds nothing.
        var (through, _) = Present(new PostProcess().Add(new ColorGrade { Brightness = 1.0001f, Tint = White }));
        CollectionAssert.AreEqual(plain.Pixels.ToArray(), through.Pixels.ToArray(), "the window stage copies the composed window pixel for pixel");
    }

    [TestMethod]
    public void The_chain_sees_the_prescaled_canvas_as_the_window_shows_it()
    {
        var smooth = Presentation.PixelArt with { Upscale = Dev.SamplerDescription.LinearClamp, Prescale = Presentation.AutoPrescale };
        var (plain, _) = Present(null, smooth);
        var (through, _) = Present(new PostProcess().Add(new ColorGrade { Brightness = 1.0001f }), smooth);
        CollectionAssert.AreEqual(plain.Pixels.ToArray(), through.Pixels.ToArray());
    }

    // ---- In a game -----------------------------------------------------------------------------------------------------

    private sealed class Painted : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer r) => r.FillRect(new RectF(4, 4, 8, 8), White);
        public override void DrawUi(IRenderer ui) => ui.FillRect(new RectF(40, 4, 8, 8), White);
    }

    private sealed class ScreenGame : Game
    {
        public override GameSettings Settings { get; } = new("screen", 64, 16) { Background = Bg };
        protected override void Load() => Scenes.Push(new Painted());
    }

    private sealed class IgnoredEvents : IEngineEventSink
    {
        public void Resized(MicroEngine.Kernel.SurfaceResized size) { }
        public void FocusChanged(bool focused) { }
        public void Suspending() { }
        public void Resumed() { }
        public void QuitRequested() { }
    }

    private static (ScreenGame Game, HeadlessPlatform Platform) Start()
    {
        var game = new ScreenGame();
        var platform = HeadlessPlatform.For(game.Settings, rasterize: true);
        platform.Resize(128, 32);                     // two window pixels per canvas pixel
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        game.StepForTest();
        return (game, platform);
    }

    private static void Render(Game game, IPlatform platform)
    {
        game.Render(platform.BeginFrame());
        platform.EndFrame();
    }

    [TestMethod]
    public void A_games_screen_chain_reaches_the_platforms_window_stage_and_its_captures()
    {
        var (game, platform) = Start();
        var capture = (IViewCapture)platform;
        Render(game, platform);
        Assert.IsNull(platform.ScreenEffects, "no effect: nothing is handed to the platform");
        Assert.AreEqual(White, capture.CaptureScreen()[42, 6]);

        game.ScreenPostProcess.Add(new ColorGrade { Tint = Red });
        Render(game, platform);
        Assert.AreSame(game.ScreenPostProcess, platform.ScreenEffects, "the game hands its chain to the window stage every frame");
        var window = capture.CaptureScreen();
        Assert.AreEqual((128, 32), (window.Width, window.Height), "the window, at its resolution");
        Assert.AreEqual(Red, window[42, 6], "the interface went through the chain");
        Assert.AreEqual(Red, window[10, 10], "and so did the world");
        Assert.AreEqual(White, capture.CaptureWorld()[5, 5], "the canvas itself is as the scenes drew it");

        game.ScreenPostProcess.Enabled = false;
        Render(game, platform);
        Assert.IsNull(platform.ScreenEffects, "a disabled chain is taken away again");
        game.ScreenPostProcess.Enabled = true;
        Render(game, platform);
        game.Shutdown();
        Assert.IsNull(platform.ScreenEffects, "a game that ended leaves the platform without its chain");
    }

    /// <summary>A platform that hides the headless platform's views and window stage, as a third-party platform without them would.</summary>
    private sealed class CanvasOnlyPlatform(HeadlessPlatform inner) : IPlatform
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
            if (typeof(T) == typeof(IScreenView) || typeof(T) == typeof(IScreenEffects)) { capability = null; return false; }
            return inner.TryGetCapability(out capability);
        }
        public void Dispose() => inner.Dispose();
    }

    [TestMethod]
    public void On_a_platform_without_a_window_stage_the_screen_chain_runs_on_the_canvas_over_the_interface_drawn_there()
    {
        var game = new ScreenGame();
        var inner = new HeadlessPlatform(64, 16) { Rasterize = true };
        var platform = new CanvasOnlyPlatform(inner);
        game.InitializeForTest(platform);
        game.StepForTest();
        game.ScreenPostProcess.Add(new ColorGrade { Tint = Red });
        Render(game, platform);
        var canvas = ((IViewCapture)inner).CaptureWorld();
        Assert.AreEqual(Red, canvas[5, 5], "the world");
        Assert.AreEqual(Red, canvas[42, 6], "and the interface, which this platform draws into the canvas");
        Assert.IsNull(inner.ScreenEffects, "the platform was not asked");
    }
}
