using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Saves;

namespace MicroEngine.Tests.Kernel;

/// <summary>Saves are an optional module: a composition that adds it gets the store as a service; one that does not, nothing.</summary>
[TestClass]
public sealed class SavesModuleTests
{
    private sealed class MemoryStorage : ISaveStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public string Location => "memory";
        public string? Read(string slot) => Slots.GetValueOrDefault(slot);
        public void Write(string slot, string text) => Slots[slot] = text;
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot) => Slots.Remove(slot);
    }

    private sealed class Progress { public int Level { get; set; } }

    [TestMethod]
    public void The_module_registers_its_store_as_a_service()
    {
        var storage = new MemoryStorage();
        var store = new SaveStore(storage);
        var module = new SavesModule(store);
        var engine = new EngineBuilder().Add(module).Build();
        Assert.AreSame(store, module.Store);
        Assert.AreSame(store, engine.Services.Get<SaveStore>());
        Assert.IsTrue(engine.Services.Get<SaveStore>().Save("progress", new Progress { Level = 3 }));
        StringAssert.Contains(storage.Slots["progress"], "\"Level\": 3", "the service is the store the module was given");
        Assert.AreEqual(0, engine.Schedule.Count, "saves run no frame system: they write when the game saves");
    }

    [TestMethod]
    public void The_module_needs_a_store()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SavesModule(null!));
    }

    [TestMethod]
    public void A_composition_without_the_module_has_no_store()
    {
        var engine = new EngineBuilder().Build();
        Assert.IsFalse(engine.Services.Contains<SaveStore>());
    }
}
