using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Physics.Box2D;

/// <summary>
/// The Box2D v3 backend of <see cref="IPhysicsWorld2D"/>: native Box2D (the release its native layer pins) behind the
/// engine's flat C layer, so no Box2D type reaches the game. It passes the common contract and declares
/// <see cref="Declared"/>: dynamic bodies, rotation, circles, capsules, convex polygons, continuous collision,
/// friction and restitution, mass properties and one-way platforms. It declares neither
/// <see cref="PhysicsCapabilities.TileGeometry"/> (build tile levels from boxes) nor
/// <see cref="PhysicsCapabilities.MovingSolids"/> nor <see cref="PhysicsCapabilities.Joints"/>.
/// </summary>
/// <remarks>
/// <para>Units: Box2D works in metres with y up; <see cref="Box2DWorldOptions"/> maps the game's units onto them
/// (<see cref="Box2DWorldOptions.UnitsPerMeter"/>, <see cref="Box2DWorldOptions.YDown"/>), so a pixel-space game passes
/// pixels and gets pixels back.</para>
/// <para><see cref="Move"/> moves a box (or a polygon or circle) along x, then y, each as a shape cast that stops a
/// little short of the first surface, and a capsule with Box2D's own mover. Resting is not exact: a body may rest up to
/// <see cref="Skin"/> away from a surface (about 1 cm, two Box2D slops), and a capsule may sink into it by up to the
/// same amount. Contacts with static and kinematic bodies come from <see cref="MoveResult"/> only (Box2D creates no
/// contacts between them); <see cref="ContactEvents"/> carries the pairs with a dynamic body.</para>
/// <para>Angles go through Box2D's deterministic sine, cosine and arctangent approximations: an angle reads back
/// within about 0.002 radians of the one set.</para>
/// <para>Deterministic per native build: the same calls give the same bits on every target the build ships for
/// (floating-point contraction off, one thread); nothing is promised against another backend or another Box2D
/// version.</para>
/// <para>The world owns native memory: dispose it when done. Box2D keeps at most 128 worlds alive in a process, in
/// one table shared by all threads: create and dispose worlds from one thread at a time. A world that is never disposed
/// is released on a later creation or disposal after the garbage collector finds it (never on the finalizer thread).</para>
/// </remarks>
public sealed unsafe class Box2DPhysicsWorld2D : IPhysicsWorld2D, IStateHashed, IDisposable
{
    /// <summary>The capabilities this backend declares.</summary>
    public const PhysicsCapabilities Declared = PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.Rotation
        | PhysicsCapabilities.Circles | PhysicsCapabilities.Capsules | PhysicsCapabilities.Polygons
        | PhysicsCapabilities.ContinuousCollision | PhysicsCapabilities.FrictionRestitution
        | PhysicsCapabilities.MassProperties | PhysicsCapabilities.OneWayPlatforms;

    /// <summary>The most vertices a polygon may have (Box2D's limit).</summary>
    public const int MaxPolygonVertices = 8;

    private const string Name = "box2d";
    private const int MaxMoveContacts = 32, EventBatch = 64;

    private sealed class Body
    {
        public uint Generation;
        public bool Alive;
        public EntityId Owner;
        public BodyKind Kind;
        public int Native;
        public readonly List<int> Shapes = [];
    }

    private sealed class Shape
    {
        public uint Generation;
        public bool Alive;
        public int Body;
        public ShapeDescription Description;
        public int Native;
    }

    private readonly record struct Pair(ShapeId A, ShapeId B, EntityId OwnerA, EntityId OwnerB);

    // Worlds the garbage collector found undisposed: destroyed on the next creation or disposal, on the game's thread.
    private static readonly ConcurrentQueue<nint> s_abandoned = new();

