using MicroEngine.Core.ECS;
using MicroEngine.Core.Exceptions;
using MicroEngine.Core.Profiling;

#pragma warning disable S2925 // Thread.Sleep is needed for time-based tests
namespace MicroEngine.Core.Tests.Profiling;

/// <summary>
/// Tests for MemorySnapshot capturing and delta calculation.
/// </summary>
public sealed class MemorySnapshotTests
{
    [Fact]
    public void Capture_CreatesValidSnapshot()
    {
        // Ensure some memory is allocated to prevent 0-byte snapshots in CI
        var _ = new byte[1024];
        var snapshot = MemorySnapshot.Capture(forceGC: false);

        Assert.NotNull(snapshot);
        Assert.True(snapshot.TotalMemoryBytes > 0);
        Assert.True(snapshot.TotalMemoryMB > 0);
        Assert.InRange(snapshot.Timestamp, DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void Capture_WithForceGC_ExecutesGC()
    {
        var gen0Before = GC.CollectionCount(0);
        var snapshot = MemorySnapshot.Capture(forceGC: true);
        var gen0After = GC.CollectionCount(0);

        Assert.NotNull(snapshot);
        Assert.True(gen0After > gen0Before, "GC should have run");
    }

    [Fact]
    public void TotalMemoryMB_CalculatesCorrectly()
    {
        var snapshot = MemorySnapshot.Capture(forceGC: false);

        var expectedMB = snapshot.TotalMemoryBytes / (1024.0 * 1024.0);
        Assert.Equal(expectedMB, snapshot.TotalMemoryMB, precision: 2);
    }

    [Fact]
    public void GCCollections_CapturesAllGenerations()
    {
        var snapshot = MemorySnapshot.Capture(forceGC: false);

        Assert.True(snapshot.Gen0Collections >= 0);
        Assert.True(snapshot.Gen1Collections >= 0);
        Assert.True(snapshot.Gen2Collections >= 0);
    }

    [Fact]
    public void DeltaFrom_CalculatesMemoryDifference()
    {
        var baseline = MemorySnapshot.Capture(forceGC: false);

        // Allocate some memory
        var _ = new byte[1024 * 1024]; // 1 MB allocation

        var current = MemorySnapshot.Capture(forceGC: false);
        var delta = current.DeltaFrom(baseline);

        Assert.True(delta.TotalMemoryBytes >= 0, "Memory should have increased or stayed the same");
    }

    [Fact]
    public void DeltaFrom_CalculatesGCDifference()
    {
        var baseline = MemorySnapshot.Capture(forceGC: false);

        // Force some GC collections
#pragma warning disable S1215 // Needed for test validation
        GC.Collect(0);
        GC.Collect(0);
#pragma warning restore S1215

        var current = MemorySnapshot.Capture(forceGC: false);
        var delta = current.DeltaFrom(baseline);

        Assert.True(delta.Gen0Collections >= 2, "At least 2 Gen0 collections should have occurred");
    }

    [Fact]
    public void DeltaFrom_HandlesCustomMetrics()
    {
        var baseline = MemorySnapshot.Capture(forceGC: false);
        baseline.CustomMetrics["EntityCount"] = 100;
        baseline.CustomMetrics["SystemCount"] = 5;

        var current = MemorySnapshot.Capture(forceGC: false);
        current.CustomMetrics["EntityCount"] = 150;
        current.CustomMetrics["SystemCount"] = 7;

        var delta = current.DeltaFrom(baseline);

        Assert.Equal(50, delta.CustomMetrics["EntityCount"]);
        Assert.Equal(2, delta.CustomMetrics["SystemCount"]);
    }

    [Fact]
    public void DeltaFrom_HandlesMissingCustomMetric()
    {
        var baseline = MemorySnapshot.Capture(forceGC: false);
        // baseline has no custom metrics

        var current = MemorySnapshot.Capture(forceGC: false);
        current.CustomMetrics["NewMetric"] = 42;

        var delta = current.DeltaFrom(baseline);

        Assert.Equal(42, delta.CustomMetrics["NewMetric"]);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var snapshot = MemorySnapshot.Capture(forceGC: false);
        var str = snapshot.ToString();

        Assert.Contains("Total:", str);
        Assert.Contains("MB", str);
        Assert.Contains("GC:", str);
        Assert.Contains("Gen0=", str);
        Assert.Contains("Gen1=", str);
        Assert.Contains("Gen2=", str);
    }

    [Fact]
    public void CustomMetrics_CanBeModified()
    {
        var snapshot = MemorySnapshot.Capture(forceGC: false);

        snapshot.CustomMetrics["Test"] = 123;
        snapshot.CustomMetrics["Another"] = 456;

        Assert.Equal(123, snapshot.CustomMetrics["Test"]);
        Assert.Equal(456, snapshot.CustomMetrics["Another"]);
        Assert.Equal(2, snapshot.CustomMetrics.Count);
    }
}

/// <summary>
/// Tests for MemoryProfiler tracking and leak detection.
/// </summary>
public sealed class MemoryProfilerTests
{
    [Fact]
    public void CaptureSnapshot_AddsToHistory()
    {
        var profiler = new MemoryProfiler();

        Assert.Equal(0, profiler.SnapshotCount);

        profiler.CaptureSnapshot(forceGC: false);
        Assert.Equal(1, profiler.SnapshotCount);

        profiler.CaptureSnapshot(forceGC: false);
        Assert.Equal(2, profiler.SnapshotCount);
    }

    [Fact]
    public void LatestSnapshot_ReturnsNewest()
    {
        var profiler = new MemoryProfiler();

        Assert.Null(profiler.LatestSnapshot);

        _ = profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(10); // Ensure different timestamps
        var second = profiler.CaptureSnapshot(forceGC: false);

        Assert.Equal(second.Timestamp, profiler.LatestSnapshot!.Timestamp);
    }

    [Fact]
    public void MaxSnapshots_EvictsOldest()
    {
        var profiler = new MemoryProfiler { MaxSnapshots = 3 };

        _ = profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(10);
        _ = profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(10);
        _ = profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(10);
        var snap4 = profiler.CaptureSnapshot(forceGC: false);

        Assert.Equal(3, profiler.SnapshotCount);
        Assert.Equal(snap4.Timestamp, profiler.LatestSnapshot!.Timestamp);
    }

    [Fact]
    public void SetBaseline_CapturesAndStoresBaseline()
    {
        var profiler = new MemoryProfiler();

        Assert.Null(profiler.Baseline);

        profiler.SetBaseline(forceGC: false);

        Assert.NotNull(profiler.Baseline);
        Assert.Equal(1, profiler.SnapshotCount);
    }

    [Fact]
    public void GetDeltaFromBaseline_ReturnsNull_WhenNoBaseline()
    {
        var profiler = new MemoryProfiler();

        var delta = profiler.GetDeltaFromBaseline();

        Assert.Null(delta);
    }

    [Fact]
    public void GetDeltaFromBaseline_CalculatesDifference()
    {
        var profiler = new MemoryProfiler();
        profiler.SetBaseline(forceGC: true);

        // Allocate memory that stays alive
        var allocation = new byte[1024 * 1024]; // 1 MB

        var delta = profiler.GetDeltaFromBaseline();

        Assert.NotNull(delta);
        Assert.True(delta!.TotalMemoryBytes >= 0, $"Expected non-negative delta but got {delta.TotalMemoryBytes}");
        
        // Keep allocation alive
        GC.KeepAlive(allocation);
    }

    [Fact]
    public void GetSnapshotsInRange_FiltersCorrectly()
    {
        var profiler = new MemoryProfiler();

        profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(100);
        var middle = DateTime.UtcNow;
        profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(100);
        var end = DateTime.UtcNow;
        profiler.CaptureSnapshot(forceGC: false);

        var snapshots = profiler.GetSnapshotsInRange(middle.AddMilliseconds(-10), end.AddMilliseconds(10));

        Assert.Equal(2, snapshots.Count);
    }

    [Fact]
    public void DetectMemoryLeak_ReturnsFalse_WithInsufficientSamples()
    {
        var profiler = new MemoryProfiler();

        profiler.CaptureSnapshot(forceGC: false);
        profiler.CaptureSnapshot(forceGC: false);

        var hasLeak = profiler.DetectMemoryLeak(sampleCount: 10);

        Assert.False(hasLeak);
    }

    [Fact]
    public void DetectMemoryLeak_ReturnsFalse_WithStableMemory()
    {
        var profiler = new MemoryProfiler();

        for (int i = 0; i < 15; i++)
        {
            profiler.CaptureSnapshot(forceGC: false);
            Thread.Sleep(5);
        }

        var hasLeak = profiler.DetectMemoryLeak(sampleCount: 10, growthThresholdMB: 5.0);

        // Should be false since we're not allocating significant memory
        Assert.False(hasLeak);
    }

    [Fact]
    public void GetStatistics_ReturnsEmptyStats_WhenNoSnapshots()
    {
        var profiler = new MemoryProfiler();

        var stats = profiler.GetStatistics();

        Assert.Equal(0, stats["MinMemoryMB"]);
        Assert.Equal(0, stats["MaxMemoryMB"]);
        Assert.Equal(0, stats["AvgMemoryMB"]);
        Assert.Equal(0, stats["CurrentMemoryMB"]);
    }

    [Fact]
    public void GetStatistics_CalculatesCorrectly()
    {
        var profiler = new MemoryProfiler();

        profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(10);
        profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(10);
        profiler.CaptureSnapshot(forceGC: false);

        var stats = profiler.GetStatistics();

        Assert.True(stats["MinMemoryMB"] > 0);
        Assert.True(stats["MaxMemoryMB"] >= stats["MinMemoryMB"]);
        Assert.True(stats["AvgMemoryMB"] >= stats["MinMemoryMB"]);
        Assert.True(stats["CurrentMemoryMB"] > 0);
    }

    [Fact]
    public void Clear_RemovesAllSnapshots()
    {
        var profiler = new MemoryProfiler();

        profiler.SetBaseline(forceGC: false);
        profiler.CaptureSnapshot(forceGC: false);
        profiler.CaptureSnapshot(forceGC: false);

        Assert.True(profiler.SnapshotCount > 0);
        Assert.NotNull(profiler.Baseline);

        profiler.Clear();

        Assert.Equal(0, profiler.SnapshotCount);
        Assert.Null(profiler.Baseline);
    }

    [Fact]
    public void ExportToCsv_GeneratesValidCsv()
    {
        var profiler = new MemoryProfiler();

        profiler.CaptureSnapshot(forceGC: false);
        Thread.Sleep(10);
        profiler.CaptureSnapshot(forceGC: false);

        var csv = profiler.ExportToCsv();

        Assert.Contains("Timestamp,TotalMemoryMB,Gen0Collections", csv);
        Assert.True(csv.Split('\n').Length >= 3); // Header + 2 data rows
    }

    [Fact]
    public void ExportToCsv_EmptyProfiler_ReturnsHeaderOnly()
    {
        var profiler = new MemoryProfiler();

        var csv = profiler.ExportToCsv();

        Assert.Contains("Timestamp,TotalMemoryMB", csv);
        Assert.Equal(2, csv.Split('\n').Length); // Header + empty line
    }
}

/// <summary>
/// Tests for EcsMemoryProfiler tracking ECS-specific memory.
/// </summary>
public sealed class EcsMemoryProfilerTests
{
    private struct Position : IComponent
    {
        public float X, Y;
    }

    private struct Velocity : IComponent
    {
        public float X, Y;
    }

    [Fact]
    public void Constructor_RequiresWorld()
    {
        var exception = Assert.Throws<InvalidProfilingOperationException>(() => new EcsMemoryProfiler(null!));

        Assert.Equal("PROF-400", exception.ErrorCode);
        Assert.Contains("World cannot be null", exception.Message);
    }

    [Fact]
    public void CaptureEcsSnapshot_IncludesEntityCount()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        world.CreateEntity("Entity1");
        world.CreateEntity("Entity2");

        var snapshot = ecsProfiler.CaptureEcsSnapshot();

        Assert.Equal(2, snapshot.CustomMetrics["EntityCount"]);
    }

    [Fact]
    public void CaptureEcsSnapshot_IncludesSystemCount()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        // Systems would be added via world.AddSystem, but we're checking the metric exists
        var snapshot = ecsProfiler.CaptureEcsSnapshot();

        Assert.True(snapshot.CustomMetrics.ContainsKey("SystemCount"));
        Assert.Equal(0, snapshot.CustomMetrics["SystemCount"]);
    }

