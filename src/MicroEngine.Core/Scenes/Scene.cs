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
    /// Initializes a new instance of the <see cref="Scene"/> class.
    /// </summary>
    /// <param name="name">The scene name.</param>
    protected Scene(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _isActive = false;
    }

    /// <inheritdoc/>
    public virtual void OnLoad()
    {
        _isActive = true;
    }

    /// <inheritdoc/>
    public virtual void OnFixedUpdate(float fixedDeltaTime)
    {
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