    private nint _world;
    private readonly float _unitsPerMeter;
    private readonly float _ySign;
    private readonly List<Body> _bodySlots = [];
    private readonly Stack<int> _freeBodies = new();
    private readonly List<Shape> _shapeSlots = [];
    private readonly Stack<int> _freeShapes = new();
    private readonly List<int> _shapeOfNative = []; // native shape handle -> shape slot (-1 once destroyed)
    private BodyId[] _live = new BodyId[16];
    private int _liveCount;
    private readonly List<Pair> _sensorPairs = [];
    private readonly List<Pair> _contactPairs = [];
    private readonly List<SensorEvent> _pendingSensorExits = [];
    private readonly List<ContactEvent> _pendingContactEnds = [];
    private readonly List<SensorEvent> _sensorEvents = [];
    private readonly List<ContactEvent> _contactEvents = [];
    private int[] _overlap = new int[64];

    /// <summary>Creates an empty world.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An option is out of range (<see cref="Box2DWorldOptions.Validate"/>).</exception>
    /// <exception cref="DllNotFoundException">The native library of this runtime is missing (<see cref="IsSupported"/>).</exception>
    /// <exception cref="InvalidOperationException">The native library is of another layer version than this assembly, or Box2D already holds 128 worlds.</exception>
    public Box2DPhysicsWorld2D(Box2DWorldOptions? options = null)
    {
        options ??= new Box2DWorldOptions();
        options.Validate();
        int api = Native.me_api_version();
        if (api != Native.ApiVersion)
            throw new InvalidOperationException($"The native me_box2d library is layer version {api}; this assembly needs version {Native.ApiVersion}.");
        Options = options;
        _unitsPerMeter = options.UnitsPerMeter;
        _ySign = options.YDown ? -1 : 1;
        Up = new Vector2(0, _ySign);
        Skin = Native.me_move_skin() * _unitsPerMeter;
        var gravity = options.Gravity is { } g ? ToMeters(g) : new Vector2(0, -9.81f);
        DestroyAbandoned();
        _world = Native.me_world_create(gravity.X, gravity.Y, options.SubSteps, options.EnableSleep ? 1 : 0);
        if (_world == 0)
        {
            // Box2D's table is full: release the worlds nobody disposed, then try once more.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            DestroyAbandoned();
            _world = Native.me_world_create(gravity.X, gravity.Y, options.SubSteps, options.EnableSleep ? 1 : 0);
        }
        if (_world == 0) throw new InvalidOperationException("Box2D already holds its maximum of 128 worlds: dispose the worlds you no longer use.");
    }

    /// <summary>Hands an undisposed native world to the next creation or disposal (Box2D is not called from this thread).</summary>
    ~Box2DPhysicsWorld2D()
    {
        if (_world != 0) s_abandoned.Enqueue(_world);
    }

    private static void DestroyAbandoned()
    {
        while (s_abandoned.TryDequeue(out var world)) Native.me_world_destroy(world);
    }

    /// <summary>
    /// Whether this process can load the native library (the runtime's <c>runtimes/&lt;rid&gt;/native</c> asset, or
    /// the browser's static library) and it is of this assembly's layer version.
    /// </summary>
    public static bool IsSupported
    {
        get
        {
            try { return Native.me_api_version() == Native.ApiVersion; }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }
    }

    /// <summary>The Box2D release the native library was built from, for example "3.2.0".</summary>
    public static string Box2DVersion
    {
        get
        {
            int v = Native.me_box2d_version();
            return $"{v / 10000}.{v / 100 % 100}.{v % 100}";
        }
    }

    /// <summary>The options the world was made with.</summary>
    public Box2DWorldOptions Options { get; }
    /// <inheritdoc/>
    public string Backend => Name;
    /// <inheritdoc/>
    public PhysicsCapabilities Capabilities => Declared;
    /// <summary>The largest gap <see cref="Move"/> leaves to a surface (about 1.5 cm, in world units), and the deepest a capsule may sink into one.</summary>
    public float Skin { get; }
    /// <summary>(0, 1), or (0, -1) with <see cref="Box2DWorldOptions.YDown"/>.</summary>
    public Vector2 Up { get; }
    /// <inheritdoc/>
    public long StepCount { get; private set; }
    /// <inheritdoc/>
    public ReadOnlySpan<BodyId> Bodies => _live.AsSpan(0, _liveCount);
    /// <summary>The contacts that began or ended in the latest step: ends first, then begins, in the order Box2D reports them.</summary>
    public ReadOnlySpan<ContactEvent> ContactEvents => CollectionsMarshal.AsSpan(_contactEvents);
    /// <summary>The sensor entries and exits of the latest step: exits first, then entries, in the order Box2D reports them.</summary>
    public ReadOnlySpan<SensorEvent> SensorEvents => CollectionsMarshal.AsSpan(_sensorEvents);