    [Fact]
    public void CaptureEcsSnapshot_EstimatesComponentMemory()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        for (int i = 0; i < 100; i++)
        {
            var entity = world.CreateEntity($"Entity{i}");
            world.AddComponent(entity, new Position { X = i, Y = i });
            world.AddComponent(entity, new Velocity { X = 1, Y = 1 });
        }

        var snapshot = ecsProfiler.CaptureEcsSnapshot();

        Assert.True(snapshot.CustomMetrics["EstimatedComponentMemoryBytes"] > 0);
    }

    [Fact]
    public void GetEcsMemoryStats_ReturnsAllMetrics()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        var entity = world.CreateEntity("TestEntity");
        world.AddComponent(entity, new Position { X = 1, Y = 2 });

        var stats = ecsProfiler.GetEcsMemoryStats();

        Assert.Equal(1, stats["EntityCount"]);
        Assert.Equal(0, stats["SystemCount"]);
        Assert.True(stats["EstimatedComponentMemoryBytes"] > 0);
        Assert.True(stats["EstimatedComponentMemoryMB"] >= 0);
    }

    [Fact]
    public void GetEcsMemoryStats_ScalesWithEntityCount()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        var statsEmpty = ecsProfiler.GetEcsMemoryStats();
        var emptyMemory = statsEmpty["EstimatedComponentMemoryBytes"];

        for (int i = 0; i < 1000; i++)
        {
            var entity = world.CreateEntity($"Entity{i}");
            world.AddComponent(entity, new Position { X = i, Y = i });
        }

        var statsFull = ecsProfiler.GetEcsMemoryStats();
        var fullMemory = statsFull["EstimatedComponentMemoryBytes"];

        Assert.True(fullMemory > emptyMemory);
    }

    [Fact]
    public void GenerateMemoryReport_HandlesEmptySnapshots()
    {
        var report = EcsMemoryProfiler.GenerateMemoryReport(new List<MemorySnapshot>());

        Assert.Contains("No snapshots available", report);
    }

    [Fact]
    public void GenerateMemoryReport_IncludesMemorySummary()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        var snapshots = new List<MemorySnapshot>
        {
            ecsProfiler.CaptureEcsSnapshot(),
            ecsProfiler.CaptureEcsSnapshot()
        };

        var report = EcsMemoryProfiler.GenerateMemoryReport(snapshots);

        Assert.Contains("ECS Memory Report", report);
        Assert.Contains("Memory Usage:", report);
        Assert.Contains("Min:", report);
        Assert.Contains("Max:", report);
        Assert.Contains("Avg:", report);
    }

    [Fact]
    public void GenerateMemoryReport_IncludesEntityCount()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        world.CreateEntity("Entity1");
        var snap1 = ecsProfiler.CaptureEcsSnapshot();

        world.CreateEntity("Entity2");
        world.CreateEntity("Entity3");
        var snap2 = ecsProfiler.CaptureEcsSnapshot();

        var report = EcsMemoryProfiler.GenerateMemoryReport(new List<MemorySnapshot> { snap1, snap2 });

        Assert.Contains("Entity Count:", report);
        Assert.Contains("Start: 1", report);
        Assert.Contains("End: 3", report);
        Assert.Contains("Delta: 2", report);
    }

    [Fact]
    public void GenerateMemoryReport_IncludesGCStatistics()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        var snap1 = ecsProfiler.CaptureEcsSnapshot();

        // Force some GC
