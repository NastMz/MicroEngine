using MicroEngine.Core.Logging;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Resources;

public class ResourceCacheTests
{
    private sealed class TestResource : IResource
    {
        private static uint _nextId = 1;

        public ResourceId Id { get; init; }
        public string Path { get; init; } = string.Empty;
        public bool IsLoaded { get; init; }
        public long SizeInBytes { get; init; }
        public bool IsDisposed { get; private set; }

        public static TestResource Create(string path)
        {
            return new TestResource
            {
                Id = new ResourceId(_nextId++),
                Path = path,
                IsLoaded = true,
                SizeInBytes = 1024
            };
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class TestResourceLoader : IResourceLoader<TestResource>
    {
        public int LoadCallCount { get; private set; }
        public int UnloadCallCount { get; private set; }
        public TestResource? LastLoaded { get; private set; }

        public IReadOnlyList<string> SupportedExtensions => new[] { ".txt", ".test" };

        public TestResource Load(string path)
        {
            LoadCallCount++;
            var resource = TestResource.Create(path);
            LastLoaded = resource;
            return resource;
        }

        public void Unload(TestResource resource)
        {
            UnloadCallCount++;
        }
    }

    [Fact]
    public void Load_NewResource_CreatesResource()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        var resource = cache.Load("test/resource.txt");

        Assert.NotNull(resource);
        Assert.EndsWith("test/resource.txt", resource.Path);
        Assert.True(resource.IsLoaded);
        Assert.Equal(1, loader.LoadCallCount);
        Assert.Equal(1, cache.CachedCount);
    }

    [Fact]
    public void Load_SamePathTwice_ReusesResource()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        var resource1 = cache.Load("test/resource.txt");
        var resource2 = cache.Load("test/resource.txt");

        Assert.Same(resource1, resource2);
        Assert.Equal(1, loader.LoadCallCount);
        Assert.Equal(1, cache.CachedCount);
    }

    [Fact]
    public void Unload_SingleReference_DisposesResource()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        var resource = cache.Load("test/resource.txt");
        cache.Unload("test/resource.txt");

        Assert.True(resource.IsDisposed);
        Assert.Equal(1, loader.UnloadCallCount);
        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void Unload_MultipleReferences_KeepsResourceAlive()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        var resource1 = cache.Load("test/resource.txt");
        _ = cache.Load("test/resource.txt");
        
        cache.Unload("test/resource.txt");

        Assert.False(resource1.IsDisposed);
        Assert.Equal(0, loader.UnloadCallCount);
        Assert.Equal(1, cache.CachedCount);
    }

    [Fact]
    public void Unload_NonExistentPath_DoesNotThrow()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        cache.Unload("nonexistent/path.txt");

        Assert.Equal(0, loader.UnloadCallCount);
    }

    [Fact]
    public void TotalMemoryUsage_SumsAllResources()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        cache.Load("test/resource1.txt");
        cache.Load("test/resource2.txt");
        cache.Load("test/resource3.txt");

        Assert.Equal(3072, cache.TotalMemoryUsage); // 3 * 1024
    }

    [Fact]
    public void Clear_DisposesAllResources()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        var resource1 = cache.Load("test/resource1.txt");
        var resource2 = cache.Load("test/resource2.txt");
        
        cache.Clear();

        Assert.True(resource1.IsDisposed);
        Assert.True(resource2.IsDisposed);
        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void Dispose_DisposesAllResources()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        var resource1 = cache.Load("test/resource1.txt");
        var resource2 = cache.Load("test/resource2.txt");
        
        cache.Dispose();

        Assert.True(resource1.IsDisposed);
        Assert.True(resource2.IsDisposed);
    }

    [Fact]
    public void PathNormalization_HandlesWindowsAndUnixPaths()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        var cache = new ResourceCache<TestResource>(loader, logger);

        var resource1 = cache.Load("test\\resource.txt");
        var resource2 = cache.Load("test/resource.txt");

        Assert.Same(resource1, resource2);
        Assert.Equal(1, loader.LoadCallCount);
    }
}
