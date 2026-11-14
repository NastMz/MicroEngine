using System.Globalization;

namespace StressBench;

/// <summary>What the command line asks for.</summary>
public sealed record StressOptions
{
    public const string Usage = """
        StressBench: extreme MicroEngine workloads, measured (docs/evaluations/perf-3.md).

          StressBench list
          StressBench run --scenario NAME[,NAME...]|all [options]     measure scenarios once
          StressBench suite [options]                                 every scenario at its default load
          StressBench sweep --scenario NAME [options]                 the largest load under a time budget
          StressBench compare BASELINE.json CURRENT.json [--tolerance PERCENT]

        Options of run, suite and sweep:
          --mode headless|render|e2e[,...]   headless: CPU only (default); render: hidden SDL window, real renderer;
                                             e2e: visible window, real time, VSync
          --load N         the scenario's load (entities, bodies, sprites...); default: the scenario's own
          --frames N       frames measured (600)          --warmup N   frames run first and left out (120)
          --seed N         seed of the workload (1)       --repeat N   runs per scenario; the fastest median is kept (1)
          --backend tiles|box2d   physics backend of the physics scenarios (tiles)
          --out FILE       write the results as JSON      --label TEXT, --commit HASH   recorded in the file
        Options of sweep:
          --budget-ms MS   the budget (16.667: one 60 Hz update)   --metric mean|p50|p95|p99|p99.9|max of the CPU work (p99)
          --start N        first load tried (the scenario's default / 8)   --max-load N   largest load tried (4000000)
        Options of compare:
          --tolerance PERCENT   how much slower the median may be (25; the 99th percentile gets twice as much)
        """;

    public string Command { get; init; } = "";
    public IReadOnlyList<string> Scenarios { get; init; } = [];
    public IReadOnlyList<RunMode> Modes { get; init; } = [RunMode.Headless];
    public int? Load { get; init; }
    public int Frames { get; init; } = 600;
    public int Warmup { get; init; } = 120;
    public int Seed { get; init; } = 1;
    public int Repeat { get; init; } = 1;
    public string Backend { get; init; } = "tiles";
    public string? Out { get; init; }
    public string? Label { get; init; }
    public string? Commit { get; init; }
    public double BudgetMs { get; init; } = 1000.0 / 60;
    public string Metric { get; init; } = "p99";
    public int? Start { get; init; }
    public int MaxLoad { get; init; } = 4_000_000;
    public double TolerancePercent { get; init; } = 25;
    public string? BaselinePath { get; init; }
    public string? CurrentPath { get; init; }

    /// <summary>Reads the command line.</summary>
    /// <exception cref="ArgumentException">Something is missing, unknown or out of range; the message says what.</exception>
    public static StressOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count == 0) throw new ArgumentException("A command is needed: run, suite, sweep, compare or list.");
        var o = new StressOptions { Command = args[0] };
        if (o.Command is not ("run" or "suite" or "sweep" or "compare" or "list" or "help"))
            throw new ArgumentException($"Unknown command '{o.Command}': run, suite, sweep, compare or list.");
        if (o.Command == "suite") o = o with { Scenarios = StressBench.Scenarios.Names };
        var files = new List<string>();
        for (int i = 1; i < args.Count; i++)
        {
            string flag = args[i];
            string Value() => i + 1 < args.Count ? args[++i] : throw new ArgumentException($"{flag} needs a value.");
            int Int(int least)
            {
                string text = Value();
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) throw new ArgumentException($"{flag}: '{text}' is not a whole number.");
                return n >= least ? n : throw new ArgumentException($"{flag} must be at least {least} (it is {n}).");
            }
            double Number()
            {
                string text = Value();
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) || !(n > 0)) throw new ArgumentException($"{flag}: '{text}' is not a positive number.");
                return n;
            }
            switch (flag)
            {
                case "--scenario": o = o with { Scenarios = ScenarioList(Value()) }; break;
                case "--mode": o = o with { Modes = [.. Value().Split(',').Select(ModeOf)] }; break;
                case "--load": o = o with { Load = Int(1) }; break;
                case "--frames": o = o with { Frames = Int(1) }; break;
                case "--warmup": o = o with { Warmup = Int(0) }; break;
                case "--seed": o = o with { Seed = Int(0) }; break;
                case "--repeat": o = o with { Repeat = Int(1) }; break;
                case "--backend":
                    string backend = Value();
                    if (backend is not ("tiles" or "box2d")) throw new ArgumentException($"Unknown backend '{backend}': tiles or box2d.");
                    o = o with { Backend = backend };
                    break;
                case "--out": o = o with { Out = Value() }; break;
                case "--label": o = o with { Label = Value() }; break;
                case "--commit": o = o with { Commit = Value() }; break;
                case "--budget-ms": o = o with { BudgetMs = Number() }; break;
                case "--metric":
                    string metric = Value();
                    if (!Dist.Metrics.Contains(metric)) throw new ArgumentException($"Unknown metric '{metric}': {string.Join(", ", Dist.Metrics)}.");
                    o = o with { Metric = metric };
                    break;
                case "--start": o = o with { Start = Int(1) }; break;
                case "--max-load": o = o with { MaxLoad = Int(1) }; break;
                case "--tolerance": o = o with { TolerancePercent = Number() }; break;
                default:
                    if (flag.StartsWith("--", StringComparison.Ordinal) || o.Command != "compare") throw new ArgumentException($"Unknown option '{flag}'.");
                    files.Add(flag);
                    break;
            }
        }
        if (o.Command == "compare")
        {
            if (files.Count != 2) throw new ArgumentException("compare needs two files: the baseline and the current results.");
            return o with { BaselinePath = files[0], CurrentPath = files[1] };
        }
        if (o.Command is "run" or "sweep" && o.Scenarios.Count == 0) throw new ArgumentException($"{o.Command} needs --scenario NAME ({string.Join(", ", StressBench.Scenarios.Names)}, or all).");
        if (o.Command == "sweep" && o.Scenarios.Count != 1) throw new ArgumentException("sweep searches the limit of one scenario at a time.");
        return o;
    }

    private static IReadOnlyList<string> ScenarioList(string text)
    {
        if (text == "all") return StressBench.Scenarios.Names;
        string[] names = text.Split(',');
        foreach (string name in names)
            if (!StressBench.Scenarios.Names.Contains(name)) throw new ArgumentException($"Unknown scenario '{name}': {string.Join(", ", StressBench.Scenarios.Names)}, or all.");
        return names;
    }

    private static RunMode ModeOf(string text) => text switch
    {
        "headless" => RunMode.Headless,
        "render" => RunMode.Render,
        "e2e" => RunMode.EndToEnd,
        _ => throw new ArgumentException($"Unknown mode '{text}': headless, render or e2e."),
    };

    /// <summary>A mode as the command line and result files name it.</summary>
    public static string NameOf(RunMode mode) => mode switch { RunMode.Headless => "headless", RunMode.Render => "render", _ => "e2e" };
}
