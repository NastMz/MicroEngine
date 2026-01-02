using MicroEngine.Core.Events;
using MicroEngine.Core.ECS;

namespace MicroEngine.Game.Scenes.Demos.Zelda;

/// <summary>
/// Event raised when an entity takes damage.
/// </summary>
public sealed class DamageEvent : GameEvent
{
    public Entity TargetEntity { get; set; }
    public int DamageAmount { get; set; }
    public Entity AttackerEntity { get; set; }

    public DamageEvent() { }

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
    public string Message { get; set; } = string.Empty;
    public bool IsGameOver { get; set; }

    public ZeldaGameStateEvent() { }

    public override void Reset()
    {
        base.Reset();
        Message = string.Empty;
        IsGameOver = false;
    }
}
