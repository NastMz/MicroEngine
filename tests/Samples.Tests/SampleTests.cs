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

/// <summary>The save-versioning example (persistence guide): old saves upgrade, newer ones are never overwritten.</summary>
[TestClass]
public sealed class SaveVersionTests
{
    private static (MemorySaveStorage Storage, SaveStore Store) With(string? profileJson)
    {
        var storage = new MemorySaveStorage();
        if (profileJson is not null) storage.Write(ProfileSaves.Slot, profileJson);
        return (storage, new SaveStore(storage));
    }

    [TestMethod]
    public void ASaveFromBeforeVersionsUpgradesThroughEveryStepAndKeepsTheOriginal()
    {
        const string original = """{ "Coins": 7 }""";
        var (storage, store) = With(original);
        var profile = ProfileSaves.Load(store, out var check);
        Assert.AreEqual(SaveCheck.Upgraded, check);
        Assert.IsNotNull(profile);
        Assert.AreEqual(7, profile.Gold, "1 -> 2 renamed Coins to Gold");
        Assert.AreEqual(3, profile.Lives, "0 -> 1 added lives");
        Assert.AreEqual(ProfileSaves.CurrentVersion, profile.Version);
        Assert.AreEqual(7, store.Load<System.Text.Json.Nodes.JsonObject>("profile.v0")!["Coins"]!.GetValue<int>(), "the original is backed up");
        Assert.AreEqual(original, storage.Read(ProfileSaves.Slot), "loading alone never rewrites the slot");

        Assert.IsTrue(ProfileSaves.Save(store, profile));
        Assert.AreEqual(SaveCheck.Current, (ProfileSaves.Load(store, out check), check).check, "saved again, it is current");
    }

    [TestMethod]
    public void ASaveFromAnIntermediateVersionRunsOnlyTheRemainingSteps()
    {
        var (_, store) = With("""{ "Version": 1, "Coins": 5, "Lives": 1 }""");
        var profile = ProfileSaves.Load(store, out var check);
        Assert.AreEqual(SaveCheck.Upgraded, check);
        Assert.AreEqual(5, profile!.Gold);
        Assert.AreEqual(1, profile.Lives, "the 0 -> 1 step did not run again");
    }

    [TestMethod]
    public void ACurrentSaveLoadsWithoutABackup()
    {
        var (storage, store) = With($$"""{ "Version": {{ProfileSaves.CurrentVersion}}, "Gold": 9, "Lives": 2 }""");
        var profile = ProfileSaves.Load(store, out var check);
        Assert.AreEqual(SaveCheck.Current, check);
        Assert.AreEqual(9, profile!.Gold);
        Assert.IsFalse(storage.Exists($"profile.v{ProfileSaves.CurrentVersion}"));
    }

    [TestMethod]
    public void NoSaveIsMissing()
    {
        var (_, store) = With(null);
        Assert.IsNull(ProfileSaves.Load(store, out var check));
        Assert.AreEqual(SaveCheck.Missing, check);
    }

    [TestMethod]
    [DataRow("""{ "Version": 99, "Gold": 1 }""", DisplayName = "written by a newer build")]
    [DataRow("""{ "Version": "two", "Gold": 1 }""", DisplayName = "a version that is not a number")]
    [DataRow("""{ "Version": 2, "Gold": "lots" }""", DisplayName = "a field of the wrong type")]
    public void ASaveThisBuildCannotReadIsProtectedAndLeftAsItIs(string json)
    {
        var (storage, store) = With(json);
        Assert.IsNull(ProfileSaves.Load(store, out var check));
        Assert.AreEqual(SaveCheck.Protected, check);
        Assert.AreEqual(json, storage.Read(ProfileSaves.Slot));
    }

    [TestMethod]
    public void WithoutABackupAnOldSaveIsNotUpgraded()
    {
        var storage = new BackupRefusingStorage();
        storage.Write(ProfileSaves.Slot, """{ "Coins": 4 }""");
        Assert.IsNull(ProfileSaves.Load(new SaveStore(storage), out var check));
        Assert.AreEqual(SaveCheck.Protected, check);
    }

    /// <summary>Memory slots that refuse every write except the profile itself, as a full disk would refuse the backup.</summary>
    private sealed class BackupRefusingStorage : ISaveStorage
    {
        private readonly MemorySaveStorage _slots = new();
        public string Location => "memory";
        public string? Read(string slot) => _slots.Read(slot);
        public void Write(string slot, string text)
        {
            if (slot != ProfileSaves.Slot) throw new IOException("disk full");
            _slots.Write(slot, text);
        }
        public bool Exists(string slot) => _slots.Exists(slot);
        public void Delete(string slot) => _slots.Delete(slot);
        public void SetAside(string slot) => _slots.SetAside(slot);
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
    public void TheLevelPlacesItsFourCoinsAndKeepsThoseThePlayerDoesNotTouch()
    {
        var game = new ExamplesGame(Path.GetTempPath());
        game.InitializeForTest(new HeadlessPlatform());
        var scene = new PlatformerExample();
        game.Scenes.Push(scene);
        for (int i = 0; i < 60; i++) game.StepForTest();

        Assert.AreEqual(4, scene.CoinsLeft);
        Assert.AreEqual(0, scene.Collected);
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
