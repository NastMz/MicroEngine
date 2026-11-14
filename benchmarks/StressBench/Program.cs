using System.Globalization;
using StressBench;

// The engine's stress harness (docs/evaluations/perf-3.md). Extreme, reproducible workloads run through the real game
// loop; see StressOptions.Usage for the commands. Run it in Release:
//
//   dotnet run -c Release --project benchmarks/StressBench -- suite --mode headless,render --out results.json
//   dotnet run -c Release --project benchmarks/StressBench -- compare docs/evaluations/perf-3/baseline.json results.json
//   dotnet run -c Release --project benchmarks/StressBench -- sweep --scenario physics
//
// Exit codes: 0 done (compare: nothing regressed), 1 a comparison found a regression or a run failed, 2 wrong options.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
StressOptions options;
try { options = StressOptions.Parse(args); }
catch (ArgumentException e)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(StressOptions.Usage);
    return 2;
}

try
{
    switch (options.Command)
    {
        case "help":
            Console.WriteLine(StressOptions.Usage);
            return 0;
        case "list":
            Console.WriteLine("| Scenario | Default load | The load counts |");
            Console.WriteLine("| --- | ---: | --- |");
            foreach (string name in Scenarios.Names) Console.WriteLine($"| {name} | {Scenarios.DefaultLoad(name):N0} | {Scenarios.Describe(name)} |");
            return 0;
        case "compare":
            var comparison = Baseline.Compare(ResultSet.FromJson(File.ReadAllText(options.BaselinePath!)), ResultSet.FromJson(File.ReadAllText(options.CurrentPath!)), options.TolerancePercent);
            Console.Write(comparison.ToMarkdown());
            Console.WriteLine();
            Console.WriteLine(comparison.Regressed
                ? $"REGRESSED: {comparison.Rows.Count(r => r.Verdict == Verdict.Regressed)} measure(s) beyond the {options.TolerancePercent:0.#}% tolerance."
                : $"No regression beyond the {options.TolerancePercent:0.#}% tolerance.");
            return comparison.Regressed ? 1 : 0;
        case "sweep":
            return Sweep(options);
        default:
            return Measure(options);
    }
}
catch (Exception e) when (e is InvalidOperationException or IOException or FormatException or DllNotFoundException)
{
    Console.Error.WriteLine($"StressBench: {e.Message}");
    return 1;
}

static SortedDictionary<string, string> Describe(StressOptions options)
{
    var environment = Harness.DescribeEnvironment(options);
    if (options.Modes.Any(m => m != RunMode.Headless)) Harness.DescribeRenderer(environment);
    Console.WriteLine($"# StressBench: {environment["cpu"]}, {environment["os"]}, {environment["runtime"]} ({environment["compilation"]}), GC {environment["gc"]}");
    Console.WriteLine();
    return environment;
}

