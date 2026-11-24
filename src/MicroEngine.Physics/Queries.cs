using System.Numerics;
using MicroEngine.Scenes;

namespace MicroEngine.Physics;

/// <summary>The geometry of a <see cref="QueryShape"/>.</summary>
public enum QueryShapeKind
{
    /// <summary>An axis-aligned box.</summary>
    Box,
    /// <summary>A circle.</summary>
    Circle,
    /// <summary>A capsule that stands along <see cref="IPhysicsWorld2D.Up"/>.</summary>
    Capsule,
}

/// <summary>
/// A shape a query asks with (<see cref="IPhysicsWorld2D.ShapeCast"/>, <see cref="IPhysicsWorld2D.Overlap(in QueryShape, Vector2, in QueryFilter, Span{ShapeId})"/>):
/// it is not a shape of the world and is centred where the query says. Every backend that declares
/// <see cref="PhysicsCapabilities.ShapeQueries"/> takes the three kinds, whatever shapes its bodies may have: an
/// explosion's circle works on the tile backend, which has no circle shapes.
/// </summary>
public readonly record struct QueryShape
{
    /// <summary>The geometry.</summary>
    public QueryShapeKind Kind { get; private init; }
    /// <summary>A box's full width and height; a capsule's width (twice its radius) and overall height; unused by a circle.</summary>
    public Vector2 Size { get; private init; }
    /// <summary>The radius of a circle or of a capsule's ends; 0 for a box.</summary>
    public float Radius { get; private init; }

    /// <summary>An axis-aligned box of <paramref name="size"/>.</summary>
    public static QueryShape Box(Vector2 size) => new() { Kind = QueryShapeKind.Box, Size = size };
    /// <summary>A circle of <paramref name="radius"/>.</summary>
    public static QueryShape Circle(float radius) => new() { Kind = QueryShapeKind.Circle, Radius = radius };
    /// <summary>A capsule <paramref name="height"/> tall overall with ends of <paramref name="radius"/>, standing along the world's up direction.</summary>
    public static QueryShape Capsule(float radius, float height) => new() { Kind = QueryShapeKind.Capsule, Radius = radius, Size = new Vector2(radius * 2, height) };

    /// <summary>
    /// The shape as a rounded box: the half width and half height of its core rectangle and the radius around it (a box
    /// has no radius, a circle no core, a capsule a core with no width).
    /// </summary>
    public (Vector2 HalfExtents, float Radius) Rounded => Kind switch
    {
        QueryShapeKind.Box => (Size / 2, 0),
        QueryShapeKind.Circle => (Vector2.Zero, Radius),
        _ => (new Vector2(0, MathF.Max(0, Size.Y / 2 - Radius)), Radius),
    };

    /// <summary>Throws when the shape is invalid.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The kind is undefined (a <c>default</c> shape is a box with no size), or a size or radius is not positive and finite.</exception>
    public void Validate()
    {
        if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Not a query shape kind.");
        if (Kind is QueryShapeKind.Box or QueryShapeKind.Capsule && !(Size.X > 0 && Size.Y > 0 && float.IsFinite(Size.X) && float.IsFinite(Size.Y)))
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "The width and height must be positive and finite.");
        if (Kind is QueryShapeKind.Circle or QueryShapeKind.Capsule && !(Radius > 0 && float.IsFinite(Radius)))
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "The radius must be positive and finite.");
    }
}

/// <summary>The surface nearest a point (<see cref="IPhysicsWorld2D.ClosestPoint"/>).</summary>
/// <param name="Shape">The nearest shape.</param>
/// <param name="Owner">The entity owning that shape's body.</param>
/// <param name="Point">The point of the shape nearest the one asked about; the point itself when it is inside the shape.</param>
/// <param name="Distance">How far that is; 0 inside the shape.</param>
public readonly record struct ClosestPointHit(ShapeId Shape, EntityId Owner, Vector2 Point, float Distance);

/// <summary>
/// Two shapes hit each other during the latest <see cref="IPhysicsWorld2D.Step"/> faster than the backend's impact
/// speed (<see cref="PhysicsCapabilities.ContactDetail"/>): what an impact sound or damage needs. Pairs with a dynamic
/// body only; a pair that keeps touching reports again whenever it hits again (a bouncing or rolling body).
/// </summary>
/// <param name="A">The first shape.</param>
/// <param name="B">The second shape.</param>
/// <param name="OwnerA">The entity owning <paramref name="A"/>'s body.</param>
/// <param name="OwnerB">The entity owning <paramref name="B"/>'s body.</param>
/// <param name="Point">Where they hit, a world point between the two surfaces.</param>
/// <param name="Normal">The contact normal, unit length, pointing from <paramref name="A"/> to <paramref name="B"/>.</param>
/// <param name="Speed">How fast the two surfaces approached each other along the normal, in units per second; always positive.</param>
/// <param name="Impulse">
/// The impulse the contact applied along the normal in that step (mass × units per second): it grows with the masses
/// as well as the speed, so a heavy crate hits harder than a light one at the same speed. 0 when the contact no longer
/// exists.
/// </param>
public readonly record struct ContactImpact(ShapeId A, ShapeId B, EntityId OwnerA, EntityId OwnerB, Vector2 Point, Vector2 Normal, float Speed, float Impulse);

/// <summary>One point at which a body touches another shape now (<see cref="IPhysicsWorld2D.ContactsOf"/>).</summary>
/// <param name="Shape">The body's own shape.</param>
/// <param name="Other">The shape it touches.</param>
/// <param name="OtherOwner">The entity owning the other shape's body.</param>
/// <param name="Point">The contact point, a world point.</param>
/// <param name="Normal">The contact normal, unit length, pointing from the other shape towards the body: up for a body resting on a floor.</param>
/// <param name="Separation">The distance between the two surfaces there: negative when they overlap.</param>
/// <param name="NormalImpulse">The impulse the solver applied at this point along the normal in the latest step (mass × units per second): how hard the two press on each other.</param>
public readonly record struct ContactPoint(ShapeId Shape, ShapeId Other, EntityId OtherOwner, Vector2 Point, Vector2 Normal, float Separation, float NormalImpulse);
