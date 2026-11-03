using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Input;

namespace MicroEngine.Tests.Input;

/// <summary>Touch taps expire by updates, and fingers no on-screen control takes reach gameplay.</summary>
[TestClass]
public sealed class TouchRoutingTests
{
    [TestMethod]
    public void A_touch_button_tap_survives_any_number_of_frames_until_an_update_reads_it()
    {
        var screen = new HeadlessTouchScreen(new Vector2(1280, 720)) { TouchFirst = true };
        var controls = new VirtualControls(TouchLayout.Of(new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Radius = 36 }));
        var map = new InputMap().Bind("jump", Key.Space);
        map.AddSource(controls);
        controls.Update(screen, 1 / 240f);
        var (center, _) = controls.Geometry("jump");
        screen.Down(1, center);
        screen.Up(1, center);
        // A 240 Hz display with a slow update: twenty frames before the next update.
        for (int frame = 0; frame < 20; frame++) controls.Update(screen, 1 / 240f);
        map.Sample(new HeadlessInput());
        Assert.IsTrue(map.Pressed("jump"), "the tap reaches the update that comes after the frames");
        map.EndUpdate();
        controls.Update(screen, 1 / 240f);
        map.Sample(new HeadlessInput());
        Assert.IsFalse(map.Down("jump"), "and only that update");
    }

    [TestMethod]
    public void A_finger_on_no_control_is_the_games_pointer()
    {
        var platform = new HeadlessPlatform();
        var game = new PointerGame();
        var loop = new GameLoop(game, platform);
        Assert.IsTrue(loop.Start());
        loop.Tick();
        platform.TouchScreen.Down(5, new Vector2(640, 360));
        loop.Tick();
        Assert.IsTrue(game.Seen.Present && game.Seen.Primary, "a finger in the middle of the screen is the pointer");
        Assert.AreEqual(160, game.Seen.Position.X, 1);
        Assert.AreEqual(90, game.Seen.Position.Y, 1);
        platform.TouchScreen.Up(5, new Vector2(640, 360));
        loop.Tick();
        loop.Tick();
        Assert.IsFalse(game.Seen.Present, "a lifted finger is gone once an update has seen it lift");
        loop.Finish();
    }

    private sealed class PointerGame : Game
    {
        public override GameSettings Settings { get; } = new("pointer");
        public PointerState Seen { get; set; }
        protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
            new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Radius = 36 });
        protected override void Load() => Scenes.Push(new Watch(this));
    }

    private sealed class Watch(PointerGame game) : Scenes.Scene
    {
        public override void Update(float dt) => game.Seen = Input.Pointer;
        public override void Draw(Graphics.IRenderer renderer) { }
    }
}
