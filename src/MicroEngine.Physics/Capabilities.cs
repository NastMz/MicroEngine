namespace MicroEngine.Physics;

/// <summary>
/// What a physics backend offers beyond the common contract of <see cref="IPhysicsWorld2D"/>. Every backend passes the
/// common conformance suite; each flag it declares has a suite of its own that runs only on backends declaring it. A
/// request that needs an undeclared flag fails with <see cref="PhysicsCapabilityException"/>: a backend never emulates
/// a capability silently.
/// </summary>
[Flags]
public enum PhysicsCapabilities
{
    /// <summary>Only the common contract.</summary>
    None = 0,
    /// <summary>Grid-aligned static geometry fed directly from tile layers (the tile backend's own <c>AddTiles</c>).</summary>
    TileGeometry = 1 << 0,
    /// <summary>
    /// One-way platforms (<see cref="ShapeDescription.OneWay"/>): a shape that stops only bodies coming down onto its
    /// up-facing side (against <see cref="IPhysicsWorld2D.Up"/>), implemented inside the backend.
    /// </summary>
    OneWayPlatforms = 1 << 1,
    /// <summary>Dynamic bodies (<see cref="BodyKind.Dynamic"/>) moved by the solver: velocities, gravity, contact events.</summary>
    DynamicBodies = 1 << 2,
    /// <summary>Rotated bodies (<see cref="BodyDescription.Rotation"/> other than 0) and bodies that rotate.</summary>
    Rotation = 1 << 3,
    /// <summary>Joints between bodies (<see cref="IPhysicsWorld2D.CreateJoint"/>): revolute, distance, prismatic, weld, wheel and target joints.</summary>
    Joints = 1 << 4,
    /// <summary>Capsule shapes (<see cref="ShapeKind.Capsule"/>), including a capsule as the shape <see cref="IPhysicsWorld2D.Move"/> moves.</summary>
    Capsules = 1 << 5,
    /// <summary>Circle shapes (<see cref="ShapeKind.Circle"/>).</summary>
    Circles = 1 << 6,
    /// <summary>Convex polygon shapes (<see cref="ShapeKind.Polygon"/>).</summary>
    Polygons = 1 << 7,
    /// <summary>Continuous collision for fast dynamic bodies (<see cref="BodyDescription.Bullet"/>).</summary>
    ContinuousCollision = 1 << 8,
    /// <summary>Per-shape friction and restitution (<see cref="ShapeDescription.Friction"/>, <see cref="ShapeDescription.Restitution"/>).</summary>
    FrictionRestitution = 1 << 9,
    /// <summary>Per-shape density and the mass it gives a dynamic body (<see cref="ShapeDescription.Density"/>).</summary>
    MassProperties = 1 << 10,
    /// <summary>
    /// Moving solids (<see cref="BodyDescription.PushesBodies"/>): a kinematic body that, when moved, pushes the
    /// kinematic bodies in its way and carries the ones resting on it.
    /// </summary>
    MovingSolids = 1 << 11,
    /// <summary>
    /// Reading shapes back (<see cref="IPhysicsWorld2D.ShapesOf"/>, <see cref="IPhysicsWorld2D.DescriptionOf(ShapeId)"/>): what a
    /// debug view needs to draw the world. Both built-in backends declare it.
    /// </summary>
    ShapeInspection = 1 << 12,
    /// <summary>
    /// Queries beyond the closest ray and the rectangle overlap: every hit of a ray
    /// (<see cref="IPhysicsWorld2D.RayCastAll"/>), a cast shape (<see cref="IPhysicsWorld2D.ShapeCast"/>), the shapes
    /// under a box, a circle or a capsule (<see cref="IPhysicsWorld2D.Overlap(in QueryShape, System.Numerics.Vector2, in QueryFilter, Span{ShapeId})"/>)
    /// or at a point (<see cref="IPhysicsWorld2D.OverlapPoint"/>), and the nearest surface
    /// (<see cref="IPhysicsWorld2D.ClosestPoint"/>). Both built-in backends declare it.
    /// </summary>
    ShapeQueries = 1 << 13,
    /// <summary>
    /// What a contact of a dynamic body is made of: the impacts of a step with their speed and impulse
    /// (<see cref="IPhysicsWorld2D.ContactImpacts"/>) and the points a body touches at now
    /// (<see cref="IPhysicsWorld2D.ContactsOf"/>).
    /// </summary>
    ContactDetail = 1 << 14,
    /// <summary>
    /// A predicate on a filter (<see cref="QueryFilter.Where"/>) that leaves single shapes out of a move or a query.
    /// Both built-in backends declare it; a backend that does not refuses a filter that has one.
    /// </summary>
    FilterPredicates = 1 << 15,
    /// <summary>
    /// Slope tiles in tile geometry (<see cref="MicroEngine.World.TileFlags.SlopeRight"/>,
    /// <see cref="MicroEngine.World.TileFlags.SlopeLeft"/>). A backend that does not declare it refuses tiles that hold
    /// one when they are added with an area.
    /// </summary>
    TileSlopes = 1 << 16,
}

