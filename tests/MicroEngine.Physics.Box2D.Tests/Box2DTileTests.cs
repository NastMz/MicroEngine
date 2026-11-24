using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine.Physics;
using MicroEngine.Physics.Box2D;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.Testing.Physics;
using MicroEngine.World;

namespace MicroEngine.Physics.Box2D.Tests;

/// <summary>The tile geometry suite on Box2D, in the engine's pixel space (the suite's grids are 16-pixel tiles, y down).</summary>
[TestClass]
public sealed class Box2DTileGeometryConformance : TileGeometryConformance
{
    [TestCleanup] public void Free() => Box2DTesting.Free();
    protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(Box2DTesting.Pixels);
    protected override bool ReadsTilesLive => false;
    protected override ShapeId AddTiles(IPhysicsWorld2D world, BodyId body, ICollisionGrid grid)
    {
        var layer = ((TileGrid)grid).Layer;
        return world.AddTiles(body, grid, layer.Width, layer.Height);
    }
}

/// <summary>The outline a tile grid becomes: one loop per region of open space, straight runs merged.</summary>
[TestClass]
public sealed class TileOutlineTests
{
    private const TileFlags S = TileFlags.Solid, O = TileFlags.OneWay, R = TileFlags.SlopeRight, L = TileFlags.SlopeLeft, E = TileFlags.None;

    private static TileOutline.Result Trace(int columns, params TileFlags[] cells) => TileOutline.Trace(cells, columns, cells.Length / columns, 0, 0);

    private static (int X, int Y)[] Loop(TileOutline.Result outline, int index)
    {
        int start = 0;
        for (int i = 0; i < index; i++) start += outline.Loops[i];
        return outline.Points.GetRange(start, outline.Loops[index]).ToArray();
    }

    /// <summary>Twice the area a loop encloses, seen with y up: positive when it goes counter-clockwise (round solid ground).</summary>
    private static int Winding((int X, int Y)[] loop)
    {
        int sum = 0;
        for (int i = 0; i < loop.Length; i++)
        {
            var (a, b) = (loop[i], loop[(i + 1) % loop.Length]);
            sum += a.X * -b.Y - b.X * -a.Y;
        }
        return sum;
    }

    [TestMethod]
    public void A_row_of_tiles_is_one_rectangle_with_no_point_where_tiles_meet()
    {
        var outline = Trace(5, S, S, S, S, S);
        Assert.AreEqual(1, outline.Loops.Count);
        var loop = Loop(outline, 0);
        CollectionAssert.AreEquivalent(new[] { (0, 0), (5, 0), (5, 1), (0, 1) }, loop, "four corners: the top is one segment");
        Assert.AreEqual(10, Winding(loop), "counter-clockwise round the solid, twice its area");
    }

    [TestMethod]
    public void An_L_shape_has_six_corners_and_a_hole_is_a_second_loop_wound_the_other_way()
    {
        var corner = Trace(3,
            S, E, E,
            S, S, S);
        Assert.AreEqual(1, corner.Loops.Count);
        Assert.AreEqual(6, corner.Loops[0]);
        Assert.AreEqual(8, Winding(Loop(corner, 0)));

        var ring = Trace(3,
            S, S, S,
            S, E, S,
            S, S, S);
        Assert.AreEqual(2, ring.Loops.Count);
        int outer = Winding(Loop(ring, 0)), inner = Winding(Loop(ring, 1));
        Assert.AreEqual(18, Math.Max(outer, inner), "the outside, counter-clockwise");
        Assert.AreEqual(-2, Math.Min(outer, inner), "the hole, clockwise: its segments face the open space inside");
    }

    [TestMethod]
    public void Cells_that_touch_at_a_corner_are_outlined_by_the_open_space_on_each_side()
    {
        // Two solid cells on a diagonal: the open space is one region on each side of the corner they share.
        var outline = Trace(2,
            S, E,
            E, S);
        Assert.AreEqual(1, outline.Loops.Count, "one loop that passes through the shared corner twice");
        var loop = Loop(outline, 0);
        Assert.AreEqual(8, loop.Length);
        Assert.AreEqual(2, loop.Count(p => p == (1, 1)));
        Assert.AreEqual(4, Winding(loop));
    }

