using System.Numerics;
using NeonRift.Art;
using NeonRift.Content;
using MicroEngine;
using MicroEngine.Graphics;

namespace NeonRift.Gameplay;

/// <summary>Common boss plumbing: health bar data, entry flight, hit flash and a long death sequence.</summary>
public abstract class Boss : Enemy
{
    protected Boss(Vector2 home, int health)
    {
        Home = home;
        Health = MaxHealth = health;
        Points = 10000;
    }

    public Vector2 Home { get; }
    public int MaxHealth { get; }
    public abstract string Title { get; }
    public override bool Big => true;
    /// <summary>Seconds spent flying into position; the boss cannot be hurt and does not attack meanwhile.</summary>
    protected float EntryTime { get; init; } = 2.2f;
    public bool Entering => Age < EntryTime;
    public float HealthFraction => Math.Clamp(Health / (float)MaxHealth, 0, 1);

    public static Boss Create(BossKind kind, Vector2 home) => kind switch
    {
        BossKind.Gunship => new Gunship(home),
        BossKind.Interceptor => new Interceptor(home),
        _ => new Dreadnought(home),
    };

    public override bool Hit(int damage, Vector2 at)
    {
        if (Entering) return false;
        return base.Hit(damage, at);
    }

    protected override void Die()
    {
        Stage.AddScore(Points);
        Stage.BossDefeated(this);
        Destroy();
    }

    protected Vector2 Toward(Vector2 from, float speed) => Vector2.Normalize(Stage.Player.Position + new Vector2(0, -24) - from) * speed;
}

/// <summary>Stage 1 boss: an armoured hauler that hovers, sprays aimed volleys and carpet-bombs on strafing runs.</summary>
public sealed class Gunship : Boss
{
    private float _timer = 1.0f;
    private int _pattern;
    private float _strafe = -1;

    public Gunship(Vector2 home) : base(home, 55)
    {
        Position = home + new Vector2(300, -40);
        Hitbox = new RectF(-70, -30, 140, 60);
    }

    public override string Title => "HAULER GX-9";

    public override void Update(float dt)
    {
        base.Update(dt);
        if (Entering)
        {
            Position = Vector2.Lerp(Position, Home, 1 - MathF.Exp(-2.5f * dt));
            return;
        }
        if (_strafe >= 0)
        {
            // Strafing run: sweep left across the arena dropping bombs, then return.
            _strafe += dt;
            float t = _strafe / 4f;
            Position = new Vector2(Home.X - MathF.Sin(t * MathF.PI) * 250, Home.Y - 40 + MathF.Sin(t * MathF.PI * 2) * 10);
            _timer -= dt;
            if (_timer <= 0)
            {
                _timer = 0.32f;
                Stage.EnemyFire(Position + new Vector2(0, 30), new Vector2(0, 40), heavy: true, gravity: 260);
            }
            if (_strafe >= 4f) { _strafe = -1; _timer = 1.2f; }
            return;
        }
        Position = Home + new Vector2(MathF.Sin(Age * 0.9f) * 18, MathF.Sin(Age * 1.7f) * 12);
        _timer -= dt;
        if (_timer > 0) return;
        var cannon = Position + new Vector2(-70, 6);
        switch (_pattern++ % 4)
        {
            case 0:
            case 2:
                var dir = Toward(cannon, 165);
                for (int i = -1; i <= 1; i++) Stage.EnemyFire(cannon, Turret.Rotate(dir, i * 0.18f));
                Stage.Sound("enemy-shot", 0.6f);
                _timer = HealthFraction < 0.5f ? 0.8f : 1.3f;
                break;
            case 1:
                for (int i = 0; i < 2; i++) Stage.Spawn(new Drone(Position + new Vector2(-20, -30 + i * 50), i * 1.5f));
                _timer = 1.5f;
                break;
            default:
                _strafe = 0;
                _timer = 0.5f;
                Stage.Sound("cannon", 0.6f);
                break;
        }
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var at = Vector2.Round(Position);
        r.SetBlendMode(BlendMode.Additive);
        r.DrawPivoted(art.Frames("gunship_thrust")[(int)(Age * 14) % 3], at + new Vector2(84, -6), 0, flipX: true);
        r.SetBlendMode(BlendMode.Alpha);
        DrawFlashing(r, art.Frames("gunship")[(int)(Age * 10) % 3], at, true, Flash);
    }
}

