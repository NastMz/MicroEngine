using StressBench;

namespace Benchmarks.Tests;

/// <summary>PERF-3: the stress harness's statistics, options, result files, baseline comparison and limit search.</summary>
[TestClass]
public class HarnessLogicTests
{
    // ------------------------------------------------------------------ statistics

    [TestMethod]
    public void Percentiles_are_nearest_rank_over_the_sorted_values()
    {
        var values = new double[1000];
        for (int i = 0; i < values.Length; i++) values[i] = 1000 - i;   // 1..1000, unsorted
        var d = Dist.Of(values);
        Assert.AreEqual(500.5, d.Mean, 1e-9);
        Assert.AreEqual(500, d.P50);
        Assert.AreEqual(950, d.P95);
        Assert.AreEqual(990, d.P99);
        Assert.AreEqual(999, d.P999);
        Assert.AreEqual(1000, d.Max);
        Assert.AreEqual(1000, values[0], "the caller's values are not reordered");
    }

    [TestMethod]
    public void Percentiles_of_few_or_no_values()
    {
        Assert.AreEqual(default, Dist.Of([]));
        var one = Dist.Of([7.5]);
        Assert.AreEqual(new Dist(7.5, 7.5, 7.5, 7.5, 7.5, 7.5), one);
        var two = Dist.Of([1, 3]);
        Assert.AreEqual(1, two.P50);
        Assert.AreEqual(3, two.P99);
        Assert.AreEqual(3, two.P999, "a tail percentile of a short series is its maximum, never past the end");
    }

    // ------------------------------------------------------------------ options

    [TestMethod]
    public void Run_options_have_defaults_and_read_every_flag()
    {
        var d = StressOptions.Parse(["run", "--scenario", "sprites"]);
        Assert.AreEqual("run", d.Command);
        CollectionAssert.AreEqual(new[] { "sprites" }, d.Scenarios.ToArray());
        CollectionAssert.AreEqual(new[] { RunMode.Headless }, d.Modes.ToArray());
        Assert.IsNull(d.Load);
        Assert.AreEqual(600, d.Frames);
        Assert.AreEqual(120, d.Warmup);
        Assert.AreEqual(1, d.Seed);
        Assert.AreEqual(1, d.Repeat);
        Assert.AreEqual("tiles", d.Backend);

        var o = StressOptions.Parse(["run", "--scenario", "sprites,particles", "--mode", "render,e2e", "--load", "5000", "--frames", "300",
            "--warmup", "30", "--seed", "9", "--repeat", "3", "--out", "r.json", "--label", "after fix", "--commit", "abc1234", "--backend", "box2d"]);
        CollectionAssert.AreEqual(new[] { "sprites", "particles" }, o.Scenarios.ToArray());
        CollectionAssert.AreEqual(new[] { RunMode.Render, RunMode.EndToEnd }, o.Modes.ToArray());
        Assert.AreEqual(5000, o.Load);
        Assert.AreEqual(300, o.Frames);
        Assert.AreEqual(30, o.Warmup);
        Assert.AreEqual(9, o.Seed);
        Assert.AreEqual(3, o.Repeat);
        Assert.AreEqual("r.json", o.Out);
        Assert.AreEqual("after fix", o.Label);
        Assert.AreEqual("abc1234", o.Commit);
        Assert.AreEqual("box2d", o.Backend);
    }

    [TestMethod]
    public void Suite_runs_every_scenario_and_all_names_them()
    {
        var suite = StressOptions.Parse(["suite", "--mode", "headless,render"]);
        CollectionAssert.AreEqual(Scenarios.Names.ToArray(), suite.Scenarios.ToArray());
        CollectionAssert.AreEqual(new[] { RunMode.Headless, RunMode.Render }, suite.Modes.ToArray());
        CollectionAssert.AreEqual(Scenarios.Names.ToArray(), StressOptions.Parse(["run", "--scenario", "all"]).Scenarios.ToArray());
    }

