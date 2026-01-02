namespace MicroEngine.Core.Resources;

/// <summary>
/// Interface for poolable objects that can be reset to a clean state.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Resets the object to its initial clean state.
    /// Called when the object is returned to the pool.
    /// </summary>
    void Reset();
}

/// <summary>
/// Generic object pool for zero-allocation event processing.
/// Reuses instances to minimize garbage collection pressure during gameplay.
/// </summary>
/// <typeparam name="T">Type of object to pool. Must implement IPoolable.</typeparam>
public sealed class ObjectPool<T> : IDisposable where T : class, IPoolable, new()
{
    private readonly Stack<T> _available;
    private readonly HashSet<T> _active = new();
    private readonly int _maxSize;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the number of available instances in the pool.
    /// </summary>
    public int AvailableCount
    {
        get
        {
            lock (_lock)
            {
                return _available.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of active instances currently rented.
    /// </summary>
    public int ActiveCount
    {
        get
        {
            lock (_lock)
            {
                return _active.Count;
            }
        }
    }

    /// <summary>
    /// Creates a new object pool with a specified capacity.
    /// </summary>
    /// <param name="initialCapacity">Initial number of instances to create. Defaults to 16.</param>
    /// <param name="maxSize">Maximum pool size. Defaults to 256.</param>
    public ObjectPool(int initialCapacity = 16, int maxSize = 256)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentException("Initial capacity cannot be negative.", nameof(initialCapacity));
        }
        
        if (maxSize < initialCapacity)
        {
            throw new ArgumentException("Max size must be >= initial capacity.", nameof(maxSize));
        }

        _maxSize = maxSize;
        _available = new Stack<T>(initialCapacity);

        // Pre-allocate initial instances
        for (int i = 0; i < initialCapacity; i++)
        {
            _available.Push(new T());
        }
    }

    /// <summary>
    /// Rents an instance from the pool, optionally initializing it with a callback.
    /// </summary>
    /// <param name="initializer">Optional delegate to initialize the rented instance.</param>
    /// <returns>A rented instance, either from the pool or newly created.</returns>
    public T Rent(Action<T>? initializer = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        T instance;

        lock (_lock)
        {
            instance = _available.Count > 0 ? _available.Pop() : new T();
            _active.Add(instance);
        }

        initializer?.Invoke(instance);
        return instance;
    }

    /// <summary>
    /// Returns an instance to the pool for reuse.
    /// The instance is reset before being made available.
    /// </summary>
    /// <param name="instance">Instance to return.</param>
    public void Return(T instance)
    {
        if (_disposed || instance == null)
        {
            return;
        }

        instance.Reset();

        lock (_lock)
        {
            _active.Remove(instance);

            // Only keep instances up to max size
            if (_available.Count < _maxSize)
            {
                _available.Push(instance);
            }
        }
    }

    /// <summary>
    /// Clears the pool, resetting all state.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _available.Clear();
            _active.Clear();
        }
    }

    /// <summary>
    /// Disposes the pool and all pooled instances.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _available.Clear();
            _active.Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
