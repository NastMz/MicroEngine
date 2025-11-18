using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Helpers;

/// <summary>
/// Factory for creating common entity archetypes.
/// Provides pre-configured entities for typical game objects.
/// </summary>
public static class EntityFactory
{
    // Default sizes
    private const float PLAYER_SIZE = 32f;
    private const float ENEMY_SIZE = 32f;
    private const float OBSTACLE_WIDTH = 50f;
    private const float OBSTACLE_HEIGHT = 70f;
    private const float COLLECTIBLE_RADIUS = 16f;
    private const float PROJECTILE_SIZE = 8f;

    /// <summary>
    /// Creates a player entity with standard components.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">The starting position.</param>
    /// <param name="color">The player color (default: blue).</param>
    /// <returns>The created player entity.</returns>
    public static Entity CreatePlayer(World world, Vector2 position, Color? color = null)
    {
        return new EntityBuilder(world)
            .WithName("Player")
            .WithTransform(position)
            .WithSprite(color ?? Color.Blue)
            .WithRigidBody(mass: 1f, drag: 5f, useGravity: true)
            .WithBoxCollider(PLAYER_SIZE, PLAYER_SIZE)
            .Build();
    }

    /// <summary>
    /// Creates a static platform entity.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">The platform position.</param>
    /// <param name="width">The platform width.</param>
    /// <param name="height">The platform height.</param>
    /// <param name="color">The platform color (default: green).</param>
    /// <returns>The created platform entity.</returns>
    public static Entity CreatePlatform(
        World world,
        Vector2 position,
        float width,
        float height,
        Color? color = null)
    {
        return new EntityBuilder(world)
            .WithName("Platform")
            .WithTransform(position)
            .WithSprite(color ?? Color.Green)
            .WithBoxCollider(width, height)
            .Build();
    }

    /// <summary>
    /// Creates a static obstacle entity.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">The obstacle position.</param>
    /// <param name="color">The obstacle color (default: red).</param>
    /// <returns>The created obstacle entity.</returns>
    public static Entity CreateObstacle(World world, Vector2 position, Color? color = null)
    {
        return new EntityBuilder(world)
            .WithName("Obstacle")
            .WithTransform(position)
            .WithSprite(color ?? Color.Red)
            .WithBoxCollider(OBSTACLE_WIDTH, OBSTACLE_HEIGHT)
            .Build();
    }

    /// <summary>
    /// Creates a collectible trigger entity.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">The collectible position.</param>
    /// <param name="color">The collectible color (default: yellow).</param>
    /// <returns>The created collectible entity.</returns>
    public static Entity CreateCollectible(World world, Vector2 position, Color? color = null)
    {
        return new EntityBuilder(world)
            .WithName("Collectible")
            .WithTransform(position)
            .WithSprite(color ?? Color.Yellow)
            .WithCircleCollider(COLLECTIBLE_RADIUS, isTrigger: true)
            .Build();
    }

    /// <summary>
    /// Creates a projectile entity with velocity.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">The starting position.</param>
    /// <param name="velocity">The projectile velocity.</param>
    /// <param name="color">The projectile color (default: white).</param>
    /// <returns>The created projectile entity.</returns>
    public static Entity CreateProjectile(
        World world,
        Vector2 position,
        Vector2 velocity,
        Color? color = null)
    {
        var entity = new EntityBuilder(world)
            .WithName("Projectile")
            .WithTransform(position)
            .WithSprite(color ?? Color.White)
            .WithRigidBody(mass: 0.1f, useGravity: false, drag: 0f)
            .WithCircleCollider(PROJECTILE_SIZE, isTrigger: true)
            .Build();

        // Set initial velocity
        ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);
        rigidBody.Velocity = velocity;

        return entity;
    }

    /// <summary>
    /// Creates an enemy entity.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">The enemy position.</param>
    /// <param name="color">The enemy color (default: dark red).</param>
    /// <returns>The created enemy entity.</returns>
    public static Entity CreateEnemy(World world, Vector2 position, Color? color = null)
    {
        return new EntityBuilder(world)
            .WithName("Enemy")
            .WithTransform(position)
            .WithSprite(color ?? new Color(180, 0, 0, 255))
            .WithRigidBody(mass: 1f, drag: 3f, useGravity: true)
            .WithBoxCollider(ENEMY_SIZE, ENEMY_SIZE)
            .Build();
    }

    /// <summary>
    /// Creates a small particle entity with velocity.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">The starting position.</param>
    /// <param name="velocity">The particle velocity.</param>
    /// <param name="color">The particle color (default: white).</param>
    /// <returns>The created particle entity.</returns>
    public static Entity CreateParticle(
        World world,
        Vector2 position,
        Vector2 velocity,
        Color? color = null)
    {
        var entity = new EntityBuilder(world)
            .WithName("Particle")
            .WithTransform(position, 0f, new Vector2(0.5f, 0.5f))
            .WithSprite(color ?? Color.White)
            .WithRigidBody(mass: 0.01f, useGravity: true, drag: 1f)
            .Build();

        // Set initial velocity
        ref var rigidBody = ref world.GetComponent<RigidBodyComponent>(entity);
        rigidBody.Velocity = velocity;

        return entity;
    }
}
