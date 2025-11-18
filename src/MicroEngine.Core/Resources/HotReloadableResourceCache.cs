using MicroEngine.Core.Logging;

namespace MicroEngine.Core.Resources;

/// <summary>
/// Resource cache with hot-reloading support.
/// Automatically reloads resources when files change on disk.
/// </summary>
/// <typeparam name="T">Type of resource managed by this cache.</typeparam>
public sealed class HotReloadableResourceCache<T> : IDisposable where T : IResource
{
    private const string LOG_CATEGORY = "Resources";

    private readonly ResourceCache<T> _cache;
    private readonly IResourceWatcher _watcher;
    private readonly ILogger _logger;
    private readonly Dictionary<string, int> _refCounts = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when a resource is successfully reloaded.
    /// </summary>
    public event EventHandler<ResourceReloadedEventArgs<T>>? ResourceReloaded;

    /// <summary>
    /// Event raised when a resource reload fails.
    /// </summary>
    public event EventHandler<ResourceReloadFailedEventArgs>? ResourceReloadFailed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HotReloadableResourceCache{T}"/> class.
    /// </summary>
    public HotReloadableResourceCache(
        IResourceLoader<T> loader,
        ILogger logger,
        IResourceWatcher? watcher = null)
    {
        _cache = new ResourceCache<T>(loader, logger);
        _logger = logger;
        _watcher = watcher ?? new FileSystemResourceWatcher();
        _watcher.ResourceChanged += OnResourceChanged;
    }

    /// <summary>
    /// Gets whether hot-reloading is enabled.
    /// </summary>
    public bool HotReloadEnabled
    {
        get => _watcher.Enabled;
        set => _watcher.Enabled = value;
    }

    /// <summary>
    /// Gets the number of currently cached resources.
    /// </summary>
    public int CachedCount => _cache.CachedCount;

    /// <summary>
    /// Gets the total memory usage of cached resources in bytes.
    /// </summary>
    public long TotalMemoryUsage => _cache.TotalMemoryUsage;

    /// <summary>
    /// Loads or retrieves a cached resource with hot-reload support.
    /// </summary>
    /// <param name="path">File path to the resource.</param>
    /// <param name="enableHotReload">Whether to watch this resource for changes. Default is true.</param>
    /// <param name="validateFirst">Whether to validate the file before loading. Default is true.</param>
    /// <returns>Loaded resource instance.</returns>
    public T Load(string path, bool enableHotReload = true, bool validateFirst = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var resource = _cache.Load(path, validateFirst);

        if (enableHotReload && !_watcher.IsWatching(resource.Path))
        {
            try
            {
                _watcher.Watch(resource.Path);
                _logger.Debug(LOG_CATEGORY, $"Hot-reload enabled for: {resource.Path}");
            }
            catch (Exception ex)
            {
                _logger.Warn(LOG_CATEGORY, $"Failed to enable hot-reload for {resource.Path}: {ex.Message}");
            }
        }

        _refCounts.TryGetValue(resource.Path, out var count);
        _refCounts[resource.Path] = count + 1;

        return resource;
    }

    /// <summary>
    /// Decrements reference count and unloads resource if no longer referenced.
    /// </summary>
    public void Unload(string path)
    {
        if (_disposed)
        {
            return;
        }

        var normalizedPath = NormalizePath(path);

        if (!_refCounts.TryGetValue(normalizedPath, out var count))
        {
            return;
        }

        count--;

        if (count <= 0)
        {
            _refCounts.Remove(normalizedPath);
            _watcher.Unwatch(normalizedPath);
            _cache.Unload(normalizedPath);
        }
        else
        {
            _refCounts[normalizedPath] = count;
            _cache.Unload(normalizedPath);
        }
    }

    /// <summary>
    /// Clears all cached resources and stops watching.
    /// </summary>
    public void Clear()
    {
        foreach (var path in _refCounts.Keys.ToList())
        {
            _watcher.Unwatch(path);
        }

        _refCounts.Clear();
        _cache.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Clear();
        _watcher.Dispose();
        _cache.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnResourceChanged(object? sender, ResourceChangedEventArgs e)
    {
        if (_disposed || !_refCounts.ContainsKey(e.Path))
        {
            return;
        }

        _logger.Info(LOG_CATEGORY, $"Resource changed ({e.ChangeType}): {e.Path}");

        if (e.ChangeType == ResourceChangeType.Deleted)
        {
            ResourceReloadFailed?.Invoke(this, new ResourceReloadFailedEventArgs
            {
                Path = e.Path,
                Error = new FileNotFoundException($"Resource file was deleted: {e.Path}")
            });
            return;
        }

        if (e.ChangeType == ResourceChangeType.Renamed)
        {
            ResourceReloadFailed?.Invoke(this, new ResourceReloadFailedEventArgs
            {
                Path = e.Path,
                Error = new InvalidOperationException($"Resource file was renamed: {e.Path}")
            });
            return;
        }

        try
        {
            var refCount = _refCounts[e.Path];
            
            for (int i = 0; i < refCount; i++)
            {
                _cache.Unload(e.Path);
            }

            var newResource = _cache.Load(e.Path, validateFirst: false);

            for (int i = 1; i < refCount; i++)
            {
                _cache.Load(e.Path, validateFirst: false);
            }

            _logger.Info(LOG_CATEGORY, $"Resource reloaded successfully: {e.Path}");

            ResourceReloaded?.Invoke(this, new ResourceReloadedEventArgs<T>
            {
                Path = e.Path,
                Resource = newResource,
                Timestamp = e.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.Error(LOG_CATEGORY, $"Failed to reload resource {e.Path}: {ex.Message}");

            ResourceReloadFailed?.Invoke(this, new ResourceReloadFailedEventArgs
            {
                Path = e.Path,
                Error = ex
            });
        }
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace('\\', '/');
    }
}

/// <summary>
/// Event arguments for successful resource reload.
/// </summary>
public sealed class ResourceReloadedEventArgs<T> : EventArgs where T : IResource
{
    /// <summary>
    /// Gets the path to the reloaded resource.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the newly loaded resource instance.
    /// </summary>
    public required T Resource { get; init; }

    /// <summary>
    /// Gets when the reload occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for failed resource reload.
/// </summary>
public sealed class ResourceReloadFailedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the resource that failed to reload.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the error that occurred during reload.
    /// </summary>
    public required Exception Error { get; init; }

    /// <summary>
    /// Gets when the failure occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
