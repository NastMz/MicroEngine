using System.Numerics;
using Arch.Core;
using MicroEngine.Graphics;

namespace EcsBench;

/// <summary>
/// The workloads on Arch 2.1.0: one <see cref="World"/> per scene, queries walked chunk by chunk over component spans
/// (the library's fastest documented path that needs no code generation), structural changes after the walk.
/// </summary>
internal sealed class ArchScene(int load, Workload workload) : EcsScene(load, workload)
{
    // The library makes its component arrays by reflection unless a factory is registered. A NativeAOT build needs the
    // factories (without them it throws for any component type the program itself never makes an array of).
    static ArchScene()
    {
        if (System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled) return;
        ArrayRegistry.Add<Position>();
        ArrayRegistry.Add<Previous>();
        ArrayRegistry.Add<Velocity>();
        ArrayRegistry.Add<Life>();
        ArrayRegistry.Add<Health>();
        ArrayRegistry.Add<Burning>();
    }

    private World _world = null!;
    private readonly List<Entity> _dead = [], _deadShots = [];
    private static readonly QueryDescription Movers = new QueryDescription().WithAll<Position, Previous, Velocity>();
    private static readonly QueryDescription Mortals = new QueryDescription().WithAll<Position, Previous, Velocity, Life>();
    private static readonly QueryDescription Agents = new QueryDescription().WithAll<Position, Previous, Velocity, Health>();
    private static readonly QueryDescription Drawn = new QueryDescription().WithAll<Position, Previous>();
    private static readonly QueryDescription PosVel = new QueryDescription().WithAll<Position, Velocity>();
    private static readonly QueryDescription HealthBurning = new QueryDescription().WithAll<Health, Burning>();
    private static readonly QueryDescription PosVelHealth = new QueryDescription().WithAll<Position, Velocity, Health>();
    private static readonly QueryDescription PosHealth = new QueryDescription().WithAll<Position, Health>();
    private static readonly QueryDescription PosBurning = new QueryDescription().WithAll<Position, Burning>();

    protected override int Live => _world.Size;

    protected override void Prepare()
    {
        _ = Objects;   // the scene's (empty) world is stepped with the scene, so Objects.Blend is the blend to draw at
        _world = World.Create();
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
                foreach (ref var chunk in _world.Query(in Movers))
                {
                    int n = chunk.Count;
                    var p = chunk.GetSpan<Position>()[..n];
                    var prev = chunk.GetSpan<Previous>()[..n];
                    var v = chunk.GetSpan<Velocity>()[..n];
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                    }
                }
                Lap(0, t);
                break;

            case Workload.Churn:
                foreach (ref var chunk in _world.Query(in Mortals))
                {
                    int n = chunk.Count;
                    var p = chunk.GetSpan<Position>()[..n];
                    var prev = chunk.GetSpan<Previous>()[..n];
                    var v = chunk.GetSpan<Velocity>()[..n];
                    var life = chunk.GetSpan<Life>()[..n];
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                        if (--life[i].Steps == 0) _dead.Add(chunk.Entity(i));
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
                foreach (ref var chunk in _world.Query(in PosVel))
                {
                    int n = chunk.Count;
                    var p = chunk.GetSpan<Position>()[..n];
                    var v = chunk.GetSpan<Velocity>()[..n];
                    for (int i = 0; i < p.Length; i++) Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                }
                t = Lap(0, t);
                foreach (ref var chunk in _world.Query(in HealthBurning))
                {
                    int n = chunk.Count;
                    var h = chunk.GetSpan<Health>()[..n];
                    var b = chunk.GetSpan<Burning>()[..n];
                    for (int i = 0; i < h.Length; i++)
                    {
                        h[i].Value -= b[i].Damage;
                        if (h[i].Value <= 0) h[i].Value = 100;
                    }
                }
                t = Lap(1, t);
                long count = 0, sum = 0;
                foreach (ref var chunk in _world.Query(in PosVelHealth))
                {
                    int n = chunk.Count;
                    var p = chunk.GetSpan<Position>()[..n];
                    var h = chunk.GetSpan<Health>()[..n];
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
                foreach (ref var chunk in _world.Query(in Agents))
                {
                    int n = chunk.Count;
                    var p = chunk.GetSpan<Position>()[..n];
                    var prev = chunk.GetSpan<Previous>()[..n];
                    var v = chunk.GetSpan<Velocity>()[..n];
                    var h = chunk.GetSpan<Health>()[..n];
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        if (Rules.Chase(ref p[i].Value, ref v[i].Value, target, dt) && --h[i].Value <= 0) _dead.Add(chunk.Entity(i));
                    }
                }
                t = Lap(0, t);
                foreach (ref var chunk in _world.Query(in Mortals))
                {
                    int n = chunk.Count;
                    var p = chunk.GetSpan<Position>()[..n];
                    var prev = chunk.GetSpan<Previous>()[..n];
                    var v = chunk.GetSpan<Velocity>()[..n];
                    var life = chunk.GetSpan<Life>()[..n];
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        p[i].Value += v[i].Value * dt;
                        if (--life[i].Steps == 0) _deadShots.Add(chunk.Entity(i));
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
        foreach (ref var chunk in _world.Query(in Drawn))
        {
            int n = chunk.Count;
            var p = chunk.GetSpan<Position>()[..n];
            var prev = chunk.GetSpan<Previous>()[..n];
            for (int i = 0; i < p.Length; i++) r.Draw(ball, Rules.Drawn(prev[i].Value, p[i].Value, blend));
        }
    }

    protected override long Sum()
    {
        long sum = 0;
        switch (Workload)
        {
            case Workload.Move:
                foreach (ref var chunk in _world.Query(in Movers))
                    foreach (ref var p in chunk.GetSpan<Position>()[..chunk.Count]) sum += Rules.HashOf(p.Value, 0);
                break;
            case Workload.Query:
                foreach (ref var chunk in _world.Query(in PosVel))
                    foreach (ref var p in chunk.GetSpan<Position>()[..chunk.Count]) sum += Rules.HashOf(p.Value, 1);
                foreach (ref var chunk in _world.Query(in PosHealth))
                {
                    var p = chunk.GetSpan<Position>()[..chunk.Count];
                    var h = chunk.GetSpan<Health>()[..chunk.Count];
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, 2) + h[i].Value;
                }
                foreach (ref var chunk in _world.Query(in PosBurning))
                {
                    var p = chunk.GetSpan<Position>()[..chunk.Count];
                    var b = chunk.GetSpan<Burning>()[..chunk.Count];
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, 3) + b[i].Damage;
                }
                break;
            default:
                foreach (ref var chunk in _world.Query(in Mortals))
                {
                    var p = chunk.GetSpan<Position>()[..chunk.Count];
                    var life = chunk.GetSpan<Life>()[..chunk.Count];
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, life[i].Steps);
                }
                foreach (ref var chunk in _world.Query(in Agents))
                {
                    var p = chunk.GetSpan<Position>()[..chunk.Count];
                    var h = chunk.GetSpan<Health>()[..chunk.Count];
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, h[i].Value);
                }
                break;
        }
        return sum;
    }

    public override void Cleanup()
    {
        if (_world is not null) World.Destroy(_world);
    }
}
