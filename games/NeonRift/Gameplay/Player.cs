using System.Numerics;
using NeonRift.Art;
using MicroEngine;
using MicroEngine.Graphics;

namespace NeonRift.Gameplay;

/// <summary>One update's worth of player intent, decoupled from devices so tests can drive it.</summary>
public readonly record struct PlayerInput(bool Left, bool Right, bool Up, bool Down, bool Jump, bool JumpHeld, bool Fire, bool Lock)
{
    public static readonly PlayerInput None = new();
}

public enum PlayerState { Alive, Dying, Waiting }

/// <summary>
/// Vela. Runs, somersault-jumps, drops through platforms and aims in eight directions: up and the
/// diagonals by holding a direction, prone by crouching on the ground, straight down while airborne.
/// Holding "lock" plants her feet so diagonals can be aimed without walking. Each life takes
/// <see cref="MaxHealth"/> hits, with a short flashing invulnerability after each one; pits and lava
/// are always fatal.
/// </summary>
public sealed class Player : Actor
{
    public const float RunSpeed = 118, Gravity = 1000, JumpSpeed = 400, MaxFall = 430;
    public const float RespawnInvulnerability = 3f, HitInvulnerability = 1.2f, BarrierTime = 10f;
    public const int MaxHealth = 3;
    private static readonly RectF StandBox = new(-6, -40, 12, 40);

    private Body _body = new() { Box = StandBox };
    private float _cooldown, _dropTimer, _lastShot = 10, _stateTime, _runTime;
    private bool _somersault;
    private Vector2 _aim = Vector2.UnitX;

    public PlayerState State { get; private set; } = PlayerState.Alive;
    public int Facing { get; private set; } = 1;
    public bool Grounded => _body.Grounded;
    public bool Crouching { get; private set; }
    public bool Somersault => _somersault && !_body.Grounded;
    public Vector2 Aim => _aim;
    public float Invulnerable { get; set; }
    public int Health { get; private set; } = MaxHealth;
    public float Barrier { get; set; }
    public WeaponSpec Weapon { get; private set; } = WeaponSpec.Of(WeaponKind.Pulse);
    /// <summary>Developer switch for capture runs: shots and contact never kill (pits and lava still do).</summary>
    public bool God { get; set; }
    public bool Vulnerable => State == PlayerState.Alive && Invulnerable <= 0 && Barrier <= 0 && !God;
    public float StateTime => _stateTime;

    /// <summary>The body that enemy shots and contact test against; it shrinks when kneeling or tucked.</summary>
    public RectF Hurtbox => (Crouching ? new RectF(-9, -28, 18, 28) : Somersault ? new RectF(-8, -36, 16, 22) : new RectF(-6, -40, 12, 38)).Offset(Position);

    public Player(Vector2 feet, WeaponKind weapon)
    {
        Position = feet;
        Hitbox = StandBox;
        Weapon = WeaponSpec.Of(weapon);
    }

    public void Equip(WeaponKind kind) => Weapon = WeaponSpec.Of(kind);

    public void Step(float dt, PlayerInput input)
    {
        Update(dt);
        _stateTime += dt;
        Invulnerable = MathF.Max(0, Invulnerable - dt);
        Barrier = MathF.Max(0, Barrier - dt);
        _lastShot += dt;
        switch (State)
        {
            case PlayerState.Alive: Live(dt, input); break;
            case PlayerState.Dying: Fall(dt); break;
        }
    }

