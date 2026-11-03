namespace MicroEngine.Kernel;

/// <summary>How important a log message is.</summary>
public enum LogLevel
{
    /// <summary>Detail for whoever is debugging the engine.</summary>
    Debug,
    /// <summary>Something worth knowing that needs no action.</summary>
    Info,
    /// <summary>Something that works but should be fixed (a load during play that shows as a hitch).</summary>
    Warning,
    /// <summary>Something failed.</summary>
    Error,
}

/// <summary>
/// Where the engine's diagnostics go. The default (<see cref="ConsoleLog"/>) writes them to the standard error stream;
/// a host that has its own (a phone's system log, a browser console, an editor panel) registers another sink as the
/// engine's <see cref="ILog"/> service.
/// </summary>
public interface ILog
{
    /// <summary>Writes one message.</summary>
    /// <param name="level">How important it is.</param>
    /// <param name="category">The part of the engine it comes from, a short lower-case word ("assets", "platform").</param>
    /// <param name="message">The message, one line, without the category.</param>
    void Write(LogLevel level, string category, string message);
}

/// <summary>
/// The default <see cref="ILog"/>: each message at or above <see cref="MinimumLevel"/> becomes one line
/// <c>category: message</c> on <see cref="Console.Error"/> (read when the message is written, so a redirected stream
/// receives it) or on the writer it was given.
/// </summary>
public sealed class ConsoleLog(TextWriter? writer = null) : ILog
{
    /// <summary>Messages below this level are dropped (<see cref="LogLevel.Info"/> by default).</summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Info;

    /// <inheritdoc/>
    public void Write(LogLevel level, string category, string message)
    {
        if (level < MinimumLevel) return;
        (writer ?? Console.Error).WriteLine($"{category}: {message}");
    }
}
