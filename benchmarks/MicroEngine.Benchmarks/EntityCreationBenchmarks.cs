using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroEngine.Core.ECS;

namespace MicroEngine.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, iterationCount: 15, warmupCount: 3)]
public class EntityCreationBenchmarks
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

    [Params(100, 1000, 5000)]
    public int EntityCount;

    [IterationSetup]
    public void Setup()
    {
        _world = new World();
    }

    [Benchmark(Baseline = true)]
    public void CreateEntities_NoComponents()
    {
        for (int i = 0; i < EntityCount; i++)
        {
            _world!.CreateEntity();
        }
    }

    [Benchmark]
    public void CreateEntities_OneComponent()
    {
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world!.CreateEntity();
            _world.AddComponent(entity, new Position { X = i, Y = i });
        }
    }

    [Benchmark]
    public void CreateEntities_TwoComponents()
    {
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world!.CreateEntity();
            _world.AddComponent(entity, new Position { X = i, Y = i });
            _world.AddComponent(entity, new Velocity { Vx = i, Vy = i });
        }
    }

    [Benchmark]
    public void CreateEntities_ThreeComponents()
    {
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world!.CreateEntity();
            _world.AddComponent(entity, new Position { X = i, Y = i });
            _world.AddComponent(entity, new Velocity { Vx = i, Vy = i });
            _world.AddComponent(entity, new Health { Value = 100 });
        }
    }

    [Benchmark]
    public void DestroyEntities()
    {
        var entities = new List<Entity>();
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world!.CreateEntity();
            _world.AddComponent(entity, new Position { X = i, Y = i });
            entities.Add(entity);
        }

        foreach (var entity in entities)
        {
            _world!.DestroyEntity(entity);
        }

        // Process destruction
        _world!.Update(0.016f);
    }
}