#pragma warning disable S1215 // Needed for test validation
        GC.Collect(0);
#pragma warning restore S1215

        var snap2 = ecsProfiler.CaptureEcsSnapshot();

        var report = EcsMemoryProfiler.GenerateMemoryReport(new List<MemorySnapshot> { snap1, snap2 });

        Assert.Contains("GC Collections:", report);
        Assert.Contains("Gen0:", report);
        Assert.Contains("Gen1:", report);
        Assert.Contains("Gen2:", report);
    }

    [Fact]
    public void GenerateMemoryReport_DetectsNoLeak_WhenStable()
    {
        var world = new World();
        var ecsProfiler = new EcsMemoryProfiler(world);

        var snapshots = new List<MemorySnapshot>
        {
            ecsProfiler.CaptureEcsSnapshot(),
            ecsProfiler.CaptureEcsSnapshot()
        };

        var report = EcsMemoryProfiler.GenerateMemoryReport(snapshots);

        Assert.Contains("No significant memory leaks detected", report);
    }
}

/// <summary>
/// Tests for profiling exception handling and validation.
/// </summary>
public sealed class ProfilingExceptionTests
{
    [Fact]
    public void EcsMemoryProfiler_ThrowsException_WhenWorldIsNull()
    {
        var exception = Assert.Throws<InvalidProfilingOperationException>(() => new EcsMemoryProfiler(null!));

        Assert.Equal("PROF-400", exception.ErrorCode);
        Assert.Contains("World cannot be null", exception.Message);
        Assert.True(exception.Context.ContainsKey("parameterName"));
    }

