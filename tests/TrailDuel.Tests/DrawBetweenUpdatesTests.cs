using System.Numerics;
using TrailDuel.Rules;
using TrailDuel.Scenes;
using TrailDuel.Visuals;
using TrailDuel.World;

namespace TrailDuel.Tests;

/// <summary>
/// Moving things are drawn between their previous and current update (Game.FrameBlend), snap on jumps, and draw
/// exactly the current state at a blend of 1. The engine sets FrameBlend itself (1 in these fixed-step runs), so the
/// tests pass the blend to the game's own drawing helpers.
/// </summary>
[TestClass]
public sealed class DrawBetweenUpdatesTests
{
    [TestMethod]
    public void AWalkingActorIsDrawnBetweenItsPreviousAndCurrentPositions()
    {
        var actor = new Actor(CharacterLook.Player, 1, 1, Direction.Right);
        actor.RecordDrawn(1);
        actor.StartStep(Direction.Right, Actor.StepTime);
        actor.Update(Actor.StepTime / 2);
        actor.RecordDrawn(2);

        Assert.AreEqual(new Vector2(24, 16), actor.PixelPosition, "half way to the next tile");
        Assert.AreEqual(new Vector2(16, 16), actor.DrawPosition(2, 0), "blend 0 draws the previous update");
        Assert.AreEqual(new Vector2(20, 16), actor.DrawPosition(2, 0.5f), "blend 0.5 draws half way between the updates");
        Assert.AreEqual(actor.PixelPosition, actor.DrawPosition(2, 1), "blend 1 draws exactly the current update");
        Assert.AreEqual(actor.PixelPosition, actor.DrawPosition(3, 0.5f), "a frame of a later update the actor missed draws it where it is");
    }

    [TestMethod]
    public void AHoppingActorsLiftIsDrawnBetweenUpdates()
    {
        var actor = new Actor(CharacterLook.Player, 1, 1, Direction.Down);
        actor.RecordDrawn(1);
        actor.StartHop(Direction.Down);
        actor.Update(Actor.HopTime / 2);
        actor.RecordDrawn(2);

        Assert.AreEqual(actor.Lift / 2, actor.DrawLift(2, 0.5f), 1e-4f);
        Assert.AreEqual(actor.Lift, actor.DrawLift(2, 1));
    }

    [TestMethod]
    public void ATeleportedActorSnapsInsteadOfStreaking()
    {
        var actor = new Actor(CharacterLook.Rival, 1, 1, Direction.Down);
        actor.RecordDrawn(1);
        actor.Teleport(9, 4);
        Assert.AreEqual(new Vector2(144, 64), actor.DrawPosition(1, 0.5f), "drawn at the new tile at once");
        actor.RecordDrawn(2);
        Assert.AreEqual(new Vector2(144, 64), actor.DrawPosition(2, 0.25f), "the next update starts from the new tile");
    }

    [TestMethod]
    public void TheOverworldCameraIsDrawnBetweenUpdatesAndSnapsWhenTheSceneIsCovered()
    {
        using var h = new Harness("route");
        h.Step(30);
        var world = (OverworldScene)h.Game.Scenes.Current!;
        h.Game.Input.SetForced("up", true);
        Vector2 before = default, after = default;
        for (int i = 0; i < 120 && before == after; i++)
        {
            before = world.CameraDrawPosition(1);
            h.Step();
            after = world.CameraDrawPosition(1);
        }
        h.Game.Input.SetForced("up", false);
        Assert.AreNotEqual(before, after, "walking moves the camera");
        Assert.AreEqual(before, world.CameraDrawPosition(0), "blend 0 draws the previous update's view");
        Assert.AreEqual(Vector2.Lerp(before, after, 0.5f), world.CameraDrawPosition(0.5f));

        for (int i = 0; i < 60 && world.Player.Moving; i++) h.Step();
        h.Press("pause", 10);
        Assert.IsInstanceOfType<StartMenuScene>(h.Game.Scenes.Current);
        Assert.AreEqual(world.CameraDrawPosition(1), world.CameraDrawPosition(0), "a covered scene is drawn where it stopped");
    }
}
