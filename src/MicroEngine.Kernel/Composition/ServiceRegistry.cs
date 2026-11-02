using System.Diagnostics.CodeAnalysis;

namespace MicroEngine.Kernel;

/// <summary>
/// The engine's services, one instance per type, registered by modules and read by whoever needs them. Typed and
/// explicit: a service is found by the exact type it was registered as (no base classes, no interfaces it also
/// implements), nothing is created on demand and nothing is discovered by reflection, so the registry works the same
/// under trimming, NativeAOT and WebAssembly.
/// </summary>
/// <remarks>Read and written on the game's thread; not thread-safe.</remarks>
public sealed class ServiceRegistry
{
    private readonly Dictionary<Type, object> _services = [];

    /// <summary>The number of registered services.</summary>
    public int Count => _services.Count;

    /// <summary>Registers <paramref name="instance"/> as the service of type <typeparamref name="T"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A service of type <typeparamref name="T"/> is already registered; use <see cref="Replace{T}"/> to change it.</exception>
    public void Add<T>(T instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!_services.TryAdd(typeof(T), instance))
            throw new InvalidOperationException($"A {Name<T>()} service is already registered; call Replace<{Name<T>()}> to change it.");
    }

    /// <summary>Registers <paramref name="instance"/> as the service of type <typeparamref name="T"/>, in place of the one registered before, if any.</summary>
    /// <returns>The service it replaced, or null when there was none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is null.</exception>
    public T? Replace<T>(T instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        _services.TryGetValue(typeof(T), out var previous);
        _services[typeof(T)] = instance;
        return (T?)previous;
    }

    /// <summary>The service registered as <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidOperationException">No service of type <typeparamref name="T"/> is registered; the message names the type.</exception>
    public T Get<T>() where T : class =>
        TryGet<T>(out var service) ? service
            : throw new InvalidOperationException($"No {Name<T>()} service is registered. Add one with Services.Add<{Name<T>()}>, or add the module that provides it.");

    /// <summary>The service registered as <typeparamref name="T"/>, if there is one.</summary>
    public bool TryGet<T>([NotNullWhen(true)] out T? service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var found))
        {
            service = (T)found;
            return true;
        }
        service = null;
        return false;
    }

    /// <summary>Whether a service is registered as <typeparamref name="T"/>.</summary>
    public bool Contains<T>() where T : class => _services.ContainsKey(typeof(T));

    /// <summary>Removes the service registered as <typeparamref name="T"/>; false when there was none.</summary>
    public bool Remove<T>() where T : class => _services.Remove(typeof(T));

    private static string Name<T>() => typeof(T).Name;
}
