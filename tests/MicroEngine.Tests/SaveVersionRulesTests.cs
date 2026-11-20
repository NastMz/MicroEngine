using System.Text.Json;
using System.Text.Json.Serialization;
using MicroEngine.Core;
using MicroEngine.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroEngine.Tests;

/// <summary>
/// The rules that keep a versioned slot whole whoever writes to it: the calls without a schema never drop a version or
/// replace a versioned slot, every versioned save reads the slot as a load would before it replaces it, protection
/// follows the last load, copies of older saves are never destroyed and can be reached, and a mistake in the schema's
/// declaration never moves a player's save.
/// </summary>
[TestClass]
public sealed partial class SaveVersionRulesTests
{
    public sealed class Profile
    {
        public int Gold { get; set; }
        public int Lives { get; set; } = 3;
    }

    /// <summary>A saved type whose own code refuses a value.</summary>
    public sealed class Strict
    {
        private int _level = 1;
        public int Level
        {
            get => _level;
            set => _level = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), "levels start at 1");
        }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(Dog), "dog")]
    public class Animal { public string Name { get; set; } = ""; }
    public sealed class Dog : Animal { public int Bones { get; set; } }

    [JsonSerializable(typeof(Profile))]
    [JsonSerializable(typeof(Strict))]
    [JsonSerializable(typeof(Animal))]
    [JsonSerializable(typeof(List<int>))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    private sealed partial class RuleTypes : JsonSerializerContext;

    private static SaveSchema<Profile> Schema() => new SaveSchema<Profile>(3, RuleTypes.Default.Profile)
        .Migrate(1, save => save["Lives"] = 5)
        .Migrate(2, save => { save["Gold"] = save["Coins"]!.DeepClone(); save.Remove("Coins"); });

    private sealed class MemoryStorage : ISaveStorage
    {
        // File systems on Windows and macOS fold letter case; so does this storage, to check the store against it.
        public Dictionary<string, string> Slots { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool FailReads;
        public string Location => "memory";
        public string? Read(string slot) => FailReads ? throw new IOException("locked") : Slots.GetValueOrDefault(slot);
        public void Write(string slot, string text) => Slots[slot] = text;
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot)
        {
            if (Slots.Remove(slot, out var text)) Slots[slot + ".corrupt"] = text;
        }
        public IReadOnlyList<SlotInfo> List() =>
            [.. Slots.Where(s => !s.Key.EndsWith(".corrupt")).Select(s => new SlotInfo(s.Key, s.Value.Length, null))];
    }

    private static (MemoryStorage Storage, SaveStore Store) With(string? json, string slot = "p")
    {
        var storage = new MemoryStorage();
        if (json is not null) storage.Slots[slot] = json;
        return (storage, new SaveStore(storage));
    }

    // ---- 1. The calls without a schema and a versioned slot ------------------------------------------------------

    [TestMethod]
    public void A_save_without_a_schema_never_replaces_a_versioned_slot()
    {
        const string newer = """{ "@version": 9, "Gold": 1 }""";
        var (storage, fresh) = With(newer);
        Assert.IsFalse(fresh.Save("p", new Profile { Gold = 123 }), "a store that never loaded the slot");
        Assert.IsFalse(fresh.Save("p", new Profile { Gold = 123 }, RuleTypes.Default.Profile));
        Assert.AreEqual(newer, storage.Slots["p"]);

        // After the slot was refused as too new, under another letter case of the same file.
        Assert.AreEqual(SaveLoadStatus.TooNew, fresh.Load("p", Schema()).Status);
        Assert.IsFalse(fresh.Save("P", new Profile { Gold = 123 }));
        Assert.IsFalse(fresh.Save("P", new Profile { Gold = 123 }, Schema()));
        Assert.AreEqual(newer, storage.Slots["p"]);

        // A slot of the current version belongs to its schema too: a save without one would drop the version.
        const string current = """{ "@version": 3, "Gold": 50, "Lives": 1 }""";
        var (owned, other) = With(current);
        Assert.IsFalse(other.Save("p", new Profile { Gold = 60 }, RuleTypes.Default.Profile));
        Assert.AreEqual(current, owned.Slots["p"]);
        Assert.AreEqual(SaveLoadStatus.Loaded, other.Load("p", Schema()).Status);
    }

    [TestMethod]
    public void A_slot_loaded_through_a_schema_is_not_saved_without_it()
    {
        // Version 1 has no version property, so only the store knows a schema manages the slot.
        const string old = """{ "Coins": 7 }""";
        var (storage, store) = With(old);
        var loaded = store.Load("p", Schema());
        Assert.AreEqual(SaveLoadStatus.Migrated, loaded.Status);
        Assert.IsFalse(store.Save("p", loaded.Value!, RuleTypes.Default.Profile), "the current shape without a version would be migrated again");
        Assert.IsFalse(store.Save("p", loaded.Value!));
        Assert.AreEqual(old, storage.Slots["p"]);

        Assert.IsTrue(store.Save("p", loaded.Value!, Schema()));
        var again = store.Load("p", Schema());
        Assert.AreEqual(SaveLoadStatus.Loaded, again.Status);
        Assert.AreEqual(7, again.Value!.Gold);
        Assert.AreEqual(5, again.Value.Lives);
    }

    [TestMethod]
    public void A_load_without_a_schema_reads_a_versioned_slot_and_leaves_it_alone()
    {
        const string current = """{ "@version": 3, "Gold": 50, "Lives": 1 }""";
        var (storage, store) = With(current);
        Assert.AreEqual(50, store.Load("p", RuleTypes.Default.Profile)!.Gold);
        Assert.AreEqual(50, store.Load<Profile>("p")!.Gold);
        Assert.AreEqual(current, storage.Slots["p"]);
        Assert.AreEqual(1, storage.Slots.Count);
    }

    [TestMethod]
    public void A_slot_that_never_had_a_version_is_saved_as_before()
    {
        var (storage, store) = With("""{ "Gold": 1 }""");
        Assert.IsTrue(store.Save("p", new Profile { Gold = 2 }, RuleTypes.Default.Profile));
        Assert.AreEqual(string.Join(Environment.NewLine, "{", """  "Gold": 2,""", """  "Lives": 3""", "}"), storage.Slots["p"]);
        Assert.IsTrue(store.Save("p", new Profile { Gold = 3 }));
        Assert.AreEqual(3, store.Load<Profile>("p")!.Gold);
        Assert.AreEqual(1, storage.Slots.Count);
    }

    // ---- 2. Protection follows the last load ---------------------------------------------------------------------

    [TestMethod]
    public void A_later_load_that_succeeds_lifts_the_protection()
    {
        const string saved = """{ "@version": 3, "Gold": 2, "Lives": 3 }""";
        var (storage, store) = With(saved);
        storage.FailReads = true;                                   // a sync client holds the file at startup
        Assert.IsTrue(store.Load("p", Schema()).Protected);
        storage.FailReads = false;
        Assert.IsFalse(store.Save("p", new Profile(), Schema()), "not until the slot has been read");
        var loaded = store.Load("p", Schema());
        Assert.AreEqual(SaveLoadStatus.Loaded, loaded.Status);
        Assert.IsFalse(loaded.Protected);
        Assert.IsTrue(store.Save("p", loaded.Value!, Schema()), "Protected told the truth");

        // The same for a slot that was too new and has been replaced by one this build reads.
        var (replaced, second) = With("""{ "@version": 9 }""");
        Assert.AreEqual(SaveLoadStatus.TooNew, second.Load("p", Schema()).Status);
        replaced.Slots["p"] = saved;
        Assert.AreEqual(SaveLoadStatus.Loaded, second.Load("p", Schema()).Status);
        Assert.IsTrue(second.Save("p", new Profile(), Schema()));

        // And for the calls without a schema.
        var (plain, third) = With("""{ "Gold": 4 }""");
        plain.FailReads = true;
        Assert.IsNull(third.Load("p", RuleTypes.Default.Profile));
        plain.FailReads = false;
        Assert.IsFalse(third.Save("p", new Profile(), RuleTypes.Default.Profile));
        Assert.AreEqual(4, third.Load("p", RuleTypes.Default.Profile)!.Gold);
        Assert.IsTrue(third.Save("p", new Profile(), RuleTypes.Default.Profile));
    }

    // ---- 3. Polymorphic roots ------------------------------------------------------------------------------------

    [TestMethod]
    public void A_versioned_save_of_a_polymorphic_type_keeps_its_type_first()
    {
        var typed = new SaveSchema<Animal>(1, RuleTypes.Default.Animal);
        var (storage, store) = With(null);
        Assert.IsTrue(store.Save("p", new Dog { Name = "rex", Bones = 2 }, typed));
        string text = storage.Slots["p"];
        Assert.IsTrue(text.IndexOf("\"$type\"", StringComparison.Ordinal) < text.IndexOf("\"@version\"", StringComparison.Ordinal), text);

        // What a build without the schema does with that file.
        Assert.AreEqual(2, ((Dog)JsonSerializer.Deserialize(text, RuleTypes.Default.Animal)!).Bones);
        Assert.AreEqual(2, ((Dog)store.Load("p", RuleTypes.Default.Animal)!).Bones);
        Assert.AreEqual(2, ((Dog)store.Load<Animal>("p")!).Bones);
        Assert.AreEqual(text, storage.Slots["p"]);
        Assert.AreEqual(2, ((Dog)store.Load("p", typed).Value!).Bones);

        // By reflection, the same text.
        var (other, reflected) = With(null);
        Assert.IsTrue(reflected.Save("p", new Dog { Name = "rex", Bones = 2 }, new SaveSchema<Animal>(1)));
        Assert.AreEqual(text, other.Slots["p"]);

        // And a file written without a schema loads through one.
        var (plain, last) = With(null);
        Assert.IsTrue(last.Save<Animal>("p", new Dog { Name = "rex", Bones = 4 }, RuleTypes.Default.Animal));
        var loaded = last.Load("p", typed);
        Assert.AreEqual(SaveLoadStatus.Loaded, loaded.Status);
        Assert.AreEqual(4, ((Dog)loaded.Value!).Bones);
        Assert.AreEqual(1, plain.Slots.Count);
    }

    // ---- 4. A type that is not a JSON object ---------------------------------------------------------------------

    [TestMethod]
    public void A_schema_for_a_type_that_is_not_a_JSON_object_is_refused_when_it_is_declared()
    {
        var list = Assert.ThrowsExactly<InvalidOperationException>(() => new SaveSchema<List<int>>(1, RuleTypes.Default.ListInt32));
        StringAssert.Contains(list.Message, "List");
        StringAssert.Contains(list.Message, "object");
        Assert.ThrowsExactly<InvalidOperationException>(() => new SaveSchema<string>(1, RuleTypes.Default.String));
        Assert.ThrowsExactly<InvalidOperationException>(() => new SaveSchema<List<int>>(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => new SaveSchema<string>(1));

        // A dictionary is an object.
        var scores = new SaveSchema<Dictionary<string, int>>(1, RuleTypes.Default.DictionaryStringInt32);
        Assert.AreEqual(SaveLoadStatus.Loaded, scores.Read(scores.Write(new() { ["a"] = 1 })).Status);
    }

    // ---- 5. The version property ---------------------------------------------------------------------------------

    [TestMethod]
    public void A_null_version_is_corrupt_like_any_version_that_is_not_a_number()
    {
        var (storage, store) = With("""{ "@version": null, "Coins": 1 }""");
        Assert.AreEqual(SaveLoadStatus.Corrupt, store.Load("p", Schema()).Status);
        Assert.IsTrue(storage.Slots.ContainsKey("p.corrupt"));
    }

    // ---- 6. A versioned save reads the slot first ----------------------------------------------------------------

    [TestMethod]
    public void A_versioned_save_treats_the_slot_as_a_load_would_have()
    {
        // Older: the original is kept, as after a load.
        const string old = """{ "@version": 2, "Coins": 7, "Lives": 1 }""";
        var (older, first) = With(old);
        Assert.IsTrue(first.Save("p", new Profile { Gold = 1 }, Schema()));
        Assert.AreEqual(old, older.Slots["p.v2.backup"]);
        StringAssert.Contains(older.Slots["p"], "\"@version\": 3");

        // An older save its steps fail on: left alone.
        const string failing = """{ "@version": 2, "Lives": 1 }""";
        var (fails, second) = With(failing);
        Assert.IsFalse(second.Save("p", new Profile { Gold = 1 }, Schema()));
        Assert.AreEqual(failing, fails.Slots["p"]);
        Assert.AreEqual(1, fails.Slots.Count);

        // Newer: left alone, every time.
        const string newer = """{ "@version": 4, "Gold": 1 }""";
        var (news, third) = With(newer);
        Assert.IsFalse(third.Save("p", new Profile(), Schema()));
        Assert.IsFalse(third.Save("p", new Profile(), Schema()));
        Assert.AreEqual(newer, news.Slots["p"]);

        // Not a save: set aside, as a load sets it aside, and the new save is written.
        const string broken = """{ "@version": "two", "Gold": 1 }""";
        var (corrupt, fourth) = With(broken);
        Assert.IsTrue(fourth.Save("p", new Profile(), Schema()));
        Assert.AreEqual(broken, corrupt.Slots["p.corrupt"]);

        // A slot that cannot be read: refused.
        var (locked, fifth) = With(newer);
        locked.FailReads = true;
        Assert.IsFalse(fifth.Save("p", new Profile(), Schema()));

        // Current: replaced, with no copy.
        var (same, sixth) = With("""{ "@version": 3, "Gold": 1 }""");
        Assert.IsTrue(sixth.Save("p", new Profile { Gold = 2 }, Schema()));
        Assert.AreEqual(1, same.Slots.Count);
    }

    // ---- 7. Copies are never destroyed, and are what their name says ---------------------------------------------

    [TestMethod]
    public void An_existing_copy_is_never_replaced()
    {
        const string old = """{ "Coins": 7 }""";
        var (storage, store) = With(old);
        storage.Slots["p.v1.backup"] = """{ "Coins": 1000 }""";    // kept by an earlier migration of this slot
        var loaded = store.Load("p", Schema());
        Assert.IsTrue(store.Save("p", loaded.Value!, Schema()));
        Assert.AreEqual("""{ "Coins": 1000 }""", storage.Slots["p.v1.backup"], "the first copy stays");
        Assert.AreEqual(old, storage.Slots["p.v1.2.backup"], "the new one takes the next name");

        // The same original again: one copy is enough.
        storage.Slots["p"] = old;
        Assert.IsTrue(store.Save("p", store.Load("p", Schema()).Value!, Schema()));
        Assert.AreEqual(3, storage.Slots.Count, string.Join(", ", storage.Slots.Keys));
    }

    [TestMethod]
    public void A_copy_holds_the_version_its_name_says()
    {
        var (storage, store) = With("""{ "Coins": 7 }""");
        var loaded = store.Load("p", Schema());
        const string newer = """{ "@version": 9, "X": 1 }""";
        storage.Slots["p"] = newer;                                 // another program wrote the slot meanwhile
        Assert.IsFalse(store.Save("p", loaded.Value!, Schema()));
        Assert.AreEqual(newer, storage.Slots["p"]);
        Assert.AreEqual(1, storage.Slots.Count, "nothing was copied under the name of version 1");
    }

    // ---- 8. Reaching the copies ----------------------------------------------------------------------------------

    [TestMethod]
    public void The_copies_of_a_slot_can_be_listed_read_and_deleted()
    {
        const string old = """{ "Coins": 7 }""";
        var (storage, store) = With(old);
        Assert.AreEqual(0, store.Backups("p").Count);
        Assert.IsTrue(store.Save("p", store.Load("p", Schema()).Value!, Schema()));
        storage.Slots["p2"] = "{}";
        storage.Slots["p2.v1.backup"] = "{}";

        var copies = store.Backups("p");
        CollectionAssert.AreEqual(new[] { "p.v1.backup" }, copies.Select(c => c.Name).ToArray());
        Assert.AreEqual(old.Length, copies[0].Size);
        CollectionAssert.AreEqual(new[] { "p", "p2" }, store.Slots().Select(s => s.Name).ToArray(), "copies are not slots");

        // Restoring: read the copy through the schema, save it to the slot.
        var copy = store.LoadBackup("p.v1.backup", Schema());
        Assert.AreEqual(SaveLoadStatus.Migrated, copy.Status);
        Assert.AreEqual(1, copy.Version);
        Assert.AreEqual(7, copy.Value!.Gold);
        Assert.IsTrue(store.Save("p", copy.Value, Schema()));
        Assert.AreEqual(old, storage.Slots["p.v1.backup"], "reading a copy changes nothing");

        // A copy is only read: one that is not a save is reported, not moved.
        storage.Slots["p.v2.backup"] = "{ nope";
        Assert.AreEqual(SaveLoadStatus.Corrupt, store.LoadBackup("p.v2.backup", Schema()).Status);
        Assert.AreEqual("{ nope", storage.Slots["p.v2.backup"]);
        Assert.AreEqual(SaveLoadStatus.Missing, store.LoadBackup("p.v7.backup", Schema()).Status);

        store.DeleteBackup("p.v1.backup");
        CollectionAssert.AreEqual(new[] { "p.v2.backup" }, store.Backups("p").Select(c => c.Name).ToArray());
        Assert.IsTrue(storage.Slots.ContainsKey("p"));

        // Only copies: a slot is not reached through these members.
        Assert.ThrowsExactly<ArgumentException>(() => store.DeleteBackup("p"));
        Assert.ThrowsExactly<ArgumentException>(() => store.LoadBackup("p", Schema()));
        Assert.ThrowsExactly<ArgumentException>(() => store.Backups("p.v1.backup"));
        Assert.ThrowsExactly<ArgumentNullException>(() => store.LoadBackup("p.v1.backup", (SaveSchema<Profile>)null!));
    }

    // ---- 10. The saved type's own code throws --------------------------------------------------------------------

    [TestMethod]
    public void A_save_the_types_own_code_refuses_is_a_save_that_does_not_fit()
    {
        var schema = new SaveSchema<Strict>(2, RuleTypes.Default.Strict).Migrate(1, save => save["Level"] = save["Stage"]?.DeepClone());

        var (storage, store) = With("""{ "@version": 2, "Level": 0 }""");
        var current = store.Load("p", schema);
        Assert.AreEqual(SaveLoadStatus.Corrupt, current.Status);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(current.Error);
        Assert.IsTrue(storage.Slots.ContainsKey("p.corrupt"));

        const string old = """{ "Stage": -4 }""";
        var (older, second) = With(old);
        var migrated = second.Load("p", schema);
        Assert.AreEqual(SaveLoadStatus.MigrationFailed, migrated.Status);
        Assert.AreEqual(old, older.Slots["p"]);
    }
}
