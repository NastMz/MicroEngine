namespace MicroEngine.Navigation.Tests;

/// <summary>Grids written as text, and an exhaustive shortest-path search the finder's answers are compared with.</summary>
internal static class Maps
{
    /// <summary>A grid of one-unit cells from text rows: '#' is a wall, a digit from 2 to 9 is that cell's cost, anything else is open.</summary>
    public static NavGrid Grid(params string[] rows)
    {
        var grid = new NavGrid(rows[0].Length, rows.Length);
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
            {
                char c = rows[y][x];
                if (c == '#') grid.SetWalkable(x, y, false);
                else if (c is >= '2' and <= '9') grid.SetCost(x, y, c - '0');
            }
        return grid;
    }

    /// <summary>The node of the cell where <paramref name="marker"/> is written.</summary>
    public static int Node(NavGrid grid, string[] rows, char marker)
    {
        for (int y = 0; y < rows.Length; y++)
        {
            int x = rows[y].IndexOf(marker);
            if (x >= 0) return grid.NodeOf(x, y);
        }
        throw new ArgumentException($"No '{marker}' in the map.");
    }

    /// <summary>A grid with walls on about <paramref name="walls"/> of its cells and, when asked, costs from 1 to 4 on the others.</summary>
    public static NavGrid Random(int seed, int width, int height, double walls, bool costs)
    {
        var random = new Random(seed);
        var grid = new NavGrid(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (random.NextDouble() < walls) grid.SetWalkable(x, y, false);
                else if (costs && random.Next(3) == 0) grid.SetCost(x, y, 1 + random.Next(4));
            }
        return grid;
    }

    /// <summary>The cost of the cheapest route by Dijkstra's algorithm over arrays (no heuristic, no heap), or infinity.</summary>
    public static float Reference(INavGraph graph, int start, int goal)
    {
        int n = graph.NodeCount;
        var distance = new float[n];
        var done = new bool[n];
        Array.Fill(distance, float.PositiveInfinity);
        distance[start] = 0;
        var edges = new List<NavEdge>();
        while (true)
        {
            int best = -1;
            for (int i = 0; i < n; i++)
                if (!done[i] && distance[i] < float.PositiveInfinity && (best < 0 || distance[i] < distance[best])) best = i;
            if (best < 0) return float.PositiveInfinity;
            if (best == goal) return distance[best];
            done[best] = true;
            edges.Clear();
            graph.GetEdges(best, edges);
            foreach (var edge in edges)
                if (distance[best] + edge.Cost < distance[edge.To]) distance[edge.To] = distance[best] + edge.Cost;
        }
    }

    /// <summary>The sum of the costs of a path's steps; fails when a step is not an edge of the graph.</summary>
    public static float CostOf(INavGraph graph, IReadOnlyList<int> path)
    {
        float cost = 0;
        var edges = new List<NavEdge>();
        for (int i = 1; i < path.Count; i++)
        {
            edges.Clear();
            graph.GetEdges(path[i - 1], edges);
            int at = edges.FindIndex(e => e.To == path[i]);
            Assert.IsGreaterThanOrEqualTo(0, at, $"step {i} ({path[i - 1]} to {path[i]}) is not an edge");
            cost += edges[at].Cost;
        }
        return cost;
    }
}
