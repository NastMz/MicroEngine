using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Physics;

/// <summary>What happened to a <see cref="CharacterController2D"/> during its latest step.</summary>
[Flags]
public enum CharacterEvents
{
    /// <summary>Nothing.</summary>
    None = 0,
    /// <summary>It was in the air and now stands on ground.</summary>
    Landed = 1,
    /// <summary>It stood on ground and no longer does (a jump, a drop, a ledge, a launch).</summary>
    LeftGround = 2,
    /// <summary>A jump started (from the ground, in coyote time, in the air or off a wall).</summary>
    Jumped = 4,
    /// <summary>Its rise was stopped by a ceiling.</summary>
    HitCeiling = 8,
    /// <summary>It began touching a wall.</summary>
    HitWall = 16,
    /// <summary>It started falling through the one-way platform it stood on.</summary>
    DroppedThrough = 32,
    /// <summary>It skidded in this step: on the ground, braking against its motion before it turns.</summary>
    Skidded = 64,
    /// <summary>Its rise was about to clip the corner of a ceiling and it was moved sideways past it.</summary>
    CornerCorrected = 128,
}

/// <summary>
/// What a character wants to do in one step: the game fills it from input, an AI from its plan. The controller never
/// reads input devices.
/// </summary>
/// <param name="Move">The sideways intent, from -1 (left) to 1 (right); values beyond are clamped.</param>
/// <param name="JumpPressed">Jump was pressed in this step.</param>
/// <param name="JumpHeld">Jump is held: a rising jump keeps its full height while it is.</param>
/// <param name="Drop">Fall through the one-way platform the character stands on (it wins over a jump there, and does nothing on solid ground).</param>
public readonly record struct CharacterInput(float Move, bool JumpPressed = false, bool JumpHeld = false, bool Drop = false);

/// <summary>
/// How a <see cref="CharacterController2D"/> moves, in the physics world's units and seconds. The defaults suit a
/// pixel-art game with 16-pixel tiles; a world in metres sets its own numbers.
/// </summary>
public sealed record CharacterMotion
{
    /// <summary>Downward acceleration, in units per second squared.</summary>
    public float Gravity { get; init; } = 900;
    /// <summary>The fastest fall, in units per second.</summary>
    public float MaxFallSpeed { get; init; } = 400;
    /// <summary>The sideways speed at full intent, in units per second.</summary>
    public float MoveSpeed { get; init; } = 90;
    /// <summary>How fast the sideways speed grows toward the intent, in units per second squared; infinite (the default) reaches it at once.</summary>
    public float Acceleration { get; init; } = float.PositiveInfinity;
    /// <summary>How fast the sideways speed falls to zero with no intent, in units per second squared; infinite (the default) stops at once.</summary>
    public float Deceleration { get; init; } = float.PositiveInfinity;
    /// <summary>The share of <see cref="Acceleration"/> and <see cref="Deceleration"/> that applies in the air, from 0 (no air control) to 1 (the default).</summary>
    public float AirControl { get; init; } = 1;
    /// <summary>The upward speed a jump starts with, in units per second: the jump rises about <c>JumpSpeed² / (2 × Gravity)</c>.</summary>
    public float JumpSpeed { get; init; } = 300;
    /// <summary>
    /// Gravity is multiplied by this while a jump rises with jump released, which makes a tap a short hop (variable
    /// jump height); 1 gives every jump the same height.
    /// </summary>
    public float JumpReleaseGravityScale { get; init; } = 2.5f;
    /// <summary>Gravity is multiplied by this while falling; above 1 makes falls faster than rises.</summary>
    public float FallGravityScale { get; init; } = 1;
    /// <summary>How long after leaving a ledge a jump is still allowed, in seconds.</summary>
    public float CoyoteTime { get; init; } = 0.1f;
    /// <summary>How long a jump pressed in the air is remembered and performed on landing, in seconds.</summary>
    public float JumpBuffer { get; init; } = 0.1f;
    /// <summary>How long one-way platforms are passed through after a drop, in seconds.</summary>
    public float DropThroughTime { get; init; } = 0.2f;
    /// <summary>Jumps allowed in the air before landing again (1 is a double jump); 0 by default.</summary>
    public int AirJumps { get; init; }
    /// <summary>The fastest fall while pushing against a wall, in units per second; 0 (the default) turns wall sliding off.</summary>
    public float WallSlideSpeed { get; init; }
    /// <summary>The speed of a jump off a wall: X away from the wall, Y upward, in units per second; zero (the default) turns wall jumps off.</summary>
    public Vector2 WallJumpSpeed { get; init; }
    /// <summary>How long after a wall jump the sideways intent is ignored, so the jump carries the character away from the wall, in seconds.</summary>
    public float WallJumpLockTime { get; init; } = 0.15f;
    /// <summary>
    /// How far down the character is pulled to stay on ground that falls away under it when it walks (going down a
    /// slope or a step), in units; 0 (the default) turns it off. A character that walks off a drop taller than this
    /// leaves the ground.
    /// </summary>
    public float GroundSnap { get; init; }

