using MicroEngine.Core.ECS;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Base abstract class for scenes.
/// Provides default implementations for scene lifecycle methods.
/// </summary>
public abstract class Scene : IScene
{
    private bool _isActive;

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

    /// <inheritdoc/>
    public virtual void OnLoad(SceneContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _isActive = true;
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