    [TestMethod]
    public void A_ramp_of_slope_tiles_is_one_straight_segment()
    {
        // A ramp that rises to the right over three tiles, on a floor.
        var outline = Trace(4,
            E, E, R, S,
            E, R, S, S,
            R, S, S, S,
            S, S, S, S);
        Assert.AreEqual(1, outline.Loops.Count);
        var loop = Loop(outline, 0);
        CollectionAssert.AreEquivalent(new[] { (0, 3), (3, 0), (4, 0), (4, 4), (0, 4) }, loop, "the ramp is one edge from (0, 3) to (3, 0)");

        var down = Trace(2,
            L, E,
            S, L);
        CollectionAssert.AreEquivalent(new[] { (0, 0), (2, 2), (0, 2) }, Loop(down, 0), "and the same the other way: a triangle");
    }

    [TestMethod]
    public void One_way_tiles_are_runs_and_not_part_of_the_outline()
    {
        var outline = Trace(5,
            O, O, E, O, S,
            E, E, E, E, E);
        CollectionAssert.AreEqual(new[] { (0, 0, 2), (0, 3, 4) }, outline.OneWay);
        Assert.AreEqual(1, outline.Loops.Count, "only the solid cell");
        Assert.AreEqual(4, outline.Loops[0]);
    }
}

/// <summary>Tile levels on Box2D: dynamic bodies on them, slopes, events, and what the tile backend says about slopes.</summary>
[TestClass]
public sealed class Box2DTileLevelTests
{
    private const int Tile = 16;
    private static readonly Dictionary<char, int> Legend = new() { ['#'] = 1, ['='] = 2, ['/'] = 3, ['\\'] = 4 };
    private static readonly Dictionary<int, TileFlags> Flags = new()
    {
        [1] = TileFlags.Solid, [2] = TileFlags.OneWay, [3] = TileFlags.SlopeRight, [4] = TileFlags.SlopeLeft,
    };

    [TestCleanup] public void Free() => Box2DTesting.Free();

    private static (Box2DPhysicsWorld2D World, ShapeId Tiles, TileGrid Grid) Level(params string[] rows)
    {
        var world = Box2DTesting.Create(Box2DTesting.Pixels);
        var grid = new TileGrid(TileLayer.Parse(rows, Legend), Tile, Flags) { OutsideFlags = TileFlags.None };
        var body = world.CreateBody(new EntityId(9, 1), BodyDescription.Static());
        return (world, world.AddTiles(body, grid, grid.Layer.Width, grid.Layer.Height), grid);
    }

    private static BodyId Crate(Box2DPhysicsWorld2D world, Vector2 centre, float size = 12, float friction = 0)
    {
        var body = world.CreateBody(default, BodyDescription.Dynamic(centre));
        world.AddShape(body, ShapeDescription.Box(new Vector2(size)) with { Friction = friction });
        return body;
    }

    [TestMethod]
    public void A_box_slides_along_a_flat_row_of_tiles_without_catching_on_a_seam()
    {
        var (world, tiles, _) = Level(
            "..............................",
            "..............................",
            "##############################");
        // The floor's top is y 32. A box without friction rests on it, then is sent right at 4 tiles a second.
        var crate = Crate(world, new Vector2(20, 26));
        for (int i = 0; i < 60; i++) world.Step(1 / 60f);
        Assert.AreEqual(26, world.PositionOf(crate).Y, 0.5f, "resting on the tiles");
        world.SetVelocity(crate, new Vector2(64, 0));
        float slowest = 64, lift = 0, turn = 0;
        for (int i = 0; i < 360; i++)
        {
            world.Step(1 / 60f);
            slowest = MathF.Min(slowest, world.VelocityOf(crate).X);
            lift = MathF.Max(lift, MathF.Abs(world.VelocityOf(crate).Y));
            turn = MathF.Max(turn, MathF.Abs(world.RotationOf(crate)));
        }
        // Six seconds: 24 tiles, across 24 seams.
        Assert.AreEqual(20 + 384, world.PositionOf(crate).X, 0.05f, "it went the whole way at its speed");
        Assert.IsTrue(slowest > 63.99f, $"it never slowed: {slowest} (over one box per tile it dips to 63.93 at the seams)");
        Assert.IsTrue(lift < 0.005f, $"it never hopped: {lift}");
        Assert.IsTrue(turn < 0.0001f, $"it never turned: {turn}");
        Span<ContactPoint> points = stackalloc ContactPoint[4];
        int count = world.ContactsOf(crate, points);
        Assert.AreEqual(2, count);
        Assert.AreEqual(tiles, points[0].Other, "it touches the tile shape");
        Assert.AreEqual(new EntityId(9, 1), points[0].OtherOwner);
    }

