using MicroEngine.Core.ECS;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Base abstract class for scenes.
/// Provides default implementations for scene lifecycle methods.
/// </summary>
public abstract class Scene : IScene
{
    private bool _isActive;
    private ISceneNavigator? _navigator;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public bool IsActive => _isActive;

    /// <summary>
    /// Gets the ECS world for this scene.
    /// </summary>
    protected World World { get; }

    /// <summary>
    /// Gets the scene context providing access to engine services.
    /// Available after OnLoad is called.
    /// </summary>
    protected SceneContext Context { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scene"/> class.
    /// </summary>
    /// <param name="name">The scene name.</param>
    protected Scene(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _isActive = false;
        World = new World();
    }

    /// <summary>
    /// Internal method to set the scene navigator reference.
    /// Called by SceneManager during scene loading.
    /// </summary>
    internal void SetNavigator(ISceneNavigator navigator)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    /// <summary>
    /// Pushes a new scene onto the scene stack.
    /// </summary>
    /// <param name="scene">The scene to push.</param>
    /// <exception cref="InvalidOperationException">Thrown if navigator is not initialized.</exception>
    protected void PushScene(Scene scene)
    {
        if (_navigator == null)
        {
            throw new InvalidOperationException("Navigator not initialized. This scene has not been loaded yet.");
        }
        
        _navigator.PushScene(scene);
    }

    /// <summary>
    /// Pushes a new scene onto the scene stack with parameters.
    /// </summary>
    /// <param name="scene">The scene to push.</param>
    /// <param name="parameters">Parameters to pass to the new scene.</param>
    /// <exception cref="InvalidOperationException">Thrown if navigator is not initialized.</exception>
    protected void PushScene(Scene scene, SceneParameters parameters)
    {
        if (_navigator == null)
        {
            throw new InvalidOperationException("Navigator not initialized. This scene has not been loaded yet.");
        }
        
        _navigator.PushScene(scene, parameters);
    }

    /// <summary>
    /// Pops the current scene from the scene stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if navigator is not initialized.</exception>
    protected void PopScene()
    {
        if (_navigator == null)
        {
            throw new InvalidOperationException("Navigator not initialized. This scene has not been loaded yet.");
        }
        
        _navigator.PopScene();
    }

    /// <summary>
    /// Replaces the current scene with a new one.
    /// </summary>
    /// <param name="scene">The scene to replace with.</param>
    /// <exception cref="InvalidOperationException">Thrown if navigator is not initialized.</exception>
    protected void ReplaceScene(Scene scene)
    {
        if (_navigator == null)
        {
            throw new InvalidOperationException("Navigator not initialized. This scene has not been loaded yet.");
        }
        
        _navigator.ReplaceScene(scene);
    }

    /// <summary>
    /// Replaces the current scene with a new one with parameters.
    /// </summary>
    /// <param name="scene">The scene to replace with.</param>
    /// <param name="parameters">Parameters to pass to the new scene.</param>
    /// <exception cref="InvalidOperationException">Thrown if navigator is not initialized.</exception>
    protected void ReplaceScene(Scene scene, SceneParameters parameters)
    {
        if (_navigator == null)
        {
            throw new InvalidOperationException("Navigator not initialized. This scene has not been loaded yet.");
        }
        
        _navigator.ReplaceScene(scene, parameters);
    }

    /// <inheritdoc/>
    public virtual void OnLoad(SceneContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _isActive = true;

        // Initialize world with scene services to enable DI for systems
        World.SetServiceProvider(context.Services);

        // Clear World to ensure fresh state on reload
        // This prevents entity accumulation when scenes are cached and reused
        World.Clear();
    }

    /// <inheritdoc/>
    public virtual void OnLoad(SceneContext context, SceneParameters parameters)
    {
        // Default implementation ignores parameters for backward compatibility
        // Derived classes can override to receive parameters
        OnLoad(context);
    }

    /// <inheritdoc/>
    public virtual void OnFixedUpdate(float fixedDeltaTime)
    {
        World.Update(fixedDeltaTime);
    }

    /// <inheritdoc/>
    public virtual void OnUpdate(float deltaTime)
    {
    }

    /// <inheritdoc/>
    public virtual void OnRender()
    {
    }

    /// <inheritdoc/>
    public virtual void OnUnload()
    {
        _isActive = false;
    }
}
