namespace MicroEngine.Core.Scenes;

/// <summary>
/// Defines the contract for a game scene.
/// Scenes represent different states or levels of the game.
/// </summary>
public interface IScene
{
    /// <summary>
    /// Gets the unique name of the scene.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the scene is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Called when the scene is loaded and initialized.
    /// </summary>
    void OnLoad();

    /// <summary>
    /// Called for fixed timestep updates (physics, deterministic logic).
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed delta time.</param>
    void OnFixedUpdate(float fixedDeltaTime);

    /// <summary>
    /// Called for variable timestep updates (input, animations).
    /// </summary>
    /// <param name="deltaTime">The frame delta time.</param>
    void OnUpdate(float deltaTime);

    /// <summary>
    /// Called to render the scene.
    /// </summary>
    void OnRender();

    /// <summary>
    /// Called when the scene is being unloaded.
    /// </summary>
    void OnUnload();
}
