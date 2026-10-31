using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.UI;
using NeonRift.Gameplay;
using NeonRift.Scenes;

namespace NeonRift.Tests;

/// <summary>Achievements earned by playing the real game: the autopilot clears stages through the play scene.</summary>
[TestClass]
public sealed class AchievementTests
{
    private static string TempDir() => Path.Combine(Path.GetTempPath(), "neonrift-tests", Guid.NewGuid().ToString("N"));

    private static RiftGame Play(int stage, string dir)
    {
        var game = new RiftGame(new LaunchOptions("stage", stage, God: true, Auto: true), saveDirectory: dir);
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        for (int i = 0; i < 60 * 60 * 8 && game.Scenes.Current is PlayScene; i++) game.StepForTest();
        Assert.IsNotInstanceOfType<PlayScene>(game.Scenes.Current, $"Stage {stage} was not cleared.");
        return game;
    }

    [TestMethod]
    public void Clearing_the_rooftops_earns_the_stage_and_kill_achievements()
    {
        string dir = TempDir();
        var game = Play(1, dir);
        var a = game.Achievements;
        Assert.IsTrue(a.IsUnlocked(RiftAchievements.FirstScrap));
        Assert.IsGreaterThan(1, a.Progress(RiftAchievements.ScrapDealer), "Every destroyed robot counts.");
        Assert.IsTrue(a.IsUnlocked(RiftAchievements.Rooftops));
        Assert.IsTrue(a.IsUnlocked(RiftAchievements.Untouched), "The autopilot clears the stage without dying.");
        Assert.IsFalse(a.IsUnlocked(RiftAchievements.Leviathan));

        game.StepForTest();
        var reopened = new AchievementTracker(RiftAchievements.Catalog, new SaveStore("NeonRift", dir));
        Assert.IsTrue(reopened.IsUnlocked(RiftAchievements.Rooftops), "Unlocks survive a restart.");
        Assert.AreEqual(3, reopened.UnlockedCount);
    }

    [TestMethod]
    public void Destroying_the_leviathan_earns_its_achievement()
    {
        var game = Play(3, TempDir());
        Assert.IsInstanceOfType<EndingScene>(game.Scenes.Current);
        Assert.IsTrue(game.Achievements.IsUnlocked(RiftAchievements.Leviathan));
        Assert.IsFalse(game.Achievements.IsUnlocked(RiftAchievements.Rooftops), "Only the cleared stage counts.");
    }

    [TestMethod]
    public void The_title_menu_lists_the_achievements()
    {
        var game = new RiftGame(saveDirectory: TempDir());
        var platform = HeadlessPlatform.For(game.Settings);
        game.InitializeForTest(platform);
        void Tap(Key key) { platform.Keys.Set(key, true); game.StepForTest(); platform.Keys.Set(key, false); game.StepForTest(); }
        Tap(Key.Down);
        Tap(Key.Down);
        Tap(Key.Enter);
        Assert.IsInstanceOfType<AchievementsScene>(game.Scenes.Current);
        Tap(Key.Escape);
        Assert.IsInstanceOfType<TitleScene>(game.Scenes.Current);
    }
}
