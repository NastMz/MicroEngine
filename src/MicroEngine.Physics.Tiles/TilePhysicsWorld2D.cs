using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace MicroEngine.Physics.Tiles;

/// <summary>
/// The tile backend of <see cref="IPhysicsWorld2D"/>: static and kinematic bodies with axis-aligned box shapes, tile grids
/// as static geometry (<see cref="AddTiles(BodyId, ICollisionGrid, ulong, ulong)"/>), and an exact swept mover. It declares
/// <see cref="PhysicsCapabilities.TileGeometry"/>, <see cref="PhysicsCapabilities.OneWayPlatforms"/> and
/// <see cref="PhysicsCapabilities.MovingSolids"/>; it has no dynamic bodies, so <see cref="ContactEvents"/> is always
/// empty, and every other capability throws <see cref="PhysicsCapabilityException"/>.
/// </summary>
/// <remarks>
/// <para>Space is the engine's 2D space: Y grows downward and <see cref="Up"/> is (0, -1); units are whatever the game
/// uses (pixels in a pixel-art game). <see cref="Move"/> resolves X, then Y, each as one exact swept test: the box stops
/// flush against the first surface in its way however thin it is or however fast the box moves, and rests on it exactly
/// (<see cref="Skin"/> is 0; a flush box may end within <c>0.001</c> units inside a surface from rounding, which never
/// counts as overlapping).</para>
/// <para>A tile grid is one shape made of cells: its <see cref="TileFlags.Solid"/> cells block from every side, its
/// <see cref="TileFlags.OneWay"/> cells are one-way platforms, and other cells are not geometry (read hazards and water
/// with <see cref="TileMotion.TouchedFlags"/>). Changing a tile changes collisions at once. Cells a box overlaps when a
/// move starts do not block it.</para>
/// <para>Deterministic: the same calls give the same results on every target (managed code with no threads). The world
/// takes part in the per-step state hash (<see cref="IStateHashed"/>).</para>
/// <para>A broad phase keeps moves, queries and sensors from looking at every shape: box shapes are filed in a grid of
/// <see cref="DefaultCellSize"/>-unit cells, and each operation looks only at the shapes in the cells it reaches (tile
/// grids are always looked at). It changes the work, never a result: candidates are taken in creation-slot order, the
/// order of a full scan.</para>
/// </remarks>
public sealed class TilePhysicsWorld2D : IPhysicsWorld2D, IStateHashed
{
    /// <summary>The capabilities this backend declares.</summary>
    public const PhysicsCapabilities Declared = PhysicsCapabilities.TileGeometry | PhysicsCapabilities.OneWayPlatforms | PhysicsCapabilities.MovingSolids
        | PhysicsCapabilities.ShapeInspection | PhysicsCapabilities.ShapeQueries | PhysicsCapabilities.FilterPredicates;

    /// <summary>The distance within which a surface counts as touching a box at the end of a move (the ground probe).</summary>
    public const float ContactDistance = 0.01f;

    /// <summary>The side of the broad phase's cells, in world units (four 16-pixel tiles).</summary>
    public const float DefaultCellSize = 64;

    /// <summary>How far beyond what a move sweeps the broad phase looks: more than every reach of the mover (the ground probe, the sweep's rounding allowance, the one-way slack).</summary>
    private const float BroadPhaseMargin = 0.1f;

    private const string Name = "tiles";

    private sealed class Body
    {
        public uint Generation;
        public bool Alive;
        public EntityId Owner;
        public BodyKind Kind;
        public Vector2 Position;
        public bool Pushes;
        public readonly List<int> Shapes = [];
    }

    private sealed class Shape
    {
        public uint Generation;
        public bool Alive;
        public int Body;
        public ShapeDescription Description;
        public ICollisionGrid? Grid;
        public int Columns, Rows; // the level's area, when the tiles were added with one
    }

    private readonly record struct Pair(ShapeId Sensor, ShapeId Visitor, EntityId SensorOwner, EntityId VisitorOwner) : IComparable<Pair>
    {
        public int CompareTo(Pair other)
        {
            int c = Sensor.Index.CompareTo(other.Sensor.Index);
            if (c == 0) c = Sensor.Generation.CompareTo(other.Sensor.Generation);
            if (c == 0) c = Visitor.Index.CompareTo(other.Visitor.Index);
            if (c == 0) c = Visitor.Generation.CompareTo(other.Visitor.Generation);
            return c;
        }
        public bool SameShapes(in Pair other) => Sensor == other.Sensor && Visitor == other.Visitor;
    }

    private readonly List<Body> _bodySlots = [];
    private readonly Stack<int> _freeBodies = new();
    private readonly List<Shape> _shapeSlots = [];
    private readonly Stack<int> _freeShapes = new();
    private BodyId[] _live = new BodyId[16];
    private int _liveCount;

    private List<Pair> _pairs = [];
    private List<Pair> _nextPairs = [];
    private readonly List<SensorEvent> _sensorEvents = [];
    private readonly List<GridObstacle> _grids = [];
    private readonly List<BoxObstacle> _boxes = [];
    private readonly List<int> _touching = [];
    private readonly List<MoveContact> _contacts = [];
    private readonly ShapeGrid _broad;
    private readonly bool _fullScan;
    private readonly List<int> _gridSlots = []; // live tile-grid shapes, ascending slot order
    private readonly List<int> _candidates = [];

