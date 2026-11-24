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
/// <para>The shadow lives in a tile physics world of its own: the level's grid as tile geometry, a one-way box for
/// each moving platform (moved to where the stage left it), a box for each hidden block with the controller's
/// <see cref="CharacterController2D.Solid"/> rule, and one kinematic body whose origin is Pip's top-left corner, so
/// positions compare without a conversion.</para>
/// <para>Each step the game's mover is also run on a copy of Pip from the state before the step (the "expected"
/// result), which separates what the mover did from what the stage did afterwards (a stomp's bounce, a spring, a
/// hit). In the closed loop the shadow is put back on Pip whenever it leaves him, so every divergence is found; in the
/// open loop it is left alone, so a divergence shows how far it carries.</para>
/// </remarks>
internal sealed class ControllerShadow
{
    /// <summary>How the game maps its feel onto <see cref="CharacterMotion"/>.</summary>
    public enum Mapping
    {
        /// <summary>Two records, walking and running, as the engine's classic sample does it.</summary>
        TwoRecords,
        /// <summary>
        /// A record chosen each step from the character's state (ground, braking above the cap, air, air momentum), and
        /// the vertical speed set by the game after a hidden block is hit: everything the public API lets a game do.
        /// </summary>
        PerState,
    }

    private const float Dt = StageHarness.Dt;
    /// <summary>Steps after the shadow was put back on Pip in which a divergence may come from that (the controller's timers and ground cannot be set).</summary>
    private const int Settle = 30;

    // The sample's numbers (samples/PhysicsPlayground/ClassicScene.cs) with the game's drop time.
    private static readonly CharacterMotion Walk = new()
    {
        MoveSpeed = Tuning.WalkSpeed, Acceleration = Tuning.GroundAccel, Deceleration = Tuning.GroundFriction, AirControl = 0.8f,
        SkidDeceleration = Tuning.SkidDecel, SkidSpeed = 30,
        JumpSpeed = Tuning.JumpSpeed, RunJumpBonus = Tuning.RunJumpBonus,
        Gravity = Tuning.RiseGravity,
        JumpReleaseGravityScale = Tuning.ReleaseGravity / Tuning.RiseGravity,
        ApexGravityScale = Tuning.ApexGravity / Tuning.RiseGravity, ApexSpeed = Tuning.ApexThreshold,
        FallGravityScale = Tuning.FallGravity / Tuning.RiseGravity, MaxFallSpeed = Tuning.TerminalVelocity,
        CoyoteTime = Tuning.CoyoteTime, JumpBuffer = Tuning.JumpBuffer, DropThroughTime = Tuning.DropThroughTime,
        CornerCorrection = Tuning.CornerCorrection, CeilingReboundSpeed = 20,
    };
    private static readonly CharacterMotion Run = Walk with { MoveSpeed = Tuning.RunSpeed, Acceleration = Tuning.RunAccel };
    // The per-state records: full control in the air, with the air's own numbers.
    private static readonly CharacterMotion GroundWalk = Walk with { AirControl = 1 };
    private static readonly CharacterMotion GroundRun = Run with { AirControl = 1 };
    private static readonly CharacterMotion WalkBraking = GroundWalk with { Acceleration = Tuning.GroundFriction };
    private static readonly CharacterMotion RunBraking = GroundRun with { Acceleration = Tuning.GroundFriction };
    private static readonly CharacterMotion AirWalk = GroundWalk with { Acceleration = Tuning.AirAccel, Deceleration = Tuning.AirDrag };
    private static readonly CharacterMotion AirRun = GroundRun with { Acceleration = Tuning.AirAccel, Deceleration = Tuning.AirDrag };

    private readonly Stage _stage;
    private readonly Level _mirror;
    private readonly TilePhysicsWorld2D _world = new();
    private readonly List<BodyId> _platforms = [];
    private readonly Dictionary<ShapeId, (int X, int Y)> _hidden = [];
    private readonly Mapping _mapping;
    private readonly bool _closedLoop;
    private bool _inSync = true;
    private int _resyncedAt = -Settle;

