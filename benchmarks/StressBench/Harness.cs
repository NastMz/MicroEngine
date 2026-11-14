using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Sdl;

namespace StressBench;

/// <summary>
/// What a run records per fixed step beside the engine's frame profiler: the time each of the scenario's subsystems
/// took and the time the runtime paused the process for garbage collections. Everything is stored in arrays created
/// before the run, so the probe itself allocates nothing while frames are measured.
/// </summary>
internal sealed class Probe
{
    private readonly double[][] _sections;
    private readonly double[] _pause;
    private readonly int[] _collections = new int[6];
    private TimeSpan _pauseAtStart, _lastPause;
    private int _step = -1;

    /// <summary>The first step that counts; earlier ones are warm-up.</summary>
    public int Warmup { get; init; }

    public Probe(int steps, string[] sections)
    {
        _pause = new double[steps + 1];
        _sections = new double[sections.Length][];
        for (int i = 0; i < sections.Length; i++) _sections[i] = new double[steps + 1];
    }

    /// <summary>Starts a fixed step: the pause time since the previous step started belongs to the previous one.</summary>
    public void BeginStep()
    {
        var pause = GC.GetTotalPauseDuration();
        if (_step >= 0 && _step < _pause.Length) _pause[_step] = (pause - _lastPause).TotalMilliseconds;
        _lastPause = pause;
        _step++;
        if (_step == Warmup)
        {
            _pauseAtStart = pause;
            for (int g = 0; g < 3; g++) _collections[g] = GC.CollectionCount(g);
        }
    }

    /// <summary>Adds the time since <paramref name="since"/> to <paramref name="section"/> of the current step; returns now.</summary>
    public long Lap(int section, long since)
    {
        long now = Stopwatch.GetTimestamp();
        if (_step >= 0 && _step < _pause.Length) _sections[section][_step] += Stopwatch.GetElapsedTime(since, now).TotalMilliseconds;
        return now;
    }

    /// <summary>Closes the last step's measurements; call once after the run.</summary>
    public void Finish()
    {
        var pause = GC.GetTotalPauseDuration();
        if (_step >= 0 && _step < _pause.Length) _pause[_step] = (pause - _lastPause).TotalMilliseconds;
        _lastPause = pause;
        for (int g = 0; g < 3; g++) _collections[3 + g] = GC.CollectionCount(g);
    }

    /// <summary>The step being run, from 0 (-1 before the first).</summary>
    public int Step => _step;

    /// <summary>Steps measured after the warm-up.</summary>
    public int Steps => Math.Max(0, Math.Min(_step + 1, _pause.Length) - Warmup);

    /// <summary>Collections of generation <paramref name="generation"/> and older during the measured steps, as the runtime counts them.</summary>
    public int Collections(int generation) => _collections[3 + generation] - _collections[generation];
    public double PauseTotalMs => (_lastPause - _pauseAtStart).TotalMilliseconds;
    public double PauseMaxMs => Steps == 0 ? 0 : _pause.AsSpan(Warmup, Steps).ToArray().Max();
    public Dist Section(int index) => Dist.Of(_sections[index].AsSpan(Warmup, Steps));
}

/// <summary>Runs scenarios through the engine's real loop and turns what the profiler and the probe saw into records.</summary>
public static class Harness
{
    /// <summary>
    /// Runs <paramref name="scenario"/> at <paramref name="load"/> in <paramref name="mode"/>:
    /// <paramref name="options"/>' warm-up frames, then its measured frames.
    /// </summary>
    /// <exception cref="InvalidOperationException">The game failed, or the mode's platform is not available here.</exception>
    public static RunRecord Run(string scenario, int load, RunMode mode, StressOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var scene = Scenarios.Create(scenario, load, options.Backend);
        var game = new StressGame(scene);
        int total = options.Warmup + options.Frames;
        var probe = new Probe(total, scene.SectionNames) { Warmup = options.Warmup };
        scene.Probe = probe;
        // In real time a frame can run no step or several, so the profiler keeps room for more frames than steps.
        var profiler = new FrameProfiler(total * 4);
        using var platform = CreatePlatform(game.Settings, mode);
        try
        {
            Settle();
            var result = GameRunner.Run(game, platform, new RunOptions
            {
                MaxUpdates = total,
                Seed = options.Seed,
                Mute = true,
                StartScreen = false,
                Profiler = profiler,
                TimeMode = mode == RunMode.EndToEnd ? TimeMode.RealTime : TimeMode.Deterministic,
            });
            if (result.Error is not null) throw new InvalidOperationException($"{scenario} at load {load} failed: {result.Error.Message}", result.Error);
            probe.Finish();
            return Record(scenario, load, mode, options, scene, profiler, probe);
        }
        finally { scene.Cleanup(); }
    }

