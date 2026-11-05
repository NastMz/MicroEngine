namespace MicroEngine.Physics.Tiles;

/// <summary>
/// The broad phase of <see cref="TilePhysicsWorld2D"/>: a uniform grid of square cells over the world's box shapes, so a
/// move, a query or a sensor looks only at the shapes near it instead of every shape of the world. Each box shape is
/// filed in the cells its rectangle covers; a shape that would cover more than <see cref="MaxCells"/> cells (a long floor)
/// is kept in a list every query includes. Tile grids are not filed: queries always include them. Candidates come back in
/// shape-slot order, the order of the full scan, so results are identical to scanning every shape.
/// </summary>
internal sealed class ShapeGrid
{
    /// <summary>The most cells a shape is filed in; larger shapes go to the list every query sees.</summary>
    public const int MaxCells = 64;

    private readonly float _cell;
    private readonly Dictionary<long, List<int>> _cells = [];
    private readonly List<int> _large = [];
    private readonly List<Entry> _entries = [];
    private readonly List<int> _found = [];
    private int[] _stamp = [];
    private int _query;

    private struct Entry
    {
        public bool Filed;
        public bool Large;
        public int X0, Y0, X1, Y1;
    }

    /// <summary>A grid of <paramref name="cellSize"/> world units a side.</summary>
    public ShapeGrid(float cellSize) => _cell = cellSize;

    /// <summary>The cell size, in world units.</summary>
    public float CellSize => _cell;

    private static long Key(int x, int y) => ((long)x << 32) | (uint)y;

    private void Range(in RectF r, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = (int)MathF.Floor(r.Left / _cell);
        y0 = (int)MathF.Floor(r.Top / _cell);
        x1 = (int)MathF.Floor(r.Right / _cell);
        y1 = (int)MathF.Floor(r.Bottom / _cell);
    }

    private ref Entry EntryOf(int slot)
    {
        while (_entries.Count <= slot) _entries.Add(default);
        return ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_entries)[slot];
    }

    /// <summary>Files (or refiles) shape <paramref name="slot"/> under <paramref name="bounds"/>; nothing when its cells did not change.</summary>
    public void Update(int slot, in RectF bounds)
    {
        Range(bounds, out int x0, out int y0, out int x1, out int y1);
        bool large = (long)(x1 - x0 + 1) * (y1 - y0 + 1) > MaxCells;
        ref var e = ref EntryOf(slot);
        if (e.Filed && e.Large == large && (large || (e.X0 == x0 && e.Y0 == y0 && e.X1 == x1 && e.Y1 == y1))) return;
        Remove(slot);
        e = ref EntryOf(slot);
        e.Filed = true;
        e.Large = large;
        e.X0 = x0; e.Y0 = y0; e.X1 = x1; e.Y1 = y1;
        if (large)
        {
            _large.Add(slot);
            return;
        }
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            if (!_cells.TryGetValue(Key(x, y), out var list)) _cells[Key(x, y)] = list = [];
            list.Add(slot);
        }
    }

    /// <summary>Takes shape <paramref name="slot"/> out of the grid; nothing when it was not filed.</summary>
    public void Remove(int slot)
    {
        if (slot >= _entries.Count) return;
        ref var e = ref EntryOf(slot);
        if (!e.Filed) return;
        if (e.Large) _large.Remove(slot);
        else
            for (int y = e.Y0; y <= e.Y1; y++)
            for (int x = e.X0; x <= e.X1; x++)
            {
                // An emptied cell keeps its list: bodies walking back and forth do not allocate.
                _cells[Key(x, y)].Remove(slot);
            }
        e.Filed = false;
    }

    /// <summary>
    /// The filed shapes whose cells touch <paramref name="area"/> (a superset of those whose rectangle overlaps it), in
    /// ascending slot order. Valid until the next query.
    /// </summary>
    public List<int> Query(in RectF area)
    {
        _found.Clear();
        if (++_query == int.MaxValue)
        {
            Array.Clear(_stamp);
            _query = 1;
        }
        if (_stamp.Length < _entries.Count) Array.Resize(ref _stamp, Math.Max(_entries.Count, _stamp.Length * 2));
        foreach (int slot in _large) Mark(slot);
        Range(area, out int x0, out int y0, out int x1, out int y1);
        if ((long)(x1 - x0 + 1) * (y1 - y0 + 1) > (long)_cells.Count * 4)
        {
            // An area larger than the filed cells: walk the cells instead of the area.
            foreach (var (key, list) in _cells)
            {
                int x = (int)(key >> 32), y = (int)key;
                if (x < x0 || x > x1 || y < y0 || y > y1) continue;
                foreach (int slot in list) Mark(slot);
            }
        }
        else
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (_cells.TryGetValue(Key(x, y), out var list))
                    foreach (int slot in list) Mark(slot);
        _found.Sort();
        return _found;
    }

    private void Mark(int slot)
    {
        if (_stamp[slot] == _query) return;
        _stamp[slot] = _query;
        _found.Add(slot);
    }
}
