namespace MicroEngine.Navigation.Tests;

[TestClass]
public sealed class DistanceFieldTests
{
    [TestMethod]
    public void Every_cell_knows_the_cost_of_its_cheapest_route_to_the_goal()
    {
        int reachable = 0, cut = 0;
        for (int seed = 0; seed < 24; seed++)
            foreach (var diagonals in Enum.GetValues<DiagonalMovement>())
            {
                var grid = Maps.Random(seed, 14, 10, 0.25, costs: seed % 2 == 1);
                grid.Diagonals = diagonals;
                int goal = AnyWalkable(grid, new Random(seed));
                var field = new DistanceField(grid);
                field.Compute(goal);
                Assert.AreEqual(0f, field.DistanceAt(goal));
                for (int node = 0; node < grid.NodeCount; node++)
                {
                    var cell = grid.CellOf(node);
                    float expected = grid.IsWalkable(cell.X, cell.Y) ? Maps.Reference(grid, node, goal) : float.PositiveInfinity;
                    if (float.IsFinite(expected)) { Assert.AreEqual(expected, field.DistanceAt(node), 1e-3f, $"seed {seed}, {diagonals}, node {node}"); reachable++; }
                    else { Assert.AreEqual(float.PositiveInfinity, field.DistanceAt(node), $"seed {seed}, {diagonals}, node {node}"); cut++; }
                }
            }
        Assert.IsGreaterThan(2000, reachable);
        Assert.IsGreaterThan(500, cut);
    }

    private static int AnyWalkable(NavGrid grid, Random random)
    {
        while (true)
        {
            int node = random.Next(grid.NodeCount);
            var cell = grid.CellOf(node);
            if (grid.IsWalkable(cell.X, cell.Y)) return node;
        }
    }

    [TestMethod]
    public void Stepping_to_the_next_cell_from_anywhere_reaches_the_goal_at_the_cost_the_field_gives()
    {
        for (int seed = 0; seed < 12; seed++)
        {
            var grid = Maps.Random(seed, 18, 12, 0.2, costs: true);
            int goal = AnyWalkable(grid, new Random(seed));
            var field = new DistanceField(grid);
            field.Compute(goal);
            Assert.AreEqual(goal, field.Next(goal), "at the goal there is nowhere to go");
            for (int start = 0; start < grid.NodeCount; start++)
            {
                if (!float.IsFinite(field.DistanceAt(start))) { Assert.AreEqual(-1, field.Next(start)); continue; }
                var walked = new List<int> { start };
                for (int at = start; at != goal;)
                {
                    at = field.Next(at);
                    walked.Add(at);
                    Assert.IsLessThanOrEqualTo(grid.NodeCount, walked.Count, "the walk never circles");
                }
                Assert.AreEqual(field.DistanceAt(start), Maps.CostOf(grid, walked), 1e-3f, $"seed {seed}, from {start}");
            }
        }
    }

    [TestMethod]
    public void Several_goals_give_the_distance_to_the_nearest_and_a_limit_leaves_the_rest_unknown()
    {
        var grid = new NavGrid(9, 1) { Diagonals = DiagonalMovement.Never };
        var field = new DistanceField(grid);
        field.Compute([0, 8]);
        CollectionAssert.AreEqual(new float[] { 0, 1, 2, 3, 4, 3, 2, 1, 0 }, Enumerable.Range(0, 9).Select(field.DistanceAt).ToArray());
        Assert.AreEqual(2, field.Next(3));
        Assert.AreEqual(6, field.Next(5));

        field.Compute(0, maxDistance: 3);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, float.PositiveInfinity }, Enumerable.Range(0, 5).Select(field.DistanceAt).ToArray());
        Assert.AreEqual(-1, field.Next(6));
    }

    [TestMethod]
    public void A_field_remembers_the_version_of_the_grid_it_was_computed_for()
    {
        var grid = Maps.Grid("   ", "   ");
        var field = new DistanceField(grid);
        Assert.AreEqual(float.PositiveInfinity, field.DistanceAt(0), "nothing is known before the first computation");
        Assert.AreEqual(-1, field.Next(0));
        field.Compute(grid.NodeOf(2, 0));
        Assert.AreEqual(grid.Version, field.Version);
        grid.SetWalkable(1, 0, false);
        Assert.AreNotEqual(grid.Version, field.Version, "the game sees the field is stale and computes it again");
        field.Compute(grid.NodeOf(2, 0));
        Assert.AreEqual(grid.Version, field.Version);
        Assert.AreEqual(4, field.DistanceAt(grid.NodeOf(0, 0)), 1e-4f, "down, along the bottom row and up: no corner is cut");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => field.Compute(6));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => field.DistanceAt(-1));
    }

    [TestMethod]
    public void Computing_again_allocates_nothing()
    {
        var grid = Maps.Random(3, 80, 80, 0.2, costs: true);
        var field = new DistanceField(grid);
        int goal = AnyWalkable(grid, new Random(3));
        int other = AnyWalkable(grid, new Random(4));
        field.Compute(goal);
        field.Next(other);
        long before = GC.GetAllocatedBytesForCurrentThread();
        field.Compute(goal);
        int next = field.Next(other);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0, allocated);
        Assert.AreNotEqual(-2, next);
    }
}