/// <summary>Stage 2 boss: a fast interceptor that dives across the arena and fires widening fans.</summary>
public sealed class Interceptor : Boss
{
    private float _timer = 1.2f;
    private int _pattern;
    private float _dash = -1;
    private Vector2 _dashFrom, _dashTo;
    private int _facing = -1;

    public Interceptor(Vector2 home) : base(home, 85)
    {
        Position = home + new Vector2(320, -80);
        Hitbox = new RectF(-60, -40, 120, 75);
    }

    public override string Title => "RAPTOR INTERCEPTOR";

    public override void Update(float dt)
    {
        base.Update(dt);
        if (Entering)
        {
            Position = Vector2.Lerp(Position, Home, 1 - MathF.Exp(-2.5f * dt));
            return;
        }
        if (_dash >= 0)
        {
            _dash += dt;
            float t = Math.Clamp(_dash / 1.3f, 0, 1);
            // A low swooping arc: dips to the floor in the middle of the dash.
            Position = Vector2.Lerp(_dashFrom, _dashTo, Ease.InOutQuad(t)) + new Vector2(0, MathF.Sin(t * MathF.PI) * 95);
            if (t >= 1) { _dash = -1; _facing = -_facing; _timer = 0.8f; }
            return;
        }
        float hoverX = _facing < 0 ? Home.X : Home.X - 300;
        Position = Vector2.Lerp(Position, new Vector2(hoverX, Home.Y + MathF.Sin(Age * 2) * 16), 1 - MathF.Exp(-3 * dt));
        _timer -= dt;
        if (_timer > 0) return;
        var nose = Position + new Vector2(_facing * 70, 10);
        switch (_pattern++ % 3)
        {
            case 0:
                var dir = Toward(nose, 150);
                int spread = HealthFraction < 0.5f ? 3 : 2;
                for (int i = -spread; i <= spread; i++) Stage.EnemyFire(nose, Turret.Rotate(dir, i * 0.2f));
                Stage.Sound("enemy-shot", 0.6f);
                _timer = 1.1f;
                break;
            case 1:
                for (int i = 0; i < (HealthFraction < 0.5f ? 8 : 5); i++)
                    Stage.Schedule(i * 0.12f, () => { if (Alive) Stage.EnemyFire(Position + new Vector2(_facing * 70, 10), Toward(Position, 190)); });
                _timer = 1.6f;
                break;
            default:
                _dash = 0;
                _dashFrom = Position;
                _dashTo = new Vector2(_facing < 0 ? Home.X - 300 : Home.X, Home.Y);
                Stage.Sound("cannon", 0.7f);
                break;
        }
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var at = Vector2.Round(Position);
        bool flip = _facing > 0;
        r.SetBlendMode(BlendMode.Additive);
        r.DrawPivoted(art.Frames("interceptor_thrust")[(int)(Age * 14) % 3], at + new Vector2(flip ? -78 : 78, -2), 0, flipX: !flip);
        r.SetBlendMode(BlendMode.Alpha);
        DrawFlashing(r, art.Frames("interceptor")[(int)(Age * 10) % 3], at, !flip, Flash);
    }
}

/// <summary>
/// Final boss: the Leviathan dreadnought. Three hull turrets must fall before the bridge core opens;
/// the core then fights on with shell barrages, skiff launches and a sweeping cannon.
/// </summary>
public sealed class Dreadnought : Boss
{
    private readonly List<DreadTurret> _turrets = [];
    private float _timer = 1.5f;
    private int _pattern;

    public Dreadnought(Vector2 home) : base(home, 120)
    {
        Position = home + new Vector2(360, -30);
        Hitbox = new RectF(-110, -40, 170, 100);
        EntryTime = 3.2f;
    }

    public override string Title => "LEVIATHAN";
    public bool CoreOpen => _turrets.All(t => !t.Alive);
    public IReadOnlyList<DreadTurret> Turrets => _turrets;

