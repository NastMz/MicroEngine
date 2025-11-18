namespace MicroEngine.Core.Resources;

/// <summary>
/// Watches resources for file system changes and triggers reload events.
/// Used for hot-reloading during development.
/// </summary>
public interface IResourceWatcher : IDisposable
{
    /// <summary>
    /// Event raised when a watched resource file changes.
    /// </summary>
    event EventHandler<ResourceChangedEventArgs>? ResourceChanged;

    /// <summary>
    /// Starts watching a resource file for changes.
    /// </summary>
    /// <param name="path">Absolute path to the resource file.</param>
    void Watch(string path);

    /// <summary>
    /// Stops watching a resource file.
    /// </summary>
    /// <param name="path">Absolute path to the resource file.</param>
    void Unwatch(string path);

    /// <summary>
    /// Gets whether a specific path is currently being watched.
    /// </summary>
    bool IsWatching(string path);

    /// <summary>
    /// Gets the total number of watched resources.
    /// </summary>
    int WatchedCount { get; }

    /// <summary>
    /// Enables or disables the watcher.
    /// When disabled, no events will be raised.
    /// </summary>
    bool Enabled { get; set; }
}

/// <summary>
/// Event arguments for resource file change events.
/// </summary>
public sealed class ResourceChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the absolute path to the changed resource.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public required ResourceChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets when the change was detected.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of resource file change.
/// </summary>
public enum ResourceChangeType
{
    /// <summary>Resource file was modified.</summary>
    Modified,

    /// <summary>Resource file was deleted.</summary>
    Deleted,

    /// <summary>Resource file was renamed.</summary>
    Renamed
}
