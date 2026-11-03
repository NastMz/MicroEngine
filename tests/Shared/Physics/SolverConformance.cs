using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// The helpers of the suites for solver capabilities (dynamic bodies and what they enable): gravity is whatever the
/// backend applies along -<see cref="IPhysicsWorld2D.Up"/>, time goes in 60 Hz steps, and resting is checked within the
/// backend's <see cref="IPhysicsWorld2D.Skin"/> on both sides (a solver lets bodies sink by its slop).
/// </summary>
public abstract class SolverConformance : CapabilityConformance
{
    /// <summary>Steps <paramref name="world"/> for <paramref name="seconds"/> at 60 Hz, collecting its contact events.</summary>
    protected static List<ContactEvent> Run(IPhysicsWorld2D world, float seconds)
    {
        var events = new List<ContactEvent>();
        for (int i = 0, n = (int)MathF.Round(seconds * 60); i < n; i++)
        {
            world.Step(1 / 60f);
            foreach (var e in world.ContactEvents) events.Add(e);
        }
        return events;
    }

    /// <summary>A static floor whose top is at height 0, wide enough for every scene.</summary>
    protected ShapeId Floor(IPhysicsWorld2D world) => Box(world, BodyDescription.Static(), -2000, 2000, -32, 0, owner: Owner.Level).Shape;

    /// <summary>The owners of the scenes' bodies.</summary>
    protected static class Owner
    {
        public static readonly EntityId Level = new(2, 1), A = new(5, 1), B = new(6, 1);
    }

    /// <summary>Asserts |<paramref name="value"/> - <paramref name="expected"/>| is within the backend's skin (suite units).</summary>
    protected void AssertNear(IPhysicsWorld2D world, float expected, float value, string what) =>
        Assert.IsTrue(MathF.Abs(value - expected) <= Tolerance(world), $"{what}: {value}, expected {expected} within {Tolerance(world)}");

    /// <summary>A velocity of (<paramref name="dx"/>, <paramref name="dh"/>) suite units per second in world units.</summary>
    protected Vector2 Speed(IPhysicsWorld2D world, float dx, float dh) => By(world, dx, dh);

    /// <summary>The velocity of <paramref name="body"/> along x, in suite units per second.</summary>
    protected float SpeedX(IPhysicsWorld2D world, BodyId body) => XOf(world.VelocityOf(body));

    /// <summary>The velocity of <paramref name="body"/> along up, in suite units per second.</summary>
    protected float SpeedUp(IPhysicsWorld2D world, BodyId body) => HeightOf(world, world.VelocityOf(body));
}

