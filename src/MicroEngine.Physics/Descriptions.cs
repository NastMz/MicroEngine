using System.Numerics;

namespace MicroEngine.Physics;

/// <summary>How a body moves.</summary>
public enum BodyKind
{
    /// <summary>Never moves by itself (level geometry). Common contract.</summary>
    Static,
    /// <summary>
    /// Moved only by the game, through <see cref="IPhysicsWorld2D.Move"/> (collide-and-slide) or
    /// <see cref="IPhysicsWorld2D.SetPosition"/> (teleport); characters and moving platforms. Common contract.
    /// </summary>
    Kinematic,
    /// <summary>Moved by the solver (gravity, velocities, contacts). Needs <see cref="PhysicsCapabilities.DynamicBodies"/>.</summary>
    Dynamic,
}

/// <summary>What <see cref="IPhysicsWorld2D.CreateBody"/> creates.</summary>
/// <param name="Kind">How the body moves.</param>
/// <param name="Position">Its origin in world units; its shapes are placed relative to it.</param>
public readonly record struct BodyDescription(BodyKind Kind, Vector2 Position)
{
    /// <summary>The body's angle in radians; anything but 0 needs <see cref="PhysicsCapabilities.Rotation"/>.</summary>
    public float Rotation { get; init; }
    /// <summary>Continuous collision for a fast dynamic body; needs <see cref="PhysicsCapabilities.ContinuousCollision"/>.</summary>
    public bool Bullet { get; init; }
    /// <summary>
    /// A kinematic body that, when moved with <see cref="IPhysicsWorld2D.Move"/>, is not blocked: it pushes the other
    /// kinematic bodies in its way and carries those resting on it (moving platforms, closing doors). Needs
    /// <see cref="PhysicsCapabilities.MovingSolids"/>; only kinematic bodies can push.
    /// </summary>
    public bool PushesBodies { get; init; }

    /// <summary>
    /// How the solver treats a dynamic body: damping, gravity scale, fixed rotation, sleep. Anything but the defaults
    /// needs <see cref="PhysicsCapabilities.DynamicBodies"/>.
    /// </summary>
    public BodyDynamics Dynamics { get; init; }

    /// <summary>A static body at <paramref name="position"/>.</summary>
    public static BodyDescription Static(Vector2 position = default) => new(BodyKind.Static, position);
    /// <summary>A kinematic body at <paramref name="position"/>.</summary>
    public static BodyDescription Kinematic(Vector2 position) => new(BodyKind.Kinematic, position);
    /// <summary>A dynamic body at <paramref name="position"/> (needs <see cref="PhysicsCapabilities.DynamicBodies"/>).</summary>
    public static BodyDescription Dynamic(Vector2 position) => new(BodyKind.Dynamic, position);

    /// <summary>The capabilities this description needs beyond the common contract.</summary>
    public PhysicsCapabilities RequiredCapabilities =>
        (Kind == BodyKind.Dynamic ? PhysicsCapabilities.DynamicBodies : 0)
        | (Rotation != 0 ? PhysicsCapabilities.Rotation : 0)
        | (Bullet ? PhysicsCapabilities.ContinuousCollision : 0)
        | (PushesBodies ? PhysicsCapabilities.MovingSolids : 0)
        | (Dynamics != default ? PhysicsCapabilities.DynamicBodies : 0);

    /// <summary>Throws when the description is invalid whatever the backend declares.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The kind is undefined, or the position or rotation is not finite.</exception>
    /// <exception cref="ArgumentException">A static or dynamic body pushes bodies (only kinematic bodies can).</exception>
    public void Validate()
    {
        if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Not a body kind.");
        if (!float.IsFinite(Position.X) || !float.IsFinite(Position.Y)) throw new ArgumentOutOfRangeException(nameof(Position), Position, "The position must be finite.");
        if (!float.IsFinite(Rotation)) throw new ArgumentOutOfRangeException(nameof(Rotation), Rotation, "The rotation must be finite.");
        if (PushesBodies && Kind != BodyKind.Kinematic) throw new ArgumentException("Only a kinematic body can push bodies.", nameof(PushesBodies));
        Dynamics.Validate();
    }
}

