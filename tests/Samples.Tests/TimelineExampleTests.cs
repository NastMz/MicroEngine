using Examples;

namespace Samples.Tests;

/// <summary>The timeline example: one press plays the strike, its event counts one hit, and the figure returns to rest.</summary>
[TestClass]
public sealed class TimelineExampleTests
{
    [TestMethod]
    public void ConfirmPlaysTheStrikeOnceAndItsEventCountsTheHit()
    {
        var (game, platform) = SettingsExampleTests.Start(new TimelineExample());
        var scene = (TimelineExample)game.Scenes.Current!;
        Assert.AreEqual("READY", scene.Pose);
        Assert.AreEqual(0, scene.Hits);
        Assert.AreEqual(0f, scene.Arm, "At rest before the first press.");

        SettingsExampleTests.Press(game, platform, "confirm");
        for (int i = 0; i < 12; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual("RAISE", scene.Pose);
        Assert.IsTrue(scene.Arm < -1f, $"The arm goes up first, at {scene.Arm}.");
        Assert.AreEqual(0, scene.Hits, "Nothing is hit while the arm is up.");

        SettingsExampleTests.Press(game, platform, "confirm");   // ignored while it plays
        for (int i = 0; i < 20; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(1, scene.Hits, "The event at the moment of the strike.");

        for (int i = 0; i < 40; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual("READY", scene.Pose);
        Assert.AreEqual(0f, scene.Arm, "Back at rest on the last key.");
        Assert.AreEqual(1, scene.Hits);

        SettingsExampleTests.Press(game, platform, "confirm");
        for (int i = 0; i < 60; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(2, scene.Hits, "A second press plays it again.");
    }

    [TestMethod]
    public void TheBreathKeepsLoopingUnderTheStrike()
    {
        var (game, platform) = SettingsExampleTests.Start(new TimelineExample());
        var scene = (TimelineExample)game.Scenes.Current!;
        float highest = 0, lowest = float.MaxValue;
        SettingsExampleTests.Press(game, platform, "confirm");
        for (int i = 0; i < 300; i++)
        {
            SettingsExampleTests.Frame(game, platform);
            highest = MathF.Max(highest, scene.Bob);
            lowest = MathF.Min(lowest, scene.Bob);
        }
        Assert.IsTrue(highest > 2.9f && lowest < 0.1f, $"The body moves between 0 and 3 for five seconds: {lowest}..{highest}.");
    }
}
