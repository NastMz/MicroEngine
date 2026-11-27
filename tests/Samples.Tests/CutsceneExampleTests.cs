using Examples;

namespace Samples.Tests;

/// <summary>
/// The cutscene example: the coroutine goes on in the update each tween ends, the show's rules run without a scene, and
/// the dancers are the children of the troupe.
/// </summary>
[TestClass]
public sealed class CutsceneExampleTests
{
    private const float Step = 1f / 60f;

    [TestMethod]
    public void TheShowsRulesRunWithoutASceneAndCountInSecondsAndInUpdates()
    {
        var show = new Show();
        show.Update(Step);
        Assert.AreEqual(Show.Act.Waiting, show.Current);
        show.Begin();
        var sparks = new List<int>();
        int bow = 0, over = 0;
        for (int update = 1; update <= 200; update++)
        {
            int before = show.Sparks;
            show.Update(Step);
            if (show.Sparks != before) sparks.Add(update);
            if (bow == 0 && show.Current == Show.Act.Bow) bow = update;
            if (over == 0 && show.Current == Show.Act.Over) over = update;
        }
        CollectionAssert.AreEqual(new[] { 20, 40, 60 }, sparks, "Three sparks, every 20 updates.");
        Assert.AreEqual(90, bow, "1.5 seconds at 60 updates a second.");
        Assert.AreEqual(135, over, "45 updates later.");
    }

    [TestMethod]
    public void TheCutsceneWaitsForEachTweenAndGoesOnInTheUpdateItEnds()
    {
        var (game, platform) = SettingsExampleTests.Start(new CutsceneExample());
        var scene = (CutsceneExample)game.Scenes.Current!;
        Assert.AreEqual(3, scene.Dancers.Count());

        // The curtain's tween lasts half a second; the update that applies its final value is the update the
        // coroutine goes on in, so the show begins there and not one update later.
        int frames = 0;
        while (scene.Curtain > 0f && frames < 60)
        {
            Assert.AreEqual(Show.Act.Waiting, scene.Show.Current, "The show waits for the curtain.");
            SettingsExampleTests.Frame(game, platform);
            frames++;
        }
        Assert.IsTrue(frames is >= 29 and <= 31, $"Half a second of updates: {frames}.");
        Assert.AreEqual(0f, scene.Curtain, "Open on the tween's last update.");
        Assert.AreEqual(Show.Act.Dance, scene.Show.Current, "And the show began in that same update.");

        // The first dancer is in the air half way through its quarter of a second, the others still on the floor.
        for (int i = 0; i < 7; i++) SettingsExampleTests.Frame(game, platform);
        var hops = scene.Dancers.Select(d => d.Hop).ToArray();
        Assert.IsTrue(hops[0] > 0.9f && hops[1] == 0 && hops[2] == 0, string.Join(" ", hops));

        for (int i = 0; i < 240; i++) SettingsExampleTests.Frame(game, platform);
        Assert.IsTrue(scene.Finished);
        Assert.AreEqual(1f, scene.Curtain, "Closed again.");
        Assert.AreEqual(3, scene.Show.Sparks);
        Assert.IsTrue(scene.Dancers.All(d => d.Hop == 0), "Every hop ended on the floor.");
    }
}
