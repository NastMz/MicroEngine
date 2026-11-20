using Arch.Buffer;
using Arch.Core;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Ecs;

// Inside a namespace under MicroEngine, World alone names the engine's MicroEngine.World namespace.
using World = Arch.Core.World;

/// <summary>
/// A component store that lives in a scene: the rows of an Entity Component System (the Arch library's
/// <see cref="World"/>) created when the scene starts and destroyed when it leaves, with what joins them to the
/// engine. The store is a scene system, so the scene steps it, hashes it and stops it; nothing in the scene's
/// <c>Update</c>, <c>HashState</c> or <c>Exit</c> mentions it. Add one with
/// <see cref="EcsExtensions.AddEcs(Scene, EcsComponents)"/>.
/// </summary>
/// <remarks>
/// <para>The library is not hidden: <see cref="World"/> is Arch's world, and rows, queries and components are used
/// exactly as its documentation says. What the store owns is the integration:</para>
/// <list type="bullet">
/// <item><b>The step.</b> Before the scene's <c>Update</c>: linked rows that follow their entity read its transform,
/// the components marked for it are kept for drawing, then <see cref="Systems"/> run in order. After the scene's
/// <c>Update</c>: <see cref="LateSystems"/> run in order, links whose row or entity is gone are settled, and linked
/// rows write their place to their entity.</item>
/// <item><b>Deferred changes.</b> <see cref="Commands"/> are applied after each system and after the scene's
/// <c>Update</c> (<see cref="Flush"/> applies them at once), so a walk never adds, removes or destroys under itself.</item>
/// <item><b>Links</b> between a row and an entity of the scene's world, found from either side and destroyed together
/// (<see cref="Link"/>).</item>
/// <item><b>Drawing</b> between two steps (<see cref="Interpolated{T}"/>, <see cref="Blend"/>).</item>
/// <item><b>The state hash</b>: the components added as state or as tags, the links and the systems' switches.</item>
/// <item><b>Registration</b>: a row with a component that is not in the store's <see cref="Components"/> fails the step.</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public override void Enter()
/// {
///     var ecs = this.AddEcs(Game1.Components);
///     ecs.Systems.Add(new Movement());
///     ecs.Systems.Add(new Combat());
///     ecs.LateSystems.Add(new Deaths());
///     ecs.World.Create(new Position { Value = start }, new Velocity { Value = speed });
/// }
/// </code>
/// </example>
public sealed class EcsStore : SceneSystem
{
    private static readonly QueryDescription LinkedRows = new QueryDescription().WithAll<SceneLink>();

    static EcsStore() => ArchArrays.Name<SceneLink>();

    private readonly List<(Entity Row, EntityId Entity)> _links = [];
    private readonly IEcsSnapshot[] _snapshots;
    private readonly StateHasher _scratch = new();
    private World? _world;
    private CommandBuffer? _commands;
    private int _archetypesChecked;

    /// <summary>Makes a store for rows of <paramref name="components"/>. It has no rows until its scene is on the stack.</summary>
    public EcsStore(EcsComponents components)
    {
        ArgumentNullException.ThrowIfNull(components);
        Components = components;
        components.Use();
        Systems = new EcsSystems(this, nameof(Systems));
        LateSystems = new EcsSystems(this, nameof(LateSystems));
        _snapshots = [.. components.InterpolatedTypes.Select(entry => entry.CreateInterpolation(this))];
    }

    /// <summary>The component types of the store's rows.</summary>
    public EcsComponents Components { get; }

    /// <summary>Whether the store has its rows: from the moment its scene is on the stack until the scene leaves.</summary>
    public bool IsRunning => _world is not null;

    /// <summary>The rows: the library's world, used as the library documents.</summary>
    /// <exception cref="InvalidOperationException">The store's scene is not on the stack (before it is pushed, after it left).</exception>
    public World World => _world ?? throw new InvalidOperationException(
        $"The component store of {(IsAttached ? Scene.GetType().Name : "a scene")} has rows only while its scene is on the stack: use it from Enter on, and not after the scene left.");

