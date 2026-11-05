using MicroEngine.Core;
using NeonRift.Scenes;

namespace NeonRift.Tests;

/// <summary>The high score is the best score ever reached, kept in the game's save folder beside the achievements.</summary>
[TestClass]
public sealed class HighScoreTests
{
    private static string TempDir() => Path.Combine(Path.GetTempPath(), "neonrift-tests", Guid.NewGuid().ToString("N"));

    private static RiftGame Launch(string dir, LaunchOptions? options = null)
    {
        var game = new RiftGame(options, saveDirectory: dir);
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        return game;
    }

    [TestMethod]
    public void A_first_launch_has_no_high_score()
    {
        Assert.AreEqual(0, Launch(TempDir()).Session.HighScore);
    }

    [TestMethod]
    public void A_new_run_keeps_the_high_score_and_the_next_launch_reads_it()
    {
        string dir = TempDir();
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
        string dir = TempDir();
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
    public void Clearing_a_stage_saves_the_high_score()
    {
        string dir = TempDir();
        var game = Launch(dir, new LaunchOptions("stage", 1, God: true, Auto: true));
        for (int i = 0; i < 60 * 60 * 8 && game.Scenes.Current is PlayScene; i++) game.StepForTest();
        Assert.IsNotInstanceOfType<PlayScene>(game.Scenes.Current, "Stage 1 was not cleared.");
        int reached = game.Session.Score;
        Assert.IsGreaterThan(0, reached);

        Assert.AreEqual(reached, Launch(dir).Session.HighScore);
    }
}
