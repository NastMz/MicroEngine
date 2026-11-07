using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Tests.Interface;

/// <summary>
/// The widgets of a scene's UI (<see cref="Scene.Widgets"/>): laid out in UI units in the UI view, driven by the engine's
/// actions under their current names and by the pointer as the highest input layer, with focus, focus navigation and a
/// text field on the input system's text channel.
/// </summary>
[TestClass]
public sealed class WidgetTests
{
    private static readonly DeviceId Keyboard = new(1), Mouse = new(2);

    /// <summary>A scene whose UI <paramref name="build"/> makes in <see cref="Scene.Enter"/>, and that records what reaches gameplay.</summary>
    private sealed class UiScene(Action<UiRoot> build) : Scene
    {
        public int GameplayConfirms, GameplayUps, GameplayGameConfirms;
        public readonly List<InputEvent> GameplayEvents = [];
        public override void Enter() => build(Widgets);
        public override void Update(float dt)
        {
            if (Input.Pressed(Input.Names.Confirm)) GameplayConfirms++;
            if (Input.Pressed(Input.Names.Up)) GameplayUps++;
            if (Input.Names.Confirm != "confirm" && Input.Pressed("confirm")) GameplayGameConfirms++;
            GameplayEvents.AddRange(Game.InputSystem.Events);
        }
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class UiGame(Scene scene, Func<InputMap>? input = null) : Game
    {
        public override GameSettings Settings { get; } = new("ui", 320, 180, 1);
        protected override InputMap CreateInput() => input?.Invoke() ?? base.CreateInput();
        protected override void Load() => Scenes.Push(scene);
    }

    private static (Game Game, HeadlessPlatform Platform) Start(Scene scene, Func<InputMap>? input = null, bool rasterize = false)
    {
        var game = new UiGame(scene, input);
        var platform = HeadlessPlatform.For(game.Settings, rasterize);
        platform.Resize(640, 360);
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        Frame(game, platform);
        return (game, platform);
    }

    /// <summary>One drawn frame, as the loop's phases run it.</summary>
    private static void Frame(Game game, HeadlessPlatform platform)
    {
        game.StepForTest();
        game.Render(platform.BeginFrame());
        platform.EndFrame();
    }

    private static void Press(Game game, HeadlessPlatform platform, Key key)
    {
        platform.Devices.Send(InputEvent.KeyDown(Keyboard, key));
        Frame(game, platform);
        platform.Devices.Send(InputEvent.KeyUp(Keyboard, key));
        Frame(game, platform);
    }

