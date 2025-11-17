namespace MicroEngine.Core.Logging;

/// <summary>
/// Represents the severity level of a log message.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed information for diagnosing issues (highest verbosity).
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Information useful for debugging during development.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// General informational messages.
    /// </summary>
    Info = 2,

    /// <summary>
    /// Warnings about potentially harmful situations.
    /// </summary>
    Warn = 3,

    /// <summary>
    /// Error events that might still allow the application to continue.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Critical errors that require immediate attention.
    /// </summary>
    Fatal = 5
}
