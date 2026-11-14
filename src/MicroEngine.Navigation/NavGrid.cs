using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.World;

namespace MicroEngine.Navigation;

/// <summary>How an agent may move between the cells of a <see cref="NavGrid"/>.</summary>
public enum DiagonalMovement
{
    /// <summary>Up, down, left and right only.</summary>
    Never,
    /// <summary>Diagonally too, but never between two cells when one of the cells beside the step is blocked: an agent as large as a cell does not clip a wall's corner. The default.</summary>
    NoCornerCutting,
    /// <summary>Diagonally whenever the cell arrived at is walkable, corners included: for agents that are points.</summary>
    Always,
}

/// <summary>A cell of a <see cref="NavGrid"/>: its column and row, from the top-left cell (0, 0).</summary>
public readonly record struct NavCell(int X, int Y);

/// <summary>
/// Walkable space as a grid of square cells, each walkable or blocked and with a cost to enter it. It is the graph a
/// <see cref="PathFinder"/> searches for tile maps and for any level that can be sampled on a grid; node
/// <c>y * Width + x</c> is cell (x, y). A grid is plain data the game owns: it is built once (by hand, from a tile map
/// with <see cref="FromTiles"/>, or from a physics world with <see cref="FromPhysics"/>) and changed when the level
/// changes (a door closes, a crate is pushed, a bridge falls). It decides nothing about who walks it or why.
/// </summary>
public sealed class NavGrid : INavGraph
{
    private const float Diagonal = 1.41421356f;
    // In cells: a route may touch a wall's face but not cross it, and a point never threads a corner.
    private const float Skin = 0.001f;

    private readonly bool[] _blocked;
    private readonly float[] _cost;
    private DiagonalMovement _diagonals = DiagonalMovement.NoCornerCutting;

