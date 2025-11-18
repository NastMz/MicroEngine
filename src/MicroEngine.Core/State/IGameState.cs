namespace MicroEngine.Core.State;

/// <summary>
/// Provides a type-safe, persistent global state storage that survives scene transitions.
/// Use this for data that needs to persist across scenes (e.g., player progress, settings, scores).
/// </summary>
/// <remarks>
/// Unlike SceneParameters (which are for one-time data transfer between scenes),
/// GameState provides mutable, persistent storage accessible from any scene.
/// Thread-safe for read operations; write operations should be done from the main thread.
/// </remarks>
public interface IGameState
{
    /// <summary>
    /// Gets the number of values stored in the game state.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Retrieves a value from the game state with compile-time type safety.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key identifying the value.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if key does not exist.</exception>
    /// <exception cref="InvalidCastException">Thrown if stored value cannot be cast to T.</exception>
    T Get<T>(string key);

    /// <summary>
    /// Attempts to retrieve a value from the game state.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key identifying the value.</param>
    /// <param name="value">The value if found, otherwise default(T).</param>
    /// <returns>True if the value exists and can be cast to T, otherwise false.</returns>
    bool TryGet<T>(string key, out T? value);

    /// <summary>
    /// Sets a value in the game state. Creates a new entry if the key doesn't exist,
    /// or updates the existing value if it does.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key identifying the value.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    void Set<T>(string key, T value);

    /// <summary>
    /// Checks if a value with the specified key exists in the game state.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists, otherwise false.</returns>
    bool Contains(string key);

    /// <summary>
    /// Removes a value from the game state.
    /// </summary>
    /// <param name="key">The key identifying the value to remove.</param>
    /// <returns>True if the value was removed, false if the key didn't exist.</returns>
    bool Remove(string key);

    /// <summary>
    /// Removes all values from the game state.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all keys currently stored in the game state.
    /// </summary>
    /// <returns>A read-only collection of all keys.</returns>
    IEnumerable<string> GetKeys();
}
