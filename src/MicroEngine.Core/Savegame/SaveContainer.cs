using System.Text.Json.Serialization;

namespace MicroEngine.Core.Savegame;

/// <summary>
/// Container for save file data with metadata and versioning.
/// </summary>
/// <typeparam name="T">The type of game data to save.</typeparam>
public sealed class SaveContainer<T>
{
    /// <summary>
    /// Gets or sets the save file metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public SaveMetadata Metadata { get; set; } = new SaveMetadata();

    /// <summary>
    /// Gets or sets the actual game data.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
