using System.Numerics;
using System.Runtime.CompilerServices;

namespace MicroEngine.Scenes;

/// <summary>
/// An item of a <see cref="SpatialIndex2D{T}"/>: a slot index and the generation of the item that held the slot when
/// the handle was made. A removed item's slot is reused with a new generation, so an old handle never reaches the new
/// item. <c>default</c> is <see cref="None"/> (generation 0 is never issued).
/// </summary>
/// <param name="Index">The slot.</param>
/// <param name="Generation">The generation of the slot's item; 0 only for <see cref="None"/>.</param>
public readonly record struct SpatialHandle(uint Index, uint Generation)
{
    /// <summary>The handle of no item.</summary>
    public static SpatialHandle None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
    /// <inheritdoc/>
    public override string ToString() => IsNone ? "spatial:none" : $"spatial:{Index}#{Generation}";
}

/// <summary>
/// Finds what is near a place: items of the game's own (entity ids, objects, numbers), each filed under a point or a
/// rectangle of the 2D world, asked for by rectangle or by circle without walking every item. A game creates one for
/// what it needs to find (enemies for the towers, neighbours for a swarm, what is in view in a world much larger than
/// the screen), moves an item's place when the item moves and removes it when it goes. A world has none unless the
/// game makes one, and the physics world is not involved: items need no body.
/// </summary>
/// <typeparam name="T">What a query gives back: <see cref="EntityId"/>, an attachment, an index into the game's arrays.</typeparam>
/// <remarks>
/// The rules:
/// <list type="number">
/// <item><b>Places.</b> An item is a point or a rectangle with its edges included: a rectangle query finds every item
/// that overlaps or touches the area, a circle query every item with a point within the radius of the centre (a point
/// item: its distance is at most the radius).</item>
/// <item><b>Order.</b> Results come in an order that depends only on the calls made on the index: the same additions,
/// moves, removals and queries give the same results in the same order on every run and machine. It is not sorted by
/// distance or by handle.</item>
/// <item><b>Cost.</b> The index is a grid of square cells of <see cref="CellSize"/> units: a query looks at the cells
/// its area reaches, so choose a cell about the size of the usual query (the radius a tower sees, the view of a
/// camera divided by a few). An item that covers more than <see cref="MaxCells"/> cells is kept apart and tested by
/// every query. A move within the item's cells changes no cell.</item>
/// <item><b>Memory.</b> Nothing is allocated by a query or a removal; adding an item, or a move to more cells, allocates only when the
/// index grows.</item>
/// <item><b>Threads.</b> Not thread-safe. A query changes nothing, so several threads may query while none adds,
/// moves or removes.</item>
/// </list>
/// </remarks>
public sealed class SpatialIndex2D<T>
{
    /// <summary>The most cells an item is filed in; a larger one is kept in a list every query tests.</summary>
    public const int MaxCells = 64;

    // Cell numbers stay within ±2^30, so a place beyond that is filed in the outermost cell and cell counts fit a long.
    private const int Outermost = 1 << 30;
    private const byte Free = 0, Filed = 1, Large = 2;

    private struct Entry
    {
        public float X, Y, Width, Height;
        public int X0, Y0, X1, Y1;   // the cells a filed item covers
        public uint Generation;
        public int Link;             // filed: its first node; large: its place in _large; free: the next free slot
        public byte State;
    }

    // One per cell an item is filed in: a link of the cell's chain (through its bucket) and of the item's own chain.
    // It carries the item's edges, so a query reads the nodes of a cell and nothing else until an item matches.
    private struct Node
    {
        public float Left, Top, Right, Bottom;
        public int Item, CellX, CellY;
        public int Next, Previous;   // the chain of the bucket; a free node keeps the next free one in Next
        public int Sibling;          // the item's next cell
        public bool Several;         // the item is filed in more than one cell
    }

    private readonly float _cell, _inverse;
    private Entry[] _entries = new Entry[16];
    private T[] _items = new T[16];
    private int _used, _count, _freeEntry = -1;
    private Node[] _nodes = new Node[16];
    private int _nodesUsed, _nodeCount, _freeNode = -1;
    private int[] _buckets = NewBuckets(64);
    private readonly List<int> _large = [];

