using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.ECS.Systems;

/// <summary>
/// System that handles collision detection between entities with ColliderComponent.
/// Provides methods to check overlaps, containment, and get bounds.
/// </summary>
public sealed class CollisionSystem : ISystem
{
    /// <summary>
    /// Update method - currently does nothing.
    /// Collision checks are performed through direct method calls.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(World world, float deltaTime)
    {
        // This system doesn't need automatic updates
        // Collision detection is performed manually via CheckOverlaps/ContainsPoint methods
    }

    /// <summary>
    /// Gets the world-space bounds of a collider as a rectangle.
    /// </summary>
    /// <param name="collider">The collider component.</param>
    /// <param name="position">The world position of the entity.</param>
    /// <returns>The bounding rectangle in world space.</returns>
    public Rectangle GetBounds(ColliderComponent collider, Vector2 position)
    {
        var worldPos = new Vector2(position.X + collider.Offset.X, position.Y + collider.Offset.Y);

        return collider.Shape switch
        {
            ColliderShape.Circle => new Rectangle(
                worldPos.X - collider.Size.X,
                worldPos.Y - collider.Size.X,
                collider.Size.X * 2,
                collider.Size.X * 2),
            
            ColliderShape.Rectangle => new Rectangle(
                worldPos.X - collider.Size.X / 2,
                worldPos.Y - collider.Size.Y / 2,
                collider.Size.X,
                collider.Size.Y),
            
            ColliderShape.Point => new Rectangle(worldPos.X, worldPos.Y, 0, 0),
            
            _ => new Rectangle(worldPos.X, worldPos.Y, 0, 0)
        };
    }

    /// <summary>
    /// Checks if two colliders overlap.
    /// </summary>
    /// <param name="colliderA">The first collider.</param>
    /// <param name="positionA">The position of the first entity.</param>
    /// <param name="colliderB">The second collider.</param>
    /// <param name="positionB">The position of the second entity.</param>
    /// <returns>True if the colliders overlap.</returns>
    public bool CheckOverlap(ColliderComponent colliderA, Vector2 positionA, 
                             ColliderComponent colliderB, Vector2 positionB)
    {
        if (!colliderA.Enabled || !colliderB.Enabled)
        {
            return false;
        }

        var centerA = new Vector2(positionA.X + colliderA.Offset.X, positionA.Y + colliderA.Offset.Y);
        var centerB = new Vector2(positionB.X + colliderB.Offset.X, positionB.Y + colliderB.Offset.Y);

        // Circle-Circle
        if (colliderA.Shape == ColliderShape.Circle && colliderB.Shape == ColliderShape.Circle)
        {
            var distance = Vector2.Distance(centerA, centerB);
            return distance < (colliderA.Size.X + colliderB.Size.X);
        }

        // Rectangle-Rectangle (AABB)
        if (colliderA.Shape == ColliderShape.Rectangle && colliderB.Shape == ColliderShape.Rectangle)
        {
            var boundsA = GetBounds(colliderA, positionA);
            var boundsB = GetBounds(colliderB, positionB);

            return boundsA.X < boundsB.X + boundsB.Width &&
                   boundsA.X + boundsA.Width > boundsB.X &&
                   boundsA.Y < boundsB.Y + boundsB.Height &&
                   boundsA.Y + boundsA.Height > boundsB.Y;
        }

        // Point collisions
        if (colliderA.Shape == ColliderShape.Point)
        {
            return ContainsPoint(colliderB, centerA, positionB);
        }

        if (colliderB.Shape == ColliderShape.Point)
        {
            return ContainsPoint(colliderA, centerB, positionA);
        }

        // Circle-Rectangle (simplified with bounding box check)
        return GetBounds(colliderA, positionA).Intersects(GetBounds(colliderB, positionB));
    }

    /// <summary>
    /// Checks if a point is inside a collider.
    /// </summary>
    /// <param name="collider">The collider component.</param>
    /// <param name="point">The point to check.</param>
    /// <param name="position">The position of the entity.</param>
    /// <returns>True if the point is inside the collider.</returns>
    public bool ContainsPoint(ColliderComponent collider, Vector2 point, Vector2 position)
    {
        var center = new Vector2(position.X + collider.Offset.X, position.Y + collider.Offset.Y);

        return collider.Shape switch
        {
            ColliderShape.Circle => Vector2.Distance(point, center) <= collider.Size.X,
            ColliderShape.Rectangle => GetBounds(collider, position).Contains(point),
            ColliderShape.Point => Vector2.Distance(point, center) < 0.01f,
            _ => false
        };
    }

    /// <summary>
    /// Checks all entities with colliders and returns pairs that overlap.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <returns>List of entity pairs that are colliding.</returns>
    public List<(Entity, Entity)> FindAllOverlaps(World world)
    {
        var results = new List<(Entity, Entity)>();
        var entities = world.GetAllEntities().ToList();

        for (int i = 0; i < entities.Count; i++)
        {
            var entityA = entities[i];
            
            if (!world.HasComponent<ColliderComponent>(entityA) ||
                !world.HasComponent<TransformComponent>(entityA))
            {
                continue;
            }

            var colliderA = world.GetComponent<ColliderComponent>(entityA);
            var transformA = world.GetComponent<TransformComponent>(entityA);

            for (int j = i + 1; j < entities.Count; j++)
            {
                var entityB = entities[j];
                
                if (!world.HasComponent<ColliderComponent>(entityB) ||
                    !world.HasComponent<TransformComponent>(entityB))
                {
                    continue;
                }

                var colliderB = world.GetComponent<ColliderComponent>(entityB);
                var transformB = world.GetComponent<TransformComponent>(entityB);

                if (CheckOverlap(colliderA, transformA.Position, colliderB, transformB.Position))
                {
                    results.Add((entityA, entityB));
                }
            }
        }

        return results;
    }
}
