using System.Numerics;
using NeonRift.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace NeonRift.Gameplay;

/// <summary>Patrol robot: runs in one direction, fires a single shot when facing the player, drops off ledges.</summary>
public sealed class Trooper : Enemy
{
    private Body _body = new() { Box = new RectF(-7, -42, 14, 42) };
    private float _shotTimer;
    private float _pause;
    private int _dir;

    public Trooper(Vector2 feet, int dir, float firstShot)
    {
        Position = feet;
        _dir = dir;
        _shotTimer = firstShot;
        Hitbox = new RectF(-8, -44, 16, 44);
        Points = 100;
    }

    /// <summary>Adds the enemy's state, then the shot timer, the firing pause, the walking direction and footing.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_shotTimer);
        hash.Add(_pause);
        hash.Add(_dir);
        hash.Add(_body.Grounded);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        var v = Velocity;
        _pause -= dt;
        v.X = _pause > 0 ? 0 : _dir * 92;
        var pos = Position;
        _body.Move(ref pos, ref v, dt, Stage.Grid);
        Position = pos;
        Velocity = v;
        if (_body.Last.HitWall) _dir = -_dir;
        _shotTimer -= dt;
        var player = Stage.Player;
        bool facing = MathF.Sign(player.Position.X - Position.X) == _dir;
        if (_shotTimer <= 0 && _body.Grounded && facing && Stage.OnScreen(Bounds) && player.State == PlayerState.Alive)
        {
            _pause = 0.35f;
            _shotTimer = 99; // one shot per trooper keeps crowds readable
            Stage.EnemyFire(Position + new Vector2(_dir * 16, -34), new Vector2(_dir * 130, 0));
        }
        if (LeftBehind() || Bounds.Left > Stage.Camera.View.Right + 160) Destroy();
    }

    protected override void Die()
    {
        Stage.Sound("robot-death", 0.45f, pan: Stage.Pan(Position.X));
        base.Die();
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var sprite = _pause > 0 || !_body.Grounded ? art.Frames("trooper_idle")[0] : art.Frames("trooper_run")[(int)(Age * 14) % 10];
        // The robot sheets face left; mirror them when moving right.
        DrawFlashing(r, sprite, Vector2.Round(DrawPosition), _dir > 0, Flash);
    }
}

/// <summary>Releases troopers from the screen edges while the player is near; stops once passed or exhausted.</summary>
public sealed class TrooperGate(float x, int count) : Enemy
{
    private int _left = count;
    private float _timer = 0.4f;
    public override bool Contact => false;

    public override bool Hit(int damage, Vector2 at) => false;

    /// <summary>Adds the enemy's state, then the troopers left to send and the time to the next one.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_left);
        hash.Add(_timer);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        _timer -= dt;
        if (_timer > 0) return;
        _timer = 1.7f + (float)Stage.Random.NextDouble() * 0.8f;
        var view = Stage.Camera.View;
        bool fromLeft = _left % 4 == 3 && Stage.Player.Position.X > view.Left + 140;
        float sx = fromLeft ? view.Left - 12 : view.Right + 12;
        if (Stage.GroundBelow(sx, view.Top) is { } ground)
        {
            Stage.Spawn(new Trooper(new Vector2(sx, ground), fromLeft ? 1 : -1, 0.5f + (float)Stage.Random.NextDouble()));
            _left--;
        }
        if (_left <= 0 || view.Left > x + 160) Destroy();
    }
}

/// <summary>Stationary rifleman: tracks the player in eight directions and fires two-round bursts.</summary>
public sealed class Gunner : Enemy
{
    private static readonly Color Visor = Color.Hex("ff3860");
    private float _timer = 1.6f;
    private int _burst;
    private int _facing = -1;

    public Gunner(Vector2 feet)
    {
        Position = feet;
        Hitbox = new RectF(-8, -44, 16, 44);
        Health = 3;
        Points = 300;
    }

