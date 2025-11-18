namespace MicroEngine.Core.Resources;

/// <summary>
/// Base interface for all resources.
/// Resources are ref-counted, loaded assets (textures, sounds, fonts, etc.).
/// </summary>
public interface IResource : IDisposable
{
    /// <summary>
    /// Gets the unique resource identifier.
    /// </summary>
    ResourceId Id { get; }

    /// <summary>
    /// Gets the file path this resource was loaded from.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets whether this resource is currently loaded and valid.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the size of the resource in bytes (estimated).
    /// </summary>
    long SizeInBytes { get; }
}
