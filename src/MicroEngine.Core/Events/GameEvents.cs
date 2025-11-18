using System.Numerics;

namespace MicroEngine.Core.Events;

/// <summary>
/// Common game events for entity lifecycle, collisions, and state changes.
/// </summary>
public abstract class GameEvent : IEvent
{
    /// <inheritdoc/>
    public DateTime Timestamp { get; }
    
    /// <inheritdoc/>
    public bool IsHandled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEvent"/> class.
    /// </summary>
    protected GameEvent()
    {
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event raised when an entity is created.
/// </summary>
public sealed class EntityCreatedEvent : GameEvent
{
    /// <summary>
    /// Gets the ID of the created entity.
    /// </summary>
    public uint EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityCreatedEvent"/> class.
    /// </summary>
    public EntityCreatedEvent(uint entityId)
    {
        EntityId = entityId;
    }
}

/// <summary>
/// Event raised when an entity is destroyed.
/// </summary>
public sealed class EntityDestroyedEvent : GameEvent
{
    /// <summary>
    /// Gets the ID of the destroyed entity.
    /// </summary>
    public uint EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityDestroyedEvent"/> class.
    /// </summary>
    public EntityDestroyedEvent(uint entityId)
    {
        EntityId = entityId;
    }
}

/// <summary>
/// Event raised when two entities collide.
/// </summary>
public sealed class CollisionEvent : GameEvent
{
    /// <summary>
    /// Gets the ID of the first entity in the collision.
    /// </summary>
    public uint EntityA { get; }
    
    /// <summary>
    /// Gets the ID of the second entity in the collision.
    /// </summary>
    public uint EntityB { get; }
    
    /// <summary>
    /// Gets the point in world space where the collision occurred.
    /// </summary>
    public Vector2 CollisionPoint { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionEvent"/> class.
    /// </summary>
    public CollisionEvent(uint entityA, uint entityB, Vector2 collisionPoint)
    {
        EntityA = entityA;
        EntityB = entityB;
        CollisionPoint = collisionPoint;
    }
}

/// <summary>
/// Event raised when a trigger zone is entered.
/// </summary>
public sealed class TriggerEnterEvent : GameEvent
{
    /// <summary>
    /// Gets the ID of the trigger entity.
    /// </summary>
    public uint TriggerEntity { get; }
    
    /// <summary>
    /// Gets the ID of the entity entering the trigger.
    /// </summary>
    public uint EnteringEntity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerEnterEvent"/> class.
    /// </summary>
    public TriggerEnterEvent(uint triggerEntity, uint enteringEntity)
    {
        TriggerEntity = triggerEntity;
        EnteringEntity = enteringEntity;
    }
}

/// <summary>
/// Event raised when a trigger zone is exited.
/// </summary>
public sealed class TriggerExitEvent : GameEvent
{
    /// <summary>
    /// Gets the ID of the trigger entity.
    /// </summary>
    public uint TriggerEntity { get; }
    
    /// <summary>
    /// Gets the ID of the entity exiting the trigger.
    /// </summary>
    public uint ExitingEntity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerExitEvent"/> class.
    /// </summary>
    public TriggerExitEvent(uint triggerEntity, uint exitingEntity)
    {
        TriggerEntity = triggerEntity;
        ExitingEntity = exitingEntity;
    }
}