    private static IPlatform CreatePlatform(GameSettings settings, RunMode mode) => mode switch
    {
        RunMode.Headless => HeadlessPlatform.For(settings),
        RunMode.Render => SdlPlatform.Profile.Create(new PlatformRequest(settings) { Hidden = true, WaitForDisplay = false, Mute = true }),
        _ => SdlPlatform.Profile.Create(new PlatformRequest(settings) { Mute = true }),
    };

    // A full, blocking collection before a run, so one scenario's garbage is not collected inside the next.
    private static void Settle()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static RunRecord Record(string scenario, int load, RunMode mode, StressOptions options, StressScene scene, FrameProfiler profiler, Probe probe)
    {
        // The warm-up is counted in steps; in real time the frames that ran them are found by their step counts.
        var samples = profiler.Samples;
        int skip = 0;
        for (long steps = 0; skip < samples.Count && steps < options.Warmup; skip++) steps += samples[skip].Steps;
        var summary = profiler.Summarize(skip);
        double maxAllocated = 0;
        for (int i = skip; i < samples.Count; i++) maxAllocated = Math.Max(maxAllocated, samples[i].Allocated);

        var resources = new SortedDictionary<string, double>(StringComparer.Ordinal);
        scene.Count(resources);
        var sections = new SortedDictionary<string, Dist>(StringComparer.Ordinal);
        for (int i = 0; i < scene.SectionNames.Length; i++) sections[scene.SectionNames[i]] = probe.Section(i);

        using var process = Process.GetCurrentProcess();
        return new RunRecord
        {
            Scenario = scenario,
            Mode = StressOptions.NameOf(mode),
            Load = load,
            Backend = options.Backend,
            Frames = summary.Frames,
            Checksum = scene.Checksum(),
            Interval = Of(summary.Interval),
            Work = Of(summary.Work),
            Update = Of(summary.Update),
            Render = Of(summary.Render),
            Present = Of(summary.Present),
            AllocatedPerFrame = summary.AllocatedPerFrame,
            AllocatedMaxFrame = maxAllocated,
            Gen0 = probe.Collections(0),
            Gen1 = probe.Collections(1),
            Gen2 = probe.Collections(2),
            PauseTotalMs = probe.PauseTotalMs,
            PauseMaxMs = probe.PauseMaxMs,
            Hitches = summary.Hitches,
            DrawsPerFrame = Math.Max(0, scene.Draws),
            StateChangesPerFrame = scene.StateChanges,
            ManagedBytes = GC.GetTotalMemory(forceFullCollection: false),
            HeapCommittedBytes = GC.GetGCMemoryInfo().TotalCommittedBytes,
            WorkingSetBytes = process.WorkingSet64,
            PrivateBytes = process.PrivateMemorySize64,
            PeakWorkingSetBytes = process.PeakWorkingSet64,
            Resources = resources,
            Sections = sections,
        };
    }

    private static Dist Of(Percentiles p) => new(p.Mean, p.P50, p.P95, p.P99, p.P999, p.Max);

    /// <summary>
    /// The best of <see cref="StressOptions.Repeat"/> runs: the one with the lowest median CPU work, the measure
    /// least disturbed by whatever else the machine is doing.
    /// </summary>
    public static RunRecord RunBest(string scenario, int load, RunMode mode, StressOptions options)
    {
        RunRecord? best = null;
        for (int i = 0; i < options.Repeat; i++)
        {
            var run = Run(scenario, load, mode, options);
            if (best is null || run.Work.P50 < best.Work.P50) best = run;
        }
        return best!;
    }