    /// <summary>
    /// Gravity is multiplied by this near the top of a jump, while the vertical speed is under
    /// <see cref="ApexSpeed"/>: on the way up unless the jump was released, and on the way down while jump is held.
    /// Under 1 it makes the top of a jump float; 1 (the default) has no such phase.
    /// </summary>
    public float ApexGravityScale { get; init; } = 1;
    /// <summary>The vertical speed under which <see cref="ApexGravityScale"/> applies, in units per second; 0 (the default) turns the phase off.</summary>
    public float ApexSpeed { get; init; }
    /// <summary>
    /// How fast a character on the ground brakes when the intent is against its motion and it moves faster than
    /// <see cref="SkidSpeed"/>, in units per second squared: it skids to that speed before it turns
    /// (<see cref="CharacterController2D.IsSkidding"/>). 0 (the default) turns skidding off: a reversal is ordinary
    /// acceleration.
    /// </summary>
    public float SkidDeceleration { get; init; }
    /// <summary>The sideways speed above which reversing is a skid, in units per second.</summary>
    public float SkidSpeed { get; init; }
    /// <summary>
    /// How far a rising character is moved sideways to get past the corner of a ceiling that only its edge would
    /// clip, in units; 0 (the default) turns it off. It is moved the least of four equal parts of this that frees the
    /// way, and only where there is room for it.
    /// </summary>
    public float CornerCorrection { get; init; }
    /// <summary>The downward speed a character leaves a ceiling with, in units per second; 0 (the default) leaves it to gravity.</summary>
    public float CeilingReboundSpeed { get; init; }
    /// <summary>Extra jump speed at full <see cref="MoveSpeed"/>, in proportion to the sideways speed when the jump starts, in units per second: a run-up jumps higher.</summary>
    public float RunJumpBonus { get; init; }

    /// <summary>Throws when a value is out of range.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A negative or not-a-number value, a non-finite speed or time, or an air control outside 0..1.</exception>
    public void Validate()
    {
        Finite(Gravity, nameof(Gravity));
        Finite(MaxFallSpeed, nameof(MaxFallSpeed));
        Finite(MoveSpeed, nameof(MoveSpeed));
        NotNegative(Acceleration, nameof(Acceleration));
        NotNegative(Deceleration, nameof(Deceleration));
        if (!(AirControl >= 0 && AirControl <= 1)) throw new ArgumentOutOfRangeException(nameof(AirControl), AirControl, "Air control is between 0 and 1.");
        Finite(JumpSpeed, nameof(JumpSpeed));
        Finite(JumpReleaseGravityScale, nameof(JumpReleaseGravityScale));
        Finite(FallGravityScale, nameof(FallGravityScale));
        Finite(CoyoteTime, nameof(CoyoteTime));
        Finite(JumpBuffer, nameof(JumpBuffer));
        Finite(DropThroughTime, nameof(DropThroughTime));
        ArgumentOutOfRangeException.ThrowIfNegative(AirJumps);
        Finite(WallSlideSpeed, nameof(WallSlideSpeed));
        Finite(WallJumpSpeed.X, nameof(WallJumpSpeed));
        Finite(WallJumpSpeed.Y, nameof(WallJumpSpeed));
        Finite(WallJumpLockTime, nameof(WallJumpLockTime));
        Finite(GroundSnap, nameof(GroundSnap));
        Finite(ApexGravityScale, nameof(ApexGravityScale));
        Finite(ApexSpeed, nameof(ApexSpeed));
        Finite(SkidDeceleration, nameof(SkidDeceleration));
        Finite(SkidSpeed, nameof(SkidSpeed));
        Finite(CornerCorrection, nameof(CornerCorrection));
        Finite(CeilingReboundSpeed, nameof(CeilingReboundSpeed));
        Finite(RunJumpBonus, nameof(RunJumpBonus));
    }

