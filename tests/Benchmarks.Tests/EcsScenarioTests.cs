using EcsBench;
using EcsBench.Prototype;
using StressBench;

namespace Benchmarks.Tests;

/// <summary>
/// ENTITY-1: the entity workloads compare stores only if every store does the same work. Each workload ends in the same
/// state on the engine's world, on Arch, on Friflo.Engine.ECS and on the first-party prototype.
/// </summary>
[TestClass]
public class EcsScenarioTests
{
    private static readonly StressOptions Short = new() { Command = "run", Frames = 40, Warmup = 8 };

    [ClassInitialize]
    public static void Register(TestContext _) => EcsScenarios.Register();

    public static IEnumerable<object[]> Workloads => EcsScenarios.WorkloadNames.Select(w => new object[] { w });

    [TestMethod]
    [DynamicData(nameof(Workloads))]
    public void Every_store_ends_a_workload_in_the_same_state(string workload)
    {
        var names = EcsScenarios.Of(workload);
        Assert.IsTrue(names.Count >= 4, string.Join(", ", names));
        var runs = names.Select(n => Harness.Run(n, 3_000, RunMode.Headless, Short)).ToList();

        foreach (var run in runs)
        {
            Assert.AreEqual(runs[0].Checksum, run.Checksum, $"{run.Scenario} against {runs[0].Scenario}: positions, lifetimes, health and what the queries found");
            Assert.AreEqual(runs[0].Resources["entities"], run.Resources["entities"], run.Scenario);
            Assert.AreEqual(runs[0].Resources["entitiesCreated"], run.Resources["entitiesCreated"], run.Scenario);
            Assert.AreEqual(runs[0].DrawsPerFrame, run.DrawsPerFrame, run.Scenario);
        }
    }

    [TestMethod]
    public void The_workloads_do_what_they_say()
    {
        var move = Harness.Run("ecs-move-simple", 3_000, RunMode.Headless, Short);
        Assert.AreEqual(3_000, move.Resources["entities"]);
        Assert.AreEqual(3_000, move.DrawsPerFrame, "every object is drawn");

        var churn = Harness.Run("ecs-churn-proto", 3_000, RunMode.Headless, Short);
        Assert.AreEqual(3_000, churn.Resources["entities"], "what dies is created again");
        Assert.AreEqual(3_000 + 48 * 300, churn.Resources["entitiesCreated"], "a tenth replaced in each of the 48 steps");

        var mixed = Harness.Run("ecs-mixed-friflo", 3_000, RunMode.Headless, Short);
        Assert.AreEqual(4_500, mixed.Resources["entities"], "the swarm and half as many shots");
        Assert.AreEqual(4_500, mixed.DrawsPerFrame);
        Assert.IsTrue(mixed.Resources["entitiesCreated"] > 4_500 + 500, $"shots live 30 to 59 steps, so most of the 1,500 expired and were fired again: {mixed.Resources["entitiesCreated"]}");
    }

    [TestMethod]
    public void A_different_load_or_more_steps_change_the_state()
    {
        long one = Harness.Run("ecs-mixed-arch", 3_000, RunMode.Headless, Short).Checksum;
        Assert.AreNotEqual(one, Harness.Run("ecs-mixed-arch", 3_001, RunMode.Headless, Short).Checksum);
        Assert.AreNotEqual(one, Harness.Run("ecs-mixed-arch", 3_000, RunMode.Headless, Short with { Frames = 41 }).Checksum);
    }

    [TestMethod]
    public void Walking_a_component_store_allocates_nothing_per_frame()
    {
        var steady = Short with { Frames = 60, Warmup = 30 };
        foreach (string scenario in new[] { "ecs-move-simple", "ecs-move-arch", "ecs-move-friflo", "ecs-move-proto", "ecs-query-simple", "ecs-query-arch", "ecs-query-friflo", "ecs-query-proto" })
            Assert.IsTrue(Harness.Run(scenario, 2_000, RunMode.Headless, steady).AllocatedPerFrame < 64, scenario);
    }

    [TestMethod]
    public void A_scenario_is_registered_once_and_only_for_the_stores_it_applies_to()
    {
        EcsScenarios.Register();
        Assert.AreEqual(1, Scenarios.Names.Count(n => n == "ecs-move-arch"));
        CollectionAssert.DoesNotContain(Scenarios.Names.ToList(), "ecs-move-simple-alloc", "a new attachment per spawn only differs where objects spawn");
        CollectionAssert.Contains(Scenarios.Names.ToList(), "ecs-churn-simple-alloc");
        CollectionAssert.Contains(Scenarios.Names.ToList(), "world-iterate", "the harness's own scenarios stay");
    }

    // ------------------------------------------------------------------ the prototype's own rules

    private struct A(int value) { public int Value = value; }
    private struct B(int value) { public int Value = value; }

    [TestMethod]
    public void The_prototype_keeps_rows_aligned_when_an_entity_is_destroyed()
    {
        var world = new MiniWorld();
        var ids = Enumerable.Range(0, 100).Select(i => world.Create(new A(i), new B(i * 10))).ToArray();
        var only = world.Create(new A(-1));

        world.Destroy(ids[0]);
        world.Destroy(ids[50]);
        world.Destroy(ids[99]);

        Assert.AreEqual(98, world.Count);
        Assert.IsFalse(world.IsAlive(ids[50]));
        for (int i = 1; i < 99; i++)
        {
            if (i == 50) continue;
            Assert.AreEqual(i, world.Get<A>(ids[i]).Value);
            Assert.AreEqual(i * 10, world.Get<B>(ids[i]).Value, "the row that moved into the hole took both components");
        }
        Assert.AreEqual(-1, world.Get<A>(only).Value);
        int both = 0, withA = 0;
        foreach (var archetype in world.With<A, B>()) both += archetype.Count;
        foreach (var archetype in world.Match(ComponentType<A>.Bit)) withA += archetype.Count;
        Assert.AreEqual(97, both);
        Assert.AreEqual(98, withA, "a query for A finds both archetypes");
    }

    [TestMethod]
    public void The_prototype_never_lets_a_stale_id_reach_the_entity_that_reused_its_slot()
    {
        var world = new MiniWorld();
        var first = world.Create(new A(1));
        world.Destroy(first);
        var second = world.Create(new A(2));

        Assert.AreEqual(first.Index, second.Index, "the slot is reused");
        Assert.AreNotEqual(first, second);
        Assert.IsFalse(world.IsAlive(first));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.Get<A>(first));
        world.Destroy(first);
        Assert.AreEqual(2, world.Get<A>(second).Value, "destroying a stale id does nothing");
        Assert.AreEqual(1, world.Count);
    }
}