    // ------------------------------------------------------------------ units

    private Vector2 ToMeters(Vector2 v) => new(v.X / _unitsPerMeter, v.Y * _ySign / _unitsPerMeter);
    private Vector2 FromMeters(float x, float y) => new(x * _unitsPerMeter, y * _ySign * _unitsPerMeter);
    private Vector2 Direction(float x, float y) => new(x, y * _ySign);
    private float Meters(float length) => length / _unitsPerMeter;

    private nint World
    {
        get
        {
            ObjectDisposedException.ThrowIf(_world == 0, this);
            return _world;
        }
    }

    // ------------------------------------------------------------------ bodies

    /// <inheritdoc/>
    public BodyId CreateBody(EntityId owner, in BodyDescription body)
    {
        body.Validate();
        PhysicsCapabilityException.ThrowIfMissing(Declared, body.RequiredCapabilities, $"CreateBody({body.Kind})", Name);
        var world = World;
        var p = ToMeters(body.Position);
        int type = body.Kind switch { BodyKind.Static => Native.Static, BodyKind.Kinematic => Native.Kinematic, _ => Native.Dynamic };
        int native = Native.me_body_create(world, type, p.X, p.Y, body.Rotation * _ySign, body.Bullet ? 1 : 0);
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
        b.Native = native;
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
        foreach (int s in b.Shapes) Forget(s);
        b.Shapes.Clear();
        Native.me_body_destroy(World, b.Native);
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

    private void State(Body b, float* s) => Native.me_body_state(World, b.Native, s);

    /// <inheritdoc/>
    public Vector2 PositionOf(BodyId body)
    {
        float* s = stackalloc float[6];
        State(Resolve(body), s);
        return FromMeters(s[0], s[1]);
    }

    /// <inheritdoc/>
    public float RotationOf(BodyId body)
    {
        float* s = stackalloc float[6];
        State(Resolve(body), s);
        return s[2] * _ySign;
    }

    /// <inheritdoc/>
    public void SetPosition(BodyId body, Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) throw new ArgumentOutOfRangeException(nameof(position), position, "The position must be finite.");
        var b = Resolve(body);
        var p = ToMeters(position);
        Native.me_body_set_position(World, b.Native, p.X, p.Y);
    }

    /// <summary>The body's linear velocity in world units per second (zero for static bodies; kinematic bodies moved by <see cref="Move"/> report zero).</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    public Vector2 VelocityOf(BodyId body)
    {
        float* s = stackalloc float[6];
        State(Resolve(body), s);
        return FromMeters(s[3], s[4]);
    }

    /// <summary>Sets a dynamic body's linear velocity, in world units per second.</summary>
    /// <exception cref="InvalidOperationException">The id is stale or the body is not dynamic.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The velocity is not finite.</exception>
    public void SetVelocity(BodyId body, Vector2 velocity)
    {
        if (!float.IsFinite(velocity.X) || !float.IsFinite(velocity.Y)) throw new ArgumentOutOfRangeException(nameof(velocity), velocity, "The velocity must be finite.");
        var b = Resolve(body);
        if (b.Kind != BodyKind.Dynamic) throw new InvalidOperationException($"Only dynamic bodies have a velocity to set: {body} is {b.Kind}.");
        var v = ToMeters(velocity);
        Native.me_body_set_velocity(World, b.Native, v.X, v.Y);
    }

    /// <summary>A dynamic body's mass in kilograms, from its shapes' densities (kilograms per square metre); 0 for other bodies.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    public float MassOf(BodyId body) => Native.me_body_mass(World, Resolve(body).Native);

    private Body Resolve(BodyId body)
    {
        if (!IsAlive(body)) throw new InvalidOperationException($"{body} is not a live body of this world (destroyed, stale or none).");
        return _bodySlots[(int)body.Index];
    }

