using System.Text.Json;

namespace MicroEngine.Core.Savegame;

/// <summary>
/// Default implementation of savegame management.
/// Stores saves in a configurable directory with JSON serialization.
/// </summary>
public sealed class SavegameManager : ISavegameManager
{
    private readonly string _saveDirectory;
    private readonly ISaveSerializer _serializer;
    private const string DEFAULT_SAVE_FOLDER = "Saves";
    private const string SAVE_EXTENSION = ".sav";

    /// <summary>
    /// Initializes a new instance of the <see cref="SavegameManager"/> class.
    /// </summary>
    /// <param name="saveDirectory">Optional custom save directory. Defaults to "./Saves".</param>
    /// <param name="serializer">Optional custom serializer. Defaults to JsonSaveSerializer.</param>
    public SavegameManager(string? saveDirectory = null, ISaveSerializer? serializer = null)
    {
        _saveDirectory = saveDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_SAVE_FOLDER);
        _serializer = serializer ?? new JsonSaveSerializer();

        // Ensure save directory exists
        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
        }
    }

    /// <inheritdoc/>
    public SaveResult Save<T>(T data, string fileName, string? saveName = null)
    {
        try
        {
            var container = new SaveContainer<T>
            {
                Metadata = new SaveMetadata
                {
                    SaveName = saveName,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    CustomData = new Dictionary<string, string>
                    {
                        { "dataType", typeof(T).FullName ?? typeof(T).Name }
                    }
                },
                Data = data
            };

            var serialized = _serializer.Serialize(container);
            var filePath = GetSavePath(fileName);

            // Update LastModified if file exists
            if (File.Exists(filePath))
            {
                var existing = LoadContainer<T>(fileName);
                if (existing != null)
                {
                    container.Metadata.CreatedAt = existing.Metadata.CreatedAt;
                }
            }

            File.WriteAllText(filePath, serialized);
            return SaveResult.Ok(filePath);
        }
        catch (Exception ex)
        {
            return SaveResult.Error($"Failed to save: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public LoadResult<T> Load<T>(string fileName)
    {
        try
        {
            var filePath = GetSavePath(fileName);

            if (!File.Exists(filePath))
            {
                return LoadResult<T>.Error($"Save file not found: {fileName}");
            }

            var json = File.ReadAllText(filePath);
            var container = _serializer.Deserialize<T>(json);

            if (container == null || container.Data == null)
            {
                return LoadResult<T>.Error("Failed to deserialize save file");
            }

            return LoadResult<T>.Ok(container.Data);
        }
        catch (Exception ex)
        {
            return LoadResult<T>.Error($"Failed to load: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public bool Delete(string fileName)
    {
        try
        {
            var filePath = GetSavePath(fileName);

            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public string GetSavePath(string fileName)
    {
        if (!fileName.EndsWith(SAVE_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            fileName += SAVE_EXTENSION;
        }

        return Path.Combine(_saveDirectory, fileName);
    }

    /// <inheritdoc/>
    public bool Exists(string fileName)
    {
        return File.Exists(GetSavePath(fileName));
    }

    /// <inheritdoc/>
    public string[] ListSaves()
    {
        if (!Directory.Exists(_saveDirectory))
        {
            return Array.Empty<string>();
        }

        var files = Directory.GetFiles(_saveDirectory, $"*{SAVE_EXTENSION}");
        return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
    }

    /// <inheritdoc/>
    public SaveMetadata? GetMetadata(string fileName)
    {
        try
        {
            var filePath = GetSavePath(fileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);

            // Use JsonDocument for lightweight metadata-only parsing
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("metadata", out var metadataElement))
            {
                return null;
            }

            return JsonSerializer.Deserialize<SaveMetadata>(metadataElement.GetRawText());
        }
        catch
        {
            return null;
        }
    }

    private SaveContainer<T>? LoadContainer<T>(string fileName)
    {
        try
        {
            var filePath = GetSavePath(fileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);
            return _serializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }
}
