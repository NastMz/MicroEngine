using System.Numerics;

namespace MicroEngine.Navigation;

/// <summary>
/// What can be reached from where on a <see cref="NavGrid"/>: the cells joined by some route form a region, and two
/// places are joined exactly when they are in the same region. It answers without a search whether a target can be
/// reached (<see cref="Connected"/>), where to go instead when it cannot (<see cref="NearestReachable"/>), and
/// whether a wall about to be built would cut a way that must stay open (<see cref="StaysConnected(RectF, int, int)"/>):
/// the placement rule of a tower defence. The regions follow the grid: after a change they are measured again by the
/// first question, so there is nothing to call.
/// </summary>
/// <example>
/// <code>
/// var regions = new NavRegions(grid);
/// // A click on an island the unit cannot reach sends it to the shore instead.
/// int goal = regions.NearestReachable(grid.NodeAt(unit.Position), grid.NodeAt(click));
/// // A tower may be built only where the creeps keep a way from the gate to the keep.
/// if (regions.StaysConnected(towerArea, gate, keep)) grid.SetBlocked(towerArea, true);
/// </code>
/// </example>
public sealed class NavRegions
{
    private readonly int _rings;
    private readonly int[] _region;
    private readonly List<int> _sizes = [];
    private readonly List<NavEdge> _edges = new(8);
    private readonly List<int> _queue = [];
    private int[] _mark = [];
    private int _stamp;
    private int _version = -1;
    private DiagonalMovement _diagonals;

