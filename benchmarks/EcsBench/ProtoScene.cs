using System.Numerics;
using EcsBench.Prototype;
using MicroEngine.Graphics;

namespace EcsBench;

/// <summary>The workloads on the first-party archetype prototype (<see cref="MiniWorld"/>), written like the other two.</summary>
internal sealed class ProtoScene(int load, Workload workload) : EcsScene(load, workload)
{
    private readonly MiniWorld _world = new();
    private readonly List<Ent> _dead = [], _deadShots = [];

    protected override int Live => _world.Count;

    protected override void Prepare()
    {
        _ = Objects;   // the scene's (empty) world is stepped with the scene, so Objects.Blend is the blend to draw at
        switch (Workload)
        {
            case Workload.Move:
                for (uint i = 0; i < Load; i++) _world.Create(new Position(Rules.Place(i)), new Previous(Rules.Place(i)), new Velocity(Rules.Speed(i)));
                break;
            case Workload.Churn:
                for (uint i = 0; i < Load; i++) SpawnMortal(i, Rules.Place(i), 1 + (int)(i % Rules.Lifetime));
                Next = (uint)Load;
                break;
            case Workload.Query:
                for (int i = 0; i < Load; i++)
                {
                    var position = new Position(Rules.Place((uint)i));
                    var velocity = new Velocity(Rules.Speed((uint)i));
                    var health = new Health(Rules.StartHealth(i));
                    var burning = new Burning(Rules.BurnOf(i));
                    switch (Rules.HasVelocity(i), Rules.HasHealth(i), Rules.HasBurning(i))
                    {
                        case (false, false, false): _world.Create(position); break;
                        case (true, false, false): _world.Create(position, velocity); break;
                        case (false, true, false): _world.Create(position, health); break;
                        case (false, false, true): _world.Create(position, burning); break;
                        case (true, true, false): _world.Create(position, velocity, health); break;
                        case (true, false, true): _world.Create(position, velocity, burning); break;
                        case (false, true, true): _world.Create(position, health, burning); break;
                        default: _world.Create(position, velocity, health, burning); break;
                    }
                }
                break;
            default:
                for (uint i = 0; i < Load; i++) SpawnAgent(i);
                for (uint i = 0; i < Load / 2; i++) SpawnMortal((uint)Load + i, Rules.Place((uint)Load + i), Rules.ShotLifeOf((uint)Load + i));
                Next = (uint)(Load + Load / 2);
                break;
        }
    }

    private void SpawnMortal(uint serial, Vector2 at, int life) =>
        _world.Create(new Position(at), new Previous(at), new Velocity(Rules.Speed(serial)), new Life(life));

    private void SpawnAgent(uint serial) =>
        _world.Create(new Position(Rules.Place(serial)), new Previous(Rules.Place(serial)), new Velocity(Vector2.Zero), new Health(Rules.AgentHealth(serial)));

    protected override void Step(float dt)
    {
        long t = Now;
        switch (Workload)
        {
            case Workload.Move:
                foreach (var archetype in _world.With<Position, Previous, Velocity>())
                {
                    var p = archetype.Span<Position>();
                    var prev = archetype.Span<Previous>();
                    var v = archetype.Span<Velocity>();
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                    }
                }
                Lap(0, t);
                break;

            case Workload.Churn:
                foreach (var archetype in _world.With<Position, Previous, Velocity, Life>())
                {
                    var p = archetype.Span<Position>();
                    var prev = archetype.Span<Previous>();
                    var v = archetype.Span<Velocity>();
                    var life = archetype.Span<Life>();
                    var entities = archetype.Entities;
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                        if (--life[i].Steps == 0) _dead.Add(entities[i]);
                    }
                }
                t = Lap(0, t);
                foreach (var e in _dead) _world.Destroy(e);
                t = Lap(1, t);
                for (int i = _dead.Count; i > 0; i--)
                {
                    uint serial = Next++;
                    SpawnMortal(serial, Rules.Place(serial), Rules.Lifetime);
                }
                _dead.Clear();
                Lap(2, t);
                break;

