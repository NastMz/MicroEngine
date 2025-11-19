namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Base exception for all backend-related errors.
/// Error codes: BACKEND-xxx
/// </summary>
public class BackendException : MicroEngineException
{
    /// <summary>
    /// Gets the backend type that caused the error.
    /// </summary>
    public string? BackendType { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendException"/> class.
    /// </summary>
    public BackendException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendException"/> class with a specified error message.
    /// </summary>
    public BackendException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendException"/> class with a specified error message and error code.
    /// </summary>
    public BackendException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendException"/> class with a specified error message and inner exception.
    /// </summary>
    public BackendException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendException"/> class with a specified error message, error code, and inner exception.
    /// </summary>
    public BackendException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException) { }
}

/// <summary>
/// Exception thrown when a backend initialization fails.
/// Error code: BACKEND-500
/// </summary>
public sealed class BackendInitializationException : BackendException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackendInitializationException"/> class.
    /// </summary>
    /// <param name="backendType">The type of backend that failed to initialize.</param>
    /// <param name="reason">The reason for the failure.</param>
    public BackendInitializationException(string backendType, string reason)
        : base($"Failed to initialize {backendType} backend: {reason}", "BACKEND-500")
    {
        BackendType = backendType;
        WithContext("backendType", backendType);
        WithContext("reason", reason);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendInitializationException"/> class with an inner exception.
    /// </summary>
    /// <param name="backendType">The type of backend that failed to initialize.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BackendInitializationException(string backendType, string reason, Exception innerException)
        : base($"Failed to initialize {backendType} backend: {reason}", "BACKEND-500", innerException)
    {
        BackendType = backendType;
        WithContext("backendType", backendType);
        WithContext("reason", reason);
    }
}

/// <summary>
/// Exception thrown when a backend operation fails.
/// Error code: BACKEND-400
/// </summary>
public sealed class BackendOperationException : BackendException
{
    /// <summary>
    /// Gets the operation that failed.
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendOperationException"/> class.
    /// </summary>
    /// <param name="backendType">The type of backend.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    public BackendOperationException(string backendType, string operation, string reason)
        : base($"{backendType} backend operation '{operation}' failed: {reason}", "BACKEND-400")
    {
        BackendType = backendType;
        Operation = operation;
        WithContext("backendType", backendType);
        WithContext("operation", operation);
        WithContext("reason", reason);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendOperationException"/> class with an inner exception.
    /// </summary>
    /// <param name="backendType">The type of backend.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BackendOperationException(string backendType, string operation, string reason, Exception innerException)
        : base($"{backendType} backend operation '{operation}' failed: {reason}", "BACKEND-400", innerException)
    {
        BackendType = backendType;
        Operation = operation;
        WithContext("backendType", backendType);
        WithContext("operation", operation);
        WithContext("reason", reason);
    }
}
