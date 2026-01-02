using System.Numerics;

namespace MicroEngine.Core.Events;

/// <summary>
/// Common game events for entity lifecycle, collisions, and state changes.
/// All events use poolable properties to support zero-allocation event processing.
/// </summary>
public abstract class GameEvent : IEvent
{
    /// <inheritdoc/>
    public DateTime Timestamp { get; protected set; }
    
    /// <inheritdoc/>
    public bool IsHandled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEvent"/> class.
    /// </summary>
    protected GameEvent()
    {
        Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public virtual void Reset()
    {
        Timestamp = DateTime.UtcNow;
        IsHandled = false;
    }
}

/// <summary>
/// Event raised when an entity is created.
/// Poolable: use EventBus.Queue with initializer instead of creating directly.
/// </summary>
public sealed class EntityCreatedEvent : GameEvent
{
    /// <summary>
    /// Gets or sets the ID of the created entity.
    /// </summary>
    public uint EntityId { get; set; }

    /// <summary>
    /// Parameterless constructor for pooling support.
    /// </summary>
    public EntityCreatedEvent()
    {
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        EntityId = 0;
    }
}

/// <summary>
/// Event raised when an entity is destroyed.
/// Poolable: use EventBus.Queue with initializer instead of creating directly.
/// </summary>
public sealed class EntityDestroyedEvent : GameEvent
{
    /// <summary>
    /// Gets or sets the ID of the destroyed entity.
    /// </summary>
    public uint EntityId { get; set; }

    /// <summary>
    /// Parameterless constructor for pooling support.
    /// </summary>
    public EntityDestroyedEvent()
    {
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        EntityId = 0;
    }
}

/// <summary>
/// Event raised when two entities collide.
/// Poolable: use EventBus.Queue with initializer instead of creating directly.
/// </summary>
public sealed class CollisionEvent : GameEvent
{
    /// <summary>
    /// Gets or sets the ID of the first entity in the collision.
    /// </summary>
    public uint EntityA { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the second entity in the collision.
    /// </summary>
    public uint EntityB { get; set; }
    
    /// <summary>
    /// Gets or sets the point in world space where the collision occurred.
    /// </summary>
    public Vector2 CollisionPoint { get; set; }

    /// <summary>
    /// Parameterless constructor for pooling support.
    /// </summary>
    public CollisionEvent()
    {
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        EntityA = 0;
        EntityB = 0;
        CollisionPoint = Vector2.Zero;
    }
}

/// <summary>
/// Event raised when a trigger zone is entered.
/// Poolable: use EventBus.Queue with initializer instead of creating directly.
/// </summary>
public sealed class TriggerEnterEvent : GameEvent
{
    /// <summary>
    /// Gets or sets the ID of the trigger entity.
    /// </summary>
    public uint TriggerEntity { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the entity entering the trigger.
    /// </summary>
    public uint EnteringEntity { get; set; }

    /// <summary>
    /// Parameterless constructor for pooling support.
    /// </summary>
    public TriggerEnterEvent()
    {
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        TriggerEntity = 0;
        EnteringEntity = 0;
    }
}

/// <summary>
/// Event raised when a trigger zone is exited.
/// Poolable: use EventBus.Queue with initializer instead of creating directly.
/// </summary>
public sealed class TriggerExitEvent : GameEvent
{
    /// <summary>
    /// Gets or sets the ID of the trigger entity.
    /// </summary>
    public uint TriggerEntity { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the entity exiting the trigger.
    /// </summary>
    public uint ExitingEntity { get; set; }

    /// <summary>
    /// Parameterless constructor for pooling support.
    /// </summary>
    public TriggerExitEvent()
    {
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        TriggerEntity = 0;
        ExitingEntity = 0;
    }
}