    [TestMethod]
    public void A_suite_isolates_its_runs_and_a_run_does_not_unless_asked()
    {
        Assert.IsTrue(StressOptions.Parse(["suite"]).Isolate);
        Assert.IsFalse(StressOptions.Parse(["suite", "--no-isolate"]).Isolate);
        Assert.IsFalse(StressOptions.Parse(["run", "--scenario", "sprites"]).Isolate);
        Assert.IsTrue(StressOptions.Parse(["sweep", "--scenario", "sprites", "--isolate"]).Isolate);

        // What an isolated run passes on parses back to the same single run, not isolated again.
        var asked = StressOptions.Parse(["suite", "--frames", "90", "--warmup", "7", "--seed", "5", "--repeat", "2", "--backend", "box2d"]);
        var child = StressOptions.Parse([.. Harness.ChildArguments("physics", 321, RunMode.Render, asked, "one.json")]);
        Assert.AreEqual("run", child.Command);
        CollectionAssert.AreEqual(new[] { "physics" }, child.Scenarios.ToArray());
        CollectionAssert.AreEqual(new[] { RunMode.Render }, child.Modes.ToArray());
        Assert.AreEqual((321, 90, 7, 5, 2, "box2d", "one.json", false), (child.Load, child.Frames, child.Warmup, child.Seed, child.Repeat, child.Backend, child.Out, child.Isolate));
    }

    [TestMethod]
    public void Sweep_and_compare_options()
    {
        var sweep = StressOptions.Parse(["sweep", "--scenario", "physics", "--budget-ms", "8.33", "--metric", "p50", "--start", "500", "--max-load", "64000"]);
        Assert.AreEqual("sweep", sweep.Command);
        Assert.AreEqual(8.33, sweep.BudgetMs, 1e-9);
        Assert.AreEqual("p50", sweep.Metric);
        Assert.AreEqual(500, sweep.Start);
        Assert.AreEqual(64000, sweep.MaxLoad);
        Assert.AreEqual(1000.0 / 60, StressOptions.Parse(["sweep", "--scenario", "physics"]).BudgetMs, 1e-9, "the default budget is one 60 Hz update");
        Assert.AreEqual("p99", StressOptions.Parse(["sweep", "--scenario", "physics"]).Metric);

        var compare = StressOptions.Parse(["compare", "base.json", "new.json", "--tolerance", "40"]);
        Assert.AreEqual("compare", compare.Command);
        Assert.AreEqual("base.json", compare.BaselinePath);
        Assert.AreEqual("new.json", compare.CurrentPath);
        Assert.AreEqual(40, compare.TolerancePercent);
        Assert.AreEqual(25, StressOptions.Parse(["compare", "a", "b"]).TolerancePercent);
    }

    [TestMethod]
    public void Wrong_options_say_what_is_wrong()
    {
        static string Error(params string[] args) => Assert.ThrowsExactly<ArgumentException>(() => StressOptions.Parse(args)).Message;
        StringAssert.Contains(Error(), "run, suite, sweep, compare or list");
        StringAssert.Contains(Error("fly"), "'fly'");
        StringAssert.Contains(Error("run"), "--scenario");
        StringAssert.Contains(Error("run", "--scenario", "nothing"), "'nothing'");
        StringAssert.Contains(Error("run", "--scenario", "sprites", "--mode", "fast"), "'fast'");
        StringAssert.Contains(Error("run", "--scenario", "sprites", "--frames"), "--frames needs a value");
        StringAssert.Contains(Error("run", "--scenario", "sprites", "--frames", "many"), "'many'");
        StringAssert.Contains(Error("run", "--scenario", "sprites", "--frames", "0"), "--frames");
        StringAssert.Contains(Error("run", "--scenario", "sprites", "--load", "-4"), "--load");
        StringAssert.Contains(Error("run", "--scenario", "sprites", "--colour", "red"), "'--colour'");
        StringAssert.Contains(Error("run", "--scenario", "sprites", "--backend", "bullet"), "'bullet'");
        StringAssert.Contains(Error("sweep", "--scenario", "sprites,particles"), "one scenario");
        StringAssert.Contains(Error("sweep", "--scenario", "sprites", "--metric", "p42"), "'p42'");
        StringAssert.Contains(Error("compare", "only-one.json"), "two files");
    }

    // ------------------------------------------------------------------ result files

