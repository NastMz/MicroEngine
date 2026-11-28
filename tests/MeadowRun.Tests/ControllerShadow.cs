using System.Globalization;
using System.Numerics;
using MeadowRun.Content;
using MeadowRun.Gameplay;
using MicroEngine;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;

namespace MeadowRun.Tests;

/// <summary>
/// The engine's <see cref="CharacterController2D"/> driven beside Pip's own mover (<see cref="Player.Step"/>), with the
/// same input over the same level, to measure where the two differ (see <see cref="ControllerDifferentialTests"/>).
/// </summary>
/// <remarks>
/// <para>The shadow lives in a tile physics world of its own: the level's grid as tile geometry, a body for each
/// moving platform (moved to where the stage left it; Pip does not collide with it, the game's own rule decides when
/// he stands on it) and one kinematic body whose origin is Pip's top-left corner, so positions compare without a conversion.</para>
/// <para>The mapping is what a developer making this game on the controller would write: one
/// <see cref="CharacterMotion"/> with the numbers of <see cref="Tuning"/>, the input passed as it is, and the two rules
/// that are the game's own written after the step (a hidden block stops the head, a platform catches Pip a little
/// before and after he touches it).</para>
/// <para>Each step the game's mover is also run on a copy of Pip from the state before the step (the "expected"
/// result), which separates what the mover did from what the stage did afterwards (a stomp's bounce, a spring, a
/// hit). What the stage does to Pip is done to the controller as the game would write it: a bounce off an enemy or a
/// spring is a placement and a <see cref="CharacterController2D.Launch"/>. Nothing else is ever given to the
/// controller: <see cref="Restores"/> counts the times its state had to be copied from Pip, and the tests require
/// none. In the open loop the stage's changes are not applied either, so a divergence shows how far it carries.</para>
/// <para>Compared after every step, exactly: position, velocity, whether it is on the ground, whether a jump can still
/// be cut, the coyote, jump-buffer and drop timers, and the step's events (jumped, landed, dropped, skidded).</para>
/// </remarks>
internal sealed class ControllerShadow
{
    private const float Dt = StageHarness.Dt;
    /// <summary>Steps after the shadow was given Pip's state in which a divergence is told apart in the report.</summary>
    private const int Settle = 30;
    /// <summary>The layer of the platforms: Pip's body does not collide with it.</summary>
    private const ulong Platforms = 1UL << 8;

    /// <summary>Pip's feel, from the game's numbers.</summary>
    public static readonly CharacterMotion Pip = new()
    {
        // On the ground: two speeds, friction, a skid when turning at speed.
        MoveSpeed = Tuning.WalkSpeed, Acceleration = Tuning.GroundAccel,
        RunSpeed = Tuning.RunSpeed, RunAcceleration = Tuning.RunAccel,
        Deceleration = Tuning.GroundFriction, OverspeedDeceleration = Tuning.GroundFriction,
        SkidDeceleration = Tuning.SkidDecel, SkidSpeed = 30,
        // In the air: less steering, little drag, and a run's momentum is kept.
        AirAcceleration = Tuning.AirAccel, AirDeceleration = Tuning.AirDrag, AirOverspeedDeceleration = 0,
        // The jump: higher with a run-up, four gravities, a fall clamp.
        JumpSpeed = Tuning.JumpSpeed, RunJumpBonus = Tuning.RunJumpBonus,
        Gravity = Tuning.RiseGravity,
        JumpReleaseGravityScale = Tuning.ReleaseGravity / Tuning.RiseGravity,
        ApexGravityScale = Tuning.ApexGravity / Tuning.RiseGravity, ApexSpeed = Tuning.ApexThreshold,
        FallGravityScale = Tuning.FallGravity / Tuning.RiseGravity, MaxFallSpeed = Tuning.TerminalVelocity,
        // Forgiveness: coyote time, a buffered jump, a landing a pixel early, corners.
        CoyoteTime = Tuning.CoyoteTime, JumpBuffer = Tuning.JumpBuffer, LandingReach = 1,
        CornerCorrection = Tuning.CornerCorrection, CeilingReboundSpeed = 20,
        // Down and jump drops through a plank; a jump leaves a platform before the platform moves Pip.
        DropThroughTime = Tuning.DropThroughTime, DropWithJump = true,
        Order = CharacterStepOrder.ActThenCarry,
    };

