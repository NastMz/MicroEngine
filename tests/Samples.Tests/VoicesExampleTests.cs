using Examples;

namespace Samples.Tests;

/// <summary>The voices example (audio manual): a looping sound that follows a moving source, on a bus of its own.</summary>
[TestClass]
public sealed class VoicesExampleTests
{
    [TestMethod]
    public void TheLoopStartsFollowsTheSourceAndItsBusHasAVolume()
    {
        var (game, platform) = SettingsExampleTests.Start(new VoicesExample());
        var scene = (VoicesExample)game.Scenes.Current!;
        CollectionAssert.Contains(platform.SilentAudio.Played, "hum", "the loop started when the scene was entered");
        Assert.IsTrue(game.Audio.HasBus("ambience"));
        float x = scene.SourceX;
        for (int i = 0; i < 10; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreNotEqual(x, scene.SourceX, "the source moves, and the voice is set from it every update");

        game.Input.SetForced("down", true);
        for (int i = 0; i < 30; i++) SettingsExampleTests.Frame(game, platform);
        game.Input.SetForced("down", false);
        Assert.AreEqual(0.5f, game.Audio.BusVolume("ambience"), 0.05f, "half a second of DOWN lowers the bus by half");

        SettingsExampleTests.Press(game, platform, "pause");
        Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current, "pause goes back to the list");
        game.Scenes.Push(new VoicesExample());
        SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(2, platform.SilentAudio.Played.Count(name => name == "hum"), "entering again starts the loop again; the sound and the bus are registered once");
    }
}
