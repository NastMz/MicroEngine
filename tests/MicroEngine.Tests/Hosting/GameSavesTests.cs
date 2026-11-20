using System.Text.Json.Serialization;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Hosting;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Hosting;

/// <summary>
/// A game reaches its saves through <see cref="Game.Saves"/>: one store on the medium the composition gives
/// (<see cref="HostBuilder.Storage"/>), else the platform's default, under the game's save id. A store built by hand is
/// the explicit alternative (<see cref="Game.CreateSaves"/>).
/// </summary>
[TestClass]
public sealed partial class GameSavesTests
{
    public sealed class Progress { public int Level { get; set; } = 1; }

    [JsonSerializable(typeof(Progress))]
    private sealed partial class GameSaveTypes : JsonSerializerContext;

    private sealed class MemoryStorage(string location) : IStorage
    {
        public Dictionary<string, string> Slots { get; } = [];
        public string Location => location;
        public string? Read(string slot) => Slots.GetValueOrDefault(slot);
        public void Write(string slot, string text) => Slots[slot] = text;
        public bool Exists(string slot) => Slots.ContainsKey(slot);
        public void Delete(string slot) => Slots.Remove(slot);
        public void SetAside(string slot) => Slots.Remove(slot);
        public IReadOnlyList<SlotInfo> List() => [.. Slots.Select(s => new SlotInfo(s.Key, s.Value.Length, null))];
    }

    private sealed class Idle : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    private class SavingGame : Game
    {
        public override GameSettings Settings { get; } = new("Saving: The Game?");
        public string Id => SaveId;
        protected override void Load() => Scenes.Push(new Idle());
    }

    private sealed class NamedGame : SavingGame
    {
        protected override string SaveId => "com.example.named";
    }

    private sealed class OwnStoreGame(SaveStore store) : SavingGame
    {
        protected override SaveStore CreateSaves() => store;
    }

    private sealed class EagerGame : SavingGame
    {
        public EagerGame() => _ = Saves;
    }

    private sealed class TrackingGame : SavingGame
    {
        public static readonly Achievement[] Catalog = [new("first", "First", "Do it once")];
        public override void Configure(EngineBuilder builder) => TrackAchievements(Catalog);
    }

    private static GameHost Host(Func<Game> create, Func<string, IStorage>? storage) =>
        GameHost.Create(new HostEnvironment(HostKind.Test) { Out = new StringWriter(), Error = new StringWriter() }, new LaunchRequest { Headless = true },
            h => { h.Storage = storage; h.UseGame(create); });

    [TestMethod]
    public void A_hosted_game_saves_on_the_compositions_medium_under_its_save_id()
    {
        var media = new Dictionary<string, MemoryStorage>();
        var host = Host(() => new NamedGame(), id => media[id] = new MemoryStorage(id));
        var game = host.CreateGame();

        Assert.AreEqual("com.example.named", game.Saves.Directory, "the composition was asked for the game's save id");
        var store = game.Saves;
        Assert.AreSame(store, game.Saves, "one store per game");
        Assert.IsTrue(game.Saves.Save("progress", new Progress { Level = 4 }, GameSaveTypes.Default.Progress));
        Assert.IsTrue(media["com.example.named"].Slots.ContainsKey("progress"));
        Assert.AreSame(media["com.example.named"], game.Saves.Storage);
    }

    [TestMethod]
    public void Without_a_composition_medium_the_store_is_the_platforms_default()
    {
        var hosted = Host(() => new NamedGame(), storage: null).CreateGame();
        Assert.AreEqual(SaveStore.DefaultDirectory("com.example.named"), hosted.Saves.Directory);

        var alone = new NamedGame();
        Assert.AreEqual(SaveStore.DefaultDirectory("com.example.named"), alone.Saves.Directory, "a game created without a host, as tests do");
    }

    [TestMethod]
    public void The_save_id_defaults_to_the_application_id_else_the_title_as_a_folder_name()
    {
        // The test project declares an application id, like a game's project does.
        var game = new SavingGame();
        Assert.AreEqual(AppIdentity.Of(game)!.Id, game.Id);
        Assert.AreEqual(SaveStore.DefaultDirectory(game.Id), game.Saves.Directory);

        // A project without one: the title names the folder, and what a folder name cannot hold is replaced.
        Assert.AreEqual("com.example.game", Game.DefaultSaveId("com.example.game", "Saving: The Game?"));
        Assert.AreEqual("Saving_ The Game_", Game.DefaultSaveId(null, "Saving: The Game?"));
        Assert.AreEqual("a_b_c", Game.DefaultSaveId(null, " a/b\\c. "));
        Assert.AreEqual("game", Game.DefaultSaveId(null, " .. "));
    }

    [TestMethod]
    public void A_game_can_build_its_own_store()
    {
        var own = new SaveStore(new MemoryStorage("own"));
        var asked = new List<string>();
        var game = Host(() => new OwnStoreGame(own), id => { asked.Add(id); return new MemoryStorage(id); }).CreateGame();
        Assert.AreSame(own, game.Saves);
        Assert.AreEqual(0, asked.Count, "the composition's medium is not asked for");
    }

    [TestMethod]
    public void Using_the_saves_in_the_constructor_of_a_hosted_game_says_so()
    {
        // The store would have been created before the host could hand over its medium, and would miss it silently.
        var host = Host(() => new EagerGame(), id => new MemoryStorage(id));
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => host.CreateGame());
        StringAssert.Contains(error.Message, "constructor");

        _ = Host(() => new EagerGame(), storage: null).CreateGame(); // no medium to miss: nothing to report
    }

    [TestMethod]
    public void Achievements_declared_without_a_store_use_the_games_saves()
    {
        var storage = new MemoryStorage("memory");
        var host = Host(() => new TrackingGame(), _ => storage);
        var game = host.CreateGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        Assert.IsTrue(game.Achievements.Unlock("first"));
        Assert.IsTrue(storage.Slots.ContainsKey(AchievementTracker.SaveSlot), "the achievements are kept where the game's saves are");
        Assert.AreSame(game.Saves, game.Services.Get<SaveStore>(), "and the engine's store service is the game's store");
    }
}
