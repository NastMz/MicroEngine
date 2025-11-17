namespace MicroEngine.Core.Logging;

/// <summary>
/// Defines the contract for logging implementations.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Gets the minimum log level that will be written.
    /// Messages below this level are ignored.
    /// </summary>
    LogLevel MinimumLevel { get; set; }

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    void Write(LogEntry entry);

    /// <summary>
    /// Writes a trace-level log message.
    /// </summary>
    /// <param name="category">The category or source.</param>
    /// <param name="message">The log message.</param>
    void Trace(string category, string message);

    /// <summary>
    /// Writes a debug-level log message.
    /// </summary>
    /// <param name="category">The category or source.</param>
    /// <param name="message">The log message.</param>
    void Debug(string category, string message);

    /// <summary>
    /// Writes an info-level log message.
    /// </summary>
    /// <param name="category">The category or source.</param>
    /// <param name="message">The log message.</param>
    void Info(string category, string message);

    /// <summary>
    /// Writes a warning-level log message.
    /// </summary>
    /// <param name="category">The category or source.</param>
    /// <param name="message">The log message.</param>
    void Warn(string category, string message);

    /// <summary>
    /// Writes an error-level log message.
    /// </summary>
    /// <param name="category">The category or source.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception.</param>
    void Error(string category, string message, Exception? exception = null);

    /// <summary>
    /// Writes a fatal-level log message.
    /// </summary>
    /// <param name="category">The category or source.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception.</param>
    void Fatal(string category, string message, Exception? exception = null);
}
