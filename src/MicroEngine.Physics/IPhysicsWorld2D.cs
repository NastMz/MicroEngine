using System.Numerics;
using MicroEngine.Scenes;

namespace MicroEngine.Physics;

/// <summary>The version of the 2D physics contract (<see cref="IPhysicsWorld2D"/>); a change to its rules raises it.</summary>
public static class PhysicsContract
{
    /// <summary>1: the common core and capabilities of the first two backends (tiles; Box2D v3).</summary>
    public const int Version = 1;
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
/// <item><b>Queries.</b> <see cref="RayCast"/> returns the closest hit; <see cref="Overlap"/> is exact (shapes that
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
/// <item><b>Stepping.</b> <see cref="PhysicsModule"/> steps registered worlds in the kernel's
/// <c>FramePhase.Physics</c>, inside the fixed step, and writes body poses to their owners' transforms; the scene world
/// interpolates them for drawing.</item>
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

    /// <summary>
    /// Advances the world by <paramref name="dt"/> seconds: dynamic bodies move, and the events of this step replace the
    /// previous ones. No game code runs during it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dt"/> is negative or not finite.</exception>
    void Step(float dt);
    /// <summary>The contacts that began or ended in the latest step (pairs with a dynamic body only).</summary>
    ReadOnlySpan<ContactEvent> ContactEvents { get; }
    /// <summary>The sensor entries and exits of the latest step.</summary>
    ReadOnlySpan<SensorEvent> SensorEvents { get; }
}