    /// <summary>An empty index with cells of <paramref name="cellSize"/> world units a side.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cellSize"/> is not positive and finite.</exception>
    public SpatialIndex2D(float cellSize)
    {
        if (!(cellSize > 0 && float.IsFinite(cellSize))) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "The cell size must be positive and finite.");
        _cell = cellSize;
        _inverse = 1 / cellSize;
    }

    /// <summary>The side of a cell, in world units.</summary>
    public float CellSize => _cell;
    /// <summary>The items in the index.</summary>
    public int Count => _count;

    // ------------------------------------------------------------------ items

    /// <summary>Adds <paramref name="item"/> at the point <paramref name="position"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is not finite.</exception>
    public SpatialHandle Add(T item, Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) throw new ArgumentOutOfRangeException(nameof(position), position, "The position must be finite.");
        return Add(item, new RectF(position.X, position.Y, 0, 0));
    }

    /// <summary>Adds <paramref name="item"/> under the rectangle <paramref name="bounds"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bounds"/> is not finite or has a negative size.</exception>
    public SpatialHandle Add(T item, in RectF bounds)
    {
        Check(bounds, nameof(bounds));
        int slot = _freeEntry;
        if (slot >= 0) _freeEntry = _entries[slot].Link;
        else
        {
            if (_used == _entries.Length)
            {
                Array.Resize(ref _entries, _used * 2);
                Array.Resize(ref _items, _used * 2);
            }
            slot = _used++;
        }
        ref var e = ref _entries[slot];
        if (e.Generation == 0) e.Generation = 1;
        _items[slot] = item;
        _count++;
        File(slot, bounds);
        return new SpatialHandle((uint)slot, e.Generation);
    }

    /// <summary>Moves the item to the point <paramref name="position"/>.</summary>
    /// <exception cref="InvalidOperationException">The handle is stale or none.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is not finite.</exception>
    public void Move(SpatialHandle handle, Vector2 position)
    {
        int slot = Resolve(handle);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) throw new ArgumentOutOfRangeException(nameof(position), position, "The position must be finite.");
        ref var e = ref _entries[slot];
        int x = Cell(position.X), y = Cell(position.Y);
        if (e.State == Filed && e.X0 == x && e.X1 == x && e.Y0 == y && e.Y1 == y)
        {
            // The usual move: within the cell the item is in.
            e.X = position.X; e.Y = position.Y; e.Width = 0; e.Height = 0;
            ref var node = ref _nodes[e.Link];
            node.Left = node.Right = position.X;
            node.Top = node.Bottom = position.Y;
            return;
        }
        Unfile(slot);
        File(slot, new RectF(position.X, position.Y, 0, 0));
    }

    /// <summary>Moves the item to the rectangle <paramref name="bounds"/>.</summary>
    /// <exception cref="InvalidOperationException">The handle is stale or none.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bounds"/> is not finite or has a negative size.</exception>
    public void Move(SpatialHandle handle, in RectF bounds)
    {
        int slot = Resolve(handle);
        Check(bounds, nameof(bounds));
        ref var e = ref _entries[slot];
        Cells(bounds, out int x0, out int y0, out int x1, out int y1);
        bool large = ((long)x1 - x0 + 1) * ((long)y1 - y0 + 1) > MaxCells;
        if (large ? e.State == Large : e.State == Filed && e.X0 == x0 && e.Y0 == y0 && e.X1 == x1 && e.Y1 == y1)
        {
            e.X = bounds.X; e.Y = bounds.Y; e.Width = bounds.Width; e.Height = bounds.Height;
            if (large) return;
            for (int n = e.Link; n >= 0; n = _nodes[n].Sibling)
            {
                ref var node = ref _nodes[n];
                node.Left = bounds.X; node.Top = bounds.Y; node.Right = bounds.X + bounds.Width; node.Bottom = bounds.Y + bounds.Height;
            }
            return;
        }
        Unfile(slot);
        File(slot, bounds);
    }

    /// <summary>Takes the item out; false for a stale or none handle (already removed).</summary>
    public bool Remove(SpatialHandle handle)
    {
        if (!Contains(handle)) return false;
        int slot = (int)handle.Index;
        Unfile(slot);
        Release(slot);
        _entries[slot].Link = _freeEntry;
        _freeEntry = slot;
        _count--;
        return true;
    }

    /// <summary>Whether <paramref name="handle"/> is an item of this index (added and not removed since).</summary>
    public bool Contains(SpatialHandle handle) =>
        handle.Index < (uint)_used && _entries[handle.Index].State != Free && _entries[handle.Index].Generation == handle.Generation;

    /// <summary>The item added under <paramref name="handle"/>.</summary>
    /// <exception cref="InvalidOperationException">The handle is stale or none.</exception>
    public T ItemOf(SpatialHandle handle) => _items[Resolve(handle)];

    /// <summary>Where the item is filed: its rectangle, of size zero for a point.</summary>
    /// <exception cref="InvalidOperationException">The handle is stale or none.</exception>
    public RectF BoundsOf(SpatialHandle handle)
    {
        ref var e = ref _entries[Resolve(handle)];
        return new RectF(e.X, e.Y, e.Width, e.Height);
    }

    /// <summary>Removes every item; every handle becomes stale. The index keeps its storage.</summary>
    public void Clear()
    {
        for (int slot = 0; slot < _used; slot++)
            if (_entries[slot].State != Free) Release(slot);
        _used = _count = 0;
        _freeEntry = -1;
        _nodesUsed = _nodeCount = 0;
        _freeNode = -1;
        Array.Fill(_buckets, -1);
        _large.Clear();
    }

    // The slot stops being an item: its handle goes stale and the index lets go of what it held.
    private void Release(int slot)
    {
        ref var e = ref _entries[slot];
        e.State = Free;
        e.Generation = e.Generation == uint.MaxValue ? 1 : e.Generation + 1;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) _items[slot] = default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Resolve(SpatialHandle handle)
    {
        uint slot = handle.Index;
        var entries = _entries;
        if (slot < (uint)_used && slot < (uint)entries.Length && entries[slot].Generation == handle.Generation && entries[slot].State != Free) return (int)slot;
        return ResolveFailed(handle);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ResolveFailed(SpatialHandle handle) => throw new InvalidOperationException(
        handle.IsNone ? "The handle is spatial:none: it refers to nothing." : $"{handle} is not an item of this index (removed, cleared or from another index).");

    private static void Check(in RectF bounds, string name)
    {
        if (!(float.IsFinite(bounds.X) && float.IsFinite(bounds.Y) && bounds.Width >= 0 && bounds.Height >= 0 && float.IsFinite(bounds.Width) && float.IsFinite(bounds.Height)))
            throw new ArgumentOutOfRangeException(name, bounds, "The rectangle must be finite and its size must not be negative.");
    }

    // ------------------------------------------------------------------ cells

    private static int[] NewBuckets(int size)
    {
        var buckets = new int[size];
        Array.Fill(buckets, -1);
        return buckets;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Cell(float v)
    {
        float c = MathF.Floor(v * _inverse);
        return c >= Outermost ? Outermost : c <= -Outermost ? -Outermost : (int)c;
    }

    private void Cells(in RectF r, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = Cell(r.X);
        y0 = Cell(r.Y);
        x1 = Cell(r.X + r.Width);
        y1 = Cell(r.Y + r.Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Bucket(int x, int y, int mask)
    {
        uint h = (uint)x * 0x9E3779B1u ^ (uint)y * 0x85EBCA77u;
        return (int)((h ^ (h >> 15)) & (uint)mask);
    }

    private void File(int slot, in RectF bounds)
    {
        ref var e = ref _entries[slot];
        e.X = bounds.X; e.Y = bounds.Y; e.Width = bounds.Width; e.Height = bounds.Height;
        Cells(bounds, out e.X0, out e.Y0, out e.X1, out e.Y1);
        long cells = ((long)e.X1 - e.X0 + 1) * ((long)e.Y1 - e.Y0 + 1);
        if (cells > MaxCells)
        {
            e.State = Large;
            e.Link = _large.Count;
            _large.Add(slot);
            return;
        }
        e.State = Filed;
        e.Link = -1;   // before the buckets grow: growing walks every filed item's nodes, and this one has none yet
        if (_nodeCount + (int)cells > _buckets.Length) Rehash(Math.Max(_buckets.Length * 2, (int)BitOperations.RoundUpToPowerOf2((uint)(_nodeCount + (int)cells))));
        int last = -1;
        for (int y = e.Y0; y <= e.Y1; y++)
        for (int x = e.X0; x <= e.X1; x++)
        {
            int n = _freeNode;
            if (n >= 0) _freeNode = _nodes[n].Next;
            else
            {
                if (_nodesUsed == _nodes.Length) Array.Resize(ref _nodes, _nodesUsed * 2);
                n = _nodesUsed++;
            }
            ref var node = ref _nodes[n];
            node.Left = bounds.X; node.Top = bounds.Y; node.Right = bounds.X + bounds.Width; node.Bottom = bounds.Y + bounds.Height;
            node.Item = slot;
            node.CellX = x;
            node.CellY = y;
            node.Sibling = -1;
            node.Several = cells > 1;
            Chain(n, ref node);
            if (last < 0) _entries[slot].Link = n; else _nodes[last].Sibling = n;
            last = n;
            _nodeCount++;
        }
    }

    // Puts a node at the head of its cell's bucket.
    private void Chain(int n, ref Node node)
    {
        ref int head = ref _buckets[Bucket(node.CellX, node.CellY, _buckets.Length - 1)];
        node.Previous = -1;
        node.Next = head;
        if (head >= 0) _nodes[head].Previous = n;
        head = n;
    }

    private void Unfile(int slot)
    {
        ref var e = ref _entries[slot];
        if (e.State == Large)
        {
            // The last large item takes the place of the one that leaves.
            int moved = _large[^1];
            _large[e.Link] = moved;
            _entries[moved].Link = e.Link;
            _large.RemoveAt(_large.Count - 1);
            return;
        }
        for (int n = e.Link; n >= 0;)
        {
            ref var node = ref _nodes[n];
            if (node.Previous >= 0) _nodes[node.Previous].Next = node.Next;
            else _buckets[Bucket(node.CellX, node.CellY, _buckets.Length - 1)] = node.Next;
            if (node.Next >= 0) _nodes[node.Next].Previous = node.Previous;
            int sibling = node.Sibling;
            node.Next = _freeNode;
            _freeNode = n;
            _nodeCount--;
            n = sibling;
        }
    }

    // More buckets: every filed item's nodes are chained again, in slot order.
    private void Rehash(int size)
    {
        _buckets = NewBuckets(size);
        for (int slot = 0; slot < _used; slot++)
        {
            if (_entries[slot].State != Filed) continue;
            for (int n = _entries[slot].Link; n >= 0; n = _nodes[n].Sibling) Chain(n, ref _nodes[n]);
        }
    }

    // ------------------------------------------------------------------ queries

    /// <summary>
    /// Writes into <paramref name="results"/> the items that overlap or touch <paramref name="area"/> and returns how
    /// many do (which may exceed the span's length: only the first ones are written).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="area"/> is not finite or has a negative size.</exception>
    public int Query(in RectF area, Span<T> results)
    {
        Check(area, nameof(area));
        return Collect(area.X, area.Y, area.X + area.Width, area.Y + area.Height, circle: false, default, 0, results, null);
    }

    /// <summary>Adds to <paramref name="results"/> (without clearing it) the items that overlap or touch <paramref name="area"/> and returns how many were added.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="area"/> is not finite or has a negative size.</exception>
    public int Query(in RectF area, List<T> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        Check(area, nameof(area));
        return Collect(area.X, area.Y, area.X + area.Width, area.Y + area.Height, circle: false, default, 0, default, results);
    }

    /// <summary>
    /// Writes into <paramref name="results"/> the items with a point within <paramref name="radius"/> of
    /// <paramref name="center"/> and returns how many there are (which may exceed the span's length: only the first
    /// ones are written).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="center"/> is not finite, or <paramref name="radius"/> is negative or not finite.</exception>
    public int QueryCircle(Vector2 center, float radius, Span<T> results)
    {
        float reach = Reach(center, radius);
        return Collect(center.X - reach, center.Y - reach, center.X + reach, center.Y + reach, circle: true, center, radius * radius, results, null);
    }

    /// <summary>Adds to <paramref name="results"/> (without clearing it) the items with a point within <paramref name="radius"/> of <paramref name="center"/> and returns how many were added.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="center"/> is not finite, or <paramref name="radius"/> is negative or not finite.</exception>
    public int QueryCircle(Vector2 center, float radius, List<T> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        float reach = Reach(center, radius);
        return Collect(center.X - reach, center.Y - reach, center.X + reach, center.Y + reach, circle: true, center, radius * radius, default, results);
    }

    // How far from the centre the cells of a circle query are looked at: the radius and a margin for the rounding of
    // the distance, so an item at exactly the radius is never in a cell the query skipped.
    private static float Reach(Vector2 center, float radius)
    {
        if (!float.IsFinite(center.X) || !float.IsFinite(center.Y)) throw new ArgumentOutOfRangeException(nameof(center), center, "The centre must be finite.");
        if (!(radius >= 0 && float.IsFinite(radius))) throw new ArgumentOutOfRangeException(nameof(radius), radius, "The radius must be finite and not negative.");
        return radius + (MathF.Max(MathF.Abs(center.X), MathF.Abs(center.Y)) + radius) * 1e-5f;
    }

    // The items whose rectangle touches the rectangle of the four edges (a circle query: whose nearest point is within
    // the radius), from the large items and then from the cells the edges reach.
    private int Collect(float left, float top, float right, float bottom, bool circle, Vector2 center, float radiusSquared, Span<T> span, List<T>? list)
    {
        var entries = _entries;
        var items = _items;
        int count = 0;
        foreach (int slot in _large)
        {
            ref var e = ref entries[slot];
            if (!Matches(e.X, e.Y, e.X + e.Width, e.Y + e.Height, left, top, right, bottom, circle, center, radiusSquared)) continue;
            if (list is not null) list.Add(items[slot]);
            else if (count < span.Length) span[count] = items[slot];
            count++;
        }

        int x0 = Cell(left), y0 = Cell(top), x1 = Cell(right), y1 = Cell(bottom);
        if (((long)x1 - x0 + 1) * ((long)y1 - y0 + 1) > _used)
        {
            // More cells than items: walk the items instead of the cells.
            for (int slot = 0; slot < _used; slot++)
            {
                ref var e = ref entries[slot];
                if (e.State != Filed || !Matches(e.X, e.Y, e.X + e.Width, e.Y + e.Height, left, top, right, bottom, circle, center, radiusSquared)) continue;
                if (list is not null) list.Add(items[slot]);
                else if (count < span.Length) span[count] = items[slot];
                count++;
            }
            return count;
        }
        var nodes = _nodes;
        var buckets = _buckets;
        int mask = buckets.Length - 1;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
            for (int n = buckets[Bucket(x, y, mask)]; n >= 0; n = nodes[n].Next)
            {
                ref var node = ref nodes[n];
                if (node.CellX != x || node.CellY != y) continue;
                if (!Matches(node.Left, node.Top, node.Right, node.Bottom, left, top, right, bottom, circle, center, radiusSquared)) continue;
                // An item over several cells is reported from the first of them the area reaches.
                if (node.Several && (x != Math.Max(entries[node.Item].X0, x0) || y != Math.Max(entries[node.Item].Y0, y0))) continue;
                if (list is not null) list.Add(items[node.Item]);
                else if (count < span.Length) span[count] = items[node.Item];
                count++;
            }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Matches(float l, float t, float r, float b, float left, float top, float right, float bottom, bool circle, Vector2 center, float radiusSquared)
    {
        if (!circle) return l <= right && r >= left && t <= bottom && b >= top;
        float dx = MathF.Max(MathF.Max(l - center.X, center.X - r), 0), dy = MathF.Max(MathF.Max(t - center.Y, center.Y - b), 0);
        return dx * dx + dy * dy <= radiusSquared;
    }
}
