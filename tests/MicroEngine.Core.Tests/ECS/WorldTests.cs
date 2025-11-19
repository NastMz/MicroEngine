using MicroEngine.Core.ECS;
using MicroEngine.Core.Exceptions;

namespace MicroEngine.Core.Tests.ECS;

public class WorldTests
{
    private readonly struct TestComponent : IComponent
    {
        public int Value { get; init; }
    }

    private readonly struct PositionComponent : IComponent
    {
        public float X { get; init; }
        public float Y { get; init; }
    }

    [Fact]
    public void World_CreateEntity_ReturnsValidEntity()
    {
        var world = new World();
        var entity = world.CreateEntity();

        Assert.False(entity.IsNull);
        Assert.True(world.IsEntityValid(entity));
        Assert.Equal(1, world.EntityCount);
    }

    [Fact]
    public void World_CreateEntity_WithName_StoresName()
    {
        var world = new World();
        var entity = world.CreateEntity("Player");

        Assert.Equal("Player", world.GetEntityName(entity));
    }

    [Fact]
    public void World_CreateMultipleEntities_IncrementIds()
    {
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        Assert.NotEqual(entity1.Id, entity2.Id);
        Assert.Equal(2, world.EntityCount);
    }

    [Fact]
    public void World_DestroyEntity_InvalidatesEntity()
    {
        var world = new World();
        var entity = world.CreateEntity();

        world.DestroyEntity(entity);
        world.Update(0f);

        Assert.False(world.IsEntityValid(entity));
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void World_DestroyEntity_IncrementsVersion()
    {
        var world = new World();
        var entity1 = world.CreateEntity();

        world.DestroyEntity(entity1);
        world.Update(0f);

        var entity2 = world.CreateEntity();

        // New entity gets next ID, not reused ID
        Assert.NotEqual(entity1.Id, entity2.Id);
        Assert.Equal(0u, entity1.Version);
        Assert.Equal(0u, entity2.Version);
    }

    [Fact]
    public void World_AddComponent_StoresComponent()
    {
        var world = new World();
        var entity = world.CreateEntity();
        var component = new TestComponent { Value = 42 };

        world.AddComponent(entity, component);

        Assert.True(world.HasComponent<TestComponent>(entity));
    }

    [Fact]
    public void World_AddComponent_ThrowsForInvalidEntity()
    {
        var world = new World();
        var entity = Entity.Null;
        var component = new TestComponent { Value = 42 };

        var exception = Assert.Throws<InvalidEntityOperationException>(() => world.AddComponent(entity, component));

        Assert.Equal("ECS-400", exception.ErrorCode);
    }

    [Fact]
    public void World_GetComponent_ReturnsCorrectComponent()
    {
        var world = new World();
        var entity = world.CreateEntity();
        var component = new TestComponent { Value = 42 };

        world.AddComponent(entity, component);
        ref var retrieved = ref world.GetComponent<TestComponent>(entity);

        Assert.Equal(42, retrieved.Value);
    }

    [Fact]
    public void World_GetComponent_AllowsModification()
    {
        var world = new World();
        var entity = world.CreateEntity();
        var component = new TestComponent { Value = 10 };

        world.AddComponent(entity, component);
        ref var retrieved = ref world.GetComponent<TestComponent>(entity);
        retrieved = new TestComponent { Value = 20 };

        ref var modified = ref world.GetComponent<TestComponent>(entity);
        Assert.Equal(20, modified.Value);
    }

    [Fact]
    public void World_TryGetComponent_ReturnsTrue_WhenComponentExists()
    {
        var world = new World();
        var entity = world.CreateEntity();
        var component = new TestComponent { Value = 42 };

        world.AddComponent(entity, component);
        bool found = world.TryGetComponent(entity, out TestComponent retrieved);

        Assert.True(found);
        Assert.Equal(42, retrieved.Value);
    }

    [Fact]
    public void World_TryGetComponent_ReturnsFalse_WhenComponentMissing()
    {
        var world = new World();
        var entity = world.CreateEntity();

        bool found = world.TryGetComponent(entity, out TestComponent _);

        Assert.False(found);
    }

    [Fact]
    public void World_RemoveComponent_RemovesComponent()
    {
        var world = new World();
        var entity = world.CreateEntity();
        var component = new TestComponent { Value = 42 };

        world.AddComponent(entity, component);
        world.RemoveComponent<TestComponent>(entity);

        Assert.False(world.HasComponent<TestComponent>(entity));
    }

    [Fact]
    public void World_HasComponent_ReturnsCorrectValue()
    {
        var world = new World();
        var entity = world.CreateEntity();

        Assert.False(world.HasComponent<TestComponent>(entity));

        world.AddComponent(entity, new TestComponent { Value = 42 });

        Assert.True(world.HasComponent<TestComponent>(entity));
    }

    [Fact]
    public void World_DestroyEntity_RemovesAllComponents()
    {
        var world = new World();
        var entity = world.CreateEntity();

        world.AddComponent(entity, new TestComponent { Value = 42 });
        world.AddComponent(entity, new PositionComponent { X = 1f, Y = 2f });

        world.DestroyEntity(entity);
        world.Update(0f);

        Assert.False(world.HasComponent<TestComponent>(entity));
        Assert.False(world.HasComponent<PositionComponent>(entity));
    }

    [Fact]
    public void World_GetEntitiesWith_ReturnsEntitiesWithComponent()
    {
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AddComponent(entity1, new TestComponent { Value = 1 });
        world.AddComponent(entity3, new TestComponent { Value = 3 });

        var entities = world.GetEntitiesWith<TestComponent>().ToList();

        Assert.Equal(2, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity3, entities);
        Assert.DoesNotContain(entity2, entities);
    }

    [Fact]
    public void World_GetEntitiesWith_ExcludesEntitiesPendingDestruction()
    {
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AddComponent(entity1, new TestComponent { Value = 1 });
        world.AddComponent(entity2, new TestComponent { Value = 2 });
        world.AddComponent(entity3, new TestComponent { Value = 3 });

        // Destroy entity2 but don't call Update yet (entity is pending destruction)
        world.DestroyEntity(entity2);

        var entities = world.GetEntitiesWith<TestComponent>().ToList();

        // Should only return valid entities (entity1 and entity3)
        Assert.Equal(2, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity3, entities);
        Assert.DoesNotContain(entity2, entities); // Pending destruction, should be filtered
    }

    [Fact]
    public void World_GetAllEntities_ReturnsAllActiveEntities()
    {
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.DestroyEntity(entity2);
        world.Update(0f);

        var entities = world.GetAllEntities().ToList();

        Assert.Equal(2, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity3, entities);
        Assert.DoesNotContain(entity2, entities);
    }

    [Fact]
    public void World_RegisterSystem_AddsSystem()
    {
        var world = new World();
        var system = new TestSystem();

        world.RegisterSystem(system);

        Assert.Equal(1, world.SystemCount);
    }

    [Fact]
    public void World_RegisterSystem_ThrowsForDuplicateSystem()
    {
        var world = new World();
        var system = new TestSystem();

        world.RegisterSystem(system);

        var exception = Assert.Throws<WorldException>(() => world.RegisterSystem(system));

        Assert.Equal("ECS-500", exception.ErrorCode);
        Assert.Contains("already registered", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void World_UnregisterSystem_RemovesSystem()
    {
        var world = new World();
        var system = new TestSystem();

        world.RegisterSystem(system);
        world.UnregisterSystem(system);

        Assert.Equal(0, world.SystemCount);
    }

    [Fact]
    public void World_Update_CallsSystemUpdate()
    {
        var world = new World();
        var system = new TestSystem();

        world.RegisterSystem(system);
        world.Update(0.016f);

        Assert.True(system.WasUpdated);
        Assert.Equal(0.016f, system.LastDeltaTime);
    }

    [Fact]
    public void World_Update_ProcessesDestroyedEntities()
    {
        var world = new World();
        var entity = world.CreateEntity();

        world.DestroyEntity(entity);
        Assert.Equal(1, world.EntityCount);

        world.Update(0f);
        Assert.Equal(0, world.EntityCount);
    }

    private class TestSystem : ISystem
    {
        public bool WasUpdated { get; private set; }
        public float LastDeltaTime { get; private set; }

        public void Update(World world, float deltaTime)
        {
            WasUpdated = true;
            LastDeltaTime = deltaTime;
        }
    }
}
