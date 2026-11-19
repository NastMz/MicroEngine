using System.Numerics;
using Arch.Buffer;
using Arch.Core;

namespace EcsModel.Ecs.Step2;

// Step 2 on an Entity Component System (Arch): status effects that come and go on live enemies. An effect is a
// component added to the row and removed from it; a behaviour that an effect switches off leaves it out of its walk.

public struct Tag { public int Value; }
public struct Position { public Vector2 Value; }
public struct Velocity { public Vector2 Value; }
public struct Health { public int Value; }
public struct Shot { public int Damage, Life, Fired; public Element Element; }
public struct Burning { public int Left, Tick; }
public struct Frozen { public int Left; }
public struct Shielded;

public sealed class Slice : ISlice
{
    private static readonly QueryDescription Movers = new QueryDescription().WithAll<Position, Velocity>().WithNone<Frozen>();
    private static readonly QueryDescription Walkers = new QueryDescription().WithAll<Position, Velocity, Health>().WithNone<Frozen>();
    private static readonly QueryDescription Flying = new QueryDescription().WithAll<Position, Shot>();
    private static readonly QueryDescription Targets = new QueryDescription().WithAll<Tag, Position, Health>();
    private static readonly QueryDescription Iced = new QueryDescription().WithAll<Frozen>();
    private static readonly QueryDescription Alight = new QueryDescription().WithAll<Burning, Health, Position>();
    private static readonly QueryDescription NotAlight = new QueryDescription().WithAll<Position, Health>().WithNone<Burning>();

    private readonly World _world = World.Create();
    // Rows are removed after the walks, never during one.
    private readonly CommandBuffer _later = new();
    private readonly List<(int Fired, Entity Target, int Damage, Element Element)> _hits = [];
    private readonly List<(Entity Entity, Vector2 At)> _sources = [];
    private readonly List<Entity> _reached = [];
    private int _fired;

    public int Kills { get; private set; }
    public int CratesBroken => 0;
    public int Shots => _world.CountEntities(in Flying);

    public void Step(IReadOnlyList<Command> due)
    {
        foreach (var command in due) Apply(command);
        Move();
        Bounce();
        Hits();
        Effects();
        Deaths();
        _later.Playback(_world);
    }

    private void Apply(Command command)
    {
        switch (command)
        {
            case SpawnEnemy spawn:
                _world.Create(new Tag { Value = spawn.Tag }, new Position { Value = spawn.At }, new Velocity { Value = spawn.Velocity }, new Health { Value = spawn.Health });
                break;
            case Shoot shoot:
                _world.Create(new Position { Value = shoot.From }, new Velocity { Value = shoot.Velocity }, new Shot { Damage = shoot.Damage, Life = Rules.ShotLife, Fired = _fired++, Element = shoot.Element });
                break;
            case Shield shield:
                // A component cannot be added during a walk: find the row first.
                if (Find(shield.Tag, out var shielded) && !_world.Has<Shielded>(shielded)) _world.Add<Shielded>(shielded);
                break;
        }
    }

    private bool Find(int tag, out Entity found)
    {
        var match = Entity.Null;
        _world.Query(in Targets, (Entity entity, ref Tag candidate) =>
        {
            if (candidate.Value == tag) match = entity;
        });
        found = match;
        return match != Entity.Null;
    }

    // Enemies and projectiles alike: everything with a place and a velocity that is not frozen.
    private void Move() =>
        _world.Query(in Movers, static (ref Position position, ref Velocity velocity) => position.Value += velocity.Value * Rules.Dt);

    private void Bounce() =>
        _world.Query(in Walkers, static (ref Position position, ref Velocity velocity) =>
        {
            if (position.Value.X < 0 || position.Value.X > Rules.Width) velocity.Value.X = -velocity.Value.X;
            if (position.Value.Y < 0 || position.Value.Y > Rules.Height) velocity.Value.Y = -velocity.Value.Y;
        });

