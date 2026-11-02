using System.Numerics;
using MeadowRun.Gameplay;
using MeadowRun.Gameplay.Actors;
using MeadowRun.Scenes;
using static MeadowRun.Tests.StageHarness;

namespace MeadowRun.Tests;

/// <summary>Frames drawn between updates place moving things between their previous and current positions.</summary>
[TestClass]
public sealed class MotionTests
{
    private static Stage Running()
    {
        var stage = Stage(
            "..................................................",
            "..S.......w.......................................",
            "##################################################",
            "##################################################");
        Run(stage, 40, Input(right: true, run: true));
        return stage;
    }

    private static void AssertBetween(Vector2 from, Vector2 to, Vector2 drawn, string what)
    {
        Assert.AreNotEqual(from, to, $"{what} should be moving");
        var expected = Vector2.Lerp(from, to, 0.5f);
        Assert.AreEqual(expected.X, drawn.X, 1e-4f, what);
        Assert.AreEqual(expected.Y, drawn.Y, 1e-4f, what);
    }

    [TestMethod]
    public void MovingThingsAreDrawnBetweenTheirLastTwoUpdates()
    {
        var stage = Running();
        var motion = new StageMotion();
        var walker = stage.Actors.OfType<Enemy>().Single();
        motion.Remember(stage);
        var pipFrom = stage.Player.Position;
        var cameraFrom = stage.Camera.Position;
        var walkerFrom = walker.Position;
        stage.Update(Dt, Input(right: true, run: true));
        var pipNow = stage.Player.Position;
        var cameraNow = stage.Camera.Position;
        var walkerNow = walker.Position;

        motion.Place(stage, 0.5f);
        AssertBetween(pipFrom, pipNow, stage.Player.Position, "Pip");
        AssertBetween(cameraFrom, cameraNow, stage.Camera.Position, "camera");
        AssertBetween(walkerFrom, walkerNow, walker.Position, "walker");
        motion.Restore(stage);

        Assert.AreEqual(pipNow, stage.Player.Position, "the rules get Pip back where they left him");
        Assert.AreEqual(cameraNow, stage.Camera.Position);
        Assert.AreEqual(walkerNow, walker.Position);
    }

    [TestMethod]
    public void BlendOneDrawsExactlyTheCurrentUpdate()
    {
        var stage = Running();
        var motion = new StageMotion();
        motion.Remember(stage);
        stage.Update(Dt, Input(right: true, run: true));
        var pipNow = stage.Player.Position;
        var cameraNow = stage.Camera.Position;

        motion.Place(stage, 1);
        Assert.AreEqual(pipNow, stage.Player.Position);
        Assert.AreEqual(cameraNow, stage.Camera.Position);
        motion.Restore(stage);
        Assert.AreEqual(pipNow, stage.Player.Position);
    }

    [TestMethod]
    public void JumpsAreDrawnAtOnceInsteadOfStreaking()
    {
        var stage = Running();
        var motion = new StageMotion();
        var walker = stage.Actors.OfType<Enemy>().Single();
        motion.Remember(stage);
        stage.Player.Position += new Vector2(200, 0);
        walker.Position += new Vector2(0, -120);
        var pipNow = stage.Player.Position;
        var walkerNow = walker.Position;

        motion.Place(stage, 0.25f);
        Assert.AreEqual(pipNow, stage.Player.Position, "a teleported Pip snaps");
        Assert.AreEqual(walkerNow, walker.Position, "a respawned actor snaps");
        motion.Restore(stage);
    }

    [TestMethod]
    public void ActorsSpawnedDuringAnUpdateStartWhereTheyAppeared()
    {
        var stage = Running();
        var shot = new SeedShot(stage, stage.Player.Center, 1);
        Assert.AreEqual(shot.Position, shot.PreviousPosition);
    }

    [TestMethod]
    public void ScenesBlendOnlyRightAfterTheyStepped()
    {
        var step = new StepBlend();
        Assert.AreEqual(1f, step.Blend(0, 0.3f), "never stepped: draw the current state");
        step.Stepped(7, Dt);
        Assert.AreEqual(0.3f, step.Blend(7, 0.3f));
        Assert.AreEqual(1f, step.Blend(8, 0.3f), "paused or covered: draw the last state");
        Assert.AreEqual(2.5f, step.Time(2.5f, 1), "blend 1 keeps the scene clock exact");
        Assert.AreEqual(2.5f - Dt / 2, step.Time(2.5f, 0.5f), 1e-6f);
    }
}