/// <summary>
/// How the solver treats a dynamic body (<see cref="BodyDescription.Dynamics"/>,
/// <see cref="IPhysicsWorld2D.SetDynamics"/>). <c>default</c> is the default body: no damping, full gravity, free to
/// rotate, allowed to sleep.
/// </summary>
public readonly record struct BodyDynamics
{
    // Stored relative to the defaults, so default(BodyDynamics) is the default body.
    private readonly float _gravityScale;
    private readonly bool _noSleep;

    /// <summary>How fast the linear velocity decays, per second (0: never); for drag and top-down bodies.</summary>
    public float LinearDamping { get; init; }
    /// <summary>How fast the angular velocity decays, per second (0: never).</summary>
    public float AngularDamping { get; init; }
    /// <summary>The share of the world's gravity the body feels: 1 by default, 0 floats, negative rises.</summary>
    public float GravityScale
    {
        get => _gravityScale + 1;
        init => _gravityScale = value - 1;
    }
    /// <summary>Whether the body never rotates (a character made of a dynamic body, a crate that must stay upright).</summary>
    public bool FixedRotation { get; init; }
    /// <summary>Whether the body may fall asleep when it rests (true by default); a sleeping body costs nothing until something touches it.</summary>
    public bool AllowSleep
    {
        get => !_noSleep;
        init => _noSleep = !value;
    }

    /// <summary>Throws when a value is out of range.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A damping that is negative or not finite, or a gravity scale that is not finite.</exception>
    public void Validate()
    {
        if (!(LinearDamping >= 0) || float.IsInfinity(LinearDamping)) throw new ArgumentOutOfRangeException(nameof(LinearDamping), LinearDamping, "Damping must be finite and zero or more.");
        if (!(AngularDamping >= 0) || float.IsInfinity(AngularDamping)) throw new ArgumentOutOfRangeException(nameof(AngularDamping), AngularDamping, "Damping must be finite and zero or more.");
        if (!float.IsFinite(GravityScale)) throw new ArgumentOutOfRangeException(nameof(GravityScale), GravityScale, "The gravity scale must be finite.");
    }
}

/// <summary>The geometry of a shape.</summary>
public enum ShapeKind
{
    /// <summary>An axis-aligned box: the common shape (and the common character shape <see cref="IPhysicsWorld2D.Move"/> moves).</summary>
    Box,
    /// <summary>A circle; needs <see cref="PhysicsCapabilities.Circles"/>.</summary>
    Circle,
    /// <summary>A vertical capsule; needs <see cref="PhysicsCapabilities.Capsules"/>.</summary>
    Capsule,
    /// <summary>A convex polygon; needs <see cref="PhysicsCapabilities.Polygons"/>.</summary>
    Polygon,
}

/// <summary>
/// The collision layers of the contract: 64 bits, any meaning the game gives them. A shape is in the layers of its
/// <see cref="ShapeDescription.Category"/> and collides with the layers of its <see cref="ShapeDescription.Mask"/>; two
/// shapes collide only when each one's mask holds the other's category.
/// </summary>
public static class CollisionLayers
{
    /// <summary>The layer shapes and queries are in by default (bit 0).</summary>
    public const ulong Default = 1;
    /// <summary>Every layer.</summary>
    public const ulong All = ulong.MaxValue;
}

