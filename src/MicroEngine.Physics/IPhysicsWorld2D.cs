using System.Numerics;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace MicroEngine.Physics;

/// <summary>The version of the 2D physics contract (<see cref="IPhysicsWorld2D"/>); a change to its rules raises it.</summary>
public static class PhysicsContract
{
    /// <summary>
    /// 2: version 1 (the common core and capabilities of the first two backends) plus tile geometry as a contract member
    /// (<see cref="IPhysicsWorld2D.AddTiles"/>), dynamics control (forces, impulses, torque, angular velocity,
    /// <see cref="BodyDynamics"/>, sleep, mass data), joints, shape queries (cast shapes, every hit of a ray, shape and
    /// point overlaps, the nearest surface) and contact detail (impacts, contact points). Every member added since version 1 has a default that refuses with the capability it needs, so
    /// a backend written for version 1 still compiles and declares what it implements.
    /// </summary>
    public const int Version = 2;
}

/// <summary>
/// A 2D physics world: bodies owned by entities, shapes with collision layers, kinematic collide-and-slide moves, ray
/// and overlap queries, sensors, and a step. The members below are the common contract every backend passes (one
/// conformance suite); everything else is a declared capability (<see cref="Capabilities"/>), and a request that needs
/// an undeclared one throws <see cref="PhysicsCapabilityException"/> without changing the world.
/// </summary>
/// <remarks>
/// The rules of the contract:
/// <list type="number">
/// <item><b>Bodies and shapes.</b> Static and kinematic bodies and axis-aligned box shapes are common; a shape is placed
/// at its body's origin plus its offset. Ids are generation-checked: a stale id is never alive, and using one throws
/// <see cref="InvalidOperationException"/>; destroying a body destroys its shapes.</item>
/// <item><b>Layers.</b> Two shapes collide only when each one's <see cref="ShapeDescription.Mask"/> holds the other's
/// <see cref="ShapeDescription.Category"/>; a query or move sees a shape only when <see cref="QueryFilter.Accepts"/> is
/// true for it.</item>
/// <item><b>Move</b> moves a kinematic body's one non-sensor shape, an axis-aligned box (the common character shape;
/// a capsule needs <see cref="PhysicsCapabilities.Capsules"/>), by collide-and-slide against the non-sensor shapes the
/// filter accepts: it never passes through a shape at the speeds the conformance suite tests, and a blocked axis stops
/// while the others go on. It may stop short of a surface by a gap of at most <see cref="Skin"/>, the tolerance the
/// backend declares (0 on the tile backend). The body's contacts and its ground are in the <see cref="MoveResult"/>; a
/// shape the box already overlaps when the move starts does not block it, so a body is never trapped.</item>
/// <item><b>Queries.</b> <see cref="RayCast"/> returns the closest hit; <see cref="Overlap(in RectF, in QueryFilter, Span{ShapeId})"/> is exact (shapes that
/// truly overlap the area, not their bounding boxes; sharing an edge is not overlapping). Both leave sensors out unless
/// the filter includes them.</item>
/// <item><b>Sensors.</b> A sensor reports the non-sensor shapes of kinematic and dynamic bodies that begin or stop
/// overlapping it, when the layers of both accept each other and the visitor has
/// <see cref="ShapeDescription.SensorEvents"/> (the default). Sensors do not see other sensors or static shapes.</item>
/// <item><b>Events are buffered per step.</b> <see cref="Step"/> computes <see cref="SensorEvents"/> and
/// <see cref="ContactEvents"/> from the poses at its end (moves made before it count in it); they stay readable until
/// the next step, in an order that depends only on the world's history. Contact events only come from pairs with a
/// dynamic body. Destroying a shape ends its pairs with an exit or end event at the next step.</item>
/// <item><b>No managed callbacks during <see cref="Step"/>.</b> Per-shape rules (one-way platforms) are shape flags the
/// backend implements itself; games read events after the step.</item>
/// <item><b>Stepping.</b> A scene steps its physics world after its own update, by the step of its world's clock, and
/// writes body poses to their owners' transforms, which the scene world interpolates for drawing (the engine's
/// <c>ScenePhysics</c>). <see cref="PhysicsModule"/> steps worlds that belong to no scene, in the kernel's
/// <c>FramePhase.Physics</c>.</item>
/// <item><b>Determinism.</b> The same backend build gives identical results on every target for the same calls (native
/// backends are built with floating-point contraction off and step on one thread). Nothing is promised between two
/// backends, or between two versions of a backend's physics library: they differ from the first step.</item>
/// </list>
/// A world is not thread-safe.
/// </remarks>
public interface IPhysicsWorld2D
{
    /// <summary>The backend's name, for messages ("tiles", "box2d").</summary>
    string Backend { get; }
    /// <summary>What the backend offers beyond the common contract.</summary>
    PhysicsCapabilities Capabilities { get; }
    /// <summary>
    /// The largest gap <see cref="Move"/> may leave between a body and the surface that stopped it or that it rests on, in
    /// world units: 0 when resting is exact.
    /// </summary>
    float Skin { get; }
    /// <summary>The world's up direction (unit length): it tells ground from ceilings and which side of a one-way platform stops bodies.</summary>
    Vector2 Up { get; }
    /// <summary>How many times <see cref="Step"/> ran.</summary>
    long StepCount { get; }

