namespace MicroEngine.Core.Scenes;

/// <summary>
/// Defines a cache for scene instances to enable scene reuse and lazy loading.
/// Reduces memory allocation and scene initialization overhead.
/// </summary>
public interface ISceneCache
{
    /// <summary>
    /// Gets the total number of cached scenes.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the maximum number of scenes that can be cached.
    /// </summary>
    int MaxCacheSize { get; }

    /// <summary>
    /// Gets or creates a scene instance using a factory function.
    /// If the scene is already cached, returns the cached instance.
    /// Otherwise, creates a new instance using the factory and caches it.
    /// </summary>
    /// <typeparam name="T">The type of scene to get or create.</typeparam>
    /// <param name="sceneKey">Unique key identifying the scene.</param>
    /// <param name="factory">Factory function to create the scene if not cached.</param>
    /// <returns>The cached or newly created scene instance.</returns>
    T GetOrCreate<T>(string sceneKey, Func<T> factory) where T : IScene;

    /// <summary>
    /// Checks if a scene with the given key is currently cached.
    /// </summary>
    /// <param name="sceneKey">Unique key identifying the scene.</param>
    /// <returns>True if the scene is cached; otherwise, false.</returns>
    bool Contains(string sceneKey);

    /// <summary>
    /// Removes a scene from the cache and calls its OnUnload method.
    /// </summary>
    /// <param name="sceneKey">Unique key identifying the scene.</param>
    /// <returns>True if the scene was removed; false if it was not in the cache.</returns>
    bool Remove(string sceneKey);

    /// <summary>
    /// Clears all cached scenes and calls OnUnload on each.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all currently cached scene keys.
    /// </summary>
    /// <returns>Collection of cached scene keys.</returns>
    IEnumerable<string> GetCachedKeys();

    /// <summary>
    /// Preloads a scene into the cache without activating it.
    /// Useful for background loading to reduce scene transition latency.
    /// </summary>
    /// <typeparam name="T">The type of scene to preload.</typeparam>
    /// <param name="sceneKey">Unique key identifying the scene.</param>
    /// <param name="factory">Factory function to create the scene.</param>
    void Preload<T>(string sceneKey, Func<T> factory) where T : IScene;

    /// <summary>
    /// Attempts to retrieve a cached scene without creating a new one.
    /// </summary>
    /// <typeparam name="T">The type of scene to retrieve.</typeparam>
    /// <param name="sceneKey">Unique key identifying the scene.</param>
    /// <param name="scene">The cached scene if found; otherwise, null.</param>
    /// <returns>True if the scene was found in cache; otherwise, false.</returns>
    bool TryGet<T>(string sceneKey, out T? scene) where T : class, IScene;

    /// <summary>
    /// Asynchronously preloads a scene into the cache in the background.
    /// Useful for loading scenes while the user is in another scene.
    /// </summary>
    /// <typeparam name="T">The type of scene to preload.</typeparam>
    /// <param name="sceneKey">Unique key identifying the scene.</param>
    /// <param name="factory">Factory function to create the scene.</param>
    /// <param name="cancellationToken">Token to cancel the preload operation.</param>
    /// <returns>A task that completes when the scene is preloaded.</returns>
    Task PreloadAsync<T>(string sceneKey, Func<T> factory, CancellationToken cancellationToken = default) where T : IScene;

    /// <summary>
    /// Asynchronously preloads multiple scenes into the cache in parallel.
    /// </summary>
    /// <param name="preloadRequests">Collection of scene keys and factories to preload.</param>
    /// <param name="cancellationToken">Token to cancel all preload operations.</param>
    /// <returns>A task that completes when all scenes are preloaded.</returns>
    Task PreloadMultipleAsync(IEnumerable<(string sceneKey, Func<IScene> factory)> preloadRequests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a scene is currently being preloaded in the background.
    /// </summary>
    /// <param name="sceneKey">Unique key identifying the scene.</param>
    /// <returns>True if the scene is currently being preloaded; otherwise, false.</returns>
    bool IsPreloading(string sceneKey);

    /// <summary>
    /// Event raised when a scene completes preloading.
    /// </summary>
    event EventHandler<ScenePreloadedEventArgs>? ScenePreloaded;
}

/// <summary>
/// Event arguments for scene preload completion.
/// </summary>
public sealed class ScenePreloadedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the key of the scene that was preloaded.
    /// </summary>
    public string SceneKey { get; }

    /// <summary>
    /// Gets a value indicating whether the preload was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the exception that occurred during preloading, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenePreloadedEventArgs"/> class.
    /// </summary>
    public ScenePreloadedEventArgs(string sceneKey, bool success, Exception? exception = null)
    {
        SceneKey = sceneKey;
        Success = success;
        Exception = exception;
    }
}
