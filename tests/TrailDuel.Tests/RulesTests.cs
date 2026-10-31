using TrailDuel.Rules;

namespace TrailDuel.Tests;

[TestClass]
public sealed class TypeChartTests
{
    [TestMethod]
    public void CoreTriangleIsSuperEffectiveOneWay()
    {
        Assert.AreEqual(2f, TypeChart.Effectiveness(ElementType.Flame, ElementType.Leaf));
        Assert.AreEqual(2f, TypeChart.Effectiveness(ElementType.Leaf, ElementType.Tide));
        Assert.AreEqual(2f, TypeChart.Effectiveness(ElementType.Tide, ElementType.Flame));
        Assert.AreEqual(0.5f, TypeChart.Effectiveness(ElementType.Leaf, ElementType.Flame));
        Assert.AreEqual(0.5f, TypeChart.Effectiveness(ElementType.Flame, ElementType.Tide));
    }

    [TestMethod]
    public void VoltCannotTouchStoneAndNormalIsResisted()
    {
        Assert.AreEqual(0f, TypeChart.Effectiveness(ElementType.Volt, ElementType.Stone));
        Assert.AreEqual(0.5f, TypeChart.Effectiveness(ElementType.Normal, ElementType.Stone));
        Assert.AreEqual(1f, TypeChart.Effectiveness(ElementType.Normal, ElementType.Gale));
    }

    [TestMethod]
    public void EffectivenessMessages()
    {
        Assert.AreEqual("It's super effective!", TypeChart.Message(2));
        Assert.AreEqual("It's not very effective...", TypeChart.Message(0.5f));
        Assert.AreEqual("It had no effect...", TypeChart.Message(0));
        Assert.IsNull(TypeChart.Message(1));
    }
}

[TestClass]
public sealed class DamageTests
{
    private static int[] Stages() => new int[4];

    private static int Hit(string attacker, string defender, string move, bool crit = false, int seed = 1)
        => Formulas.Damage(Creature.Create(attacker, 10), Stages(), Creature.Create(defender, 10), Stages(), MoveDex.Get(move), new Random(seed), crit).Damage;

    [TestMethod]
    public void SameSeedGivesSameDamage()
        => Assert.AreEqual(Hit("emberpup", "puffmop", "ember", seed: 9), Hit("emberpup", "puffmop", "ember", seed: 9));

    [TestMethod]
    public void SuperEffectiveRoughlyDoublesAndResistedFalls()
    {
        int neutral = Hit("emberpup", "puffmop", "ember");
        int super = Hit("emberpup", "sprigbun", "ember");
        int resisted = Hit("emberpup", "puddlepip", "ember");
        Assert.IsTrue(super > neutral * 1.5, $"super {super} vs neutral {neutral}");
        Assert.IsTrue(resisted < neutral, $"resisted {resisted} vs neutral {neutral}");
    }

    [TestMethod]
    public void StabBoostsMatchingType()
    {
        var target = Creature.Create("puffmop", 10);
        var flameUser = Creature.Create("emberpup", 10);
        var stab = Formulas.Damage(flameUser, Stages(), target, Stages(), MoveDex.Get("ember"), new Random(3), false);
        var plain = Formulas.Damage(flameUser, Stages(), target, Stages(), MoveDex.Get("gust"), new Random(3), false);
        Assert.IsTrue(stab.Stab);
        Assert.IsFalse(plain.Stab);
        Assert.IsTrue(stab.Damage > plain.Damage);
    }

    [TestMethod]
    public void CriticalHitsDealMore()
        => Assert.IsTrue(Hit("emberpup", "puffmop", "scratch", crit: true) > Hit("emberpup", "puffmop", "scratch", crit: false));

    [TestMethod]
    public void NoEffectDealsZero()
    {
        var r = Formulas.Damage(Creature.Create("fizzbit", 10), Stages(), Creature.Create("pebblit", 10), Stages(), MoveDex.Get("spark"), new Random(1));
        Assert.AreEqual(0, r.Damage);
        Assert.AreEqual(0f, r.Effectiveness);
    }

    [TestMethod]
    public void BurnHalvesPhysicalDamage()
    {
        var attacker = Creature.Create("bouldox", 15);
        var target = Creature.Create("puffmop", 15);
        int healthy = Formulas.Damage(attacker, Stages(), target, Stages(), MoveDex.Get("rockslam"), new Random(4), false).Damage;
        attacker.Status = StatusCondition.Burn;
        int burned = Formulas.Damage(attacker, Stages(), target, Stages(), MoveDex.Get("rockslam"), new Random(4), false).Damage;
        Assert.IsTrue(burned <= healthy / 2 + 2, $"{burned} vs {healthy}");
    }

