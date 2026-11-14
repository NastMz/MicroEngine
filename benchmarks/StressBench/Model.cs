using System.Globalization;
using System.Text;
using System.Text.Json;

namespace StressBench;

/// <summary>How a series of per-frame values is distributed (nearest-rank percentiles, as the engine's frame profiler computes them).</summary>
public readonly record struct Dist(double Mean, double P50, double P95, double P99, double P999, double Max)
{
    /// <summary>The distribution of <paramref name="values"/>, which are not reordered; all zero for no values.</summary>
    public static Dist Of(ReadOnlySpan<double> values)
    {
        if (values.Length == 0) return default;
        var sorted = values.ToArray();
        Array.Sort(sorted);
        double sum = 0;
        foreach (double v in sorted) sum += v;
        double At(double q) => sorted[Math.Clamp((int)Math.Ceiling(q * sorted.Length) - 1, 0, sorted.Length - 1)];
        return new Dist(sum / sorted.Length, At(0.50), At(0.95), At(0.99), At(0.999), sorted[^1]);
    }

    /// <summary>The value a limit search reads: "mean", "p50", "p95", "p99", "p99.9" or "max".</summary>
    public double At(string metric) => metric switch
    {
        "mean" => Mean, "p50" => P50, "p95" => P95, "p99" => P99, "p99.9" => P999, "max" => Max,
        _ => throw new ArgumentException($"Unknown metric '{metric}': mean, p50, p95, p99, p99.9 or max.", nameof(metric)),
    };

    /// <summary>The metrics <see cref="At"/> reads.</summary>
    public static IReadOnlyList<string> Metrics { get; } = ["mean", "p50", "p95", "p99", "p99.9", "max"];
}

/// <summary>What a run measures: the CPU alone, the CPU and the real renderer, or everything a player sees.</summary>
public enum RunMode
{
    /// <summary>The headless platform: update and draw submission, no graphics device. Unpaced, one fixed step per frame.</summary>
    Headless,
    /// <summary>A hidden SDL window: the same plus the real renderer's submission and present. Unpaced, one fixed step per frame.</summary>
    Render,
    /// <summary>A visible SDL window in real time with VSync: frame intervals as a display shows them.</summary>
    EndToEnd,
}

/// <summary>One scenario at one load in one mode, as measured.</summary>
public sealed record RunRecord
{
    public required string Scenario { get; init; }
    public required string Mode { get; init; }
    public required int Load { get; init; }
    public string Backend { get; init; } = "";
    /// <summary>Frames measured (after the warm-up).</summary>
    public int Frames { get; init; }
    /// <summary>A hash of the simulation's final state: the same for the same scenario, load, seed and frame count.</summary>
    public long Checksum { get; init; }
    /// <summary>Time between frame starts, ms.</summary>
    public Dist Interval { get; init; }
    /// <summary>Events, update and draw submission: the CPU time of a frame before presenting, ms.</summary>
    public Dist Work { get; init; }
    public Dist Update { get; init; }
    public Dist Render { get; init; }
    public Dist Present { get; init; }
    /// <summary>Mean bytes allocated per frame by the loop's thread.</summary>
    public double AllocatedPerFrame { get; init; }
    public double AllocatedMaxFrame { get; init; }
    public int Gen0 { get; init; }
    public int Gen1 { get; init; }
    public int Gen2 { get; init; }
    /// <summary>Time the runtime paused the process for garbage collections during the measured frames, ms.</summary>
    public double PauseTotalMs { get; init; }
    /// <summary>The longest pause time seen inside one frame, ms.</summary>
    public double PauseMaxMs { get; init; }
    /// <summary>Frames whose interval exceeded 1.5 times the median interval.</summary>
    public int Hitches { get; init; }
    public double DrawsPerFrame { get; init; }
    /// <summary>Texture and blend-mode switches the scenario asks for per frame.</summary>
    public double StateChangesPerFrame { get; init; }
    public long ManagedBytes { get; init; }
    public long HeapCommittedBytes { get; init; }
    public long WorkingSetBytes { get; init; }
    public long PrivateBytes { get; init; }
    public long PeakWorkingSetBytes { get; init; }
    /// <summary>Resource counts at the end of the run: entities, bodies, particles, textures...</summary>
    public SortedDictionary<string, double> Resources { get; init; } = new(StringComparer.Ordinal);
    /// <summary>CPU time per subsystem inside the update, ms per frame.</summary>
    public SortedDictionary<string, Dist> Sections { get; init; } = new(StringComparer.Ordinal);

