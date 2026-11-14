namespace MicroEngine.Navigation.Tests;

[TestClass]
public sealed class PathFinderTests
{
    private const float Diagonal = 1.41421356f;

    [TestMethod]
    public void An_open_grid_is_crossed_at_the_octile_cost_from_start_to_goal_inclusive()
    {
        var grid = Maps.Grid("     ", "     ", "     ");
        var finder = new PathFinder();
        Assert.AreEqual(PathStatus.Found, finder.Find(grid, grid.NodeOf(0, 0), grid.NodeOf(4, 2)));
        Assert.AreEqual(2 * Diagonal + 2, finder.Cost, 1e-4f);
        Assert.HasCount(5, finder.Path);
        Assert.AreEqual(grid.NodeOf(0, 0), finder.Path[0]);
        Assert.AreEqual(grid.NodeOf(4, 2), finder.Path[^1]);
        Assert.AreEqual(finder.Cost, Maps.CostOf(grid, finder.Path), 1e-4f);
    }

    [TestMethod]
    [DataRow(DiagonalMovement.Never, 6f)]
    [DataRow(DiagonalMovement.NoCornerCutting, 6f)]
    [DataRow(DiagonalMovement.Always, 2 + 2 * Diagonal)]
    public void A_wall_is_walked_around_and_only_corner_cutting_squeezes_past_its_end(DiagonalMovement diagonals, float cost)
    {
        string[] rows = ["S#G", " # ", "   "];
        var grid = Maps.Grid(rows);
        grid.Diagonals = diagonals;
        var finder = new PathFinder();
        Assert.AreEqual(PathStatus.Found, finder.Find(grid, Maps.Node(grid, rows, 'S'), Maps.Node(grid, rows, 'G')));
        Assert.AreEqual(cost, finder.Cost, 1e-4f);
        foreach (int node in finder.Path) Assert.IsTrue(grid.IsWalkable(grid.CellOf(node).X, grid.CellOf(node).Y));
    }

