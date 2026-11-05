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

    /// <summary>
    /// One frame of the game's loop that catches up with <paramref name="updates"/> fixed updates, recording at each
    /// update whether the action is pressed and down.
    /// </summary>
    private static List<(bool Pressed, bool Down)> CatchUpFrame(TapGame game, int updates)
    {
        var engine = game.Engine;
        double step = engine.Time.FixedDelta;
        var frame = new MicroEngine.Kernel.FrameContext(step * updates, step, 1f, game.UpdateCount, engine.Clock.Frame);
        engine.Run(MicroEngine.Kernel.FramePhase.BeginFrame, frame);
        engine.Run(MicroEngine.Kernel.FramePhase.Input, frame);
        game.Seen.Clear();
        for (int i = 0; i < updates; i++) engine.RunFixedStep(frame with { Blend = 0 });
        engine.Run(MicroEngine.Kernel.FramePhase.Update, frame);
        return [.. game.Seen];
    }

    private static readonly (bool, bool)[] OnceThenGone = [(true, true), (false, false), (false, false)];

    [TestMethod]
    public void A_tap_shorter_than_a_frame_is_down_in_the_first_update_of_a_catch_up_frame_only_for_keys_buttons_and_touch()
    {
        var platform = new HeadlessPlatform();
        platform.TouchScreen.TouchFirst = true;
        var game = new TapGame();
        game.InitializeForTest(platform);
        CatchUpFrame(game, 1);   // lays the controls out
        var (center, _) = game.Touch.Geometry("jump");

        game.InputSystem.Send(InputEvent.KeyDown(new DeviceId(1), Key.Space));
        game.InputSystem.Send(InputEvent.KeyUp(new DeviceId(1), Key.Space));
        CollectionAssert.AreEqual(OnceThenGone, CatchUpFrame(game, 3), "a key tap");

        var pad = new DeviceId(2);
        game.InputSystem.Send(InputEvent.Added(new InputDeviceInfo(pad, DeviceKind.Gamepad, "pad")));
        game.InputSystem.Send(InputEvent.ButtonDown(pad, PadButton.A));
        game.InputSystem.Send(InputEvent.ButtonUp(pad, PadButton.A));
        CollectionAssert.AreEqual(OnceThenGone, CatchUpFrame(game, 3), "a gamepad button tap");

        platform.TouchScreen.Down(1, center);
        platform.TouchScreen.Up(1, center);
        CollectionAssert.AreEqual(OnceThenGone, CatchUpFrame(game, 3), "a touch button tap");
    }

    [TestMethod]
    public void A_touch_tap_in_a_frame_without_updates_waits_for_the_next_update_and_a_held_touch_stays_down()
    {
        var platform = new HeadlessPlatform();
        platform.TouchScreen.TouchFirst = true;
        var game = new TapGame();
        game.InitializeForTest(platform);
        CatchUpFrame(game, 1);
        var (center, _) = game.Touch.Geometry("jump");

        platform.TouchScreen.Down(1, center);
        platform.TouchScreen.Up(1, center);
        Assert.AreEqual(0, CatchUpFrame(game, 0).Count, "a fast display draws a frame with no update");
        CollectionAssert.AreEqual(OnceThenGone, CatchUpFrame(game, 3), "the tap reaches the next update, once");

        platform.TouchScreen.Down(1, center);
        CollectionAssert.AreEqual(new[] { (true, true), (false, true), (false, true) }, CatchUpFrame(game, 3), "a held button is down in every update");
        platform.TouchScreen.Up(1, center);
        CatchUpFrame(game, 1);
    }

    private sealed class TapGame : Game
    {
        public override GameSettings Settings { get; } = new("taps");
        public List<(bool Pressed, bool Down)> Seen { get; } = [];
        protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
            new TouchButton("jump", "confirm", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Radius = 36 });
        protected override void Load() => Scenes.Push(new Recorder(this));
    }

    private sealed class Recorder(TapGame game) : Scenes.Scene
    {
        public override void Update(float dt) => game.Seen.Add((Input.Pressed("confirm"), Input.Down("confirm")));
        public override void Draw(Graphics.IRenderer renderer) { }
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
