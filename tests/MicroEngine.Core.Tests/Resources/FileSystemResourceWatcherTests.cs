using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Resources;

public sealed class FileSystemResourceWatcherTests : IDisposable
{
    private readonly string _testFilesDir;
    private readonly FileSystemResourceWatcher _watcher;

    public FileSystemResourceWatcherTests()
    {
        _testFilesDir = Path.Combine(Path.GetTempPath(), "MicroEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFilesDir);
        _watcher = new FileSystemResourceWatcher();
    }

    public void Dispose()
    {
        _watcher.Dispose();
        if (Directory.Exists(_testFilesDir))
        {
            Directory.Delete(_testFilesDir, true);
        }
    }

    [Fact]
    public void Watch_ValidFile_StartsWatching()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        _watcher.Watch(filePath);

        Assert.True(_watcher.IsWatching(filePath));
        Assert.Equal(1, _watcher.WatchedCount);
    }

    [Fact]
    public void Watch_NonExistentFile_ThrowsFileNotFoundException()
    {
        var filePath = Path.Combine(_testFilesDir, "nonexistent.txt");

        Assert.Throws<FileNotFoundException>(() => _watcher.Watch(filePath));
    }

    [Fact]
    public void Watch_NullPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _watcher.Watch(null!));
    }

    [Fact]
    public void Watch_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _watcher.Watch(""));
    }

    [Fact]
    public void Watch_SameFileTwice_DoesNotDuplicate()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        _watcher.Watch(filePath);
        _watcher.Watch(filePath);

        Assert.Equal(1, _watcher.WatchedCount);
    }

    [Fact]
    public void Unwatch_WatchedFile_StopsWatching()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        _watcher.Watch(filePath);
        _watcher.Unwatch(filePath);

        Assert.False(_watcher.IsWatching(filePath));
        Assert.Equal(0, _watcher.WatchedCount);
    }

    [Fact]
    public void Unwatch_UnwatchedFile_DoesNotThrow()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");

        _watcher.Unwatch(filePath);

        Assert.Equal(0, _watcher.WatchedCount);
    }

    [Fact]
    public void Enabled_SetToFalse_DisablesWatcher()
    {
        _watcher.Enabled = false;

        Assert.False(_watcher.Enabled);
    }

    [Fact]
    public void FileChange_RaisesResourceChangedEvent()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "original");

        ResourceChangedEventArgs? eventArgs = null;
        _watcher.ResourceChanged += (sender, e) => eventArgs = e;

        _watcher.Watch(filePath);

        Thread.Sleep(100);
        File.WriteAllText(filePath, "modified");

        Thread.Sleep(500);

        Assert.NotNull(eventArgs);
        Assert.Equal(filePath, eventArgs.Path);
        Assert.Equal(ResourceChangeType.Modified, eventArgs.ChangeType);
    }

    [Fact]
    public void FileDelete_RaisesResourceChangedEvent()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        ResourceChangedEventArgs? eventArgs = null;
        _watcher.ResourceChanged += (sender, e) => eventArgs = e;

        _watcher.Watch(filePath);

        Thread.Sleep(100);
        File.Delete(filePath);

        Thread.Sleep(500);

        Assert.NotNull(eventArgs);
        Assert.Equal(filePath, eventArgs.Path);
        Assert.Equal(ResourceChangeType.Deleted, eventArgs.ChangeType);
    }

    [Fact]
    public void Disabled_DoesNotRaiseEvents()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "original");

        ResourceChangedEventArgs? eventArgs = null;
        _watcher.ResourceChanged += (sender, e) => eventArgs = e;

        _watcher.Watch(filePath);
        _watcher.Enabled = false;

        Thread.Sleep(100);
        File.WriteAllText(filePath, "modified");

        Thread.Sleep(500);

        Assert.Null(eventArgs);
    }

    [Fact]
    public void MultipleFiles_InSameDirectory_SharesWatcher()
    {
        var file1 = Path.Combine(_testFilesDir, "file1.txt");
        var file2 = Path.Combine(_testFilesDir, "file2.txt");
        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        _watcher.Watch(file1);
        _watcher.Watch(file2);

        Assert.Equal(2, _watcher.WatchedCount);
        Assert.True(_watcher.IsWatching(file1));
        Assert.True(_watcher.IsWatching(file2));
    }

    [Fact]
    public void Dispose_StopsAllWatching()
    {
        var filePath = Path.Combine(_testFilesDir, "test.txt");
        File.WriteAllText(filePath, "content");

        _watcher.Watch(filePath);
        _watcher.Dispose();

        Assert.Equal(0, _watcher.WatchedCount);
    }
}