    // ------------------------------------------------------------------ shapes

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">A polygon has more than <see cref="MaxPolygonVertices"/> vertices.</exception>
    /// <exception cref="ArgumentException">A polygon's vertices do not make a convex polygon of some area.</exception>
    public ShapeId AddShape(BodyId body, in ShapeDescription shape)
    {
        shape.Validate();
        PhysicsCapabilityException.ThrowIfMissing(Declared, shape.RequiredCapabilities, $"AddShape({shape.Kind})", Name);
        if (shape.Kind == ShapeKind.Polygon && shape.Vertices.Length > MaxPolygonVertices)
            throw new ArgumentOutOfRangeException(nameof(shape), shape.Vertices.Length, $"Box2D polygons have at most {MaxPolygonVertices} vertices.");
        var b = Resolve(body);
        float* g = stackalloc float[2 * MaxPolygonVertices];
        int points = 0, kind;
        float radius = 0;
        var centre = ToMeters(shape.Offset);
        switch (shape.Kind)
        {
            case ShapeKind.Box:
                kind = Native.Box;
                g[0] = Meters(shape.Size.X) / 2; g[1] = Meters(shape.Size.Y) / 2; g[2] = centre.X; g[3] = centre.Y;
                break;
            case ShapeKind.Circle:
                kind = Native.Circle;
                g[0] = centre.X; g[1] = centre.Y; radius = Meters(shape.Radius);
                break;
            case ShapeKind.Capsule:
                kind = Native.Capsule;
                radius = Meters(shape.Radius);
                float half = MathF.Max(0, Meters(shape.Size.Y) / 2 - radius);
                g[0] = centre.X; g[1] = centre.Y - half; g[2] = centre.X; g[3] = centre.Y + half;
                break;
            default:
                kind = Native.Polygon;
                points = shape.Vertices.Length;
                var vertices = shape.Vertices.Span;
                for (int i = 0; i < points; i++)
                {
                    var v = ToMeters(shape.Offset + vertices[i]);
                    g[2 * i] = v.X; g[2 * i + 1] = v.Y;
                }
                break;
        }
        int flags = (shape.IsSensor ? Native.Sensor : 0) | (shape.SensorEvents ? Native.SensorEvents : 0) | (shape.OneWay ? Native.OneWay : 0);
        int native = Native.me_shape_create(World, b.Native, kind, g, points, radius, shape.Category, shape.Mask, flags,
            shape.Density ?? -1, shape.Friction ?? -1, shape.Restitution ?? -1);
        if (native < 0) throw new ArgumentException("The vertices do not make a convex polygon of some area (Box2D could not build its hull).", nameof(shape));
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
        s.Body = (int)body.Index;
        s.Description = shape;
        s.Native = native;
        b.Shapes.Add(slot);
        while (_shapeOfNative.Count <= native) _shapeOfNative.Add(-1);
        _shapeOfNative[native] = slot;
        return new ShapeId((uint)slot, s.Generation);
    }

    /// <inheritdoc/>
    public void RemoveShape(ShapeId shape)
    {
        if (!IsAlive(shape)) return;
        var s = _shapeSlots[(int)shape.Index];
        _bodySlots[s.Body].Shapes.Remove((int)shape.Index);
        int native = s.Native;
        Forget((int)shape.Index);
        Native.me_shape_destroy(World, native);
    }

    /// <summary>Marks a shape dead and ends its sensor and contact pairs with events at the next step.</summary>
    private void Forget(int slot)
    {
        var s = _shapeSlots[slot];
        var id = IdOf(slot);
        for (int i = _sensorPairs.Count - 1; i >= 0; i--)
        {
            var p = _sensorPairs[i];
            if (p.A != id && p.B != id) continue;
            _pendingSensorExits.Add(new SensorEvent(SensorEventKind.Exit, p.A, p.B, p.OwnerA, p.OwnerB));
            _sensorPairs.RemoveAt(i);
        }
        for (int i = _contactPairs.Count - 1; i >= 0; i--)
        {
            var p = _contactPairs[i];
            if (p.A != id && p.B != id) continue;
            _pendingContactEnds.Add(new ContactEvent(ContactEventKind.End, p.A, p.B, p.OwnerA, p.OwnerB, Vector2.Zero));
            _contactPairs.RemoveAt(i);
        }
        _shapeOfNative[s.Native] = -1;
        s.Alive = false;
        _freeShapes.Push(slot);
    }