    /// <summary>Creates a body owned by <paramref name="owner"/>.</summary>
    /// <exception cref="PhysicsCapabilityException">The description needs an undeclared capability.</exception>
    /// <exception cref="ArgumentException">The description is invalid (<see cref="BodyDescription.Validate"/>).</exception>
    BodyId CreateBody(EntityId owner, in BodyDescription body);
    /// <summary>Destroys the body and its shapes; nothing for a stale or none id. Their sensor pairs end at the next step.</summary>
    void DestroyBody(BodyId body);
    /// <summary>Whether <paramref name="body"/> exists.</summary>
    bool IsAlive(BodyId body);
    /// <summary>The live bodies, in creation order (a destroyed body leaves its place).</summary>
    ReadOnlySpan<BodyId> Bodies { get; }
    /// <summary>The entity that owns <paramref name="body"/>.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    EntityId OwnerOf(BodyId body);
    /// <summary>How <paramref name="body"/> moves.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    BodyKind KindOf(BodyId body);
    /// <summary>The body's origin.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    Vector2 PositionOf(BodyId body);
    /// <summary>The body's angle in radians (0 for backends without <see cref="PhysicsCapabilities.Rotation"/>).</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    float RotationOf(BodyId body);
    /// <summary>Teleports the body: no collision, no pushing; sensor events follow at the next step.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The position is not finite.</exception>
    void SetPosition(BodyId body, Vector2 position);
    /// <summary>A dynamic body's velocity.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    Vector2 VelocityOf(BodyId body);
    /// <summary>Sets a dynamic body's velocity.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    void SetVelocity(BodyId body, Vector2 velocity);

    // ------------------------------------------------------------------ dynamics

    /// <summary>The error of a member the backend does not implement: the capabilities it lacks among <paramref name="needed"/>.</summary>
    private PhysicsCapabilityException Refused(string operation, PhysicsCapabilities needed)
    {
        var missing = needed & ~Capabilities;
        return new PhysicsCapabilityException(operation, Backend, missing != PhysicsCapabilities.None ? missing : needed);
    }

