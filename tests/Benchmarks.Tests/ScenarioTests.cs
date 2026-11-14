using StressBench;

namespace Benchmarks.Tests;

/// <summary>PERF-3: every stress scenario runs through the real game loop headless and repeats exactly.</summary>
[TestClass]
public class ScenarioTests
{
    private static readonly StressOptions Short = new() { Command = "run", Frames = 12, Warmup = 4 };

    // A small load of each scenario: enough to exercise every path (churn batches, queries, particles alive).
    private static int SmallLoad(string scenario) => scenario == "asset-churn" ? 2 : 200;

    public static IEnumerable<object[]> Names => Scenarios.Names.Select(n => new object[] { n });

    [TestMethod]
    [DynamicData(nameof(Names))]
    public void A_scenario_repeats_exactly_and_reports_what_it_ran(string scenario)
    {
        var first = Harness.Run(scenario, SmallLoad(scenario), RunMode.Headless, Short);
        var second = Harness.Run(scenario, SmallLoad(scenario), RunMode.Headless, Short);

        Assert.AreEqual(first.Checksum, second.Checksum, "the same scenario, load, seed and frames end in the same state");
        Assert.AreEqual(first.DrawsPerFrame, second.DrawsPerFrame);
        Assert.AreEqual(scenario, first.Scenario);
        Assert.AreEqual("headless", first.Mode);
        Assert.AreEqual(SmallLoad(scenario), first.Load);
        Assert.AreEqual(12, first.Frames, "the warm-up frames are left out");
        Assert.IsTrue(first.Work.P50 > 0 && first.Work.Max >= first.Work.P99 && first.Work.P99 >= first.Work.P50);
        Assert.IsTrue(first.Resources.Count > 0, "a scenario counts its resources");
        Assert.IsTrue(first.ManagedBytes > 0 && first.WorkingSetBytes > 0);
        foreach (var (name, time) in first.Sections) Assert.IsTrue(time.Max >= 0, name);
    }

    [TestMethod]
    public void Draw_calls_and_state_changes_are_what_the_scenario_submits()
    {
        Assert.AreEqual(200, Harness.Run("sprites", 200, RunMode.Headless, Short).DrawsPerFrame);
        var broken = Harness.Run("batch-breaks", 200, RunMode.Headless, Short);
        Assert.AreEqual(200, broken.DrawsPerFrame);
        Assert.AreEqual(300, broken.StateChangesPerFrame, "a texture switch per draw and a blend switch every other draw");
        Assert.AreEqual(400, Harness.Run("shapes", 200, RunMode.Headless, Short).DrawsPerFrame, "a circle and a line or a triangle each");
        Assert.AreEqual(200, Harness.Run("world-iterate", 200, RunMode.Headless, Short).DrawsPerFrame);
        Assert.AreEqual(0, Harness.Run("entity-churn", 200, RunMode.Headless, Short).DrawsPerFrame);
    }

    [TestMethod]
    public void The_seed_and_the_load_change_the_workload()
    {
        long one = Harness.Run("sprites", 200, RunMode.Headless, Short).Checksum;
        Assert.AreNotEqual(one, Harness.Run("sprites", 200, RunMode.Headless, Short with { Seed = 2 }).Checksum);
        Assert.AreNotEqual(one, Harness.Run("sprites", 201, RunMode.Headless, Short).Checksum);
    }

    [TestMethod]
    public void Churn_keeps_the_world_at_its_load()
    {
        var churn = Harness.Run("entity-churn", 200, RunMode.Headless, Short);
        Assert.AreEqual(200, churn.Resources["entities"]);
        Assert.AreEqual(200 + 16 * 20, churn.Resources["entitiesCreated"], "a tenth replaced in each of the 16 steps");
        var assets = Harness.Run("asset-churn", 2, RunMode.Headless, Short);
        Assert.AreEqual(2, assets.Resources["texturesLive"]);
        Assert.AreEqual(32, assets.Resources["texturesLoaded"]);
    }

    [TestMethod]
    public void A_steady_scenario_allocates_nothing_per_frame()
    {
        var long_ = Short with { Frames = 60, Warmup = 30 };
        foreach (string scenario in new[] { "entity-churn", "world-iterate", "spatial-query", "sprites", "batch-breaks", "shapes", "text", "particles" })
            Assert.IsTrue(Harness.Run(scenario, 200, RunMode.Headless, long_).AllocatedPerFrame < 64, $"{scenario}: what a frame allocates is the engine's, and it should be nothing");
    }

    [TestMethod]
    public void Every_scenario_has_a_default_load_and_a_description()
    {
        foreach (string name in Scenarios.Names)
        {
            Assert.IsTrue(Scenarios.DefaultLoad(name) > 0, name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(Scenarios.Describe(name)), name);
        }
        Assert.ThrowsExactly<ArgumentException>(() => Scenarios.DefaultLoad("nothing"));
    }
}