    [TestMethod]
    public void Four_way_movement_never_steps_diagonally()
    {
        var grid = Maps.Grid("    ", "    ", "    ");
        grid.Diagonals = DiagonalMovement.Never;
        var finder = new PathFinder();
        finder.Find(grid, grid.NodeOf(0, 0), grid.NodeOf(3, 2));
        Assert.AreEqual(5, finder.Cost, 1e-4f);
        for (int i = 1; i < finder.Path.Count; i++)
        {
            var (a, b) = (grid.CellOf(finder.Path[i - 1]), grid.CellOf(finder.Path[i]));
            Assert.AreEqual(1, Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y));
        }
    }

    [TestMethod]
    public void Paths_are_the_cheapest_ones_on_random_maps_with_walls_and_costs()
    {
        var finder = new PathFinder();
        int found = 0, unreachable = 0;
        for (int seed = 0; seed < 60; seed++)
            foreach (var diagonals in Enum.GetValues<DiagonalMovement>())
            {
                var grid = Maps.Random(seed, 16, 12, 0.3, costs: seed % 2 == 0);
                grid.Diagonals = diagonals;
                var random = new Random(seed * 31 + (int)diagonals);
                int start = random.Next(grid.NodeCount), goal = random.Next(grid.NodeCount);
                float expected = Maps.Reference(grid, start, goal);
                var goalCell = grid.CellOf(goal);
                // Entering the goal is an edge, so a walled goal has no route even when the exhaustive search starts next to it.
                bool reachable = float.IsFinite(expected) && (start == goal || grid.IsWalkable(goalCell.X, goalCell.Y));
                var status = finder.Find(grid, start, goal);
                Assert.AreEqual(reachable ? PathStatus.Found : PathStatus.Unreachable, status, $"seed {seed}, {diagonals}");
                Assert.AreEqual(start, finder.Path[0]);
                Assert.AreEqual(finder.Cost, Maps.CostOf(grid, finder.Path), 1e-3f, "the cost is that of the path's steps");
                if (!reachable) { unreachable++; continue; }
                found++;
                Assert.AreEqual(goal, finder.Path[^1]);
                Assert.AreEqual(expected, finder.Cost, 1e-3f, $"seed {seed}, {diagonals}");
            }
        Assert.IsGreaterThan(40, found);
        Assert.IsGreaterThan(20, unreachable);
    }

    [TestMethod]
    public void Costly_cells_are_avoided_when_going_around_is_cheaper_and_crossed_when_it_is_not()
    {
        string[] rows = ["S222G", "     "];
        var grid = Maps.Grid(rows);
        var finder = new PathFinder();
        finder.Find(grid, Maps.Node(grid, rows, 'S'), Maps.Node(grid, rows, 'G'));
        Assert.AreEqual(2 + 2 * Diagonal, finder.Cost, 1e-4f, "around, through the second row");
        foreach (int node in finder.Path) Assert.AreEqual(1f, grid.CostAt(grid.CellOf(node).X, grid.CellOf(node).Y));

        string[] walled = ["S222G", "#####"];
        grid = Maps.Grid(walled);
        finder.Find(grid, Maps.Node(grid, walled, 'S'), Maps.Node(grid, walled, 'G'));
        Assert.AreEqual(7, finder.Cost, 1e-4f, "three cells that cost 2 to enter, then the goal");
    }

    [TestMethod]
    public void An_unreachable_goal_reports_it_and_leads_to_the_closest_cell_that_can_be_reached()
    {
        string[] rows = ["S #G", "  # "];
        var grid = Maps.Grid(rows);
        var finder = new PathFinder();
        Assert.AreEqual(PathStatus.Unreachable, finder.Find(grid, Maps.Node(grid, rows, 'S'), Maps.Node(grid, rows, 'G')));
        CollectionAssert.AreEqual(new[] { grid.NodeOf(0, 0), grid.NodeOf(1, 0) }, finder.Path.ToArray());
        Assert.AreEqual(1, finder.Cost, 1e-4f);
        Assert.AreEqual(4, finder.Expanded, "every cell on the start's side was looked at");
    }

    [TestMethod]
    public void The_start_is_its_own_path_and_a_walled_start_may_leave_but_a_walled_goal_is_never_entered()
    {
        var grid = Maps.Grid("   ", " # ", "   ");
        var finder = new PathFinder();
        Assert.AreEqual(PathStatus.Found, finder.Find(grid, 4, 4));
        CollectionAssert.AreEqual(new[] { 4 }, finder.Path.ToArray());
        Assert.AreEqual(0f, finder.Cost);

        Assert.AreEqual(PathStatus.Found, finder.Find(grid, grid.NodeOf(1, 1), grid.NodeOf(2, 2)), "an agent caught inside a wall walks out");
        Assert.AreEqual(Diagonal, finder.Cost, 1e-4f);
        Assert.AreEqual(PathStatus.Unreachable, finder.Find(grid, grid.NodeOf(0, 0), grid.NodeOf(1, 1)));
        Assert.AreNotEqual(grid.NodeOf(1, 1), finder.Path[^1]);
    }

    [TestMethod]
    public void A_search_budget_stops_the_search_and_keeps_the_best_route_so_far()
    {
        var grid = new NavGrid(64, 64);
        for (int y = 0; y < 63; y++) grid.SetWalkable(32, y, false);   // a wall with its gap at the far end
        var finder = new PathFinder { MaxExpansions = 50 };
        int start = grid.NodeOf(0, 0), goal = grid.NodeOf(63, 0);
        Assert.AreEqual(PathStatus.Incomplete, finder.Find(grid, start, goal));
        Assert.AreEqual(50, finder.Expanded);
        Assert.AreEqual(start, finder.Path[0]);
        Assert.IsLessThan(grid.Estimate(start, goal), grid.Estimate(finder.Path[^1], goal), "the route so far ends nearer the goal");
        finder.MaxExpansions = int.MaxValue;
        Assert.AreEqual(PathStatus.Found, finder.Find(grid, start, goal));
    }

    [TestMethod]
    public void A_door_that_closes_invalidates_the_path_and_the_next_search_goes_around()
    {
        string[] rows = ["S D G", " ### ", "     "];
        var grid = Maps.Grid(rows);
        var finder = new PathFinder();
        int start = Maps.Node(grid, rows, 'S'), goal = Maps.Node(grid, rows, 'G');
        finder.Find(grid, start, goal);
        Assert.AreEqual(4, finder.Cost, 1e-4f);
        var through = finder.Path.ToArray();
        Assert.IsTrue(grid.IsOpen(through));

        int version = grid.Version;
        grid.SetWalkable(2, 0, false);
        Assert.AreNotEqual(version, grid.Version, "a change is visible without comparing cells");
        Assert.IsFalse(grid.IsOpen(through));
        Assert.IsTrue(grid.IsOpen(through, from: 3), "the part already behind the agent does not matter");
        Assert.AreEqual(PathStatus.Found, finder.Find(grid, start, goal));
        Assert.AreEqual(8, finder.Cost, 1e-4f, "down, along the bottom row and up");

        version = grid.Version;
        grid.SetWalkable(2, 0, false);
        Assert.AreEqual(version, grid.Version, "setting a cell to what it already is changes nothing");
        grid.SetWalkable(2, 0, true);
        finder.Find(grid, start, goal);
        CollectionAssert.AreEqual(through, finder.Path.ToArray());
    }

    [TestMethod]
    public void The_same_question_always_has_the_same_answer_whatever_the_finder_did_before()
    {
        var used = new PathFinder();
        for (int seed = 0; seed < 20; seed++)
        {
            var grid = Maps.Random(seed, 24, 24, 0.2, costs: false);
            var random = new Random(seed);
            int start = random.Next(grid.NodeCount), goal = random.Next(grid.NodeCount);
            var status = used.Find(grid, start, goal);
            var first = used.Path.ToArray();
            used.Find(grid, goal, start);
            Assert.AreEqual(status, used.Find(grid, start, goal));
            CollectionAssert.AreEqual(first, used.Path.ToArray(), "asked twice");
            var fresh = new PathFinder();
            Assert.AreEqual(status, fresh.Find(grid, start, goal));
            CollectionAssert.AreEqual(first, fresh.Path.ToArray(), "asked of a new finder");
        }
        // Many routes of equal cost cross an open grid; the one returned is pinned.
        var open = new NavGrid(4, 3);
        used.Find(open, open.NodeOf(0, 0), open.NodeOf(3, 1));
        CollectionAssert.AreEqual(new[] { open.NodeOf(0, 0), open.NodeOf(1, 1), open.NodeOf(2, 1), open.NodeOf(3, 1) }, used.Path.ToArray());
    }

    [TestMethod]
    public void Searching_allocates_nothing_once_the_finder_has_seen_the_graph()
    {
        var grid = Maps.Random(7, 96, 96, 0.25, costs: true);
        var finder = new PathFinder();
        var waypoints = new List<System.Numerics.Vector2>(512);
        var random = new Random(1);
        var queries = Enumerable.Range(0, 40).Select(_ => (random.Next(grid.NodeCount), random.Next(grid.NodeCount))).ToArray();
        foreach (var (a, b) in queries) { finder.Find(grid, a, b); grid.FindPath(finder, grid.CenterOf(a), grid.CenterOf(b), waypoints, 0.3f); }

        long before = GC.GetAllocatedBytesForCurrentThread();
        int found = 0;
        foreach (var (a, b) in queries)
        {
            if (finder.Find(grid, a, b) == PathStatus.Found) found++;
            grid.FindPath(finder, grid.CenterOf(a), grid.CenterOf(b), waypoints, 0.3f);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0, allocated, "bytes allocated by 80 searches");
        Assert.IsGreaterThan(5, found);
    }

    [TestMethod]
    public void Bad_questions_are_refused()
    {
        var grid = new NavGrid(3, 3);
        var finder = new PathFinder();
        Assert.ThrowsExactly<ArgumentNullException>(() => finder.Find(null!, 0, 1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => finder.Find(grid, -1, 1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => finder.Find(grid, 0, 9));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => finder.MaxExpansions = 0);
        Assert.ThrowsExactly<InvalidOperationException>(() => finder.Find(new Broken(), 0, 1), "an edge with a negative cost");
    }

    private sealed class Broken : INavGraph
    {
        public int NodeCount => 2;
        public void GetEdges(int node, List<NavEdge> edges) => edges.Add(new NavEdge(1 - node, -1));
        public float Estimate(int from, int to) => 0;
    }

    /// <summary>A game's own graph: a 3D lattice of rooms on floors, six neighbours each, some of them solid.</summary>
    private sealed class Lattice(int width, int height, int depth, HashSet<int> solid) : INavGraph
    {
        public int NodeCount => width * height * depth;
        public int Node(int x, int y, int z) => (z * height + y) * width + x;
        public void GetEdges(int node, List<NavEdge> edges)
        {
            int x = node % width, y = node / width % height, z = node / (width * height);
            Span<(int, int, int)> steps = [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)];
            foreach (var (dx, dy, dz) in steps)
            {
                int nx = x + dx, ny = y + dy, nz = z + dz;
                if (nx < 0 || ny < 0 || nz < 0 || nx >= width || ny >= height || nz >= depth) continue;
                int to = Node(nx, ny, nz);
                if (!solid.Contains(to)) edges.Add(new NavEdge(to, dz != 0 ? 3 : 1));   // stairs cost more
            }
        }
        public float Estimate(int from, int to) =>
            Math.Abs(from % width - to % width) + Math.Abs(from / width % height - to / width % height) + 3 * Math.Abs(from / (width * height) - to / (width * height));
    }

    [TestMethod]
    public void A_graph_of_the_games_own_in_three_dimensions_is_searched_the_same_way()
    {
        // Two floors of 5 × 5 rooms.
        var solid = new HashSet<int>();
        var lattice = new Lattice(5, 5, 2, solid);
        var finder = new PathFinder();
        int start = lattice.Node(0, 0, 0), goal = lattice.Node(0, 0, 1);
        Assert.AreEqual(PathStatus.Found, finder.Find(lattice, start, goal));
        Assert.AreEqual(3, finder.Cost, 1e-4f, "straight up");
        solid.Add(goal);
        Assert.AreEqual(PathStatus.Unreachable, finder.Find(lattice, start, goal), "a solid room is never entered");
        for (int seed = 0; seed < 20; seed++)
        {
            var random = new Random(seed);
            var walls = new HashSet<int>();
            var maze = new Lattice(6, 5, 4, walls);
            for (int i = 0; i < 40; i++) walls.Add(random.Next(maze.NodeCount));
            int a = random.Next(maze.NodeCount), b = random.Next(maze.NodeCount);
            float expected = Maps.Reference(maze, a, b);
            bool reachable = float.IsFinite(expected) && (a == b || !walls.Contains(b));
            Assert.AreEqual(reachable, finder.Find(maze, a, b) == PathStatus.Found, $"seed {seed}");
            if (reachable) Assert.AreEqual(expected, finder.Cost, 1e-3f, $"seed {seed}");
        }
    }

    /// <summary>A graph given as lists, with an estimate that never exceeds the true cost but jumps between neighbours.</summary>
    private sealed class Listed(List<NavEdge>[] edges, float[] estimate) : INavGraph
    {
        public int NodeCount => edges.Length;
        public void GetEdges(int node, List<NavEdge> into) => into.AddRange(edges[node]);
        public float Estimate(int from, int to) => estimate[from];
    }

    [TestMethod]
    public void An_estimate_that_never_exceeds_the_true_cost_gives_the_cheapest_path_even_when_it_is_uneven()
    {
        var finder = new PathFinder();
        for (int seed = 0; seed < 40; seed++)
        {
            var random = new Random(seed);
            const int n = 24;
            var edges = Enumerable.Range(0, n).Select(_ => new List<NavEdge>()).ToArray();
            for (int i = 0; i < 70; i++)
            {
                int a = random.Next(n), b = random.Next(n);
                if (a != b) edges[a].Add(new NavEdge(b, 1 + random.Next(9)));
            }
            int goal = random.Next(n), start = random.Next(n);
            var exact = new Listed(edges, new float[n]);
            var estimate = new float[n];
            for (int i = 0; i < n; i++)
            {
                float truth = Maps.Reference(exact, i, goal);
                estimate[i] = float.IsFinite(truth) ? truth * (float)random.NextDouble() : 0;
            }
            var graph = new Listed(edges, estimate);
            float expected = Maps.Reference(exact, start, goal);
            Assert.AreEqual(float.IsFinite(expected), finder.Find(graph, start, goal) == PathStatus.Found, $"seed {seed}");
            if (float.IsFinite(expected)) Assert.AreEqual(expected, finder.Cost, 1e-3f, $"seed {seed}");
        }
    }
}