    /// <summary>A grid of walkable cells that cost 1.</summary>
    /// <param name="width">Columns; positive.</param>
    /// <param name="height">Rows; positive.</param>
    /// <param name="cellSize">The side of a cell in world units (a tile's size in pixels for a tile map); positive.</param>
    /// <param name="origin">The world position of the grid's top-left corner.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size is not positive.</exception>
    public NavGrid(int width, int height, float cellSize = 1, Vector2 origin = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (!(cellSize > 0) || !float.IsFinite(cellSize)) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "The cell size must be positive.");
        Width = width;
        Height = height;
        CellSize = cellSize;
        Origin = origin;
        _blocked = new bool[width * height];
        _cost = new float[width * height];
        Array.Fill(_cost, 1f);
    }

    /// <summary>Columns.</summary>
    public int Width { get; }
    /// <summary>Rows.</summary>
    public int Height { get; }
    /// <summary>The side of a cell, in world units.</summary>
    public float CellSize { get; }
    /// <summary>The world position of the top-left corner of cell (0, 0).</summary>
    public Vector2 Origin { get; }
    /// <inheritdoc/>
    public int NodeCount => _blocked.Length;

    /// <summary>How agents step between cells; <see cref="DiagonalMovement.NoCornerCutting"/> by default.</summary>
    public DiagonalMovement Diagonals
    {
        get => _diagonals;
        set
        {
            if (_diagonals == value) return;
            _diagonals = value;
            Version++;
        }
    }

    /// <summary>
    /// A number that changes whenever a cell's walkability or cost, or <see cref="Diagonals"/>, changes: an agent that
    /// remembers the version its path was found for knows when to check it (<see cref="IsOpen"/>) or search again.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>Whether cell (<paramref name="x"/>, <paramref name="y"/>) is inside the grid.</summary>
    public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

    /// <summary>Whether cell (<paramref name="x"/>, <paramref name="y"/>) can be walked; cells outside the grid cannot.</summary>
    public bool IsWalkable(int x, int y) => InBounds(x, y) && !_blocked[y * Width + x];

    /// <summary>Makes cell (<paramref name="x"/>, <paramref name="y"/>) walkable or blocked. A blocked cell keeps its cost.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public void SetWalkable(int x, int y, bool walkable)
    {
        int node = NodeOf(x, y);
        if (_blocked[node] == !walkable) return;
        _blocked[node] = !walkable;
        Version++;
    }

    /// <summary>What entering cell (<paramref name="x"/>, <paramref name="y"/>) costs, as a multiple of open ground (1).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public float CostAt(int x, int y) => _cost[NodeOf(x, y)];

    /// <summary>
    /// Sets what entering cell (<paramref name="x"/>, <paramref name="y"/>) costs: 1 for open ground, more for ground
    /// agents should avoid when a way around is cheaper (mud, water, a guarded corridor). A step costs its length (1,
    /// or the square root of 2 diagonally) times the cost of the cell entered.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid, or <paramref name="cost"/> is less than 1 or not finite.</exception>
    public void SetCost(int x, int y, float cost)
    {
        int node = NodeOf(x, y);
        if (!(cost >= 1) || !float.IsFinite(cost)) throw new ArgumentOutOfRangeException(nameof(cost), cost, "A cell costs 1 (open ground) or more.");
        if (_cost[node] == cost) return;
        _cost[node] = cost;
        Version++;
    }

    /// <summary>
    /// Blocks or frees every cell <paramref name="area"/> (in world units) overlaps: a door, a crate, a fallen bridge.
    /// Cells the rectangle only touches at an edge are left alone; the part outside the grid is ignored.
    /// </summary>
    public void SetBlocked(RectF area, bool blocked)
    {
        if (!(area.Width > 0) || !(area.Height > 0)) return;
        int x0 = Math.Max(0, (int)MathF.Floor((area.Left - Origin.X) / CellSize)), x1 = Math.Min(Width - 1, (int)MathF.Ceiling((area.Right - Origin.X) / CellSize) - 1);
        int y0 = Math.Max(0, (int)MathF.Floor((area.Top - Origin.Y) / CellSize)), y1 = Math.Min(Height - 1, (int)MathF.Ceiling((area.Bottom - Origin.Y) / CellSize) - 1);
        bool changed = false;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                ref bool cell = ref _blocked[y * Width + x];
                changed |= cell != blocked;
                cell = blocked;
            }
        if (changed) Version++;
    }

    /// <summary>The node of cell (<paramref name="x"/>, <paramref name="y"/>): <c>y * Width + x</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public int NodeOf(int x, int y)
    {
        if (!InBounds(x, y)) throw new ArgumentOutOfRangeException(nameof(x), $"Cell ({x}, {y}) is outside the {Width} × {Height} grid.");
        return y * Width + x;
    }

    /// <summary>The cell of <paramref name="node"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the grid.</exception>
    public NavCell CellOf(int node)
    {
        if ((uint)node >= (uint)_blocked.Length) throw new ArgumentOutOfRangeException(nameof(node), node, $"The grid has {_blocked.Length} nodes.");
        return new NavCell(node % Width, node / Width);
    }

    /// <summary>The cell that holds the world point <paramref name="point"/>; it may be outside the grid (<see cref="InBounds"/>).</summary>
    public NavCell CellAt(Vector2 point) =>
        new((int)MathF.Floor((point.X - Origin.X) / CellSize), (int)MathF.Floor((point.Y - Origin.Y) / CellSize));

    /// <summary>The node of the cell that holds the world point <paramref name="point"/>, or -1 outside the grid.</summary>
    public int NodeAt(Vector2 point)
    {
        var cell = CellAt(point);
        return InBounds(cell.X, cell.Y) ? cell.Y * Width + cell.X : -1;
    }

    /// <summary>The world position of the centre of <paramref name="node"/>'s cell.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the grid.</exception>
    public Vector2 CenterOf(int node)
    {
        var cell = CellOf(node);
        return new Vector2(Origin.X + (cell.X + 0.5f) * CellSize, Origin.Y + (cell.Y + 0.5f) * CellSize);
    }

    /// <summary>
    /// The steps out of a cell: to each walkable neighbour (up, right, down, left, then the diagonals clockwise from
    /// up-right, as <see cref="Diagonals"/> allows), at the step's length times the neighbour's cost. The cell itself
    /// may be blocked: an agent caught in a closing door can still walk out.
    /// </summary>
    public void GetEdges(int node, List<NavEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        int x = node % Width, y = node / Width;
        bool up = IsWalkable(x, y - 1), right = IsWalkable(x + 1, y), down = IsWalkable(x, y + 1), left = IsWalkable(x - 1, y);
        if (up) edges.Add(new NavEdge(node - Width, _cost[node - Width]));
        if (right) edges.Add(new NavEdge(node + 1, _cost[node + 1]));
        if (down) edges.Add(new NavEdge(node + Width, _cost[node + Width]));
        if (left) edges.Add(new NavEdge(node - 1, _cost[node - 1]));
        if (_diagonals == DiagonalMovement.Never) return;
        bool any = _diagonals == DiagonalMovement.Always;
        if ((any || (up && right)) && IsWalkable(x + 1, y - 1)) edges.Add(new NavEdge(node - Width + 1, Diagonal * _cost[node - Width + 1]));
        if ((any || (down && right)) && IsWalkable(x + 1, y + 1)) edges.Add(new NavEdge(node + Width + 1, Diagonal * _cost[node + Width + 1]));
        if ((any || (down && left)) && IsWalkable(x - 1, y + 1)) edges.Add(new NavEdge(node + Width - 1, Diagonal * _cost[node + Width - 1]));
        if ((any || (up && left)) && IsWalkable(x - 1, y - 1)) edges.Add(new NavEdge(node - Width - 1, Diagonal * _cost[node - Width - 1]));
    }

    /// <summary>The cost between two cells over open ground with no wall between them: the least any route can cost.</summary>
    public float Estimate(int from, int to)
    {
        int dx = Math.Abs(from % Width - to % Width), dy = Math.Abs(from / Width - to / Width);
        return _diagonals == DiagonalMovement.Never ? dx + dy : Math.Max(dx, dy) + (Diagonal - 1) * Math.Min(dx, dy);
    }

    /// <summary>
    /// Whether <paramref name="path"/> (nodes, as <see cref="PathFinder.Path"/> gives them) can still be walked from
    /// its index <paramref name="from"/> on: every cell is walkable and every step is still allowed. An agent checks
    /// the rest of its path when <see cref="Version"/> changes and searches again only when this is false.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="from"/> is negative, or a node is not of this grid.</exception>
    public bool IsOpen(IReadOnlyList<int> path, int from = 0)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentOutOfRangeException.ThrowIfNegative(from);
        for (int i = from; i < path.Count; i++)
        {
            var cell = CellOf(path[i]);
            if (_blocked[path[i]]) return false;
            if (i == from) continue;
            var previous = CellOf(path[i - 1]);
            int dx = cell.X - previous.X, dy = cell.Y - previous.Y;
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1) return false;
            if (dx == 0 || dy == 0 || _diagonals == DiagonalMovement.Always) continue;
            if (_diagonals == DiagonalMovement.Never || !IsWalkable(previous.X + dx, previous.Y) || !IsWalkable(previous.X, previous.Y + dy)) return false;
        }
        return true;
    }

    /// <summary>
    /// Whether an agent can go straight from <paramref name="from"/> to <paramref name="to"/> (world points): no
    /// blocked cell, and nothing outside the grid, is touched by the square of half-side <paramref name="radius"/>
    /// centred on it along the way. With a radius of 0 the agent is a point; a point does not pass between two
    /// blocked cells that meet at a corner.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative or not finite.</exception>
    public bool HasLineOfSight(Vector2 from, Vector2 to, float radius = 0)
    {
        CheckRadius(radius);
        return Clear(from, to, radius, float.PositiveInfinity);
    }

    /// <summary>
    /// Turns a path of cells into the points an agent walks through: the centres of its cells with every one removed
    /// that the agent can skip by walking straight (<see cref="HasLineOfSight"/> with <paramref name="radius"/>), so
    /// the route turns only where a wall makes it. <paramref name="waypoints"/> is replaced; the first cell, where the
    /// agent is, is not in it. A straight leg never crosses ground costlier than the cells it replaces.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative or not finite, or a node is not of this grid.</exception>
    public void Smooth(IReadOnlyList<int> path, List<Vector2> waypoints, float radius = 0)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(waypoints);
        CheckRadius(radius);
        waypoints.Clear();
        if (path.Count > 0) Straighten(path, CenterOf(path[0]), null, waypoints, radius);
    }

    /// <summary>
    /// The route an agent at <paramref name="from"/> walks to reach <paramref name="to"/> (world points): searches
    /// the cells with <paramref name="finder"/> and fills <paramref name="waypoints"/> with the points to walk
    /// through in order, straightened as <see cref="Smooth"/> does, ending at <paramref name="to"/> itself. The cells
    /// stay in <see cref="PathFinder.Path"/>. When the point cannot be reached (it is in a wall, cut off, or outside
    /// the grid) the route leads to the nearest cell that can; when the agent is outside the grid there is no route.
    /// </summary>
    /// <param name="finder">The finder that searches; its last path becomes this route's cells.</param>
    /// <param name="from">Where the agent is.</param>
    /// <param name="to">Where it wants to be.</param>
    /// <param name="waypoints">Replaced by the route; the agent's own position is not in it.</param>
    /// <param name="radius">Half the side of the agent's square, in world units: straight legs keep that distance from walls.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative or not finite, or a point is not finite.</exception>
    public PathStatus FindPath(PathFinder finder, Vector2 from, Vector2 to, List<Vector2> waypoints, float radius = 0)
    {
        ArgumentNullException.ThrowIfNull(finder);
        ArgumentNullException.ThrowIfNull(waypoints);
        CheckRadius(radius);
        if (!float.IsFinite(from.X) || !float.IsFinite(from.Y)) throw new ArgumentOutOfRangeException(nameof(from), from, "The point must be finite.");
        if (!float.IsFinite(to.X) || !float.IsFinite(to.Y)) throw new ArgumentOutOfRangeException(nameof(to), to, "The point must be finite.");
        waypoints.Clear();
        int start = NodeAt(from);
        if (start < 0) return PathStatus.Unreachable;
        var cell = CellAt(to);
        bool inside = InBounds(cell.X, cell.Y);
        int goal = Math.Clamp(cell.Y, 0, Height - 1) * Width + Math.Clamp(cell.X, 0, Width - 1);
        var status = finder.Find(this, start, goal);
        Straighten(finder.Path, from, status == PathStatus.Found && inside ? to : null, waypoints, radius);
        return status == PathStatus.Found && !inside ? PathStatus.Unreachable : status;
    }

    /// <summary>
    /// Reads every cell again from a tile map: a cell is blocked when its tile has any of the
    /// <paramref name="blocking"/> flags. Call it after tiles change; for one cell, <see cref="SetWalkable"/> is
    /// cheaper. Returns whether anything changed.
    /// </summary>
    public bool ReadTiles(ICollisionGrid tiles, TileFlags blocking = TileFlags.Solid)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        bool changed = false;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                bool blocked = (tiles.FlagsAt(x, y) & blocking) != 0;
                ref bool cell = ref _blocked[y * Width + x];
                changed |= cell != blocked;
                cell = blocked;
            }
        if (changed) Version++;
        return changed;
    }

    /// <summary>
    /// A grid over a tile map, one cell per tile: tiles with any of the <paramref name="blocking"/> flags are
    /// blocked (solid tiles by default; add <see cref="TileFlags.Hazard"/> or a custom flag for tiles agents must not
    /// enter). The grid is a copy: when tiles change, call <see cref="ReadTiles"/> or <see cref="SetWalkable"/>.
    /// </summary>
    public static NavGrid FromTiles(TileGrid tiles, TileFlags blocking = TileFlags.Solid)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        var grid = new NavGrid(tiles.Layer.Width, tiles.Layer.Height, tiles.TileSize);
        grid.ReadTiles(tiles, blocking);
        grid.Version = 0;
        return grid;
    }

    /// <summary>
    /// Reads every cell again from a physics world: a cell is blocked when a shape <paramref name="filter"/> accepts
    /// overlaps it. Give the filter of the agents that will walk the grid, so walls block and other agents, pickups
    /// and sensors do not. Returns whether anything changed.
    /// </summary>
    public bool ReadPhysics(IPhysicsWorld2D world, in QueryFilter filter = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        Span<ShapeId> hit = stackalloc ShapeId[1];
        float inset = CellSize * 0.02f, side = CellSize - 2 * inset;
        bool changed = false;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var area = new RectF(Origin.X + x * CellSize + inset, Origin.Y + y * CellSize + inset, side, side);
                bool blocked = world.Overlap(area, filter, hit) > 0;
                ref bool cell = ref _blocked[y * Width + x];
                changed |= cell != blocked;
                cell = blocked;
            }
        if (changed) Version++;
        return changed;
    }

    /// <summary>
    /// A grid of <paramref name="cellSize"/> cells over <paramref name="bounds"/> of a physics world, for levels made
    /// of shapes instead of tiles: a cell is blocked when a shape <paramref name="filter"/> accepts overlaps it. The
    /// grid is a copy: when static shapes move, call <see cref="ReadPhysics"/> or <see cref="SetBlocked"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The bounds are empty or <paramref name="cellSize"/> is not positive.</exception>
    public static NavGrid FromPhysics(IPhysicsWorld2D world, RectF bounds, float cellSize, in QueryFilter filter = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!(bounds.Width > 0) || !(bounds.Height > 0)) throw new ArgumentOutOfRangeException(nameof(bounds), bounds, "The bounds must have an area.");
        if (!(cellSize > 0)) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "The cell size must be positive.");
        var grid = new NavGrid((int)MathF.Ceiling(bounds.Width / cellSize - 1e-4f), (int)MathF.Ceiling(bounds.Height / cellSize - 1e-4f), cellSize, bounds.Position);
        grid.ReadPhysics(world, filter);
        grid.Version = 0;
        return grid;
    }

    /// <summary>What entering <paramref name="node"/> costs.</summary>
    internal float CostOf(int node) => _cost[node];

    /// <summary>Whether <paramref name="node"/> is blocked.</summary>
    internal bool IsBlocked(int node) => _blocked[node];

    /// <summary>The length of the step between two neighbouring nodes.</summary>
    internal float StepLength(int a, int b) => a % Width != b % Width && a / Width != b / Width ? Diagonal : 1;

    private static void CheckRadius(float radius)
    {
        if (!(radius >= 0) || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(radius), radius, "The radius is zero or more.");
    }

    // The points after `from` through the centres of path[1..], the last one replaced by `last` when given, keeping only
    // those where the straight line from the previous kept point stops being clear.
    private void Straighten(IReadOnlyList<int> path, Vector2 from, Vector2? last, List<Vector2> waypoints, float radius)
    {
        int count = path.Count - 1;
        if (count <= 0)
        {
            if (last is { } only) waypoints.Add(only);
            return;
        }
        var anchor = from;
        float anchorCost = _cost[path[0]];
        int i = 0;
        while (i < count)
        {
            int j = i;
            float limit = Math.Max(anchorCost, _cost[path[i + 1]]);
            while (j + 1 < count)
            {
                float next = Math.Max(limit, _cost[path[j + 2]]);
                if (!Clear(anchor, Point(path, j + 1, count, last), radius, next)) break;
                limit = next;
                j++;
            }
            anchor = Point(path, j, count, last);
            waypoints.Add(anchor);
            anchorCost = _cost[path[j + 1]];
            i = j + 1;
        }
    }

    private Vector2 Point(IReadOnlyList<int> path, int index, int count, Vector2? last) =>
        index == count - 1 && last is { } point ? point : CenterOf(path[index + 1]);

    // Whether the square of half-side `radius` swept from a to b touches only walkable cells that cost at most
    // `costLimit`. Row by row: the part of the segment whose square reaches the row, and the columns that part covers.
    private bool Clear(Vector2 a, Vector2 b, float radius, float costLimit)
    {
        float ax = (a.X - Origin.X) / CellSize, ay = (a.Y - Origin.Y) / CellSize, bx = (b.X - Origin.X) / CellSize, by = (b.Y - Origin.Y) / CellSize;
        if (!float.IsFinite(ax + ay + bx + by)) return false;
        float r = MathF.Max(radius / CellSize - Skin, Skin);
        float top = MathF.Min(ay, by) - r, bottom = MathF.Max(ay, by) + r;
        if (top < 0 || bottom > Height || MathF.Min(ax, bx) - r < 0 || MathF.Max(ax, bx) + r > Width) return false;
        int row0 = (int)MathF.Floor(top), row1 = (int)MathF.Ceiling(bottom) - 1;
        float dx = bx - ax, dy = by - ay;
        for (int row = row0; row <= row1; row++)
        {
            float t0 = 0, t1 = 1;
            if (MathF.Abs(dy) > 1e-6f)
            {
                float enter = (row - r - ay) / dy, leave = (row + 1 + r - ay) / dy;
                if (enter > leave) (enter, leave) = (leave, enter);
                t0 = MathF.Max(0, enter);
                t1 = MathF.Min(1, leave);
                if (t0 > t1) continue;
            }
            float x0 = ax + dx * t0, x1 = ax + dx * t1;
            int column0 = (int)MathF.Floor(MathF.Min(x0, x1) - r), column1 = Math.Min(Width - 1, (int)MathF.Ceiling(MathF.Max(x0, x1) + r) - 1);
            for (int node = row * Width + Math.Max(0, column0), end = row * Width + column1; node <= end; node++)
                if (_blocked[node] || _cost[node] > costLimit) return false;
        }
        return true;
    }
}
