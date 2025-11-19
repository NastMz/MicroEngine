namespace MicroEngine.Core.Savegame;

/// <summary>
/// Interface for savegame serialization.
/// Abstracts the serialization format (JSON, binary, etc.).
/// </summary>
public interface ISaveSerializer
{
    /// <summary>
    /// Serializes a save container to a string.
    /// </summary>
    /// <typeparam name="T">The type of data in the container.</typeparam>
    /// <param name="container">The save container to serialize.</param>
    /// <returns>The serialized string.</returns>
    string Serialize<T>(SaveContainer<T> container);

    /// <summary>
    /// Deserializes a save container from a string.
    /// </summary>
    /// <typeparam name="T">The type of data in the container.</typeparam>
    /// <param name="data">The serialized string.</param>
    /// <returns>The deserialized save container, or null if deserialization failed.</returns>
    SaveContainer<T>? Deserialize<T>(string data);
}
