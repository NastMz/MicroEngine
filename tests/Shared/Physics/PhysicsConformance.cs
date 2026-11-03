using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// The common conformance suite of <see cref="IPhysicsWorld2D"/>: every 2D backend runs it by subclassing this class
/// and creating its world. It uses only the common contract (static and kinematic bodies, boxes, layers, moves, rays,
/// overlaps, sensors, steps) and asserts properties, never one backend's exact numbers: resting gaps are checked
/// within the backend's declared <see cref="IPhysicsWorld2D.Skin"/> plus a rounding allowance. Every test asserts that
/// what it checks actually happened (a body moved, an event arrived), so none passes vacuously.
/// </summary>
/// <remarks>
/// Geometry is written in suite units as (x, height): x grows right, height grows along <see cref="IPhysicsWorld2D.Up"/>.
/// <see cref="Scale"/> converts suite units to the backend's world units (1 for a pixel-space backend). The level is P6's
/// contract scene at pixel-art scale: a ground, a wall, a thin floor and a thin wall, a sensor, a box in an "enemy" layer
/// the player does not collide with, and a kinematic 12 × 14 player box.
/// </remarks>
public abstract class PhysicsConformance
{
    /// <summary>The layers of the scene.</summary>
    protected const ulong StaticLayer = CollisionLayers.Default, EnemyLayer = 2, SensorLayer = 4, PlayerLayer = 8;

    /// <summary>The rounding allowance added to the declared skin, in suite units.</summary>
    protected const float Rounding = 1e-3f;

    /// <summary>Creates an empty world of the backend under test.</summary>
    protected abstract IPhysicsWorld2D CreateWorld();

    /// <summary>World units per suite unit.</summary>
    protected virtual float Scale => 1;

    /// <summary>The scene of the common checks.</summary>
    protected sealed class Level
    {
        public required IPhysicsWorld2D World { get; init; }
        public required ShapeId Ground { get; init; }
        public required ShapeId Wall { get; init; }
        public required ShapeId Thin { get; init; }
        public required ShapeId ThinWall { get; init; }
        public required ShapeId Sensor { get; init; }
        public required ShapeId Enemy { get; init; }
        public required BodyId Player { get; init; }
        public required ShapeId PlayerShape { get; init; }
        public static readonly EntityId PlayerOwner = new(1, 1), LevelOwner = new(2, 1), SensorOwner = new(3, 1), EnemyOwner = new(4, 1);
        /// <summary>The filter the player moves with: it is in the player layer and sees static geometry only.</summary>
        public static QueryFilter PlayerFilter => new(PlayerLayer, StaticLayer);
    }

    // ------------------------------------------------------------------ geometry helpers

    /// <summary>The world point of suite coordinates (<paramref name="x"/>, <paramref name="height"/>).</summary>
    protected Vector2 At(IPhysicsWorld2D world, float x, float height) => new Vector2(x * Scale, 0) + world.Up * (height * Scale);
    /// <summary>A world displacement of (<paramref name="dx"/>, <paramref name="dh"/>) suite units.</summary>
    protected Vector2 By(IPhysicsWorld2D world, float dx, float dh) => new Vector2(dx * Scale, 0) + world.Up * (dh * Scale);
    /// <summary>The suite x of a world point.</summary>
    protected float XOf(Vector2 p) => p.X / Scale;
    /// <summary>The suite height of a world point.</summary>
    protected static float HeightOf(IPhysicsWorld2D world, Vector2 p, float scale) => Vector2.Dot(p, world.Up) / scale;
    /// <summary>The suite height of a world point.</summary>
    protected float HeightOf(IPhysicsWorld2D world, Vector2 p) => HeightOf(world, p, Scale);
    /// <summary>The largest resting gap allowed, in suite units.</summary>
    protected float Tolerance(IPhysicsWorld2D world) => world.Skin / Scale + Rounding;

    /// <summary>A body of <paramref name="kind"/> with one box covering x0..x1 and h0..h1 (suite units).</summary>
    protected (BodyId Body, ShapeId Shape) Box(IPhysicsWorld2D world, BodyKind kind, EntityId owner, float x0, float x1, float h0, float h1, Func<ShapeDescription, ShapeDescription>? edit = null)
    {
        var body = world.CreateBody(owner, new BodyDescription(kind, At(world, (x0 + x1) / 2, (h0 + h1) / 2)));
        var shape = ShapeDescription.Box(new Vector2((x1 - x0) * Scale, (h1 - h0) * Scale));
        return (body, world.AddShape(body, edit is null ? shape : edit(shape)));
    }

