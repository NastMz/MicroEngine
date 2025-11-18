namespace MicroEngine.Core.Resources;

/// <summary>
/// Validates resource files before loading.
/// Provides security, size, and format validation.
/// </summary>
public sealed class ResourceValidator
{
    private const long DEFAULT_MAX_FILE_SIZE = 100 * 1024 * 1024; // 100 MB
    private readonly long _maxFileSizeBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceValidator"/> class.
    /// </summary>
    /// <param name="maxFileSizeBytes">Maximum allowed file size in bytes. Default is 100 MB.</param>
    public ResourceValidator(long maxFileSizeBytes = DEFAULT_MAX_FILE_SIZE)
    {
        _maxFileSizeBytes = maxFileSizeBytes;
    }

    /// <summary>
    /// Validates a resource file path.
    /// </summary>
    /// <param name="path">Path to the resource file.</param>
    /// <param name="supportedExtensions">List of supported file extensions.</param>
    /// <returns>Validation result.</returns>
    public ResourceValidationResult Validate(string path, IReadOnlyList<string> supportedExtensions)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.InvalidPath,
                "Resource path cannot be null or empty"
            );
        }

        if (!IsValidPath(path))
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.InvalidPath,
                $"Resource path contains invalid characters: {path}"
            );
        }

        if (!File.Exists(path))
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.FileNotFound,
                $"Resource file not found: {path}"
            );
        }

        var fileInfo = new FileInfo(path);

        var extension = fileInfo.Extension.ToLowerInvariant();
        if (supportedExtensions.Count > 0 && !supportedExtensions.Contains(extension))
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.UnsupportedExtension,
                $"File extension '{extension}' is not supported. Supported: {string.Join(", ", supportedExtensions)}"
            );
        }

        if (fileInfo.Length == 0)
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.InvalidFileData,
                $"Resource file is empty: {path}"
            );
        }

        if (fileInfo.Length > _maxFileSizeBytes)
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.FileTooLarge,
                $"Resource file exceeds maximum size ({_maxFileSizeBytes} bytes): {path} ({fileInfo.Length} bytes)"
            );
        }

        try
        {
            using var stream = File.OpenRead(path);
        }
        catch (UnauthorizedAccessException)
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.AccessDenied,
                $"Access denied to resource file: {path}"
            );
        }
        catch (IOException)
        {
            return ResourceValidationResult.Failure(
                ResourceValidationError.FileLocked,
                $"Resource file is locked by another process: {path}"
            );
        }

        return ResourceValidationResult.Success();
    }

    /// <summary>
    /// Checks if a path is valid and safe to use.
    /// </summary>
    private static bool IsValidPath(string path)
    {
        try
        {
            var invalidChars = Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }
}
