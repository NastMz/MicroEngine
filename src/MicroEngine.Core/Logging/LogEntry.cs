namespace MicroEngine.Core.Logging;

/// <summary>
/// Represents a structured log entry.
/// </summary>
public sealed class LogEntry
{
    /// <summary>
    /// Gets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the severity level of the log entry.
    /// </summary>
    public LogLevel Level { get; init; }

    /// <summary>
    /// Gets the category or source of the log entry.
    /// </summary>
    public string Category { get; init; }

    /// <summary>
    /// Gets the log message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Gets the exception associated with this log entry, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets additional contextual data for the log entry.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class.
    /// </summary>
    /// <param name="level">The severity level.</param>
    /// <param name="category">The category or source.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="data">Optional contextual data.</param>
    public LogEntry(
        LogLevel level,
        string category,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
    {
        Timestamp = DateTime.UtcNow;
        Level = level;
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception;
        Data = data;
    }
}
