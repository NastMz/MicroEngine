using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Tests.Interface;

/// <summary>
/// A control's look as a function of the game's (<see cref="ControlSkin"/>): a button or a panel drawn by a delegate
/// that receives the control's state, on one control or on every control of a theme, without a widget type of the game's own.
/// </summary>
[TestClass]
public sealed class ControlSkinTests
{
    private static readonly DeviceId Keyboard = new(1), Mouse = new(2);

    private sealed class SkinScene(Action<UiRoot> build) : Scene
    {
        public override void Enter() => build(Widgets);
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class SkinGame(Scene scene) : Game
    {
        public override GameSettings Settings { get; } = new("skins", 320, 180, 1);
        protected override void Load() => Scenes.Push(scene);
    }

    private static (Game Game, HeadlessPlatform Platform) Start(Action<UiRoot> build)
    {
        var game = new SkinGame(new SkinScene(build));
        var platform = HeadlessPlatform.For(game.Settings);
        platform.Resize(320, 180);
        platform.PumpEvents(new IgnoredEvents());
        game.InitializeForTest(platform);
        Frame(game, platform);
        return (game, platform);
    }

    private static void Frame(Game game, HeadlessPlatform platform)
    {
        game.StepForTest();
        game.Render(platform.BeginFrame());
        platform.EndFrame();
    }

    private static Vector2 Center(Widget w) => new(w.Bounds.X + w.Bounds.Width / 2, w.Bounds.Y + w.Bounds.Height / 2);

    [TestMethod]
    public void A_button_with_a_skin_is_drawn_by_the_skin_alone_with_its_state()
    {
        var seen = new Dictionary<string, ControlState>();
        ControlSkin skin = (IRenderer r, in UiContext context, in ControlState state) => seen[state.Text] = state;
        Button? play = null, locked = null;
        var (_, platform) = Start(ui => ui.Content = new Stack()
            .Add(play = new Button("PLAY") { Skin = skin })
            .Add(locked = new Button("LOCKED") { Skin = skin, Enabled = false }));
        Assert.AreEqual(0, platform.UiRenderer.DrawCalls, "the skin draws nothing here, and the button draws nothing of its own");
        Assert.AreEqual(play!.Bounds, seen["PLAY"].Bounds);
        Assert.IsTrue(seen["PLAY"].Focused, "the first button has the focus");
        Assert.IsTrue(seen["PLAY"].Enabled);
        Assert.IsFalse(seen["PLAY"].Pressed);
        Assert.IsFalse(seen["LOCKED"].Focused);
        Assert.IsFalse(seen["LOCKED"].Enabled, "a disabled button says so to its skin");
        Assert.AreEqual(locked!.Bounds, seen["LOCKED"].Bounds);
    }