    /// <summary>The regions of <paramref name="grid"/> for agents of one size.</summary>
    /// <param name="grid">The grid.</param>
    /// <param name="radius">
    /// Half the side of the agents' square, in world units: for agents wider than a cell, two rooms joined by a gap
    /// they do not fit through are two regions (<see cref="NavGrid.ForAgent"/>). Agents of different widths ask
    /// different regions.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative or not finite.</exception>
    public NavRegions(NavGrid grid, float radius = 0)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        if (!(radius >= 0) || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(radius), radius, "The radius is zero or more.");
        Radius = radius;
        _rings = grid.Rings(radius);
        _region = new int[grid.NodeCount];
    }

    /// <summary>The grid the regions describe.</summary>
    public NavGrid Grid { get; }

    /// <summary>Half the side of the square of the agents the regions are for, in world units.</summary>
    public float Radius { get; }

    /// <summary>How many regions the grid has: 1 when every cell an agent can stand on is joined to every other.</summary>
    public int Count
    {
        get
        {
            Refresh();
            return _sizes.Count;
        }
    }

    /// <summary>
    /// The region of <paramref name="node"/>, from 0: regions are numbered in the order of their first cell (row by
    /// row from the top-left), so the same grid always gives the same numbers. For a cell an agent can only leave (a
    /// blocked cell, or one too tight for a wide body pressed against a wall) it is the region of the first cell the
    /// agent can step into, and -1 when there is none. The numbers change when the grid does: compare them, do not
    /// keep them.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the grid.</exception>
    public int RegionOf(int node)
    {
        Check(node, nameof(node));
        Refresh();
        if (_region[node] >= 0) return _region[node];
        _edges.Clear();
        Grid.GetEdges(node, _edges, _rings);
        return _edges.Count > 0 ? _region[_edges[0].To] : -1;
    }

    /// <summary>How many cells <paramref name="region"/> has.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="region"/> is not a region of the grid.</exception>
    public int SizeOf(int region)
    {
        Refresh();
        if ((uint)region >= (uint)_sizes.Count) throw new ArgumentOutOfRangeException(nameof(region), region, $"The grid has {_sizes.Count} regions.");
        return _sizes[region];
    }

    /// <summary>Whether an agent at node <paramref name="a"/> can walk to node <paramref name="b"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A node is not of the grid.</exception>
    public bool Connected(int a, int b)
    {
        Check(b, nameof(b));
        int region = RegionOf(a);
        return region >= 0 && _region[b] == region;
    }

    /// <summary>
    /// Where an agent at node <paramref name="from"/> goes when it is sent to node <paramref name="to"/>:
    /// <paramref name="to"/> itself when it can be reached, otherwise the cell of the agent's region nearest to it in
    /// a straight line (the lowest node among equals), and -1 when the agent can go nowhere. A search to the answer
    /// always finds its goal, so it never has to look at the whole region to learn that a click on an island or
    /// inside a wall cannot be reached.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A node is not of the grid.</exception>
    public int NearestReachable(int from, int to)
    {
        Check(to, nameof(to));
        int region = RegionOf(from);
        if (region < 0) return -1;
        if (_region[to] == region) return to;
        int width = Grid.Width, height = Grid.Height, tx = to % width, ty = to / width;
        int best = -1;
        long bestDistance = long.MaxValue;
        // Square rings around the target. A ring's cells are at least its number of cells away, so the search ends
        // once a ring is further than the best cell found.
        for (int ring = 1; ring < width + height && (long)ring * ring <= bestDistance; ring++)
        {
            for (int y = ty - ring; y <= ty + ring; y++)
            {
                if ((uint)y >= (uint)height) continue;
                bool edge = y == ty - ring || y == ty + ring;
                for (int x = tx - ring; x <= tx + ring; x += edge ? 1 : 2 * ring)
                {
                    if ((uint)x >= (uint)width || _region[y * width + x] != region) continue;
                    long dx = x - tx, dy = y - ty, distance = dx * dx + dy * dy;
                    int node = y * width + x;
                    if (distance < bestDistance || (distance == bestDistance && node < best))
                    {
                        bestDistance = distance;
                        best = node;
                    }
                }
            }
        }
        return best;
    }

    /// <summary>
    /// Whether nodes <paramref name="a"/> and <paramref name="b"/> would still be joined if every cell
    /// <paramref name="area"/> overlaps were blocked (the cells <see cref="NavGrid.SetBlocked"/> would block). Nothing
    /// is changed: ask before building. False when they are not joined now, or when the area covers one of them.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A node is not of the grid.</exception>
    public bool StaysConnected(RectF area, int a, int b) => StaysConnected(area, [a, b]);

    /// <summary>
    /// Whether every one of <paramref name="nodes"/> would still be joined to the others if every cell
    /// <paramref name="area"/> overlaps were blocked: the gate, the keep and the cell of each creep on the field, so
    /// that a tower shuts nobody in. Nothing is changed. False when they are not all joined now, or when the area
    /// covers one of them (for agents wider than a cell: when a body centred there would touch the new wall). It
    /// looks at the cells reached from the first node, at most once each.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A node is not of the grid.</exception>
    public bool StaysConnected(RectF area, ReadOnlySpan<int> nodes)
    {
        foreach (int node in nodes) Check(node, nameof(nodes));
        if (nodes.Length == 0) return true;
        var grid = Grid;
        var closed = CellsOf(area);
        var shut = closed.Grown(_rings);

        // Where each place stands: its own cell, or for a wide body pressed against a wall the first cell it can step
        // into. A place the new wall would touch is not kept.
        if (_mark.Length < grid.NodeCount) _mark = new int[grid.NodeCount];
        if (_stamp >= int.MaxValue - 2)
        {
            Array.Clear(_mark);
            _stamp = 0;
        }
        int wanted = _stamp + 1, reached = _stamp + 2;
        _stamp += 2;
        int start = -1, left = 0;
        foreach (int node in nodes)
        {
            int cell = node % grid.Width, row = node / grid.Width;
            if (shut.Contains(cell, row) || grid.IsBlocked(node)) return false;
            int stand = node;
            if (!grid.Passable(node, _rings))
            {
                _edges.Clear();
                grid.GetEdges(node, _edges, _rings, closed);
                if (_edges.Count == 0) return false;
                stand = _edges[0].To;
            }
            if (start < 0) start = stand;
            if (_mark[stand] == wanted) continue;
            _mark[stand] = wanted;
            left++;
        }

        _queue.Clear();
        _queue.Add(start);
        _mark[start] = reached;
        left--;
        for (int head = 0; head < _queue.Count && left > 0; head++)
        {
            _edges.Clear();
            grid.GetEdges(_queue[head], _edges, _rings, closed);
            foreach (var edge in _edges)
            {
                if (_mark[edge.To] == reached) continue;
                if (_mark[edge.To] == wanted) left--;
                _mark[edge.To] = reached;
                _queue.Add(edge.To);
            }
        }
        return left == 0;
    }

    // The regions as they would be with an area closed, for questions about many pairs of places (NavAgents.CanBlock):
    // labelled once per token.
    private int[]? _with;
    private int _withToken = -1;
    private CellArea _withClosed;

    /// <summary>
    /// Whether an agent at <paramref name="from"/> would still reach <paramref name="to"/> with <paramref name="area"/>
    /// blocked. Every call with the same <paramref name="token"/> must give the same area: the grid is labelled once.
    /// </summary>
    internal bool ConnectedWith(RectF area, int token, int from, int to)
    {
        var grid = Grid;
        if (token != _withToken)
        {
            _withToken = token;
            _withClosed = CellsOf(area);
            var labels = _with ??= new int[grid.NodeCount];
            Array.Fill(labels, -1);
            var shut = _withClosed.Grown(_rings);
            int count = 0;
            for (int first = 0; first < labels.Length; first++)
            {
                if (labels[first] >= 0 || !grid.Passable(first, _rings) || shut.Contains(first % grid.Width, first / grid.Width)) continue;
                _queue.Clear();
                _queue.Add(first);
                labels[first] = count;
                for (int head = 0; head < _queue.Count; head++)
                {
                    _edges.Clear();
                    grid.GetEdges(_queue[head], _edges, _rings, _withClosed);
                    foreach (var edge in _edges)
                    {
                        if (labels[edge.To] >= 0) continue;
                        labels[edge.To] = count;
                        _queue.Add(edge.To);
                    }
                }
                count++;
            }
        }
        int region = _with![from];
        if (region < 0)
        {
            _edges.Clear();
            grid.GetEdges(from, _edges, _rings, _withClosed);
            if (_edges.Count == 0) return false;
            region = _with[_edges[0].To];
        }
        return region >= 0 && _with[to] == region;
    }

    // The cells a rectangle of the world overlaps, as NavGrid.SetBlocked takes them.
    private CellArea CellsOf(RectF area)
    {
        if (!(area.Width > 0) || !(area.Height > 0)) return default;
        var grid = Grid;
        return new CellArea(
            Math.Max(0, (int)MathF.Floor((area.Left - grid.Origin.X) / grid.CellSize)),
            Math.Max(0, (int)MathF.Floor((area.Top - grid.Origin.Y) / grid.CellSize)),
            Math.Min(grid.Width - 1, (int)MathF.Ceiling((area.Right - grid.Origin.X) / grid.CellSize) - 1),
            Math.Min(grid.Height - 1, (int)MathF.Ceiling((area.Bottom - grid.Origin.Y) / grid.CellSize) - 1));
    }

    private void Check(int node, string name)
    {
        if ((uint)node >= (uint)_region.Length) throw new ArgumentOutOfRangeException(name, node, $"The grid has {_region.Length} nodes.");
    }

    // Labels every cell an agent can stand on with its region: a flood from each cell not yet labelled, in node order.
    private void Refresh()
    {
        var grid = Grid;
        if (_version == grid.Version && _diagonals == grid.Diagonals) return;
        _version = grid.Version;
        _diagonals = grid.Diagonals;
        _sizes.Clear();
        Array.Fill(_region, -1);
        for (int first = 0; first < _region.Length; first++)
        {
            if (_region[first] >= 0 || !grid.Passable(first, _rings)) continue;
            int region = _sizes.Count;
            _queue.Clear();
            _queue.Add(first);
            _region[first] = region;
            for (int head = 0; head < _queue.Count; head++)
            {
                _edges.Clear();
                grid.GetEdges(_queue[head], _edges, _rings);
                foreach (var edge in _edges)
                {
                    if (_region[edge.To] >= 0) continue;
                    _region[edge.To] = region;
                    _queue.Add(edge.To);
                }
            }
            _sizes.Add(_queue.Count);
        }
    }
}
