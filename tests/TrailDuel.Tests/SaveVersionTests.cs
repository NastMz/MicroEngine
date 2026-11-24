using System.Text.Json.Nodes;
using MicroEngine.Core;
using TrailDuel.Rules;

namespace TrailDuel.Tests;

/// <summary>
/// The adventure's save is versioned by the engine (<see cref="GameState.Schema"/>), and the files earlier releases
/// wrote still load: the ones with the game's own "SaveVersion" property and the ones from before it.
/// </summary>
[TestClass]
public sealed class SaveVersionTests
{
    /// <summary>A save as the release before the engine's versions wrote it, property for property.</summary>
    private const string SavedByThePreviousRelease = """
        {
          "MapId": "route1",
          "X": 9,
          "Y": 24,
          "Facing": 2,
          "Party": [
            {
              "SpeciesId": "emberpup",
              "Level": 7,
              "Xp": 274,
              "Hp": 23,
              "Status": 0,
              "SleepTurns": 0,
              "Moves": [
                {
                  "Id": "scratch",
                  "Pp": 35
                },
                {
                  "Id": "glare",
                  "Pp": 30
                },
                {
                  "Id": "ember",
                  "Pp": 25
                }
              ],
              "HpFraction": 1,
              "XpFraction": 0,
              "CanEvolve": false
            }
          ],
          "Box": [],
          "Bag": {
            "Items": {
              "potion": 2
            }
          },
          "Money": 2750,
          "Flags": [
            "starter"
          ],
          "Seen": [
            "emberpup"
          ],
          "Caught": [
            "emberpup"
          ],
          "StarterId": "emberpup",
          "RespawnMap": "home",
          "RespawnX": 4,
          "RespawnY": 6,
          "PlaySeconds": 12.5,
          "BattlesWon": 3,
          "Badges": 0,
          "AnyHealthy": true,
          "SaveVersion": 1
        }
        """;

    private readonly MemorySaveStorage _saves = new();

    private void Write(string text) => _saves.Write(GameState.SaveSlot, text);

    private static void AssertTheAdventure(GameState? loaded)
    {
        Assert.IsNotNull(loaded);
        Assert.AreEqual(("route1", 9, 24, Direction.Left), (loaded.MapId, loaded.X, loaded.Y, loaded.Facing));
        Assert.AreEqual(2750, loaded.Money);
        Assert.IsTrue(loaded.Has("starter"));
        Assert.AreEqual("emberpup", loaded.StarterId);
        var creature = loaded.Party.Single();
        Assert.AreEqual(("emberpup", 7, 274, 23), (creature.SpeciesId, creature.Level, creature.Xp, creature.Hp));
        CollectionAssert.AreEqual(new[] { "scratch", "glare", "ember" }, creature.Moves.Select(m => m.Id).ToArray());
        Assert.AreEqual(25, creature.Moves[2].Pp);
        Assert.AreEqual(0, loaded.Box.Count);
        Assert.AreEqual(2, loaded.Bag.Items["potion"]);
        CollectionAssert.AreEqual(new[] { "emberpup" }, loaded.Caught.ToArray());
        Assert.AreEqual(("home", 4, 6), (loaded.RespawnMap, loaded.RespawnX, loaded.RespawnY));
        Assert.AreEqual(12.5, loaded.PlaySeconds);
        Assert.AreEqual(3, loaded.BattlesWon);
    }

    [TestMethod]
    public void ASaveWrittenByThePreviousReleaseLoads()
    {
        Write(SavedByThePreviousRelease);
        AssertTheAdventure(GameState.Load(new SaveStore(_saves)));
    }

    [TestMethod]
    public void ASaveWrittenBeforeTheGameKeptAVersionLoads()
    {
        var json = JsonNode.Parse(SavedByThePreviousRelease)!.AsObject();
        Assert.IsTrue(json.Remove("SaveVersion"));
        Write(json.ToJsonString());
        AssertTheAdventure(GameState.Load(new SaveStore(_saves)));
    }

    [TestMethod]
    public void SavingWritesTheEnginesVersionAndTheSameAdventure()
    {
        Write(SavedByThePreviousRelease);
        var store = new SaveStore(_saves);
        var loaded = GameState.Load(store)!;
        loaded.Save(store);

        var written = JsonNode.Parse(_saves.Read(GameState.SaveSlot)!)!.AsObject();
        Assert.AreEqual(SaveSchema.VersionProperty, written.First().Key, "the version is the first property");
        Assert.AreEqual(1, (int?)written[SaveSchema.VersionProperty]);
        Assert.IsFalse(written.ContainsKey("SaveVersion"), "the game's own version property is gone");

        // Everything else is what the previous release wrote, in the same order.
        var before = JsonNode.Parse(SavedByThePreviousRelease)!.AsObject();
        before.Remove("SaveVersion");
        written.Remove(SaveSchema.VersionProperty);
        Assert.AreEqual(before.ToJsonString(), written.ToJsonString());
        Assert.HasCount(1, _saves.List(), "a save of the same version is replaced without a copy");
        AssertTheAdventure(GameState.Load(new SaveStore(_saves)));
    }

