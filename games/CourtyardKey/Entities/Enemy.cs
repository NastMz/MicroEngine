using System.Numerics;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace CourtyardKey.Entities;

/// <summary>
/// Shared enemy behaviour: health with brief invulnerability, knockback, boomerang stun (with a
/// little star orbit), white hit flash, a death puff and a loot roll.
/// </summary>
public abstract class Enemy : Actor, IHittable
{
    private Vector2 _knock;
    private float _knockTime;

    protected Enemy(PlayScene world, Vector2 at, int health) : base(world)
    {
        Position = at;
        Health = new Health(health);
        Hitbox = new RectF(-6, -10, 12, 10);
    }

    public Health Health { get; }
    public float Stun { get; protected set; }
    public bool Stunned => Stun > 0;
    protected float Flash { get; private set; }
    protected float Time { get; private set; }
    public virtual int ContactDamage => 1;
    public virtual bool Flies => false;
    /// <summary>Heavy enemies are not pushed back by the sword.</summary>
    protected virtual float KnockbackScale => 1f;
    /// <summary>Whether a boomerang strike also deals damage (fragile fliers die to it).</summary>
    protected virtual bool FragileToBoomerang => false;
    /// <summary>Counts toward clearing ambush rooms.</summary>
    public virtual bool CountsForClear => true;
    public virtual RectF Hurtbox => Bounds;
    /// <summary>Whether touching the hero hurts right now.</summary>
    public virtual bool Harmful => !Stunned;

    /// <summary>
    /// State hash: health, stun, hit flash, age and knockback, then the enemy's own AI state (<see cref="HashAiState"/>).
    /// </summary>
    protected sealed override void HashOwnState(StateHasher hash)
    {
        hash.Add(Health);
        hash.Add(Stun);
        hash.Add(Flash);
        hash.Add(Time);
        hash.Add(_knock);
        hash.Add(_knockTime);
        HashAiState(hash);
    }

    /// <summary>Adds the enemy's AI state (timers, velocities, facing).</summary>
    protected abstract void HashAiState(StateHasher hash);

    public override void Update(float dt)
    {
        Time += dt;
        Health.Update(dt);
        Flash = MathF.Max(0, Flash - dt);
        if (_knockTime > 0)
        {
            _knockTime -= dt;
            Move(_knock * dt, Flies);
            return;
        }
        if (Stun > 0) { Stun = MathF.Max(0, Stun - dt); return; }
        Think(dt);
    }

    /// <summary>AI step when free to act (not stunned or reeling).</summary>
    protected abstract void Think(float dt);

    /// <summary>True when a sword strike from <paramref name="hit"/> is deflected (shields, armour).</summary>
    protected virtual bool Blocks(in Hit hit) => false;

    public virtual HitOutcome OnHit(in Hit hit)
    {
        if (hit.Kind == HitKind.Boomerang)
        {
            Stun = Combat.BoomerangStun;
            World.Audio.Play("hit", 0.7f, 1.3f);
            if (!FragileToBoomerang) return HitOutcome.Stunned;
        }
        else if (Blocks(hit))
        {
            World.OnShieldBlock(this, hit);
            Knock(hit.From, 0.5f);
            return HitOutcome.Blocked;
        }
        // Fragile fliers lose a point to the boomerang even though it carries no blade damage.
        int damage = hit.Kind == HitKind.Boomerang ? Math.Max(1, hit.Damage) : hit.Damage;
        if (!Health.TryDamage(damage, Combat.EnemyInvulnerability)) return HitOutcome.Ignored;
        Flash = 0.18f;
        Knock(hit.From, 1f);
        World.OnEnemyDamaged(this);
        if (Health.IsDead)
        {
            Die();
            return HitOutcome.Destroyed;
        }
        return HitOutcome.Damaged;
    }

    protected void Knock(Vector2 from, float strength)
    {
        if (KnockbackScale <= 0) return;
        _knock = Combat.KnockbackDirection(from, Center) * Combat.EnemyKnockbackSpeed * strength * KnockbackScale;
        _knockTime = Combat.KnockbackTime;
    }

    protected virtual void Die()
    {
        World.OnEnemyKilled(this);
        Destroy();
    }

    /// <summary>The flash twin of a sprite while recently hit.</summary>
    protected Sprite Flashing(string name) => Flash > 0 && (int)(Flash * 30) % 2 == 0 ? Art.Flash(name) : Art[name];

    protected void DrawStunStars(IRenderer r, float height)
    {
        if (!Stunned) return;
        for (int i = 0; i < 3; i++)
        {
            float a = Time * 6 + i * MathF.Tau / 3;
            var p = DrawPosition + new Vector2(MathF.Cos(a) * 7, -height + MathF.Sin(a) * 2.5f);
            World.Font.Draw(r, "★", p - new Vector2(3, 4), Visuals.Pal.Gold[3]);
        }
    }

    protected Vector2 ToHero => World.Hero.Center - Center;
}
