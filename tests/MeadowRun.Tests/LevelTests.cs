using MeadowRun.Content;
using MeadowRun.Gameplay;
using MicroEngine.World;

namespace MeadowRun.Tests;

[TestClass]
public sealed class LevelTests
{
    public static IEnumerable<object[]> AllLevels => Levels.All.Select((_, i) => new object[] { i });

    [TestMethod]
    [DynamicData(nameof(AllLevels))]
    public void LevelIsWellFormed(int index)
    {
        var def = Levels.All[index];
        Assert.AreEqual(Levels.Rows, def.Rows.Length);
        Assert.IsTrue(def.Rows.All(r => r.Length == def.Rows[0].Length), "rows have equal width");
        foreach (var row in def.Rows)
            foreach (char c in row)
                Assert.IsTrue(c == '.' || Legend.Tiles.ContainsKey(c) || Legend.Entities.Contains(c), $"unknown level char {c}");
        var level = new Level(def);
        var s = level.Start;
        Assert.IsFalse(level.IsSolid(s.X, s.Y));
        Assert.IsTrue(level.IsSolid(s.X, s.Y + 1), "start stands on ground");
        Assert.IsTrue(level.Goal is not null ^ def.IsBoss, "exactly one way to finish: goal pole or boss");
        if (level.Goal is { } g)
        {
            Assert.IsTrue(level.IsSolid(g.X, g.Y + 1), "goal pole stands on ground");
            for (int y = g.Y - 8; y <= g.Y; y++) Assert.IsFalse(level.IsSolid(g.X, y), "goal pole is not buried");
        }
        // Closed on both sides; open only above (sky) and below (pits).
        Assert.AreEqual(TileFlags.Solid, level.Grid.FlagsAt(-1, 5));
        Assert.AreEqual(TileFlags.Solid, level.Grid.FlagsAt(level.Width, 5));
        Assert.AreEqual(TileFlags.None, level.Grid.FlagsAt(5, level.Height));
    }

    [TestMethod]
    public void CampaignHasFourBiomesAndEndsWithTheBoss()
    {
        Assert.IsTrue(Levels.All.Count >= 4);
        Assert.AreEqual(4, Levels.All.Select(l => l.Biome).Distinct().Count());
        Assert.IsTrue(Levels.All[^1].IsBoss);
    }

    [TestMethod]
    public void AutopilotBeatsTheFirstLevelUnderFullRules() => AssertCompletable(0, ignoreEnemies: false);

    /// <summary>
    /// Every platforming level is traversable: pits, lava, spikes, fire bars and platform timing all
    /// apply, but enemy contact is ignored (dodging enemies is player skill, not level geometry).
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AllLevels))]
    public void EveryPlatformingLevelIsTraversable(int index)
    {
        if (!Levels.All[index].IsBoss) AssertCompletable(index, ignoreEnemies: true);
    }

    private static void AssertCompletable(int index, bool ignoreEnemies)
    {
        var def = Levels.All[index];
        // Like a player: five lives, respawning at the last checkpoint, adjusting timing after each death.
        var session = new Session();
        int checkpoint = 0;
        Stage stage = null!;
        for (int attempt = 0; attempt < Session.StartingLives; attempt++)
        {
            stage = StageHarness.New(new Level(def), session, checkpoint);
            var bot = new Autopilot(attempt);
            for (int f = 0; f < 60 * 200 && stage.Phase != StagePhase.Done; f++)
            {
                if (ignoreEnemies) stage.IgnoreEnemiesForTesting = true;
                StageHarness.Step(stage, bot.Next(stage));
            }
            if (stage.Outcome == StageOutcome.Cleared) break;
            checkpoint = stage.CheckpointReached;
        }
        Assert.AreEqual(StageOutcome.Cleared, stage.Outcome,
            $"bot stopped at x={stage.Player.Position.X:0} (tile {stage.Player.Position.X / 16:0}) y={stage.Player.Position.Y:0}, phase {stage.Phase}, cause {stage.DeathCause}");
    }
}
