using System.Collections.Immutable;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Immutable container for passing data between scenes in a type-safe manner.
/// Supports storing multiple values by key with compile-time type checking.
/// </summary>
public sealed class SceneParameters
{
    private readonly ImmutableDictionary<string, object> _parameters;

    /// <summary>
    /// Gets an empty SceneParameters instance with no data.
    /// </summary>
    public static SceneParameters Empty { get; } = new SceneParameters(ImmutableDictionary<string, object>.Empty);

    /// <summary>
    /// Gets the number of parameters stored.
    /// </summary>
    public int Count => _parameters.Count;

    private SceneParameters(ImmutableDictionary<string, object> parameters)
    {
        _parameters = parameters;
    }

    /// <summary>
    /// Creates a new SceneParameters builder for constructing parameters fluently.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static Builder Create()
    {
        return new Builder();
    }

    /// <summary>
    /// Retrieves a parameter value by key with compile-time type safety.
    /// </summary>
    /// <typeparam name="T">The expected type of the parameter.</typeparam>
    /// <param name="key">The parameter key.</param>
    /// <returns>The parameter value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if key does not exist.</exception>
    /// <exception cref="InvalidCastException">Thrown if stored value cannot be cast to T.</exception>
    public T Get<T>(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (!_parameters.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Parameter key '{key}' not found in SceneParameters.");
        }

        if (value is not T typedValue)
        {
            throw new InvalidCastException($"Parameter '{key}' is of type {value.GetType().Name}, cannot cast to {typeof(T).Name}.");
        }

        return typedValue;
    }

    /// <summary>
    /// Attempts to retrieve a parameter value by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the parameter.</typeparam>
    /// <param name="key">The parameter key.</param>
    /// <param name="value">The parameter value if found, otherwise default(T).</param>
    /// <returns>True if the parameter exists and can be cast to T, otherwise false.</returns>
    public bool TryGet<T>(string key, out T? value)
    {
        if (key == null || !_parameters.TryGetValue(key, out var objValue))
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

    /// <summary>
    /// Checks if a parameter with the specified key exists.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>True if the parameter exists, otherwise false.</returns>
    public bool Contains(string key)
    {
        return key != null && _parameters.ContainsKey(key);
    }

    /// <summary>
    /// Builder for constructing SceneParameters instances fluently.
    /// </summary>
    public sealed class Builder
    {
        private ImmutableDictionary<string, object>.Builder _builder;

        internal Builder()
        {
            _builder = ImmutableDictionary.CreateBuilder<string, object>();
        }

        /// <summary>
        /// Adds a parameter with the specified key and value.
        /// </summary>
        /// <typeparam name="T">The type of the parameter value.</typeparam>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        /// <returns>This builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        /// <exception cref="ArgumentException">Thrown if key already exists.</exception>
        public Builder Add<T>(string key, T value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_builder.ContainsKey(key))
            {
                throw new ArgumentException($"Parameter key '{key}' already exists.", nameof(key));
            }

            _builder.Add(key, value!);
            return this;
        }

        /// <summary>
        /// Builds the immutable SceneParameters instance.
        /// </summary>
        /// <returns>A new immutable SceneParameters instance.</returns>
        public SceneParameters Build()
        {
            return new SceneParameters(_builder.ToImmutable());
        }
    }
}
