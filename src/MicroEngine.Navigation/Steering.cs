using System.Numerics;

namespace MicroEngine.Navigation;

/// <summary>
/// The steering rules <see cref="NavAgents"/> is made of, for games that move things their own way (objects of an
/// entity store, creatures that fly, a platformer's enemies): each takes where things are and returns a velocity or
/// a push to add to it. They keep nothing between calls and use only sums, products and square roots, so the same
/// inputs give the same result on every machine.
/// </summary>
/// <example>
/// <code>
/// // A fish that swims to the bait and keeps clear of the others.
/// var wanted = Steering.Arrive(fish.Position, bait, fish.Speed, slowingDistance: 24);
/// foreach (var other in nearby)
///     wanted += fish.Speed * Steering.Separate(fish.Position, fish.Radius, other.Position, other.Radius, distance: 8);
/// fish.Velocity = Steering.Limit(wanted, fish.Speed);
/// </code>
/// </example>
public static class Steering
{
    /// <summary>The velocity that heads straight for <paramref name="target"/> at <paramref name="speed"/>; zero when already there.</summary>
    public static Vector2 Seek(Vector2 position, Vector2 target, float speed)
    {
        var to = target - position;
        float distance = to.Length();
        return distance > 0 ? to / distance * speed : Vector2.Zero;
    }

    /// <summary>The velocity that heads straight away from <paramref name="threat"/> at <paramref name="speed"/>; zero when on it.</summary>
    public static Vector2 Flee(Vector2 position, Vector2 threat, float speed) => -Seek(position, threat, speed);

    /// <summary>
    /// The velocity that heads for <paramref name="target"/> and slows down over the last
    /// <paramref name="slowingDistance"/> units, in proportion to what is left, so the body comes to rest on the
    /// target instead of running through it. With a slowing distance of 0 it is <see cref="Seek"/>.
    /// </summary>
    public static Vector2 Arrive(Vector2 position, Vector2 target, float speed, float slowingDistance)
    {
        var to = target - position;
        float distance = to.Length();
        if (!(distance > 0)) return Vector2.Zero;
        float wanted = distance < slowingDistance ? speed * (distance / slowingDistance) : speed;
        return to / distance * wanted;
    }

    /// <summary>
    /// The push that keeps a body away from one neighbour: a direction away from it whose length grows from 0, when
    /// the gap between the two bodies is <paramref name="distance"/> or more, to 1 when they touch. Add the pushes
    /// of every neighbour, multiply by a speed and add the result to the velocity wanted.
    /// </summary>
    /// <param name="position">The body's centre.</param>
    /// <param name="radius">The body's radius.</param>
    /// <param name="other">The neighbour's centre.</param>
    /// <param name="otherRadius">The neighbour's radius.</param>
    /// <param name="distance">The gap the bodies try to keep between them; positive.</param>
    public static Vector2 Separate(Vector2 position, float radius, Vector2 other, float otherRadius, float distance)
    {
        var away = position - other;
        float between = away.Length(), gap = between - radius - otherRadius;
        if (!(gap < distance) || !(distance > 0)) return Vector2.Zero;
        float push = gap <= 0 ? 1 : 1 - gap / distance;
        // On the very same spot there is no direction to take: along X, for both. The game parts them (by their order,
        // or by not letting bodies overlap, as NavAgents does).
        if (!(between > 0)) return new Vector2(push, 0);
        return away / between * push;
    }

    /// <summary>
    /// The push that turns a moving body aside from one neighbour it is about to run into: zero unless, both keeping
    /// their velocities, the two bodies would touch within <paramref name="horizon"/> seconds; otherwise a direction
    /// away from where the neighbour will be at their closest, longer the sooner that is (up to 1). Two bodies that
    /// meet head-on each turn to their own right, so they pass. Add the pushes of the neighbours, multiply by a speed
    /// and add the result to the velocity wanted.
    /// </summary>
    /// <param name="position">The body's centre.</param>
    /// <param name="velocity">The velocity the body wants.</param>
    /// <param name="radius">The body's radius.</param>
    /// <param name="other">The neighbour's centre.</param>
    /// <param name="otherVelocity">The neighbour's velocity.</param>
    /// <param name="otherRadius">The neighbour's radius.</param>
    /// <param name="horizon">How many seconds ahead a meeting matters; positive.</param>
    public static Vector2 Avoid(Vector2 position, Vector2 velocity, float radius, Vector2 other, Vector2 otherVelocity, float otherRadius, float horizon)
    {
        var offset = other - position;
        var closing = velocity - otherVelocity;
        float speed = closing.LengthSquared();
        if (!(speed > 1e-12f) || !(horizon > 0)) return Vector2.Zero;
        // When the two are closest, and how far apart then.
        float time = Vector2.Dot(offset, closing) / speed;
        if (!(time > 0) || time >= horizon) return Vector2.Zero;
        var miss = offset - closing * time;
        float reach = radius + otherRadius, missed = miss.Length();
        if (missed >= reach) return Vector2.Zero;
        float urgency = 1 - time / horizon;
        // Dead ahead: across the way, to the body's right. Otherwise away from the side the neighbour will be on.
        if (missed < reach * 1e-3f)
        {
            float length = MathF.Sqrt(speed);
            return new Vector2(-closing.Y, closing.X) / length * urgency;
        }
        return miss / missed * -urgency;
    }

    /// <summary><paramref name="velocity"/>, shortened to <paramref name="speed"/> when it is faster.</summary>
    public static Vector2 Limit(Vector2 velocity, float speed)
    {
        float length = velocity.LengthSquared();
        return length > speed * speed && length > 0 ? velocity * (speed / MathF.Sqrt(length)) : velocity;
    }
}
