using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using Xunit;

namespace MicroEngine.Core.Tests.Graphics;

public sealed class SpriteRegionTests
{
    [Fact]
    public void Constructor_WithSourceAndPivot_SetsProperties()
    {
        // Arrange
        var source = new Rectangle(10, 20, 100, 50);
        var pivot = new Vector2(0.5f, 0.5f);

        // Act
        var region = new SpriteRegion(source, pivot, "test");

        // Assert
        Assert.Equal(source, region.Source);
        Assert.Equal(pivot, region.Pivot);
        Assert.Equal("test", region.Name);
    }

    [Fact]
    public void Constructor_WithSourceOnly_UsesCenterPivot()
    {
        // Arrange
        var source = new Rectangle(0, 0, 64, 64);

        // Act
        var region = new SpriteRegion(source);

        // Assert
        Assert.Equal(new Vector2(0.5f, 0.5f), region.Pivot);
    }

    [Fact]
    public void FromBounds_CreatesSpriteRegion()
    {
        // Act
        var region = SpriteRegion.FromBounds(10f, 20f, 100f, 50f, "bounds");

        // Assert
        Assert.Equal(10f, region.Source.X);
        Assert.Equal(20f, region.Source.Y);
        Assert.Equal(100f, region.Source.Width);
        Assert.Equal(50f, region.Source.Height);
        Assert.Equal("bounds", region.Name);
    }

    [Fact]
    public void FullTexture_CreatesFullSizeRegion()
    {
        // Act
        var region = SpriteRegion.FullTexture(256, 128);

        // Assert
        Assert.Equal(0f, region.Source.X);
        Assert.Equal(0f, region.Source.Y);
        Assert.Equal(256f, region.Source.Width);
        Assert.Equal(128f, region.Source.Height);
        Assert.Equal("Full", region.Name);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var region1 = new SpriteRegion(new Rectangle(0, 0, 64, 64), "test");
        var region2 = new SpriteRegion(new Rectangle(0, 0, 64, 64), "test");

        // Act & Assert
        Assert.Equal(region1, region2);
        Assert.True(region1 == region2);
        Assert.False(region1 != region2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var region1 = new SpriteRegion(new Rectangle(0, 0, 64, 64), "test1");
        var region2 = new SpriteRegion(new Rectangle(0, 0, 32, 32), "test2");

        // Act & Assert
        Assert.NotEqual(region1, region2);
        Assert.False(region1 == region2);
        Assert.True(region1 != region2);
    }

    [Fact]
    public void GetHashCode_IsConsistent()
    {
        // Arrange
        var region = new SpriteRegion(new Rectangle(10, 20, 100, 50), "test");

        // Act
        var hash1 = region.GetHashCode();
        var hash2 = region.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ToString_ReturnsDescriptiveString()
    {
        // Arrange
        var region = new SpriteRegion(new Rectangle(0, 0, 64, 64), "player");

        // Act
        var str = region.ToString();

        // Assert
        Assert.Contains("player", str);
        Assert.Contains("SpriteRegion", str);
    }
}
