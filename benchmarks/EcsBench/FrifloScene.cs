using System.Numerics;
using Friflo.Engine.ECS;
using MicroEngine.Graphics;

namespace EcsBench;

/// <summary>
/// The workloads on Friflo.Engine.ECS 3.6.0: one <see cref="EntityStore"/> per scene, queries walked chunk by chunk
/// over component spans, structural changes after the walk.
/// </summary>
internal sealed class FrifloScene(int load, Workload workload) : EcsScene(load, workload)
{
    private EntityStore _store = null!;
    private readonly List<int> _dead = [], _deadShots = [];
    private ArchetypeQuery<Position, Previous, Velocity> _movers = null!;
    private ArchetypeQuery<Position, Previous, Velocity, Life> _mortals = null!;
    private ArchetypeQuery<Position, Previous, Velocity, Health> _agents = null!;
    private ArchetypeQuery<Position, Previous> _drawn = null!;
    private ArchetypeQuery<Position, Velocity> _posVel = null!;
    private ArchetypeQuery<Health, Burning> _healthBurning = null!;
    private ArchetypeQuery<Position, Velocity, Health> _posVelHealth = null!;
    private ArchetypeQuery<Position, Health> _posHealth = null!;
    private ArchetypeQuery<Position, Burning> _posBurning = null!;

    protected override int Live => _store.Count;