    private void Live(float dt, PlayerInput input)
    {
        int move = (input.Right ? 1 : 0) - (input.Left ? 1 : 0);
        int vertical = (input.Down ? 1 : 0) - (input.Up ? 1 : 0);
        bool planted = input.Lock && _body.Grounded;
        Crouching = _body.Grounded && input.Down && move == 0;
        if (move != 0) Facing = move;

        var v = Velocity;
        v.X = Crouching || planted ? 0 : move * RunSpeed;
        _dropTimer = MathF.Max(0, _dropTimer - dt);
        if (input.Jump && _body.Grounded)
        {
            if (input.Down && StandingOnPlatform())
            {
                _dropTimer = 0.25f;
                Position += new Vector2(0, 2);
            }
            else
            {
                v.Y = -JumpSpeed;
                _somersault = true;
                Crouching = false;
                Stage.Sound("jump", 0.5f);
            }
        }
        // Releasing jump early cuts the rise for a shorter hop.
        if (!input.JumpHeld && v.Y < -JumpSpeed * 0.45f && _somersault) v.Y = -JumpSpeed * 0.45f;
        bool wasGrounded = _body.Grounded;
        var pos = Position;
        _body.Move(ref pos, ref v, dt, Stage.Grid, Gravity, MaxFall, _dropTimer > 0);
        Position = new Vector2(Math.Clamp(pos.X, Stage.Camera.View.Left + 8, Stage.Camera.View.Right - 8), pos.Y);
        Velocity = v;
        if (_body.Grounded)
        {
            if (!wasGrounded && _somersault) Stage.Sound("land", 0.35f);
            _somersault = false;
        }
        _runTime = _body.Grounded && v.X != 0 ? _runTime + dt : 0;

        // Aim: horizontal follows facing unless only up/down is held.
        int ax = move != 0 ? move : 0, ay = vertical;
        if (_body.Grounded && ay > 0 && ax == 0) ay = 0; // kneeling shoots forward
        if (ax == 0 && ay == 0) ax = Facing;
        _aim = Vector2.Normalize(new Vector2(ax, ay));

        _cooldown -= dt;
        if (input.Fire && _cooldown <= 0 && Stage.PlayerShots.Count < Weapon.MaxOnScreen)
        {
            _cooldown = Weapon.Cooldown;
            _lastShot = 0;
            var muzzle = Muzzle();
            foreach (var dir in Weapon.Directions(_aim, Stage.Random))
                Stage.PlayerShots.Add(new PlayerShot(Weapon, muzzle, dir * Weapon.Speed) { Stage = Stage });
            Stage.Sound(Weapon.Sound, Weapon.Kind == WeaponKind.Rapid ? 0.35f : 0.5f);
            Stage.Flash(muzzle);
        }
    }

    private bool StandingOnPlatform()
    {
        int ts = Stage.Grid.TileSize;
        var feet = Position + new Vector2(0, 1);
        int y = (int)MathF.Floor(feet.Y / ts);
        bool any = false;
        foreach (float dx in new[] { -5f, 0, 5f })
        {
            var flags = Stage.Grid.FlagsAt((int)MathF.Floor((feet.X + dx) / ts), y);
            if ((flags & MicroEngine.World.TileFlags.Solid) != 0) return false;
            any |= (flags & MicroEngine.World.TileFlags.OneWay) != 0;
        }
        return any;
    }

    /// <summary>World position where bullets appear for the current pose and aim.</summary>
    public Vector2 Muzzle()
    {
        if (Somersault || !_body.Grounded && _aim.Y > 0.5f) return Position + new Vector2(0, -26) + _aim * 16;
        if (Crouching) return Position + new Vector2(22 * Facing, -24);
        var rig = Stage.Rigs?.Invoke(_body.Grounded && Velocity.X != 0 ? "run-shoot" + RunFrame : "shoot0");
        if (rig is null) return Position + new Vector2(0, -38) + _aim * 22;
        var shoulder = Position + new Vector2(rig.Shoulder.X * Facing, rig.Shoulder.Y);
        return shoulder + _aim * rig.Length;
    }

    public int RunFrame => (int)(_runTime * 12) % 8;

    /// <summary>Applies one hit from a shot or an enemy. Returns false if the player was protected.</summary>
    public bool Hurt()
    {
        if (!Vulnerable) return false;
        Health--;
        if (Health <= 0) return Kill(force: true);
        Invulnerable = HitInvulnerability;
        Velocity = new Vector2(-Facing * 60, _body.Grounded ? -140 : Velocity.Y);
        Stage.Sound("hit", 0.9f, 0.8f);
        Stage.Camera.Shake(0.2f);
        return true;
    }

