using MicroEngine.Core.Logging;

namespace MicroEngine.Core.Resources;

/// <summary>
/// Resource cache that manages loading, unloading, and lifetime of resources.
/// Implements reference counting to share resources between multiple consumers.
/// </summary>
/// <typeparam name="T">Type of resource managed by this cache.</typeparam>
public sealed class ResourceCache<T> : IDisposable where T : IResource
{
    private const string LOG_CATEGORY = "Resources";

    private readonly IResourceLoader<T> _loader;
    private readonly ILogger _logger;
    private readonly Dictionary<string, CachedResource> _cache = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceCache{T}"/> class.
    /// </summary>
    public ResourceCache(IResourceLoader<T> loader, ILogger logger)
    {
        _loader = loader;
        _logger = logger;
    }

    /// <summary>
    /// Loads or retrieves a cached resource.
    /// Increments reference count if already loaded.
    /// </summary>
    public T Load(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(path);

        if (_cache.TryGetValue(normalizedPath, out var cached))
        {
            cached.RefCount++;
            _logger.Debug(LOG_CATEGORY, $"Reusing cached resource: {normalizedPath} (refs={cached.RefCount})");
            return cached.Resource;
        }

        _logger.Info(LOG_CATEGORY, $"Loading resource: {normalizedPath}");
        var resource = _loader.Load(normalizedPath);
        
        _cache[normalizedPath] = new CachedResource
        {
            Resource = resource,
            RefCount = 1
        };

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

        if (!_cache.TryGetValue(normalizedPath, out var cached))
        {
            _logger.Warn(LOG_CATEGORY, $"Attempted to unload non-cached resource: {normalizedPath}");
            return;
        }

        cached.RefCount--;

        if (cached.RefCount <= 0)
        {
            _logger.Info(LOG_CATEGORY, $"Unloading resource: {normalizedPath}");
            _loader.Unload(cached.Resource);
            cached.Resource.Dispose();
            _cache.Remove(normalizedPath);
        }
        else
        {
            _logger.Debug(LOG_CATEGORY, $"Resource still referenced: {normalizedPath} (refs={cached.RefCount})");
        }
    }

    /// <summary>
    /// Gets the number of currently cached resources.
    /// </summary>
    public int CachedCount => _cache.Count;

    /// <summary>
    /// Gets the total memory usage of cached resources in bytes (estimated).
    /// </summary>
    public long TotalMemoryUsage => _cache.Values.Sum(c => c.Resource.SizeInBytes);

    /// <summary>
    /// Clears all cached resources regardless of reference count.
    /// Use with caution - may leave dangling references.
    /// </summary>
    public void Clear()
    {
        var resources = _cache.Values.Select(c => c.Resource).ToList();
        
        foreach (var resource in resources)
        {
            _loader.Unload(resource);
            resource.Dispose();
        }

        _cache.Clear();
        _logger.Info(LOG_CATEGORY, "Resource cache cleared");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace('\\', '/');
    }

    private sealed class CachedResource
    {
        public required T Resource { get; init; }
        public int RefCount { get; set; }
    }
}