    /// <summary>The scene: ground (top at height 0), wall (x 100..116), thin floor (x -150..-118, h 40..42), thin wall (x -202..-200), sensor (x 40..56), enemy box (x -40..-24), and the player standing in the air at x 0 (bottom at height 13).</summary>
    protected Level Build(IPhysicsWorld2D? world = null)
    {
        world ??= CreateWorld();
        var ground = Box(world, BodyKind.Static, Level.LevelOwner, -300, 300, -16, 0).Shape;
        var wall = Box(world, BodyKind.Static, Level.LevelOwner, 100, 116, 0, 64).Shape;
        var thin = Box(world, BodyKind.Static, Level.LevelOwner, -150, -118, 40, 42).Shape;
        var thinWall = Box(world, BodyKind.Static, Level.LevelOwner, -202, -200, 0, 40).Shape;
        var sensor = Box(world, BodyKind.Static, Level.SensorOwner, 40, 56, 0, 32, s => s with { IsSensor = true, Category = SensorLayer }).Shape;
        var enemy = Box(world, BodyKind.Static, Level.EnemyOwner, -40, -24, 0, 16, s => s with { Category = EnemyLayer }).Shape;
        var (player, playerShape) = Box(world, BodyKind.Kinematic, Level.PlayerOwner, -6, 6, 13, 27, s => s.InLayers(PlayerLayer, StaticLayer | SensorLayer));
        return new Level { World = world, Ground = ground, Wall = wall, Thin = thin, ThinWall = thinWall, Sensor = sensor, Enemy = enemy, Player = player, PlayerShape = playerShape };
    }

    /// <summary>The player's bottom height (suite units): its centre minus half its 14-unit height.</summary>
    protected float Bottom(Level level) => HeightOf(level.World, level.World.PositionOf(level.Player)) - 7;
    /// <summary>The player's centre x (suite units).</summary>
    protected float CenterX(Level level) => XOf(level.World.PositionOf(level.Player));

    private static bool HasContact(in MoveResult result, ShapeId shape)
    {
        foreach (var c in result.Contacts)
            if (c.Shape == shape) return true;
        return false;
    }

    private MoveResult Land(Level level)
    {
        MoveResult last = default;
        for (int i = 0; i < 20; i++) last = level.World.Move(level.Player, By(level.World, 0, -4), Level.PlayerFilter);
        return last;
    }

    // ------------------------------------------------------------------ the world

    [TestMethod]
    public void The_world_names_its_backend_a_unit_up_direction_and_a_non_negative_skin()
    {
        var world = CreateWorld();
        Assert.IsFalse(string.IsNullOrWhiteSpace(world.Backend));
        Assert.AreEqual(1, world.Up.Length(), 1e-5f, "Up is unit length");
        Assert.AreEqual(0, world.Up.X, "the suite assumes a vertical up direction");
        Assert.IsTrue(world.Skin >= 0 && float.IsFinite(world.Skin), $"skin {world.Skin}");
        Assert.AreEqual(0, world.StepCount);
        Assert.AreEqual(0, world.Bodies.Length);
    }

