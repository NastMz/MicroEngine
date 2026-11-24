using System.Text.Json.Nodes;
using CourtyardKey.Model;
using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Platform;
using MicroEngine.Testing;

namespace CourtyardKey.Tests;

/// <summary>
/// The adventure's save is versioned by the engine (<see cref="Progress.Schema"/>), the files earlier releases wrote
/// still load, and the game keeps its saves where its run says: the host's medium, or the one a test hands it.
/// </summary>
[TestClass]
public sealed class SaveFormatTests
{
    /// <summary>A save as the release before the engine's versions wrote it, property for property.</summary>
    private const string SavedByThePreviousRelease = """
        {
          "Version": 1,
          "MaxHealth": 10,
          "Health": 7,
          "Gems": 123,
          "SmallKeys": 2,
          "HasSword": true,
          "HasBoomerang": true,
          "HasBossKey": true,
          "HasCourtyardKey": false,
          "Deaths": 3,
          "PlaySeconds": 456.5,
          "Flags": [
            "chest:dungeon:0,2:0"
          ],
          "Visited": [
            "dungeon:1,2"
          ],
          "Entrance": {
            "Area": "dungeon",
            "RoomX": 1,
            "RoomY": 3,
            "TileX": 10,
            "TileY": 8.2,
            "Facing": 1
          }
        }
        """;

    /// <summary>The saves of one test: what a release left on the player's disk, handed to the run that starts over them.</summary>
    private readonly MemorySaveStorage _saves = new();

    private string? Saved => _saves.Read(SaveSlots.Slot);

    /// <summary>The game started over <see cref="_saves"/> and stepped once, as a launch finds them.</summary>
    private CourtyardKeyGame Launched(string? start = null)
    {
        var game = new CourtyardKeyGame(start);
        game.InitializeForTest(new HeadlessPlatform(), saves: _saves);
        game.StepForTest();
        return game;
    }

    private static void AssertTheAdventure(Progress? loaded)
    {
        Assert.IsNotNull(loaded);
        Assert.AreEqual((10, 7, 123, 2), (loaded.MaxHealth, loaded.Health, loaded.Gems, loaded.SmallKeys));
        Assert.IsTrue(loaded.HasSword && loaded.HasBoomerang && loaded.HasBossKey);
        Assert.IsFalse(loaded.HasCourtyardKey);
        Assert.AreEqual(3, loaded.Deaths);
        Assert.AreEqual(456.5, loaded.PlaySeconds, 1e-9);
        Assert.IsTrue(loaded.Has("chest:dungeon:0,2:0"));
        Assert.IsTrue(loaded.WasVisited("dungeon", 1, 2));
        Assert.AreEqual(new Checkpoint("dungeon", 1, 3, 10f, 8.2f, Dir.Up), loaded.Entrance);
    }

    [TestMethod]
    public void ASaveWrittenByThePreviousReleaseLoads()
    {
        _saves.Write(SaveSlots.Slot, SavedByThePreviousRelease);
        AssertTheAdventure(Launched().Slots.Load());
    }

    private static string FixtureFolder => Path.Combine(StateTraceRuns.RepositoryRoot(), "tests/CourtyardKey.Tests/Fixtures/saves");

    /// <summary>
    /// The saves the game wrote before it was moved to the run's own store and the engine's state machines, kept as it
    /// wrote them (<c>Fixtures/saves</c>: a new game with the sword, the village, two dungeon rooms and the boss room,
    /// written by scripted runs with <c>--saves</c>).
    /// </summary>
    public static IEnumerable<object[]> WrittenBeforeTheModernization =>
        Directory.GetFiles(FixtureFolder, "*.json").Select(f => new object[] { Path.GetFileName(f) });

    [TestMethod]
    public void TheFixturesAreThere() => Assert.AreEqual(5, WrittenBeforeTheModernization.Count());

    /// <summary>Each loads whole (every property the file holds is in the progress) and is written back as it was.</summary>
    [TestMethod]
    [DynamicData(nameof(WrittenBeforeTheModernization))]
    public void ASaveWrittenBeforeTheModernizationLoadsAndIsWrittenBackUnchanged(string file)
    {
        string text = File.ReadAllText(Path.Combine(FixtureFolder, file)).ReplaceLineEndings("\n");
        _saves.Write(SaveSlots.Slot, text);
        var slots = Launched().Slots;
        Assert.AreEqual(SaveLoadStatus.Loaded, slots.Check());
        var loaded = slots.Load();
        Assert.IsNotNull(loaded);

        var json = JsonNode.Parse(text)!.AsObject();
        Assert.AreEqual((int)json["MaxHealth"]!, loaded.MaxHealth);
        Assert.AreEqual((int)json["Health"]!, loaded.Health);
        Assert.AreEqual((int)json["Gems"]!, loaded.Gems);
        Assert.AreEqual((int)json["SmallKeys"]!, loaded.SmallKeys);
        Assert.AreEqual((bool)json["HasSword"]!, loaded.HasSword);
        Assert.AreEqual((bool)json["HasBoomerang"]!, loaded.HasBoomerang);
        Assert.AreEqual((bool)json["HasBossKey"]!, loaded.HasBossKey);
        Assert.AreEqual((bool)json["HasCourtyardKey"]!, loaded.HasCourtyardKey);
        Assert.AreEqual((int)json["Deaths"]!, loaded.Deaths);
        Assert.AreEqual((double)json["PlaySeconds"]!, loaded.PlaySeconds);
        CollectionAssert.AreEquivalent(json["Flags"]!.AsArray().Select(n => (string)n!).ToArray(), loaded.Flags.ToArray());
        CollectionAssert.AreEquivalent(json["Visited"]!.AsArray().Select(n => (string)n!).ToArray(), loaded.Visited.ToArray());
        var entrance = json["Entrance"]!;
        Assert.AreEqual(new Checkpoint((string)entrance["Area"]!, (int)entrance["RoomX"]!, (int)entrance["RoomY"]!,
            (float)entrance["TileX"]!, (float)entrance["TileY"]!, (Dir)(int)entrance["Facing"]!), loaded.Entrance);
        Assert.AreEqual(14, json.Count, "a property this test does not compare");

        Assert.IsTrue(slots.Save(loaded));
        Assert.AreEqual(text, Saved!.ReplaceLineEndings("\n"), "saving it again writes the same file");
    }