    [TestMethod]
    public void StatStagesScaleDamage()
    {
        var attacker = Creature.Create("emberpup", 10);
        var target = Creature.Create("puffmop", 10);
        int normal = Formulas.Damage(attacker, Stages(), target, Stages(), MoveDex.Get("scratch"), new Random(2), false).Damage;
        int boosted = Formulas.Damage(attacker, [2, 0, 0, 0], target, Stages(), MoveDex.Get("scratch"), new Random(2), false).Damage;
        Assert.IsTrue(boosted > normal * 1.5, $"{boosted} vs {normal}");
        Assert.AreEqual(2f, Formulas.StageMultiplier(2));
        Assert.AreEqual(0.5f, Formulas.StageMultiplier(-2));
    }

    [TestMethod]
    public void AccuracyAndCritRollsAreDeterministicAndCalibrated()
    {
        var a = new Random(42);
        var b = new Random(42);
        var thunder = MoveDex.Get("thundercrash");
        for (int i = 0; i < 50; i++) Assert.AreEqual(Formulas.RollHit(thunder, a), Formulas.RollHit(thunder, b));

        var rng = new Random(5);
        int hits = Enumerable.Range(0, 20000).Count(_ => Formulas.RollHit(thunder, rng));
        Assert.AreEqual(0.85, hits / 20000.0, 0.02);
        Assert.IsTrue(Enumerable.Range(0, 1000).All(_ => Formulas.RollHit(MoveDex.Get("hunker"), rng)));

        int crits = Enumerable.Range(0, 32000).Count(_ => Formulas.RollCritical(MoveDex.Get("tackle"), rng));
        int highCrits = Enumerable.Range(0, 32000).Count(_ => Formulas.RollCritical(MoveDex.Get("wingcut"), rng));
        Assert.AreEqual(1 / 16.0, crits / 32000.0, 0.01);
        Assert.AreEqual(1 / 8.0, highCrits / 32000.0, 0.01);
    }
}

[TestClass]
public sealed class ProgressionTests
{
    [TestMethod]
    public void XpCurveIsIncreasing()
    {
        for (int l = 2; l < Creature.MaxLevel; l++) Assert.IsTrue(Creature.XpForLevel(l + 1) > Creature.XpForLevel(l));
    }

    [TestMethod]
    public void GainingXpLevelsUpAndGrowsStats()
    {
        var c = Creature.Create("emberpup", 5);
        int hpBefore = c.Hp, maxBefore = c.MaxHp, atkBefore = c.Attack;
        var ups = Progression.GainXp(c, Creature.XpForLevel(7) - c.Xp, out _);
        Assert.AreEqual(2, ups.Count);
        Assert.AreEqual(7, c.Level);
        Assert.IsTrue(c.Attack > atkBefore);
        Assert.AreEqual(hpBefore + (c.MaxHp - maxBefore), c.Hp);
    }

    [TestMethod]
    public void LearnsMoveWithFreeSlotAutomatically()
    {
        var c = Creature.Create("emberpup", 8);
        Assert.AreEqual(3, c.Moves.Count);
        var ups = Progression.GainXp(c, Creature.XpForLevel(9) - c.Xp, out var pending);
        Assert.IsTrue(c.Knows("quickdash"));
        CollectionAssert.Contains(ups[0].NewMoves.ToList(), "quickdash");
        Assert.AreEqual(0, pending.Count);
    }

    [TestMethod]
    public void FifthMoveIsOfferedNotForced()
    {
        var c = Creature.Create("emberpup", 12);
        Assert.AreEqual(4, c.Moves.Count);
        Progression.GainXp(c, Creature.XpForLevel(13) - c.Xp, out var pending);
        CollectionAssert.AreEqual(new[] { "flamefang" }, pending);
        Assert.IsFalse(c.Knows("flamefang"));
        Progression.ReplaceMove(c, 0, "flamefang");
        Assert.IsTrue(c.Knows("flamefang"));
        Assert.AreEqual(MoveDex.Get("flamefang").Pp, c.Moves[0].Pp);
    }

    [TestMethod]
    public void StartersEvolveAtTwelve()
    {
        var c = Creature.Create("puddlepip", 11);
        Assert.IsFalse(c.CanEvolve);
        Progression.GainXp(c, Creature.XpForLevel(12) - c.Xp, out _);
        Assert.IsTrue(c.CanEvolve);
        c.Hp -= 5;
        c.EvolveInto("tidalotl");
        Assert.AreEqual("Tidefang", c.Name);
        Assert.AreEqual(c.MaxHp - 5, c.Hp);
    }

