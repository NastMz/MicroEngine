using Examples;
using MicroEngine.Core;
using MicroEngine.Scenes;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Samples.Tests;

/// <summary>
/// The focused examples, played headlessly through their public API: what the documentation says they do is what they
/// do. The tutorial's game has its own file, GemGardenTests.cs, which the testing guide quotes whole.
/// </summary>
[TestClass]
public sealed class ExampleTests
{
    [TestMethod]
    public void EveryExampleOpensRunsAndReturnsToTheList()
    {
        var saves = Directory.CreateTempSubdirectory("examples-").FullName;
        var game = new ExamplesGame(saves);
        game.InitializeForTest(new HeadlessPlatform());
        foreach (var (name, _) in ExamplesGame.All)
        {
            game.Input.SetForced("confirm", true);
            game.StepForTest();
            game.StepForTest();
            game.Input.SetForced("confirm", false);
            for (int i = 0; i < 90; i++) game.StepForTest();
            Assert.IsNotInstanceOfType<MenuScene>(game.Scenes.Current, name);
            game.Input.SetForced("pause", true);
            game.StepForTest();
            game.Input.SetForced("pause", false);
            game.StepForTest();
            Assert.IsInstanceOfType<MenuScene>(game.Scenes.Current, $"{name}: pause goes back to the list");
            game.Input.SetForced("down", true);
            game.StepForTest();
            game.Input.SetForced("down", false);
            game.StepForTest();
        }
        Directory.Delete(saves, true);
    }

    [TestMethod]
    public void TheSavingExampleWritesAndReadsItsSlot()
    {
        var saves = Directory.CreateTempSubdirectory("examples-").FullName;
        var game = new ExamplesGame(saves);
        game.InitializeForTest(new HeadlessPlatform());
        game.Scenes.Push(new SaveExample(saves));
        game.StepForTest();
        foreach (var action in new[] { "right", "right", "confirm" })
        {
            game.Input.SetForced(action, true);
            game.StepForTest();
            game.Input.SetForced(action, false);
            game.StepForTest();
        }
        var saved = new SaveStore("MicroEngineExamples", saves).Load<SaveExample.Progress>("example");
        Assert.IsNotNull(saved);
        Assert.AreEqual(2, saved.Coins);
        Assert.HasCount(1, saved.Visited);
        Directory.Delete(saves, true);
    }
}

[TestClass]
public sealed class ExtendingTests
{
    [TestMethod]
    public void AGameSavesThroughACustomStorage()
    {
        var storage = new MemorySaveStorage();
        var store = new SaveStore(storage);
        Assert.IsTrue(store.Save("slot", new SaveExample.Progress { Coins = 3 }));
        Assert.AreEqual(3, store.Load<SaveExample.Progress>("slot")!.Coins);
        storage.Write("broken", "{ not json");
        Assert.IsNull(store.Load<SaveExample.Progress>("broken"), "unreadable data loads as null");
        Assert.IsTrue(storage.Exists("broken.corrupt"), "and is set aside, not lost");
    }

    [TestMethod]
    [DoNotParallelize] // redirects the console
    public void AWrappedProfileJoinsTheCatalogAndRunsTheGame()
    {
        var profile = ReportingPlatform.Wrap(HeadlessPlatform.Profile);
        var catalog = new PlatformCatalog().Add(HeadlessPlatform.Profile).Add(profile);
        Assert.AreEqual("headless-reported", profile.Name);
        var output = new StringWriter();
        var previous = Console.Out;
        Console.SetOut(output);
        int exit;
        try { exit = GameLauncher.Run(new ExamplesGame(), ["--platform", "headless-reported", "--frames", "30"], catalog); }
        finally { Console.SetOut(previous); }
        Assert.AreEqual(0, exit);
        StringAssert.Contains(output.ToString(), "started MicroEngine.Core.HeadlessPlatform (without sound)");
        StringAssert.Contains(output.ToString(), "done: 30 updates, 30 frames", "the original platform, its preset included");
    }
}

[TestClass]
public sealed class PlatformerExampleTests
{
    private static void Hold(Game game, string action, int steps)
    {
        game.Input.SetForced(action, true);
        for (int i = 0; i < steps; i++) game.StepForTest();
        game.Input.SetForced(action, false);
        game.StepForTest();
    }

    [TestMethod]
    public void JumpsUpThroughAOneWayPlatformLandsOnItAndDropsBack()
    {
        var game = new ExamplesGame(Path.GetTempPath());
        game.InitializeForTest(new HeadlessPlatform());
        var scene = new PlatformerExample();
        game.Scenes.Push(scene);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(160, scene.Player.Bottom, "lands on the solid floor");

        Hold(game, "right", 30);                     // under the one-way ledge (x 64..112, top 112)
        Hold(game, "confirm", 2);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(112, scene.Player.Bottom, "jumped up through the ledge and stands on it");

        game.Input.SetForced("down", true);
        Hold(game, "confirm", 2);
        game.Input.SetForced("down", false);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(160, scene.Player.Bottom, "down + jump drops through it to the floor");
    }
}
