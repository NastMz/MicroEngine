using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.World;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// Tile geometry: tile grids fed directly into a world as static geometry. Solid cells block from every side, one-way
/// cells are one-way platforms, other cells are not geometry, and changing a tile changes collisions (at once on a
/// backend that reads the grid at every query, after the tiles are added again on one that builds shapes from them).
/// </summary>
/// <remarks>Grids are 16-unit tiles in pixel space (Y down), so this suite runs at <see cref="PhysicsConformanceHelpers.Scale"/> 1.</remarks>
public abstract class TileGeometryConformance : CapabilityConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.TileGeometry;

    /// <summary>Adds <paramref name="grid"/> to the static <paramref name="body"/> with the backend's own call.</summary>
    protected abstract ShapeId AddTiles(IPhysicsWorld2D world, BodyId body, ICollisionGrid grid);

    /// <summary>Whether the backend reads the grid at every query (true) or builds shapes from it when it is added.</summary>
    protected virtual bool ReadsTilesLive => true;

    private static readonly Dictionary<char, int> Legend = new() { ['#'] = 1, ['='] = 2, ['^'] = 3 };
    private static readonly Dictionary<int, TileFlags> Flags = new() { [1] = TileFlags.Solid, [2] = TileFlags.OneWay, [3] = TileFlags.Hazard };

    private (IPhysicsWorld2D World, ShapeId Tiles, TileGrid Grid) Map(params string[] rows)
    {
        var world = CreateWorld();
        var grid = new TileGrid(TileLayer.Parse(rows, Legend), 16, Flags) { OutsideFlags = TileFlags.None };
        var body = world.CreateBody(default, BodyDescription.Static());
        return (world, AddTiles(world, body, grid), grid);
    }

    private static BodyId Player(IPhysicsWorld2D world, float left, float top)
    {
        var body = world.CreateBody(default, BodyDescription.Kinematic(new Vector2(left + 6, top + 7)));
        world.AddShape(body, ShapeDescription.Box(new Vector2(12, 14)));
        return body;
    }

    [TestMethod]
    public void A_box_lands_flush_on_solid_tiles_and_walks_into_a_tile_wall()
    {
        var (world, tiles, _) = Map("......#", "......#", "#######");
        var player = Player(world, 2, 0);
        var land = world.Move(player, new Vector2(0, 40));
        Assert.AreEqual(32 - 7, world.PositionOf(player).Y, Tolerance(world), land.ToString());
        Assert.IsTrue(land.OnGround && land.Ground == tiles, land.ToString());
        var walk = world.Move(player, new Vector2(200, 0));
        Assert.AreEqual(96 - 6, world.PositionOf(player).X, Tolerance(world), "flush with the tile wall at x 96");
        Assert.IsTrue(walk.HitWall && walk.OnGround, walk.ToString());
    }

    [TestMethod]
    public void Two_views_of_the_same_tiles_block_different_movers()
    {
        // Water (~) stops a walker and not a flyer; a wall (#) stops both. One grid, a view for each kind of mover.
        const ulong WalkerTiles = 1UL << 4, FlyerTiles = 1UL << 5;
        var world = CreateWorld();
        var grid = new TileGrid(TileLayer.Parse(["........", "...~...#", "........"], new Dictionary<char, int> { ['#'] = 1, ['~'] = 2 }), 16,
            new Dictionary<int, TileFlags> { [1] = TileFlags.Solid, [2] = TileFlags.Water }) { OutsideFlags = TileFlags.None };
        var level = world.CreateBody(default, BodyDescription.Static());
        world.AddTiles(level, grid.Blocking(TileFlags.Solid | TileFlags.Water), 8, 3, WalkerTiles);
        world.AddTiles(level, grid.Blocking(TileFlags.Solid), 8, 3, FlyerTiles);
        Assert.AreEqual(TileFlags.Water | TileFlags.Solid, grid.Blocking(TileFlags.Water).FlagsAt(3, 1), "the view keeps the tile's own flags");
        Assert.AreEqual(TileFlags.None, grid.Blocking(TileFlags.Water).FlagsAt(7, 1), "and a wall is no wall for a mover it does not block");

        BodyId Mover(ulong tiles)
        {
            var body = world.CreateBody(default, BodyDescription.Kinematic(new Vector2(8, 24)));
            world.AddShape(body, ShapeDescription.Box(new Vector2(12, 12)).InLayers(CollisionLayers.Default, tiles));
            return body;
        }
        var walker = Mover(WalkerTiles);
        var flyer = Mover(FlyerTiles);
        var walked = world.Move(walker, new Vector2(200, 0), new QueryFilter(CollisionLayers.Default, WalkerTiles));
        Assert.AreEqual(48 - 6, world.PositionOf(walker).X, Tolerance(world), $"the walker stops at the water: {walked}");
        var flown = world.Move(flyer, new Vector2(200, 0), new QueryFilter(CollisionLayers.Default, FlyerTiles));
        Assert.AreEqual(112 - 6, world.PositionOf(flyer).X, Tolerance(world), $"the flyer crosses it and stops at the wall: {flown}");
    }

    [TestMethod]
    public void A_fast_box_does_not_skip_a_row_of_tiles()
    {
        var (world, _, _) = Map(".", ".", ".", ".", ".", "#");
        var player = Player(world, 2, 0);
        var fall = world.Move(player, new Vector2(0, 500));
        Assert.AreEqual(80 - 7, world.PositionOf(player).Y, Tolerance(world), fall.ToString());
    }

    [TestMethod]
    public void One_way_tiles_are_one_way_platforms_and_hazards_are_not_geometry()
    {
        var (world, tiles, _) = Map("....", "....", "====", "....", "^^^^");
        var player = Player(world, 0, 10);
        var land = world.Move(player, new Vector2(0, 20));
        Assert.AreEqual(32 - 7, world.PositionOf(player).Y, Tolerance(world), $"lands on the one-way row: {land}");
        Assert.IsTrue(land.OnGround && land.Ground == tiles);
        var drop = world.Move(player, new Vector2(0, 40), new QueryFilter { PassOneWay = true });
        Assert.AreEqual(25 + 40, world.PositionOf(player).Y, Tolerance(world), $"drops through, and through the hazards: {drop}");
        world.SetPosition(player, new Vector2(6, 60));
        var jump = world.Move(player, new Vector2(0, -40));
        Assert.AreEqual(20, world.PositionOf(player).Y, Tolerance(world), $"jumps up through: {jump}");
    }

    [TestMethod]
    public void Changing_a_tile_changes_collisions_at_once()
    {
        var (world, tiles, grid) = Map("....", "....", "....");
        var player = Player(world, 0, 0);
        grid.Layer[0, 2] = 1;
        if (!ReadsTilesLive)
        {
            // The tiles were read when they were added: before they are added again nothing has changed.
            var before = world.Move(player, new Vector2(0, 30));
            Assert.AreEqual(37, world.PositionOf(player).Y, Tolerance(world), before.ToString());
            world.SetPosition(player, new Vector2(6, 7));
            var body = world.BodyOf(tiles);
            world.RemoveShape(tiles);
            Assert.IsFalse(world.IsAlive(tiles));
            AddTiles(world, body, grid);
        }
        world.Move(player, new Vector2(0, 40));
        Assert.AreEqual(32 - 7, world.PositionOf(player).Y, Tolerance(world));
    }

    [TestMethod]
    public void Rays_and_overlaps_see_solid_and_one_way_cells_exactly()
    {
        var (world, tiles, _) = Map("....", "...#", "====");
        Assert.IsTrue(world.RayCast(new Vector2(8, 0), new Vector2(8, 64), default, out var down));
        Assert.AreEqual(tiles, down.Shape);
        Assert.AreEqual(32, down.Point.Y, Rounding, "the one-way row's top");
        Assert.AreEqual(new Vector2(0, -1), down.Normal);
        Assert.IsTrue(world.RayCast(new Vector2(0, 24), new Vector2(64, 24), default, out var right));
        Assert.AreEqual(48, right.Point.X, Rounding, "the solid cell at x 48");
        Assert.AreEqual(new Vector2(-1, 0), right.Normal);
        Assert.IsFalse(world.RayCast(new Vector2(8, 60), new Vector2(8, 0), default, out _), "one-way cells are not hit from below");
        Span<ShapeId> found = stackalloc ShapeId[2];
        Assert.AreEqual(0, world.Overlap(new RectF(0, 0, 47, 32), default, found), "touching a solid cell's edge is not overlapping it");
        Assert.AreEqual(1, world.Overlap(new RectF(40, 20, 10, 4), default, found));
        Assert.AreEqual(0, world.Overlap(new RectF(0, 30, 16, 10), new QueryFilter { PassOneWay = true }, found));
    }

    [TestMethod]
    public void Shape_queries_see_the_cells_of_a_grid()
    {
        var (world, tiles, _) = Map("....", "...#", "====", "....", "####");
        if ((world.Capabilities & PhysicsCapabilities.ShapeQueries) == 0) Assert.Inconclusive("the backend has no shape queries");
        float slack = world.Skin + Rounding;
        // A circle of radius 4 falls onto the one-way row (top 32) and, passing it, onto the solid row (top 64).
        Assert.IsTrue(world.ShapeCast(QueryShape.Circle(4), new Vector2(8, 8), new Vector2(8, 88), default, out var down));
        Assert.AreEqual(tiles, down.Shape);
        Assert.AreEqual(20f / 80, down.Fraction, slack / 80);
        Assert.AreEqual(-1, down.Normal.Y, 1e-3f);
        Assert.IsTrue(world.ShapeCast(QueryShape.Circle(4), new Vector2(8, 8), new Vector2(8, 88), new QueryFilter { PassOneWay = true }, out var through));
        Assert.AreEqual(52f / 80, through.Fraction, slack / 80);
        Assert.IsFalse(world.ShapeCast(QueryShape.Box(new Vector2(8, 8)), new Vector2(8, 56), new Vector2(8, 8), default, out _), "one-way cells are passed from below");
        // A box going right in the second row stops against the solid cell at x 48.
        Assert.IsTrue(world.ShapeCast(QueryShape.Box(new Vector2(8, 8)), new Vector2(8, 24), new Vector2(72, 24), default, out var right));
        Assert.AreEqual(36f / 64, right.Fraction, slack / 64);
        Assert.AreEqual(-1, right.Normal.X, 1e-3f);
        Assert.AreEqual(48, right.Point.X, slack);

        Span<ShapeId> found = stackalloc ShapeId[2];
        Assert.AreEqual(0, world.Overlap(QueryShape.Circle(5), new Vector2(42, 10), default, found), "5 from the cell's corner (48, 16) is 7.2 away");
        Assert.AreEqual(1, world.Overlap(QueryShape.Circle(5), new Vector2(45, 13), default, found));
        Assert.AreEqual(tiles, found[0]);
        Assert.AreEqual(1, world.OverlapPoint(new Vector2(56, 24), default, found), "inside the solid cell");
        Assert.AreEqual(0, world.OverlapPoint(new Vector2(24, 24), default, found));
        Assert.AreEqual(1, world.OverlapPoint(new Vector2(24, 72), default, found), "inside the solid row");

        Assert.IsTrue(world.ClosestPoint(new Vector2(40, 24), 12, default, out var near));
        Assert.AreEqual(tiles, near.Shape);
        Assert.AreEqual(8, near.Distance, Rounding);
        Assert.AreEqual(new Vector2(48, 24), near.Point);
        Assert.IsFalse(world.ClosestPoint(new Vector2(8, 8), 10, default, out _));

        Span<RayHit> hits = stackalloc RayHit[4];
        Assert.AreEqual(1, world.RayCastAll(new Vector2(8, 0), new Vector2(8, 80), default, hits), "a grid is one shape: one hit");
        Assert.AreEqual(32, hits[0].Point.Y, Rounding);
    }

    [TestMethod]
    public void A_ray_on_the_surface_of_tiles_hits_going_in_and_nothing_going_out()
    {
        var (world, tiles, _) = Map(".....", "#####");
        Assert.IsFalse(world.RayCast(new Vector2(40, 16), new Vector2(60, 4), default, out _), "leaving the floor it starts on");
        Assert.IsFalse(world.RayCast(new Vector2(8, 16), new Vector2(72, 16), default, out _), "along the floor's top, across the tiles");
        Assert.IsTrue(world.RayCast(new Vector2(40, 16), new Vector2(60, 28), default, out var into));
        Assert.AreEqual(tiles, into.Shape);
        Assert.AreEqual(0, into.Fraction, Rounding);
        Assert.AreEqual(new Vector2(0, -1), into.Normal);
    }

    [TestMethod]
    public void A_shape_sunk_into_tiles_is_not_stopped_by_the_faces_between_them()
    {
        var (world, _, _) = Map("..........", "##########", "##########");
        // The floor's top is y 16. A box sunk half a unit into it, cast and moved along it.
        if ((world.Capabilities & PhysicsCapabilities.ShapeQueries) != 0)
        {
            Assert.IsFalse(world.ShapeCast(QueryShape.Box(new Vector2(12, 14)), new Vector2(40, 9.5f), new Vector2(120, 9.5f), default, out var hit), $"nothing but the floor it is sunk in: {hit}");
            Assert.IsFalse(world.ShapeCast(QueryShape.Circle(6), new Vector2(40, 10.5f), new Vector2(120, 10.5f), default, out hit), hit.ToString());
        }
        var player = world.CreateBody(default, BodyDescription.Kinematic(new Vector2(40, 9.5f)));
        world.AddShape(player, ShapeDescription.Box(new Vector2(12, 14)));
        var moved = world.Move(player, new Vector2(80, 0));
        Assert.AreEqual(120, world.PositionOf(player).X, Tolerance(world), $"the move goes the whole way, as the cast said: {moved}");
        Assert.IsFalse(moved.HitWall, moved.ToString());
    }

    [TestMethod]
    public void A_one_way_tile_is_a_platform_a_quarter_of_a_tile_thick_for_overlaps_points_and_the_nearest_surface()
    {
        var (world, tiles, _) = Map("....", "....", "====", "....", "....");
        if ((world.Capabilities & PhysicsCapabilities.ShapeQueries) == 0) Assert.Inconclusive("the backend has no shape queries");
        Span<ShapeId> found = stackalloc ShapeId[2];
        // The row's cells are y 32..48; the platform is their top quarter, y 32..36.
        Assert.AreEqual(1, world.OverlapPoint(new Vector2(24, 34), default, found), "inside the platform");
        Assert.AreEqual(tiles, found[0]);
        Assert.AreEqual(0, world.OverlapPoint(new Vector2(24, 42), default, found), "in the cell, under the platform");
        Assert.AreEqual(1, world.Overlap(new RectF(20, 30, 8, 4), default, found));
        Assert.AreEqual(0, world.Overlap(new RectF(20, 38, 8, 8), default, found), "an area under the platform, inside the cell");
        Assert.AreEqual(1, world.Overlap(QueryShape.Circle(3), new Vector2(24, 34), default, found));
        Assert.AreEqual(0, world.Overlap(QueryShape.Circle(3), new Vector2(24, 43), default, found));
        Assert.IsTrue(world.ClosestPoint(new Vector2(24, 44), 10, default, out var under));
        Assert.AreEqual(8, under.Distance, Tolerance(world), "from under it, the platform's underside at y 36");
        Assert.AreEqual(36, under.Point.Y, Tolerance(world));
        Assert.IsTrue(world.ClosestPoint(new Vector2(24, 34), 10, default, out var inside));
        Assert.AreEqual(0, inside.Distance, Tolerance(world));
        Assert.AreEqual(0, world.OverlapPoint(new Vector2(24, 34), new QueryFilter { PassOneWay = true }, found));
    }

    [TestMethod]
    public void The_count_of_a_ray_through_one_way_and_solid_tiles_does_not_depend_on_the_room_given()
    {
        var (world, tiles, _) = Map("....", "====", "....", "####");
        if ((world.Capabilities & PhysicsCapabilities.ShapeQueries) == 0) Assert.Inconclusive("the backend has no shape queries");
        Span<RayHit> hits = stackalloc RayHit[4];
        Assert.AreEqual(1, world.RayCastAll(new Vector2(24, 0), new Vector2(24, 64), default, hits), "a grid is one shape");
        Assert.AreEqual(tiles, hits[0].Shape);
        Assert.AreEqual(16, hits[0].Point.Y, Rounding, "at the first of its surfaces");
        Assert.AreEqual(1, world.RayCastAll(new Vector2(24, 0), new Vector2(24, 64), default, []), "the same count with no room");
        Assert.AreEqual(1, world.RayCastAll(new Vector2(24, 0), new Vector2(24, 64), default, hits[..1]));
    }

    [TestMethod]
    public void Tiles_are_static_geometry_only()
    {
        var world = CreateWorld();
        var kinematic = world.CreateBody(default, BodyDescription.Kinematic(Vector2.Zero));
        Assert.ThrowsExactly<InvalidOperationException>(() => AddTiles(world, kinematic, new TileGrid(new TileLayer(2, 2), 16, new Dictionary<int, TileFlags>())));
    }
}