    /// <summary>Creates an empty world.</summary>
    public TilePhysicsWorld2D() : this(DefaultCellSize, fullScan: false) { }

    /// <summary>A world whose broad phase has cells of <paramref name="cellSize"/> units, or none (every operation scans every shape).</summary>
    internal TilePhysicsWorld2D(float cellSize, bool fullScan)
    {
        if (!(cellSize > 0 && float.IsFinite(cellSize))) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "The cell size must be positive and finite.");
        _broad = new ShapeGrid(cellSize);
        _fullScan = fullScan;
    }

    /// <inheritdoc/>
    public string Backend => Name;
    /// <inheritdoc/>
    public PhysicsCapabilities Capabilities => Declared;
    /// <summary>0: <see cref="Move"/> rests exactly on surfaces.</summary>
    public float Skin => 0;
    /// <summary>(0, -1): Y grows downward.</summary>
    public Vector2 Up => new(0, -1);
    /// <inheritdoc/>
    public long StepCount { get; private set; }
    /// <inheritdoc/>
    public ReadOnlySpan<BodyId> Bodies => _live.AsSpan(0, _liveCount);
    /// <summary>Always empty: this backend has no dynamic bodies.</summary>
    public ReadOnlySpan<ContactEvent> ContactEvents => [];
    /// <summary>The sensor entries and exits of the latest step: exits first, then entries, each in sensor then visitor order.</summary>
    public ReadOnlySpan<SensorEvent> SensorEvents => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_sensorEvents);

    // ------------------------------------------------------------------ bodies

    /// <inheritdoc/>
    public BodyId CreateBody(EntityId owner, in BodyDescription body)
    {
        body.Validate();
        PhysicsCapabilityException.ThrowIfMissing(Declared, body.RequiredCapabilities, $"CreateBody({body.Kind})", Name);
        int slot;
        if (_freeBodies.Count > 0) slot = _freeBodies.Pop();
        else
        {
            slot = _bodySlots.Count;
            _bodySlots.Add(new Body());
        }
        var b = _bodySlots[slot];
        b.Generation++;
        b.Alive = true;
        b.Owner = owner;
        b.Kind = body.Kind;
        b.Position = body.Position;
        b.Pushes = body.PushesBodies;
        b.Shapes.Clear();
        var id = new BodyId((uint)slot, b.Generation);
        if (_liveCount == _live.Length) Array.Resize(ref _live, _live.Length * 2);
        _live[_liveCount++] = id;
        return id;
    }

    /// <inheritdoc/>
    public void DestroyBody(BodyId body)
    {
        if (!IsAlive(body)) return;
        var b = _bodySlots[(int)body.Index];
        foreach (int s in b.Shapes) KillShape(s);
        b.Shapes.Clear();
        b.Alive = false;
        int at = Array.IndexOf(_live, body, 0, _liveCount);
        Array.Copy(_live, at + 1, _live, at, _liveCount - at - 1);
        _liveCount--;
        _freeBodies.Push((int)body.Index);
    }

    /// <inheritdoc/>
    public bool IsAlive(BodyId body) =>
        body.Generation != 0 && body.Index < (uint)_bodySlots.Count && _bodySlots[(int)body.Index] is { Alive: true } b && b.Generation == body.Generation;

    /// <inheritdoc/>
    public EntityId OwnerOf(BodyId body) => Resolve(body).Owner;
    /// <inheritdoc/>
    public BodyKind KindOf(BodyId body) => Resolve(body).Kind;
    /// <inheritdoc/>
    public Vector2 PositionOf(BodyId body) => Resolve(body).Position;
    /// <summary>Always 0: this backend has no rotation.</summary>
    public float RotationOf(BodyId body)
    {
        Resolve(body);
        return 0;
    }

    /// <inheritdoc/>
    public void SetPosition(BodyId body, Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) throw new ArgumentOutOfRangeException(nameof(position), position, "The position must be finite.");
        var b = Resolve(body);
        b.Position = position;
        Refile(b);
    }

    /// <summary>Always throws: this backend has no dynamic bodies.</summary>
    /// <exception cref="PhysicsCapabilityException">Always.</exception>
    public Vector2 VelocityOf(BodyId body) => throw new PhysicsCapabilityException("VelocityOf", Name, PhysicsCapabilities.DynamicBodies);
    /// <summary>Always throws: this backend has no dynamic bodies.</summary>
    /// <exception cref="PhysicsCapabilityException">Always.</exception>
    public void SetVelocity(BodyId body, Vector2 velocity) => throw new PhysicsCapabilityException("SetVelocity", Name, PhysicsCapabilities.DynamicBodies);

    // ------------------------------------------------------------------ shapes

    /// <inheritdoc/>
    public ShapeId AddShape(BodyId body, in ShapeDescription shape)
    {
        shape.Validate();
        PhysicsCapabilityException.ThrowIfMissing(Declared, shape.RequiredCapabilities, $"AddShape({shape.Kind})", Name);
        var b = Resolve(body);
        return NewShape((int)body.Index, b, shape, grid: null);
    }

    /// <summary>
    /// Attaches <paramref name="grid"/> to a static body as one shape (<see cref="PhysicsCapabilities.TileGeometry"/>):
    /// cell (x, y) covers the body's origin plus (x, y) × <see cref="ICollisionGrid.TileSize"/>. Solid cells block, one-way
    /// cells are one-way platforms; the grid is read at every query, so changing a tile changes collisions at once.
    /// </summary>
    /// <param name="body">A static body.</param>
    /// <param name="grid">The tiles.</param>
    /// <param name="category">The layers the grid is in.</param>
    /// <param name="mask">The layers it collides with.</param>
    /// <exception cref="InvalidOperationException">The body is stale or not static.</exception>
    public ShapeId AddTiles(BodyId body, ICollisionGrid grid, ulong category = CollisionLayers.Default, ulong mask = CollisionLayers.All)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var b = Resolve(body);
        if (b.Kind != BodyKind.Static) throw new InvalidOperationException($"Tiles are static geometry: {body} is {b.Kind}.");
        var description = ShapeDescription.Box(new Vector2(grid.TileSize)).InLayers(category, mask) with { SensorEvents = false };
        return NewShape((int)body.Index, b, description, grid);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The grid is read at every query, inside and outside <paramref name="columns"/> × <paramref name="rows"/>. Slope
    /// tiles are not geometry on this backend: the area and one tile around it are looked at once, here, and a slope
    /// tile in them is refused (<see cref="PhysicsCapabilities.TileSlopes"/>); one that appears later is empty space.
    /// </remarks>
    /// <exception cref="PhysicsCapabilityException">A tile of the area is a slope.</exception>
    public ShapeId AddTiles(BodyId body, ICollisionGrid grid, int columns, int rows, ulong category = CollisionLayers.Default, ulong mask = CollisionLayers.All)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentNullException.ThrowIfNull(grid);
        // Slope tiles are not geometry here: a level that holds one is refused, not played without its slopes.
        for (int y = -1; y <= rows; y++)
        for (int x = -1; x <= columns; x++)
            if ((grid.FlagsAt(x, y) & (TileFlags.SlopeRight | TileFlags.SlopeLeft)) != 0)
                throw new PhysicsCapabilityException($"AddTiles (a slope tile at {x}, {y})", Name, PhysicsCapabilities.TileSlopes);
        var id = AddTiles(body, grid, category, mask);
        _shapeSlots[(int)id.Index].Columns = columns;
        _shapeSlots[(int)id.Index].Rows = rows;
        return id;
    }

    private ShapeId NewShape(int bodySlot, Body b, in ShapeDescription description, ICollisionGrid? grid)
    {
        int slot;
        if (_freeShapes.Count > 0) slot = _freeShapes.Pop();
        else
        {
            slot = _shapeSlots.Count;
            _shapeSlots.Add(new Shape());
        }
        var s = _shapeSlots[slot];
        s.Generation++;
        s.Alive = true;
        s.Body = bodySlot;
        s.Description = description;
        s.Grid = grid;
        s.Columns = s.Rows = 0;
        b.Shapes.Add(slot);
        if (grid is not null)
        {
            int at = _gridSlots.BinarySearch(slot);
            _gridSlots.Insert(at < 0 ? ~at : at, slot);
        }
        else _broad.Update(slot, BoxOf(b.Position, description));
        return new ShapeId((uint)slot, s.Generation);
    }

    /// <inheritdoc/>
    public void RemoveShape(ShapeId shape)
    {
        if (!IsAlive(shape)) return;
        _bodySlots[_shapeSlots[(int)shape.Index].Body].Shapes.Remove((int)shape.Index);
        KillShape((int)shape.Index);
    }

    private void KillShape(int slot)
    {
        var s = _shapeSlots[slot];
        if (s.Grid is not null) _gridSlots.Remove(slot);
        else _broad.Remove(slot);
        s.Alive = false;
        s.Grid = null;
        _freeShapes.Push(slot);
    }

    /// <summary>Refiles the box shapes of a body that moved.</summary>
    private void Refile(Body b)
    {
        foreach (int s in b.Shapes)
        {
            var shape = _shapeSlots[s];
            if (shape.Grid is null) _broad.Update(s, BoxOf(b.Position, shape.Description));
        }
    }

    /// <summary>
    /// The shape slots an operation over <paramref name="area"/> may concern, in ascending slot order: every slot for a
    /// full scan, else the tile grids and the box shapes filed in the cells the area reaches. Valid until the next call.
    /// </summary>
    private List<int> Candidates(in RectF area)
    {
        _candidates.Clear();
        if (_fullScan)
        {
            for (int i = 0; i < _shapeSlots.Count; i++) _candidates.Add(i);
            return _candidates;
        }
        var boxes = _broad.Query(area);
        int g = 0, k = 0;
        while (g < _gridSlots.Count || k < boxes.Count)
        {
            if (k == boxes.Count || (g < _gridSlots.Count && _gridSlots[g] < boxes[k])) _candidates.Add(_gridSlots[g++]);
            else _candidates.Add(boxes[k++]);
        }
        return _candidates;
    }

    private static RectF Grow(RectF r, float by) => new(r.X - by, r.Y - by, r.Width + 2 * by, r.Height + 2 * by);

    /// <inheritdoc/>
    public bool IsAlive(ShapeId shape) =>
        shape.Generation != 0 && shape.Index < (uint)_shapeSlots.Count && _shapeSlots[(int)shape.Index] is { Alive: true } s && s.Generation == shape.Generation;

    /// <inheritdoc/>
    public int ShapesOf(BodyId body, Span<ShapeId> results)
    {
        var b = Resolve(body);
        for (int i = 0; i < b.Shapes.Count && i < results.Length; i++)
            results[i] = new ShapeId((uint)b.Shapes[i], _shapeSlots[b.Shapes[i]].Generation);
        return b.Shapes.Count;
    }

    /// <inheritdoc/>
    public ShapeDescription DescriptionOf(ShapeId shape)
    {
        if (!IsAlive(shape)) throw new InvalidOperationException($"{shape} is not a live shape of this world.");
        return _shapeSlots[(int)shape.Index].Description;
    }

    /// <inheritdoc/>
    public ICollisionGrid? TilesOf(ShapeId shape)
    {
        if (!IsAlive(shape)) throw new InvalidOperationException($"{shape} is not a live shape of this world.");
        return _shapeSlots[(int)shape.Index].Grid;
    }

    /// <summary>The world bounds of <paramref name="shape"/>: a box's rectangle; for tiles, the area they were added with (empty at the body's origin when they were added without one).</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    public RectF BoundsOf(ShapeId shape)
    {
        if (!IsAlive(shape)) throw new InvalidOperationException($"{shape} is not a live shape of this world.");
        var s = _shapeSlots[(int)shape.Index];
        var origin = _bodySlots[s.Body].Position;
        return s.Grid is null ? BoxOf(origin, s.Description) : new RectF(origin.X, origin.Y, s.Columns * s.Grid.TileSize, s.Rows * s.Grid.TileSize);
    }

    /// <inheritdoc/>
    public BodyId BodyOf(ShapeId shape)
    {
        if (!IsAlive(shape)) throw new InvalidOperationException($"{shape} is not a live shape of this world.");
        int b = _shapeSlots[(int)shape.Index].Body;
        return new BodyId((uint)b, _bodySlots[b].Generation);
    }

    private ShapeId IdOf(int slot) => new((uint)slot, _shapeSlots[slot].Generation);

    private Body Resolve(BodyId body)
    {
        if (!IsAlive(body)) throw new InvalidOperationException($"{body} is not a live body of this world (destroyed, stale or none).");
        return _bodySlots[(int)body.Index];
    }

    private static RectF BoxOf(Vector2 position, in ShapeDescription d) =>
        new(position.X + d.Offset.X - d.Size.X / 2, position.Y + d.Offset.Y - d.Size.Y / 2, d.Size.X, d.Size.Y);

    // ------------------------------------------------------------------ moves

    /// <inheritdoc/>
    public MoveResult Move(BodyId body, Vector2 delta, in QueryFilter filter = default)
    {
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y)) throw new ArgumentOutOfRangeException(nameof(delta), delta, "The movement must be finite.");
        var b = Resolve(body);
        if (b.Kind != BodyKind.Kinematic) throw new InvalidOperationException($"Only kinematic bodies are moved with Move: {body} is {b.Kind}.");
        int mover = MoverShape(body, b);
        if (b.Pushes) return MoveSolid((int)body.Index, b, mover, delta, filter);
        return Slide((int)body.Index, b, mover, delta, filter, exclude: -1);
    }

    private int MoverShape(BodyId id, Body b)
    {
        int mover = -1;
        foreach (int s in b.Shapes)
        {
            if (_shapeSlots[s].Description.IsSensor) continue;
            if (mover >= 0) throw new InvalidOperationException($"{id} has more than one non-sensor shape; Move moves one box.");
            mover = s;
        }
        if (mover < 0) throw new InvalidOperationException($"{id} has no non-sensor shape to move.");
        return mover;
    }

    private MoveResult Slide(int bodySlot, Body b, int mover, Vector2 delta, in QueryFilter filter, int exclude)
    {
        var description = _shapeSlots[mover].Description;
        var start = BoxOf(b.Position, description);
        var reach = TileSweep.Union(TileSweep.Union(start, start.Offset(delta.X, 0)), start.Offset(delta.X, delta.Y));
        Gather(bodySlot, filter, exclude, Grow(reach, BroadPhaseMargin));
        var grids = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_grids);
        var boxes = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_boxes);
        bool passOneWay = filter.PassOneWay;
        _contacts.Clear();

        var box = BoxOf(b.Position, description);
        var position = b.Position;

        box = TileSweep.Sweep(box, delta.X, horizontal: true, grids, boxes, passOneWay, compatible: false, out bool blockedX, out int hitX);
        if (blockedX)
        {
            position.X = box.X - description.Offset.X + description.Size.X / 2;
            AddContact(hitX, new Vector2(-MathF.Sign(delta.X), 0), stopped: true);
        }
        else position.X += delta.X;

        box = TileSweep.Sweep(box, delta.Y, horizontal: false, grids, boxes, passOneWay, compatible: false, out bool blockedY, out int hitY);
        if (blockedY)
        {
            position.Y = box.Y - description.Offset.Y + description.Size.Y / 2;
            AddContact(hitY, new Vector2(0, -MathF.Sign(delta.Y)), stopped: true);
        }
        else position.Y += delta.Y;

        // What the box touches where it ends: below (ground), above, left, right.
        Probe(box, ContactDistance, horizontal: false, grids, boxes, passOneWay, new Vector2(0, -1));
        Probe(box, -ContactDistance, horizontal: false, grids, boxes, passOneWay, new Vector2(0, 1));
        Probe(box, -ContactDistance, horizontal: true, grids, boxes, passOneWay, new Vector2(1, 0));
        Probe(box, ContactDistance, horizontal: true, grids, boxes, passOneWay, new Vector2(-1, 0));

        var moved = position - b.Position;
        b.Position = position;
        Refile(b);
        return new MoveResult(position, moved, Up, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_contacts));
    }

    private void Probe(RectF box, float d, bool horizontal, ReadOnlySpan<GridObstacle> grids, ReadOnlySpan<BoxObstacle> boxes, bool passOneWay, Vector2 normal)
    {
        _touching.Clear();
        TileSweep.Touching(box, d, horizontal, grids, boxes, passOneWay, _touching);
        foreach (int source in _touching) AddContact(source, normal);
    }

    private void AddContact(int shapeSlot, Vector2 normal, bool stopped = false)
    {
        var id = IdOf(shapeSlot);
        foreach (var c in _contacts)
            if (c.Shape == id && c.Normal == normal) return;
        _contacts.Add(new MoveContact(id, _bodySlots[_shapeSlots[shapeSlot].Body].Owner, normal) { Stopped = stopped });
    }

    /// <summary>The non-sensor shapes of other bodies <paramref name="filter"/> accepts that may reach <paramref name="area"/>, as obstacles.</summary>
    private void Gather(int bodySlot, in QueryFilter filter, int exclude, in RectF area)
    {
        _grids.Clear();
        _boxes.Clear();
        foreach (int i in Candidates(area))
        {
            var s = _shapeSlots[i];
            if (!s.Alive || s.Body == bodySlot || s.Body == exclude || s.Description.IsSensor) continue;
            if (!filter.Accepts(s.Description.Category, s.Description.Mask) || !filter.Considers(IdOf(i))) continue;
            var owner = _bodySlots[s.Body];
            if (s.Grid is not null) _grids.Add(new GridObstacle(s.Grid, owner.Position, i));
            else _boxes.Add(new BoxObstacle(BoxOf(owner.Position, s.Description), s.Description.OneWay, i));
        }
    }

    /// <summary>A body that pushes: moves the whole way, then pushes and carries the kinematic bodies its filter accepts.</summary>
    private MoveResult MoveSolid(int bodySlot, Body b, int mover, Vector2 delta, in QueryFilter filter)
    {
        var description = _shapeSlots[mover].Description;
        var start = b.Position;
        foreach (bool horizontal in (ReadOnlySpan<bool>)[true, false])
        {
            float d = horizontal ? delta.X : delta.Y;
            if (d == 0) continue;
            var before = BoxOf(b.Position, description);
            // Riders: kinematic bodies resting on top of the solid, found before it moves.
            List<int>? riders = null;
            for (int i = 0; i < _liveCount; i++)
            {
                int other = (int)_live[i].Index;
                if (!Pushable(other, bodySlot, filter, out _, out var riderBox)) continue;
                if (MathF.Abs(riderBox.Bottom - before.Top) <= ContactDistance && riderBox.Right > before.Left && riderBox.Left < before.Right)
                    (riders ??= []).Add(other);
            }
            b.Position += horizontal ? new Vector2(d, 0) : new Vector2(0, d);
            Refile(b);
            var after = BoxOf(b.Position, description);
            var swept = TileSweep.Union(before, after);
            for (int i = 0; i < _liveCount; i++)
            {
                int other = (int)_live[i].Index;
                if (!Pushable(other, bodySlot, filter, out var rider, out var box)) continue;
                var shape = _shapeSlots[rider];
                var riderFilter = QueryFilter.For(shape.Description);
                Vector2 push;
                if (TileSweep.Hits(box, swept, TileSweep.Tolerance))
                {
                    // In the solid's way (where it ends or what it swept through): out of it, flush with its far side.
                    float amount = horizontal
                        ? (d > 0 ? after.Right - box.Left : after.Left - box.Right)
                        : (d > 0 ? after.Bottom - box.Top : after.Top - box.Bottom);
                    push = horizontal ? new Vector2(amount, 0) : new Vector2(0, amount);
                }
                else if (riders is not null && riders.Contains(other)) push = horizontal ? new Vector2(d, 0) : new Vector2(0, d);
                else continue;
                Slide(other, _bodySlots[other], rider, push, riderFilter, exclude: bodySlot);
            }
        }
        _contacts.Clear();
        return new MoveResult(b.Position, b.Position - start, Up, []);
    }

    /// <summary>Whether body <paramref name="slot"/> is a kinematic body the solid moves: not a pusher, with one movable shape its filter accepts.</summary>
    private bool Pushable(int slot, int solid, in QueryFilter filter, out int shape, out RectF box)
    {
        shape = -1;
        box = default;
        var o = _bodySlots[slot];
        if (slot == solid || o.Kind != BodyKind.Kinematic || o.Pushes) return false;
        foreach (int s in o.Shapes)
        {
            if (_shapeSlots[s].Description.IsSensor) continue;
            if (shape >= 0) return false;
            shape = s;
        }
        if (shape < 0) return false;
        var d = _shapeSlots[shape].Description;
        if (!filter.Accepts(d.Category, d.Mask) || !filter.Considers(IdOf(shape))) return false;
        box = BoxOf(o.Position, d);
        return true;
    }

    // ------------------------------------------------------------------ queries

    /// <inheritdoc/>
    public bool RayCast(Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
    {
        if (!float.IsFinite(from.X) || !float.IsFinite(from.Y)) throw new ArgumentOutOfRangeException(nameof(from), from, "The ray must be finite.");
        if (!float.IsFinite(to.X) || !float.IsFinite(to.Y)) throw new ArgumentOutOfRangeException(nameof(to), to, "The ray must be finite.");
        hit = default;
        bool found = false;
        var dir = to - from;
        var span = new RectF(MathF.Min(from.X, to.X), MathF.Min(from.Y, to.Y), MathF.Abs(dir.X), MathF.Abs(dir.Y));
        foreach (int i in Candidates(Grow(span, BroadPhaseMargin)))
        {
            var s = _shapeSlots[i];
            if (!s.Alive || (s.Description.IsSensor && !filter.IncludeSensors)) continue;
            if (!filter.Accepts(s.Description.Category, s.Description.Mask) || !filter.Considers(IdOf(i))) continue;
            var owner = _bodySlots[s.Body];
            bool any = s.Grid is not null
                ? TileRay.Cast(s.Grid, owner.Position, from, dir, filter.PassOneWay, out float t, out var normal)
                : TileRay.Box(BoxOf(owner.Position, s.Description), from, dir, s.Description.OneWay && !filter.PassOneWay, s.Description.OneWay && filter.PassOneWay, out t, out normal);
            if (!any || (found && t >= hit.Fraction)) continue;
            found = true;
            hit = new RayHit(IdOf(i), owner.Owner, from + dir * t, normal, t);
        }
        return found;
    }

    /// <inheritdoc/>
    public int Overlap(in RectF area, in QueryFilter filter, Span<ShapeId> results)
    {
        int count = 0;
        foreach (int i in Candidates(Grow(area, BroadPhaseMargin)))
        {
            var s = _shapeSlots[i];
            if (!s.Alive || (s.Description.IsSensor && !filter.IncludeSensors)) continue;
            if (!filter.Accepts(s.Description.Category, s.Description.Mask) || !filter.Considers(IdOf(i))) continue;
            if (s.Description.OneWay && filter.PassOneWay) continue;
            var position = _bodySlots[s.Body].Position;
            bool overlaps = s.Grid is not null ? GridOverlaps(s.Grid, position, area, filter.PassOneWay) : BoxOf(position, s.Description).Intersects(area);
            if (!overlaps) continue;
            if (count < results.Length) results[count] = IdOf(i);
            count++;
        }
        return count;
    }

    private static bool GridOverlaps(ICollisionGrid grid, Vector2 origin, RectF area, bool passOneWay)
    {
        if (!(area.Width > 0 && area.Height > 0)) return false;
        int ts = grid.TileSize;
        int x0 = (int)MathF.Floor((area.Left - origin.X) / ts), x1 = (int)MathF.Ceiling((area.Right - origin.X) / ts) - 1;
        int y0 = (int)MathF.Floor((area.Top - origin.Y) / ts), y1 = (int)MathF.Ceiling((area.Bottom - origin.Y) / ts) - 1;
        var mask = passOneWay ? TileFlags.Solid : TileFlags.Solid | TileFlags.OneWay;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            var flags = grid.FlagsAt(x, y);
            if ((flags & mask) != 0 && TileCast.Blocking(flags, new RectF(x * ts + origin.X, y * ts + origin.Y, ts, ts)).Intersects(area))
                return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ shape queries

    private readonly List<RayHit> _hits = [];

    private static void Finite(Vector2 value, string name, string what)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(name, value, what);
    }

    /// <summary>Whether a query with <paramref name="filter"/> looks at shape slot <paramref name="i"/>.</summary>
    private bool Queried(int i, in QueryFilter filter, out Shape s)
    {
        s = _shapeSlots[i];
        if (!s.Alive || (s.Description.IsSensor && !filter.IncludeSensors)) return false;
        return filter.Accepts(s.Description.Category, s.Description.Mask) && filter.Considers(IdOf(i));
    }

    /// <inheritdoc/>
    /// <remarks>A tile grid is one shape: it is hit once, at the first of its cells the ray enters.</remarks>
    public int RayCastAll(Vector2 from, Vector2 to, in QueryFilter filter, Span<RayHit> hits)
    {
        Finite(from, nameof(from), "The ray must be finite.");
        Finite(to, nameof(to), "The ray must be finite.");
        _hits.Clear();
        var dir = to - from;
        var span = new RectF(MathF.Min(from.X, to.X), MathF.Min(from.Y, to.Y), MathF.Abs(dir.X), MathF.Abs(dir.Y));
        foreach (int i in Candidates(Grow(span, BroadPhaseMargin)))
        {
            if (!Queried(i, filter, out var s)) continue;
            var owner = _bodySlots[s.Body];
            bool any = s.Grid is not null
                ? TileRay.Cast(s.Grid, owner.Position, from, dir, filter.PassOneWay, out float t, out var normal)
                : TileRay.Box(BoxOf(owner.Position, s.Description), from, dir, s.Description.OneWay && !filter.PassOneWay, s.Description.OneWay && filter.PassOneWay, out t, out normal);
            if (!any) continue;
            // Nearest first; shapes at the same distance stay in slot order.
            int at = _hits.Count;
            while (at > 0 && _hits[at - 1].Fraction > t) at--;
            _hits.Insert(at, new RayHit(IdOf(i), owner.Owner, from + dir * t, normal, t));
        }
        for (int i = 0; i < _hits.Count && i < hits.Length; i++) hits[i] = _hits[i];
        return _hits.Count;
    }

    /// <inheritdoc/>
    /// <remarks>Exact: the shape stops flush against what it hits. A tile grid is hit at the first of its cells the shape touches.</remarks>
    public bool ShapeCast(in QueryShape shape, Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
    {
        shape.Validate();
        Finite(from, nameof(from), "The cast must be finite.");
        Finite(to, nameof(to), "The cast must be finite.");
        var (half, radius) = shape.Rounded;
        var dir = to - from;
        var reach = half + new Vector2(radius);
        var span = new RectF(MathF.Min(from.X, to.X) - reach.X, MathF.Min(from.Y, to.Y) - reach.Y, MathF.Abs(dir.X) + 2 * reach.X, MathF.Abs(dir.Y) + 2 * reach.Y);
        hit = default;
        bool found = false;
        foreach (int i in Candidates(Grow(span, BroadPhaseMargin)))
        {
            if (!Queried(i, filter, out var s)) continue;
            var owner = _bodySlots[s.Body];
            float t;
            Vector2 normal;
            RectF surface;
            if (s.Grid is not null)
            {
                if (!TileCast.Grid(s.Grid, owner.Position, half, radius, from, dir, filter.PassOneWay, out t, out normal, out surface)) continue;
            }
            else
            {
                surface = BoxOf(owner.Position, s.Description);
                if (s.Description.OneWay && filter.PassOneWay) continue;
                if (!TileCast.Rounded(surface, half, radius, from, dir, out t, out normal)) continue;
                if (s.Description.OneWay && !TileCast.OneWayStops(surface, half, radius, from, normal)) continue;
            }
            if (found && t >= hit.Fraction) continue;
            found = true;
            hit = new RayHit(IdOf(i), owner.Owner, TileCast.Nearest(surface, from + dir * t), normal, t);
        }
        return found;
    }

    /// <inheritdoc/>
    public int Overlap(in QueryShape shape, Vector2 center, in QueryFilter filter, Span<ShapeId> results)
    {
        shape.Validate();
        Finite(center, nameof(center), "The centre must be finite.");
        var (half, radius) = shape.Rounded;
        var reach = half + new Vector2(radius);
        int count = 0;
        foreach (int i in Candidates(Grow(new RectF(center.X - reach.X, center.Y - reach.Y, 2 * reach.X, 2 * reach.Y), BroadPhaseMargin)))
        {
            if (!Queried(i, filter, out var s)) continue;
            if (s.Description.OneWay && filter.PassOneWay) continue;
            var position = _bodySlots[s.Body].Position;
            bool overlaps = s.Grid is not null
                ? TileCast.GridOverlaps(s.Grid, position, half, radius, center, filter.PassOneWay)
                : TileCast.Overlaps(BoxOf(position, s.Description), half, radius, center);
            if (!overlaps) continue;
            if (count < results.Length) results[count] = IdOf(i);
            count++;
        }
        return count;
    }

    /// <inheritdoc/>
    /// <remarks>A tile grid holds the point when the cell it is in blocks.</remarks>
    public int OverlapPoint(Vector2 point, in QueryFilter filter, Span<ShapeId> results)
    {
        Finite(point, nameof(point), "The point must be finite.");
        int count = 0;
        foreach (int i in Candidates(Grow(new RectF(point.X, point.Y, 0, 0), BroadPhaseMargin)))
        {
            if (!Queried(i, filter, out var s)) continue;
            if (s.Description.OneWay && filter.PassOneWay) continue;
            var position = _bodySlots[s.Body].Position;
            bool holds;
            if (s.Grid is not null)
            {
                // Inside a solid cell (the faces between solid cells are inside too), or inside a one-way cell's platform.
                int ts = s.Grid.TileSize;
                float tx = (point.X - position.X) / ts, ty = (point.Y - position.Y) / ts;
                int cx = (int)MathF.Floor(tx), cy = (int)MathF.Floor(ty);
                var flags = s.Grid.FlagsAt(cx, cy);
                if ((flags & TileFlags.Solid) != 0)
                    holds = (tx > cx || (s.Grid.FlagsAt(cx - 1, cy) & TileFlags.Solid) != 0) && (ty > cy || (s.Grid.FlagsAt(cx, cy - 1) & TileFlags.Solid) != 0);
                else
                    holds = !filter.PassOneWay && (flags & TileFlags.OneWay) != 0 && tx > cx && ty > cy && ty - cy < PhysicsContract.OneWayTileThickness;
            }
            else
            {
                var box = BoxOf(position, s.Description);
                holds = point.X > box.Left && point.X < box.Right && point.Y > box.Top && point.Y < box.Bottom;
            }
            if (!holds) continue;
            if (count < results.Length) results[count] = IdOf(i);
            count++;
        }
        return count;
    }

    /// <inheritdoc/>
    public bool ClosestPoint(Vector2 point, float maxDistance, in QueryFilter filter, out ClosestPointHit hit)
    {
        Finite(point, nameof(point), "The point must be finite.");
        if (!(maxDistance >= 0) || float.IsInfinity(maxDistance)) throw new ArgumentOutOfRangeException(nameof(maxDistance), maxDistance, "The distance must be finite and zero or more.");
        hit = default;
        bool found = false;
        float best = maxDistance * maxDistance;
        foreach (int i in Candidates(Grow(new RectF(point.X - maxDistance, point.Y - maxDistance, 2 * maxDistance, 2 * maxDistance), BroadPhaseMargin)))
        {
            if (!Queried(i, filter, out var s)) continue;
            if (s.Description.OneWay && filter.PassOneWay) continue;
            var owner = _bodySlots[s.Body];
            Vector2 nearest;
            float d;
            if (s.Grid is not null)
            {
                if (!TileCast.GridNearest(s.Grid, owner.Position, point, maxDistance, filter.PassOneWay, out nearest, out d)) continue;
            }
            else
            {
                var box = BoxOf(owner.Position, s.Description);
                d = TileCast.DistanceSquared(box, point);
                nearest = TileCast.Nearest(box, point);
            }
            if (d > best || (found && d >= best)) continue;
            found = true;
            best = d;
            hit = new ClosestPointHit(IdOf(i), owner.Owner, nearest, MathF.Sqrt(d));
        }
        return found;
    }

    // ------------------------------------------------------------------ step

    /// <inheritdoc/>
    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt), dt, "The step must be finite and not negative.");
        StepCount++;
        _sensorEvents.Clear();
        _nextPairs.Clear();
        for (int si = 0; si < _shapeSlots.Count; si++)
        {
            var sensor = _shapeSlots[si];
            if (!sensor.Alive || !sensor.Description.IsSensor) continue;
            var sensorBody = _bodySlots[sensor.Body];
            var sensorBox = BoxOf(sensorBody.Position, sensor.Description);
            foreach (int vi in Candidates(Grow(sensorBox, BroadPhaseMargin)))
            {
                var visitor = _shapeSlots[vi];
                if (!visitor.Alive || visitor.Description.IsSensor || !visitor.Description.SensorEvents || visitor.Grid is not null || visitor.Body == sensor.Body) continue;
                var visitorBody = _bodySlots[visitor.Body];
                if (visitorBody.Kind == BodyKind.Static) continue;
                if ((sensor.Description.Mask & visitor.Description.Category) == 0 || (visitor.Description.Mask & sensor.Description.Category) == 0) continue;
                if (!sensorBox.Intersects(BoxOf(visitorBody.Position, visitor.Description))) continue;
                _nextPairs.Add(new Pair(IdOf(si), IdOf(vi), sensorBody.Owner, visitorBody.Owner));
            }
        }
        // Both lists are sorted by sensor then visitor slot: exits are the old pairs missing now, entries the new ones.
        foreach (var old in _pairs)
            if (_nextPairs.BinarySearch(old) is var at && (at < 0 || !_nextPairs[at].SameShapes(old)))
                _sensorEvents.Add(new SensorEvent(SensorEventKind.Exit, old.Sensor, old.Visitor, old.SensorOwner, old.VisitorOwner));
        foreach (var now in _nextPairs)
            if (_pairs.BinarySearch(now) is var at && (at < 0 || !_pairs[at].SameShapes(now)))
                _sensorEvents.Add(new SensorEvent(SensorEventKind.Enter, now.Sensor, now.Visitor, now.SensorOwner, now.VisitorOwner));
        (_pairs, _nextPairs) = (_nextPairs, _pairs);
    }

    /// <summary>Adds the world's simulation state: the step count, every live body (id, kind, position, shapes) and the sensor pairs.</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(StepCount);
        hash.Add(_liveCount);
        foreach (var id in Bodies)
        {
            var b = _bodySlots[(int)id.Index];
            hash.Add((long)id.Index);
            hash.Add((long)id.Generation);
            hash.Add((int)b.Kind);
            hash.Add(b.Position);
            hash.Add(b.Shapes.Count);
            foreach (int s in b.Shapes) hash.Add((long)_shapeSlots[s].Generation);
        }
        hash.Add(_pairs.Count);
        foreach (var p in _pairs)
        {
            hash.Add((long)p.Sensor.Index);
            hash.Add((long)p.Visitor.Index);
        }
    }
}
