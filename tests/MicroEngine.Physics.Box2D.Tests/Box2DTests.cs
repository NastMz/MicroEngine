using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Physics.Box2D;
using MicroEngine.Scenes;
using MicroEngine.Testing.Physics;

namespace MicroEngine.Physics.Box2D.Tests;

/// <summary>Creates Box2D worlds for the tests, or makes the test inconclusive (failed when required) without the native library.</summary>
internal static class Box2DTesting
{
    /// <summary>World units per metre of the pixel worlds the tests also run: a 12 × 14 player is 0.375 × 0.4375 m.</summary>
    public const float PixelsPerMeter = 32;

    /// <summary>The engine's pixel space: 32 pixels a metre, y down.</summary>
    public static readonly Box2DWorldOptions Pixels = new() { UnitsPerMeter = PixelsPerMeter, YDown = true };

    private static readonly List<Box2DPhysicsWorld2D> s_created = [];

    /// <summary>A new world, disposed by <see cref="Free"/> after the test.</summary>
    public static Box2DPhysicsWorld2D Create(Box2DWorldOptions? options = null)
    {
        RequireNatives();
        var world = new Box2DPhysicsWorld2D(options);
        s_created.Add(world);
        return world;
    }

    /// <summary>Disposes the worlds the test created (Box2D holds at most 128 at a time).</summary>
    public static void Free()
    {
        foreach (var world in s_created) world.Dispose();
        s_created.Clear();
    }

    public static void RequireNatives()
    {
        if (Box2DPhysicsWorld2D.IsSupported) return;
        const string message = "The me_box2d native library of this runtime is missing: run src/MicroEngine.Physics.Box2D/native/build.sh <rid>.";
        if (Environment.GetEnvironmentVariable("MICROENGINE_BOX2D_REQUIRED") == "1") Assert.Fail(message);
        Assert.Inconclusive(message);
    }
}

// ------------------------------------------------------------------ the common suite, in metres and in pixels

/// <summary>The common suite in Box2D's own units: suite units become metres at 32 a metre, y up.</summary>
[TestClass]
public sealed class Box2DPhysicsConformance : PhysicsConformance
{
    [TestCleanup] public void Free() => Box2DTesting.Free();
    protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create();
    protected override float Scale => 1 / Box2DTesting.PixelsPerMeter;
}

/// <summary>The common suite in the engine's pixel space, the conversion done by the world (<see cref="Box2DWorldOptions"/>).</summary>
[TestClass]
public sealed class Box2DPixelConformance : PhysicsConformance
{
    [TestCleanup] public void Free() => Box2DTesting.Free();
    protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(Box2DTesting.Pixels);
}

// ------------------------------------------------------------------ the capabilities it declares

[TestClass] public sealed class Box2DOneWayPlatformConformance : OneWayPlatformConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(Box2DTesting.Pixels); }
[TestClass] public sealed class Box2DDynamicBodyConformance : DynamicBodyConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(); protected override float Scale => 1 / Box2DTesting.PixelsPerMeter; }
[TestClass] public sealed class Box2DPixelDynamicBodyConformance : DynamicBodyConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(Box2DTesting.Pixels); }
[TestClass] public sealed class Box2DRotationConformance : RotationConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(); protected override float Scale => 1 / Box2DTesting.PixelsPerMeter; }
[TestClass] public sealed class Box2DPixelRotationConformance : RotationConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(Box2DTesting.Pixels); }
[TestClass] public sealed class Box2DCircleConformance : CircleConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(); protected override float Scale => 1 / Box2DTesting.PixelsPerMeter; }
[TestClass] public sealed class Box2DCapsuleConformance : CapsuleConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(); protected override float Scale => 1 / Box2DTesting.PixelsPerMeter; }
[TestClass] public sealed class Box2DPolygonConformance : PolygonConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(); protected override float Scale => 1 / Box2DTesting.PixelsPerMeter; }
[TestClass] public sealed class Box2DPixelPolygonConformance : PolygonConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(Box2DTesting.Pixels); }
[TestClass] public sealed class Box2DContinuousCollisionConformance : ContinuousCollisionConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(); protected override float Scale => 1 / Box2DTesting.PixelsPerMeter; }
[TestClass] public sealed class Box2DFrictionRestitutionConformance : FrictionRestitutionConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(); protected override float Scale => 1 / Box2DTesting.PixelsPerMeter; }
[TestClass] public sealed class Box2DMassPropertiesConformance : MassPropertiesConformance { [TestCleanup] public void Free() => Box2DTesting.Free(); protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(); protected override float Scale => 1 / Box2DTesting.PixelsPerMeter; }

