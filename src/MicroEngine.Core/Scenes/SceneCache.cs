using System.Collections.Concurrent;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Thread-safe scene cache with LRU eviction policy.
/// Enables scene reuse and lazy loading to reduce memory allocation overhead.
/// </summary>
public sealed class SceneCache : ISceneCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly int _maxCacheSize;
    private readonly object _lock = new();

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
