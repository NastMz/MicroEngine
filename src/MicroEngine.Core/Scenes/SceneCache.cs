using System.Collections.Concurrent;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Thread-safe scene cache with LRU eviction policy.
/// Enables scene reuse and lazy loading to reduce memory allocation overhead.
/// </summary>
public sealed class SceneCache : ISceneCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, Task> _preloadTasks = new();
    private readonly int _maxCacheSize;
    private readonly object _lock = new();

    /// <inheritdoc />
    public event EventHandler<ScenePreloadedEventArgs>? ScenePreloaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneCache"/> class.
    /// </summary>
    /// <param name="maxCacheSize">Maximum number of scenes to cache. Default is 10.</param>
    /// <exception cref="ArgumentException">Thrown when maxCacheSize is less than 1.</exception>
    public SceneCache(int maxCacheSize = 10)
    {
        if (maxCacheSize < 1)
        {
            throw new ArgumentException("Max cache size must be at least 1.", nameof(maxCacheSize));
        }

        _maxCacheSize = maxCacheSize;
    }

    /// <inheritdoc />
    public int Count => _cache.Count;

    /// <inheritdoc />
    public int MaxCacheSize => _maxCacheSize;

    /// <inheritdoc />
    public T GetOrCreate<T>(string sceneKey, Func<T> factory) where T : IScene
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            throw new ArgumentException("Scene key cannot be null or whitespace.", nameof(sceneKey));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        lock (_lock)
        {
            if (_cache.TryGetValue(sceneKey, out var entry))
            {
                // Update last access time (LRU tracking)
                entry.UpdateAccess();

                if (entry.Scene is T typedScene)
                {
                    return typedScene;
                }

                throw new InvalidCastException(
                    $"Cached scene '{sceneKey}' is of type '{entry.Scene.GetType().Name}' " +
                    $"but requested type is '{typeof(T).Name}'.");
            }

            // Evict if cache is full
            if (_cache.Count >= _maxCacheSize)
            {
                EvictLeastRecentlyUsed();
            }

            // Create new scene
            var newScene = factory();
            if (newScene == null)
            {
                throw new InvalidOperationException($"Factory returned null for scene key '{sceneKey}'.");
            }

            var newEntry = new CacheEntry(newScene);
            _cache[sceneKey] = newEntry;

            return newScene;
        }
    }

    /// <inheritdoc />
    public bool Contains(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            return false;
        }

        return _cache.ContainsKey(sceneKey);
    }

    /// <inheritdoc />
    public bool Remove(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            return false;
        }

        lock (_lock)
        {
            if (_cache.TryRemove(sceneKey, out var entry))
            {
                entry.Scene.OnUnload();
                return true;
            }

            return false;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            foreach (var entry in _cache.Values)
            {
                entry.Scene.OnUnload();
            }

            _cache.Clear();
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetCachedKeys()
    {
        return _cache.Keys.ToList();
    }

    /// <inheritdoc />
    public void Preload<T>(string sceneKey, Func<T> factory) where T : IScene
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            throw new ArgumentException("Scene key cannot be null or whitespace.", nameof(sceneKey));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        lock (_lock)
        {
            // Only preload if not already cached
            if (_cache.ContainsKey(sceneKey))
            {
                return;
            }

            // Evict if cache is full
            if (_cache.Count >= _maxCacheSize)
            {
                EvictLeastRecentlyUsed();
            }

            // Create and cache scene
            var scene = factory();
            if (scene == null)
            {
                throw new InvalidOperationException($"Factory returned null for scene key '{sceneKey}'.");
            }

            var entry = new CacheEntry(scene);
            _cache[sceneKey] = entry;
        }
    }

    /// <inheritdoc />
    public bool TryGet<T>(string sceneKey, out T? scene) where T : class, IScene
    {
        scene = null;

        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            return false;
        }

        lock (_lock)
        {
            if (_cache.TryGetValue(sceneKey, out var entry))
            {
                entry.UpdateAccess();

                if (entry.Scene is T typedScene)
                {
                    scene = typedScene;
                    return true;
                }

                return false;
            }

            return false;
        }
    }

    private void EvictLeastRecentlyUsed()
    {
        // Must be called within lock
        if (_cache.IsEmpty)
        {
            return;
        }

        // Find least recently used entry
        var lruKey = _cache
            .OrderBy(kvp => kvp.Value.LastAccessTime)
            .First()
            .Key;

        if (_cache.TryRemove(lruKey, out var entry))
        {
            entry.Scene.OnUnload();
        }
    }

    /// <inheritdoc />
    public async Task PreloadAsync<T>(string sceneKey, Func<T> factory, CancellationToken cancellationToken = default) where T : IScene
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            throw new ArgumentException("Scene key cannot be null or whitespace.", nameof(sceneKey));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        // Check if already cached or preloading
        if (Contains(sceneKey) || IsPreloading(sceneKey))
        {
            return;
        }

        var preloadTask = Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Preload using synchronous Preload method
                Preload(sceneKey, factory);

                // Raise success event
                OnScenePreloaded(sceneKey, true, null);
            }
            catch (OperationCanceledException)
            {
                // Remove from preload tracking if cancelled
                _preloadTasks.TryRemove(sceneKey, out _);
                throw;
            }
            catch (Exception ex)
            {
                // Raise failure event
                OnScenePreloaded(sceneKey, false, ex);
                throw;
            }
            finally
            {
                // Remove from preload tracking when complete
                _preloadTasks.TryRemove(sceneKey, out _);
            }
        }, cancellationToken);

        // Track preload task
        _preloadTasks[sceneKey] = preloadTask;

        await preloadTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PreloadMultipleAsync(IEnumerable<(string sceneKey, Func<IScene> factory)> preloadRequests, CancellationToken cancellationToken = default)
    {
        if (preloadRequests == null)
        {
            throw new ArgumentNullException(nameof(preloadRequests));
        }

        var tasks = preloadRequests
            .Select(request => PreloadAsync(request.sceneKey, request.factory, cancellationToken))
            .ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool IsPreloading(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            return false;
        }

        return _preloadTasks.TryGetValue(sceneKey, out var task) && !task.IsCompleted;
    }

    private void OnScenePreloaded(string sceneKey, bool success, Exception? exception)
    {
        ScenePreloaded?.Invoke(this, new ScenePreloadedEventArgs(sceneKey, success, exception));
    }

    /// <summary>
    /// Internal cache entry with LRU tracking.
    /// </summary>
    private sealed class CacheEntry
    {
        public IScene Scene { get; }
        public DateTime LastAccessTime { get; private set; }

        public CacheEntry(IScene scene)
        {
            Scene = scene;
            LastAccessTime = DateTime.UtcNow;
        }

        public void UpdateAccess()
        {
            LastAccessTime = DateTime.UtcNow;
        }
    }
}
