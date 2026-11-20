using System.Numerics;
using Arch.Core;
using MicroEngine.Ecs;
using MicroEngine.Kernel;

namespace EcsEffects;

#region movement
/// <summary>Everything with a place and a velocity moves, unless it is frozen: a bolt and a walker alike.</summary>
public sealed class Movement : EcsSystem
{
    private static readonly QueryDescription Movers = new QueryDescription().WithAll<Position, Velocity>().WithNone<Frozen>();
    private static readonly QueryDescription Walkers = new QueryDescription().WithAll<Position, Velocity, Health>().WithNone<Frozen>();

    protected override void Update(float dt)
    {
        foreach (ref var chunk in World.Query(in Movers))
        {
            var positions = chunk.GetSpan<Position>();
            var velocities = chunk.GetSpan<Velocity>();
            for (int i = 0; i < chunk.Count; i++) positions[i].Value += velocities[i].Value * dt;
        }
        // What has health turns at the edges; a bolt flies out and ends.
        foreach (ref var chunk in World.Query(in Walkers))
        {
            var positions = chunk.GetSpan<Position>();
            var velocities = chunk.GetSpan<Velocity>();
            for (int i = 0; i < chunk.Count; i++)
            {
                if (positions[i].Value.X < 4 || positions[i].Value.X > Game.Width - 4) velocities[i].Value.X = -velocities[i].Value.X;
                if (positions[i].Value.Y < 16 || positions[i].Value.Y > Game.Height - 30) velocities[i].Value.Y = -velocities[i].Value.Y;
            }
        }
    }
}
#endregion

#region hits
/// <summary>A bolt hits the nearest thing with health in reach, and what the hit does depends on what that thing has.</summary>
public sealed class Hits : EcsSystem
{
    private static readonly QueryDescription Flying = new QueryDescription().WithAll<Position, Shot>();
    private static readonly QueryDescription Targets = new QueryDescription().WithAll<Position, Health>();
    private readonly List<(int Fired, Entity Target, int Damage, Element Element)> _hits = [];

    protected override void Update(float dt)
    {
        foreach (ref var chunk in World.Query(in Flying))
        {
            var positions = chunk.GetSpan<Position>();
            var shots = chunk.GetSpan<Shot>();
            for (int i = 0; i < chunk.Count; i++)
            {
                if (Nearest(positions[i].Value, out var target)) _hits.Add((shots[i].Fired, target, shots[i].Damage, shots[i].Element));
                else if (--shots[i].Life > 0) continue;
                Commands.Destroy(chunk.Entity(i));   // applied when this system returns
            }
        }
        // A hit adds and removes components, which a walk does not allow, so hits are settled after it: in the order
        // the bolts were fired, because the store walks rows in no promised order.
        _hits.Sort(static (a, b) => a.Fired.CompareTo(b.Fired));
        foreach (var hit in _hits) Hit(World, hit.Target, hit.Damage, hit.Element);
        _hits.Clear();
    }

    private bool Nearest(Vector2 at, out Entity nearest)
    {
        nearest = Entity.Null;
        float best = Rules.HitRadius * Rules.HitRadius;
        foreach (ref var chunk in World.Query(in Targets))
        {
            var positions = chunk.GetSpan<Position>();
            for (int i = 0; i < chunk.Count; i++)
            {
                float distance = Vector2.DistanceSquared(positions[i].Value, at);
                var row = chunk.Entity(i);
                if (distance < best || (distance == best && nearest != Entity.Null && row.Id < nearest.Id)) (nearest, best) = (row, distance);
            }
        }
        return nearest != Entity.Null;
    }

    // The order of these checks is the rule: a shield first, then ice, then the hit and what it carries.
    private static void Hit(World world, Entity target, int damage, Element element)
    {
        if (world.Has<Shielded>(target))
        {
            world.Remove<Shielded>(target);
            return;
        }
        if (world.Has<Frozen>(target))
        {
            world.Remove<Frozen>(target);
            world.Get<Health>(target).Value -= element == Element.Fire ? damage : damage * 2;   // fire thaws, anything else shatters
            return;
        }
        world.Get<Health>(target).Value -= damage;
        if (element == Element.Fire) Effects.Ignite(world, target);
        if (element == Element.Ice)
        {
            if (world.Has<Burning>(target)) world.Remove<Burning>(target);
            else if (world.Has<Freezes>(target)) world.Add(target, new Frozen { Left = Rules.FrozenSteps });
        }
    }
}
#endregion

