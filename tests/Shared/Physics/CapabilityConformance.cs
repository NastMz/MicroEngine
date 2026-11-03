using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// The base of a capability-specific suite: it runs only for backends that declare <see cref="Capability"/>, and a
/// subclass for a backend that does not declare it fails instead of passing vacuously.
/// </summary>
public abstract class CapabilityConformance : PhysicsConformanceHelpers
{
    /// <summary>The capability the suite checks.</summary>
    protected abstract PhysicsCapabilities Capability { get; }

    [TestInitialize]
    public void TheBackendDeclaresTheCapability()
    {
        var world = CreateWorld();
        Assert.AreEqual(Capability, world.Capabilities & Capability, $"{world.Backend} runs the {Capability} suite without declaring it");
    }
}

/// <summary>The geometry helpers of the physics suites (suite units: x and height along <see cref="IPhysicsWorld2D.Up"/>).</summary>
public abstract class PhysicsConformanceHelpers
{
    /// <summary>The rounding allowance added to the declared skin, in suite units.</summary>
    protected const float Rounding = 1e-3f;

    /// <summary>Creates an empty world of the backend under test.</summary>
    protected abstract IPhysicsWorld2D CreateWorld();
    /// <summary>World units per suite unit.</summary>
    protected virtual float Scale => 1;

    protected Vector2 At(IPhysicsWorld2D world, float x, float height) => new Vector2(x * Scale, 0) + world.Up * (height * Scale);
    protected Vector2 By(IPhysicsWorld2D world, float dx, float dh) => new Vector2(dx * Scale, 0) + world.Up * (dh * Scale);
    protected float XOf(Vector2 p) => p.X / Scale;
    protected float HeightOf(IPhysicsWorld2D world, Vector2 p) => Vector2.Dot(p, world.Up) / Scale;
    protected float Tolerance(IPhysicsWorld2D world) => world.Skin / Scale + Rounding;

    protected (BodyId Body, ShapeId Shape) Box(IPhysicsWorld2D world, BodyDescription body, float x0, float x1, float h0, float h1, Func<ShapeDescription, ShapeDescription>? edit = null, EntityId owner = default)
    {
        var id = world.CreateBody(owner, body with { Position = At(world, (x0 + x1) / 2, (h0 + h1) / 2) });
        var shape = ShapeDescription.Box(new Vector2((x1 - x0) * Scale, (h1 - h0) * Scale));
        return (id, world.AddShape(id, edit is null ? shape : edit(shape)));
    }

    /// <summary>The bottom height of a body whose one box is <paramref name="height"/> tall and centred on its origin.</summary>
    protected float Bottom(IPhysicsWorld2D world, BodyId body, float height) => HeightOf(world, world.PositionOf(body)) - height / 2;

    protected void AssertResting(IPhysicsWorld2D world, float gap, string what) =>
        Assert.IsTrue(gap >= -Rounding && gap <= Tolerance(world), $"{what}: gap {gap}, skin {world.Skin}");
}

/// <summary>One-way platforms: stop bodies coming down onto their top, let them through from below and when the filter passes them.</summary>
public abstract class OneWayPlatformConformance : CapabilityConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.OneWayPlatforms;

    private (IPhysicsWorld2D World, ShapeId Platform, BodyId Player) Scene()
    {
        var world = CreateWorld();
        var (_, platform) = Box(world, BodyDescription.Static(), -50, 50, 30, 34, s => s with { OneWay = true });
        var (player, _) = Box(world, BodyDescription.Kinematic(default), -6, 6, 10, 24);
        return (world, platform, player);
    }

    [TestMethod]
    public void A_box_rises_through_a_one_way_platform_and_lands_on_it()
    {
        var (world, platform, player) = Scene();
        var up = world.Move(player, By(world, 0, 40));
        Assert.AreEqual(50, Bottom(world, player, 14), Rounding, $"it rose through: {up}");
        Assert.IsFalse(up.HitCeiling, up.ToString());
        MoveResult down = default;
        for (int i = 0; i < 10; i++) down = world.Move(player, By(world, 0, -4));
        AssertResting(world, Bottom(world, player, 14) - 34, "landing on the platform");
        Assert.IsTrue(down.OnGround && down.Ground == platform, down.ToString());
    }

    [TestMethod]
    public void A_filter_that_passes_one_way_platforms_drops_through()
    {
        var (world, _, player) = Scene();
        world.SetPosition(player, At(world, 0, 41));
        var stand = world.Move(player, By(world, 0, -2));
        Assert.IsTrue(stand.OnGround, stand.ToString());
        var drop = world.Move(player, By(world, 0, -10), new QueryFilter { PassOneWay = true });
        Assert.AreEqual(24, Bottom(world, player, 14), Rounding, $"dropped through: {drop}");
        Assert.IsFalse(drop.OnGround, drop.ToString());
    }

    [TestMethod]
    public void A_one_way_platform_never_blocks_sideways()
    {
        var (world, platform, player) = Scene();
        world.SetPosition(player, At(world, -60, 32));
        var walk = world.Move(player, By(world, 30, 0));
        Assert.AreEqual(-30, XOf(world.PositionOf(player)), Rounding, walk.ToString());
        foreach (var c in walk.Contacts) Assert.AreNotEqual(platform, c.Shape);
    }

    [TestMethod]
    public void Rays_hit_a_one_way_platform_from_above_only()
    {
        var (world, platform, _) = Scene();
        Assert.IsTrue(world.RayCast(At(world, 20, 60), At(world, 20, 0), default, out var hit));
        Assert.AreEqual(platform, hit.Shape);
        Assert.AreEqual(34, HeightOf(world, hit.Point), Rounding);
        Assert.IsFalse(world.RayCast(At(world, 20, 0), At(world, 20, 60), default, out _), "from below it passes");
    }
}

