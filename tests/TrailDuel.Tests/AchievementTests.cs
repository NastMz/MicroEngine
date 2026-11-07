using MicroEngine.Core;
using MicroEngine.UI;
using TrailDuel.Rules;
using TrailDuel.Scenes;

namespace TrailDuel.Tests;

/// <summary>Achievements earned through real play: catching, evolving, and the title menu that lists them.</summary>
[TestClass]
public sealed class AchievementTests
{
    [TestMethod]
    public void CatchingAWildCreatureInTheGrassEarnsNewFriend()
    {
        using var h = new Harness("route-mid");
        foreach (var c in h.State.Party) c.Hp = c.MaxHp;
        h.State.Bag.Items.Clear();
        h.State.Bag.Add("superorb", 40);
        h.Step(60);
        string[] walk = ["down", "up", "down", "up", "left", "right"];
        for (int i = 0; i < 6000 && !h.Game.Achievements.IsUnlocked(TrailAchievements.FirstCatch); i++)
        {
            switch (h.Game.Scenes.Current)
            {
                case OverworldScene { Busy: false } w when !w.Player.Moving && !h.Game.Scenes.InTransition:
                    h.Hold(walk[i % walk.Length], 14);
                    break;
                case BattleScene { AwaitingChoice: true, InMoveMenu: false }:
                    h.Press("right");   // FIGHT -> BAG
                    h.Press("confirm", 10);
                    break;
                case BagScene:
                    h.Press("confirm", 10);   // the only item: a Super Orb
                    break;
                default:
                    AutoPlay.Advance(h);
                    break;
            }
        }
        Assert.IsTrue(h.Game.Achievements.IsUnlocked(TrailAchievements.FirstCatch), "A wild creature should have been caught.");
        Assert.IsGreaterThan(2, h.State.Party.Count + h.State.Box.Count);
    }

    [TestMethod]
    public void WatchingAnEvolutionEarnsGrowingUpOnce()
    {
        using var h = new Harness("evolve");
        int toasts = 0;
        h.Game.Achievements.Unlocked += _ => toasts++;
        for (int i = 0; i < 400 && h.Game.Scenes.Current is EvolutionScene; i++) AutoPlay.Advance(h);
        Assert.IsNotInstanceOfType<EvolutionScene>(h.Game.Scenes.Current);
        Assert.IsTrue(h.Game.Achievements.IsUnlocked(TrailAchievements.Evolution));
        Assert.AreEqual(1, toasts);
        Assert.IsTrue(h.Platform.SilentAudio.Played.Contains("sparkle"), "The unlock plays a sparkle.");
        Assert.IsTrue(new AchievementTracker(TrailAchievements.Catalog, new SaveStore("TrailDuel", h.SaveDir)).IsUnlocked(TrailAchievements.Evolution), "Saved to the game's folder.");
    }

    [TestMethod]
    public void TheTitleMenuOpensTheAchievementList()
    {
        using var h = new Harness();
        h.Step(30);
        h.Press("confirm", 10);   // press start: NEW GAME, CONTROLS, ACHIEVEMENTS, QUIT
        h.Press("down");
        h.Press("down");
        h.Press("confirm", 10);
        Assert.IsInstanceOfType<DemoKit.AchievementList>(h.Game.Scenes.Current);
        h.Press("cancel", 10);
        Assert.IsInstanceOfType<TitleScene>(h.Game.Scenes.Current);
    }
}
