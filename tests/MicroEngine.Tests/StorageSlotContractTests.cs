using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroEngine.Tests;

/// <summary>
/// Who checks a slot name. A storage owns what its names can be: the file medium refuses whatever is not a plain file
/// name, so nothing reaches outside its folder whoever calls it; a game's own storage keeps the names it understands.
/// The callers that take a slot from the game (<see cref="InputMap.SaveBindings"/> and
/// <see cref="InputMap.LoadBindings"/>) refuse only a slot with no name, and let the storage's own answer through.
/// </summary>
[TestClass]
public sealed class StorageSlotContractTests
{
    private sealed class MemoryStorage : IStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public int Calls;
        public Exception? Failure;
        public string Location => "memory";
        public string? Read(string slot) { Touch(); return Slots.GetValueOrDefault(slot); }
        public void Write(string slot, string text) { Touch(); Slots[slot] = text; }
        public bool Exists(string slot) { Touch(); return Slots.ContainsKey(slot); }
        public void Delete(string slot) { Touch(); Slots.Remove(slot); }
        public void SetAside(string slot) { Calls++; Slots.Remove(slot); }
        private void Touch()
        {
            Calls++;
            if (Failure is not null) throw Failure;
        }
    }

    private string _root = "";
    private string _saves = "";

    [TestInitialize]
    public void CreateFolders()
    {
        _root = Path.Combine(Path.GetTempPath(), "microengine-storage-" + Guid.NewGuid().ToString("N"));
        _saves = Path.Combine(_root, "saves");
        Directory.CreateDirectory(_saves);
    }

    [TestCleanup]
    public void RemoveFolders()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private static readonly (string Name, Action<InputMap, IStorage, string> Call)[] BindingCalls =
    [
        ("SaveBindings", (map, storage, slot) => map.SaveBindings(storage, slot)),
        ("LoadBindings", (map, storage, slot) => map.LoadBindings(storage, slot)),
    ];

    [TestMethod]
    public void BindingsRefuseASlotWithNoNameBeforeTheStorageIsTouched()
    {
        foreach (var (name, call) in BindingCalls)
        {
            var storage = new MemoryStorage();
            var missing = Assert.ThrowsExactly<ArgumentNullException>(() => call(InputMap.CreateDefault(), storage, null!), name);
            Assert.AreEqual("slot", missing.ParamName, name);
            foreach (var slot in new[] { "", " ", "\t" })
            {
                var empty = Assert.ThrowsExactly<ArgumentException>(() => call(InputMap.CreateDefault(), storage, slot), $"{name} with slot \"{slot}\"");
                Assert.AreEqual("slot", empty.ParamName, name);
            }
            Assert.AreEqual(0, storage.Calls, $"{name} reached the storage");
        }
    }

    [TestMethod]
    public void BindingsOverTheFileMediumNeverReachOutsideTheSaveFolderAndReportTheNameAsAnArgumentError()
    {
        var outside = Path.Combine(_root, "up.json");
        var absolute = Path.Combine(_root, "absolute");
        var saved = InputMap.CreateDefault().ExportBindings().ToJson();
        foreach (var slot in new[] { "../up", "..\\up", "sub/../../up", absolute, "..", "sub/name", "NUL", "name.", "a:b" })
        {
            foreach (var (name, call) in BindingCalls)
            {
                File.WriteAllText(outside, saved);
                File.WriteAllText(absolute + ".json", saved);
                var e = Assert.ThrowsExactly<ArgumentException>(() => call(InputMap.CreateDefault(), new FileSaveStorage(_saves), slot), $"{name} with slot \"{slot}\"");
                Assert.AreEqual("slot", e.ParamName, name);
                Assert.AreEqual(saved, File.ReadAllText(outside), $"{name} with slot \"{slot}\" changed a file outside the save folder");
                Assert.AreEqual(saved, File.ReadAllText(absolute + ".json"), $"{name} with slot \"{slot}\" changed a file outside the save folder");
                Assert.AreEqual(0, Directory.GetFileSystemEntries(_saves).Length, $"{name} with slot \"{slot}\" left something in the save folder");
            }
        }
    }

    [TestMethod]
    public void BindingsKeepTheNamesAGamesOwnStorageUnderstands()
    {
        // A storage that is not a folder of files (a cloud service, a key-value store) may use names the file medium
        // refuses; the input map is not the one to forbid them.
        var storage = new MemoryStorage();
        foreach (var slot in new[] { "profiles/1/bindings", "player:2", "a..b ", InputMap.BindingsSlot })
        {
            InputMap.CreateDefault().Unbind("confirm").Bind("confirm", Key.Q).SaveBindings(storage, slot);
            Assert.IsTrue(storage.Slots.ContainsKey(slot), slot);
            var loaded = InputMap.CreateDefault();
            Assert.IsTrue(loaded.LoadBindings(storage, slot), slot);
            CollectionAssert.AreEqual(new[] { Key.Q }, loaded.GetBindings("confirm")!.Keys.ToArray(), slot);
        }
    }

    [TestMethod]
    public void BindingsLetAStorageFailureThroughAndKeepTheCurrentBindings()
    {
        foreach (var failure in new Exception[] { new IOException("full"), new UnauthorizedAccessException("read-only") })
        {
            var storage = new MemoryStorage { Failure = failure };
            var map = InputMap.CreateDefault();
            Assert.AreSame(failure, Assert.Throws<Exception>(() => map.SaveBindings(storage)));
            Assert.AreSame(failure, Assert.Throws<Exception>(() => map.LoadBindings(storage)));
            CollectionAssert.AreEqual(InputMap.CreateDefault().GetBindings("confirm")!.Keys.ToArray(), map.GetBindings("confirm")!.Keys.ToArray());
        }
    }

    [TestMethod]
    public void TheBindingsSlotIsAPlainNameSoItIsTheSameFileOrKeyOnEverySystem()
    {
        var map = InputMap.CreateDefault();
        map.SaveBindings(new FileSaveStorage(_saves));
        Assert.IsTrue(File.Exists(Path.Combine(_saves, "input-bindings.json")));
        Assert.IsTrue(new SaveStore("game", _saves).Exists(InputMap.BindingsSlot), "a store over the same folder accepts the name");
    }

    public sealed class Data { public int Level { get; set; } }

    [TestMethod]
    public void SettingASlotAsideNeverReplacesAnotherSlot()
    {
        // "<slot>.corrupt.json" is where unreadable data of <slot> goes, so no slot may have that file.
        var store = new SaveStore("game", _saves);
        Assert.ThrowsExactly<ArgumentException>(() => store.Save("save.corrupt", new Data { Level = 9 }));
        Assert.ThrowsExactly<ArgumentException>(() => new FileSaveStorage(_saves).Write("save.Corrupt", "{}"));
        Assert.AreEqual(0, Directory.GetFileSystemEntries(_saves).Length);

        // A file an earlier version wrote under such a name stays where it is until the slot beside it is set aside.
        var earlier = Path.Combine(_saves, "other.corrupt.json");
        File.WriteAllText(earlier, "{\"Level\":9}");
        Assert.ThrowsExactly<ArgumentException>(() => store.Load<Data>("other.corrupt"));
        Assert.ThrowsExactly<ArgumentException>(() => store.Delete("other.corrupt"));
        Assert.AreEqual("{\"Level\":9}", File.ReadAllText(earlier));
    }

    [TestMethod]
    public void SettingAsideAgainReplacesTheEarlierCopy()
    {
        var store = new SaveStore("game", _saves);
        var slot = Path.Combine(_saves, "save.json");
        var aside = Path.Combine(_saves, "save.corrupt.json");
        File.WriteAllText(slot, "{ first");
        Assert.IsNull(store.Load<Data>("save"));
        Assert.AreEqual("{ first", File.ReadAllText(aside));
        File.WriteAllText(slot, "{ second");
        Assert.IsNull(store.Load<Data>("save"));
        Assert.AreEqual("{ second", File.ReadAllText(aside), "the latest unreadable data is the copy kept");
        Assert.IsFalse(File.Exists(slot));
        CollectionAssert.AreEqual(new[] { aside }, Directory.GetFiles(_saves));
    }