    private readonly Stage _stage;
    private readonly TilePhysicsWorld2D _world = new();
    private readonly List<BodyId> _platforms = [];
    private readonly List<ShapeId> _platformShapes = [];
    private readonly List<Vector2> _platformWas = [];
    private readonly bool _closedLoop;
    private Player? _last;
    private (int X, int Y)? _bumped;
    private int _resyncedAt = -Settle;
    private Divergence? _apart;
    private int _apartSince;

    public CharacterController2D Hero { get; }
    /// <summary>Steps in which both movers ran.</summary>
    public int Steps { get; private set; }
    /// <summary>Steps before which the stage had changed Pip since his mover last ran (a stomp, a spring, a hit, the goal).</summary>
    public int Interventions { get; private set; }
    /// <summary>Times the stage threw Pip (a stomp, a spring, spikes) and the shadow did the same with a placement and a launch.</summary>
    public int Launches { get; private set; }
    /// <summary>Times the stage placed or stopped Pip (the goal pole) and the shadow did the same by setting its position and velocity.</summary>
    public int Placements { get; private set; }
    /// <summary>Times the controller's state was copied from Pip because the two were apart: none in a run the controller follows.</summary>
    public int Restores { get; private set; }
    /// <summary>Steps after which the controller's air time (0 on the ground) was not Pip's, and landings whose impact computed from it was not Pip's.</summary>
    public (int AirTime, int LandImpact) AirApart { get; private set; }
    /// <summary>For each step compared, Pip before it and the input: his position, velocity, ground and jump state.</summary>
    public List<(Vector2 Position, Vector2 Velocity, bool Grounded, bool Jumping, PlayerInput Input)> Situations { get; } = [];
    /// <summary>Kinds of step compared: a name (landing, jump, drop, platform, head bump...) and how many.</summary>
    public Dictionary<string, int> Kinds { get; } = [];
    /// <summary>Closed loop: steps that started with the two movers in the same state and ended with them apart.</summary>
    public List<Divergence> Divergences { get; } = [];
    /// <summary>Open loop: the first step at which the shadow was not where Pip was; -1 when it never left him.</summary>
    public int FirstApart { get; private set; } = -1;
    /// <summary>Open loop: the steps at which the shadow was not where Pip was.</summary>
    public int StepsApart { get; private set; }
    /// <summary>Open loop: the largest distance between the two, in pixels, and the step it was reached at.</summary>
    public (float Pixels, int Step) Farthest { get; private set; }
    /// <summary>Open loop: the distance between the two after the latest step.</summary>
    public Vector2 Apart { get; private set; }
    /// <summary>Open loop: both states at the first step apart.</summary>
    public string FirstApartDetail { get; private set; } = "";
    /// <summary>Steps in which Pip's mover nudged him past a ceiling corner, and steps in which the controller did.</summary>
    public (int Game, int Controller) Corners { get; private set; }

    /// <summary>
    /// One step that started with the two movers in the same state and ended with them apart. <paramref name="Pristine"/>:
    /// the shadow had followed Pip by itself for the last <see cref="Settle"/> steps, so the step shows a rule and not
    /// what putting the shadow back left behind.
    /// </summary>
    public sealed record Divergence(int Step, string Situation, string Differs, Vector2 Position, Vector2 Velocity, bool Pristine, string Detail)
    {
        /// <summary>Steps the controller went on by itself until it was again what Pip was; -1 when it was not within <see cref="HealLimit"/> steps.</summary>
        public int Healed { get; set; } = -1;
        /// <summary>The farthest the controller got from Pip while they were apart, in pixels.</summary>
        public float Farthest { get; set; }
    }

    /// <summary>Steps a controller that left Pip is left on its own before it is given his state.</summary>
    public const int HealLimit = 240;

    /// <summary>Shadows <paramref name="stage"/>'s Pip from now on: the stage calls the shadow before each step of his mover.</summary>
    public ControllerShadow(Stage stage, bool closedLoop)
    {
        _stage = stage;
        _closedLoop = closedLoop;
        var level = _world.CreateBody(default, BodyDescription.Static());
        _world.AddTiles(level, stage.Level.Grid);
        foreach (var platform in stage.Platforms)
        {
            var bounds = platform.Bounds;
            var body = _world.CreateBody(default, BodyDescription.Kinematic(bounds.Position));
            _platformShapes.Add(_world.AddShape(body, ShapeDescription.Box(bounds.Size, bounds.Size / 2).InLayers(Platforms, CollisionLayers.All)));
            _platforms.Add(body);
            _platformWas.Add(bounds.Position);
        }
        var size = new Vector2(Player.Width, Player.Height);
        Hero = CharacterController2D.Create(_world, default, stage.Player.Position, size, Pip, mask: CollisionLayers.All & ~Platforms, offset: size / 2);
        stage.PipSteppingForTesting = OnPipStep;
    }

