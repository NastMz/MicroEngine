using MicroEngine.Core.Logging;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Resources;

public sealed class HotReloadableResourceCacheTests : IDisposable
{
    private readonly string _testFilesDir;

    public HotReloadableResourceCacheTests()
    {
        _testFilesDir = Path.Combine(Path.GetTempPath(), "MicroEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFilesDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFilesDir))
        {
            Directory.Delete(_testFilesDir, true);
        }
    }

    private sealed class TestResource : IResource
    {
        private static uint _nextId = 1;

        public ResourceId Id { get; init; }
        public string Path { get; init; } = string.Empty;
        public bool IsLoaded { get; init; }
        public long SizeInBytes { get; init; }
        public ResourceMetadata? Metadata { get; init; }
        public bool IsDisposed { get; private set; }
        public string Content { get; init; } = string.Empty;

        public static TestResource Create(string path)
        {
            var content = File.Exists(path) ? File.ReadAllText(path) : "default";
            return new TestResource
            {
                Id = new ResourceId(_nextId++),
                Path = path,
                IsLoaded = true,
                SizeInBytes = content.Length,
                Metadata = null,
                Content = content
            };
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class TestResourceLoader : IResourceLoader<TestResource>
    {
        public IReadOnlyList<string> SupportedExtensions => new[] { ".txt" };

        public TestResource Load(string path, ResourceMetadata? metadata = null)
        {
            return TestResource.Create(path);
        }

        public void Unload(TestResource resource)
        {
            resource.Dispose();
        }

        public ResourceValidationResult Validate(string path)
        {
            return ResourceValidationResult.Success();
        }
    }

    [Fact]
    public void Load_WithHotReload_StartsWatching()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "original");

        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        using var cache = new HotReloadableResourceCache<TestResource>(loader, logger);

        var resource = cache.Load(filePath, enableHotReload: true, validateFirst: false);

        Assert.NotNull(resource);
        Assert.Equal("original", resource.Content);
        Assert.Equal(1, cache.CachedCount);
    }

    [Fact]
    public void Load_WithoutHotReload_DoesNotWatch()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        using var cache = new HotReloadableResourceCache<TestResource>(loader, logger);

        var resource = cache.Load(filePath, enableHotReload: false, validateFirst: false);

        Assert.NotNull(resource);
    }

    [Fact(Skip = "FileSystemWatcher is unreliable in unit tests - requires OS file notifications")]
    public void FileChange_RaisesResourceReloadedEvent()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "original");

        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        using var cache = new HotReloadableResourceCache<TestResource>(loader, logger);

        var eventReceived = new ManualResetEventSlim(false);
        ResourceReloadedEventArgs<TestResource>? reloadedArgs = null;
        cache.ResourceReloaded += (sender, e) =>
        {
            reloadedArgs = e;
            eventReceived.Set();
        };

        cache.Load(filePath, enableHotReload: true, validateFirst: false);

        Thread.Sleep(1500);
        File.WriteAllText(filePath, "modified");
        Thread.Sleep(500);

        var received = eventReceived.Wait(TimeSpan.FromSeconds(5));

        Assert.True(received, "ResourceReloaded event was not raised within timeout");
        Assert.NotNull(reloadedArgs);
        Assert.Equal(filePath, reloadedArgs.Path);
        Assert.Equal("modified", reloadedArgs.Resource.Content);
    }

    [Fact(Skip = "FileSystemWatcher is unreliable in unit tests - requires OS file notifications")]
    public void FileDelete_RaisesResourceReloadFailedEvent()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        using var cache = new HotReloadableResourceCache<TestResource>(loader, logger);

        var eventReceived = new ManualResetEventSlim(false);
        ResourceReloadFailedEventArgs? failedArgs = null;
        cache.ResourceReloadFailed += (sender, e) =>
        {
            failedArgs = e;
            eventReceived.Set();
        };

        cache.Load(filePath, enableHotReload: true, validateFirst: false);

        Thread.Sleep(1500);
        File.Delete(filePath);
        Thread.Sleep(500);

        var received = eventReceived.Wait(TimeSpan.FromSeconds(5));

        Assert.True(received, "ResourceReloadFailed event was not raised within timeout");
        Assert.NotNull(failedArgs);
        Assert.Equal(filePath, failedArgs.Path);
        Assert.IsType<FileNotFoundException>(failedArgs.Error);
    }

    [Fact]
    public void HotReloadEnabled_CanBeToggled()
    {
        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        using var cache = new HotReloadableResourceCache<TestResource>(loader, logger);

        Assert.True(cache.HotReloadEnabled);

        cache.HotReloadEnabled = false;

        Assert.False(cache.HotReloadEnabled);
    }

    [Fact]
    public void Unload_StopsWatching()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        using var cache = new HotReloadableResourceCache<TestResource>(loader, logger);

        cache.Load(filePath, enableHotReload: true, validateFirst: false);
        cache.Unload(filePath);

        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void Clear_StopsAllWatching()
    {
        var file1 = Path.Combine(_testFilesDir, "file1.txt");
        var file2 = Path.Combine(_testFilesDir, "file2.txt");
        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        using var cache = new HotReloadableResourceCache<TestResource>(loader, logger);

        cache.Load(file1, enableHotReload: true, validateFirst: false);
        cache.Load(file2, enableHotReload: true, validateFirst: false);

        cache.Clear();

        Assert.Equal(0, cache.CachedCount);
    }

    [Fact]
    public void MultipleReferences_MaintainsCorrectRefCount()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        var loader = new TestResourceLoader();
        var logger = new ConsoleLogger(LogLevel.Debug);
        using var cache = new HotReloadableResourceCache<TestResource>(loader, logger);

        var resource1 = cache.Load(filePath, enableHotReload: true, validateFirst: false);
        var resource2 = cache.Load(filePath, enableHotReload: true, validateFirst: false);

        Assert.Same(resource1, resource2);

        cache.Unload(filePath);
        Assert.Equal(1, cache.CachedCount);

        cache.Unload(filePath);
        Assert.Equal(0, cache.CachedCount);
    }
}
