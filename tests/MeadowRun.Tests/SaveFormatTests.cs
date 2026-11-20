using System.Text.Json.Nodes;
using MeadowRun.Content;
using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Platform;

namespace MeadowRun.Tests;

/// <summary>
/// The progress is versioned by the engine, the files earlier releases wrote still load, and the game keeps its saves
/// where its host says.
/// </summary>
[TestClass]
public sealed class SaveFormatTests
{
    /// <summary>A save as the release before the engine's versions wrote it, property for property.</summary>
    private const string SavedByThePreviousRelease = """
        {
          "Unlocked": 3,
          "BestScores": [
            1200,
            3400,
            0,
            0,
            0
          ],
          "Finished": false,
          "BestCoins": [
            12,
            30,
            0,
            0,
            0
          ]
        }
        """;

    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "meadowrun-save-format-" + Guid.NewGuid().ToString("N"))).FullName;

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private string SaveFile => Path.Combine(_dir, "progress.json");

    private MeadowGame Launch()
    {
        var game = new MeadowGame(_dir);
        game.InitializeForTest(new HeadlessPlatform());
        return game;
    }

    [TestMethod]
    public void ASaveWrittenByThePreviousReleaseLoads()
    {
        Assert.HasCount(5, Levels.All, "the literal save has one entry per level");
        File.WriteAllText(SaveFile, SavedByThePreviousRelease);
        var save = Launch().Save;
        Assert.AreEqual(3, save.Unlocked);
        Assert.IsFalse(save.Finished);
        CollectionAssert.AreEqual(new[] { 1200, 3400, 0, 0, 0 }, save.BestScores);
        CollectionAssert.AreEqual(new[] { 12, 30, 0, 0, 0 }, save.BestCoins);
    }

    [TestMethod]
    public void SavingWritesTheEnginesVersionAndTheSameProgress()
    {
        File.WriteAllText(SaveFile, SavedByThePreviousRelease);
        Launch().Persist();

        var written = JsonNode.Parse(File.ReadAllText(SaveFile))!.AsObject();
        Assert.AreEqual(SaveSchema.VersionProperty, written.First().Key, "the version is the first property");
        Assert.AreEqual(1, (int?)written[SaveSchema.VersionProperty]);
        written.Remove(SaveSchema.VersionProperty);
        Assert.AreEqual(JsonNode.Parse(SavedByThePreviousRelease)!.ToJsonString(), written.ToJsonString(), "everything else is what the previous release wrote");
        Assert.AreEqual(3, Launch().Save.Unlocked);
    }

    [TestMethod]
    public void ASaveFromANewerReleaseIsNotLoadedAndNotReplaced()
    {
        var json = JsonNode.Parse(SavedByThePreviousRelease)!.AsObject();
        json.Insert(0, SaveSchema.VersionProperty, 2);
        string newer = json.ToJsonString();
        File.WriteAllText(SaveFile, newer);

        var game = Launch();
        Assert.AreEqual(1, game.Save.Unlocked, "the game starts with fresh progress");
        game.Persist();
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
        var game = (MeadowGame)host.CreateGame();
        game.InitializeForTest(new HeadlessPlatform());

        game.Persist();
        Assert.IsTrue(game.Achievements.Unlock(game.Achievements.Catalog[0].Id));

        CollectionAssert.AreEqual(new[] { "MeadowRun" }, asked.Distinct().ToArray(), "under the id its saves have always had");
        Assert.IsTrue(storage.Slots.ContainsKey("progress"), "the progress");
        Assert.IsTrue(storage.Slots.ContainsKey(AchievementTracker.SaveSlot), "and the achievements");
    }
}