    /// <summary>
    /// Called by the stage just before Pip's mover runs (while playing, on the walk off the goal, in the victory
    /// pose): the platforms have moved, the tiles are what the mover will see. Runs the mover on a copy of Pip and
    /// the controller, and compares them.
    /// </summary>
    private void OnPipStep(float dt, PlayerInput raw)
    {
        var player = _stage.Player;
        // Input is ignored while Pip is not controllable (the goal, the victory); the game passes none.
        var input = player.Controllable ? raw : default;

        // What the stage did to Pip since his mover last ran, written as the game writes it.
        if (_last is { } last && !(last.Position == player.Position && last.Velocity == player.Velocity && last.Grounded == player.Grounded && last.Jumping == player.Jumping))
        {
            Interventions++;
            if (_closedLoop)
            {
                Hero.Position = player.Position;
                if (player.Velocity != last.Velocity && player.Velocity.Y < 0)
                {
                    // A stomp's bounce, a spring, spikes.
                    Hero.Launch(player.Velocity, cuttable: player.Jumping);
                    Launches++;
                }
                else
                {
                    // The goal pole: Pip is put on it and stopped.
                    Hero.Velocity = player.Velocity;
                    Placements++;
                }
            }
        }
        if (_closedLoop && !SameAs(player, events: false))
        {
            // A controller that left Pip goes on by itself for a while, to see whether and when it is him again.
            if (_apart is not null && Steps - _apartSince < HealLimit) _apart.Farthest = MathF.Max(_apart.Farthest, Vector2.Distance(Hero.Position, player.Position));
            else
            {
                Resync(player);
                _apart = null;
            }
        }

        var rides = new List<IRideable>();
        for (int i = 0; i < _stage.Platforms.Count; i++)
        {
            var platform = _stage.Platforms[i];
            rides.Add(new Ride(platform.Bounds, platform.Delta));
            _platformWas[i] = _world.PositionOf(_platforms[i]);
            _world.SetPosition(_platforms[i], platform.Bounds.Position);
        }
        var expected = player.Clone();
        expected.Step(dt, input, _stage.Level, rides);
        Situations.Add((player.Position, player.Velocity, player.Grounded, player.Jumping, input));

        StepHero(dt, input);
        Steps++;
        bool nudged = player.Velocity.X == 0 && input.Axis == 0 && player.Riding is null && expected.Position.X != player.Position.X;
        Corners = (Corners.Game + (nudged ? 1 : 0), Corners.Controller + ((Hero.Events & CharacterEvents.CornerCorrected) != 0 ? 1 : 0));
        Count(player, expected, input, nudged);
        float air = Hero.IsGrounded ? 0 : Hero.AirTime;
        bool landed = (expected.Events & PlayerEvents.Landed) != 0;
        AirApart = (AirApart.AirTime + (air != expected.AirTime ? 1 : 0), AirApart.LandImpact + (landed && MathF.Min(1, Hero.AirTime / 0.6f) != expected.LandImpact ? 1 : 0));

        bool followed = SameAs(expected, events: true);
        if (_closedLoop)
        {
            if (followed && _apart is not null)
            {
                _apart.Healed = Steps - _apartSince;
                _apart = null;
            }
            else if (!followed && _apart is null)
            {
                _apart = Describe(player, expected, input);
                _apartSince = Steps;
                Divergences.Add(_apart);
            }
            if (_apart is not null) _apart.Farthest = MathF.Max(_apart.Farthest, Vector2.Distance(Hero.Position, expected.Position));
        }
        else
        {
            Apart = Hero.Position - expected.Position;
            float distance = Apart.Length();
            if (!followed)
            {
                StepsApart++;
                if (FirstApart < 0)
                {
                    FirstApart = Steps;
                    FirstApartDetail = string.Create(CultureInfo.InvariantCulture,
                        $"game pos=({expected.Position.X:R}, {expected.Position.Y:R}) vel=({expected.Velocity.X:R}, {expected.Velocity.Y:R}) grounded={expected.Grounded} events={expected.Events}; controller pos=({Hero.Position.X:R}, {Hero.Position.Y:R}) vel=({Hero.Velocity.X:R}, {Hero.Velocity.Y:R}) grounded={Hero.IsGrounded} events={Hero.Events}");
                }
            }
            if (distance > Farthest.Pixels) Farthest = (distance, Steps);
        }
        _last = expected;
    }

