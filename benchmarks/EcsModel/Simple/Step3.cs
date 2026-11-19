using System.Numerics;
using MicroEngine.Scenes;

namespace EcsModel.Simple.Step3;

// Step 3 on the scene world: a crate that burns and breaks but neither moves, freezes nor takes a shield. What the
// crate shares with an enemy moves to a base class; an attachment is found by the type it was attached under, so an
// enemy is attached twice (as a Thing, for what both do, and as an Enemy, for what only it does).

public abstract class Thing(EntityId entity, int tag, int health)
{
    public EntityId Entity => entity;
    public int Tag => tag;
    public int Health = health;
    public int BurnLeft, BurnTick;
}

public sealed class Enemy(EntityId entity, int tag, Vector2 velocity, int health) : Thing(entity, tag, health)
{
    public Vector2 Velocity = velocity;
    public int FrozenLeft;
    public bool Shielded;
}

public sealed class Crate(EntityId entity, int tag, int health) : Thing(entity, tag, health);

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
    private readonly List<Thing> _sources = [], _reached = [];

    public int Kills { get; private set; }
    public int CratesBroken { get; private set; }
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
                var enemy = _world.Attach(entity, new Enemy(entity, spawn.Tag, spawn.Velocity, spawn.Health));
                _world.Attach<Thing>(entity, enemy);
                break;
            }
            case SpawnCrate spawn:
            {
                var entity = _world.Create();
                _world.Local(entity).Position2D = spawn.At;
                _world.Attach<Thing>(entity, new Crate(entity, spawn.Tag, spawn.Health));
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
    private static void Hit(Thing target, int damage, Element element)
    {
        var enemy = target as Enemy;   // only an enemy has a shield or freezes
        if (enemy is { Shielded: true })
        {
            enemy.Shielded = false;
            return;
        }
        if (enemy is { FrozenLeft: > 0 })
        {
            enemy.FrozenLeft = 0;
            target.Health -= element == Element.Fire ? damage : damage * 2;   // fire thaws, anything else shatters
            return;
        }
        target.Health -= damage;
        if (element == Element.Fire) Ignite(target);
        if (element == Element.Ice)
        {
            if (target.BurnLeft > 0) target.BurnLeft = 0;
            else if (enemy is not null) enemy.FrozenLeft = Rules.FrozenSteps;
        }
    }

    private static void Ignite(Thing target)
    {
        if (target.BurnLeft == 0) target.BurnTick = Rules.BurnTick;
        target.BurnLeft = Rules.BurnSteps;
    }

    private void Effects()
    {
        _sources.Clear();
        _reached.Clear();
        foreach (var enemy in _world.All<Enemy>())
            if (enemy.FrozenLeft > 0) enemy.FrozenLeft--;
        foreach (var thing in _world.All<Thing>())
        {
            if (thing.BurnLeft == 0) continue;
            if (--thing.BurnTick == 0)
            {
                thing.Health--;
                thing.BurnTick = Rules.BurnTick;
                _sources.Add(thing);
            }
            thing.BurnLeft--;
        }
        // Fire reaches from every thing that burned this step. Who is reached is decided before anyone is changed, so
        // the outcome does not depend on the order of the walk.
        foreach (var source in _sources)
        {
            var from = _world.Local(source.Entity).Position2D;
            foreach (var thing in _world.All<Thing>())
            {
                if (thing == source || thing.BurnLeft > 0 || _reached.Contains(thing)) continue;
                if (Vector2.DistanceSquared(_world.Local(thing.Entity).Position2D, from) < Rules.SpreadRadius * Rules.SpreadRadius) _reached.Add(thing);
            }
        }
        foreach (var thing in _reached)
        {
            if (thing is Enemy { FrozenLeft: > 0 } frozen) frozen.FrozenLeft = 0;
            else Ignite(thing);
        }
    }

    private Thing? Nearest(Vector2 at)
    {
        Thing? nearest = null;
        float best = Rules.HitRadius * Rules.HitRadius;
        foreach (var thing in _world.All<Thing>())
        {
            float distance = Vector2.DistanceSquared(_world.Local(thing.Entity).Position2D, at);
            if (distance < best || (distance == best && nearest is not null && thing.Tag < nearest.Tag)) (nearest, best) = (thing, distance);
        }
        return nearest;
    }

    private void Deaths()
    {
        foreach (var thing in _world.All<Thing>())
        {
            if (thing.Health > 0) continue;
            _world.Destroy(thing.Entity);
            if (thing is Enemy) Kills++;
            else CratesBroken++;
        }
    }

    public List<Seen> Things()
    {
        var seen = new List<Seen>();
        foreach (var thing in _world.All<Thing>())
        {
            var enemy = thing as Enemy;
            seen.Add(new Seen(thing.Tag, enemy is null ? 'c' : 'e', _world.Local(thing.Entity).Position2D, thing.Health, thing.BurnLeft > 0, enemy is { FrozenLeft: > 0 }, enemy is { Shielded: true }));
        }
        return seen;
    }

    // The late question: every thing is looked at, and each condition is a test of its fields.
    public List<int> BurningUnshieldedNear(Vector2 center, float radius)
    {
        var tags = new List<int>();
        foreach (var thing in _world.All<Thing>())
        {
            if (thing.BurnLeft == 0 || thing is Enemy { Shielded: true }) continue;
            if (Vector2.DistanceSquared(_world.Local(thing.Entity).Position2D, center) < radius * radius) tags.Add(thing.Tag);
        }
        tags.Sort();
        return tags;
    }
}
