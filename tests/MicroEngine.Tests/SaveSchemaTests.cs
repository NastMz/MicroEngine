using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MicroEngine.Core;
using MicroEngine.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroEngine.Tests;

/// <summary>
/// Versioned saves: a saved type declares its current version and one step per earlier version
/// (<see cref="SaveSchema{T}"/>); the store brings an older save up to date when it loads it, refuses a save written by
/// a newer version of the game, and never moves or replaces a save this build could not bring up to date.
/// </summary>
[TestClass]
public sealed partial class SaveSchemaTests
{
    /// <summary>Version 3 of a profile. Version 1 had "Coins" and no lives; version 2 added lives; version 3 renamed coins.</summary>
    public sealed class Profile
    {
        public int Gold { get; set; }
        public int Lives { get; set; } = 3;
        public string Name { get; set; } = "";
    }

    [JsonSerializable(typeof(Profile))]
    [JsonSerializable(typeof(List<int>))]
    private sealed partial class SchemaTypes : JsonSerializerContext;

    private static void AddLives(JsonObject save) => save["Lives"] = 5;

    private static void RenameCoins(JsonObject save)
    {
        save["Gold"] = save["Coins"]?.DeepClone();
        save.Remove("Coins");
    }

    private static SaveSchema<Profile> Schema() => new SaveSchema<Profile>(3, SchemaTypes.Default.Profile)
        .Migrate(1, AddLives)
        .Migrate(2, RenameCoins);

