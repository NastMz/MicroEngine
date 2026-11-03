using System.Numerics;
using CourtyardKey.Scenes;
using MicroEngine.Input;

namespace CourtyardKey.Tests;

/// <summary>
/// Moving things are drawn between their previous and current update; jumps are never blended. P7 port of
/// tests/CourtyardKey.Tests/FrameBlendTests.cs: the same four checks, reading the world's interpolation
/// (<c>Objects.DrawPosition2D</c>, <c>Objects.Blend</c>) instead of the game's own (<c>DrawPositionAt</c>,
/// <c>CameraDrawPositionAt</c>, <c>DrawBlend</c>), which the port deleted.
/// </summary>
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
        var world = h.Play.Objects;
        var hero = h.Play.Hero.Id;

        Assert.AreNotEqual(previous, current, "The hero moved during the update.");
        Assert.AreEqual(Vector2.Lerp(previous, current, 0.5f), world.DrawPosition2D(hero, 0.5f));
        Assert.AreEqual(previous, world.DrawPosition2D(hero, 0));
        Assert.AreEqual(current, world.DrawPosition2D(hero, 1), "At 1 (fixed-step runs) the hero is drawn exactly where it is.");
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
        var previous = h.Play.CameraPosition;
        h.Step();
        var current = h.Play.CameraPosition;
        h.Platform.Keys.Set(Key.Left, false);

        Assert.AreNotEqual(previous, current, "The camera moved during the update.");
        Assert.AreEqual(Vector2.Lerp(previous, current, 0.25f), h.Play.Objects.DrawPosition2D(h.Play.CameraEntity, 0.25f));
        Assert.AreEqual(current, h.Play.Objects.DrawPosition2D(h.Play.CameraEntity, 1));
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
        var world = h.Play.Objects;
        var hero = h.Play.Hero.Id;

        h.Play.PlaceAtCheckpoint(h.Play.Progress.Entrance);

        Assert.AreNotEqual(before, h.Play.Hero.Position, "The checkpoint is somewhere else.");
        Assert.AreEqual(h.Play.Hero.Position, world.DrawPosition2D(hero, 0.5f));
        Assert.AreEqual(h.Play.Hero.Position, world.DrawPosition2D(hero, 0));
        Assert.AreEqual(h.Play.CameraPosition, h.Play.Objects.DrawPosition2D(h.Play.CameraEntity, 0));
    }

    [TestMethod]
    public void APausedRoomIsDrawnAsItsLastUpdate()
    {
        using var h = new Harness("road");
        h.Step(40);
        h.Play.Objects.BeginDraw(h.Game.UpdateCount, 0.4f);
        Assert.AreEqual(0.4f, h.Play.Objects.Blend, "While playing, the room follows the engine's blend.");
        h.Press(Key.Escape);
        Assert.IsInstanceOfType<PauseScene>(h.Game.Scenes.Current);
        h.Play.Objects.BeginDraw(h.Game.UpdateCount, 0.4f);
        Assert.AreEqual(1f, h.Play.Objects.Blend, "Under the pause menu nothing moves, so nothing is blended.");
    }
}
