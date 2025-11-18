using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Resources;

public class ResourceIdTests
{
    [Fact]
    public void Constructor_CreatesValidId()
    {
        var id = new ResourceId(42);
        
        Assert.Equal(42u, id.Value);
    }

    [Fact]
    public void Invalid_HasZeroValue()
    {
        Assert.Equal(0u, ResourceId.Invalid.Value);
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var id1 = new ResourceId(123);
        var id2 = new ResourceId(123);
        
        Assert.Equal(id1, id2);
        Assert.True(id1.Equals(id2));
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var id1 = new ResourceId(123);
        var id2 = new ResourceId(456);
        
        Assert.NotEqual(id1, id2);
        Assert.False(id1.Equals(id2));
        Assert.True(id1 != id2);
        Assert.False(id1 == id2);
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHashCode()
    {
        var id1 = new ResourceId(123);
        var id2 = new ResourceId(123);
        
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var id = new ResourceId(42);
        
        Assert.Equal("ResourceId(42)", id.ToString());
    }
}