    private void Count(Player before, Player expected, PlayerInput input, bool nudged)
    {
        void Add(string kind) => Kinds[kind] = Kinds.GetValueOrDefault(kind) + 1;
        var e = expected.Events;
        if ((e & PlayerEvents.Jumped) != 0) Add(before.Grounded ? "jump" : "coyote jump");
        if ((e & PlayerEvents.Landed) != 0) Add(expected.Riding is not null ? "landing on a platform" : "landing");
        if ((e & PlayerEvents.Skid) != 0) Add("skid");
        if ((e & PlayerEvents.DroppedThrough) != 0) Add("drop through a plank");
        if ((e & PlayerEvents.HeadBump) != 0) Add(_stage.Level.Tiles[expected.BumpedTile.X, expected.BumpedTile.Y] == Tile.HiddenLife ? "hidden block" : "head bump");
        if (nudged) Add("corner nudge");
        if (before.Riding is not null && expected.Riding is not null) Add("riding a platform");
        if (before.Riding is not null && (e & PlayerEvents.Jumped) != 0) Add("jump off a platform");
        if (before.Velocity.X != 0 && expected.Velocity.X == 0 && input.Axis != 0) Add("wall stop");
        if (before.Grounded && !expected.Grounded && e == PlayerEvents.None) Add("ledge");
        if (before.Jumping && !input.JumpHeld && before.Velocity.Y < 0) Add("jump cut");
        if (!before.Jumping && before.Velocity.Y < 0 && !before.Grounded) Add("rising after a launch");
        if (before.DropTimer > 0) Add("inside the drop window");
        if (_stage.Phase != StagePhase.Playing) Add(_stage.Phase == StagePhase.WalkOff ? "walk off the goal" : "victory pose");
        if (_stage.Boss is { Active: true }) Add("boss fight");
    }

    /// <summary>The game's step on the controller: what a developer would write in the game.</summary>
    private void StepHero(float dt, PlayerInput input)
    {
        float topBefore = Hero.Position.Y, bottomBefore = topBefore + Player.Height;
        // Down and jump drops through a plank, not through a platform Pip rides.
        bool riding = _platformShapes.Contains(Hero.Ground);
        Hero.Step(dt, new CharacterInput(input.Axis, input.JumpPressed, input.JumpHeld, Drop: input.Down && !riding, Run: input.RunHeld));

        // The block bumped is the one over the middle of Pip's head, else the one over a corner of it.
        bool ceiling = (Hero.Events & CharacterEvents.HitCeiling) != 0;
        _bumped = ceiling ? BumpedTile(Hero.Position) : null;

        // A hidden block is no shape of the world: a head that comes up from under it, in its column, finds it. Pip
        // stops under it where other ceilings send him down.
        var now = Hero.Position;
        if (ceiling || now.Y < topBefore)
        {
            int row = (int)MathF.Floor((ceiling ? now.Y - 0.5f : now.Y) / Level.TileSize), column = (int)MathF.Floor((now.X + Player.Width / 2f) / Level.TileSize);
            float under = (row + 1) * Level.TileSize;
            if (row >= 0 && topBefore >= under - 0.01f && _stage.Level.Tiles[column, row] == Tile.HiddenLife)
            {
                Hero.Position = new Vector2(now.X, under);
                Hero.Launch(Hero.Velocity with { Y = 0 });
                _bumped = (column, row);
            }
        }

        // A platform catches Pip when his feet were above it (or up to 2.5 px inside it) and are now within half a
        // pixel over its top or below it, a pixel in from its ends.
        if (Hero.Velocity.Y < 0) return;
        for (int i = 0; i < _platforms.Count; i++)
        {
            var at = Hero.Position;
            var top = _world.PositionOf(_platforms[i]);
            float width = _stage.Platforms[i].Bounds.Width, moved = top.Y - _platformWas[i].Y;
            if (at.X + Player.Width <= top.X + 1 || at.X >= top.X + width - 1) continue;
            if (bottomBefore <= top.Y - Math.Min(0, moved) + 2.5f && at.Y + Player.Height >= top.Y - 0.5f)
            {
                Hero.StandOn(_platformShapes[i], new Vector2(at.X, top.Y - Player.Height));
                break;
            }
        }
    }

