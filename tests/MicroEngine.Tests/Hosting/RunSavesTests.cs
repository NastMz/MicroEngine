using System.Text.Json.Serialization;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Hosting;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Hosting;

/// <summary>
/// Where a run keeps the game's saves. A run nobody plays (a script, deterministic time, a game stepped by a test)
/// has a store of its own, in memory and empty at start, so it neither reads nor writes the player's saves and two
/// such runs start from the same state. The run can name a medium (<c>--saves DIR</c>, <see cref="RunOptions.Saves"/>),
/// the composition's medium wins over the run's own store, and a run somebody plays is where it always was.
/// </summary>
/// <remarks>
/// Every game here has a save id no real game has, and no test saves through the platform's default medium: a test
/// that fails leaves at most an empty-named folder of its own, which <see cref="RemoveDefaultFolder"/> deletes.
/// </remarks>
[TestClass]
public sealed partial class RunSavesTests
{
    public sealed class Progress { public int Launches { get; set; } }

    [JsonSerializable(typeof(Progress))]
    private sealed partial class RunSaveTypes : JsonSerializerContext;

    private sealed class Idle : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    /// <summary>Counts its launches in a slot: what it found at start is what the run started from.</summary>
    private class CountingGame(string id) : Game
    {
        public int FoundAtStart = -1;
        public override GameSettings Settings { get; } = new("Run saves");
        protected override string SaveId => id;
        public string Id => id;
        protected override void Load()
        {
            FoundAtStart = Saves.Load("progress", RunSaveTypes.Default.Progress)?.Launches ?? 0;
            Assert.IsTrue(Saves.Save("progress", new Progress { Launches = FoundAtStart + 1 }, RunSaveTypes.Default.Progress));
            Scenes.Push(new Idle());
        }
    }

    private sealed class EagerGame : CountingGame
    {
        public EagerGame(string id) : base(id) => _ = Saves;
    }

    private readonly string _id = "microengine-run-saves-test-" + Guid.NewGuid().ToString("N");
    private string _folder = "";

    [TestInitialize]
    public void CreateFolder() => _folder = Directory.CreateTempSubdirectory("microengine-run-saves-").FullName;

