using MicroEngine.Core.ECS;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Demo scene showcasing the ECS system.
/// Creates entities with components and systems that process them.
/// </summary>
public class EcsDemoScene : Scene
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new ECS demo scene.
    /// </summary>
    public EcsDemoScene(ILogger logger) : base("ECS Demo")
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();

        _logger.Info("Scene", "ECS Demo Scene loaded");

        RegisterSystems();
        CreateEntities();
    }

    private void RegisterSystems()
    {
        World.RegisterSystem(new MovementSystem(_logger));
        World.RegisterSystem(new LifetimeSystem(_logger));
    }

    private void CreateEntities()
    {
        CreatePlayer();
        CreateEnemies(3);
        CreateParticles(5);

        _logger.Info("Scene", $"Created {World.EntityCount} entities");
    }

    private void CreatePlayer()
    {
        var player = World.CreateEntity("Player");

        World.AddComponent(player, new PositionComponent { X = 0f, Y = 0f });
        World.AddComponent(player, new VelocityComponent { X = 10f, Y = 5f });
        World.AddComponent(player, new HealthComponent { Current = 100f, Max = 100f });

        _logger.Debug("Scene", "Created player entity");
    }

    private void CreateEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var enemy = World.CreateEntity($"Enemy_{i}");

            World.AddComponent(enemy, new PositionComponent { X = i * 50f, Y = 100f });
            World.AddComponent(enemy, new VelocityComponent { X = -5f, Y = 0f });
            World.AddComponent(enemy, new HealthComponent { Current = 50f, Max = 50f });
        }

        _logger.Debug("Scene", $"Created {count} enemies");
    }

    private void CreateParticles(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var particle = World.CreateEntity($"Particle_{i}");

            World.AddComponent(particle, new PositionComponent
            {
                X = Random.Shared.NextSingle() * 100f,
                Y = Random.Shared.NextSingle() * 100f
            });

            World.AddComponent(particle, new VelocityComponent
            {
                X = (Random.Shared.NextSingle() - 0.5f) * 20f,
                Y = (Random.Shared.NextSingle() - 0.5f) * 20f
            });

            World.AddComponent(particle, new LifetimeComponent
            {
                Remaining = Random.Shared.NextSingle() * 2f + 1f
            });
        }

        _logger.Debug("Scene", $"Created {count} particles");
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        _logger.Info("Scene", "ECS Demo Scene unloaded");
        base.OnUnload();
    }
}

#region Components

#pragma warning disable IDE0044, S1104, CS1591 // Fields in ECS components are public by design

/// <summary>
/// Position component storing 2D coordinates.
/// </summary>
public struct PositionComponent : IComponent
{
    /// <summary>
    /// X coordinate.
    /// </summary>
    public float X;

    /// <summary>
    /// Y coordinate.
    /// </summary>
    public float Y;

    /// <inheritdoc/>
    public override readonly string ToString() => $"({X:F2}, {Y:F2})";
}

/// <summary>
/// Velocity component storing 2D movement speed.
/// </summary>
public struct VelocityComponent : IComponent
{
    /// <summary>
    /// X velocity.
    /// </summary>
    public float X;

    /// <summary>
    /// Y velocity.
    /// </summary>
    public float Y;

    /// <inheritdoc/>
    public override readonly string ToString() => $"({X:F2}, {Y:F2})";
}

/// <summary>
/// Health component for entities that can take damage.
/// </summary>
public struct HealthComponent : IComponent
{
    /// <summary>
    /// Current health value.
    /// </summary>
    public float Current;

    /// <summary>
    /// Maximum health value.
    /// </summary>
    public float Max;

    /// <summary>
    /// Health as percentage (0-1).
    /// </summary>
    public readonly float Percentage => Current / Max;
}

/// <summary>
/// Lifetime component for temporary entities.
/// </summary>
public struct LifetimeComponent : IComponent
{
    /// <summary>
    /// Remaining lifetime in seconds.
    /// </summary>
    public float Remaining;
}

#pragma warning restore IDE0044, S1104, CS1591

#endregion

#region Systems

/// <summary>
/// System that updates position based on velocity.
/// </summary>
public class MovementSystem : ISystem
{
    private readonly ILogger _logger;
    private int _updateCount = 0;

    /// <summary>
    /// Initializes a new movement system.
    /// </summary>
    public MovementSystem(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Update(World world, float deltaTime)
    {
        _updateCount++;

        var entities = world.GetEntitiesWith<PositionComponent>().ToList();

        foreach (var entity in entities)
        {
            if (!world.HasComponent<VelocityComponent>(entity))
            {
                continue;
            }

            ref var position = ref world.GetComponent<PositionComponent>(entity);
            ref var velocity = ref world.GetComponent<VelocityComponent>(entity);

            position.X += velocity.X * deltaTime;
            position.Y += velocity.Y * deltaTime;
        }

        if (_updateCount % 60 == 0)
        {
            _logger.Debug("MovementSystem", $"Updated {entities.Count} entities with velocity");
        }
    }
}

/// <summary>
/// System that manages entity lifetime and destroys expired entities.
/// </summary>
public class LifetimeSystem : ISystem
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new lifetime system.
    /// </summary>
    public LifetimeSystem(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Update(World world, float deltaTime)
    {
        var entities = world.GetEntitiesWith<LifetimeComponent>().ToList();

        foreach (var entity in entities)
        {
            ref var lifetime = ref world.GetComponent<LifetimeComponent>(entity);
            lifetime.Remaining -= deltaTime;

            if (lifetime.Remaining <= 0f)
            {
                var name = world.GetEntityName(entity) ?? entity.ToString();
                _logger.Debug("LifetimeSystem", $"Destroying expired entity: {name}");
                world.DestroyEntity(entity);
            }
        }
    }
}

#endregion
