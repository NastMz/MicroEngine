using System.Text.Json;

namespace MicroEngine.Core.Savegame;

/// <summary>
/// JSON-based save serializer using System.Text.Json.
/// </summary>
public sealed class JsonSaveSerializer : ISaveSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSaveSerializer"/> class.
    /// </summary>
    /// <param name="writeIndented">Whether to format JSON with indentation (default: true).</param>
    public JsonSaveSerializer(bool writeIndented = true)
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc/>
    public string Serialize<T>(SaveContainer<T> container)
    {
        return JsonSerializer.Serialize(container, _options);
    }

    /// <inheritdoc/>
    public SaveContainer<T>? Deserialize<T>(string data)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveContainer<T>>(data, _options);
        }
        catch
        {
            return null;
        }
    }
}