    [TestMethod]
    [DataRow("breezel", "gustowl", 10)]
    [DataRow("pebblit", "bouldox", 12)]
    [DataRow("fizzbit", "voltusk", 12)]
    public void WildCreaturesEvolveIntoTheirLine(string from, string to, int level)
    {
        var c = Creature.Create(from, level - 1);
        Assert.IsFalse(c.CanEvolve);
        Progression.GainXp(c, Creature.XpForLevel(level) - c.Xp, out _);
        Assert.IsTrue(c.CanEvolve);
        Assert.AreEqual(to, c.Species.EvolvesInto);
        Assert.AreEqual(SpeciesDex.Get(from).Type, SpeciesDex.Get(to).Type);
    }

    [TestMethod]
    public void EvolvedFormsKeepEveryMoveTheirBaseLearnsBeforeEvolving()
    {
        foreach (var s in SpeciesDex.All.Where(s => s.EvolvesInto is not null))
        {
            var evolved = SpeciesDex.Get(s.EvolvesInto!);
            foreach (var (level, move) in s.Learnset.Where(l => l.Level < s.EvolveLevel))
                Assert.IsTrue(evolved.Learnset.Any(l => l.Move == move && l.Level <= level), $"{evolved.Id} should learn {move} by level {level}.");
        }
    }

    [TestMethod]
    public void EvolvedFormsComeRightAfterTheirBaseInTheDex()
    {
        foreach (var s in SpeciesDex.All.Where(s => s.EvolvesInto is not null))
            Assert.AreEqual(SpeciesDex.Number(s.Id) + 1, SpeciesDex.Number(s.EvolvesInto!), s.Id);
    }

    [TestMethod]
    [DataRow("puffmop")]
    [DataRow("mossnail")]
    public void CreaturesThatNeverEvolveStandBetweenBaseAndEvolvedForms(string id)
    {
        static int Total(SpeciesData s) => s.Base.Hp + s.Base.Attack + s.Base.Defense + s.Base.Special + s.Base.Speed;
        var species = SpeciesDex.Get(id);
        Assert.IsNull(species.EvolvesInto);
        int strongestBase = SpeciesDex.All.Where(s => s.EvolvesInto is not null).Max(Total);
        int weakestEvolved = SpeciesDex.All.Where(s => s.EvolvesInto is not null).Min(s => Total(SpeciesDex.Get(s.EvolvesInto!)));
        Assert.IsTrue(Total(species) > strongestBase && Total(species) < weakestEvolved, $"{id}: {Total(species)}");
    }

    [TestMethod]
    public void TrainerBattlesPayMoreXp()
    {
        var foe = Creature.Create("pebblit", 8);
        Assert.IsTrue(Formulas.ExperienceFor(foe, true) > Formulas.ExperienceFor(foe, false));
    }
}

[TestClass]
public sealed class CaptureTests
{
    [TestMethod]
    public void ChanceStaysWithinBoundsAndRisesAsHpFalls()
    {
        foreach (var s in SpeciesDex.All)
        {
            var c = Creature.Create(s.Id, 10);
            float full = Formulas.CaptureChance(c, 1);
            c.Hp = 1;
            float low = Formulas.CaptureChance(c, 1);
            Assert.IsTrue(full > 0 && full <= 1, s.Id);
            Assert.IsTrue(low >= full && low <= 1, s.Id);
        }
    }

    [TestMethod]
    public void StatusAndBetterOrbsHelp()
    {
        var c = Creature.Create("gustowl", 8);
        c.Hp = c.MaxHp / 2;
        float plain = Formulas.CaptureChance(c, 1);
        float super = Formulas.CaptureChance(c, ItemDex.SuperOrb.OrbMultiplier);
        c.Status = StatusCondition.Sleep;
        float asleep = Formulas.CaptureChance(c, 1);
        Assert.IsTrue(super > plain);
        Assert.IsTrue(asleep > plain);
    }

    [TestMethod]
    public void EasyCatchAtOneHpIsCertainAndRollsAreDeterministic()
    {
        var c = Creature.Create("breezel", 3);
        c.Hp = 1;
        c.Status = StatusCondition.Sleep;
        Assert.AreEqual(1f, Formulas.CaptureChance(c, 1.5f));
        Assert.IsTrue(Formulas.RollCapture(c, 1.5f, new Random(1)).Caught);
        var tough = Creature.Create("bouldox", 14);
        Assert.AreEqual(Formulas.RollCapture(tough, 1, new Random(77)), Formulas.RollCapture(tough, 1, new Random(77)));
    }
}