    public CharacterController2D Hero { get; }
    /// <summary>Steps in which both movers ran.</summary>
    public int Steps { get; private set; }
    /// <summary>Steps in which the stage changed Pip after his mover ran (a stomp, a spring, a hit, the goal).</summary>
    public int Interventions { get; private set; }
    /// <summary>Closed loop: steps that started with the two together and ended with them apart.</summary>
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
    public sealed record Divergence(int Step, string Situation, string Differs, Vector2 Position, Vector2 Velocity, bool Pristine, string Detail);

    public ControllerShadow(Stage stage, Mapping mapping, bool closedLoop)
    {
        _stage = stage;
        _mapping = mapping;
        _closedLoop = closedLoop;
        _mirror = new Level(stage.Level.Def);
        var level = _world.CreateBody(default, BodyDescription.Static());
        _world.AddTiles(level, _mirror.Grid);
        var tiles = stage.Level.Tiles;
        for (int y = 0; y < tiles.Height; y++)
        for (int x = 0; x < tiles.Width; x++)
            if (tiles[x, y] == Tile.HiddenLife)
                _hidden[_world.AddShape(level, ShapeDescription.Box(new Vector2(16, 16), new Vector2(x * 16 + 8, y * 16 + 8)))] = (x, y);
        foreach (var platform in stage.Platforms)
        {
            var bounds = platform.Bounds;
            var body = _world.CreateBody(default, BodyDescription.Kinematic(bounds.Position));
            _world.AddShape(body, ShapeDescription.Box(bounds.Size, bounds.Size / 2) with { OneWay = true });
            _platforms.Add(body);
        }
        var shape = ShapeDescription.Box(new Vector2(Player.Width, Player.Height), new Vector2(Player.Width / 2f, Player.Height / 2f));
        var pip = _world.CreateBody(default, BodyDescription.Kinematic(stage.Player.Position));
        _world.AddShape(pip, shape);
        Hero = new CharacterController2D(_world, pip, GroundWalk) { Filter = QueryFilter.For(shape) };
        // A hidden block is solid only for a head that comes up from below it, in its column, until it is found
        // (the stage then makes the tile a used block, which the grid holds).
        Hero.Solid = (hero, id) =>
        {
            if (!_hidden.TryGetValue(id, out var cell)) return true;
            if (_mirror.Tiles[cell.X, cell.Y] != Tile.HiddenLife) return false;
            float head = hero.Position.Y, under = (cell.Y + 1) * Level.TileSize;
            int column = (int)MathF.Floor((hero.Position.X + Player.Width / 2f) / Level.TileSize);
            return hero.Velocity.Y < 0 && head >= under - 0.01f && column == cell.X;
        };
    }