    /// <summary>Adds the enemy's state, then the fire timer, the burst count and the facing.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_timer);
        hash.Add(_burst);
        hash.Add(_facing);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        var player = Stage.Player;
        _facing = player.Position.X < Position.X ? -1 : 1;
        _timer -= dt;
        if (_timer <= 0 && Stage.OnScreen(Bounds) && player.State == PlayerState.Alive)
        {
            var from = Position + new Vector2(_facing * 14, -36);
            var dir = Snap8(player.Position + new Vector2(0, -24) - from);
            Stage.EnemyFire(from, dir * 145);
            _burst++;
            _timer = _burst % 2 == 0 ? 2.4f : 0.25f;
        }
        if (LeftBehind()) Destroy();
    }

    /// <summary>Rounds a direction to the nearest of eight compass directions.</summary>
    public static Vector2 Snap8(Vector2 delta)
    {
        float angle = MathF.Round(MathF.Atan2(delta.Y, delta.X) / (MathF.PI / 4)) * (MathF.PI / 4);
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var sprite = art.Frames("trooper_idle")[(int)(Age * 5) % 3];
        DrawFlashing(r, sprite, Vector2.Round(DrawPosition), _facing > 0, Flash);
        // A red visor stripe marks riflemen apart from runners.
        r.FillRect(new RectF(MathF.Round(DrawPosition.X) + (_facing < 0 ? -9 : 4), MathF.Round(DrawPosition.Y) - 54, 5, 1), Visor);
    }
}

/// <summary>Hover drone: sweeps in on a sine path, then dives at where the player was and fires once.</summary>
public sealed class Drone : Enemy
{
    private readonly float _phase, _baseY;
    private Vector2 _dive;
    private bool _diving, _fired;

    public Drone(Vector2 at, float phase)
    {
        Position = at;
        _baseY = at.Y;
        _phase = phase;
        Hitbox = new RectF(-12, -10, 24, 20);
        Health = 2;
        Points = 200;
    }

    /// <summary>Adds the enemy's state, then the dive (whether, and its velocity) and whether it fired; the bobbing phase and height are fixed at spawn.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_diving);
        hash.Add(_dive);
        hash.Add(_fired);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        var player = Stage.Player;
        if (!_diving)
        {
            Position = new Vector2(Position.X - 85 * dt, _baseY + MathF.Sin(Age * 3 + _phase) * 22);
            if (Age > 1.6f + _phase * 0.4f && Stage.OnScreen(Bounds))
            {
                _diving = true;
                _dive = Vector2.Normalize(player.Position + new Vector2(0, -30) - Position) * 150;
            }
        }
        else
        {
            Position += _dive * dt;
            if (!_fired && Age > 2.2f + _phase * 0.4f)
            {
                _fired = true;
                Stage.EnemyFire(Position, Vector2.Normalize(player.Position + new Vector2(0, -24) - Position) * 140);
            }
        }
        if (LeftBehind() || Position.Y > Stage.Def.PixelHeight + 40 || Position.X > Stage.Camera.View.Right + 200) Destroy();
    }

    internal override void Render(IRenderer r, Assets art)
        // The drone sheet looks left; mirror it only while diving to the right.
        => DrawFlashing(r, art.Frames("drone")[(int)(Age * 12) % 4], Vector2.Round(DrawPosition), _diving && _dive.X > 0, Flash);
}

/// <summary>Bolted gun emplacement: aims at the player and fires, three-way when the player is close.</summary>
public sealed class Turret : Enemy
{
    private float _timer = 1f, _anim = 9;

    public Turret(Vector2 feet)
    {
        Position = feet;
        Hitbox = new RectF(-11, -21, 22, 21);
        Health = 6;
        Points = 400;
    }

    /// <summary>Adds the enemy's state, then the fire timer; the recoil animation time is left out (drawing only).</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_timer);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        _anim += dt;
        _timer -= dt;
        var player = Stage.Player;
        if (_timer <= 0 && Stage.OnScreen(Bounds) && player.State == PlayerState.Alive)
        {
            _timer = 2f;
            _anim = 0;
            var from = Position + new Vector2(player.Position.X < Position.X ? -9 : 9, -17);
            var dir = Vector2.Normalize(player.Position + new Vector2(0, -24) - from);
            Stage.Flash(from);
            Stage.EnemyFire(from, dir * 150);
            if (Vector2.Distance(player.Position, Position) < 110)
            {
                Stage.EnemyFire(from, Rotate(dir, 0.3f) * 150);
                Stage.EnemyFire(from, Rotate(dir, -0.3f) * 150);
            }
        }
        if (LeftBehind()) Destroy();
    }

    public static Vector2 Rotate(Vector2 v, float radians)
    {
        float c = MathF.Cos(radians), s = MathF.Sin(radians);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    internal override void Render(IRenderer r, Assets art)
    {
        // Frame 0 looks left (the rest of the sheet is a full head spin); a one-pixel dip sells the recoil.
        var at = Vector2.Round(DrawPosition) + new Vector2(0, _anim < 0.12f ? 1 : 0);
        DrawFlashing(r, art.Frames("turret")[0], at, Stage.Player.Position.X > Position.X, Flash);
    }
}