// ------------------------------------------------------------------ what this backend promises beyond the suites

/// <summary>The Box2D backend's own rules: what it declares, its units, its limits, its native library.</summary>
[TestClass]
public sealed class Box2DPhysicsWorldTests
{
    [TestInitialize]
    public void Natives() => Box2DTesting.RequireNatives();

    [TestMethod]
    public void The_assembly_references_only_the_physics_contract_the_scene_world_and_the_kernel()
    {
        var engine = typeof(Box2DPhysicsWorld2D).Assembly.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("MicroEngine", StringComparison.Ordinal)).Order().ToArray();
        CollectionAssert.AreEqual(new[] { "MicroEngine.Kernel", "MicroEngine.Physics", "MicroEngine.Scene" }, engine, string.Join(", ", engine));
    }

    [TestMethod]
    public void It_declares_the_solver_capabilities_a_skin_of_three_slops_and_y_up_by_default()
    {
        using var world = new Box2DPhysicsWorld2D();
        Assert.AreEqual("box2d", world.Backend);
        Assert.AreEqual(PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.Rotation | PhysicsCapabilities.Circles | PhysicsCapabilities.Capsules
            | PhysicsCapabilities.Polygons | PhysicsCapabilities.ContinuousCollision | PhysicsCapabilities.FrictionRestitution
            | PhysicsCapabilities.MassProperties | PhysicsCapabilities.OneWayPlatforms, world.Capabilities);
        Assert.AreEqual(0, (int)(world.Capabilities & (PhysicsCapabilities.TileGeometry | PhysicsCapabilities.MovingSolids | PhysicsCapabilities.Joints)));
        Assert.AreEqual(0.015f, world.Skin, 1e-6f);
        Assert.AreEqual(new Vector2(0, 1), world.Up);
        Assert.AreEqual("3.2.0", Box2DPhysicsWorld2D.Box2DVersion);
    }

    [TestMethod]
    public void A_pixel_world_takes_and_returns_pixels_with_y_down()
    {
        using var world = new Box2DPhysicsWorld2D(Box2DTesting.Pixels);
        Assert.AreEqual(new Vector2(0, -1), world.Up);
        Assert.AreEqual(0.015f * 32, world.Skin, 1e-5f);
        var body = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(160, 48)) with { Rotation = 0.25f });
        world.AddShape(body, ShapeDescription.Box(new Vector2(16, 16)));
        Assert.AreEqual(new Vector2(160, 48), world.PositionOf(body));
        Assert.AreEqual(0.25f, world.RotationOf(body), 0.005f, "Box2D's angle round trip (its own sine, cosine and arctangent)");
        world.Step(0.5f);
        Assert.IsTrue(world.PositionOf(body).Y > 48 + 30, $"gravity pulls toward +y: {world.PositionOf(body)}");
        Assert.AreEqual(9.81f * 32 * 0.5f, world.VelocityOf(body).Y, 1, "9.81 m/s² is 313.9 px/s²");
    }

    [TestMethod]
    public void Options_out_of_range_are_refused()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = 0 }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = float.NaN }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Box2DPhysicsWorld2D(new Box2DWorldOptions { SubSteps = 0 }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Box2DPhysicsWorld2D(new Box2DWorldOptions { Gravity = new Vector2(float.PositiveInfinity, 0) }));
    }

    [TestMethod]
    public void Custom_gravity_is_in_world_units()
    {
        using var world = new Box2DPhysicsWorld2D(Box2DTesting.Pixels with { Gravity = new Vector2(64, 0) });
        var body = world.CreateBody(default, BodyDescription.Dynamic(Vector2.Zero));
        world.AddShape(body, ShapeDescription.Box(new Vector2(8, 8)));
        world.Step(1);
        Assert.AreEqual(64, world.VelocityOf(body).X, 0.5f);
        Assert.AreEqual(0, world.VelocityOf(body).Y, 1e-4f);
    }

    [TestMethod]
    public void A_polygon_of_more_than_eight_vertices_is_refused_before_anything_changes()
    {
        using var world = new Box2DPhysicsWorld2D();
        var body = world.CreateBody(default, BodyDescription.Static());
        var nine = Enumerable.Range(0, 9).Select(i => new Vector2(MathF.Cos(i * 0.7f), MathF.Sin(i * 0.7f))).ToArray();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.AddShape(body, ShapeDescription.Polygon(nine)));
        Span<ShapeId> found = stackalloc ShapeId[1];
        Assert.AreEqual(0, world.Overlap(new RectF(-2, -2, 4, 4), default, found));
    }

    [TestMethod]
    public void Only_dynamic_bodies_take_a_velocity_and_mass_comes_from_density()
    {
        using var world = new Box2DPhysicsWorld2D();
        var kinematic = world.CreateBody(default, BodyDescription.Kinematic(Vector2.Zero));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.SetVelocity(kinematic, Vector2.One));
        Assert.AreEqual(Vector2.Zero, world.VelocityOf(kinematic));
        var crate = world.CreateBody(default, BodyDescription.Dynamic(Vector2.Zero));
        world.AddShape(crate, ShapeDescription.Box(new Vector2(0.5f, 2)) with { Density = 3 });
        Assert.AreEqual(3f, world.MassOf(crate), 1e-5f, "1 m² at 3 kg/m²");
        Assert.AreEqual(0, world.MassOf(kinematic));
    }

    [TestMethod]
    public void A_dynamic_box_thrown_up_through_a_one_way_platform_lands_on_it()
    {
        using var world = new Box2DPhysicsWorld2D();
        var ground = world.CreateBody(default, BodyDescription.Static(new Vector2(0, -0.5f)));
        world.AddShape(ground, ShapeDescription.Box(new Vector2(20, 1)));
        var platform = world.CreateBody(default, BodyDescription.Static(new Vector2(0, 1.05f)));
        world.AddShape(platform, ShapeDescription.Box(new Vector2(3, 0.1f)) with { OneWay = true });
        var box = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(0, 0.15f)));
        world.AddShape(box, ShapeDescription.Box(new Vector2(0.3f, 0.3f)));
        world.SetVelocity(box, new Vector2(0, 7));
        for (int i = 0; i < 180; i++) world.Step(1 / 60f);
        Assert.AreEqual(1.25f, world.PositionOf(box).Y, 0.02f, "it passed up through the platform and rests on its top");
    }

    [TestMethod]
    public void A_disposed_world_refuses_every_call_and_disposing_twice_is_harmless()
    {
        var world = new Box2DPhysicsWorld2D();
        var body = world.CreateBody(default, BodyDescription.Static());
        world.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => world.Step(1 / 60f));
        Assert.ThrowsExactly<ObjectDisposedException>(() => world.PositionOf(body));
        Assert.ThrowsExactly<ObjectDisposedException>(() => world.CreateBody(default, BodyDescription.Static()));
        world.Dispose();
    }

    [TestMethod]
    public void Bounds_of_a_shape_follow_its_body()
    {
        using var world = new Box2DPhysicsWorld2D(Box2DTesting.Pixels);
        var body = world.CreateBody(default, BodyDescription.Kinematic(new Vector2(100, 50)));
        var shape = world.AddShape(body, ShapeDescription.Box(new Vector2(12, 14), new Vector2(0, -7)));
        var bounds = world.BoundsOf(shape);
        Assert.AreEqual(94, bounds.X, 1e-3f);
        Assert.AreEqual(36, bounds.Y, 1e-3f);
        Assert.AreEqual(12, bounds.Width, 1e-3f);
        Assert.AreEqual(14, bounds.Height, 1e-3f);
    }

    /// <summary>
    /// P6's benchmark scene, built through the contract: the same native build gives these bits on every runtime it
    /// ships for (the native self-test prints the same hash, and CI compares it across runtimes). A new Box2D release or
    /// a change of the layer's solver settings changes it: update the constant together with box2d.env.
    /// </summary>
    [TestMethod]
    public void The_benchmark_scene_hashes_to_the_bits_every_runtime_computes()
    {
        using var world = new Box2DPhysicsWorld2D(new Box2DWorldOptions { Gravity = new Vector2(0, -10), EnableSleep = false });
        BodyId Box(BodyKind kind, float x0, float x1, float y0, float y1)
        {
            var id = world.CreateBody(default, new BodyDescription(kind, new Vector2((x0 + x1) / 2, (y0 + y1) / 2)));
            world.AddShape(id, ShapeDescription.Box(new Vector2(x1 - x0, y1 - y0)));
            return id;
        }
        Box(BodyKind.Static, -6.5f, 6.5f, -1, 0);
        Box(BodyKind.Static, -6.5f, -6, 0, 20);
        Box(BodyKind.Static, 6, 6.5f, 0, 20);
        var boxes = new BodyId[500];
        for (int i = 0; i < boxes.Length; i++)
        {
            int column = i % 20, row = i / 20;
            boxes[i] = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(-5 + column * 0.55f, 1 + row * 0.6f)) with { Rotation = 0.05f * (i % 7 - 3) });
            world.AddShape(boxes[i], ShapeDescription.Box(new Vector2(0.5f, 0.5f)));
        }
        ulong h = 14695981039346656037UL;
        void Bits(float f)
        {
            uint u = BitConverter.SingleToUInt32Bits(f);
            for (int i = 0; i < 4; i++) { h ^= (u >> (8 * i)) & 0xff; h *= 1099511628211UL; }
        }
        for (int step = 1; step <= 600; step++)
        {
            world.Step(1 / 60f);
            if (step is 1 or 10 or 60 or 120 or 300 or 600)
                foreach (var b in boxes)
                {
                    var p = world.PositionOf(b);
                    Bits(p.X); Bits(p.Y); Bits(world.RotationOf(b));
                }
        }
        Assert.AreEqual("93f3a79ebf294b23", h.ToString("x16"));
    }

    /// <summary>Every function of the native layer, called once from managed code (the browser runs this on the wasm interpreter).</summary>
    [TestMethod]
    public unsafe void Every_function_of_the_native_layer_answers()
    {
        Assert.AreEqual(Native.ApiVersion, Native.me_api_version());
        Assert.AreEqual(30200, Native.me_box2d_version());
        Assert.AreEqual(0.015f, Native.me_move_skin(), 1e-6f);
        nint w = Native.me_world_create(0, -10, 4, 1);
        Assert.AreNotEqual(0, w);
        try
        {
            float* g = stackalloc float[8];
            g[0] = 10; g[1] = 0.5f; g[2] = 0; g[3] = 0;
            int ground = Native.me_body_create(w, Native.Static, 0, -0.5f, 0, 0);
            int groundShape = Native.me_shape_create(w, ground, Native.Box, g, 0, 0, 1, ulong.MaxValue, 0, -1, -1, -1);
            int body = Native.me_body_create(w, Native.Dynamic, 0, 2, 0, 1);
            g[0] = 0; g[1] = 0;
            int ball = Native.me_shape_create(w, body, Native.Circle, g, 0, 0.25f, 1, ulong.MaxValue, Native.SensorEvents, 1, 0.5f, 0);
            Native.me_body_set_velocity(w, body, 1, 0);
            Native.me_body_set_position(w, body, 0, 1);
            Assert.IsTrue(Native.me_body_mass(w, body) > 0);
            Native.me_world_step(w, 1 / 60f);
            float* s = stackalloc float[6];
            Native.me_body_state(w, body, s);
            Assert.IsTrue(s[0] > 0 && s[1] < 1, $"({s[0]}, {s[1]})");
            float* b = stackalloc float[4];
            Native.me_shape_bounds(w, ball, b);
            Assert.AreEqual(0.5f, b[2] - b[0], 1e-4f);
            float* r = stackalloc float[5];
            Assert.AreEqual(groundShape, Native.me_ray_cast(w, 3, 2, 0, -3, 1, ulong.MaxValue, 0, r));
            int* found = stackalloc int[4];
            Assert.AreEqual(1, Native.me_overlap_box(w, 2, -0.2f, 3, -0.1f, 1, ulong.MaxValue, 0, found, 4));
            int mover = Native.me_body_create(w, Native.Kinematic, -3, 2, 0, 0);
            g[0] = 0; g[1] = -0.2f; g[2] = 0; g[3] = 0.2f;
            int capsule = Native.me_shape_create(w, mover, Native.Capsule, g, 0, 0.2f, 1, ulong.MaxValue, Native.SensorEvents, -1, -1, -1);
            float* xy = stackalloc float[2];
            int* shapes = stackalloc int[8];
            float* normals = stackalloc float[16];
            for (int i = 0; i < 30; i++) Native.me_move(w, mover, capsule, 0, -0.1f, 1, ulong.MaxValue, 0, xy, shapes, normals, 8);
            Assert.AreEqual(0.4f, xy[1], 0.016f);
            int* begin = stackalloc int[8];
            int* end = stackalloc int[8];
            int ends;
            Native.me_sensor_events(w, begin, 4, end, 4, &ends);
            Native.me_contact_events(w, begin, normals, 4, end, 4, &ends);
            int* counters = stackalloc int[4];
            Native.me_world_counters(w, counters);
            Assert.AreEqual(3, counters[0]);
            Native.me_shape_destroy(w, ball);
            Native.me_body_destroy(w, body);
            Native.me_world_counters(w, counters);
            Assert.AreEqual(2, counters[0]);
        }
        finally
        {
            Native.me_world_destroy(w);
        }
    }
}
