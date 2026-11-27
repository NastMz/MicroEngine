using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Tests.Interface;

/// <summary>
/// A scene's widgets on the game's canvas (<see cref="UiSpace.Canvas"/>): laid out and drawn in canvas pixels, on whole
/// pixels, by the scene's own <see cref="Scene.Draw"/>, and given the pointer in canvas pixels, whatever the window's size.
/// </summary>
[TestClass]
public sealed class CanvasWidgetTests
{
    private static readonly DeviceId Keyboard = new(1), Mouse = new(2), Finger = new(3);

    /// <summary>A scene on a 320x180 canvas whose widgets live in the canvas and are drawn by its own <see cref="Draw"/>.</summary>
    private sealed class CanvasScene(Action<UiRoot> build, UiSpace space = UiSpace.Canvas) : Scene
    {
        public int GameplayConfirms;
        public override void Enter()
        {
            Widgets.Space = space;
            build(Widgets);
        }
        public override void Update(float dt)
        {
            if (Input.Pressed(Input.Names.Confirm)) GameplayConfirms++;
        }
        public override void Draw(IRenderer renderer)
        {
            renderer.Clear(Color.Black);
            Widgets.Draw(renderer);
        }
    }

    private sealed class CanvasGame(Scene scene) : Game
    {
        public override GameSettings Settings { get; } = new("canvas-ui", 320, 180, 1);
        protected override void Load() => Scenes.Push(scene);
    }

