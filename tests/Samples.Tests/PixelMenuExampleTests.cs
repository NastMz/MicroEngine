using System.Numerics;
using Examples;
using MicroEngine.Input;
using MicroEngine.UI;

namespace Samples.Tests;

/// <summary>
/// The pixel menu example (UI guide): a menu and a dialogue box of widgets on the 320x180 canvas, drawn by skins, with a
/// text that reveals itself, driven by the keyboard's actions, the mouse and a finger.
/// </summary>
[TestClass]
public sealed class PixelMenuExampleTests
{
    private static readonly DeviceId Mouse = new(2), Finger = new(3);

    private static (ExamplesGame Game, MicroEngine.Core.HeadlessPlatform Platform, PixelMenuExample Scene) Open()
    {
        var (game, platform) = SettingsExampleTests.Start(new PixelMenuExample());
        return (game, platform, (PixelMenuExample)game.Scenes.Current!);
    }

    private static Vector2 Center(Widget w) => new(w.Bounds.X + w.Bounds.Width / 2, w.Bounds.Y + w.Bounds.Height / 2);

    private static void Whole(Widget w, string what)
    {
        var b = w.Bounds;
        Assert.IsTrue(b.X == MathF.Round(b.X) && b.Y == MathF.Round(b.Y) && b.Width == MathF.Round(b.Width) && b.Height == MathF.Round(b.Height),
            $"{what} is on whole canvas pixels ({b})");
    }

    [TestMethod]
    public void TheMenuIsLaidOutInWholeCanvasPixelsOnTheCanvas()
    {
        var (game, _, scene) = Open();
        Assert.AreEqual(UiSpace.Canvas, scene.Widgets.Space);
        Assert.AreEqual(new Vector2(320, 180), scene.Widgets.Size, "the canvas, not the 960x540 window");
        Whole(scene.Menu, "the menu");
        foreach (var row in scene.Rows) Whole(row, row.Text);
        Assert.AreEqual(160, scene.Menu.Bounds.X + scene.Menu.Bounds.Width / 2, 0.5f, "centred in the canvas");
        Assert.AreEqual(new Vector2(96, game.Font.LineHeight + 4), scene.Rows[0].Bounds.Size, "a row: a line of text and two pixels above and below");
        Assert.AreSame(scene.Rows[0], scene.Widgets.Focused);
    }

    [TestMethod]
    public void TheKeyboardWalksTheMenuAndTheDialogueRevealsAtTheDemosRate()
    {
        var (game, platform, scene) = Open();
        SettingsExampleTests.Press(game, platform, "down");
        Assert.AreSame(scene.Rows[2], scene.Widgets.Focused, "TRADE cannot be chosen: the cursor skips it");
        SettingsExampleTests.Press(game, platform, "up");
        Assert.IsFalse(scene.Dialogue.Visible);

        SettingsExampleTests.Press(game, platform, "confirm");              // TALK
        Assert.IsTrue(scene.Dialogue.Visible);
        Assert.AreSame(scene.More, scene.Widgets.Focused, "confirm now goes on with the dialogue");
        Whole(scene.Dialogue, "the dialogue");
        var reveal = scene.Text.Reveal!;
        // 45 characters a second: the count the demos' typewriter gives on the same updates. The press took two
        // updates: the one that chose TALK, and the first of the reveal.
        float visible = MathF.Min(reveal.Count, 0 + 45f * (1f / 60f));
        Assert.AreEqual((int)visible, reveal.Revealed, "the first update of the reveal");
        for (int i = 1; i < 40; i++)
        {
            SettingsExampleTests.Frame(game, platform);
            visible = MathF.Min(reveal.Count, visible + 45f * (1f / 60f));
            Assert.AreEqual(Math.Min(reveal.Count, (int)visible), reveal.Revealed, $"update {i + 1} of the reveal");
        }
        Assert.IsTrue(scene.Blips > 20, "a sound per letter");
        Assert.IsFalse(reveal.Complete);
        var box = scene.Dialogue.Bounds;

        SettingsExampleTests.Press(game, platform, "confirm");              // the rest of the page at once
        Assert.IsTrue(reveal.Complete);
        Assert.AreEqual(3, scene.Text.Lines.Count, "the page takes the three lines of the box");
        SettingsExampleTests.Press(game, platform, "confirm");              // the next page
        Assert.IsFalse(reveal.Complete);
        StringAssert.StartsWith(reveal.Text, "Take the lantern");
        Assert.AreEqual(box, scene.Dialogue.Bounds, "the box keeps its size and place");
        SettingsExampleTests.Press(game, platform, "confirm");
        SettingsExampleTests.Press(game, platform, "confirm");              // the last page closes the box
        Assert.IsFalse(scene.Dialogue.Visible);
        Assert.AreSame(scene.Rows[0], scene.Widgets.Focused, "back in the menu");
    }

    [TestMethod]
    public void TheMouseAndAFingerPressTheSameButtonsAtTheirCanvasPositions()
    {
        var (game, platform, scene) = Open();
        // The mouse over LEAVE moves the cursor there; positions are canvas pixels, as backends report them.
        platform.Devices.Send(InputEvent.PointerMove(Mouse, 0, PointerKind.Mouse, Center(scene.Rows[2])));
        SettingsExampleTests.Frame(game, platform);
        SettingsExampleTests.Frame(game, platform);
        Assert.AreSame(scene.Rows[2], scene.Widgets.Focused);

        // A click on TALK opens the dialogue.
        var talk = Center(scene.Rows[0]);
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, talk));
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, talk));
        SettingsExampleTests.Frame(game, platform);
        Assert.IsTrue(scene.Dialogue.Visible);
        SettingsExampleTests.Frame(game, platform);

        // A finger on the arrow shows the rest of the page, then turns it.
        var arrow = Center(scene.More);
        for (int tap = 0; tap < 2; tap++)
        {
            platform.Devices.Send(InputEvent.PointerDown(Finger, 5, PointerKind.Touch, arrow));
            platform.Devices.Send(InputEvent.PointerUp(Finger, 5, PointerKind.Touch, arrow));
            SettingsExampleTests.Frame(game, platform);
            SettingsExampleTests.Frame(game, platform);
        }
        StringAssert.StartsWith(scene.Text.Reveal!.Text, "Take the lantern", "the second tap turned the page");

        // The menu under the dialogue takes nothing meanwhile.
        platform.Devices.Send(InputEvent.PointerDown(Mouse, 0, PointerKind.Mouse, Center(scene.Rows[2])));
        platform.Devices.Send(InputEvent.PointerUp(Mouse, 0, PointerKind.Mouse, Center(scene.Rows[2])));
        SettingsExampleTests.Frame(game, platform);
        Assert.AreSame(scene, game.Scenes.Current, "LEAVE is disabled with its menu");
    }
}
