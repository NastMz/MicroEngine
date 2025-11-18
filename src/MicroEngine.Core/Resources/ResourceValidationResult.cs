namespace MicroEngine.Core.Resources;

/// <summary>
/// Result of resource validation.
/// </summary>
public sealed class ResourceValidationResult
{
    /// <summary>
    /// Gets whether the validation passed.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the validation error code.
    /// </summary>
    public ResourceValidationError? ErrorCode { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ResourceValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ResourceValidationResult Failure(ResourceValidationError errorCode, string errorMessage)
    {
        return new ResourceValidationResult
        {
            IsValid = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Resource validation error codes.
/// </summary>
public enum ResourceValidationError
{
    /// <summary>File does not exist.</summary>
    FileNotFound,

    /// <summary>File extension is not supported.</summary>
    UnsupportedExtension,

    /// <summary>File size exceeds maximum allowed.</summary>
    FileTooLarge,

    /// <summary>File is empty or corrupted.</summary>
    InvalidFileData,

    /// <summary>Path contains invalid characters.</summary>
    InvalidPath,

    /// <summary>Access denied to the file.</summary>
    AccessDenied,

    /// <summary>File is locked by another process.</summary>
    FileLocked,

    /// <summary>Invalid file format or header.</summary>
    InvalidFormat
}
