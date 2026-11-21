using System.Numerics;

namespace MicroEngine.Navigation.Tests;

/// <summary>Agents wider than a cell: the search itself keeps them out of gaps they do not fit through.</summary>
[TestClass]
public sealed class ClearanceTests
{
    private static readonly string[] Halls =
    [
        "###########",
        "#S..#.....#",
        "#...#.....#",
        "#.........#",
        "#...#.....#",
        "#...#.....#",
        "#...#....G#",
        "#.........#",
        "#.........#",
        "#.........#",
        "###########",
    ];

    /// <summary>The same map with every cell an agent of <paramref name="rings"/> extra cells cannot stand on blocked.</summary>
    private static NavGrid Dilated(NavGrid grid, int rings)
    {
        var dilated = new NavGrid(grid.Width, grid.Height);
        for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                bool fits = true;
                for (int dy = -rings; dy <= rings && fits; dy++)
                    for (int dx = -rings; dx <= rings && fits; dx++)
                        fits = grid.IsWalkable(x + dx, y + dy);
                dilated.SetWalkable(x, y, fits);
                if (grid.IsWalkable(x, y)) dilated.SetCost(x, y, grid.CostAt(x, y));
            }
        return dilated;
    }

    [TestMethod]
    public void Clearance_counts_the_free_cells_around_a_cell_and_follows_the_map()
    {
        var grid = Maps.Grid(Halls);
        Assert.AreEqual(0, grid.ClearanceAt(0, 0), "a wall");
        Assert.AreEqual(0, grid.ClearanceAt(-3, 2), "outside the grid");
        Assert.AreEqual(1, grid.ClearanceAt(1, 1), "beside a wall");
        Assert.AreEqual(2, grid.ClearanceAt(2, 2));
        Assert.AreEqual(1, grid.ClearanceAt(4, 3), "the gap in the wall is one cell wide");
        Assert.AreEqual(3, grid.ClearanceAt(7, 3));
        Assert.AreEqual(3, grid.ClearanceAt(7, 5), "the room is five cells wide: its middle has three free on each side");

        Assert.IsTrue(grid.Fits(2, 2, 1.5f), "three cells across fit where two cells are free on every side");
        Assert.IsFalse(grid.Fits(1, 1, 1.5f));
        Assert.IsTrue(grid.Fits(1, 1, 0.5f), "an agent as wide as a cell fits wherever a cell is walkable");
        Assert.IsFalse(grid.Fits(0, 0, 0));

        grid.SetWalkable(7, 4, false);
        Assert.AreEqual(0, grid.ClearanceAt(7, 4));
        Assert.AreEqual(1, grid.ClearanceAt(7, 3), "the map changed: so did the room around the cell");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => grid.Fits(1, 1, -1));
    }

    [TestMethod]
    public void A_wide_agent_goes_around_the_gap_a_narrow_one_walks_through()
    {
        var grid = Maps.Grid(Halls);
        var finder = new PathFinder();
        int start = grid.NodeOf(2, 2), goal = grid.NodeOf(8, 5), gap = grid.NodeOf(4, 3);

        Assert.AreEqual(PathStatus.Found, finder.Find(grid, start, goal));
        CollectionAssert.Contains(finder.Path.ToArray(), gap, "a cell-sized agent takes the gap");
        float narrow = finder.Cost;

        Assert.AreEqual(PathStatus.Found, finder.Find(grid.ForAgent(1.5f), start, goal));
        CollectionAssert.DoesNotContain(finder.Path.ToArray(), gap, "three cells across do not fit");
        Assert.IsGreaterThan(narrow, finder.Cost);
        foreach (int node in finder.Path)
        {
            var cell = grid.CellOf(node);
            Assert.IsTrue(grid.Fits(cell.X, cell.Y, 1.5f), $"cell {cell} is too tight");
        }
        Assert.AreSame(grid, grid.ForAgent(0.5f), "an agent no wider than a cell walks the grid itself");
        Assert.AreSame(grid.ForAgent(1.5f), grid.ForAgent(1.2f), "sizes that need the same room share one view");
    }

    [TestMethod]
    public void Sized_paths_are_the_cheapest_on_random_maps()
    {
        var finder = new PathFinder();
        int checkedPairs = 0;
        for (int seed = 0; seed < 40; seed++)
        {
            var grid = Maps.Random(seed, 24, 18, 0.03, costs: seed % 2 == 0);
            int rings = 1 + seed % 2;
            var view = grid.ForAgent(rings + 0.5f);
            var reference = Dilated(grid, rings);
            var random = new Random(seed);
            for (int i = 0; i < 40; i++)
            {
                int start = random.Next(grid.NodeCount), goal = random.Next(grid.NodeCount);
                // From a cell too tight for the body the rules differ (it may leave across a corner): see the field's test.
                if (!grid.Fits(grid.CellOf(start).X, grid.CellOf(start).Y, rings + 0.5f)) continue;
                checkedPairs++;
                float expected = Maps.Reference(reference, start, goal);
                var status = finder.Find(view, start, goal);
                if (float.IsPositiveInfinity(expected)) Assert.AreEqual(PathStatus.Unreachable, status, $"seed {seed}, {start} to {goal}");
                else
                {
                    Assert.AreEqual(PathStatus.Found, status, $"seed {seed}, {start} to {goal}");
                    Assert.AreEqual(expected, finder.Cost, 1e-3f, $"seed {seed}, {start} to {goal}");
                    Assert.AreEqual(expected, Maps.CostOf(reference, finder.Path), 1e-3f);
                }
            }
        }
        Assert.IsGreaterThan(100, checkedPairs);
    }

    [TestMethod]
    public void A_route_for_a_wide_agent_keeps_its_whole_body_clear()
    {
        var grid = new NavGrid(Halls[0].Length, Halls.Length, cellSize: 16);
        for (int y = 0; y < Halls.Length; y++)
            for (int x = 0; x < Halls[y].Length; x++)
                if (Halls[y][x] == '#') grid.SetWalkable(x, y, false);
        var finder = new PathFinder();
        var route = new List<Vector2>();
        const float Radius = 20;
        var from = grid.CenterOf(grid.NodeOf(2, 2));
        // A point of a cell the body fits in, but so far to its right that the body would touch the wall.
        var to = new Vector2(8 * 16 + 15, 5 * 16 + 10);

        Assert.AreEqual(PathStatus.Found, grid.FindPath(finder, from, to, route, Radius));
        CollectionAssert.DoesNotContain(finder.Path.ToArray(), grid.NodeOf(4, 3));
        var at = from;
        foreach (var point in route)
        {
            Assert.IsTrue(grid.HasLineOfSight(at, point, Radius), $"the leg from {at} to {point} touches a wall");
            at = point;
        }
        Assert.AreEqual(grid.NodeOf(8, 5), grid.NodeAt(route[^1]), "the route ends in the cell asked for");
        Assert.AreEqual(to.Y, route[^1].Y, "as near the point as the body fits");
        Assert.AreNotEqual(to.X, route[^1].X);

        // Nothing wider than the room's gap reaches the left room's far corner cell, which is beside two walls.
        Assert.AreEqual(PathStatus.Unreachable, grid.FindPath(finder, grid.CenterOf(grid.NodeOf(7, 5)), grid.CenterOf(grid.NodeOf(1, 1)), route, Radius));
        Assert.IsGreaterThan(0, route.Count, "it goes as near as it fits");
        Assert.IsTrue(grid.Fits(grid.CellAt(route[^1]).X, grid.CellAt(route[^1]).Y, Radius));
    }

    [TestMethod]
    public void A_path_closes_for_a_wide_agent_when_the_room_beside_it_closes()
    {
        var grid = Maps.Grid(Halls);
        var finder = new PathFinder();
        Assert.AreEqual(PathStatus.Found, finder.Find(grid.ForAgent(1.5f), grid.NodeOf(2, 8), grid.NodeOf(8, 8)));
        var path = finder.Path.ToArray();
        CollectionAssert.Contains(path, grid.NodeOf(4, 8), "under the wall, where three rows are free");
        Assert.IsTrue(grid.IsOpen(path, radius: 1.5f));

        grid.SetWalkable(4, 9, false);
        Assert.IsTrue(grid.IsOpen(path), "no cell of the path closed");
        Assert.IsFalse(grid.IsOpen(path, radius: 1.5f), "but the body no longer fits beside the new wall");
        Assert.AreEqual(PathStatus.Unreachable, finder.Find(grid.ForAgent(1.5f), grid.NodeOf(2, 8), grid.NodeOf(8, 8)));
    }

    [TestMethod]
    public void A_field_for_wide_agents_leads_only_through_room_they_fit_in()
    {
        var grid = Maps.Grid(Halls);
        var field = new DistanceField(grid, radius: 1.5f);
        var narrow = new DistanceField(grid);
        int goal = grid.NodeOf(8, 5);
        field.Compute(goal);
        narrow.Compute(goal);
        Assert.AreEqual(1.5f, field.Radius);

        Assert.IsTrue(float.IsPositiveInfinity(field.DistanceAt(grid.NodeOf(4, 3))), "the gap is known to nobody that wide");
        Assert.IsFalse(float.IsPositiveInfinity(narrow.DistanceAt(grid.NodeOf(4, 3))));
        Assert.IsGreaterThan(narrow.DistanceAt(grid.NodeOf(2, 2)), field.DistanceAt(grid.NodeOf(2, 2)), "the wide way is longer");

        // Walking the field from the left room arrives without ever stepping where the body does not fit.
        int node = grid.NodeOf(2, 2);
        for (int steps = 0; node != goal; steps++)
        {
            Assert.IsLessThan(60, steps, "the field leads to the goal");
            node = field.Next(node);
            var cell = grid.CellOf(node);
            Assert.IsTrue(grid.Fits(cell.X, cell.Y, 1.5f), $"cell {cell} is too tight");
        }

        // A body pressed into a corner has its centre in a cell too tight for it: the field still leads out.
        Assert.AreEqual(1, grid.ClearanceAt(1, 1));
        Assert.IsTrue(float.IsPositiveInfinity(field.DistanceAt(grid.NodeOf(1, 1))));
        Assert.AreEqual(grid.NodeOf(2, 2), field.Next(grid.NodeOf(1, 1)));
        Assert.AreEqual(-1, field.Next(grid.NodeOf(0, 0)), "but not out of a wall");
    }
}