    [Fact]
    public void MemoryProfiler_ThrowsException_WhenMaxSnapshotsIsZero()
    {
        var profiler = new MemoryProfiler();

        var exception = Assert.Throws<InvalidProfilingConfigurationException>(() => profiler.MaxSnapshots = 0);

        Assert.Equal("PROF-422", exception.ErrorCode);
        Assert.Equal("MaxSnapshots", exception.ParameterName);
        Assert.Contains("must be at least 1", exception.Message);
    }

    [Fact]
    public void MemoryProfiler_ThrowsException_WhenMaxSnapshotsIsNegative()
    {
        var profiler = new MemoryProfiler();

        var exception = Assert.Throws<InvalidProfilingConfigurationException>(() => profiler.MaxSnapshots = -5);

        Assert.Equal("PROF-422", exception.ErrorCode);
        Assert.Contains("-5", exception.Message);
    }

    [Fact]
    public void DetectMemoryLeak_ThrowsException_WhenSampleCountIsOne()
    {
        var profiler = new MemoryProfiler();
        profiler.CaptureSnapshot();
        profiler.CaptureSnapshot();

        var exception = Assert.Throws<InvalidProfilingConfigurationException>(
            () => profiler.DetectMemoryLeak(sampleCount: 1));

        Assert.Equal("PROF-422", exception.ErrorCode);
        Assert.Equal("sampleCount", exception.ParameterName);
        Assert.Contains("must be at least 2", exception.Message);
    }