    /// <summary>
    /// Changes to apply when no walk is running: the library's command buffer. Ask it to create, destroy, add and
    /// remove from inside a query; the store applies everything asked when the system that asked returns, and after
    /// the scene's <c>Update</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The store's scene is not on the stack.</exception>
    public CommandBuffer Commands => _commands ?? throw new InvalidOperationException("The store's commands exist only while its scene is on the stack.");

    /// <summary>The systems that run before the scene's <c>Update</c>, in the order they were added.</summary>
    public EcsSystems Systems { get; }

    /// <summary>The systems that run after the scene's <c>Update</c>, in the order they were added: what reacts to the step (deaths, scores, what the camera follows).</summary>
    public EcsSystems LateSystems { get; }

    /// <summary>How many steps the store has taken.</summary>
    internal int StepNumber { get; private set; }

    /// <summary>
    /// Applies what was asked of <see cref="Commands"/> now. The store calls it after each system; a system calls it
    /// between two of its own walks when the second must see what the first asked for. Never call it inside a query.
    /// </summary>
    public void Flush()
    {
        var world = World;
        if (_commands!.Size > 0) _commands.Playback(world, true);
        CheckComponents(world);
    }

    // ------------------------------------------------------------------ the scene's hooks

    /// <inheritdoc/>
    protected override void Start()
    {
        _world = World.Create();
        _commands = new CommandBuffer();
        _archetypesChecked = 0;
        Systems.StartAll();
        LateSystems.StartAll();
    }

    /// <inheritdoc/>
    protected override void Update(float dt)
    {
        var world = World;
        StepNumber++;
        if (_links.Count > 0)
        {
            var objects = Scene.Objects;
            foreach (var synced in Components.SyncedTypes) synced.Pull(world, objects);
        }
        foreach (var snapshot in _snapshots) snapshot.Take(world, StepNumber);
        Systems.Run(dt);
    }

    /// <inheritdoc/>
    protected override void LateUpdate(float dt)
    {
        Flush();   // what the scene's own Update asked for
        LateSystems.Run(dt);
        if (_links.Count == 0) return;
        var world = World;
        var objects = Scene.Objects;
        SettleLinks(world, objects);
        foreach (var synced in Components.SyncedTypes) synced.Push(world, objects);
    }

    /// <inheritdoc/>
    protected override void BeginDraw(float blend) => PrepareDraw(blend);

    internal void PrepareDraw(float blend)
    {
        Blend = blend;
        FramesPrepared++;
    }

    internal int FramesPrepared { get; private set; }

    /// <inheritdoc/>
    protected override void Stop()
    {
        if (_world is not { } world) return;
        if (_links.Count > 0)
        {
            // The scene's entities outlive a store that is removed from a running scene: they keep no way to its rows.
            var objects = Scene.Objects;
            foreach (var (_, entity) in _links)
                if (objects.IsAlive(entity)) objects.Detach<EcsRow>(entity);
            _links.Clear();
        }
        Systems.StopAll();
        LateSystems.StopAll();
        _commands!.Dispose();
        _commands = null;
        _world = null;
        World.Destroy(world);
    }

    // ------------------------------------------------------------------ registration

    private void CheckComponents(World world)
    {
        // The library makes an archetype the first time a row has a combination of components: only then is there
        // anything new to look at.
        var archetypes = world.Archetypes;
        if (archetypes.Count == _archetypesChecked) return;
        foreach (var archetype in archetypes.AsSpan())
            foreach (var component in archetype.Signature.Components)
            {
                var type = component.Type;
                if (type == typeof(SceneLink) || Components.Contains(type)) continue;
                throw new InvalidOperationException(
                    $"A row of {Scene.GetType().Name}'s store has a {type.Name} component, which is not in the store's EcsComponents. " +
                    $"Add it where the components are declared (Add<{type.Name}>(), AddState<{type.Name}>() or AddTag<{type.Name}>()): " +
                    "the store hashes, blends and names for NativeAOT builds only the types it was given.");
            }
        _archetypesChecked = archetypes.Count;
    }

    // ------------------------------------------------------------------ links

    /// <summary>How many rows are linked to an entity of the scene's world.</summary>
    public int LinkCount => _links.Count;