    internal static void NotNegative(float value, string name)
    {
        if (!(value >= 0)) throw new ArgumentOutOfRangeException(name, value, "The value must be zero or more.");
    }

    internal static void Finite(float value, string name)
    {
        if (!(value >= 0) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(name, value, "The value must be finite and zero or more.");
    }
}

/// <summary>
/// A side-view character: one kinematic body moved with <see cref="IPhysicsWorld2D.Move"/> under gravity, with
/// variable jump height, coyote time, jump buffering, drops through one-way platforms, optional air jumps, wall slides
/// and wall jumps, and the ground, wall and ceiling state a game reads. It works on every backend, since it uses only
/// the common contract (and one-way platforms where the backend declares them); it takes intent
/// (<see cref="CharacterInput"/>) instead of reading input, so a player and an AI drive it the same way.
/// </summary>
/// <remarks>
/// <para>Call <see cref="Step"/> once per fixed step, before the physics world steps. A character that stands still
/// is not moved at all; one that stands on a body that moved since its last step (a platform the game moved) is moved
/// with it first, unless the backend already carried it (<see cref="PhysicsCapabilities.MovingSolids"/>).</para>
/// <para>Deterministic: its state is its velocity, its timers and what it touches, all in
/// <see cref="HashState"/>; add it to the state hash of whatever owns it.</para>
/// </remarks>
/// <example>
/// <code>
/// var hero = CharacterController2D.Create(physics, entity, new Vector2(40, 100), new Vector2(12, 14));
/// // each fixed step
/// hero.Step(dt, new CharacterInput(Input.Axis("left", "right"), Input.Pressed("jump"), Input.Down("jump"), Input.Down("down")));
/// if ((hero.Events &amp; CharacterEvents.Landed) != 0) PlayThud();
/// </code>
/// </example>
public sealed class CharacterController2D : IStateHashed
{
    private readonly Vector2 _up, _right;
    private CharacterMotion _motion;
    private float _run, _rise;
    private float _coyote, _buffer, _drop, _lock, _air;
    private Func<ShapeId, bool>? _where;
    private int _airJumps;
    private BodyId _groundBody;
    private Vector2 _groundAt, _restAt;

    /// <summary>Drives <paramref name="body"/>, a kinematic body with one non-sensor shape.</summary>
    /// <param name="world">The physics world.</param>
    /// <param name="body">The character's body.</param>
    /// <param name="motion">How it moves; the defaults when null.</param>
    /// <exception cref="InvalidOperationException">The body is stale or not kinematic.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value of <paramref name="motion"/> is out of range.</exception>
    public CharacterController2D(IPhysicsWorld2D world, BodyId body, CharacterMotion? motion = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.KindOf(body) != BodyKind.Kinematic) throw new InvalidOperationException($"A character is a kinematic body: {body} is {world.KindOf(body)}.");
        _motion = motion ?? new CharacterMotion();
        _motion.Validate();
        World = world;
        Body = body;
        _up = world.Up;
        _right = Sideways(_up);
        _restAt = world.PositionOf(body);
    }

