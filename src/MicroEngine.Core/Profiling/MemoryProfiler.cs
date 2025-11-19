using MicroEngine.Core.Exceptions;

namespace MicroEngine.Core.Profiling;

/// <summary>
/// Memory profiler for tracking memory usage over time and detecting memory leaks.
/// Provides snapshots, delta tracking, and automatic leak detection.
/// </summary>
public sealed class MemoryProfiler
{
    private readonly List<MemorySnapshot> _snapshots = new();
    private readonly object _lock = new();
    private MemorySnapshot? _baseline;

    private int _maxSnapshots = 1000;

    /// <summary>
    /// Gets or sets the maximum number of snapshots to retain (default: 1000).
    /// Older snapshots are automatically evicted.
    /// </summary>
    /// <exception cref="InvalidProfilingConfigurationException">Thrown when value is less than 1.</exception>
    public int MaxSnapshots
    {
        get => _maxSnapshots;
        set
        {
            if (value < 1)
            {
                throw new InvalidProfilingConfigurationException(nameof(MaxSnapshots),
                    $"MaxSnapshots must be at least 1, got {value}")
                    .WithContext("value", value.ToString());
            }
            _maxSnapshots = value;
        }
    }

    /// <summary>
    /// Gets the number of snapshots currently stored.
    /// </summary>
    public int SnapshotCount
    {
        get
        {
            lock (_lock)
            {
                return _snapshots.Count;
            }
        }
    }

    /// <summary>
    /// Gets the most recent snapshot, or null if none have been captured.
    /// </summary>
    public MemorySnapshot? LatestSnapshot
    {
        get
        {
            lock (_lock)
            {
                return _snapshots.Count > 0 ? _snapshots[^1] : null;
            }
        }
    }

    /// <summary>
    /// Gets the baseline snapshot used for delta calculations, or null if not set.
    /// </summary>
    public MemorySnapshot? Baseline
    {
        get
        {
            lock (_lock)
            {
                return _baseline;
            }
        }
    }