    private static (Game Game, HeadlessPlatform Platform) Start(Scene scene, int scale = 2, bool rasterize = false)
    {
        var game = new CanvasGame(scene);
        var platform = HeadlessPlatform.For(game.Settings, rasterize);
        platform.Resize(320 * scale, 180 * scale);
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
    public void Widgets_on_the_canvas_are_laid_out_in_canvas_pixels_whatever_the_window()
    {
        foreach (int scale in new[] { 2, 3 })
        {
            Stack? stack = null;
            var (game, platform) = Start(new CanvasScene(ui => ui.Content = stack = new Stack().Add(new Button("PLAY")).Add(new Button("QUIT"))), scale);
            Assert.AreEqual(new Vector2(320, 180), game.Scenes.Current!.Widgets.Size, $"the canvas's size at window scale {scale}");
            Assert.AreEqual(160, stack!.Bounds.X + stack.Bounds.Width / 2, 0.5f, "centred in the 320-pixel canvas");
            Assert.AreEqual(90, stack.Bounds.Y + stack.Bounds.Height / 2, 0.5f);
            Assert.AreEqual(0, platform.UiRenderer.DrawCalls, "nothing went to the UI view");
            Assert.IsFalse(((IViewCapture)platform).FrameHasUi, "the frame has no UI view: the widgets are part of the canvas");
            Assert.IsTrue(platform.Renderer.DrawCalls > 0, "the widgets were drawn in the canvas");
        }
    }

    [TestMethod]
    public void A_root_in_the_UI_view_is_still_drawn_by_the_engine_there()
    {
        var (_, platform) = Start(new CanvasScene(ui => ui.Content = new Button("PLAY"), UiSpace.UiView));
        Assert.IsTrue(platform.UiRenderer.DrawCalls > 0, "the engine drew the root in the UI view");
    }

    [TestMethod]
    public void A_click_and_a_tap_reach_a_widget_at_its_canvas_position()
    {
        int clicks = 0;
        Button? play = null;
        var scene = new CanvasScene(ui => ui.Content = play = new Button("PLAY", () => clicks++));
        var (game, platform) = Start(scene, scale: 3);
        var at = Center(play!);

        // Backends report the pointer in canvas pixels: the widget's own units here.
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, at));
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, at));
        Frame(game, platform);
        Assert.AreEqual(1, clicks, "a click at the button's canvas position");

        platform.Devices.Send(InputEvent.PointerDown(Finger, 7, PointerKind.Touch, at));
        platform.Devices.Send(InputEvent.PointerUp(Finger, 7, PointerKind.Touch, at));
        Frame(game, platform);
        Assert.AreEqual(2, clicks, "a tap at the button's canvas position");

        // The same point of the window in UI units (three times as far) is not on the button.
        var elsewhere = at * 3;
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, elsewhere));
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, elsewhere));
        Frame(game, platform);
        Assert.AreEqual(2, clicks);
    }

    [TestMethod]
    public void The_pointer_over_a_canvas_widget_is_on_the_UI()
    {
        Button? play = null;
        var (game, platform) = Start(new CanvasScene(ui => ui.Content = play = new Button("PLAY")), scale: 3);
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, Center(play!)));
        Frame(game, platform);
        Assert.IsTrue(game.Views.PointerOnUi, "the pointer is over the button");
        Assert.IsFalse(game.Views.WorldPointer().Present, "so it is not the world's");
        Assert.AreSame(play, game.Scenes.Current!.Widgets.Hovered);

        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, new Vector2(2, 2)));
        Frame(game, platform);
        Assert.IsFalse(game.Views.PointerOnUi);
    }

    [TestMethod]
    public void The_keyboard_moves_and_confirms_on_the_canvas_as_in_the_UI_view()
    {
        int quits = 0;
        var scene = new CanvasScene(ui => ui.Content = new Stack().Add(new Button("PLAY")).Add(new Button("QUIT", () => quits++)));
        var (game, platform) = Start(scene);
        foreach (var key in new[] { Key.Down, Key.Enter })
        {
            platform.Devices.Send(InputEvent.KeyDown(Keyboard, key));
            Frame(game, platform);
            platform.Devices.Send(InputEvent.KeyUp(Keyboard, key));
            Frame(game, platform);
        }
        Assert.AreEqual(1, quits);
        Assert.AreEqual(0, scene.GameplayConfirms, "the widgets consumed the confirm");
    }

    /// <summary>A widget that measures a size that is not whole, to see where it lands.</summary>
    private sealed class Odd : Widget
    {
        protected override Vector2 MeasureContent(UiContext context) => new(33, 9);
        protected override void DrawContent(IRenderer renderer, UiContext context) => renderer.FillRect(Bounds, Color.White);
    }

    [TestMethod]
    public void Widgets_on_the_canvas_land_on_whole_pixels()
    {
        Odd? odd = null;
        var scene = new CanvasScene(ui =>
        {
            ui.Offset = new Vector2(0.4f, 0.6f);
            ui.Content = new Stack { Align = Align.Center }.Add(new Button("A LONGER LINE")).Add(odd = new Odd { Offset = new Vector2(0.3f, 0) });
        });
        Start(scene);
        var b = odd!.Bounds;
        Assert.AreEqual(MathF.Round(b.X), b.X, "x is a whole canvas pixel");
        Assert.AreEqual(MathF.Round(b.Y), b.Y, "y is a whole canvas pixel");
    }

    [TestMethod]
    public void A_root_in_the_UI_view_keeps_fractional_positions()
    {
        Odd? odd = null;
        Start(new CanvasScene(ui =>
        {
            ui.Anchor = Anchor.TopLeft;
            ui.Offset = new Vector2(0.5f, 0.25f);
            ui.Content = odd = new Odd();
        }, UiSpace.UiView));
        Assert.AreEqual(new Vector2(0.5f, 0.25f), odd!.Bounds.Position, "UI units are not pixels: nothing is rounded there");
    }

    [TestMethod]
    public void The_picture_is_whole_canvas_pixels_at_any_window_scale()
    {
        PixelImage? first = null;
        foreach (int scale in new[] { 2, 3 })
        {
            var scene = new CanvasScene(ui => ui.Content = new Panel(new Stack().Add(new Label("PAUSED")).Add(new Button("RESUME")).Add(new Button("QUIT"))));
            var (_, platform) = Start(scene, scale, rasterize: true);
            var capture = (IViewCapture)platform;
            var world = capture.CaptureWorld();
            Assert.AreEqual((320, 180), (world.Width, world.Height));
            // Nothing was drawn at the window's resolution: what the player sees is the canvas, scaled whole by the platform.
            var screen = capture.CaptureScreen();
            Assert.AreEqual((320, 180), (screen.Width, screen.Height), "the capture is the canvas itself");
            int lit = 0;
            for (int y = 0; y < 180; y++)
                for (int x = 0; x < 320; x++)
                    if (world[x, y] != Color.Black) lit++;
            Assert.IsTrue(lit > 500, "the menu was drawn");
            if (first is { } other)
            {
                for (int y = 0; y < 180; y++)
                    for (int x = 0; x < 320; x++)
                        if (other[x, y] != world[x, y]) Assert.Fail($"canvas pixel {x},{y} differs between window scales");
            }
            first = world;
        }
    }

    private sealed class CameraScene : Scene
    {
        public Button Play = null!;
        public override void Enter()
        {
            WorldCamera = new Camera2D(320, 180) { Position = new Vector2(500, 300) };
            Widgets.Space = UiSpace.Canvas;
            Widgets.Anchor = Anchor.TopLeft;
            Widgets.Content = Play = new Button("PLAY");
        }
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer)
        {
            renderer.Clear(Color.Black);
            Widgets.Draw(renderer);
        }
    }

    [TestMethod]
    public void Canvas_widgets_stay_where_they_are_under_the_scene_s_world_camera()
    {
        var scene = new CameraScene();
        var (_, platform) = Start(scene, rasterize: true);
        var world = ((IViewCapture)platform).CaptureWorld();
        Assert.AreNotEqual(Color.Black, world[1, 1], "the button is at the canvas's corner, not moved by the camera");
        Assert.AreEqual(0, platform.Renderer.CameraDepth);
    }
}