/// <summary>What <see cref="IPhysicsWorld2D.AddShape"/> attaches to a body. Build one with <see cref="Box"/> (common) or a capability's factory.</summary>
public readonly record struct ShapeDescription
{
    /// <summary>The geometry.</summary>
    public ShapeKind Kind { get; init; }
    /// <summary>A box's full width and height, in world units; both positive. Unused by circles.</summary>
    public Vector2 Size { get; init; }
    /// <summary>The radius of a circle or a capsule's ends.</summary>
    public float Radius { get; init; }
    /// <summary>The convex polygon's vertices, relative to <see cref="Offset"/>, counter-clockwise.</summary>
    public ReadOnlyMemory<Vector2> Vertices { get; init; }
    /// <summary>The shape's centre relative to its body's origin.</summary>
    public Vector2 Offset { get; init; }
    /// <summary>The layers the shape is in (<see cref="CollisionLayers"/>); <see cref="CollisionLayers.Default"/> by default.</summary>
    public ulong Category { get; init; }
    /// <summary>The layers it collides with; <see cref="CollisionLayers.All"/> by default.</summary>
    public ulong Mask { get; init; }
    /// <summary>
    /// A sensor: it never blocks and is never blocked; it reports the shapes that enter and leave it as
    /// <see cref="IPhysicsWorld2D.SensorEvents"/>.
    /// </summary>
    public bool IsSensor { get; init; }
    /// <summary>
    /// Whether sensors see this shape (true by default). Sensor events need it on the visiting shape as well as a
    /// sensor that accepts its layers; the backend enables whatever it needs from this flag.
    /// </summary>
    public bool SensorEvents { get; init; }
    /// <summary>A one-way platform (needs <see cref="PhysicsCapabilities.OneWayPlatforms"/>): it stops only bodies coming down onto its up-facing side.</summary>
    public bool OneWay { get; init; }
    /// <summary>Friction; null for the backend's default. A value needs <see cref="PhysicsCapabilities.FrictionRestitution"/>.</summary>
    public float? Friction { get; init; }
    /// <summary>Restitution (bounciness); null for the backend's default. A value needs <see cref="PhysicsCapabilities.FrictionRestitution"/>.</summary>
    public float? Restitution { get; init; }
    /// <summary>Density; null for the backend's default. A value needs <see cref="PhysicsCapabilities.MassProperties"/>.</summary>
    public float? Density { get; init; }

    /// <summary>An axis-aligned box of <paramref name="size"/> centred at <paramref name="offset"/> from its body's origin, in the default layers.</summary>
    public static ShapeDescription Box(Vector2 size, Vector2 offset = default) =>
        new() { Kind = ShapeKind.Box, Size = size, Offset = offset, Category = CollisionLayers.Default, Mask = CollisionLayers.All, SensorEvents = true };

    /// <summary>A box sensor of <paramref name="size"/> centred at <paramref name="offset"/>.</summary>
    public static ShapeDescription Sensor(Vector2 size, Vector2 offset = default) => Box(size, offset) with { IsSensor = true };

    /// <summary>A circle (needs <see cref="PhysicsCapabilities.Circles"/>).</summary>
    public static ShapeDescription Circle(float radius, Vector2 offset = default) => Box(default, offset) with { Kind = ShapeKind.Circle, Radius = radius };

    /// <summary>A vertical capsule <paramref name="height"/> tall overall with ends of <paramref name="radius"/> (needs <see cref="PhysicsCapabilities.Capsules"/>).</summary>
    public static ShapeDescription Capsule(float radius, float height, Vector2 offset = default) =>
        Box(new Vector2(radius * 2, height), offset) with { Kind = ShapeKind.Capsule, Radius = radius };

    /// <summary>A convex polygon (needs <see cref="PhysicsCapabilities.Polygons"/>).</summary>
    public static ShapeDescription Polygon(ReadOnlyMemory<Vector2> vertices, Vector2 offset = default) =>
        Box(default, offset) with { Kind = ShapeKind.Polygon, Vertices = vertices };

    /// <summary>The same shape in other layers.</summary>
    public ShapeDescription InLayers(ulong category, ulong mask) => this with { Category = category, Mask = mask };

    /// <summary>The capabilities this description needs beyond the common contract.</summary>
    public PhysicsCapabilities RequiredCapabilities =>
        Kind switch
        {
            ShapeKind.Circle => PhysicsCapabilities.Circles,
            ShapeKind.Capsule => PhysicsCapabilities.Capsules,
            ShapeKind.Polygon => PhysicsCapabilities.Polygons,
            _ => PhysicsCapabilities.None,
        }
        | (OneWay ? PhysicsCapabilities.OneWayPlatforms : 0)
        | (Friction is not null || Restitution is not null ? PhysicsCapabilities.FrictionRestitution : 0)
        | (Density is not null ? PhysicsCapabilities.MassProperties : 0);

    /// <summary>Throws when the description is invalid whatever the backend declares.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The kind is undefined, a size, radius or offset is not positive or not finite, or a box's size is not positive.</exception>
    /// <exception cref="ArgumentException">A sensor is also one-way.</exception>
    public void Validate()
    {
        if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Not a shape kind.");
        if (!float.IsFinite(Offset.X) || !float.IsFinite(Offset.Y)) throw new ArgumentOutOfRangeException(nameof(Offset), Offset, "The offset must be finite.");
        if (Kind is ShapeKind.Box or ShapeKind.Capsule && !(Size.X > 0 && Size.Y > 0 && float.IsFinite(Size.X) && float.IsFinite(Size.Y)))
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "A box's width and height must be positive and finite.");
        if (Kind is ShapeKind.Circle or ShapeKind.Capsule && !(Radius > 0 && float.IsFinite(Radius)))
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "The radius must be positive and finite.");
        if (Kind == ShapeKind.Polygon && Vertices.Length < 3)
            throw new ArgumentOutOfRangeException(nameof(Vertices), Vertices.Length, "A polygon needs at least three vertices.");
        if (IsSensor && OneWay) throw new ArgumentException("A sensor cannot be a one-way platform.", nameof(OneWay));
    }
}

