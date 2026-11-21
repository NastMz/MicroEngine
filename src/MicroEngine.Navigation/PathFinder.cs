namespace MicroEngine.Navigation;

/// <summary>
/// Finds the cheapest path between two nodes of an <see cref="INavGraph"/> with the A* algorithm. A finder keeps the
/// memory of its searches, so searching again allocates nothing once it has seen a graph of that size; keep one per
/// game (or one per thread: a finder serves one search at a time). The same question on the same graph always gives
/// the same path, on every platform.
/// </summary>
public sealed class PathFinder
{
    private float[] _cost = [], _estimate = [], _total = [];
    private int[] _parent = [], _seen = [];
    private int _search;
    private int _maxExpansions = int.MaxValue;
    private readonly NodeHeap _open = new();
    private readonly List<NavEdge> _edges = new(16);
    private readonly List<int> _path = new(64);
    // The search in progress (Begin, Continue): its graph, goal, the node nearest the goal so far and its estimate.
    private INavGraph? _graph;
    private int _goal, _end;
    private float _nearest;

    /// <summary>
    /// The most nodes a search looks at before it gives up with <see cref="PathStatus.Incomplete"/>; unlimited by
    /// default. A goal that cannot be reached makes a search look at every node the start reaches: a limit bounds
    /// that cost on large maps.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive.</exception>
    public int MaxExpansions
    {
        get => _maxExpansions;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _maxExpansions = value;
        }
    }

    /// <summary>
    /// The nodes of the last search's path, from the start to its end, both included. The next search replaces it:
    /// copy what must be kept.
    /// </summary>
    public IReadOnlyList<int> Path => _path;

    /// <summary>The cost of <see cref="Path"/>: the sum of its steps.</summary>
    public float Cost { get; private set; }

    /// <summary>How many nodes the last search looked at (a measure of its work).</summary>
    public int Expanded { get; private set; }

    /// <summary>
    /// Searches <paramref name="graph"/> for the cheapest path from <paramref name="start"/> to <paramref name="goal"/>
    /// and leaves it in <see cref="Path"/>. When the goal cannot be reached, or the search stops at
    /// <see cref="MaxExpansions"/>, the path leads as near to the goal as the search got (it may be the start alone).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="goal"/> is not a node of the graph.</exception>
    /// <exception cref="InvalidOperationException">The graph gave a step with a negative cost or to a node it does not have.</exception>
    public PathStatus Find(INavGraph graph, int start, int goal)
    {
        Begin(graph, start, goal);
        return Continue(int.MaxValue);
    }

    /// <summary>Whether a search begun with <see cref="Begin"/> has not ended yet.</summary>
    public bool IsSearching => _graph is not null;

    /// <summary>
    /// Begins a search from <paramref name="start"/> to <paramref name="goal"/> that <see cref="Continue"/> advances
    /// a few nodes at a time, so a long search on a large map is spread over several steps of the game instead of
    /// stalling one. It replaces a search in progress. The graph must not change until the search ends: begin again
    /// when it does.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="goal"/> is not a node of the graph.</exception>
    public void Begin(INavGraph graph, int start, int goal)
    {
        ArgumentNullException.ThrowIfNull(graph);
        int count = graph.NodeCount;
        if ((uint)start >= (uint)count) throw new ArgumentOutOfRangeException(nameof(start), start, $"The graph has {count} nodes.");
        if ((uint)goal >= (uint)count) throw new ArgumentOutOfRangeException(nameof(goal), goal, $"The graph has {count} nodes.");
        Prepare(count);
        _path.Clear();
        Expanded = 0;
        Cost = 0;
        _graph = graph;
        _goal = goal;
        _end = start;
        _nearest = graph.Estimate(start, goal);
        Open(start, -1, 0, _nearest);
    }

    /// <summary>
    /// Advances the search begun with <see cref="Begin"/> by at most <paramref name="expansions"/> nodes. It returns
    /// <see cref="PathStatus.Searching"/> while the answer is not known (<see cref="Path"/> is empty until then),
    /// and otherwise how the search ended, with the path in <see cref="Path"/>: the same path, cost and
    /// <see cref="Expanded"/> as <see cref="Find"/> gives, however the work was divided. <see cref="MaxExpansions"/>
    /// bounds the whole search, not one call.
    /// </summary>
    /// <exception cref="InvalidOperationException">No search is in progress.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expansions"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">The graph gave a step with a negative cost or to a node it does not have.</exception>
    public PathStatus Continue(int expansions)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expansions);
        var graph = _graph ?? throw new InvalidOperationException("No search is in progress: call Begin first.");
        int count = graph.NodeCount, goal = _goal, end = _end, spent = 0;
        float nearest = _nearest;
        var status = PathStatus.Unreachable;
        while (_open.Count > 0)
        {
            if (spent == expansions)
            {
                _end = end;
                _nearest = nearest;
                return PathStatus.Searching;
            }
            int node = _open.Pop();
            if (node == goal) { status = PathStatus.Found; end = goal; break; }
            if (Expanded >= _maxExpansions) { status = PathStatus.Incomplete; break; }
            Expanded++;
            spent++;
            _edges.Clear();
            graph.GetEdges(node, _edges);
            float cost = _cost[node];
            for (int i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];
                if (!(edge.Cost >= 0)) throw new InvalidOperationException($"The step from node {node} to node {edge.To} has the cost {edge.Cost}; costs are zero or more.");
                if ((uint)edge.To >= (uint)count) throw new InvalidOperationException($"Node {node} has a step to node {edge.To}; the graph has {count} nodes.");
                float reached = cost + edge.Cost;
                if (_seen[edge.To] != _search)
                {
                    float estimate = graph.Estimate(edge.To, goal);
                    Open(edge.To, node, reached, estimate);
                    if (estimate < nearest) { nearest = estimate; end = edge.To; }
                }
                else if (reached < _cost[edge.To])
                {
                    // A cheaper way to a node already seen: it moves up the queue, or returns to it when an uneven
                    // estimate had let it out too early.
                    _cost[edge.To] = reached;
                    _total[edge.To] = reached + _estimate[edge.To];
                    _parent[edge.To] = node;
                    if (_open.Contains(edge.To)) _open.Raise(edge.To);
                    else _open.Push(edge.To);
                }
            }
        }

        _graph = null;
        for (int node = end; node >= 0; node = _parent[node]) _path.Add(node);
        _path.Reverse();
        Cost = _cost[end];
        return status;
    }

    private void Open(int node, int parent, float cost, float estimate)
    {
        _seen[node] = _search;
        _cost[node] = cost;
        _estimate[node] = estimate;
        _total[node] = cost + estimate;
        _parent[node] = parent;
        _open.Push(node);
    }

    private void Prepare(int count)
    {
        if (_seen.Length < count)
        {
            _cost = new float[count];
            _estimate = new float[count];
            _total = new float[count];
            _parent = new int[count];
            _seen = new int[count];
            _search = 0;
        }
        if (++_search == int.MaxValue)
        {
            Array.Clear(_seen);
            _search = 1;
        }
        // Lowest total first; between equal totals the node further along (the larger cost so far), then the lower
        // number: a total order, so the queue's shape never decides a path.
        _open.Reset(_total, _cost, count);
    }
}

