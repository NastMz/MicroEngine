using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.World;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// Tile geometry: tile grids fed directly into a world as static geometry. Solid cells block from every side, one-way
/// cells are one-way platforms, other cells are not geometry, and changing a tile changes collisions at once.
/// </summary>
/// <remarks>Grids are 16-unit tiles in pixel space (Y down), so this suite runs at <see cref="PhysicsConformanceHelpers.Scale"/> 1.</remarks>
public abstract class TileGeometryConformance : CapabilityConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.TileGeometry;

    /// <summary>Adds <paramref name="grid"/> to the static <paramref name="body"/> with the backend's own call.</summary>
    protected abstract ShapeId AddTiles(IPhysicsWorld2D world, BodyId body, ICollisionGrid grid);

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
        Assert.AreEqual(32 - 7, world.PositionOf(player).Y, Rounding, land.ToString());
        Assert.IsTrue(land.OnGround && land.Ground == tiles, land.ToString());
        var walk = world.Move(player, new Vector2(200, 0));
        Assert.AreEqual(96 - 6, world.PositionOf(player).X, Rounding, "flush with the tile wall at x 96");
        Assert.IsTrue(walk.HitWall && walk.OnGround, walk.ToString());
    }

    [TestMethod]
    public void A_fast_box_does_not_skip_a_row_of_tiles()
    {
        var (world, _, _) = Map(".", ".", ".", ".", ".", "#");
        var player = Player(world, 2, 0);
        var fall = world.Move(player, new Vector2(0, 500));
        Assert.AreEqual(80 - 7, world.PositionOf(player).Y, Rounding, fall.ToString());
    }

    [TestMethod]
    public void One_way_tiles_are_one_way_platforms_and_hazards_are_not_geometry()
    {
        var (world, tiles, _) = Map("....", "....", "====", "....", "^^^^");
        var player = Player(world, 0, 10);
        var land = world.Move(player, new Vector2(0, 20));
        Assert.AreEqual(32 - 7, world.PositionOf(player).Y, Rounding, $"lands on the one-way row: {land}");
        Assert.IsTrue(land.OnGround && land.Ground == tiles);
        var drop = world.Move(player, new Vector2(0, 40), new QueryFilter { PassOneWay = true });
        Assert.AreEqual(25 + 40, world.PositionOf(player).Y, Rounding, $"drops through, and through the hazards: {drop}");
        world.SetPosition(player, new Vector2(6, 60));
        var jump = world.Move(player, new Vector2(0, -40));
        Assert.AreEqual(20, world.PositionOf(player).Y, Rounding, $"jumps up through: {jump}");
    }

    [TestMethod]
    public void Changing_a_tile_changes_collisions_at_once()
    {
        var (world, _, grid) = Map("....", "....", "....");
        var player = Player(world, 0, 0);
        grid.Layer[0, 2] = 1;
        world.Move(player, new Vector2(0, 40));
        Assert.AreEqual(32 - 7, world.PositionOf(player).Y, Rounding);
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
    public void Tiles_are_static_geometry_only()
    {
        var world = CreateWorld();
        var kinematic = world.CreateBody(default, BodyDescription.Kinematic(Vector2.Zero));
        Assert.ThrowsExactly<InvalidOperationException>(() => AddTiles(world, kinematic, new TileGrid(new TileLayer(2, 2), 16, new Dictionary<int, TileFlags>())));
    }
}
