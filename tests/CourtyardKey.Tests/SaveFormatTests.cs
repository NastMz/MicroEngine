using System.Text.Json.Nodes;
using CourtyardKey.Model;
using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Platform;

namespace CourtyardKey.Tests;

/// <summary>
/// The adventure's save is versioned by the engine (<see cref="Progress.Schema"/>), the files earlier releases wrote
/// still load, and the game keeps its saves where its host says.
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

    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = Path.Combine(Path.GetTempPath(), "courtyard-save-format-" + Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private string SaveFile => Path.Combine(_dir, SaveSlots.Slot + ".json");

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
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SaveFile, SavedByThePreviousRelease);
        AssertTheAdventure(new CourtyardKeyGame(_dir).Slots.Load());
    }

    [TestMethod]
    public void SavingWritesTheEnginesVersionAndTheSameAdventure()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SaveFile, SavedByThePreviousRelease);
        var slots = new CourtyardKeyGame(_dir).Slots;
        slots.Save(slots.Load()!);

        var written = JsonNode.Parse(File.ReadAllText(SaveFile))!.AsObject();
        Assert.AreEqual(SaveSchema.VersionProperty, written.First().Key, "the version is the first property");
        Assert.AreEqual(1, (int?)written[SaveSchema.VersionProperty]);
        Assert.IsFalse(written.ContainsKey("Version"), "the game's own version property is gone");

        // Everything else is what the previous release wrote, in the same order.
        var before = JsonNode.Parse(SavedByThePreviousRelease)!.AsObject();
        before.Remove("Version");
        written.Remove(SaveSchema.VersionProperty);
        Assert.AreEqual(before.ToJsonString(), written.ToJsonString());
        AssertTheAdventure(new CourtyardKeyGame(_dir).Slots.Load());
    }

    [TestMethod]
    public void ASaveFromANewerReleaseIsNotLoadedAndNotReplaced()
    {
        var json = JsonNode.Parse(SavedByThePreviousRelease)!.AsObject();
        json.Insert(0, SaveSchema.VersionProperty, 2);
        string newer = json.ToJsonString();
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SaveFile, newer);

        var slots = new CourtyardKeyGame(_dir).Slots;
        Assert.IsNull(slots.Load());
        slots.Save(new Progress());
        Assert.AreEqual(newer, File.ReadAllText(SaveFile));
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
