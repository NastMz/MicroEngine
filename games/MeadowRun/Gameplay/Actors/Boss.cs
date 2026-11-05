using System.Numerics;
using MeadowRun.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace MeadowRun.Gameplay.Actors;

/// <summary>Pip's thrown ember seed: arcs, bounces along the floor and knocks out enemies.</summary>
internal sealed class SeedShot : Actor
{
    private int _bounces;
    public SeedShot(Stage stage, Vector2 center, int facing) : base(stage, center - new Vector2(4, 4))
    {
        Hitbox = new RectF(1, 1, 6, 6);
        Velocity = new Vector2(facing * 210, 50);
    }

    public override bool AlwaysActive => true;

    /// <summary>Adds how many times the seed has bounced.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_bounces);
    }

    protected override void Tick(float dt)
    {
        var hit = MoveBody(dt, 900, 300);
        if (hit.HitFloor)
        {
            Velocity.Y = -175;
            if (++_bounces > 4) Pop();
        }
        if (hit.HitWall || hit.HitCeiling || Age > 2.4f || !Stage.IsNearView(Bounds, 8)) Pop();
    }

    public void Pop()
    {
        if (!Alive) return;
        Stage.Puff(Center, Pal.Flame);
        Destroy();
    }

    public override void Render(IRenderer r, Assets a, float time)
    {
        r.SetBlendMode(BlendMode.Additive);
        r.Draw(a["glow_small"], Snap(DrawCenter), Pal.Flame.WithAlpha(0.5f));
        r.SetBlendMode(BlendMode.Alpha);
        r.Draw(a.Frames("seedshot")[(int)(Age * 16) % 4], Snap(DrawCenter));
    }
}

/// <summary>A boulder shaken loose from the ceiling during the boss fight; its shadow is the tell.</summary>
internal sealed class Rock : Enemy
{
    private readonly float _delay;
    private readonly float _groundY;
    public Rock(Stage stage, float x, float ceilingY, float groundY, float delay) : base(stage, new Vector2(x - 7, ceilingY))
    {
        Hitbox = new RectF(1, 1, 12, 12);
        _delay = delay;
        _groundY = groundY;
    }

    public override bool AlwaysActive => true;
    public override bool Stompable => false;
    public override bool Knockable => false;
    public bool Falling => Age >= _delay;

    protected override void Live(float dt)
    {
        if (!Falling) return;
        Velocity.Y = MathF.Min(Velocity.Y + 700 * dt, 320);
        Position += Velocity * dt;
        if (Bounds.Bottom >= _groundY)
        {
            Stage.Burst(Center, Pal.Dusk, 10);
            Stage.Sfx("crumble", 1, 0.6f, Center);
            Stage.Shake(0.15f);
            Destroy();
        }
    }

    protected override Sprite Frame(Assets a, float time) => a["rock"];

    public override void Render(IRenderer r, Assets a, float time)
    {
        // Shadow on the floor grows as the rock approaches: the warning players learn to read.
        float t = Math.Clamp((Age + 0.6f) / (_delay + 0.9f), 0, 1);
        float w = 4 + 8 * t;
        r.FillRect(new RectF(MathF.Round(DrawCenter.X - w), _groundY - 2, MathF.Round(w * 2), 2), Pal.Ink.WithAlpha(0.5f));
        if (Falling) r.Draw(a["rock"], Snap(DrawCenter));
    }
}

/// <summary>
/// Grumbold, the Cinder Mole King. Three phases, each adding an attack:
/// charge (all phases), leap-and-slam with falling rocks (phase 2+), burrow ambush (phase 3).
/// Every attack is telegraphed and ends in a dazed window where his helmet can be stomped.
/// </summary>
internal sealed class Grumbold : Enemy
{
    public const int MaxHealth = 9;
    public const int StompDamage = 3;
    private enum Mode { Asleep, Intro, Idle, Telegraph, Charge, Leap, Recover, Burrow, Stalk, Erupt, Hurt, Defeated }

    private Mode _mode = Mode.Asleep;
    private float _timer;
    private int _attack;
    private float _flash;
    private float _moundX;
    private bool _fleeing;

