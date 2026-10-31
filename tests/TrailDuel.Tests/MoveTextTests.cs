using TrailDuel.Rules;

namespace TrailDuel.Tests;

/// <summary>Move descriptions shown to the player state exactly what the battle rules do.</summary>
[TestClass]
public sealed class MoveTextTests
{
    private static MoveData M(string id) => MoveDex.Get(id);

    [TestMethod]
    public void PlainAttackShowsPowerAccuracyAndNoAddedEffect()
    {
        var tackle = M("tackle");
        Assert.AreEqual("Physical", MoveText.Category(tackle));
        Assert.AreEqual("40", MoveText.Power(tackle));
        Assert.AreEqual("100%", MoveText.Accuracy(tackle));
        Assert.AreEqual("A full-body charge. No added effect.", MoveText.Describe(tackle));
    }

    [TestMethod]
    public void SecondaryStatusStatesItsChanceAndWhatTheStatusDoes()
    {
        Assert.AreEqual("Tiny flames. 10% chance to burn the foe (it loses 1/8 of its max HP each turn and its physical attacks deal half damage). Flame types can't be burned.",
            MoveText.Describe(M("ember")));
        Assert.AreEqual("Sprays bubbles. 15% chance to lower the foe's Speed by 1 stage.", MoveText.Describe(M("bubble")));
    }

    [TestMethod]
    public void StatusMovesHaveNoPowerAndExplainTheirEffect()
    {
        var spores = M("toxicspore");
        Assert.AreEqual("Status", MoveText.Category(spores));
        Assert.AreEqual("-", MoveText.Power(spores));
        Assert.AreEqual("Poisons the foe: it loses 1/8 of its max HP each turn. Fails if the foe already has a status.", MoveText.Effects(spores));
        Assert.AreEqual("Puts the foe to sleep for 1 to 3 turns. Fails if the foe already has a status.", MoveText.Effects(M("lullaby")));
    }

    [TestMethod]
    public void StatChangesNameTheStatStagesAndTarget()
    {
        Assert.AreEqual("Raises your Defense by 2 stages.", MoveText.Effects(M("harden")));
        Assert.AreEqual("Lowers the foe's Attack by 1 stage.", MoveText.Effects(M("glare")));
    }

    [TestMethod]
    public void HealingDrainPriorityAndCriticalsAreExplained()
    {
        Assert.AreEqual("Restores half of your max HP. Fails at full HP.", MoveText.Effects(M("soothe")));
        Assert.AreEqual("Heals you by half the damage dealt.", MoveText.Effects(M("sapsip")));
        Assert.AreEqual("Strikes before ordinary moves.", MoveText.Effects(M("quickdash")));
        Assert.AreEqual("High critical-hit rate (1 in 8 instead of 1 in 16).", MoveText.Effects(M("wingcut")));
    }

    [TestMethod]
    public void MovesThatCannotMissShowNoAccuracy()
    {
        var hunker = M("hunker");
        Assert.IsTrue(hunker.NeverMisses);
        Assert.AreEqual("-", MoveText.Accuracy(hunker));
    }

    [TestMethod]
    public void EveryMoveHasAFullSentenceDescription()
    {
        foreach (var move in MoveDex.All)
        {
            var text = MoveText.Describe(move);
            Assert.IsFalse(string.IsNullOrWhiteSpace(text), move.Id);
            Assert.IsTrue(text.EndsWith('.'), $"{move.Id}: '{text}'");
        }
    }

    [TestMethod]
    public void MatchupAgainstAFoeUsesTheTypeChart()
    {
        Assert.AreEqual("Super effective", MoveText.Matchup(M("ember"), ElementType.Leaf));
        Assert.AreEqual("Not very effective", MoveText.Matchup(M("ember"), ElementType.Tide));
        Assert.AreEqual("No effect", MoveText.Matchup(M("spark"), ElementType.Stone));
        Assert.IsNull(MoveText.Matchup(M("tackle"), ElementType.Gale), "Neutral hits need no label.");
        Assert.IsNull(MoveText.Matchup(M("harden"), ElementType.Leaf), "Status moves have no matchup.");
    }
}