    public override void Update(float dt)
    {
        base.Update(dt);
        if (_turrets.Count == 0)
            foreach (var offset in new[] { new Vector2(-150, -28), new Vector2(-128, 22), new Vector2(-70, 76) })
                _turrets.Add(Stage.Spawn(new DreadTurret(this, offset)));
        Armored = !CoreOpen;
        if (Entering)
        {
            Position = Vector2.Lerp(Position, Home, 1 - MathF.Exp(-1.8f * dt));
            return;
        }
        Position = Home + new Vector2(MathF.Sin(Age * 0.5f) * 14, MathF.Sin(Age * 1.1f) * (CoreOpen ? 26 : 10));
        _timer -= dt;
        if (_timer > 0) return;
        var mouth = Position + new Vector2(-150, 10);
        switch (_pattern++ % (CoreOpen ? 4 : 3))
        {
            case 0:
                // Shell barrage: heavy shells lobbed in arcs that burst on the floor.
                for (int i = 0; i < 4; i++)
                    Stage.EnemyFire(mouth, new Vector2(-90 - i * 45, -160 - i * 10), heavy: true, gravity: 300);
                Stage.Sound("cannon", 0.7f);
                _timer = 1.8f;
                break;
            case 1:
                for (int i = 0; i < 3; i++) Stage.Spawn(new Skiff(Position + new Vector2(-40, -70 + i * 28), i * 0.7f));
                _timer = 2f;
                break;
            case 2:
                // Sweeping cannon: a rotating stream from the bow.
                for (int i = 0; i < 10; i++)
                {
                    float angle = MathF.PI * (0.62f + i * 0.04f);
                    Stage.Schedule(i * 0.09f, () => { if (Alive) Stage.EnemyFire(Position + new Vector2(-150, 10), new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 175); });
                }
                _timer = 1.7f;
                break;
            default:
                var dir = Toward(mouth, 170);
                for (int i = -3; i <= 3; i++) Stage.EnemyFire(mouth, Turret.Rotate(dir, i * 0.16f));
                Stage.Sound("enemy-shot", 0.7f);
                _timer = 1.2f;
                break;
        }
    }

    protected override void Die()
    {
        foreach (var t in _turrets) t.Destroy();
        base.Die();
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var at = Vector2.Round(Position);
        r.SetBlendMode(BlendMode.Additive);
        r.DrawPivoted(art.Frames("dread_thrust_back")[(int)(Age * 14) % 3], at + new Vector2(170, -30), 0, flipX: true);
        r.SetBlendMode(BlendMode.Alpha);
        DrawFlashing(r, art.Frames("dreadnought")[(int)(Age * 8) % 3], at, true, Flash);
        if (CoreOpen)
        {
            // The exposed core pulses red so the new weak point is unmistakable.
            r.SetBlendMode(BlendMode.Additive);
            float pulse = 0.5f + 0.5f * MathF.Sin(Age * 8);
            r.DrawPivoted(art.Frames("hit5")[3], at + new Vector2(-20, -10), Age, new Vector2(1.8f), Color.Hex("ff3050").WithAlpha(0.4f + 0.4f * pulse));
            r.SetBlendMode(BlendMode.Alpha);
        }
    }
}

/// <summary>A gun pod bolted to the dreadnought hull. Follows its parent and fires aimed pairs.</summary>
public sealed class DreadTurret : Enemy
{
    private readonly Dreadnought _parent;
    private readonly Vector2 _offset;
    private float _timer;

    public DreadTurret(Dreadnought parent, Vector2 offset)
    {
        _parent = parent;
        _offset = offset;
        Position = parent.Position + offset;
        Hitbox = new RectF(-13, -13, 26, 26);
        Health = 16;
        Points = 1500;
        _timer = 1.5f + offset.Y * 0.01f;
    }

    public override bool Big => true;

    public override bool Hit(int damage, Vector2 at) => !_parent.Entering && base.Hit(damage, at);

    public override void Update(float dt)
    {
        base.Update(dt);
        if (!_parent.Alive) { Destroy(); return; }
        Position = _parent.Position + _offset;
        if (_parent.Entering) return;
        _timer -= dt;
        if (_timer <= 0)
        {
            _timer = 1.6f;
            var dir = Vector2.Normalize(Stage.Player.Position + new Vector2(0, -24) - Position) * 150;
            Stage.EnemyFire(Position, dir);
            Stage.Schedule(0.2f, () => { if (Alive) Stage.EnemyFire(Position, dir); });
        }
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var at = Vector2.Round(Position);
        r.FillCircle(at, 12, Color.Hex("1b1630"));
        var frames = art.Frames("turret");
        // Hung upside down: the half-turn already mirrors the left-facing frame, so flip when the player is left.
        DrawFlashing(r, frames[0], at + new Vector2(0, 10), Stage.Player.Position.X < Position.X, Flash, MathF.PI);
        r.SetBlendMode(BlendMode.Additive);
        r.FillCircle(at, 3 + MathF.Sin(Age * 6), Color.Hex("ff5070").WithAlpha(0.7f));
        r.SetBlendMode(BlendMode.Alpha);
    }
}
