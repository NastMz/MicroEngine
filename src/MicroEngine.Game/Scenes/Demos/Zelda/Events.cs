using MicroEngine.Core.Events;
using MicroEngine.Core.ECS;

namespace MicroEngine.Game.Scenes.Demos.Zelda;

/// <summary>
/// Event raised when an entity takes damage.
/// </summary>
public sealed class DamageEvent : GameEvent
{
    /// <summary>
    /// Gets or sets the entity that received the damage.
    /// </summary>
    public Entity TargetEntity { get; set; }

    /// <summary>
    /// Gets or sets the damage amount dealt to the target entity.
    /// </summary>
    public int DamageAmount { get; set; }

    /// <summary>
    /// Gets or sets the entity that initiated the damage event.
    /// </summary>
    public Entity AttackerEntity { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DamageEvent"/> class.
    /// </summary>
    public DamageEvent() { }

    /// <summary>
    /// Resets the event data to default values for pooling reuse.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        TargetEntity = default;
        DamageAmount = 0;
        AttackerEntity = default;
    }
}

/// <summary>
/// Event raised when the game state changes (e.g., player death).
/// </summary>
public sealed class ZeldaGameStateEvent : GameEvent
{
    /// <summary>
    /// Gets or sets the message describing the game state change.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the game is over.
    /// </summary>
    public bool IsGameOver { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZeldaGameStateEvent"/> class.
    /// </summary>
    public ZeldaGameStateEvent() { }

    /// <summary>
    /// Resets the game state notification to its default values.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        Message = string.Empty;
        IsGameOver = false;
    }
}
