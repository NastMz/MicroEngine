using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Tests.Interface;

/// <summary>
/// Corrections to the interface on the canvas, the text reveal, the list cursor and the time with the focus: a canvas
/// root is always drawn, once; a reveal survives any rate and step; a revealing label reuses what it can; a list that
/// shrinks keeps its window full; widgets on the canvas have whole sizes; a cursor restarts when its menu shows again.
/// </summary>
[TestClass]
public sealed class InterfaceCorrectionTests
{
    private static readonly DeviceId Keyboard = new(1), Mouse = new(2);
    private static readonly Color Red = new(255, 0, 0), White = Color.White;

    /// <summary>Where a canvas scene draws its widgets.</summary>
    public enum Drawn { Nowhere, InDraw, InDrawUi, TwiceInDraw }

    /// <summary>A white 20x10 block that counts how often it is drawn.</summary>
    private sealed class Block : Widget
    {
        public int Draws;
        public override bool Focusable => true;
        protected override Vector2 MeasureContent(UiContext context) => new(20, 10);
        protected override void DrawContent(IRenderer renderer, UiContext context)
        {
            Draws++;
            renderer.FillRect(Bounds, White);
        }
    }

    private sealed class CanvasScene(Drawn drawn, Action<UiRoot>? build = null) : Scene
    {
        public readonly Block Block = new();
        public bool Cover;
        public override void Enter()
        {
            Widgets.Space = UiSpace.Canvas;
            Widgets.Anchor = Anchor.TopLeft;
            if (build is null) Widgets.Content = Block; else build(Widgets);
        }
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer)
        {
            renderer.Clear(Color.Black);
            if (drawn is Drawn.InDraw or Drawn.TwiceInDraw) Widgets.Draw(renderer);
            if (drawn is Drawn.TwiceInDraw) Widgets.Draw(renderer);
            if (Cover) renderer.FillRect(new RectF(0, 0, 4, 4), Red);     // over the widgets, when the scene drew them first
        }
        public override void DrawUi(IRenderer ui)
        {
            if (drawn is Drawn.InDrawUi) Widgets.Draw(ui);
        }
    }

    private sealed class CanvasGame(Scene scene) : Game
    {
        public override GameSettings Settings { get; } = new("corrections", 320, 180, 1);
        protected override void Load() => Scenes.Push(scene);
    }

    private static (Game Game, HeadlessPlatform Platform) Start(Scene scene, bool rasterize = true)
    {
        var game = new CanvasGame(scene);
        var platform = HeadlessPlatform.For(game.Settings, rasterize);
        platform.Resize(640, 360);
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

    private static PixelImage World(HeadlessPlatform platform) => ((IViewCapture)platform).CaptureWorld();

    // ---- D1: a canvas root is always drawn, once

    [TestMethod]
    [DataRow(Drawn.Nowhere, DisplayName = "the scene never draws its widgets")]
    [DataRow(Drawn.InDrawUi, DisplayName = "the scene draws them from DrawUi only")]
    public void A_canvas_root_the_scene_does_not_draw_is_drawn_by_the_engine_after_the_scene(Drawn drawn)
    {
        var scene = new CanvasScene(drawn);
        var (_, platform) = Start(scene);
        Assert.AreEqual(1, scene.Block.Draws, "drawn once in the frame");
        Assert.AreEqual(White, World(platform)[5, 5], "the widget shows in the canvas: what takes input is visible");
        Assert.IsFalse(((IViewCapture)platform).FrameHasUi, "nothing went to the UI view");
    }

    [TestMethod]
    [DataRow(Drawn.InDraw, 1, DisplayName = "once in Draw")]
    [DataRow(Drawn.TwiceInDraw, 2, DisplayName = "twice in Draw: the scene's own business")]
    public void A_canvas_root_the_scene_draws_itself_is_not_drawn_again_by_the_engine(Drawn drawn, int expected)
    {
        var scene = new CanvasScene(drawn) { Cover = true };
        var (game, platform) = Start(scene);
        Assert.AreEqual(expected, scene.Block.Draws);
        Assert.AreEqual(Red, World(platform)[1, 1], "what the scene draws after its widgets stays over them");
        Assert.AreEqual(White, World(platform)[10, 5]);
        Frame(game, platform);
        Assert.AreEqual(2 * expected, scene.Block.Draws, "every frame the same");
    }

    [TestMethod]
    public void A_canvas_root_the_engine_draws_goes_over_what_the_scene_drew()
    {
        var scene = new CanvasScene(Drawn.Nowhere) { Cover = true };
        var (_, platform) = Start(scene);
        Assert.AreEqual(White, World(platform)[1, 1], "the interface is over the scene's picture");
    }

    [TestMethod]
    public void Canvas_widgets_go_through_the_scenes_and_the_games_post_processing_however_they_are_drawn()
    {
        foreach (var drawn in new[] { Drawn.Nowhere, Drawn.InDraw })
        {
            var scene = new CanvasScene(drawn);
            var (game, platform) = Start(scene);
            scene.PostProcess.Add(new ColorGrade { Tint = Red });
            Frame(game, platform);
            Assert.AreEqual(Red, World(platform)[5, 5], $"{drawn}: the scene's chain tints its widgets with the rest of its picture");

            scene.PostProcess.Enabled = false;
            game.PostProcess.Add(new ColorGrade { Tint = Red });
            Frame(game, platform);
            Assert.AreEqual(Red, World(platform)[5, 5], $"{drawn}: the canvas's chain covers them too");
            game.Shutdown();
        }
    }

    [TestMethod]
    public void An_undrawn_canvas_root_no_longer_exists_what_is_pressed_is_what_is_seen()
    {
        int clicks = 0;
        Button? play = null;
        var scene = new CanvasScene(Drawn.Nowhere, ui => ui.Content = play = new Button("PLAY", () => clicks++));
        var (game, platform) = Start(scene);
        var b = play!.Bounds;
        Assert.AreNotEqual(Color.Black, World(platform)[(int)b.X + 1, (int)b.Y + 1], "the button is in the picture");
        platform.Devices.Send(InputEvent.KeyDown(Keyboard, Key.Enter));
        Frame(game, platform);
        platform.Devices.Send(InputEvent.KeyUp(Keyboard, Key.Enter));
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, b.Center));
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, b.Center));
        Frame(game, platform);
        Assert.AreEqual(2, clicks, "and it is pressed by confirm and by a click, as a button on screen is");
    }

    // ---- D2: a reveal survives any rate and step

    [TestMethod]
    public void A_reveal_stays_inside_its_text_for_any_rate_and_step()
    {
        const float step = 1f / 60f;
        var reveal = new TextReveal { CharactersPerSecond = -5 };
        reveal.Start("HELLO THERE");
        for (int i = 0; i < 100; i++) reveal.Update(step);
        Assert.AreEqual(0, reveal.Revealed, "a negative rate reveals nothing");
        reveal.CharactersPerSecond = 45;
        reveal.Update(step);
        reveal.Update(step);
        Assert.AreEqual(1, reveal.Revealed, "and nothing was owed: the reveal goes on from where it was");

        reveal.Start("HELLO THERE");
        reveal.Update(-1f);
        Assert.AreEqual(0, reveal.Revealed, "a negative step is no time");
        reveal.Update(step);
        reveal.Update(step);
        Assert.AreEqual(1, reveal.Revealed);

        foreach (float bad in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f })
        {
            reveal.Start("HELLO THERE");
            reveal.CharactersPerSecond = bad;
            reveal.Update(step);
            Assert.AreEqual(0, reveal.Revealed, $"a rate of {bad} reveals nothing");
            Assert.AreEqual("", reveal.Visible.ToString());
            reveal.CharactersPerSecond = 60;
            reveal.Update(bad);
            Assert.AreEqual(0, reveal.Revealed, $"a step of {bad} is no time");
            reveal.Update(step);
            Assert.AreEqual(1, reveal.Revealed, $"the reveal is not stuck after {bad}");
            Assert.AreEqual(1, reveal.VisibleLines[0].Length);
        }

        reveal.Start("AB", -3);
        Assert.AreEqual(0, reveal.Revealed);
    }

    // ---- D3: a revealing label reuses what it can

    private sealed class RevealScene(string text) : Scene
    {
        public Label Text = null!;
        public override void Enter() => Widgets.Content = Text = new Label(text) { Reveal = new TextReveal { CharactersPerSecond = 60 } };
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    [TestMethod]
    [DoNotParallelize]
    public void A_revealing_label_makes_one_string_per_new_character_and_nothing_else()
    {
        // 400 lines of 20 characters: the label that showed the cost of an array of all its lines per character.
        string line = "ABCDEFGHIJKLMNOPQRST";
        var scene = new RevealScene(string.Join('\n', Enumerable.Repeat(line, 400)));
        var (game, _) = Start(scene, rasterize: false);
        var label = scene.Text;
        var reveal = label.Reveal!;
        for (int i = 0; i < 30; i++) game.StepForTest();          // warm: the caches exist
        var list = label.VisibleLines;

        long worst = 0;
        int steps = 0;
        for (int i = 0; i < 200; i++)
        {
            int before = reveal.Revealed;
            reveal.Update(1f / 60f);
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            var lines = label.VisibleLines;
            bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Assert.AreSame(list, lines, "the same list, with its entries brought up to date");
            if (reveal.Revealed != before) steps++;
            worst = Math.Max(worst, bytes);
        }
        Assert.IsTrue(steps > 150);
        // One string: the part of one 20-character line that shows (at most 22 + 2 * 20 bytes, rounded up).
        Assert.IsTrue(worst <= 72, $"a new character costs one short string, not a list of every line ({worst} bytes at worst)");

        long idle = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) _ = label.VisibleLines;
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - idle, "asked again without a new character: nothing");
    }

    // ---- D4: a list that shrinks keeps its window full

    [TestMethod]
    public void A_list_that_shrinks_keeps_its_window_full()
    {
        var cursor = new MenuCursor(10) { Wrap = false, Index = 9 };
        Assert.AreEqual(7, cursor.Top(3));
        cursor.Count = 5;
        Assert.AreEqual(4, cursor.Index);
        Assert.AreEqual(2, cursor.Top(3), "rows 2 to 4 of 5: no blank row under the last");
        cursor.Count = 2;
        Assert.AreEqual(0, cursor.Top(3), "a list shorter than its box starts at its first row");

        var grid = new MenuCursor(12, columns: 3) { Index = 11 };
        Assert.AreEqual(2, grid.Top(2), "rows 2 and 3 of 4");
        grid.Count = 6;
        Assert.AreEqual(0, grid.Top(2), "two rows left: both show");
    }

    // ---- D5: whole sizes on the canvas

    private sealed class Half : Widget
    {
        protected override Vector2 MeasureContent(UiContext context) => new(40.5f, 9.5f);
        protected override void DrawContent(IRenderer renderer, UiContext context) { }
    }

    [TestMethod]
    public void Widgets_on_the_canvas_have_whole_sizes_and_meet_without_gaps()
    {
        Panel? panel = null;
        Stack? stack = null;
        var scene = new CanvasScene(Drawn.Nowhere, ui => ui.Content = panel = new Panel(stack = new Stack { Spacing = 2.5f }
            .Add(new Half()).Add(new Half { Width = 31.25f }).Add(new Half())) { Padding = 3.5f });
        var (game, _) = Start(scene, rasterize: false);
        foreach (var w in game.Scenes.Current!.Widgets.AllWidgets)
        {
            var b = w.Bounds;
            Assert.IsTrue(b.X == MathF.Round(b.X) && b.Y == MathF.Round(b.Y) && b.Width == MathF.Round(b.Width) && b.Height == MathF.Round(b.Height),
                $"{w.GetType().Name} is on whole canvas pixels: {b}");
        }
        Assert.IsTrue(panel!.Bounds.Height > 30 && stack!.Children[1].Bounds.Y >= stack.Children[0].Bounds.Bottom, "children still follow each other");
    }

    // ---- D6: the time with the focus starts again when the menu shows again

    [TestMethod]
    public void The_time_with_the_focus_starts_again_when_the_content_is_shown_again()
    {
        Button? play = null;
        var scene = new CanvasScene(Drawn.Nowhere, ui => ui.Content = play = new Button("PLAY"));
        var (game, platform) = Start(scene, rasterize: false);
        var root = game.Scenes.Current!.Widgets;
        for (int i = 0; i < 20; i++) Frame(game, platform);
        float before = root.FocusTime;
        Assert.IsTrue(before > 0.3f);

        play!.Visible = false;
        for (int i = 0; i < 60; i++) Frame(game, platform);
        play.Visible = true;
        Frame(game, platform);
        Assert.AreSame(play, root.Focused, "the focus is where it was");
        Assert.AreEqual(0f, root.FocusTime, "and its time starts again: a cursor bobs from its first position");
        Frame(game, platform);
        Assert.IsTrue(root.FocusTime > 0 && root.FocusTime < before);
    }
}