/// <summary>
/// A request needed a physics capability the backend does not declare (<see cref="IPhysicsWorld2D.Capabilities"/>). It
/// is thrown before anything changes: the world is as it was.
/// </summary>
public sealed class PhysicsCapabilityException : NotSupportedException
{
    /// <summary>Creates the error.</summary>
    /// <param name="operation">The refused operation, e.g. "CreateBody(Dynamic)".</param>
    /// <param name="backend">The backend name (<see cref="IPhysicsWorld2D.Backend"/>).</param>
    /// <param name="missing">The undeclared capabilities the request needs.</param>
    public PhysicsCapabilityException(string operation, string backend, PhysicsCapabilities missing)
        : base($"{operation} on '{backend}' needs the physics capability {missing}, which this backend does not declare{Provider(missing)}")
    {
        Operation = operation;
        Backend = backend;
        Missing = missing;
    }

    /// <summary>The capabilities only the optional Box2D backend declares, and the ones only the tile backend declares.</summary>
    private const PhysicsCapabilities Box2DOnly = PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.Rotation | PhysicsCapabilities.Joints
            | PhysicsCapabilities.Capsules | PhysicsCapabilities.Circles | PhysicsCapabilities.Polygons | PhysicsCapabilities.ContinuousCollision
            | PhysicsCapabilities.FrictionRestitution | PhysicsCapabilities.MassProperties | PhysicsCapabilities.ContactDetail
            | PhysicsCapabilities.TileSlopes,
        TilesOnly = PhysicsCapabilities.MovingSolids;

    /// <summary>Which of the engine's backends declares what is missing, for the message.</summary>
    private static string Provider(PhysicsCapabilities missing) =>
        (missing & Box2DOnly) != 0 ? "; the Box2D backend (the optional MicroEngine.Physics.Box2D package) declares it"
        : (missing & TilesOnly) != 0 ? "; the tile backend declares it"
        : "";

    /// <summary>The refused operation.</summary>
    public string Operation { get; }
    /// <summary>The backend name.</summary>
    public string Backend { get; }
    /// <summary>The undeclared capabilities the request needs.</summary>
    public PhysicsCapabilities Missing { get; }

    /// <summary>
    /// Throws when <paramref name="required"/> holds a flag <paramref name="declared"/> lacks: the check every backend
    /// runs before acting on a request.
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">A required capability is not declared.</exception>
    public static void ThrowIfMissing(PhysicsCapabilities declared, PhysicsCapabilities required, string operation, string backend)
    {
        var missing = required & ~declared;
        if (missing != PhysicsCapabilities.None) throw new PhysicsCapabilityException(operation, backend, missing);
    }
}
