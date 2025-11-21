namespace MicroEngine.Core.DependencyInjection;

/// <summary>
/// Implementation of dependency injection container supporting singleton, scoped, and transient lifetimes.
/// Thread-safe service resolution with parent-child scope hierarchy.
/// </summary>
public sealed class ServiceContainer : IServiceContainer
{
    private readonly IServiceContainer? _parent;
    private readonly Dictionary<Type, ServiceRegistration> _registrations = new();
    private readonly Dictionary<Type, object> _singletonInstances = new();
    private readonly Dictionary<Type, object> _scopedInstances = new();
    private readonly List<IDisposable> _disposables = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new root service container.
    /// </summary>
    public ServiceContainer()
    {
        _parent = null;
    }

    /// <summary>
    /// Initializes a new scoped service container with a parent.
    /// </summary>
    private ServiceContainer(IServiceContainer parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <inheritdoc/>
    public void RegisterSingleton<T>(T instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            var type = typeof(T);
            _registrations[type] = new ServiceRegistration(ServiceLifetime.Singleton, null);
            _singletonInstances[type] = instance;
        }
    }

    /// <inheritdoc/>
    public void RegisterSingleton<T>(Func<IServiceContainer, T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            var type = typeof(T);
            _registrations[type] = new ServiceRegistration(ServiceLifetime.Singleton, factory);
        }
    }

    /// <inheritdoc/>
    public void RegisterScoped<T>(Func<IServiceContainer, T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            var type = typeof(T);
            _registrations[type] = new ServiceRegistration(ServiceLifetime.Scoped, factory);
        }
    }

    /// <inheritdoc/>
    public void RegisterTransient<T>(Func<IServiceContainer, T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            var type = typeof(T);
            _registrations[type] = new ServiceRegistration(ServiceLifetime.Transient, factory);
        }
    }

    /// <inheritdoc/>
    public T GetService<T>() where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var type = typeof(T);

        lock (_lock)
        {
            // Try to get from current container
            if (_registrations.TryGetValue(type, out var registration))
            {
                return (T)ResolveService(type, registration);
            }

            // Try parent container
            if (_parent != null)
            {
                return _parent.GetService<T>();
            }

            throw new InvalidOperationException($"Service of type {type.Name} is not registered.");
        }
    }

    /// <inheritdoc/>
    public IServiceContainer CreateScope()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new ServiceContainer(this);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            // Dispose all scoped instances in reverse order
            for (int i = _disposables.Count - 1; i >= 0; i--)
            {
                try
                {
                    _disposables[i].Dispose();
                }
                catch
                {
                    // Swallow disposal exceptions to ensure all disposables are attempted
                }
            }

            _disposables.Clear();
            _scopedInstances.Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    private object ResolveService(Type type, ServiceRegistration registration)
    {
        switch (registration.Lifetime)
        {
            case ServiceLifetime.Singleton:
                return ResolveSingleton(type, registration);

            case ServiceLifetime.Scoped:
                return ResolveScoped(type, registration);

            case ServiceLifetime.Transient:
                return ResolveTransient(type, registration);

            default:
                throw new InvalidOperationException($"Unknown service lifetime: {registration.Lifetime}");
        }
    }

    private object ResolveSingleton(Type type, ServiceRegistration registration)
    {
        // Check if already instantiated
        if (_singletonInstances.TryGetValue(type, out var instance))
        {
            return instance;
        }

        // Check parent for singleton
        if (_parent != null && _parent is ServiceContainer parentContainer)
        {
            lock (parentContainer._lock)
            {
                if (parentContainer._singletonInstances.TryGetValue(type, out var parentInstance))
                {
                    return parentInstance;
                }

                // If parent has registration, create there
                if (parentContainer._registrations.TryGetValue(type, out var parentReg) &&
                    parentReg.Lifetime == ServiceLifetime.Singleton)
                {
                    return parentContainer.ResolveSingleton(type, parentReg);
                }
            }
        }

        // Create new singleton instance
        if (registration.Factory == null)
        {
            throw new InvalidOperationException($"Singleton service {type.Name} has no factory.");
        }

        instance = registration.Factory(this);
        _singletonInstances[type] = instance;
        return instance;
    }

    private object ResolveScoped(Type type, ServiceRegistration registration)
    {
        // Check if already instantiated in this scope
        if (_scopedInstances.TryGetValue(type, out var instance))
        {
            return instance;
        }

        // Create new scoped instance
        if (registration.Factory == null)
        {
            throw new InvalidOperationException($"Scoped service {type.Name} has no factory.");
        }

        instance = registration.Factory(this);
        _scopedInstances[type] = instance;

        // Track for disposal if IDisposable
        if (instance is IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        return instance;
    }

    private object ResolveTransient(Type type, ServiceRegistration registration)
    {
        if (registration.Factory == null)
        {
            throw new InvalidOperationException($"Transient service {type.Name} has no factory.");
        }

        return registration.Factory(this);
    }

    private sealed class ServiceRegistration
    {
        public ServiceLifetime Lifetime { get; }
        public Func<IServiceContainer, object>? Factory { get; }

        public ServiceRegistration(ServiceLifetime lifetime, Delegate? factory)
        {
            Lifetime = lifetime;
            Factory = factory != null ? container => factory.DynamicInvoke(container)! : null;
        }
    }
}
