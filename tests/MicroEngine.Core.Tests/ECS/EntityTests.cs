using MicroEngine.Core.ECS;

namespace MicroEngine.Core.Tests.ECS;

public class EntityTests
{
    [Fact]
    public void Entity_DefaultConstructor_CreatesValidEntity()
    {
        var entity = new Entity(1, 0);

        Assert.Equal(1u, entity.Id);
        Assert.Equal(0u, entity.Version);
        Assert.False(entity.IsNull);
    }

    [Fact]
    public void Entity_NullEntity_IsNull()
    {
        var entity = Entity.Null;

        Assert.True(entity.IsNull);
        Assert.Equal(0u, entity.Id);
        Assert.Equal(0u, entity.Version);
    }

    [Fact]
    public void Entity_Equality_WorksCorrectly()
    {
        var entity1 = new Entity(1, 0);
        var entity2 = new Entity(1, 0);
        var entity3 = new Entity(1, 1);
        var entity4 = new Entity(2, 0);

        Assert.Equal(entity1, entity2);
        Assert.NotEqual(entity1, entity3);
        Assert.NotEqual(entity1, entity4);
    }

    [Fact]
    public void Entity_EqualityOperators_WorkCorrectly()
    {
        var entity1 = new Entity(1, 0);
        var entity2 = new Entity(1, 0);
        var entity3 = new Entity(2, 0);

        Assert.True(entity1 == entity2);
        Assert.False(entity1 != entity2);
        Assert.True(entity1 != entity3);
        Assert.False(entity1 == entity3);
    }

    [Fact]
    public void Entity_GetHashCode_ConsistentForEqualEntities()
    {
        var entity1 = new Entity(1, 0);
        var entity2 = new Entity(1, 0);

        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void Entity_ToString_ReturnsCorrectFormat()
    {
        var entity = new Entity(42, 3);
        var str = entity.ToString();

        Assert.Contains("42", str);
        Assert.Contains("3", str);
    }
}
