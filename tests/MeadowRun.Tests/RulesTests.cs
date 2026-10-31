using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MeadowRun.Gameplay;
using MeadowRun.Gameplay.Actors;
using static MeadowRun.Tests.StageHarness;

namespace MeadowRun.Tests;

[TestClass]
public sealed class RulesTests
{
    [TestMethod]
    public void HundredCoinsGrantALife()
    {
        var s = new Session();
        int gained = 0;
        s.LifeGained += () => gained++;
        for (int i = 0; i < 100; i++) s.AddCoin();
        Assert.AreEqual(Session.StartingLives + 1, s.Lives);
        Assert.AreEqual(0, s.Coins);
        Assert.AreEqual(1, gained);
        Assert.AreEqual(100 * Session.CoinScore, s.Score);
    }

    [TestMethod]
    public void StompChainDoublesThenAwardsLives()
    {
        var s = new Session();
        Assert.AreEqual("100", s.RewardStomp(0));
        Assert.AreEqual("200", s.RewardStomp(1));
        Assert.AreEqual("8000", s.RewardStomp(7));
        Assert.AreEqual("1UP", s.RewardStomp(8));
        Assert.AreEqual(Session.StartingLives + 1, s.Lives);
    }

    [TestMethod]
    public void LosingLastLifeEndsTheRunAndContinueRefills()
    {
        var s = new Session { Form = PipForm.Ember };
        for (int i = 0; i < Session.StartingLives - 1; i++) Assert.IsFalse(s.LoseLife());
        Assert.AreEqual(PipForm.Sprout, s.Form, "dying resets the form");
        Assert.IsTrue(s.LoseLife());
        s.Continue();
        Assert.AreEqual(Session.StartingLives, s.Lives);
        Assert.AreEqual(0, s.Score);
    }

    [TestMethod]
    public void FlagPointsRiseWithGrabHeight()
    {
        Assert.AreEqual(100, Session.FlagPoints(0));
        Assert.AreEqual(800, Session.FlagPoints(0.6f));
        Assert.AreEqual(5000, Session.FlagPoints(1));
    }

    private static Stage With(char enemy)
    {
        var stage = Stage(
            "......................",
            $"..S.......{enemy}...........",
            "######################",
            "######################");
        Run(stage, 5, default);
        return stage;
    }

    private static void DropOnto(Stage stage, Enemy enemy)
    {
        stage.Player.Position = new Vector2(enemy.Bounds.X, enemy.Bounds.Top - Player.Height - 4);
        stage.Player.Velocity = new Vector2(0, 150);
        Run(stage, 3, default);
    }

    [TestMethod]
    public void LandingOnAWalkerStompsItAndBounces()
    {
        var stage = With('w');
        var walker = stage.Actors.OfType<Bumblet>().Single();
        int stomps = 0;
        stage.StompSink = () => stomps++;
        DropOnto(stage, walker);
        Assert.IsTrue(walker.Dead);
        Assert.AreEqual(1, stomps, "each stomp is reported once for achievements");
        Assert.IsTrue(stage.Player.Velocity.Y < 0, "Pip bounces off");
        Assert.AreEqual(100, stage.Session.Score);
        Assert.AreEqual(StagePhase.Playing, stage.Phase);
    }

    [TestMethod]
    public void WalkingIntoAWalkerHurts()
    {
        var stage = With('w');
        Run(stage, 200, Input(right: true));
        Assert.AreNotEqual(StagePhase.Playing, stage.Phase, "a small Pip dies from a side hit");
    }

    [TestMethod]
    public void ThornbacksCannotBeStomped()
    {
        var stage = With('t');
        var thorn = stage.Actors.OfType<Thornback>().Single();
        int stomps = 0;
        stage.StompSink = () => stomps++;
        DropOnto(stage, thorn);
        Assert.AreEqual(0, stomps);
        Assert.IsFalse(thorn.Dead);
        Assert.AreEqual(StagePhase.Dying, stage.Phase);
    }

