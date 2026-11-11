#pragma warning disable CS0618 // the engine's banners and list, deprecated, are tested until they are removed
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Tests;

[TestClass]
public sealed class AchievementTests
{
    private static readonly Achievement First = new("first", "First Step", "Do the thing once.");
    private static readonly Achievement Ten = new("ten", "Ten Times", "Do the thing ten times.", Goal: 10);
    private static readonly Achievement[] Catalog = [First, Ten];

    private string _dir = null!;

    [TestInitialize]
    public void Setup() => _dir = Path.Combine(Path.GetTempPath(), "me-ach-" + Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private SaveStore Store(string game = "game") => new(game, Path.Combine(_dir, game));

    /// <summary>Declares its achievements when it is created, before the engine is composed.</summary>
    private sealed class TestGame : Game
    {
        public TestGame(SaveStore store, Achievement[] catalog) => TrackAchievements(catalog, store);
        public override GameSettings Settings { get; } = new("achievements");
        protected override void Load() => Scenes.Push(new Idle());
    }

    /// <summary>Declares its achievements from <see cref="Game.Configure"/>, the other place before composition.</summary>
    private sealed class ConfiguredGame(SaveStore store) : Game
    {
        public override GameSettings Settings { get; } = new("achievements");
        public override void Configure(MicroEngine.Kernel.EngineBuilder builder) => TrackAchievements(Catalog, store);
        protected override void Load() => Scenes.Push(new Idle());
    }

    /// <summary>Asks for achievements only once the engine runs, which is too late to compose them.</summary>
    private sealed class LateGame(SaveStore store) : Game
    {
        public override GameSettings Settings { get; } = new("achievements");
        protected override void Load() => TrackAchievements(Catalog, store);
    }

    private sealed class PlainGame : Game
    {
        public override GameSettings Settings { get; } = new("plain");
        protected override void Load() => Scenes.Push(new Idle());
    }

    [TestMethod]
    public void A_game_that_declares_achievements_gets_the_module_its_store_and_its_flush_system()
    {
        foreach (var game in new Game[] { new TestGame(Store(), Catalog), new ConfiguredGame(Store()) })
        {
            game.InitializeForTest(new HeadlessPlatform());
            Assert.AreSame(game.Achievements, game.Services.Get<AchievementTracker>(), game.GetType().Name);
            Assert.IsTrue(game.Services.Contains<SaveStore>(), "the store achievements persist in is a service too");
            CollectionAssert.AreEqual(new[] { "engine.audio", "engine.achievements", "engine.input.end", "engine.assets.playing" },
                game.Schedule.Order(MicroEngine.Kernel.FramePhase.PostFixed).ToArray(), "the system keeps its place in the step");
        }
    }

    /// <summary>Tracks its achievements and draws its own banners above every scene.</summary>
    private sealed class TrackingGame(SaveStore store) : Game
    {
        public static readonly Color Banner = new(200, 40, 40);
        public List<string> Drawn { get; } = [];
        public override GameSettings Settings { get; } = new("achievements", 32, 16);
        public override void Configure(MicroEngine.Kernel.EngineBuilder builder) => TrackAchievements(Catalog, store);
        protected override void Load() => Scenes.Push(new Filled(Drawn));
        protected override void DrawAboveScenes(IRenderer canvas)
        {
            Drawn.Add("above");
            if (Achievements.IsUnlocked(First.Id)) canvas.FillRect(new RectF(0, 0, canvas.Width, 4), Banner);
        }
    }

    private sealed class Filled(List<string> drawn) : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer)
        {
            drawn.Add("scene");
            renderer.FillRect(new RectF(0, 0, renderer.Width, renderer.Height), Color.Black);
        }
    }

    [TestMethod]
    public void A_game_that_tracks_achievements_keeps_their_progress()
    {
        var store = Store();
        var game = new TrackingGame(store);
        game.InitializeForTest(new HeadlessPlatform());
        Assert.IsTrue(game.HasAchievements);
        Assert.AreSame(game.Achievements, game.Services.Get<AchievementTracker>());
        Assert.IsTrue(game.Schedule.Contains("engine.achievements"), "progress is written by the engine.achievements system");
        game.Achievements.Unlock(First.Id);
        Assert.IsTrue(store.Exists(AchievementTracker.SaveSlot), "the unlock is saved");
    }

    [TestMethod]
    public void A_game_draws_above_every_scene_after_the_scenes()
    {
        var game = new TrackingGame(Store());
        var platform = new HeadlessPlatform(32, 16) { Rasterize = true };
        game.InitializeForTest(platform);
        game.Achievements.Unlock(First.Id);
        game.StepForTest();
        game.Render(platform.BeginFrame());
        platform.EndFrame();
        CollectionAssert.AreEqual(new[] { "scene", "above" }, game.Drawn);
        var frame = ((IViewCapture)platform).CaptureWorld();
        Assert.AreEqual(TrackingGame.Banner, frame[5, 1], "drawn over the scene");
        Assert.AreEqual(Color.Black, frame[5, 10], "the scene below");
    }

    [TestMethod]
    public void A_game_without_achievements_pays_nothing_for_them()
    {
        var game = new PlainGame();
        game.InitializeForTest(new HeadlessPlatform());
        Assert.IsFalse(game.HasAchievements);
        Assert.IsFalse(game.Services.Contains<AchievementTracker>());
        Assert.IsFalse(game.Services.Contains<SaveStore>());
        Assert.IsFalse(game.Schedule.Contains("engine.achievements"), "no flush system");
        Assert.ThrowsExactly<InvalidOperationException>(() => game.Achievements);
    }

    [TestMethod]
    public void Achievements_asked_for_once_the_engine_runs_are_refused_with_the_fix()
    {
        var store = Store();
        var game = new LateGame(store);
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => game.InitializeForTest(new HeadlessPlatform()));
        StringAssert.Contains(error.Message, "constructor");
        StringAssert.Contains(error.Message, "Configure");
        Assert.IsFalse(game.HasAchievements);
        Assert.IsFalse(store.Exists(AchievementTracker.SaveSlot), "nothing was written");
    }

    private sealed class Idle : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    [TestMethod]
    public void Registry_lists_the_catalog_in_order_with_everything_locked()
    {
        var t = new AchievementTracker(Catalog);
        CollectionAssert.AreEqual(new[] { "first", "ten" }, t.All.Select(s => s.Id).ToArray());
        Assert.IsTrue(t.All.All(s => !s.Unlocked && s.Progress == 0));
        Assert.AreEqual(0, t.UnlockedCount);
        Assert.AreEqual(2, t.Count);
        Assert.IsTrue(t.Get("ten").IsProgressive);
        Assert.IsFalse(t.Get("first").IsProgressive);
    }

    [TestMethod]
    public void Registry_rejects_duplicate_ids_empty_ids_and_goals_below_one()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new AchievementTracker([First, First with { Name = "Again" }]));
        Assert.ThrowsExactly<ArgumentException>(() => new AchievementTracker([new Achievement(" ", "x", "y")]));
        Assert.ThrowsExactly<ArgumentException>(() => new AchievementTracker([new Achievement("zero", "x", "y", 0)]));
    }

    [TestMethod]
    public void Unknown_ids_fail_loudly_and_negative_progress_is_rejected()
    {
        var t = new AchievementTracker(Catalog);
        Assert.ThrowsExactly<KeyNotFoundException>(() => t.Unlock("typo"));
        Assert.ThrowsExactly<KeyNotFoundException>(() => t.AddProgress("typo"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => t.AddProgress("ten", -1));
    }

    [TestMethod]
    public void Unlock_happens_once_and_raises_the_event_once()
    {
        var t = new AchievementTracker(Catalog);
        var raised = new List<string>();
        t.Unlocked += a => raised.Add(a.Id);
        Assert.IsTrue(t.Unlock("first"));
        Assert.IsFalse(t.Unlock("first"));
        Assert.IsFalse(t.AddProgress("first"));
        CollectionAssert.AreEqual(new[] { "first" }, raised);
        Assert.IsTrue(t.IsUnlocked("first"));
        Assert.AreEqual(1, t.UnlockedCount);
    }

    [TestMethod]
    public void Progress_accumulates_and_unlocks_exactly_at_the_goal()
    {
        var t = new AchievementTracker(Catalog);
        int raised = 0;
        t.Unlocked += _ => raised++;
        for (int i = 0; i < 9; i++) Assert.IsFalse(t.AddProgress("ten"));
        Assert.AreEqual(9, t.Progress("ten"));
        Assert.AreEqual(0.9f, t.Get("ten").Fraction, 1e-4f);
        Assert.IsFalse(t.IsUnlocked("ten"));
        Assert.IsTrue(t.AddProgress("ten"));
        Assert.IsFalse(t.AddProgress("ten", 5));
        Assert.AreEqual(10, t.Progress("ten"));
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void Progress_overshoot_is_capped_and_huge_amounts_do_not_overflow()
    {
        var t = new AchievementTracker(Catalog);
        Assert.IsFalse(t.AddProgress("ten", 0));
        Assert.IsTrue(t.AddProgress("ten", int.MaxValue));
        Assert.AreEqual(10, t.Progress("ten"));
    }

    [TestMethod]
    public void Set_progress_is_monotonic_and_unlocks_at_the_goal()
    {
        var t = new AchievementTracker(Catalog);
        Assert.IsFalse(t.SetProgress("ten", 7));
        Assert.IsFalse(t.SetProgress("ten", 3));
        Assert.AreEqual(7, t.Progress("ten"));
        Assert.IsFalse(t.SetProgress("ten", -4));
        Assert.IsTrue(t.SetProgress("ten", 12));
        Assert.IsTrue(t.IsUnlocked("ten"));
    }

    [TestMethod]
    public void Unlocks_persist_immediately_and_progress_after_flush()
    {
        var a = new AchievementTracker(Catalog, Store());
        a.Unlock("first");
        a.AddProgress("ten", 4);
        Assert.IsTrue(a.HasUnsavedChanges);
        Assert.IsFalse(new AchievementTracker(Catalog, Store()).Get("ten").Progress == 4, "Progress waits for a flush.");
        Assert.IsTrue(new AchievementTracker(Catalog, Store()).IsUnlocked("first"), "Unlocks are written at once.");
        Assert.IsTrue(a.Flush());
        Assert.IsFalse(a.HasUnsavedChanges);

        var b = new AchievementTracker(Catalog, Store());
        Assert.IsTrue(b.IsUnlocked("first"));
        Assert.AreEqual(4, b.Progress("ten"));
        int raised = 0;
        b.Unlocked += _ => raised++;
        Assert.IsFalse(b.Unlock("first"), "A reloaded unlock never unlocks again.");
        b.AddProgress("ten", 6);
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void Games_keep_separate_progress()
    {
        var meadow = new AchievementTracker(Catalog, Store("MeadowRun"));
        meadow.Unlock("first");
        var rift = new AchievementTracker(Catalog, Store("NeonRift"));
        Assert.IsFalse(rift.IsUnlocked("first"), "Same id in another game must stay locked.");
        Assert.AreNotEqual(new SaveStore("MeadowRun").Directory, new SaveStore("NeonRift").Directory);
    }

    [TestMethod]
    public void A_grown_catalog_keeps_old_progress_and_adds_new_entries_locked()
    {
        var v1 = new AchievementTracker([First], Store());
        v1.Unlock("first");
        var v2 = new AchievementTracker([new Achievement("new", "New", "Added later."), First with { Name = "Renamed" }], Store());
        Assert.IsTrue(v2.IsUnlocked("first"));
        Assert.AreEqual("Renamed", v2.Get("first").Definition.Name);
        Assert.IsFalse(v2.IsUnlocked("new"));
    }

    [TestMethod]
    public void Entries_removed_from_the_catalog_survive_a_save()
    {
        new AchievementTracker(Catalog, Store()).Unlock("ten");
        var reduced = new AchievementTracker([First], Store());
        reduced.Unlock("first");
        var restored = new AchievementTracker(Catalog, Store());
        Assert.IsTrue(restored.IsUnlocked("ten"));
        Assert.IsTrue(restored.IsUnlocked("first"));
    }

    [TestMethod]
    public void A_lowered_goal_unlocks_quietly_on_load()
    {
        var old = new AchievementTracker([Ten], Store());
        old.AddProgress("ten", 6);
        old.Flush();
        var lowered = new AchievementTracker([Ten with { Goal = 5 }], Store());
        int raised = 0;
        lowered.Unlocked += _ => raised++;
        Assert.IsTrue(lowered.IsUnlocked("ten"));
        Assert.IsFalse(lowered.AddProgress("ten"));
        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void Missing_or_corrupt_data_starts_fresh()
    {
        var store = Store();
        Directory.CreateDirectory(store.Directory);
        string file = Path.Combine(store.Directory, AchievementTracker.SaveSlot + ".json");
        foreach (var junk in new[] { "{ not json", "null", "{\"Entries\":null}", "{\"Entries\":{\"first\":null}}", "{\"Entries\":{\"first\":{\"Progress\":\"lots\"}}}" })
        {
            File.WriteAllText(file, junk);
            var t = new AchievementTracker(Catalog, store);
            Assert.AreEqual(0, t.UnlockedCount, junk);
            Assert.IsTrue(t.Unlock("first"), junk);
        }
        File.WriteAllText(file, "{\"Entries\":{\"ten\":{\"Progress\":-50}}}");
        Assert.AreEqual(0, new AchievementTracker(Catalog, store).Progress("ten"));
    }

    [TestMethod]
    public void A_corrupt_file_is_set_aside_instead_of_being_overwritten()
    {
        var store = Store();
        Directory.CreateDirectory(store.Directory);
        string file = Path.Combine(store.Directory, AchievementTracker.SaveSlot + ".json");
        string kept = Path.Combine(store.Directory, AchievementTracker.SaveSlot + ".corrupt.json");
        const string junk = "{\"Entries\":{\"first\":{\"Progress\":1,\"Unlocked\":tru";
        File.WriteAllText(file, junk);
        var t = new AchievementTracker(Catalog, store);
        Assert.AreEqual(junk, File.ReadAllText(kept), "The damaged file is kept byte for byte for recovery.");
        Assert.IsTrue(t.Unlock("ten"));
        Assert.AreEqual(junk, File.ReadAllText(kept), "Saving new progress never touches the kept copy.");
        Assert.IsTrue(new AchievementTracker(Catalog, store).IsUnlocked("ten"));
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows, IgnoreMessage = "Exclusive file locks are only enforced on Windows.")]
    public void An_unreadable_file_is_never_overwritten()
    {
        var first = new AchievementTracker(Catalog, Store());
        first.Unlock("first");
        string file = Path.Combine(Store().Directory, AchievementTracker.SaveSlot + ".json");
        string before = File.ReadAllText(file);
        AchievementTracker t;
        using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            t = new AchievementTracker(Catalog, Store());
        Assert.IsTrue(t.Unlock("ten"), "The game keeps working in memory.");
        Assert.IsFalse(t.Flush(), "Writing would replace data that could not be read.");
        Assert.AreEqual(before, File.ReadAllText(file));
    }

    [TestMethod]
    public void A_failed_save_keeps_progress_in_memory()
    {
        var blocked = Path.Combine(_dir, "blocked");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(blocked, "a file where the save folder should be");
        var t = new AchievementTracker(Catalog, new SaveStore("x", blocked));
        Assert.IsTrue(t.Unlock("first"), "Unlocking must not throw when the disk refuses.");
        Assert.IsTrue(t.IsUnlocked("first"));
        Assert.IsTrue(t.HasUnsavedChanges);
        Assert.IsFalse(t.Flush());

        File.Delete(blocked);   // storage comes back
        t.AddProgress("ten", 2);
        Assert.IsTrue(t.Flush(), "The next flush retries and succeeds.");
        Assert.IsFalse(t.HasUnsavedChanges);
        var reloaded = new AchievementTracker(Catalog, new SaveStore("x", blocked));
        Assert.IsTrue(reloaded.IsUnlocked("first"), "The unlock that failed to save earlier is written too.");
        Assert.AreEqual(2, reloaded.Progress("ten"));
    }

    [TestMethod]
    public void Game_reports_each_unlock_once_and_flushes_progress_while_running()
    {
        var game = new TestGame(Store(), Catalog);
        var unlocked = new List<Achievement>();
        game.InitializeForTest(new HeadlessPlatform());
        game.Achievements.Unlocked += unlocked.Add;
        game.Achievements.Unlock("first");
        game.Achievements.Unlock("first");
        game.Achievements.AddProgress("ten", 3);
        CollectionAssert.AreEqual(new[] { First }, unlocked, "A repeated unlock is not reported again.");

        for (int i = 0; i < 70; i++) game.StepForTest();
        Assert.AreEqual(3, new AchievementTracker(Catalog, Store()).Progress("ten"), "The loop flushes progress about once a second.");
    }
}
