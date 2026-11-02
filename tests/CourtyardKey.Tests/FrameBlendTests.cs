using System.Numerics;
using CourtyardKey.Scenes;
using MicroEngine.Input;

namespace CourtyardKey.Tests;

/// <summary>Moving things are drawn between their previous and current update; jumps are never blended.</summary>
[TestClass]
public sealed class FrameBlendTests
{
    [TestMethod]
    public void TheWalkingHeroIsDrawnBetweenItsLastTwoUpdates()
    {
        using var h = new Harness("road");
        h.Step(40);
        h.Platform.Keys.Set(Key.Right, true);
        h.Step(5);
        var previous = h.Play.Hero.Position;
        h.Step();
        var current = h.Play.Hero.Position;
        h.Platform.Keys.Set(Key.Right, false);

        Assert.AreNotEqual(previous, current, "The hero moved during the update.");
        Assert.AreEqual(Vector2.Lerp(previous, current, 0.5f), h.Play.Hero.DrawPositionAt(0.5f));
        Assert.AreEqual(previous, h.Play.Hero.DrawPositionAt(0));
        Assert.AreEqual(current, h.Play.Hero.DrawPositionAt(1), "At 1 (fixed-step runs) the hero is drawn exactly where it is.");
    }

    [TestMethod]
    public void TheScrollingCameraIsDrawnBetweenItsLastTwoUpdates()
    {
        using var h = new Harness("road");
        h.Step(40);
        h.WalkToTile(10, 4.8f);
        h.Platform.Keys.Set(Key.Left, true);
        h.WaitUntil(() => h.Play.Mode == PlayMode.Scroll, 400, "scroll start");
        h.Step(10);
        var previous = h.Play.Camera.Position;
        h.Step();
        var current = h.Play.Camera.Position;
        h.Platform.Keys.Set(Key.Left, false);

        Assert.AreNotEqual(previous, current, "The camera moved during the update.");
        Assert.AreEqual(Vector2.Lerp(previous, current, 0.25f), h.Play.CameraDrawPositionAt(0.25f));
        Assert.AreEqual(current, h.Play.CameraDrawPositionAt(1));
    }

    [TestMethod]
    public void ACheckpointJumpIsDrawnWhereItLandsNotStreakedAcrossTheRoom()
    {
        using var h = new Harness("road");
        h.Step(40);
        h.Platform.Keys.Set(Key.Right, true);
        h.Step(30);
        h.Platform.Keys.Set(Key.Right, false);
        var before = h.Play.Hero.Position;

        h.Play.PlaceAtCheckpoint(h.Play.Progress.Entrance);

        Assert.AreNotEqual(before, h.Play.Hero.Position, "The checkpoint is somewhere else.");
        Assert.AreEqual(h.Play.Hero.Position, h.Play.Hero.DrawPositionAt(0.5f));
        Assert.AreEqual(h.Play.Hero.Position, h.Play.Hero.DrawPositionAt(0));
        Assert.AreEqual(h.Play.Camera.Position, h.Play.CameraDrawPositionAt(0));
    }

    [TestMethod]
    public void APausedRoomIsDrawnAsItsLastUpdate()
    {
        using var h = new Harness("road");
        h.Step(40);
        Assert.AreEqual(h.Game.FrameBlend, h.Play.DrawBlend, "While playing, the room follows the engine's blend.");
        h.Press(Key.Escape);
        Assert.IsInstanceOfType<PauseScene>(h.Game.Scenes.Current);
        Assert.AreEqual(1f, h.Play.DrawBlend, "Under the pause menu nothing moves, so nothing is blended.");
    }
}
