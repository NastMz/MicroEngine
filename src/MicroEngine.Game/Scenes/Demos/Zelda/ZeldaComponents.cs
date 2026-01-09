using MicroEngine.Core.ECS;

namespace MicroEngine.Game.Scenes.Demos.Zelda;

/// <summary>
/// Describes the current state of the player character.
/// </summary>
public enum PlayerState
{
    /// <summary>
    /// Player is idle.
    /// </summary>
    Idle,

    /// <summary>
    /// Player is walking.
    /// </summary>
    Walking,

    /// <summary>
    /// Player is performing an attack.
    /// </summary>
    Attacking
}

/// <summary>
/// Component storing player-specific state data.
/// </summary>
public struct PlayerComponent : IComponent
{
    /// <summary>
    /// Gets or sets the movement/attack state of the player.
    /// </summary>
    public PlayerState State;

    /// <summary>
    /// Gets or sets the movement speed in units per second.
    /// </summary>
    public float Speed;

    /// <summary>
    /// Gets or sets the elapsed attack timer.
    /// </summary>
    public float AttackTimer;

    /// <summary>
    /// Gets or sets the duration of an attack.
    /// </summary>
    public float AttackDuration;

    /// <summary>
    /// Gets or sets a value indicating whether the attack input has been latched.
    /// </summary>
    public bool AttackInputLatched; 
}

/// <summary>
/// Component storing enemy AI parameters.
/// </summary>
public struct EnemyComponent : IComponent
{
    /// <summary>
    /// Gets or sets the movement speed for the enemy.
    /// </summary>
    public float Speed;

    /// <summary>
    /// Gets or sets the detection radius used to chase the player.
    /// </summary>
    public float DetectionRadius;
}

/// <summary>
/// Component containing health values for an entity.
/// </summary>
public struct HealthComponent : IComponent
{
    /// <summary>
    /// Gets or sets the current health value.
    /// </summary>
    public int Current;

    /// <summary>
    /// Gets or sets the maximum health value.
    /// </summary>
    public int Max;

    /// <summary>
    /// Gets or sets the remaining invulnerability duration after taking damage.
    /// </summary>
    public float InvulnerabilityTimer;
}

/// <summary>
/// Component describing the damage output of an entity.
/// </summary>
public struct DamageComponent : IComponent
{
    /// <summary>
    /// Gets or sets the damage amount inflicted by the entity.
    /// </summary>
    public int Amount;
}

/// <summary>
/// Component describing projectile properties.
/// </summary>
public struct ProjectileComponent : IComponent
{
    /// <summary>
    /// Gets or sets the normalized travel direction of the projectile.
    /// </summary>
    public System.Numerics.Vector2 Direction;

    /// <summary>
    /// Gets or sets the speed of the projectile in units per second.
    /// </summary>
    public float Speed;

    /// <summary>
    /// Gets or sets the remaining lifetime in seconds.
    /// </summary>
    public float LifeTime;
}

/// <summary>
/// Component storing the tilemap collision data for navigation.
/// </summary>
public struct MapComponent : IComponent
{
    /// <summary>
    /// The 2D array of tile types.
    /// </summary>
    public int[,] Tiles;

    /// <summary>
    /// Width of the map in tiles.
    /// </summary>
    public int Width;

    /// <summary>
    /// Height of the map in tiles.
    /// </summary>
    public int Height;

    /// <summary>
    /// Checks if a tile coordinate is passable.
    /// </summary>
    public readonly bool IsPassable(int tx, int ty)
    {
        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height)
        {
            return false;
        }
        // In this demo, tile type 0 is passable (ground)
        return Tiles[ty, tx] == ZeldaConstants.TILE_ID_FLOOR_STONE_VAR1;
    }
}

/// <summary>
/// Component storing audio clip references for an entity.
/// </summary>
public struct AudioComponent : IComponent
{
    /// <summary>
    /// Clip played when the entity attacks.
    /// </summary>
    public MicroEngine.Core.Resources.IAudioClip? AttackClip;

    /// <summary>
    /// Clip played when the entity takes damage.
    /// </summary>
    public MicroEngine.Core.Resources.IAudioClip? HitClip;
}
