namespace MicroEngine.Core.Physics;

/// <summary>
/// Pre-defined physics layers for common game scenarios.
/// </summary>
public static class PhysicsLayers
{
    // Layer ID constants
    private const int DEFAULT_ID = 0;
    private const int PLAYER_ID = 1;
    private const int ENEMY_ID = 2;
    private const int ENVIRONMENT_ID = 3;
    private const int PROJECTILE_ID = 4;
    private const int TRIGGER_ID = 5;
    private const int UI_ID = 6;
    private const int PARTICLE_ID = 7;

    // Layer name constants
    private const string DEFAULT_NAME = "Default";
    private const string PLAYER_NAME = "Player";
    private const string ENEMY_NAME = "Enemy";
    private const string ENVIRONMENT_NAME = "Environment";
    private const string PROJECTILE_NAME = "Projectile";
    private const string TRIGGER_NAME = "Trigger";
    private const string UI_NAME = "UI";
    private const string PARTICLE_NAME = "Particle";

    /// <summary>
    /// Default layer (0). Used for objects without a specific layer.
    /// </summary>
    public static readonly CollisionLayer Default = new(DEFAULT_ID, DEFAULT_NAME);

    /// <summary>
    /// Player layer (1). Used for player-controlled entities.
    /// </summary>
    public static readonly CollisionLayer Player = new(PLAYER_ID, PLAYER_NAME);

    /// <summary>
    /// Enemy layer (2). Used for AI-controlled enemies.
    /// </summary>
    public static readonly CollisionLayer Enemy = new(ENEMY_ID, ENEMY_NAME);

    /// <summary>
    /// Environment layer (3). Used for static world geometry (walls, platforms, etc.).
    /// </summary>
    public static readonly CollisionLayer Environment = new(ENVIRONMENT_ID, ENVIRONMENT_NAME);

    /// <summary>
    /// Projectile layer (4). Used for bullets, missiles, etc.
    /// </summary>
    public static readonly CollisionLayer Projectile = new(PROJECTILE_ID, PROJECTILE_NAME);

    /// <summary>
    /// Trigger layer (5). Used for trigger zones and collectibles.
    /// </summary>
    public static readonly CollisionLayer Trigger = new(TRIGGER_ID, TRIGGER_NAME);

    /// <summary>
    /// UI layer (6). Used for UI elements that may need physics interaction.
    /// </summary>
    public static readonly CollisionLayer UI = new(UI_ID, UI_NAME);

    /// <summary>
    /// Particle layer (7). Used for particle effects.
    /// </summary>
    public static readonly CollisionLayer Particle = new(PARTICLE_ID, PARTICLE_NAME);
}
