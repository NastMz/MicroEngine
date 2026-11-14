using System.Text.Json.Serialization;
using MicroEngine.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroEngine.Tests;

/// <summary>
/// A slot is a name, not a path: nothing a game passes as a slot may reach a file outside the save folder, and a name
/// that one supported system cannot store is refused on all of them, so saves stay portable. The names games use
/// (letters, digits, dashes, underscores, dots and spaces inside the name) keep resolving to the same file.
/// </summary>
[TestClass]
public sealed partial class SaveSlotNameTests
{
    public sealed class Data { public int Level { get; set; } }

    [JsonSerializable(typeof(Data))]
    private sealed partial class SlotTypes : JsonSerializerContext;

    private sealed class MemoryStorage : ISaveStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public int Calls;
        public string Location => "memory";
        public string? Read(string slot) { Calls++; return Slots.GetValueOrDefault(slot); }
        public void Write(string slot, string text) { Calls++; Slots[slot] = text; }
        public bool Exists(string slot) { Calls++; return Slots.ContainsKey(slot); }
        public void Delete(string slot) { Calls++; Slots.Remove(slot); }
        public void SetAside(string slot) { Calls++; Slots.Remove(slot); }
    }

    private string _root = "";
    private string _saves = "";

    [TestInitialize]
    public void CreateFolders()
    {
        _root = Path.Combine(Path.GetTempPath(), "microengine-slots-" + Guid.NewGuid().ToString("N"));
        _saves = Path.Combine(_root, "saves");
        Directory.CreateDirectory(_saves);
    }

    [TestCleanup]
    public void RemoveFolders()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string[] FilesOutsideTheSaveFolder() =>
        [.. Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Where(f => !f.StartsWith(_saves + Path.DirectorySeparatorChar, StringComparison.Ordinal))];

    /// <summary>Every call of the store that takes a slot, by name.</summary>
    private static readonly (string Name, Action<SaveStore, string> Call)[] EntryPoints =
    [
        ("Save(slot, data)", (s, slot) => s.Save(slot, new Data())),
        ("Save(slot, data, typeInfo)", (s, slot) => s.Save(slot, new Data(), SlotTypes.Default.Data)),
        ("Load<T>(slot)", (s, slot) => s.Load<Data>(slot)),
        ("Load(slot, typeInfo)", (s, slot) => s.Load(slot, SlotTypes.Default.Data)),
        ("Exists(slot)", (s, slot) => s.Exists(slot)),
        ("Delete(slot)", (s, slot) => s.Delete(slot)),
    ];

    private static readonly string[] Invalid =
    [
        "", " ", "\t", "..", ".", "../up", "..\\up", "a/../../up", "sub/name", "sub\\name", "/rooted", "\\rooted", "C:\\Temp\\up", "C:up", "D:",
        "a:b", "a*b", "a?b", "a\"b", "a<b", "a>b", "a|b", "a\nb", "a\0b", "name.", "name ", "name..",
        "CON", "con", "NUL", "PRN", "AUX", "COM1", "com9", "LPT1", "COM\u00b9", "CONIN$", "CONOUT$", "NUL.txt", "con.backup.v2", "aux .x",
        "\ud800",
    ];

    private static readonly string[] Valid =
    [
        "progress", "adventure", "record", "achievements", "input-bindings", "example", "profile", "profile.v0", "profile.v12", "slot_1", "Slot 2", " lead", "save.corrupt",
        "partida-ñ", "セーブ", ".hidden", "a..b", "console", "COM10", "LPT", "nul1", "com", "CONSOLE.v1", "a'b", "a(b)", "a+b,c;d=e@f#g$h%i&j!k~l`m^n{o}p[q]r",
    ];

    [TestMethod]
    public void ASlotThatIsNotAPlainNameIsRefusedByEveryCallBeforeTheMediumIsTouched()
    {
        foreach (var slot in Invalid)
        {
            foreach (var (name, call) in EntryPoints)
            {
                var storage = new MemoryStorage();
                var store = new SaveStore(storage);
                var e = Assert.ThrowsExactly<ArgumentException>(() => call(store, slot), $"{name} with slot \"{Show(slot)}\"");
                Assert.AreEqual("slot", e.ParamName, name);
                StringAssert.Contains(e.Message, slot, $"{name}: the message names the slot");
                Assert.AreEqual(0, storage.Calls, $"{name} with slot \"{Show(slot)}\" reached the medium");
            }
        }
    }

    [TestMethod]
    public void ANullSlotIsRefusedByEveryCall()
    {
        foreach (var (name, call) in EntryPoints)
        {
            var storage = new MemoryStorage();
            var e = Assert.ThrowsExactly<ArgumentNullException>(() => call(new SaveStore(storage), null!), name);
            Assert.AreEqual("slot", e.ParamName, name);
            Assert.AreEqual(0, storage.Calls, name);
        }
    }

    [TestMethod]
    public void NoSlotWritesReadsOrDeletesOutsideTheSaveFolder()
    {
        var outside = Path.Combine(_root, "up.json");
        var absolute = Path.Combine(_root, "absolute");
        string[] escapes = ["../up", "..\\up", "sub/../../up", absolute, Path.GetFileName(_saves) + "/../../up", ".."];
        foreach (var slot in escapes)
        {
            foreach (var (name, call) in EntryPoints)
            {
                File.WriteAllText(outside, "{\"Level\":41}");
                File.WriteAllText(absolute + ".json", "{\"Level\":42}");
                var store = new SaveStore("game", _saves);
                Assert.Throws<ArgumentException>(() => call(store, slot), $"{name} with slot \"{slot}\"");
                Assert.AreEqual("{\"Level\":41}", File.ReadAllText(outside), $"{name} with slot \"{slot}\" changed a file outside the save folder");
                Assert.AreEqual("{\"Level\":42}", File.ReadAllText(absolute + ".json"), $"{name} with slot \"{slot}\" changed a file outside the save folder");
                CollectionAssert.AreEquivalent(new[] { outside, absolute + ".json" }, FilesOutsideTheSaveFolder(), $"{name} with slot \"{slot}\"");
                Assert.AreEqual(0, Directory.GetFileSystemEntries(_saves).Length, $"{name} with slot \"{slot}\" left something in the save folder");
            }
        }
    }

    [TestMethod]
    public void TheFileMediumRefusesTheSameNamesWhenItIsUsedWithoutAStore()
    {
        var outside = Path.Combine(_root, "up.json");
        File.WriteAllText(outside, "kept");
        var files = new FileSaveStorage(_saves);
        (string Name, Action<string> Call)[] calls =
        [
            ("Read", slot => files.Read(slot)), ("Write", slot => files.Write(slot, "{}")), ("Exists", slot => files.Exists(slot)),
            ("Delete", slot => files.Delete(slot)), ("SetAside", slot => files.SetAside(slot)),
        ];
        foreach (var slot in new[] { "../up", "..\\up", "", "sub/name", "NUL", "name." })
        {
            foreach (var (name, call) in calls)
            {
                var e = Assert.ThrowsExactly<ArgumentException>(() => call(slot), $"{name} with slot \"{slot}\"");
                Assert.AreEqual("slot", e.ParamName);
            }
        }
        Assert.AreEqual("kept", File.ReadAllText(outside));
        CollectionAssert.AreEquivalent(new[] { outside }, FilesOutsideTheSaveFolder());
        Assert.AreEqual(0, Directory.GetFileSystemEntries(_saves).Length);
    }

    [TestMethod]
    public void TheNamesGamesUseKeepTheirFile()
    {
        var store = new SaveStore("game", _saves);
        foreach (var slot in Valid)
        {
            Assert.IsFalse(store.Exists(slot), slot);
            Assert.IsNull(store.Load(slot, SlotTypes.Default.Data), slot);
            Assert.IsTrue(store.Save(slot, new Data { Level = 7 }, SlotTypes.Default.Data), slot);
            var file = Path.Combine(_saves, slot + ".json");
            Assert.IsTrue(File.Exists(file), $"slot \"{slot}\" is the file {slot}.json in the save folder");
            Assert.AreEqual(7, store.Load<Data>(slot)!.Level, slot);
            Assert.IsTrue(store.Save(slot, new Data { Level = 8 }), slot);
            Assert.AreEqual(8, store.Load(slot, SlotTypes.Default.Data)!.Level, slot);
            Assert.IsTrue(store.Exists(slot), slot);
            store.Delete(slot);
            Assert.IsFalse(File.Exists(file), slot);
        }
        Assert.AreEqual(0, FilesOutsideTheSaveFolder().Length);
    }

    private static string Show(string slot) => string.Concat(slot.Select(c => c < ' ' || char.IsSurrogate(c) ? $"\\u{(int)c:x4}" : c.ToString()));
}
