using System.Numerics;
using Examples;
using MicroEngine.Input;
using MicroEngine.UI;

namespace Samples.Tests;

/// <summary>The mouse actions example (input guide): click to move, wheel to zoom, drag to pan, and a mouse button rebound by listening.</summary>
[TestClass]
public sealed class MouseActionsExampleTests
{
    private static readonly DeviceId Mouse = new(2), Keyboard = new(1);

    private static void Send(MicroEngine.Core.HeadlessPlatform platform, ExamplesGame game, params InputEvent[] events)
    {
        foreach (var e in events) platform.Devices.Send(e);
        SettingsExampleTests.Frame(game, platform);
    }

    private static InputEvent Down(Vector2 at, PointerButton button = PointerButton.Primary) => InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, at, button);
    private static InputEvent Up(Vector2 at, PointerButton button = PointerButton.Primary) => InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, at, button);

    [TestMethod]
    public void AClickOrdersTheScoutTheWheelZoomsAndADragMovesTheView()
    {
        var (game, platform) = SettingsExampleTests.Start(new MouseActionsExample());
        var scene = (MouseActionsExample)game.Scenes.Current!;
        var start = scene.Scout;

        var click = new Vector2(250, 120);
        Send(platform, game, Down(click));
        Send(platform, game, Up(click));
        Assert.AreEqual(scene.Camera.ScreenToWorld(click), scene.Target, "the click is an order to go where the pointer is, in the world");
        for (int i = 0; i < 240; i++) SettingsExampleTests.Frame(game, platform);
        Assert.IsTrue(Vector2.Distance(scene.Scout, scene.Target) < 1 && scene.Scout != start, "the scout walked there");

        float zoom = scene.Camera.Zoom;
        Send(platform, game, InputEvent.WheelMoved(Mouse, new Vector2(0, 1), click));
        Assert.IsTrue(scene.Camera.Zoom > zoom, "a notch away from the player zooms in");
        Send(platform, game, InputEvent.WheelMoved(Mouse, new Vector2(0, -1), click));
        Send(platform, game, InputEvent.WheelMoved(Mouse, new Vector2(0, -1), click));
        Assert.IsTrue(scene.Camera.Zoom < zoom, "two notches towards the player, one right after the other, zoom out past where it started");
        Send(platform, game, InputEvent.KeyDown(Keyboard, Key.Equals));
        Send(platform, game, InputEvent.KeyUp(Keyboard, Key.Equals));
        Assert.AreEqual(zoom, scene.Camera.Zoom, 1e-4f, "the key bound beside the wheel does the same");

        var view = scene.Camera.Position;
        Send(platform, game, Down(click, PointerButton.Secondary));
        Assert.IsTrue(platform.Cursor.Relative, "the cursor is locked while the view is dragged");
        Send(platform, game, InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, click, delta: new Vector2(-30, 0)));
        Assert.IsTrue(scene.Camera.Position.X > view.X, "dragging left moves the view right, by the mouse's own motion");
        Send(platform, game, Up(click, PointerButton.Secondary));
        SettingsExampleTests.Frame(game, platform);
        Assert.IsFalse(platform.Cursor.Relative, "and freed when the button is let go");
    }

    [TestMethod]
    public void AClickOnTheButtonIsNotAnOrderAndTheNextMouseButtonBecomesTheOrder()
    {
        var (game, platform) = SettingsExampleTests.Start(new MouseActionsExample());
        var scene = (MouseActionsExample)game.Scenes.Current!;
        var target = scene.Target;
        var button = scene.Widgets.AllWidgets.OfType<Button>().Single();
        var on = game.Views.UiToCanvas(new Vector2(button.Bounds.X + button.Bounds.Width / 2, button.Bounds.Y + button.Bounds.Height / 2));

        Send(platform, game, Down(on));
        Send(platform, game, Up(on));
        SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(target, scene.Target, "the click went to the button, not to the game under it");
        Assert.IsTrue(scene.Listening, "and the button started listening");

        var away = new Vector2(200, 150);
        Send(platform, game, Down(away, PointerButton.Middle));
        Send(platform, game, Up(away, PointerButton.Middle));
        Assert.IsFalse(scene.Listening);
        Assert.AreEqual(target, scene.Target, "the click that chose the button is not an order either");
        CollectionAssert.AreEqual(new InputControl[] { PointerButton.Middle }, game.Input.GetBindings("order")!.Controls.ToArray());
        StringAssert.Contains(button.Text, "MMB");

        Send(platform, game, Down(away, PointerButton.Middle));
        Send(platform, game, Up(away, PointerButton.Middle));
        Assert.AreEqual(scene.Camera.ScreenToWorld(away), scene.Target, "the middle button orders now");
        Send(platform, game, Down(away));
        Send(platform, game, Up(away));
        Assert.AreEqual(scene.Camera.ScreenToWorld(away), scene.Target);
    }

    [TestMethod]
    public void AButtonAnotherActionUsedIsSwappedAndEscapeKeepsTheBinding()
    {
        var map = InputMap.CreateDefault();
        MouseActionsExample.BindDefaults(map);
        MouseActionsExample.Assign(map, "order", Down(default, PointerButton.Secondary));
        CollectionAssert.AreEqual(new InputControl[] { PointerButton.Secondary }, map.GetBindings("order")!.Controls.ToArray());
        CollectionAssert.AreEqual(new InputControl[] { PointerButton.Primary }, map.GetBindings("grab")!.Controls.ToArray(), "the drag got the button the order gave up");
        MouseActionsExample.Assign(map, "order", InputEvent.WheelMoved(Mouse, new Vector2(0, 1)));
        CollectionAssert.AreEqual(new InputControl[] { InputControl.WheelUp }, map.GetBindings("order")!.Controls.ToArray());
        CollectionAssert.AreEqual(new InputControl[] { Key.Equals, PointerButton.Secondary }, map.GetBindings("zoom-in")!.Controls.ToArray(), "zooming keeps its key and takes the button");

        var (game, platform) = SettingsExampleTests.Start(new MouseActionsExample());
        var scene = (MouseActionsExample)game.Scenes.Current!;
        scene.Listen();
        Send(platform, game, InputEvent.KeyDown(Keyboard, Key.Escape));
        Send(platform, game, InputEvent.KeyUp(Keyboard, Key.Escape));
        Assert.IsFalse(scene.Listening);
        CollectionAssert.AreEqual(new InputControl[] { PointerButton.Primary }, game.Input.GetBindings("order")!.Controls.ToArray());
        Assert.IsInstanceOfType<MouseActionsExample>(game.Scenes.Current, "the Escape that ended the listening did not also leave the example");

        SettingsExampleTests.Press(game, platform, "pause");
        Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current);
        Assert.AreEqual(0, game.Input.GetBindings("order")!.Controls.Count, "leaving the example unbinds its actions");
    }
}
