using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MicroEngine.Scenes;

/// <summary>
/// The objects of a scene: entities with an id, a transform (with an optional parent) and typed attachments. The world
/// runs no game logic: it stores, identifies, orders, interpolates and destroys. Game code programs against this
/// interface; the scene that owns the world drives its step and draw hooks (<see cref="SceneWorld"/>).
/// </summary>
/// <remarks>
/// The rules of the contract:
/// <list type="number">
/// <item><b>Ids</b> are generation-checked; a released slot is reused with a new generation. For a stale id
/// <see cref="IsAlive"/> and <see cref="TryGet{T}"/> are false; <see cref="Local"/>, <see cref="Get{T}"/> and
/// <see cref="ParentOf"/> throw <see cref="InvalidOperationException"/> naming the id and why.</item>
/// <item><b>Destroy</b> takes effect at once for queries (<see cref="IsAlive"/> false, <see cref="Count"/> drops, every
/// iteration skips it and its subtree). Its transform and attachments stay readable through its id until the world ends
/// the step; then <see cref="IDestroyedCallback"/> attachments are told in destroy order and the slot is released, never
/// during an iteration. Destroying a destroyed, stale or <see cref="EntityId.None"/> id does nothing.</item>
/// <item><b>Creation</b> takes effect at once: the id is valid and its transform writable. An iteration already running
/// does not visit it; every later one does. There is no spawn queue.</item>
/// <item><b>Iteration</b>: <see cref="All{T}"/> walks the attachments of one type of live entities in
/// attach order, stable under removal. Attachments are keyed by the type argument, not the runtime type. <c>foreach</c>
/// allocates nothing.</item>
/// <item><b>Interpolation</b>: drawing reads <see cref="WorldMatrix"/> or <see cref="DrawPosition2D"/> at the world's
/// <see cref="Blend"/>, from each entity's transform at the start of the step to its current one; at blend 1, or when
/// nothing moved, the result is exactly the simulated transform. A child's matrix is its interpolated local matrix
/// times its parent's.</item>
/// <item><b>Teleport</b>: a new, teleported or re-parented entity is drawn at its current transform until the next step
/// starts; a teleported parent carries its children.</item>
/// <item><b>Step tracking</b>: <see cref="Blend"/> is the engine's blend when this world stepped in the engine's latest
/// fixed step, otherwise 1 (paused, under an overlay, in a transition), so a frozen world is drawn as its last step.</item>
/// <item><b>Clock</b>: <see cref="Clock"/> advances once per step of this world; at scale 1 the scaled step is the engine's
/// step bit for bit.</item>
/// </list>
/// </remarks>
public interface IWorld
{
    /// <summary>Live entities (destroyed ones not counted).</summary>
    int Count { get; }
    /// <summary>The world's clock: scaled time, time scale and freezes (hitstop).</summary>
    WorldClock Clock { get; }
    /// <summary>The blend drawing uses this frame: the engine's, or 1 when this world did not step in the latest engine step.</summary>
    float Blend { get; }

    /// <summary>Creates an entity at <see cref="Transform.Identity"/>, under <paramref name="parent"/> when given.</summary>
    /// <exception cref="InvalidOperationException"><paramref name="parent"/> is stale or destroyed.</exception>
    EntityId Create(EntityId parent = default);
    /// <summary>Whether <paramref name="entity"/> exists and was not destroyed.</summary>
    bool IsAlive(EntityId entity);
    /// <summary>Destroys the entity and its subtree (see the rules); nothing for a destroyed, stale or none id.</summary>
    void Destroy(EntityId entity);
    /// <summary>The parent, or <see cref="EntityId.None"/> for a root.</summary>
    EntityId ParentOf(EntityId entity);
    /// <summary>
    /// Moves the entity under <paramref name="parent"/> (<see cref="EntityId.None"/>: to the root), keeping its local
    /// transform; it is drawn at its current transform until the next step starts.
    /// </summary>
    /// <exception cref="InvalidOperationException">The move would make the entity its own ancestor, or an id is stale or destroyed.</exception>
    void SetParent(EntityId entity, EntityId parent);
    /// <summary>
    /// The live direct children of <paramref name="parent"/>, in the order they were given that parent; <c>foreach</c>
    /// allocates nothing. A child created during the walk is visited by the next one; re-parenting during it throws.
    /// </summary>
    EntityChildren Children(EntityId parent);

    /// <summary>The transform relative to the parent, by reference (write through it).</summary>
    ref Transform Local(EntityId entity);
    /// <summary>The world matrix drawn this frame (at <see cref="Blend"/>).</summary>
    Matrix4x4 WorldMatrix(EntityId entity);
    /// <summary>The world X/Y drawn this frame (at <see cref="Blend"/>); no matrix work for a root or under translation-only parents.</summary>
    Vector2 DrawPosition2D(EntityId entity);
    /// <summary>Draws the entity (and so its children) at its current transform until the next step starts.</summary>
    void Teleport(EntityId entity);

    /// <summary>Attaches <paramref name="attachment"/> under the type <typeparamref name="T"/>; replaces one already there, in its place.</summary>
    /// <exception cref="InvalidOperationException">The entity is stale or destroyed.</exception>
    T Attach<T>(EntityId entity, T attachment) where T : class;
    /// <summary>The attachment of type <typeparamref name="T"/>, if any (also of an entity destroyed this step).</summary>
    bool TryGet<T>(EntityId entity, [NotNullWhen(true)] out T? attachment) where T : class;
    /// <summary>The attachment of type <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidOperationException">There is none, or the id is stale.</exception>
    T Get<T>(EntityId entity) where T : class;
    /// <summary>Removes the attachment of type <typeparamref name="T"/>; false when there was none.</summary>
    bool Detach<T>(EntityId entity) where T : class;
    /// <summary>The attachments of type <typeparamref name="T"/> of live entities, in attach order; <c>foreach</c> allocates nothing.</summary>
    Attachments<T> All<T>() where T : class;
}

/// <summary>An attachment told when its entity is released (at the end of the step its entity was destroyed in).</summary>
public interface IDestroyedCallback
{
    /// <summary>Called once; the entity's transform and attachments are still readable, and it may destroy other entities.</summary>
    void OnDestroyed(EntityId entity);
}