    /// <summary>
    /// Links <paramref name="row"/> to <paramref name="entity"/> of the scene's world, for an object that needs both:
    /// components for the store's systems, and a transform with children, a physics body or a place in a spatial
    /// index. Each is found from the other (<see cref="TryGetEntity"/>, <see cref="TryGetRow"/>), and destroying
    /// either destroys both when the step ends. Most rows need no entity and are never linked.
    /// </summary>
    /// <remarks>Linking adds a <see cref="SceneLink"/> component to the row, so not inside a query.</remarks>
    /// <param name="row">A live row without a link.</param>
    /// <param name="entity">A live entity without a link.</param>
    /// <param name="mode">Which side follows the other's place (<see cref="EcsComponents.SyncTransform{T}"/>).</param>
    /// <exception cref="InvalidOperationException">The row or the entity is gone, or one of them is already linked.</exception>
    public void Link(Entity row, EntityId entity, EcsLink mode = EcsLink.RowMovesEntity)
    {
        var world = World;
        var objects = Scene.Objects;
        if (!world.IsAlive(row)) throw new InvalidOperationException($"{row} is not a live row of this store.");
        if (!objects.IsAlive(entity)) throw new InvalidOperationException($"{entity} is not a live entity of the scene's world.");
        if (world.Has<SceneLink>(row)) throw new InvalidOperationException($"{row} is already linked to {world.Get<SceneLink>(row).Entity}.");
        if (objects.TryGet<EcsRow>(entity, out _)) throw new InvalidOperationException($"{entity} is already linked to a row.");
        world.Add(row, new SceneLink { Entity = entity, Mode = mode });
        objects.Attach(entity, new EcsRow(this, row));
        _links.Add((row, entity));
        CheckComponents(world);
    }

    /// <summary>Ends the link of <paramref name="row"/>, destroying neither side; false when the row is gone or not linked.</summary>
    public bool Unlink(Entity row)
    {
        var world = World;
        if (!world.IsAlive(row) || !world.Has<SceneLink>(row)) return false;
        var entity = world.Get<SceneLink>(row).Entity;
        world.Remove<SceneLink>(row);
        var objects = Scene.Objects;
        if (objects.IsAlive(entity)) objects.Detach<EcsRow>(entity);
        _links.Remove((row, entity));
        return true;
    }

    /// <summary>The entity <paramref name="row"/> is linked to; false for a row that is gone (even when a newer row took its place), not linked, or whose entity was destroyed.</summary>
    public bool TryGetEntity(Entity row, out EntityId entity)
    {
        entity = default;
        var world = World;
        if (_links.Count == 0 || !world.IsAlive(row) || !world.Has<SceneLink>(row)) return false;
        var linked = world.Get<SceneLink>(row).Entity;
        if (!Scene.Objects.IsAlive(linked)) return false;
        entity = linked;
        return true;
    }

    /// <summary>The row <paramref name="entity"/> is linked to; false for an entity that is gone or destroyed, not linked, or whose row is gone.</summary>
    public bool TryGetRow(EntityId entity, out Entity row)
    {
        row = default;
        if (_links.Count == 0) return false;
        var objects = Scene.Objects;
        if (!objects.IsAlive(entity) || !objects.TryGet<EcsRow>(entity, out var attachment) || attachment.Store != this || !World.IsAlive(attachment.Row)) return false;
        row = attachment.Row;
        return true;
    }

    /// <summary>Destroys the other side of every link that lost one, in the order the links were made.</summary>
    private void SettleLinks(World world, IWorld objects)
    {
        int kept = 0;
        for (int i = 0; i < _links.Count; i++)
        {
            var (row, entity) = _links[i];
            bool rowAlive = world.IsAlive(row), entityAlive = objects.IsAlive(entity);
            if (rowAlive && entityAlive && world.Has<SceneLink>(row))
            {
                _links[kept++] = _links[i];
                continue;
            }
            if (rowAlive && entityAlive) objects.Detach<EcsRow>(entity);   // the game removed the link's component itself
            else if (rowAlive) world.Destroy(row);
            else if (entityAlive) objects.Destroy(entity);
        }
        _links.RemoveRange(kept, _links.Count - kept);
    }

