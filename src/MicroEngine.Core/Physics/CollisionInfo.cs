using MicroEngine.Core.Math;

namespace MicroEngine.Core.Physics;

/// <summary>
/// Contains information about a collision between two objects.
/// </summary>
public readonly struct CollisionInfo
{
    /// <summary>
    /// Gets the collision normal vector (pointing from B to A).
    /// </summary>
    public Vector2 Normal { get; init; }

    /// <summary>
    /// Gets the penetration depth of the collision.
    /// </summary>
    public float Penetration { get; init; }

    /// <summary>
    /// Gets the contact point in world space.
    /// </summary>
    public Vector2 ContactPoint { get; init; }

    /// <summary>
    /// Gets the time of impact (0-1 range for swept collision).
    /// 0 means collision at start of movement, 1 means at end.
    /// </summary>
    public float TimeOfImpact { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a valid collision.
    /// </summary>
    public bool IsColliding { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionInfo"/> struct.
    /// </summary>
    /// <param name="normal">The collision normal.</param>
    /// <param name="penetration">The penetration depth.</param>
    /// <param name="contactPoint">The contact point.</param>
    /// <param name="timeOfImpact">The time of impact.</param>
    /// <param name="isColliding">Whether collision is valid.</param>
    public CollisionInfo(Vector2 normal, float penetration, Vector2 contactPoint, float timeOfImpact = 1.0f, bool isColliding = true)
    {
        Normal = normal;
        Penetration = penetration;
        ContactPoint = contactPoint;
        TimeOfImpact = timeOfImpact;
        IsColliding = isColliding;
    }

    /// <summary>
    /// Creates a no-collision result.
    /// </summary>
    /// <returns>A CollisionInfo indicating no collision.</returns>
    public static CollisionInfo None() => new CollisionInfo(Vector2.Zero, 0, Vector2.Zero, 1.0f, false);
}