    [TestMethod]
    public void A_skin_sees_the_pointer_over_the_button_and_the_press()
    {
        ControlState last = default;
        Button? play = null;
        var (game, platform) = Start(ui => ui.Content = play = new Button("PLAY") { Skin = (IRenderer r, in UiContext c, in ControlState s) => last = s });
        var at = game.Views.UiToCanvas(Center(play!));
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, at));
        Frame(game, platform);
        Assert.IsTrue(last.Hovered);
        Assert.IsFalse(last.Pressed);
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, at));
        Frame(game, platform);
        Assert.IsTrue(last.Pressed, "held");
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, at));
        Frame(game, platform);
        Assert.IsFalse(last.Pressed);
    }

    [TestMethod]
    public void The_time_a_control_has_had_the_focus_starts_again_when_the_focus_moves()
    {
        var times = new Dictionary<string, float>();
        ControlSkin skin = (IRenderer r, in UiContext c, in ControlState s) => times[s.Text] = s.FocusTime;
        UiRoot? root = null;
        var (game, platform) = Start(ui => (root = ui).Content = new Stack().Add(new Button("A") { Skin = skin }).Add(new Button("B") { Skin = skin }));
        float step = root!.Time;                       // one update so far
        Assert.AreEqual(0f, times["A"], "it took the focus on this update");
        Assert.AreEqual(0f, times["B"], "a control without the focus has had it for no time");
        Frame(game, platform);
        Frame(game, platform);
        Assert.AreEqual(root.Time - step, times["A"], 1e-6f, "two updates later");
        Assert.AreEqual(root.FocusTime, times["A"]);

        platform.Devices.Send(InputEvent.KeyDown(Keyboard, Key.Down));
        Frame(game, platform);
        Assert.AreEqual(0f, times["B"], "B took the focus on this update");
        Assert.AreEqual(0f, times["A"]);
        platform.Devices.Send(InputEvent.KeyUp(Keyboard, Key.Down));
        Frame(game, platform);
        Assert.IsTrue(times["B"] > 0);
    }

    [TestMethod]
    public void A_theme_s_skin_draws_every_button_and_a_button_s_own_wins()
    {
        var byTheme = new List<string>();
        var byButton = new List<string>();
        var (_, platform) = Start(ui =>
        {
            ui.Theme = UiTheme.Default with { ButtonSkin = (IRenderer r, in UiContext c, in ControlState s) => byTheme.Add(s.Text) };
            ui.Content = new Stack()
                .Add(new Button("ONE"))
                .Add(new Button("TWO") { Skin = (IRenderer r, in UiContext c, in ControlState s) => byButton.Add(s.Text) });
        });
        CollectionAssert.AreEqual(new[] { "ONE" }, byTheme);
        CollectionAssert.AreEqual(new[] { "TWO" }, byButton);
        Assert.AreEqual(0, platform.UiRenderer.DrawCalls);
    }

    [TestMethod]
    public void A_button_s_padding_sets_the_room_around_its_text()
    {
        Button? row = null, plain = null;
        var (game, _) = Start(ui => ui.Content = new Stack { Align = Align.Start }
            .Add(row = new Button("NEW GAME") { Padding = new Vector2(5, 0) })
            .Add(plain = new Button("NEW GAME")));
        float text = game.Font.MeasureWidth("NEW GAME"), line = game.Font.LineHeight;
        Assert.AreEqual(new Vector2(text + 10, line), row!.Bounds.Size, "five units either side, none above or below");
        Assert.AreEqual(new Vector2(text + 12, line + 8), plain!.Bounds.Size, "the default: six and four layout units");
    }

    [TestMethod]
    public void A_panel_with_a_skin_is_drawn_by_it_and_still_draws_its_content()
    {
        ControlState box = default;
        int labels = 0;
        Panel? panel = null;
        var (_, platform) = Start(ui => ui.Content = panel = new Panel(new Label("INSIDE"))
        {
            Skin = (IRenderer r, in UiContext c, in ControlState s) => { box = s; labels++; },
        });
        Assert.AreEqual(panel!.Bounds, box.Bounds);
        Assert.AreEqual("", box.Text);
        Assert.AreEqual(1, labels);
        Assert.IsTrue(platform.UiRenderer.DrawCalls > 0, "the label inside is drawn");

        int themed = 0;
        Start(ui =>
        {
            ui.Theme = UiTheme.Default with { PanelSkin = (IRenderer r, in UiContext c, in ControlState s) => themed++ };
            ui.Content = new Panel(new Panel());
        });
        Assert.AreEqual(2, themed, "the theme's skin draws every panel");
    }

    [TestMethod]
    public void A_faded_control_keeps_its_theme_s_skin_and_tells_it_the_opacity()
    {
        float opacity = -1;
        Start(ui =>
        {
            ui.Theme = UiTheme.Default with { ButtonSkin = (IRenderer r, in UiContext c, in ControlState s) => opacity = c.Opacity };
            ui.Content = new Stack { Opacity = 0.5f }.Add(new Button("FADED"));
        });
        Assert.AreEqual(0.5f, opacity);
    }
}
