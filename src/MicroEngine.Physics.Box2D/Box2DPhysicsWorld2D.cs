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
/// friction and restitution, mass properties, one-way platforms and joints. It declares neither
/// <see cref="PhysicsCapabilities.TileGeometry"/> (build tile levels from boxes) nor
/// <see cref="PhysicsCapabilities.MovingSolids"/>.
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
        | PhysicsCapabilities.MassProperties | PhysicsCapabilities.OneWayPlatforms | PhysicsCapabilities.ShapeInspection
        | PhysicsCapabilities.Joints | PhysicsCapabilities.ShapeQueries | PhysicsCapabilities.ContactDetail;

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
        public bool Tuned; // its dynamics were set: they are hashed
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

    private sealed class Joint
    {
        public uint Generation;
        public bool Alive;
        public int Native;
        public JointDescription Description;
    }

    private readonly record struct Pair(ShapeId A, ShapeId B, EntityId OwnerA, EntityId OwnerB);

    // Worlds the garbage collector found undisposed: destroyed on the next creation or disposal, on the game's thread.
    private static readonly ConcurrentQueue<nint> s_abandoned = new();

    // Box2D keeps its worlds in one table that it does not guard: creating or destroying two worlds at the same time
    // from different threads corrupts it. Everything else belongs to one world.
    private static readonly Lock s_table = new();

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
    private readonly List<ContactImpact> _impacts = [];
    private int[] _overlap = new int[64];
    private readonly List<Joint> _jointSlots = [];
    private readonly Stack<int> _freeJoints = new();
    private JointId[] _liveJoints = new JointId[8];
    private int _liveJointCount;
    private readonly List<JointId> _brokenJoints = [];

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
        _world = Create(gravity, options);
        if (_world == 0)
        {
            // Box2D's table is full: release the worlds nobody disposed, then try once more.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _world = Create(gravity, options);
        }
        if (_world == 0) throw new InvalidOperationException("Box2D already holds its maximum of 128 worlds: dispose the worlds you no longer use.");
        if (options.ImpactSpeed is { } impact) Native.me_world_set_impact_speed(_world, Meters(impact));
    }

    /// <summary>Hands an undisposed native world to the next creation or disposal (Box2D is not called from this thread).</summary>
    ~Box2DPhysicsWorld2D()
    {
        if (_world != 0) s_abandoned.Enqueue(_world);
    }

    private static nint Create(Vector2 gravity, Box2DWorldOptions options)
    {
        lock (s_table)
        {
            DestroyAbandoned();
            return Native.me_world_create(gravity.X, gravity.Y, options.SubSteps, options.EnableSleep ? 1 : 0);
        }
    }

    // Called with s_table held.
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
        b.Tuned = false;
        b.Shapes.Clear();
        if (body.Dynamics != default) Apply(b, body.Dynamics);
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
        // The body's joints go with it (the native layer destroys them with the body).
        for (int i = _liveJointCount - 1; i >= 0; i--)
        {
            var d = _jointSlots[(int)_liveJoints[i].Index].Description;
            if (d.BodyB == body || (d.Kind != JointKind.Target && d.BodyA == body)) Forget(_liveJoints[i]);
        }
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

    // ------------------------------------------------------------------ dynamics

    private Body Dynamic(BodyId body, string what)
    {
        var b = Resolve(body);
        if (b.Kind != BodyKind.Dynamic) throw new InvalidOperationException($"Only dynamic bodies take {what}: {body} is {b.Kind}.");
        return b;
    }

    private static void Finite(Vector2 value, string name)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(name, value, "The value must be finite.");
    }

    private static void Finite(float value, string name)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name, value, "The value must be finite.");
    }

    /// <inheritdoc/>
    public void SetRotation(BodyId body, float radians)
    {
        Finite(radians, nameof(radians));
        Native.me_body_set_rotation(World, Resolve(body).Native, radians * _ySign);
    }

    /// <inheritdoc/>
    public float AngularVelocityOf(BodyId body)
    {
        float* s = stackalloc float[6];
        State(Resolve(body), s);
        return s[5] * _ySign;
    }

    /// <inheritdoc/>
    public void SetAngularVelocity(BodyId body, float radiansPerSecond)
    {
        Finite(radiansPerSecond, nameof(radiansPerSecond));
        Native.me_body_set_angular_velocity(World, Dynamic(body, "an angular velocity").Native, radiansPerSecond * _ySign);
    }

    /// <inheritdoc/>
    public void ApplyForce(BodyId body, Vector2 force, bool wake = true) => Apply(body, 0, force, default, false, wake, "forces");
    /// <inheritdoc/>
    public void ApplyForce(BodyId body, Vector2 force, Vector2 point, bool wake = true) => Apply(body, 0, force, point, true, wake, "forces");
    /// <inheritdoc/>
    public void ApplyImpulse(BodyId body, Vector2 impulse, bool wake = true) => Apply(body, 1, impulse, default, false, wake, "impulses");
    /// <inheritdoc/>
    public void ApplyImpulse(BodyId body, Vector2 impulse, Vector2 point, bool wake = true) => Apply(body, 1, impulse, point, true, wake, "impulses");

    private void Apply(BodyId body, int kind, Vector2 value, Vector2 point, bool atPoint, bool wake, string what)
    {
        var b = Dynamic(body, what);
        Finite(value, nameof(value));
        Finite(point, nameof(point));
        var v = ToMeters(value);
        var p = ToMeters(point);
        Native.me_body_apply(World, b.Native, kind, v.X, v.Y, p.X, p.Y, atPoint ? 1 : 0, wake ? 1 : 0);
    }

    /// <inheritdoc/>
    public void ApplyTorque(BodyId body, float torque, bool wake = true) => ApplyAngular(body, 0, torque, wake, "torque");
    /// <inheritdoc/>
    public void ApplyAngularImpulse(BodyId body, float impulse, bool wake = true) => ApplyAngular(body, 1, impulse, wake, "angular impulses");

    private void ApplyAngular(BodyId body, int kind, float value, bool wake, string what)
    {
        var b = Dynamic(body, what);
        Finite(value, nameof(value));
        Native.me_body_apply_angular(World, b.Native, kind, value * _ySign / (_unitsPerMeter * _unitsPerMeter), wake ? 1 : 0);
    }

    /// <inheritdoc/>
    public BodyDynamics DynamicsOf(BodyId body)
    {
        float* d = stackalloc float[5];
        Native.me_body_get_dynamics(World, Resolve(body).Native, d);
        return new BodyDynamics { LinearDamping = d[0], AngularDamping = d[1], GravityScale = d[2], FixedRotation = d[3] != 0, AllowSleep = d[4] != 0 };
    }

    /// <inheritdoc/>
    public void SetDynamics(BodyId body, in BodyDynamics dynamics)
    {
        dynamics.Validate();
        Apply(Resolve(body), dynamics);
    }

    private void Apply(Body b, in BodyDynamics dynamics)
    {
        float* d = stackalloc float[5];
        d[0] = dynamics.LinearDamping;
        d[1] = dynamics.AngularDamping;
        d[2] = dynamics.GravityScale;
        d[3] = dynamics.FixedRotation ? 1 : 0;
        d[4] = dynamics.AllowSleep ? 1 : 0;
        Native.me_body_set_dynamics(World, b.Native, d);
        b.Tuned = true;
    }

    /// <inheritdoc/>
    public bool IsAwake(BodyId body) => Native.me_body_is_awake(World, Resolve(body).Native) != 0;
    /// <inheritdoc/>
    public void SetAwake(BodyId body, bool awake) => Native.me_body_set_awake(World, Resolve(body).Native, awake ? 1 : 0);

    /// <summary>A dynamic body's mass in kilograms, from its shapes' densities (kilograms per square metre); 0 for other bodies.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    public float MassOf(BodyId body) => Native.me_body_mass(World, Resolve(body).Native);

    /// <inheritdoc/>
    public Vector2 CenterOfMassOf(BodyId body)
    {
        float* m = stackalloc float[4];
        Native.me_body_mass_data(World, Resolve(body).Native, m);
        return FromMeters(m[1], m[2]);
    }

    /// <summary>A dynamic body's rotational inertia about its centre of mass, in kilograms × world units squared; 0 for other bodies.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    public float InertiaOf(BodyId body)
    {
        float* m = stackalloc float[4];
        Native.me_body_mass_data(World, Resolve(body).Native, m);
        return m[3] * _unitsPerMeter * _unitsPerMeter;
    }

    private Body Resolve(BodyId body)
    {
        if (!IsAlive(body)) throw new InvalidOperationException($"{body} is not a live body of this world (destroyed, stale or none).");
        return _bodySlots[(int)body.Index];
    }

    // ------------------------------------------------------------------ joints

    /// <inheritdoc/>
    public ReadOnlySpan<JointId> Joints => _liveJoints.AsSpan(0, _liveJointCount);
    /// <inheritdoc/>
    public ReadOnlySpan<JointId> BrokenJoints => CollectionsMarshal.AsSpan(_brokenJoints);

    /// <inheritdoc/>
    public bool IsAlive(JointId joint) =>
        joint.Generation != 0 && joint.Index < (uint)_jointSlots.Count && _jointSlots[(int)joint.Index] is { Alive: true } j && j.Generation == joint.Generation;

    private Joint Resolve(JointId joint)
    {
        if (!IsAlive(joint)) throw new InvalidOperationException($"{joint} is not a live joint of this world (destroyed, broken, stale or none).");
        return _jointSlots[(int)joint.Index];
    }

    /// <summary>Whether the joint's limit and coordinate are angles (and so turn with the world's y direction).</summary>
    private static bool AngularLimit(JointKind kind) => kind == JointKind.Revolute;
    /// <summary>Whether the joint's motor is a torque that turns at radians per second.</summary>
    private static bool AngularMotor(JointKind kind) => kind is JointKind.Revolute or JointKind.Wheel;

    private void NativeLimit(JointKind kind, float lower, float upper, out float nativeLower, out float nativeUpper)
    {
        if (!AngularLimit(kind)) (nativeLower, nativeUpper) = (Meters(lower), Meters(upper));
        else if (_ySign < 0) (nativeLower, nativeUpper) = (-upper, -lower);
        else (nativeLower, nativeUpper) = (lower, upper);
    }

    private float NativeMotorSpeed(JointKind kind, float speed) => AngularMotor(kind) ? speed * _ySign : Meters(speed);
    private float NativeMotorForce(JointKind kind, float force) => AngularMotor(kind) ? force / (_unitsPerMeter * _unitsPerMeter) : force / _unitsPerMeter;

    /// <inheritdoc/>
    /// <remarks>
    /// Lengths, speeds, forces and torques are in world units (<see cref="Box2DWorldOptions.UnitsPerMeter"/>); a target
    /// joint is Box2D's motor joint with a linear spring.
    /// </remarks>
    public JointId CreateJoint(in JointDescription joint)
    {
        joint.Validate();
        var world = World;
        var b = Resolve(joint.BodyB);
        int nativeA = joint.Kind == JointKind.Target ? -1 : Resolve(joint.BodyA).Native;
        float* p = stackalloc float[17];
        var anchorA = ToMeters(joint.Kind == JointKind.Target ? joint.TargetPoint : joint.AnchorA);
        var anchorB = ToMeters(joint.AnchorB);
        var axis = Direction(joint.Axis.X, joint.Axis.Y);
        NativeLimit(joint.Kind, joint.Lower, joint.Upper, out float lower, out float upper);
        p[0] = anchorA.X; p[1] = anchorA.Y; p[2] = anchorB.X; p[3] = anchorB.Y;
        p[4] = axis.X; p[5] = axis.Y;
        p[6] = joint.EnableLimit ? 1 : 0; p[7] = lower; p[8] = upper;
        p[9] = joint.EnableMotor ? 1 : 0; p[10] = NativeMotorSpeed(joint.Kind, joint.MotorSpeed); p[11] = NativeMotorForce(joint.Kind, joint.MaxMotorForce);
        p[12] = joint.EnableSpring ? 1 : 0; p[13] = joint.Hertz; p[14] = joint.DampingRatio;
        p[15] = joint.Length < 0 ? -1 : Meters(joint.Length);
        p[16] = joint.MaxForce / _unitsPerMeter;
        int native = Native.me_joint_create(world, (int)joint.Kind, nativeA, b.Native, p, joint.CollideConnected ? 1 : 0);
        if (native < 0) throw new InvalidOperationException("Box2D could not create the joint.");
        var description = joint;
        if (joint.Kind == JointKind.Distance && joint.Length < 0)
        {
            float* state = stackalloc float[8];
            Native.me_joint_state(world, native, state);
            description = joint with { Length = state[7] * _unitsPerMeter };
        }
        int slot;
        if (_freeJoints.Count > 0) slot = _freeJoints.Pop();
        else
        {
            slot = _jointSlots.Count;
            _jointSlots.Add(new Joint());
        }
        var j = _jointSlots[slot];
        j.Generation++;
        j.Alive = true;
        j.Native = native;
        j.Description = description;
        var id = new JointId((uint)slot, j.Generation);
        if (_liveJointCount == _liveJoints.Length) Array.Resize(ref _liveJoints, _liveJoints.Length * 2);
        _liveJoints[_liveJointCount++] = id;
        return id;
    }

    /// <inheritdoc/>
    public void DestroyJoint(JointId joint)
    {
        if (!IsAlive(joint)) return;
        Native.me_joint_destroy(World, _jointSlots[(int)joint.Index].Native);
        Forget(joint);
    }

    /// <summary>Marks a joint dead and gives its slot back.</summary>
    private void Forget(JointId joint)
    {
        _jointSlots[(int)joint.Index].Alive = false;
        int at = Array.IndexOf(_liveJoints, joint, 0, _liveJointCount);
        Array.Copy(_liveJoints, at + 1, _liveJoints, at, _liveJointCount - at - 1);
        _liveJointCount--;
        _freeJoints.Push((int)joint.Index);
    }

    /// <inheritdoc/>
    public JointDescription DescriptionOf(JointId joint) => Resolve(joint).Description;

    /// <inheritdoc/>
    public Vector2 AnchorAOf(JointId joint)
    {
        float* s = stackalloc float[8];
        Native.me_joint_state(World, Resolve(joint).Native, s);
        return FromMeters(s[0], s[1]);
    }

    /// <inheritdoc/>
    public Vector2 AnchorBOf(JointId joint)
    {
        float* s = stackalloc float[8];
        Native.me_joint_state(World, Resolve(joint).Native, s);
        return FromMeters(s[2], s[3]);
    }

    /// <inheritdoc/>
    public Vector2 ReactionForceOf(JointId joint)
    {
        float* s = stackalloc float[8];
        Native.me_joint_state(World, Resolve(joint).Native, s);
        return FromMeters(s[4], s[5]);
    }

    /// <inheritdoc/>
    public float ReactionTorqueOf(JointId joint)
    {
        float* s = stackalloc float[8];
        Native.me_joint_state(World, Resolve(joint).Native, s);
        return s[6] * _ySign * _unitsPerMeter * _unitsPerMeter;
    }

    /// <inheritdoc/>
    public float JointCoordinateOf(JointId joint)
    {
        var j = Resolve(joint);
        float* s = stackalloc float[8];
        Native.me_joint_state(World, j.Native, s);
        return j.Description.Kind switch
        {
            JointKind.Revolute => s[7] * _ySign,
            JointKind.Distance or JointKind.Prismatic => s[7] * _unitsPerMeter,
            _ => 0,
        };
    }

    /// <inheritdoc/>
    public void SetJointLimit(JointId joint, bool enabled, float lower, float upper)
    {
        var j = Resolve(joint);
        var kind = j.Description.Kind;
        if (kind is JointKind.Weld or JointKind.Target) throw new InvalidOperationException($"A {kind} joint has no limit.");
        JointDescription.ValidateLimit(lower, upper);
        NativeLimit(kind, lower, upper, out float nativeLower, out float nativeUpper);
        Native.me_joint_set_limit(World, j.Native, enabled ? 1 : 0, nativeLower, nativeUpper);
        j.Description = j.Description with { EnableLimit = enabled, Lower = lower, Upper = upper };
    }

    /// <inheritdoc/>
    public void SetJointMotor(JointId joint, bool enabled, float speed, float maxForce)
    {
        var j = Resolve(joint);
        var kind = j.Description.Kind;
        if (kind is JointKind.Weld or JointKind.Target) throw new InvalidOperationException($"A {kind} joint has no motor.");
        Finite(speed, nameof(speed));
        if (!(maxForce >= 0)) throw new ArgumentOutOfRangeException(nameof(maxForce), maxForce, "The value must be zero or more.");
        Native.me_joint_set_motor(World, j.Native, enabled ? 1 : 0, NativeMotorSpeed(kind, speed), NativeMotorForce(kind, maxForce));
        j.Description = j.Description with { EnableMotor = enabled, MotorSpeed = speed, MaxMotorForce = maxForce };
    }

    /// <inheritdoc/>
    public void SetJointSpring(JointId joint, bool enabled, float hertz, float dampingRatio)
    {
        var j = Resolve(joint);
        if (!(hertz >= 0)) throw new ArgumentOutOfRangeException(nameof(hertz), hertz, "The value must be zero or more.");
        if (!(dampingRatio >= 0)) throw new ArgumentOutOfRangeException(nameof(dampingRatio), dampingRatio, "The value must be zero or more.");
        Native.me_joint_set_spring(World, j.Native, enabled ? 1 : 0, hertz, dampingRatio);
        j.Description = j.Description with { EnableSpring = enabled, Hertz = hertz, DampingRatio = dampingRatio };
    }

    /// <inheritdoc/>
    public void SetJointTarget(JointId joint, Vector2 target)
    {
        var j = Resolve(joint);
        if (j.Description.Kind != JointKind.Target) throw new InvalidOperationException($"Only a target joint has a target: {joint} is a {j.Description.Kind} joint.");
        Finite(target, nameof(target));
        var t = ToMeters(target);
        Native.me_joint_set_target(World, j.Native, t.X, t.Y);
        j.Description = j.Description with { TargetPoint = target };
    }

    /// <inheritdoc/>
    public void SetJointLength(JointId joint, float length)
    {
        var j = Resolve(joint);
        if (j.Description.Kind != JointKind.Distance) throw new InvalidOperationException($"Only a distance joint has a length: {joint} is a {j.Description.Kind} joint.");
        if (!(length >= 0) || float.IsInfinity(length)) throw new ArgumentOutOfRangeException(nameof(length), length, "The length must be finite and zero or more.");
        Native.me_joint_set_length(World, j.Native, Meters(length));
        j.Description = j.Description with { Length = length };
    }

    /// <summary>Destroys the joints whose reaction force passed their break force in the step that just ran.</summary>
    private void BreakJoints(nint world)
    {
        _brokenJoints.Clear();
        float* s = stackalloc float[8];
        for (int i = _liveJointCount - 1; i >= 0; i--)
        {
            var id = _liveJoints[i];
            var j = _jointSlots[(int)id.Index];
            if (!(j.Description.BreakForce > 0)) continue;
            Native.me_joint_state(world, j.Native, s);
            if (MathF.Sqrt(s[4] * s[4] + s[5] * s[5]) * _unitsPerMeter <= j.Description.BreakForce) continue;
            Native.me_joint_destroy(world, j.Native);
            Forget(id);
            _brokenJoints.Add(id);
        }
        _brokenJoints.Reverse();
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
    public BodyId BodyOf(ShapeId shape)
    {
        if (!IsAlive(shape)) throw new InvalidOperationException($"{shape} is not a live shape of this world.");
        int b = _shapeSlots[(int)shape.Index].Body;
        return new BodyId((uint)b, _bodySlots[b].Generation);
    }

    /// <inheritdoc/>
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

    // ------------------------------------------------------------------ shape queries

    private const int HitBatch = 32;

    /// <inheritdoc/>
    public int RayCastAll(Vector2 from, Vector2 to, in QueryFilter filter, Span<RayHit> hits)
    {
        Finite(from, nameof(from));
        Finite(to, nameof(to));
        var a = ToMeters(from);
        var t = ToMeters(to) - a;
        var world = World;
        int room = Math.Max(hits.Length, 1);
        int* shapes = stackalloc int[HitBatch];
        float* values = stackalloc float[5 * HitBatch];
        if (room <= HitBatch)
        {
            int total = Native.me_ray_cast_all(world, a.X, a.Y, t.X, t.Y, filter.Category, filter.Mask, QueryFlags(filter), shapes, values, room);
            WriteHits(shapes, values, Math.Min(total, room), hits);
            return total;
        }
        var manyShapes = new int[room];
        var manyValues = new float[5 * room];
        fixed (int* ps = manyShapes)
        fixed (float* pv = manyValues)
        {
            int total = Native.me_ray_cast_all(world, a.X, a.Y, t.X, t.Y, filter.Category, filter.Mask, QueryFlags(filter), ps, pv, room);
            WriteHits(ps, pv, Math.Min(total, room), hits);
            return total;
        }
    }

    private void WriteHits(int* shapes, float* values, int count, Span<RayHit> hits)
    {
        for (int i = 0; i < count && i < hits.Length; i++)
        {
            int slot = SlotOf(shapes[i]);
            float* r = values + 5 * i;
            hits[i] = slot < 0 ? default : new RayHit(IdOf(slot), OwnerOfShape(slot), FromMeters(r[0], r[1]), Direction(r[2], r[3]), r[4]);
        }
    }

    private void Rounded(in QueryShape shape, out float hx, out float hy, out float radius)
    {
        shape.Validate();
        var (half, r) = shape.Rounded;
        hx = Meters(half.X);
        hy = Meters(half.Y);
        radius = Meters(r);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The shape stops within a Box2D slop (half a centimetre) of the surface: short of it for a box, and up to that
    /// far into it for a circle or a capsule. A surface nearer than that where the cast starts counts as overlapped
    /// when the two are less than a millimetre apart, and as hit at once otherwise.
    /// </remarks>
    public bool ShapeCast(in QueryShape shape, Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit)
    {
        Rounded(shape, out float hx, out float hy, out float radius);
        Finite(from, nameof(from));
        Finite(to, nameof(to));
        var a = ToMeters(from);
        var t = ToMeters(to) - a;
        float* r = stackalloc float[5];
        int slot = SlotOf(Native.me_shape_cast(World, hx, hy, radius, a.X, a.Y, t.X, t.Y, filter.Category, filter.Mask, QueryFlags(filter), r));
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
    public int Overlap(in QueryShape shape, Vector2 center, in QueryFilter filter, Span<ShapeId> results)
    {
        Rounded(shape, out float hx, out float hy, out float radius);
        Finite(center, nameof(center));
        return Overlaps(true, hx, hy, radius, ToMeters(center), filter, results);
    }

    /// <inheritdoc/>
    /// <remarks>Shapes are returned in creation order.</remarks>
    public int OverlapPoint(Vector2 point, in QueryFilter filter, Span<ShapeId> results)
    {
        Finite(point, nameof(point));
        return Overlaps(false, 0, 0, 0, ToMeters(point), filter, results);
    }

    /// <summary>Runs a shape or point overlap into the scratch buffer (grown when the caller has room for more) and writes the live shapes.</summary>
    private int Overlaps(bool shape, float hx, float hy, float radius, Vector2 c, in QueryFilter filter, Span<ShapeId> results)
    {
        var world = World;
        int flags = QueryFlags(filter), total;
        while (true)
        {
            fixed (int* found = _overlap)
                total = shape
                    ? Native.me_overlap_shape(world, hx, hy, radius, c.X, c.Y, filter.Category, filter.Mask, flags, found, _overlap.Length)
                    : Native.me_overlap_point(world, c.X, c.Y, filter.Category, filter.Mask, flags, found, _overlap.Length);
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

    /// <inheritdoc/>
    public bool ClosestPoint(Vector2 point, float maxDistance, in QueryFilter filter, out ClosestPointHit hit)
    {
        Finite(point, nameof(point));
        if (!(maxDistance >= 0) || float.IsInfinity(maxDistance)) throw new ArgumentOutOfRangeException(nameof(maxDistance), maxDistance, "The distance must be finite and zero or more.");
        var p = ToMeters(point);
        float* r = stackalloc float[3];
        int slot = SlotOf(Native.me_closest_point(World, p.X, p.Y, Meters(maxDistance), filter.Category, filter.Mask, QueryFlags(filter), r));
        if (slot < 0)
        {
            hit = default;
            return false;
        }
        float distance = r[2] * _unitsPerMeter;
        hit = new ClosestPointHit(IdOf(slot), OwnerOfShape(slot), distance == 0 ? point : FromMeters(r[0], r[1]), distance);
        return true;
    }

    // ------------------------------------------------------------------ contact detail

    /// <summary>The impacts of the latest step, in the order Box2D reports them; the speed that makes a contact an impact is <see cref="Box2DWorldOptions.ImpactSpeed"/>.</summary>
    public ReadOnlySpan<ContactImpact> ContactImpacts => CollectionsMarshal.AsSpan(_impacts);

    /// <inheritdoc/>
    /// <remarks>Impulses are in kilograms × world units per second. Box2D keeps contacts between a dynamic body and anything else only.</remarks>
    public int ContactsOf(BodyId body, Span<ContactPoint> results)
    {
        var b = Resolve(body);
        var world = World;
        int room = Math.Max(results.Length, 1);
        if (room <= HitBatch)
        {
            int* shapes = stackalloc int[2 * HitBatch];
            float* values = stackalloc float[6 * HitBatch];
            int total = Native.me_body_contacts(world, b.Native, shapes, values, room);
            WritePoints(shapes, values, Math.Min(total, room), results);
            return total;
        }
        var manyShapes = new int[2 * room];
        var manyValues = new float[6 * room];
        fixed (int* ps = manyShapes)
        fixed (float* pv = manyValues)
        {
            int total = Native.me_body_contacts(world, b.Native, ps, pv, room);
            WritePoints(ps, pv, Math.Min(total, room), results);
            return total;
        }
    }

    private void WritePoints(int* shapes, float* values, int count, Span<ContactPoint> results)
    {
        for (int i = 0; i < count && i < results.Length; i++)
        {
            int own = SlotOf(shapes[2 * i]), other = SlotOf(shapes[2 * i + 1]);
            float* v = values + 6 * i;
            results[i] = own < 0 || other < 0 ? default
                : new ContactPoint(IdOf(own), IdOf(other), OwnerOfShape(other), FromMeters(v[0], v[1]), Direction(v[2], v[3]), v[4] * _unitsPerMeter, v[5] * _unitsPerMeter);
        }
    }

    private void ReadImpacts(nint world)
    {
        _impacts.Clear();
        int* shapes = stackalloc int[2 * EventBatch];
        float* values = stackalloc float[6 * EventBatch];
        int count = Native.me_impact_events(world, shapes, values, EventBatch);
        if (count > EventBatch)
        {
            var s = new int[2 * count];
            var v = new float[6 * count];
            fixed (int* ps = s)
            fixed (float* pv = v)
            {
                Native.me_impact_events(world, ps, pv, count);
                Impacts(ps, pv, count);
            }
            return;
        }
        Impacts(shapes, values, count);
    }

    private void Impacts(int* shapes, float* values, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int a = SlotOf(shapes[2 * i]), b = SlotOf(shapes[2 * i + 1]);
            if (a < 0 || b < 0) continue;
            float* v = values + 6 * i;
            _impacts.Add(new ContactImpact(IdOf(a), IdOf(b), OwnerOfShape(a), OwnerOfShape(b), FromMeters(v[0], v[1]), Direction(v[2], v[3]), v[4] * _unitsPerMeter, v[5] * _unitsPerMeter));
        }
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
        ReadImpacts(world);
        BreakJoints(world);
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
        float* normals = stackalloc float[4 * EventBatch];
        int* ends = stackalloc int[2 * EventBatch];
        int endCount;
        int beginCount = Native.me_contact_events(world, begins, normals, EventBatch, ends, EventBatch, &endCount);
        if (beginCount > EventBatch || endCount > EventBatch)
        {
            var b = new int[2 * beginCount];
            var n = new float[4 * beginCount];
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
            _contactEvents.Add(new ContactEvent(ContactEventKind.Begin, pair.A, pair.B, pair.OwnerA, pair.OwnerB, Direction(normals[4 * i], normals[4 * i + 1]))
            {
                Point = FromMeters(normals[4 * i + 2], normals[4 * i + 3]),
            });
        }
    }

    // ------------------------------------------------------------------ state

    /// <summary>Adds the world's simulation state: the step count and every live body (id, kind, position, angle, velocity, shapes, and its dynamics once they were set).</summary>
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
            if (b.Tuned)
            {
                // Bodies left with the default dynamics hash as they did before dynamics could be set.
                Native.me_body_get_dynamics(_world, b.Native, s);
                for (int i = 0; i < 5; i++) hash.Add(s[i]);
            }
        }
        hash.Add(_sensorPairs.Count);
        hash.Add(_contactPairs.Count);
        // A world that never had a joint hashes as it did before joints existed.
        if (_jointSlots.Count == 0) return;
        hash.Add(_liveJointCount);
        foreach (var id in Joints)
        {
            var d = _jointSlots[(int)id.Index].Description;
            hash.Add((long)id.Index);
            hash.Add((long)id.Generation);
            hash.Add((int)d.Kind);
            hash.Add((long)d.BodyA.Index);
            hash.Add((long)d.BodyB.Index);
            hash.Add(d.EnableLimit); hash.Add(d.Lower); hash.Add(d.Upper);
            hash.Add(d.EnableMotor); hash.Add(d.MotorSpeed); hash.Add(d.MaxMotorForce);
            hash.Add(d.EnableSpring); hash.Add(d.Hertz); hash.Add(d.DampingRatio);
            hash.Add(d.Length); hash.Add(d.TargetPoint); hash.Add(d.MaxForce); hash.Add(d.BreakForce);
        }
    }

    /// <summary>Destroys the native world. Every later call except <see cref="Dispose"/> throws <see cref="ObjectDisposedException"/>.</summary>
    public void Dispose()
    {
        lock (s_table)
        {
            if (_world != 0)
            {
                Native.me_world_destroy(_world);
                _world = 0;
            }
            DestroyAbandoned();
        }
        GC.SuppressFinalize(this);
    }
}
