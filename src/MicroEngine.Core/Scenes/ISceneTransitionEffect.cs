namespace MicroEngine.Core.Scenes;

/// <summary>
/// Defines the interface for scene transition effects.
/// Transitions control visual effects when switching between scenes.
/// </summary>
public interface ISceneTransitionEffect
{
    /// <summary>
    /// Gets a value indicating whether the transition has completed.
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Gets the current progress of the transition (0.0 to 1.0).
    /// </summary>
    float Progress { get; }

    /// <summary>
    /// Starts the transition in the specified direction.
    /// </summary>
    /// <param name="fadeOut">True for fade out (leaving scene), false for fade in (entering scene).</param>
    void Start(bool fadeOut);

    /// <summary>
    /// Updates the transition state.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    void Update(float deltaTime);

    /// <summary>
    /// Renders the transition effect.
    /// Must be called after scene rendering to overlay the effect.
    /// </summary>
    void Render();

    /// <summary>
    /// Resets the transition to its initial state.
    /// </summary>
    void Reset();
}