    /// <summary>Turns the body to <paramref name="radians"/> where it is: no collision, like <see cref="SetPosition"/>.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.Rotation"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The angle is not finite.</exception>
    void SetRotation(BodyId body, float radians) => throw Refused(nameof(SetRotation), PhysicsCapabilities.Rotation);
    /// <summary>A body's angular velocity in radians per second; positive turns the way <see cref="RotationOf"/> grows.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/> and <see cref="PhysicsCapabilities.Rotation"/>.</exception>
    float AngularVelocityOf(BodyId body) => throw Refused(nameof(AngularVelocityOf), PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.Rotation);
    /// <summary>Sets a dynamic body's angular velocity, in radians per second.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/> and <see cref="PhysicsCapabilities.Rotation"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale or the body is not dynamic.</exception>
    void SetAngularVelocity(BodyId body, float radiansPerSecond) => throw Refused(nameof(SetAngularVelocity), PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.Rotation);
    /// <summary>
    /// Applies <paramref name="force"/> (mass × world units per second squared) through a dynamic body's centre of
    /// mass for the next step only: apply it every step for as long as it acts.
    /// </summary>
    /// <param name="body">A dynamic body.</param>
    /// <param name="force">The force.</param>
    /// <param name="wake">Whether a sleeping body wakes; when false, a sleeping body ignores the force.</param>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale or the body is not dynamic.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The force is not finite.</exception>
    void ApplyForce(BodyId body, Vector2 force, bool wake = true) => throw Refused(nameof(ApplyForce), PhysicsCapabilities.DynamicBodies);
    /// <summary>Applies <paramref name="force"/> at the world point <paramref name="point"/> for the next step only: off the centre of mass it also turns the body.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale or the body is not dynamic.</exception>
    void ApplyForce(BodyId body, Vector2 force, Vector2 point, bool wake = true) => throw Refused(nameof(ApplyForce), PhysicsCapabilities.DynamicBodies);
    /// <summary>
    /// Applies <paramref name="impulse"/> (mass × world units per second) through a dynamic body's centre of mass: its
    /// velocity changes at once by the impulse divided by its mass (a jump, a hit, an explosion).
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale or the body is not dynamic.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The impulse is not finite.</exception>
    void ApplyImpulse(BodyId body, Vector2 impulse, bool wake = true) => throw Refused(nameof(ApplyImpulse), PhysicsCapabilities.DynamicBodies);
    /// <summary>Applies <paramref name="impulse"/> at the world point <paramref name="point"/>: off the centre of mass it also spins the body.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale or the body is not dynamic.</exception>
    void ApplyImpulse(BodyId body, Vector2 impulse, Vector2 point, bool wake = true) => throw Refused(nameof(ApplyImpulse), PhysicsCapabilities.DynamicBodies);
    /// <summary>Applies a torque (inertia × radians per second squared) to a dynamic body for the next step only.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/> and <see cref="PhysicsCapabilities.Rotation"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale or the body is not dynamic.</exception>
    void ApplyTorque(BodyId body, float torque, bool wake = true) => throw Refused(nameof(ApplyTorque), PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.Rotation);
    /// <summary>Applies an angular impulse (inertia × radians per second) to a dynamic body: its angular velocity changes at once.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/> and <see cref="PhysicsCapabilities.Rotation"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale or the body is not dynamic.</exception>
    void ApplyAngularImpulse(BodyId body, float impulse, bool wake = true) => throw Refused(nameof(ApplyAngularImpulse), PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.Rotation);
    /// <summary>How the solver treats <paramref name="body"/>: damping, gravity scale, fixed rotation, sleep.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    BodyDynamics DynamicsOf(BodyId body) => throw Refused(nameof(DynamicsOf), PhysicsCapabilities.DynamicBodies);
    /// <summary>Changes how the solver treats <paramref name="body"/>.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range (<see cref="BodyDynamics.Validate"/>).</exception>
    void SetDynamics(BodyId body, in BodyDynamics dynamics) => throw Refused(nameof(SetDynamics), PhysicsCapabilities.DynamicBodies);
    /// <summary>Whether a body is awake (simulated); bodies that rest fall asleep when they may (<see cref="BodyDynamics.AllowSleep"/>).</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    bool IsAwake(BodyId body) => throw Refused(nameof(IsAwake), PhysicsCapabilities.DynamicBodies);
    /// <summary>Wakes a body or puts it to sleep (with the bodies it touches).</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.DynamicBodies"/>.</exception>
    void SetAwake(BodyId body, bool awake) => throw Refused(nameof(SetAwake), PhysicsCapabilities.DynamicBodies);
    /// <summary>A dynamic body's mass, from its shapes' densities; 0 for other bodies.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.MassProperties"/>.</exception>
    float MassOf(BodyId body) => throw Refused(nameof(MassOf), PhysicsCapabilities.MassProperties);
    /// <summary>A body's centre of mass, a world point (its origin for bodies without mass).</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.MassProperties"/>.</exception>
    Vector2 CenterOfMassOf(BodyId body) => throw Refused(nameof(CenterOfMassOf), PhysicsCapabilities.MassProperties);
    /// <summary>A dynamic body's rotational inertia about its centre of mass (mass × world units squared); 0 for other bodies.</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.MassProperties"/>.</exception>
    float InertiaOf(BodyId body) => throw Refused(nameof(InertiaOf), PhysicsCapabilities.MassProperties);