    /// <inheritdoc/>
    public bool IsAlive(ShapeId shape) =>
        shape.Generation != 0 && shape.Index < (uint)_shapeSlots.Count && _shapeSlots[(int)shape.Index] is { Alive: true } s && s.Generation == shape.Generation;

    /// <inheritdoc/>
    public BodyId BodyOf(ShapeId shape)
    {
        if (!IsAlive(shape)) throw new InvalidOperationException($"{shape} is not a live shape of this world.");
        int b = _shapeSlots[(int)shape.Index].Body;
        return new BodyId((uint)b, _bodySlots[b].Generation);
    }

    /// <summary>The world bounds of <paramref name="shape"/> as it is now.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    public RectF BoundsOf(ShapeId shape)
    {
        if (!IsAlive(shape)) throw new InvalidOperationException($"{shape} is not a live shape of this world.");
        float* b = stackalloc float[4];
        Native.me_shape_bounds(World, _shapeSlots[(int)shape.Index].Native, b);
        var a = FromMeters(b[0], b[1]);
        var c = FromMeters(b[2], b[3]);
        return new RectF(MathF.Min(a.X, c.X), MathF.Min(a.Y, c.Y), MathF.Abs(c.X - a.X), MathF.Abs(c.Y - a.Y));
    }

    private ShapeId IdOf(int slot) => new((uint)slot, _shapeSlots[slot].Generation);

    /// <summary>The live shape a native handle names, or -1.</summary>
    private int SlotOf(int native) => native >= 0 && native < _shapeOfNative.Count ? _shapeOfNative[native] : -1;

    private EntityId OwnerOfShape(int slot) => _bodySlots[_shapeSlots[slot].Body].Owner;

    // ------------------------------------------------------------------ moves

