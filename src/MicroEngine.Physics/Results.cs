using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using MicroEngine.Scenes;

namespace MicroEngine.Physics;

/// <summary>A surface a moving body touched (<see cref="MoveResult.Contacts"/>).</summary>
/// <param name="Shape">The shape touched.</param>
/// <param name="Owner">The entity owning that shape's body.</param>
/// <param name="Normal">The surface normal, unit length, pointing from the surface towards the moving body.</param>
public readonly record struct MoveContact(ShapeId Shape, EntityId Owner, Vector2 Normal);

/// <summary>The contacts of one <see cref="MoveResult"/>, stored inline (no allocation).</summary>
[InlineArray(MoveResult.MaxContacts)]
public struct MoveContactBuffer
{
    private MoveContact _element;
}

/// <summary>
/// What <see cref="IPhysicsWorld2D.Move"/> did: where the body ended, how far it went, and every surface it was stopped
/// by or touches at the end (within the backend's <see cref="IPhysicsWorld2D.Skin"/>), each one with its normal. A
/// kinematic body's contacts and its ground come from here, never from <see cref="IPhysicsWorld2D.ContactEvents"/>.
/// </summary>
public readonly struct MoveResult
{
    /// <summary>The most contacts one result holds; further ones are counted in <see cref="TotalContacts"/> but not kept.</summary>
    public const int MaxContacts = 8;

    /// <summary>
    /// The smallest dot product of a contact's normal with <see cref="IPhysicsWorld2D.Up"/> that counts as ground (about
    /// 45 degrees); a dot product of at most its negation is a ceiling, anything between is a wall.
    /// </summary>
    public const float GroundThreshold = 0.7f;

    private readonly MoveContactBuffer _contacts;
    private readonly int _count;

    /// <summary>Creates a result from the contacts the backend found (the first <see cref="MaxContacts"/> are kept).</summary>
    /// <param name="position">The body's origin after the move.</param>
    /// <param name="delta">The displacement actually applied.</param>
    /// <param name="up">The world's up direction, which tells ground, walls and ceilings apart.</param>
    /// <param name="contacts">The contacts, in the order the backend found them.</param>
    public MoveResult(Vector2 position, Vector2 delta, Vector2 up, ReadOnlySpan<MoveContact> contacts)
    {
        Position = position;
        Delta = delta;
        TotalContacts = contacts.Length;
        int kept = Math.Min(contacts.Length, MaxContacts);
        contacts[..kept].CopyTo(_contacts);
        _count = kept;
        foreach (var c in contacts)
        {
            float d = Vector2.Dot(c.Normal, up);
            if (d >= GroundThreshold)
            {
                if (!OnGround || d > Vector2.Dot(GroundNormal, up)) { GroundNormal = c.Normal; Ground = c.Shape; }
                OnGround = true;
            }
            else if (d <= -GroundThreshold) HitCeiling = true;
            else HitWall = true;
        }
    }

    /// <summary>The body's origin after the move.</summary>
    public Vector2 Position { get; }
    /// <summary>The displacement actually applied (the requested one when nothing was in the way).</summary>
    public Vector2 Delta { get; }
    /// <summary>Whether a contact is ground: its normal points up within <see cref="GroundThreshold"/>.</summary>
    public bool OnGround { get; }
    /// <summary>The most upward ground normal; zero when <see cref="OnGround"/> is false.</summary>
    public Vector2 GroundNormal { get; }
    /// <summary>The ground shape (<see cref="GroundNormal"/>'s); <see cref="ShapeId.None"/> when not on ground.</summary>
    public ShapeId Ground { get; }
    /// <summary>Whether a contact is a ceiling (its normal points down).</summary>
    public bool HitCeiling { get; }
    /// <summary>Whether a contact is a wall (neither ground nor ceiling).</summary>
    public bool HitWall { get; }
    /// <summary>How many contacts the backend found; <see cref="Contacts"/> holds at most <see cref="MaxContacts"/> of them.</summary>
    public int TotalContacts { get; }

    /// <summary>The contacts kept, in the order the backend found them.</summary>
    [UnscopedRef]
    public ReadOnlySpan<MoveContact> Contacts => ((ReadOnlySpan<MoveContact>)_contacts)[.._count];

    /// <inheritdoc/>
    public override string ToString() => $"MoveResult({Position}, delta {Delta}, ground {OnGround}, wall {HitWall}, ceiling {HitCeiling}, {TotalContacts} contacts)";
}

/// <summary>The closest shape a ray hit (<see cref="IPhysicsWorld2D.RayCast"/>).</summary>
/// <param name="Shape">The shape hit.</param>
/// <param name="Owner">The entity owning that shape's body.</param>
/// <param name="Point">Where the ray entered it.</param>
/// <param name="Normal">The surface normal there, unit length, facing the ray's origin.</param>
/// <param name="Fraction">How far along the ray, from 0 (its start) to 1 (its end).</param>
public readonly record struct RayHit(ShapeId Shape, EntityId Owner, Vector2 Point, Vector2 Normal, float Fraction);

/// <summary>Whether a sensor event is an entry or an exit.</summary>
public enum SensorEventKind
{
    /// <summary>The visitor began overlapping the sensor.</summary>
    Enter,
    /// <summary>The visitor stopped overlapping the sensor, or one of the two shapes was destroyed.</summary>
    Exit,
}

/// <summary>
/// A shape entered or left a sensor during the latest <see cref="IPhysicsWorld2D.Step"/>. The ids and owners are copied
/// when the event is made, so an exit caused by destroying a shape still names it.
/// </summary>
/// <param name="Kind">Entry or exit.</param>
/// <param name="Sensor">The sensor shape.</param>
/// <param name="Visitor">The shape that entered or left.</param>
/// <param name="SensorOwner">The entity owning the sensor's body.</param>
/// <param name="VisitorOwner">The entity owning the visitor's body.</param>
public readonly record struct SensorEvent(SensorEventKind Kind, ShapeId Sensor, ShapeId Visitor, EntityId SensorOwner, EntityId VisitorOwner);

/// <summary>Whether a contact event is a begin or an end.</summary>
public enum ContactEventKind
{
    /// <summary>The two shapes began touching.</summary>
    Begin,
    /// <summary>The two shapes stopped touching, or one of them was destroyed.</summary>
    End,
}

/// <summary>
/// Two shapes began or stopped touching during the latest <see cref="IPhysicsWorld2D.Step"/>; only pairs with at least
/// one dynamic body report them (a kinematic body's contacts come from <see cref="MoveResult"/>).
/// </summary>
/// <param name="Kind">Begin or end.</param>
/// <param name="A">The first shape.</param>
/// <param name="B">The second shape.</param>
/// <param name="OwnerA">The entity owning <paramref name="A"/>'s body.</param>
/// <param name="OwnerB">The entity owning <paramref name="B"/>'s body.</param>
/// <param name="Normal">For a begin, the contact normal, unit length, pointing from <paramref name="A"/> to <paramref name="B"/>; zero for an end.</param>
public readonly record struct ContactEvent(ContactEventKind Kind, ShapeId A, ShapeId B, EntityId OwnerA, EntityId OwnerB, Vector2 Normal);