/// <summary>Moving solids: a body that pushes moves the whole way, pushes kinematic bodies out of its way and carries those resting on it.</summary>
public abstract class MovingSolidConformance : CapabilityConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.MovingSolids;

    private (IPhysicsWorld2D World, BodyId Lift, ShapeId LiftShape, BodyId Rider) Scene()
    {
        var world = CreateWorld();
        Box(world, BodyDescription.Static(), -300, 300, -16, 0);
        var (lift, liftShape) = Box(world, BodyDescription.Kinematic(default) with { PushesBodies = true }, -30, 30, 0, 8);
        var (rider, _) = Box(world, BodyDescription.Kinematic(default), -6, 6, 8, 22);
        return (world, lift, liftShape, rider);
    }

    [TestMethod]
    public void A_rising_solid_lifts_the_body_resting_on_it_without_embedding_it()
    {
        var (world, lift, liftShape, rider) = Scene();
        var r = world.Move(lift, By(world, 0, 4));
        Assert.AreEqual(4, HeightOf(world, world.PositionOf(lift)) - 4, Rounding, $"the solid is not blocked: {r}");
        AssertResting(world, Bottom(world, rider, 14) - 12, "the rider sits on the lifted solid");
        var fall = world.Move(rider, By(world, 0, -2));
        Assert.IsTrue(fall.OnGround && fall.Ground == liftShape, $"and stands on it: {fall}");
        AssertResting(world, Bottom(world, rider, 14) - 12, "after its own fall");
    }

    [TestMethod]
    public void A_moving_solid_carries_its_rider_sideways_and_down()
    {
        var (world, lift, _, rider) = Scene();
        world.Move(lift, By(world, 0, 20));
        world.Move(lift, By(world, 25, 0));
        Assert.AreEqual(25, XOf(world.PositionOf(rider)), Rounding, "carried sideways");
        world.Move(lift, By(world, 0, -10));
        AssertResting(world, Bottom(world, rider, 14) - 18, "carried down with it");
    }

    [TestMethod]
    public void A_moving_solid_pushes_a_body_in_its_way_until_the_body_meets_a_wall()
    {
        var world = CreateWorld();
        Box(world, BodyDescription.Static(), 100, 116, 0, 64);
        var (door, _) = Box(world, BodyDescription.Kinematic(default) with { PushesBodies = true }, -20, -10, 0, 40);
        var (body, _) = Box(world, BodyDescription.Kinematic(default), 0, 12, 0, 14);
        world.Move(door, By(world, 30, 0));
        Assert.AreEqual(20, XOf(world.PositionOf(body)) - 6, Rounding, "pushed flush with the door's far side");
        world.Move(door, By(world, 200, 0));
        float gap = 100 - (XOf(world.PositionOf(body)) + 6);
        AssertResting(world, gap, "the pushed body stops at the wall");
    }

    [TestMethod]
    public void A_moving_solid_leaves_bodies_outside_its_filter_alone()
    {
        var world = CreateWorld();
        var (door, _) = Box(world, BodyDescription.Kinematic(default) with { PushesBodies = true }, -20, -10, 0, 40);
        var (ghost, _) = Box(world, BodyDescription.Kinematic(default), 0, 12, 0, 14, s => s.InLayers(2, CollisionLayers.All));
        world.Move(door, By(world, 30, 0), new QueryFilter(CollisionLayers.Default, CollisionLayers.Default));
        Assert.AreEqual(6, XOf(world.PositionOf(ghost)), Rounding);
    }
}
