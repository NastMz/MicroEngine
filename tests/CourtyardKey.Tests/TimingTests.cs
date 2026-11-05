using CourtyardKey.Scenes;

namespace CourtyardKey.Tests;

/// <summary>
/// Presentation sequences advance by the step the loop provides, so they last the same seconds at any update rate.
/// </summary>
[TestClass]
public sealed class TimingTests
{
    [TestMethod]
    [DataRow(30)]
    [DataRow(60)]
    [DataRow(120)]
    public void TheOpeningFadeLastsHalfASecondAtAnyUpdateRate(int rate)
    {
        using var h = new Harness("road", rate);
        int steps = 0;
        bool faded = false;
        while (steps < rate * 3)
        {
            h.Step();
            steps++;
            if (h.Game.Play is not { } play) continue;
            if (play.FadeAmount > 0) faded = true;
            else if (faded) break;
        }
        Assert.IsTrue(faded, "The play scene never faded in.");
        Assert.AreEqual(0.5, steps / (double)rate, 2.5 / rate, $"{steps} steps at {rate} Hz");
    }

    [TestMethod]
    [DataRow(30)]
    [DataRow(60)]
    [DataRow(120)]
    public void TheDeathSequenceLastsTheSameSecondsAtAnyUpdateRate(int rate)
    {
        using var h = new Harness("road", rate);
        h.Step(rate);
        h.Play.Hero.TakeDamage(99, h.Play.Hero.Position);
        int steps = 0;
        while (h.Game.Scenes.Current is not GameOverScene && steps < rate * 5)
        {
            h.Step();
            steps++;
        }
        Assert.IsInstanceOfType<GameOverScene>(h.Game.Scenes.Current, "The game over screen never showed.");
        Assert.AreEqual(1.8, steps / (double)rate, 2.5 / rate, $"{steps} steps at {rate} Hz");
    }
}
