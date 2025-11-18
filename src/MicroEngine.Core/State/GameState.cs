using System.Collections.Concurrent;

namespace MicroEngine.Core.State;

/// <summary>
/// Thread-safe implementation of IGameState using ConcurrentDictionary for persistent global state.
/// </summary>
public sealed class GameState : IGameState
{
    private readonly ConcurrentDictionary<string, object> _state = new();

    /// <inheritdoc/>
    public int Count => _state.Count;

    /// <inheritdoc/>
    public T Get<T>(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (!_state.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Game state key '{key}' not found.");
        }

        if (value is not T typedValue)
        {
            throw new InvalidCastException($"Game state value for key '{key}' is of type {value.GetType().Name}, cannot cast to {typeof(T).Name}.");
        }

        return typedValue;
    }

    /// <inheritdoc/>
    public bool TryGet<T>(string key, out T? value)
    {
        if (key == null || !_state.TryGetValue(key, out var objValue))
        {
            value = default;
            return false;
        }

        if (objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public void Set<T>(string key, T value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _state[key] = value!;
    }

    /// <inheritdoc/>
    public bool Contains(string key)
    {
        return key != null && _state.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        if (key == null)
        {
            return false;
        }

        return _state.TryRemove(key, out _);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _state.Clear();
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetKeys()
    {
        return _state.Keys.ToList(); // ToList() creates snapshot for thread safety
    }
}
