namespace MicroEngine.Core.DependencyInjection;

/// <summary>
/// Defines a dependency injection container for managing service lifetimes.
/// Supports singleton, scoped, and transient service lifetimes.
/// </summary>
public interface IServiceContainer : IDisposable
{
    /// <summary>
    /// Registers a singleton service instance.
    /// The same instance will be returned for all requests.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    void RegisterSingleton<T>(T instance) where T : class;

    /// <summary>
    /// Registers a singleton service factory.
    /// The factory will be called once on first request, and the result cached.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="factory">Factory function to create the service.</param>
    void RegisterSingleton<T>(Func<IServiceContainer, T> factory) where T : class;

    /// <summary>
    /// Registers a scoped service factory.
    /// A new instance will be created for each scope, but reused within that scope.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="factory">Factory function to create the service.</param>
    void RegisterScoped<T>(Func<IServiceContainer, T> factory) where T : class;

    /// <summary>
    /// Registers a transient service factory.
    /// A new instance will be created every time the service is requested.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="factory">Factory function to create the service.</param>
    void RegisterTransient<T>(Func<IServiceContainer, T> factory) where T : class;

    /// <summary>
    /// Resolves a service from the container.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the service is not registered.</exception>
    T GetService<T>() where T : class;

    /// <summary>
    /// Creates a new child scope.
    /// Scoped services registered in the parent will create new instances in the child scope.
    /// Singleton services are shared between parent and child scopes.
    /// </summary>
    /// <returns>A new scoped container.</returns>
    IServiceContainer CreateScope();
}