static int Measure(StressOptions options)
{
    var environment = Describe(options);
    Console.WriteLine($"{options.Frames} frames after {options.Warmup} of warm-up, seed {options.Seed}, best of {options.Repeat}. Times in ms; work = events + update + draw submission.");
    Console.WriteLine();
    Console.WriteLine("| Scenario | Mode | Load | Work p50 | p95 | p99 | p99.9 | max | Update p50 | Draw p50 | Present p50 | Alloc B/frame | GC 0/1/2 | Pause total / max | Draws | State changes | Managed MB | Native MB |");
    Console.WriteLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: | ---: | ---: |");
    var runs = new List<RunRecord>();
    foreach (string scenario in options.Scenarios)
        foreach (var mode in options.Modes)
        {
            var r = Harness.RunBest(scenario, options.Load ?? Scenarios.DefaultLoad(scenario), mode, options);
            runs.Add(r);
            Console.WriteLine($"| {r.Scenario} | {r.Mode} | {r.Load:N0} | {r.Work.P50:0.000} | {r.Work.P95:0.000} | {r.Work.P99:0.000} | {r.Work.P999:0.000} | {r.Work.Max:0.000} | {r.Update.P50:0.000} | {r.Render.P50:0.000} | {r.Present.P50:0.000} | {r.AllocatedPerFrame:N0} | {r.Gen0}/{r.Gen1}/{r.Gen2} | {r.PauseTotalMs:0.00} / {r.PauseMaxMs:0.00} | {r.DrawsPerFrame:N0} | {r.StateChangesPerFrame:N0} | {r.ManagedBytes / 1048576.0:0.0} | {r.NativeBytes / 1048576.0:0.0} |");
        }
    Console.WriteLine();
    Console.WriteLine("| Scenario | Mode | Subsystem | Mean | p50 | p99 | max |");
    Console.WriteLine("| --- | --- | --- | ---: | ---: | ---: | ---: |");
    foreach (var r in runs)
        foreach (var (name, d) in r.Sections)
            Console.WriteLine($"| {r.Scenario} | {r.Mode} | {name} | {d.Mean:0.000} | {d.P50:0.000} | {d.P99:0.000} | {d.Max:0.000} |");
    if (options.Modes.Contains(RunMode.EndToEnd))
    {
        Console.WriteLine();
        Console.WriteLine("| Scenario (e2e) | Interval p50 | p95 | p99 | p99.9 | max | Hitches |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var r in runs.Where(r => r.Mode == "e2e"))
            Console.WriteLine($"| {r.Scenario} at {r.Load:N0} | {r.Interval.P50:0.00} | {r.Interval.P95:0.00} | {r.Interval.P99:0.00} | {r.Interval.P999:0.00} | {r.Interval.Max:0.00} | {r.Hitches} |");
    }
    Save(options, new ResultSet(environment, runs));
    return 0;
}

static int Sweep(StressOptions options)
{
    var environment = Describe(options);
    string scenario = options.Scenarios[0];
    var runs = new List<RunRecord>();
    foreach (var mode in options.Modes)
    {
        var measured = new Dictionary<int, RunRecord>();
        int start = options.Start ?? Math.Max(1, Scenarios.DefaultLoad(scenario) / 8);
        var limit = LimitSearch.Find(load =>
        {
            var r = Harness.RunBest(scenario, load, mode, options);
            measured[load] = r;
            return r.Work.At(options.Metric);
        }, start, options.BudgetMs, Math.Max(start, options.MaxLoad), refinements: 3);
        Console.WriteLine($"## {scenario}, {StressOptions.NameOf(mode)}: {options.Metric} of the CPU work against {options.BudgetMs:0.###} ms ({options.Frames} frames after {options.Warmup}, best of {options.Repeat})");
        Console.WriteLine();
        Console.WriteLine("| Load | Work p50 | p99 | p99.9 | max | Alloc B/frame | GC 0/1/2 | Pause max | Within budget |");
        Console.WriteLine("| ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: | --- |");
        foreach (var point in limit.Points)
        {
            var r = measured[point.Load];
            Console.WriteLine($"| {r.Load:N0} | {r.Work.P50:0.000} | {r.Work.P99:0.000} | {r.Work.P999:0.000} | {r.Work.Max:0.000} | {r.AllocatedPerFrame:N0} | {r.Gen0}/{r.Gen1}/{r.Gen2} | {r.PauseMaxMs:0.00} | {(point.Value <= options.BudgetMs ? "yes" : "no")} |");
            runs.Add(r);
        }
        Console.WriteLine();
        Console.WriteLine(limit switch
        {
            { Limit: 0 } => $"Limit: below {limit.FirstOver:N0} (the first load tried is already over the budget).",
            { FirstOver: 0 } => $"Limit: above {limit.Limit:N0} (the largest load tried is within the budget).",
            _ => $"Limit: between {limit.Limit:N0} (within the budget) and {limit.FirstOver:N0} (over it).",
        });
        Console.WriteLine();
    }
    Save(options, new ResultSet(environment, runs));
    return 0;
}

static void Save(StressOptions options, ResultSet results)
{
    if (options.Out is null) return;
    if (Path.GetDirectoryName(Path.GetFullPath(options.Out)) is { } folder) Directory.CreateDirectory(folder);
    File.WriteAllText(options.Out, results.ToJson());
    Console.WriteLine();
    Console.WriteLine($"Results written to {options.Out}.");
}
