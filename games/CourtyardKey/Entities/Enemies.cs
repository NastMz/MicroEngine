using System.Numerics;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace CourtyardKey.Entities;

/// <summary>Hops toward the hero in short committed leaps; telegraphs each hop with a squash.</summary>
public sealed class Slime : Enemy
{
    private const float HopTime = 0.42f;
    private float _wait;
    private float _hop = -1;
    private Vector2 _hopVelocity;

    public Slime(PlayScene world, Vector2 at) : base(world, at, 2)
    {
        Hitbox = new RectF(-6, -9, 12, 9);
        _wait = 0.4f + (float)world.Rng.NextDouble() * 0.8f;
    }

    public float Height => _hop >= 0 ? MathF.Sin(_hop / HopTime * MathF.PI) * 7 : 0;

    /// <summary>State hash: the wait before the next hop and the hop in progress.</summary>
    protected override void HashAiState(StateHasher hash)
    {
        hash.Add(_wait);
        hash.Add(_hop);
        hash.Add(_hopVelocity);
    }

    protected override void Think(float dt)
    {
        if (_hop >= 0)
        {
            _hop += dt;
            var r = Move(_hopVelocity * dt);
            if (r.HitWall) _hopVelocity.X = -_hopVelocity.X;
            if (r.HitCeiling || r.HitFloor) _hopVelocity.Y = -_hopVelocity.Y;
            if (_hop >= HopTime)
            {
                _hop = -1;
                _wait = 0.55f + (float)World.Rng.NextDouble() * 0.6f;
            }
            return;
        }
        _wait -= dt;
        if (_wait > 0) return;
        var toHero = ToHero;
        // Mostly toward the hero, sometimes a random hop so groups spread out.
        var dir = toHero.LengthSquared() > 1 && World.Rng.NextDouble() < 0.75 ? Vector2.Normalize(toHero) : MicroEngine.MathX.FromAngle((float)World.Rng.NextDouble() * MathF.Tau);
        _hopVelocity = dir * 62;
        _hop = 0;
        World.Audio.Play("hop", 0.5f, 1f, 0.15f);
    }

    public override void DrawShadow(IRenderer r) => r.Draw(Art["shadow_m"], DrawPosition);

    public override void Draw(IRenderer r)
    {
        string frame = _hop >= 0 ? "slime2" : _wait < 0.18f && !Stunned ? "slime1" : "slime0";
        float wobble = _hop < 0 ? MathF.Round(MicroEngine.MathX.Wave(Time, 1.5f, 0.6f)) : 0;
        DrawSprite(r, Flashing(frame), DrawPosition + new Vector2(0, -Height + wobble));
        DrawStunStars(r, 16);
    }
}

/// <summary>Flutters erratically, rests now and then, and swoops toward the hero.</summary>
public sealed class Bat : Enemy
{
    private float _turn, _rest, _flight;
    private Vector2 _velocity;
    private float _flapSound;

    public Bat(PlayScene world, Vector2 at) : base(world, at, 1)
    {
        Hitbox = new RectF(-6, -8, 12, 8);
        _rest = 0.5f + (float)world.Rng.NextDouble() * 1.2f;
    }

    private static readonly string[] Frames = ["bat0", "bat1", "bat2"];

    /// <summary>State hash: turn, rest and flight timers, the velocity and the flap sound timer.</summary>
    protected override void HashAiState(StateHasher hash)
    {
        hash.Add(_turn);
        hash.Add(_rest);
        hash.Add(_flight);
        hash.Add(_velocity);
        hash.Add(_flapSound);
    }

    public override bool Flies => true;
    protected override bool FragileToBoomerang => true;
    private bool Resting => _rest > 0;
    private float Altitude => Resting ? 2 : 9 + MathF.Sin(Time * 5) * 2;
    public override RectF Hurtbox => Bounds.Offset(0, -Altitude + 2);

    protected override void Think(float dt)
    {
        if (_rest > 0)
        {
            _rest -= dt;
            if (_rest <= 0) _flight = 2.2f + (float)World.Rng.NextDouble() * 2.5f;
            return;
        }
        _flight -= dt;
        _turn -= dt;
        if (_turn <= 0)
        {
            _turn = 0.25f + (float)World.Rng.NextDouble() * 0.4f;
            float angle = (float)World.Rng.NextDouble() * MathF.Tau;
            var wander = MicroEngine.MathX.FromAngle(angle);
            var toHero = ToHero;
            if (toHero.LengthSquared() > 1 && World.Rng.NextDouble() < 0.45) wander = Vector2.Normalize(toHero);
            _velocity = wander * (50 + (float)World.Rng.NextDouble() * 30);
        }
        var r = Move(_velocity * dt, flying: true);
        if (r.HitWall) _velocity.X = -_velocity.X;
        if (r.HitCeiling || r.HitFloor) _velocity.Y = -_velocity.Y;
        if ((_flapSound -= dt) <= 0) { _flapSound = 0.5f; World.Audio.Play("flap", 0.25f, 1f, 0.2f); }
        if (_flight <= 0) _rest = 0.8f + (float)World.Rng.NextDouble() * 1.4f;
    }

    public override void DrawShadow(IRenderer r) => r.Draw(Art["shadow_s"], DrawPosition);

    public override void Draw(IRenderer r)
    {
        int f = Resting || Stunned ? 1 : (int)(Time * 14) % 3;
        DrawSprite(r, Flashing(Frames[f]), DrawPosition + new Vector2(0, -Altitude));
        DrawStunStars(r, Altitude + 8);
    }
}

/// <summary>A rooted thorn bulb: swells (telegraph), then spits a seed straight at the hero.</summary>
public sealed class Spitter : Enemy
{
    private const float Idle = 1.5f, Swell = 0.45f, Fire = 0.25f;
    private float _cycle;