    private void Hits()
    {
        _hits.Clear();
        _world.Query(in Flying, (Entity entity, ref Position position, ref Shot shot) =>
        {
            if (Nearest(position.Value, out var target))
            {
                _hits.Add((shot.Fired, target, shot.Damage, shot.Element));
                _later.Destroy(entity);
            }
            else if (--shot.Life <= 0) _later.Destroy(entity);
        });
        // A hit adds and removes components, which a walk does not allow, so hits are settled after it; and the store
        // walks rows in no promised order, so they are settled in the order the projectiles were fired.
        _hits.Sort(static (a, b) => a.Fired.CompareTo(b.Fired));
        foreach (var hit in _hits) Hit(hit.Target, hit.Damage, hit.Element);
    }

    // The order of these checks is the rule: a shield first, then ice, then the hit and what it carries.
    private void Hit(Entity target, int damage, Element element)
    {
        if (_world.Has<Shielded>(target))
        {
            _world.Remove<Shielded>(target);
            return;
        }
        if (_world.Has<Frozen>(target))
        {
            _world.Remove<Frozen>(target);
            _world.Get<Health>(target).Value -= element == Element.Fire ? damage : damage * 2;   // fire thaws, anything else shatters
            return;
        }
        _world.Get<Health>(target).Value -= damage;
        if (element == Element.Fire) Ignite(target);
        if (element == Element.Ice)
        {
            if (_world.Has<Burning>(target)) _world.Remove<Burning>(target);
            else _world.Add(target, new Frozen { Left = Rules.FrozenSteps });
        }
    }

    private void Ignite(Entity target)
    {
        if (_world.Has<Burning>(target)) _world.Get<Burning>(target).Left = Rules.BurnSteps;
        else _world.Add(target, new Burning { Left = Rules.BurnSteps, Tick = Rules.BurnTick });
    }

    private void Effects()
    {
        _sources.Clear();
        _reached.Clear();
        _world.Query(in Iced, (Entity entity, ref Frozen frozen) =>
        {
            if (--frozen.Left == 0) _later.Remove<Frozen>(entity);
        });
        _world.Query(in Alight, (Entity entity, ref Burning burning, ref Health health, ref Position position) =>
        {
            if (--burning.Tick == 0)
            {
                health.Value--;
                burning.Tick = Rules.BurnTick;
                _sources.Add((entity, position.Value));
            }
            if (--burning.Left == 0) _later.Remove<Burning>(entity);
        });
        // The effects that ended leave their rows now: the walk below must not see them.
        _later.Playback(_world);
        // Fire reaches from every row that burned this step. Who is reached is decided before any row is changed.
        foreach (var (source, from) in _sources)
            _world.Query(in NotAlight, (Entity entity, ref Position position) =>
            {
                if (entity == source || _reached.Contains(entity)) return;
                if (Vector2.DistanceSquared(position.Value, from) < Rules.SpreadRadius * Rules.SpreadRadius) _reached.Add(entity);
            });
        foreach (var entity in _reached)
        {
            if (_world.Has<Frozen>(entity)) _world.Remove<Frozen>(entity);
            else Ignite(entity);
        }
    }

    private bool Nearest(Vector2 at, out Entity nearest)
    {
        nearest = Entity.Null;
        float best = Rules.HitRadius * Rules.HitRadius;
        int bestTag = 0;
        foreach (ref var chunk in _world.Query(in Targets))
        {
            var tags = chunk.GetSpan<Tag>();
            var positions = chunk.GetSpan<Position>();
            for (int i = 0; i < chunk.Count; i++)
            {
                float distance = Vector2.DistanceSquared(positions[i].Value, at);
                if (distance < best || (distance == best && nearest != Entity.Null && tags[i].Value < bestTag))
                    (nearest, best, bestTag) = (chunk.Entity(i), distance, tags[i].Value);
            }
        }
        return nearest != Entity.Null;
    }

    private void Deaths() =>
        _world.Query(in Targets, (Entity entity, ref Health health) =>
        {
            if (health.Value > 0) return;
            _later.Destroy(entity);
            Kills++;
        });

    public List<Seen> Things()
    {
        var seen = new List<Seen>();
        _world.Query(in Targets, (Entity entity, ref Tag tag, ref Position position, ref Health health) =>
            seen.Add(new Seen(tag.Value, 'e', position.Value, health.Value, _world.Has<Burning>(entity), _world.Has<Frozen>(entity), _world.Has<Shielded>(entity))));
        return seen;
    }

    public List<int> BurningUnshieldedNear(Vector2 center, float radius) => [];
}