    /// <summary>The world released a linked entity (its attachment was told): the row goes too, if it is still this entity's.</summary>
    internal void Released(Entity row, EntityId entity)
    {
        if (_world is not { } world) return;
        _links.Remove((row, entity));
        if (world.IsAlive(row) && world.Has<SceneLink>(row) && world.Get<SceneLink>(row).Entity == entity) world.Destroy(row);
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>
    /// How far the frame being drawn lies between the store's last two steps (0 to 1): the frame's blend while the
    /// scene is stepped, 1 while it is frozen (under an overlay, in a transition) and before the first frame.
    /// </summary>
    public float Blend { get; private set; } = 1f;

    /// <summary>The blended reader of a component type marked with <see cref="EcsComponents.Interpolate{T}"/>. Keep it in a field: a walk that draws many rows asks it for each.</summary>
    /// <exception cref="InvalidOperationException">The type is not marked.</exception>
    public EcsInterpolation<T> Interpolated<T>() where T : struct
    {
        foreach (var snapshot in _snapshots)
            if (snapshot.Type == typeof(T)) return (EcsInterpolation<T>)snapshot;
        throw new InvalidOperationException(
            $"{typeof(T).Name} is not kept for drawing: mark it with Interpolate<{typeof(T).Name}>(...) where the store's EcsComponents are declared.");
    }

    /// <summary>The component of <paramref name="row"/> as this frame draws it. For one row; a walk uses <see cref="Interpolated{T}"/>.</summary>
    public T Blended<T>(Entity row) where T : struct => Interpolated<T>().Blended(row);

    // ------------------------------------------------------------------ the state hash

    /// <summary>
    /// Adds the store's simulation state: how many rows it has, each component type added with <c>AddState</c> or
    /// <c>AddTag</c> (by row, whatever order the library keeps them in), the links, and each system's name, switch
    /// and own state. Nothing drawing writes is in it.
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        if (_world is not { } world) return;
        hash.Add(world.Size);
        foreach (var entry in Components.Hashed) entry.Hash(world, hash, _scratch);

        ulong links = 0;
        foreach (ref var chunk in world.Query(in LinkedRows))
        {
            var linked = chunk.GetSpan<SceneLink>();
            for (int i = 0; i < chunk.Count; i++)
            {
                _scratch.Reset();
                _scratch.Add(chunk.Entity(i).Id);
                _scratch.Add((long)linked[i].Entity.Index << 32 | linked[i].Entity.Generation);
                _scratch.Add((byte)linked[i].Mode);
                links += _scratch.Value;
            }
        }
        hash.Add(_links.Count);
        hash.Add(links);
        Systems.HashState(hash);
        LateSystems.HashState(hash);
    }
}

/// <summary>The store from a scene, and for every scene of a game.</summary>
public static class EcsExtensions
{
    /// <summary>Gives <paramref name="scene"/> a component store for rows of <paramref name="components"/> and returns it. Call it from the scene's constructor or its <c>Enter</c>.</summary>
    /// <exception cref="InvalidOperationException">The scene already has a store.</exception>
    public static EcsStore AddEcs(this Scene scene, EcsComponents components)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return scene.Systems.Add(new EcsStore(components));
    }

    /// <summary>The scene's component store.</summary>
    /// <exception cref="InvalidOperationException">The scene has none.</exception>
    public static EcsStore Ecs(this Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return scene.Systems.TryGet<EcsStore>(out var store)
            ? store
            : throw new InvalidOperationException(
                $"{scene.GetType().Name} has no component store: give it one with AddEcs(components) in its Enter, or every scene one with Scenes.AddEcs(components).");
    }

    /// <summary>
    /// Gives every scene pushed from now on a component store for rows of <paramref name="components"/>, with no code
    /// in the scenes: call it once, from the game's <c>Load</c>. A scene reads its store with <see cref="Ecs"/>.
    /// </summary>
    /// <param name="scenes">The game's scenes.</param>
    /// <param name="components">The component types of the game.</param>
    /// <param name="where">Which scenes get one (a menu or a pause overlay rarely needs it); every scene when null.</param>
    public static void AddEcs(this SceneManager scenes, EcsComponents components, Func<Scene, bool>? where = null)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(components);
        scenes.AddSystem(scene => scene.Systems.Contains<EcsStore>() || (where is not null && !where(scene)) ? null : new EcsStore(components));
    }
}