/// <summary>
/// A binary heap of node numbers ordered by a key array the owner fills: the lowest <c>primary</c> first, then the
/// highest <c>secondary</c>, then the lowest node. It knows where each node sits, so a node whose key dropped moves up
/// without a second entry.
/// </summary>
internal sealed class NodeHeap
{
    private int[] _nodes = new int[64];
    private int[] _slot = [];
    private float[] _primary = [], _secondary = [];
    private int _count;

    public int Count => _count;

    /// <summary>Empties the heap and binds the keys of a graph of <paramref name="nodes"/> nodes.</summary>
    public void Reset(float[] primary, float[] secondary, int nodes)
    {
        // Slots of a previous search are stale only for nodes still queued; clear those.
        for (int i = 0; i < _count; i++) _slot[_nodes[i]] = 0;
        _count = 0;
        if (_slot.Length < nodes) _slot = new int[nodes];
        _primary = primary;
        _secondary = secondary;
    }

    /// <summary>Whether <paramref name="node"/> is queued.</summary>
    public bool Contains(int node) => _slot[node] != 0;

    public void Push(int node)
    {
        if (_count == _nodes.Length) Array.Resize(ref _nodes, _count * 2);
        _nodes[_count] = node;
        _slot[node] = _count + 1;
        Up(_count++);
    }

    /// <summary>Moves a queued node whose key dropped.</summary>
    public void Raise(int node) => Up(_slot[node] - 1);

    public int Pop()
    {
        int top = _nodes[0];
        _slot[top] = 0;
        if (--_count > 0)
        {
            _nodes[0] = _nodes[_count];
            _slot[_nodes[0]] = 1;
            Down(0);
        }
        return top;
    }

    private bool Before(int a, int b)
    {
        float pa = _primary[a], pb = _primary[b];
        if (pa != pb) return pa < pb;
        float sa = _secondary[a], sb = _secondary[b];
        if (sa != sb) return sa > sb;
        return a < b;
    }

    private void Up(int at)
    {
        int node = _nodes[at];
        while (at > 0)
        {
            int parent = (at - 1) / 2;
            if (!Before(node, _nodes[parent])) break;
            _nodes[at] = _nodes[parent];
            _slot[_nodes[at]] = at + 1;
            at = parent;
        }
        _nodes[at] = node;
        _slot[node] = at + 1;
    }

    private void Down(int at)
    {
        int node = _nodes[at];
        while (true)
        {
            int child = at * 2 + 1;
            if (child >= _count) break;
            if (child + 1 < _count && Before(_nodes[child + 1], _nodes[child])) child++;
            if (!Before(_nodes[child], node)) break;
            _nodes[at] = _nodes[child];
            _slot[_nodes[at]] = at + 1;
            at = child;
        }
        _nodes[at] = node;
        _slot[node] = at + 1;
    }
}