    /// <summary>
    /// Creates a kinematic body with one box of <paramref name="size"/> centred at <paramref name="position"/> and a
    /// controller for it, moving through the layers of <paramref name="mask"/>.
    /// </summary>
    /// <param name="world">The physics world.</param>
    /// <param name="owner">The entity that owns the body.</param>
    /// <param name="position">The centre of the box.</param>
    /// <param name="size">The box's width and height.</param>
    /// <param name="motion">How it moves; the defaults when null.</param>
    /// <param name="category">The layers the character is in.</param>
    /// <param name="mask">The layers it collides with.</param>
    public static CharacterController2D Create(IPhysicsWorld2D world, EntityId owner, Vector2 position, Vector2 size, CharacterMotion? motion = null,
        ulong category = CollisionLayers.Default, ulong mask = CollisionLayers.All)
    {
        ArgumentNullException.ThrowIfNull(world);
        var shape = ShapeDescription.Box(size).InLayers(category, mask);
        shape.Validate();
        (motion ?? new CharacterMotion()).Validate();
        var body = world.CreateBody(owner, BodyDescription.Kinematic(position));
        world.AddShape(body, shape);
        return new CharacterController2D(world, body, motion) { Filter = QueryFilter.For(shape) };
    }

    /// <summary>The direction characters run toward with a positive intent: across <paramref name="up"/>, toward +x.</summary>
    internal static Vector2 Sideways(Vector2 up)
    {
        var across = Vector2.UnitX - up * Vector2.Dot(Vector2.UnitX, up);
        return across.LengthSquared() > 1e-6f ? Vector2.Normalize(across) : Vector2.UnitY;
    }

