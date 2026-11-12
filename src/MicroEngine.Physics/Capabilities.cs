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
    /// <summary>Joints between bodies.</summary>
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
    /// Reading shapes back (<see cref="IPhysicsWorld2D.ShapesOf"/>, <see cref="IPhysicsWorld2D.DescriptionOf"/>): what a
    /// debug view needs to draw the world. Both built-in backends declare it.
    /// </summary>
    ShapeInspection = 1 << 12,
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
        : base($"{operation} on '{backend}' needs the physics capability {missing}, which this backend does not declare")
    {
        Operation = operation;
        Backend = backend;
        Missing = missing;
    }

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
