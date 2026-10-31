using MicroEngine.Core;
using TrailDuel.Rules;
using TrailDuel.Scenes;
using TrailDuel.World;

namespace TrailDuel.Tests;

[TestClass]
public sealed class BattleTests
{
    private static Creature With(string species, int level, params string[] moves)
    {
        var c = Creature.Create(species, level);
        if (moves.Length > 0) c.Moves = moves.Select(m => new MoveSlot(m)).ToList();
        return c;
    }

    private static TrainerInfo Trainer(params Creature[] team) => new("Tester", team, 100, "Well done!");

    [TestMethod]
    public void BurnTicksAnEighthAtEndOfTurn()
    {
        var player = With("puffmop", 10, "hunker");
        var foe = With("bouldox", 10, "harden");
        player.Status = StatusCondition.Burn;
        var b = new Battle([player], [foe], null, new Bag(), new Random(1));
        b.Start();
        int before = player.Hp;
        var ev = b.Submit(new PlayerAction.Fight(0));
        Assert.AreEqual(before - player.MaxHp / 8, player.Hp);
        Assert.IsTrue(ev.OfType<StatusTickEvent>().Any(t => t.Target == Side.Player));
    }

    [TestMethod]
    public void FlameTypesCannotBeBurned()
    {
        // Ember's burn chance never applies to a Flame target, however many times it lands.
        var player = With("puffmop", 5, "ember");
        var foe = With("emberpup", 30, "hunker");
        var b = new Battle([player], [foe], null, new Bag(), new Random(1));
        for (int i = 0; i < 25; i++)
        {
            player.Moves[0].Pp = 25;
            b.Submit(new PlayerAction.Fight(0));
            Assert.AreNotEqual(StatusCondition.Burn, foe.Status);
        }
    }

    [TestMethod]
    public void SleepSkipsTurnsThenWakes()
    {
        var player = With("puffmop", 10, "hunker");
        var foe = With("pebblit", 10, "tackle");
        foe.Status = StatusCondition.Sleep;
        foe.SleepTurns = 2;
        var b = new Battle([player], [foe], null, new Bag(), new Random(3));
        var first = b.Submit(new PlayerAction.Fight(0));
        Assert.IsTrue(first.OfType<MessageEvent>().Any(m => m.Text.Contains("fast asleep")));
        Assert.AreEqual(player.MaxHp, player.Hp);
        var second = b.Submit(new PlayerAction.Fight(0));
        Assert.IsTrue(second.OfType<MessageEvent>().Any(m => m.Text.Contains("woke up")));
        Assert.AreEqual(StatusCondition.None, foe.Status);
    }

    [TestMethod]
    public void PoisonMoveAppliesStatus()
    {
        var player = With("mossnail", 20, "toxicspore");
        var foe = With("puffmop", 5, "hunker");
        var b = new Battle([player], [foe], null, new Bag(), new Random(8));
        for (int i = 0; i < 4 && foe.Status != StatusCondition.Poison; i++) b.Submit(new PlayerAction.Fight(0));
        Assert.AreEqual(StatusCondition.Poison, foe.Status);
    }

    [TestMethod]
    public void StatMovesChangeStages()
    {
        var player = With("pebblit", 10, "harden");
        var foe = With("puffmop", 10, "glare");
        var b = new Battle([player], [foe], null, new Bag(), new Random(2));
        b.Submit(new PlayerAction.Fight(0));
        Assert.AreEqual(2, b.PlayerStages[(int)Stat.Defense]);
        Assert.AreEqual(-1, b.PlayerStages[(int)Stat.Attack]);
    }

    [TestMethod]
    public void FasterCreatureMovesFirstAndPriorityBeatsSpeed()
    {
        var slow = With("bouldox", 10, "quickdash");
        var fast = With("fizzbit", 10, "tackle");
        var b = new Battle([slow], [fast], null, new Bag(), new Random(1));
        var ev = b.Submit(new PlayerAction.Fight(0));
        var firstMove = ev.OfType<MoveUsedEvent>().First();
        Assert.AreEqual(Side.Player, firstMove.User, "Quick Dash has priority over a faster foe.");
    }

