using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MeadowRun.Gameplay;
using MeadowRun.Gameplay.Actors;
using MeadowRun.Scenes;
using MicroEngine.Core;

namespace MeadowRun.Tests;

[TestClass]
public sealed class PlaythroughTests
{
    private static string TempDir() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "meadowrun-tests", Guid.NewGuid().ToString("N"))).FullName;

    private static void Press(MeadowGame game, string action, int frames = 2)
    {
        game.Input.SetForced(action, true);
        for (int i = 0; i < frames; i++) game.StepForTest();
        game.Input.SetForced(action, false);
        game.StepForTest();
    }

    private static void StepUntil(MeadowGame game, Func<bool> condition, int limit, string what = "")
    {
        for (int i = 0; i < limit && !condition(); i++) game.StepForTest();
        Assert.IsTrue(condition(), $"not reached in time: {what} (current scene {game.Scenes.Current?.GetType().Name})");
    }

    /// <summary>
    /// The whole game loop headlessly: title, new game, world map, level card, level 1 played by the
    /// autopilot through the real input bindings, the clear tally, and back to the map with progress saved.
    /// </summary>
    [TestMethod]
    public void NewGameThroughFirstLevelSavesProgress()
    {
        string dir = TempDir();
        var game = new MeadowGame(dir);
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        Assert.IsInstanceOfType<TitleScene>(game.Scenes.Current);

        for (int i = 0; i < 40; i++) game.StepForTest();
        Press(game, "confirm");
        StepUntil(game, () => game.Scenes.Current is MapScene && !game.Scenes.InTransition, 200, "map");
        Press(game, "confirm");
        StepUntil(game, () => game.Scenes.Current is PlayScene && !game.Scenes.InTransition, 400, "play");

        // Play until back on the map; after a lost life the level card and a fresh attempt follow.
        string[] actions = ["left", "right", "down", "jump", "run"];
        PlayScene? play = null;
        Autopilot bot = new();
        int attempts = 0;
        for (int f = 0; f < 60 * 600 && game.Scenes.Current is not MapScene; f++)
        {
            bool[] down = new bool[actions.Length];
            if (game.Scenes.Current is PlayScene current)
            {
                if (current != play) { play = current; bot = new Autopilot(attempts++); }
                if (current.Stage.Phase == StagePhase.Playing)
                {
                    var input = bot.Next(current.Stage);
                    down = [input.Left, input.Right, input.Down, input.JumpHeld || input.JumpPressed, input.RunHeld];
                }
            }
            for (int a = 0; a < actions.Length; a++) game.Input.SetForced(actions[a], down[a]);
            game.StepForTest();
        }
        foreach (var a in actions) game.Input.SetForced(a, false);
        Assert.IsInstanceOfType<MapScene>(game.Scenes.Current, $"back on the map after {attempts} attempt(s)");
        Assert.IsTrue(attempts <= Session.StartingLives);

        Assert.AreEqual(2, game.Save.Unlocked, "clearing level 1 unlocks level 2");
        Assert.IsTrue(game.Save.BestScores[0] > 0);
        var reloaded = new SaveStore("MeadowRun", dir).Load<SaveData>("progress");
        Assert.IsNotNull(reloaded);
        Assert.AreEqual(2, reloaded.Unlocked, "progress is persisted to disk");
        Assert.AreEqual(0, game.MissingSounds.Count, "every requested sound exists: " + string.Join(", ", game.MissingSounds));
        Assert.IsTrue(platform.SilentAudio.Played.Contains("jump"));
        Assert.IsTrue(platform.SilentAudio.Played.Contains("flag"));

        Assert.IsTrue(game.Achievements.IsUnlocked(MeadowAchievements.FirstCourse), "clearing level 1 earns First Steps");
        Assert.IsTrue(platform.SilentAudio.Played.Contains("checkpoint"), "the unlock plays its jingle");
        var relaunched = new MeadowGame(dir);
        relaunched.InitializeForTest(new HeadlessPlatform());
        Assert.IsTrue(relaunched.Achievements.IsUnlocked(MeadowAchievements.FirstCourse), "achievements survive a restart");
        Assert.IsNull(relaunched.AchievementToasts!.Current, "a reloaded unlock is not announced again");
    }

    [TestMethod]
    public void CoinsAndStompsAccumulateIntoProgressiveAchievements()
    {
        string dir = TempDir();
        var game = new MeadowGame(dir, new LaunchOptions("level"));
        game.InitializeForTest(new HeadlessPlatform());
        for (int i = 0; i < 5; i++) game.StepForTest();
        var play = (PlayScene)game.Scenes.Current!;
        for (int i = 0; i < 7; i++) game.Session.AddCoin();
        for (int i = 0; i < 3; i++) play.Stage.StompSink!();
        Assert.AreEqual(7, game.Achievements.Progress(MeadowAchievements.CoinPurse));
        Assert.AreEqual(3, game.Achievements.Progress(MeadowAchievements.Stomper));
        for (int i = 0; i < 70; i++) game.StepForTest();
        var relaunched = new MeadowGame(dir);
        relaunched.InitializeForTest(new HeadlessPlatform());
        Assert.AreEqual(7, relaunched.Achievements.Progress(MeadowAchievements.CoinPurse), "progress is flushed while playing");
        Assert.AreEqual(3, relaunched.Achievements.Progress(MeadowAchievements.Stomper));
    }

    [TestMethod]
    public void TitleMenuOpensAndClosesTheAchievementList()
    {
        var game = new MeadowGame(TempDir());
        game.InitializeForTest(new HeadlessPlatform());
        for (int i = 0; i < 40; i++) game.StepForTest();
        Press(game, "down");
        Press(game, "down");
        Press(game, "down");
        Press(game, "confirm");
        Assert.IsInstanceOfType<MicroEngine.UI.AchievementsScene>(game.Scenes.Current);
        Press(game, "cancel");
        Assert.IsInstanceOfType<TitleScene>(game.Scenes.Current);
    }

    [TestMethod]
    public void EveryScreenRendersHeadlessly()
    {
        foreach (var start in new[] { "title", "map", "level", "credits", "gameover", "sheet" })
        {
            var game = new MeadowGame(TempDir(), new LaunchOptions(start, Level: 4));
            var platform = new HeadlessPlatform();
            var result = GameRunner.Run(game, platform, new RunOptions { MaxUpdates = 240 });
            Assert.IsNull(result.Error, $"{start}: {result.Error}");
            Assert.AreEqual(0, game.MissingSounds.Count, $"{start}: missing {string.Join(", ", game.MissingSounds)}");
        }
    }

    [TestMethod]
    public void PauseMenuRestartsTheLevel()
    {
        var game = new MeadowGame(TempDir(), new LaunchOptions("level"));
        game.InitializeForTest(new HeadlessPlatform());
        for (int i = 0; i < 20; i++) game.StepForTest();
        Press(game, "pause");
        Assert.IsInstanceOfType<PauseScene>(game.Scenes.Current);
        Press(game, "down");
        Press(game, "confirm");
        StepUntil(game, () => game.Scenes.Current is LevelCardScene, 200);
    }

    /// <summary>Fights the Mole King: wake him, stomp him while dazed three times, claim the Sunstone.</summary>
    [TestMethod]
    public void TheMoleKingCanBeDefeatedAndTheSunstoneEndsTheGame()
    {
        var session = new Session();
        var stage = StageHarness.New(new Level(Levels.All[^1]), session);
        stage.IgnoreEnemiesForTesting = false;
        var boss = stage.Boss!;
        Assert.IsNotNull(boss);
        stage.Player.Position = new Vector2(boss.Arena.Left + 48, boss.Arena.Bottom - Player.Height);
        StageHarness.Run(stage, 2, default);
        Assert.IsTrue(boss.Active, "entering the arena wakes the boss");
        Assert.IsNotNull(stage.CameraLock);

        for (int hit = 0; hit < 3; hit++)
        {
            for (int f = 0; f < 60 * 20 && !boss.Vulnerable; f++)
            {
                stage.Player.Invulnerable = 5; // this test is about the boss's state machine, not dodging
                StageHarness.Run(stage, 1, default);
            }
            Assert.IsTrue(boss.Vulnerable, $"boss becomes vulnerable before hit {hit + 1}");
            stage.Player.Position = new Vector2(boss.Center.X - Player.Width / 2f, boss.Bounds.Top - Player.Height - 3);
            stage.Player.Velocity = new Vector2(0, 120);
            StageHarness.Run(stage, 3, default);
        }
        Assert.AreEqual(0, boss.Health);
        Assert.IsTrue(boss.Defeated);

        Sunstone? stone = null;
        for (int f = 0; f < 120 && stone is null; f++) { StageHarness.Run(stage, 1, default); stone = stage.Actors.OfType<Sunstone>().FirstOrDefault(); }
        Assert.IsNotNull(stone, "the Sunstone appears");
        StageHarness.Run(stage, 90, default);
        stage.Player.Position = stone.Center - new Vector2(Player.Width / 2f, Player.Height / 2f);
        for (int f = 0; f < 60 * 6 && stage.Phase != StagePhase.Done; f++) StageHarness.Run(stage, 1, default);
        Assert.AreEqual(StageOutcome.Won, stage.Outcome);
    }

    [TestMethod]
    public void ClearingTheFinalLevelMarksTheGameFinished()
    {
        var game = new MeadowGame(TempDir());
        game.InitializeForTest(new HeadlessPlatform());
        for (int i = 0; i < Levels.All.Count; i++) game.RecordClear(i, 1000 + i, 10);
        Assert.IsTrue(game.Save.Finished);
        Assert.AreEqual(Levels.All.Count, game.Save.Unlocked);
    }

    [TestMethod]
    public void CorruptSaveFallsBackToFreshProgress()
    {
        string dir = TempDir();
        File.WriteAllText(Path.Combine(dir, "progress.json"), "{ not json");
        var game = new MeadowGame(dir);
        game.InitializeForTest(new HeadlessPlatform());
        Assert.AreEqual(1, game.Save.Unlocked);
    }
}
