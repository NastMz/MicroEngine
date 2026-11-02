using System.Diagnostics.CodeAnalysis;

namespace MicroEngine.Kernel;

/// <summary>
/// Optional features something in the engine offers (a display that can change its mode, a touch screen, platform
/// diagnostics), declared by whoever provides them and queried by type. A feature that is absent is simply not
/// declared: callers ask with <see cref="TryGet{T}"/> instead of casting a platform or device to find out.
/// </summary>
/// <remarks>Read and written on the game's thread; not thread-safe.</remarks>
public sealed class CapabilityRegistry
{
    private readonly Dictionary<Type, object> _capabilities = [];

    /// <summary>The number of declared capabilities.</summary>
    public int Count => _capabilities.Count;

    /// <summary>Declares <paramref name="capability"/> as the capability of type <typeparamref name="T"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="capability"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A capability of type <typeparamref name="T"/> is already declared; use <see cref="Replace{T}"/>.</exception>
    public void Add<T>(T capability) where T : class
    {
        ArgumentNullException.ThrowIfNull(capability);
        if (!_capabilities.TryAdd(typeof(T), capability))
            throw new InvalidOperationException($"A {typeof(T).Name} capability is already declared; call Replace<{typeof(T).Name}> to change it.");
    }

    /// <summary>Declares <paramref name="capability"/> as the capability of type <typeparamref name="T"/>, in place of the one declared before, if any.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="capability"/> is null.</exception>
    public void Replace<T>(T capability) where T : class
    {
        ArgumentNullException.ThrowIfNull(capability);
        _capabilities[typeof(T)] = capability;
    }

    /// <summary>The capability declared as <typeparamref name="T"/>, if there is one.</summary>
    public bool TryGet<T>([NotNullWhen(true)] out T? capability) where T : class
    {
        if (_capabilities.TryGetValue(typeof(T), out var found))
        {
            capability = (T)found;
            return true;
        }
        capability = null;
        return false;
    }

    /// <summary>Whether a capability is declared as <typeparamref name="T"/>.</summary>
    public bool Has<T>() where T : class => _capabilities.ContainsKey(typeof(T));

    /// <summary>Withdraws the capability declared as <typeparamref name="T"/>; false when there was none.</summary>
    public bool Remove<T>() where T : class => _capabilities.Remove(typeof(T));
}
