using GemGarden;
using GemGarden.Scenes;
using MicroEngine.Core;

namespace GemGarden.Tests;

/// <summary>Gem Garden played headlessly: the game is built, initialised on HeadlessPlatform and stepped one update at a time.</summary>
[TestClass]
public sealed class GemGardenTests
{
    /// <summary>A new game with a store of its own, in memory and empty: a test never reads or writes the player's saves.</summary>
    private static MainGame Start()
    {
        var game = new MainGame();
        game.InitializeForTest(new HeadlessPlatform());
        return game;
    }

    private static void Hold(Game game, string action, int steps)
    {
        game.Input.SetForced(action, true);
        Step(game, steps);
        game.Input.SetForced(action, false);
        Step(game, 1);
    }

    private static void Step(Game game, int steps)
    {
        for (int i = 0; i < steps; i++) game.StepForTest();
    }

    /// <summary>From the title, PLAY (the first option) starts a round once the fade is over.</summary>
    private static PlayScene Play(MainGame game)
    {
        Assert.IsInstanceOfType<TitleScene>(game.Scenes.Current);
        Hold(game, "confirm", 2);
        Step(game, 60);
        return (PlayScene)game.Scenes.Current!;
    }

    [TestMethod]
    public void WalkingRightPicksUpTheFirstGem()
    {
        var game = Start();
        var play = Play(game);
        Assert.AreEqual(0, play.Gems);
        Assert.AreEqual(24, play.TotalGems);
        Hold(game, "right", 70);
        Assert.AreEqual(1, play.Gems, "the gem five tiles to the right of the start");
    }

    [TestMethod]
    public void PauseFreezesTheRoundAndResumes()
    {
        var game = Start();
        var play = Play(game);
        Hold(game, "pause", 2);
        Assert.IsInstanceOfType<PauseScene>(game.Scenes.Current);
        float frozen = play.TimeLeft;
        Step(game, 120);
        Assert.AreEqual(frozen, play.TimeLeft, "no time passes under the pause menu");
        Hold(game, "confirm", 2);   // RESUME
        Assert.AreSame(play, game.Scenes.Current);
        Step(game, 60);
        Assert.IsLessThan(frozen, play.TimeLeft);
    }

    [TestMethod]
    public void TheRoundEndsWhenTimeRunsOutAndTheBestScoreIsSaved()
    {
        // The same saves for this game and the next one: what a second launch finds.
        var saves = new MemorySaveStorage();
        var game = new MainGame();
        game.InitializeForTest(new HeadlessPlatform(), saves: saves);
        Play(game);
        Hold(game, "right", 70);
        Step(game, (int)(PlayScene.RoundSeconds * 60) + 60);
        Assert.IsInstanceOfType<ResultsScene>(game.Scenes.Current);
        Assert.AreEqual(1, game.BestScore);
        Assert.IsTrue(saves.Exists("progress"), "the game saved its slot");

        var relaunched = new MainGame();
        relaunched.InitializeForTest(new HeadlessPlatform(), saves: saves);
        Assert.AreEqual(1, relaunched.BestScore, "the next session loads the best score");
        Assert.IsFalse(relaunched.RecordScore(1), "an equal score is not a new best");
        Assert.IsTrue(relaunched.RecordScore(2));
    }

    [TestMethod]
    public void QuitFromTheTitleEndsTheGame()
    {
        var game = Start();
        Hold(game, "down", 2);
        Hold(game, "confirm", 2);
        Assert.IsTrue(game.QuitRequested);
    }
}
