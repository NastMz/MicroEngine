using System.Numerics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Scenes;
using TrailDuel.Visuals;
using TrailDuel.World;

namespace TrailDuel.Tests;

/// <summary>
/// Moving things are drawn between the start and the end of their scene's latest step (the scene's objects blend, set
/// by the engine before drawing; 1 in these fixed-step runs), snap on jumps, and draw exactly the current state at a
/// blend of 1 or when their scene did not step. The tests step a world themselves, or set the blend of a scene's world.
/// </summary>
[TestClass]
public sealed class DrawBetweenUpdatesTests
{
    private const float Dt = 1f / 60;

    [TestMethod]
    public void AWalkingActorIsDrawnBetweenWhereItStartedAndEndedTheStep()
    {
        var world = new SceneWorld();
        var actor = new Actor(CharacterLook.Player, 1, 1, Direction.Right, world);
        world.BeginStep(1, Dt);
        world.EndStep();
        world.BeginStep(2, Dt);
        actor.StartStep(Direction.Right, Actor.StepTime);
        actor.Update(Actor.StepTime / 2);
        world.EndStep();

        Assert.AreEqual(new Vector2(24, 16), actor.PixelPosition, "half way to the next tile");
        world.BeginDraw(2, 0);
        Assert.AreEqual(new Vector2(16, 16), actor.DrawPosition, "blend 0 draws the start of the step");
        world.BeginDraw(2, 0.5f);
        Assert.AreEqual(new Vector2(20, 16), actor.DrawPosition, "blend 0.5 draws half way through the step");
        world.BeginDraw(2, 1);
        Assert.AreEqual(actor.PixelPosition, actor.DrawPosition, "blend 1 draws exactly the end of the step");
        world.BeginDraw(3, 0.5f);
        Assert.AreEqual(actor.PixelPosition, actor.DrawPosition, "a frame of a step the world missed draws it where it is");
    }

    [TestMethod]
    public void AHoppingActorsLiftIsDrawnBetweenSteps()
    {
        var world = new SceneWorld();
        var actor = new Actor(CharacterLook.Player, 1, 1, Direction.Down, world);
        world.BeginStep(1, Dt);
        world.EndStep();
        world.BeginStep(2, Dt);
        actor.StartHop(Direction.Down);
        actor.Update(Actor.HopTime / 2);
        world.EndStep();

        world.BeginDraw(2, 0.5f);
        Assert.AreEqual(actor.Lift / 2, actor.DrawPosition.Y - actor.DrawSpritePosition.Y, 1e-4f, "the sprite rises half the lift");
        world.BeginDraw(2, 1);
        Assert.AreEqual(actor.PixelPosition - new Vector2(0, actor.Lift), actor.DrawSpritePosition);
        Assert.AreEqual(actor.PixelPosition, actor.DrawPosition, "the shadow stays on the ground");
    }

    [TestMethod]
    public void ATeleportedActorSnapsInsteadOfStreaking()
    {
        var world = new SceneWorld();
        var actor = new Actor(CharacterLook.Rival, 1, 1, Direction.Down, world);
        world.BeginStep(1, Dt);
        world.EndStep();
        world.BeginStep(2, Dt);
        actor.Teleport(9, 4);
        world.EndStep();
        world.BeginDraw(2, 0.5f);
        Assert.AreEqual(new Vector2(144, 64), actor.DrawPosition, "drawn at the new tile at once");
        world.BeginStep(3, Dt);
        world.EndStep();
        world.BeginDraw(3, 0.25f);
        Assert.AreEqual(new Vector2(144, 64), actor.DrawPosition, "the next step starts from the new tile");
    }

    [TestMethod]
    public void AnActorLeavingTheMapLeavesTheScenesObjects()
    {
        var world = new SceneWorld();
        var actor = new Actor(CharacterLook.Player, 1, 1, Direction.Down, world);
        Assert.AreEqual(2, world.Count, "the actor and its sprite");
        actor.Destroy();
        Assert.AreEqual(0, world.Count);
    }

    [TestMethod]
    public void TheOverworldCameraIsDrawnBetweenStepsAndStillWhenTheSceneIsCovered()
    {
        using var h = new Harness("route");
        h.Step(30);
        var scene = (OverworldScene)h.Game.Scenes.Current!;
        var objects = (SceneWorld)scene.Objects;
        h.Game.Input.SetForced("up", true);
        Vector2 before = default, after = default;
        for (int i = 0; i < 120 && before == after; i++)
        {
            objects.BeginDraw(h.Game.UpdateCount, 1);
            before = scene.CameraDrawPosition;
            h.Step();
            objects.BeginDraw(h.Game.UpdateCount, 1);
            after = scene.CameraDrawPosition;
        }
        h.Game.Input.SetForced("up", false);
        Assert.AreNotEqual(before, after, "walking moves the camera");
        objects.BeginDraw(h.Game.UpdateCount, 0);
        Assert.AreEqual(before, scene.CameraDrawPosition, "blend 0 draws the start of the step");
        objects.BeginDraw(h.Game.UpdateCount, 0.5f);
        Assert.AreEqual(Vector2.Lerp(before, after, 0.5f), scene.CameraDrawPosition);

        for (int i = 0; i < 60 && scene.Player.Moving; i++) h.Step();
        h.Press("pause", 10);
        Assert.IsInstanceOfType<StartMenuScene>(h.Game.Scenes.Current);
        objects.BeginDraw(h.Game.UpdateCount, 1);
        var still = scene.CameraDrawPosition;
        objects.BeginDraw(h.Game.UpdateCount, 0);
        Assert.AreEqual(still, scene.CameraDrawPosition, "a covered scene is drawn where it stopped");
    }

    [TestMethod]
    public void TheBattlePlatformsSlideInBetweenSteps()
    {
        using var h = new Harness("battle");
        h.Step(10);
        var battle = (BattleScene)h.Game.Scenes.Current!;
        var objects = (SceneWorld)battle.Objects;
        objects.BeginDraw(h.Game.UpdateCount - 1, 1);
        var before = battle.EnemyPlatformDrawPosition;
        h.Step();
        objects.BeginDraw(h.Game.UpdateCount, 1);
        var after = battle.EnemyPlatformDrawPosition;
        Assert.AreNotEqual(before, after, "the platform slides in during the intro");
        objects.BeginDraw(h.Game.UpdateCount, 0.5f);
        Assert.AreEqual(Vector2.Lerp(before, after, 0.5f), battle.EnemyPlatformDrawPosition);
    }

    [TestMethod]
    public void LeavingAMapDestroysItsActors()
    {
        using var h = new Harness("route");
        h.Step(5);
        var scene = (OverworldScene)h.Game.Scenes.Current!;
        int onRoute = scene.Objects.Count;
        Assert.AreEqual(1 + 2 * (scene.Map.Npcs.Count + 1), onRoute, "the camera and each actor with its sprite");
        // South of the start is the warp into Sprout Town.
        for (int i = 0; i < 600 && scene.Map.Def.Id == "route1"; i++)
        {
            h.Game.Input.SetForced("down", true);
            h.Step();
        }
        h.Game.Input.SetForced("down", false);
        h.Step(60);
        Assert.AreNotEqual("route1", scene.Map.Def.Id, "walked off the route");
        Assert.AreEqual(1 + 2 * (scene.Map.Npcs.Count + 1), scene.Objects.Count, "only the new map's actors remain");
    }
}
