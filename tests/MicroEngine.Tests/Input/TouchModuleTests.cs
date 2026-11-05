using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Input;

/// <summary>
/// Touch controls are an opt-in module: they activate on a touch-first screen or when the game sets a layout; until
/// then <see cref="Game.Touch"/> is an inactive object that costs nothing (no frame system, no input layer, no action
/// source, no overlay).
/// </summary>
[TestClass]
public sealed class TouchModuleTests
{
    private static readonly TouchLayout Buttons = TouchLayout.Of(
        new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 80), Radius = 36 }) with
    {
        Visibility = TouchVisibility.Always,
    };

    private sealed class Idle : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class PlainGame : Game
    {
        public override GameSettings Settings { get; } = new("touch");
        protected override InputMap CreateInput() => InputMap.CreateDefault().Bind("jump", Key.Space);
        protected override void Load() => Scenes.Push(new Idle());
    }

    /// <summary>Sets a layout on <see cref="Game.Touch"/> from <see cref="Game.Configure"/>, before the engine is built.</summary>
    private sealed class ConfiguredGame : Game
    {
        public override GameSettings Settings { get; } = new("touch");
        protected override InputMap CreateInput() => InputMap.CreateDefault().Bind("jump", Key.Space);
        public override void Configure(EngineBuilder builder) => Touch.Layout = Buttons;
        protected override void Load() => Scenes.Push(new Idle());
    }

    /// <summary>Sets a layout once it runs, as a scene with its own controls does.</summary>
    private sealed class RunningGame : Game
    {
        public override GameSettings Settings { get; } = new("touch");
        protected override InputMap CreateInput() => InputMap.CreateDefault().Bind("jump", Key.Space);
        protected override void Load()
        {
            Touch.Layout = Buttons;
            Scenes.Push(new Idle());
        }
    }

    private static void AssertInactive(Game game, HeadlessPlatform platform)
    {
        Assert.IsNotNull(game.Touch, "the object always exists, so games can read and configure it");
        Assert.IsFalse(game.Touch.IsActive);
        Assert.IsFalse(game.Schedule.Contains("engine.touch"), "no frame system");
        Assert.IsFalse(game.InputSystem.Layers.Contains(game.Touch), "no input layer");
        CollectionAssert.DoesNotContain(game.Input.Sources.ToList(), game.Touch, "no action source");
        Assert.IsNull(platform.TouchScreen.Overlay, "nothing drawn over the game");
    }

    private static void AssertActive(Game game, HeadlessPlatform platform)
    {
        Assert.IsTrue(game.Touch.IsActive);
        Assert.IsTrue(game.InputSystem.Layers.Contains(game.Touch), "the controls take the touches over them");
        CollectionAssert.Contains(game.Input.Sources.ToList(), game.Touch, "they press actions");
        Assert.AreSame(game.Touch, platform.TouchScreen.Overlay, "they are drawn over the game");
    }

    [TestMethod]
    public void Without_a_layout_on_a_screen_that_is_not_touch_first_the_controls_cost_nothing()
    {
        var game = new PlainGame();
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        for (int i = 0; i < 3; i++) game.StepForTest();
        AssertInactive(game, platform);
        CollectionAssert.AreEqual(new[] { "engine.input.events" }, game.Schedule.Order(FramePhase.Input).ToArray());

        // A touch then reaches gameplay as a pointer and presses nothing.
        platform.TouchScreen.Down(1, platform.TouchScreen.Size - new System.Numerics.Vector2(70, 80));
        game.StepForTest();
        Assert.IsFalse(game.Input.Down("jump"));
        Assert.IsFalse(game.Touch.IsActive, "touching does not turn them on");
    }

    [TestMethod]
    public void A_touch_first_screen_activates_the_default_controls_without_a_layout()
    {
        var game = new PlainGame();
        var platform = new HeadlessPlatform();
        platform.TouchScreen.TouchFirst = true;
        game.InitializeForTest(platform);
        game.StepForTest();
        AssertActive(game, platform);
        CollectionAssert.AreEqual(new[] { "engine.input.events", "engine.touch" }, game.Schedule.Order(FramePhase.Input).ToArray(),
            "active when the engine is built, the controls keep their system");
        Assert.AreEqual(TouchLayout.CreateDefault().Controls.Count, game.Touch.Layout.Controls.Count, "the default layout");
        Assert.IsTrue(game.Touch.Shown, "drawn on a touch-first screen");
    }

    [TestMethod]
    public void A_layout_set_while_composing_activates_the_controls_on_any_screen()
    {
        var game = new ConfiguredGame();
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        game.StepForTest();
        AssertActive(game, platform);
        Assert.IsTrue(game.Schedule.Contains("engine.touch"));
        PressJump(game, platform);
    }

    [TestMethod]
    public void A_layout_set_while_running_activates_the_controls_at_the_next_frame_on_a_desktop_screen()
    {
        var game = new RunningGame();
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        Assert.IsTrue(game.Touch.IsActive, "setting a layout activates the controls");
        Assert.IsFalse(game.Schedule.Contains("engine.touch"), "the schedule was fixed before the layout was set");
        game.StepForTest();
        AssertActive(game, platform);
        Assert.IsTrue(game.Touch.Shown, "the layout asks to be drawn always");
        PressJump(game, platform);
    }

    /// <summary>Declares its layout the usual way, by overriding <c>CreateTouchLayout</c>.</summary>
    private sealed class DeclaredGame : Game
    {
        public override GameSettings Settings { get; } = new("touch");
        protected override InputMap CreateInput() => InputMap.CreateDefault().Bind("jump", Key.Space);
        protected override TouchLayout CreateTouchLayout() => Buttons;
        protected override void Load() => Scenes.Push(new Idle());
    }

    [TestMethod]
    public void A_layout_of_the_game_s_own_from_CreateTouchLayout_activates_the_controls_on_any_screen()
    {
        var game = new DeclaredGame();
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        game.StepForTest();
        AssertActive(game, platform);
        CollectionAssert.AreEqual(new[] { "engine.input.events", "engine.touch" }, game.Schedule.Order(FramePhase.Input).ToArray());
        PressJump(game, platform);
    }

    private static void PressJump(Game game, HeadlessPlatform platform)
    {
        var (center, _) = game.Touch.Geometry("jump");
        platform.TouchScreen.Down(7, center);
        game.StepForTest();
        game.StepForTest();
        Assert.IsTrue(game.Input.Down("jump"), "a finger on the button presses its action");
        Assert.AreEqual(InputDevice.Touch, game.Input.LastDevice);
        platform.TouchScreen.Up(7);
        game.StepForTest();
        game.StepForTest();
        Assert.IsFalse(game.Input.Down("jump"));
    }
}
