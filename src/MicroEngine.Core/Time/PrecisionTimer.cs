using System.Diagnostics;

namespace MicroEngine.Core.Time;

/// <summary>
/// High-precision stopwatch wrapper for timing operations.
/// </summary>
public sealed class PrecisionTimer
{
    private readonly Stopwatch _stopwatch;
    private long _lastElapsedTicks;

    /// <summary>
    /// Gets whether the timer is currently running.
    /// </summary>
    public bool IsRunning => _stopwatch.IsRunning;

    /// <summary>
    /// Gets the total elapsed time in seconds.
    /// </summary>
    public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;

    /// <summary>
    /// Gets the total elapsed time in milliseconds.
    /// </summary>
    public double ElapsedMilliseconds => _stopwatch.Elapsed.TotalMilliseconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrecisionTimer"/> class.
    /// </summary>
    public PrecisionTimer()
    {
        _stopwatch = new Stopwatch();
        _lastElapsedTicks = 0;
    }

    /// <summary>
    /// Starts the timer.
    /// </summary>
    public void Start()
    {
        _stopwatch.Start();
    }

    /// <summary>
    /// Stops the timer.
    /// </summary>
    public void Stop()
    {
        _stopwatch.Stop();
    }

    /// <summary>
    /// Resets the timer to zero.
    /// </summary>
    public void Reset()
    {
        _stopwatch.Reset();
        _lastElapsedTicks = 0;
    }

    /// <summary>
    /// Restarts the timer from zero.
    /// </summary>
    public void Restart()
    {
        _stopwatch.Restart();
        _lastElapsedTicks = 0;
    }

    /// <summary>
    /// Gets the time elapsed since the last call to this method in seconds.
    /// </summary>
    /// <returns>Delta time in seconds.</returns>
    public float GetDeltaTime()
    {
        long currentTicks = _stopwatch.ElapsedTicks;
        long deltaTicks = currentTicks - _lastElapsedTicks;
        _lastElapsedTicks = currentTicks;

        return (float)(deltaTicks / (double)Stopwatch.Frequency);
    }
}