    /// <summary>Starts the death fall (or, after falling into a pit, skips straight to the respawn wait). Returns false if the player was protected.</summary>
    public bool Kill(bool force = false, bool fell = false)
    {
        if (State != PlayerState.Alive || !force && !Vulnerable) return false;
        State = fell ? PlayerState.Waiting : PlayerState.Dying;
        _stateTime = 0;
        Crouching = false;
        _somersault = false;
        Velocity = new Vector2(-Facing * 70, -260);
        Stage.Sound("player-death", 0.7f);
        Stage.Camera.Shake(0.4f);
        return true;
    }

    private void Fall(float dt)
    {
        var v = Velocity;
        var pos = Position;
        _body.Move(ref pos, ref v, dt, Stage.Grid, Gravity, MaxFall);
        if (_body.Grounded) v.X = 0;
        Position = pos;
        Velocity = v;
        // Off the bottom of the map there is nothing left to show.
        if (_stateTime > 1.3f || Position.Y > Stage.Def.PixelHeight + 60) { State = PlayerState.Waiting; _stateTime = 0; }
    }

    /// <summary>Drops back in from the top of the screen at <paramref name="x"/>, briefly invulnerable.</summary>
    public void Respawn(float x, WeaponKind weapon)
    {
        Position = new Vector2(x, Stage.Camera.View.Top + 40);
        Snap();
        Velocity = Vector2.Zero;
        State = PlayerState.Alive;
        _stateTime = 0;
        Health = MaxHealth;
        Invulnerable = RespawnInvulnerability;
        _somersault = true;
        Facing = 1;
        Equip(weapon);
    }

    internal override void Render(IRenderer r, Assets art)
    {
        if (State == PlayerState.Waiting) return;
        if (Invulnerable > 0 && State == PlayerState.Alive && (int)(Invulnerable * 20) % 2 == 0) return;
        var feet = Vector2.Round(DrawPosition);
        bool flip = Facing < 0;
        if (Barrier > 0 && (Barrier > 2 || (int)(Barrier * 10) % 2 == 0))
        {
            r.SetBlendMode(BlendMode.Additive);
            var ring = art.Frames("charged")[(int)(Age * 14) % 6];
            r.DrawPivoted(ring, feet + new Vector2(0, -24), Age * 2, new Vector2(0.8f, 1.1f), Color.Hex("7af0ff").WithAlpha(0.7f));
            r.SetBlendMode(BlendMode.Alpha);
        }
        if (State == PlayerState.Dying)
        {
            r.DrawPivoted(art["hero_hurt0"], feet, _body.Grounded ? -MathF.PI / 2 * Facing * 0.9f : _stateTime * -9 * Facing, flipX: flip);
            return;
        }
        if (Somersault)
        {
            var frames = art.Frames("hero_back-jump");
            r.Draw(frames[2 + (int)(Age * 16) % 4], feet, flipX: flip);
            return;
        }
        if (!_body.Grounded)
        {
            var frames = art.Frames("hero_jump");
            r.Draw(frames[Velocity.Y < 0 ? 1 : 3], feet, flipX: flip);
            return;
        }
        if (Crouching) { r.Draw(art["hero_crouch0"], feet, flipX: flip); return; }
        bool running = Velocity.X != 0;
        bool straight = MathF.Abs(_aim.Y) < 0.01f;
        if (straight)
        {
            if (running) r.Draw(art.Frames("hero_run-shoot")[RunFrame], feet, flipX: flip);
            else if (_lastShot < 0.7f) r.Draw(art["hero_shoot0"], feet, flipX: flip);
            else r.Draw(art.Frames("hero_idle")[(int)(Age * 6) % 4], feet, flipX: flip);
            return;
        }
        var rig = art.Rig(running ? "run-shoot" + RunFrame : "shoot0");
        float angle = MathF.Atan2(_aim.Y, MathF.Abs(_aim.X) < 0.01f ? 0.0001f : MathF.Abs(_aim.X));
        var shoulder = feet + new Vector2(rig.Shoulder.X * Facing, rig.Shoulder.Y);
        r.Draw(rig.Body, feet, flipX: flip);
        r.DrawPivoted(rig.Arm, shoulder, flip ? -angle : angle, flipX: flip);
    }
}