    /// <inheritdoc/>
    /// <remarks>
    /// Boxes, polygons and circles move along x, then y, each as one shape cast that stops within <see cref="Skin"/>
    /// of the first surface (contacts get axis normals); a capsule moves with Box2D's mover (contacts get the surfaces'
    /// normals). The body is teleported to the result: dynamic bodies in the way block it like any other shape.
    /// </remarks>
    public MoveResult Move(BodyId body, Vector2 delta, in QueryFilter filter = default)
    {
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y)) throw new ArgumentOutOfRangeException(nameof(delta), delta, "The movement must be finite.");
        var b = Resolve(body);
        if (b.Kind != BodyKind.Kinematic) throw new InvalidOperationException($"Only kinematic bodies are moved with Move: {body} is {b.Kind}.");
        int mover = -1;
        foreach (int s in b.Shapes)
        {
            if (_shapeSlots[s].Description.IsSensor) continue;
            if (mover >= 0) throw new InvalidOperationException($"{body} has more than one non-sensor shape; Move moves one shape.");
            mover = s;
        }
        if (mover < 0) throw new InvalidOperationException($"{body} has no non-sensor shape to move.");

        var start = PositionOf(body);
        var d = ToMeters(delta);
        float* xy = stackalloc float[2];
        int* shapes = stackalloc int[MaxMoveContacts];
        float* normals = stackalloc float[2 * MaxMoveContacts];
        int count = Native.me_move(World, b.Native, _shapeSlots[mover].Native, d.X, d.Y, filter.Category, filter.Mask,
            filter.PassOneWay ? Native.QueryPassOneWay : 0, xy, shapes, normals, MaxMoveContacts);
        int kept = Math.Min(count, MaxMoveContacts);
        Span<MoveContact> contacts = stackalloc MoveContact[kept];
        int n = 0;
        for (int i = 0; i < kept; i++)
        {
            int slot = SlotOf(shapes[i]);
            if (slot < 0) continue;
            contacts[n++] = new MoveContact(IdOf(slot), OwnerOfShape(slot), Direction(normals[2 * i], normals[2 * i + 1]));
        }
        var position = FromMeters(xy[0], xy[1]);
        return new MoveResult(position, position - start, Up, contacts[..n]);
    }

    // ------------------------------------------------------------------ queries

    private static int QueryFlags(in QueryFilter filter) =>
        (filter.IncludeSensors ? Native.QuerySensors : 0) | (filter.PassOneWay ? Native.QueryPassOneWay : 0);

    /// <inheritdoc/>
    public bool RayCast(Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
    {
        if (!float.IsFinite(from.X) || !float.IsFinite(from.Y)) throw new ArgumentOutOfRangeException(nameof(from), from, "The ray must be finite.");
        if (!float.IsFinite(to.X) || !float.IsFinite(to.Y)) throw new ArgumentOutOfRangeException(nameof(to), to, "The ray must be finite.");
        var a = ToMeters(from);
        var t = ToMeters(to) - a;
        float* r = stackalloc float[5];
        int native = Native.me_ray_cast(World, a.X, a.Y, t.X, t.Y, filter.Category, filter.Mask, QueryFlags(filter), r);
        int slot = SlotOf(native);
        if (slot < 0)
        {
            hit = default;
            return false;
        }
        hit = new RayHit(IdOf(slot), OwnerOfShape(slot), FromMeters(r[0], r[1]), Direction(r[2], r[3]), r[4]);
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>Shapes are returned in creation order.</remarks>
    public int Overlap(in RectF area, in QueryFilter filter, Span<ShapeId> results)
    {
        if (!(area.Width > 0 && area.Height > 0)) return 0;
        var a = ToMeters(area.Position);
        var c = ToMeters(new Vector2(area.Right, area.Bottom));
        var world = World;
        int total;
        while (true)
        {
            fixed (int* found = _overlap)
                total = Native.me_overlap_box(world, MathF.Min(a.X, c.X), MathF.Min(a.Y, c.Y), MathF.Max(a.X, c.X), MathF.Max(a.Y, c.Y),
                    filter.Category, filter.Mask, QueryFlags(filter), found, _overlap.Length);
            if (total <= _overlap.Length || _overlap.Length >= results.Length) break;
            _overlap = new int[Math.Max(total, _overlap.Length * 2)];
        }
        int written = 0;
        for (int i = 0; i < Math.Min(total, _overlap.Length) && written < results.Length; i++)
        {
            int slot = SlotOf(_overlap[i]);
            if (slot >= 0) results[written++] = IdOf(slot);
        }
        return total;
    }

    // ------------------------------------------------------------------ step

    /// <inheritdoc/>
    public void Step(float dt)
    {
        if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt), dt, "The step must be finite and not negative.");
        var world = World;
        Native.me_world_step(world, dt);
        StepCount++;
        ReadSensorEvents(world);
        ReadContactEvents(world);
    }

    private void ReadSensorEvents(nint world)
    {
        _sensorEvents.Clear();
        _sensorEvents.AddRange(_pendingSensorExits);
        _pendingSensorExits.Clear();
        int* begins = stackalloc int[2 * EventBatch];
        int* ends = stackalloc int[2 * EventBatch];
        int endCount;
        int beginCount = Native.me_sensor_events(world, begins, EventBatch, ends, EventBatch, &endCount);
        if (beginCount > EventBatch || endCount > EventBatch)
        {
            // More events than the stack batch: read them again into buffers that fit.
            var b = new int[2 * beginCount];
            var e = new int[2 * endCount];
            fixed (int* pb = b)
            fixed (int* pe = e)
            {
                Native.me_sensor_events(world, pb, beginCount, pe, endCount, &endCount);
                Sensors(pb, beginCount, pe, endCount);
            }
            return;
        }
        Sensors(begins, beginCount, ends, endCount);
    }

    private void Sensors(int* begins, int beginCount, int* ends, int endCount)
    {
        for (int i = 0; i < endCount; i++)
        {
            int sensor = SlotOf(ends[2 * i]), visitor = SlotOf(ends[2 * i + 1]);
            if (sensor < 0 || visitor < 0) continue; // a destroyed shape: its exit was reported when it went
            int at = _sensorPairs.FindIndex(p => p.A == IdOf(sensor) && p.B == IdOf(visitor));
            if (at < 0) continue;
            var pair = _sensorPairs[at];
            _sensorPairs.RemoveAt(at);
            _sensorEvents.Add(new SensorEvent(SensorEventKind.Exit, pair.A, pair.B, pair.OwnerA, pair.OwnerB));
        }
        for (int i = 0; i < beginCount; i++)
        {
            int sensor = SlotOf(begins[2 * i]), visitor = SlotOf(begins[2 * i + 1]);
            if (sensor < 0 || visitor < 0) continue;
            var pair = new Pair(IdOf(sensor), IdOf(visitor), OwnerOfShape(sensor), OwnerOfShape(visitor));
            _sensorPairs.Add(pair);
            _sensorEvents.Add(new SensorEvent(SensorEventKind.Enter, pair.A, pair.B, pair.OwnerA, pair.OwnerB));
        }
    }

    private void ReadContactEvents(nint world)
    {
        _contactEvents.Clear();
        _contactEvents.AddRange(_pendingContactEnds);
        _pendingContactEnds.Clear();
        int* begins = stackalloc int[2 * EventBatch];
        float* normals = stackalloc float[2 * EventBatch];
        int* ends = stackalloc int[2 * EventBatch];
        int endCount;
        int beginCount = Native.me_contact_events(world, begins, normals, EventBatch, ends, EventBatch, &endCount);
        if (beginCount > EventBatch || endCount > EventBatch)
        {
            var b = new int[2 * beginCount];
            var n = new float[2 * beginCount];
            var e = new int[2 * endCount];
            fixed (int* pb = b)
            fixed (float* pn = n)
            fixed (int* pe = e)
            {
                Native.me_contact_events(world, pb, pn, beginCount, pe, endCount, &endCount);
                Contacts(pb, pn, beginCount, pe, endCount);
            }
            return;
        }
        Contacts(begins, normals, beginCount, ends, endCount);
    }

    private void Contacts(int* begins, float* normals, int beginCount, int* ends, int endCount)
    {
        for (int i = 0; i < endCount; i++)
        {
            int a = SlotOf(ends[2 * i]), b = SlotOf(ends[2 * i + 1]);
            if (a < 0 || b < 0) continue;
            ShapeId ia = IdOf(a), ib = IdOf(b);
            int at = _contactPairs.FindIndex(p => p.A == ia && p.B == ib || p.A == ib && p.B == ia);
            if (at < 0) continue;
            var pair = _contactPairs[at];
            _contactPairs.RemoveAt(at);
            _contactEvents.Add(new ContactEvent(ContactEventKind.End, pair.A, pair.B, pair.OwnerA, pair.OwnerB, Vector2.Zero));
        }
        for (int i = 0; i < beginCount; i++)
        {
            int a = SlotOf(begins[2 * i]), b = SlotOf(begins[2 * i + 1]);
            if (a < 0 || b < 0) continue;
            var pair = new Pair(IdOf(a), IdOf(b), OwnerOfShape(a), OwnerOfShape(b));
            _contactPairs.Add(pair);
            _contactEvents.Add(new ContactEvent(ContactEventKind.Begin, pair.A, pair.B, pair.OwnerA, pair.OwnerB, Direction(normals[2 * i], normals[2 * i + 1])));
        }
    }

    // ------------------------------------------------------------------ state

    /// <summary>Adds the world's simulation state: the step count and every live body (id, kind, position, angle, velocity, shapes).</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(StepCount);
        hash.Add(_liveCount);
        float* s = stackalloc float[6];
        foreach (var id in Bodies)
        {
            var b = _bodySlots[(int)id.Index];
            State(b, s);
            hash.Add((long)id.Index);
            hash.Add((long)id.Generation);
            hash.Add((int)b.Kind);
            for (int i = 0; i < 6; i++) hash.Add(s[i]);
            hash.Add(b.Shapes.Count);
        }
        hash.Add(_sensorPairs.Count);
        hash.Add(_contactPairs.Count);
    }

    /// <summary>Destroys the native world. Every later call except <see cref="Dispose"/> throws <see cref="ObjectDisposedException"/>.</summary>
    public void Dispose()
    {
        if (_world != 0)
        {
            Native.me_world_destroy(_world);
            _world = 0;
        }
        DestroyAbandoned();
        GC.SuppressFinalize(this);
    }
}
