using MicroEngine.Core.Events;
using MicroEngine.Core.ECS;

namespace MicroEngine.Game.Scenes.Demos.Zelda;

/// <summary>
/// Event raised when an entity takes damage.
/// </summary>
public sealed class DamageEvent : GameEvent
{
    public Entity TargetEntity { get; }
    public int DamageAmount { get; }
    public Entity AttackerEntity { get; }

    public DamageEvent(Entity targetEntity, int damageAmount, Entity attackerEntity = default)
    {
        TargetEntity = targetEntity;
        DamageAmount = damageAmount;
        AttackerEntity = attackerEntity;
    }
}

/// <summary>
/// Event raised when the game state changes (e.g., player death).
/// </summary>
public sealed class ZeldaGameStateEvent : GameEvent
{
    public string Message { get; }
    public bool IsGameOver { get; }

    public ZeldaGameStateEvent(string message, bool isGameOver)
    {
        Message = message;
        IsGameOver = isGameOver;
    }
}
