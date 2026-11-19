using FsmCompare;

namespace Benchmarks.Tests;

/// <summary>
/// FSM-1: the three versions of the enemy (by hand, on Stateless, on the engine's machine) can be compared only if
/// they are the same enemy. Each must show the same state, time, hitbox and attack count at every step of the script.
/// </summary>
[TestClass]
public class FsmCompareTests
{
    [TestMethod]
    public void The_three_enemies_behave_the_same_at_every_step()
    {
        const int steps = 2400, killAt = 2000;
        var hand = Compare.Trace(new HandEnemy(), steps, killAt);
        CollectionAssert.AreEqual(hand, Compare.Trace(new StatelessEnemy(), steps, killAt), "Stateless");
        CollectionAssert.AreEqual(hand, Compare.Trace(new EngineEnemy(), steps, killAt), "engine");
    }

    [TestMethod]
    public void The_script_visits_every_state()
    {
        var seen = new HashSet<Mode>();
        var enemy = new EngineEnemy();
        int live = 0;
        for (int i = 0; i < 2400; i++)
        {
            enemy.Step(Compare.SenseAt(i, 2000), Compare.Dt);
            seen.Add(enemy.Mode);
            if (enemy.HitboxLive) live++;
        }
        CollectionAssert.AreEquivalent(new[] { Mode.Patrol, Mode.Chase, Mode.Attack, Mode.Stunned, Mode.Dead }, seen.ToArray());
        Assert.IsTrue(enemy.Attacks > 3 && live > 0, "the attack's entry and exit ran");
        Assert.IsFalse(enemy.HitboxLive, "a hit or a kill during an attack still runs its exit");
    }
}