    [TestMethod]
    public void A_dynamic_box_lands_on_one_way_tiles_and_passes_them_from_below()
    {
        var (world, _, _) = Level(
            "......",
            "......",
            "..==..",
            "......",
            "......",
            "######");
        var above = Crate(world, new Vector2(40, 10), 8);
        var below = Crate(world, new Vector2(56, 70), 8);
        world.SetVelocity(below, new Vector2(0, -260));
        float highest = 70;
        for (int i = 0; i < 120; i++)
        {
            world.Step(1 / 60f);
            highest = MathF.Min(highest, world.PositionOf(below).Y);
        }
        Assert.AreEqual(32 - 4, world.PositionOf(above).Y, 1, "the one from above rests on the platform's top (y 32)");
        Assert.IsTrue(highest < 32, $"the one from below went up through the platform: {highest}");
    }

    [TestMethod]
    public void A_ball_rolls_down_a_ramp_of_slope_tiles_and_a_capsule_character_walks_up_it()
    {
        var (world, tiles, _) = Level(
            "#........#",
            "#........#",
            "#...../###",
            "#..../####",
            "#.../#####",
            "##########");
        Assert.AreEqual(PhysicsCapabilities.TileSlopes, world.Capabilities & PhysicsCapabilities.TileSlopes);
        // The ramp goes from (64, 80) up to (112, 32). A ball dropped on its middle rolls down to the left.
        var ball = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(92, 30)));
        world.AddShape(ball, ShapeDescription.Circle(5) with { Friction = 0.8f });
        for (int i = 0; i < 150; i++) world.Step(1 / 60f);
        var rolled = world.PositionOf(ball);
        Assert.IsTrue(rolled.X < 60 && rolled.X > 16, $"it rolled off the ramp to the left, as far as the wall: {rolled}");
        Assert.AreEqual(75, rolled.Y, 1.5f, "onto the floor (top y 80)");

        // A capsule character walks from the floor up the ramp onto the ledge.
        var body = world.CreateBody(default, BodyDescription.Kinematic(new Vector2(44, 72)));
        world.AddShape(body, ShapeDescription.Capsule(5, 14));
        var hero = new CharacterController2D(world, body, new CharacterMotion { Gravity = 900, MaxFallSpeed = 400, MoveSpeed = 60, JumpSpeed = 300, GroundSnap = 4 });
        int airborne = 0;
        bool onRamp = false;
        for (int i = 0; i < 240; i++)
        {
            hero.Step(1 / 60f, new CharacterInput(1));
            if (i > 20 && !hero.IsGrounded) airborne++;
            if (hero.Position.X < 130) Assert.IsFalse(hero.IsOnWall, $"no wall on the way up, at the foot of the ramp or where it meets the ledge: step {i}, {hero.Position}, {hero.WallNormal}");
            if (hero.IsGrounded && hero.LastMove.GroundNormal.X < -0.6f)
            {
                onRamp = true;
                Assert.AreEqual(tiles, hero.Ground);
            }
        }
        Assert.IsTrue(onRamp, "on the ramp the ground's normal leans to the left");
        Assert.IsTrue(hero.Position.X > 118, $"it reached the ledge: {hero.Position}, {airborne} steps in the air");
        Assert.AreEqual(32 - 7, hero.Position.Y, 1.5f, "standing on the ledge (top y 32)");
        Assert.IsTrue(airborne <= 2, $"it stayed on the ground on the way: {airborne} steps in the air");
    }

    [TestMethod]
    public void A_body_that_touches_several_faces_of_the_tiles_begins_and_ends_one_contact()
    {
        var (world, tiles, _) = Level(
            "#....",
            "#....",
            "#####");
        var crate = Crate(world, new Vector2(40, 20));
        int begins = 0, ends = 0;
        void Run(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                world.Step(1 / 60f);
                foreach (var e in world.ContactEvents)
                {
                    Assert.IsTrue(e.A == tiles || e.B == tiles, e.ToString());
                    if (e.Kind == ContactEventKind.Begin) begins++;
                    else ends++;
                }
            }
        }
        Run(60);
        world.SetVelocity(crate, new Vector2(-80, 0));
        Run(60);
        Assert.AreEqual(22, world.PositionOf(crate).X, 2, "against the wall (x 16) and on the floor");
        Assert.AreEqual(1, begins, "the floor and the wall are one shape: one contact began");
        Assert.AreEqual(0, ends);
        world.SetVelocity(crate, new Vector2(60, -200));
        Run(20);
        Assert.AreEqual(1, ends, "and one ended when it left both");
    }

    [TestMethod]
    public void The_inside_of_solid_tiles_answers_overlaps_and_removing_the_tiles_removes_everything()
    {
        var (world, tiles, grid) = Level(
            ".....",
            "#####",
            "#####",
            "#####");
        Span<ShapeId> found = stackalloc ShapeId[4];
        Assert.AreEqual(1, world.Overlap(new RectF(36, 36, 8, 8), default, found), "an area wholly inside solid tiles");
        Assert.AreEqual(tiles, found[0]);
        Assert.AreEqual(1, world.Overlap(QueryShape.Circle(3), new Vector2(40, 40), default, found));
        Assert.AreEqual(1, world.OverlapPoint(new Vector2(40, 40), default, found));
        Assert.IsTrue(world.ClosestPoint(new Vector2(40, 40), 4, default, out var held));
        Assert.AreEqual(0, held.Distance);
        Assert.AreEqual(0, world.Overlap(new RectF(36, 4, 8, 8), default, found), "above them");
        Assert.AreEqual(new RectF(0, 0, 80, 64), world.BoundsOf(tiles), "the area they were added with");
        Assert.AreSame(grid, world.TilesOf(tiles));
        Assert.AreEqual(ShapeKind.Box, world.DescriptionOf(tiles).Kind);
        Assert.AreEqual(new Vector2(Tile), world.DescriptionOf(tiles).Size);

        var crate = Crate(world, new Vector2(40, 8));
        for (int i = 0; i < 30; i++) world.Step(1 / 60f);
        Assert.AreEqual(10, world.PositionOf(crate).Y, 0.5f, "resting on the tiles (top y 16)");
        world.RemoveShape(tiles);
        Assert.IsFalse(world.IsAlive(tiles));
        for (int i = 0; i < 30; i++) world.Step(1 / 60f);
        Assert.IsTrue(world.PositionOf(crate).Y > 30, "with the tiles gone it falls");
        Assert.AreEqual(0, world.OverlapPoint(new Vector2(40, 40), default, found));
        Assert.IsFalse(world.RayCast(new Vector2(70, 0), new Vector2(70, 100), default, out _));
    }

    [TestMethod]
    public void The_bounds_of_tiles_follow_their_body_when_it_is_turned()
    {
        var (world, tiles, _) = Level("####", "####");
        Assert.AreEqual(new RectF(0, 0, 64, 32), world.BoundsOf(tiles));
        // A quarter turn about the origin: the 64 x 32 area stands 32 wide and 64 tall.
        world.SetRotation(world.BodyOf(tiles), MathF.PI / 2);
        var turned = world.BoundsOf(tiles);
        Assert.AreEqual(32, turned.Width, 0.2f, turned.ToString());
        Assert.AreEqual(64, turned.Height, 0.2f, turned.ToString());
        Span<ShapeId> found = stackalloc ShapeId[2];
        Assert.AreEqual(1, world.OverlapPoint(turned.Center, default, found), "the tiles are where their bounds say");
        world.SetPosition(world.BodyOf(tiles), new Vector2(100, 50));
        Assert.AreEqual(turned.Center + new Vector2(100, 50), world.BoundsOf(tiles).Center);
    }

    [TestMethod]
    public void The_tile_backend_refuses_a_level_with_slope_tiles_instead_of_playing_it_without_them()
    {
        var grid = new TileGrid(TileLayer.Parse(["..../", "#####"], Legend), Tile, Flags) { OutsideFlags = TileFlags.None };
        IPhysicsWorld2D world = new TilePhysicsWorld2D();
        var body = world.CreateBody(default, BodyDescription.Static());
        var e = Assert.ThrowsExactly<PhysicsCapabilityException>(() => world.AddTiles(body, grid, 5, 2));
        Assert.AreEqual(PhysicsCapabilities.TileSlopes, e.Missing);
        StringAssert.Contains(e.Message, "MicroEngine.Physics.Box2D");
        Span<ShapeId> shapes = stackalloc ShapeId[2];
        Assert.AreEqual(0, world.ShapesOf(body, shapes), "nothing was added");
    }
}

