using System.Numerics;
using NeonRift.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace NeonRift.Gameplay;

/// <summary>A player bullet. Piercing shots keep flying and remember whom they already hit.</summary>
public sealed class PlayerShot : Actor
{
    private readonly HashSet<Enemy> _hit = [];
    public WeaponSpec Weapon { get; }
    public float Life { get; set; } = 1.4f;
    private protected override void AttachTo(IWorld world, EntityId id) => world.Attach<PlayerShot>(id, this);

    public PlayerShot(WeaponSpec weapon, Vector2 at, Vector2 velocity)
    {
        Weapon = weapon;
        Position = at;
        Velocity = velocity;
        float size = weapon.Kind switch { WeaponKind.Lance => 10, WeaponKind.Spread => 6, _ => 5 };
        Hitbox = new RectF(-size / 2, -size / 2, size, size);
    }

    /// <summary>Adds the actor's state, then the weapon, the lifetime and how many enemies a piercing shot already hit.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add((int)Weapon.Kind);
        hash.Add(Life);
        hash.Add(_hit.Count);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        Position += Velocity * dt;
        if (Age > Life || !Stage.Camera.View.Inflate(24).Contains(Position)) { Destroy(); return; }
        if (!Weapon.Piercing && TileMotion.IsSolidAt(Position, Stage.Grid))
        {
            Stage.Spark(Position);
            Destroy();
            return;
        }
        // Copied first: a hit can spawn or destroy enemies. The stage's buffer is reused instead of a new list per shot.
        var targets = Stage.HitBuffer;
        targets.Clear();
        var reach = Bounds.Inflate(Weapon.Kind == WeaponKind.Lance ? 6 : 0);
        foreach (var e in Stage.Enemies) if (e.Bounds.Intersects(reach)) targets.Add(e);
        foreach (var enemy in targets)
        {
            if (_hit.Contains(enemy)) continue;
            if (!enemy.Hit(Weapon.Damage, Position)) continue;
            _hit.Add(enemy);
            Stage.Spark(Position);
            if (!Weapon.Piercing) { Destroy(); return; }
        }
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var frames = art.Frames(Weapon.Sprite);
        var frame = frames[(int)(Age * 18) % frames.Count];
        float angle = MathF.Atan2(Velocity.Y, Velocity.X);
        r.SetBlendMode(BlendMode.Additive);
        if (Weapon.Kind == WeaponKind.Spread)
            r.DrawPivoted(frame, Vector2.Round(DrawPosition), Age * 14f, new Vector2(0.45f));
        else if (Weapon.Kind == WeaponKind.Lance)
            r.DrawPivoted(frame, Vector2.Round(DrawPosition), angle, new Vector2(0.6f, 0.5f));
        else if (Weapon.Kind == WeaponKind.Rapid)
            r.DrawPivoted(frame, Vector2.Round(DrawPosition), angle, new Vector2(0.45f, 0.4f));
        else
            r.DrawPivoted(frame, Vector2.Round(DrawPosition), angle);
        r.SetBlendMode(BlendMode.Alpha);
    }
}

/// <summary>An enemy projectile: a glowing orb, or a heavier shell when <see cref="Heavy"/> is set.</summary>
public sealed class EnemyShot : Actor
{
    public bool Heavy { get; init; }
    public float Gravity { get; init; }
    private protected override void AttachTo(IWorld world, EntityId id) => world.Attach<EnemyShot>(id, this);

    public EnemyShot(Vector2 at, Vector2 velocity, bool heavy = false)
    {
        Position = at;
        Velocity = velocity;
        Heavy = heavy;
        float size = heavy ? 8 : 4;
        Hitbox = new RectF(-size / 2, -size / 2, size, size);
    }

    /// <summary>Adds the actor's state, then whether the shell is heavy and its gravity.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(Heavy);
        hash.Add(Gravity);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        Velocity +=new Vector2(0, Gravity * dt);
        Position += Velocity * dt;
        if (Age > 6 || !Stage.Camera.View.Inflate(40).Contains(Position)) Destroy();
        else if (Gravity > 0 && TileMotion.IsSolidAt(Position, Stage.Grid))
        {
            Stage.Explode(Position, false);
            Stage.Sound("explode", 0.4f, pan: Stage.Pan(Position.X));
            Destroy();
        }
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var frames = art.Frames("enemy_bullet");
        var frame = frames[(int)(Age * 12) % frames.Count];
        r.SetBlendMode(BlendMode.Additive);
        r.DrawPivoted(frame, Vector2.Round(DrawPosition), 0, new Vector2(Heavy ? 1.4f : 0.85f), Heavy ? Color.Hex("ff9a5a") : Color.Hex("ff6a8a"));
        r.SetBlendMode(BlendMode.Alpha);
    }
}
