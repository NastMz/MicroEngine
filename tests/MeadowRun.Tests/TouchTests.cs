using MeadowRun.Scenes;
using MicroEngine.Core;
using MicroEngine.Input;

namespace MeadowRun.Tests;

/// <summary>Meadow Run played through its on-screen controls: touches in, the player's movement out.</summary>
[TestClass]
public sealed class TouchTests
{
    private static (MeadowGame Game, HeadlessPlatform Platform, PlayScene Play) Level()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "meadowrun-tests", Guid.NewGuid().ToString("N"))).FullName;
        var game = new MeadowGame(dir, new LaunchOptions("level", 1));
        var platform = new HeadlessPlatform();
        platform.TouchScreen.TouchFirst = true;
        game.InitializeForTest(platform);
        for (int i = 0; i < 90; i++) game.StepForTest();
        return (game, platform, (PlayScene)game.Scenes.Current!);
    }

    [TestMethod]
    public void HoldingRightOnTheDPadWhileTappingJumpRunsAndJumps()
    {
        var (game, platform, play) = Level();
        var player = play.Stage.Player;
        Assert.IsTrue(player.Grounded, "standing at the start");
        float x0 = player.Position.X, y0 = player.Position.Y;
        var screen = platform.TouchScreen;
        var (pad, padRadius) = game.Touch.Geometry("dpad");
        screen.Down(1, pad + new System.Numerics.Vector2(padRadius * 0.8f, 0));
        for (int i = 0; i < 20; i++) game.StepForTest();
        Assert.IsTrue(player.Position.X > x0 + 5, "the right thumb on the d-pad walks right");
        Assert.IsTrue(game.Touch.Shown, "the controls are drawn on a touch-first screen");

        float before = player.Position.X;
        screen.Down(2, game.Touch.Geometry("jump").Center);
        float highest = player.Position.Y;
        for (int i = 0; i < 12; i++)
        {
            game.StepForTest();
            highest = Math.Min(highest, player.Position.Y);
        }
        Assert.IsTrue(highest < y0 - 12, $"the jump lifts the player (from {y0} to {highest})");
        Assert.IsTrue(player.Position.X > before + 5, "the direction stays held during the jump");
        screen.Up(2);
        screen.Up(1);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.IsTrue(player.Grounded);
        float rest = player.Position.X;
        for (int i = 0; i < 20; i++) game.StepForTest();
        Assert.AreEqual(rest, player.Position.X, 0.5f, "lifting the thumbs stops the player: nothing stays pressed");
    }

    [TestMethod]
    public void TheDPadIsFourWaySoARolledThumbNeverCrouches()
    {
        var (game, platform, _) = Level();
        var (pad, r) = game.Touch.Geometry("dpad");
        platform.TouchScreen.Down(1, pad + new System.Numerics.Vector2(r * 0.8f, r * 0.6f));
        game.StepForTest();
        Assert.IsTrue(game.Input.Down("right") && !game.Input.Down("down"));
    }

    [TestMethod]
    public void TheAButtonConfirmsMenusAndTheBButtonRuns()
    {
        var (game, platform, _) = Level();
        platform.TouchScreen.Down(1, game.Touch.Geometry("run").Center);
        game.StepForTest();
        Assert.IsTrue(game.Input.Down("run"), "B runs");
        Assert.IsTrue(game.Input.Down("cancel"), "and cancels, like X on the keyboard");
        platform.TouchScreen.Move(1, game.Touch.Geometry("jump").Center);
        game.StepForTest();
        Assert.IsTrue(game.Input.Down("jump"), "the thumb rolls from B onto A");
        Assert.IsTrue(game.Input.Down("confirm"), "A is also confirm");
        Assert.IsFalse(game.Input.Down("run"), "and B is let go");
    }
}
