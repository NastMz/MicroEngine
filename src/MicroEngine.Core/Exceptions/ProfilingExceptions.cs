namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Base exception for all profiling-related errors.
/// Error codes: PROF-xxx
/// </summary>
public class ProfilingException : MicroEngineException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilingException"/> class.
    /// </summary>
    public ProfilingException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilingException"/> class with a specified error message.
    /// </summary>
    public ProfilingException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilingException"/> class with a specified error message and error code.
    /// </summary>
    public ProfilingException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilingException"/> class with a specified error message and inner exception.
    /// </summary>
    public ProfilingException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilingException"/> class with a specified error message, error code, and inner exception.
    /// </summary>
    public ProfilingException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException) { }
}

/// <summary>
/// Exception thrown when a profiling operation is invalid.
/// Error code: PROF-400
/// </summary>
public sealed class InvalidProfilingOperationException : ProfilingException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProfilingOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public InvalidProfilingOperationException(string message)
        : base(message, "PROF-400")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProfilingOperationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidProfilingOperationException(string message, Exception innerException)
        : base(message, "PROF-400", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a profiling configuration is invalid.
/// Error code: PROF-422
/// </summary>
public sealed class InvalidProfilingConfigurationException : ProfilingException
{
    /// <summary>
    /// Gets the name of the invalid configuration parameter.
    /// </summary>
    public string? ParameterName { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProfilingConfigurationException"/> class.
    /// </summary>
    /// <param name="parameterName">The name of the invalid parameter.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public InvalidProfilingConfigurationException(string parameterName, string message)
        : base(message, "PROF-422")
    {
        ParameterName = parameterName;
        WithContext("parameterName", parameterName);
    }
}