/// <summary>Dynamic bodies: gravity, resting contact, velocities and contact events (pairs with a dynamic body).</summary>
public abstract class DynamicBodyConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.DynamicBodies;

    [TestMethod]
    public void A_dynamic_box_falls_and_rests_on_the_floor_with_one_begin_contact()
    {
        var world = CreateWorld();
        var floor = Floor(world);
        var (box, shape) = Box(world, BodyDescription.Dynamic(default), -8, 8, 40, 56, owner: Owner.A);
        Assert.AreEqual(BodyKind.Dynamic, world.KindOf(box));
        world.Step(1 / 60f);
        Assert.IsTrue(SpeedUp(world, box) < 0, "gravity pulls it down");
        var events = Run(world, 3);
        AssertNear(world, 8, HeightOf(world, world.PositionOf(box)), "its centre rests half its height above the floor");
        Assert.AreEqual(0, world.VelocityOf(box).Length() / Scale, 1, "at rest");
        var begins = events.Where(e => e.Kind == ContactEventKind.Begin).ToList();
        Assert.AreEqual(1, begins.Count, string.Join(", ", events));
        var begin = begins[0];
        Assert.IsTrue(begin.A == shape && begin.B == floor || begin.A == floor && begin.B == shape, begin.ToString());
        var fromFloor = begin.A == floor ? begin.Normal : -begin.Normal;
        Assert.AreEqual(1, Vector2.Dot(fromFloor, world.Up), 1e-3f, $"the normal points from A to B: {begin.Normal}");
        Assert.AreEqual(begin.A == shape ? Owner.A : Owner.Level, begin.OwnerA);
    }

    [TestMethod]
    public void Velocity_is_set_and_read_in_world_units_and_moves_the_body()
    {
        var world = CreateWorld();
        var (box, _) = Box(world, BodyDescription.Dynamic(default), -8, 8, 400, 416);
        world.SetVelocity(box, Speed(world, 120, 60));
        Assert.AreEqual(120, SpeedX(world, box), 1e-3f);
        Assert.AreEqual(60, SpeedUp(world, box), 1e-3f);
        var start = world.PositionOf(box);
        Run(world, 0.5f);
        Assert.AreEqual(60, XOf(world.PositionOf(box) - start), 1, "x moves at its speed: no gravity along x");
        Assert.IsTrue(SpeedUp(world, box) < 60, "gravity slows the rise");
    }

    [TestMethod]
    public void A_contact_ends_when_the_bodies_part_and_when_a_shape_is_destroyed()
    {
        var world = CreateWorld();
        var floor = Floor(world);
        var (box, shape) = Box(world, BodyDescription.Dynamic(default), -8, 8, 1, 17);
        Run(world, 1);
        world.SetVelocity(box, Speed(world, 0, 400));
        var parted = Run(world, 0.2f);
        Assert.AreEqual(1, parted.Count(e => e.Kind == ContactEventKind.End), string.Join(", ", parted));
        Assert.AreEqual(Vector2.Zero, parted.First(e => e.Kind == ContactEventKind.End).Normal, "an end has no normal");
        Run(world, 3);
        world.DestroyBody(box);
        world.Step(1 / 60f);
        var end = world.ContactEvents.ToArray().Single(e => e.Kind == ContactEventKind.End);
        Assert.IsTrue(end.A == shape || end.B == shape, "the end still names the destroyed shape");
        Assert.IsTrue(end.A == floor || end.B == floor);
        Assert.IsFalse(world.IsAlive(shape));
    }

    [TestMethod]
    public void A_sensor_sees_a_dynamic_body_fall_through_it()
    {
        var world = CreateWorld();
        Floor(world);
        var (_, sensor) = Box(world, BodyDescription.Static(), -32, 32, 40, 72, s => s with { IsSensor = true });
        var (_, shape) = Box(world, BodyDescription.Dynamic(default), -4, 4, 120, 128);
        var events = new List<SensorEvent>();
        for (int i = 0; i < 240; i++)
        {
            world.Step(1 / 60f);
            events.AddRange(world.SensorEvents.ToArray());
        }
        CollectionAssert.AreEqual(new[] { SensorEventKind.Enter, SensorEventKind.Exit }, events.Select(e => e.Kind).ToArray());
        Assert.IsTrue(events.All(e => e.Sensor == sensor && e.Visitor == shape));
    }

    [TestMethod]
    public void A_kinematic_move_is_blocked_by_a_dynamic_body()
    {
        var world = CreateWorld();
        Floor(world);
        var (crate, crateShape) = Box(world, BodyDescription.Dynamic(default), 40, 56, 0, 16);
        Run(world, 1);
        var (player, _) = Box(world, BodyDescription.Kinematic(default), -6, 6, 1, 15);
        MoveResult result = default;
        for (int i = 0; i < 20; i++) result = world.Move(player, By(world, 4, 0));
        Assert.IsTrue(XOf(world.PositionOf(player)) + 6 <= XOf(world.PositionOf(crate)) - 8 + Tolerance(world), "stopped at the crate");
        Assert.IsTrue(result.HitWall && result.Contacts.ToArray().Any(c => c.Shape == crateShape), result.ToString());
    }
}