    // ------------------------------------------------------------------ joints

    /// <summary>Creates a joint between two bodies (<see cref="PhysicsCapabilities.Joints"/>).</summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.Joints"/>.</exception>
    /// <exception cref="ArgumentException">The description is invalid (<see cref="JointDescription.Validate"/>).</exception>
    /// <exception cref="InvalidOperationException">A body id is stale.</exception>
    JointId CreateJoint(in JointDescription joint) => throw Refused(nameof(CreateJoint), PhysicsCapabilities.Joints);
    /// <summary>Destroys a joint; nothing for a stale or none id. Destroying a body destroys its joints.</summary>
    void DestroyJoint(JointId joint) { }
    /// <summary>Whether <paramref name="joint"/> exists.</summary>
    bool IsAlive(JointId joint) => false;
    /// <summary>The live joints, in creation order.</summary>
    ReadOnlySpan<JointId> Joints => [];
    /// <summary>The joints that broke in the latest step (<see cref="JointDescription.BreakForce"/>); they no longer exist.</summary>
    ReadOnlySpan<JointId> BrokenJoints => [];
    /// <summary>The description the joint was created with, kept up to date by the setters (anchors and axis are as they were when it was created).</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    JointDescription DescriptionOf(JointId joint) => throw Refused(nameof(DescriptionOf), PhysicsCapabilities.Joints);
    /// <summary>The joint's anchor on body A, a world point, as the body is now.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    Vector2 AnchorAOf(JointId joint) => throw Refused(nameof(AnchorAOf), PhysicsCapabilities.Joints);
    /// <summary>The joint's anchor on body B, a world point, as the body is now.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    Vector2 AnchorBOf(JointId joint) => throw Refused(nameof(AnchorBOf), PhysicsCapabilities.Joints);
    /// <summary>The force the joint applied in the latest step to hold its bodies together (mass × world units per second squared).</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    Vector2 ReactionForceOf(JointId joint) => throw Refused(nameof(ReactionForceOf), PhysicsCapabilities.Joints);
    /// <summary>The torque the joint applied in the latest step.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    float ReactionTorqueOf(JointId joint) => throw Refused(nameof(ReactionTorqueOf), PhysicsCapabilities.Joints);
    /// <summary>
    /// Where the joint is along what it frees: a revolute joint's angle in radians since it was created, a prismatic
    /// joint's translation, a distance joint's length; 0 for the other kinds.
    /// </summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    float JointCoordinateOf(JointId joint) => throw Refused(nameof(JointCoordinateOf), PhysicsCapabilities.Joints);
    /// <summary>Turns a joint's limit on or off and sets its bounds (revolute, prismatic, wheel, distance).</summary>
    /// <exception cref="InvalidOperationException">The id is stale, or the joint's kind has no limit.</exception>
    /// <exception cref="ArgumentException">The lower bound is above the upper one.</exception>
    void SetJointLimit(JointId joint, bool enabled, float lower, float upper) => throw Refused(nameof(SetJointLimit), PhysicsCapabilities.Joints);
    /// <summary>Turns a joint's motor on or off and sets its speed and the most force or torque it applies (revolute, prismatic, wheel, distance).</summary>
    /// <exception cref="InvalidOperationException">The id is stale, or the joint's kind has no motor.</exception>
    void SetJointMotor(JointId joint, bool enabled, float speed, float maxForce) => throw Refused(nameof(SetJointMotor), PhysicsCapabilities.Joints);
    /// <summary>Turns a joint's spring on or off and sets its frequency and damping ratio.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    void SetJointSpring(JointId joint, bool enabled, float hertz, float dampingRatio) => throw Refused(nameof(SetJointSpring), PhysicsCapabilities.Joints);
    /// <summary>Moves a target joint's world point.</summary>
    /// <exception cref="InvalidOperationException">The id is stale, or the joint is not a target joint.</exception>
    void SetJointTarget(JointId joint, Vector2 target) => throw Refused(nameof(SetJointTarget), PhysicsCapabilities.Joints);
    /// <summary>Sets a distance joint's rest length (a winch).</summary>
    /// <exception cref="InvalidOperationException">The id is stale, or the joint is not a distance joint.</exception>
    void SetJointLength(JointId joint, float length) => throw Refused(nameof(SetJointLength), PhysicsCapabilities.Joints);

