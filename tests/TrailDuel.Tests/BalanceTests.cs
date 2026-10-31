using TrailDuel.Rules;
using TrailDuel.World;

namespace TrailDuel.Tests;

/// <summary>Simulated duels with a sensible player (best-scoring move, switch on faint) to keep difficulty fair.</summary>
[TestClass]
public sealed class BalanceTests
{
    private static BattleOutcome Simulate(List<Creature> party, TrainerDef trainer, int seed)
    {
        var info = trainer.ToInfo();
        var b = new Battle(party, info.Team, info, new Bag(), new Random(seed));
        b.Start();
        for (int turn = 0; turn < 200 && b.Phase != BattlePhase.Ended; turn++)
        {
            if (b.Phase == BattlePhase.ForcedSwitch)
            {
                b.SubmitReplacement(party.FindIndex(c => !c.Fainted));
                continue;
            }
            int move = TrainerAI.ChooseMove(b.Player, b.PlayerStages, b.Enemy, b.EnemyStages, true, new Random(turn));
            b.Submit(new PlayerAction.Fight(move));
        }
        return b.Outcome;
    }

    private static List<Creature> Team(params (string, int)[] t) => t.Select(x => Creature.Create(x.Item1, x.Item2)).ToList();

    [TestMethod]
    public void MasterIsBeatableButNotTrivial()
    {
        var master = Maps.Get("hall").Npcs.First(n => n.Id == "odessa").Trainer!;
        int wins = 0, underleveledWins = 0;
        for (int seed = 0; seed < 10; seed++)
        {
            if (Simulate(Team(("blazehound", 15), ("mossnail", 12), ("fizzbit", 12), ("breezel", 12)), master, seed) == BattleOutcome.Won) wins++;
            if (Simulate(Team(("emberpup", 8)), master, seed) == BattleOutcome.Won) underleveledWins++;
        }
        Assert.IsTrue(wins >= 6, $"A prepared team should usually win ({wins}/10).");
        Assert.AreEqual(0, underleveledWins, "A lone level 8 starter should not beat the Master.");
    }

    [TestMethod]
    public void RouteTrainersAreFairForAFreshStarter()
    {
        foreach (var id in new[] { "mira", "bram" })
        {
            var trainer = Maps.Get("route1").Npcs.First(n => n.Id == id).Trainer!;
            int wins = 0;
            foreach (var starter in SpeciesDex.Starters)
                for (int seed = 0; seed < 4; seed++)
                    if (Simulate(Team((starter, 7)), trainer, seed) == BattleOutcome.Won) wins++;
            Assert.IsTrue(wins >= 8, $"{id}: {wins}/12 wins for level 7 starters.");
        }
    }
}
