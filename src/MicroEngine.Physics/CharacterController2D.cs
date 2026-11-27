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
/// <param name="Drop">
/// Fall through the one-way platform the character stands on (it wins over a jump there, and does nothing on solid
/// ground). With <see cref="CharacterMotion.DropWithJump"/> it is the "down" of "down and jump": the drop happens when
/// a jump would.
/// </param>
/// <param name="Run">Run is held: the character moves at <see cref="CharacterMotion.RunSpeed"/> when the motion has one.</param>
public readonly record struct CharacterInput(float Move, bool JumpPressed = false, bool JumpHeld = false, bool Drop = false, bool Run = false);

/// <summary>The order of the things a <see cref="CharacterController2D"/> does in one step (<see cref="CharacterMotion.Order"/>).</summary>
public enum CharacterStepOrder
{
    /// <summary>
    /// The ground carries the character first (a platform that moved since its last step), then the jump or the drop
    /// is read: a jump off a moving platform starts from where the platform took the character in this step.
    /// </summary>
    CarryThenAct,
    /// <summary>
    /// The jump or the drop is read first: a character that jumps or drops has left the ground before the ground
    /// moves it, so it is not carried in that step (a jump off a moving platform starts from where the character
    /// stood), and a drop's first step is a step in the air (its gravity is the air's, the floaty top of
    /// <see cref="CharacterMotion.ApexGravityScale"/> included). A solid that pushes
    /// (<see cref="BodyDescription.PushesBodies"/>) has carried its riders before the character steps, whatever the order.
    /// </summary>
    ActThenCarry,
}

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
    /// <summary>
    /// The share of <see cref="Acceleration"/> and <see cref="Deceleration"/> that applies in the air, from 0 (no air
    /// control) to 1 (the default). <see cref="AirAcceleration"/> and <see cref="AirDeceleration"/> replace it with a
    /// number each.
    /// </summary>
    public float AirControl { get; init; } = 1;
    /// <summary>
    /// How fast the sideways speed grows toward the intent in the air, in units per second squared: how much the
    /// player steers a jump. Null (the default) is <see cref="Acceleration"/> times <see cref="AirControl"/>.
    /// </summary>
    public float? AirAcceleration { get; init; }
    /// <summary>
    /// How fast the sideways speed falls to zero in the air with no intent, in units per second squared: a low value
    /// lets a jump keep travelling when the player lets go. Null (the default) is <see cref="Deceleration"/> times
    /// <see cref="AirControl"/>.
    /// </summary>
    public float? AirDeceleration { get; init; }
    /// <summary>
    /// The sideways speed at full intent while <see cref="CharacterInput.Run"/> is held, in units per second; 0 (the
    /// default) has no run: the character always moves at <see cref="MoveSpeed"/>.
    /// </summary>
    public float RunSpeed { get; init; }
    /// <summary>How fast the sideways speed grows on the ground while running, in units per second squared; null (the default) is <see cref="Acceleration"/>.</summary>
    public float? RunAcceleration { get; init; }
    /// <summary>
    /// How fast a character on the ground loses the speed it has above what the intent asks for while it keeps
    /// pushing the way it moves, in units per second squared: what happens when run is released at full speed, or
    /// after a boost. Null (the default) uses the acceleration, as for any other change of speed; a game usually
    /// sets its <see cref="Deceleration"/> here (the ground's friction).
    /// </summary>
    public float? OverspeedDeceleration { get; init; }
    /// <summary>
    /// The same in the air, in units per second squared: 0 keeps a run's momentum for the whole jump, whatever the
    /// player does with run in the air. Null (the default) uses the air's acceleration.
    /// </summary>
    public float? AirOverspeedDeceleration { get; init; }
    /// <summary>The upward speed a jump starts with, in units per second: the jump rises about <c>JumpSpeed² / (2 × Gravity)</c>.</summary>
    public float JumpSpeed { get; init; } = 300;
    /// <summary>
    /// Gravity is multiplied by this while a jump rises with jump released, which makes a tap a short hop (variable
    /// jump height); 1 gives every jump the same height.
    /// </summary>
    public float JumpReleaseGravityScale { get; init; } = 2.5f;
    /// <summary>
    /// A jump that rises with jump released is slowed at once to this share of <see cref="JumpSpeed"/> (a slower rise
    /// is kept): 0.45 cuts a released jump to 45 % of its starting speed in the step jump is let go, a sharper hop
    /// than <see cref="JumpReleaseGravityScale"/> gives. 1 (the default) cuts nothing.
    /// </summary>
    public float JumpReleaseSpeedScale { get; init; } = 1;
    /// <summary>Gravity is multiplied by this while falling; above 1 makes falls faster than rises.</summary>
    public float FallGravityScale { get; init; } = 1;
    /// <summary>How long after leaving a ledge a jump is still allowed, in seconds.</summary>
    public float CoyoteTime { get; init; } = 0.1f;
    /// <summary>How long a jump pressed in the air is remembered and performed on landing, in seconds.</summary>
    public float JumpBuffer { get; init; } = 0.1f;
    /// <summary>How long one-way platforms are passed through after a drop, in seconds.</summary>
    public float DropThroughTime { get; init; } = 0.2f;
    /// <summary>
    /// Whether a drop needs a jump: with it, <see cref="CharacterInput.Drop"/> held makes the jump of a character on
    /// a one-way platform a drop (pressed in that step or still remembered by <see cref="JumpBuffer"/>, which the
    /// drop uses up), and holding it alone does nothing: "down and jump". False (the default) drops as soon as
    /// <see cref="CharacterInput.Drop"/> is held.
    /// </summary>
    public bool DropWithJump { get; init; }
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
    /// How near the ground a character that is not rising counts as standing on it, in units; 0 (the default) lands
    /// on contact. With 1, a fall that ends a step within a unit of the floor has landed in that step (it can jump,
    /// its fall speed is gone) and closes the rest of the way in the next steps: landings come a step earlier, as in
    /// games that test for ground a pixel under the feet.
    /// </summary>
    public float LandingReach { get; init; }
    /// <summary>Whether the ground carries the character before or after its jump or drop is read; <see cref="CharacterStepOrder.CarryThenAct"/> by default.</summary>
    public CharacterStepOrder Order { get; init; }

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
    /// <summary>
    /// Extra jump speed at the motion's top speed (<see cref="RunSpeed"/> when it has one, else <see cref="MoveSpeed"/>),
    /// in proportion to the sideways speed when the jump starts, in units per second: a run-up jumps higher. The top
    /// speed is the measure whether run is held or not, so the same speed always gives the same jump.
    /// </summary>
    public float RunJumpBonus { get; init; }

    /// <summary>Throws when a value is out of range.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A negative or not-a-number value, a non-finite speed or time, an air control or a release share outside 0..1, or an undefined order.</exception>
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
        Finite(RunSpeed, nameof(RunSpeed));
        if (RunAcceleration is { } run) NotNegative(run, nameof(RunAcceleration));
        if (AirAcceleration is { } air) NotNegative(air, nameof(AirAcceleration));
        if (AirDeceleration is { } drag) NotNegative(drag, nameof(AirDeceleration));
        if (OverspeedDeceleration is { } brake) NotNegative(brake, nameof(OverspeedDeceleration));
        if (AirOverspeedDeceleration is { } bleed) NotNegative(bleed, nameof(AirOverspeedDeceleration));
        if (!(JumpReleaseSpeedScale >= 0 && JumpReleaseSpeedScale <= 1)) throw new ArgumentOutOfRangeException(nameof(JumpReleaseSpeedScale), JumpReleaseSpeedScale, "The share is between 0 and 1.");
        Finite(LandingReach, nameof(LandingReach));
        if (!Enum.IsDefined(Order)) throw new ArgumentOutOfRangeException(nameof(Order), Order, "Not a step order.");
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
    /// Creates a kinematic body with one box of <paramref name="size"/> and a controller for it, moving through the
    /// layers of <paramref name="mask"/>. The body's origin is the box's centre unless <paramref name="offset"/> says
    /// otherwise.
    /// </summary>
    /// <param name="world">The physics world.</param>
    /// <param name="owner">The entity that owns the body.</param>
    /// <param name="position">The body's origin: what <see cref="Position"/> reads and what a scene's physics writes to the owner's transform.</param>
    /// <param name="size">The box's width and height.</param>
    /// <param name="motion">How it moves; the defaults when null.</param>
    /// <param name="category">The layers the character is in.</param>
    /// <param name="mask">The layers it collides with.</param>
    /// <param name="offset">
    /// The box's centre relative to the body's origin; zero (the default) centres the box on it. A game that places
    /// its characters by their feet passes half the height upward (<c>new Vector2(0, -size.Y / 2)</c> where y grows
    /// downward); one that places them by their top-left corner passes <c>size / 2</c>.
    /// </param>
    public static CharacterController2D Create(IPhysicsWorld2D world, EntityId owner, Vector2 position, Vector2 size, CharacterMotion? motion = null,
        ulong category = CollisionLayers.Default, ulong mask = CollisionLayers.All, Vector2 offset = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        var shape = ShapeDescription.Box(size, offset).InLayers(category, mask);
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
    /// <summary>
    /// The body's origin. Setting it places the body there without collision and changes nothing else: the character
    /// keeps its ground, its velocity and its timers (keeping it inside the camera's view, a scripted placement).
    /// <see cref="Teleport"/> is the jump to another place, which leaves the ground.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The position is not finite.</exception>
    public Vector2 Position
    {
        get => World.PositionOf(Body);
        set
        {
            World.SetPosition(Body, value);
            _restAt = value;
        }
    }
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
    /// Throws the character with <paramref name="velocity"/> (a spring, a knock-back, a bounce off an enemy): it
    /// leaves the ground.
    /// </summary>
    /// <param name="velocity">The velocity it leaves with.</param>
    /// <param name="cuttable">
    /// Whether the rise is a jump the player shapes: releasing jump cuts it short as it cuts a jump
    /// (<see cref="CharacterMotion.JumpReleaseGravityScale"/>, <see cref="CharacterMotion.JumpReleaseSpeedScale"/>),
    /// so a bounce is higher while jump is held. False (the default): the rise is not cut by a released jump button.
    /// </param>
    public void Launch(Vector2 velocity, bool cuttable = false)
    {
        Velocity = velocity;
        IsJumping = cuttable && _rise > 0;
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

    /// <summary>
    /// Puts the character on ground the game decided: at <paramref name="position"/>, standing on
    /// <paramref name="ground"/>, with no vertical speed. For ground with a rule of the game's own that the world's
    /// shapes do not give (a platform that catches a character a little before it touches, the back of a creature):
    /// call it after <see cref="Step"/> when the rule holds. From then on the character is grounded as on any other
    /// ground: it is carried when the body of <paramref name="ground"/> moves, it can jump, and it stays until a
    /// step finds nothing under it, so a rule that still holds calls this again after each step. A character that
    /// left the ground in the latest step and is put back has not left it (no <see cref="CharacterEvents.LeftGround"/>);
    /// one that was in the air has landed.
    /// </summary>
    /// <param name="ground">The shape it stands on; its body need not collide with the character.</param>
    /// <param name="position">Where the body's origin goes.</param>
    /// <exception cref="InvalidOperationException"><paramref name="ground"/> is not a live shape.</exception>
    public void StandOn(ShapeId ground, Vector2 position)
    {
        if (!World.IsAlive(ground)) throw new InvalidOperationException($"{ground} is not a live shape of this world.");
        World.SetPosition(Body, position);
        _restAt = position;
        if (!IsGrounded)
        {
            if ((Events & CharacterEvents.LeftGround) != 0)
            {
                // It left the ground in the latest step and is back on it: it never left.
                Events &= ~CharacterEvents.LeftGround;
                _air = 0;
            }
            else
            {
                Events |= CharacterEvents.Landed;
                LandingSpeed = MathF.Max(0, -_rise);
            }
        }
        _rise = 0;
        _airJumps = 0;
        IsJumping = false;
        IsGrounded = true;
        Ground = ground;
        _groundBody = World.BodyOf(ground);
        _groundAt = World.PositionOf(_groundBody);
    }

    /// <summary>
    /// The character's whole state: where it is, how it moves, what it stands on, its timers and what its latest
    /// step reported. <see cref="Restore"/> puts it back, on this controller or on another one of the same world: a
    /// planner tries a jump and takes it back, a checkpoint keeps the character.
    /// </summary>
    public CharacterState Snapshot()
    {
        var position = World.PositionOf(Body);
        return new CharacterState
        {
            Position = position, Velocity = Velocity,
            IsGrounded = IsGrounded, Ground = Ground, GroundPosition = _groundAt,
            IsOnWall = IsOnWall, WallNormal = WallNormal, IsJumping = IsJumping, IsSkidding = IsSkidding,
            CoyoteTime = _coyote, JumpBuffer = _buffer, DropThroughTime = _drop, WallJumpLockTime = _lock,
            AirJumpsUsed = _airJumps, AirTime = _air, LandingSpeed = LandingSpeed, Events = Events, LastMove = LastMove,
            Rest = position == _restAt ? null : _restAt,
        };
    }

    /// <summary>
    /// Makes the character what <paramref name="state"/> says (<see cref="Snapshot"/>, or one built by the game): the
    /// body is placed without collision, and the next step goes on from there as it would have. A ground shape that
    /// no longer exists is no ground.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A position, velocity or time of <paramref name="state"/> is not finite, or a time or count is negative.</exception>
    public void Restore(in CharacterState state)
    {
        StateVector(state.Velocity, nameof(CharacterState.Velocity));
        StateVector(state.GroundPosition, nameof(CharacterState.GroundPosition));
        StateVector(state.WallNormal, nameof(CharacterState.WallNormal));
        CharacterMotion.Finite(state.CoyoteTime, nameof(CharacterState.CoyoteTime));
        CharacterMotion.Finite(state.JumpBuffer, nameof(CharacterState.JumpBuffer));
        CharacterMotion.Finite(state.DropThroughTime, nameof(CharacterState.DropThroughTime));
        CharacterMotion.Finite(state.WallJumpLockTime, nameof(CharacterState.WallJumpLockTime));
        CharacterMotion.Finite(state.AirTime, nameof(CharacterState.AirTime));
        CharacterMotion.Finite(state.LandingSpeed, nameof(CharacterState.LandingSpeed));
        ArgumentOutOfRangeException.ThrowIfNegative(state.AirJumpsUsed, nameof(CharacterState.AirJumpsUsed));
        World.SetPosition(Body, state.Position);
        Velocity = state.Velocity;
        bool ground = state.IsGrounded && World.IsAlive(state.Ground);
        IsGrounded = state.IsGrounded;
        Ground = ground ? state.Ground : ShapeId.None;
        _groundBody = ground ? World.BodyOf(state.Ground) : BodyId.None;
        _groundAt = state.GroundPosition;
        _restAt = state.Rest ?? state.Position;
        IsOnWall = state.IsOnWall;
        WallNormal = state.WallNormal;
        IsJumping = state.IsJumping;
        IsSkidding = state.IsSkidding;
        _coyote = state.CoyoteTime;
        _buffer = state.JumpBuffer;
        _drop = state.DropThroughTime;
        _lock = state.WallJumpLockTime;
        _airJumps = state.AirJumpsUsed;
        _air = state.AirTime;
        LandingSpeed = state.LandingSpeed;
        Events = state.Events;
        LastMove = state.LastMove;
    }

    private static void StateVector(Vector2 value, string name)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(name, value, "The value must be finite.");
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
        bool actFirst = m.Order == CharacterStepOrder.ActThenCarry;
        bool carried = !actFirst && Carry();

        // Sideways speed toward the intent.
        float axis = float.IsNaN(input.Move) ? 0 : Math.Clamp(input.Move, -1, 1);
        _lock = MathF.Max(0, _lock - dt);
        IsSkidding = false;
        if (_lock <= 0)
        {
            if (wasGrounded && m.SkidDeceleration > 0 && axis * _run < 0 && MathF.Abs(_run) > m.SkidSpeed)
            {
                // Reversing at speed: brake before turning.
                _run = MathX.Approach(_run, 0, m.SkidDeceleration * dt);
                IsSkidding = true;
                Events |= CharacterEvents.Skidded;
            }
            else
            {
                bool running = input.Run && m.RunSpeed > 0;
                float target = axis * (running ? m.RunSpeed : m.MoveSpeed);
                // Faster than the intent asks for, and pushing the way it moves.
                bool over = axis * _run > 0 && MathF.Abs(_run) > MathF.Abs(target);
                if (wasGrounded)
                {
                    float rate = axis == 0 ? m.Deceleration
                        : over && m.OverspeedDeceleration is { } brake ? brake
                        : running && m.RunAcceleration is { } quick ? quick
                        : m.Acceleration;
                    _run = MathX.Approach(_run, target, rate * dt);
                }
                else if ((axis == 0 ? m.AirDeceleration : over && m.AirOverspeedDeceleration is { } bleed ? bleed : m.AirAcceleration) is { } air)
                    _run = MathX.Approach(_run, target, air * dt);
                else if (m.AirControl > 0)
                    _run = MathX.Approach(_run, target, (axis != 0 ? m.Acceleration : m.Deceleration) * m.AirControl * dt);
            }
        }

        // Timers: the drop window, coyote time and the jump buffer.
        _drop = MathF.Max(0, _drop - dt);
        _coyote = wasGrounded ? m.CoyoteTime : MathF.Max(0, _coyote - dt);
        _buffer = input.JumpPressed ? m.JumpBuffer : MathF.Max(0, _buffer - dt);
        bool acted = false, dropped = false;
        bool wantsJump = input.JumpPressed || _buffer > 0;
        if (input.Drop && wasGrounded && (!m.DropWithJump || wantsJump) && !Supported(m, passOneWay: true))
        {
            // Only one-way platforms hold it up: fall through them.
            _drop = m.DropThroughTime;
            _buffer = 0;
            _coyote = 0;
            IsGrounded = false;
            acted = dropped = true;
            Events |= CharacterEvents.DroppedThrough;
        }
        else if (wantsJump)
        {
            float top = m.RunSpeed > 0 ? m.RunSpeed : m.MoveSpeed;
            if (wasGrounded || _coyote > 0) acted = Jump(m.JumpSpeed + (top > 0 ? m.RunJumpBonus * MathF.Min(1, MathF.Abs(_run) / top) : 0));
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

        // A character that jumped or dropped has left the ground: the ground no longer carries it.
        if (actFirst && !acted) carried = Carry();
        // A released jump is cut to a share of its starting speed at once.
        if (m.JumpReleaseSpeedScale < 1 && IsJumping && !input.JumpHeld && _rise > m.JumpSpeed * m.JumpReleaseSpeedScale) _rise = m.JumpSpeed * m.JumpReleaseSpeedScale;

        var filter = Active(_drop > 0);
        bool inAir = !wasGrounded || (actFirst && dropped);
        MoveResult result;
        // A character at rest is probed, not moved: it neither sinks nor creeps down a slope.
        if (!(wasGrounded && !acted && !carried && _run == 0 && _rise <= 0 && (result = World.Move(Body, Vector2.Zero, filter)).OnGround))
        {
            // Gravity by phase: rising, rising with jump released, near the top, falling.
            float scale = _rise > 0
                ? (IsJumping && !input.JumpHeld ? m.JumpReleaseGravityScale : _rise < m.ApexSpeed ? m.ApexGravityScale : 1)
                : (input.JumpHeld && inAir && -_rise < m.ApexSpeed ? m.ApexGravityScale : m.FallGravityScale);
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
        var ground = result.Ground;
        // Ground within reach under a character that is not rising: it has landed, where it is.
        if (!grounded && _rise <= 0 && m.LandingReach > 0 && Reaches(filter, m.LandingReach, out ground)) grounded = true;
        if (!grounded && wasGrounded && !acted && _rise <= 0 && m.GroundSnap > 0)
        {
            // The ground fell away under a walking character: follow it down when it is within reach.
            var before = result.Position;
            var snap = World.Move(Body, _up * -m.GroundSnap, filter);
            if (snap.OnGround)
            {
                result = snap;
                ground = snap.Ground;
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
        Ground = grounded ? ground : ShapeId.None;
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

    /// <summary>
    /// Whether a surface within <paramref name="reach"/> under the character would stop it: the body is moved down
    /// by the reach and put back. A surface exactly at the reach, which the move only comes to touch, is not within it.
    /// </summary>
    private bool Reaches(in QueryFilter filter, float reach, out ShapeId ground)
    {
        var at = World.PositionOf(Body);
        var probe = World.Move(Body, _up * -reach, filter);
        World.SetPosition(Body, at);
        foreach (ref readonly var contact in probe.Contacts)
        {
            if (!contact.Stopped || Vector2.Dot(contact.Normal, _up) < MoveResult.GroundThreshold) continue;
            ground = contact.Shape;
            return true;
        }
        ground = ShapeId.None;
        return false;
    }

    /// <summary>Whether ground holds the character up where it is: ground it touches, or ground within the landing reach.</summary>
    private bool Supported(CharacterMotion m, bool passOneWay)
    {
        var filter = Active(passOneWay);
        return World.Move(Body, Vector2.Zero, filter).OnGround || (m.LandingReach > 0 && Reaches(filter, m.LandingReach, out _));
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

/// <summary>
/// The whole state of a <see cref="CharacterController2D"/> (<see cref="CharacterController2D.Snapshot"/>,
/// <see cref="CharacterController2D.Restore"/>). Every value can be read and, with <c>with</c>, changed: a game that
/// keeps the character's state elsewhere (a save, a replay, another simulation) builds one.
/// </summary>
public readonly struct CharacterState
{
    /// <summary>The body's origin.</summary>
    public Vector2 Position { get; init; }
    /// <summary>The velocity, in units per second.</summary>
    public Vector2 Velocity { get; init; }
    /// <summary>Whether it stands on ground.</summary>
    public bool IsGrounded { get; init; }
    /// <summary>The shape it stands on, which carries it when its body moves; <see cref="ShapeId.None"/> for none.</summary>
    public ShapeId Ground { get; init; }
    /// <summary>Where the body of <see cref="Ground"/> was when the character last stepped: it is carried by how far that body has moved since.</summary>
    public Vector2 GroundPosition { get; init; }
    /// <summary>Whether it touches a wall.</summary>
    public bool IsOnWall { get; init; }
    /// <summary>The normal of that wall.</summary>
    public Vector2 WallNormal { get; init; }
    /// <summary>Whether a jump is rising and can still be cut short.</summary>
    public bool IsJumping { get; init; }
    /// <summary>Whether it skids.</summary>
    public bool IsSkidding { get; init; }
    /// <summary>The coyote time left, in seconds.</summary>
    public float CoyoteTime { get; init; }
    /// <summary>How much longer a pressed jump is remembered, in seconds.</summary>
    public float JumpBuffer { get; init; }
    /// <summary>How much longer one-way platforms are passed through, in seconds.</summary>
    public float DropThroughTime { get; init; }
    /// <summary>How much longer the sideways intent is ignored after a wall jump, in seconds.</summary>
    public float WallJumpLockTime { get; init; }
    /// <summary>Air jumps used since it last stood on ground.</summary>
    public int AirJumpsUsed { get; init; }
    /// <summary>How long it has been in the air, in seconds.</summary>
    public float AirTime { get; init; }
    /// <summary>The downward speed of its last landing.</summary>
    public float LandingSpeed { get; init; }
    /// <summary>What its latest step reported.</summary>
    public CharacterEvents Events { get; init; }
    /// <summary>The result of its latest step's move.</summary>
    public MoveResult LastMove { get; init; }
    /// <summary>Where the body was after the character's latest step, when something has moved it since (a pushing solid); null when it is where the step left it.</summary>
    internal Vector2? Rest { get; init; }
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

    /// <summary>
    /// Creates a kinematic body with one box of <paramref name="size"/> and a controller for it. The body's origin,
    /// <paramref name="position"/>, is the box's centre unless <paramref name="offset"/> (the box's centre relative
    /// to the origin) says otherwise: a game that places its characters by their feet passes half the height upward.
    /// </summary>
    public static TopDownController2D Create(IPhysicsWorld2D world, EntityId owner, Vector2 position, Vector2 size, TopDownMotion? motion = null,
        ulong category = CollisionLayers.Default, ulong mask = CollisionLayers.All, Vector2 offset = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        var shape = ShapeDescription.Box(size, offset).InLayers(category, mask);
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
    /// <summary>The body's origin. Setting it places the body there without collision; the velocity is kept.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The position is not finite.</exception>
    public Vector2 Position
    {
        get => World.PositionOf(Body);
        set => World.SetPosition(Body, value);
    }
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