/// <summary>Armoured pod: invulnerable while shut; opens to fire a three-way fan and can be hurt only then.</summary>
public sealed class EggPod : Enemy
{
    private float _cycle;
    public override bool Big => true;
    public bool Open => _cycle is > 2.2f and < 3.8f;

    public EggPod(Vector2 feet)
    {
        Position = feet;
        Hitbox = new RectF(-16, -56, 32, 56);
        Health = 12;
        Points = 800;
        Armored = true;
    }

    /// <summary>Adds the enemy's state, then the open/close cycle time.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_cycle);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        if (!Stage.OnScreen(Bounds)) { if (LeftBehind()) Destroy(); return; }
        float before = _cycle;
        _cycle = (_cycle + dt) % 4.2f;
        Armored = !Open;
        if (before < 2.8f && _cycle >= 2.8f)
        {
            var from = Position + new Vector2(0, -40);
            var dir = Vector2.Normalize(Stage.Player.Position + new Vector2(0, -24) - from);
            for (int i = -1; i <= 1; i++) Stage.EnemyFire(from, Turret.Rotate(dir, i * 0.3f) * 125);
        }
    }

    internal override void Render(IRenderer r, Assets art)
    {
        Sprite sprite;
        if (_cycle < 2.0f) sprite = art.Frames("egg_idle")[(int)(Age * 3) % 2];
        else
        {
            var frames = art.Frames("egg_shoot");
            float t = _cycle - 2.0f;
            int frame = t < 0.2f ? (int)(t / 0.2f * 3) : _cycle > 3.8f ? Math.Max(0, 3 - (int)((_cycle - 3.8f) / 0.4f * 3)) : 3;
            sprite = frames[Math.Clamp(frame, 0, 3)];
        }
        // The gun port is drawn on the left; turn it towards a player on the right.
        DrawFlashing(r, sprite, Vector2.Round(DrawPosition), Stage.Player.Position.X > Position.X, Flash);
    }
}

/// <summary>Light fighter flying in formation from the right; peppers the ground with aimed shots.</summary>
public sealed class Skiff : Enemy
{
    private readonly float _baseY, _phase;
    private float _timer;

    public Skiff(Vector2 at, float phase)
    {
        Position = at;
        _baseY = at.Y;
        _phase = phase;
        _timer = 1.2f + phase;
        Hitbox = new RectF(-11, -8, 22, 16);
        Points = 150;
    }

    /// <summary>Adds the enemy's state, then the fire timer; the bobbing phase and height are fixed at spawn.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_timer);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        Position = new Vector2(Position.X - 105 * dt, _baseY + MathF.Sin(Age * 2.4f + _phase * 2) * 28);
        _timer -= dt;
        if (_timer <= 0 && Stage.OnScreen(Bounds))
        {
            _timer = 2.4f;
            Stage.EnemyFire(Position, Vector2.Normalize(Stage.Player.Position + new Vector2(0, -20) - Position) * 125);
        }
        if (Bounds.Right < Stage.Camera.View.Left - 40) Destroy();
    }

    internal override void Render(IRenderer r, Assets art)
        => DrawFlashing(r, art.Frames("skiff")[(int)(Age * 10) % 5], Vector2.Round(DrawPosition), false, Flash);
}

/// <summary>Supply ship that crosses the screen once; shooting it releases a weapon capsule.</summary>
public sealed class Carrier : Enemy
{
    private readonly char _letter;
    private readonly float _baseY;

    public Carrier(Vector2 at, char letter)
    {
        Position = at;
        _baseY = at.Y;
        _letter = letter;
        Hitbox = new RectF(-14, -10, 28, 20);
        Health = 2;
        Points = 500;
    }

    public override bool Contact => false;
    public char Letter => _letter;

    /// <summary>Adds the enemy's state, then the letter of the capsule it drops.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add((int)_letter);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        Position = new Vector2(Position.X + 140 * dt, _baseY + MathF.Sin(Age * 2.2f) * 18);
        if (Bounds.Left > Stage.Camera.View.Right + 30) Destroy();
    }

    protected override void Die()
    {
        Stage.Spawn(new Capsule(Center, _letter));
        base.Die();
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var at = Vector2.Round(DrawPosition);
        DrawFlashing(r, art.Frames("skiff")[(int)(Age * 10) % 5], at, true, Flash);
        Capsule.DrawPod(r, art, at + new Vector2(0, 13), _letter, Age);
    }
}