    [TestMethod]
    public void PoweredPipShrinksInsteadOfDying()
    {
        var stage = With('w');
        stage.Player.Form = PipForm.Ember;
        stage.Hurt();
        Assert.AreEqual(PipForm.Bloom, stage.Player.Form);
        Assert.IsTrue(stage.Player.Invulnerable > 0);
        stage.Hurt();
        Assert.AreEqual(PipForm.Bloom, stage.Player.Form, "invulnerability frames ignore further hits");
        Assert.AreEqual(StagePhase.Playing, stage.Phase);
    }

    [TestMethod]
    public void BumpingBlocksGivesCoinsPowerUpsAndBreaksBricksWhenBloomed()
    {
        var stage = Stage("..?PB.....", "..........", "..........", "..S.......", "##########", "##########");
        int y = stage.Level.Height - 6;
        stage.BumpBlock(2, y);
        Assert.AreEqual(1, stage.Session.Coins);
        Assert.AreEqual(Tile.UsedBlock, stage.Level.Tiles[2, y]);
        stage.BumpBlock(3, y);
        Assert.AreEqual(PowerKind.Sunpetal, stage.Actors.OfType<PowerItem>().Single().Kind);
        stage.BumpBlock(4, y);
        Assert.AreEqual(Tile.Brick, stage.Level.Tiles[4, y], "a small Pip only bumps bricks");
        stage.Player.Form = PipForm.Bloom;
        stage.BumpBlock(4, y);
        Assert.AreEqual(Tile.Empty, stage.Level.Tiles[4, y]);
    }

    [TestMethod]
    public void JumpingIntoABlockFromBelowBumpsIt()
    {
        var stage = Stage("..?.......", "..........", "..........", "..S.......", "##########", "##########");
        Run(stage, 5, default);
        JumpPeak(stage, 60, 60);
        Assert.AreEqual(1, stage.Session.Coins);
    }

    [TestMethod]
    public void FallingIntoAPitCostsTheAttempt()
    {
        var stage = Stage("..........", "..S.......", "###...####", "###...####");
        Run(stage, 400, Input(right: true));
        Assert.AreEqual(StageOutcome.Died, stage.Outcome);
    }

    [TestMethod]
    public void TimeRunningOutKillsPip()
    {
        var stage = Stage("..........", "..S.......", "##########", "##########");
        Run(stage, (int)(300 * Gameplay.Stage.TimeUnitSeconds * 60) + 5, default);
        Assert.AreEqual(StagePhase.Dying, stage.Phase);
    }

    [TestMethod]
    public void EmberPipThrowsAtMostTwoSeeds()
    {
        var stage = Stage("..........................", "..S.......................", "##########################", "##########################");
        stage.Player.Form = PipForm.Ember;
        Run(stage, 5, default);
        for (int i = 0; i < 6; i++) { stage.Update(Dt, Input(run: true, runPressed: true)); stage.Update(Dt, default); }
        Assert.AreEqual(2, stage.Actors.OfType<SeedShot>().Count());
    }

    [TestMethod]
    public void SpringLaunchesHigherWhenJumpIsHeld()
    {
        float Launch(bool held)
        {
            var stage = Stage("..........", "..S.j.....", "##########", "##########");
            Run(stage, 5, default);
            // Drop Pip onto the spring from a little above it.
            stage.Player.Position = new Vector2(4 * 16 + 2, stage.Player.Position.Y - 24);
            float best = stage.Player.Feet.Y;
            for (int i = 0; i < 150; i++)
            {
                stage.Update(Dt, Input(jumpHeld: held));
                best = MathF.Min(best, stage.Player.Feet.Y);
            }
            return stage.Level.PixelHeight - 32 - best;
        }
        float low = Launch(false), high = Launch(true);
        Assert.IsTrue(low > 40, $"spring launches ({low})");
        Assert.IsTrue(high > low + 15, $"held {high} vs {low}");
    }
}
