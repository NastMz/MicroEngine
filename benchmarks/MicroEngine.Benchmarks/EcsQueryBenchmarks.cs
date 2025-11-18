using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroEngine.Core.ECS;

namespace MicroEngine.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, iterationCount: 15, warmupCount: 3)]
public class EcsQueryBenchmarks
{
    private struct Position : IComponent
    {
        public float X, Y;
    }

    private struct Velocity : IComponent
    {
        public float Vx, Vy;
    }

    private struct Health : IComponent
    {
        public int Value;
    }

    private struct Damage : IComponent
    {
        public int Value;
    }

    private World? _world;
    private CachedQuery? _positionQuery;
    private CachedQuery? _positionVelocityQuery;
    private CachedQuery? _positionVelocityHealthQuery;
    private CachedQuery? _allComponentsQuery;

    [Params(100, 1000, 5000, 10000)]
    public int EntityCount;

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();

        // Create entities with various component combinations
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity($"Entity_{i}");

            // All entities have Position
            _world.AddComponent(entity, new Position { X = i, Y = i * 2 });

            // 80% have Velocity
            if (i % 5 != 0)
            {
                _world.AddComponent(entity, new Velocity { Vx = i * 0.1f, Vy = i * 0.2f });
            }

            // 60% have Health
            if (i % 5 < 3)
            {
                _world.AddComponent(entity, new Health { Value = 100 });
            }

            // 40% have Damage
            if (i % 5 < 2)
            {
                _world.AddComponent(entity, new Damage { Value = 10 });
            }
        }

        // Create cached queries
        _positionQuery = _world.CreateCachedQuery<Position>();
        _positionVelocityQuery = _world.CreateCachedQuery<Position, Velocity>();
        _positionVelocityHealthQuery = _world.CreateCachedQuery(typeof(Position), typeof(Velocity), typeof(Health));
        _allComponentsQuery = _world.CreateCachedQuery(typeof(Position), typeof(Velocity), typeof(Health), typeof(Damage));
    }

    [Benchmark(Baseline = true)]
    public int Query_SingleComponent()
    {
        int count = 0;
        foreach (var entity in _positionQuery!.Entities)
        {
            var pos = _world!.GetComponent<Position>(entity);
            count += (int)pos.X;
        }
        return count;
    }

    [Benchmark]
    public int Query_TwoComponents()
    {
        int count = 0;
        foreach (var entity in _positionVelocityQuery!.Entities)
        {
            var pos = _world!.GetComponent<Position>(entity);
            var vel = _world.GetComponent<Velocity>(entity);
            count += (int)(pos.X + vel.Vx);
        }
        return count;
    }

    [Benchmark]
    public int Query_ThreeComponents()
    {
        int count = 0;
        foreach (var entity in _positionVelocityHealthQuery!.Entities)
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int Query_FourComponents()
    {
        int count = 0;
        foreach (var entity in _allComponentsQuery!.Entities)
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public void Query_Refresh_SingleComponent()
    {
        _positionQuery!.Refresh();
    }

    [Benchmark]
    public void Query_Refresh_TwoComponents()
    {
        _positionVelocityQuery!.Refresh();
    }

    [Benchmark]
    public void Query_Refresh_ThreeComponents()
    {
        _positionVelocityHealthQuery!.Refresh();
    }

    [Benchmark]
    public void Query_Refresh_FourComponents()
    {
        _allComponentsQuery!.Refresh();
    }
}
