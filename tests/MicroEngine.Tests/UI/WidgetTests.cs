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
    public void Every_change_of_focus_is_announced_with_the_widget_it_left_and_the_one_it_reached()
    {
        Button a = new("A"), b = new("B"), c = new("C") { Enabled = false };
        var changes = new List<FocusChange>();
        var scene = new UiScene(ui =>
        {
            ui.FocusChanged += changes.Add;
            ui.Content = new Stack().Add(a).Add(b).Add(c);
        });
        var (game, platform) = Start(scene);
        var root = scene.Widgets;
        CollectionAssert.AreEqual(new[] { new FocusChange(null, a) }, changes, "the first focus comes from nowhere");
        Press(game, platform, Key.Down);
        Assert.AreEqual(new FocusChange(a, b), changes[^1], "a move by the arrows");
        Press(game, platform, Key.Down);
        Assert.AreEqual(new FocusChange(b, a), changes[^1], "the disabled widget is skipped");
        Click(game, platform, Center(b));
        Assert.AreEqual(new FocusChange(a, b), changes[^1], "a press moves the focus too");
        int count = changes.Count;
        root.Focus(b);
        Assert.AreEqual(count, changes.Count, "focusing the focused widget changes nothing");
        root.Focus(null);
        Assert.AreEqual(new FocusChange(b, null), changes[^1]);
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

    /// <summary>A widget that fills its bounds with one colour.</summary>
    private sealed class Block(Color color, float height) : Widget
    {
        protected override Vector2 MeasureContent(UiContext context) => new(60, height);
        protected override void DrawContent(IRenderer renderer, UiContext context) => renderer.FillRect(Bounds, color);
    }

    private static Stack Buttons(int count, out Button[] buttons)
    {
        buttons = [.. Enumerable.Range(0, count).Select(i => new Button($"ITEM {i}"))];
        var stack = new Stack();
        foreach (var b in buttons) stack.Add(b);
        return stack;
    }

    [TestMethod]
    public void A_scroll_view_scrolls_to_keep_the_focused_widget_inside_it()
    {
        var view = new ScrollView(Buttons(10, out var items)) { Height = 60 };
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = view; });
        var (game, platform) = Start(scene);
        Assert.AreEqual(0, view.Scroll);
        Assert.IsGreaterThan(0, view.MaxScroll, "ten buttons are taller than sixty units");
        for (int i = 0; i < 7; i++) Press(game, platform, Key.Down);
        Assert.AreSame(items[7], scene.Widgets.Focused);
        Assert.IsGreaterThan(0, view.Scroll);
        Assert.IsTrue(view.Bounds.Contains(new Vector2(items[7].Bounds.X + 1, items[7].Bounds.Y + 1))
            && view.Bounds.Contains(new Vector2(items[7].Bounds.X + 1, items[7].Bounds.Bottom - 1)), "the focused button is inside the view");
        for (int i = 0; i < 7; i++) Press(game, platform, Key.Up);
        Assert.AreEqual(0, view.Scroll, "back at the first button, the view is back at the top");
    }

    [TestMethod]
    public void The_wheel_scrolls_a_scroll_view_within_its_range()
    {
        var view = new ScrollView(Buttons(10, out _)) { Height = 60 };
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = view; });
        var (game, platform) = Start(scene);
        platform.Devices.Send(InputEvent.WheelMoved(Mouse, new Vector2(0, -1), game.Views.UiToCanvas(new Vector2(5, 5))));
        Frame(game, platform);
        Assert.IsGreaterThan(0, view.Scroll, "the wheel towards the player scrolls down");
        platform.Devices.Send(InputEvent.WheelMoved(Mouse, new Vector2(0, -100), game.Views.UiToCanvas(new Vector2(5, 5))));
        Frame(game, platform);
        Assert.AreEqual(view.MaxScroll, view.Scroll);
        platform.Devices.Send(InputEvent.WheelMoved(Mouse, new Vector2(0, 100), game.Views.UiToCanvas(new Vector2(5, 5))));
        Frame(game, platform);
        Assert.AreEqual(0, view.Scroll);
    }

    [TestMethod]
    public void A_widget_scrolled_out_of_view_cannot_be_clicked_where_another_widget_shows()
    {
        int afterClicks = 0;
        var view = new ScrollView(Buttons(10, out var items)) { Height = 40 };
        var after = new Button("AFTER", () => afterClicks++);
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = new Stack { Spacing = 0 }.Add(view).Add(after); });
        var (game, platform) = Start(scene);
        var hidden = items.First(b => b.Bounds.Contains(Center(after)));
        Click(game, platform, Center(after));
        Assert.AreEqual(1, afterClicks);
        Assert.AreSame(after, scene.Widgets.Focused, $"{hidden.Text} lies under it but outside the view");
    }

    [TestMethod]
    public void Dragging_inside_a_scroll_view_scrolls_it_and_a_tap_still_clicks()
    {
        int clicks = 0;
        var view = new ScrollView(Buttons(10, out var items)) { Height = 60 };
        items[0].Clicked += () => clicks++;
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = view; });
        var (game, platform) = Start(scene);
        var start = Center(items[0]);
        Click(game, platform, start);
        Assert.AreEqual(1, clicks, "a tap clicks the button under it");

        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Touch, game.Views.UiToCanvas(start)));
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Touch, game.Views.UiToCanvas(start - new Vector2(0, 30))));
        Frame(game, platform);
        Assert.IsTrue(view.Scroll is > 20 and <= 30, $"dragged up 30 units, the content follows ({view.Scroll})");
        Assert.IsFalse(items[0].IsPressed, "the drag took the press from the button");
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Touch, game.Views.UiToCanvas(start - new Vector2(0, 30))));
        Frame(game, platform);
        Assert.AreEqual(1, clicks, "a drag is not a click");
    }

    [TestMethod]
    public void A_clipping_container_keeps_its_clip_around_a_text_field_inside_it()
    {
        var red = Color.Hex("ff0000");
        var view = new ScrollView(new Stack { Spacing = 0 }.Add(new TextField()).Add(new Block(red, 200))) { Height = 40 };
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = view; });
        var (game, platform) = Start(scene, rasterize: true);
        Frame(game, platform);
        var ui = ((IViewCapture)platform).CaptureUi()!;
        Assert.AreEqual(red, ui[5, 35], "the block shows inside the view");
        Assert.AreNotEqual(red, ui[5, 60], "and nowhere below it, though the text field drew before it");
    }

    [TestMethod]
    public void A_grid_places_children_in_rows_of_equal_cells()
    {
        var cells = new[] { new Block(Color.White, 10), new Block(Color.White, 30), new Block(Color.White, 20), new Block(Color.White, 10), new Block(Color.White, 10) };
        var grid = new Grid(columns: 3) { Spacing = 2 };
        foreach (var c in cells) grid.Add(c);
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = grid; });
        Start(scene);
        Assert.AreEqual(new RectF(0, 0, 3 * 60 + 2 * 2, 2 * 30 + 2), grid.Bounds, "three columns and two rows of the largest cell, 60 by 30");
        Assert.AreEqual(new RectF(0, 0, 60, 30), cells[0].Bounds, "a child fills its cell");
        Assert.AreEqual(new RectF(124, 0, 60, 30), cells[2].Bounds);
        Assert.AreEqual(new RectF(0, 32, 60, 30), cells[3].Bounds, "the fourth child starts the second row");
        Assert.AreEqual(new RectF(62, 32, 60, 30), cells[4].Bounds);
    }

    [TestMethod]
    public void A_hidden_child_of_a_grid_takes_no_cell()
    {
        var a = new Block(Color.White, 10);
        var b = new Block(Color.White, 10) { Visible = false };
        var c = new Block(Color.White, 10);
        var grid = new Grid(columns: 2) { Spacing = 0 }.Add(a).Add(b).Add(c);
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = grid; });
        Start(scene);
        Assert.AreEqual(new RectF(60, 0, 60, 10), c.Bounds, "c moves into b's place");
        Assert.AreEqual(10, grid.Bounds.Height, "one row");
    }

    [TestMethod]
    public void Focus_moves_through_a_grid_by_rows_and_columns()
    {
        var buttons = Enumerable.Range(0, 6).Select(i => new Button($"B{i}")).ToArray();
        var grid = new Grid(columns: 3);
        foreach (var b in buttons) grid.Add(b);
        var scene = new UiScene(ui => ui.Content = grid);
        var (game, platform) = Start(scene);
        Assert.AreSame(buttons[0], scene.Widgets.Focused);
        Press(game, platform, Key.Right);
        Assert.AreSame(buttons[1], scene.Widgets.Focused);
        Press(game, platform, Key.Down);
        Assert.AreSame(buttons[4], scene.Widgets.Focused, "down goes to the cell below, not the next child");
    }

    /// <summary>A block that fades its own colour with the context, as a widget of fixed colours does.</summary>
    private sealed class FadingBlock(Color color) : Widget
    {
        protected override Vector2 MeasureContent(UiContext context) => new(20, 20);
        protected override void DrawContent(IRenderer renderer, UiContext context) => renderer.FillRect(Bounds, context.Fade(color));
    }

    [TestMethod]
    public void An_offset_moves_a_widget_and_everything_in_it_for_layout_and_the_pointer()
    {
        int clicks = 0;
        var button = new Button("GO", () => clicks++);
        var panel = new Panel(button) { Offset = new Vector2(100, 10) };
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = panel; });
        var (game, platform) = Start(scene);
        Assert.AreEqual(new Vector2(100, 10), panel.Bounds.Position);
        Assert.AreEqual(new Vector2(106, 16), button.Bounds.Position, "the child moves with it (6 units of padding)");
        Click(game, platform, Center(button));
        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public void Opacity_fades_a_widget_and_its_children_and_multiplies_down_the_tree()
    {
        var red = Color.Hex("ff0000");
        var themed = new ThemeBlock();
        var inner = new FadingBlock(red) { Opacity = 0.5f };
        var group = new Stack { Spacing = 0, Opacity = 0.5f }.Add(themed).Add(inner);
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = group; });
        var (game, platform) = Start(scene, rasterize: true);
        Frame(game, platform);
        var ui = ((IViewCapture)platform).CaptureUi()!;
        var accent = ui[5, 5];
        Assert.IsTrue(Math.Abs(accent.A - 128) <= 2, $"a theme colour inside is faded to half without the widget doing anything ({accent.A})");
        var block = ui[5, 25];
        Assert.IsTrue(block.R > 0 && block.G == 0 && block.B == 0, "red, stored premultiplied by its alpha");
        Assert.IsTrue(Math.Abs(block.A - 64) <= 2, $"a widget's own colour through Fade, half of half ({block.A})");
    }

    [TestMethod]
    public void A_wrapping_label_breaks_its_text_to_its_width_and_grows_in_height()
    {
        var plain = new Label("THE GEMS ONLY GROW AT NIGHT");
        var wrapped = new Label("THE GEMS ONLY GROW AT NIGHT") { Width = 60, Wrap = true };
        var scene = new UiScene(ui => ui.Content = new Stack { Spacing = 0, Align = Align.Start }.Add(plain).Add(wrapped));
        var (game, _) = Start(scene);
        var font = game.Font;
        Assert.AreEqual(1, plain.Lines.Count, "without Wrap a label keeps its lines");
        Assert.AreEqual(font.MeasureWidth(plain.Text), plain.Bounds.Width, "and measures as wide as its text");
        Assert.IsTrue(wrapped.Lines.Count > 1, "with Wrap the text breaks into lines");
        Assert.AreEqual(string.Join(" ", wrapped.Lines), wrapped.Text, "between words, keeping every word");
        foreach (var line in wrapped.Lines)
            Assert.IsTrue(font.MeasureWidth(line) <= 60, $"no line is wider than the label ({line})");
        Assert.AreEqual(60, wrapped.Bounds.Width);
        Assert.AreEqual(wrapped.Lines.Count * font.LineHeight + (wrapped.Lines.Count - 1), wrapped.Bounds.Height, "one line height per line, a unit apart");
    }

    [TestMethod]
    public void Visible_characters_reveal_a_label_without_moving_its_lines()
    {
        var red = Color.Hex("ff0000");
        var label = new Label("AAAA") { Color = red, Shadow = false, VisibleCharacters = 2 };
        var scene = new UiScene(ui => { ui.Anchor = Anchor.TopLeft; ui.Content = label; });
        var (game, platform) = Start(scene, rasterize: true);
        var full = label.Bounds;
        Frame(game, platform);
        var ui = ((IViewCapture)platform).CaptureUi()!;
        static bool Red(PixelImage image, int x0, int x1)
        {
            for (int x = x0; x < x1; x++)
                for (int y = 0; y < 8; y++)
                    if (image[x, y].R > 0) return true;
            return false;
        }
        int advance = game.Font.Advance('A');
        Assert.IsTrue(Red(ui, 0, 2 * advance), "the first two letters are drawn");
        Assert.IsFalse(Red(ui, 2 * advance, 4 * advance), "the last two are not yet");
        Assert.AreEqual(full, label.Bounds, "the label keeps the size of its whole text");

        label.VisibleCharacters = null;
        Frame(game, platform);
        Assert.IsTrue(Red(((IViewCapture)platform).CaptureUi()!, 2 * advance, 4 * advance), "null shows the whole text");
    }

    [TestMethod]
    public void Visible_characters_count_the_text_as_written_across_wrapped_lines()
    {
        var label = new Label("AB CD\nEF") { Width = 20, Wrap = true, VisibleCharacters = 4 };
        var (_, _) = Start(new UiScene(ui => ui.Content = label));
        CollectionAssert.AreEqual(new[] { "AB", "CD", "EF" }, label.Lines.ToArray());
        CollectionAssert.AreEqual(new[] { "AB", "C", "" }, label.VisibleLines.ToArray(), "A, B, the space the wrap took, then C");
        label.VisibleCharacters = 7;
        CollectionAssert.AreEqual(new[] { "AB", "CD", "E" }, label.VisibleLines.ToArray(), "the line break counts as a character too");
    }

    private static readonly Color SliceCorner = Color.Hex("ff0000"), SliceEdge = Color.Hex("00ff00"), SliceCentre = Color.Hex("0000ff");

    /// <summary>A 3×3 image: red corners, green edges, a blue centre; one pixel of border on every side.</summary>
    private static NineSlice Slice(Game game, float scale)
    {
        var image = new PixelImage(3, 3);
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                image[x, y] = x == 1 && y == 1 ? SliceCentre : x == 1 || y == 1 ? SliceEdge : SliceCorner;
        return new NineSlice(game.Graphics.CreateTexture(image, TextureFilter.Nearest), new RectF(0, 0, 3, 3), 1) { Scale = scale };
    }

    /// <summary>A widget that draws a nine-slice image over its bounds.</summary>
    private sealed class SliceBox(Func<NineSlice> slice) : Widget
    {
        protected override Vector2 MeasureContent(UiContext context) => new(20, 10);
        protected override void DrawContent(IRenderer renderer, UiContext context) => renderer.DrawNineSlice(slice(), Bounds);
    }

    [TestMethod]
    public void A_nine_slice_keeps_its_corners_and_stretches_its_edges_and_centre()
    {
        UiScene scene = null!;
        scene = new UiScene(ui =>
        {
            var slice = Slice(scene.Game, 2);
            ui.Anchor = Anchor.TopLeft;
            ui.Content = new SliceBox(() => slice);
        });
        var (game, platform) = Start(scene, rasterize: true);
        Frame(game, platform);
        var ui = ((IViewCapture)platform).CaptureUi()!;
        Assert.AreEqual(SliceCorner, ui[0, 0]);
        Assert.AreEqual(SliceCorner, ui[1, 1], "a corner is its source pixels times the scale, 2 by 2");
        Assert.AreEqual(SliceEdge, ui[2, 0], "the top edge starts after the corner");
        Assert.AreEqual(SliceEdge, ui[17, 1], "and stretches to the next corner");
        Assert.AreEqual(SliceCorner, ui[18, 0]);
        Assert.AreEqual(SliceEdge, ui[0, 5], "the left edge stretches down");
        Assert.AreEqual(SliceCentre, ui[10, 5], "the centre fills the rest");
        Assert.AreEqual(SliceCorner, ui[19, 9], "the bottom-right corner ends at the bounds");
        Assert.AreEqual(SliceEdge, ui[17, 9]);
    }

    [TestMethod]
    public void A_panel_style_with_an_image_draws_panels_and_buttons_with_it_tinted_by_their_state()
    {
        var theme = UiTheme.Default;
        Button? button = null;
        Panel? panel = null;
        UiScene scene = null!;
        scene = new UiScene(ui =>
        {
            var slice = Slice(scene.Game, 1);
            ui.Anchor = Anchor.TopLeft;
            ui.Theme = theme with { Panel = theme.Panel with { Image = slice } };
            ui.Content = new Stack { Spacing = 0, Align = Align.Start }
                .Add(button = new Button("GO"))
                .Add(panel = new Panel(new Block(SliceCentre, 10)) { Padding = 4 });
        });
        var (game, platform) = Start(scene, rasterize: true);
        Frame(game, platform);
        var ui = ((IViewCapture)platform).CaptureUi()!;
        Assert.AreEqual(SliceCorner, ui[(int)panel!.Bounds.X, (int)panel.Bounds.Y], "the panel draws the image, not the theme's box");
        Assert.AreEqual(SliceEdge, ui[(int)panel.Bounds.X + 2, (int)panel.Bounds.Y]);
        var tinted = new Color(0, 0, theme.Accent.B);                  // blue times the accent
        var centre = ui[(int)button!.Bounds.X + 2, (int)button.Bounds.Y + 2];
        Assert.IsTrue(button.IsFocused);
        Assert.AreEqual(tinted, centre, "the focused button tints its image with the accent");
    }

    /// <summary>A block in the theme's accent colour, as engine widgets draw.</summary>
    private sealed class ThemeBlock : Widget
    {
        protected override Vector2 MeasureContent(UiContext context) => new(20, 20);
        protected override void DrawContent(IRenderer renderer, UiContext context) => renderer.FillRect(Bounds, context.Theme.Accent);
    }

    [TestMethod]
    public void A_layer_places_children_at_its_edges_or_at_a_point_and_lets_the_pointer_through_elsewhere()
    {
        var corner = new Block(Color.White, 10);
        var bubble = new Block(Color.White, 20);
        var layer = new Layer().Add(corner, Anchor.BottomRight, new Vector2(-4, -4)).AddAt(bubble, new Vector2(200, 100), Anchor.Bottom);
        var scene = new UiScene(ui => ui.Content = layer);
        var (game, platform) = Start(scene);
        Assert.AreEqual(new RectF(0, 0, 640, 360), layer.Bounds, "a layer fills the UI view");
        Assert.AreEqual(new RectF(640 - 4 - 60, 360 - 4 - 10, 60, 10), corner.Bounds);
        Assert.AreEqual(new RectF(170, 80, 60, 20), bubble.Bounds, "its bottom centre on the point");

        layer.Move(bubble, new Vector2(300, 150));
        Frame(game, platform);
        Assert.AreEqual(new RectF(270, 130, 60, 20), bubble.Bounds, "moved, as a bubble follows a character");

        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(20, 20))));
        Frame(game, platform);
        Assert.IsTrue(scene.GameplayEvents.Any(e => e.Kind == InputEventKind.PointerDown), "where the layer has no child, the press is gameplay's");
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

    /// <summary>Presses <paramref name="key"/> while <paramref name="modifier"/> is held.</summary>
    private static void Chord(Game game, HeadlessPlatform platform, Key modifier, Key key)
    {
        platform.Devices.Send(InputEvent.KeyDown(Keyboard, modifier));
        Press(game, platform, key);
        platform.Devices.Send(InputEvent.KeyUp(Keyboard, modifier));
        Frame(game, platform);
    }

    [TestMethod]
    public void A_text_field_selects_with_shift_and_the_pointer_and_typing_replaces_the_selection()
    {
        TextField? field = null;
        var scene = new UiScene(ui => ui.Content = new Panel(field = new TextField()));
        var (game, platform) = Start(scene);
        platform.Devices.Send(InputEvent.TextInput(Keyboard, "hello world"));
        Frame(game, platform);
        Assert.IsFalse(field!.HasSelection);

        platform.Devices.Send(InputEvent.KeyDown(Keyboard, Key.LeftShift));
        for (int i = 0; i < 5; i++) Press(game, platform, Key.Left);
        platform.Devices.Send(InputEvent.KeyUp(Keyboard, Key.LeftShift));
        Frame(game, platform);
        Assert.AreEqual("world", field.SelectedText, "Shift with the arrows extends the selection from where it started");
        Assert.AreEqual((6, 5), (field.SelectionStart, field.SelectionLength));
        Assert.AreEqual(0, scene.GameplayUps, "Shift and the arrows stayed in the field");

        platform.Devices.Send(InputEvent.TextInput(Keyboard, "there"));
        Frame(game, platform);
        Assert.AreEqual("hello there", field.Text, "typing replaces the selection");
        Assert.IsFalse(field.HasSelection);

        Chord(game, platform, Key.LeftShift, Key.Home);
        Assert.AreEqual("hello there", field.SelectedText);
        Press(game, platform, Key.Right);
        Assert.AreEqual((false, 11), (field.HasSelection, field.Caret), "an arrow without Shift ends the selection at its side");

        Chord(game, platform, Key.LeftControl, Key.A);
        Assert.AreEqual("hello there", field.SelectedText, "Ctrl+A selects everything");
        Press(game, platform, Key.Backspace);
        Assert.AreEqual("", field.Text, "Backspace deletes the selection");

        field.Text = "abcdef";
        Frame(game, platform);
        var b = field.Bounds;
        float y = b.Y + b.Height / 2;
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(b.X + 1, y))));
        Frame(game, platform);
        Assert.AreEqual((0, false), (field.Caret, field.HasSelection), "a press puts the caret where it lands");
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(b.Right - 1, y))));
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, game.Views.UiToCanvas(new Vector2(b.Right - 1, y))));
        Frame(game, platform);
        Assert.AreEqual("abcdef", field.SelectedText, "dragging selects");

        field.Select(1, 3);
        Assert.AreEqual(("bc", 3), (field.SelectedText, field.Caret), "Select sets the anchor and the caret");
        field.Text = "new";
        Assert.IsFalse(field.HasSelection, "setting the text ends the selection");
    }

    [TestMethod]
    public void A_text_field_copies_cuts_and_pastes_through_the_platform_clipboard()
    {
        TextField? field = null;
        var scene = new UiScene(ui => ui.Content = new Panel(field = new TextField { MaxLength = 10 }));
        var (game, platform) = Start(scene);
        Assert.AreSame(platform.Clipboard, scene.Widgets.Clipboard, "a scene's widgets use the platform's clipboard");
        field!.Text = "copy me";

        Chord(game, platform, Key.LeftControl, Key.A);
        Chord(game, platform, Key.LeftControl, Key.C);
        Assert.AreEqual("copy me", platform.Clipboard.Text, "Ctrl+C copies the selection");
        Assert.AreEqual("copy me", field.Text);

        Press(game, platform, Key.End);
        Chord(game, platform, Key.LeftShift, Key.Home);
        Chord(game, platform, Key.LeftSuper, Key.X);
        Assert.AreEqual(("copy me", ""), (platform.Clipboard.Text, field.Text), "Command+X (macOS) cuts as Ctrl+X does");

        platform.Clipboard.Text = "two\r\nlines that are long";
        Chord(game, platform, Key.LeftControl, Key.V);
        Assert.AreEqual("two lines ", field.Text, "a paste is one line, cut to MaxLength");
        Assert.AreEqual(0, scene.GameplayUps + scene.GameplayConfirms, "the shortcuts stayed in the field");

        field.Select(0, 3);
        Assert.IsTrue(field.Copy());
        Assert.AreEqual("two", platform.Clipboard.Text, "Copy, Cut and Paste are there for buttons of a game's own");
        Assert.IsTrue(field.Paste());
        Assert.AreEqual("two lines ", field.Text);

        var detached = new TextField { Text = "abc" };
        detached.Select(0, 3);
        Assert.IsFalse(detached.Copy(), "a field outside a root with a clipboard copies nothing");
        Assert.IsFalse(detached.Paste());
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
}