    /// <summary>A click at <paramref name="ui"/> (UI units: window pixels here), sent as the canvas position a backend reports.</summary>
    private static void Click(Game game, HeadlessPlatform platform, Vector2 ui)
    {
        var canvas = game.Views.UiToCanvas(ui);
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, canvas));
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, canvas));
        Frame(game, platform);
    }

    private static Vector2 Center(Widget w) => new(w.Bounds.X + w.Bounds.Width / 2, w.Bounds.Y + w.Bounds.Height / 2);

    [TestMethod]
    public void A_vertical_stack_lays_its_children_out_in_UI_units_centred_in_the_UI_view()
    {
        Button? play = null, quit = null;
        Stack? stack = null;
        var (_, platform) = Start(new UiScene(ui => ui.Content = stack = new Stack { Spacing = 4 }.Add(play = new Button("PLAY")).Add(quit = new Button("QUIT"))));
        Assert.AreEqual(play!.Bounds.Width, quit!.Bounds.Width, "children stretch to the stack's width");
        Assert.AreEqual(play.Bounds.Bottom + 4, quit.Bounds.Y, "children follow each other with the spacing");
        Assert.AreEqual(320, stack!.Bounds.X + stack.Bounds.Width / 2, 0.5f, "centred in the 640-unit-wide UI view");
        Assert.AreEqual(180, stack.Bounds.Y + stack.Bounds.Height / 2, 0.5f);
        Assert.IsTrue(platform.UiRenderer.DrawCalls > 0, "the widgets are drawn in the UI view");
    }

    [TestMethod]
    public void Confirm_clicks_the_focused_button_and_is_consumed_so_gameplay_does_not_see_it()
    {
        int clicks = 0;
        var scene = new UiScene(ui => ui.Content = new Stack().Add(new Button("PLAY", () => clicks++)));
        var (game, platform) = Start(scene);
        Press(game, platform, Key.Enter);
        Assert.AreEqual(1, clicks, "the first focusable widget has the focus");
        Assert.AreEqual(0, scene.GameplayConfirms, "the UI took the confirm");
    }

    [TestMethod]
    public void Without_a_focusable_widget_confirm_reaches_gameplay()
    {
        var scene = new UiScene(ui => ui.Content = new Label("SCORE 10"));
        var (game, platform) = Start(scene);
        Press(game, platform, Key.Enter);
        Assert.AreEqual(1, scene.GameplayConfirms);
    }

    [TestMethod]
    public void Renaming_confirm_in_a_game_does_not_break_engine_widgets()
    {
        int clicks = 0;
        var names = ActionNames.Default with { Confirm = "accept" };
        // The game's own "confirm" is an unrelated gameplay action on Q.
        var scene = new UiScene(ui => ui.Content = new Button("PLAY", () => clicks++));
        var (game, platform) = Start(scene, () => InputMap.CreateDefault(names).Bind("confirm", Key.Q));
        Press(game, platform, Key.Q);
        Assert.AreEqual(0, clicks, "a gameplay action that happens to be called confirm does not press buttons");
        Assert.AreEqual(1, scene.GameplayGameConfirms);
        Press(game, platform, Key.Enter);
        Assert.AreEqual(1, clicks, "the renamed confirm (accept) does");
        Assert.AreEqual(0, scene.GameplayConfirms, "and the UI consumed it");
    }

    [TestMethod]
    public void A_game_can_choose_other_actions_for_its_widgets()
    {
        int clicks = 0;
        var scene = new UiScene(ui =>
        {
            ui.Actions = new UiActions { Confirm = "select" };
            ui.Content = new Button("PLAY", () => clicks++);
        });
        var (game, platform) = Start(scene, () => InputMap.CreateDefault().Bind("select", Key.T));
        Press(game, platform, Key.Enter);
        Assert.AreEqual(0, clicks);
        Press(game, platform, Key.T);
        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public void Up_and_down_move_the_focus_through_a_vertical_stack_and_wrap()
    {
        Button a = new("A"), b = new("B"), c = new("C");
        var scene = new UiScene(ui => ui.Content = new Stack().Add(a).Add(b).Add(c));
        var (game, platform) = Start(scene);
        var root = scene.Widgets;
        Assert.AreSame(a, root.Focused);
        Press(game, platform, Key.Down);
        Assert.AreSame(b, root.Focused);
        Assert.IsTrue(b.IsFocused);
        Press(game, platform, Key.Up);
        Press(game, platform, Key.Up);
        Assert.AreSame(c, root.Focused, "moving up from the first wraps to the last");
        Assert.AreEqual(0, scene.GameplayUps, "navigation was consumed");
    }

    [TestMethod]
    public void Left_and_right_move_the_focus_through_a_horizontal_stack()
    {
        Button yes = new("YES"), no = new("NO");
        var scene = new UiScene(ui => ui.Content = new Stack { Orientation = Orientation.Horizontal }.Add(yes).Add(no));
        var (game, platform) = Start(scene);
        Press(game, platform, Key.Right);
        Assert.AreSame(no, scene.Widgets.Focused);
        Press(game, platform, Key.Left);
        Assert.AreSame(yes, scene.Widgets.Focused);
    }

    [TestMethod]
    public void A_click_on_a_button_presses_it_and_does_not_reach_gameplay()
    {
        int clicks = 0;
        Button? quit = null;
        var scene = new UiScene(ui => ui.Content = new Stack().Add(new Button("PLAY")).Add(quit = new Button("QUIT", () => clicks++)));
        var (game, platform) = Start(scene);
        Click(game, platform, Center(quit!));
        Assert.AreEqual(1, clicks);
        Assert.AreSame(quit, scene.Widgets.Focused, "a click focuses what it presses");
        Assert.IsFalse(scene.GameplayEvents.Any(e => e.Kind is InputEventKind.PointerDown or InputEventKind.PointerUp), "the UI layer took the press");
        Assert.IsFalse(game.Input.Pointer.Primary, "gameplay never saw the button held");

        Click(game, platform, new Vector2(5, 5));
        Assert.AreEqual(1, clicks);
        Assert.IsTrue(scene.GameplayEvents.Any(e => e.Kind == InputEventKind.PointerDown), "a press outside the UI reaches gameplay");
    }

    [TestMethod]
    public void A_press_that_leaves_the_button_before_release_does_not_click()
    {
        int clicks = 0;
        Button? play = null;
        var scene = new UiScene(ui => ui.Content = play = new Button("PLAY", () => clicks++));
        var (game, platform) = Start(scene);
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(Center(play!))));
        Frame(game, platform);
        Assert.IsTrue(play!.IsPressed);
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new Vector2(1, 1)));
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, new Vector2(1, 1)));
        Frame(game, platform);
        Assert.AreEqual(0, clicks);
        Assert.IsFalse(play.IsPressed);
        Assert.IsFalse(scene.GameplayEvents.Any(e => e.Kind == InputEventKind.PointerUp), "the release belongs to the press the UI took");
    }

    /// <summary>A focusable widget of fixed size that records the pointer events it receives, in UI units.</summary>
    private sealed class PointerRecorder : Widget
    {
        public readonly List<string> Seen = [];
        public PointerRecorder() { Width = 100; Height = 20; }
        public override bool Focusable => true;
        protected override Vector2 MeasureContent(UiContext context) => Vector2.Zero;
        protected override void DrawContent(IRenderer renderer, UiContext context) { }
        protected internal override void OnPointerDown(Vector2 position) => Seen.Add($"down {position.X:0},{position.Y:0}");
        protected internal override void OnPointerMove(Vector2 position) => Seen.Add($"move {position.X:0},{position.Y:0}");
        protected internal override void OnPointerUp(Vector2 position) => Seen.Add($"up {position.X:0},{position.Y:0}");
        protected internal override void OnPointerCancel() => Seen.Add("cancel");
        protected internal override void OnClick() => Seen.Add("click");
    }

    /// <summary>A container of one child that takes the wheel.</summary>
    private sealed class WheelBox : Widget
    {
        private readonly Widget _child;
        public readonly List<Vector2> Wheel = [];
        public WheelBox(Widget child) { _child = child; Adopt(child); }
        public override IReadOnlyList<Widget> Children => [_child];
        protected override Vector2 MeasureContent(UiContext context) => _child.Measure(context);
        protected override void ArrangeContent(RectF bounds, UiContext context) => _child.Arrange(bounds, context);
        protected override void DrawContent(IRenderer renderer, UiContext context) => _child.Draw(renderer, context);
        protected internal override bool OnWheel(Vector2 delta) { Wheel.Add(delta); return true; }
    }

    [TestMethod]
    public void A_pressed_widget_follows_the_pointer_in_UI_units_until_the_release()
    {
        var recorder = new PointerRecorder();
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = recorder; });
        var (game, platform) = Start(scene);
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(10, 10))));
        Frame(game, platform);
        Assert.IsEmpty(recorder.Seen, "a move without a press is only hover");

        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(10, 10))));
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(60, 12))));
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(300, 90))));
        Frame(game, platform);
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(40, 10))));
        Frame(game, platform);
        CollectionAssert.AreEqual(new[] { "down 10,10", "move 60,12", "move 300,90", "up 40,10", "click" }, recorder.Seen,
            "moves outside the widget still reach it while it holds the press; a release on it also clicks");
        Assert.IsFalse(scene.GameplayEvents.Any(e => e.Kind is InputEventKind.PointerDown or InputEventKind.PointerUp));
    }

    [TestMethod]
    public void A_cancelled_press_tells_the_widget_and_does_not_click()
    {
        var recorder = new PointerRecorder();
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = recorder; });
        var (game, platform) = Start(scene);
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Touch, game.Views.UiToCanvas(new Vector2(10, 10))));
        Frame(game, platform);
        platform.Devices.Send(new InputEvent { Kind = InputEventKind.PointerCancel, Device = Mouse, Pointer = 0, PointerKind = PointerKind.Touch });
        Frame(game, platform);
        CollectionAssert.AreEqual(new[] { "down 10,10", "cancel" }, recorder.Seen);
        Assert.IsFalse(recorder.IsPressed);
    }

    [TestMethod]
    public void The_wheel_reaches_the_widget_under_it_and_goes_up_to_the_first_that_takes_it()
    {
        var button = new Button("PLAY");
        var box = new WheelBox(button);
        var scene = new UiScene(ui => ui.Content = box);
        var (game, platform) = Start(scene);
        platform.Devices.Send(InputEvent.WheelMoved(Mouse, new Vector2(0, -2), game.Views.UiToCanvas(Center(button))));
        Frame(game, platform);
        CollectionAssert.AreEqual(new[] { new Vector2(0, -2) }, box.Wheel, "the button ignores it, its container takes it");
        Assert.IsFalse(scene.GameplayEvents.Any(e => e.Kind == InputEventKind.Wheel), "the wheel over the UI never reaches gameplay");

        platform.Devices.Send(InputEvent.WheelMoved(Mouse, new Vector2(0, 1), new Vector2(1, 1)));
        Frame(game, platform);
        Assert.HasCount(1, box.Wheel, "the wheel outside the UI is gameplay's");
    }

    [TestMethod]
    public void A_focused_widget_that_is_disabled_hands_the_focus_on()
    {
        Button a = new("A"), b = new("B");
        var scene = new UiScene(ui => ui.Content = new Stack().Add(a).Add(b));
        var (game, platform) = Start(scene);
        Assert.AreSame(a, scene.Widgets.Focused);
        a.Enabled = false;
        Frame(game, platform);
        Assert.AreSame(b, scene.Widgets.Focused);
    }

    [TestMethod]
    public void Leaving_a_scene_releases_the_text_channel_its_field_held()
    {
        var scene = new UiScene(ui => ui.Content = new TextField());
        var (game, platform) = Start(scene);
        Assert.IsTrue(platform.Devices.TextInput.Active);
        game.Scenes.Push(new UiScene(_ => { }));
        Frame(game, platform);
        Assert.IsFalse(platform.Devices.TextInput.Active, "the covered scene's field stops typing");
        Assert.IsNull(scene.Widgets.Focused);
    }

    [TestMethod]
    public void The_pointer_over_a_widget_is_on_the_UI_and_not_in_the_world()
    {
        Button? play = null;
        var scene = new UiScene(ui => ui.Content = play = new Button("PLAY"));
        var (game, platform) = Start(scene);
        platform.Keys.Pointer = new PointerState(true, game.Views.UiToCanvas(Center(play!)), false, false, false);
        game.StepForTest();
        Assert.IsTrue(game.Views.PointerOnUi);
        Assert.IsFalse(game.Views.WorldPointer().Present);
        Assert.IsTrue(scene.Widgets.Contains(Center(play!)));
        Assert.IsFalse(scene.Widgets.Contains(new Vector2(1, 1)));
    }

    [TestMethod]
    public void A_disabled_button_neither_takes_the_focus_nor_clicks()
    {
        int clicks = 0;
        Button off = new("OFF", () => clicks++) { Enabled = false }, on = new("ON");
        var scene = new UiScene(ui => ui.Content = new Stack().Add(off).Add(on));
        var (game, platform) = Start(scene);
        Assert.AreSame(on, scene.Widgets.Focused, "the first enabled focusable widget gets the focus");
        Click(game, platform, Center(off));
        Assert.AreEqual(0, clicks);
    }

    [TestMethod]
    public void A_text_field_edits_through_the_text_channel_and_keeps_typing_from_gameplay()
    {
        TextField? name = null;
        string? submitted = null;
        var scene = new UiScene(ui => ui.Content = new Panel(name = new TextField { MaxLength = 8 }));
        var (game, platform) = Start(scene);
        name!.Submitted += text => submitted = text;
        Assert.AreSame(name, scene.Widgets.Focused);
        Assert.IsTrue(platform.Devices.TextInput.Active, "a focused text field turns text input on (the on-screen keyboard, the IME)");

        platform.Devices.Send(InputEvent.KeyDown(Keyboard, Key.W));
        platform.Devices.Send(InputEvent.TextInput(Keyboard, "w"));
        platform.Devices.Send(InputEvent.KeyUp(Keyboard, Key.W));
        Frame(game, platform);
        Assert.AreEqual("w", name.Text);
        Assert.AreEqual(0, scene.GameplayUps, "W is up in gameplay, but it was typed");

        platform.Devices.Send(InputEvent.Composition(Keyboard, "かな"));
        Frame(game, platform);
        Assert.AreEqual("かな", name.Composition, "the input method's composition is shown until committed");
        platform.Devices.Send(InputEvent.TextInput(Keyboard, "仮名"));
        Frame(game, platform);
        Assert.AreEqual("w仮名", name.Text);
        Assert.AreEqual("", name.Composition);

        Press(game, platform, Key.Backspace);
        Assert.AreEqual("w仮", name.Text, "Backspace deletes before the caret (and is not cancel)");
        Press(game, platform, Key.Left);
        Assert.AreEqual(1, name.Caret);
        platform.Devices.Send(InputEvent.TextInput(Keyboard, "123456789"));
        Frame(game, platform);
        Assert.AreEqual("w123456仮", name.Text, "typing stops at MaxLength");

        Press(game, platform, Key.Enter);
        Assert.AreEqual("w123456仮", submitted);
        Assert.AreEqual(0, scene.GameplayConfirms, "Enter submitted the field");

        scene.Widgets.Focus(null);
        Assert.IsFalse(platform.Devices.TextInput.Active, "losing the focus turns text input off");
    }

    [TestMethod]
    public void The_UI_state_is_part_of_the_scene_state_hash()
    {
        static ulong Typed(string text)
        {
            var scene = new UiScene(ui => ui.Content = new TextField());
            var (game, platform) = Start(scene);
            platform.Devices.Send(InputEvent.TextInput(Keyboard, text));
            Frame(game, platform);
            return game.ComputeStateHash().Hash;
        }
        Assert.AreNotEqual(Typed("a"), Typed("b"));
        Assert.AreEqual(Typed("a"), Typed("a"));
    }

    [TestMethod]
    public void Toasts_show_one_banner_at_a_time_for_its_time()
    {
        var toasts = new Toasts();
        toasts.Show(new Toast("SAVED", "SLOT 1"));
        toasts.Show(new Toast("SAVED", "SLOT 2", "Autosave"));
        Assert.AreEqual("SLOT 1", toasts.Current!.Title);
        Assert.AreEqual(1, toasts.Pending);
        toasts.Update(Toasts.ShowTime);
        Assert.AreEqual("SLOT 2", toasts.Current!.Title);
        var renderer = new HeadlessRenderer(320, 180);
        var device = new HeadlessGraphicsDevice();
        toasts.Draw(renderer, BitmapFont.CreateDefault(device));
        Assert.IsTrue(renderer.DrawCalls > 0);
        toasts.Update(Toasts.ShowTime);
        Assert.IsNull(toasts.Current);
    }
}
