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
    // The free cells around each cell (ClearanceAt), measured again when a sized question follows a change of walls.
    private ushort[]? _clearance;
    private bool _clearanceStale = true;
    private readonly List<SizedGrid> _sized = [];

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
        Walls();
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
        if (changed) Walls();
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
    /// <param name="path">The nodes of the path.</param>
    /// <param name="from">The index of the path's cell the agent is in. The cells before it are not looked at, and the agent's own cell may be one its body does not fit in (it is pressed against a wall).</param>
    /// <param name="radius">Half the side of the agent's square, in world units: for an agent wider than a cell, every cell ahead must still have room for it (<see cref="Fits(int, int, float)"/>).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="from"/> is negative, <paramref name="radius"/> is negative or not finite, or a node is not of this grid.</exception>
    public bool IsOpen(IReadOnlyList<int> path, int from = 0, float radius = 0)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentOutOfRangeException.ThrowIfNegative(from);
        CheckRadius(radius);
        int rings = Rings(radius);
        bool corners = _diagonals == DiagonalMovement.Always && rings == 0;
        for (int i = from; i < path.Count; i++)
        {
            var cell = CellOf(path[i]);
            if (_blocked[path[i]]) return false;
            if (i == from) continue;
            if (!Passable(path[i], rings)) return false;
            var previous = CellOf(path[i - 1]);
            int dx = cell.X - previous.X, dy = cell.Y - previous.Y;
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1) return false;
            if (dx == 0 || dy == 0 || corners) continue;
            if (_diagonals == DiagonalMovement.Never || !Room(previous.X + dx, previous.Y, rings) || !Room(previous.X, previous.Y + dy, rings)) return false;
        }
        return true;
    }

    private bool Room(int x, int y, int rings) => InBounds(x, y) && Passable(y * Width + x, rings);

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
    /// <param name="radius">
    /// Half the side of the agent's square, in world units: straight legs keep that distance from walls, an agent
    /// wider than a cell is led only through passages it fits in (<see cref="ForAgent"/>), and the route ends at the
    /// point of the goal's cell nearest <paramref name="to"/> where the body touches no wall.
    /// </param>
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
        var status = finder.Find(ForAgent(radius), start, goal);
        Straighten(finder.Path, from, status == PathStatus.Found && inside ? Settle(to, goal, radius) : null, waypoints, radius);
        return status == PathStatus.Found && !inside ? PathStatus.Unreachable : status;
    }

    /// <summary>
    /// Moves a body whose square has the half-side <paramref name="radius"/> from <paramref name="position"/> by
    /// <paramref name="delta"/> (world units) and returns where it ends: it stops at the face of a blocked cell or
    /// of the grid's edge and slides along it, so it never enters a wall. For things that walk a grid without a
    /// physics body; agents of <see cref="NavAgents"/> move this way. A body that is already in a wall (one was
    /// built on it) can walk out.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative or not finite.</exception>
    public Vector2 Slide(Vector2 position, Vector2 delta, float radius = 0)
    {
        CheckRadius(radius);
        float x = (position.X - Origin.X) / CellSize, y = (position.Y - Origin.Y) / CellSize, r = radius / CellSize;
        if (!float.IsFinite(x + y + delta.X + delta.Y)) return position;
        if (delta.X != 0) x = Sweep(x, y, delta.X / CellSize, r, Width, Height, rows: false);
        if (delta.Y != 0) y = Sweep(y, x, delta.Y / CellSize, r, Height, Width, rows: true);
        return new Vector2(Origin.X + x * CellSize, Origin.Y + y * CellSize);
    }

    // Moves the centre `at` along one axis by `by` cells and returns where it stops: at the first line of cells (a
    // column, or a row when `rows`) its leading edge would enter where a cell the body spans across is blocked.
    private float Sweep(float at, float across, float by, float r, int lines, int span, bool rows)
    {
        int low = Math.Max(0, (int)MathF.Floor(across - r + Skin));
        int high = Math.Min(span - 1, Math.Max(low, (int)MathF.Ceiling(across + r - Skin) - 1));
        if (by > 0)
        {
            float edge = at + r, end = edge + by;
            for (int line = (int)MathF.Ceiling(edge - Skin), last = (int)MathF.Ceiling(end - Skin) - 1; line <= last; line++)
                if (Shut(line, low, high, lines, rows)) return line - r;
        }
        else
        {
            float edge = at - r, end = edge + by;
            for (int line = (int)MathF.Floor(edge + Skin) - 1, last = (int)MathF.Floor(end + Skin); line >= last; line--)
                if (Shut(line, low, high, lines, rows)) return line + 1 + r;
        }
        return at + by;
    }

    // Whether a line of cells (column `line`, or row `line`) is outside the grid or blocked between `low` and `high`.
    private bool Shut(int line, int low, int high, int lines, bool rows)
    {
        if ((uint)line >= (uint)lines) return true;
        for (int i = low; i <= high; i++)
            if (_blocked[rows ? line * Width + i : i * Width + line]) return true;
        return false;
    }

    /// <summary>The route FindPath makes of a finished search: <paramref name="path"/> straightened from <paramref name="from"/>, ending at <paramref name="to"/> when <paramref name="exact"/>.</summary>
    internal void Route(IReadOnlyList<int> path, Vector2 from, Vector2 to, bool exact, List<Vector2> waypoints, float radius)
    {
        waypoints.Clear();
        if (path.Count > 0) Straighten(path, from, exact ? Settle(to, path[^1], radius) : null, waypoints, radius);
    }

    /// <summary>Whether the straight way between two points is clear for a body, crossing no ground costlier than where it ends.</summary>
    internal bool HasDirectWay(Vector2 from, Vector2 to, float radius)
    {
        int b = NodeAt(to);
        return b >= 0 && Clear(from, to, radius, _cost[b]);
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
        if (changed) Walls();
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
        if (changed) Walls();
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

    /// <summary>
    /// How much free room surrounds cell (<paramref name="x"/>, <paramref name="y"/>): 0 for a blocked cell or one
    /// outside the grid, 1 for a walkable cell with a wall (or the grid's edge) beside it, corners included, 2 when
    /// the nearest wall is two cells away, and so on.
    /// </summary>
    public int ClearanceAt(int x, int y) => InBounds(x, y) ? Clearance()[y * Width + x] : 0;

    /// <summary>
    /// Whether an agent whose square has the half-side <paramref name="radius"/> (world units) can stand centred in
    /// cell (<paramref name="x"/>, <paramref name="y"/>) without touching a blocked cell or leaving the grid. An
    /// agent no wider than a cell fits in every walkable cell.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative or not finite.</exception>
    public bool Fits(int x, int y, float radius)
    {
        CheckRadius(radius);
        return Room(x, y, Rings(radius));
    }

    /// <summary>
    /// The grid as an agent whose square has the half-side <paramref name="radius"/> (world units) walks it: the
    /// same nodes, with steps only into cells where the whole body fits (<see cref="Fits(int, int, float)"/>).
    /// Search it with <see cref="PathFinder.Find"/> when the game moves cell by cell; <see cref="FindPath"/> does it
    /// by itself. An agent no wider than a cell walks the grid itself, which is what this returns for it. A wider
    /// agent is given whole cells of room on every side: wider than one cell it needs a passage three cells wide,
    /// wider than three cells one of five. A wide agent never cuts a corner. The view follows the grid's changes.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative or not finite.</exception>
    public INavGraph ForAgent(float radius)
    {
        CheckRadius(radius);
        int rings = Rings(radius);
        if (rings == 0) return this;
        foreach (var view in _sized)
            if (view.Rings == rings) return view;
        var made = new SizedGrid(this, rings);
        _sized.Add(made);
        return made;
    }

    /// <summary>How many cells of room beyond its own an agent of half-side <paramref name="radius"/> needs on every side.</summary>
    internal int Rings(float radius) => Math.Max(0, (int)MathF.Ceiling(radius / CellSize - 0.5f - 1e-4f));

    /// <summary>Whether an agent that needs <paramref name="rings"/> cells of room can stand in <paramref name="node"/>.</summary>
    internal bool Passable(int node, int rings) => rings == 0 ? !_blocked[node] : Clearance()[node] > rings;

    /// <summary>
    /// The steps out of <paramref name="node"/> for an agent that needs <paramref name="rings"/> cells of room, in
    /// the order of <see cref="GetEdges(int, List{NavEdge})"/>, as if the cells of <paramref name="closed"/> were
    /// blocked too. A wide agent never cuts a corner, except to leave a cell it does not fit in.
    /// </summary>
    internal void GetEdges(int node, List<NavEdge> edges, int rings, in CellArea closed = default)
    {
        if (rings == 0 && closed.IsEmpty)
        {
            GetEdges(node, edges);
            return;
        }
        var clearance = rings == 0 ? null : Clearance();
        var shut = closed.Grown(rings);
        int x = node % Width, y = node / Width;
        bool up = Open(x, y - 1), right = Open(x + 1, y), down = Open(x, y + 1), left = Open(x - 1, y);
        if (up) edges.Add(new NavEdge(node - Width, _cost[node - Width]));
        if (right) edges.Add(new NavEdge(node + 1, _cost[node + 1]));
        if (down) edges.Add(new NavEdge(node + Width, _cost[node + Width]));
        if (left) edges.Add(new NavEdge(node - 1, _cost[node - 1]));
        if (_diagonals == DiagonalMovement.Never) return;
        // A body pressed into a tight cell (beside a wall, in a corner) leaves it any way it can.
        bool any = (_diagonals == DiagonalMovement.Always && rings == 0) || (rings > 0 && !Open(x, y));
        if ((any || (up && right)) && Open(x + 1, y - 1)) edges.Add(new NavEdge(node - Width + 1, Diagonal * _cost[node - Width + 1]));
        if ((any || (down && right)) && Open(x + 1, y + 1)) edges.Add(new NavEdge(node + Width + 1, Diagonal * _cost[node + Width + 1]));
        if ((any || (down && left)) && Open(x - 1, y + 1)) edges.Add(new NavEdge(node + Width - 1, Diagonal * _cost[node + Width - 1]));
        if ((any || (up && left)) && Open(x - 1, y - 1)) edges.Add(new NavEdge(node - Width - 1, Diagonal * _cost[node - Width - 1]));

        bool Open(int cx, int cy)
        {
            if (!InBounds(cx, cy) || shut.Contains(cx, cy)) return false;
            int at = cy * Width + cx;
            return clearance is null ? !_blocked[at] : clearance[at] > rings;
        }
    }

    // A wall appeared or went: the grid's version changes, and the room around every cell is measured again when asked.
    private void Walls()
    {
        Version++;
        _clearanceStale = true;
    }

    // The distance from every cell to the nearest blocked cell or the outside, a diagonal step counting as one: two
    // passes, each cell taking the least of the neighbours already visited plus one.
    private ushort[] Clearance()
    {
        var clearance = _clearance ??= new ushort[_blocked.Length];
        if (!_clearanceStale) return clearance;
        _clearanceStale = false;
        int w = Width, h = Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int node = y * w + x;
                if (_blocked[node]) { clearance[node] = 0; continue; }
                int least = x == 0 || y == 0 || x == w - 1 ? 0
                    : Math.Min(Math.Min(clearance[node - 1], clearance[node - w]), Math.Min(clearance[node - w - 1], clearance[node - w + 1]));
                clearance[node] = (ushort)Math.Min(least + 1, ushort.MaxValue);
            }
        for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                int node = y * w + x;
                if (_blocked[node]) continue;
                int least = x == w - 1 || y == h - 1 || x == 0 ? 0
                    : Math.Min(Math.Min(clearance[node + 1], clearance[node + w]), Math.Min(clearance[node + w + 1], clearance[node + w - 1]));
                if (least + 1 < clearance[node]) clearance[node] = (ushort)(least + 1);
            }
        return clearance;
    }

    // The point of `node`'s cell nearest `point` where the agent's square touches no wall: the point itself, else the
    // point moved to the cell's centre along one axis, else the centre.
    internal Vector2 Settle(Vector2 point, int node, float radius)
    {
        if (radius == 0 || Clear(point, point, radius, float.PositiveInfinity)) return point;
        var center = CenterOf(node);
        var along = new Vector2(point.X, center.Y);
        if (Clear(along, along, radius, float.PositiveInfinity)) return along;
        along = new Vector2(center.X, point.Y);
        return Clear(along, along, radius, float.PositiveInfinity) ? along : center;
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
        // What a leg may cross is what the cells it replaces cost to enter. The cell the agent stands in is not
        // entered: standing in mud is no licence to walk on through it.
        float anchorCost = 0;
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

/// <summary>A rectangle of cells, both corners included; <c>default</c> holds no cell.</summary>
internal readonly struct CellArea
{
    private readonly bool _any;
    public readonly int X0, Y0, X1, Y1;

    public CellArea(int x0, int y0, int x1, int y1)
    {
        X0 = x0;
        Y0 = y0;
        X1 = x1;
        Y1 = y1;
        _any = x1 >= x0 && y1 >= y0;
    }

    public bool IsEmpty => !_any;
    public bool Contains(int x, int y) => _any && x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    public CellArea Grown(int by) => _any ? new CellArea(X0 - by, Y0 - by, X1 + by, Y1 + by) : default;
}

/// <summary>A grid as an agent wider than a cell walks it (<see cref="NavGrid.ForAgent"/>).</summary>
internal sealed class SizedGrid(NavGrid grid, int rings) : INavGraph
{
    public int Rings => rings;
    public int NodeCount => grid.NodeCount;
    public void GetEdges(int node, List<NavEdge> edges) => grid.GetEdges(node, edges, rings);
    public float Estimate(int from, int to) => grid.Estimate(from, to);
}
