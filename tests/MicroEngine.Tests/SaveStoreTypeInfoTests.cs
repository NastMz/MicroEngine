using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MicroEngine.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroEngine.Tests;

/// <summary>
/// Saving and loading with the game's own type metadata (<c>Save(slot, data, typeInfo)</c>, <c>Load(slot, typeInfo)</c>):
/// the path that needs no reflection. It must behave like the path without metadata in everything but how the object
/// becomes text, and both must read each other's slots and the files earlier versions wrote.
/// </summary>
[TestClass]
public sealed partial class SaveStoreTypeInfoTests
{
    public enum Difficulty { Easy, Hard }

    public sealed class Progress
    {
        public int Level { get; set; } = 1;
        public Hero Hero { get; set; } = new();
        public Difficulty Difficulty { get; set; }
        public Dictionary<string, int> Scores { get; set; } = [];
        public List<Item> Bag { get; set; } = [];
        public string? Note { get; set; }
    }

    public sealed class Hero
    {
        public string Name { get; set; } = "";
        public List<string> Visited { get; set; } = [];
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(Potion), "potion")]
    [JsonDerivedType(typeof(Key), "key")]
    public abstract class Item { public string Name { get; set; } = ""; }
    public sealed class Potion : Item { public int Heals { get; set; } }
    public sealed class Key : Item { public string Door { get; set; } = ""; }

    /// <summary>The shape the existing tests save, for slots written without metadata.</summary>
    public sealed class Simple { public int Level { get; set; } public string Name { get; set; } = ""; }

    [JsonSerializable(typeof(Progress))]
    [JsonSerializable(typeof(Simple))]
    [JsonSerializable(typeof(JsonObject))]
    private sealed partial class Types : JsonSerializerContext;

    private sealed class MemoryStorage : ISaveStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public bool FailReads, FailWrites;
        public int Writes;
        public string Location => "memory";
        public string? Read(string slot) => FailReads ? throw new IOException("read refused") : Slots.GetValueOrDefault(slot);
        public void Write(string slot, string text)
        {
            if (FailWrites) throw new IOException("quota exceeded");
            Writes++;
            Slots[slot] = text;
        }
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot)
        {
            if (Slots.Remove(slot, out var text)) Slots[slot + ".corrupt"] = text;
        }
    }

    private static Progress Sample() => new()
    {
        Level = 3,
        Hero = new Hero { Name = "Ada", Visited = ["forest", "cave"] },
        Difficulty = Difficulty.Hard,
        Scores = new() { ["forest"] = 1200, ["cave"] = 880 },
        Bag = [new Potion { Name = "red", Heals = 5 }, new Key { Name = "brass", Door = "cellar" }],
    };

    private static void AssertSample(Progress? loaded)
    {
        Assert.IsNotNull(loaded);
        Assert.AreEqual(3, loaded.Level);
        Assert.AreEqual("Ada", loaded.Hero.Name);
        CollectionAssert.AreEqual(new[] { "forest", "cave" }, loaded.Hero.Visited);
        Assert.AreEqual(Difficulty.Hard, loaded.Difficulty);
        Assert.AreEqual(1200, loaded.Scores["forest"]);
        Assert.AreEqual(880, loaded.Scores["cave"]);
        Assert.AreEqual(2, loaded.Bag.Count);
        Assert.AreEqual(5, ((Potion)loaded.Bag[0]).Heals, "the derived type comes back, not the base");
        Assert.AreEqual("cellar", ((Key)loaded.Bag[1]).Door);
        Assert.IsNull(loaded.Note);
    }

    [TestMethod]
    public void A_nested_model_with_collections_and_derived_types_round_trips_with_its_metadata()
    {
        var store = new SaveStore(new MemoryStorage());
        Assert.IsNull(store.Load("progress", Types.Default.Progress), "no save yet");
        Assert.IsTrue(store.Save("progress", Sample(), Types.Default.Progress));
        Assert.IsTrue(store.Exists("progress"));
        AssertSample(store.Load("progress", Types.Default.Progress));
    }

    [TestMethod]
    public void Both_paths_write_the_same_text_and_read_each_other()
    {
        var storage = new MemoryStorage();
        var store = new SaveStore(storage);
        Assert.IsTrue(store.Save("plain", Sample()));
        Assert.IsTrue(store.Save("typed", Sample(), Types.Default.Progress));
        Assert.AreEqual(storage.Slots["plain"], storage.Slots["typed"], "one file format, whichever call wrote it");
        AssertSample(store.Load("plain", Types.Default.Progress));
        AssertSample(store.Load<Progress>("typed"));
    }

    [TestMethod]
    public void The_text_of_a_slot_is_indented_json_with_the_properties_by_name()
    {
        var storage = new MemoryStorage();
        var store = new SaveStore(storage);
        string expected = "{\n  \"Level\": 3,\n  \"Name\": \"Pip\"\n}".Replace("\n", Environment.NewLine);
        store.Save("plain", new Simple { Level = 3, Name = "Pip" });
        store.Save("typed", new Simple { Level = 3, Name = "Pip" }, Types.Default.Simple);
        Assert.AreEqual(expected, storage.Slots["plain"], "the format games have on disk");
        Assert.AreEqual(expected, storage.Slots["typed"]);
    }

    [TestMethod]
    [DataRow("{\r\n  \"Level\": 3,\r\n  \"Name\": \"Pip\"\r\n}", DisplayName = "written on Windows")]
    [DataRow("{\n  \"Level\": 3,\n  \"Name\": \"Pip\"\n}", DisplayName = "written on Linux, macOS, phones and browsers")]
    [DataRow("{\"Name\":\"Pip\",\"Level\":3,\"Removed\":true}", DisplayName = "compact, reordered, with a property this build no longer has")]
    public void Slots_written_by_earlier_versions_load_on_both_paths(string onDisk)
    {
        var storage = new MemoryStorage();
        storage.Slots["slot"] = onDisk;
        var store = new SaveStore(storage);
        var plain = store.Load<Simple>("slot");
        var typed = store.Load("slot", Types.Default.Simple);
        Assert.AreEqual(3, plain!.Level);
        Assert.AreEqual("Pip", plain.Name);
        Assert.AreEqual(3, typed!.Level);
        Assert.AreEqual("Pip", typed.Name);
        Assert.AreEqual(onDisk, storage.Slots["slot"], "loading never rewrites a slot");
    }

    [TestMethod]
    [DataRow("{ not json", DisplayName = "invalid JSON")]
    [DataRow("null", DisplayName = "the JSON null")]
    [DataRow("{ \"Level\": \"three\" }", DisplayName = "a value of the wrong type")]
    public void Corrupt_data_yields_null_and_is_set_aside(string onDisk)
    {
        var storage = new MemoryStorage();
        storage.Slots["slot"] = onDisk;
        var store = new SaveStore(storage);
        Assert.IsNull(store.Load("slot", Types.Default.Simple));
        Assert.AreEqual(onDisk, storage.Slots["slot.corrupt"], "kept aside, not lost on the next save");
        Assert.IsFalse(store.Exists("slot"));
        Assert.IsTrue(store.Save("slot", new Simple { Level = 1 }, Types.Default.Simple), "the game goes on with a new save");
    }

    [TestMethod]
    public void A_slot_that_could_not_be_read_is_never_overwritten()
    {
        var storage = new MemoryStorage();
        storage.Slots["slot"] = """{ "Level": 9, "Name": "Valid" }""";
        var store = new SaveStore(storage);
        storage.FailReads = true;
        Assert.IsNull(store.Load("slot", Types.Default.Simple), "the game starts fresh");
        storage.FailReads = false;
        Assert.IsFalse(store.Save("slot", new Simple { Level = 1 }, Types.Default.Simple), "fresh progress must not replace data that exists but was unreadable");
        Assert.IsFalse(store.Save("slot", new Simple { Level = 1 }), "on either path");
        Assert.AreEqual("""{ "Level": 9, "Name": "Valid" }""", storage.Slots["slot"]);
        Assert.AreEqual(0, storage.Writes);
        store.Delete("slot");
        Assert.IsTrue(store.Save("slot", new Simple { Level = 1 }, Types.Default.Simple), "deleting is the game's choice to start over");
    }

    [TestMethod]
    public void A_refused_write_is_reported_instead_of_crashing_the_game()
    {
        var storage = new MemoryStorage { FailWrites = true };
        var store = new SaveStore(storage);
        Assert.IsFalse(store.Save("slot", new Simple { Level = 2 }, Types.Default.Simple));
        storage.FailWrites = false;
        Assert.IsTrue(store.Save("slot", new Simple { Level = 2 }, Types.Default.Simple), "the next save tries again");
        Assert.AreEqual(2, store.Load("slot", Types.Default.Simple)!.Level);
    }

    [TestMethod]
    public void File_slots_are_written_whole_and_corrupt_files_are_kept()
    {
        var dir = Path.Combine(Path.GetTempPath(), "microengine-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SaveStore("test", dir);
            Assert.IsNull(store.Load("slot", Types.Default.Progress));
            Assert.IsTrue(store.Save("slot", Sample(), Types.Default.Progress));
            Assert.IsTrue(store.Save("slot", Sample(), Types.Default.Progress), "saving over an existing slot");
            CollectionAssert.AreEqual(new[] { "slot.json" }, System.IO.Directory.GetFiles(dir).Select(Path.GetFileName).ToArray(), "no temporary file is left behind");
            AssertSample(new SaveStore("test", dir).Load("slot", Types.Default.Progress));
            File.WriteAllText(Path.Combine(dir, "slot.json"), "{ \"Level\": 3, \"Hero\"");   // a save cut short
            Assert.IsNull(store.Load("slot", Types.Default.Progress));
            Assert.AreEqual("{ \"Level\": 3, \"Hero\"", File.ReadAllText(Path.Combine(dir, "slot.corrupt.json")));
            Assert.IsFalse(store.Exists("slot"));
        }
        finally { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
    }

    [TestMethod]
    public void A_slot_reads_as_json_for_upgrades_between_versions()
    {
        var storage = new MemoryStorage();
        storage.Slots["profile"] = """{ "Coins": 12 }""";   // written before "Coins" was renamed "Level"
        var store = new SaveStore(storage);
        var json = store.Load("profile", Types.Default.JsonObject);
        Assert.IsNotNull(json);
        json["Level"] = json["Coins"]?.DeepClone();
        json.Remove("Coins");
        Assert.IsTrue(store.Save("profile", json, Types.Default.JsonObject));
        Assert.AreEqual(12, store.Load("profile", Types.Default.Simple)!.Level);
    }

    [TestMethod]
    public void Missing_metadata_is_a_programming_error()
    {
        var store = new SaveStore(new MemoryStorage());
        Assert.ThrowsExactly<ArgumentNullException>(() => store.Save("slot", new Simple(), null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => store.Load<Simple>("slot", null!));
    }
}