    private static RunRecord Record(string scenario = "sprites", double p50 = 2, double p99 = 3, double allocated = 100, long checksum = 42, string mode = "headless", int load = 1000) => new()
    {
        Scenario = scenario, Mode = mode, Load = load, Backend = "tiles", Frames = 600, Checksum = checksum,
        Interval = new Dist(1, 2, 3, 4, 5, 6), Work = new Dist(p50, p50, p99, p99, p99, p99 * 2), Update = new Dist(0.5, 0.5, 0.6, 0.7, 0.8, 0.9),
        Render = new Dist(1.5, 1.5, 1.6, 1.7, 1.8, 1.9), Present = new Dist(0.1, 0.1, 0.1, 0.1, 0.1, 0.2),
        AllocatedPerFrame = allocated, AllocatedMaxFrame = allocated * 4, Gen0 = 3, Gen1 = 2, Gen2 = 1, PauseTotalMs = 1.25, PauseMaxMs = 0.75, Hitches = 4,
        DrawsPerFrame = 1000, StateChangesPerFrame = 12, ManagedBytes = 1 << 20, HeapCommittedBytes = 2 << 20, WorkingSetBytes = 90 << 20,
        PrivateBytes = 70 << 20, PeakWorkingSetBytes = 95 << 20,
        Resources = new SortedDictionary<string, double>(StringComparer.Ordinal) { ["entities"] = 1000, ["textures"] = 1 },
        Sections = new SortedDictionary<string, Dist>(StringComparer.Ordinal) { ["world"] = new Dist(0.25, 0.2, 0.3, 0.4, 0.5, 0.6) },
    };

    private static ResultSet Set(params RunRecord[] runs) => new(
        new SortedDictionary<string, string>(StringComparer.Ordinal) { ["cpu"] = "Test CPU \"quoted\"", ["runtime"] = ".NET 10", ["compilation"] = "jit", ["gc"] = "workstation, concurrent" },
        [.. runs]);

    [TestMethod]
    public void A_result_file_round_trips()
    {
        var set = Set(Record(), Record("particles", 4.125, 9.5, 0));
        var read = ResultSet.FromJson(set.ToJson());
        Assert.AreEqual(set.ToJson(), read.ToJson());
        Assert.AreEqual("Test CPU \"quoted\"", read.Environment["cpu"]);
        Assert.AreEqual(2, read.Runs.Count);
        var r = read.Runs[1];
        Assert.AreEqual("particles/headless/1000", r.Key);
        Assert.AreEqual(4.125, r.Work.P50);
        Assert.AreEqual(42, r.Checksum);
        Assert.AreEqual(1000, r.Resources["entities"]);
        Assert.AreEqual(0.4, r.Sections["world"].P99);
        Assert.AreEqual(1.25, r.PauseTotalMs);
        Assert.AreEqual((70 << 20) - (2 << 20), r.NativeBytes, "native memory is what the process holds beyond the managed heap");
    }

    [TestMethod]
    public void A_file_that_is_not_a_result_says_so()
    {
        StringAssert.Contains(Assert.ThrowsExactly<FormatException>(() => ResultSet.FromJson("{ \"frames\": [] }")).Message, "StressBench result");
        Assert.ThrowsExactly<FormatException>(() => ResultSet.FromJson("not json"));
    }

    // ------------------------------------------------------------------ comparison

    [TestMethod]
    public void The_same_results_do_not_regress()
    {
        var c = Baseline.Compare(Set(Record()), Set(Record()), 25);
        Assert.IsFalse(c.Regressed);
        Assert.IsTrue(c.Rows.All(r => r.Verdict == Verdict.Same));
        Assert.AreEqual(0, c.Warnings.Count);
    }

    [TestMethod]
    public void Slower_than_the_tolerance_regresses_and_faster_improves()
    {
        var slower = Baseline.Compare(Set(Record(p50: 2)), Set(Record(p50: 2.6)), 25);
        Assert.IsTrue(slower.Regressed);
        var row = slower.Rows.Single(r => r.Verdict == Verdict.Regressed);
        Assert.AreEqual("sprites/headless/1000", row.Key);
        Assert.AreEqual("work p50 ms", row.Metric);
        Assert.AreEqual(30, row.ChangePercent, 1e-6);

        Assert.IsFalse(Baseline.Compare(Set(Record(p50: 2)), Set(Record(p50: 2.4)), 25).Regressed, "20% is inside a 25% tolerance");
        var faster = Baseline.Compare(Set(Record(p50: 2)), Set(Record(p50: 1)), 25);
        Assert.IsFalse(faster.Regressed);
        Assert.AreEqual(Verdict.Improved, faster.Rows.Single(r => r.Metric == "work p50 ms").Verdict);
    }

