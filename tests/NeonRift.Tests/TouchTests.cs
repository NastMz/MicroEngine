using System.Numerics;
using MicroEngine.Core;
using NeonRift.Gameplay;
using NeonRift.Scenes;

namespace NeonRift.Tests;

/// <summary>Neon Rift through its on-screen controls: joystick aiming, firing, jumping and the lock toggle at once.</summary>
[TestClass]
public sealed class TouchTests
{
    private static (RiftGame Game, HeadlessPlatform Platform, PlayScene Play) Stage()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "neonrift-tests", Guid.NewGuid().ToString("N"))).FullName;
        var game = new RiftGame(new LaunchOptions("stage", God: true), saveDirectory: dir);
        var platform = HeadlessPlatform.For(game.Settings);
        platform.TouchScreen.TouchFirst = true;
        game.InitializeForTest(platform);
        for (int i = 0; i < 60; i++) game.StepForTest();
        return (game, platform, (PlayScene)game.Scenes.Current!);
    }

    [TestMethod]
    public void AimingUpRightWithTheStickWhileHoldingFireShootsDiagonally()
    {
        var (game, _, play) = Stage();
        game.Touch.ScriptTouch(1, "stick", Vector2.Normalize(new Vector2(1, -1)));
        game.Touch.ScriptTouch(2, "fire", default);
        for (int i = 0; i < 20; i++) game.StepForTest();
        Assert.IsTrue(game.Input.Down("right") && game.Input.Down("up") && game.Input.Down("fire"), "two thumbs: aim and fire");
        var shots = play.Stage.PlayerShots.ToList();
        Assert.IsTrue(shots.Count > 0, "firing");
        Assert.IsTrue(shots.All(s => s.Velocity.X > 0 && s.Velocity.Y < 0), "every shot goes up and right");
    }

    [TestMethod]
    public void JumpingWhileRunningAndFiringNeedsNoThirdHandAndLockToggles()
    {
        var (game, platform, play) = Stage();
        var player = play.Stage.Player;
        float x0 = player.Position.X, y0 = player.Position.Y;
        game.Touch.ScriptTouch(1, "stick", new Vector2(1, 0));
        game.Touch.ScriptTouch(2, "fire", default);
        for (int i = 0; i < 10; i++) game.StepForTest();
        // The right thumb rolls from FIRE onto JUMP (sliding is on in this layout).
        platform.TouchScreen.Down(3, game.Touch.Geometry("jump").Center);
        float highest = player.Position.Y;
        for (int i = 0; i < 15; i++) { game.StepForTest(); highest = Math.Min(highest, player.Position.Y); }
        Assert.IsTrue(highest < y0 - 10, "jumped");
        Assert.IsTrue(player.Position.X > x0 + 5, "while running right");
        platform.TouchScreen.Up(3);
        game.Touch.ScriptLift(1);
        game.Touch.ScriptLift(2);
        for (int i = 0; i < 40; i++) game.StepForTest();

        game.Touch.ScriptTouch(4, "lock", default);
        game.Touch.ScriptLift(4);
        for (int i = 0; i < 3; i++) game.StepForTest();
        Assert.IsTrue(game.Input.Down("lock"), "a tap turns the aim lock on");
        float x1 = player.Position.X;
        game.Touch.ScriptTouch(5, "stick", new Vector2(1, 0));
        for (int i = 0; i < 20; i++) game.StepForTest();
        Assert.AreEqual(x1, player.Position.X, 0.5f, "locked: the stick aims without walking");
        Assert.AreEqual(MicroEngine.Input.InputDevice.Touch, InputDevice(game));
    }

    private static MicroEngine.Input.InputDevice InputDevice(RiftGame game) => game.Input.LastDevice;
}