/// <summary>Rotation: bodies created turned, and bodies the solver turns.</summary>
public abstract class RotationConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.Rotation;

    [TestMethod]
    public void A_body_created_turned_reports_its_angle_and_its_box_is_turned()
    {
        var world = CreateWorld();
        var turned = world.CreateBody(Owner.Level, BodyDescription.Static(At(world, 0, 0)) with { Rotation = MathF.PI / 4 });
        var shape = world.AddShape(turned, ShapeDescription.Box(new Vector2(20, 20) * Scale));
        Assert.AreEqual(MathF.PI / 4, world.RotationOf(turned), 0.005f, "within the rounding of the backend's angle math");
        // A diamond: a ray down at x = 5 meets its upper-right face, which faces up and right.
        Assert.IsTrue(world.RayCast(At(world, 5, 40), At(world, 5, -40), default, out var hit));
        Assert.AreEqual(shape, hit.Shape);
        Assert.AreEqual(MathF.Sqrt(2) * 10 - 5, HeightOf(world, hit.Point), 0.01f, "the corner is at 10√2");
        Assert.AreEqual(MathF.Sqrt(0.5f), Vector2.Dot(hit.Normal, world.Up), 1e-3f, $"a 45° face: {hit.Normal}");
        Assert.AreEqual(MathF.Sqrt(0.5f), hit.Normal.X, 1e-3f);
    }

    [TestMethod]
    public void A_box_dropped_on_its_corner_tumbles_and_comes_to_rest_flat()
    {
        var world = CreateWorld();
        Floor(world);
        var body = world.CreateBody(Owner.A, BodyDescription.Dynamic(At(world, 0, 40)) with { Rotation = 0.6f });
        world.AddShape(body, ShapeDescription.Box(new Vector2(16, 16) * Scale));
        Run(world, 4);
        float angle = world.RotationOf(body);
        float quarter = MathF.Round(angle / (MathF.PI / 2)) * (MathF.PI / 2);
        Assert.AreEqual(quarter, angle, 0.02f, "flat on one of its sides");
        Assert.AreNotEqual(0.6f, angle, "the solver turned it");
        AssertNear(world, 8, HeightOf(world, world.PositionOf(body)), "resting on a side");
    }
}

/// <summary>Circles: exact round geometry for rays, overlaps and resting.</summary>
public abstract class CircleConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.Circles;

    [TestMethod]
    public void Rays_and_overlaps_see_the_circle_not_its_bounding_box()
    {
        var world = CreateWorld();
        var body = world.CreateBody(Owner.Level, BodyDescription.Static(At(world, 0, 0)));
        var circle = world.AddShape(body, ShapeDescription.Circle(10 * Scale));
        Assert.IsTrue(world.RayCast(At(world, 6, 40), At(world, 6, -40), default, out var hit));
        Assert.AreEqual(circle, hit.Shape);
        Assert.AreEqual(8, HeightOf(world, hit.Point), 0.01f, "a circle of radius 10 at x 6 is 8 high");
        Assert.AreEqual(0.6f, hit.Normal.X, 1e-3f, $"the normal is radial: {hit.Normal}");
        Span<ShapeId> found = stackalloc ShapeId[2];
        Assert.AreEqual(0, world.Overlap(Area(world, 8, 10, 8, 10), default, found), "inside the bounding box, outside the circle");
        Assert.AreEqual(1, world.Overlap(Area(world, 5, 7, 5, 7), default, found));
    }

    [TestMethod]
    public void A_dynamic_circle_rests_on_the_floor_one_radius_up()
    {
        var world = CreateWorld();
        Floor(world);
        var ball = world.CreateBody(Owner.A, BodyDescription.Dynamic(At(world, 0, 50)));
        world.AddShape(ball, ShapeDescription.Circle(8 * Scale));
        Run(world, 3);
        AssertNear(world, 8, HeightOf(world, world.PositionOf(ball)), "a ball of radius 8");
    }

    private RectF Area(IPhysicsWorld2D world, float x0, float x1, float h0, float h1)
    {
        var a = At(world, x0, h0);
        var b = At(world, x1, h1);
        return new RectF(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Abs(b.X - a.X), MathF.Abs(b.Y - a.Y));
    }
}

