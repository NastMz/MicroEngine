using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace EcsBench;

// The attachments a game on the simple world would write: one class per kind of object, the position in the entity's
// transform (so the world interpolates it), the entity's id kept so a walk over the attachments reaches the transform.

internal sealed class Mover
{
    public EntityId Id;
    public Vector2 Velocity;
    public int Life;
}

internal sealed class Chaser
{
    public EntityId Id;
    public Vector2 Velocity;
    public int Health;
}

internal sealed class Shot
{
    public EntityId Id;
    public Vector2 Velocity;
    public int Life;
}

internal sealed class Motion
{
    public EntityId Id;
    public Vector2 Velocity;
}

internal sealed class Vitals
{
    public EntityId Id;
    public int Value;
}

internal sealed class Burn
{
    public EntityId Id;
    public int Damage;
}

/// <summary>
/// The workloads on the engine's world (<see cref="IWorld"/>): <c>All&lt;T&gt;</c> walks one attachment type,
/// <c>Local</c> reaches the transform, <c>TryGet&lt;T&gt;</c> a second attachment. With <paramref name="pooled"/> the
/// attachments of destroyed objects are reused; without it every spawn allocates one.
/// </summary>
internal sealed class SimpleScene(int load, Workload workload, bool pooled) : EcsScene(load, workload)
{
    private readonly Stack<Mover> _movers = new();
    private readonly Stack<Chaser> _chasers = new();
    private readonly Stack<Shot> _shots = new();
    private readonly List<Mover> _deadMovers = [];
    private readonly List<Chaser> _deadChasers = [];
    private readonly List<Shot> _deadShots = [];

    protected override int Live => Objects.Count;

    protected override void Prepare()
    {
        var world = Objects;
        switch (Workload)
        {
            case Workload.Move:
                for (uint i = 0; i < Load; i++) SpawnMover(i, 0);
                break;
            case Workload.Churn:
                for (uint i = 0; i < Load; i++) SpawnMover(i, 1 + (int)(i % Rules.Lifetime));
                Next = (uint)Load;
                break;
            case Workload.Query:
                for (int i = 0; i < Load; i++)
                {
                    var id = world.Create();
                    world.Local(id).Position2D = Rules.Place((uint)i);
                    if (Rules.HasVelocity(i)) world.Attach(id, new Motion { Id = id, Velocity = Rules.Speed((uint)i) });
                    if (Rules.HasHealth(i)) world.Attach(id, new Vitals { Id = id, Value = Rules.StartHealth(i) });
                    if (Rules.HasBurning(i)) world.Attach(id, new Burn { Id = id, Damage = Rules.BurnOf(i) });
                }
                break;
            default:
                for (uint i = 0; i < Load; i++) SpawnChaser(i);
                for (uint i = 0; i < Load / 2; i++) SpawnShot((uint)Load + i, Rules.Place((uint)Load + i));
                Next = (uint)(Load + Load / 2);
                break;
        }
    }

    private void SpawnMover(uint serial, int life)
    {
        var world = Objects;
        var id = world.Create();
        var mover = pooled && _movers.Count > 0 ? _movers.Pop() : new Mover();
        mover.Id = id;
        mover.Velocity = Rules.Speed(serial);
        mover.Life = life;
        world.Attach(id, mover);
        world.Local(id).Position2D = Rules.Place(serial);
    }

    private void SpawnChaser(uint serial)
    {
        var world = Objects;
        var id = world.Create();
        var chaser = pooled && _chasers.Count > 0 ? _chasers.Pop() : new Chaser();
        chaser.Id = id;
        chaser.Velocity = Vector2.Zero;
        chaser.Health = Rules.AgentHealth(serial);
        world.Attach(id, chaser);
        world.Local(id).Position2D = Rules.Place(serial);
    }

    private void SpawnShot(uint serial, Vector2 at)
    {
        var world = Objects;
        var id = world.Create();
        var shot = pooled && _shots.Count > 0 ? _shots.Pop() : new Shot();
        shot.Id = id;
        shot.Velocity = Rules.Speed(serial);
        shot.Life = Rules.ShotLifeOf(serial);
        world.Attach(id, shot);
        world.Local(id).Position2D = at;
    }