    private (int X, int Y) BumpedTile(Vector2 at)
    {
        var level = _stage.Level;
        int row = (int)MathF.Floor((at.Y - 1) / Level.TileSize);
        int center = (int)MathF.Floor((at.X + Player.Width / 2f) / Level.TileSize);
        if (level.IsSolid(center, row)) return (center, row);
        int left = (int)MathF.Floor(at.X / Level.TileSize), right = (int)MathF.Floor((at.X + Player.Width - 0.01f) / Level.TileSize);
        return level.IsSolid(left, row) ? (left, row) : (right, row);
    }

    /// <summary>Gives the shadow Pip's state: his position, velocity, ground, jump and timers.</summary>
    private void Resync(Player pip)
    {
        _resyncedAt = Steps;
        Restores++;
        int ride = pip.Riding is null ? -1 : IndexOfRide(pip.Riding);
        Hero.Restore(Hero.Snapshot() with
        {
            Position = pip.Position, Velocity = pip.Velocity,
            IsGrounded = pip.Grounded, IsJumping = pip.Jumping,
            Ground = ride < 0 ? ShapeId.None : _platformShapes[ride],
            GroundPosition = ride < 0 ? default : _platformWas[ride],
            CoyoteTime = pip.CoyoteTimer, JumpBuffer = pip.BufferTimer, DropThroughTime = pip.DropTimer,
        });
    }

    /// <summary>The index of the platform <paramref name="ride"/> is: the stage's own, or the copy made of it for a step.</summary>
    private int IndexOfRide(IRideable ride)
    {
        for (int i = 0; i < _stage.Platforms.Count; i++)
            if (ReferenceEquals(_stage.Platforms[i], ride) || _stage.Platforms[i].Bounds == ride.Bounds) return i;
        return -1;
    }

    /// <summary>
    /// Whether the controller is what <paramref name="pip"/> is: everything the next step depends on (position,
    /// velocity, ground, the platform ridden, the jump that can be cut, the timers) and, with <paramref name="events"/>,
    /// what the step reported (jumped, landed, dropped, skidded, the block bumped).
    /// </summary>
    private bool SameAs(Player pip, bool events)
    {
        var state = Hero.Snapshot();
        int ride = _platformShapes.IndexOf(state.Ground);
        if (!(state.Position == pip.Position && state.Velocity == pip.Velocity && state.IsGrounded == pip.Grounded
            && ride == (pip.Riding is null ? -1 : IndexOfRide(pip.Riding))
            && state.IsJumping == pip.Jumping
            && state.CoyoteTime == pip.CoyoteTimer && state.JumpBuffer == pip.BufferTimer && state.DropThroughTime == pip.DropTimer)) return false;
        if (!events) return true;
        bool bump = (pip.Events & PlayerEvents.HeadBump) != 0;
        return state.IsSkidding == pip.Skidding
            && ((state.Events & CharacterEvents.Jumped) != 0) == ((pip.Events & PlayerEvents.Jumped) != 0)
            && ((state.Events & CharacterEvents.Landed) != 0) == ((pip.Events & PlayerEvents.Landed) != 0)
            && ((state.Events & CharacterEvents.DroppedThrough) != 0) == ((pip.Events & PlayerEvents.DroppedThrough) != 0)
            && ((state.Events & CharacterEvents.Skidded) != 0) == ((pip.Events & PlayerEvents.Skid) != 0)
            && bump == _bumped.HasValue && (!bump || _bumped == pip.BumpedTile);
    }

