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
    public void Platform_time_equals_game_time_at_any_rate(int rate)
    {
        var game = new TouchGame(rate);
        var platform = HeadlessPlatform.For(game.Settings);
        var result = GameRunner.Run(game, platform, new RunOptions { MaxUpdates = 3 * rate });
        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.AreEqual(3 * rate, game.UpdateCount);
        Assert.AreEqual(3.0, game.Time, 1e-4);
        Assert.AreEqual(game.Time, platform.Now, 1e-4, "one update per frame, each 1/UpdateRate seconds long");
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
    public void A_headless_platform_made_without_settings_keeps_sixty_steps_a_second()
    {
        var platform = new HeadlessPlatform();
        Assert.AreEqual(60, platform.UpdateRate);
        for (int i = 0; i < 90; i++) platform.PollEvents();
        Assert.AreEqual(1.5, platform.Now, 1e-9);
        Assert.AreEqual(120, new HeadlessPlatform { UpdateRate = 120 }.UpdateRate);
        Assert.AreEqual(30, HeadlessPlatform.For(new GameSettings("rate") { UpdateRate = 30 }).UpdateRate);
        Assert.AreEqual(25, ((HeadlessPlatform)HeadlessPlatform.Profile.Create(new PlatformRequest(new GameSettings("rate") { UpdateRate = 25 }))).UpdateRate);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HeadlessPlatform { UpdateRate = 0 });
    }
}
