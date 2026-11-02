using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MicroEngine.Core;

/// <summary>One frame as <see cref="FrameProfiler"/> measured it. Times are in milliseconds.</summary>
/// <param name="Interval">Time from the start of the previous frame to the start of this one (0 for the first frame).</param>
/// <param name="Events">Polling system events (<see cref="IPlatform.PollEvents"/>).</param>
/// <param name="Update">Running the fixed steps of this frame.</param>
/// <param name="Render">The game drawing its frame (<see cref="IPlatform.BeginFrame"/> and the scenes).</param>
/// <param name="Present">Finishing and presenting the frame (<see cref="IPlatform.EndFrame"/>); with VSync on, this is
/// mostly the wait for the display.</param>
/// <param name="Steps">Fixed steps run in this frame.</param>
/// <param name="SimulatedTime">Seconds of game time after the frame's last step (<see cref="Game.Time"/>).</param>
/// <param name="InterpolatedTime">Seconds of game time a game drawing with <see cref="Game.FrameBlend"/> shows.</param>
/// <param name="Allocated">Bytes the loop's thread allocated during the frame.</param>
/// <param name="Collections">Garbage collections (any generation) that happened during the frame.</param>
public readonly record struct FrameSample(double Interval, double Events, double Update, double Render, double Present,
    int Steps, double SimulatedTime, double InterpolatedTime, long Allocated, int Collections);

/// <summary>
/// Measures a run frame by frame and the phases of its start, for <c>--profile</c> and benchmarks. It only measures
/// what the engine itself sees: when <see cref="IPlatform.EndFrame"/> returns, not when the display shows the frame
/// (presentation times need the platform's own tools). Samples go into a buffer allocated up front, so profiling
/// adds no allocations to the frames it measures; a run without a profiler does not measure anything.
/// </summary>
public sealed class FrameProfiler
{
    private readonly FrameSample[] _samples;
    private readonly List<(string Phase, double Milliseconds)> _phases = [];
    private readonly long _created = Stopwatch.GetTimestamp();
    private long _phaseStart;
    private int _count;
    private long _total;

    /// <summary>Creates a profiler that keeps the last <paramref name="capacity"/> frames (36,000 by default: ten minutes at 60 frames per second).</summary>
    public FrameProfiler(int capacity = 36_000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _samples = new FrameSample[capacity];
        _phaseStart = _created;
    }

    /// <summary>Frames measured since the start (including those no longer kept).</summary>
    public long FrameCount => _total;

    /// <summary>The kept frames, oldest first.</summary>
    public IReadOnlyList<FrameSample> Samples
    {
        get
        {
            var list = new List<FrameSample>(_count);
            int start = _total > _samples.Length ? (int)(_total % _samples.Length) : 0;
            for (int i = 0; i < _count; i++) list.Add(_samples[(start + i) % _samples.Length]);
            return list;
        }
    }

    /// <summary>The start phases recorded with <see cref="Phase"/>, in order, with their durations in milliseconds.</summary>
    public IReadOnlyList<(string Phase, double Milliseconds)> Phases => _phases;

    /// <summary>
    /// The refresh rate of the display pacing the run (<see cref="IPlatform.DisplayRate"/>), or 0 when unknown. With it,
    /// motion is judged against the whole refresh periods each frame stayed on screen rather than against the loop's
    /// irregular wake-ups; the loop sets it when the run starts.
    /// </summary>
    public double DisplayRate { get; set; }

