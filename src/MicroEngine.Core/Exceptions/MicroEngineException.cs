namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Base exception class for all MicroEngine exceptions.
/// Provides error code support and context management.
/// </summary>
public class MicroEngineException : Exception
{
    /// <summary>
    /// Gets the error code associated with this exception.
    /// Error codes follow the format: [MODULE]-[NUMBER] (e.g., "ECS-404", "RES-001").
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets additional context information for this exception.
    /// Used for structured logging and diagnostics.
    /// </summary>
    public Dictionary<string, object> Context { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroEngineException"/> class.
    /// </summary>
    public MicroEngineException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroEngineException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MicroEngineException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroEngineException"/> class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code (e.g., "ECS-404").</param>
    public MicroEngineException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroEngineException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MicroEngineException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroEngineException"/> class with a specified error message, error code, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code (e.g., "ECS-404").</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MicroEngineException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Adds context information to this exception.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    /// <returns>This exception instance for method chaining.</returns>
    public MicroEngineException WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }

    /// <summary>
    /// Gets a formatted string containing the error code, message, and context.
    /// </summary>
    public override string ToString()
    {
        var result = ErrorCode != null
            ? $"[{ErrorCode}] {Message}"
            : Message;

        if (Context.Count > 0)
        {
            var contextStr = string.Join(", ", Context.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            result += $" | Context: {contextStr}";
        }

        if (InnerException != null)
        {
            result += $"\n---> {InnerException}";
        }

        result += $"\n{StackTrace}";

        return result;
    }
}
