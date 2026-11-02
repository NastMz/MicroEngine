using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Scenes;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MeadowRun.Tests;

/// <summary>
/// A scripted Meadow Run scenario checked by properties of its captures (<see cref="CaptureProperties"/>) instead of a
/// recorded frame: the headless platform rasterizes with the CPU reference, a probe records the game's state at each
/// capture, and the test checks what that state implies on screen.
/// </summary>
[TestClass]
public sealed class CapturePropertyTests
{
    /// <summary>The game state a capture shows, read by the probe when the capture is written.</summary>
    private sealed record Snapshot(string Path, Vector2 Feet, Vector2 CameraOffset, int Facing, PipForm Form, bool Grounded, float SpeedX, int Time, int Lives, int Coins);

    /// <summary>The rasterizing headless platform, plus a probe that reads the scenario's state at each capture.</summary>
    private sealed class ProbePlatform(HeadlessPlatform inner, MeadowGame game) : IPlatform
    {
        public List<Snapshot> Snapshots { get; } = [];
        public IGraphicsDevice Graphics => inner.Graphics;
        public IAudioDevice Audio => inner.Audio;
        public IInputSource Input => inner.Input;
        public bool CloseRequested => inner.CloseRequested;
        public double Now => inner.Now;
        public bool RealTime => inner.RealTime;
        public void PollEvents() => inner.PollEvents();
        public IRenderer BeginFrame() => inner.BeginFrame();
        public void EndFrame() => inner.EndFrame();
        public void ToggleFullscreen() => inner.ToggleFullscreen();
        public void Dispose() => inner.Dispose();

        public void SaveScreenshot(string path)
        {
            inner.SaveScreenshot(path);
            var play = (PlayScene)game.Scenes.Current!;
            var p = play.Stage.Player;
            Snapshots.Add(new Snapshot(path, p.Feet, play.Stage.Camera.RenderOffset, p.Facing, p.Form, p.Grounded, p.Velocity.X,
                (int)MathF.Ceiling(play.Stage.TimeLeft), game.Session.Lives, game.Session.Coins));
        }
    }

    [TestMethod]
    public void LevelOneStartShowsPipWhereTheStateSaysAndTheTimerRegionFollowsTheTimer()
    {
        string dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "meadowrun-tests", Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var game = new MeadowGame(Path.Combine(dir, "save"), new LaunchOptions("level", 1));
            var probe = new ProbePlatform(HeadlessPlatform.For(game.Settings, rasterize: true), game);
            var script = new InputScript().Capture(120, "a.png").Capture(240, "b.png").QuitAfter(241);
            var result = GameRunner.Run(game, probe, new RunOptions { Script = script, CaptureDirectory = Path.Combine(dir, "captures") });
            Assert.IsNull(result.Error, result.Error?.ToString());
            Assert.HasCount(2, probe.Snapshots);
            var (a, b) = (probe.Snapshots[0], probe.Snapshots[1]);
            PixelImage first = PixelImage.Load(a.Path), second = PixelImage.Load(b.Path);
            // Position (exact): Pip stands still at the start, so he is drawn unscaled with one of his idle frames at his
            // rounded feet minus the frames' pivot, moved by the camera.
            foreach (var (shot, image) in new[] { (a, first), (b, second) })
            {
                Assert.IsTrue(shot.Grounded && shot.SpeedX == 0, $"the scenario expects Pip standing still: {shot}");
                IReadOnlyList<PixelImage> idle = [.. HeroArt.Frames("idle", shot.Form), .. HeroArt.Frames("blink", shot.Form)];
                var topLeft = new Vector2(MathF.Round(shot.Feet.X), MathF.Round(shot.Feet.Y)) - HeroArt.Pivot + shot.CameraOffset;
                CaptureProperties.AssertSpriteAt(image, idle, (int)topLeft.X, (int)topLeft.Y, $"Pip in {Path.GetFileName(shot.Path)}", flipX: shot.Facing < 0);
            }

            // HUD icons (exact): drawn at their fixed places in both captures.
            foreach (var image in new[] { first, second })
            {
                CaptureProperties.AssertSpriteAt(image, [UiArt.LifeIcon()], 5, 3, "life icon");
                CaptureProperties.AssertSpriteAt(image, [UiArt.CoinIcon()], 46, 4, "coin icon");
                CaptureProperties.AssertSpriteAt(image, [UiArt.ClockIcon()], 266, 3, "clock icon");
            }

            // State: the timer's text region changes because the time changed; the lives and coins regions hold
            // because those values did not.
            Assert.AreNotEqual(a.Time, b.Time, "the scenario lets the timer run");
            CaptureProperties.AssertRegionChanged(first, second, new(278, 2, 40, 11), "timer");
            Assert.AreEqual(a.Lives, b.Lives);
            CaptureProperties.AssertRegionUnchanged(first, second, new(18, 2, 24, 11), "lives");
            Assert.AreEqual(a.Coins, b.Coins);
            CaptureProperties.AssertRegionUnchanged(first, second, new(56, 2, 24, 11), "coins");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [TestMethod]
    public void ThePositionPropertyCatchesASpriteMovedByTwelvePixelsAndAChangedRegion()
    {
        // The regressions a grid fingerprint passes: a sprite moved 12 px, and a HUD value that changed.
        var sprite = UiArt.CoinIcon();
        var frame = new PixelImage(64, 24).Clear(new Color(10, 20, 30)).Blit(sprite, 4, 4);
        var moved = new PixelImage(64, 24).Clear(new Color(10, 20, 30)).Blit(sprite, 16, 4);
        CaptureProperties.AssertSpriteAt(frame, [sprite], 4, 4, "coin");
        var e = Assert.ThrowsExactly<AssertFailedException>(() => CaptureProperties.AssertSpriteAt(moved, [sprite], 4, 4, "coin"));
        StringAssert.Contains(e.Message, "moved by (12,0)");
        Assert.ThrowsExactly<AssertFailedException>(() => CaptureProperties.AssertRegionUnchanged(frame, moved, new(0, 0, 32, 24), "hud"));
        Assert.ThrowsExactly<AssertFailedException>(() => CaptureProperties.AssertRegionChanged(frame, frame, new(0, 0, 32, 24), "hud"));
    }
}