    /// <summary>Free-form facts about the run (machine, platform, renderer, display), written with the results.</summary>
    public IDictionary<string, string> Environment { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Ends the current start phase: records the time since the previous phase ended (or since the profiler was created) under <paramref name="name"/>.</summary>
    public void Phase(string name)
    {
        long now = Stopwatch.GetTimestamp();
        _phases.Add((name, Stopwatch.GetElapsedTime(_phaseStart, now).TotalMilliseconds));
        _phaseStart = now;
    }

    /// <summary>Records a frame.</summary>
    public void Add(in FrameSample sample)
    {
        _samples[(int)(_total % _samples.Length)] = sample;
        _total++;
        if (_count < _samples.Length) _count++;
    }

    /// <summary>Statistics of the kept frames, skipping the first <paramref name="warmup"/> (start-up and JIT).</summary>
    public FrameSummary Summarize(int warmup = 0) => FrameSummary.Of(Samples.Skip(Math.Max(0, warmup)).ToList(), DisplayRate);

    /// <summary>The results as JSON: environment, start phases, summary and every kept frame.</summary>
    public string ToJson(int warmup = 0)
    {
        var s = Summarize(warmup);
        var json = new StringBuilder();
        json.Append("{\n  \"environment\": {");
        json.AppendJoin(",", Environment.Select(e => $"\n    {Quote(e.Key)}: {Quote(e.Value)}"));
        json.Append("\n  },\n  \"displayRate\": ").Append(N(DisplayRate));
        json.Append(",\n  \"phases\": [");
        json.AppendJoin(",", _phases.Select(p => $"\n    {{ \"phase\": {Quote(p.Phase)}, \"ms\": {N(p.Milliseconds)} }}"));
        json.Append("\n  ],\n  \"summary\": ").Append(s.ToJson("  "));
        json.Append(",\n  \"frames\": [");
        json.AppendJoin(",", Samples.Select(f => $"\n    [{N(f.Interval)},{N(f.Events)},{N(f.Update)},{N(f.Render)},{N(f.Present)},{f.Steps},{N(f.SimulatedTime * 1000)},{N(f.InterpolatedTime * 1000)},{f.Allocated},{f.Collections}]"));
        json.Append("\n  ],\n  \"frameColumns\": [\"intervalMs\",\"eventsMs\",\"updateMs\",\"renderMs\",\"presentMs\",\"steps\",\"simulatedMs\",\"interpolatedMs\",\"allocatedBytes\",\"collections\"]\n}\n");
        return json.ToString();
    }

    /// <summary>Every kept frame as CSV, one row per frame.</summary>
    public string ToCsv()
    {
        var csv = new StringBuilder("intervalMs,eventsMs,updateMs,renderMs,presentMs,steps,simulatedMs,interpolatedMs,allocatedBytes,collections\n");
        foreach (var f in Samples)
            csv.Append(CultureInfo.InvariantCulture, $"{N(f.Interval)},{N(f.Events)},{N(f.Update)},{N(f.Render)},{N(f.Present)},{f.Steps},{N(f.SimulatedTime * 1000)},{f.Allocated},{f.Collections}\n");
        return csv.ToString();
    }

    /// <summary>Writes <see cref="ToJson"/> to <paramref name="path"/>, or <see cref="ToCsv"/> when it ends in ".csv".</summary>
    public void Save(string path, int warmup = 0)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } dir) Directory.CreateDirectory(dir);
        File.WriteAllText(path, path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? ToCsv() : ToJson(warmup));
    }

    internal static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Quote(string text)
    {
        var s = new StringBuilder("\"");
        foreach (char c in text)
            s.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                < ' ' => $"\\u{(int)c:x4}",
                _ => c.ToString(),
            });
        return s.Append('"').ToString();
    }
}

/// <summary>Percentiles of one frame measurement, in milliseconds.</summary>
public readonly record struct Percentiles(double Mean, double P50, double P95, double P99, double P999, double Max)
{
    internal static Percentiles Of(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return default;
        var sorted = values.Order().ToArray();
        double At(double q) => sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(q * sorted.Length) - 1 is var i && i < 0 ? 0 : (int)Math.Ceiling(q * sorted.Length) - 1)];
        return new(sorted.Average(), At(0.50), At(0.95), At(0.99), At(0.999), sorted[^1]);
    }

    internal string ToJson() => $"{{ \"mean\": {FrameProfiler.N(Mean)}, \"p50\": {FrameProfiler.N(P50)}, \"p95\": {FrameProfiler.N(P95)}, \"p99\": {FrameProfiler.N(P99)}, \"p99_9\": {FrameProfiler.N(P999)}, \"max\": {FrameProfiler.N(Max)} }}";
}

