using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Platform;

namespace NeonRift.Tests;

/// <summary>
/// The high score is versioned by the engine, the files earlier releases wrote still load, and the game keeps its saves
/// where its host says.
/// </summary>
[TestClass]
public sealed class SaveFormatTests
{
    /// <summary>The record as the release before the engine's versions wrote it.</summary>
    private const string SavedByThePreviousRelease = """
        {
          "HighScore": 12340
        }
        """;

    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "neonrift-save-format-" + Guid.NewGuid().ToString("N"))).FullName;

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private string SaveFile => Path.Combine(_dir, RiftGame.RecordSlot + ".json");

    private RiftGame Launch()
    {
        var game = new RiftGame(saveDirectory: _dir);
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        return game;
    }

    [TestMethod]
    public void ARecordWrittenByThePreviousReleaseLoads()
    {
        File.WriteAllText(SaveFile, SavedByThePreviousRelease);
        Assert.AreEqual(12340, Launch().Session.HighScore);
    }

    [TestMethod]
    public void SavingWritesTheEnginesVersionAndTheSameRecord()
    {
        File.WriteAllText(SaveFile, SavedByThePreviousRelease);
        var game = Launch();
        game.Session.AddScore(20000);
        game.SaveHighScore();

        string expected = string.Join(Environment.NewLine, "{", """  "@version": 1,""", """  "HighScore": 20000""", "}");
        Assert.AreEqual(expected, File.ReadAllText(SaveFile));
        Assert.AreEqual(20000, Launch().Session.HighScore);
    }

    [TestMethod]
    public void ARecordFromANewerReleaseIsNotLoadedAndNotReplaced()
    {
        const string newer = """{ "@version": 2, "Best": { "Score": 99999 } }""";
        File.WriteAllText(SaveFile, newer);
        var game = Launch();
        Assert.AreEqual(0, game.Session.HighScore);
        game.Session.AddScore(500);
        game.SaveHighScore();
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
        var game = (RiftGame)host.CreateGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));

        game.Session.AddScore(700);
        game.SaveHighScore();
        Assert.IsTrue(game.Achievements.Unlock(game.Achievements.Catalog[0].Id));

        CollectionAssert.AreEqual(new[] { "NeonRift" }, asked.Distinct().ToArray(), "under the id its saves have always had");
        Assert.IsTrue(storage.Slots.ContainsKey(RiftGame.RecordSlot), "the high score");
        Assert.IsTrue(storage.Slots.ContainsKey(AchievementTracker.SaveSlot), "and the achievements");
    }
}