    [Fact]
    public void DetectMemoryLeak_ThrowsException_WhenGrowthThresholdIsNegative()
    {
        var profiler = new MemoryProfiler();
        profiler.CaptureSnapshot();
        profiler.CaptureSnapshot();

        var exception = Assert.Throws<InvalidProfilingConfigurationException>(
            () => profiler.DetectMemoryLeak(growthThresholdMB: -1.0));

        Assert.Equal("PROF-422", exception.ErrorCode);
        Assert.Equal("growthThresholdMB", exception.ParameterName);
        Assert.Contains("must be positive", exception.Message);
    }

    [Fact]
    public void DetectMemoryLeak_ThrowsException_WhenGrowthThresholdIsZero()
    {
        var profiler = new MemoryProfiler();

        var exception = Assert.Throws<InvalidProfilingConfigurationException>(
            () => profiler.DetectMemoryLeak(growthThresholdMB: 0.0));

        Assert.Equal("PROF-422", exception.ErrorCode);
    }

    [Fact]
    public void GetSnapshotsInRange_ThrowsException_WhenEndTimeBeforeStartTime()
    {
        var profiler = new MemoryProfiler();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(-1);

        var exception = Assert.Throws<InvalidProfilingOperationException>(
            () => profiler.GetSnapshotsInRange(startTime, endTime));

        Assert.Equal("PROF-400", exception.ErrorCode);
        Assert.Contains("must be after start time", exception.Message);
        Assert.True(exception.Context.ContainsKey("startTime"));
        Assert.True(exception.Context.ContainsKey("endTime"));
    }
}