    /// <summary>What the process holds beyond the managed heap: native libraries, the graphics driver, textures.</summary>
    public long NativeBytes => Math.Max(0, PrivateBytes - HeapCommittedBytes);
    /// <summary>What identifies the run in a comparison.</summary>
    public string Key => $"{Scenario}/{Mode}/{Load}";
}

/// <summary>A result file: where and how the runs were measured, and the runs.</summary>
public sealed record ResultSet(SortedDictionary<string, string> Environment, List<RunRecord> Runs)
{
    private const string Format = "microengine-stressbench/1";

    /// <summary>The file as JSON: written by hand (no reflection), so the harness also runs as a NativeAOT program.</summary>
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            w.WriteStartObject();
            w.WriteString("format", Format);
            w.WriteStartObject("environment");
            foreach (var (key, value) in Environment) w.WriteString(key, value);
            w.WriteEndObject();
            w.WriteStartArray("runs");
            foreach (var r in Runs)
            {
                w.WriteStartObject();
                w.WriteString("scenario", r.Scenario);
                w.WriteString("mode", r.Mode);
                w.WriteNumber("load", r.Load);
                w.WriteString("backend", r.Backend);
                w.WriteNumber("frames", r.Frames);
                w.WriteNumber("checksum", r.Checksum);
                Write(w, "intervalMs", r.Interval);
                Write(w, "workMs", r.Work);
                Write(w, "updateMs", r.Update);
                Write(w, "renderMs", r.Render);
                Write(w, "presentMs", r.Present);
                w.WriteNumber("allocatedBytesPerFrame", Round(r.AllocatedPerFrame));
                w.WriteNumber("allocatedBytesMaxFrame", Round(r.AllocatedMaxFrame));
                w.WriteNumber("gen0", r.Gen0);
                w.WriteNumber("gen1", r.Gen1);
                w.WriteNumber("gen2", r.Gen2);
                w.WriteNumber("pauseTotalMs", Round(r.PauseTotalMs));
                w.WriteNumber("pauseMaxMs", Round(r.PauseMaxMs));
                w.WriteNumber("hitches", r.Hitches);
                w.WriteNumber("drawsPerFrame", Round(r.DrawsPerFrame));
                w.WriteNumber("stateChangesPerFrame", Round(r.StateChangesPerFrame));
                w.WriteNumber("managedBytes", r.ManagedBytes);
                w.WriteNumber("heapCommittedBytes", r.HeapCommittedBytes);
                w.WriteNumber("workingSetBytes", r.WorkingSetBytes);
                w.WriteNumber("privateBytes", r.PrivateBytes);
                w.WriteNumber("peakWorkingSetBytes", r.PeakWorkingSetBytes);
                w.WriteStartObject("resources");
                foreach (var (key, value) in r.Resources) w.WriteNumber(key, Round(value));
                w.WriteEndObject();
                w.WriteStartObject("sectionsMs");
                foreach (var (key, value) in r.Sections) Write(w, key, value);
                w.WriteEndObject();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()).ReplaceLineEndings("\n") + "\n";
    }

    // Four decimals: a tenth of a microsecond, and files whose last digits are not noise.
    private static double Round(double value) => Math.Round(value, 4);

    private static void Write(Utf8JsonWriter w, string name, Dist d)
    {
        w.WriteStartObject(name);
        w.WriteNumber("mean", Round(d.Mean));
        w.WriteNumber("p50", Round(d.P50));
        w.WriteNumber("p95", Round(d.P95));
        w.WriteNumber("p99", Round(d.P99));
        w.WriteNumber("p99_9", Round(d.P999));
        w.WriteNumber("max", Round(d.Max));
        w.WriteEndObject();
    }

    /// <summary>Reads a file <see cref="ToJson"/> wrote.</summary>
    /// <exception cref="FormatException">The text is not a StressBench result.</exception>
    public static ResultSet FromJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("format", out var format) || format.GetString() != Format)
                throw new FormatException($"Not a StressBench result (it has no \"format\": \"{Format}\").");
            var environment = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var e in root.GetProperty("environment").EnumerateObject()) environment[e.Name] = e.Value.GetString() ?? "";
            var runs = new List<RunRecord>();
            foreach (var r in root.GetProperty("runs").EnumerateArray())
            {
                var resources = new SortedDictionary<string, double>(StringComparer.Ordinal);
                foreach (var e in r.GetProperty("resources").EnumerateObject()) resources[e.Name] = e.Value.GetDouble();
                var sections = new SortedDictionary<string, Dist>(StringComparer.Ordinal);
                foreach (var e in r.GetProperty("sectionsMs").EnumerateObject()) sections[e.Name] = ReadDist(e.Value);
                runs.Add(new RunRecord
                {
                    Scenario = r.GetProperty("scenario").GetString()!,
                    Mode = r.GetProperty("mode").GetString()!,
                    Load = r.GetProperty("load").GetInt32(),
                    Backend = r.GetProperty("backend").GetString() ?? "",
                    Frames = r.GetProperty("frames").GetInt32(),
                    Checksum = r.GetProperty("checksum").GetInt64(),
                    Interval = ReadDist(r.GetProperty("intervalMs")),
                    Work = ReadDist(r.GetProperty("workMs")),
                    Update = ReadDist(r.GetProperty("updateMs")),
                    Render = ReadDist(r.GetProperty("renderMs")),
                    Present = ReadDist(r.GetProperty("presentMs")),
                    AllocatedPerFrame = r.GetProperty("allocatedBytesPerFrame").GetDouble(),
                    AllocatedMaxFrame = r.GetProperty("allocatedBytesMaxFrame").GetDouble(),
                    Gen0 = r.GetProperty("gen0").GetInt32(),
                    Gen1 = r.GetProperty("gen1").GetInt32(),
                    Gen2 = r.GetProperty("gen2").GetInt32(),
                    PauseTotalMs = r.GetProperty("pauseTotalMs").GetDouble(),
                    PauseMaxMs = r.GetProperty("pauseMaxMs").GetDouble(),
                    Hitches = r.GetProperty("hitches").GetInt32(),
                    DrawsPerFrame = r.GetProperty("drawsPerFrame").GetDouble(),
                    StateChangesPerFrame = r.GetProperty("stateChangesPerFrame").GetDouble(),
                    ManagedBytes = r.GetProperty("managedBytes").GetInt64(),
                    HeapCommittedBytes = r.GetProperty("heapCommittedBytes").GetInt64(),
                    WorkingSetBytes = r.GetProperty("workingSetBytes").GetInt64(),
                    PrivateBytes = r.GetProperty("privateBytes").GetInt64(),
                    PeakWorkingSetBytes = r.GetProperty("peakWorkingSetBytes").GetInt64(),
                    Resources = resources,
                    Sections = sections,
                });
            }
            return new ResultSet(environment, runs);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new FormatException($"Not a StressBench result: {e.Message}", e);
        }
    }

    private static Dist ReadDist(JsonElement e) => new(e.GetProperty("mean").GetDouble(), e.GetProperty("p50").GetDouble(), e.GetProperty("p95").GetDouble(),
        e.GetProperty("p99").GetDouble(), e.GetProperty("p99_9").GetDouble(), e.GetProperty("max").GetDouble());
}