    protected override void Prepare()
    {
        _ = Objects;   // the scene's (empty) world is stepped with the scene, so Objects.Blend is the blend to draw at
        _store = new EntityStore();
        _movers = _store.Query<Position, Previous, Velocity>();
        _mortals = _store.Query<Position, Previous, Velocity, Life>();
        _agents = _store.Query<Position, Previous, Velocity, Health>();
        _drawn = _store.Query<Position, Previous>();
        _posVel = _store.Query<Position, Velocity>();
        _healthBurning = _store.Query<Health, Burning>();
        _posVelHealth = _store.Query<Position, Velocity, Health>();
        _posHealth = _store.Query<Position, Health>();
        _posBurning = _store.Query<Position, Burning>();
        switch (Workload)
        {
            case Workload.Move:
                for (uint i = 0; i < Load; i++) _store.CreateEntity(new Position(Rules.Place(i)), new Previous(Rules.Place(i)), new Velocity(Rules.Speed(i)));
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
                        case (false, false, false): _store.CreateEntity(position); break;
                        case (true, false, false): _store.CreateEntity(position, velocity); break;
                        case (false, true, false): _store.CreateEntity(position, health); break;
                        case (false, false, true): _store.CreateEntity(position, burning); break;
                        case (true, true, false): _store.CreateEntity(position, velocity, health); break;
                        case (true, false, true): _store.CreateEntity(position, velocity, burning); break;
                        case (false, true, true): _store.CreateEntity(position, health, burning); break;
                        default: _store.CreateEntity(position, velocity, health, burning); break;
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
        _store.CreateEntity(new Position(at), new Previous(at), new Velocity(Rules.Speed(serial)), new Life(life));

    private void SpawnAgent(uint serial) =>
        _store.CreateEntity(new Position(Rules.Place(serial)), new Previous(Rules.Place(serial)), new Velocity(Vector2.Zero), new Health(Rules.AgentHealth(serial)));

    protected override void Step(float dt)
    {
        long t = Now;
        switch (Workload)
        {
            case Workload.Move:
                foreach (var (positions, previous, velocities, _) in _movers.Chunks)
                {
                    var p = positions.Span;
                    var prev = previous.Span;
                    var v = velocities.Span;
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                    }
                }
                Lap(0, t);
                break;

            case Workload.Churn:
                foreach (var (positions, previous, velocities, lives, entities) in _mortals.Chunks)
                {
                    var p = positions.Span;
                    var prev = previous.Span;
                    var v = velocities.Span;
                    var life = lives.Span;
                    var ids = entities.Ids;
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                        if (--life[i].Steps == 0) _dead.Add(ids[i]);
                    }
                }
                t = Lap(0, t);
                foreach (int id in _dead) _store.GetEntityById(id).DeleteEntity();
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
                foreach (var (positions, velocities, _) in _posVel.Chunks)
                {
                    var p = positions.Span;
                    var v = velocities.Span;
                    for (int i = 0; i < p.Length; i++) Rules.Move(ref p[i].Value, ref v[i].Value, dt);
                }
                t = Lap(0, t);
                foreach (var (healths, burnings, _) in _healthBurning.Chunks)
                {
                    var h = healths.Span;
                    var b = burnings.Span;
                    for (int i = 0; i < h.Length; i++)
                    {
                        h[i].Value -= b[i].Damage;
                        if (h[i].Value <= 0) h[i].Value = 100;
                    }
                }
                t = Lap(1, t);
                long count = 0, sum = 0;
                foreach (var (positions, _, healths, _) in _posVelHealth.Chunks)
                {
                    var p = positions.Span;
                    var h = healths.Span;
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
                foreach (var (positions, previous, velocities, healths, entities) in _agents.Chunks)
                {
                    var p = positions.Span;
                    var prev = previous.Span;
                    var v = velocities.Span;
                    var h = healths.Span;
                    var ids = entities.Ids;
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        if (Rules.Chase(ref p[i].Value, ref v[i].Value, target, dt) && --h[i].Value <= 0) _dead.Add(ids[i]);
                    }
                }
                t = Lap(0, t);
                foreach (var (positions, previous, velocities, lives, entities) in _mortals.Chunks)
                {
                    var p = positions.Span;
                    var prev = previous.Span;
                    var v = velocities.Span;
                    var life = lives.Span;
                    var ids = entities.Ids;
                    for (int i = 0; i < p.Length; i++)
                    {
                        prev[i].Value = p[i].Value;
                        p[i].Value += v[i].Value * dt;
                        if (--life[i].Steps == 0) _deadShots.Add(ids[i]);
                    }
                }
                t = Lap(1, t);
                foreach (int id in _dead) _store.GetEntityById(id).DeleteEntity();
                foreach (int id in _deadShots) _store.GetEntityById(id).DeleteEntity();
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
        foreach (var (positions, previous, _) in _drawn.Chunks)
        {
            var p = positions.Span;
            var prev = previous.Span;
            for (int i = 0; i < p.Length; i++) r.Draw(ball, Rules.Drawn(prev[i].Value, p[i].Value, blend));
        }
    }

    protected override long Sum()
    {
        long sum = 0;
        switch (Workload)
        {
            case Workload.Move:
                foreach (var (positions, _, _, _) in _movers.Chunks)
                    foreach (ref var p in positions.Span) sum += Rules.HashOf(p.Value, 0);
                break;
            case Workload.Query:
                foreach (var (positions, _, _) in _posVel.Chunks)
                    foreach (ref var p in positions.Span) sum += Rules.HashOf(p.Value, 1);
                foreach (var (positions, healths, _) in _posHealth.Chunks)
                {
                    var p = positions.Span;
                    var h = healths.Span;
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, 2) + h[i].Value;
                }
                foreach (var (positions, burnings, _) in _posBurning.Chunks)
                {
                    var p = positions.Span;
                    var b = burnings.Span;
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, 3) + b[i].Damage;
                }
                break;
            default:
                foreach (var (positions, _, _, lives, _) in _mortals.Chunks)
                {
                    var p = positions.Span;
                    var life = lives.Span;
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, life[i].Steps);
                }
                foreach (var (positions, _, _, healths, _) in _agents.Chunks)
                {
                    var p = positions.Span;
                    var h = healths.Span;
                    for (int i = 0; i < p.Length; i++) sum += Rules.HashOf(p[i].Value, h[i].Value);
                }
                break;
        }
        return sum;
    }
}