    private sealed class MemoryStorage : ISaveStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public Dictionary<string, DateTimeOffset> Times { get; } = [];
        public Func<string, bool> RefuseWrite = _ => false;
        public bool FailReads, FailList;
        public int Reads;
        public string Location => "memory";
        public string? Read(string slot)
        {
            Reads++;
            return FailReads ? throw new IOException("read refused") : Slots.GetValueOrDefault(slot);
        }
        public void Write(string slot, string text)
        {
            if (RefuseWrite(slot)) throw new IOException("disk full");
            Slots[slot] = text;
        }
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot)
        {
            if (Slots.Remove(slot, out var text)) Slots[slot + ".corrupt"] = text;
        }
        public IReadOnlyList<SlotInfo> List() => FailList ? throw new IOException("list refused")
            : [.. Slots.Where(s => !s.Key.EndsWith(".corrupt")).Select(s => new SlotInfo(s.Key, s.Value.Length, Times.TryGetValue(s.Key, out var time) ? time : null))];
    }

    private static (MemoryStorage Storage, SaveStore Store) With(string? json, string slot = "profile")
    {
        var storage = new MemoryStorage();
        if (json is not null) storage.Slots[slot] = json;
        return (storage, new SaveStore(storage));
    }

    // ---- Loading -------------------------------------------------------------------------------------------------

    [TestMethod]
    public void An_older_save_loads_through_every_step_in_order()
    {
        const string old = """{ "@version": 1, "Coins": 7, "Name": "Ada" }""";
        var (storage, store) = With(old);
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.Migrated, result.Status);
        Assert.AreEqual(1, result.Version, "the result names the version the save was written in");
        Assert.AreEqual(7, result.Value!.Gold);
        Assert.AreEqual(5, result.Value.Lives);
        Assert.AreEqual("Ada", result.Value.Name);
        Assert.IsFalse(result.Protected);
        Assert.AreEqual(old, storage.Slots["profile"], "loading writes nothing");
        Assert.AreEqual(1, storage.Slots.Count);
    }

    [TestMethod]
    public void A_save_starts_at_the_step_of_its_own_version()
    {
        var (_, store) = With("""{ "@version": 2, "Coins": 7, "Lives": 1 }""");
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.Migrated, result.Status);
        Assert.AreEqual(2, result.Version);
        Assert.AreEqual(7, result.Value!.Gold);
        Assert.AreEqual(1, result.Value.Lives, "the step from version 1 did not run");
    }

    [TestMethod]
    public void A_save_without_a_version_is_version_1()
    {
        var (_, store) = With("""{ "Coins": 4 }""");
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.Migrated, result.Status);
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual(4, result.Value!.Gold);
        Assert.AreEqual(5, result.Value.Lives);

        // A type that has never changed is at version 1: the saves written before it had a schema load as they are.
        var (_, plain) = With("""{ "Gold": 9, "Lives": 2 }""");
        var same = plain.Load("profile", new SaveSchema<Profile>(1, SchemaTypes.Default.Profile));
        Assert.AreEqual(SaveLoadStatus.Loaded, same.Status);
        Assert.AreEqual(1, same.Version);
        Assert.AreEqual(9, same.Value!.Gold);
    }

    [TestMethod]
    public void A_save_of_the_current_version_loads_as_it_is()
    {
        var (_, store) = With("""{ "@version": 3, "Gold": 2, "Lives": 1 }""");
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.Loaded, result.Status);
        Assert.AreEqual(3, result.Version);
        Assert.AreEqual(2, result.Value!.Gold);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void A_missing_slot_is_reported_as_missing()
    {
        var (_, store) = With(null);
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.Missing, result.Status);
        Assert.IsNull(result.Value);
        Assert.AreEqual(0, result.Version);
        Assert.IsFalse(result.Protected);
        Assert.IsTrue(store.Save("profile", new Profile(), Schema()));
    }

    [TestMethod]
    public void A_save_from_a_newer_version_is_refused_and_left_alone()
    {
        const string newer = """{ "@version": 4, "Gold": 1, "Gems": 3 }""";
        var (storage, store) = With(newer);
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.TooNew, result.Status);
        Assert.AreEqual(4, result.Version);
        Assert.IsNull(result.Value);
        Assert.IsTrue(result.Protected);

        Assert.IsFalse(store.Save("profile", new Profile { Gold = 99 }, Schema()), "the store does not replace a save it refused");
        Assert.IsFalse(store.Save("profile", new Profile { Gold = 99 }, SchemaTypes.Default.Profile), "by any way of saving");
        Assert.AreEqual(newer, storage.Slots["profile"]);
        Assert.AreEqual(1, storage.Slots.Count, "a newer save is not corrupt data: it is not set aside");

        store.Delete("profile");
        Assert.IsTrue(store.Save("profile", new Profile(), Schema()), "deleting is the game's choice to start over");
    }

    [TestMethod]
    public void Saving_never_replaces_a_newer_save_the_store_has_not_loaded()
    {
        const string newer = """{ "@version": 9, "Gold": 1 }""";
        var (storage, store) = With(newer);
        Assert.IsFalse(store.Save("profile", new Profile { Gold = 5 }, Schema()));
        Assert.AreEqual(newer, storage.Slots["profile"]);
        Assert.IsFalse(store.Save("profile", new Profile { Gold = 5 }, Schema()), "and keeps refusing");
    }

    [TestMethod]
    [DataRow("{ not json", DisplayName = "invalid JSON")]
    [DataRow("[1, 2]", DisplayName = "not an object")]
    [DataRow("null", DisplayName = "null")]
    [DataRow("""{ "@version": "two", "Gold": 1 }""", DisplayName = "a version that is not a number")]
    [DataRow("""{ "@version": 0, "Gold": 1 }""", DisplayName = "a version below 1")]
    [DataRow("""{ "@version": 3, "Gold": "lots" }""", DisplayName = "a field of the wrong type")]
    public void Corrupt_data_is_set_aside_like_any_corrupt_slot(string text)
    {
        var (storage, store) = With(text);
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.Corrupt, result.Status);
        Assert.IsNull(result.Value);
        Assert.IsFalse(result.Protected);
        Assert.AreEqual(text, storage.Slots["profile.corrupt"]);
        Assert.IsFalse(storage.Slots.ContainsKey("profile"));
        Assert.IsTrue(store.Save("profile", new Profile(), Schema()), "a fresh slot can be written");
    }

    [TestMethod]
    public void A_slot_that_cannot_be_read_is_protected()
    {
        const string saved = """{ "@version": 3, "Gold": 2 }""";
        var (storage, store) = With(saved);
        storage.FailReads = true;
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.Unreadable, result.Status);
        Assert.IsTrue(result.Protected);
        storage.FailReads = false;
        Assert.IsFalse(store.Save("profile", new Profile(), Schema()));
        Assert.AreEqual(saved, storage.Slots["profile"]);

        // Saving first, over a slot that cannot be looked at, is refused too.
        var (blocked, second) = With(saved);
        blocked.FailReads = true;
        Assert.IsFalse(second.Save("profile", new Profile(), Schema()));
        Assert.AreEqual(saved, blocked.Slots["profile"]);
    }

    [TestMethod]
    public void A_step_that_fails_leaves_the_save_alone_and_reports_why()
    {
        const string old = """{ "@version": 1, "Coins": "many" }""";
        var schema = new SaveSchema<Profile>(2, SchemaTypes.Default.Profile)
            .Migrate(1, save => save["Gold"] = save["Coins"]!.GetValue<int>() * 2);
        var (storage, store) = With(old);
        var result = store.Load("profile", schema);
        Assert.AreEqual(SaveLoadStatus.MigrationFailed, result.Status);
        Assert.AreEqual(1, result.Version);
        Assert.IsNotNull(result.Error);
        Assert.IsNull(result.Value);
        Assert.IsTrue(result.Protected);
        Assert.IsFalse(store.Save("profile", new Profile(), schema));
        Assert.AreEqual(old, storage.Slots["profile"]);
        Assert.AreEqual(1, storage.Slots.Count, "an old save this build failed to bring up to date is not set aside");
    }

    [TestMethod]
    public void An_old_save_whose_steps_do_not_produce_the_type_is_left_alone()
    {
        // The steps ran, but what they left does not fit the type: the fault may be the steps', so the save stays.
        const string old = """{ "Coins": "lots" }""";
        var (storage, store) = With(old);
        var result = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.MigrationFailed, result.Status);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(old, storage.Slots["profile"]);
        Assert.AreEqual(1, storage.Slots.Count);
    }

    // ---- Saving --------------------------------------------------------------------------------------------------

    [TestMethod]
    public void Saving_writes_the_version_first_and_the_type_as_it_always_was()
    {
        var (storage, store) = With(null);
        Assert.IsTrue(store.Save("profile", new Profile { Gold = 2, Lives = 1, Name = "Ada" }, Schema()));
        string versionLine = """  "@version": 3,""" + Environment.NewLine;
        string expected = string.Join(Environment.NewLine,
            "{",
            """  "@version": 3,""",
            """  "Gold": 2,""",
            """  "Lives": 1,""",
            """  "Name": "Ada" """.TrimEnd(),
            "}");
        Assert.AreEqual(expected, storage.Slots["profile"]);

        var loaded = store.Load("profile", Schema());
        Assert.AreEqual(SaveLoadStatus.Loaded, loaded.Status);
        Assert.AreEqual("Ada", loaded.Value!.Name);

        // The same text but for the version line, and a load without a schema reads it.
        var plainStorage = new MemoryStorage();
        new SaveStore(plainStorage).Save("profile", new Profile { Gold = 2, Lives = 1, Name = "Ada" }, SchemaTypes.Default.Profile);
        Assert.AreEqual(plainStorage.Slots["profile"], expected.Replace(versionLine, ""));
        Assert.AreEqual(2, store.Load("profile", SchemaTypes.Default.Profile)!.Gold);
        Assert.AreEqual(2, store.Load<Profile>("profile")!.Gold);
    }

    [TestMethod]
    public void The_first_save_over_an_older_save_keeps_the_original()
    {
        const string old = """{ "@version": 2, "Coins": 7, "Lives": 1 }""";
        var (storage, store) = With(old);
        var profile = store.Load("profile", Schema()).Value!;
        Assert.AreEqual(1, storage.Slots.Count, "nothing is copied until the slot is about to change");

        profile.Gold++;
        Assert.IsTrue(store.Save("profile", profile, Schema()));
        Assert.AreEqual(old, storage.Slots["profile.v2.backup"]);
        StringAssert.Contains(storage.Slots["profile"], """
            "@version": 3
            """.Trim());

        storage.Slots.Remove("profile.v2.backup");
        Assert.IsTrue(store.Save("profile", profile, Schema()));
        Assert.IsFalse(storage.Slots.ContainsKey("profile.v2.backup"), "the copy is made once, of the save the older version wrote");
    }

    [TestMethod]
    public void Without_the_copy_an_older_save_is_not_replaced()
    {
        const string old = """{ "Coins": 7 }""";
        var (storage, store) = With(old);
        storage.RefuseWrite = slot => slot != "profile";
        var profile = store.Load("profile", Schema()).Value!;
        Assert.IsFalse(store.Save("profile", profile, Schema()));
        Assert.AreEqual(old, storage.Slots["profile"]);

        storage.RefuseWrite = _ => false;
        Assert.IsTrue(store.Save("profile", profile, Schema()), "the save goes through once the copy can be written");
        Assert.AreEqual(old, storage.Slots["profile.v1.backup"]);
    }

    [TestMethod]
    public void The_reflection_schema_reads_and_writes_the_same_slots()
    {
        var reflection = new SaveSchema<Profile>(3).Migrate(1, AddLives).Migrate(2, RenameCoins);
        var (storage, store) = With("""{ "Coins": 4 }""");
        var loaded = store.Load("profile", reflection);
        Assert.AreEqual(SaveLoadStatus.Migrated, loaded.Status);
        Assert.AreEqual(4, loaded.Value!.Gold);
        Assert.IsTrue(store.Save("profile", loaded.Value, reflection));
        string written = storage.Slots["profile"];
        Assert.AreEqual(written, Schema().Write(loaded.Value));
        Assert.AreEqual(SaveLoadStatus.Loaded, store.Load("profile", Schema()).Status);
    }

    // ---- The schema by itself ------------------------------------------------------------------------------------

    [TestMethod]
    public void A_step_is_a_function_of_the_document_and_runs_alone()
    {
        var save = JsonNode.Parse("""{ "Coins": 7, "Lives": 1 }""")!.AsObject();
        RenameCoins(save);
        Assert.AreEqual(7, save["Gold"]!.GetValue<int>());
        Assert.IsFalse(save.ContainsKey("Coins"));
    }

    [TestMethod]
    public void A_schema_reads_and_writes_text_without_a_store()
    {
        var schema = Schema();
        Assert.AreEqual(3, schema.Version);
        var read = schema.Read("""{ "Coins": 7 }""");
        Assert.AreEqual(SaveLoadStatus.Migrated, read.Status);
        Assert.AreEqual(7, read.Value!.Gold);
        Assert.AreEqual(SaveLoadStatus.TooNew, schema.Read("""{ "@version": 4 }""").Status);
        Assert.AreEqual(SaveLoadStatus.Corrupt, schema.Read("nope").Status);
        Assert.IsNotNull(schema.Read("nope").Error);
        Assert.AreEqual(SaveLoadStatus.Loaded, schema.Read(schema.Write(new Profile())).Status);
    }

    [TestMethod]
    public void A_schema_with_a_wrong_declaration_is_a_programming_error()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SaveSchema<Profile>(0, SchemaTypes.Default.Profile));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SaveSchema<Profile>(1, null!));
        var schema = new SaveSchema<Profile>(3, SchemaTypes.Default.Profile).Migrate(1, AddLives);
        Assert.ThrowsExactly<ArgumentNullException>(() => schema.Migrate(2, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => schema.Migrate(0, AddLives), "versions start at 1");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => schema.Migrate(3, AddLives), "no step leaves the current version");
        Assert.ThrowsExactly<ArgumentException>(() => schema.Migrate(1, AddLives), "one step per version");

        // The step from version 2 is missing: every use says so, instead of a player's old save finding out.
        var missing = Assert.ThrowsExactly<InvalidOperationException>(() => schema.Read("""{ "@version": 3 }"""));
        StringAssert.Contains(missing.Message, "version 2");
        Assert.ThrowsExactly<InvalidOperationException>(() => schema.Write(new Profile()));
        var store = new SaveStore(new MemoryStorage());
        Assert.ThrowsExactly<InvalidOperationException>(() => store.Load("profile", schema));
        Assert.ThrowsExactly<InvalidOperationException>(() => store.Save("profile", new Profile(), schema));

        Assert.ThrowsExactly<ArgumentNullException>(() => store.Load("profile", (SaveSchema<Profile>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => store.Save("profile", new Profile(), (SaveSchema<Profile>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Schema().Read(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Schema().Write(null!));
    }

    // ---- Slots ---------------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_store_lists_the_slots_that_hold_a_save()
    {
        var (storage, store) = With(null);
        var written = new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);
        storage.Slots["save-2"] = "{}";
        storage.Slots["save-1"] = """{ "Gold": 1 }""";
        storage.Times["save-1"] = written;
        storage.Slots["settings"] = "{}";
        storage.Slots["save-1.corrupt"] = "{";
        storage.Slots["save-1.v1.backup"] = "{}";
        storage.Slots["not/a slot"] = "{}";

        CollectionAssert.AreEqual(new[] { "save-1", "save-2", "settings" }, store.Slots().Select(s => s.Name).ToArray(), "slots, by name; no copies, no foreign entries");
        var saves = store.Slots("save-");
        CollectionAssert.AreEqual(new[] { "save-1", "save-2" }, saves.Select(s => s.Name).ToArray());
        Assert.AreEqual(written, saves[0].LastWrite);
        Assert.AreEqual(13, saves[0].Size);
        Assert.IsNull(saves[1].LastWrite);

        storage.FailList = true;
        Assert.AreEqual(0, store.Slots().Count, "a medium that fails lists nothing; it does not throw");
        Assert.ThrowsExactly<ArgumentNullException>(() => store.Slots(null!));
        Assert.AreSame(storage, store.Storage);
    }

    [TestMethod]
    public void The_file_storage_lists_its_slot_files()
    {
        string dir = Path.Combine(Path.GetTempPath(), "microengine-slots-" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new FileSaveStorage(dir);
            Assert.AreEqual(0, storage.List().Count, "no folder yet: no slots");
            var store = new SaveStore("game", dir);
            Assert.AreEqual(0, store.Slots().Count);

            var before = DateTimeOffset.UtcNow.AddSeconds(-5);
            store.Save("slot 1", new Profile { Gold = 1 }, SchemaTypes.Default.Profile);
            store.Save("slot 2", new Profile { Gold = 2 }, SchemaTypes.Default.Profile);
            File.WriteAllText(Path.Combine(dir, "broken.json"), "{ nope");
            Assert.IsNull(store.Load("broken", SchemaTypes.Default.Profile));     // set aside as broken.corrupt.json
            File.WriteAllText(Path.Combine(dir, "notes.txt"), "not a slot");
            File.WriteAllText(Path.Combine(dir, "slot 1.json.tmp"), "half a write");

            var slots = store.Slots();
            CollectionAssert.AreEqual(new[] { "slot 1", "slot 2" }, slots.Select(s => s.Name).ToArray());
            Assert.AreEqual(new FileInfo(Path.Combine(dir, "slot 1.json")).Length, slots[0].Size);
            Assert.IsTrue(slots[0].LastWrite >= before && slots[0].LastWrite <= DateTimeOffset.UtcNow.AddSeconds(5), $"{slots[0].LastWrite}");
            Assert.AreEqual(dir, store.Storage.Location);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void A_slot_cannot_take_the_name_of_a_copy()
    {
        var (storage, store) = With(null);
        Assert.ThrowsExactly<ArgumentException>(() => store.Save("profile.v1.backup", new Profile(), Schema()));
        Assert.ThrowsExactly<ArgumentException>(() => store.Load("profile.BACKUP", SchemaTypes.Default.Profile));
        Assert.ThrowsExactly<ArgumentException>(() => store.Exists("a.backup"));
        Assert.ThrowsExactly<ArgumentException>(() => store.Delete("a.backup"));
        Assert.AreEqual(0, storage.Slots.Count);
        Assert.IsTrue(store.Save("backup", new Profile(), Schema()), "only the ending is reserved");
    }
}