    [TestCleanup]
    public void RemoveFolders()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        RemoveDefaultFolder();
    }

    private void RemoveDefaultFolder()
    {
        if (Directory.Exists(SaveStore.DefaultDirectory(_id))) Directory.Delete(SaveStore.DefaultDirectory(_id), true);
    }

    private static InputScript Script => InputScript.Parse("2 press confirm\n4 quit\n");

    private static RunResult Run(Game game, RunOptions options, RunPreset? preset = null)
    {
        using var platform = preset is null ? new HeadlessPlatform() : new HeadlessPlatform { Preset = preset };
        var result = GameRunner.Run(game, platform, options);
        Assert.IsNull(result.Error, result.Error?.ToString());
        return result;
    }

    private GameHost Host(string[] args, Func<Game> create, Func<string, IStorage>? storage = null) =>
        GameHost.Create(new HostEnvironment(HostKind.Test) { Out = new StringWriter(), Error = new StringWriter() }, LaunchRequest.Parse(args, _ => true),
            h => { h.Storage = storage; h.UseGame(create).UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile)); });

    [TestMethod]
    public void A_scripted_run_keeps_its_saves_in_a_store_of_its_own()
    {
        // Real time, as "--script x --time realtime" asks: the script alone makes the run one nobody plays.
        var game = new CountingGame(_id);
        Run(game, new RunOptions { Script = Script, TimeMode = TimeMode.RealTime, Mute = true, CaptureDirectory = _folder }, new RunPreset());

        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)), "nothing was written where the player's saves are");
        Assert.AreNotEqual(SaveStore.DefaultDirectory(_id), game.Saves.Directory, "and nothing is read from there");
        Assert.IsInstanceOfType<MemorySaveStorage>(game.Saves.Storage);
        Assert.AreEqual("memory", game.Saves.Directory);
        Assert.AreEqual(0, game.FoundAtStart, "empty at start");
        Assert.AreEqual(1, game.Saves.Load("progress", RunSaveTypes.Default.Progress)!.Launches, "what the run saved is there for the run");
    }

    [TestMethod]
    public void A_run_in_deterministic_time_keeps_its_saves_in_a_store_of_its_own()
    {
        // The run's choice ("--time deterministic").
        var chosen = new CountingGame(_id);
        Run(chosen, new RunOptions { TimeMode = TimeMode.Deterministic, MaxUpdates = 3 }, new RunPreset());
        Assert.IsInstanceOfType<MemorySaveStorage>(chosen.Saves.Storage);

        // The platform's preset (the headless platform's, as "--headless" and tests get it).
        var preset = new CountingGame(_id);
        Run(preset, new RunOptions { MaxUpdates = 3 });
        Assert.IsInstanceOfType<MemorySaveStorage>(preset.Saves.Storage);
        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)));
    }

    [TestMethod]
    public void Two_scripted_runs_in_a_row_start_from_the_same_state()
    {
        var first = new CountingGame(_id);
        var second = new CountingGame(_id);
        Run(first, new RunOptions { Script = Script, Mute = true, CaptureDirectory = _folder });
        Run(second, new RunOptions { Script = Script, Mute = true, CaptureDirectory = _folder });

        Assert.AreEqual(0, first.FoundAtStart);
        Assert.AreEqual(0, second.FoundAtStart, "the second run found nothing of the first");
        Assert.AreNotSame(first.Saves.Storage, second.Saves.Storage);
    }

    [TestMethod]
    public void A_run_somebody_plays_uses_the_platforms_default_medium()
    {
        // Real time and no script: the store is the player's. Asking where it is writes nothing.
        var game = new ProbeGame(_id);
        Run(game, new RunOptions { TimeMode = TimeMode.RealTime, MaxUpdates = 2 }, new RunPreset());

        Assert.AreEqual(SaveStore.DefaultDirectory(_id), game.Saves.Directory);
        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)), "the test saved nothing there");
    }

    private sealed class ProbeGame(string id) : CountingGame(id)
    {
        protected override void Load()
        {
            _ = Saves;
            Scenes.Push(new Idle());
        }
    }

    [TestMethod]
    public void The_saves_option_names_the_folder_a_scripted_run_reads_and_writes()
    {
        string scriptPath = Path.Combine(_folder, "run.script");
        File.WriteAllText(scriptPath, "2 press confirm\n4 quit\n");
        string saves = Path.Combine(_folder, "given");
        string[] args = ["--script", scriptPath, "--headless", "--mute", "--captures", _folder, "--saves", saves];

        CountingGame? first = null, second = null;
        Assert.IsNull(Host(args, () => first = new CountingGame(_id)).Play().Error);
        Assert.IsNull(Host(args, () => second = new CountingGame(_id)).Play().Error);

        Assert.AreEqual(0, first!.FoundAtStart);
        Assert.AreEqual(1, second!.FoundAtStart, "the second run started from what the first one saved");
        Assert.AreEqual(Path.GetFullPath(saves), second.Saves.Directory);
        Assert.IsTrue(File.Exists(Path.Combine(saves, "progress.json")), "and the saves are there to look at");
        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)));
    }

    [TestMethod]
    public void The_saves_option_serves_a_run_somebody_plays()
    {
        string saves = Path.Combine(_folder, "second-profile");
        ProbeGame? game = null;
        var host = Host(["--platform", "headless", "--time", "realtime", "--frames", "2", "--saves", saves], () => game = new ProbeGame(_id));
        Assert.IsNull(host.Play().Error);
        Assert.AreEqual(Path.GetFullPath(saves), game!.Saves.Directory);
    }

    [TestMethod]
    public void The_launch_parses_the_saves_option()
    {
        Assert.AreEqual("out/saves", LaunchRequest.Parse(["--saves", "out/saves"]).SaveDirectory);
        Assert.IsNull(LaunchRequest.Parse([]).SaveDirectory);
        Assert.AreEqual("out/saves", LaunchArguments.Parse(["--saves", "out/saves"]).SaveDirectory);
        StringAssert.Contains(Assert.ThrowsExactly<FormatException>(() => LaunchRequest.Parse(["--saves"])).Message, "--saves");
        StringAssert.Contains(LaunchRequest.Usage, "[--saves DIR]");
    }

    [TestMethod]
    public void The_compositions_medium_wins_over_the_runs_own_store_and_the_saves_option_over_both()
    {
        var composed = new MemorySaveStorage();
        CountingGame? game = null;
        Assert.IsNull(Host(["--headless", "--frames", "2"], () => game = new CountingGame(_id), _ => composed).Play().Error);
        Assert.AreSame(composed, game!.Saves.Storage, "a composition that names a medium decides for its scripted runs too");

        string saves = Path.Combine(_folder, "named");
        Assert.IsNull(Host(["--headless", "--frames", "2", "--saves", saves], () => game = new CountingGame(_id), _ => composed).Play().Error);
        Assert.AreEqual(Path.GetFullPath(saves), game.Saves.Directory, "the launch's folder is the most explicit choice");
    }

    [TestMethod]
    public void A_game_stepped_by_a_test_has_a_store_of_its_own_or_the_one_the_test_hands_it()
    {
        var alone = new CountingGame(_id);
        alone.InitializeForTest(new HeadlessPlatform());
        Assert.IsInstanceOfType<MemorySaveStorage>(alone.Saves.Storage);
        Assert.AreEqual(0, alone.FoundAtStart);

        // A relaunch over the same saves: both games get the same medium.
        var shared = new MemorySaveStorage();
        var first = new CountingGame(_id);
        first.InitializeForTest(new HeadlessPlatform(), saves: shared);
        var relaunched = new CountingGame(_id);
        relaunched.InitializeForTest(new HeadlessPlatform(), saves: shared);
        Assert.AreSame(shared, relaunched.Saves.Storage);
        Assert.AreEqual(1, relaunched.FoundAtStart);
        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)));
    }

    [TestMethod]
    public void A_game_that_used_its_saves_before_such_a_run_is_told()
    {
        // The store already exists on the platform's default medium: the run cannot keep its saves to itself.
        var game = new EagerGame(_id);
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => game.InitializeForTest(new HeadlessPlatform()));
        StringAssert.Contains(error.Message, "before its run began");

        var named = new EagerGame(_id);
        Assert.ThrowsExactly<InvalidOperationException>(() => named.InitializeForTest(new HeadlessPlatform(), saves: new MemorySaveStorage()));

        // A store the game built by hand is the game's own: the run leaves it.
        var own = new SaveStore(new MemorySaveStorage());
        var builder = new OwnStoreGame(_id, own);
        builder.InitializeForTest(new HeadlessPlatform(), saves: new MemorySaveStorage());
        Assert.AreSame(own, builder.Saves);
    }

    private sealed class OwnStoreGame(string id, SaveStore store) : CountingGame(id)
    {
        protected override SaveStore CreateSaves() => store;
    }

    [TestMethod]
    public void The_memory_storage_keeps_slots_as_the_file_storage_does()
    {
        var memory = new MemorySaveStorage();
        Assert.IsNull(memory.Read("progress"));
        Assert.IsFalse(memory.Exists("progress"));
        Assert.AreEqual(0, memory.List().Count);

        memory.Write("progress", "{\"é\":1}");
        Assert.AreEqual("{\"é\":1}", memory.Read("progress"));
        Assert.IsTrue(memory.Exists("PROGRESS"), "names fold case, as the folders of Windows and macOS do");
        var listed = memory.List().Single();
        Assert.AreEqual(new SlotInfo("progress", 8, null), listed, "the size a file would have, and no time of writing");

        memory.SetAside("progress");
        Assert.IsFalse(memory.Exists("progress"));
        Assert.AreEqual("{\"é\":1}", memory.ReadSetAside("progress"), "what was set aside is kept");
        Assert.AreEqual(0, memory.List().Count, "and is not a slot");
        memory.SetAside("missing");

        memory.Write("settings", "{}");
        memory.Delete("settings");
        memory.Delete("settings");
        Assert.IsFalse(memory.Exists("settings"));

        // The names the file storage refuses are refused here, so a slot that works in a test works on a disk.
        foreach (var call in new Action[]
        {
            () => memory.Read("../elsewhere"), () => memory.Write("a/b", ""), () => memory.Exists("CON"),
            () => memory.Delete("x.corrupt"), () => memory.SetAside(""), () => memory.ReadSetAside("a\\b"),
        })
            Assert.ThrowsExactly<ArgumentException>(call);
        Assert.ThrowsExactly<ArgumentNullException>(() => memory.Write("progress", null!));

        // A store on it behaves as on any medium.
        var store = new SaveStore(memory);
        Assert.IsTrue(store.Save("progress", new Progress { Launches = 3 }, RunSaveTypes.Default.Progress));
        Assert.AreEqual(3, store.Load("progress", RunSaveTypes.Default.Progress)!.Launches);
    }
}