#pragma warning disable CA1416 // BrowserStorage is for browsers; only what it does before it calls the page is run here.
    [TestMethod]
    public void BrowserStorageRefusesASlotWithNoNameBeforeItCallsThePage()
    {
        var storage = new BrowserStorage("game");
        (string Name, Action<string> Call)[] calls =
        [
            ("Read", slot => storage.Read(slot)), ("Write", slot => storage.Write(slot, "{}")), ("Exists", slot => storage.Exists(slot)),
            ("Delete", slot => storage.Delete(slot)), ("SetAside", slot => storage.SetAside(slot)),
        ];
        foreach (var (name, call) in calls)
        {
            var missing = Assert.ThrowsExactly<ArgumentNullException>(() => call(null!), name);
            Assert.AreEqual("slot", missing.ParamName, name);
            var empty = Assert.ThrowsExactly<ArgumentException>(() => call(""), name);
            Assert.AreEqual("slot", empty.ParamName, name);
        }
    }

    [TestMethod]
    public void BrowserStorageKeepsEachGamesSlotsUnderItsOwnKeyPrefix()
    {
        Assert.AreEqual("localStorage:microengine/com.example.game/", new BrowserStorage("com.example.game").Location);
        Assert.AreEqual("microengine/com.example.game/progress", BrowserStorage.Key("microengine/com.example.game/", "progress"));
        Assert.AreEqual("microengine/com.example.game/profile.v2", BrowserStorage.Key("microengine/com.example.game/", "profile.v2"));
        // A browser key is one flat text: nothing in a slot shortens or leaves the prefix.
        foreach (var slot in new[] { "../other/progress", "..", "/rooted", "a/b", "\\x" })
            Assert.AreEqual("microengine/game/" + slot, BrowserStorage.Key("microengine/game/", slot), slot);
    }
#pragma warning restore CA1416
}