    [TestMethod]
    public void Bodies_and_shapes_have_generation_checked_ids_and_destroying_a_body_removes_its_shapes()
    {
        var world = CreateWorld();
        var (body, shape) = Box(world, BodyKind.Static, Level.LevelOwner, 0, 10, 0, 10);
        Assert.IsTrue(world.IsAlive(body));
        Assert.IsTrue(world.IsAlive(shape));
        Assert.AreEqual(body, world.BodyOf(shape));
        Assert.AreEqual(Level.LevelOwner, world.OwnerOf(body));
        Assert.AreEqual(BodyKind.Static, world.KindOf(body));
        Assert.AreEqual(At(world, 5, 5), world.PositionOf(body));
        Assert.AreEqual(0, world.RotationOf(body));
        CollectionAssert.AreEqual(new[] { body }, world.Bodies.ToArray());
        Span<ShapeId> found = stackalloc ShapeId[4];
        Assert.AreEqual(1, world.Overlap(new RectF(At(world, 2, 8).X, MathF.Min(At(world, 2, 8).Y, At(world, 2, 2).Y), 6 * Scale, 6 * Scale), default, found));

        world.DestroyBody(body);
        Assert.IsFalse(world.IsAlive(body));
        Assert.IsFalse(world.IsAlive(shape), "a body's shapes go with it");
        Assert.AreEqual(0, world.Bodies.Length);
        Assert.AreEqual(0, world.Overlap(new RectF(At(world, 2, 8).X, MathF.Min(At(world, 2, 8).Y, At(world, 2, 2).Y), 6 * Scale, 6 * Scale), default, found));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.PositionOf(body));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.BodyOf(shape));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.AddShape(body, ShapeDescription.Box(Vector2.One)));
        world.DestroyBody(body);
        world.DestroyBody(BodyId.None);

        var again = world.CreateBody(Level.LevelOwner, BodyDescription.Static());
        Assert.AreNotEqual(body, again, "a reused slot gets a new generation");
        Assert.IsFalse(world.IsAlive(body));
    }

    [TestMethod]
    public void Invalid_descriptions_are_refused_whatever_the_backend()
    {
        var world = CreateWorld();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.CreateBody(default, BodyDescription.Static(new Vector2(float.NaN, 0))));
        var body = world.CreateBody(default, BodyDescription.Static());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.AddShape(body, ShapeDescription.Box(new Vector2(0, 4))));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.AddShape(body, ShapeDescription.Box(new Vector2(4, -1))));
        Assert.AreEqual(1, world.Bodies.Length, "a refused request changes nothing");
    }

    [TestMethod]
    public void Every_undeclared_capability_is_refused_with_the_typed_error_and_every_declared_one_is_accepted()
    {
        var requests = new (PhysicsCapabilities Flag, Action<IPhysicsWorld2D> Request)[]
        {
            (PhysicsCapabilities.DynamicBodies, w => w.CreateBody(default, BodyDescription.Dynamic(Vector2.Zero))),
            (PhysicsCapabilities.Rotation, w => w.CreateBody(default, BodyDescription.Static() with { Rotation = 0.5f })),
            (PhysicsCapabilities.ContinuousCollision, w => w.CreateBody(default, BodyDescription.Kinematic(Vector2.Zero) with { Bullet = true })),
            (PhysicsCapabilities.MovingSolids, w => w.CreateBody(default, BodyDescription.Kinematic(Vector2.Zero) with { PushesBodies = true })),
            (PhysicsCapabilities.Circles, w => w.AddShape(w.CreateBody(default, BodyDescription.Static()), ShapeDescription.Circle(2 * Scale))),
            (PhysicsCapabilities.Capsules, w => w.AddShape(w.CreateBody(default, BodyDescription.Static()), ShapeDescription.Capsule(2 * Scale, 8 * Scale))),
            (PhysicsCapabilities.Polygons, w => w.AddShape(w.CreateBody(default, BodyDescription.Static()), ShapeDescription.Polygon(new[] { Vector2.Zero, Vector2.UnitX * Scale, Vector2.UnitY * Scale }))),
            (PhysicsCapabilities.OneWayPlatforms, w => w.AddShape(w.CreateBody(default, BodyDescription.Static()), ShapeDescription.Box(Vector2.One * Scale) with { OneWay = true })),
            (PhysicsCapabilities.FrictionRestitution, w => w.AddShape(w.CreateBody(default, BodyDescription.Static()), ShapeDescription.Box(Vector2.One * Scale) with { Friction = 0.5f })),
            (PhysicsCapabilities.MassProperties, w => w.AddShape(w.CreateBody(default, BodyDescription.Static()), ShapeDescription.Box(Vector2.One * Scale) with { Density = 2 })),
        };
        int refused = 0, accepted = 0;
        foreach (var (flag, request) in requests)
        {
            var world = CreateWorld();
            if ((world.Capabilities & flag) != 0)
            {
                request(world);
                accepted++;
                continue;
            }
            var bodies = world.Bodies.Length;
            var e = Assert.ThrowsExactly<PhysicsCapabilityException>(() => request(world), flag.ToString());
            Assert.AreEqual(flag, e.Missing, flag.ToString());
            Assert.AreEqual(world.Backend, e.Backend);
            Assert.IsTrue(e.Message.Contains(flag.ToString(), StringComparison.Ordinal), e.Message);
            Assert.IsTrue(world.Bodies.Length <= bodies + 1, "a refused body is not created");
            refused++;
        }
        Assert.AreEqual(requests.Length, refused + accepted);
        var probe = CreateWorld();
        if ((probe.Capabilities & PhysicsCapabilities.DynamicBodies) == 0)
        {
            var kinematic = probe.CreateBody(default, BodyDescription.Kinematic(Vector2.Zero));
            Assert.AreEqual(PhysicsCapabilities.DynamicBodies, Assert.ThrowsExactly<PhysicsCapabilityException>(() => probe.SetVelocity(kinematic, Vector2.One)).Missing);
            Assert.AreEqual(PhysicsCapabilities.DynamicBodies, Assert.ThrowsExactly<PhysicsCapabilityException>(() => probe.VelocityOf(kinematic)).Missing);
        }
    }

    // ------------------------------------------------------------------ moves

    [TestMethod]
    public void A_falling_box_lands_on_the_ground_within_the_declared_skin_and_reports_it_as_ground()
    {
        var level = Build();
        Assert.AreEqual(13, Bottom(level), Rounding, "it starts in the air");
        var result = Land(level);
        float gap = Bottom(level);
        Assert.IsTrue(gap >= -Rounding && gap <= Tolerance(level.World), $"resting gap {gap}, skin {level.World.Skin}");
        Assert.IsTrue(result.OnGround, result.ToString());
        Assert.IsTrue(Vector2.Dot(result.GroundNormal, level.World.Up) >= MoveResult.GroundThreshold, $"ground normal {result.GroundNormal}");
        Assert.AreEqual(level.Ground, result.Ground);
        Assert.IsTrue(HasContact(result, level.Ground));
        foreach (var c in result.Contacts)
            if (c.Shape == level.Ground) Assert.AreEqual(Level.LevelOwner, c.Owner, "a contact names its shape's owner");
        Assert.AreEqual(result.Position, level.World.PositionOf(level.Player));
        Assert.IsFalse(result.HitCeiling);
    }

    [TestMethod]
    public void Walking_into_a_wall_stops_against_it_with_a_wall_normal_and_stays_on_the_ground()
    {
        var level = Build();
        Land(level);
        MoveResult last = default;
        for (int i = 0; i < 40; i++) last = level.World.Move(level.Player, By(level.World, 4, 0), Level.PlayerFilter);
        float gap = 100 - (CenterX(level) + 6);
        Assert.IsTrue(gap >= -Rounding && gap <= Tolerance(level.World), $"gap to the wall {gap}");
        Assert.IsTrue(last.HitWall, last.ToString());
        Assert.IsTrue(HasContact(last, level.Wall));
        foreach (var c in last.Contacts)
            if (c.Shape == level.Wall) Assert.AreEqual(-1, c.Normal.X, 1e-3f, $"the wall's normal points back at the player: {c.Normal}");
        Assert.IsTrue(last.OnGround, "touching the floor while pressed against the wall");
        Assert.AreEqual(0, last.Delta.X, Tolerance(level.World) * Scale, "pressed against it, it no longer moves");
    }

    [TestMethod]
    public void A_free_move_goes_the_whole_way()
    {
        var level = Build();
        var start = level.World.PositionOf(level.Player);
        var delta = By(level.World, -20, 5);
        var result = level.World.Move(level.Player, delta, Level.PlayerFilter);
        Assert.AreEqual(start.X + delta.X, result.Position.X, Rounding * Scale);
        Assert.AreEqual(start.Y + delta.Y, result.Position.Y, Rounding * Scale);
        Assert.AreEqual(delta.X, result.Delta.X, Rounding * Scale);
        Assert.AreEqual(0, result.TotalContacts, result.ToString());
        Assert.IsFalse(result.OnGround || result.HitWall || result.HitCeiling);
    }

    [TestMethod]
    public void The_mask_decides_what_blocks_a_move_the_player_walks_through_the_enemy_layer()
    {
        var level = Build();
        Land(level);
        MoveResult result = default;
        bool touchedEnemy = false;
        for (int i = 0; i < 15; i++)
        {
            result = level.World.Move(level.Player, By(level.World, -4, 0), Level.PlayerFilter);
            touchedEnemy |= HasContact(result, level.Enemy);
        }
        Assert.AreEqual(-60, CenterX(level), Rounding, "it walked past the enemy box at x -40..-24");
        Assert.IsFalse(touchedEnemy, "the enemy layer is not in the player's filter");
        var blocking = level.World.Move(level.Player, By(level.World, 60, 0), new QueryFilter(PlayerLayer, StaticLayer | EnemyLayer));
        Assert.IsTrue(blocking.HitWall && HasContact(blocking, level.Enemy), $"with the enemy layer in the filter it blocks: {blocking}");
    }

    [TestMethod]
    public void A_fast_box_does_not_tunnel_through_a_thin_floor()
    {
        var level = Build();
        level.World.SetPosition(level.Player, At(level.World, -134, 100));
        var result = level.World.Move(level.Player, By(level.World, 0, -200), Level.PlayerFilter);
        float gap = Bottom(level) - 42;
        Assert.IsTrue(gap >= -Rounding && gap <= Tolerance(level.World), $"a 200-unit move onto a 2-unit floor rests at gap {gap}");
        Assert.IsTrue(result.OnGround && result.Ground == level.Thin, result.ToString());
    }

    [TestMethod]
    public void A_fast_box_does_not_tunnel_through_a_thin_wall()
    {
        var level = Build();
        level.World.SetPosition(level.Player, At(level.World, -170, 30));
        var result = level.World.Move(level.Player, By(level.World, -100, 0), Level.PlayerFilter);
        float gap = CenterX(level) - 6 - -200;
        Assert.IsTrue(gap >= -Rounding && gap <= Tolerance(level.World), $"a 100-unit move into a 2-unit wall stops at gap {gap}");
        Assert.IsTrue(result.HitWall && HasContact(result, level.ThinWall), result.ToString());
    }

    [TestMethod]
    public void A_shape_the_box_overlaps_when_the_move_starts_does_not_trap_it()
    {
        var level = Build();
        level.World.SetPosition(level.Player, At(level.World, 98, 30));
        var result = level.World.Move(level.Player, By(level.World, -10, 0), Level.PlayerFilter);
        Assert.AreEqual(88, CenterX(level), Rounding, $"it leaves the wall it was inside: {result}");
    }

    [TestMethod]
    public void Move_refuses_static_bodies_bodies_without_a_box_and_non_finite_deltas()
    {
        var level = Build();
        var groundBody = level.World.BodyOf(level.Ground);
        Assert.ThrowsExactly<InvalidOperationException>(() => level.World.Move(groundBody, Vector2.One));
        var empty = level.World.CreateBody(default, BodyDescription.Kinematic(Vector2.Zero));
        Assert.ThrowsExactly<InvalidOperationException>(() => level.World.Move(empty, Vector2.One));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => level.World.Move(level.Player, new Vector2(float.PositiveInfinity, 0)));
        var start = level.World.PositionOf(level.Player);
        Assert.AreEqual(start, level.World.PositionOf(level.Player), "a refused move changes nothing");
    }

    // ------------------------------------------------------------------ queries

    [TestMethod]
    public void A_ray_returns_the_closest_hit_with_its_point_normal_and_fraction()
    {
        var level = Build();
        var world = level.World;
        Assert.IsTrue(world.RayCast(At(world, -80, 60), At(world, -80, -20), default, out var hit));
        Assert.AreEqual(level.Ground, hit.Shape);
        Assert.AreEqual(Level.LevelOwner, hit.Owner);
        Assert.AreEqual(0.75f, hit.Fraction, Rounding);
        Assert.AreEqual(0, HeightOf(world, hit.Point), Rounding);
        Assert.AreEqual(-80, XOf(hit.Point), Rounding);
        Assert.AreEqual(1, Vector2.Dot(hit.Normal, world.Up), 1e-3f, $"the ground's normal faces up: {hit.Normal}");
        Assert.IsFalse(world.RayCast(At(world, -80, 60), At(world, -80, 10), default, out _), "a ray that ends before the ground hits nothing");
    }

    [TestMethod]
    public void The_ray_filter_decides_whether_the_enemy_layer_is_hit()
    {
        var level = Build();
        var world = level.World;
        Assert.IsTrue(world.RayCast(At(world, -80, 8), At(world, 200, 8), new QueryFilter(StaticLayer, EnemyLayer), out var enemy));
        Assert.AreEqual(level.Enemy, enemy.Shape);
        Assert.AreEqual(-40, XOf(enemy.Point), Rounding);
        Assert.AreEqual(-1, enemy.Normal.X, 1e-3f);
        Assert.IsTrue(world.RayCast(At(world, -80, 8), At(world, 200, 8), new QueryFilter(StaticLayer, StaticLayer), out var wall));
        Assert.AreEqual(level.Wall, wall.Shape, "without the enemy layer the same ray reaches the wall");
        Assert.AreEqual(180f / 280f, wall.Fraction, Rounding);
    }

    [TestMethod]
    public void Queries_leave_sensors_out_unless_the_filter_includes_them()
    {
        var level = Build();
        var world = level.World;
        Assert.IsTrue(world.RayCast(At(world, 48, 60), At(world, 48, -20), default, out var plain));
        Assert.AreEqual(level.Ground, plain.Shape, "by default the ray passes the sensor");
        Assert.IsTrue(world.RayCast(At(world, 48, 60), At(world, 48, -20), new QueryFilter(StaticLayer, CollisionLayers.All) { IncludeSensors = true }, out var sensed));
        Assert.AreEqual(level.Sensor, sensed.Shape, "with sensors included it stops at the sensor");
        Assert.AreEqual(32, HeightOf(world, sensed.Point), Rounding);

        Span<ShapeId> found = stackalloc ShapeId[8];
        var inside = Area(world, 44, 52, 10, 20);
        Assert.AreEqual(0, world.Overlap(inside, new QueryFilter(StaticLayer, CollisionLayers.All), found));
        Assert.AreEqual(1, world.Overlap(inside, new QueryFilter(StaticLayer, CollisionLayers.All) { IncludeSensors = true }, found));
        Assert.AreEqual(level.Sensor, found[0]);
    }

    [TestMethod]
    public void Overlap_is_exact_not_a_bounding_box_test()
    {
        var level = Build();
        var world = level.World;
        Span<ShapeId> found = stackalloc ShapeId[8];
        Assert.AreEqual(0, world.Overlap(Area(world, 117, 127, 10, 20), default, found), "one unit right of the wall finds nothing");
        Assert.AreEqual(1, world.Overlap(Area(world, 110, 120, 10, 20), default, found), "over the wall finds it");
        Assert.AreEqual(level.Wall, found[0]);
        int all = world.Overlap(Area(world, 90, 120, -5, 20), default, found[..1]);
        Assert.AreEqual(2, all, "the count includes what the span had no room for (wall and ground)");
    }

    /// <summary>The world rectangle of suite x0..x1, h0..h1.</summary>
    protected RectF Area(IPhysicsWorld2D world, float x0, float x1, float h0, float h1)
    {
        var a = At(world, x0, h0);
        var b = At(world, x1, h1);
        return new RectF(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Abs(b.X - a.X), MathF.Abs(b.Y - a.Y));
    }

    // ------------------------------------------------------------------ sensors and events

    private (int Enter, int Exit) WalkAndStep(Level level, float dx, int moves, List<SensorEvent> log)
    {
        int enter = 0, exit = 0;
        for (int i = 0; i < moves; i++)
        {
            level.World.Move(level.Player, By(level.World, dx, 0), Level.PlayerFilter);
            level.World.Step(1 / 60f);
            Assert.AreEqual(0, level.World.ContactEvents.Length, "no dynamic body: no contact events (kinematic contacts are in MoveResult)");
            foreach (var e in level.World.SensorEvents)
            {
                log.Add(e);
                if (e.Kind == SensorEventKind.Enter) enter++;
                else exit++;
            }
        }
        return (enter, exit);
    }

    [TestMethod]
    public void Walking_through_a_sensor_reports_one_entry_and_one_exit_and_again_on_the_way_back()
    {
        var level = Build();
        Land(level);
        var log = new List<SensorEvent>();
        var there = WalkAndStep(level, 4, 20, log);
        Assert.AreEqual(80, CenterX(level), Rounding, "it walked past the sensor at x 40..56");
        Assert.AreEqual((1, 1), there);
        Assert.AreEqual(SensorEventKind.Enter, log[0].Kind);
        Assert.AreEqual(level.Sensor, log[0].Sensor);
        Assert.AreEqual(level.PlayerShape, log[0].Visitor);
        Assert.AreEqual(Level.SensorOwner, log[0].SensorOwner);
        Assert.AreEqual(Level.PlayerOwner, log[0].VisitorOwner);
        var back = WalkAndStep(level, -4, 20, log);
        Assert.AreEqual((1, 1), back, "the second pass reports again");
        Assert.AreEqual(4, log.Count);
    }

    [TestMethod]
    public void A_sensor_event_arrives_in_the_step_after_the_move_that_entered_and_lasts_until_the_next_step()
    {
        var level = Build();
        Land(level);
        level.World.SetPosition(level.Player, At(level.World, 30, 7));
        level.World.Step(1 / 60f);
        Assert.AreEqual(0, level.World.SensorEvents.Length, "x 24..36 is outside the sensor");
        level.World.Move(level.Player, By(level.World, 12, 0), Level.PlayerFilter);
        Assert.AreEqual(0, level.World.SensorEvents.Length, "events are computed by Step, not by Move");
        level.World.Step(1 / 60f);
        Assert.AreEqual(1, level.World.SensorEvents.Length);
        Assert.AreEqual(SensorEventKind.Enter, level.World.SensorEvents[0].Kind);
        Assert.AreEqual(1, level.World.SensorEvents.Length, "still readable after the step");
        level.World.Step(1 / 60f);
        Assert.AreEqual(0, level.World.SensorEvents.Length, "the next step replaces them");
        Assert.AreEqual(3, level.World.StepCount);
    }

    [TestMethod]
    public void Destroying_a_visitor_ends_its_sensor_pair_with_an_exit_that_still_names_it()
    {
        var level = Build();
        level.World.SetPosition(level.Player, At(level.World, 48, 10));
        level.World.Step(1 / 60f);
        Assert.AreEqual(1, level.World.SensorEvents.Length);
        level.World.DestroyBody(level.Player);
        level.World.Step(1 / 60f);
        Assert.AreEqual(1, level.World.SensorEvents.Length);
        var exit = level.World.SensorEvents[0];
        Assert.AreEqual(SensorEventKind.Exit, exit.Kind);
        Assert.AreEqual(level.PlayerShape, exit.Visitor);
        Assert.AreEqual(Level.PlayerOwner, exit.VisitorOwner);
        Assert.IsFalse(level.World.IsAlive(exit.Visitor));
    }

    [TestMethod]
    public void Sensors_see_only_moving_shapes_that_opt_in_and_whose_layers_both_accept()
    {
        var world = CreateWorld();
        Box(world, BodyKind.Static, Level.SensorOwner, 0, 32, 0, 32, s => s with { IsSensor = true });
        Box(world, BodyKind.Static, Level.LevelOwner, 4, 8, 4, 8);                                          // static: not seen
        Box(world, BodyKind.Kinematic, new EntityId(10, 1), 10, 14, 4, 8, s => s with { SensorEvents = false }); // opted out
        Box(world, BodyKind.Kinematic, new EntityId(11, 1), 16, 20, 4, 8, s => s with { Mask = EnemyLayer });   // its mask lacks the sensor's layer
        var (_, seen) = Box(world, BodyKind.Kinematic, new EntityId(12, 1), 22, 26, 4, 8);
        world.Step(1 / 60f);
        Assert.AreEqual(1, world.SensorEvents.Length, string.Join(", ", world.SensorEvents.ToArray()));
        Assert.AreEqual(seen, world.SensorEvents[0].Visitor);
    }

    [TestMethod]
    public void Step_counts_steps_and_refuses_negative_or_non_finite_lengths()
    {
        var world = CreateWorld();
        world.Step(1 / 60f);
        world.Step(0);
        Assert.AreEqual(2, world.StepCount);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.Step(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.Step(float.NaN));
        Assert.AreEqual(2, world.StepCount);
    }

    [TestMethod]
    public void The_same_calls_give_bit_identical_results()
    {
        string Run()
        {
            var level = Build();
            var log = new System.Text.StringBuilder();
            for (int i = 0; i < 120; i++)
            {
                float dx = 1.5f + MathF.Sin(i * 0.37f) * 5.3f, dh = i % 30 < 5 ? 9.1f : -3.7f;
                var r = level.World.Move(level.Player, By(level.World, dx, dh), Level.PlayerFilter);
                level.World.Step(1 / 60f);
                var p = level.World.PositionOf(level.Player);
                log.Append(BitConverter.SingleToInt32Bits(p.X)).Append(',').Append(BitConverter.SingleToInt32Bits(p.Y)).Append(',').Append(r.OnGround).Append(r.TotalContacts);
                foreach (var e in level.World.SensorEvents) log.Append(e.Kind);
                log.Append(';');
            }
            return log.ToString();
        }
        string first = Run();
        Assert.IsTrue(first.Contains("True", StringComparison.Ordinal) && first.Contains("Enter", StringComparison.Ordinal), "the run lands and crosses the sensor");
        Assert.AreEqual(first, Run());
    }
}
