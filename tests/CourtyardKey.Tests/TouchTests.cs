using System.Numerics;
using MicroEngine.Input;

namespace CourtyardKey.Tests;

/// <summary>Courtyard Key through its on-screen joystick: analog walking, diagonals, and parity with the keyboard.</summary>
[TestClass]
public sealed class TouchTests
{
    /// <summary>Where the hero is after walking for 24 frames from the "road" start, driven by <paramref name="drive"/>.</summary>
    private static Vector2 Walk(Action<Harness> drive)
    {
        using var h = new Harness("road");
        h.Step(40);
        var start = h.Play.Hero.Position;
        drive(h);
        h.Step(24);
        return h.Play.Hero.Position - start;
    }

    [TestMethod]
    public void TheJoystickWalksAtTheSpeedItIsPushed()
    {
        var keyboard = Walk(h => h.Platform.Keys.Set(Key.Down, true));
        var full = Walk(h => h.Game.Touch.ScriptTouch(1, "stick", new Vector2(0, 1)));
        var half = Walk(h => h.Game.Touch.ScriptTouch(1, "stick", new Vector2(0, 0.55f)));
        Assert.IsTrue(keyboard.Y > 20, $"the keyboard walks down ({keyboard})");
        Assert.AreEqual(keyboard.Y, full.Y, 1.5f, "a full push walks as fast as a key");
        Assert.IsTrue(half.Y > 3 && half.Y < full.Y * 0.7f, $"a half push walks slower ({half.Y} vs {full.Y})");
    }

    [TestMethod]
    public void JoystickDiagonalsMatchTheKeyboardsDiagonals()
    {
        var keys = Walk(h => { h.Platform.Keys.Set(Key.Left, true); h.Platform.Keys.Set(Key.Down, true); });
        var stick = Walk(h => h.Game.Touch.ScriptTouch(1, "stick", Vector2.Normalize(new Vector2(-1, 1))));
        Assert.IsTrue(stick.X < -5 && stick.Y > 5, $"moved down-left ({stick})");
        Assert.AreEqual(keys.X, stick.X, 1.5f);
        Assert.AreEqual(keys.Y, stick.Y, 1.5f);
    }

    [TestMethod]
    public void ButtonsAndPauseAreReachable()
    {
        using var h = new Harness("road");
        h.Step(40);
        h.Platform.TouchScreen.TouchFirst = true;
        h.Game.Touch.ScriptTouch(1, "pause", default);
        h.Game.Touch.ScriptLift(1);
        h.Step(6);
        Assert.IsInstanceOfType<Scenes.PauseScene>(h.Game.Scenes.Current, "the pause button paused the game");
    }
}