    /// <summary>One update of the stage inside one step of its world, then the same step for the shadow.</summary>
    public void Step(PlayerInput input)
    {
        var player = _stage.Player;
        var before = player.Clone();
        var expected = player.Clone();
        var phase = _stage.Phase;
        var tiles = _stage.Level.Tiles;
        for (int y = 0; y < tiles.Height; y++)
        for (int x = 0; x < tiles.Width; x++)
            if (_mirror.Tiles[x, y] != tiles[x, y]) _mirror.SetTile(x, y, tiles[x, y]);

        var world = (SceneWorld)_stage.Objects;
        world.BeginStep(world.SteppedAt + 1, Dt);
        float step = world.Clock.Delta;
        _stage.Update(Dt, input);
        world.EndStep();
        if (phase != StagePhase.Playing || step == 0)
        {
            // Pip's mover did not run (a hit-stop, the goal, a death): nothing to compare.
            if (phase != StagePhase.Playing) _inSync = false;
            return;
        }

        var rides = new List<IRideable>();
        for (int i = 0; i < _stage.Platforms.Count; i++)
        {
            var platform = _stage.Platforms[i];
            rides.Add(new Ride(platform.Bounds, platform.Delta));
            _world.SetPosition(_platforms[i], platform.Bounds.Position);
        }
        expected.Step(step, input, _mirror, rides);
        bool intervened = !Same(expected.Position, expected.Velocity, expected.Grounded, player);
        if (intervened) Interventions++;

        if (_closedLoop && !_inSync) Resync(before);
        Hero.Motion = MotionFor(input);
        // The game drops through a platform with down and jump together; the controller with its drop intent.
        Hero.Step(step, new CharacterInput(input.Axis, input.JumpPressed, input.JumpHeld, Drop: input.Down && input.JumpPressed));
        // The game's reaction to what the step touched: a hidden block stops the head where other ceilings send it down.
        if (_mapping == Mapping.PerState && (Hero.Events & CharacterEvents.HitCeiling) != 0)
            foreach (var contact in Hero.LastMove.Contacts)
                if (_hidden.ContainsKey(contact.Shape)) Hero.Velocity = Hero.Velocity with { Y = 0 };
        Steps++;
        bool nudged = before.Velocity.X == 0 && input.Axis == 0 && before.Riding is null && expected.Position.X != before.Position.X;
        Corners = (Corners.Game + (nudged ? 1 : 0), Corners.Controller + ((Hero.Events & CharacterEvents.CornerCorrected) != 0 ? 1 : 0));

        if (_closedLoop)
        {
            if (_inSync && !Same(Hero.Position, Hero.Velocity, Hero.IsGrounded, expected))
                Divergences.Add(Describe(before, expected, input));
            _inSync = Same(Hero.Position, Hero.Velocity, Hero.IsGrounded, player) && Hero.IsJumping == player.Jumping;
            return;
        }
        Apart = Hero.Position - player.Position;
        float distance = Apart.Length();
        if (distance > 0 || Hero.Velocity != player.Velocity || Hero.IsGrounded != player.Grounded)
        {
            StepsApart++;
            if (FirstApart < 0)
            {
                FirstApart = Steps;
                FirstApartDetail = string.Create(CultureInfo.InvariantCulture,
                    $"game pos=({player.Position.X:R}, {player.Position.Y:R}) vel=({player.Velocity.X:R}, {player.Velocity.Y:R}) grounded={player.Grounded} events={player.Events}; controller pos=({Hero.Position.X:R}, {Hero.Position.Y:R}) vel=({Hero.Velocity.X:R}, {Hero.Velocity.Y:R}) grounded={Hero.IsGrounded} events={Hero.Events}");
            }
        }
        if (distance > Farthest.Pixels) Farthest = (distance, Steps);
    }

    /// <summary>
    /// Puts the shadow where Pip is, with his velocity. The controller's ground and jump state and its timers cannot be
    /// set: a divergence found soon afterwards may come from them, which <see cref="Divergence.Pristine"/> tells.
    /// </summary>
    private void Resync(Player pip)
    {
        _resyncedAt = Steps;
        _world.SetPosition(Hero.Body, pip.Position);
        Hero.Velocity = pip.Velocity;
        _inSync = Hero.IsGrounded == pip.Grounded && Hero.IsJumping == pip.Jumping;
    }

    private CharacterMotion MotionFor(PlayerInput input)
    {
        if (_mapping == Mapping.TwoRecords) return input.RunHeld ? Run : Walk;
        float cap = input.RunHeld ? Tuning.RunSpeed : Tuning.WalkSpeed, run = Hero.Velocity.X;
        if (Hero.IsGrounded)
            return MathF.Abs(run) > cap && input.Axis != 0 ? (input.RunHeld ? RunBraking : WalkBraking) : (input.RunHeld ? GroundRun : GroundWalk);
        // Air momentum: pushing the way it moves never slows it toward a lower cap, so the cap follows the speed.
        var air = input.RunHeld ? AirRun : AirWalk;
        float momentum = input.Axis != 0 && MathF.Sign(run) == input.Axis ? MathF.Abs(run) : 0;
        return momentum > cap ? air with { MoveSpeed = momentum } : air;
    }

    private static bool Same(Vector2 position, Vector2 velocity, bool grounded, Player pip) =>
        position == pip.Position && velocity == pip.Velocity && grounded == pip.Grounded;

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
            parts.Add(_mirror.Tiles[expected.BumpedTile.X, expected.BumpedTile.Y] == Tile.HiddenLife ? "hidden block" : "head bump");
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
        string detail = string.Create(CultureInfo.InvariantCulture,
            $"before pos=({before.Position.X:R}, {before.Position.Y:R}) vel=({before.Velocity.X:R}, {before.Velocity.Y:R}); game pos=({expected.Position.X:R}, {expected.Position.Y:R}) vel=({expected.Velocity.X:R}, {expected.Velocity.Y:R}); controller pos=({Hero.Position.X:R}, {Hero.Position.Y:R}) vel=({Hero.Velocity.X:R}, {Hero.Velocity.Y:R})");
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