    /// <summary>The physics world.</summary>
    public IPhysicsWorld2D World { get; }
    /// <summary>The character's body.</summary>
    public BodyId Body { get; }
    /// <summary>How the character moves; replace it to change its feel (a power-up, water).</summary>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
    public CharacterMotion Motion
    {
        get => _motion;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            value.Validate();
            _motion = value;
        }
    }
    /// <summary>The shapes that block the character (<see cref="IPhysicsWorld2D.Move"/>'s filter); the default filter unless set.</summary>
    public QueryFilter Filter { get; set; }
    /// <summary>The body's origin.</summary>
    public Vector2 Position => World.PositionOf(Body);
    /// <summary>The character's velocity, in units per second.</summary>
    public Vector2 Velocity
    {
        get => _right * _run + _up * _rise;
        set
        {
            _run = Vector2.Dot(value, _right);
            _rise = Vector2.Dot(value, _up);
        }
    }
    /// <summary>Whether it stands on ground.</summary>
    public bool IsGrounded { get; private set; }
    /// <summary>The shape it stands on; <see cref="ShapeId.None"/> in the air.</summary>
    public ShapeId Ground { get; private set; }
    /// <summary>Whether it touches a wall.</summary>
    public bool IsOnWall { get; private set; }
    /// <summary>The normal of the wall it touches, pointing away from the wall; zero when it touches none.</summary>
    public Vector2 WallNormal { get; private set; }
    /// <summary>Whether a jump is rising and can still be cut short by releasing jump.</summary>
    public bool IsJumping { get; private set; }
    /// <summary>Whether it skids: on the ground, braking against its motion before it turns (<see cref="CharacterMotion.SkidDeceleration"/>).</summary>
    public bool IsSkidding { get; private set; }
    /// <summary>
    /// How long it has been in the air, in seconds; in the step it lands, the length of the flight that ended (for a
    /// landing effect); 0 on the ground after that.
    /// </summary>
    public float AirTime => _air;
    /// <summary>The downward speed it had when it last landed, in units per second (for a landing's dust, sound or damage).</summary>
    public float LandingSpeed { get; private set; }
    /// <summary>
    /// Decides, shape by shape, what is solid for this character, beyond what the layers of <see cref="Filter"/> say:
    /// it is asked about each shape near the character before each of its moves, with the character as it is then
    /// (its <see cref="Velocity"/> is the one it is about to move with), and a shape it returns false for is passed
    /// through. For solids that depend on how they are met: a hidden block that only a head coming from below finds,
    /// a gate that lets one character through. Null (the default) asks nothing. It needs a backend that declares
    /// <see cref="PhysicsCapabilities.FilterPredicates"/>; it may read the world but not query or change it.
    /// </summary>
    public Func<CharacterController2D, ShapeId, bool>? Solid { get; set; }
    /// <summary>What happened in the latest <see cref="Step"/>.</summary>
    public CharacterEvents Events { get; private set; }
    /// <summary>The result of the latest step's move: every surface the character touches.</summary>
    public MoveResult LastMove { get; private set; }

    /// <summary>
    /// Throws the character with <paramref name="velocity"/> (a spring, a knock-back): it leaves the ground, and the
    /// rise is not cut short by a released jump button.
    /// </summary>
    public void Launch(Vector2 velocity)
    {
        Velocity = velocity;
        IsJumping = false;
        IsGrounded = false;
        Ground = ShapeId.None;
        _groundBody = BodyId.None;
        _coyote = 0;
    }

    /// <summary>Places the character at <paramref name="position"/> without collision; it keeps its velocity and is in the air until its next step.</summary>
    public void Teleport(Vector2 position)
    {
        World.SetPosition(Body, position);
        _restAt = position;
        IsGrounded = false;
        Ground = ShapeId.None;
        _groundBody = BodyId.None;
        _coyote = 0;
    }

    /// <summary>Advances the character by <paramref name="dt"/> seconds with <paramref name="input"/> and returns the move's result (also <see cref="LastMove"/>).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dt"/> is negative or not finite.</exception>
    public MoveResult Step(float dt, in CharacterInput input)
    {
        if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt), dt, "The step must be finite and not negative.");
        Events = CharacterEvents.None;
        if (dt == 0) return LastMove;
        var m = _motion;
        bool wasGrounded = IsGrounded, wasOnWall = IsOnWall;
        bool carried = Carry();

        // Sideways speed toward the intent.
        float axis = float.IsNaN(input.Move) ? 0 : Math.Clamp(input.Move, -1, 1);
        float control = wasGrounded ? 1 : m.AirControl;
        _lock = MathF.Max(0, _lock - dt);
        IsSkidding = false;
        if (control > 0 && _lock <= 0)
        {
            if (wasGrounded && m.SkidDeceleration > 0 && axis * _run < 0 && MathF.Abs(_run) > m.SkidSpeed)
            {
                // Reversing at speed: brake before turning.
                _run = MathX.Approach(_run, 0, m.SkidDeceleration * dt);
                IsSkidding = true;
                Events |= CharacterEvents.Skidded;
            }
            else _run = MathX.Approach(_run, axis * m.MoveSpeed, (axis != 0 ? m.Acceleration : m.Deceleration) * control * dt);
        }

        // Timers: the drop window, coyote time and the jump buffer.
        _drop = MathF.Max(0, _drop - dt);
        _coyote = wasGrounded ? m.CoyoteTime : MathF.Max(0, _coyote - dt);
        _buffer = input.JumpPressed ? m.JumpBuffer : MathF.Max(0, _buffer - dt);
        bool acted = false;
        if (input.Drop && wasGrounded && !World.Move(Body, Vector2.Zero, Active(passOneWay: true)).OnGround)
        {
            // Only one-way platforms hold it up: fall through them.
            _drop = m.DropThroughTime;
            _buffer = 0;
            _coyote = 0;
            IsGrounded = false;
            acted = true;
            Events |= CharacterEvents.DroppedThrough;
        }
        else if (input.JumpPressed || _buffer > 0)
        {
            if (wasGrounded || _coyote > 0) acted = Jump(m.JumpSpeed + (m.MoveSpeed > 0 ? m.RunJumpBonus * MathF.Min(1, MathF.Abs(_run) / m.MoveSpeed) : 0));
            else if (input.JumpPressed && wasOnWall && m.WallJumpSpeed != Vector2.Zero)
            {
                _run = MathF.Sign(Vector2.Dot(WallNormal, _right)) * m.WallJumpSpeed.X;
                _lock = m.WallJumpLockTime;
                acted = Jump(m.WallJumpSpeed.Y);
            }
            else if (input.JumpPressed && _airJumps < m.AirJumps)
            {
                _airJumps++;
                acted = Jump(m.JumpSpeed);
            }
        }

        var filter = Active(_drop > 0);
        MoveResult result;
        // A character at rest is probed, not moved: it neither sinks nor creeps down a slope.
        if (!(wasGrounded && !acted && !carried && _run == 0 && _rise <= 0 && (result = World.Move(Body, Vector2.Zero, filter)).OnGround))
        {
            // Gravity by phase: rising, rising with jump released, near the top, falling.
            float scale = _rise > 0
                ? (IsJumping && !input.JumpHeld ? m.JumpReleaseGravityScale : _rise < m.ApexSpeed ? m.ApexGravityScale : 1)
                : (input.JumpHeld && !wasGrounded && -_rise < m.ApexSpeed ? m.ApexGravityScale : m.FallGravityScale);
            _rise = MathF.Max(_rise - m.Gravity * scale * dt, -m.MaxFallSpeed);
            if (m.WallSlideSpeed > 0 && wasOnWall && !wasGrounded && _rise < 0 && axis * Vector2.Dot(WallNormal, _right) < 0)
                _rise = MathF.Max(_rise, -m.WallSlideSpeed);
            if (_rise <= 0) IsJumping = false;
            var delta = _right * (_run * dt) + _up * (_rise * dt);
            var from = m.CornerCorrection > 0 && _rise > 0 ? World.PositionOf(Body) : default;
            result = World.Move(Body, delta, filter);
            if (m.CornerCorrection > 0 && _rise > 0 && result.HitCeiling && CorrectCorner(from, delta, filter, ref result))
                Events |= CharacterEvents.CornerCorrected;
        }

        // Walls: the first one touched, and no speed into any of them.
        bool onWall = false;
        var wallNormal = Vector2.Zero;
        foreach (ref readonly var contact in result.Contacts)
        {
            float along = Vector2.Dot(contact.Normal, _up);
            if (along >= MoveResult.GroundThreshold || along <= -MoveResult.GroundThreshold) continue;
            if (!onWall) wallNormal = contact.Normal;
            onWall = true;
            if (Vector2.Dot(contact.Normal, _right) * _run < 0) _run = 0;
        }
        if (onWall && !wasOnWall) Events |= CharacterEvents.HitWall;
        IsOnWall = onWall;
        WallNormal = wallNormal;

        if (result.HitCeiling && _rise > 0)
        {
            _rise = m.CeilingReboundSpeed > 0 ? -m.CeilingReboundSpeed : 0;
            IsJumping = false;
            Events |= CharacterEvents.HitCeiling;
        }

        bool grounded = result.OnGround && _rise <= 0;
        if (!grounded && wasGrounded && !acted && _rise <= 0 && m.GroundSnap > 0)
        {
            // The ground fell away under a walking character: follow it down when it is within reach.
            var before = result.Position;
            var snap = World.Move(Body, _up * -m.GroundSnap, filter);
            if (snap.OnGround)
            {
                result = snap;
                grounded = true;
            }
            else World.SetPosition(Body, before);
        }
        if (grounded)
        {
            if (!wasGrounded)
            {
                Events |= CharacterEvents.Landed;
                LandingSpeed = MathF.Max(0, -_rise);
            }
            else _air = 0;
            if (_rise < 0) _rise = 0;
            _airJumps = 0;
            IsJumping = false;
        }
        else
        {
            if (wasGrounded) Events |= CharacterEvents.LeftGround;
            _air = wasGrounded ? dt : _air + dt;
        }

        IsGrounded = grounded;
        Ground = grounded ? result.Ground : ShapeId.None;
        LastMove = result;
        _restAt = World.PositionOf(Body);
        if (grounded && World.IsAlive(Ground))
        {
            _groundBody = World.BodyOf(Ground);
            _groundAt = World.PositionOf(_groundBody);
        }
        else _groundBody = BodyId.None;
        return result;
    }

    private bool Jump(float speed)
    {
        _rise = speed;
        IsJumping = true;
        IsGrounded = false;
        _coyote = 0;
        _buffer = 0;
        Events |= CharacterEvents.Jumped;
        return true;
    }

    /// <summary>Follows the body the character stands on when the game moved it since the last step; false when there was nothing to follow.</summary>
    private bool Carry()
    {
        if (!IsGrounded || _groundBody.IsNone || !World.IsAlive(_groundBody)) return false;
        var delta = World.PositionOf(_groundBody) - _groundAt;
        if (delta == Vector2.Zero) return false;
        // Already moved since its step (a pushing solid carries its riders, a teleport): nothing to add.
        if (World.PositionOf(Body) != _restAt) return true;
        World.Move(Body, delta, Active(passOneWay: false));
        return true;
    }

    /// <summary>The filter of this step's moves: the character's, with what <see cref="Solid"/> leaves out.</summary>
    private QueryFilter Active(bool passOneWay)
    {
        var filter = passOneWay ? Filter with { PassOneWay = true } : Filter;
        if (Solid is null) return filter;
        _where ??= shape => (Filter.Where is null || Filter.Where(shape)) && (Solid is null || Solid(this, shape));
        return filter with { Where = _where };
    }

    /// <summary>
    /// A rise that a ceiling cut short: tries the same move from a little to each side, nearest first, and keeps the
    /// first that meets no ceiling. False (and the body where the blocked move left it) when none does.
    /// </summary>
    private bool CorrectCorner(Vector2 from, Vector2 delta, in QueryFilter filter, ref MoveResult result)
    {
        var blocked = result.Position;
        float reach = _motion.CornerCorrection;
        for (int step = 1; step <= 4; step++)
        for (int side = -1; side <= 1; side += 2)
        {
            var nudge = _right * (side * reach * step / 4);
            World.SetPosition(Body, from);
            var across = World.Move(Body, nudge, filter);
            // The nudge itself must be free: no wall on that side.
            if (Vector2.DistanceSquared(across.Delta, nudge) > 1e-6f * nudge.LengthSquared()) continue;
            var up = World.Move(Body, delta, filter);
            if (up.HitCeiling) continue;
            result = up;
            return true;
        }
        World.SetPosition(Body, blocked);
        return false;
    }

    /// <summary>Adds the character's state: velocity, ground, wall, jump and skid state, timers, air time, the last landing's speed, air jumps used and the latest events.</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(_run);
        hash.Add(_rise);
        hash.Add(IsGrounded);
        hash.Add(IsOnWall);
        hash.Add(WallNormal);
        hash.Add(IsJumping);
        hash.Add(_coyote);
        hash.Add(_buffer);
        hash.Add(_drop);
        hash.Add(_lock);
        hash.Add(_airJumps);
        hash.Add((int)Events);
        hash.Add((long)Ground.Index);
        hash.Add((long)Ground.Generation);
        hash.Add(_groundAt);
        hash.Add(_restAt);
        hash.Add(IsSkidding);
        hash.Add(_air);
        hash.Add(LandingSpeed);
    }
}

