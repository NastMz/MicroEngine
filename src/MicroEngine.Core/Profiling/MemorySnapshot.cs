namespace MicroEngine.Core.Profiling;

/// <summary>
/// Represents a snapshot of memory usage at a specific point in time.
/// Captures GC heap, managed memory, and generation statistics.
/// </summary>
public sealed class MemorySnapshot
{
    /// <summary>
    /// Gets the timestamp when the snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the total managed memory allocated in bytes.
    /// </summary>
    public long TotalMemoryBytes { get; init; }

    /// <summary>
    /// Gets the number of Generation 0 collections.
    /// </summary>
    public int Gen0Collections { get; init; }

    /// <summary>
    /// Gets the number of Generation 1 collections.
    /// </summary>
    public int Gen1Collections { get; init; }

    /// <summary>
    /// Gets the number of Generation 2 collections.
    /// </summary>
    public int Gen2Collections { get; init; }

    /// <summary>
    /// Gets the total memory allocated in Generation 0 in bytes.
    /// </summary>
    public long Gen0MemoryBytes { get; init; }

    /// <summary>
    /// Gets the total memory allocated in Generation 1 in bytes.
    /// </summary>
    public long Gen1MemoryBytes { get; init; }

    /// <summary>
    /// Gets the total memory allocated in Generation 2 in bytes.
    /// </summary>
    public long Gen2MemoryBytes { get; init; }

    /// <summary>
    /// Gets optional custom memory metrics (e.g., resource cache usage).
    /// </summary>
    public Dictionary<string, long> CustomMetrics { get; init; } = new();

    /// <summary>
    /// Gets the total managed memory in megabytes.
    /// </summary>
    public double TotalMemoryMB => TotalMemoryBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Creates a memory snapshot from current system state.
    /// </summary>
    /// <param name="forceGC">Whether to force a full GC before capturing (expensive).</param>
    /// <returns>A new memory snapshot.</returns>
    public static MemorySnapshot Capture(bool forceGC = false)
    {
        if (forceGC)
        {
#pragma warning disable S1215 // GC.Collect is required for accurate memory profiling
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
#pragma warning restore S1215
            GC.WaitForPendingFinalizers();
        }

            var totalMemory = GC.GetTotalMemory(forceFullCollection: false);
            if (totalMemory <= 0)
            {
                totalMemory = GC.GetGCMemoryInfo().HeapSizeBytes;
            }

            return new MemorySnapshot
            {
                Timestamp = DateTime.UtcNow,
                TotalMemoryBytes = totalMemory,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                Gen0MemoryBytes = GC.GetGCMemoryInfo(GCKind.Ephemeral).GenerationInfo[0].SizeAfterBytes,
                Gen1MemoryBytes = GC.GetGCMemoryInfo(GCKind.Ephemeral).GenerationInfo[1].SizeAfterBytes,
                Gen2MemoryBytes = GC.GetGCMemoryInfo(GCKind.FullBlocking).GenerationInfo[2].SizeAfterBytes
            };
    }

    /// <summary>
    /// Compares two snapshots and returns the memory delta.
    /// </summary>
    /// <param name="baseline">The earlier snapshot to compare against.</param>
    /// <returns>A new snapshot representing the difference.</returns>
    public MemorySnapshot DeltaFrom(MemorySnapshot baseline)
    {
        var delta = new MemorySnapshot
        {
            Timestamp = Timestamp,
            TotalMemoryBytes = TotalMemoryBytes - baseline.TotalMemoryBytes,
            Gen0Collections = Gen0Collections - baseline.Gen0Collections,
            Gen1Collections = Gen1Collections - baseline.Gen1Collections,
            Gen2Collections = Gen2Collections - baseline.Gen2Collections,
            Gen0MemoryBytes = Gen0MemoryBytes - baseline.Gen0MemoryBytes,
            Gen1MemoryBytes = Gen1MemoryBytes - baseline.Gen1MemoryBytes,
            Gen2MemoryBytes = Gen2MemoryBytes - baseline.Gen2MemoryBytes
        };

        foreach (var (key, value) in CustomMetrics)
        {
            var baselineValue = baseline.CustomMetrics.TryGetValue(key, out var v) ? v : 0;
            delta.CustomMetrics[key] = value - baselineValue;
        }

        return delta;
    }

    /// <summary>
    /// Returns a human-readable string representation of the snapshot.
    /// </summary>
    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] Total: {TotalMemoryMB:F2} MB | " +
               $"GC: Gen0={Gen0Collections}, Gen1={Gen1Collections}, Gen2={Gen2Collections}";
    }
}