    // ------------------------------------------------------------------ shapes

    /// <summary>Attaches a shape to <paramref name="body"/>.</summary>
    /// <exception cref="PhysicsCapabilityException">The description needs an undeclared capability.</exception>
    /// <exception cref="ArgumentException">The description is invalid (<see cref="ShapeDescription.Validate"/>).</exception>
    /// <exception cref="InvalidOperationException">The body id is stale.</exception>
    ShapeId AddShape(BodyId body, in ShapeDescription shape);
    /// <summary>Removes a shape; nothing for a stale or none id. Its sensor pairs end at the next step.</summary>
    void RemoveShape(ShapeId shape);
    /// <summary>Whether <paramref name="shape"/> exists.</summary>
    bool IsAlive(ShapeId shape);
    /// <summary>The body <paramref name="shape"/> is attached to.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    BodyId BodyOf(ShapeId shape);
    /// <summary>
    /// Writes the live shapes of <paramref name="body"/>, in the order they were added, into <paramref name="results"/>
    /// and returns how many it has (which may exceed the span's length: only the first ones are written).
    /// </summary>
    /// <exception cref="InvalidOperationException">The body is stale.</exception>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeInspection"/>.</exception>
    int ShapesOf(BodyId body, Span<ShapeId> results) =>
        throw new PhysicsCapabilityException(nameof(ShapesOf), Backend, PhysicsCapabilities.ShapeInspection);
    /// <summary>The description <paramref name="shape"/> was added with (its geometry is relative to its body's origin).</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeInspection"/>.</exception>
    ShapeDescription DescriptionOf(ShapeId shape) =>
        throw new PhysicsCapabilityException(nameof(DescriptionOf), Backend, PhysicsCapabilities.ShapeInspection);

