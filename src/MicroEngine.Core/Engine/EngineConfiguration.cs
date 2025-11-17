namespace MicroEngine.Core.Engine;

/// <summary>
/// Configuration options for the game engine.
/// </summary>
public sealed class EngineConfiguration
{
    /// <summary>
    /// Gets the target frames per second for rendering.
    /// </summary>
    public int TargetFPS { get; init; } = 60;

    /// <summary>
    /// Gets the fixed timestep for physics and deterministic updates (in seconds).
    /// </summary>
    public float FixedTimeStep { get; init; } = 1f / 60f;

    /// <summary>
    /// Gets the maximum number of fixed updates per frame.
    /// Prevents spiral of death in case the game falls behind.
    /// </summary>
    public int MaxFixedUpdatesPerFrame { get; init; } = 5;

    /// <summary>
    /// Gets whether vertical sync is enabled.
    /// </summary>
    public bool VSync { get; init; } = true;

    /// <summary>
    /// Gets the initial window width.
    /// </summary>
    public int WindowWidth { get; init; } = 1280;

    /// <summary>
    /// Gets the initial window height.
    /// </summary>
    public int WindowHeight { get; init; } = 720;

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string WindowTitle { get; init; } = "MicroEngine Game";

    /// <summary>
    /// Gets whether the game starts in fullscreen mode.
    /// </summary>
    public bool Fullscreen { get; init; } = false;

    /// <summary>
    /// Gets whether the window is resizable.
    /// </summary>
    public bool Resizable { get; init; } = true;

    /// <summary>
    /// Validates the configuration values.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (TargetFPS <= 0)
        {
            throw new InvalidOperationException($"TargetFPS must be greater than 0, got {TargetFPS}");
        }

        if (FixedTimeStep <= 0)
        {
            throw new InvalidOperationException($"FixedTimeStep must be greater than 0, got {FixedTimeStep}");
        }

        if (MaxFixedUpdatesPerFrame <= 0)
        {
            throw new InvalidOperationException($"MaxFixedUpdatesPerFrame must be greater than 0, got {MaxFixedUpdatesPerFrame}");
        }

        if (WindowWidth <= 0)
        {
            throw new InvalidOperationException($"WindowWidth must be greater than 0, got {WindowWidth}");
        }

        if (WindowHeight <= 0)
        {
            throw new InvalidOperationException($"WindowHeight must be greater than 0, got {WindowHeight}");
        }

        if (string.IsNullOrWhiteSpace(WindowTitle))
        {
            throw new InvalidOperationException("WindowTitle cannot be null or empty");
        }
    }
}
