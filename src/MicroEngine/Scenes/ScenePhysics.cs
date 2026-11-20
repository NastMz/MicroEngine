using MicroEngine.Kernel;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;

namespace MicroEngine.Scenes;

/// <summary>
/// A scene's physics world (<see cref="Scene.Physics"/>): a scene system that steps the world after the scene's
/// <see cref="Scene.Update"/>, by the same step (the step on the scene world's clock, so slow motion slows physics and
/// a frozen or covered scene's physics stands still), writes the pose of every moving body to its owner's transform,
/// adds the world to the scene's state hash and disposes it when the scene leaves the stack.
/// </summary>
/// <remarks>
/// <para>The backend is whatever <see cref="Core.Game.PhysicsBackend"/> makes, the tile backend by default: a game
/// that wants another one names it there, once, and its scenes do not change. A scene that needs its own world adds
/// the system itself before reading <see cref="Scene.Physics"/>:
/// <c>Systems.Add(new ScenePhysics(world))</c>.</para>
/// <para>Game code moves kinematic bodies, steps character controllers and applies forces in the scene's
/// <see cref="Scene.Update"/>; the world then steps, so its events are ready for the next update.</para>
/// </remarks>
/// <example>
/// <code>
/// public override void Enter()
/// {
///     var level = Physics.World.CreateBody(default, BodyDescription.Static());
///     Physics.World.AddTiles(level, grid, grid.Layer.Width, grid.Layer.Height);
///     _hero = Physics.Track(CharacterController2D.Create(Physics.World, Objects.Create(), start, new Vector2(12, 14)));
/// }
///
/// public override void Update(float dt) =>
///     _hero.Step(dt, new CharacterInput(Input.Axis("left", "right"), Input.Pressed("jump"), Input.Down("jump")));
/// </code>
/// </example>
public sealed class ScenePhysics : SceneSystem
{
    private readonly List<IStateHashed> _tracked = [];

    /// <summary>A scene's physics on the tile backend.</summary>
    public ScenePhysics() : this(new TilePhysicsWorld2D()) { }

    /// <summary>A scene's physics on <paramref name="world"/>, which the scene owns from now on: it is disposed with the scene.</summary>
    public ScenePhysics(IPhysicsWorld2D world)
    {
        ArgumentNullException.ThrowIfNull(world);
        World = world;
    }

    /// <summary>The physics world.</summary>
    public IPhysicsWorld2D World { get; }

    /// <summary>
    /// Whether body poses are written to their owners' transforms after each step (true by default; only when the
    /// scene has <see cref="Scene.Objects"/>). A scene whose bodies are not owned by entities turns it off.
    /// </summary>
    public bool SyncTransforms { get; set; } = true;

    /// <summary>
    /// Adds <paramref name="state"/> (a character controller, anything whose state belongs with the physics) to what
    /// the scene hashes after the world, and returns it.
    /// </summary>
    public T Track<T>(T state) where T : class, IStateHashed
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!_tracked.Contains(state)) _tracked.Add(state);
        return state;
    }

    /// <summary>Stops hashing <paramref name="state"/>; false when it was not tracked.</summary>
    public bool Untrack(IStateHashed state) => _tracked.Remove(state);

    /// <inheritdoc/>
    protected override void LateUpdate(float dt)
    {
        if (dt > 0) World.Step(dt);
        if (SyncTransforms && Scene.HasWorld) PhysicsSync.WriteTransforms(World, Scene.Objects);
    }

    /// <summary>Adds the world's state (when the backend hashes it) and every tracked state, in the order they were tracked.</summary>
    public override void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(World.Backend);
        (World as IStateHashed)?.HashState(hash);
        hash.Add(_tracked.Count);
        foreach (var state in _tracked) state.HashState(hash);
    }

    /// <inheritdoc/>
    protected override void Stop()
    {
        _tracked.Clear();
        (World as IDisposable)?.Dispose();
    }
}
