using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Scenes;

namespace TrailDuel.Tests;

/// <summary>
/// Plays the real game headlessly through its input actions: title, new game, walking to the lab,
/// choosing a starter, the rival duel, then out to Route 1 to win a wild battle.
/// </summary>
[TestClass]
public sealed class PlaythroughTests
{
    private sealed class Driver(Harness h)
    {
        public long Frames;
        private Scene? Top => h.Game.Scenes.Current;
        public OverworldScene? World => Top as OverworldScene;

        private void Step(int n = 1) { h.Step(n); Frames += n; }

        private void Press(string action)
        {
            h.Game.Input.SetForced(action, true);
            Step(2);
            h.Game.Input.SetForced(action, false);
            Step(4);
        }

        private bool Settled => World is { Busy: false } w && !w.Player.Moving && !h.Game.Scenes.InTransition;

        /// <summary>Advances through dialogue, menus and battles until the player is free to walk.</summary>
        public void Settle(int maxFrames = 20000)
        {
            int start = (int)Frames;
            while (Frames - start < maxFrames)
            {
                Step(2);
                if (Settled) { Step(4); if (Settled) return; }
                if (h.Game.Scenes.InTransition) continue;
                switch (Top)
                {
                    case PartyScene:
                        // Forced switch: move to the next healthy creature and send it out.
                        Press("down");
                        Press("confirm");
                        Press("confirm");
                        break;
                    case BattleScene { InMoveMenu: true } bs:
                        ChooseBestMove(bs);
                        break;
                    default:
                        Press("confirm");
                        break;
                }
            }
            Assert.Fail($"Did not settle within {maxFrames} frames (top scene {Top?.GetType().Name}).");
        }

        private void ChooseBestMove(BattleScene bs)
        {
            var b = bs.Battle;
            int best = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < b.Player.Moves.Count; i++)
            {
                if (b.Player.Moves[i].Pp <= 0) continue;
                float s = TrailDuel.Rules.TrainerAI.Score(b.Player, b.PlayerStages, b.Enemy, b.EnemyStages, b.Player.Moves[i].Data);
                if (s > bestScore) { bestScore = s; best = i; }
            }
            int cur = bs.MoveCursor;
            if (best % 2 > cur % 2) Press("right");
            if (best % 2 < cur % 2) Press("left");
            if (best / 2 > cur / 2) Press("down");
            if (best / 2 < cur / 2) Press("up");
            Press("confirm");
        }

        public void Walk(string action, int tiles)
        {
            for (int i = 0; i < tiles; i++)
            {
                var w = World!;
                var (x, y, map) = (h.State.X, h.State.Y, h.State.MapId);
                h.Game.Input.SetForced(action, true);
                int guard = 0;
                while ((h.State.X, h.State.Y, h.State.MapId) == (x, y, map) && guard++ < 600) Step();
                h.Game.Input.SetForced(action, false);
                while (World is { } ww && ww.Player.Moving) Step();
                Step();
                if (World is null || World.Busy || World != w) { Settle(); return; }
            }
        }

        public void Face(string action)
        {
            h.Game.Input.SetForced(action, true);
            Step(3);
            h.Game.Input.SetForced(action, false);
            Step(3);
        }