    public Spitter(PlayScene world, Vector2 at) : base(world, at, 3)
    {
        Hitbox = new RectF(-6, -12, 12, 12);
        _cycle = (float)world.Rng.NextDouble() * Idle;
    }

    private static readonly string[] Frames = ["spitter0", "spitter1", "spitter2"];

    /// <summary>State hash: the position in the idle, swell and fire cycle.</summary>
    protected override void HashAiState(StateHasher hash) => hash.Add(_cycle);

    protected override float KnockbackScale => 0;
    public override bool IsSolid => true;
    public override RectF SolidBox => new(Position.X - 6, Position.Y - 8, 12, 8);

    protected override void Think(float dt)
    {
        float before = _cycle;
        _cycle += dt;
        if (before < Idle + Swell && _cycle >= Idle + Swell)
        {
            var dir = ToHero;
            if (dir.LengthSquared() < 1 || dir.Length() > 170) { _cycle = 0; return; }
            dir = Vector2.Normalize(dir);
            new Projectile(World, Center + dir * 6, dir * 92, "seed", damage: 1);
            World.Audio.Play("spit", 0.6f, 1f, 0.1f);
        }
        if (_cycle >= Idle + Swell + Fire) _cycle = 0;
    }

    public override void Draw(IRenderer r)
    {
        int f = _cycle < Idle ? 0 : _cycle < Idle + Swell ? 1 : 2;
        var shake = f == 1 ? new Vector2((int)(Time * 30) % 2, 0) : Vector2.Zero;
        DrawSprite(r, Flashing(Frames[f]), Position + shake);
        DrawStunStars(r, 16);
    }
}

/// <summary>
/// An armoured sentinel. Its shield blocks sword strikes from the front, so the hero must
/// flank it, or stun it with the boomerang so the guard drops.
/// </summary>
public sealed class Knight : Enemy
{
    private const float WalkSpeed = 30f;
    private float _think, _anim;
    private Dir _facing = Dir.Down;

    public Knight(PlayScene world, Vector2 at) : base(world, at, 4) => Hitbox = new RectF(-6, -10, 12, 10);

    private static readonly string[] DownFrames = ["knight_down0", "knight_down1"];
    private static readonly string[] UpFrames = ["knight_up0", "knight_up1"];
    private static readonly string[] SideFrames = ["knight_side0", "knight_side1"];

    /// <summary>State hash: the think and walk timers and the facing (which decides what the shield blocks).</summary>
    protected override void HashAiState(StateHasher hash)
    {
        hash.Add(_think);
        hash.Add(_anim);
        hash.Add((int)_facing);
    }

    public override int ContactDamage => 2;
    protected override float KnockbackScale => 0.6f;
    public Dir Facing => _facing;

    protected override bool Blocks(in Hit hit) => Combat.ShieldBlocks(_facing, Center, hit.From, Stunned);

    protected override void Think(float dt)
    {
        _think -= dt;
        if (_think <= 0)
        {
            // Re-aim slowly: this delay is the window the player uses to slip around the shield.
            _think = 0.7f + (float)World.Rng.NextDouble() * 0.4f;
            var d = ToHero;
            _facing = DirExtensions.FromVector(d);
        }
        var r = Move(_facing.ToVector() * WalkSpeed * dt);
        if (r.HitWall || r.HitCeiling || r.HitFloor)
        {
            var d = ToHero;
            // Try the other axis toward the hero.
            _facing = _facing.IsHorizontal() ? (d.Y < 0 ? Dir.Up : Dir.Down) : (d.X < 0 ? Dir.Left : Dir.Right);
            _think = 0.4f;
        }
        _anim += dt;
    }

    public override void DrawShadow(IRenderer r) => r.Draw(Art["shadow_m"], DrawPosition);

    public override void Draw(IRenderer r)
    {
        var frames = _facing switch { Dir.Down => DownFrames, Dir.Up => UpFrames, _ => SideFrames };
        int f = Stunned ? 0 : (int)(_anim * 5) % 2;
        var at = DrawPosition + (Stunned ? new Vector2((int)(Time * 20) % 2, 0) : Vector2.Zero);
        DrawSprite(r, Flashing(frames[f]), at, _facing == Dir.Left);
        DrawStunStars(r, 18);
    }
}

/// <summary>An enemy shot. Blocked by walls (not water); the sword knocks it out of the air.</summary>
public sealed class Projectile : Actor
{
    private readonly Vector2 _velocity;
    private readonly string _sprite;
    private float _life = 3f;

    public Projectile(PlayScene world, Vector2 at, Vector2 velocity, string sprite, int damage) : base(world)
    {
        Position = at;
        _velocity = velocity;
        _sprite = sprite;
        Damage = damage;
        Hitbox = new RectF(-3, -3, 6, 6);
    }

    public int Damage { get; }
    public override float Depth => Position.Y + 6;

    /// <summary>State hash: velocity, remaining life and damage (the sprite name is drawing only).</summary>
    protected override void HashOwnState(StateHasher hash)
    {
        hash.Add(_velocity);
        hash.Add(_life);
        hash.Add(Damage);
    }

    public override void Update(float dt)
    {
        _life -= dt;
        var r = Move(_velocity * dt, flying: true, throughActors: true);
        if (r.HitWall || r.HitCeiling || r.HitFloor || _life <= 0) Pop();
    }

    public void Pop()
    {
        World.Burst(Position, Fx.HitSpark, 4, additive: true);
        Destroy();
    }

    public override void Draw(IRenderer r)
    {
        var at = DrawPosition;
        r.Draw(Art[_sprite], new Vector2(MathF.Round(at.X), MathF.Round(at.Y - 4)));
    }
}
