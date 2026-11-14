using System.Numerics;

namespace MicroEngine.Navigation;

/// <summary>
/// Walkable space as points joined by hand: waypoints along roads, rooms joined by doors, platforms joined by jumps.
/// Each node has a position; each connection has a cost (the distance between its ends unless told otherwise), a
/// direction (both ways, or one way for a drop, a jump or a slide) and a tag the game reads to know how to cross it.
/// A <see cref="PathFinder"/> searches it like any <see cref="INavGraph"/>.
/// </summary>
public sealed class NavGraph : INavGraph
{
    private readonly List<Vector2> _positions = [];
    private readonly List<List<NavEdge>> _edges = [];
    private readonly List<bool> _disabled = [];
    // The least cost per unit of distance of any connection made: distance times it never exceeds a route's cost.
    private float _costPerDistance = float.PositiveInfinity;

    /// <inheritdoc/>
    public int NodeCount => _positions.Count;

    /// <summary>A number that changes whenever a node or a connection is added, removed, enabled or disabled.</summary>
    public int Version { get; private set; }

    /// <summary>Adds a node at <paramref name="position"/> and returns its number (0 for the first, then 1...).</summary>
    public int AddNode(Vector2 position)
    {
        _positions.Add(position);
        _edges.Add([]);
        _disabled.Add(false);
        Version++;
        return _positions.Count - 1;
    }

    /// <summary>The position <paramref name="node"/> was added at.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the graph.</exception>
    public Vector2 PositionOf(int node) => _positions[Checked(node, nameof(node))];

    /// <summary>
    /// Joins <paramref name="from"/> to <paramref name="to"/>, replacing a connection already there.
    /// </summary>
    /// <param name="from">The node the connection leaves.</param>
    /// <param name="to">The node it arrives at.</param>
    /// <param name="cost">What crossing it costs; the distance between the two positions when null. A cost below the distance (a teleporter, a fast road) is fine.</param>
    /// <param name="tag">Free for the game: how the connection is crossed. <see cref="TryGetEdge"/> gives it back for each step of a path.</param>
    /// <param name="bothWays">Whether the same connection is made from <paramref name="to"/> back to <paramref name="from"/>; false for a drop or a jump that works in one direction.</param>
    /// <exception cref="ArgumentOutOfRangeException">A node is not of the graph, or <paramref name="cost"/> is negative or not finite.</exception>
    /// <exception cref="ArgumentException">Both ends are the same node.</exception>
    public void Connect(int from, int to, float? cost = null, int tag = 0, bool bothWays = true)
    {
        Checked(from, nameof(from));
        Checked(to, nameof(to));
        if (from == to) throw new ArgumentException($"Node {from} cannot be joined to itself.", nameof(to));
        float distance = Vector2.Distance(_positions[from], _positions[to]);
        float value = cost ?? distance;
        if (!(value >= 0) || !float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(cost), cost, "A connection costs zero or more.");
        Set(from, new NavEdge(to, value, tag));
        if (bothWays) Set(to, new NavEdge(from, value, tag));
        if (distance > 0) _costPerDistance = MathF.Min(_costPerDistance, value / distance);
        Version++;
    }

    private void Set(int from, NavEdge edge)
    {
        var edges = _edges[from];
        int at = edges.FindIndex(e => e.To == edge.To);
        if (at >= 0) edges[at] = edge;
        else edges.Add(edge);
    }

    /// <summary>Removes the connection from <paramref name="from"/> to <paramref name="to"/> (and back, with <paramref name="bothWays"/>); false when there was none.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A node is not of the graph.</exception>
    public bool Disconnect(int from, int to, bool bothWays = true)
    {
        Checked(from, nameof(from));
        Checked(to, nameof(to));
        bool removed = _edges[from].RemoveAll(e => e.To == to) > 0;
        if (bothWays) removed |= _edges[to].RemoveAll(e => e.To == from) > 0;
        if (removed) Version++;
        return removed;
    }

    /// <summary>Whether paths may pass through <paramref name="node"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the graph.</exception>
    public bool IsEnabled(int node) => !_disabled[Checked(node, nameof(node))];

    /// <summary>
    /// Closes or reopens <paramref name="node"/> without touching its connections: no path enters a disabled node (a
    /// locked room, a platform that fell), though a path may still start from it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the graph.</exception>
    public void SetEnabled(int node, bool enabled)
    {
        if (_disabled[Checked(node, nameof(node))] == !enabled) return;
        _disabled[node] = !enabled;
        Version++;
    }

    /// <summary>The connection from <paramref name="from"/> to <paramref name="to"/>, with its cost and tag: what a game reads for each step of a path.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A node is not of the graph.</exception>
    public bool TryGetEdge(int from, int to, out NavEdge edge)
    {
        Checked(to, nameof(to));
        foreach (var candidate in _edges[Checked(from, nameof(from))])
            if (candidate.To == to) { edge = candidate; return true; }
        edge = default;
        return false;
    }

    /// <summary>The enabled node closest to <paramref name="point"/> (the first added among equals), or -1 when there is none.</summary>
    public int Nearest(Vector2 point)
    {
        int nearest = -1;
        float best = float.PositiveInfinity;
        for (int i = 0; i < _positions.Count; i++)
        {
            if (_disabled[i]) continue;
            float distance = Vector2.DistanceSquared(_positions[i], point);
            if (distance < best) { best = distance; nearest = i; }
        }
        return nearest;
    }

    /// <summary>The connections that leave <paramref name="node"/> to enabled nodes, in the order they were made.</summary>
    public void GetEdges(int node, List<NavEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        foreach (var edge in _edges[Checked(node, nameof(node))])
            if (!_disabled[edge.To]) edges.Add(edge);
    }

    /// <summary>
    /// The straight distance between the two nodes times the cheapest cost per unit of distance of any connection
    /// made, so it never exceeds a route's cost, whatever the connections cost.
    /// </summary>
    public float Estimate(int from, int to) =>
        float.IsPositiveInfinity(_costPerDistance) ? 0 : Vector2.Distance(_positions[from], _positions[to]) * _costPerDistance;

    private int Checked(int node, string name)
    {
        if ((uint)node >= (uint)_positions.Count) throw new ArgumentOutOfRangeException(name, node, $"The graph has {_positions.Count} nodes.");
        return node;
    }
}
