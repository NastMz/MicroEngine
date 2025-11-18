# Performance Profiling Guide

Guide for profiling and measuring performance in MicroEngine.

---

## Available Metrics

### Frame Timing

The engine tracks frame times automatically:

```csharp
// In your update loop
var frameTime = gameEngine.Time.DeltaTime; // Time since last frame (seconds)
var fps = 1.0f / frameTime; // Frames per second
```

### ECS Performance

Track entity and system performance:

```csharp
// Entity count
var entityCount = world.EntityCount;

// System count
var systemCount = world.SystemCount;
```

### Resource System Metrics

Monitor resource cache performance:

```csharp
var cachedCount = cache.CachedCount;
var memoryUsage = cache.TotalMemoryUsage;
var memoryMB = memoryUsage / (1024.0 * 1024.0);

Console.WriteLine($"Resources: {cachedCount}, Memory: {memoryMB:F2} MB");
```

---

## Profiling Techniques

### 1. Manual Timing

Use `Stopwatch` for custom measurements:

```csharp
using System.Diagnostics;

var sw = Stopwatch.StartNew();

// Code to measure
PerformExpensiveOperation();

sw.Stop();
Console.WriteLine($"Operation took {sw.ElapsedMilliseconds} ms");
```

### 2. Scene Performance

Track update and render times separately:

```csharp
public override void OnUpdate(float deltaTime)
{
    var updateStart = Stopwatch.GetTimestamp();

    // Update logic
    base.OnUpdate(deltaTime);

    var updateTime = (Stopwatch.GetTimestamp() - updateStart) / (double)Stopwatch.Frequency * 1000;
    // Log or display updateTime
}
```

### 3. System Benchmarks

Measure individual ECS system performance:

```csharp
foreach (var system in systems)
{
    var sw = Stopwatch.StartNew();
    system.Update(world, deltaTime);
    sw.Stop();

    Console.WriteLine($"{system.GetType().Name}: {sw.ElapsedMilliseconds} ms");
}
```

---

## Performance Targets

### Frame Budget (60 FPS)

Target: 16.67ms per frame

-   **Update:** ≤ 10ms
-   **Render:** ≤ 6ms
-   **Buffer:** ~0.67ms

### ECS Scalability

Expected performance with default systems:

| Entities | Update Time | Target FPS |
| -------- | ----------- | ---------- |
| 100      | < 1ms       | 1000+      |
| 1,000    | < 5ms       | 200+       |
| 10,000   | < 15ms      | 60+        |

### Resource Loading

Acceptable load times:

-   **Texture (1MB PNG):** < 10ms
-   **Font (500KB TTF):** < 15ms
-   **Audio (5MB WAV):** < 30ms

---

## Optimization Guidelines

### 1. Reduce Allocations

```csharp
// Bad: Allocates every frame
void Update()
{
    var items = new List<Item>(); // Allocation!
    foreach (var entity in entities)
    {
        items.Add(ProcessEntity(entity));
    }
}

// Good: Reuse collection
private readonly List<Item> _itemCache = new();

void Update()
{
    _itemCache.Clear();
    foreach (var entity in entities)
    {
        _itemCache.Add(ProcessEntity(entity));
    }
}
```

### 2. Batch Operations

```csharp
// Bad: Multiple render calls
foreach (var sprite in sprites)
{
    renderer.DrawSprite(sprite);
}

// Good: Batch draw
var batch = new SpriteBatch();
batch.Begin();
foreach (var sprite in sprites)
{
    batch.Draw(sprite);
}
batch.End();
```

### 3. Cache Expensive Computations

```csharp
// Bad: Recalculate every frame
void Update()
{
    var distance = Vector2.Distance(playerPos, enemyPos);
    var normalized = Vector2.Normalize(direction);
}

// Good: Cache when data changes
private float _cachedDistance;
private Vector2 _cachedNormalized;
private bool _isDirty = true;

void OnPositionChanged()
{
    _isDirty = true;
}

void Update()
{
    if (_isDirty)
    {
        _cachedDistance = Vector2.Distance(playerPos, enemyPos);
        _cachedNormalized = Vector2.Normalize(direction);
        _isDirty = false;
    }
}
```

---

## Debug Overlays

Display performance metrics on screen:

```csharp
public class DebugOverlay
{
    private readonly Queue<float> _frameTimes = new(60);

    public void Update(float deltaTime)
    {
        _frameTimes.Enqueue(deltaTime);
        if (_frameTimes.Count > 60)
        {
            _frameTimes.Dequeue();
        }
    }

    public void Render(IRenderBackend renderer)
    {
        var avgFrameTime = _frameTimes.Average();
        var fps = 1.0f / avgFrameTime;

        renderer.DrawText(
            $"FPS: {fps:F1}\n" +
            $"Frame: {avgFrameTime * 1000:F2}ms\n" +
            $"Entities: {entityCount}",
            new Vector2(10, 10),
            20,
            Color.Green
        );
    }
}
```

---

## Memory Profiling

### Track Allocations

Monitor GC collections:

```csharp
var gen0 = GC.CollectionCount(0);
var gen1 = GC.CollectionCount(1);
var gen2 = GC.CollectionCount(2);

Console.WriteLine($"GC Gen0: {gen0}, Gen1: {gen1}, Gen2: {gen2}");
```

### Memory Usage

```csharp
var totalMemory = GC.GetTotalMemory(forceFullCollection: false);
var memoryMB = totalMemory / (1024.0 * 1024.0);
Console.WriteLine($"Memory: {memoryMB:F2} MB");
```

---

## External Profilers

### Visual Studio Profiler

1. Debug → Performance Profiler
2. Select "CPU Usage" and ".NET Object Allocation"
3. Start profiling
4. Run game for 30-60 seconds
5. Stop and analyze hotspots

### dotTrace (JetBrains)

1. Run → Profile
2. Select "Timeline" or "Sampling"
3. Analyze call trees and allocations

### PerfView (Microsoft)

For advanced low-level profiling:

```bash
perfview collect -MaxCollectSec:30 -NoGui
```

---

## Continuous Monitoring

Log key metrics periodically:

```csharp
private float _logTimer = 0;

void Update(float deltaTime)
{
    _logTimer += deltaTime;

    if (_logTimer >= 5.0f) // Log every 5 seconds
    {
        logger.Info($"FPS: {fps:F1}, Entities: {entityCount}, Memory: {memoryMB:F2} MB");
        _logTimer = 0;
    }
}
```

---

## Summary

**Key Metrics to Monitor:**

-   Frame time (target: < 16.67ms)
-   Entity count (manageable up to 10,000+)
-   Memory usage (watch for leaks)
-   GC collections (minimize Gen1/Gen2)
-   Resource cache size

**Optimization Priorities:**

1. Reduce per-frame allocations
2. Batch draw calls
3. Cache expensive calculations
4. Preload resources
5. Profile before optimizing

For resource-specific performance, see [RESOURCE_USAGE_GUIDE.md](MODULES/RESOURCE_USAGE_GUIDE.md).
