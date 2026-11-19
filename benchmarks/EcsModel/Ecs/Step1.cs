using System.Numerics;
using Arch.Buffer;
using Arch.Core;

namespace EcsModel.Ecs.Step1;

// Step 1 on an Entity Component System (Arch): enemies that move and projectiles that hurt them. A thing is a row of
// components; a behaviour is a walk over every row that has the components it needs.

public struct Tag { public int Value; }
public struct Position { public Vector2 Value; }
public struct Velocity { public Vector2 Value; }
public struct Health { public int Value; }
public struct Shot { public int Damage, Life; }

public sealed class Slice : ISlice
{
    private static readonly QueryDescription Movers = new QueryDescription().WithAll<Position, Velocity>();
    private static readonly QueryDescription Walkers = new QueryDescription().WithAll<Position, Velocity, Health>();
    private static readonly QueryDescription Flying = new QueryDescription().WithAll<Position, Shot>();
    private static readonly QueryDescription Targets = new QueryDescription().WithAll<Tag, Position, Health>();

    private readonly World _world = World.Create();
    // Rows are removed after the walks, never during one.
    private readonly CommandBuffer _later = new();

    public int Kills { get; private set; }
    public int CratesBroken => 0;
    public int Shots => _world.CountEntities(in Flying);

    public void Step(IReadOnlyList<Command> due)
    {
        foreach (var command in due) Apply(command);
        Move();
        Bounce();
        Hits();
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
                _world.Create(new Position { Value = shoot.From }, new Velocity { Value = shoot.Velocity }, new Shot { Damage = shoot.Damage, Life = Rules.ShotLife });
                break;
        }
    }

    // Enemies and projectiles alike: everything with a place and a velocity.
    private void Move() =>
        _world.Query(in Movers, static (ref Position position, ref Velocity velocity) => position.Value += velocity.Value * Rules.Dt);

    private void Bounce() =>
        _world.Query(in Walkers, static (ref Position position, ref Velocity velocity) =>
        {
            if (position.Value.X < 0 || position.Value.X > Rules.Width) velocity.Value.X = -velocity.Value.X;
            if (position.Value.Y < 0 || position.Value.Y > Rules.Height) velocity.Value.Y = -velocity.Value.Y;
        });

    private void Hits() =>
        _world.Query(in Flying, (Entity entity, ref Position position, ref Shot shot) =>
        {
            if (Nearest(position.Value, out var target))
            {
                _world.Get<Health>(target).Value -= shot.Damage;
                _later.Destroy(entity);
            }
            else if (--shot.Life <= 0) _later.Destroy(entity);
        });

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
        _world.Query(in Targets, (ref Tag tag, ref Position position, ref Health health) =>
            seen.Add(new Seen(tag.Value, 'e', position.Value, health.Value, false, false, false)));
        return seen;
    }

    public List<int> BurningUnshieldedNear(Vector2 center, float radius) => [];
}
