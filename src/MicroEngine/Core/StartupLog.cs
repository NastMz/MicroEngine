using MicroEngine.Kernel;

namespace MicroEngine.Core;

/// <summary>
/// The log a platform writes to before the game's engine exists: it keeps the messages until it is attached to the
/// game's <see cref="ILog"/>, then replays them in order and forwards every later one.
/// </summary>
internal sealed class StartupLog : ILog
{
    private readonly List<(LogLevel Level, string Category, string Message)> _pending = [];
    private ILog? _target;
    private readonly Lock _lock = new();

    /// <summary>Whether <see cref="Attach"/> was called.</summary>
    public bool IsAttached => _target is not null;

    public void Write(LogLevel level, string category, string message)
    {
        lock (_lock)
        {
            if (_target is null)
            {
                _pending.Add((level, category, message));
                return;
            }
        }
        _target.Write(level, category, message);
    }

    /// <summary>Sends the kept messages to <paramref name="target"/> and every later one; nothing when already attached.</summary>
    public void Attach(ILog target)
    {
        (LogLevel, string, string)[] pending;
        lock (_lock)
        {
            if (_target is not null) return;
            _target = target;
            pending = [.. _pending];
            _pending.Clear();
        }
        foreach (var (level, category, message) in pending) target.Write(level, category, message);
    }
}
