using Examples;

namespace Samples.Tests;

/// <summary>The nearby objects example (large worlds guide): a spatial index decides what is moved, lit and drawn.</summary>
[TestClass]
public sealed class NearbyExampleTests
{
    [TestMethod]
    public void OnlyTheFirefliesNearTheViewAreMovedAndTheOnesNearTheLanternAreFound()
    {
        var (game, platform) = SettingsExampleTests.Start(new NearbyExample());
        var scene = (NearbyExample)game.Scenes.Current!;
        Assert.AreEqual(3000, scene.Total);
        // The view and its margin are 448×308 of a 1600×900 meadow: a tenth of it.
        Assert.IsTrue(scene.Awake is > 150 and < 500, $"about three hundred fireflies are near the view: {scene.Awake}");
        Assert.IsTrue(scene.Near is > 0 and < 40, $"about ten are within the lantern's 40 pixels: {scene.Near}");

        var start = scene.Lantern;
        game.Input.SetForced("right", true);
        for (int i = 0; i < 120; i++) SettingsExampleTests.Frame(game, platform);
        game.Input.SetForced("right", false);
        Assert.IsTrue(scene.Lantern.X > start.X + 300, "two seconds of RIGHT carry the lantern 320 pixels");
        Assert.IsTrue(scene.Awake is > 150 and < 500, $"the region moved with the view and is still a tenth of the meadow: {scene.Awake}");
        Assert.AreEqual(3000, scene.Total);

        SettingsExampleTests.Press(game, platform, "pause");
        Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current, "pause goes back to the list");
    }
}