    [TestMethod]
    public void FaintingForcesASwitchAndFaintedCreaturesCannotFight()
    {
        var weak = With("breezel", 2, "tackle");
        weak.Hp = 1;
        var backup = With("bouldox", 20, "rockslam");
        var foe = With("voltusk", 20, "bodyslam");
        var b = new Battle([weak, backup], [foe], null, new Bag(), new Random(4));
        b.Submit(new PlayerAction.Fight(0));
        Assert.AreEqual(BattlePhase.ForcedSwitch, b.Phase);
        Assert.IsTrue(weak.Fainted);
        Assert.ThrowsExactly<InvalidOperationException>(() => b.SubmitReplacement(0));
        b.SubmitReplacement(1);
        Assert.AreEqual(1, b.ActiveIndex);
        Assert.AreEqual(BattlePhase.ChooseAction, b.Phase);
        Assert.IsFalse(b.CanSubmit(new PlayerAction.Switch(0), out _));
    }

    [TestMethod]
    public void LosingEveryCreatureEndsTheBattle()
    {
        var weak = With("breezel", 2, "tackle");
        weak.Hp = 1;
        var b = new Battle([weak], [With("voltusk", 20, "bodyslam")], null, new Bag(), new Random(4));
        var ev = b.Submit(new PlayerAction.Fight(0));
        Assert.AreEqual(BattleOutcome.Lost, b.Outcome);
        Assert.IsInstanceOfType<EndEvent>(ev[^1]);
    }

    [TestMethod]
    public void TrainersSendTheirNextCreatureAndPayPrizeMoney()
    {
        var hero = With("blazehound", 30, "blazerush");
        var first = With("sprigbun", 3, "tackle");
        var second = With("mossnail", 3, "tackle");
        var b = new Battle([hero], [first, second], Trainer(first, second), new Bag(), new Random(1));
        var ev = b.Submit(new PlayerAction.Fight(0));
        Assert.AreEqual(1, b.EnemyIndex);
        Assert.IsTrue(ev.OfType<SendOutEvent>().Any(s => s.Side == Side.Enemy));
        ev = b.Submit(new PlayerAction.Fight(0));
        Assert.AreEqual(BattleOutcome.Won, b.Outcome);
        Assert.AreEqual(100, ev.OfType<MoneyEvent>().Single().Amount);
    }

    [TestMethod]
    public void NoRunningOrCatchingInTrainerDuels()
    {
        var foe = With("pebblit", 5);
        var bag = new Bag();
        bag.Add("orb");
        var b = new Battle([With("emberpup", 5)], [foe], Trainer(foe), bag, new Random(1));
        Assert.IsFalse(b.CanSubmit(new PlayerAction.Run(), out _));
        Assert.IsFalse(b.CanSubmit(new PlayerAction.UseItem("orb", -1), out _));
    }

    [TestMethod]
    public void CatchingAddsToPartyAndUsesTheOrb()
    {
        var wild = With("breezel", 3, "tackle");
        wild.Hp = 1;
        wild.Status = StatusCondition.Sleep;
        wild.SleepTurns = 3;
        var party = new List<Creature> { With("emberpup", 5) };
        var bag = new Bag();
        bag.Add("superorb");
        var b = new Battle(party, [wild], null, bag, new Random(1));
        b.Submit(new PlayerAction.UseItem("superorb", -1));
        Assert.AreEqual(BattleOutcome.Caught, b.Outcome);
        Assert.AreEqual(2, party.Count);
        Assert.AreEqual(0, bag.Count("superorb"));
    }

