using MicroEngine.Core.ECS;

namespace MicroEngine.Game.Scenes.Demos.Zelda;

public enum PlayerState
{
    Idle,
    Walking,
    Attacking
}

public struct PlayerComponent : IComponent
{
    public PlayerState State;
    public float Speed;
    public float AttackTimer;
    public float AttackDuration;
    public bool AttackInputLatched; 
}

public struct EnemyComponent : IComponent
{
    public float Speed;
    public float DetectionRadius;
}

public struct HealthComponent : IComponent
{
    public int Current;
    public int Max;
    public float InvulnerabilityTimer;
}

public struct DamageComponent : IComponent
{
    public int Amount;
}

public struct ProjectileComponent : IComponent
{
    public System.Numerics.Vector2 Direction;
    public float Speed;
    public float LifeTime;
}
