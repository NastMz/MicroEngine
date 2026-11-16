namespace MicroEngine.Navigation;

/// <summary>
/// The cost of the cheapest route from every cell of a <see cref="NavGrid"/> to a goal, computed once for all of
/// them: what many agents with the same goal read instead of each searching its own path (a horde after the player,
/// units sent to one point, creatures fleeing to the nearest exit). Each agent asks for the cell to step to
/// (<see cref="Next"/>); the game computes the field again when the goal moves to another cell or the grid changes
/// (<see cref="Version"/>).
/// </summary>
public sealed class DistanceField
{
    private readonly float[] _distance;
    private readonly NodeHeap _queue = new();
    private readonly List<NavEdge> _edges = new(8);

    /// <summary>A field over <paramref name="grid"/>; nothing is known until <see cref="Compute(int, float)"/>.</summary>
    public DistanceField(NavGrid grid)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _distance = new float[grid.NodeCount];
        Array.Fill(_distance, float.PositiveInfinity);
    }

    /// <summary>The grid the field measures.</summary>
    public NavGrid Grid { get; }

    /// <summary>
    /// The grid's <see cref="NavGrid.Version"/> when the field was last computed (-1 before the first time): when it
    /// differs from the grid's, the field describes a level that changed.
    /// </summary>
    public int Version { get; private set; } = -1;

    /// <summary>Computes the cost from every cell to the node <paramref name="goal"/>.</summary>
    /// <param name="goal">The goal's node (<see cref="NavGrid.NodeAt"/>). A blocked goal is reached from nowhere.</param>
    /// <param name="maxDistance">Cells further than this cost stay unknown; a limit keeps the work near the goal on large maps.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="goal"/> is not a node of the grid, or <paramref name="maxDistance"/> is negative or not a number.</exception>
    public void Compute(int goal, float maxDistance = float.PositiveInfinity) => Compute(new ReadOnlySpan<int>(in goal), maxDistance);

    /// <summary>Computes the cost from every cell to the nearest of several <paramref name="goals"/> (the exits of a room, every pickup).</summary>
    /// <exception cref="ArgumentOutOfRangeException">A goal is not a node of the grid, or <paramref name="maxDistance"/> is negative or not a number.</exception>
    public void Compute(ReadOnlySpan<int> goals, float maxDistance = float.PositiveInfinity)
    {
        if (!(maxDistance >= 0)) throw new ArgumentOutOfRangeException(nameof(maxDistance), maxDistance, "The limit is zero or more.");
        foreach (int goal in goals)
            if ((uint)goal >= (uint)_distance.Length) throw new ArgumentOutOfRangeException(nameof(goals), goal, $"The grid has {_distance.Length} nodes.");
        Array.Fill(_distance, float.PositiveInfinity);
        _queue.Reset(_distance, _distance, _distance.Length);
        foreach (int goal in goals)
        {
            if (_distance[goal] == 0) continue;
            _distance[goal] = 0;
            _queue.Push(goal);
        }
        // Outwards from the goals. A neighbour reaches the cell just taken by the step back to it, which costs the
        // step's length times this cell's cost; the grid's steps are the same in both directions.
        while (_queue.Count > 0)
        {
            int node = _queue.Pop();
            if (Grid.IsBlocked(node)) continue;
            float here = _distance[node], enter = Grid.CostOf(node);
            _edges.Clear();
            Grid.GetEdges(node, _edges);
            for (int i = 0; i < _edges.Count; i++)
            {
                int neighbour = _edges[i].To;
                float cost = here + Grid.StepLength(node, neighbour) * enter;
                if (cost > maxDistance || cost >= _distance[neighbour]) continue;
                bool queued = _queue.Contains(neighbour);
                _distance[neighbour] = cost;
                if (queued) _queue.Raise(neighbour);
                else _queue.Push(neighbour);
            }
        }
        Version = Grid.Version;
    }

    /// <summary>The cost of the cheapest route from <paramref name="node"/> to the goal: 0 at a goal, infinity where no route exists or nothing was computed.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the grid.</exception>
    public float DistanceAt(int node)
    {
        if ((uint)node >= (uint)_distance.Length) throw new ArgumentOutOfRangeException(nameof(node), node, $"The grid has {_distance.Length} nodes.");
        return _distance[node];
    }

    /// <summary>
    /// The neighbouring node an agent at <paramref name="node"/> steps to on its cheapest route: the node itself at a
    /// goal, -1 where no route exists. Walk towards its centre (<see cref="NavGrid.CenterOf"/>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the grid.</exception>
    public int Next(int node)
    {
        float here = DistanceAt(node);
        if (float.IsPositiveInfinity(here)) return -1;
        if (here == 0) return node;
        _edges.Clear();
        Grid.GetEdges(node, _edges);
        int next = -1;
        float best = float.PositiveInfinity;
        for (int i = 0; i < _edges.Count; i++)
        {
            float cost = _edges[i].Cost + _distance[_edges[i].To];
            if (cost < best) { best = cost; next = _edges[i].To; }
        }
        return next;
    }
}
