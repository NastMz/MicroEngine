namespace MicroEngine.Core.Resources;

/// <summary>
/// File system-based implementation of IResourceWatcher.
/// Uses FileSystemWatcher to monitor resource file changes.
/// </summary>
public sealed class FileSystemResourceWatcher : IResourceWatcher
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
    private readonly HashSet<string> _watchedPaths = new();
    private bool _disposed;
    private bool _enabled = true;

    /// <inheritdoc/>
    public event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

    /// <inheritdoc/>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = _enabled;
            }
        }
    }

    /// <inheritdoc/>
    public int WatchedCount => _watchedPaths.Count;

    /// <inheritdoc/>
    public void Watch(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Cannot watch non-existent file: {path}", path);
        }

        var normalizedPath = Path.GetFullPath(path);

        if (_watchedPaths.Contains(normalizedPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(normalizedPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException($"Cannot determine directory for: {normalizedPath}");
        }

        var fileName = Path.GetFileName(normalizedPath);

        if (!_watchers.TryGetValue(directory, out var watcher))
        {
            watcher = new FileSystemWatcher(directory)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = false
            };

            watcher.Changed += OnFileChanged;
            watcher.Deleted += OnFileDeleted;
            watcher.Renamed += OnFileRenamed;

            watcher.EnableRaisingEvents = _enabled;

            _watchers[directory] = watcher;
        }

        _watchedPaths.Add(normalizedPath);
    }

    /// <inheritdoc/>
    public void Unwatch(string path)
    {
        if (_disposed)
        {
            return;
        }

        var normalizedPath = Path.GetFullPath(path);

        if (!_watchedPaths.Remove(normalizedPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(normalizedPath);
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        var stillWatchingDirectory = _watchedPaths.Any(p => Path.GetDirectoryName(p) == directory);
        if (!stillWatchingDirectory && _watchers.TryGetValue(directory, out var watcher))
        {
            watcher.Dispose();
            _watchers.Remove(directory);
        }
    }

    /// <inheritdoc/>
    public bool IsWatching(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        return _watchedPaths.Contains(normalizedPath);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var watcher in _watchers.Values)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
        _watchedPaths.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_enabled || !_watchedPaths.Contains(e.FullPath))
        {
            return;
        }

        ResourceChanged?.Invoke(this, new ResourceChangedEventArgs
        {
            Path = e.FullPath,
            ChangeType = ResourceChangeType.Modified
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!_enabled || !_watchedPaths.Contains(e.FullPath))
        {
            return;
        }

        ResourceChanged?.Invoke(this, new ResourceChangedEventArgs
        {
            Path = e.FullPath,
            ChangeType = ResourceChangeType.Deleted
        });
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!_enabled)
        {
            return;
        }

        if (_watchedPaths.Contains(e.OldFullPath))
        {
            _watchedPaths.Remove(e.OldFullPath);

            ResourceChanged?.Invoke(this, new ResourceChangedEventArgs
            {
                Path = e.OldFullPath,
                ChangeType = ResourceChangeType.Renamed
            });
        }
    }
}
