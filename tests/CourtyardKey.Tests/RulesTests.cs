using System.Numerics;
using CourtyardKey.Model;
using MicroEngine.Core;

namespace CourtyardKey.Tests;

[TestClass]
public sealed class RulesTests
{
    [TestMethod]
    public void DamageIsMeasuredInHalfHeartsAndGrantsInvulnerability()
    {
        var hp = new Health(6);
        Assert.IsTrue(hp.TryDamage(1, Combat.HeroInvulnerability));
        Assert.AreEqual(5, hp.Current);
        Assert.IsTrue(hp.IsInvulnerable);
        Assert.IsFalse(hp.TryDamage(2, Combat.HeroInvulnerability), "A second hit during i-frames must be ignored.");
        Assert.AreEqual(5, hp.Current);
        hp.Update(Combat.HeroInvulnerability + 0.01f);
        Assert.IsTrue(hp.TryDamage(2, Combat.HeroInvulnerability));
        Assert.AreEqual(3, hp.Current);
    }

    [TestMethod]
    public void HealthNeverDropsBelowZeroOrHealsPastMax()
    {
        var hp = new Health(4);
        Assert.IsTrue(hp.TryDamage(9, 0));
        Assert.AreEqual(0, hp.Current);
        Assert.IsTrue(hp.IsDead);
        Assert.AreEqual(0, hp.Heal(2), "The dead do not heal.");
        var alive = new Health(4, 1);
        Assert.AreEqual(3, alive.Heal(10));
        Assert.AreEqual(4, alive.Current);
        alive.RaiseMax(2, fill: true);
        Assert.AreEqual(6, alive.Max);
        Assert.AreEqual(6, alive.Current);
    }

    [TestMethod]
    public void KnockbackPushesAwayFromTheAttackerAlongTheDominantAxis()
    {
        Assert.AreEqual(new Vector2(1, 0), Combat.KnockbackDirection(new Vector2(0, 0), new Vector2(10, 3)));
        Assert.AreEqual(new Vector2(0, -1), Combat.KnockbackDirection(new Vector2(5, 20), new Vector2(4, 2)));
        Assert.AreEqual(new Vector2(0, 1), Combat.KnockbackDirection(Vector2.One, Vector2.One), "Overlapping bodies get a defined push.");
    }

    [TestMethod]
    public void ShieldBlocksFrontalStrikesButNotFlanksOrWhileStunned()
    {
        var knight = new Vector2(100, 100);
        Assert.IsTrue(Combat.ShieldBlocks(Dir.Down, knight, new Vector2(100, 120), stunned: false), "Straight on is blocked.");
        Assert.IsTrue(Combat.ShieldBlocks(Dir.Down, knight, new Vector2(108, 120), stunned: false), "Slightly off-centre is still blocked.");
        Assert.IsFalse(Combat.ShieldBlocks(Dir.Down, knight, new Vector2(120, 100), stunned: false), "A flank gets through.");
        Assert.IsFalse(Combat.ShieldBlocks(Dir.Down, knight, new Vector2(100, 80), stunned: false), "The back gets through.");
        Assert.IsFalse(Combat.ShieldBlocks(Dir.Down, knight, new Vector2(100, 120), stunned: true), "A stunned knight drops its guard.");
    }

    [TestMethod]
    public void SmallKeysAndGemsFollowInventoryRules()
    {
        var p = new Progress();
        Assert.IsFalse(p.TryUseSmallKey());
        p.SmallKeys = 1;
        Assert.IsTrue(p.TryUseSmallKey());
        Assert.AreEqual(0, p.SmallKeys);
        p.AddGems(5000);
        Assert.AreEqual(Progress.MaxGems, p.Gems);
        Assert.IsFalse(p.TrySpend(Progress.MaxGems + 1));
        Assert.IsTrue(p.TrySpend(9));
        Assert.AreEqual(Progress.MaxGems - 9, p.Gems);
    }

    [TestMethod]
    public void SaveRoundTripPreservesTheWholeAdventure()
    {
        string dir = Path.Combine(Path.GetTempPath(), "courtyard-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var slots = new SaveSlots(new SaveStore("CourtyardKey", dir));
            Assert.IsFalse(slots.HasSave);
            var p = new Progress
            {
                MaxHealth = 10, Health = 7, Gems = 123, SmallKeys = 2, HasSword = true, HasBoomerang = true,
                HasBossKey = true, Deaths = 3, PlaySeconds = 456.5,
                Entrance = new Checkpoint("dungeon", 1, 3, 10f, 8.2f, Dir.Up),
            };
            p.Set("chest:dungeon:0,2:0");
            p.MarkVisited("dungeon", 1, 2);
            slots.Save(p);
            Assert.IsTrue(slots.HasSave);
            var loaded = slots.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(10, loaded.MaxHealth);
            Assert.AreEqual(7, loaded.Health);
            Assert.AreEqual(123, loaded.Gems);
            Assert.AreEqual(2, loaded.SmallKeys);
            Assert.IsTrue(loaded.HasSword && loaded.HasBoomerang && loaded.HasBossKey);
            Assert.IsFalse(loaded.HasCourtyardKey);
            Assert.AreEqual(3, loaded.Deaths);
            Assert.AreEqual(456.5, loaded.PlaySeconds, 1e-9);
            Assert.AreEqual(p.Entrance, loaded.Entrance);
            Assert.IsTrue(loaded.Has("chest:dungeon:0,2:0"));
            Assert.IsTrue(loaded.WasVisited("dungeon", 1, 2));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void CorruptSaveIsTreatedAsNoSave()
    {
        string dir = Path.Combine(Path.GetTempPath(), "courtyard-tests", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, SaveSlots.Slot + ".json"), "{ not json");
            Assert.IsNull(new SaveSlots(new SaveStore("CourtyardKey", dir)).Load());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
