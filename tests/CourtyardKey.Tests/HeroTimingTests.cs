using System.Numerics;
using CourtyardKey.Entities;
using CourtyardKey.Scenes;
using MicroEngine.Input;

namespace CourtyardKey.Tests;

/// <summary>
/// The updates the hero's actions happen on, at the game's 60 updates a second: when a swing starts, when its blade
/// can hit and stops hitting, when the hero can walk again; the same for a throw, a hit taken and a fall. These are
/// what the player feels as the weight of each action, so they are pinned update by update.
/// </summary>
[TestClass]
public sealed class HeroTimingTests
{
    [TestMethod]
    public void ASwingAThrowAHitAndAFallHappenOnTheSameUpdates()
    {
        using var h = new Harness("channel");
        h.Step(40);
        var hero = h.Play.Hero;
        var log = new List<string>();
        var state = hero.State;
        bool blade = false, gameOver = false;
        for (int update = 1; update <= 420; update++)
        {
            h.Platform.Keys.Set(Key.Z, update is 5 or 6);
            h.Platform.Keys.Set(Key.C, update is 60 or 61);
            if (update == 120) hero.TakeDamage(1, hero.Position + new Vector2(10, 0));
            if (update == 200) hero.TakeDamage(99, hero.Position + new Vector2(0, 10));
            h.Step();
            if (hero.State != state) log.Add($"{update}:{state = hero.State}");
            if (hero.SwordBox is not null != blade) log.Add($"{update}:{((blade = !blade) ? "blade" : "sheathed")}");
            if (h.Game.Scenes.Current is GameOverScene != gameOver) log.Add($"{update}:{((gameOver = !gameOver) ? "game over" : "back")}");
        }
        Assert.AreEqual("5:Swing 7:blade 17:sheathed 21:Walk 60:Throw 71:Walk 120:Hurt 135:Walk 200:Dead 309:game over", string.Join(" ", log));
    }
}