        public void Interact() { Press("confirm"); Settle(); }
    }

    [TestMethod]
    public void NewGameToStarterRivalDuelAndFirstWildVictory()
    {
        using var h = new Harness();
        var d = new Driver(h);
        h.Step(30);
        Assert.IsInstanceOfType<TitleScene>(h.Game.Scenes.Current);
        h.Press("confirm", 10);
        h.Press("confirm", 10);
        d.Settle();
        Assert.AreEqual("home", h.State.MapId);
        Assert.IsTrue(h.State.Has("intro"));

        // Out of the house and over to the lab.
        d.Walk("down", 3); d.Walk("left", 2); d.Walk("down", 1);
        Assert.AreEqual("sprout", h.State.MapId);
        d.Walk("down", 1); d.Walk("right", 12); d.Walk("up", 2);
        Assert.AreEqual("lab", h.State.MapId);

        // Talk to the mentor, then pick Emberpup from the left pedestal.
        d.Walk("up", 3); d.Walk("right", 3); d.Walk("up", 1);
        d.Interact();
        Assert.IsTrue(h.State.Has("met-mentor"));
        d.Walk("down", 1); d.Walk("left", 4);
        d.Face("up");
        d.Interact();
        Assert.IsTrue(h.State.Has("starter"));
        Assert.AreEqual("emberpup", h.State.StarterId);
        Assert.IsTrue(h.State.Has("rival1"), "The rival duel follows the starter choice.");
        Assert.IsTrue(h.State.Has("gift:orbs"));
        Assert.AreEqual(5, h.State.Bag.Count("orb"));
        Assert.AreEqual("lab", h.State.MapId);

        // Leave the lab and head north to Route 1's tall grass.
        d.Walk("down", 4);
        d.Walk("right", 1);
        d.Walk("down", 1);
        Assert.AreEqual("sprout", h.State.MapId);
        d.Walk("left", 5); d.Walk("up", 8);
        Assert.AreEqual("route1", h.State.MapId);
        d.Walk("up", 4); d.Walk("right", 5);

        int wins = h.State.BattlesWon;
        for (int i = 0; i < 80 && h.State.BattlesWon == wins; i++)
        {
            if (h.State.MapId != "route1") Assert.Fail("Lost the wild battle and whited out.");
            d.Walk(i % 2 == 0 ? "right" : "left", 1);
        }
        Assert.IsTrue(h.State.BattlesWon > wins, "A wild battle should have been fought and won.");
        Assert.IsTrue(h.Game.Achievements.IsUnlocked(TrailAchievements.FirstWin), "The rival duel was the first victory.");
        Assert.AreEqual(h.State.BattlesWon, h.Game.Achievements.Progress(TrailAchievements.Seasoned), "Every win counts toward Seasoned Trainer.");
        Assert.IsTrue(h.State.Party[0].Xp > Creature.XpForLevel(5), "The starter earned experience.");
        Assert.IsInstanceOfType<OverworldScene>(h.Game.Scenes.Current);
    }

    [TestMethod]
    public void SaveFromMenuThenContinueFromTitle()
    {
        using var h = new Harness("town");
        h.Step(60);
        h.Press("pause", 10);
        Assert.IsInstanceOfType<StartMenuScene>(h.Game.Scenes.Current);
        for (int i = 0; i < 3; i++) h.Press("down", 4);
        h.Press("confirm", 10);
        Assert.IsTrue(h.Game.HasSave);

        using var again = new Harness();
        // Point the new game at the same save folder by copying the file.
        Directory.CreateDirectory(again.SaveDir);
        foreach (var f in Directory.GetFiles(h.SaveDir)) File.Copy(f, Path.Combine(again.SaveDir, Path.GetFileName(f)));
        var game = new TrailDuelGame(again.SaveDir, "none");
        game.InitializeForTest(new MicroEngine.Core.HeadlessPlatform(), 1);
        Assert.IsTrue(game.HasSave);
        game.ContinueGame();
        for (int i = 0; i < 120; i++) game.StepForTest();
        Assert.AreEqual("sprout", game.State.MapId);
        Assert.AreEqual("emberpup", game.State.Party[0].SpeciesId);
        Assert.IsInstanceOfType<OverworldScene>(game.Scenes.Current);
    }
}

[TestClass]
public sealed class EndgameTests
{
    [TestMethod]
    public void ClimbTheHallBeatTheMasterAndRollCredits()
    {
        using var h = new Harness("hall");
        h.State.Party.Clear();
        foreach (var (s, l) in new[] { ("blazehound", 16), ("mossnail", 13), ("fizzbit", 13), ("breezel", 13) })
            h.State.Party.Add(Creature.Create(s, l));
        h.Step(60);
        Assert.IsInstanceOfType<OverworldScene>(h.Game.Scenes.Current);
        Assert.AreEqual("hall", h.State.MapId);
        int money = h.State.Money;

        // Walking up the carpet: the apprentice spots us first, then the Master.
        for (int i = 0; i < 4000 && !h.State.Has("trainer:odessa"); i++)
        {
            if (h.Game.Scenes.Current is OverworldScene { Busy: false } w && !w.Player.Moving) h.Hold("up", 16);
            else AutoPlay.Advance(h);
        }
        Assert.IsTrue(h.State.Has("trainer:lio"));
        Assert.IsTrue(h.State.Has("trainer:odessa"), "The Master should be beaten.");
        Assert.AreEqual(money + 500 + 2000, h.State.Money, "Both prizes are paid.");
        for (int i = 0; i < 400 && h.Game.Scenes.Current is not CreditsScene; i++) AutoPlay.Advance(h);
        Assert.IsTrue(h.State.Has("master"));
        Assert.IsTrue(h.Game.Achievements.IsUnlocked(TrailAchievements.Champion));
        Assert.IsInstanceOfType<CreditsScene>(h.Game.Scenes.Current);
        Assert.IsTrue(h.Game.HasSave, "Becoming champion saves the game.");
    }
}