    [TestMethod]
    public void PotionsHealInBattle()
    {
        var hurt = With("emberpup", 10, "scratch");
        hurt.Hp = 5;
        var bag = new Bag();
        bag.Add("potion");
        var b = new Battle([hurt], [With("puffmop", 2, "hunker")], null, bag, new Random(1));
        b.Submit(new PlayerAction.UseItem("potion", 0));
        Assert.AreEqual(Math.Min(hurt.MaxHp, 25), hurt.Hp);
        Assert.AreEqual(0, bag.Count("potion"));
    }

    [TestMethod]
    public void WinningGrantsXpToTheActiveCreature()
    {
        var hero = With("emberpup", 5, "scratch");
        var foe = With("puffmop", 5, "hunker");
        foe.Hp = 1;
        int xp = hero.Xp;
        var b = new Battle([hero], [foe], null, new Bag(), new Random(1));
        var ev = b.Submit(new PlayerAction.Fight(0));
        Assert.AreEqual(BattleOutcome.Won, b.Outcome);
        Assert.AreEqual(xp + Formulas.ExperienceFor(foe, false), hero.Xp);
        Assert.IsTrue(ev.OfType<XpEvent>().Any());
    }
}

[TestClass]
public sealed class AiTests
{
    [TestMethod]
    public void TrainerPicksTheSuperEffectiveMove()
    {
        var user = Creature.Create("fizzbit", 12);
        user.Moves = [new MoveSlot("tackle"), new MoveSlot("voltbolt"), new MoveSlot("statichum")];
        var target = Creature.Create("puddlepip", 12);
        for (int seed = 0; seed < 20; seed++)
            Assert.AreEqual(1, TrainerAI.ChooseMove(user, new int[4], target, new int[4], smart: true, new Random(seed)));
    }

    [TestMethod]
    public void TrainerAvoidsMovesThatCannotHit()
    {
        var user = Creature.Create("fizzbit", 12);
        user.Moves = [new MoveSlot("voltbolt"), new MoveSlot("tackle")];
        var stone = Creature.Create("bouldox", 12);
        Assert.AreEqual(1, TrainerAI.ChooseMove(user, new int[4], stone, new int[4], smart: true, new Random(3)));
    }

    [TestMethod]
    public void NoPpMeansStruggle()
    {
        var user = Creature.Create("fizzbit", 12);
        foreach (var m in user.Moves) m.Pp = 0;
        Assert.AreEqual(-1, TrainerAI.ChooseMove(user, new int[4], Creature.Create("puffmop", 5), new int[4], true, new Random(1)));
    }
}

[TestClass]
public sealed class PersistenceTests
{
    [TestMethod]
    public void SaveAndLoadRoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "trailduel-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SaveStore("TrailDuel", dir);
            var s = GameState.NewGame();
            s.MapId = "route1";
            (s.X, s.Y, s.Facing) = (9, 12, Direction.Left);
            s.AddCreature(Creature.Create("sprigbun", 9));
            s.Party[0].Status = StatusCondition.Poison;
            s.Party[0].Moves[0].Pp = 3;
            s.Set("starter");
            s.Bag.Add("superorb", 2);
            s.Money = 1234;
            s.Save(store);

