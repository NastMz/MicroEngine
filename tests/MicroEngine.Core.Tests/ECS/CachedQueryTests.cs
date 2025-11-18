using MicroEngine.Core.ECS;

namespace MicroEngine.Core.Tests.ECS;

public sealed class CachedQueryTests
{
    private readonly struct Position : IComponent
    {
        public float X { get; init; }
        public float Y { get; init; }
    }

    private readonly struct Velocity : IComponent
    {
        public float VX { get; init; }
        public float VY { get; init; }
    }

    private readonly struct Health : IComponent
    {
        public int Value { get; init; }
    }

    [Fact]
    public void CreateCachedQuery_WithSingleComponent_ShouldCacheCorrectEntities()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AddComponent(entity1, new Position { X = 1, Y = 1 });
        world.AddComponent(entity2, new Position { X = 2, Y = 2 });
        world.AddComponent(entity3, new Velocity { VX = 1, VY = 1 });

        // Act
        var query = world.CreateCachedQuery<Position>();

        // Assert
        Assert.Equal(2, query.Entities.Count);
        Assert.Contains(entity1, query.Entities);
        Assert.Contains(entity2, query.Entities);
        Assert.DoesNotContain(entity3, query.Entities);
    }

    [Fact]
    public void CreateCachedQuery_WithMultipleComponents_ShouldCacheOnlyEntitiesWithAllComponents()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AddComponent(entity1, new Position { X = 1, Y = 1 });
        world.AddComponent(entity1, new Velocity { VX = 1, VY = 1 });

        world.AddComponent(entity2, new Position { X = 2, Y = 2 });

        world.AddComponent(entity3, new Velocity { VX = 2, VY = 2 });

        // Act
        var query = world.CreateCachedQuery<Position, Velocity>();

        // Assert
        Assert.Single(query.Entities);
        Assert.Contains(entity1, query.Entities);
        Assert.DoesNotContain(entity2, query.Entities);
        Assert.DoesNotContain(entity3, query.Entities);
    }

    [Fact]
    public void CachedQuery_AfterAddingComponent_ShouldInvalidateAndRefresh()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AddComponent(entity1, new Position { X = 1, Y = 1 });

        var query = world.CreateCachedQuery<Position>();

        // Initial state
        Assert.Single(query.Entities);
        Assert.Contains(entity1, query.Entities);

        // Act - Add component to entity2
        world.AddComponent(entity2, new Position { X = 2, Y = 2 });

        // Assert - Query should now include entity2
        Assert.Equal(2, query.Entities.Count);
        Assert.Contains(entity1, query.Entities);
        Assert.Contains(entity2, query.Entities);
    }

    [Fact]
    public void CachedQuery_AfterRemovingComponent_ShouldInvalidateAndRefresh()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AddComponent(entity1, new Position { X = 1, Y = 1 });
        world.AddComponent(entity2, new Position { X = 2, Y = 2 });

        var query = world.CreateCachedQuery<Position>();

        // Initial state
        Assert.Equal(2, query.Entities.Count);

        // Act - Remove component from entity1
        world.RemoveComponent<Position>(entity1);

        // Assert - Query should only include entity2
        Assert.Single(query.Entities);
        Assert.Contains(entity2, query.Entities);
        Assert.DoesNotContain(entity1, query.Entities);
    }

    [Fact]
    public void CachedQuery_Count_ShouldReturnCorrectCount()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AddComponent(entity1, new Position { X = 1, Y = 1 });
        world.AddComponent(entity2, new Position { X = 2, Y = 2 });
        world.AddComponent(entity3, new Velocity { VX = 1, VY = 1 });

        var query = world.CreateCachedQuery<Position>();

        // Act & Assert
        Assert.Equal(2, query.Count);
    }

    [Fact]
    public void CachedQuery_IsDirty_ShouldBeInitiallyTrue()
    {
        // Arrange
        var world = new World();
        var query = world.CreateCachedQuery<Position>();

        // Act & Assert
        Assert.True(query.IsDirty);
    }

    [Fact]
    public void CachedQuery_IsDirty_ShouldBeFalseAfterFirstAccess()
    {
        // Arrange
        var world = new World();
        var query = world.CreateCachedQuery<Position>();

        // Act - Access entities to trigger refresh
        _ = query.Entities;

        // Assert
        Assert.False(query.IsDirty);
    }

    [Fact]
    public void CachedQuery_IsDirty_ShouldBeTrueAfterComponentChange()
    {
        // Arrange
        var world = new World();
        var entity = world.CreateEntity();
        var query = world.CreateCachedQuery<Position>();

        // Access to clear dirty flag
        _ = query.Entities;
        Assert.False(query.IsDirty);

        // Act - Add component
        world.AddComponent(entity, new Position { X = 1, Y = 1 });

        // Assert
        Assert.True(query.IsDirty);
    }

    [Fact]
    public void CachedQuery_ManualInvalidate_ShouldMarkAsDirty()
    {
        // Arrange
        var world = new World();
        var query = world.CreateCachedQuery<Position>();

        // Access to clear dirty flag
        _ = query.Entities;
        Assert.False(query.IsDirty);

        // Act
        query.Invalidate();

        // Assert
        Assert.True(query.IsDirty);
    }

    [Fact]
    public void CachedQuery_ManualRefresh_ShouldUpdateResults()
    {
        // Arrange
        var world = new World();
        var entity = world.CreateEntity();
        var query = world.CreateCachedQuery<Position>();

        // Initial state (no entities)
        _ = query.Entities;
        Assert.Empty(query.Entities);

        // Manually mark dirty
        query.Invalidate();

        // Add component
        world.AddComponent(entity, new Position { X = 1, Y = 1 });

        // Act
        query.Refresh();

        // Assert
        Assert.Single(query.Entities);
        Assert.Contains(entity, query.Entities);
        Assert.False(query.IsDirty);
    }

    [Fact]
    public void CachedQuery_MultipleQueries_ShouldAllInvalidateOnComponentChange()
    {
        // Arrange
        var world = new World();
        var entity = world.CreateEntity();

        var queryPosition = world.CreateCachedQuery<Position>();
        var queryVelocity = world.CreateCachedQuery<Velocity>();
        var queryBoth = world.CreateCachedQuery<Position, Velocity>();

        // Access to clear dirty flags
        _ = queryPosition.Entities;
        _ = queryVelocity.Entities;
        _ = queryBoth.Entities;

        Assert.False(queryPosition.IsDirty);
        Assert.False(queryVelocity.IsDirty);
        Assert.False(queryBoth.IsDirty);

        // Act - Add component
        world.AddComponent(entity, new Position { X = 1, Y = 1 });

        // Assert - All queries should be dirty
        Assert.True(queryPosition.IsDirty);
        Assert.True(queryVelocity.IsDirty);
        Assert.True(queryBoth.IsDirty);
    }

    [Fact]
    public void CachedQuery_WithThreeComponents_ShouldCacheCorrectly()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AddComponent(entity1, new Position { X = 1, Y = 1 });
        world.AddComponent(entity1, new Velocity { VX = 1, VY = 1 });
        world.AddComponent(entity1, new Health { Value = 100 });

        world.AddComponent(entity2, new Position { X = 2, Y = 2 });
        world.AddComponent(entity2, new Velocity { VX = 2, VY = 2 });

        world.AddComponent(entity3, new Position { X = 3, Y = 3 });
        world.AddComponent(entity3, new Health { Value = 50 });

        // Act
        var query = world.CreateCachedQuery(typeof(Position), typeof(Velocity), typeof(Health));

        // Assert
        Assert.Single(query.Entities);
        Assert.Contains(entity1, query.Entities);
        Assert.DoesNotContain(entity2, query.Entities);
        Assert.DoesNotContain(entity3, query.Entities);
    }

    [Fact]
    public void CachedQuery_EmptyWorld_ShouldReturnEmptyList()
    {
        // Arrange
        var world = new World();
        var query = world.CreateCachedQuery<Position>();

        // Act
        var entities = query.Entities;

        // Assert
        Assert.Empty(entities);
        Assert.Equal(0, query.Count);
    }

    [Fact]
    public void CachedQuery_RepeatedAccess_ShouldNotRefreshIfNotDirty()
    {
        // Arrange
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position { X = 1, Y = 1 });

        var query = world.CreateCachedQuery<Position>();

        // Act - Multiple accesses
        var entities1 = query.Entities;
        var entities2 = query.Entities;
        var entities3 = query.Entities;

        // Assert - Should return same cached list
        Assert.Same(entities1, entities2);
        Assert.Same(entities2, entities3);
        Assert.False(query.IsDirty);
    }
}
