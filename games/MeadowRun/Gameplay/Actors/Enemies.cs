using System.Numerics;
using MeadowRun.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace MeadowRun.Gameplay.Actors;

internal enum DeathKind { None, Squash, Flip }

/// <summary>
/// Shared enemy behaviour: stomp and knock-out deaths, facing, and sprite anchoring.
/// Art faces left, so sprites flip when the enemy faces right.
/// </summary>
internal abstract class Enemy : Actor
{
    protected Enemy(Stage stage, Vector2 position) : base(stage, position) { }

    public int Facing { get; set; } = -1;
    public DeathKind Death { get; private set; }
    public float DeathTime { get; private set; }
    public bool Dead => Death != DeathKind.None;
    public virtual bool Stompable => true;
    /// <summary>Walkers turn around when they bump into each other.</summary>
    public virtual bool Walker => false;
    public virtual int Points => 100;
    /// <summary>Seeds and bumps knock the enemy out; some enemies (the boss) override this.</summary>
    public virtual bool Knockable => true;

    public virtual void Stomped()
    {
        Death = DeathKind.Squash;
        Velocity = Vector2.Zero;
    }

    public virtual void Knocked(int direction)
    {
        Death = DeathKind.Flip;
        Velocity = new Vector2(direction * 60, -230);
    }

    protected sealed override void Tick(float dt)
    {
        if (Death == DeathKind.Squash)
        {
            DeathTime += dt;
            if (DeathTime > 0.5f) Destroy();
            return;
        }
        if (Death == DeathKind.Flip)
        {
            DeathTime += dt;
            Velocity.Y += 900 * dt;
            Position += Velocity * dt;
            if (Position.Y > Stage.Level.PixelHeight + 40) Destroy();
            return;
        }
        Live(dt);
        if (Position.Y > Stage.Level.PixelHeight + 40) Destroy();
    }

    protected abstract void Live(float dt);

    /// <summary>Adds facing and how (and how long ago) the enemy was defeated.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(Facing);
        hash.Add((int)Death);
        hash.Add(DeathTime);
    }
    protected abstract Sprite Frame(Assets a, float time);

    public override void Render(IRenderer r, Assets a, float time)
    {
        var sprite = Frame(a, time);
        var pos = Snap(Feet);
        bool flipX = Facing > 0;
        if (Death == DeathKind.Flip)
        {
            r.Draw(sprite.Texture, sprite.Source, pos - new Vector2(sprite.Origin.X, sprite.Source.Height), Color.White, new DrawOptions(FlipX: flipX, FlipY: true));
            return;
        }
        r.Draw(sprite, pos, null, flipX);
    }

    /// <summary>Simple patrol: walk, turn at walls (and at ledges when <paramref name="ledgeAware"/>).</summary>
    protected void Patrol(float dt, float speed, bool ledgeAware)
    {
        if (ledgeAware && Velocity.Y == 0 && LedgeAhead(Facing)) Facing = -Facing;
        Velocity.X = Facing * speed;
        var hit = MoveBody(dt);
        if (hit.HitWall) Facing = -Facing;
    }
}

/// <summary>Bumblet: the basic stompable walker. Walks off ledges.</summary>
internal sealed class Bumblet : Enemy
{
    public Bumblet(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft) => Hitbox = new RectF(2, 4, 12, 12);
    public override bool Walker => true;
    protected override void Live(float dt) => Patrol(dt, 30, ledgeAware: false);
    protected override Sprite Frame(Assets a, float time) => a.Frames("bumblet")[Death == DeathKind.Squash ? 2 : (int)(Age * 6) % 2];
}

/// <summary>Thornback: a spiny walker that cannot be stomped and keeps to its platform.</summary>
internal sealed class Thornback : Enemy
{
    public Thornback(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft) => Hitbox = new RectF(2, 5, 13, 11);
    public override bool Stompable => false;
    public override bool Walker => true;
    public override int Points => 200;
    protected override void Live(float dt) => Patrol(dt, 26, ledgeAware: true);
    protected override Sprite Frame(Assets a, float time) => a.Frames("thornback")[(int)(Age * 5) % 2];
}

/// <summary>Hopkin: sits, crouches as a tell, then leaps toward Pip.</summary>
internal sealed class Hopkin : Enemy
{
    private float _timer = 1f;
    private bool _crouching;
    public Hopkin(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft) => Hitbox = new RectF(2, 5, 12, 11);
    public override int Points => 200;
    private bool Airborne => Velocity.Y != 0;