    public int Health { get; private set; } = MaxHealth;
    public int Phase => Health > 6 ? 1 : Health > 3 ? 2 : 3;
    public bool Vulnerable => _mode == Mode.Recover;
    public bool Hidden => _mode is Mode.Burrow or Mode.Stalk;
    public bool Defeated => _mode == Mode.Defeated;
    public RectF Arena { get; set; }

    public Grumbold(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft + new Vector2(-10, -14)) => Hitbox = new RectF(4, 6, 36, 24);

    /// <summary>
    /// Adds the attack mode and its timer, the attack rotation, health, the burrow mound, the flight after defeat and
    /// the arena. Leaves out the hit flash (only the sprite reads it).
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add((int)_mode);
        hash.Add(_timer);
        hash.Add(_attack);
        hash.Add(Health);
        hash.Add(_moundX);
        hash.Add(_fleeing);
        hash.Add(Arena.X);
        hash.Add(Arena.Y);
        hash.Add(Arena.Width);
        hash.Add(Arena.Height);
    }
    public override bool AlwaysActive => true;
    public override bool Stompable => Vulnerable;
    public override bool Knockable => false;
    public override int Points => 5000;
    public bool Harmful => _mode is not (Mode.Asleep or Mode.Intro or Mode.Defeated or Mode.Burrow or Mode.Stalk) && !Vulnerable;
    public bool Active => _mode != Mode.Asleep;

    /// <summary>Wakes the boss once Pip enters the arena.</summary>
    public void Wake()
    {
        if (_mode != Mode.Asleep) return;
        _mode = Mode.Intro;
        _timer = 1.6f;
        Stage.Sfx("roar", 1, 1, Center);
        Stage.Shake(0.5f);
    }

    public override void Stomped()
    {
        if (!Vulnerable) return;
        Damage(StompDamage);
    }

    /// <summary>Ember seeds chip away one point; they cannot hurt him while burrowed.</summary>
    public void SeedHit()
    {
        if (Hidden || _mode is Mode.Asleep or Mode.Intro or Mode.Defeated or Mode.Hurt) return;
        _flash = 0.12f;
        Damage(1);
    }

    private void Damage(int amount)
    {
        int before = Phase;
        Health = Math.Max(0, Health - amount);
        _flash = 0.3f;
        Stage.Shake(0.35f);
        Stage.Freeze(0.08f);
        Stage.Burst(Center, Pal.Gold, 12);
        if (Health == 0)
        {
            _mode = Mode.Defeated;
            _timer = 0;
            Velocity = Vector2.Zero;
            Stage.Sfx("bossdown", 1, 1, Center);
            Stage.BossDefeated(this);
            return;
        }
        Stage.Sfx(amount >= StompDamage ? "bosshit" : "chip", 1, 1, Center);
        if (amount >= StompDamage || Phase != before)
        {
            _mode = Mode.Hurt;
            _timer = 0.8f;
            Velocity = new Vector2(Stage.Player.Center.X < Center.X ? 90 : -90, -160);
        }
    }

    protected override void Live(float dt)
    {
        _flash = MathF.Max(0, _flash - dt);
        _timer -= dt;
        float ground = Arena.Bottom;
        switch (_mode)
        {
            case Mode.Asleep:
                MoveBody(dt);
                break;
            case Mode.Intro:
                MoveBody(dt);
                if (_timer <= 0) Next(Mode.Idle, 0.6f);
                break;
            case Mode.Idle:
                Velocity.X = 0;
                MoveBody(dt);
                FacePlayer();
                if (_timer <= 0) ChooseAttack();
                break;
            case Mode.Telegraph:
                MoveBody(dt);
                if ((int)(_timer * 20) % 3 == 0) Stage.Dust(Feet + new Vector2(-Facing * 14, 0), -Facing);
                if (_timer <= 0) { Next(Mode.Charge, 4f); Stage.Sfx("charge", 1, 1, Center); }
                break;
            case Mode.Charge:
            {
                Velocity.X = Facing * (150 + Phase * 35);
                var hit = MoveBody(dt);
                if ((int)(Age * 20) % 2 == 0) Stage.Dust(Feet, -Facing);
                if (hit.HitWall || _timer <= 0)
                {
                    Stage.Shake(0.6f);
                    Stage.Sfx("thud", 0.7f, 1, Center);
                    Stage.Burst(Center + new Vector2(Facing * 18, 0), Pal.Sand, 14);
                    if (Phase >= 2) DropRocks(2 + Phase);
                    Velocity.X = -Facing * 60;
                    Next(Mode.Recover, Phase == 3 ? 1.2f : 1.7f);
                }
                break;
            }
            case Mode.Leap:
            {
                var hit = MoveBody(dt, 700);
                if (hit.HitWall) Velocity.X = 0;
                if (hit.HitFloor && _timer < 0.9f)
                {
                    Stage.Shake(0.7f);
                    Stage.Sfx("slam", 1, 1, Center);
                    Stage.Shockwave(Feet);
                    DropRocks(3 + Phase);
                    Velocity = Vector2.Zero;
                    Next(Mode.Recover, 1.3f);
                }
                break;
            }
            case Mode.Recover:
                Velocity.X = MathX.Approach(Velocity.X, 0, 200 * dt);
                MoveBody(dt);
                if (_timer <= 0) Next(Mode.Idle, Phase == 3 ? 0.35f : 0.7f);
                break;
            case Mode.Burrow:
                if (_timer <= 0)
                {
                    _moundX = Center.X;
                    Next(Mode.Stalk, 2.0f);
                }
                break;
            case Mode.Stalk:
                // The mound chases Pip, then freezes and trembles before the eruption.
                if (_timer > 0.55f) _moundX = MathX.Approach(_moundX, Stage.Player.Center.X, 120 * dt);
                _moundX = Math.Clamp(_moundX, Arena.Left + 24, Arena.Right - 24);
                if ((int)(_timer * 30) % 4 == 0) Stage.Dust(new Vector2(_moundX, ground), 0);
                if (_timer <= 0)
                {
                    Position = new Vector2(_moundX - Hitbox.Center.X, ground - Hitbox.Bottom);
                    Teleport();
                    Velocity = new Vector2(0, -430);
                    Stage.Sfx("erupt", 1, 1, Center);
                    Stage.Burst(new Vector2(_moundX, ground), Pal.Sand, 18);
                    Stage.Shake(0.5f);
                    Next(Mode.Erupt, 3f);
                }
                break;
            case Mode.Erupt:
            {
                var hit = MoveBody(dt, 800);
                if (hit.HitFloor && Velocity.Y >= 0 && _timer < 2.8f)
                {
                    Stage.Shake(0.4f);
                    Next(Mode.Recover, 1.1f);
                }
                break;
            }
            case Mode.Hurt:
                MoveBody(dt);
                if (_timer <= 0) Next(Mode.Idle, 0.4f);
                break;
            case Mode.Defeated:
                _timer += dt;
                if ((int)(_timer * 12) % 3 == 0) Stage.Burst(Center + new Vector2(MathF.Sin(_timer * 13) * 14, MathF.Cos(_timer * 9) * 10), Pal.Flame, 4);
                if (_timer > 2.2f)
                {
                    // Launched off the arena in a defeated tumble (ignores tiles).
                    if (!_fleeing) { _fleeing = true; Velocity = new Vector2(-Facing * 50, -320); Stage.Sfx("leap", 0.7f, 1, Center); }
                    Velocity.Y += 600 * dt;
                    Position += Velocity * dt;
                }
                else MoveBody(dt);
                if (Position.Y > Stage.Level.PixelHeight + 60) Destroy();
                break;
        }
    }

    private void Next(Mode mode, float time)
    {
        _mode = mode;
        _timer = time;
    }

    private void FacePlayer() => Facing = Stage.Player.Center.X > Center.X ? 1 : -1;

    private void ChooseAttack()
    {
        FacePlayer();
        int attacks = Phase;
        int choice = _attack++ % attacks;
        if (choice == 0 || attacks == 1)
        {
            Next(Mode.Telegraph, Phase == 3 ? 0.5f : 0.8f);
            Stage.Sfx("paw", 1, 0.8f, Center);
        }
        else if (choice == 1)
        {
            float dx = Stage.Player.Center.X - Center.X;
            Velocity = new Vector2(Math.Clamp(dx * 0.9f, -150, 150), -380);
            Stage.Sfx("leap", 1, 1, Center);
            Next(Mode.Leap, 1.6f);
        }
        else
        {
            Stage.Sfx("burrow", 1, 1, Center);
            Stage.Burst(Feet, Pal.Sand, 16);
            Next(Mode.Burrow, 0.5f);
        }
    }

    private void DropRocks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Arena.Left + 24 + (Arena.Width - 48) * ((i + 0.5f) / count) + MathF.Sin(Age * 7 + i * 3) * 10;
            Stage.Spawn(new Rock(Stage, x, Arena.Top + 8, Arena.Bottom, 0.5f + i * 0.18f));
        }
    }

    protected override Sprite Frame(Assets a, float time)
    {
        int f = _mode switch
        {
            Mode.Telegraph => 2,
            Mode.Charge => 3 + (int)(Age * 10) % 2,
            Mode.Recover => 5,
            Mode.Hurt => 6,
            Mode.Leap or Mode.Erupt => 3,
            Mode.Defeated => 6,
            Mode.Intro => (int)(Age * 8) % 2 == 0 ? 2 : 7,
            _ => (int)(Age * 2) % 2,
        };
        return a.Frames(_flash > 0 && (int)(time * 30) % 2 == 0 ? "grumbold_flash" : "grumbold")[f];
    }

    public override void Render(IRenderer r, Assets a, float time)
    {
        if (_mode == Mode.Burrow)
        {
            float sink = 1 - Math.Clamp(_timer / 0.5f, 0, 1);
            var s = Frame(a, time);
            int visible = (int)(s.Source.Height * (1 - sink));
            if (visible > 0)
                r.Draw(s.Texture, s.Source with { Height = visible }, Snap(DrawFeet) - new Vector2(Facing > 0 ? s.Source.Width - s.Origin.X : s.Origin.X, s.Origin.Y - (s.Source.Height - visible)) , Color.White, new DrawOptions(FlipX: Facing > 0));
            return;
        }
        if (_mode == Mode.Stalk)
        {
            float shake = _timer < 0.55f ? MathF.Round(MathF.Sin(time * 80) * 1.5f) : 0;
            r.Draw(a.Frames("mound")[(int)(time * 8) % 2], Snap(new Vector2(_moundX + shake, Arena.Bottom)));
            return;
        }
        if (_mode == Mode.Recover)
        {
            // Dazed stars circle the helmet: the "hit me now" signal.
            for (int i = 0; i < 3; i++)
            {
                var c = DrawFeet + new Vector2(-Facing * 8, -42) + MathX.FromAngle(time * 5 + i * MathF.Tau / 3, 9) * new Vector2(1, 0.4f);
                r.FillRect(new RectF(MathF.Round(c.X), MathF.Round(c.Y), 2, 2), Pal.Butter);
            }
        }
        base.Render(r, a, time);
    }
}

/// <summary>The Sunstone the boss leaves behind; touching it ends the game.</summary>
internal sealed class Sunstone : Actor
{
    private readonly float _restY;
    public Sunstone(Stage stage, Vector2 center, float restY) : base(stage, center - new Vector2(10, 10))
    {
        Hitbox = new RectF(2, 2, 16, 16);
        _restY = restY;
    }

    public override bool AlwaysActive => true;

    protected override void Tick(float dt)
    {
        float targetY = _restY + MathX.Wave(Age, 0.5f, 3);
        Position = Position with { Y = MathX.Damp(Position.Y, targetY, 3, dt) };
        if ((int)(Age * 10) % 3 == 0) Stage.Sparkle(Center + MathX.FromAngle(Age * 3, 12), Pal.Gold);
    }

    public override void Render(IRenderer r, Assets a, float time)
    {
        r.SetBlendMode(BlendMode.Additive);
        r.Draw(a["glow"], Snap(DrawCenter), Pal.Gold.WithAlpha(0.7f));
        r.SetBlendMode(BlendMode.Alpha);
        r.Draw(a.Frames("sunstone")[(int)(time * 6) % 4], Snap(DrawCenter));
    }
}
