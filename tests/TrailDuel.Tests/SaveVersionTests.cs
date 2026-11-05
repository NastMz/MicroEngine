using System.Text.Json.Nodes;
using MicroEngine.Core;
using TrailDuel.Rules;

namespace TrailDuel.Tests;

/// <summary>The adventure's save file names its format version, and saves written before the version existed still load.</summary>
[TestClass]
public sealed class SaveVersionTests
{
    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = Path.Combine(Path.GetTempPath(), "trailduel-save-version-" + Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private string SaveFile => Path.Combine(_dir, GameState.SaveSlot + ".json");

    private static GameState Adventure()
    {
        var state = GameState.NewGame();
        state.MapId = "route1";
        (state.X, state.Y) = (9, 24);
        state.Money = 2750;
        state.Set("starter");
        state.StarterId = "emberpup";
        state.AddCreature(Creature.Create("emberpup", 7));
        return state;
    }

    [TestMethod]
    public void SavingWritesTheFormatVersion()
    {
        Adventure().Save(new SaveStore("TrailDuel", _dir));
        var json = JsonNode.Parse(File.ReadAllText(SaveFile))!;
        Assert.AreEqual(1, (int?)json["SaveVersion"]);
    }

    [TestMethod]
    public void ASaveWrittenBeforeTheVersionExistedStillLoads()
    {
        var store = new SaveStore("TrailDuel", _dir);
        Adventure().Save(store);
        // The same file as an earlier release wrote it: the same properties, without the version.
        var json = JsonNode.Parse(File.ReadAllText(SaveFile))!.AsObject();
        json.Remove("SaveVersion");
        File.WriteAllText(SaveFile, json.ToJsonString());

        var loaded = GameState.Load(store);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(GameState.CurrentSaveVersion, loaded.SaveVersion, "an unversioned save is read as the current format");
        Assert.AreEqual(("route1", 9, 24), (loaded.MapId, loaded.X, loaded.Y));
        Assert.AreEqual(2750, loaded.Money);
        Assert.IsTrue(loaded.Has("starter"));
        Assert.AreEqual("emberpup", loaded.Party.Single().SpeciesId);
        Assert.AreEqual(7, loaded.Party.Single().Level);
    }
}
