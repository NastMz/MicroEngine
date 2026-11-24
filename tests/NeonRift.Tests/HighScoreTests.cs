using MicroEngine.Core;
using MicroEngine.Saves;
using NeonRift.Scenes;

namespace NeonRift.Tests;

/// <summary>The high score is the best score ever reached, kept in the game's saves beside the achievements.</summary>
[TestClass]
public sealed class HighScoreTests
{
    /// <summary>Starts the game over <paramref name="saves"/>: two launches over the same saves are a restart.</summary>
    private static RiftGame Launch(MemorySaveStorage saves, LaunchOptions? options = null)
    {
        var game = new RiftGame(options);
        game.InitializeForTest(HeadlessPlatform.For(game.Settings), saves: saves);
        return game;
    }

    [TestMethod]
    public void A_first_launch_has_no_high_score()
    {
        Assert.AreEqual(0, Launch(new MemorySaveStorage()).Session.HighScore);
    }

    [TestMethod]
    public void A_new_run_keeps_the_high_score_and_the_next_launch_reads_it()
    {
        var dir = new MemorySaveStorage();
        var first = Launch(dir);
        first.Session.AddScore(12340);
        first.NewGame();
        Assert.AreEqual(0, first.Session.Score, "a new run starts from zero");
        Assert.AreEqual(12340, first.Session.HighScore, "a new run keeps the best score");

        Assert.AreEqual(12340, Launch(dir).Session.HighScore, "the high score survives a restart");
    }

    [TestMethod]
    public void A_lower_score_does_not_replace_the_saved_high_score()
    {
        var dir = new MemorySaveStorage();
        var first = Launch(dir);
        first.Session.AddScore(5000);
        first.NewGame();
        var second = Launch(dir);
        second.Session.AddScore(300);
        second.NewGame();

        Assert.AreEqual(5000, second.Session.HighScore);
        Assert.AreEqual(5000, Launch(dir).Session.HighScore);
    }

    [TestMethod]
    public void Quitting_a_run_from_the_pause_menu_saves_the_high_score()
    {
        var dir = new MemorySaveStorage();
        var game = new RiftGame(new LaunchOptions("stage", 1));
        var platform = HeadlessPlatform.For(game.Settings);
        game.InitializeForTest(platform, saves: dir);
        void Tap(MicroEngine.Input.Key key) { platform.Keys.Set(key, true); game.StepForTest(); platform.Keys.Set(key, false); game.StepForTest(); }
        for (int i = 0; i < 30; i++) game.StepForTest();
        game.Session.AddScore(4321);
        Tap(MicroEngine.Input.Key.Escape);
        Assert.IsInstanceOfType<PauseScene>(game.Scenes.Current);
        Tap(MicroEngine.Input.Key.Down);
        Tap(MicroEngine.Input.Key.Enter);
        for (int i = 0; i < 60 && game.Scenes.Current is not TitleScene; i++) game.StepForTest();
        Assert.IsInstanceOfType<TitleScene>(game.Scenes.Current);

        Assert.AreEqual(4321, Launch(dir).Session.HighScore);
    }

    [TestMethod]
    public void Clearing_a_stage_saves_the_high_score()
    {
        var dir = new MemorySaveStorage();
        var game = Launch(dir, new LaunchOptions("stage", 1, God: true, Auto: true));
        for (int i = 0; i < 60 * 60 * 8 && game.Scenes.Current is PlayScene; i++) game.StepForTest();
        Assert.IsNotInstanceOfType<PlayScene>(game.Scenes.Current, "Stage 1 was not cleared.");
        int reached = game.Session.Score;
        Assert.IsGreaterThan(0, reached);

        Assert.AreEqual(reached, Launch(dir).Session.HighScore);
    }
}
