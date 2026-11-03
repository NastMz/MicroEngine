using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.Testing.Physics;
using MicroEngine.World;

namespace MicroEngine.Tests.Physics;

/// <summary>The tile backend runs the common physics suite.</summary>
[TestClass]
public sealed class TilePhysicsConformance : PhysicsConformance
{
    protected override IPhysicsWorld2D CreateWorld() => new TilePhysicsWorld2D();
}

/// <summary>The tile backend declares one-way platforms.</summary>
[TestClass]
public sealed class TileOneWayPlatformConformance : OneWayPlatformConformance
{
    protected override IPhysicsWorld2D CreateWorld() => new TilePhysicsWorld2D();
}

/// <summary>The tile backend declares moving solids.</summary>
[TestClass]
public sealed class TileMovingSolidConformance : MovingSolidConformance
{
    protected override IPhysicsWorld2D CreateWorld() => new TilePhysicsWorld2D();
}

/// <summary>The tile backend declares tile geometry.</summary>
[TestClass]
public sealed class TileGeometryBackendConformance : TileGeometryConformance
{
    protected override IPhysicsWorld2D CreateWorld() => new TilePhysicsWorld2D();
    protected override ShapeId AddTiles(IPhysicsWorld2D world, BodyId body, ICollisionGrid grid) => ((TilePhysicsWorld2D)world).AddTiles(body, grid);
}

/// <summary>What the tile backend promises beyond the common suite: exact resting, its declared capabilities, the fixed mover (D6), state hashing.</summary>
[TestClass]
public sealed class TilePhysicsWorldTests
{
    private static BodyId Box(TilePhysicsWorld2D world, BodyDescription body, RectF rect)
    {
        var id = world.CreateBody(default, body with { Position = rect.Center });
        world.AddShape(id, ShapeDescription.Box(rect.Size));
        return id;
    }

    [TestMethod]
    public void It_declares_tile_geometry_one_way_platforms_and_moving_solids_a_zero_skin_and_y_down()
    {
        var world = new TilePhysicsWorld2D();
        Assert.AreEqual(PhysicsCapabilities.TileGeometry | PhysicsCapabilities.OneWayPlatforms | PhysicsCapabilities.MovingSolids, world.Capabilities);
        Assert.AreEqual(0, world.Skin);
        Assert.AreEqual(new Vector2(0, -1), world.Up);
        Assert.AreEqual("tiles", world.Backend);
        Assert.AreEqual(0, world.ContactEvents.Length);
    }

    [TestMethod]
    public void A_landing_box_rests_exactly_on_the_surface()
    {
        var world = new TilePhysicsWorld2D();
        Box(world, BodyDescription.Static(), new RectF(0, 100, 200, 16));
        var player = Box(world, BodyDescription.Kinematic(default), new RectF(10, 37.3f, 12, 14));
        var start = world.PositionOf(player);
        var result = world.Move(player, new Vector2(0, 91.7f));
        Assert.AreEqual(100 - 7, world.PositionOf(player).Y, "exact: the skin is zero");
        Assert.AreEqual(new Vector2(0, 100 - 7 - start.Y), result.Delta);
    }

    [TestMethod]
    public void D6_a_thin_solid_stops_a_fast_small_body_E1()
    {
        // A3 WO-2 E1: a 2×2 body moving 15.9 px down through a 4 px solid at y 100..104 used to end at y 105.9.
        var world = new TilePhysicsWorld2D();
        Box(world, BodyDescription.Static(), new RectF(0, 100, 40, 4));
        var bullet = Box(world, BodyDescription.Kinematic(default), new RectF(20, 90, 2, 2));
        var result = world.Move(bullet, new Vector2(0, 15.9f));
        Assert.AreEqual(99, world.PositionOf(bullet).Y);
        Assert.IsTrue(result.OnGround, result.ToString());
    }

    [TestMethod]
    public void D6_a_rising_platform_lifts_a_resting_body_and_it_stays_on_ground_E2()
    {
        // A3 WO-2 E2: a platform raised into a resting 10×10 body used to leave it embedded 4 px with IsOnGround false.
        var world = new TilePhysicsWorld2D();
        var platform = Box(world, BodyDescription.Kinematic(default) with { PushesBodies = true }, new RectF(0, 90, 40, 8));
        var body = Box(world, BodyDescription.Kinematic(default), new RectF(10, 80, 10, 10));
        world.Move(platform, new Vector2(0, -2));
        Assert.AreEqual(88 - 5, world.PositionOf(body).Y, "lifted flush onto the platform's top at 88");
        var fall = world.Move(body, new Vector2(0, 2));
        Assert.AreEqual(83, world.PositionOf(body).Y, "not embedded after its own fall");
        Assert.IsTrue(fall.OnGround, fall.ToString());
    }

    [TestMethod]
    public void Moving_into_a_corner_reports_the_floor_and_the_wall_and_a_ceiling_from_below()
    {
        var world = new TilePhysicsWorld2D();
        var floor = world.AddShape(world.CreateBody(default, BodyDescription.Static(new Vector2(100, 108))), ShapeDescription.Box(new Vector2(200, 16)));
        var wall = world.AddShape(world.CreateBody(default, BodyDescription.Static(new Vector2(108, 50))), ShapeDescription.Box(new Vector2(16, 100)));
        var player = Box(world, BodyDescription.Kinematic(default), new RectF(80, 80, 12, 14));
        var corner = world.Move(player, new Vector2(30, 30));
        Assert.IsTrue(corner.OnGround && corner.HitWall && !corner.HitCeiling, corner.ToString());
        var shapes = corner.Contacts.ToArray().Select(c => (c.Shape, c.Normal)).ToList();
        CollectionAssert.Contains(shapes, (floor, new Vector2(0, -1)));
        CollectionAssert.Contains(shapes, (wall, new Vector2(-1, 0)));
        var ceiling = Box(world, BodyDescription.Static(), new RectF(0, 40, 50, 8));
        var head = Box(world, BodyDescription.Kinematic(default), new RectF(10, 60, 12, 14));
        var bump = world.Move(head, new Vector2(0, -30));
        Assert.IsTrue(bump.HitCeiling, bump.ToString());
        Assert.AreEqual(48 + 7, world.PositionOf(head).Y);
        Assert.IsTrue(world.IsAlive(ceiling));
    }

