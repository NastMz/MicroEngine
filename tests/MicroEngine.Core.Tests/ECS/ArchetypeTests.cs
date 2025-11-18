using MicroEngine.Core.ECS;

namespace MicroEngine.Core.Tests.ECS;

public class ArchetypeTests
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

    [Fact]
    public void ArchetypeId_SameComponentTypes_ProducesSameHash()
    {
        var id1 = new ArchetypeId(new[] { typeof(Position), typeof(Velocity) });
        var id2 = new ArchetypeId(new[] { typeof(Position), typeof(Velocity) });

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ArchetypeId_DifferentOrder_ProducesSameHash()
    {
        var id1 = new ArchetypeId(new[] { typeof(Position), typeof(Velocity) });
        var id2 = new ArchetypeId(new[] { typeof(Velocity), typeof(Position) });

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ArchetypeId_DifferentComponentTypes_ProducesDifferentHash()
    {
        var id1 = new ArchetypeId(new[] { typeof(Position), typeof(Velocity) });
        var id2 = new ArchetypeId(new[] { typeof(Position), typeof(Health) });

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Archetype_AddEntity_StoresEntityAndComponents()
    {
        var archetype = new Archetype(
            new ArchetypeId(new[] { typeof(Position), typeof(Velocity) }),
            new[] { typeof(Position), typeof(Velocity) }
        );

        var entity = new Entity(1, 0);
        var components = new Dictionary<Type, object>
        {
            { typeof(Position), new Position { X = 10, Y = 20 } },
            { typeof(Velocity), new Velocity { Vx = 1, Vy = 2 } }
        };

        archetype.AddEntity(entity, components);

        Assert.Contains(entity, archetype.Entities);
        Assert.Single(archetype.Entities);
    }

    [Fact]
    public void Archetype_AddEntity_ThrowsIfEntityAlreadyExists()
    {
        var archetype = new Archetype(
            new ArchetypeId(new[] { typeof(Position) }),
            new[] { typeof(Position) }
        );

        var entity = new Entity(1, 0);
        var components = new Dictionary<Type, object>
        {
            { typeof(Position), new Position { X = 10, Y = 20 } }
        };

        archetype.AddEntity(entity, components);

        Assert.Throws<InvalidOperationException>(() => archetype.AddEntity(entity, components));
    }

    [Fact]
    public void Archetype_RemoveEntity_RemovesEntityAndComponents()
    {
        var archetype = new Archetype(
            new ArchetypeId(new[] { typeof(Position) }),
            new[] { typeof(Position) }
        );

        var entity = new Entity(1, 0);
        var components = new Dictionary<Type, object>
        {
            { typeof(Position), new Position { X = 10, Y = 20 } }
        };

        archetype.AddEntity(entity, components);
        archetype.RemoveEntity(entity);

        Assert.DoesNotContain(entity, archetype.Entities);
        Assert.Empty(archetype.Entities);
    }

    [Fact]
    public void Archetype_MatchesQuery_WithAllRequiredTypes_ReturnsTrue()
    {
        var archetype = new Archetype(
            new ArchetypeId(new[] { typeof(Position), typeof(Velocity), typeof(Health) }),
            new[] { typeof(Position), typeof(Velocity), typeof(Health) }
        );

        var requiredTypes = new HashSet<Type> { typeof(Position), typeof(Velocity) };

        Assert.True(archetype.MatchesQuery(requiredTypes));
    }

    [Fact]
    public void Archetype_MatchesQuery_WithoutAllRequiredTypes_ReturnsFalse()
    {
        var archetype = new Archetype(
            new ArchetypeId(new[] { typeof(Position) }),
            new[] { typeof(Position) }
        );

        var requiredTypes = new HashSet<Type> { typeof(Position), typeof(Velocity) };

        Assert.False(archetype.MatchesQuery(requiredTypes));
    }

    [Fact]
    public void ArchetypeManager_GetOrCreateArchetype_CreatesDifferentArchetypesForDifferentTypes()
    {
        var manager = new ArchetypeManager();

        var archetype1 = manager.GetOrCreateArchetype(new[] { typeof(Position), typeof(Velocity) });
        var archetype2 = manager.GetOrCreateArchetype(new[] { typeof(Position), typeof(Health) });

        Assert.NotSame(archetype1, archetype2);
    }

    [Fact]
    public void ArchetypeManager_GetOrCreateArchetype_ReusesSameArchetypeForSameTypes()
    {
        var manager = new ArchetypeManager();

        var archetype1 = manager.GetOrCreateArchetype(new[] { typeof(Position), typeof(Velocity) });
        var archetype2 = manager.GetOrCreateArchetype(new[] { typeof(Position), typeof(Velocity) });

        Assert.Same(archetype1, archetype2);
    }

    [Fact]
    public void ArchetypeManager_GetMatchingArchetypes_ReturnsOnlyMatchingArchetypes()
    {
        var manager = new ArchetypeManager();

        var archetype1 = manager.GetOrCreateArchetype(new[] { typeof(Position), typeof(Velocity) });
        var archetype2 = manager.GetOrCreateArchetype(new[] { typeof(Position), typeof(Health) });
        var archetype3 = manager.GetOrCreateArchetype(new[] { typeof(Velocity), typeof(Health) });

        var requiredTypes = new HashSet<Type> { typeof(Position) };
        var matchingArchetypes = manager.GetMatchingArchetypes(requiredTypes).ToList();

        Assert.Contains(archetype1, matchingArchetypes);
        Assert.Contains(archetype2, matchingArchetypes);
        Assert.DoesNotContain(archetype3, matchingArchetypes);
    }

    [Fact]
    public void World_WithArchetypes_QueriesUseArchetypesInternally()
    {
        var world = new World();
        var entity1 = world.CreateEntity("E1");
        var entity2 = world.CreateEntity("E2");
        var entity3 = world.CreateEntity("E3");

        world.AddComponent(entity1, new Position { X = 1, Y = 1 });
        world.AddComponent(entity1, new Velocity { Vx = 1, Vy = 1 });

        world.AddComponent(entity2, new Position { X = 2, Y = 2 });
        world.AddComponent(entity2, new Velocity { Vx = 2, Vy = 2 });

        world.AddComponent(entity3, new Position { X = 3, Y = 3 });

        var query = world.CreateCachedQuery<Position, Velocity>();

        Assert.Equal(2, query.Count);
        Assert.Contains(entity1, query.Entities);
        Assert.Contains(entity2, query.Entities);
        Assert.DoesNotContain(entity3, query.Entities);
    }

    [Fact]
    public void World_ArchetypeChanges_WhenComponentsAddedOrRemoved()
    {
        var world = new World();
        var entity = world.CreateEntity("E1");

        world.AddComponent(entity, new Position { X = 1, Y = 1 });

        var queryPosOnly = world.CreateCachedQuery<Position>();
        var queryPosVel = world.CreateCachedQuery<Position, Velocity>();

        Assert.Single(queryPosOnly.Entities);
        Assert.Empty(queryPosVel.Entities);

        world.AddComponent(entity, new Velocity { Vx = 1, Vy = 1 });

        Assert.Single(queryPosOnly.Entities);
        Assert.Single(queryPosVel.Entities);

        world.RemoveComponent<Velocity>(entity);

        Assert.Single(queryPosOnly.Entities);
        Assert.Empty(queryPosVel.Entities);
    }

    [Fact]
    public void World_DestroyEntity_RemovesFromArchetype()
    {
        var world = new World();
        var entity = world.CreateEntity("E1");

        world.AddComponent(entity, new Position { X = 1, Y = 1 });
        world.AddComponent(entity, new Velocity { Vx = 1, Vy = 1 });

        var query = world.CreateCachedQuery<Position, Velocity>();
        Assert.Single(query.Entities);

        world.DestroyEntity(entity);
        world.Update(0.016f);

        Assert.Empty(query.Entities);
    }
}
