using System.Text.Json.Serialization;
using MicroEngine;
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

    /// <summary>
    /// A platform somebody sits in front of, as far as the run can tell: the headless one without what marks it as
    /// a platform nobody plays on (its run preset).
    /// </summary>
    private sealed class PlayedPlatform(HeadlessPlatform inner) : IPlatform
    {
        public IRenderer BeginFrame() => inner.BeginFrame();
        public void EndFrame() => inner.EndFrame();
        public IGraphicsDevice Graphics => inner.Graphics;
        public MicroEngine.Audio.IAudioDevice Audio => inner.Audio;
        public MicroEngine.Input.IInputSource Input => inner.Input;
        public IMonotonicClock Clock => inner.Clock;
        public bool PumpEvents(IEngineEventSink sink) => inner.PumpEvents(sink);
        public ISurface? MainSurface => inner.MainSurface;
        public bool CanBlock => inner.CanBlock;
        public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class
        {
            capability = null;
            return typeof(T) != typeof(RunPreset) && inner.TryGetCapability(out capability);
        }
        public Color Letterbox { get => inner.Letterbox; set => inner.Letterbox = value; }
        public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
        public void ToggleFullscreen() => inner.ToggleFullscreen();
        public void Dispose() => inner.Dispose();
    }

    [TestMethod]
    public void A_run_somebody_plays_uses_the_platforms_default_medium()
    {
        // A window, real time, no script, no end of its own: the store is the player's. Asking where it is writes nothing.
        var game = new ProbeGame(_id);
        using var platform = new PlayedPlatform(new HeadlessPlatform());
        game.InitializeForTest(platform);

        Assert.AreEqual(SaveStore.DefaultDirectory(_id), game.Saves.Directory);
        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)), "the test saved nothing there");
    }

    [TestMethod]
    public void A_headless_run_keeps_its_saves_to_itself_whatever_its_time_mode()
    {
        // "--headless --time realtime": no window is nobody playing, with the wall clock or without.
        var game = new CountingGame(_id);
        game.InitializeForTest(new HeadlessPlatform { Preset = new RunPreset() });
        Assert.IsInstanceOfType<MemorySaveStorage>(game.Saves.Storage, "a headless platform that states no time mode");

        var chosen = new CountingGame(_id);
        Run(chosen, new RunOptions { TimeMode = TimeMode.RealTime, Script = Script, Mute = true, CaptureDirectory = _folder });
        Assert.IsInstanceOfType<MemorySaveStorage>(chosen.Saves.Storage);
        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)));
    }

    [TestMethod]
    public void A_run_that_ends_by_itself_keeps_its_saves_to_itself()
    {
        // "--frames N" on a window, in real time: a run that stops after N updates is a check, not somebody playing.
        var game = new CountingGame(_id);
        using var platform = new PlayedPlatform(new HeadlessPlatform());
        var result = GameRunner.Run(game, platform, new RunOptions { TimeMode = TimeMode.RealTime, MaxUpdates = 2 });
        Assert.IsNull(result.Error, result.Error?.ToString());

        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)), "nothing was written where the player's saves are");
        Assert.IsInstanceOfType<MemorySaveStorage>(game.Saves.Storage);
    }

    [TestMethod]
    public void The_launch_says_whether_somebody_plays()
    {
        Assert.IsFalse(LaunchRequest.Parse([]).IsUnattended);
        Assert.IsFalse(LaunchRequest.Parse(["--hidden", "--mute", "--report", "--seed", "4"]).IsUnattended, "a hidden window, a report or a seed decide nothing");
        Assert.IsFalse(LaunchRequest.Parse(["--time", "realtime"]).IsUnattended);
        foreach (var args in new string[][]
        {
            ["--script", "x.script"], ["--script", "x.script", "--time", "realtime"], ["--time", "deterministic"], ["--headless"],
            ["--headless", "--time", "realtime"], ["--platform", "Headless"], ["--frames", "3"], ["--hidden", "--frames", "30", "--report"],
        })
            Assert.IsTrue(LaunchRequest.Parse(args, _ => true).IsUnattended, string.Join(' ', args));
    }

    [TestMethod]
    public void A_store_asked_of_the_composition_follows_the_launch()
    {
        SaveStore? asked = null;
        GameHost Compose(string[] args, Func<string, IStorage>? storage = null) =>
            GameHost.Create(new HostEnvironment(HostKind.Test) { Out = new StringWriter(), Error = new StringWriter() }, LaunchRequest.Parse(args, _ => true),
                h =>
                {
                    h.Storage = storage;
                    asked = h.SaveStore(_id);
                    h.UseGame(() => new CountingGame(_id)).UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile));
                });

        // A launch with a script: a store in memory, and the one the game's own Saves are on.
        var host = Compose(["--script", "x.script"]);
        Assert.AreNotEqual(SaveStore.DefaultDirectory(_id), asked!.Directory, "not the player's folder");
        Assert.IsInstanceOfType<MemorySaveStorage>(asked.Storage);
        Assert.AreSame(asked.Storage, host.CreateGame().Saves.Storage, "the same saves as Game.Saves in that launch");
        Assert.AreNotSame(asked.Storage, host.Builder.SaveStore(_id + ".other").Storage, "another game id has its own");

        Compose(["--frames", "3"]);
        Assert.IsInstanceOfType<MemorySaveStorage>(asked.Storage, "a run that ends by itself");

        // The folder the launch names, over the composition's medium too.
        string saves = Path.Combine(_folder, "asked");
        Compose(["--script", "x.script", "--saves", saves], _ => new MemorySaveStorage());
        Assert.AreEqual(Path.GetFullPath(saves), asked.Directory);

        // The composition's medium, in a scripted launch as in any other.
        var composed = new MemorySaveStorage();
        Compose(["--script", "x.script"], _ => composed);
        Assert.AreSame(composed, asked.Storage);

        // A launch somebody plays: the player's saves.
        Compose([]);
        Assert.AreEqual(SaveStore.DefaultDirectory(_id), asked.Directory);
        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)));
    }

    private sealed class CrashingGame(string id) : CountingGame(id)
    {
        protected override void Load() => throw new InvalidOperationException("the level is missing");
    }

    [TestMethod]
    public void The_crash_report_of_a_run_nobody_plays_is_beside_its_output_not_in_the_players_folder()
    {
        string scriptPath = Path.Combine(_folder, "run.script");
        File.WriteAllText(scriptPath, "2 press confirm\n4 quit\n");
        string captures = Path.Combine(_folder, "out");
        var errors = new StringWriter();
        // Where a played run's report goes: the folder of the application id, which is not the save id.
        string players = SaveStore.DefaultDirectory(AppIdentity.Of(new CrashingGame(_id))?.Id ?? "Run saves");
        int Reports() => Directory.Exists(players) ? Directory.GetFiles(players, "crash-*.txt").Length : 0;
        int before = Reports();
        GameHost Compose(string[] args) =>
            GameHost.Create(new HostEnvironment(HostKind.Test) { Out = new StringWriter(), Error = errors }, LaunchRequest.Parse(args),
                h => h.UseGame(() => new CrashingGame(_id)).UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile)));

        // With a script: where its captures go.
        Assert.AreEqual(1, Compose(["--script", scriptPath, "--headless", "--captures", captures]).Run());
        Assert.AreEqual(before, Reports(), "nothing in the player's folder");
        string report = Directory.GetFiles(captures, "crash-*.txt").Single();
        StringAssert.Contains(errors.ToString(), $"crash report: {report}");
        StringAssert.Contains(File.ReadAllText(report), "the level is missing");

        // Without one: the folder the run was started from.
        errors.GetStringBuilder().Clear();
        string started = Environment.CurrentDirectory;
        string work = Directory.CreateDirectory(Path.Combine(_folder, "work")).FullName;
        try
        {
            Environment.CurrentDirectory = work;
            Assert.AreEqual(1, Compose(["--headless", "--frames", "3"]).Run());
        }
        finally { Environment.CurrentDirectory = started; }
        Assert.AreEqual(1, Directory.GetFiles(work, "crash-*.txt").Length, errors.ToString());
        Assert.IsFalse(Directory.Exists(SaveStore.DefaultDirectory(_id)));
        Assert.AreEqual(before, Reports(), "no report was added to the player's folder");
    }

    [TestMethod]
    public void A_saves_option_that_names_no_folder_is_refused_when_the_launch_is_read()
    {
        string file = Path.Combine(_folder, "a-file.json");
        File.WriteAllText(file, "{}");
        foreach (var value in new[] { "", "   ", "sav\0es", file })
        {
            var error = Assert.ThrowsExactly<FormatException>(() => LaunchRequest.Parse(["--saves", value]), $"'{value}'");
            StringAssert.Contains(error.Message, "--saves");
        }
        StringAssert.Contains(Assert.ThrowsExactly<FormatException>(() => LaunchRequest.Parse(["--saves", file])).Message, "is a file");
        Assert.AreEqual(Path.Combine(_folder, "new"), LaunchRequest.Parse(["--saves", Path.Combine(_folder, "new")]).SaveDirectory, "a folder that does not exist yet is created by the first save");
        Assert.AreEqual(_folder, LaunchRequest.Parse(["--saves", _folder]).SaveDirectory);
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