    /// <summary>
    /// Captures a new memory snapshot and adds it to the profiler's history.
    /// </summary>
    /// <param name="forceGC">Whether to force a full GC before capturing (expensive, use for leak detection).</param>
    /// <returns>The captured snapshot.</returns>
    public MemorySnapshot CaptureSnapshot(bool forceGC = false)
    {
        var snapshot = MemorySnapshot.Capture(forceGC);

        lock (_lock)
        {
            _snapshots.Add(snapshot);

            // Evict oldest snapshot if over limit
            if (_snapshots.Count > MaxSnapshots)
            {
                _snapshots.RemoveAt(0);
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Sets the current snapshot as the baseline for delta tracking.
    /// </summary>
    /// <param name="forceGC">Whether to force a full GC before capturing the baseline.</param>
    public void SetBaseline(bool forceGC = false)
    {
        var snapshot = CaptureSnapshot(forceGC);

        lock (_lock)
        {
            _baseline = snapshot;
        }
    }

    /// <summary>
    /// Gets the memory delta between the current state and the baseline.
    /// </summary>
    /// <returns>The delta snapshot, or null if no baseline is set.</returns>
    public MemorySnapshot? GetDeltaFromBaseline()
    {
        lock (_lock)
        {
            if (_baseline == null)
            {
                return null;
            }

            var current = MemorySnapshot.Capture(forceGC: false);
            return current.DeltaFrom(_baseline);
        }
    }

    /// <summary>
    /// Gets all snapshots captured within a time range.
    /// </summary>
    /// <param name="startTime">The start of the time range.</param>
    /// <param name="endTime">The end of the time range.</param>
    /// <returns>List of snapshots within the time range.</returns>
    /// <exception cref="InvalidProfilingOperationException">Thrown when endTime is before startTime.</exception>
    public List<MemorySnapshot> GetSnapshotsInRange(DateTime startTime, DateTime endTime)
    {
        if (endTime < startTime)
        {
            throw new InvalidProfilingOperationException(
                $"End time ({endTime:O}) must be after start time ({startTime:O})")
                .WithContext("startTime", startTime.ToString("O"))
                .WithContext("endTime", endTime.ToString("O"));
        }

        lock (_lock)
        {
            return _snapshots
                .Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime)
                .ToList();
        }
    }

    /// <summary>
    /// Detects potential memory leaks by comparing recent snapshots.
    /// A leak is suspected if memory consistently grows over the sample window.
    /// </summary>
    /// <param name="sampleCount">Number of recent snapshots to analyze (default: 10).</param>
    /// <param name="growthThresholdMB">Memory growth threshold in MB to trigger leak detection (default: 5 MB).</param>
    /// <returns>True if a potential memory leak is detected.</returns>
    /// <exception cref="InvalidProfilingConfigurationException">Thrown when parameters are invalid.</exception>
    public bool DetectMemoryLeak(int sampleCount = 10, double growthThresholdMB = 5.0)
    {
        if (sampleCount < 2)
        {
            throw new InvalidProfilingConfigurationException(nameof(sampleCount),
                $"Sample count must be at least 2, got {sampleCount}")
                .WithContext("value", sampleCount.ToString());
        }

        if (growthThresholdMB <= 0)
        {
            throw new InvalidProfilingConfigurationException(nameof(growthThresholdMB),
                $"Growth threshold must be positive, got {growthThresholdMB}")
                .WithContext("value", growthThresholdMB.ToString());
        }

        lock (_lock)
        {
            if (_snapshots.Count < sampleCount)
            {
                return false;
            }

            var recentSnapshots = _snapshots.TakeLast(sampleCount).ToList();
            var firstSnapshot = recentSnapshots[0];
            var lastSnapshot = recentSnapshots[^1];

            var memoryGrowthMB = lastSnapshot.TotalMemoryMB - firstSnapshot.TotalMemoryMB;

            // Check if memory consistently grew (monotonic increase)
            var isMonotonicIncrease = true;
            for (int i = 1; i < recentSnapshots.Count; i++)
            {
                if (recentSnapshots[i].TotalMemoryBytes < recentSnapshots[i - 1].TotalMemoryBytes)
                {
                    isMonotonicIncrease = false;
                    break;
                }
            }

            return isMonotonicIncrease && memoryGrowthMB >= growthThresholdMB;
        }
    }

    /// <summary>
    /// Gets memory statistics over all captured snapshots.
    /// </summary>
    /// <returns>A dictionary containing min, max, average, and current memory usage.</returns>
    public Dictionary<string, double> GetStatistics()
    {
        lock (_lock)
        {
            if (_snapshots.Count == 0)
            {
                return new Dictionary<string, double>
                {
                    { "MinMemoryMB", 0 },
                    { "MaxMemoryMB", 0 },
                    { "AvgMemoryMB", 0 },
                    { "CurrentMemoryMB", 0 },
                    { "TotalGen0Collections", 0 },
                    { "TotalGen1Collections", 0 },
                    { "TotalGen2Collections", 0 }
                };
            }

            var minMemory = _snapshots.Min(s => s.TotalMemoryMB);
            var maxMemory = _snapshots.Max(s => s.TotalMemoryMB);
            var avgMemory = _snapshots.Average(s => s.TotalMemoryMB);
            var currentMemory = _snapshots[^1].TotalMemoryMB;

            var totalGen0 = _snapshots[^1].Gen0Collections - _snapshots[0].Gen0Collections;
            var totalGen1 = _snapshots[^1].Gen1Collections - _snapshots[0].Gen1Collections;
            var totalGen2 = _snapshots[^1].Gen2Collections - _snapshots[0].Gen2Collections;

            return new Dictionary<string, double>
            {
                { "MinMemoryMB", minMemory },
                { "MaxMemoryMB", maxMemory },
                { "AvgMemoryMB", avgMemory },
                { "CurrentMemoryMB", currentMemory },
                { "TotalGen0Collections", totalGen0 },
                { "TotalGen1Collections", totalGen1 },
                { "TotalGen2Collections", totalGen2 }
            };
        }
    }

    /// <summary>
    /// Clears all snapshots and resets the baseline.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _snapshots.Clear();
            _baseline = null;
        }
    }

    /// <summary>
    /// Exports snapshots to a CSV format for external analysis.
    /// </summary>
    /// <returns>CSV string containing all snapshots.</returns>
    public string ExportToCsv()
    {
        lock (_lock)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Timestamp,TotalMemoryMB,Gen0Collections,Gen1Collections,Gen2Collections,Gen0MemoryMB,Gen1MemoryMB,Gen2MemoryMB");

            foreach (var snapshot in _snapshots)
            {
                sb.AppendLine($"{snapshot.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                              $"{snapshot.TotalMemoryMB:F2}," +
                              $"{snapshot.Gen0Collections}," +
                              $"{snapshot.Gen1Collections}," +
                              $"{snapshot.Gen2Collections}," +
                              $"{snapshot.Gen0MemoryBytes / (1024.0 * 1024.0):F2}," +
                              $"{snapshot.Gen1MemoryBytes / (1024.0 * 1024.0):F2}," +
                              $"{snapshot.Gen2MemoryBytes / (1024.0 * 1024.0):F2}");
            }

            return sb.ToString();
        }
    }
}
