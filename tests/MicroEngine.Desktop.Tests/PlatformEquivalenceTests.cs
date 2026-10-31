using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Desktop;
using MicroEngine.Graphics;
using MicroEngine.Raylib;
using MicroEngine.Scenes;
using MicroEngine.Sdl;

[assembly: DoNotParallelize]

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The official desktop platforms are interchangeable: the same game, unchanged, draws the same pixels on SDL (with each
/// GPU driver this machine has) and on raylib, and a subsystem of one backend can be replaced by another backend's.
/// These tests open real windows and devices; without a display they are skipped (failed under MICROENGINE_NATIVE_TESTS=1).
/// </summary>
[TestClass]
public sealed class PlatformEquivalenceTests
{
    private static readonly Color Background = new(10, 20, 30);
    private static readonly Color Red = new(255, 0, 0);

    /// <summary>One frame exercising every <see cref="IRenderer"/> operation, captured at update 1.</summary>
    private sealed class ProbeGame : Game
    {
        public override GameSettings Settings { get; } = new("probe", 64, 36, 2) { Background = new Color(10, 20, 30) };
        public int SoundsPlayed { get; private set; }
        protected override void Load()
        {
            Audio.AddSound("blip", Synth.Render(new SfxParams { Frequency = 660, Sustain = 0.05f }));
            Scenes.Push(new ProbeScene(this));
        }
        public void Blip() { Audio.Play("blip"); SoundsPlayed++; }
    }

    private sealed class ProbeScene(ProbeGame game) : Scene
    {
        private Texture? _texture;
        public override void Update(float dt) => game.Blip();
        public override void Draw(IRenderer r)
        {
            _texture ??= game.Graphics.CreateTexture(Texel());
            var src = _texture.Bounds;
            r.FillRect(new RectF(2, 2, 4, 3), Red);
            r.Draw(_texture, src, new Vector2(10, 2), Color.White);
            r.Draw(_texture, src, new Vector2(16, 2), Color.White, new DrawOptions(FlipX: true));
            r.Draw(_texture, src, new Vector2(22, 2), Color.White, new DrawOptions(Scale: new Vector2(2)));
            r.Draw(_texture, src, new Vector2(40, 2), new Color(255, 255, 255, 128), new DrawOptions(Rotation: MathF.PI / 2));
            r.FillRect(new RectF(2, 10, 4, 4), new Color(100, 0, 0));
            r.SetBlendMode(BlendMode.Additive);
            r.FillRect(new RectF(2, 10, 4, 4), new Color(50, 60, 0));
            r.SetBlendMode(BlendMode.Alpha);
            r.FillRect(new RectF(8, 10, 4, 4), Color.White);
            r.SetBlendMode(BlendMode.Multiply);
            r.FillRect(new RectF(8, 10, 4, 4), new Color(128, 64, 255));
            r.SetBlendMode(BlendMode.Alpha);
            r.SetClip(new RectF(14, 10, 2, 2));
            r.FillRect(new RectF(12, 8, 8, 8), new Color(0, 255, 0));
            r.SetClip(null);
            r.PushCamera(new Camera2D(64, 36) { Position = new Vector2(-30, -20) });
            r.FillTriangle(new Vector2(0, 0), new Vector2(8, 0), new Vector2(0, 8), new Color(255, 255, 0));
            r.PopCamera();
            r.FillCircle(new Vector2(50, 26), 5, new Color(0, 128, 255));
            r.DrawLine(new Vector2(2, 30), new Vector2(30, 34), 2, new Color(255, 0, 255, 200));
        }
    }

    /// <summary>4 × 4 texture whose every texel differs: (x·60, y·60, 200).</summary>
    private static PixelImage Texel()
    {
        var image = new PixelImage(4, 4);
        for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) image[x, y] = new Color((byte)(x * 60), (byte)(y * 60), 200);
        return image;
    }

    private static PlatformRequest Request(ProbeGame game) => new(game.Settings) { Hidden = true, FixedStep = true, Mute = true, CaptureScale = 1 };

    /// <summary>Runs the probe on a platform and returns the captured frame, or skips when the platform cannot open here.</summary>
    private static PixelImage Capture(PlatformProfile profile, Func<ProbeGame, PlatformRequest>? request = null)
    {
        var game = new ProbeGame();
        IPlatform platform;
        try { platform = profile.Create((request ?? Request)(game)); }
        catch (InvalidOperationException e)
        {
            Native.Require(false, $"{profile.Name} cannot open a window here: {e.Message}");
            throw;
        }
        string dir = Path.Combine(Path.GetTempPath(), "microengine-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (platform)
            {
                var result = GameRunner.Run(game, platform, new RunOptions { Script = new InputScript().Capture(1, "probe.png").QuitAfter(2), CaptureDirectory = dir });
                Assert.IsNull(result.Error, result.Error?.ToString());
            }
            return PixelImage.Load(Path.Combine(dir, "probe.png"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    private static void AssertSameColours(PixelImage expected, PixelImage actual, string what)
    {
        Assert.AreEqual(expected.Width, actual.Width, what);
        Assert.AreEqual(expected.Height, actual.Height, what);
        var differing = new List<string>();
        for (int y = 0; y < expected.Height; y++)
            for (int x = 0; x < expected.Width; x++)
            {
                Color a = expected[x, y], b = actual[x, y];
                if (a.R != b.R || a.G != b.G || a.B != b.B) differing.Add($"({x},{y}) {a.R},{a.G},{a.B} vs {b.R},{b.G},{b.B}");
            }
        Assert.IsEmpty(differing, $"{what}: {differing.Count} pixels differ, first {string.Join("; ", differing.Take(6))}");
    }

    private static void AssertRgb(PixelImage image, int x, int y, Color expected)
    {
        var c = image[x, y];
        Assert.IsTrue(Math.Abs(c.R - expected.R) <= 1 && Math.Abs(c.G - expected.G) <= 1 && Math.Abs(c.B - expected.B) <= 1,
            $"pixel ({x},{y}) is {c.R},{c.G},{c.B}, expected {expected.R},{expected.G},{expected.B}");
    }

    [TestMethod]
    public void SdlDrawsEveryRendererOperationWithTheEngineConventions()
    {
        var frame = Capture(SdlPlatform.Profile);
        AssertRgb(frame, 0, 0, Background);
        AssertRgb(frame, 2, 2, Red);
        AssertRgb(frame, 5, 4, Red);
        AssertRgb(frame, 6, 2, Background);
        var tex = Texel();
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                AssertRgb(frame, 10 + x, 2 + y, tex[x, y]);
                AssertRgb(frame, 16 + x, 2 + y, tex[3 - x, y]);
                AssertRgb(frame, 22 + 2 * x + 1, 2 + 2 * y + 1, tex[x, y]);
            }
        AssertRgb(frame, 3, 11, new Color(150, 60, 0));
        AssertRgb(frame, 9, 11, new Color(128, 64, 255));
        AssertRgb(frame, 14, 10, new Color(0, 255, 0));
        AssertRgb(frame, 15, 11, new Color(0, 255, 0));
        AssertRgb(frame, 13, 10, Background);
        AssertRgb(frame, 16, 12, Background);
        AssertRgb(frame, 31, 21, new Color(255, 255, 0));
        AssertRgb(frame, 50, 26, new Color(0, 128, 255));
    }

    [TestMethod]
    public void EverySdlGpuDriverDrawsTheSameFrame()
    {
        var reference = Capture(SdlPlatform.Profile);
        var tried = new List<string>();
        foreach (var driver in SdlPlatform.RenderDrivers.Where(d => d is not ("software" or "gpu")))
        {
            PixelImage frame;
            try { frame = Capture(SdlPlatform.CreateProfile(driver)); }
            catch (AssertInconclusiveException) { continue; }
            AssertSameColours(reference, frame, $"sdl-{driver}");
            tried.Add(driver);
        }
        Native.Require(tried.Count >= 2, $"fewer than two GPU drivers could open here ({string.Join(", ", tried)})");
    }

    [TestMethod]
    public void TheSameGameDrawsTheSameFrameOnSdlAndRaylib()
    {
        var sdl = Capture(SdlPlatform.Profile);
        var raylib = Capture(RaylibPlatform.Profile);
        AssertSameColours(raylib, sdl, "sdl vs raylib");
    }

    [TestMethod]
    public void RaylibCanRunWithSdlAudioInsteadOfItsOwn()
    {
        var game = new ProbeGame();
        IPlatform platform;
        try
        {
            platform = RaylibPlatform.Profile.Create(Request(game) with { Mute = false, Audio = SdlAudioDevice.TryCreate });
        }
        catch (InvalidOperationException e)
        {
            Native.Require(false, $"raylib cannot open a window here: {e.Message}");
            throw;
        }
        using (platform)
        {
            Native.Require(platform.Audio is SdlAudioDevice, "no audio output device here");
            Assert.IsFalse(Raylib_cs.Raylib.IsAudioDeviceReady(), "raylib must not open its own device when audio is replaced");
            var result = GameRunner.Run(game, platform, new RunOptions { MaxUpdates = 30 });
            Assert.IsNull(result.Error, result.Error?.ToString());
            Assert.AreEqual(30, game.SoundsPlayed);
        }
    }

    [TestMethod]
    public void AnUnavailableSdlDriverIsAClearError()
    {
        var error = new StringWriter();
        var previous = Console.Error;
        Console.SetError(error);
        int code;
        try
        {
            var catalog = DesktopLauncher.CreateCatalog().Add(SdlPlatform.CreateProfile("teletype"));
            code = DesktopLauncher.Run(new ProbeGame(), ["--platform", "sdl-teletype", "--hidden", "--frames", "2"], catalog);
        }
        finally { Console.SetError(previous); }
        if (error.ToString().Contains("could not start its video subsystem") || error.ToString().Contains("could not open a window"))
            Native.Require(false, "no display here");
        Assert.AreEqual(1, code);
        StringAssert.Contains(error.ToString(), "error: SDL could not create its 'teletype' renderer (drivers here: ");
    }
}