/// <summary>Capsules: a capsule as a shape and as the shape <see cref="IPhysicsWorld2D.Move"/> moves.</summary>
public abstract class CapsuleConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.Capsules;

    private (IPhysicsWorld2D World, ShapeId Floor, ShapeId Wall, BodyId Player) Scene()
    {
        var world = CreateWorld();
        var floor = Floor(world);
        var (_, wall) = Box(world, BodyDescription.Static(), 100, 116, 0, 64);
        var player = world.CreateBody(Owner.A, BodyDescription.Kinematic(At(world, 0, 20)));
        world.AddShape(player, ShapeDescription.Capsule(6 * Scale, 24 * Scale));
        return (world, floor, wall, player);
    }

    [TestMethod]
    public void A_falling_capsule_lands_on_the_floor_within_the_skin()
    {
        var (world, floor, _, player) = Scene();
        MoveResult result = default;
        for (int i = 0; i < 20; i++) result = world.Move(player, By(world, 0, -4));
        AssertNear(world, 12, HeightOf(world, world.PositionOf(player)), "a capsule 24 tall rests on its round end");
        Assert.IsTrue(result.OnGround && result.Ground == floor, result.ToString());
    }

    [TestMethod]
    public void A_walking_capsule_stops_at_a_wall()
    {
        var (world, _, wall, player) = Scene();
        for (int i = 0; i < 20; i++) world.Move(player, By(world, 0, -4));
        MoveResult result = default;
        for (int i = 0; i < 40; i++) result = world.Move(player, By(world, 4, 0));
        AssertNear(world, 94, XOf(world.PositionOf(player)), "its side (radius 6) against the wall at x 100");
        Assert.IsTrue(result.HitWall && result.Contacts.ToArray().Any(c => c.Shape == wall), result.ToString());
        Assert.IsTrue(result.OnGround, "and still on the floor");
    }

    [TestMethod]
    public void A_ray_meets_the_capsule_round_side()
    {
        var (world, _, _, player) = Scene();
        Assert.IsTrue(world.RayCast(At(world, -40, 20), At(world, 40, 20), new QueryFilter(CollisionLayers.Default, CollisionLayers.All), out var hit));
        Assert.AreEqual(world.BodyOf(hit.Shape), player);
        Assert.AreEqual(-6, XOf(hit.Point), 0.01f);
        Assert.AreEqual(-1, hit.Normal.X, 1e-3f);
    }
}

/// <summary>Convex polygons.</summary>
public abstract class PolygonConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.Polygons;

    [TestMethod]
    public void A_ray_meets_the_slope_of_a_triangle_with_its_face_normal()
    {
        var world = CreateWorld();
        var body = world.CreateBody(Owner.Level, BodyDescription.Static(At(world, 0, 0)));
        // A ramp rising to the right: (0, 0), (40, 0), (40, 20).
        var ramp = world.AddShape(body, ShapeDescription.Polygon(new[] { By(world, 0, 0), By(world, 40, 0), By(world, 40, 20) }));
        Assert.IsTrue(world.RayCast(At(world, 20, 60), At(world, 20, -10), default, out var hit));
        Assert.AreEqual(ramp, hit.Shape);
        Assert.AreEqual(10, HeightOf(world, hit.Point), 0.01f, "half way up the slope");
        var expected = Vector2.Normalize(new Vector2(-1, 2));
        Assert.AreEqual(expected.X, hit.Normal.X, 1e-3f, hit.Normal.ToString());
        Assert.AreEqual(expected.Y, Vector2.Dot(hit.Normal, world.Up), 1e-3f);
    }

    [TestMethod]
    public void Collinear_points_are_refused_and_change_nothing()
    {
        var world = CreateWorld();
        var body = world.CreateBody(Owner.Level, BodyDescription.Static());
        Assert.ThrowsExactly<ArgumentException>(() => world.AddShape(body, ShapeDescription.Polygon(new[] { By(world, 0, 0), By(world, 10, 0), By(world, 20, 0) })));
        Span<ShapeId> found = stackalloc ShapeId[1];
        Assert.AreEqual(0, world.Overlap(new RectF(-100 * Scale, -100 * Scale, 200 * Scale, 200 * Scale), default, found));
    }

    [TestMethod]
    public void A_dynamic_hexagon_rests_on_a_flat_side()
    {
        var world = CreateWorld();
        Floor(world);
        var points = Enumerable.Range(0, 6).Select(i => By(world, 10 * MathF.Cos(i * MathF.PI / 3), 10 * MathF.Sin(i * MathF.PI / 3))).ToArray();
        var hexagon = world.CreateBody(Owner.A, BodyDescription.Dynamic(At(world, 0, 30)) with { Rotation = 0.2f });
        world.AddShape(hexagon, ShapeDescription.Polygon(points));
        Run(world, 4);
        AssertNear(world, 10 * MathF.Sqrt(3) / 2, HeightOf(world, world.PositionOf(hexagon)), "on a side, its centre is the apothem up");
    }
}

