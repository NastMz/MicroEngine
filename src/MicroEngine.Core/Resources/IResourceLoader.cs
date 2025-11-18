namespace MicroEngine.Core.Resources;

/// <summary>
/// Resource loader interface for loading specific resource types.
/// Implementations are backend-specific (Raylib, SDL, etc.).
/// </summary>
/// <typeparam name="T">Type of resource to load.</typeparam>
public interface IResourceLoader<T> where T : IResource
{
    /// <summary>
    /// Loads a resource from the specified path.
    /// </summary>
    /// <param name="path">File path to the resource.</param>
    /// <returns>Loaded resource instance.</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="InvalidDataException">If the file format is invalid.</exception>
    T Load(string path);

    /// <summary>
    /// Unloads a resource and frees its memory.
    /// </summary>
    /// <param name="resource">Resource to unload.</param>
    void Unload(T resource);

    /// <summary>
    /// Gets the supported file extensions for this loader.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }
}
