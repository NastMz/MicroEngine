using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroEngine.Core.ECS;

namespace MicroEngine.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, iterationCount: 15, warmupCount: 3)]
public class ComponentOperationBenchmarks
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

    private World? _world;
    private List<Entity> _entities = new();

    [Params(1000, 5000)]
    public int EntityCount;

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _entities = new List<Entity>();

        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new Position { X = i, Y = i });
            _world.AddComponent(entity, new Velocity { Vx = i, Vy = i });
            _entities.Add(entity);
        }
    }

    [Benchmark(Baseline = true)]
    public void GetComponent_Position()
    {
        foreach (var entity in _entities)
        {
            ref var pos = ref _world!.GetComponent<Position>(entity);
            pos.X += 1;
        }
    }

    [Benchmark]
    public void GetComponent_TwoComponents()
    {
        foreach (var entity in _entities)
        {
            ref var pos = ref _world!.GetComponent<Position>(entity);
            ref var vel = ref _world.GetComponent<Velocity>(entity);
            pos.X += vel.Vx;
        }
    }

    [Benchmark]
    public void HasComponent_Check()
    {
        int count = 0;
        foreach (var entity in _entities)
        {
            if (_world!.HasComponent<Position>(entity))
            {
                count++;
            }
        }
    }

    [Benchmark]
    public void AddRemoveComponent()
    {
        foreach (var entity in _entities.Take(100))
        {
            _world!.AddComponent(entity, new Health { Value = 100 });
            _world.RemoveComponent<Health>(entity);
        }
    }

    [Benchmark]
    public void ModifyComponents_System()
    {
        var query = _world!.CreateCachedQuery<Position, Velocity>();
        foreach (var entity in query.Entities)
        {
            ref var pos = ref _world.GetComponent<Position>(entity);
            var vel = _world.GetComponent<Velocity>(entity);
            pos.X += vel.Vx * 0.016f;
            pos.Y += vel.Vy * 0.016f;
        }
    }
}