#region effects
/// <summary>Effects pass their time: ice ends, fire hurts, spreads to what is near and ends.</summary>
public sealed class Effects : EcsSystem
{
    private static readonly QueryDescription Iced = new QueryDescription().WithAll<Frozen>();
    private static readonly QueryDescription Alight = new QueryDescription().WithAll<Burning, Health, Position>();
    private static readonly QueryDescription NotAlight = new QueryDescription().WithAll<Position, Health>().WithNone<Burning>();
    private readonly List<(Entity Row, Vector2 At)> _sources = [];
    private readonly List<Entity> _reached = [];

    public static void Ignite(World world, Entity target)
    {
        if (world.Has<Burning>(target)) world.Get<Burning>(target).Left = Rules.BurnSteps;
        else world.Add(target, new Burning { Left = Rules.BurnSteps, Tick = Rules.BurnTick });
    }

    protected override void Update(float dt)
    {
        foreach (ref var chunk in World.Query(in Iced))
        {
            var frozen = chunk.GetSpan<Frozen>();
            for (int i = 0; i < chunk.Count; i++)
                if (--frozen[i].Left == 0) Commands.Remove<Frozen>(chunk.Entity(i));
        }
        foreach (ref var chunk in World.Query(in Alight))
        {
            var burning = chunk.GetSpan<Burning>();
            var health = chunk.GetSpan<Health>();
            var positions = chunk.GetSpan<Position>();
            for (int i = 0; i < chunk.Count; i++)
            {
                if (--burning[i].Tick == 0)
                {
                    health[i].Value--;
                    burning[i].Tick = Rules.BurnTick;
                    _sources.Add((chunk.Entity(i), positions[i].Value));
                }
                if (--burning[i].Left == 0) Commands.Remove<Burning>(chunk.Entity(i));
            }
        }
        // The effects that ended leave their rows now: the walk below must not see them.
        Store.Flush();

        // Fire reaches from every row that burned this step. Who is reached is decided before any row is changed.
        foreach (var (source, from) in _sources)
            foreach (ref var chunk in World.Query(in NotAlight))
            {
                var positions = chunk.GetSpan<Position>();
                for (int i = 0; i < chunk.Count; i++)
                {
                    var row = chunk.Entity(i);
                    if (row == source || _reached.Contains(row)) continue;
                    if (Vector2.DistanceSquared(positions[i].Value, from) < Rules.SpreadRadius * Rules.SpreadRadius) _reached.Add(row);
                }
            }
        foreach (var row in _reached)
        {
            if (World.Has<Frozen>(row)) World.Remove<Frozen>(row);   // fire takes the ice and does not catch
            else Ignite(World, row);
        }
        _sources.Clear();
        _reached.Clear();
    }
}
#endregion

#region deaths
/// <summary>What ran out of health leaves, and is counted by what it was. It runs after the scene's own update.</summary>
public sealed class Deaths : EcsSystem
{
    private static readonly QueryDescription Mortal = new QueryDescription().WithAll<Health>();

    public int Kills { get; private set; }
    public int CratesBroken { get; private set; }

    protected override void Update(float dt)
    {
        foreach (ref var chunk in World.Query(in Mortal))
        {
            var health = chunk.GetSpan<Health>();
            for (int i = 0; i < chunk.Count; i++)
            {
                if (health[i].Value > 0) continue;
                var row = chunk.Entity(i);
                Commands.Destroy(row);
                if (World.Has<Crate>(row)) CratesBroken++;
                else Kills++;
            }
        }
    }

    // What a system keeps between steps is simulation state too.
    public override void HashState(StateHasher hash)
    {
        hash.Add(Kills);
        hash.Add(CratesBroken);
    }
}
#endregion
