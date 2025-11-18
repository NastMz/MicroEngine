namespace MicroEngine.Core.Time;

/// <summary>
/// Service responsible for time management and frame timing in the engine.
/// Provides delta time, FPS tracking, and frame rate control.
/// </summary>
/// <remarks>
/// This service decouples timing from the rendering backend, following
/// the principle that the engine controls time flow, not individual backends.
/// Implementations should use platform-agnostic timing mechanisms.
/// </remarks>
public interface ITimeService
{
    /// <summary>
    /// Gets the time elapsed since the last frame in seconds.
    /// </summary>
    /// <remarks>
    /// This is the primary value used for frame-rate independent movement and animations.
    /// Typical values range from 0.016s (60 FPS) to 0.033s (30 FPS).
    /// </remarks>
    float DeltaTime { get; }

    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    /// <remarks>
    /// This is calculated based on actual frame times, not the target FPS.
    /// Useful for debugging and performance monitoring.
    /// </remarks>
    int CurrentFPS { get; }

    /// <summary>
    /// Gets or sets the target frames per second.
    /// </summary>
    /// <remarks>
    /// The engine will attempt to maintain this frame rate by controlling
    /// frame timing. Set to 0 for unlimited FPS (not recommended for games).
    /// Common values: 30, 60, 120, 144.
    /// </remarks>
    int TargetFPS { get; set; }

    /// <summary>
    /// Gets the total time elapsed since the engine started, in seconds.
    /// </summary>
    /// <remarks>
    /// Useful for time-based effects, animations, and gameplay timers.
    /// This value continuously increases and is not affected by frame rate.
    /// </remarks>
    double TotalTime { get; }

    /// <summary>
    /// Updates the time service, calculating delta time and managing frame rate.
    /// </summary>
    /// <remarks>
    /// This should be called once per frame at the beginning of the game loop,
    /// before any Update() calls that depend on DeltaTime.
    /// </remarks>
    void Update();

    /// <summary>
    /// Resets the time service to its initial state.
    /// </summary>
    /// <remarks>
    /// Useful for scene transitions or when restarting game logic.
    /// Resets TotalTime to 0 and clears frame timing history.
    /// </remarks>
    void Reset();
}
