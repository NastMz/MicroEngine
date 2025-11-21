namespace MicroEngine.Core.DependencyInjection;

/// <summary>
/// Defines the lifetime of a service in the dependency injection container.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// A single instance is created and shared across the entire application.
    /// The instance is created on first request and reused for all subsequent requests.
    /// </summary>
    Singleton,

    /// <summary>
    /// A new instance is created for each scope (e.g., per scene load).
    /// The same instance is reused within the scope, but different scopes get different instances.
    /// Scoped instances are disposed when the scope is disposed.
    /// </summary>
    Scoped,

    /// <summary>
    /// A new instance is created every time the service is requested.
    /// The caller is responsible for disposing transient services.
    /// </summary>
    Transient
}
