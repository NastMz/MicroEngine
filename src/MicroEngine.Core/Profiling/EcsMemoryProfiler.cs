using MicroEngine.Core.ECS;
using MicroEngine.Core.Exceptions;

namespace MicroEngine.Core.Profiling;

/// <summary>
/// Memory profiler specifically designed for tracking ECS memory usage.
/// Provides detailed insights into entity, component, and archetype memory consumption.
/// </summary>
public sealed class EcsMemoryProfiler
{
    // Metric key constants to avoid magic strings
    private const string METRIC_ENTITY_COUNT = "EntityCount";
    private const string METRIC_SYSTEM_COUNT = "SystemCount";
    private const string METRIC_ESTIMATED_COMPONENT_MEMORY = "EstimatedComponentMemoryBytes";

    private readonly World _world;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsMemoryProfiler"/> class.
    /// </summary>
    /// <param name="world">The ECS world to profile.</param>
    /// <exception cref="InvalidProfilingOperationException">Thrown when world is null.</exception>
    public EcsMemoryProfiler(World world)
    {
        _world = world ?? throw new InvalidProfilingOperationException("World cannot be null")
            .WithContext("parameterName", nameof(world));
    }

    /// <summary>
    /// Captures a detailed ECS memory snapshot.
    /// </summary>
    /// <returns>A memory snapshot with ECS-specific metrics.</returns>
    public MemorySnapshot CaptureEcsSnapshot()
    {
        var snapshot = MemorySnapshot.Capture(forceGC: false);

        // Add ECS-specific metrics
        snapshot.CustomMetrics[METRIC_ENTITY_COUNT] = _world.EntityCount;
        snapshot.CustomMetrics[METRIC_SYSTEM_COUNT] = _world.SystemCount;

        // Estimate total component memory (approximation based on entity count)
        // Each component array has overhead + data
        var estimatedComponentMemory = EstimateComponentMemory();
        snapshot.CustomMetrics[METRIC_ESTIMATED_COMPONENT_MEMORY] = estimatedComponentMemory;

        return snapshot;
    }

    /// <summary>
    /// Estimates total memory used by component storage.
    /// This is an approximation based on entity count and average component size.
    /// </summary>
    /// <returns>Estimated component memory in bytes.</returns>
    private long EstimateComponentMemory()
    {
        // Conservative estimate: 
        // - Base overhead per entity: ~64 bytes (Entity struct, dictionary entries, etc.)
        // - Average component size: ~32 bytes (position, velocity, health, etc.)
        // - Average components per entity: ~3
        const int BASE_OVERHEAD_PER_ENTITY = 64;
        const int AVG_COMPONENT_SIZE = 32;
        const int AVG_COMPONENTS_PER_ENTITY = 3;

        var entityCount = _world.EntityCount;
        var estimatedBytes = entityCount * (BASE_OVERHEAD_PER_ENTITY + (AVG_COMPONENT_SIZE * AVG_COMPONENTS_PER_ENTITY));

        return estimatedBytes;
    }

    /// <summary>
    /// Gets memory statistics specific to the ECS world.
    /// </summary>
    /// <returns>A dictionary containing ECS memory metrics.</returns>
    public Dictionary<string, long> GetEcsMemoryStats()
    {
        return new Dictionary<string, long>
        {
            { METRIC_ENTITY_COUNT, _world.EntityCount },
            { METRIC_SYSTEM_COUNT, _world.SystemCount },
            { METRIC_ESTIMATED_COMPONENT_MEMORY, EstimateComponentMemory() },
            { "EstimatedComponentMemoryMB", EstimateComponentMemory() / (1024 * 1024) }
        };
    }

    /// <summary>
    /// Monitors ECS memory usage over a period of time and detects anomalies.
    /// </summary>
    /// <param name="profiler">The memory profiler to use for tracking.</param>
    /// <param name="durationSeconds">Duration to monitor in seconds.</param>
    /// <param name="sampleIntervalMs">Interval between samples in milliseconds.</param>
    /// <returns>A list of captured snapshots during the monitoring period.</returns>
    public async Task<List<MemorySnapshot>> MonitorMemoryAsync(
        MemoryProfiler profiler,
        int durationSeconds = 60,
        int sampleIntervalMs = 1000)
    {
        var snapshots = new List<MemorySnapshot>();
        var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);

        while (DateTime.UtcNow < endTime)
        {
            var snapshot = CaptureEcsSnapshot();
            snapshots.Add(snapshot);
            profiler.CaptureSnapshot(forceGC: false);

            await Task.Delay(sampleIntervalMs);
        }

        return snapshots;
    }

    /// <summary>
    /// Generates a report of ECS memory usage based on captured snapshots.
    /// </summary>
    /// <param name="snapshots">The snapshots to analyze.</param>
    /// <returns>A formatted string report.</returns>
    public static string GenerateMemoryReport(List<MemorySnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return "No snapshots available for report.";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== ECS Memory Report ===");
        sb.AppendLine($"Snapshots: {snapshots.Count}");
        sb.AppendLine($"Time Range: {snapshots[0].Timestamp:yyyy-MM-dd HH:mm:ss} to {snapshots[^1].Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Memory summary
        var minMemory = snapshots.Min(s => s.TotalMemoryMB);
        var maxMemory = snapshots.Max(s => s.TotalMemoryMB);
        var avgMemory = snapshots.Average(s => s.TotalMemoryMB);

        sb.AppendLine("Memory Usage:");
        sb.AppendLine($"  Min: {minMemory:F2} MB");
        sb.AppendLine($"  Max: {maxMemory:F2} MB");
        sb.AppendLine($"  Avg: {avgMemory:F2} MB");
        sb.AppendLine($"  Delta: {maxMemory - minMemory:F2} MB");
        sb.AppendLine();

        // Entity count summary
        if (snapshots[0].CustomMetrics.TryGetValue(METRIC_ENTITY_COUNT, out var firstEntityCount) &&
            snapshots[^1].CustomMetrics.TryGetValue(METRIC_ENTITY_COUNT, out var lastEntityCount))
        {
            sb.AppendLine("Entity Count:");
            sb.AppendLine($"  Start: {firstEntityCount}");
            sb.AppendLine($"  End: {lastEntityCount}");
            sb.AppendLine($"  Delta: {lastEntityCount - firstEntityCount}");
            sb.AppendLine();
        }

        // GC statistics
        var gen0Delta = snapshots[^1].Gen0Collections - snapshots[0].Gen0Collections;
        var gen1Delta = snapshots[^1].Gen1Collections - snapshots[0].Gen1Collections;
        var gen2Delta = snapshots[^1].Gen2Collections - snapshots[0].Gen2Collections;

        sb.AppendLine("GC Collections:");
        sb.AppendLine($"  Gen0: {gen0Delta}");
        sb.AppendLine($"  Gen1: {gen1Delta}");
        sb.AppendLine($"  Gen2: {gen2Delta}");
        sb.AppendLine();

        // Leak detection
        var memoryGrowthMB = maxMemory - minMemory;
        var hasLeak = memoryGrowthMB > 10.0; // Threshold: 10 MB growth

        if (hasLeak)
        {
            sb.AppendLine("⚠️ WARNING: Potential memory leak detected!");
            sb.AppendLine($"   Memory grew by {memoryGrowthMB:F2} MB over monitoring period.");
        }
        else
        {
            sb.AppendLine("✅ No significant memory leaks detected.");
        }

        return sb.ToString();
    }
}