    /// <summary>Where and how the process measures: machine, system, runtime, compilation and collector.</summary>
    public static SortedDictionary<string, string> DescribeEnvironment(StressOptions options)
    {
        var env = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["cpu"] = CpuName(),
            ["processors"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            ["memoryGB"] = (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024)).ToString("0.0", CultureInfo.InvariantCulture),
            ["os"] = RuntimeInformation.OSDescription,
            ["architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["runtime"] = RuntimeInformation.FrameworkDescription,
            ["compilation"] = RuntimeFeature.IsDynamicCodeCompiled ? "jit" : "aot",
            ["gc"] = (GCSettings.IsServerGC ? "server" : "workstation") + (Concurrent() ? ", concurrent" : ", non-concurrent"),
            ["gcLatency"] = GCSettings.LatencyMode.ToString(),
            ["engine"] = typeof(Game).Assembly.GetName().Version?.ToString() ?? "",
            ["frames"] = options.Frames.ToString(CultureInfo.InvariantCulture),
            ["warmup"] = options.Warmup.ToString(CultureInfo.InvariantCulture),
            ["seed"] = options.Seed.ToString(CultureInfo.InvariantCulture),
            ["repeat"] = options.Repeat.ToString(CultureInfo.InvariantCulture),
        };
        if (options.Label is not null) env["label"] = options.Label;
        if (options.Commit is not null) env["commit"] = options.Commit;
        // Runtime switches set from outside (DOTNET_gcServer, DOTNET_TieredPGO...), so a file says what it ran with.
        var knobs = new List<string>();
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            if (e.Key is string key && key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase) && IsRuntimeKnob(key)) knobs.Add($"{key}={e.Value}");
        knobs.Sort(StringComparer.Ordinal);
        if (knobs.Count > 0) env["runtimeSwitches"] = string.Join(' ', knobs);
        return env;
    }

    private static bool IsRuntimeKnob(string key) =>
        key.Contains("gc", StringComparison.OrdinalIgnoreCase) || key.Contains("Tiered", StringComparison.OrdinalIgnoreCase)
        || key.Contains("TC_", StringComparison.OrdinalIgnoreCase) || key.Contains("ReadyToRun", StringComparison.OrdinalIgnoreCase);

    private static bool Concurrent()
    {
        if (Environment.GetEnvironmentVariable("DOTNET_gcConcurrent") is { } text) return text != "0";
        return AppContext.TryGetSwitch("System.GC.Concurrent", out bool on) ? on : true;
    }

    /// <summary>Adds what the SDL platform reports about its renderer, when a mode used it.</summary>
    public static void DescribeRenderer(SortedDictionary<string, string> env)
    {
        try
        {
            var settings = new GameSettings("StressBench probe", 320, 180, 3);
            using var platform = SdlPlatform.Profile.Create(new PlatformRequest(settings) { Hidden = true, WaitForDisplay = false, Mute = true });
            if (!platform.TryGetCapability<IPlatformDiagnostics>(out var facts)) return;
            foreach (string key in new[] { "sdl", "renderer", "videoDriver", "adapter", "display", "vsync" })
                if (facts.Diagnostics.TryGetValue(key, out string? value)) env[$"sdl.{key}"] = value;
        }
        catch (InvalidOperationException e) { env["sdl.renderer"] = $"unavailable ({e.Message})"; }
    }

    private static string CpuName()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key?.GetValue("ProcessorNameString") is string name) return name.Trim();
            }
            else if (File.Exists("/proc/cpuinfo"))
            {
                foreach (string line in File.ReadLines("/proc/cpuinfo"))
                    if (line.StartsWith("model name", StringComparison.Ordinal)) return line[(line.IndexOf(':') + 1)..].Trim();
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException) { }
        return RuntimeInformation.ProcessArchitecture.ToString();
    }
}