    [TestMethod]
    public void The_tail_gets_twice_the_tolerance_and_small_absolute_changes_are_noise()
    {
        Assert.IsFalse(Baseline.Compare(Set(Record(p99: 3)), Set(Record(p99: 4.2)), 25).Regressed, "p99 +40% is inside twice 25%");
        Assert.IsTrue(Baseline.Compare(Set(Record(p99: 3)), Set(Record(p99: 4.8)), 25).Regressed);
        Assert.IsFalse(Baseline.Compare(Set(Record(p50: 0.01, p99: 0.02)), Set(Record(p50: 0.03, p99: 0.06)), 25).Regressed, "hundredths of a millisecond are below the noise floor");
    }

    [TestMethod]
    public void More_allocation_per_frame_regresses()
    {
        var c = Baseline.Compare(Set(Record(allocated: 0)), Set(Record(allocated: 4096)), 25);
        Assert.IsTrue(c.Regressed);
        Assert.AreEqual("allocated B/frame", c.Rows.Single(r => r.Verdict == Verdict.Regressed).Metric);
        Assert.IsFalse(Baseline.Compare(Set(Record(allocated: 0)), Set(Record(allocated: 200)), 25).Regressed, "a few hundred bytes are below the noise floor");
    }

    [TestMethod]
    public void Runs_are_matched_by_scenario_mode_and_load()
    {
        var c = Baseline.Compare(Set(Record(), Record("particles")), Set(Record(), Record("text")), 25);
        Assert.AreEqual(Verdict.Missing, c.Rows.Single(r => r.Key == "particles/headless/1000").Verdict);
        Assert.AreEqual(Verdict.New, c.Rows.Single(r => r.Key == "text/headless/1000").Verdict);
        Assert.IsFalse(c.Regressed, "a run that is not in both files is reported, not judged");
        StringAssert.Contains(c.ToMarkdown(), "particles/headless/1000");
    }

    [TestMethod]
    public void A_different_workload_or_machine_is_not_compared_silently()
    {
        var changed = Baseline.Compare(Set(Record(checksum: 1, p50: 2)), Set(Record(checksum: 2, p50: 9)), 25);
        Assert.AreEqual(Verdict.Changed, changed.Rows.Single().Verdict);
        Assert.IsFalse(changed.Regressed);
        StringAssert.Contains(changed.Warnings.Single(), "checksum");

        var other = Set(Record());
        other.Environment["cpu"] = "Another CPU";
        other.Environment["compilation"] = "aot";
        var c = Baseline.Compare(Set(Record()), other, 25);
        Assert.AreEqual(2, c.Warnings.Count);
        StringAssert.Contains(c.Warnings[0], "compilation");
        StringAssert.Contains(c.Warnings[1], "cpu");
    }

    // ------------------------------------------------------------------ limit search

    [TestMethod]
    public void The_limit_is_the_largest_load_under_the_budget()
    {
        var asked = new List<int>();
        // 1 ms per 1000 units: the 16 ms budget holds up to 16,000.
        var r = LimitSearch.Find(load => { asked.Add(load); return load / 1000.0; }, start: 1000, budget: 16, max: 1_000_000, refinements: 3);
        CollectionAssert.AreEqual(new[] { 1000, 2000, 4000, 8000, 16000, 32000, 24000, 20000, 18000 }, asked);
        Assert.AreEqual(16000, r.Limit);
        Assert.AreEqual(18000, r.FirstOver);
        Assert.AreEqual(9, r.Points.Count);
        Assert.IsTrue(r.Points.SequenceEqual(r.Points.OrderBy(p => p.Load)), "points are listed by load");
    }

    [TestMethod]
    public void A_limit_outside_the_searched_range_is_reported_as_such()
    {
        var over = LimitSearch.Find(_ => 99, start: 100, budget: 16, max: 10_000, refinements: 3);
        Assert.AreEqual(0, over.Limit, "the first load is already over the budget");
        Assert.AreEqual(100, over.FirstOver);

        var never = LimitSearch.Find(_ => 1, start: 100, budget: 16, max: 1000, refinements: 3);
        Assert.AreEqual(1000, never.Limit, "the search stops at the largest load allowed");
        Assert.AreEqual(0, never.FirstOver);
        CollectionAssert.AreEqual(new[] { 100, 200, 400, 800, 1000 }, never.Points.Select(p => p.Load).ToArray());
    }
}