[TestClass]
public sealed class PrizeMoneyTests
{
    [TestMethod]
    public void TrainerPrizesArePaidEvenAfterLosingTwice()
    {
        using var h = new Harness("hall");
        h.State.Party.Clear();
        foreach (var (s, l) in new[] { ("blazehound", 16), ("mossnail", 13), ("fizzbit", 13), ("breezel", 13) })
            h.State.Party.Add(Creature.Create(s, l));
        h.State.Money = 1500;
        h.State.WhiteOut();
        h.State.WhiteOut();
        Assert.AreEqual(375, h.State.Money);
        h.Step(60);

        for (int i = 0; i < 3000 && !h.State.Has("trainer:lio"); i++)
        {
            if (h.Game.Scenes.Current is OverworldScene { Busy: false } w && !w.Player.Moving) h.Hold("up", 16);
            else AutoPlay.Advance(h);
        }
        Assert.IsTrue(h.State.Has("trainer:lio"), "The apprentice should be beaten.");
        Assert.AreEqual(375 + 500, h.State.Money, "Apprentice Lio's prize is added to the purse.");
    }
}

/// <summary>Minimal autopilot: confirms text and picks the best-scoring move in battle.</summary>
internal static class AutoPlay
{
    public static void Advance(Harness h)
    {
        switch (h.Game.Scenes.Current)
        {
            case BattleScene { InMoveMenu: true } bs:
                var b = bs.Battle;
                int best = TrailDuel.Rules.TrainerAI.ChooseMove(b.Player, b.PlayerStages, b.Enemy, b.EnemyStages, true, new Random(1));
                if (best < 0) best = 0;
                int cur = bs.MoveCursor;
                if (best % 2 > cur % 2) h.Press("right");
                if (best % 2 < cur % 2) h.Press("left");
                if (best / 2 > cur / 2) h.Press("down");
                if (best / 2 < cur / 2) h.Press("up");
                h.Press("confirm");
                break;
            case PartyScene:
                h.Press("down");
                h.Press("confirm");
                h.Press("confirm");
                break;
            default:
                h.Press("confirm", 8);
                break;
        }
    }
}

[TestClass]
public sealed class ForestTests
{
    [TestMethod]
    public void RivalAmbushesNearTheForestExit()
    {
        using var h = new Harness("forest");
        h.State.Party.Clear();
        foreach (var (s, l) in new[] { ("blazehound", 15), ("fizzbit", 13), ("mossnail", 13) })
            h.State.Party.Add(Creature.Create(s, l));
        h.Step(60);
        (string Dir, int Tiles)[] route = [("up", 4), ("right", 8), ("up", 11)];
        foreach (var (dir, tiles) in route)
        {
            int walked = 0;
            for (int guard = 0; guard < 6000 && walked < tiles; guard++)
            {
                if (h.Game.Scenes.Current is OverworldScene { Busy: false } w && !w.Player.Moving && !h.Game.Scenes.InTransition)
                {
                    var before = (h.State.X, h.State.Y);
                    h.Hold(dir, 14);
                    while (h.Game.Scenes.Current is OverworldScene { Player.Moving: true }) h.Step();
                    if ((h.State.X, h.State.Y) != before) walked++;
                }
                else AutoPlay.Advance(h);
            }
        }
        for (int i = 0; i < 3000 && !h.State.Has("rival2"); i++) AutoPlay.Advance(h);
        Assert.IsTrue(h.State.Has("rival2"), "The rival duel at the forest exit should be won.");
        Assert.IsTrue(h.State.Has("trainer:sola"), "Birdkeeper Sola watches the forest path.");
    }
}