/// <summary>How a <see cref="TopDownController2D"/> moves, in the physics world's units and seconds.</summary>
public sealed record TopDownMotion
{
    /// <summary>The speed at full intent, in units per second.</summary>
    public float MoveSpeed { get; init; } = 90;
    /// <summary>How fast the velocity grows toward the intent, in units per second squared; infinite (the default) reaches it at once.</summary>
    public float Acceleration { get; init; } = float.PositiveInfinity;
    /// <summary>How fast the velocity falls to zero with no intent, in units per second squared; infinite (the default) stops at once.</summary>
    public float Deceleration { get; init; } = float.PositiveInfinity;

    /// <summary>Throws when a value is out of range.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A negative or not-a-number value, or a speed that is not finite.</exception>
    public void Validate()
    {
        CharacterMotion.Finite(MoveSpeed, nameof(MoveSpeed));
        CharacterMotion.NotNegative(Acceleration, nameof(Acceleration));
        CharacterMotion.NotNegative(Deceleration, nameof(Deceleration));
    }
}

/// <summary>
/// A top-down character: one kinematic body that moves in any direction and slides along what it touches
/// (<see cref="IPhysicsWorld2D.Move"/>), with no gravity. It takes a direction instead of reading input, like
/// <see cref="CharacterController2D"/>, and works on every backend.
/// </summary>
/// <example>
/// <code>
/// var hero = TopDownController2D.Create(physics, entity, start, new Vector2(12, 12));
/// // each fixed step
/// hero.Step(dt, new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down")));
/// </code>
/// </example>
public sealed class TopDownController2D : IStateHashed
{
    private TopDownMotion _motion;

