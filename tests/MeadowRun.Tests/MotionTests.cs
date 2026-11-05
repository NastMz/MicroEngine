using System.Numerics;
using MeadowRun.Gameplay;
using MeadowRun.Gameplay.Actors;
using static MeadowRun.Tests.StageHarness;

namespace MeadowRun.Tests;

/// <summary>
/// Frames drawn between updates place moving things between their previous and current positions: the stage keeps
/// its actors, Pip and the camera in its scene's world, which interpolates them; the rules never see drawn positions.
/// </summary>
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
        Assert.AreEqual(expected.X, drawn.X, 1e-3f, what);
        Assert.AreEqual(expected.Y, drawn.Y, 1e-3f, what);
    }

    [TestMethod]
    public void MovingThingsAreDrawnBetweenTheirLastTwoUpdates()
    {
        var stage = Running();
        var walker = stage.Actors.OfType<Enemy>().Single();
        var pipFrom = stage.Player.Position;
        var cameraFrom = stage.Camera.Position;
        var walkerFrom = walker.Position;
        Step(stage, Input(right: true, run: true));
        var pipNow = stage.Player.Position;
        var cameraNow = stage.Camera.Position;
        var walkerNow = walker.Position;

        Draw(stage, 0.5f);
        AssertBetween(pipFrom, pipNow, stage.PipDrawPosition, "Pip");
        AssertBetween(cameraFrom, cameraNow, stage.CameraDrawPosition, "camera");
        AssertBetween(walkerFrom, walkerNow, walker.DrawPosition, "walker");
        Assert.AreEqual(pipNow, stage.Player.Position, "drawing never moves what the rules see");
        Assert.AreEqual(cameraNow, stage.Camera.Position);
        Assert.AreEqual(walkerNow, walker.Position);
    }

    [TestMethod]
    public void BlendOneDrawsExactlyTheCurrentUpdate()
    {
        var stage = Running();
        var walker = stage.Actors.OfType<Enemy>().Single();
        Step(stage, Input(right: true, run: true));

        Draw(stage, 1);
        Assert.AreEqual(stage.Player.Position, stage.PipDrawPosition);
        Assert.AreEqual(stage.Camera.Position, stage.CameraDrawPosition);
        Assert.AreEqual(walker.Position, walker.DrawPosition);
    }

    [TestMethod]
    public void ActorsSpawnedDuringAnUpdateAreDrawnWhereTheyAre()
    {
        var stage = Running();
        var shot = new SeedShot(stage, stage.Player.Center, 1);
        stage.Spawn(shot);
        shot.Position += new Vector2(5, 0);
        Draw(stage, 0.25f);
        Assert.AreEqual(shot.Position, shot.DrawPosition, "a new actor has no previous position to blend from");
    }

    /// <summary>A falling platform that returns to its place is drawn there at once instead of streaking up the screen.</summary>
    [TestMethod]
    public void AReturningPlatformIsDrawnAtOnce()
    {
        var stage = Stage(
            "..................................................",
            "..S...............................................",
            "##########...v....................................",
            "##########........................................");
        var platform = stage.Platforms.OfType<FallingPlatform>().Single();
        var home = platform.Position;
        platform.StoodOn();
        bool returned = false;
        for (int i = 0; i < 60 * 6 && !returned; i++)
        {
            var before = platform.Position;
            Step(stage, default);
            returned = before != home && platform.Position == home;
        }
        Assert.IsTrue(returned, "the platform fell and came back");
        Draw(stage, 0.25f);
        Assert.AreEqual(home, platform.DrawPosition);
    }

    /// <summary>A hitstop freezes the rules through the world's clock for its length; popups and particles keep moving.</summary>
    [TestMethod]
    public void AHitstopFreezesTheRulesButNotTheEffects()
    {
        var stage = Stage(
            "..................................................",
            "..S...............................................",
            "##################################################",
            "##################################################");
        Run(stage, 20, Input(right: true, run: true));
        stage.Popup("100", stage.Player.Center, default);
        stage.Freeze(0.09f);
        Assert.IsTrue(stage.Frozen);
        float clock = stage.Clock;
        var pip = stage.Player.Position;
        Run(stage, 6, Input(right: true, run: true));
        Assert.AreEqual(clock, stage.Clock, "six frozen steps: the stage clock waits");
        Assert.AreEqual(pip, stage.Player.Position, "Pip waits");
        Assert.AreEqual(6 * Dt, stage.Popups[^1].Age, 1e-5f, "the popup kept rising");
        Assert.IsFalse(stage.Frozen, "0.09 s of freeze is six steps of 1/60 s");
        Step(stage, Input(right: true, run: true));
        Assert.AreEqual(clock + Dt, stage.Clock);
        Assert.AreNotEqual(pip, stage.Player.Position, "the rules run again");
    }
}