    [TestMethod]
    public void ASaveFromANewerReleaseIsNotLoadedAndNotReplaced()
    {
        var json = JsonNode.Parse(SavedByThePreviousRelease)!.AsObject();
        json.Insert(0, SaveSchema.VersionProperty, 2);
        string newer = json.ToJsonString();
        Write(newer);
        var store = new SaveStore(_saves);

        Assert.IsNull(GameState.Load(store));
        GameState.NewGame().Save(store);
        Assert.AreEqual(newer, _saves.Read(GameState.SaveSlot));
    }
}

/// <summary>What the player is told about a save the game cannot use.</summary>
[TestClass]
public sealed class SaveFeedbackTests
{
    private readonly MemorySaveStorage _saves = new();

    private TrailDuelGame Launch(string? debug = null)
    {
        var game = new TrailDuelGame(debug);
        game.InitializeForTest(new HeadlessPlatform(), 3, _saves);
        game.StepForTest();
        return game;
    }

    [TestMethod]
    public void ASaveFromANewerReleaseIsNotOfferedAsContinue()
    {
        _saves.Write(GameState.SaveSlot, """{ "@version": 2, "Money": 5 }""");
        var title = Assert.IsInstanceOfType<Scenes.TitleScene>(Launch().Scenes.Current);
        CollectionAssert.DoesNotContain(title.Options.ToArray(), "CONTINUE");
    }

    [TestMethod]
    public void ASaveThatLoadsIsOfferedAsContinue()
    {
        GameState.NewGame().Save(new SaveStore(_saves));
        var title = Assert.IsInstanceOfType<Scenes.TitleScene>(Launch().Scenes.Current);
        Assert.AreEqual("CONTINUE", title.Options[0]);
    }

    [TestMethod]
    public void ARefusedSaveIsNotReportedAsSaved()
    {
        const string newer = """{ "@version": 2, "Money": 5 }""";
        _saves.Write(GameState.SaveSlot, newer);
        var game = Launch("town");

        Assert.IsFalse(game.SaveGame());
        Assert.AreEqual(newer, _saves.Read(GameState.SaveSlot));
        Assert.AreEqual("The trail could not be saved.", Scenes.StartMenuScene.SaveMessage(false));
        Assert.AreEqual("Progress saved! Rest easy.", Scenes.StartMenuScene.SaveMessage(true));

        _saves.Delete(GameState.SaveSlot);
        Assert.IsTrue(game.SaveGame());
    }
}

/// <summary>The game keeps its saves where its host says, under the id they have always had.</summary>
[TestClass]
public sealed class SaveMediumTests
{
    private sealed class MemoryStorage : MicroEngine.Platform.IStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public string Location => "memory";
        public string? Read(string slot) => Slots.GetValueOrDefault(slot);
        public void Write(string slot, string text) => Slots[slot] = text;
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot) => Slots.Remove(slot);
        public IReadOnlyList<MicroEngine.Platform.SlotInfo> List() => [.. Slots.Select(s => new MicroEngine.Platform.SlotInfo(s.Key, s.Value.Length, null))];
    }

    [TestMethod]
    public void TheGameKeepsItsSavesWhereItsHostSays()
    {
        var storage = new MemoryStorage();
        var asked = new List<string>();
        var host = GameHost.Create(new MicroEngine.Hosting.HostEnvironment(MicroEngine.Hosting.HostKind.Test) { Out = new StringWriter(), Error = new StringWriter() },
            new MicroEngine.Hosting.LaunchRequest { Headless = true },
            h =>
            {
                Startup.Configure(h);
                h.Storage = id => { asked.Add(id); return storage; };
            });
        var game = (TrailDuelGame)host.CreateGame();
        game.InitializeForTest(new HeadlessPlatform(), 3);

        game.SaveGame();
        Assert.IsTrue(game.HasSave);
        Assert.IsTrue(game.Achievements.Unlock(game.Achievements.Catalog[0].Id));

        CollectionAssert.AreEqual(new[] { "TrailDuel" }, asked.Distinct().ToArray(), "under the id its saves have always had");
        Assert.IsTrue(storage.Slots.ContainsKey(GameState.SaveSlot), "the adventure");
        Assert.IsTrue(storage.Slots.ContainsKey(AchievementTracker.SaveSlot), "and the achievements");
    }
}
