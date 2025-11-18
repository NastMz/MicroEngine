using System.Diagnostics;

namespace MicroEngine.Core.Time;

/// <summary>
/// Platform-agnostic time service using .NET's Stopwatch for precise timing.
/// </summary>
/// <remarks>
/// This implementation provides high-resolution timing independent of the rendering backend.
/// It uses Stopwatch for precise frame timing and implements frame rate limiting.
/// Thread-safe for read operations; Update() should be called from the main thread only.
/// </remarks>
public sealed class TimeService : ITimeService
{
    private readonly Stopwatch _stopwatch;
    private readonly Stopwatch _frameStopwatch;
    private long _lastFrameTicks;
    private int _targetFPS;
    private float _deltaTime;
    private int _currentFPS;
    private int _frameCount;
    private double _fpsTimer;

    /// <inheritdoc/>
    public float DeltaTime => _deltaTime;

    /// <inheritdoc/>
    public int CurrentFPS => _currentFPS;

    /// <inheritdoc/>
    public int TargetFPS
    {
        get => _targetFPS;
        set => _targetFPS = value < 0 ? 0 : value;
    }

    /// <inheritdoc/>
    public double TotalTime => _stopwatch.Elapsed.TotalSeconds;

    /// <summary>
    /// Initializes a new instance of the TimeService.
    /// </summary>
    /// <param name="targetFPS">Initial target frames per second (default: 60).</param>
    public TimeService(int targetFPS = 60)
    {
        _stopwatch = new Stopwatch();
        _frameStopwatch = new Stopwatch();
        _targetFPS = targetFPS > 0 ? targetFPS : 60;
        _deltaTime = 0f;
        _currentFPS = 0;
        _frameCount = 0;
        _fpsTimer = 0.0;
        _lastFrameTicks = 0;
    }

    /// <inheritdoc/>
    public void Update()
    {
        // Start stopwatches on first update
        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
            _frameStopwatch.Start();
            _lastFrameTicks = _frameStopwatch.ElapsedTicks;
            _deltaTime = 1f / _targetFPS; // Initial delta assumes target FPS
            return;
        }

        // Calculate delta time from last frame
        long currentTicks = _frameStopwatch.ElapsedTicks;
        long deltaTicks = currentTicks - _lastFrameTicks;
        _deltaTime = (float)(deltaTicks / (double)Stopwatch.Frequency);
        _lastFrameTicks = currentTicks;

        // Update FPS counter
        _frameCount++;
        _fpsTimer += _deltaTime;

        if (_fpsTimer >= 1.0)
        {
            _currentFPS = _frameCount;
            _frameCount = 0;
            _fpsTimer -= 1.0;
        }

        // Frame rate limiting (if target FPS > 0)
        if (_targetFPS > 0)
        {
            double targetFrameTime = 1.0 / _targetFPS;
            double frameTime = _deltaTime;
            double sleepTime = targetFrameTime - frameTime;

            if (sleepTime > 0.001) // Only sleep if we have at least 1ms to spare
            {
                // Use high-precision sleep
                Thread.Sleep(TimeSpan.FromSeconds(sleepTime));

                // Recalculate delta time after sleep
                currentTicks = _frameStopwatch.ElapsedTicks;
                deltaTicks = currentTicks - _lastFrameTicks;
                _deltaTime = (float)(deltaTicks / (double)Stopwatch.Frequency);
                _lastFrameTicks = currentTicks;
            }
        }

        // Clamp delta time to prevent spiral of death
        // Max 100ms (10 FPS) to avoid huge time steps
        if (_deltaTime > 0.1f)
        {
            _deltaTime = 0.1f;
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _stopwatch.Reset();
        _frameStopwatch.Reset();
        _lastFrameTicks = 0;
        _deltaTime = 0f;
        _currentFPS = 0;
        _frameCount = 0;
        _fpsTimer = 0.0;
    }
}
