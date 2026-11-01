using MicroEngine.Input;
using TrailDuel.Scenes;

namespace TrailDuel.Tests;

/// <summary>Trail Duel through its on-screen controls: grid steps, the start menu and the stick's four sectors.</summary>
[TestClass]
public sealed class TouchTests
{
    [TestMethod]
    public void TheDPadStepsOnTheGridAndMenuOpensTheStartMenu()
    {
        using var h = new Harness("town");
        h.Step(60);
        var world = (OverworldScene)h.Game.Scenes.Current!;
        int x0 = world.Player.X, y0 = world.Player.Y;
        h.Game.Touch.ScriptTouch(1, "dpad", new System.Numerics.Vector2(0, 0.8f));
        h.Step(40);
        h.Game.Touch.ScriptLift(1);
        h.Step(20);
        Assert.IsTrue(world.Player.X != x0 || world.Player.Y != y0, "the d-pad walks the player on the grid");

        h.Game.Touch.ScriptTouch(2, "pause", default);
        h.Game.Touch.ScriptLift(2);
        h.Step(10);
        Assert.IsInstanceOfType<StartMenuScene>(h.Game.Scenes.Current);
        h.Game.Touch.ScriptTouch(3, "cancel", default);
        h.Game.Touch.ScriptLift(3);
        h.Step(10);
        Assert.IsInstanceOfType<OverworldScene>(h.Game.Scenes.Current, "B closes the menu");
    }

    [TestMethod]
    public void AGamepadStickNeverStepsDiagonally()
    {
        using var h = new Harness("town");
        h.Step(10);
        h.Platform.Keys.SetLeftStick(System.Numerics.Vector2.Normalize(new System.Numerics.Vector2(1, 0.8f)));
        h.Step(1);
        Assert.IsTrue(h.Game.Input.Down("right") && !h.Game.Input.Down("down"), "four sectors: the dominant direction only");
        Assert.AreEqual(StickDirections.Four, h.Game.Input.StickMode);
    }
}