            var loaded = GameState.Load(store)!;
            Assert.AreEqual("route1", loaded.MapId);
            Assert.AreEqual((9, 12, Direction.Left), (loaded.X, loaded.Y, loaded.Facing));
            Assert.AreEqual(1234, loaded.Money);
            Assert.IsTrue(loaded.Has("starter"));
            Assert.AreEqual(2, loaded.Bag.Count("superorb"));
            Assert.AreEqual("sprigbun", loaded.Party[0].SpeciesId);
            Assert.AreEqual(9, loaded.Party[0].Level);
            Assert.AreEqual(StatusCondition.Poison, loaded.Party[0].Status);
            Assert.AreEqual(3, loaded.Party[0].Moves[0].Pp);
            Assert.IsTrue(loaded.Caught.Contains("sprigbun"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void WhiteOutHealsReturnsHomeAndCostsHalfTheMoney()
    {
        var s = GameState.NewGame();
        s.AddCreature(Creature.Create("emberpup", 6));
        s.Party[0].Hp = 0;
        s.Money = 1000;
        s.MapId = "forest";
        int lost = s.WhiteOut();
        Assert.AreEqual(500, lost);
        Assert.AreEqual(500, s.Money);
        Assert.AreEqual("home", s.MapId);
        Assert.IsFalse(s.Party[0].Fainted);
    }

    [TestMethod]
    public void ShopChargesAndRefusesWhenBroke()
    {
        var s = GameState.NewGame();
        s.Money = 500;
        Assert.IsTrue(ShopScene.TryBuy(s, ItemDex.Orb, 3));
        Assert.AreEqual(50, s.Money);
        Assert.AreEqual(3, s.Bag.Count("orb"));
        Assert.IsFalse(ShopScene.TryBuy(s, ItemDex.Potion, 1));
        Assert.AreEqual(50, s.Money);
    }

    [TestMethod]
    public void PartyIsCappedAtSixAndOverflowGoesToTheBox()
    {
        var s = GameState.NewGame();
        for (int i = 0; i < 7; i++) s.AddCreature(Creature.Create("breezel", 3));
        Assert.AreEqual(6, s.Party.Count);
        Assert.AreEqual(1, s.Box.Count);
    }
}

[TestClass]
public sealed class WorldTests
{
    [TestMethod]
    public void EveryMapIsRectangularAndWarpsLandOnWalkableTiles()
    {
        var state = GameState.NewGame();
        foreach (var map in Maps.Every)
        {
            Assert.IsTrue(map.Rows.All(r => r.Length == map.Width), $"{map.Id} rows differ in width");
            foreach (var w in map.Warps)
            {
                var target = new MapRuntime(Maps.Get(w.To), state);
                Assert.IsFalse(target.TileSolid(w.ToX, w.ToY), $"{map.Id} warp to {w.To} {w.ToX},{w.ToY} lands on a solid tile");
            }
            var runtime = new MapRuntime(map, state);
            foreach (var npc in map.Npcs) Assert.IsFalse(runtime.TileSolid(npc.X, npc.Y), $"{map.Id}:{npc.Id} stands on a solid tile");
            foreach (var item in map.Items) Assert.IsFalse(runtime.TileSolid(item.X, item.Y), $"{map.Id} item at {item.X},{item.Y}");
        }
    }

    [TestMethod]
    public void LedgesOnlyAllowHoppingDown()
    {
        var map = new MapRuntime(Maps.Get("route1"), GameState.NewGame());
        var player = new Actor(Visuals.CharacterLook.Player, 4, 10, Direction.Down);
        Assert.AreEqual(StepKind.Hop, map.CheckStep(player, Direction.Down, player));
        var below = new Actor(Visuals.CharacterLook.Player, 4, 12, Direction.Up);
        Assert.AreEqual(StepKind.Blocked, map.CheckStep(below, Direction.Up, below));
    }

    [TestMethod]
    public void TrainersSeeAlongTheirFacingLine()
    {
        var map = new MapRuntime(Maps.Get("route1"), GameState.NewGame());
        var mira = map.Npcs.First(n => n.Def!.Id == "mira");
        Assert.IsTrue(map.TrainerSees(mira, 10, 5));
        Assert.IsFalse(map.TrainerSees(mira, 10, 6));
        Assert.IsFalse(map.TrainerSees(mira, 15, 5));
    }

    [TestMethod]
    public void EncounterTablesOnlyRollTheirSpecies()
    {
        var table = Maps.Get("forest").Encounters!;
        var rng = new Random(1);
        for (int i = 0; i < 200; i++)
        {
            var c = table.Roll(rng);
            var slot = table.Slots.Single(s => s.Species == c.SpeciesId);
            Assert.IsTrue(c.Level >= slot.MinLevel && c.Level <= slot.MaxLevel);
        }
    }
}
