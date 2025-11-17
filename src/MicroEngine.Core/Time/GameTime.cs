namespace MicroEngine.Core.Time;

/// <summary>
/// Manages time-related values for the engine.
/// Provides delta time, total elapsed time, and frame tracking.
/// </summary>
public sealed class GameTime
{
    private long _frameCount;
    private double _totalElapsedSeconds;

    /// <summary>
    /// Gets the time elapsed since the last update in seconds.
    /// </summary>
    public float DeltaTime { get; private set; }

    /// <summary>
    /// Gets the total time elapsed since the engine started in seconds.
    /// </summary>
    public double TotalElapsedTime => _totalElapsedSeconds;

    /// <summary>
    /// Gets the total number of frames that have been processed.
    /// </summary>
    public long FrameCount => _frameCount;

    /// <summary>
    /// Gets the target fixed timestep in seconds.
    /// Used for physics and deterministic updates.
    /// </summary>
    public float FixedDeltaTime { get; init; }

    /// <summary>
    /// Gets the time scale multiplier.
    /// 1.0 = normal speed, 0.5 = half speed, 2.0 = double speed.
    /// </summary>
    public float TimeScale { get; set; }

    /// <summary>
    /// Gets the scaled delta time (DeltaTime * TimeScale).
    /// </summary>
    public float ScaledDeltaTime => DeltaTime * TimeScale;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameTime"/> class.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed timestep for deterministic updates.</param>
    public GameTime(float fixedDeltaTime = 1f / 60f)
    {
        FixedDeltaTime = fixedDeltaTime;
        TimeScale = 1f;
        DeltaTime = 0f;
        _totalElapsedSeconds = 0.0;
        _frameCount = 0;
    }

    /// <summary>
    /// Updates the time values.
    /// Should be called once per frame by the engine.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame in seconds.</param>
    internal void Update(float deltaTime)
    {
        DeltaTime = deltaTime;
        _totalElapsedSeconds += deltaTime;
        _frameCount++;
    }

    /// <summary>
    /// Resets all time values to their initial state.
    /// </summary>
    internal void Reset()
    {
        DeltaTime = 0f;
        _totalElapsedSeconds = 0.0;
        _frameCount = 0;
    }
}
