using System.Numerics;
using MicroEngine.Scenes;

namespace EcsModel.Simple.Step1;

// Step 1 on the scene world: enemies that move and projectiles that hurt them. One class per kind of object,
// attached to an entity; the entity's transform is its place.

public sealed class Enemy(EntityId entity, int tag, Vector2 velocity, int health)
{
    public EntityId Entity => entity;
    public int Tag => tag;
    public Vector2 Velocity = velocity;
    public int Health = health;
}

public sealed class Projectile(EntityId entity, Vector2 velocity, int damage)
{
    public EntityId Entity => entity;
    public Vector2 Velocity => velocity;
    public int Damage => damage;
    public int Life = Rules.ShotLife;
}

public sealed class Slice : ISlice
{
    private readonly SceneWorld _world = new();
    private long _step;

    public int Kills { get; private set; }
    public int CratesBroken => 0;
    public int Shots => _world.All<Projectile>().Count();

    public void Step(IReadOnlyList<Command> due)
    {
        _world.BeginStep(_step++, Rules.Dt);
        foreach (var command in due) Apply(command);
        MoveEnemies();
        MoveProjectiles();
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
                _world.Attach(entity, new Projectile(entity, shoot.Velocity, shoot.Damage));
                break;
            }
        }
    }

    private void MoveEnemies()
    {
        foreach (var enemy in _world.All<Enemy>())
        {
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
                target.Health -= shot.Damage;
                _world.Destroy(shot.Entity);
            }
            else if (--shot.Life <= 0) _world.Destroy(shot.Entity);
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
            seen.Add(new Seen(enemy.Tag, 'e', _world.Local(enemy.Entity).Position2D, enemy.Health, false, false, false));
        return seen;
    }

    public List<int> BurningUnshieldedNear(Vector2 center, float radius) => [];
}
