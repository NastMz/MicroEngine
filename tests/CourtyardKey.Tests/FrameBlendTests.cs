using System.Numerics;
using CourtyardKey.Scenes;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace CourtyardKey.Tests;

/// <summary>
/// Moving things are drawn between their previous and current update; jumps are never blended. The hero, the room's
/// actors and the camera are entities of the play scene's world, so the engine interpolates them: these checks set the
/// frame's blend as the engine does before drawing (<see cref="SceneWorld.BeginDraw"/>) and read what would be drawn.
/// </summary>
[TestClass]
public sealed class FrameBlendTests
{
    /// <summary>Sets the frame's blend as the engine does before drawing a frame after the latest update.</summary>
    private static IWorld DrawingAt(Harness h, float blend)
    {
        ((SceneWorld)h.Play.Objects).BeginDraw(h.Game.UpdateCount, blend);
        return h.Play.Objects;
    }

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
        var hero = h.Play.Hero.Id;

        Assert.AreNotEqual(previous, current, "The hero moved during the update.");
        Assert.AreEqual(Vector2.Lerp(previous, current, 0.5f), DrawingAt(h, 0.5f).DrawPosition2D(hero));
        Assert.AreEqual(previous, DrawingAt(h, 0).DrawPosition2D(hero));
        Assert.AreEqual(current, DrawingAt(h, 1).DrawPosition2D(hero), "At 1 (fixed-step runs) the hero is drawn exactly where it is.");
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
        var camera = h.Play.CameraEntity;

        Assert.AreNotEqual(previous, current, "The camera moved during the update.");
        Assert.AreEqual(Vector2.Lerp(previous, current, 0.25f), DrawingAt(h, 0.25f).DrawPosition2D(camera));
        Assert.AreEqual(current, DrawingAt(h, 1).DrawPosition2D(camera));
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
        var hero = h.Play.Hero.Id;

        h.Play.PlaceAtCheckpoint(h.Play.Progress.Entrance);

        Assert.AreNotEqual(before, h.Play.Hero.Position, "The checkpoint is somewhere else.");
        Assert.AreEqual(h.Play.Hero.Position, DrawingAt(h, 0.5f).DrawPosition2D(hero));
        Assert.AreEqual(h.Play.Hero.Position, DrawingAt(h, 0).DrawPosition2D(hero));
        Assert.AreEqual(h.Play.CameraPosition, DrawingAt(h, 0).DrawPosition2D(h.Play.CameraEntity));
    }

    [TestMethod]
    public void APausedRoomIsDrawnAsItsLastUpdate()
    {
        using var h = new Harness("road");
        h.Step(40);
        Assert.AreEqual(0.4f, DrawingAt(h, 0.4f).Blend, "While playing, the room follows the engine's blend.");
        h.Press(Key.Escape);
        Assert.IsInstanceOfType<PauseScene>(h.Game.Scenes.Current);
        Assert.AreEqual(1f, DrawingAt(h, 0.4f).Blend, "Under the pause menu nothing moves, so nothing is blended.");
    }
}
