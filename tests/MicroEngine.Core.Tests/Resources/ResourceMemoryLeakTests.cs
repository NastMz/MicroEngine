using MicroEngine.Core.Logging;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Resources;

/// <summary>
/// Tests for verifying resource system has no memory leaks.
/// Tests reference counting, proper disposal, and cache clearing.
/// </summary>
public sealed class ResourceMemoryLeakTests
{
    private sealed class DisposableTestResource : IResource
    {
        private static uint _nextId = 1;
        public static int DisposeCallCount { get; private set; }

        public ResourceId Id { get; init; }
        public string Path { get; init; } = string.Empty;
        public bool IsLoaded { get; init; }
        public long SizeInBytes { get; init; }
        public ResourceMetadata? Metadata { get; init; }
        public bool IsDisposed { get; private set; }

        public static DisposableTestResource Create(string path, long size = 1000)
        {
            return new DisposableTestResource
            {
                Id = new ResourceId(_nextId++),
                Path = path,
                IsLoaded = true,
                SizeInBytes = size,
                Metadata = null
            };
        }

        public static void ResetDisposeCount() => DisposeCallCount = 0;

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                DisposeCallCount++;
            }
        }
    }

    private sealed class DisposableResourceLoader : IResourceLoader<DisposableTestResource>
    {
        public IReadOnlyList<string> SupportedExtensions => new[] { ".test" };

        public DisposableTestResource Load(string path, ResourceMetadata? metadata = null)
        {
            return DisposableTestResource.Create(path);
        }

        public void Unload(DisposableTestResource resource)
        {
            resource.Dispose();
        }

        public ResourceValidationResult Validate(string path)
        {
            return ResourceValidationResult.Success();
        }
    }

    [Fact]
    public void Cache_WhenDisposed_DisposesAllResources()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        cache.Load("test1.test", validateFirst: false);
        cache.Load("test2.test", validateFirst: false);
        cache.Load("test3.test", validateFirst: false);

        Assert.Equal(3, cache.CachedCount);

        cache.Dispose();

        Assert.Equal(3, DisposableTestResource.DisposeCallCount);
        Assert.Equal(0, cache.CachedCount);
        Assert.Equal(0, cache.TotalMemoryUsage);
    }

    [Fact]
    public void Clear_DisposesAllCachedResources()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        cache.Load("test1.test", validateFirst: false);
        cache.Load("test2.test", validateFirst: false);

        Assert.Equal(2, cache.CachedCount);

        cache.Clear();

        Assert.Equal(2, DisposableTestResource.DisposeCallCount);
        Assert.Equal(0, cache.CachedCount);
        Assert.Equal(0, cache.TotalMemoryUsage);
    }

    [Fact]
    public void Unload_WhenRefCountZero_DisposesResource()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        cache.Load("test.test", validateFirst: false);
        cache.Unload("test.test");

        Assert.Equal(1, DisposableTestResource.DisposeCallCount);
        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void Unload_WhenRefCountNotZero_DoesNotDisposeResource()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        cache.Load("test.test", validateFirst: false);
        cache.Load("test.test", validateFirst: false);

        cache.Unload("test.test");

        Assert.Equal(0, DisposableTestResource.DisposeCallCount);
        Assert.Equal(1, cache.CachedCount);

        cache.Unload("test.test");

        Assert.Equal(1, DisposableTestResource.DisposeCallCount);
        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void LoadAndUnloadCycle_DoesNotLeakResources()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        for (int i = 0; i < 100; i++)
        {
            cache.Load($"test{i}.test", validateFirst: false);
        }

        Assert.Equal(100, cache.CachedCount);

        for (int i = 0; i < 100; i++)
        {
            cache.Unload($"test{i}.test");
        }

        Assert.Equal(100, DisposableTestResource.DisposeCallCount);
        Assert.Equal(0, cache.CachedCount);
        Assert.Equal(0, cache.TotalMemoryUsage);
    }

    [Fact]
    public void ReuseAndUnload_MaintainsCorrectRefCount()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        for (int i = 0; i < 10; i++)
        {
            cache.Load("shared.test", validateFirst: false);
        }

        Assert.Equal(1, cache.CachedCount);

        for (int i = 0; i < 9; i++)
        {
            cache.Unload("shared.test");
        }

        Assert.Equal(0, DisposableTestResource.DisposeCallCount);
        Assert.Equal(1, cache.CachedCount);

        cache.Unload("shared.test");

        Assert.Equal(1, DisposableTestResource.DisposeCallCount);
        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void MemoryUsage_TracksCorrectly()
    {
        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        cache.Load("test1.test", validateFirst: false);
        Assert.Equal(1000, cache.TotalMemoryUsage);

        cache.Load("test2.test", validateFirst: false);
        Assert.Equal(2000, cache.TotalMemoryUsage);

        cache.Unload("test1.test");
        Assert.Equal(1000, cache.TotalMemoryUsage);

        cache.Clear();
        Assert.Equal(0, cache.TotalMemoryUsage);
    }

    [Fact]
    public void ClearAfterPartialUnload_DisposesRemainingResources()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        using var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        cache.Load("test1.test", validateFirst: false);
        cache.Load("test2.test", validateFirst: false);
        cache.Load("test3.test", validateFirst: false);

        cache.Unload("test1.test");
        Assert.Equal(1, DisposableTestResource.DisposeCallCount);

        cache.Clear();

        Assert.Equal(3, DisposableTestResource.DisposeCallCount);
        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void MultipleDisposeCalls_DoNotDoubleDispose()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        var cache = new ResourceCache<DisposableTestResource>(loader, logger);

        cache.Load("test.test", validateFirst: false);

        cache.Dispose();
        cache.Dispose();
        cache.Dispose();

        Assert.Equal(1, DisposableTestResource.DisposeCallCount);
    }

    [Fact]
    public void HotReloadCache_DisposesResourcesCorrectly()
    {
        DisposableTestResource.ResetDisposeCount();

        var loader = new DisposableResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Error);
        var cache = new HotReloadableResourceCache<DisposableTestResource>(loader, logger);

        cache.Load("test1.test", enableHotReload: true, validateFirst: false);
        cache.Load("test2.test", enableHotReload: false, validateFirst: false);

        Assert.Equal(2, cache.CachedCount);

        cache.Dispose();

        Assert.Equal(2, DisposableTestResource.DisposeCallCount);
        Assert.Equal(0, cache.CachedCount);
    }
}
