using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace MicroEngine.Navigation.Tests;

[TestClass]
public sealed class NavGridTests
{
    [TestMethod]
    public void Cells_and_world_points_convert_through_the_cell_size_and_the_origin()
    {
        var grid = new NavGrid(4, 3, cellSize: 16, origin: new Vector2(100, 50));
        Assert.AreEqual((4, 3, 12, 16f), (grid.Width, grid.Height, grid.NodeCount, grid.CellSize));
        Assert.AreEqual(new NavCell(0, 0), grid.CellAt(new Vector2(100, 50)));
        Assert.AreEqual(new NavCell(1, 0), grid.CellAt(new Vector2(131.9f, 65.9f)));
        Assert.AreEqual(new NavCell(-1, 3), grid.CellAt(new Vector2(99, 98)), "outside the grid, cells keep counting");
        Assert.AreEqual(grid.NodeOf(3, 2), grid.NodeAt(new Vector2(163, 97)));
        Assert.AreEqual(-1, grid.NodeAt(new Vector2(164, 97)));
        Assert.AreEqual(new Vector2(124, 90), grid.CenterOf(grid.NodeOf(1, 2)));
        Assert.AreEqual(new NavCell(1, 2), grid.CellOf(grid.NodeOf(1, 2)));
        Assert.IsFalse(grid.IsWalkable(4, 0), "nothing outside the grid can be walked");
        Assert.IsFalse(grid.InBounds(0, -1));
    }

