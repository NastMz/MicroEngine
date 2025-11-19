using MicroEngine.Core.Math;

namespace MicroEngine.Core.Physics;

/// <summary>
/// Provides swept collision detection (Continuous Collision Detection).
/// Prevents tunneling by checking collision along movement path.
/// </summary>
public static class SweptCollision
{
    private const float EPSILON = 0.0001f;

    /// <summary>
    /// Performs swept AABB collision detection.
    /// </summary>
    /// <param name="movingBox">The moving AABB.</param>
    /// <param name="velocity">The velocity of the moving box.</param>
    /// <param name="staticBox">The static AABB.</param>
    /// <returns>Collision information including time of impact.</returns>
    public static CollisionInfo SweptAABB(Rectangle movingBox, Vector2 velocity, Rectangle staticBox)
    {
        // No movement = static check
        if (velocity.X == 0 && velocity.Y == 0)
        {
            return StaticAABB(movingBox, staticBox);
        }

        // Broadphase - expand static box by moving box size
        var broadphaseBox = new Rectangle(
            staticBox.X - movingBox.Width / 2,
            staticBox.Y - movingBox.Height / 2,
            staticBox.Width + movingBox.Width,
            staticBox.Height + movingBox.Height
        );

        var movingCenter = new Vector2(movingBox.X + movingBox.Width / 2, movingBox.Y + movingBox.Height / 2);

        // Check if ray from center hits broadphase box
        var hit = RayVsAABB(movingCenter, velocity, broadphaseBox);

        if (!hit.IsColliding || hit.TimeOfImpact > 1.0f)
        {
            return CollisionInfo.None();
        }

        // Calculate contact point and normal
        var contactPoint = new Vector2(
            movingCenter.X + velocity.X * hit.TimeOfImpact,
            movingCenter.Y + velocity.Y * hit.TimeOfImpact
        );

        // Determine normal based on which face was hit
        var normal = CalculateNormal(contactPoint, staticBox);

        return new CollisionInfo(normal, 0, contactPoint, hit.TimeOfImpact, true);
    }

    /// <summary>
    /// Ray vs AABB intersection test.
    /// </summary>
    /// <param name="origin">Ray origin.</param>
    /// <param name="direction">Ray direction (velocity).</param>
    /// <param name="target">Target AABB.</param>
    /// <returns>Collision info with time of impact.</returns>
    private static CollisionInfo RayVsAABB(Vector2 origin, Vector2 direction, Rectangle target)
    {
        var targetCenter = new Vector2(target.X + target.Width / 2, target.Y + target.Height / 2);
        var targetHalfSize = new Vector2(target.Width / 2, target.Height / 2);

        var min = new Vector2(targetCenter.X - targetHalfSize.X, targetCenter.Y - targetHalfSize.Y);
        var max = new Vector2(targetCenter.X + targetHalfSize.X, targetCenter.Y + targetHalfSize.Y);

        float tNearX, tFarX, tNearY, tFarY;

        // X axis
        if (System.Math.Abs(direction.X) < EPSILON)
        {
            // Ray parallel to YZ plane
            if (origin.X < min.X || origin.X > max.X)
            {
                return CollisionInfo.None();
            }
            tNearX = float.NegativeInfinity;
            tFarX = float.PositiveInfinity;
        }
        else
        {
            tNearX = (min.X - origin.X) / direction.X;
            tFarX = (max.X - origin.X) / direction.X;

            if (tNearX > tFarX)
            {
                (tNearX, tFarX) = (tFarX, tNearX);
            }
        }

        // Y axis
        if (System.Math.Abs(direction.Y) < EPSILON)
        {
            if (origin.Y < min.Y || origin.Y > max.Y)
            {
                return CollisionInfo.None();
            }
            tNearY = float.NegativeInfinity;
            tFarY = float.PositiveInfinity;
        }
        else
        {
            tNearY = (min.Y - origin.Y) / direction.Y;
            tFarY = (max.Y - origin.Y) / direction.Y;

            if (tNearY > tFarY)
            {
                (tNearY, tFarY) = (tFarY, tNearY);
            }
        }

        // Check for intersection
        if (tNearX > tFarY || tNearY > tFarX)
        {
            return CollisionInfo.None();
        }

        var tNear = System.Math.Max(tNearX, tNearY);
        var tFar = System.Math.Min(tFarX, tFarY);

        // Behind ray origin
        if (tFar < 0)
        {
            return CollisionInfo.None();
        }

        var timeOfImpact = tNear < 0 ? 0 : tNear;
        var contactPoint = new Vector2(origin.X + direction.X * timeOfImpact, origin.Y + direction.Y * timeOfImpact);

        return new CollisionInfo(Vector2.Zero, 0, contactPoint, timeOfImpact, true);
    }

    /// <summary>
    /// Static AABB intersection (fallback for zero velocity).
    /// </summary>
    /// <param name="a">First AABB.</param>
    /// <param name="b">Second AABB.</param>
    /// <returns>Collision info if intersecting.</returns>
    private static CollisionInfo StaticAABB(Rectangle a, Rectangle b)
    {
        // Calculate half-sizes and centers
        var aCenter = new Vector2(a.X + a.Width / 2, a.Y + a.Height / 2);
        var bCenter = new Vector2(b.X + b.Width / 2, b.Y + b.Height / 2);

        var aHalf = new Vector2(a.Width / 2, a.Height / 2);
        var bHalf = new Vector2(b.Width / 2, b.Height / 2);

        // Calculate distance between centers
        var delta = new Vector2(bCenter.X - aCenter.X, bCenter.Y - aCenter.Y);

        // Calculate overlap on each axis
        var overlapX = aHalf.X + bHalf.X - System.Math.Abs(delta.X);
        var overlapY = aHalf.Y + bHalf.Y - System.Math.Abs(delta.Y);

        if (overlapX <= 0 || overlapY <= 0)
        {
            return CollisionInfo.None();
        }

        // Determine collision normal (smallest overlap axis)
        Vector2 normal;
        float penetration;

        if (overlapX < overlapY)
        {
            normal = new Vector2(delta.X < 0 ? -1 : 1, 0);
            penetration = overlapX;
        }
        else
        {
            normal = new Vector2(0, delta.Y < 0 ? -1 : 1);
            penetration = overlapY;
        }

        var contactPoint = new Vector2(
            aCenter.X + normal.X * (aHalf.X - penetration / 2),
            aCenter.Y + normal.Y * (aHalf.Y - penetration / 2)
        );

        return new CollisionInfo(normal, penetration, contactPoint, 0, true);
    }

    /// <summary>
    /// Calculates the collision normal based on contact point and AABB.
    /// </summary>
    /// <param name="contactPoint">The contact point.</param>
    /// <param name="box">The AABB.</param>
    /// <returns>The collision normal.</returns>
    private static Vector2 CalculateNormal(Vector2 contactPoint, Rectangle box)
    {
        var center = new Vector2(box.X + box.Width / 2, box.Y + box.Height / 2);
        var delta = new Vector2(contactPoint.X - center.X, contactPoint.Y - center.Y);

        var absX = System.Math.Abs(delta.X);
        var absY = System.Math.Abs(delta.Y);

        if (absX > absY)
        {
            return new Vector2(delta.X > 0 ? 1 : -1, 0);
        }
        else
        {
            return new Vector2(0, delta.Y > 0 ? 1 : -1);
        }
    }
}
