using MicroEngine.Core;
using TrailDuel.Scenes;

namespace TrailDuel.Tests;

/// <summary>Presentation follows the loop's step: an animation lasts the same seconds at any fixed update rate.</summary>
[TestClass]
public sealed class UpdateRateTests
{
    /// <summary>Seconds from the start of a wild battle until its action menu opens, at <paramref name="rate"/> updates a second.</summary>
    private static double SecondsUntilTheBattleMenu(int rate)
    {
        string saves = Path.Combine(Path.GetTempPath(), "trailduel-rate-" + Guid.NewGuid().ToString("N"));
        try
        {
            var game = new TrailDuelGame(saves, "battle") { UpdateRate = rate };
            game.InitializeForTest(new HeadlessPlatform(), 3);
            game.StepForTest();
            var battle = Assert.IsInstanceOfType<BattleScene>(game.Scenes.Current);
            int steps = 1;
            for (; steps < rate * 30 && !battle.AwaitingChoice; steps++) game.StepForTest();
            Assert.IsTrue(battle.AwaitingChoice, "the action menu opens");
            return steps / (double)rate;
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }

    [TestMethod]
    public void TheBattleIntroLastsAsLongAtThirtyUpdatesASecondAsAtSixty()
    {
        double at60 = SecondsUntilTheBattleMenu(60), at30 = SecondsUntilTheBattleMenu(30);
        // Each of the intro's waits (slides, typing, pauses) ends on a whole step, so a few steps of 30 Hz may add up;
        // timing counted in 60 Hz frames would take more than two seconds longer.
        Assert.AreEqual(at60, at30, 5 / 30.0, $"60 Hz: {at60:0.000} s, 30 Hz: {at30:0.000} s");
    }
}