/// <summary>
/// What a profiled run looked like. <see cref="MotionError"/> is the measure of uneven motion: for every frame, how far
/// the game time it advanced differs from the wall time that passed, so a frame that repeats the previous picture or
/// jumps two steps counts even when every frame took the same time.
/// </summary>
/// <param name="Frames">Frames summarized.</param>
/// <param name="Interval">Time between frame starts.</param>
/// <param name="Work">Events, update and render: the CPU time of a frame before presenting.</param>
/// <param name="Update">Fixed steps.</param>
/// <param name="Render">Drawing.</param>
/// <param name="Present">Presenting (the VSync wait, when on).</param>
/// <param name="MotionError">|game time advanced − time the frame stood for| per frame, drawing the last step. The time
/// a frame stood for is the whole number of refresh periods nearest to its interval when the display rate is known
/// (<see cref="FrameProfiler.DisplayRate"/>), its measured interval otherwise.</param>
/// <param name="InterpolatedMotionError">The same for a game that draws with <see cref="Game.FrameBlend"/>.</param>
/// <param name="StepsPerFrame">How many frames ran 0, 1, 2... steps (index = steps).</param>
/// <param name="AllocatedPerFrame">Mean bytes allocated per frame.</param>
/// <param name="Collections">Garbage collections during the frames.</param>
/// <param name="Hitches">Frames whose interval exceeded 1.5 times the median interval.</param>
public sealed record FrameSummary(int Frames, Percentiles Interval, Percentiles Work, Percentiles Update, Percentiles Render,
    Percentiles Present, Percentiles MotionError, Percentiles InterpolatedMotionError, IReadOnlyList<int> StepsPerFrame, double AllocatedPerFrame, int Collections, int Hitches)
{
    internal static FrameSummary Of(IReadOnlyList<FrameSample> frames, double displayRate = 0)
    {
        double Shown(double interval) => displayRate <= 0 ? interval
            : Math.Max(1, Math.Round(interval * displayRate / 1000)) * 1000 / displayRate;
        var timed = frames.Skip(1).ToList(); // the first frame has no interval
        var interval = Percentiles.Of(timed.Select(f => f.Interval).ToList());
        var motion = new List<double>();
        var interpolated = new List<double>();
        for (int i = 1; i < frames.Count; i++)
        {
            double shown = Shown(frames[i].Interval);
            motion.Add(Math.Abs((frames[i].SimulatedTime - frames[i - 1].SimulatedTime) * 1000 - shown));
            interpolated.Add(Math.Abs((frames[i].InterpolatedTime - frames[i - 1].InterpolatedTime) * 1000 - shown));
        }
        var steps = new int[frames.Count == 0 ? 0 : frames.Max(f => f.Steps) + 1];
        foreach (var f in frames) steps[f.Steps]++;
        return new FrameSummary(
            frames.Count,
            interval,
            Percentiles.Of(frames.Select(f => f.Events + f.Update + f.Render).ToList()),
            Percentiles.Of(frames.Select(f => f.Update).ToList()),
            Percentiles.Of(frames.Select(f => f.Render).ToList()),
            Percentiles.Of(frames.Select(f => f.Present).ToList()),
            Percentiles.Of(motion),
            Percentiles.Of(interpolated),
            steps,
            frames.Count == 0 ? 0 : frames.Average(f => (double)f.Allocated),
            frames.Sum(f => f.Collections),
            timed.Count(f => f.Interval > interval.P50 * 1.5));
    }

    internal string ToJson(string indent) =>
        $"{{\n{indent}  \"frames\": {Frames},\n{indent}  \"intervalMs\": {Interval.ToJson()},\n{indent}  \"workMs\": {Work.ToJson()},\n" +
        $"{indent}  \"updateMs\": {Update.ToJson()},\n{indent}  \"renderMs\": {Render.ToJson()},\n{indent}  \"presentMs\": {Present.ToJson()},\n" +
        $"{indent}  \"motionErrorMs\": {MotionError.ToJson()},\n{indent}  \"interpolatedMotionErrorMs\": {InterpolatedMotionError.ToJson()},\n{indent}  \"stepsPerFrame\": [{string.Join(", ", StepsPerFrame)}],\n" +
        $"{indent}  \"allocatedBytesPerFrame\": {FrameProfiler.N(AllocatedPerFrame)},\n{indent}  \"collections\": {Collections},\n{indent}  \"hitches\": {Hitches}\n{indent}}}";

    /// <summary>A few lines for a console.</summary>
    public override string ToString() => string.Join('\n',
        FormattableString.Invariant($"frames: {Frames}, hitches (>1.5x median interval): {Hitches}, collections: {Collections}, allocated/frame: {AllocatedPerFrame:0} B"),
        FormattableString.Invariant($"interval ms  p50 {Interval.P50:0.00}  p95 {Interval.P95:0.00}  p99 {Interval.P99:0.00}  p99.9 {Interval.P999:0.00}  max {Interval.Max:0.00}"),
        FormattableString.Invariant($"work ms      p50 {Work.P50:0.00}  p95 {Work.P95:0.00}  p99 {Work.P99:0.00}  p99.9 {Work.P999:0.00}  max {Work.Max:0.00}"),
        FormattableString.Invariant($"present ms   p50 {Present.P50:0.00}  p95 {Present.P95:0.00}  p99 {Present.P99:0.00}  max {Present.Max:0.00}"),
        FormattableString.Invariant($"motion error ms  mean {MotionError.Mean:0.00}  p95 {MotionError.P95:0.00}  p99 {MotionError.P99:0.00}  (interpolated: mean {InterpolatedMotionError.Mean:0.00}  p95 {InterpolatedMotionError.P95:0.00})"),
        FormattableString.Invariant($"steps per frame: {string.Join(" ", StepsPerFrame.Select((n, i) => $"{i}:{n}"))}"));
}
