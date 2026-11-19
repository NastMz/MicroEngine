using System.Numerics;
using MicroEngine.Scenes;

namespace EcsModel.Simple.Step2;

// Step 2 on the scene world: status effects that come and go on live enemies. An effect is fields of the enemy's
// class (a counter that is zero when the effect is off) and code in the places it matters.

public sealed class Enemy(EntityId entity, int tag, Vector2 velocity, int health)
{
    public EntityId Entity => entity;
    public int Tag => tag;
    public Vector2 Velocity = velocity;
    public int Health = health;
    public int BurnLeft, BurnTick, FrozenLeft;
    public bool Shielded;
}

public sealed class Projectile(EntityId entity, Vector2 velocity, int damage, Element element)
{
    public EntityId Entity => entity;
    public Vector2 Velocity => velocity;
    public int Damage => damage;
    public Element Element => element;
    public int Life = Rules.ShotLife;
}

public sealed class Slice : ISlice
{
    private readonly SceneWorld _world = new();
    private long _step;
    private readonly List<Enemy> _sources = [], _reached = [];

    public int Kills { get; private set; }
    public int CratesBroken => 0;
    public int Shots => _world.All<Projectile>().Count();

    public void Step(IReadOnlyList<Command> due)
    {
        _world.BeginStep(_step++, Rules.Dt);
        foreach (var command in due) Apply(command);
        MoveEnemies();
        MoveProjectiles();
        Effects();
        Deaths();
        _world.EndStep();
    }

    private void Apply(Command command)
    {
        switch (command)
        {
            case SpawnEnemy spawn:
            {
                var entity = _world.Create();
                _world.Local(entity).Position2D = spawn.At;
                _world.Attach(entity, new Enemy(entity, spawn.Tag, spawn.Velocity, spawn.Health));
                break;
            }
            case Shoot shoot:
            {
                var entity = _world.Create();
                _world.Local(entity).Position2D = shoot.From;
                _world.Attach(entity, new Projectile(entity, shoot.Velocity, shoot.Damage, shoot.Element));
                break;
            }
            case Shield shield:
                foreach (var enemy in _world.All<Enemy>())
                    if (enemy.Tag == shield.Tag) enemy.Shielded = true;
                break;
        }
    }

    private void MoveEnemies()
    {
        foreach (var enemy in _world.All<Enemy>())
        {
            if (enemy.FrozenLeft > 0) continue;
            ref var local = ref _world.Local(enemy.Entity);
            var at = local.Position2D + enemy.Velocity * Rules.Dt;
            if (at.X < 0 || at.X > Rules.Width) enemy.Velocity.X = -enemy.Velocity.X;
            if (at.Y < 0 || at.Y > Rules.Height) enemy.Velocity.Y = -enemy.Velocity.Y;
            local.Position2D = at;
        }
    }

    private void MoveProjectiles()
    {
        // Attachments are walked in the order they were attached, so projectiles hit in the order they were fired.
        foreach (var shot in _world.All<Projectile>())
        {
            ref var local = ref _world.Local(shot.Entity);
            local.Position2D += shot.Velocity * Rules.Dt;
            if (Nearest(local.Position2D) is { } target)
            {
                Hit(target, shot.Damage, shot.Element);
                _world.Destroy(shot.Entity);
            }
            else if (--shot.Life <= 0) _world.Destroy(shot.Entity);
        }
    }

    // The order of these checks is the rule: a shield first, then ice, then the hit and what it carries.
    private static void Hit(Enemy target, int damage, Element element)
    {
        if (target.Shielded)
        {
            target.Shielded = false;
            return;
        }
        if (target.FrozenLeft > 0)
        {
            target.FrozenLeft = 0;
            target.Health -= element == Element.Fire ? damage : damage * 2;   // fire thaws, anything else shatters
            return;
        }
        target.Health -= damage;
        if (element == Element.Fire) Ignite(target);
        if (element == Element.Ice)
        {
            if (target.BurnLeft > 0) target.BurnLeft = 0;
            else target.FrozenLeft = Rules.FrozenSteps;
        }
    }

    private static void Ignite(Enemy target)
    {
        if (target.BurnLeft == 0) target.BurnTick = Rules.BurnTick;
        target.BurnLeft = Rules.BurnSteps;
    }

    private void Effects()
    {
        _sources.Clear();
        _reached.Clear();
        foreach (var enemy in _world.All<Enemy>())
        {
            if (enemy.FrozenLeft > 0) enemy.FrozenLeft--;
            if (enemy.BurnLeft == 0) continue;
            if (--enemy.BurnTick == 0)
            {
                enemy.Health--;
                enemy.BurnTick = Rules.BurnTick;
                _sources.Add(enemy);
            }
            enemy.BurnLeft--;
        }
        // Fire reaches from every enemy that burned this step. Who is reached is decided before anyone is changed, so
        // the outcome does not depend on the order of the walk.
        foreach (var source in _sources)
        {
            var from = _world.Local(source.Entity).Position2D;
            foreach (var enemy in _world.All<Enemy>())
            {
                if (enemy == source || enemy.BurnLeft > 0 || _reached.Contains(enemy)) continue;
                if (Vector2.DistanceSquared(_world.Local(enemy.Entity).Position2D, from) < Rules.SpreadRadius * Rules.SpreadRadius) _reached.Add(enemy);
            }
        }
        foreach (var enemy in _reached)
        {
            if (enemy.FrozenLeft > 0) enemy.FrozenLeft = 0;
            else Ignite(enemy);
        }
    }

    private Enemy? Nearest(Vector2 at)
    {
        Enemy? nearest = null;
        float best = Rules.HitRadius * Rules.HitRadius;
        foreach (var enemy in _world.All<Enemy>())
        {
            float distance = Vector2.DistanceSquared(_world.Local(enemy.Entity).Position2D, at);
            if (distance < best || (distance == best && nearest is not null && enemy.Tag < nearest.Tag)) (nearest, best) = (enemy, distance);
        }
        return nearest;
    }

    private void Deaths()
    {
        foreach (var enemy in _world.All<Enemy>())
        {
            if (enemy.Health > 0) continue;
            _world.Destroy(enemy.Entity);
            Kills++;
        }
    }

    public List<Seen> Things()
    {
        var seen = new List<Seen>();
        foreach (var enemy in _world.All<Enemy>())
            seen.Add(new Seen(enemy.Tag, 'e', _world.Local(enemy.Entity).Position2D, enemy.Health, enemy.BurnLeft > 0, enemy.FrozenLeft > 0, enemy.Shielded));
        return seen;
    }

    public List<int> BurningUnshieldedNear(Vector2 center, float radius) => [];
}
