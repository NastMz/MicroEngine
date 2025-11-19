using System.Text.Json.Serialization;

namespace MicroEngine.Core.Savegame;

/// <summary>
/// Contains metadata about a save file.
/// </summary>
public sealed class SaveMetadata
{
    /// <summary>
    /// Gets or sets the save file format version (semantic versioning).
    /// Used for backward compatibility checks.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the engine version that created this save.
    /// </summary>
    [JsonPropertyName("engineVersion")]
    public string EngineVersion { get; set; } = "0.7.5";

    /// <summary>
    /// Gets or sets the timestamp when the save was created (UTC).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp of the last modification (UTC).
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets optional user-defined save name/description.
    /// </summary>
    [JsonPropertyName("saveName")]
    public string? SaveName { get; set; }

    /// <summary>
    /// Gets or sets optional custom metadata as key-value pairs.
    /// </summary>
    [JsonPropertyName("customData")]
    public Dictionary<string, string> CustomData { get; set; } = new Dictionary<string, string>();
}
