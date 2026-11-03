using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>Headless time follows the game's update rate, so everything timed in seconds behaves the same at any rate.</summary>
[TestClass]
public sealed class HeadlessTimeTests
{
    private sealed class GlowSkin : ITouchSkin
    {
        public float Glow { get; private set; }
        public void Draw(IOverlayCanvas canvas, in TouchControlView view) => Glow = view.Glow;
    }

    private sealed class TouchGame(int rate) : Game
    {
        public GlowSkin Skin { get; } = new();
        public override GameSettings Settings { get; } = new("headless time") { UpdateRate = rate };
        protected override TouchLayout CreateTouchLayout() =>
            TouchLayout.Of(new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80) }) with
            {
                Visibility = TouchVisibility.Always,
                Skin = Skin,
            };
        protected override void Load() => Scenes.Push(new Idle());
        private sealed class Idle : Scene
        {
            public override void Update(float dt) { }
            public override void Draw(IRenderer renderer) { }
        }
    }

    [TestMethod]
    [DataRow(120)]
    [DataRow(60)]
    [DataRow(30)]
    public void Game_time_follows_the_update_rate_one_step_per_frame(int rate)
    {
        var game = new TouchGame(rate);
        var platform = HeadlessPlatform.For(game.Settings);
        var result = GameRunner.Run(game, platform, new RunOptions { MaxUpdates = 3 * rate });
        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.AreEqual(3 * rate, game.UpdateCount);
        Assert.AreEqual(3 * rate, result.Frames, "one update per frame");
        Assert.AreEqual(3.0, game.Time, 1e-4, "each 1/UpdateRate seconds long");
        Assert.AreEqual(1.0 / rate, game.TimeSettings.FixedDelta, 1e-12, "the loop's rate is the game's");
        Assert.AreEqual(1.0 / rate, game.Frame.Delta, 1e-12, "a deterministic frame stands for one step");
        Assert.IsTrue(platform.Clock.Now < 3.0, "the platform's clock is the wall clock, not the frames");
    }

    [TestMethod]
    [DataRow(120)]
    [DataRow(30)]
    public void Touch_press_feedback_fades_in_the_same_seconds_at_any_rate(int rate)
    {
        var game = new TouchGame(rate);
        var platform = HeadlessPlatform.For(game.Settings);
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        loop.Tick();
        var (center, _) = game.Touch.Geometry("jump");
        platform.TouchScreen.Down(1, center);
        loop.Tick();
        platform.TouchScreen.DrawOverlay();
        Assert.AreEqual(1f, game.Skin.Glow, "lit while pressed");
        platform.TouchScreen.Up(1, center);
        // A tenth of a second after the release: the feedback fades over 0.12 s whatever the rate.
        for (int i = 0; i < rate / 10; i++) loop.Tick();
        platform.TouchScreen.DrawOverlay();
        Assert.AreEqual(1 - 0.1f / 0.12f, game.Skin.Glow, 1e-3f);
        loop.Finish();
    }

    [TestMethod]
    public void The_loop_takes_the_rate_from_the_game_on_any_headless_platform()
    {
        // The platform no longer knows the rate: a headless platform made without settings runs a 25 Hz game at 25 Hz.
        var game = new TouchGame(25);
        var result = GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { MaxUpdates = 50 });
        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.AreEqual(2.0, game.Time, 1e-4);
        var profiled = new TouchGame(30);
        GameRunner.Run(profiled, (HeadlessPlatform)HeadlessPlatform.Profile.Create(new PlatformRequest(profiled.Settings)), new RunOptions { MaxUpdates = 30 });
        Assert.AreEqual(1.0, profiled.Time, 1e-4);
    }
}
