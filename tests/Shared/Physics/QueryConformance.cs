using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// Shape queries (<see cref="PhysicsCapabilities.ShapeQueries"/>): every hit of a ray, cast boxes, circles and
/// capsules, shape and point overlaps, and the nearest surface. Distances are checked within the backend's
/// <see cref="IPhysicsWorld2D.Skin"/>.
/// </summary>
/// <remarks>
/// The scene: a floor whose top is at height 0 (x -200..200), a wall at x 50..60 from the floor to height 40, and a
/// sensor at x -80..-70.
/// </remarks>
public abstract class ShapeQueryConformance : CapabilityConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.ShapeQueries;

    private const ulong EnemyLayer = 2;
    private static readonly EntityId LevelOwner = new(2, 1), WallOwner = new(3, 1);

    private (IPhysicsWorld2D World, ShapeId Floor, ShapeId Wall, ShapeId Sensor) Scene()
    {
        var world = CreateWorld();
        var floor = Box(world, BodyDescription.Static(), -200, 200, -16, 0, owner: LevelOwner).Shape;
        var wall = Box(world, BodyDescription.Static(), 50, 60, 0, 40, owner: WallOwner).Shape;
        var sensor = Box(world, BodyDescription.Static(), -80, -70, 0, 40, s => s with { IsSensor = true }).Shape;
        return (world, floor, wall, sensor);
    }

    private Vector2 Size(float width, float height) => new(width * Scale, height * Scale);

    /// <summary>How far along a cast of <paramref name="length"/> suite units the shape may stop short: the skin, as a fraction.</summary>
    private float Slack(IPhysicsWorld2D world, float length) => Tolerance(world) / length;

    // ------------------------------------------------------------------ every hit of a ray

    [TestMethod]
    public void Every_hit_of_a_ray_comes_back_nearest_first_one_per_shape()
    {
        var world = CreateWorld();
        var far = Box(world, BodyDescription.Static(), 80, 90, 0, 20).Shape;
        var near = Box(world, BodyDescription.Static(), 20, 30, 0, 20).Shape;
        var middle = Box(world, BodyDescription.Static(), 50, 60, 0, 20, s => s with { Category = EnemyLayer }).Shape;
        Span<RayHit> hits = stackalloc RayHit[8];
        var from = At(world, 0, 10);
        var to = At(world, 100, 10);
        var everything = new QueryFilter(CollisionLayers.Default, CollisionLayers.All);
        Assert.AreEqual(3, world.RayCastAll(from, to, everything, hits));
        Assert.AreEqual(near, hits[0].Shape);
        Assert.AreEqual(middle, hits[1].Shape);
        Assert.AreEqual(far, hits[2].Shape);
        Assert.AreEqual(0.2f, hits[0].Fraction, Rounding);
        Assert.AreEqual(0.5f, hits[1].Fraction, Rounding);
        Assert.AreEqual(0.8f, hits[2].Fraction, Rounding);
        Assert.AreEqual(50, XOf(hits[1].Point), Rounding);
        Assert.AreEqual(10, HeightOf(world, hits[1].Point), Rounding);
        Assert.AreEqual(-1, hits[2].Normal.X, 1e-3f, "each hit has the normal of the face the ray entered");

        Assert.IsTrue(world.RayCast(from, to, everything, out var closest));
        Assert.AreEqual(closest.Shape, hits[0].Shape, "the first hit is the closest one");

        Span<RayHit> two = stackalloc RayHit[2];
        Assert.AreEqual(3, world.RayCastAll(from, to, everything, two), "the count includes the hits the span had no room for");
        Assert.AreEqual(near, two[0].Shape);
        Assert.AreEqual(middle, two[1].Shape);

        Assert.AreEqual(2, world.RayCastAll(from, to, new QueryFilter(CollisionLayers.Default, CollisionLayers.Default), hits), "the mask leaves the enemy layer out");
        Assert.AreEqual(far, hits[1].Shape);
        Assert.AreEqual(2, world.RayCastAll(At(world, 25, 10), to, everything, hits), "a shape that holds the start is not hit");
        Assert.AreEqual(middle, hits[0].Shape);
        Assert.AreEqual(0, world.RayCastAll(from, At(world, 10, 10), everything, hits));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.RayCastAll(new Vector2(float.NaN, 0), Vector2.Zero, default, []));
    }

    // ------------------------------------------------------------------ cast shapes

    [TestMethod]
    public void A_cast_box_stops_where_it_would_touch_and_reports_the_surface()
    {
        var (world, floor, wall, _) = Scene();
        var box = QueryShape.Box(Size(12, 14));
        // Down onto the floor: the centre stops 7 above it, half way along a 40-unit cast.
        Assert.IsTrue(world.ShapeCast(box, At(world, 0, 27), At(world, 0, -13), default, out var down));
        Assert.AreEqual(floor, down.Shape);
        Assert.AreEqual(LevelOwner, down.Owner);
        Assert.AreEqual(0.5f, down.Fraction, Slack(world, 40), $"fraction {down.Fraction}");
        Assert.AreEqual(1, Vector2.Dot(down.Normal, world.Up), 1e-3f, $"the floor's normal faces up: {down.Normal}");
        Assert.AreEqual(0, HeightOf(world, down.Point), Tolerance(world), "the point is on the floor");
        Assert.IsTrue(MathF.Abs(XOf(down.Point)) <= 6 + Rounding, $"under the box: {down.Point}");

        // Sideways into the wall: the centre stops 6 short of it.
        Assert.IsTrue(world.ShapeCast(box, At(world, 0, 20), At(world, 100, 20), default, out var side));
        Assert.AreEqual(wall, side.Shape);
        Assert.AreEqual(WallOwner, side.Owner);
        Assert.AreEqual(0.44f, side.Fraction, Slack(world, 100), $"fraction {side.Fraction}");
        Assert.AreEqual(-1, side.Normal.X, 1e-3f);
        Assert.AreEqual(50, XOf(side.Point), Tolerance(world));

        Assert.IsFalse(world.ShapeCast(box, At(world, 0, 27), At(world, 0, 10), default, out _), "a cast that ends before the floor hits nothing");
        Assert.IsFalse(world.ShapeCast(box, At(world, 0, 3), At(world, 0, -20), default, out _), "a shape it overlaps where it starts is not hit");
        Assert.IsTrue(world.ShapeCast(box, At(world, 55, 3), At(world, 55, 80), default, out _) is false, "nor on the way out of two shapes it starts in");
        Assert.IsFalse(world.ShapeCast(box, At(world, 0, 20), At(world, 100, 20), new QueryFilter(CollisionLayers.Default, EnemyLayer), out _), "the mask decides what is hit");
    }

    [TestMethod]
    public void A_cast_circle_goes_round_a_corner_a_box_would_catch()
    {
        var (world, floor, wall, _) = Scene();
        var circle = QueryShape.Circle(5 * Scale);
        Assert.IsTrue(world.ShapeCast(circle, At(world, 0, 25), At(world, 0, -15), default, out var down));
        Assert.AreEqual(floor, down.Shape);
        Assert.AreEqual(0.5f, down.Fraction, Slack(world, 40), $"fraction {down.Fraction}");
        Assert.AreEqual(0, XOf(down.Point), Tolerance(world), "a circle touches the floor right under its centre");

        // Passing 3 above the wall's top, the circle touches its corner (50, 40) when its centre is at x 46.
        Assert.IsTrue(world.ShapeCast(circle, At(world, 30, 43), At(world, 70, 43), default, out var corner));
        Assert.AreEqual(wall, corner.Shape);
        Assert.AreEqual(0.4f, corner.Fraction, 2 * Slack(world, 40), $"fraction {corner.Fraction}");
        Assert.AreEqual(50, XOf(corner.Point), Tolerance(world));
        Assert.AreEqual(40, HeightOf(world, corner.Point), Tolerance(world));
        Assert.AreEqual(-0.8f, corner.Normal.X, 0.05f, $"the normal goes from the corner to the centre: {corner.Normal}");
        Assert.AreEqual(0.6f, Vector2.Dot(corner.Normal, world.Up), 0.05f);

        // 6 above the top the circle clears the wall; a box of the same width does not clear it at 4.
        Assert.IsFalse(world.ShapeCast(circle, At(world, 30, 46), At(world, 70, 46), default, out _));
        Assert.IsTrue(world.ShapeCast(QueryShape.Box(Size(10, 10)), At(world, 30, 44), At(world, 70, 44), default, out var caught));
        Assert.AreEqual(wall, caught.Shape);
    }

    [TestMethod]
    public void A_cast_capsule_stands_along_up()
    {
        var (world, floor, wall, _) = Scene();
        var capsule = QueryShape.Capsule(4 * Scale, 20 * Scale);
        Assert.IsTrue(world.ShapeCast(capsule, At(world, 0, 30), At(world, 0, -10), default, out var down));
        Assert.AreEqual(floor, down.Shape);
        Assert.AreEqual(0.5f, down.Fraction, Slack(world, 40), $"its centre stops half its height above the floor: {down.Fraction}");
        Assert.IsTrue(world.ShapeCast(capsule, At(world, 0, 20), At(world, 100, 20), default, out var side));
        Assert.AreEqual(wall, side.Shape);
        Assert.AreEqual(0.46f, side.Fraction, Slack(world, 100), $"its side is its radius wide: {side.Fraction}");
    }

    [TestMethod]
    public void Casts_and_overlaps_refuse_invalid_shapes_and_points()
    {
        var (world, _, _, _) = Scene();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.ShapeCast(default, Vector2.Zero, Vector2.One, default, out _));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.ShapeCast(QueryShape.Circle(-1), Vector2.Zero, Vector2.One, default, out _));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.ShapeCast(QueryShape.Circle(1), new Vector2(float.NaN, 0), Vector2.One, default, out _));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.Overlap(QueryShape.Box(new Vector2(0, 1)), Vector2.Zero, default, []));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.Overlap(QueryShape.Circle(1), new Vector2(0, float.PositiveInfinity), default, []));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.OverlapPoint(new Vector2(float.NaN, 0), default, []));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.ClosestPoint(Vector2.Zero, -1, default, out _));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.ClosestPoint(Vector2.Zero, float.NaN, default, out _));
    }

    [TestMethod]
    public void A_one_way_platform_is_hit_by_a_cast_only_from_above()
    {
        var world = CreateWorld();
        if ((world.Capabilities & PhysicsCapabilities.OneWayPlatforms) == 0) Assert.Inconclusive("the backend has no one-way platforms");
        var platform = Box(world, BodyDescription.Static(), -50, 50, 30, 34, s => s with { OneWay = true }).Shape;
        var box = QueryShape.Box(Size(12, 14));
        Assert.IsTrue(world.ShapeCast(box, At(world, 0, 60), At(world, 0, 20), default, out var down));
        Assert.AreEqual(platform, down.Shape);
        Assert.IsTrue(down.Fraction <= 19f / 40 + Rounding && down.Fraction >= 19f / 40 - Slack(world, 40), $"fraction {down.Fraction}");
        Assert.IsFalse(world.ShapeCast(box, At(world, 0, 10), At(world, 0, 60), default, out _), "going up it is passed");
        Assert.IsFalse(world.ShapeCast(box, At(world, -80, 32), At(world, 0, 32), default, out _), "and sideways");
        Assert.IsFalse(world.ShapeCast(box, At(world, 0, 60), At(world, 0, 20), new QueryFilter(CollisionLayers.Default, CollisionLayers.All) { PassOneWay = true }, out _));
    }

    // ------------------------------------------------------------------ overlaps

    [TestMethod]
    public void A_circle_overlap_is_exact_not_a_bounding_box_test()
    {
        var (world, floor, wall, _) = Scene();
        var circle = QueryShape.Circle(5 * Scale);
        Span<ShapeId> found = stackalloc ShapeId[8];
        Assert.AreEqual(0, world.Overlap(circle, At(world, 44, 20), default, found), "one unit short of the wall");
        Assert.AreEqual(1, world.Overlap(circle, At(world, 46, 20), default, found));
        Assert.AreEqual(wall, found[0]);
        // Near the wall's corner (50, 40): the circle's bounding box reaches the wall, the circle does not.
        Assert.AreEqual(0, world.Overlap(circle, At(world, 46, 44), default, found), "5.66 from the corner");
        Assert.AreEqual(1, world.Overlap(circle, At(world, 47, 43), default, found), "4.24 from the corner");
        Assert.AreEqual(2, world.Overlap(circle, At(world, 48, 2), default, found[..1]), "the count includes what the span had no room for (floor and wall)");
        Assert.AreEqual(floor, found[0], "in creation order");
        Assert.AreEqual(0, world.Overlap(circle, At(world, 48, 2), new QueryFilter(CollisionLayers.Default, EnemyLayer), found));
    }

    [TestMethod]
    public void Box_and_capsule_overlaps_find_what_the_shape_covers_and_sensors_only_when_asked()
    {
        var (world, floor, wall, sensor) = Scene();
        Span<ShapeId> found = stackalloc ShapeId[8];
        Assert.AreEqual(1, world.Overlap(QueryShape.Box(Size(10, 10)), At(world, 46, 20), default, found));
        Assert.AreEqual(wall, found[0]);
        Assert.AreEqual(0, world.Overlap(QueryShape.Box(Size(10, 10)), At(world, 44, 20), default, found), "one unit away");
        var capsule = QueryShape.Capsule(4 * Scale, 20 * Scale);
        Assert.AreEqual(0, world.Overlap(capsule, At(world, 0, 11), default, found), "a capsule standing one unit above the floor");
        Assert.AreEqual(1, world.Overlap(capsule, At(world, 0, 9), default, found));
        Assert.AreEqual(floor, found[0]);
        Assert.AreEqual(0, world.Overlap(capsule, At(world, 45, 20), default, found), "its side is its radius wide");
        Assert.AreEqual(1, world.Overlap(capsule, At(world, 47, 20), default, found));

        Assert.AreEqual(0, world.Overlap(QueryShape.Circle(3 * Scale), At(world, -75, 20), default, found), "sensors are left out");
        Assert.AreEqual(1, world.Overlap(QueryShape.Circle(3 * Scale), At(world, -75, 20), new QueryFilter(CollisionLayers.Default, CollisionLayers.All) { IncludeSensors = true }, found));
        Assert.AreEqual(sensor, found[0]);
    }

    [TestMethod]
    public void A_point_finds_the_shapes_that_hold_it()
    {
        var (world, floor, wall, sensor) = Scene();
        Span<ShapeId> found = stackalloc ShapeId[8];
        Assert.AreEqual(1, world.OverlapPoint(At(world, 55, 20), default, found));
        Assert.AreEqual(wall, found[0]);
        Assert.AreEqual(1, world.OverlapPoint(At(world, 0, -5), default, found));
        Assert.AreEqual(floor, found[0]);
        Assert.AreEqual(0, world.OverlapPoint(At(world, 48, 20), default, found), "outside every shape");
        Assert.AreEqual(0, world.OverlapPoint(At(world, 55, 20), new QueryFilter(CollisionLayers.Default, EnemyLayer), found));
        Assert.AreEqual(0, world.OverlapPoint(At(world, -75, 20), default, found), "sensors are left out");
        Assert.AreEqual(1, world.OverlapPoint(At(world, -75, 20), new QueryFilter(CollisionLayers.Default, CollisionLayers.All) { IncludeSensors = true }, found));
        Assert.AreEqual(sensor, found[0]);
        Assert.AreEqual(1, world.OverlapPoint(At(world, 55, 20), default, []), "the count needs no room");
    }

    // ------------------------------------------------------------------ the nearest surface

    [TestMethod]
    public void The_closest_point_is_on_the_nearest_surface_within_reach()
    {
        var (world, floor, wall, _) = Scene();
        Assert.IsTrue(world.ClosestPoint(At(world, 40, 20), 15 * Scale, default, out var hit));
        Assert.AreEqual(wall, hit.Shape);
        Assert.AreEqual(WallOwner, hit.Owner);
        Assert.AreEqual(10, hit.Distance / Scale, Rounding);
        Assert.AreEqual(50, XOf(hit.Point), Rounding);
        Assert.AreEqual(20, HeightOf(world, hit.Point), Rounding);
        Assert.IsFalse(world.ClosestPoint(At(world, 40, 20), 5 * Scale, default, out _), "nothing within 5");

        Assert.IsTrue(world.ClosestPoint(At(world, 45, 3), 15 * Scale, default, out var low));
        Assert.AreEqual(floor, low.Shape, "the floor, 3 away, is nearer than the wall, 5 away");
        Assert.AreEqual(3, low.Distance / Scale, Rounding);
        Assert.AreEqual(0, HeightOf(world, low.Point), Rounding);

        var inside = At(world, 55, 20);
        Assert.IsTrue(world.ClosestPoint(inside, 15 * Scale, default, out var held));
        Assert.AreEqual(wall, held.Shape);
        Assert.AreEqual(0, held.Distance, "a shape that holds the point is at distance 0");
        Assert.AreEqual(inside, held.Point);
        Assert.IsFalse(world.ClosestPoint(At(world, 40, 20), 15 * Scale, new QueryFilter(CollisionLayers.Default, EnemyLayer), out _));
    }
}