/// <summary>Continuous collision: a fast bullet does not pass through a thin body.</summary>
public abstract class ContinuousCollisionConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.ContinuousCollision | PhysicsCapabilities.DynamicBodies;

    [TestMethod]
    public void A_bullet_stops_at_a_thin_wall_it_would_cross_in_one_step()
    {
        foreach (var kind in new[] { BodyKind.Static, BodyKind.Kinematic })
        {
            var world = CreateWorld();
            Box(world, new BodyDescription(kind, default), 200, 202, -100, 100);
            var (bullet, _) = Box(world, BodyDescription.Dynamic(default) with { Bullet = true }, -2, 2, -2, 2);
            world.SetVelocity(bullet, Speed(world, 60000, 0)); // 1000 suite units a step against a 2-unit wall
            Run(world, 0.2f);
            Assert.IsTrue(XOf(world.PositionOf(bullet)) < 200, $"{kind} wall: the bullet is at x {XOf(world.PositionOf(bullet))}");
        }
    }
}

/// <summary>Friction and restitution per shape.</summary>
public abstract class FrictionRestitutionConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.FrictionRestitution | PhysicsCapabilities.DynamicBodies;

    private float BounceHeight(float restitution)
    {
        var world = CreateWorld();
        var floor = world.CreateBody(Owner.Level, BodyDescription.Static(At(world, 0, -16)));
        world.AddShape(floor, ShapeDescription.Box(new Vector2(4000, 32) * Scale) with { Restitution = restitution });
        var (ball, _) = Box(world, BodyDescription.Dynamic(default), -4, 4, 100, 108, s => s with { Restitution = restitution });
        float best = 0;
        bool landed = false;
        for (int i = 0; i < 240; i++)
        {
            world.Step(1 / 60f);
            float h = HeightOf(world, world.PositionOf(ball)) - 4;
            if (!landed && h < 2) landed = true;
            if (landed) best = MathF.Max(best, h);
        }
        Assert.IsTrue(landed, "it reached the floor");
        return best;
    }

    private float SlideDistance(float friction)
    {
        var world = CreateWorld();
        var floor = world.CreateBody(Owner.Level, BodyDescription.Static(At(world, 0, -16)));
        world.AddShape(floor, ShapeDescription.Box(new Vector2(40000, 32) * Scale) with { Friction = friction });
        var (box, _) = Box(world, BodyDescription.Dynamic(default), -8, 8, 0, 16, s => s with { Friction = friction });
        Run(world, 0.5f);
        world.SetVelocity(box, Speed(world, 100, 0));
        Run(world, 2);
        return XOf(world.PositionOf(box));
    }

    [TestMethod]
    public void A_bouncy_shape_bounces_back_up_and_a_dead_one_does_not()
    {
        float bouncy = BounceHeight(0.9f), dead = BounceHeight(0);
        Assert.IsTrue(bouncy > 60, $"restitution 0.9 bounces to {bouncy} of 100");
        Assert.IsTrue(dead < 5, $"restitution 0 bounces to {dead}");
    }

    [TestMethod]
    public void A_frictionless_box_keeps_sliding_and_a_rough_one_stops()
    {
        float smooth = SlideDistance(0), rough = SlideDistance(1);
        Assert.AreEqual(200, smooth, 5, "100 units a second for 2 seconds");
        Assert.IsTrue(rough < 40, $"friction 1 stops it within {rough}");
    }
}

/// <summary>Mass from density: a denser body pushes harder.</summary>
public abstract class MassPropertiesConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.MassProperties | PhysicsCapabilities.DynamicBodies;

    private float StruckSpeed(float density)
    {
        // In free fall side by side: gravity does not act along x, and no floor adds friction.
        var world = CreateWorld();
        var (hammer, _) = Box(world, BodyDescription.Dynamic(default), -40, -24, 0, 16, s => s with { Density = density });
        var (target, _) = Box(world, BodyDescription.Dynamic(default), 0, 16, 0, 16, s => s with { Density = 1 });
        world.SetVelocity(hammer, Speed(world, 300, 0));
        Run(world, 0.3f);
        return SpeedX(world, target);
    }

    [TestMethod]
    public void A_dense_box_sends_a_light_one_off_faster_than_a_box_of_its_own_density()
    {
        float equal = StruckSpeed(1), heavy = StruckSpeed(8);
        Assert.IsTrue(equal > 50, $"the struck box moves: {equal}");
        Assert.IsTrue(heavy > equal * 1.3f, $"density 8 sends it at {heavy}, density 1 at {equal}");
    }
}
