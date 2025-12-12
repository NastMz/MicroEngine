namespace MicroEngine.Core.Scenes;

/// <summary>
/// Provides scene navigation capabilities.
/// Allows scenes to navigate without depending on SceneManager directly.
/// This interface follows the Dependency Inversion Principle, enabling scenes
/// to request navigation without knowing about the concrete SceneManager implementation.
/// </summary>
public interface ISceneNavigator
{
    /// <summary>
    /// Pushes a new scene onto the scene stack.
    /// </summary>
    /// <param name="scene">The scene to push.</param>
    void PushScene(Scene scene);

    /// <summary>
    /// Pushes a new scene onto the scene stack with parameters.
    /// </summary>
    /// <param name="scene">The scene to push.</param>
    /// <param name="parameters">Parameters to pass to the new scene.</param>
    void PushScene(Scene scene, SceneParameters parameters);

    /// <summary>
    /// Pops the current scene from the scene stack.
    /// </summary>
    void PopScene();

    /// <summary>
    /// Replaces the current scene with a new one.
    /// </summary>
    /// <param name="scene">The scene to replace with.</param>
    void ReplaceScene(Scene scene);

    /// <summary>
    /// Replaces the current scene with a new one with parameters.
    /// </summary>
    /// <param name="scene">The scene to replace with.</param>
    /// <param name="parameters">Parameters to pass to the new scene.</param>
    void ReplaceScene(Scene scene, SceneParameters parameters);

    /// <summary>
    /// Sets the transition effect to use for scene changes.
    /// </summary>
    /// <param name="effect">The transition effect, or null for no transition.</param>
    void SetTransition(ISceneTransitionEffect? effect);
}