            case Workload.Query:
            {
                foreach (var archetype in _world.With<Position, Velocity>())
                {
                    var p = archetype.Span<Position>();
                    var v = archetype.Span<Velocity>();
                    for (int i = 0; i < p.Length; i++) Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                }
                t = Lap(0, t);
                foreach (var archetype in _world.With<Health, Burning>())
                {
                    var h = archetype.Span<Health>();
                    var b = archetype.Span<Burning>();
                    for (int i = 0; i < h.Length; i++)
                    {
                        h[i].Value -= b[i].Damage;
                        if (h[i].Value <= 0) h[i].Value = 100;
                    }
                }
                t = Lap(1, t);
                long count = 0, sum = 0;
                foreach (var archetype in _world.With<Position, Velocity, Health>())
                {
                    var p = archetype.Span<Position>();
                    var h = archetype.Span<Health>();
                    for (int i = 0; i < p.Length; i++)
                    {
                        if (p[i].Value.X >= Rules.Width / 2) continue;
                        count++;
                        sum += h[i].Value;
                    }
                }
                Found += count * 31 + sum;
                Lap(2, t);
                break;
            }

            default:
            {
                var target = Rules.Target(Steps);
                foreach (var archetype in _world.With<Position, Previous, Velocity, Health>())
                {
                    var p = archetype.Span<Position>();
                    var prev = archetype.Span<Previous>();
                    var v = archetype.Span<Velocity>();
                    var h = archetype.Span<Health>();
                    var entities = archetype.Entities;
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        if (Rules.Chase(ref p[i].Value, ref v[i].Value, target, dt) && --h[i].Value <= 0) _dead.Add(entities[i]);
                    }
                }
                t = Lap(0, t);
                foreach (var archetype in _world.With<Position, Previous, Velocity, Life>())
                {
                    var p = archetype.Span<Position>();
                    var prev = archetype.Span<Previous>();
                    var v = archetype.Span<Velocity>();
                    var life = archetype.Span<Life>();
                    var entities = archetype.Entities;
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        p[i].Value += v[i].Value * dt;
                        if (--life[i].Steps == 0) _deadShots.Add(entities[i]);
                    }
                }
                t = Lap(1, t);
                foreach (var e in _dead) _world.Destroy(e);
                foreach (var e in _deadShots) _world.Destroy(e);
                for (int i = _dead.Count; i > 0; i--) SpawnAgent(Next++);
                for (int i = _deadShots.Count; i > 0; i--)
                {
                    uint serial = Next++;
                    SpawnMortal(serial, target, Rules.ShotLifeOf(serial));
                }
                _dead.Clear();
                _deadShots.Clear();
                Lap(2, t);
                break;
            }
        }
        Steps++;
    }

    public override void Render(IRenderer r)
    {
        if (Workload is not (Workload.Move or Workload.Mixed)) return;
        var ball = Bench.Ball;
        float blend = Objects.Blend;
        foreach (var archetype in _world.With<Position, Previous>())
        {
            var p = archetype.Span<Position>();
            var prev = archetype.Span<Previous>();
            for (int i = 0; i < p.Length; i++) r.Draw(ball, Rules.Drawn(prev[i].Value, p[i].Value, blend));
        }
    }

    protected override long Sum()
    {
        long sum = 0;
        switch (Workload)
        {
            case Workload.Move:
                foreach (var archetype in _world.With<Position, Previous, Velocity>())
                    foreach (ref var p in archetype.Span<Position>()) sum += Rules.HashOf(p.Value, 0);
                break;
            case Workload.Query:
                foreach (var archetype in _world.With<Position, Velocity>())
                    foreach (ref var p in archetype.Span<Position>()) sum += Rules.HashOf(p.Value, 1);
                foreach (var archetype in _world.With<Position, Health>())
                {
                    var p = archetype.Span<Position>();
                    var h = archetype.Span<Health>();
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, 2) + h[i].Value;
                }
                foreach (var archetype in _world.With<Position, Burning>())
                {
                    var p = archetype.Span<Position>();
                    var b = archetype.Span<Burning>();
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, 3) + b[i].Damage;
                }
                break;
            default:
                foreach (var archetype in _world.With<Position, Previous, Velocity, Life>())
                {
                    var p = archetype.Span<Position>();
                    var life = archetype.Span<Life>();
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, life[i].Steps);
                }
                foreach (var archetype in _world.With<Position, Previous, Velocity, Health>())
                {
                    var p = archetype.Span<Position>();
                    var h = archetype.Span<Health>();
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, h[i].Value);
                }
                break;
        }
        return sum;
    }
}
