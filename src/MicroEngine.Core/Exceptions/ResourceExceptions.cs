using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Base exception for all resource-related errors.
/// Error codes: RES-xxx
/// </summary>
public class ResourceException : MicroEngineException
{
    /// <summary>
    /// Gets the resource path that caused the error.
    /// </summary>
    public string? ResourcePath { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceException"/> class.
    /// </summary>
    public ResourceException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceException"/> class with a specified error message.
    /// </summary>
    public ResourceException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceException"/> class with a specified error message and error code.
    /// </summary>
    public ResourceException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceException"/> class with a specified error message and inner exception.
    /// </summary>
    public ResourceException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceException"/> class with a specified error message, error code, and inner exception.
    /// </summary>
    public ResourceException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException) { }
}

/// <summary>
/// Exception thrown when a resource file is not found.
/// Error code: RES-404
/// </summary>
public sealed class ResourceNotFoundException : ResourceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    /// <param name="path">The path to the resource that was not found.</param>
    public ResourceNotFoundException(string path)
        : base($"Resource not found: {path}", "RES-404")
    {
        ResourcePath = path;
        WithContext("path", path);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="path">The path to the resource that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceNotFoundException(string path, Exception innerException)
        : base($"Resource not found: {path}", "RES-404", innerException)
    {
        ResourcePath = path;
        WithContext("path", path);
    }
}

/// <summary>
/// Exception thrown when a resource fails to load.
/// Error code: RES-500
/// </summary>
public sealed class ResourceLoadException : ResourceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLoadException"/> class.
    /// </summary>
    /// <param name="path">The path to the resource that failed to load.</param>
    /// <param name="reason">The reason for the failure.</param>
    public ResourceLoadException(string path, string reason)
        : base($"Failed to load resource '{path}': {reason}", "RES-500")
    {
        ResourcePath = path;
        WithContext("path", path);
        WithContext("reason", reason);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLoadException"/> class with an inner exception.
    /// </summary>
    /// <param name="path">The path to the resource that failed to load.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceLoadException(string path, string reason, Exception innerException)
        : base($"Failed to load resource '{path}': {reason}", "RES-500", innerException)
    {
        ResourcePath = path;
        WithContext("path", path);
        WithContext("reason", reason);
    }
}

/// <summary>
/// Exception thrown when a resource has an invalid or corrupted format.
/// Error code: RES-400
/// </summary>
public sealed class InvalidResourceFormatException : ResourceException
{
    /// <summary>
    /// Gets the expected resource format.
    /// </summary>
    public string? ExpectedFormat { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidResourceFormatException"/> class.
    /// </summary>
    /// <param name="path">The path to the resource with invalid format.</param>
    /// <param name="expectedFormat">The expected format.</param>
    public InvalidResourceFormatException(string path, string expectedFormat)
        : base($"Invalid resource format for '{path}'. Expected: {expectedFormat}", "RES-400")
    {
        ResourcePath = path;
        ExpectedFormat = expectedFormat;
        WithContext("path", path);
        WithContext("expectedFormat", expectedFormat);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidResourceFormatException"/> class with an inner exception.
    /// </summary>
    /// <param name="path">The path to the resource with invalid format.</param>
    /// <param name="expectedFormat">The expected format.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidResourceFormatException(string path, string expectedFormat, Exception innerException)
        : base($"Invalid resource format for '{path}'. Expected: {expectedFormat}", "RES-400", innerException)
    {
        ResourcePath = path;
        ExpectedFormat = expectedFormat;
        WithContext("path", path);
        WithContext("expectedFormat", expectedFormat);
    }
}

/// <summary>
/// Exception thrown when a resource validation fails.
/// Error code: RES-422
/// </summary>
public sealed class ResourceValidationException : ResourceException
{
    /// <summary>
    /// Gets the validation error code.
    /// </summary>
    public ResourceValidationError ValidationError { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceValidationException"/> class.
    /// </summary>
    /// <param name="path">The path to the resource that failed validation.</param>
    /// <param name="validationError">The validation error code.</param>
    /// <param name="reason">The reason for the validation failure.</param>
    public ResourceValidationException(string path, ResourceValidationError validationError, string reason)
        : base($"Resource validation failed for '{path}': {reason}", "RES-422")
    {
        ResourcePath = path;
        ValidationError = validationError;
        WithContext("path", path);
        WithContext("validationError", validationError.ToString());
        WithContext("reason", reason);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceValidationException"/> class with an inner exception.
    /// </summary>
    /// <param name="path">The path to the resource that failed validation.</param>
    /// <param name="validationError">The validation error code.</param>
    /// <param name="reason">The reason for the validation failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceValidationException(string path, ResourceValidationError validationError, string reason, Exception innerException)
        : base($"Resource validation failed for '{path}': {reason}", "RES-422", innerException)
    {
        ResourcePath = path;
        ValidationError = validationError;
        WithContext("path", path);
        WithContext("validationError", validationError.ToString());
        WithContext("reason", reason);
    }
}
