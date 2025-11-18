using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Systems;

/// <summary>
/// System that handles entity movement and transformation.
/// Provides methods to translate, rotate, and scale entities.
/// </summary>
public sealed class MovementSystem : ISystem
{
    /// <summary>
    /// Update method - currently does nothing.
    /// Movement is handled through direct method calls (Translate, Rotate, etc).
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(World world, float deltaTime)
    {
        // This system doesn't need automatic updates
        // Movement is controlled manually via Translate/Rotate/Scale methods
    }

    /// <summary>
    /// Translates an entity's position.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity to translate.</param>
    /// <param name="offset">The offset to add to the position.</param>
    public void Translate(World world, Entity entity, Vector2 offset)
    {
        if (!world.HasComponent<TransformComponent>(entity))
        {
            return;
        }

        ref var transform = ref world.GetComponent<TransformComponent>(entity);
        
        transform.Position = new Vector2(transform.Position.X + offset.X, transform.Position.Y + offset.Y);
    }

    /// <summary>
    /// Rotates an entity.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity to rotate.</param>
    /// <param name="angle">The angle in radians to add to the rotation.</param>
    public void Rotate(World world, Entity entity, float angle)
    {
        if (!world.HasComponent<TransformComponent>(entity))
        {
            return;
        }

        ref var transform = ref world.GetComponent<TransformComponent>(entity);
        
        transform.Rotation = transform.Rotation + angle;
    }

    /// <summary>
    /// Scales an entity.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity to scale.</param>
    /// <param name="factor">The scale factor to multiply with current scale.</param>
    public void ScaleBy(World world, Entity entity, Vector2 factor)
    {
        if (!world.HasComponent<TransformComponent>(entity))
        {
            return;
        }

        ref var transform = ref world.GetComponent<TransformComponent>(entity);
        
        transform.Scale = new Vector2(transform.Scale.X * factor.X, transform.Scale.Y * factor.Y);
    }

    /// <summary>
    /// Gets the forward direction vector for an entity based on its rotation.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity.</param>
    /// <returns>A unit vector pointing in the forward direction, or Zero if no transform.</returns>
    public Vector2 GetForward(World world, Entity entity)
    {
        if (!world.HasComponent<TransformComponent>(entity))
        {
            return Vector2.Zero;
        }

        var transform = world.GetComponent<TransformComponent>(entity);
        
        return new Vector2(
            (float)System.Math.Cos(transform.Rotation),
            (float)System.Math.Sin(transform.Rotation)
        );
    }

    /// <summary>
    /// Gets the right direction vector for an entity based on its rotation.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="entity">The entity.</param>
    /// <returns>A unit vector pointing to the right, or Zero if no transform.</returns>
    public Vector2 GetRight(World world, Entity entity)
    {
        if (!world.HasComponent<TransformComponent>(entity))
        {
            return Vector2.Zero;
        }

        var transform = world.GetComponent<TransformComponent>(entity);
        
        return new Vector2(
            (float)System.Math.Cos(transform.Rotation + System.Math.PI / 2),
            (float)System.Math.Sin(transform.Rotation + System.Math.PI / 2)
        );
    }
}
