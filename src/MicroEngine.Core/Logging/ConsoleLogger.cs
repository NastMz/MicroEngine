namespace MicroEngine.Core.Logging;

/// <summary>
/// Console-based logger implementation.
/// </summary>
public sealed class ConsoleLogger : ILogger
{
    /// <inheritdoc/>
    public LogLevel MinimumLevel { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
    /// </summary>
    /// <param name="minimumLevel">The minimum log level to write.</param>
    public ConsoleLogger(LogLevel minimumLevel = LogLevel.Info)
    {
        MinimumLevel = minimumLevel;
    }

    /// <inheritdoc/>
    public void Write(LogEntry entry)
    {
        if (entry.Level < MinimumLevel)
        {
            return;
        }

        SetConsoleColor(entry.Level);

        var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = entry.Level.ToString().ToUpperInvariant().PadRight(5);
        var message = $"[{timestamp}] [{level}] [{entry.Category}] {entry.Message}";

        Console.WriteLine(message);

        if (entry.Exception != null)
        {
            Console.WriteLine($"Exception: {entry.Exception}");
        }

        if (entry.Data != null && entry.Data.Count > 0)
        {
            foreach (var kvp in entry.Data)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        Console.ResetColor();
    }

    /// <inheritdoc/>
    public void Trace(string category, string message)
    {
        Write(new LogEntry(LogLevel.Trace, category, message));
    }

    /// <inheritdoc/>
    public void Debug(string category, string message)
    {
        Write(new LogEntry(LogLevel.Debug, category, message));
    }

    /// <inheritdoc/>
    public void Info(string category, string message)
    {
        Write(new LogEntry(LogLevel.Info, category, message));
    }

    /// <inheritdoc/>
    public void Warn(string category, string message)
    {
        Write(new LogEntry(LogLevel.Warn, category, message));
    }

    /// <inheritdoc/>
    public void Error(string category, string message, Exception? exception = null)
    {
        Write(new LogEntry(LogLevel.Error, category, message, exception));
    }

    /// <inheritdoc/>
    public void Fatal(string category, string message, Exception? exception = null)
    {
        Write(new LogEntry(LogLevel.Fatal, category, message, exception));
    }

    private static void SetConsoleColor(LogLevel level)
    {
        Console.ForegroundColor = level switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.DarkGray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}