    [TestMethod]
    public void The_world_takes_part_in_the_state_hash()
    {
        static ulong Hash(TilePhysicsWorld2D w)
        {
            var h = new StateHasher();
            w.HashState(h);
            return h.Value;
        }
        var world = new TilePhysicsWorld2D();
        var player = Box(world, BodyDescription.Kinematic(default), new RectF(0, 0, 10, 10));
        ulong before = Hash(world);
        world.Move(player, new Vector2(1, 0));
        Assert.AreNotEqual(before, Hash(world), "a moved body changes the hash");
        var copy = new TilePhysicsWorld2D();
        var other = Box(copy, BodyDescription.Kinematic(default), new RectF(0, 0, 10, 10));
        copy.Move(other, new Vector2(1, 0));
        Assert.AreEqual(Hash(world), Hash(copy), "the same history gives the same hash");
    }
}

/// <summary>The physics module: stepping in the Physics phase, binding to a scene world, writing transforms.</summary>
[TestClass]
public sealed class PhysicsModuleTests
{
    private static FrameContext Step(long n) => new(1 / 60.0, 1 / 60.0, 1, n, n);

    [TestMethod]
    public void The_module_steps_registered_worlds_in_the_physics_phase_by_the_fixed_step()
    {
        var builder = new EngineBuilder().Add(new PhysicsModule());
        Assert.AreEqual(FramePhase.Physics, builder.Schedule.PhaseOf(PhysicsModule.SystemName));
        var engine = builder.Build();
        var system = engine.Services.Get<PhysicsSystem>();
        var physics = new TilePhysicsWorld2D();
        system.Add(physics);
        Assert.ThrowsExactly<InvalidOperationException>(() => system.Add(physics));
        engine.RunFixedStep(Step(0));
        engine.RunFixedStep(Step(1));
        Assert.AreEqual(2, physics.StepCount);
        Assert.IsTrue(system.Remove(physics));
        engine.RunFixedStep(Step(2));
        Assert.AreEqual(2, physics.StepCount, "removed worlds no longer step");
    }

    [TestMethod]
    public void A_bound_world_steps_only_when_its_scene_world_stepped_and_writes_body_poses_to_transforms()
    {
        var system = new PhysicsSystem();
        var world = new SceneWorld();
        var physics = new TilePhysicsWorld2D();
        system.Add(physics, world);
        var owner = world.Create();
        var player = physics.CreateBody(owner, BodyDescription.Kinematic(new Vector2(5, 6)));
        physics.AddShape(player, ShapeDescription.Box(new Vector2(4, 4)));
        var wallOwner = world.Create();
        world.Local(wallOwner).Position2D = new Vector2(-1, -1);
        physics.CreateBody(wallOwner, BodyDescription.Static(new Vector2(50, 50)));

        system.Run(Step(0));
        Assert.AreEqual(0, physics.StepCount, "the scene world has not stepped");
        Assert.AreEqual(Vector2.Zero, world.Local(owner).Position2D);

        world.BeginStep(0, 1 / 60f);
        physics.Move(player, new Vector2(3, 0));
        world.EndStep();
        system.Run(Step(0));
        Assert.AreEqual(1, physics.StepCount);
        Assert.AreEqual(new Vector2(8, 6), world.Local(owner).Position2D, "the kinematic body's pose reached its owner");
        Assert.AreEqual(new Vector2(-1, -1), world.Local(wallOwner).Position2D, "static bodies are never written");
        system.Run(Step(1));
        Assert.AreEqual(1, physics.StepCount, "no second step without a world step");

        world.Clock.Freeze(1);
        world.BeginStep(1, 1 / 60f);
        world.EndStep();
        system.Run(Step(1));
        Assert.AreEqual(1, physics.StepCount, "a frozen world's physics stands still");
    }

    [TestMethod]
    public void Owners_of_moving_bodies_must_be_root_entities_and_teleport_moves_both()
    {
        var world = new SceneWorld();
        var physics = new TilePhysicsWorld2D();
        var parent = world.Create();
        var child = world.Create(parent);
        physics.CreateBody(child, BodyDescription.Kinematic(Vector2.One));
        Assert.ThrowsExactly<InvalidOperationException>(() => PhysicsSync.WriteTransforms(physics, world));

        var world2 = new SceneWorld();
        var physics2 = new TilePhysicsWorld2D();
        var owner = world2.Create();
        var body = physics2.CreateBody(owner, BodyDescription.Kinematic(Vector2.One));
        PhysicsSync.Teleport(physics2, world2, body, new Vector2(40, 2));
        Assert.AreEqual(new Vector2(40, 2), physics2.PositionOf(body));
        Assert.AreEqual(new Vector2(40, 2), world2.Local(owner).Position2D);
        Assert.AreEqual(new Vector2(40, 2), world2.DrawPosition2D(owner), "drawn there at once");
    }
}
