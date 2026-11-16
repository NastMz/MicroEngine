using MicroEngine.Scenes;

namespace MicroEngine.Physics.Tiles;

/// <summary>
/// The broad phase of <see cref="TilePhysicsWorld2D"/>: a uniform grid of square cells over the world's box shapes, so a
/// move, a query or a sensor looks only at the shapes near it instead of every shape of the world. The cells are a
/// <see cref="SpatialIndex2D{T}"/> of shape slots, which gives back the shapes whose rectangle touches the area asked
/// for and no other shape of the same cells; a shape that would cover more than <see cref="MaxCells"/> cells (a long
/// floor) is kept in a list every query includes. Tile grids are not filed: queries always include them. Candidates come
/// back in shape-slot order, the order of the full scan, so results are identical to scanning every shape.
/// </summary>
internal sealed class ShapeGrid
{
    /// <summary>The most cells a shape is filed in; larger shapes go to the list every query sees.</summary>
    public const int MaxCells = 64;

    // Beyond this no coordinate tells two places apart; an area's edges are brought within it so its size stays finite.
    private const float Farthest = 1e30f;

    private readonly float _cell;
    private readonly SpatialIndex2D<int> _index;
    private readonly List<int> _large = [];
    private readonly List<Entry> _entries = [];
    private readonly List<int> _found = [];

    private struct Entry
    {
        public bool Filed;
        public bool Large;
        public SpatialHandle Place;
    }

    /// <summary>A grid of <paramref name="cellSize"/> world units a side.</summary>
    public ShapeGrid(float cellSize)
    {
        _cell = cellSize;
        _index = new SpatialIndex2D<int>(cellSize);
    }

    /// <summary>The cell size, in world units.</summary>
    public float CellSize => _cell;

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

    /// <summary>Files (or refiles) shape <paramref name="slot"/> under <paramref name="bounds"/>.</summary>
    public void Update(int slot, in RectF bounds)
    {
        Range(bounds, out int x0, out int y0, out int x1, out int y1);
        // A rectangle the cells cannot hold (an edge that is not a number) is kept with the large ones: always a candidate.
        bool large = ((long)x1 - x0 + 1) * ((long)y1 - y0 + 1) > MaxCells
            || !(float.IsFinite(bounds.X) && float.IsFinite(bounds.Y) && bounds.Width >= 0 && bounds.Height >= 0 && float.IsFinite(bounds.Width) && float.IsFinite(bounds.Height));
        ref var e = ref EntryOf(slot);
        if (e.Filed && e.Large == large)
        {
            if (!large) _index.Move(e.Place, bounds);
            return;
        }
        Remove(slot);
        e = ref EntryOf(slot);
        e.Filed = true;
        e.Large = large;
        if (large) _large.Add(slot);
        else e.Place = _index.Add(slot, bounds);
    }

    /// <summary>Takes shape <paramref name="slot"/> out of the grid; nothing when it was not filed.</summary>
    public void Remove(int slot)
    {
        if (slot >= _entries.Count) return;
        ref var e = ref EntryOf(slot);
        if (!e.Filed) return;
        if (e.Large) _large.Remove(slot);
        else _index.Remove(e.Place);
        e.Filed = false;
    }

    /// <summary>
    /// The large shapes and the filed shapes whose rectangle overlaps or touches <paramref name="area"/> (a superset of
    /// those that overlap it), in ascending slot order. Valid until the next query.
    /// </summary>
    public List<int> Query(in RectF area)
    {
        _found.Clear();
        foreach (int slot in _large) _found.Add(slot);
        float left = Math.Clamp(area.Left, -Farthest, Farthest), top = Math.Clamp(area.Top, -Farthest, Farthest);
        float right = Math.Clamp(area.Right, -Farthest, Farthest), bottom = Math.Clamp(area.Bottom, -Farthest, Farthest);
        // An area with no inside (or with an edge that is not a number) reaches no filed shape.
        if (right >= left && bottom >= top) _index.Query(new RectF(left, top, Reaching(left, right), Reaching(top, bottom)), _found);
        if (_found.Count > 1) _found.Sort();
        return _found;
    }

    // A size that, added to the near edge, is not short of the far edge: the subtraction may round down, and a shape
    // that begins in the gap must still be a candidate.
    private static float Reaching(float near, float far)
    {
        float size = far - near;
        while (near + size < far) size = MathF.BitIncrement(size);
        return size;
    }
}