    protected override void Step(float dt)
    {
        var world = Objects;
        long t = Now;
        switch (Workload)
        {
            case Workload.Move:
                foreach (var m in world.All<Mover>())
                {
                    ref var local = ref world.Local(m.Id);
                    var p = local.Position2D;
                    Rules.Move(ref p, ref m.Velocity, dt);
                    local.Position2D = p;
                }
                Lap(0, t);
                break;

            case Workload.Churn:
                foreach (var m in world.All<Mover>())
                {
                    ref var local = ref world.Local(m.Id);
                    var p = local.Position2D;
                    Rules.Move(ref p, ref m.Velocity, dt);
                    local.Position2D = p;
                    if (--m.Life == 0) _deadMovers.Add(m);
                }
                t = Lap(0, t);
                foreach (var m in _deadMovers)
                {
                    world.Destroy(m.Id);
                    if (pooled) _movers.Push(m);
                }
                t = Lap(1, t);
                for (int i = _deadMovers.Count; i > 0; i--) SpawnMover(Next++, Rules.Lifetime);
                _deadMovers.Clear();
                Lap(2, t);
                break;

            case Workload.Query:
            {
                foreach (var m in world.All<Motion>())
                {
                    ref var local = ref world.Local(m.Id);
                    var p = local.Position2D;
                    Rules.Move(ref p, ref m.Velocity, dt);
                    local.Position2D = p;
                }
                t = Lap(0, t);
                foreach (var b in world.All<Burn>())
                {
                    if (!world.TryGet<Vitals>(b.Id, out var v)) continue;
                    v.Value -= b.Damage;
                    if (v.Value <= 0) v.Value = 100;
                }
                t = Lap(1, t);
                long count = 0, sum = 0;
                foreach (var v in world.All<Vitals>())
                {
                    if (!world.TryGet<Motion>(v.Id, out _) || world.Local(v.Id).Position2D.X >= Rules.Width / 2) continue;
                    count++;
                    sum += v.Value;
                }
                Found += count * 31 + sum;
                Lap(2, t);
                break;
            }

            default:
            {
                var target = Rules.Target(Steps);
                foreach (var c in world.All<Chaser>())
                {
                    ref var local = ref world.Local(c.Id);
                    var p = local.Position2D;
                    bool near = Rules.Chase(ref p, ref c.Velocity, target, dt);
                    local.Position2D = p;
                    if (near && --c.Health <= 0) _deadChasers.Add(c);
                }
                t = Lap(0, t);
                foreach (var s in world.All<Shot>())
                {
                    ref var local = ref world.Local(s.Id);
                    local.Position2D += s.Velocity * dt;
                    if (--s.Life == 0) _deadShots.Add(s);
                }
                t = Lap(1, t);
                foreach (var c in _deadChasers)
                {
                    world.Destroy(c.Id);
                    if (pooled) _chasers.Push(c);
                }
                foreach (var s in _deadShots)
                {
                    world.Destroy(s.Id);
                    if (pooled) _shots.Push(s);
                }
                for (int i = _deadChasers.Count; i > 0; i--) SpawnChaser(Next++);
                for (int i = _deadShots.Count; i > 0; i--) SpawnShot(Next++, target);
                _deadChasers.Clear();
                _deadShots.Clear();
                Lap(2, t);
                break;
            }
        }
        Steps++;
    }

    public override void Render(IRenderer r)
    {
        var world = Objects;
        var ball = Bench.Ball;
        switch (Workload)
        {
            case Workload.Move:
                foreach (var m in world.All<Mover>()) r.Draw(ball, world.DrawPosition2D(m.Id));
                break;
            case Workload.Mixed:
                foreach (var c in world.All<Chaser>()) r.Draw(ball, world.DrawPosition2D(c.Id));
                foreach (var s in world.All<Shot>()) r.Draw(ball, world.DrawPosition2D(s.Id));
                break;
        }
    }

    protected override long Sum()
    {
        var world = Objects;
        long sum = 0;
        switch (Workload)
        {
            case Workload.Move:
            case Workload.Churn:
                foreach (var m in world.All<Mover>()) sum += Rules.HashOf(world.Local(m.Id).Position2D, m.Life);
                break;
            case Workload.Query:
                // Every entity has a Motion, a Vitals or neither: the ones with neither are reached through Burn or not
                // at all, so the sum walks the three kinds apart (a position is counted once per attachment it has).
                foreach (var m in world.All<Motion>()) sum += Rules.HashOf(world.Local(m.Id).Position2D, 1);
                foreach (var v in world.All<Vitals>()) sum += Rules.HashOf(world.Local(v.Id).Position2D, 2) + v.Value;
                foreach (var b in world.All<Burn>()) sum += Rules.HashOf(world.Local(b.Id).Position2D, 3) + b.Damage;
                break;
            default:
                foreach (var c in world.All<Chaser>()) sum += Rules.HashOf(world.Local(c.Id).Position2D, c.Health);
                foreach (var s in world.All<Shot>()) sum += Rules.HashOf(world.Local(s.Id).Position2D, s.Life);
                break;
        }
        return sum;
    }
}