    /// <summary>Adds the leap timer and the crouch tell.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_timer);
        hash.Add(_crouching);
    }

    protected override void Live(float dt)
    {
        var hit = MoveBody(dt, 800);
        if (hit.HitWall) Velocity.X = -Velocity.X * 0.5f;
        if (!hit.HitFloor && Airborne) return;
        Velocity.X = 0;
        _timer -= dt;
        Facing = Stage.Player.Center.X > Center.X ? 1 : -1;
        if (!_crouching && _timer <= 0.35f) _crouching = true;
        if (_timer <= 0)
        {
            _crouching = false;
            _timer = 1.3f;
            Velocity = new Vector2(Facing * 62, -290);
            Stage.Sfx("hop", 1f, 0.5f, Center);
        }
    }

    protected override Sprite Frame(Assets a, float time) => a.Frames("hopkin")[Airborne ? 2 : _crouching ? 1 : 0];
}

/// <summary>Mothwing: drifts on a sine path; stomping it sends it tumbling.</summary>
internal sealed class Mothwing : Enemy
{
    private readonly Vector2 _home;
    public Mothwing(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft)
    {
        Hitbox = new RectF(3, 3, 12, 11);
        _home = tileTopLeft;
    }

    public override int Points => 200;

    protected override void Live(float dt)
    {
        float t = Age;
        var next = _home + new Vector2(MathF.Sin(t * 0.7f) * 40, MathF.Sin(t * 2.4f) * 14);
        Facing = next.X > Position.X ? 1 : -1;
        Position = next;
    }

    public override void Stomped() => Knocked(Facing);
    protected override Sprite Frame(Assets a, float time) => a.Frames("mothwing")[(int)(time * 10) % 2];

    public override void Render(IRenderer r, Assets a, float time)
    {
        var sprite = Frame(a, time);
        var pos = Snap(Position + new Vector2(9, 8));
        if (Death == DeathKind.Flip)
            r.Draw(sprite.Texture, sprite.Source, pos - sprite.Origin, Color.White, new DrawOptions(FlipX: Facing > 0, FlipY: true));
        else r.Draw(sprite, pos, null, Facing > 0);
    }
}

/// <summary>Spitpod: a potted plant that swells (tell) and spits a thorn at Pip when he is near.</summary>
internal sealed class Spitpod : Enemy
{
    private float _timer = 1.2f;
    private int _pose;
    public Spitpod(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft) => Hitbox = new RectF(3, 0, 10, 16);
    public override int Points => 200;

    /// <summary>Adds the spit timer and the pose (the swell tell gates the next shot).</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(_timer);
        hash.Add(_pose);
    }

    protected override void Live(float dt)
    {
        var toPlayer = Stage.Player.Center - Center;
        Facing = toPlayer.X > 0 ? 1 : -1;
        bool inRange = MathF.Abs(toPlayer.X) < 170 && MathF.Abs(toPlayer.Y) < 90;
        _timer -= dt;
        if (_pose == 2 && _timer < 1.2f) _pose = 0;
        if (!inRange) { if (_pose == 1) _pose = 0; _timer = MathF.Max(_timer, 0.6f); return; }
        if (_pose == 0 && _timer <= 0.55f) { _pose = 1; Stage.Sfx("swell", 1, 0.35f, Center); }
        if (_timer <= 0)
        {
            _pose = 2;
            _timer = 2.1f;
            var mouth = Center + new Vector2(Facing * 7, -4);
            Stage.Spawn(new ThornShot(Stage, mouth, new Vector2(Facing * 105, 0)));
            Stage.Sfx("spit", 1, 0.6f, Center);
        }
    }

    protected override Sprite Frame(Assets a, float time) => a.Frames("spitpod")[_pose];
}

/// <summary>An enemy projectile that flies straight until it hits a wall.</summary>
internal sealed class ThornShot : Enemy
{
    public ThornShot(Stage stage, Vector2 center, Vector2 velocity) : base(stage, center - new Vector2(4, 4))
    {
        Hitbox = new RectF(1, 1, 6, 6);
        Velocity = velocity;
    }

    public override bool AlwaysActive => true;
    public override bool Stompable => false;
    public override int Points => 50;

    protected override void Live(float dt)
    {
        Position += Velocity * dt;
        if (MicroEngine.World.TileMotion.IsSolidAt(Center, Stage.Level.Grid) || Age > 4)
        {
            Stage.Puff(Center, Pal.Lilac);
            Destroy();
        }
    }

    protected override Sprite Frame(Assets a, float time) => a["thorn"];
    public override void Render(IRenderer r, Assets a, float time) => r.Draw(a["thorn"], Snap(Center));
}