    private Divergence Describe(Player before, Player expected, PlayerInput input)
    {
        var parts = new List<string> { before.Grounded ? "ground" : before.Velocity.Y < 0 ? "rising" : "falling" };
        float cap = input.RunHeld ? Tuning.RunSpeed : Tuning.WalkSpeed;
        if (before.Riding is not null || expected.Riding is not null) parts.Add("platform");
        if (before.Grounded && input.Axis != 0 && MathF.Abs(before.Velocity.X) > cap) parts.Add("above the cap");
        if (!before.Grounded && input.Axis != 0 && MathF.Sign(before.Velocity.X) == input.Axis && MathF.Abs(before.Velocity.X) > cap) parts.Add("air momentum");
        if (!before.Grounded && input.Axis == 0 && before.Velocity.X != 0) parts.Add("air drag");
        if (!before.Grounded && input.Axis != 0 && MathF.Abs(before.Velocity.X) <= cap) parts.Add("air control");
        if ((expected.Events & PlayerEvents.Jumped) != 0) parts.Add(input.RunHeld ? "jump" : "walking jump");
        if ((expected.Events & PlayerEvents.Landed) != 0) parts.Add("landing");
        if ((expected.Events & PlayerEvents.Skid) != 0) parts.Add("skid");
        if ((expected.Events & PlayerEvents.DroppedThrough) != 0) parts.Add("drop");
        if ((expected.Events & PlayerEvents.HeadBump) != 0)
            parts.Add(_stage.Level.Tiles[expected.BumpedTile.X, expected.BumpedTile.Y] == Tile.HiddenLife ? "hidden block" : "head bump");
        if ((Hero.Events & CharacterEvents.CornerCorrected) != 0) parts.Add("corner (controller)");
        if ((Hero.Events & CharacterEvents.HitCeiling) != 0 && (expected.Events & PlayerEvents.HeadBump) == 0) parts.Add("ceiling (controller only)");
        if ((Hero.Events & CharacterEvents.Landed) != 0 && (expected.Events & PlayerEvents.Landed) == 0) parts.Add("landing (controller only)");
        if ((Hero.Events & CharacterEvents.Jumped) != 0 && (expected.Events & PlayerEvents.Jumped) == 0) parts.Add("jump (controller only)");
        if ((Hero.Events & CharacterEvents.DroppedThrough) != 0 && (expected.Events & PlayerEvents.DroppedThrough) == 0) parts.Add("drop (controller only)");

        var position = Hero.Position - expected.Position;
        var velocity = Hero.Velocity - expected.Velocity;
        var differs = new List<string>();
        if (position.X != 0) differs.Add("x");
        if (position.Y != 0) differs.Add("y");
        if (velocity.X != 0) differs.Add("vx");
        if (velocity.Y != 0) differs.Add("vy");
        if (Hero.IsGrounded != expected.Grounded) differs.Add(expected.Grounded ? "grounded (game only)" : "grounded (controller only)");
        var state = Hero.Snapshot();
        if (state.IsJumping != expected.Jumping) differs.Add("jumping");
        if (state.CoyoteTime != expected.CoyoteTimer) differs.Add("coyote");
        if (state.JumpBuffer != expected.BufferTimer) differs.Add("buffer");
        if (state.DropThroughTime != expected.DropTimer) differs.Add("drop timer");
        if (_platformShapes.Contains(state.Ground) != (expected.Riding is not null)) differs.Add("riding");
        if (differs.Count == 0) differs.Add($"events (game {expected.Events} bumped {expected.BumpedTile}, controller {Hero.Events} bumped {_bumped})");
        string detail = string.Create(CultureInfo.InvariantCulture,
            $"before pos=({before.Position.X:R}, {before.Position.Y:R}) vel=({before.Velocity.X:R}, {before.Velocity.Y:R}); game pos=({expected.Position.X:R}, {expected.Position.Y:R}) vel=({expected.Velocity.X:R}, {expected.Velocity.Y:R}); controller pos=({Hero.Position.X:R}, {Hero.Position.Y:R}) vel=({Hero.Velocity.X:R}, {Hero.Velocity.Y:R})");
        var contacts = new List<string>();
        foreach (var contact in Hero.LastMove.Contacts)
            contacts.Add(string.Create(CultureInfo.InvariantCulture, $"tiles normal ({contact.Normal.X}, {contact.Normal.Y}){(contact.Stopped ? " stopped" : "")}"));
        detail += string.Create(CultureInfo.InvariantCulture, $"; controller events {Hero.Events}, moved by ({Hero.LastMove.Delta.X:R}, {Hero.LastMove.Delta.Y:R}), touches [{string.Join("; ", contacts)}]; input {input}");
        return new Divergence(Steps, string.Join(", ", parts), string.Join(" ", differs), position, velocity, Steps - _resyncedAt > Settle, detail);
    }

    /// <summary>A platform as the stage left it in this step, without the stage's own reaction to being stood on.</summary>
    private sealed class Ride(RectF bounds, Vector2 delta) : IRideable
    {
        public RectF Bounds => bounds;
        public Vector2 Delta => delta;
        public void StoodOn() { }
    }
}
