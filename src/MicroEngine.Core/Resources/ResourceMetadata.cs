namespace MicroEngine.Core.Resources;

/// <summary>
/// Metadata information for a resource.
/// Provides validation, versioning, and diagnostic information.
/// </summary>
public sealed class ResourceMetadata
{
    /// <summary>
    /// Gets the file extension of the resource.
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Gets the timestamp when the file was last modified.
    /// </summary>
    public required DateTime LastModified { get; init; }

    /// <summary>
    /// Gets the timestamp when the resource was loaded into memory.
    /// </summary>
    public required DateTime LoadedAt { get; init; }

    /// <summary>
    /// Gets the version or format identifier of the resource.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets custom metadata specific to the resource type.
    /// </summary>
    public IReadOnlyDictionary<string, string>? CustomMetadata { get; init; }

    /// <summary>
    /// Gets whether the resource is compressed.
    /// </summary>
    public bool IsCompressed { get; init; }

    /// <summary>
    /// Gets the compression ratio if applicable (original size / compressed size).
    /// </summary>
    public float? CompressionRatio { get; init; }

    /// <summary>
    /// Creates metadata from a file path.
    /// </summary>
    public static ResourceMetadata FromFile(string path)
    {
        var fileInfo = new FileInfo(path);

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Cannot create metadata: file not found: {path}", path);
        }

        return new ResourceMetadata
        {
            Extension = fileInfo.Extension,
            FileSizeBytes = fileInfo.Length,
            LastModified = fileInfo.LastWriteTimeUtc,
            LoadedAt = DateTime.UtcNow,
            Version = null,
            CustomMetadata = null,
            IsCompressed = false,
            CompressionRatio = null
        };
    }

    /// <summary>
    /// Creates a copy with custom metadata.
    /// </summary>
    public ResourceMetadata WithCustomMetadata(IReadOnlyDictionary<string, string> customMetadata)
    {
        return new ResourceMetadata
        {
            Extension = Extension,
            FileSizeBytes = FileSizeBytes,
            LastModified = LastModified,
            LoadedAt = LoadedAt,
            Version = Version,
            CustomMetadata = customMetadata,
            IsCompressed = IsCompressed,
            CompressionRatio = CompressionRatio
        };
    }

    /// <summary>
    /// Creates a copy with version information.
    /// </summary>
    public ResourceMetadata WithVersion(string version)
    {
        return new ResourceMetadata
        {
            Extension = Extension,
            FileSizeBytes = FileSizeBytes,
            LastModified = LastModified,
            LoadedAt = LoadedAt,
            Version = version,
            CustomMetadata = CustomMetadata,
            IsCompressed = IsCompressed,
            CompressionRatio = CompressionRatio
        };
    }

    /// <summary>
    /// Creates a copy with compression information.
    /// </summary>
    public ResourceMetadata WithCompression(bool isCompressed, float? compressionRatio = null)
    {
        return new ResourceMetadata
        {
            Extension = Extension,
            FileSizeBytes = FileSizeBytes,
            LastModified = LastModified,
            LoadedAt = LoadedAt,
            Version = Version,
            CustomMetadata = CustomMetadata,
            IsCompressed = isCompressed,
            CompressionRatio = compressionRatio
        };
    }
}
