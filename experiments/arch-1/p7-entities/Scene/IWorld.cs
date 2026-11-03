using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Arch1.Scene;

/// <summary>
/// The objects of a scene: entities with an id, a transform (with an optional parent) and typed attachments. The world
/// runs no game logic: it stores, identifies, orders, interpolates and destroys. Game code reaches it through this
/// interface; the scene module drives the step and draw hooks of <see cref="World"/>.
/// </summary>
/// <remarks>
/// Rules (the P7 candidate for the stage 3 contract):
/// <list type="bullet">
/// <item>Ids: generation-checked; an id of a released entity is stale: <see cref="IsAlive"/> and
/// <see cref="TryGet{T}"/> are false, <see cref="Local"/> and <see cref="Get{T}"/> throw.</item>
/// <item>Destroy: at once for queries (<see cref="IsAlive"/> false, every iteration skips it, children included);
/// its transform and attachments stay readable through its id until the world ends the step, when
/// <see cref="IDestroyedCallback"/> attachments are told (in destroy order) and the slot is released. Destroying a
/// destroyed or stale id does nothing.</item>
/// <item>Iteration (<see cref="All{T}"/>): attach order of <typeparamref name="T"/>, stable across destruction; an
/// iteration visits the attachments that existed when it started and skips those destroyed meanwhile; entities
/// created during it are visited by the next one. Attachments are keyed by the type argument, not the runtime type.</item>
/// <item>Interpolation: the world keeps each entity's transform at the start of the step; drawing reads
/// <see cref="WorldMatrix(EntityId)"/> or <see cref="DrawPosition2D(EntityId)"/>, blended by <see cref="Blend"/>,
/// which is 1 when this world did not step in the engine's latest fixed step (paused, under an overlay, in a
/// transition). At blend 1 the drawn transform is exactly the simulated one. A new or <see cref="Teleport"/>ed entity
/// is drawn at its current transform until the next step starts.</item>
/// <item>Hierarchy: optional; a child's matrix is its local matrix times its parent's. Root entities pay one branch.</item>
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
    EntityId Create(EntityId parent = default);
    /// <summary>Whether <paramref name="entity"/> exists and was not destroyed.</summary>
    bool IsAlive(EntityId entity);
    /// <summary>Destroys the entity and its children (see the rules).</summary>
    void Destroy(EntityId entity);
    /// <summary>The parent, or <see cref="EntityId.None"/> for a root.</summary>
    EntityId ParentOf(EntityId entity);
    /// <summary>Moves the entity under <paramref name="parent"/> (None: to the root), keeping its local transform.</summary>
    void SetParent(EntityId entity, EntityId parent);

    /// <summary>The transform relative to the parent, by reference (write through it).</summary>
    ref Transform Local(EntityId entity);
    /// <summary>The world matrix drawn this frame (at <see cref="Blend"/>).</summary>
    Matrix4x4 WorldMatrix(EntityId entity);
    /// <summary>The world matrix <paramref name="blend"/> of the way from the step's start to now.</summary>
    Matrix4x4 WorldMatrix(EntityId entity, float blend);
    /// <summary>The world X/Y drawn this frame (at <see cref="Blend"/>); no matrix work for a root entity.</summary>
    Vector2 DrawPosition2D(EntityId entity);
    /// <summary>The world X/Y <paramref name="blend"/> of the way from the step's start to now.</summary>
    Vector2 DrawPosition2D(EntityId entity, float blend);
    /// <summary>Draws the entity (and so its children) at its current transform until the next step starts.</summary>
    void Teleport(EntityId entity);

    /// <summary>Attaches <paramref name="attachment"/> under the type <typeparamref name="T"/>; replaces one already there.</summary>
    T Attach<T>(EntityId entity, T attachment) where T : class;
    /// <summary>The attachment of type <typeparamref name="T"/>, if any.</summary>
    bool TryGet<T>(EntityId entity, [NotNullWhen(true)] out T? attachment) where T : class;
    /// <summary>The attachment of type <typeparamref name="T"/>; throws when there is none or the id is stale.</summary>
    T Get<T>(EntityId entity) where T : class;
    /// <summary>Removes the attachment of type <typeparamref name="T"/>; false when there was none.</summary>
    bool Detach<T>(EntityId entity) where T : class;
    /// <summary>The attachments of type <typeparamref name="T"/> of live entities, in attach order; <c>foreach</c> allocates nothing.</summary>
    Attachments<T> All<T>() where T : class;
}

/// <summary>An attachment told when its entity is released (at the end of the step its entity was destroyed in).</summary>
public interface IDestroyedCallback
{
    /// <summary>Called once; the entity's transform and attachments are still readable.</summary>
    void OnDestroyed(EntityId entity);
}

/// <summary>Where attachments live; P7 measures both designs (and the order-breaking swap-remove variant).</summary>
public enum AttachmentStorage
{
    /// <summary>One dense array per type with a sparse slot index; removals compact in place (attach order kept).</summary>
    DenseStable,
    /// <summary>As <see cref="DenseStable"/>, but a removal moves the last element into the hole (attach order lost).</summary>
    DenseSwapRemove,
    /// <summary>One <c>Dictionary&lt;Type, object&gt;</c> per entity; iteration walks every entity in creation order.</summary>
    PerEntityDictionary,
}