/// <summary>The way out to Box2D's own API: opt-in ids and the functions the library exports.</summary>
[TestClass]
public sealed class Box2DNativeAccessTests
{
    [DllImport(Box2DNative.Library)] private static extern float b2Body_GetMass(B2BodyId body);
    [DllImport(Box2DNative.Library)] private static extern void b2Body_SetBullet(B2BodyId body, [MarshalAs(UnmanagedType.U1)] bool flag);
    [DllImport(Box2DNative.Library)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool b2Body_IsBullet(B2BodyId body);
    [DllImport(Box2DNative.Library)] private static extern int b2World_GetAwakeBodyCount(B2WorldId world);
    [DllImport(Box2DNative.Library)] private static extern float b2Shape_GetDensity(B2ShapeId shape);
    [DllImport(Box2DNative.Library)] private static extern float b2DistanceJoint_GetCurrentLength(B2JointId joint);

    [TestCleanup] public void Free() => Box2DTesting.Free();

    [TestMethod]
    public void A_world_gives_its_ids_only_when_it_was_made_for_it()
    {
        var plain = Box2DTesting.Create();
        var e = Assert.ThrowsExactly<InvalidOperationException>(() => plain.NativeIds);
        StringAssert.Contains(e.Message, "NativeAccess");
    }

    [DllImport(Box2DNative.Library)] private static extern void b2DestroyBody(B2BodyId body);

    [TestMethod]
    public void An_engine_body_destroyed_through_Box2D_is_refused_with_an_error_instead_of_ending_the_process()
    {
        var world = Box2DTesting.Create(new Box2DWorldOptions { NativeAccess = true });
        var crate = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(0, 1)));
        var shape = world.AddShape(crate, ShapeDescription.Box(new Vector2(0.5f)));
        var other = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(3, 1)));
        b2DestroyBody(world.NativeIds.Body(crate));
        var e = Assert.ThrowsExactly<InvalidOperationException>(() => world.PositionOf(crate));
        StringAssert.Contains(e.Message, "Box2D's own API");
        Assert.ThrowsExactly<InvalidOperationException>(() => world.SetVelocity(crate, Vector2.One));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.NativeIds.Shape(shape));
        Assert.AreEqual(new Vector2(3, 1), world.PositionOf(other), "the rest of the world is as it was");
    }

    [TestMethod]
    public void A_game_calls_Box2D_functions_with_the_ids_of_engine_objects()
    {
        var world = Box2DTesting.Create(new Box2DWorldOptions { NativeAccess = true });
        var anchor = world.CreateBody(default, BodyDescription.Static(new Vector2(0, 3)));
        world.AddShape(anchor, ShapeDescription.Box(new Vector2(0.2f)));
        var crate = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(0, 1)));
        var shape = world.AddShape(crate, ShapeDescription.Box(new Vector2(0.5f)) with { Density = 3 });
        var rope = world.CreateJoint(JointDescription.Distance(anchor, crate, new Vector2(0, 3), new Vector2(0, 1)));
        var ids = world.NativeIds;

        Assert.AreEqual(world.MassOf(crate), b2Body_GetMass(ids.Body(crate)), 1e-6f, "Box2D answers about the engine's body");
        Assert.AreEqual(3, b2Shape_GetDensity(ids.Shape(shape)), 1e-6f);
        Assert.AreEqual(2, b2DistanceJoint_GetCurrentLength(ids.Joint(rope)), 1e-4f);
        Assert.AreEqual(1, b2World_GetAwakeBodyCount(ids.World));
        // Something the contract has no call for: turning continuous collision on for a body that exists.
        Assert.IsFalse(b2Body_IsBullet(ids.Body(crate)));
        b2Body_SetBullet(ids.Body(crate), true);
        Assert.IsTrue(b2Body_IsBullet(ids.Body(crate)));
        world.Step(1 / 60f);

        Assert.ThrowsExactly<InvalidOperationException>(() => ids.Body(BodyId.None));
        Assert.ThrowsExactly<InvalidOperationException>(() => ids.Shape(ShapeId.None));
        Assert.ThrowsExactly<InvalidOperationException>(() => ids.Joint(JointId.None));
        var level = world.CreateBody(default, BodyDescription.Static());
        var tiles = world.AddTiles(level, new TileGrid(new TileLayer(2, 2), 16, new Dictionary<int, TileFlags>()), 2, 2);
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => ids.Shape(tiles)).Message, "tile geometry");
        world.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => ids.World);
    }
}