/// <summary>What a comparison says about one measure of one run.</summary>
public enum Verdict { Same, Regressed, Improved, Missing, New, Changed }

public sealed record ComparisonRow(string Key, string Metric, double Baseline, double Current, double ChangePercent, Verdict Verdict);

public sealed record Comparison(IReadOnlyList<ComparisonRow> Rows, IReadOnlyList<string> Warnings)
{
    public bool Regressed => Rows.Any(r => r.Verdict == Verdict.Regressed);

    /// <summary>The comparison as a Markdown table, warnings first.</summary>
    public string ToMarkdown()
    {
        var text = new StringBuilder();
        foreach (string warning in Warnings) text.Append("> Warning: ").Append(warning).Append('\n');
        if (Warnings.Count > 0) text.Append('\n');
        text.Append("| Run | Measure | Baseline | Current | Change | Verdict |\n| --- | --- | ---: | ---: | ---: | --- |\n");
        foreach (var r in Rows)
        {
            string change = double.IsFinite(r.ChangePercent) ? r.ChangePercent.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%" : "new";
            text.Append(CultureInfo.InvariantCulture, $"| {r.Key} | {r.Metric} | {r.Baseline:0.###} | {r.Current:0.###} | {change} | {r.Verdict.ToString().ToLowerInvariant()} |\n");
        }
        return text.ToString();
    }
}

/// <summary>Compares a later run with a stored one.</summary>
public static class Baseline
{
    /// <summary>Environment facts that make two files measurements of different things when they differ.</summary>
    private static readonly string[] Comparable = ["compilation", "cpu", "gc", "runtime"];

    // Below these absolute changes a difference is timer and scheduler noise, whatever its percentage.
    private const double TimeFloorMs = 0.05, AllocationFloorBytes = 256;