    [TestMethod]
    public void SavingWritesTheEnginesVersionAndTheSameAdventure()
    {
        _saves.Write(SaveSlots.Slot, SavedByThePreviousRelease);
        var slots = Launched().Slots;
        slots.Save(slots.Load()!);

        var written = JsonNode.Parse(Saved!)!.AsObject();
        Assert.AreEqual(SaveSchema.VersionProperty, written.First().Key, "the version is the first property");
        Assert.AreEqual(1, (int?)written[SaveSchema.VersionProperty]);
        Assert.IsFalse(written.ContainsKey("Version"), "the game's own version property is gone");

        // Everything else is what the previous release wrote, in the same order.
        var before = JsonNode.Parse(SavedByThePreviousRelease)!.AsObject();
        before.Remove("Version");
        written.Remove(SaveSchema.VersionProperty);
        Assert.AreEqual(before.ToJsonString(), written.ToJsonString());
        AssertTheAdventure(Launched().Slots.Load());
    }

    [TestMethod]
    public void ASaveFromANewerReleaseIsNotLoadedAndNotReplaced()
    {
        var json = JsonNode.Parse(SavedByThePreviousRelease)!.AsObject();
        json.Insert(0, SaveSchema.VersionProperty, 2);
        string newer = json.ToJsonString();
        _saves.Write(SaveSlots.Slot, newer);

        var slots = Launched().Slots;
        Assert.IsNull(slots.Load());
        slots.Save(new Progress());
        Assert.AreEqual(newer, Saved);
    }

    [TestMethod]
    public void ASaveFromANewerReleaseIsNotOfferedAsContinue()
    {
        _saves.Write(SaveSlots.Slot, """{ "@version": 2, "Gems": 5 }""");
        var game = Launched();

        var title = Assert.IsInstanceOfType<Scenes.TitleScene>(game.Scenes.Current);
        Assert.AreEqual("Newer save", title.Menu.Options[1], "the entry says why it cannot be chosen");
        Assert.IsFalse(title.Menu.IsEnabled(1));
        Assert.AreEqual(0, title.Menu.Index, "the cursor starts on New Game");
    }

    [TestMethod]
    public void ASaveThatLoadsIsOfferedAsContinue()
    {
        _saves.Write(SaveSlots.Slot, SavedByThePreviousRelease);
        var game = Launched();

        var title = Assert.IsInstanceOfType<Scenes.TitleScene>(game.Scenes.Current);
        Assert.AreEqual("Continue", title.Menu.Options[1]);
        Assert.IsTrue(title.Menu.IsEnabled(1));
        Assert.AreEqual(1, title.Menu.Index);
    }

    [TestMethod]
    public void ARefusedSaveIsNotReportedAsSaved()
    {
        const string newer = """{ "@version": 2, "Gems": 5 }""";
        _saves.Write(SaveSlots.Slot, newer);
        var game = Launched("road");

        game.Play!.SaveGame();
        Assert.AreEqual("Could not save", game.Play.Hud.ToastText);
        Assert.AreEqual(newer, Saved);

        _saves.Delete(SaveSlots.Slot);
        game.Play.SaveGame();
        Assert.AreEqual("Progress saved", game.Play.Hud.ToastText);
    }

    private sealed class MemoryStorage : IStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public string Location => "memory";
        public string? Read(string slot) => Slots.GetValueOrDefault(slot);
        public void Write(string slot, string text) => Slots[slot] = text;
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot) => Slots.Remove(slot);
        public IReadOnlyList<SlotInfo> List() => [.. Slots.Select(s => new SlotInfo(s.Key, s.Value.Length, null))];
    }

    [TestMethod]
    public void TheGameKeepsItsSavesWhereItsHostSays()
    {
        var storage = new MemoryStorage();
        var asked = new List<string>();
        var host = GameHost.Create(new HostEnvironment(HostKind.Test) { Out = new StringWriter(), Error = new StringWriter() }, new LaunchRequest { Headless = true },
            h =>
            {
                Startup.Configure(h);
                h.Storage = id => { asked.Add(id); return storage; };
            });
        var game = (CourtyardKeyGame)host.CreateGame();
        game.InitializeForTest(new HeadlessPlatform());

        game.Slots.Save(new Progress { Gems = 5 });
        Assert.IsTrue(game.Achievements.Unlock(game.Achievements.Catalog[0].Id));

        CollectionAssert.AreEqual(new[] { "CourtyardKey" }, asked.Distinct().ToArray(), "under the id its saves have always had");
        Assert.IsTrue(storage.Slots.ContainsKey(SaveSlots.Slot), "the adventure");
        Assert.IsTrue(storage.Slots.ContainsKey(AchievementTracker.SaveSlot), "and the achievements");
    }
}