/// <summary>
/// Which shapes a query (<see cref="IPhysicsWorld2D.RayCast"/>, <see cref="IPhysicsWorld2D.Overlap(in RectF, in QueryFilter, Span{ShapeId})"/>) or a
/// <see cref="IPhysicsWorld2D.Move"/> considers. A shape matches when its category is in the filter's
/// <see cref="Mask"/> and the filter's <see cref="Category"/> is in the shape's mask. <c>default</c> is
/// <see cref="Default"/>: category <see cref="CollisionLayers.Default"/>, every layer, sensors left out.
/// </summary>
public readonly record struct QueryFilter
{
    // Stored relative to the defaults, so default(QueryFilter) is the default filter.
    private readonly ulong _category;
    private readonly ulong _mask;

    /// <summary>A filter in <paramref name="category"/> that sees <paramref name="mask"/>.</summary>
    public QueryFilter(ulong category, ulong mask)
    {
        _category = category ^ CollisionLayers.Default;
        _mask = ~mask;
    }

    /// <summary>The default filter: category <see cref="CollisionLayers.Default"/>, every layer, no sensors.</summary>
    public static QueryFilter Default => default;

    /// <summary>The filter that moves or queries as <paramref name="shape"/> would collide: its category and mask.</summary>
    public static QueryFilter For(in ShapeDescription shape) => new(shape.Category, shape.Mask);

    /// <summary>The layers the query is in (shapes whose mask lacks them ignore it).</summary>
    public ulong Category
    {
        get => _category ^ CollisionLayers.Default;
        init => _category = value ^ CollisionLayers.Default;
    }

    /// <summary>The layers the query sees.</summary>
    public ulong Mask
    {
        get => ~_mask;
        init => _mask = ~value;
    }

    /// <summary>
    /// Whether ray casts and overlaps return sensor shapes (false by default). Sensors never block
    /// <see cref="IPhysicsWorld2D.Move"/>, whatever this says.
    /// </summary>
    public bool IncludeSensors { get; init; }

    /// <summary>Whether one-way platforms are passed through (dropping down through a platform); false by default.</summary>
    public bool PassOneWay { get; init; }

    /// <summary>
    /// Leaves single shapes out, whatever their layers: a move or a query considers a shape only when this returns
    /// true for it (null, the default, considers every shape the layers accept). For what layers cannot say: a ray
    /// that must skip the body that fires it, a block that is solid only for a character that comes from below. It
    /// needs <see cref="PhysicsCapabilities.FilterPredicates"/>, which both of the engine's backends declare. It is
    /// called before the move or the query runs, once per shape near it; it may read the world
    /// (<see cref="IPhysicsWorld2D.BodyOf"/>, <see cref="IPhysicsWorld2D.PositionOf"/>,
    /// <see cref="IPhysicsWorld2D.BoundsOf"/>) but must not query or change it. Keep the delegate in a field: one made
    /// in every step allocates in every step.
    /// </summary>
    public Func<ShapeId, bool>? Where { get; init; }

    /// <summary>Whether <see cref="Where"/> leaves <paramref name="shape"/> in (true without a predicate).</summary>
    public bool Considers(ShapeId shape) => Where is null || Where(shape);

    /// <summary>Whether a shape in <paramref name="category"/> colliding with <paramref name="mask"/> matches the filter's layers.</summary>
    public bool Accepts(ulong category, ulong mask) => (category & Mask) != 0 && (mask & Category) != 0;
}