    [TestMethod]
    public void Sizes_and_costs_that_make_no_sense_are_refused()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new NavGrid(0, 3));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new NavGrid(3, 3, cellSize: 0));
        var grid = new NavGrid(3, 3);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => grid.SetCost(0, 0, 0.5f), "cheaper than open ground would break the estimate");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => grid.SetCost(0, 0, float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => grid.SetCost(3, 0, 2));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => grid.SetWalkable(0, 3, false));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => grid.NodeOf(-1, 0));
        grid.SetCost(1, 1, 3);
        grid.SetWalkable(1, 1, false);
        grid.SetWalkable(1, 1, true);
        Assert.AreEqual(3f, grid.CostAt(1, 1), "a door keeps its cost while closed");
    }

    [TestMethod]
    public void A_rectangle_blocks_the_cells_it_overlaps_and_not_the_ones_it_only_touches()
    {
        var grid = new NavGrid(4, 4, cellSize: 10);
        grid.SetBlocked(new RectF(10, 10, 10, 10), true);
        Assert.AreEqual(1, Blocked(grid));
        Assert.IsFalse(grid.IsWalkable(1, 1));
        grid.SetBlocked(new RectF(5, 5, 10, 10), true);
        Assert.AreEqual(4, Blocked(grid));
        grid.SetBlocked(new RectF(-50, -50, 200, 200), false);
        Assert.AreEqual(0, Blocked(grid), "a rectangle larger than the grid is clipped to it");
    }

    private static int Blocked(NavGrid grid)
    {
        int count = 0;
        for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
                if (!grid.IsWalkable(x, y)) count++;
        return count;
    }

    [TestMethod]
    public void A_grid_is_read_from_a_tile_map_by_the_flags_that_block_and_read_again_when_tiles_change()
    {
        string[] rows = ["#  #", "# =#", "####"];
        var tiles = new TileGrid(TileLayer.Parse(rows, new Dictionary<char, int> { ['#'] = 1, ['='] = 2 }), 16,
            new Dictionary<int, TileFlags> { [1] = TileFlags.Solid, [2] = TileFlags.OneWay });
        var grid = NavGrid.FromTiles(tiles);
        Assert.AreEqual((4, 3, 16f), (grid.Width, grid.Height, grid.CellSize));
        Assert.AreEqual(8, Blocked(grid), "the solid tiles");
        Assert.IsTrue(grid.IsWalkable(2, 1), "a one-way platform does not block unless the game says so");
        Assert.AreEqual(9, Blocked(NavGrid.FromTiles(tiles, TileFlags.Solid | TileFlags.OneWay)));

        int version = grid.Version;
        Assert.IsFalse(grid.ReadTiles(tiles), "nothing changed");
        Assert.AreEqual(version, grid.Version);
        tiles.Layer[1, 0] = 1;      // a door closes in the tile map
        Assert.IsTrue(grid.ReadTiles(tiles));
        Assert.IsFalse(grid.IsWalkable(1, 0));
        Assert.AreNotEqual(version, grid.Version);
    }

    [TestMethod]
    public void A_grid_is_read_from_a_physics_world_through_the_filter_of_the_agent()
    {
        const ulong Walls = 1, Agents = 2;
        var world = new TilePhysicsWorld2D();
        // A wall 32 × 16 centred at (48, 24): cells (2,1) and (3,1) of 16-unit cells.
        var wall = world.CreateBody(new EntityId(1, 1), BodyDescription.Kinematic(new Vector2(48, 24)));
        world.AddShape(wall, ShapeDescription.Box(new Vector2(32, 16)).InLayers(Walls, CollisionLayers.All));
        // A sensor and another agent: neither blocks.
        world.AddShape(world.CreateBody(new EntityId(2, 1), BodyDescription.Static(new Vector2(8, 8))), ShapeDescription.Sensor(new Vector2(16, 16)));
        world.AddShape(world.CreateBody(new EntityId(3, 1), BodyDescription.Kinematic(new Vector2(88, 40))),
            ShapeDescription.Box(new Vector2(12, 12)).InLayers(Agents, CollisionLayers.All));
        // Tiles of the same world: one solid tile at cell (0, 2).
        var tiles = new TileGrid(TileLayer.Parse(["  ", "  ", "# "], new Dictionary<char, int> { ['#'] = 1 }), 16,
            new Dictionary<int, TileFlags> { [1] = TileFlags.Solid }) { OutsideFlags = TileFlags.None };
        world.AddTiles(world.CreateBody(new EntityId(4, 1), BodyDescription.Static()), tiles);

        var filter = new QueryFilter(Agents, Walls);
        var grid = NavGrid.FromPhysics(world, new RectF(0, 0, 96, 48), 16, filter);
        Assert.AreEqual((6, 3), (grid.Width, grid.Height));
        var blocked = new List<NavCell>();
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 6; x++)
                if (!grid.IsWalkable(x, y)) blocked.Add(new NavCell(x, y));
        CollectionAssert.AreEqual(new[] { new NavCell(2, 1), new NavCell(3, 1), new NavCell(0, 2) }, blocked);

        world.SetPosition(wall, new Vector2(48, 8));
        Assert.IsTrue(grid.ReadPhysics(world, filter), "the wall moved up a row");
        Assert.IsFalse(grid.IsWalkable(2, 0));
        Assert.IsTrue(grid.IsWalkable(2, 1));
        Assert.IsFalse(grid.ReadPhysics(world, filter));
    }

    [TestMethod]
    public void Line_of_sight_stops_at_walls_at_corners_and_at_the_edge_of_the_grid()
    {
        var grid = Maps.Grid("   ", " # ", "   ");
        Assert.IsTrue(grid.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(2.5f, 0.5f)));
        Assert.IsFalse(grid.HasLineOfSight(new Vector2(0.5f, 1.5f), new Vector2(2.5f, 1.5f)));
        Assert.IsFalse(grid.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(2.5f, 2.5f)));
        Assert.IsFalse(grid.HasLineOfSight(new Vector2(1.5f, 0.5f), new Vector2(2.5f, 1.5f)), "exactly through the wall's corner");
        Assert.IsTrue(grid.HasLineOfSight(new Vector2(1.5f, 0.5f), new Vector2(2.9f, 1.5f)), "past the corner");
        Assert.IsFalse(grid.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(5, 0.5f)), "outside the grid nothing can be walked");
        Assert.IsTrue(grid.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)));
    }

    [TestMethod]
    public void Line_of_sight_of_an_agent_with_a_size_needs_room_for_its_whole_square()
    {
        var corridor = Maps.Grid("###", "   ", "###");
        var (from, to) = (new Vector2(0.5f, 1.5f), new Vector2(2.5f, 1.5f));
        Assert.IsTrue(corridor.HasLineOfSight(from, to, radius: 0.5f), "an agent as wide as the corridor fits it");
        Assert.IsFalse(corridor.HasLineOfSight(from, to, radius: 0.6f));

        var grid = Maps.Grid("    ", " #  ", "    ");
        // A line that passes 0.3 above the wall's top-right corner region: free as a point, not for a square of 0.4.
        Assert.IsTrue(grid.HasLineOfSight(new Vector2(0.5f, 0.7f), new Vector2(3.5f, 0.7f)));
        Assert.IsFalse(grid.HasLineOfSight(new Vector2(0.5f, 0.7f), new Vector2(3.5f, 0.7f), radius: 0.4f));
        Assert.IsTrue(grid.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(3.5f, 0.5f), radius: 0.4f));
    }

    [TestMethod]
    public void A_route_over_open_ground_is_one_straight_leg_to_the_exact_point_asked_for()
    {
        var grid = new NavGrid(10, 10, cellSize: 16);
        var finder = new PathFinder();
        var waypoints = new List<Vector2> { new(1, 1) };
        var to = new Vector2(150, 100);
        Assert.AreEqual(PathStatus.Found, grid.FindPath(finder, new Vector2(8, 8), to, waypoints));
        CollectionAssert.AreEqual(new[] { to }, waypoints, "the list is replaced, and the start is not in it");
        Assert.AreEqual(PathStatus.Found, grid.FindPath(finder, to, to, waypoints));
        CollectionAssert.AreEqual(new[] { to }, waypoints);
    }

    [TestMethod]
    public void A_route_turns_only_where_a_wall_makes_it()
    {
        var grid = Maps.Grid("     ", " ### ", "     ");
        var finder = new PathFinder();
        var waypoints = new List<Vector2>();
        Assert.AreEqual(PathStatus.Found, grid.FindPath(finder, new Vector2(1.5f, 2.5f), new Vector2(4.3f, 0.2f), waypoints));
        CollectionAssert.AreEqual(new[] { new Vector2(4.5f, 2.5f), new Vector2(4.3f, 0.2f) }, waypoints);
        Assert.HasCount(6, finder.Path, "the cells under the route stay available");
    }

    [TestMethod]
    public void Every_leg_of_a_route_is_clear_for_the_size_it_was_asked_for()
    {
        var finder = new PathFinder();
        var waypoints = new List<Vector2>();
        int legs = 0, shorter = 0;
        for (int seed = 0; seed < 40; seed++)
        {
            var grid = Maps.Random(seed, 20, 16, 0.2, costs: false);
            var random = new Random(seed);
            int a = random.Next(grid.NodeCount), b = random.Next(grid.NodeCount);
            if (!grid.IsWalkable(grid.CellOf(a).X, grid.CellOf(a).Y)) continue;
            if (grid.FindPath(finder, grid.CenterOf(a), grid.CenterOf(b), waypoints, radius: 0.45f) != PathStatus.Found) continue;
            Assert.AreEqual(grid.CenterOf(b), waypoints[^1]);
            var at = grid.CenterOf(a);
            foreach (var point in waypoints)
            {
                Assert.IsTrue(grid.HasLineOfSight(at, point, 0.45f), $"seed {seed}: {at} to {point}");
                at = point;
                legs++;
            }
            if (waypoints.Count < finder.Path.Count - 1) shorter++;
        }
        Assert.IsGreaterThan(30, legs);
        Assert.IsGreaterThan(10, shorter, "routes have fewer legs than cells");
    }

    [TestMethod]
    public void A_route_does_not_cut_across_costly_ground_the_search_went_around()
    {
        var grid = Maps.Grid(" 9 ", "   ");
        var finder = new PathFinder();
        var waypoints = new List<Vector2>();
        grid.FindPath(finder, new Vector2(0.5f, 0.5f), new Vector2(2.5f, 0.5f), waypoints);
        CollectionAssert.AreEqual(new[] { new Vector2(1.5f, 1.5f), new Vector2(2.5f, 0.5f) }, waypoints);
    }

    [TestMethod]
    public void A_point_that_cannot_be_reached_gives_the_route_to_the_nearest_cell_that_can()
    {
        var grid = Maps.Grid("  # ", "  # ");
        var finder = new PathFinder();
        var waypoints = new List<Vector2>();
        Assert.AreEqual(PathStatus.Unreachable, grid.FindPath(finder, new Vector2(0.5f, 0.5f), new Vector2(3.5f, 0.5f), waypoints));
        CollectionAssert.AreEqual(new[] { new Vector2(1.5f, 0.5f) }, waypoints);
        Assert.AreEqual(PathStatus.Unreachable, grid.FindPath(finder, new Vector2(0.5f, 0.5f), new Vector2(1.5f, 9), waypoints), "a point outside the grid");
        CollectionAssert.AreEqual(new[] { new Vector2(1.5f, 1.5f) }, waypoints, "the cell of the grid nearest to it");
        Assert.AreEqual(PathStatus.Unreachable, grid.FindPath(finder, new Vector2(-3, 0.5f), new Vector2(1.5f, 0.5f), waypoints), "an agent outside the grid");
        Assert.IsEmpty(waypoints);
    }

    [TestMethod]
    public void A_path_stops_being_open_when_a_cell_closes_or_a_corner_it_passes_does()
    {
        var grid = new NavGrid(3, 3);
        int[] path = [grid.NodeOf(0, 0), grid.NodeOf(1, 1), grid.NodeOf(2, 2)];
        Assert.IsTrue(grid.IsOpen(path));
        grid.SetWalkable(1, 0, false);
        Assert.IsFalse(grid.IsOpen(path), "the first diagonal step would now cut a corner");
        grid.Diagonals = DiagonalMovement.Always;
        Assert.IsTrue(grid.IsOpen(path));
        Assert.IsTrue(grid.IsOpen(path, from: 1));
        grid.SetWalkable(2, 2, false);
        Assert.IsFalse(grid.IsOpen(path, from: 1));
        Assert.IsTrue(grid.IsOpen([]));
    }
}