    /// <summary>Drives <paramref name="body"/>, a kinematic body with one non-sensor shape.</summary>
    /// <exception cref="InvalidOperationException">The body is stale or not kinematic.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value of <paramref name="motion"/> is out of range.</exception>
    public TopDownController2D(IPhysicsWorld2D world, BodyId body, TopDownMotion? motion = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.KindOf(body) != BodyKind.Kinematic) throw new InvalidOperationException($"A character is a kinematic body: {body} is {world.KindOf(body)}.");
        _motion = motion ?? new TopDownMotion();
        _motion.Validate();
        World = world;
        Body = body;
    }

    /// <summary>Creates a kinematic body with one box of <paramref name="size"/> centred at <paramref name="position"/> and a controller for it.</summary>
    public static TopDownController2D Create(IPhysicsWorld2D world, EntityId owner, Vector2 position, Vector2 size, TopDownMotion? motion = null,
        ulong category = CollisionLayers.Default, ulong mask = CollisionLayers.All)
    {
        ArgumentNullException.ThrowIfNull(world);
        var shape = ShapeDescription.Box(size).InLayers(category, mask);
        shape.Validate();
        (motion ?? new TopDownMotion()).Validate();
        var body = world.CreateBody(owner, BodyDescription.Kinematic(position));
        world.AddShape(body, shape);
        return new TopDownController2D(world, body, motion) { Filter = QueryFilter.For(shape) };
    }

    /// <summary>The physics world.</summary>
    public IPhysicsWorld2D World { get; }
    /// <summary>The character's body.</summary>
    public BodyId Body { get; }
    /// <summary>How the character moves.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
    public TopDownMotion Motion
    {
        get => _motion;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            value.Validate();
            _motion = value;
        }
    }
    /// <summary>The shapes that block the character; the default filter unless set.</summary>
    public QueryFilter Filter { get; set; }
    /// <summary>The body's origin.</summary>
    public Vector2 Position => World.PositionOf(Body);
    /// <summary>The character's velocity, in units per second.</summary>
    public Vector2 Velocity { get; set; }
    /// <summary>The result of the latest step's move: every surface the character touches.</summary>
    public MoveResult LastMove { get; private set; }

    /// <summary>
    /// Advances the character by <paramref name="dt"/> seconds toward <paramref name="move"/>, a direction whose length
    /// is the share of <see cref="TopDownMotion.MoveSpeed"/> wanted (longer than 1 counts as 1, so a diagonal is no
    /// faster), and returns the move's result.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dt"/> is negative or not finite.</exception>
    public MoveResult Step(float dt, Vector2 move)
    {
        if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt), dt, "The step must be finite and not negative.");
        if (dt == 0) return LastMove;
        if (!float.IsFinite(move.X) || !float.IsFinite(move.Y)) move = Vector2.Zero;
        float length = move.Length();
        if (length > 1) move /= length;
        var velocity = MathX.Approach(Velocity, move * _motion.MoveSpeed, (length > 0 ? _motion.Acceleration : _motion.Deceleration) * dt);
        var result = World.Move(Body, velocity * dt, Filter);
        foreach (ref readonly var contact in result.Contacts)
        {
            float into = Vector2.Dot(velocity, contact.Normal);
            if (into < 0) velocity -= contact.Normal * into;
        }
        Velocity = velocity;
        LastMove = result;
        return result;
    }

    /// <summary>Adds the character's velocity.</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(Velocity);
    }
}