    /// <summary>
    /// Compares the runs both files have (same scenario, mode and load). A run regresses when its median CPU work is
    /// more than <paramref name="tolerancePercent"/> slower, its 99th percentile more than twice that, or its
    /// allocation per frame more than <paramref name="tolerancePercent"/> larger, each beyond a noise floor. Runs whose
    /// workload changed (different checksum or frame count) or that are in one file only are reported, not judged.
    /// </summary>
    public static Comparison Compare(ResultSet baseline, ResultSet current, double tolerancePercent)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        var rows = new List<ComparisonRow>();
        var warnings = new List<string>();
        foreach (string key in Comparable)
        {
            string was = baseline.Environment.GetValueOrDefault(key, ""), now = current.Environment.GetValueOrDefault(key, "");
            if (was != now) warnings.Add($"{key} differs (baseline: '{was}', current: '{now}'): the files measure different conditions.");
        }
        var later = current.Runs.ToDictionary(r => r.Key, StringComparer.Ordinal);
        foreach (var was in baseline.Runs)
        {
            if (!later.Remove(was.Key, out var now))
            {
                rows.Add(new ComparisonRow(was.Key, "run", 0, 0, 0, Verdict.Missing));
                continue;
            }
            if (was.Checksum != now.Checksum || was.Frames != now.Frames)
            {
                warnings.Add($"{was.Key}: the workload changed (checksum {was.Checksum} -> {now.Checksum}, frames {was.Frames} -> {now.Frames}); not compared.");
                rows.Add(new ComparisonRow(was.Key, "run", 0, 0, 0, Verdict.Changed));
                continue;
            }
            rows.Add(Row(was.Key, "work p50 ms", was.Work.P50, now.Work.P50, tolerancePercent, TimeFloorMs));
            rows.Add(Row(was.Key, "work p99 ms", was.Work.P99, now.Work.P99, tolerancePercent * 2, TimeFloorMs));
            rows.Add(Row(was.Key, "allocated B/frame", was.AllocatedPerFrame, now.AllocatedPerFrame, tolerancePercent, AllocationFloorBytes));
        }
        foreach (var added in current.Runs)
            if (later.ContainsKey(added.Key)) rows.Add(new ComparisonRow(added.Key, "run", 0, 0, 0, Verdict.New));
        return new Comparison(rows, warnings);
    }

    private static ComparisonRow Row(string key, string metric, double was, double now, double tolerancePercent, double floor)
    {
        double change = was > 0 ? (now - was) / was * 100 : now > 0 ? double.PositiveInfinity : 0;
        var verdict = Verdict.Same;
        if (Math.Abs(now - was) > floor)
        {
            if (change > tolerancePercent) verdict = Verdict.Regressed;
            else if (change < -tolerancePercent) verdict = Verdict.Improved;
        }
        return new ComparisonRow(key, metric, was, now, change, verdict);
    }
}

public readonly record struct LimitPoint(int Load, double Value);

/// <summary><see cref="Limit"/>: the largest load measured under the budget (0: none was); <see cref="FirstOver"/>: the smallest over it (0: none was).</summary>
public sealed record LimitResult(int Limit, int FirstOver, IReadOnlyList<LimitPoint> Points);

/// <summary>Finds the load at which a measure crosses a budget: doubling, then bisecting.</summary>
public static class LimitSearch
{
    /// <summary>
    /// Measures at <paramref name="start"/> and doubles the load while the measure stays within
    /// <paramref name="budget"/> (never past <paramref name="max"/>), then halves the gap between the last load under
    /// and the first over <paramref name="refinements"/> times.
    /// </summary>
    public static LimitResult Find(Func<int, double> measure, int start, double budget, int max, int refinements)
    {
        ArgumentNullException.ThrowIfNull(measure);
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(max, start);
        var points = new List<LimitPoint>();
        bool Over(int load)
        {
            double value = measure(load);
            points.Add(new LimitPoint(load, value));
            return value > budget;
        }
        int under = 0, over = 0;
        for (int load = start; ;)
        {
            if (Over(load)) { over = load; break; }
            under = load;
            if (load >= max) break;
            load = (int)Math.Min(max, (long)load * 2);
        }
        for (int i = 0; i < refinements && under > 0 && over > 0 && over - under > 1; i++)
        {
            int middle = under + (over - under) / 2;
            if (Over(middle)) over = middle;
            else under = middle;
        }
        points.Sort((a, b) => a.Load.CompareTo(b.Load));
        return new LimitResult(under, over, points);
    }
}
