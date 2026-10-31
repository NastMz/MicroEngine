using MeadowRun.Gameplay;
using static MeadowRun.Tests.StageHarness;

namespace MeadowRun.Tests;

[TestClass]
public sealed class PlayerFeelTests
{
    private static readonly string[] Flat =
    [
        "..............................",
        "..S...........................",
        "##############################",
        "##############################",
    ];

    private static Stage Settled(params string[] rows)
    {
        var stage = Stage(rows.Length > 0 ? rows : Flat);
        Run(stage, 10, default);
        Assert.IsTrue(stage.Player.Grounded, "Pip should start on the ground.");
        return stage;
    }

    [TestMethod]
    public void HoldingJumpJumpsHigherThanTapping()
    {
        float tap = JumpPeak(Settled(), holdSteps: 1);
        float full = JumpPeak(Settled(), holdSteps: 60);
        Assert.IsTrue(full > tap * 2, $"full {full} vs tap {tap}");
        Assert.IsTrue(full is > 60 and < 90, $"full jump {full}px should be about 4-5 tiles");
        Assert.IsTrue(tap is > 12 and < 40, $"tap jump {tap}px should be one to two tiles");
    }

    [TestMethod]
    public void RunningJumpIsHigherThanStandingJump()
    {
        float standing = JumpPeak(Settled(), 60);
        var running = Settled();
        Run(running, 60, Input(right: true, run: true));
        float run = JumpPeak(running, 60, right: true);
        Assert.IsTrue(run > standing, $"run {run} vs standing {standing}");
    }

    [TestMethod]
    public void RunModifierRaisesTopSpeed()
    {
        var walk = Settled(); Run(walk, 90, Input(right: true));
        var run = Settled(); Run(run, 90, Input(right: true, run: true));
        Assert.AreEqual(Tuning.WalkSpeed, walk.Player.Velocity.X, 0.01f);
        Assert.AreEqual(Tuning.RunSpeed, run.Player.Velocity.X, 0.01f);
    }

    [TestMethod]
    public void ReversingAtSpeedSkidsBeforeTurning()
    {
        var stage = Settled();
        Run(stage, 60, Input(right: true, run: true));
        stage.Update(Dt, Input(left: true));
        Assert.IsTrue(stage.Player.Skidding);
        Assert.IsTrue(stage.Player.Velocity.X > 0, "momentum carries through the skid");
        Assert.AreEqual(1, stage.Player.Facing, "facing does not flip mid-skid");
    }

    [TestMethod]
    public void TerminalVelocityCapsFalls()
    {
        var stage = Stage("..S...", "......", "......", "......", "......", "......", "......", "......", "......", "......", "......", "......", "......", "######");
        float max = 0;
        for (int i = 0; i < 60; i++) { stage.Update(Dt, default); max = MathF.Max(max, stage.Player.Velocity.Y); }
        Assert.AreEqual(Tuning.TerminalVelocity, max, 0.01f);
    }

    private static readonly string[] Ledge =
    [
        "....................",
        "..S.................",
        "#####...............",
        "#####...............",
    ];

    [TestMethod]
    public void CoyoteTimeAllowsJumpJustAfterLeavingLedge()
    {
        var stage = Settled(Ledge);
        while (stage.Player.Grounded) stage.Update(Dt, Input(right: true));
        Run(stage, 3, Input(right: true)); // 3 steps (0.05 s) after leaving
        stage.Update(Dt, Input(right: true, jumpPressed: true, jumpHeld: true));
        Assert.IsTrue(stage.Player.Velocity.Y < -200, "jump should still fire inside the coyote window");
    }

    [TestMethod]
    public void NoJumpLongAfterLeavingLedge()
    {
        var stage = Settled(Ledge);
        while (stage.Player.Grounded) stage.Update(Dt, Input(right: true));
        Run(stage, 12, Input(right: true)); // 0.2 s: past coyote time
        float vy = stage.Player.Velocity.Y;
        stage.Update(Dt, Input(right: true, jumpPressed: true, jumpHeld: true));
        Assert.IsTrue(stage.Player.Velocity.Y >= vy, "no mid-air jump after coyote time");
    }

    [TestMethod]
    public void JumpPressedJustBeforeLandingIsBuffered()
    {
        var stage = Settled();
        stage.Update(Dt, Input(jumpPressed: true, jumpHeld: true));
        while (stage.Player.Velocity.Y < 0 || !stage.Player.Grounded)
        {
            // Press again a few frames before touching down.
            bool nearGround = stage.Player.Velocity.Y > 0 && stage.Player.Feet.Y > stage.Level.PixelHeight - 32 - 10;
            stage.Update(Dt, Input(jumpPressed: nearGround && !_pressed, jumpHeld: false));
            if (nearGround) _pressed = true;
            if (stage.Player.Grounded) break;
        }
        stage.Update(Dt, default);
        Assert.IsTrue(stage.Player.Velocity.Y < -200, "buffered press should jump on landing");
    }

    private bool _pressed;

    [TestMethod]
    public void OneWayPlatformsLetPipUpThroughAndHoldHimOnTop()
    {
        var stage = Settled(
            "..........",
            "..........",
            "..----....",
            "..........",
            "..S.......",
            "##########",
            "##########");
        float platformTop = (stage.Level.Height - 5) * 16;
        JumpPeak(stage, 60, 90);
        Run(stage, 30, default);
        Assert.AreEqual(platformTop, stage.Player.Feet.Y, 0.01f, "Pip passes up through and lands on the plank");
        // Down + jump drops back through.
        stage.Update(Dt, Input(down: true, jumpPressed: true, jumpHeld: true));
        Run(stage, 40, default);
        Assert.IsTrue(stage.Player.Feet.Y > platformTop + 16);
    }

    [TestMethod]
    public void CornerCorrectionSlidesPastCeilingEdges()
    {
        // A block whose left edge overlaps Pip's head by two pixels.
        var stage = Settled(
            "..........",
            "..........",
            "...#######",
            "..........",
            "..........",
            "..S.......",
            "##########",
            "##########");
        stage.Player.Position = stage.Player.Position with { X = 48 - Player.Width + 2 };
        JumpPeak(stage, 60, 40);
        Assert.IsTrue(stage.Player.Bounds.Right <= 48, "Pip is nudged left of the block instead of bonking");
        Assert.IsTrue(stage.Player.Feet.Y < (stage.Level.Height - 5) * 16, "and keeps rising past its bottom");
    }
}