/// <summary>
/// Contact detail (<see cref="PhysicsCapabilities.ContactDetail"/>): the impacts of a step with their speed, point and
/// impulse, the points a body touches at, and the point of a contact that begins.
/// </summary>
public abstract class ContactDetailConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.ContactDetail | PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.MassProperties;

    [TestMethod]
    public void A_falling_box_reports_its_impact_with_the_speed_the_point_and_the_impulse()
    {
        var world = CreateWorld();
        var floor = Floor(world);
        var (box, shape) = Box(world, BodyDescription.Dynamic(default), -8, 8, 60, 76, owner: Owner.A);
        Assert.AreEqual(0, world.ContactImpacts.Length);
        var impacts = new List<ContactImpact>();
        var begins = new List<ContactEvent>();
        float falling = 0, before = 0;
        for (int i = 0; i < 180; i++)
        {
            float speed = -SpeedUp(world, box);
            world.Step(1 / 60f);
            if (world.ContactImpacts.Length > 0 && impacts.Count == 0) before = MathF.Max(speed, falling);
            falling = speed;
            foreach (var impact in world.ContactImpacts) impacts.Add(impact);
            foreach (var e in world.ContactEvents)
                if (e.Kind == ContactEventKind.Begin) begins.Add(e);
        }
        Assert.IsTrue(impacts.Count >= 1, "the landing is an impact");
        var hit = impacts[0];
        Assert.IsTrue(hit.A == shape && hit.B == floor || hit.A == floor && hit.B == shape, hit.ToString());
        Assert.AreEqual(hit.A == shape ? Owner.A : Owner.Level, hit.OwnerA);
        Assert.AreEqual(hit.B == shape ? Owner.A : Owner.Level, hit.OwnerB);
        float speedInSuite = hit.Speed / Scale;
        Assert.IsTrue(before > 50, $"the box was falling: {before}");
        Assert.IsTrue(speedInSuite > 0.7f * before && speedInSuite < 1.3f * before, $"the speed of the impact is the speed it fell at: {speedInSuite}, falling at {before}");
        var fromFloor = hit.A == floor ? hit.Normal : -hit.Normal;
        Assert.AreEqual(1, Vector2.Dot(fromFloor, world.Up), 1e-2f, $"the normal points from A to B: {hit.Normal}");
        Assert.AreEqual(0, HeightOf(world, hit.Point), 4, $"the point is where they met: {hit.Point}");
        Assert.IsTrue(MathF.Abs(XOf(hit.Point)) <= 8 + Rounding, $"under the box: {hit.Point}");
        float momentum = world.MassOf(box) * hit.Speed;
        Assert.IsTrue(hit.Impulse > 0.5f * momentum && hit.Impulse < 2 * momentum, $"the impulse stopped the box: {hit.Impulse}, momentum {momentum}");

        Assert.AreEqual(1, begins.Count);
        Assert.AreEqual(0, HeightOf(world, begins[0].Point), 4, $"a contact that begins says where: {begins[0].Point}");
        Assert.IsTrue(MathF.Abs(XOf(begins[0].Point)) <= 8 + Rounding);
        world.Step(1 / 60f);
        Assert.AreEqual(0, world.ContactImpacts.Length, "a body at rest hits nothing");
    }

    [TestMethod]
    public void A_gentle_touch_is_not_an_impact_and_a_heavier_body_hits_harder()
    {
        var world = CreateWorld();
        Floor(world);
        var (gentle, _) = Box(world, BodyDescription.Dynamic(default), -108, -92, 0.5f, 16.5f);
        int count = 0;
        for (int i = 0; i < 60; i++)
        {
            world.Step(1 / 60f);
            count += world.ContactImpacts.Length;
        }
        Assert.AreEqual(0, count, "a box set down from half a unit makes no impact");
        Assert.IsTrue(world.IsAlive(gentle));

        var (light, lightShape) = Box(world, BodyDescription.Dynamic(default), -8, 8, 60, 76, s => s with { Density = 1 });
        var (heavy, heavyShape) = Box(world, BodyDescription.Dynamic(default), 92, 108, 60, 76, s => s with { Density = 4 });
        float lightImpulse = 0, heavyImpulse = 0, lightSpeed = 0, heavySpeed = 0;
        for (int i = 0; i < 180; i++)
        {
            world.Step(1 / 60f);
            foreach (var impact in world.ContactImpacts)
            {
                if ((impact.A == lightShape || impact.B == lightShape) && lightImpulse == 0) (lightImpulse, lightSpeed) = (impact.Impulse, impact.Speed);
                if ((impact.A == heavyShape || impact.B == heavyShape) && heavyImpulse == 0) (heavyImpulse, heavySpeed) = (impact.Impulse, impact.Speed);
            }
        }
        Assert.IsTrue(lightImpulse > 0 && heavyImpulse > 0, $"both landed: {lightImpulse}, {heavyImpulse}");
        Assert.AreEqual(lightSpeed, heavySpeed, 0.05f * lightSpeed, "they fall at the same speed");
        Assert.IsTrue(heavyImpulse > 3 * lightImpulse && heavyImpulse < 5 * lightImpulse, $"four times the mass, four times the impulse: {lightImpulse}, {heavyImpulse}");
        Assert.AreEqual(4 * world.MassOf(light), world.MassOf(heavy), 1e-3f * world.MassOf(heavy));
    }

    [TestMethod]
    public void A_body_lists_the_points_it_touches_at_and_how_hard_it_presses()
    {
        var world = CreateWorld();
        var floor = Floor(world);
        var floorBody = world.BodyOf(floor);
        var (light, lightShape) = Box(world, BodyDescription.Dynamic(default), -8, 8, 0, 16, s => s with { Density = 1 }, owner: Owner.A);
        var (heavy, _) = Box(world, BodyDescription.Dynamic(default), 92, 108, 0, 16, s => s with { Density = 4 }, owner: Owner.B);
        var (flying, _) = Box(world, BodyDescription.Dynamic(default) with { Dynamics = new BodyDynamics { GravityScale = 0 } }, -108, -92, 100, 116);
        Run(world, 2);
        Span<ContactPoint> points = stackalloc ContactPoint[8];
        int count = world.ContactsOf(light, points);
        Assert.AreEqual(2, count, "a box rests on two points");
        float press = 0;
        foreach (var p in points[..count])
        {
            Assert.AreEqual(lightShape, p.Shape);
            Assert.AreEqual(floor, p.Other);
            Assert.AreEqual(Owner.Level, p.OtherOwner);
            Assert.AreEqual(1, Vector2.Dot(p.Normal, world.Up), 1e-2f, $"the normal points from the floor to the body: {p.Normal}");
            Assert.AreEqual(0, HeightOf(world, p.Point), Tolerance(world) + 0.5f, $"on the floor: {p.Point}");
            Assert.AreEqual(8, MathF.Abs(XOf(p.Point)), 1, $"at the box's corners: {p.Point}");
            Assert.IsTrue(MathF.Abs(p.Separation) / Scale <= Tolerance(world) + 0.5f, $"touching: {p.Separation}");
            Assert.IsTrue(p.NormalImpulse >= 0);
            press += p.NormalImpulse;
        }
        Assert.IsTrue(press > 0, "the floor holds the box up");
        float heavyPress = 0;
        int heavyCount = world.ContactsOf(heavy, points);
        foreach (var p in points[..heavyCount]) heavyPress += p.NormalImpulse;
        Assert.AreEqual(4 * press, heavyPress, 0.2f * heavyPress, "four times the weight presses four times as hard");

        Assert.AreEqual(0, world.ContactsOf(flying, points), "a body in the air touches nothing");
        Assert.AreEqual(2, world.ContactsOf(light, points[..1]), "the count includes what the span had no room for");
        int under = world.ContactsOf(floorBody, points);
        Assert.AreEqual(4, under, "the floor touches both boxes");
        foreach (var p in points[..under])
        {
            Assert.AreEqual(floor, p.Shape);
            Assert.AreEqual(-1, Vector2.Dot(p.Normal, world.Up), 1e-2f, "seen from the floor the normal points down");
            Assert.IsTrue(p.OtherOwner == Owner.A || p.OtherOwner == Owner.B);
        }
        world.DestroyBody(light);
        Assert.ThrowsExactly<InvalidOperationException>(() => world.ContactsOf(light, []));
    }
}