    /// <summary>The world bounds of <paramref name="shape"/> as it is now (the area of the level for tile geometry).</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeInspection"/>.</exception>
    RectF BoundsOf(ShapeId shape) => throw new PhysicsCapabilityException(nameof(BoundsOf), Backend, PhysicsCapabilities.ShapeInspection);
    /// <summary>
    /// Attaches the tiles of <paramref name="grid"/> to a static body as one shape
    /// (<see cref="PhysicsCapabilities.TileGeometry"/>): cell (x, y) is a square of <see cref="ICollisionGrid.TileSize"/>
    /// units, x tiles sideways and y tiles downward (against <see cref="Up"/>) from the body's origin, so row 0 is the
    /// top row whichever way the world's y grows. <see cref="TileFlags.Solid"/> cells block from every side and
    /// <see cref="TileFlags.OneWay"/> cells are one-way platforms (where the backend declares them); other cells are
    /// not geometry.
    /// </summary>
    /// <param name="body">A static body.</param>
    /// <param name="grid">The tiles.</param>
    /// <param name="columns">How many columns of the grid hold the level.</param>
    /// <param name="rows">How many rows of the grid hold the level.</param>
    /// <param name="category">The layers the tiles are in.</param>
    /// <param name="mask">The layers they collide with.</param>
    /// <remarks>
    /// The tile backend reads the grid at every query, inside and outside that area, so changing a tile changes
    /// collisions at once. A backend that builds shapes from the tiles reads the area and one tile around it (the border
    /// a grid closes its level with) when this is called: remove the shape and add the tiles again after changing them.
    /// </remarks>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.TileGeometry"/>.</exception>
    /// <exception cref="InvalidOperationException">The body is stale or not static.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> or <paramref name="rows"/> is not positive.</exception>
    ShapeId AddTiles(BodyId body, ICollisionGrid grid, int columns, int rows, ulong category = CollisionLayers.Default, ulong mask = CollisionLayers.All) =>
        throw new PhysicsCapabilityException(nameof(AddTiles), Backend, PhysicsCapabilities.TileGeometry);
    /// <summary>The tiles <paramref name="shape"/> stands for when it was added with <see cref="AddTiles"/>; null for any other shape.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    ICollisionGrid? TilesOf(ShapeId shape) => IsAlive(shape) ? null : throw new InvalidOperationException($"{shape} is not a live shape of this world.");

    /// <summary>
    /// Moves a kinematic body by <paramref name="delta"/> with collide-and-slide against the shapes
    /// <paramref name="filter"/> accepts (see the rules); a body that pushes bodies
    /// (<see cref="BodyDescription.PushesBodies"/>) is not blocked and moves the bodies in its way instead.
    /// </summary>
    /// <exception cref="InvalidOperationException">The id is stale, the body is not kinematic, or it has no single non-sensor shape the backend can move.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is not finite.</exception>
    MoveResult Move(BodyId body, Vector2 delta, in QueryFilter filter = default);
    /// <summary>The closest shape the segment from <paramref name="from"/> to <paramref name="to"/> enters; a shape holding <paramref name="from"/> is not hit.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A point is not finite.</exception>
    bool RayCast(Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit);
    /// <summary>
    /// Writes into <paramref name="results"/> the shapes that truly overlap <paramref name="area"/>, in an order that
    /// depends only on the world's history, and returns how many overlap (which may exceed the span's length: only the
    /// first ones are written).
    /// </summary>
    int Overlap(in RectF area, in QueryFilter filter, Span<ShapeId> results);

    // ------------------------------------------------------------------ shape queries

