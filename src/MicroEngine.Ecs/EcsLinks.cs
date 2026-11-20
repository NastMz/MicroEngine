using Arch.Core;
using MicroEngine.Scenes;

namespace MicroEngine.Ecs;

/// <summary>Which side of a link is moved by the game, and so which side follows (<see cref="EcsComponents.SyncTransform{T}"/>).</summary>
public enum EcsLink : byte
{
    /// <summary>The row is moved by the store's systems; when a step ends its place is written to the entity's transform.</summary>
    RowMovesEntity,
    /// <summary>The entity is moved by something else (a physics body, a tween, the scene); when a step starts its transform is read into the row.</summary>
    EntityMovesRow,
    /// <summary>Nothing is copied: the two only find each other and are destroyed together.</summary>
    Unsynced,
}

/// <summary>
/// The component of a row that is linked to an entity of the scene's world (<see cref="EcsStore.Link"/>). A query can
/// ask for it to walk the linked rows. The store adds and removes it; a game reads it.
/// </summary>
public struct SceneLink
{
    /// <summary>The entity of the scene's world.</summary>
    public EntityId Entity;
    /// <summary>Which side follows the other.</summary>
    public EcsLink Mode;
}

/// <summary>
/// The attachment of a scene entity that is linked to a row (<see cref="EcsStore.Link"/>): the way from the entity to
/// its row. The world tells it when the entity is released, and the row goes with it.
/// </summary>
public sealed class EcsRow : IDestroyedCallback
{
    internal EcsRow(EcsStore store, Entity row) => (Store, Row) = (store, row);

    /// <summary>The store the row is in.</summary>
    public EcsStore Store { get; }
    /// <summary>The row. Ask <see cref="EcsStore.TryGetRow"/> rather than trusting it: the row may be gone.</summary>
    public Entity Row { get; }

    /// <inheritdoc/>
    public void OnDestroyed(EntityId entity) => Store.Released(Row, entity);
}