    /// <summary>
    /// Writes into <paramref name="hits"/> every shape the segment from <paramref name="from"/> to <paramref name="to"/>
    /// enters, nearest first (shapes at the same distance in creation order), one hit per shape, and returns how many
    /// it enters (which may exceed the span's length: only the nearest ones are written). The rules of
    /// <see cref="RayCast"/> apply to each: a shape holding <paramref name="from"/> is not hit. For a shot that goes
    /// through several targets, or a line of sight that must skip some of them.
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeQueries"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A point is not finite.</exception>
    int RayCastAll(Vector2 from, Vector2 to, in QueryFilter filter, Span<RayHit> hits) => throw Refused(nameof(RayCastAll), PhysicsCapabilities.ShapeQueries);
    /// <summary>
    /// Moves <paramref name="shape"/> with its centre going from <paramref name="from"/> to <paramref name="to"/> and
    /// returns the first shape it would touch: <see cref="RayHit.Fraction"/> is how far along the way its centre is
    /// then, <see cref="RayHit.Point"/> where the two touch and <see cref="RayHit.Normal"/> the surface normal there. A
    /// shape it overlaps where it starts is not hit (like <see cref="Move"/>, which this predicts: "is there room to
    /// stand up", "what would this dash hit"); one-way platforms are hit only from above, coming down. The shape stops
    /// within <see cref="Skin"/> of the exact place.
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeQueries"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The shape is invalid (<see cref="QueryShape.Validate"/>) or a point is not finite.</exception>
    bool ShapeCast(in QueryShape shape, Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit) => throw Refused(nameof(ShapeCast), PhysicsCapabilities.ShapeQueries);
    /// <summary>
    /// Writes into <paramref name="results"/> the shapes that truly overlap <paramref name="shape"/> centred at
    /// <paramref name="center"/> (touching is not overlapping), in an order that depends only on the world's history,
    /// and returns how many overlap, like <see cref="Overlap(in RectF, in QueryFilter, Span{ShapeId})"/>: an explosion's
    /// radius, a sword's reach.
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeQueries"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The shape is invalid or the centre is not finite.</exception>
    int Overlap(in QueryShape shape, Vector2 center, in QueryFilter filter, Span<ShapeId> results) => throw Refused(nameof(Overlap), PhysicsCapabilities.ShapeQueries);
    /// <summary>
    /// Writes into <paramref name="results"/> the shapes that hold <paramref name="point"/> (inside them, not on their
    /// edge) and returns how many do: what is under the pointer.
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeQueries"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The point is not finite.</exception>
    int OverlapPoint(Vector2 point, in QueryFilter filter, Span<ShapeId> results) => throw Refused(nameof(OverlapPoint), PhysicsCapabilities.ShapeQueries);
    /// <summary>
    /// The surface nearest <paramref name="point"/> among the shapes within <paramref name="maxDistance"/> of it
    /// (shapes at the same distance: the first created); a shape that holds the point is at distance 0.
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ShapeQueries"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The point is not finite, or the distance is negative or not finite.</exception>
    bool ClosestPoint(Vector2 point, float maxDistance, in QueryFilter filter, out ClosestPointHit hit) => throw Refused(nameof(ClosestPoint), PhysicsCapabilities.ShapeQueries);

    /// <summary>
    /// Advances the world by <paramref name="dt"/> seconds: dynamic bodies move, and the events of this step replace the
    /// previous ones. No game code runs during it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dt"/> is negative or not finite.</exception>
    void Step(float dt);
    /// <summary>The contacts that began or ended in the latest step (pairs with a dynamic body only).</summary>
    ReadOnlySpan<ContactEvent> ContactEvents { get; }
    /// <summary>
    /// The impacts of the latest step: every pair with a dynamic body whose surfaces met faster than the backend's
    /// impact speed, with the point, the speed and the impulse (<see cref="PhysicsCapabilities.ContactDetail"/>). Empty
    /// on a backend that does not declare it.
    /// </summary>
    ReadOnlySpan<ContactImpact> ContactImpacts => [];
    /// <summary>
    /// Writes into <paramref name="results"/> the points at which <paramref name="body"/> touches other shapes now
    /// (up to two per pair of shapes), as the latest step left them, and returns how many there are (which may exceed
    /// the span's length). Contacts exist between a dynamic body and anything else: how hard a crate presses on a
    /// plate, whether a dynamic body stands on something.
    /// </summary>
    /// <exception cref="PhysicsCapabilityException">The backend does not declare <see cref="PhysicsCapabilities.ContactDetail"/>.</exception>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    int ContactsOf(BodyId body, Span<ContactPoint> results) => throw Refused(nameof(ContactsOf), PhysicsCapabilities.ContactDetail);
    /// <summary>The sensor entries and exits of the latest step.</summary>
    ReadOnlySpan<SensorEvent> SensorEvents { get; }
}
