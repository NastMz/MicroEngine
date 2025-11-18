using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;
using Xunit;

namespace MicroEngine.Core.Tests.Graphics;

public sealed class SpriteBatchItemTests
{
    private sealed class StubTexture : ITexture
    {
        public ResourceId Id { get; init; }
        public string Path { get; init; } = string.Empty;
        public bool IsLoaded { get; init; } = true;
        public long SizeInBytes { get; init; }
        public ResourceMetadata? Metadata { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public string Format { get; init; } = "RGBA32";
        public void Dispose() { }
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        // Arrange
        var texture = new StubTexture { Id = new ResourceId(1), Width = 64, Height = 64 };
        var position = new Vector2(100, 200);
        var region = SpriteRegion.FromBounds(0, 0, 32, 32);
        const float ROTATION = 1.5f;
        var scale = new Vector2(2f, 2f);
        var tint = new Color(255, 128, 0, 255);
        const float LAYER_DEPTH = 0.5f;

        // Act
        var item = new SpriteBatchItem(
            texture,
            position,
            region,
            ROTATION,
            scale,
            tint,
            LAYER_DEPTH);

        // Assert
        Assert.Same(texture, item.Texture);
        Assert.Equal(position, item.Position);
        Assert.Equal(region, item.Region);
        Assert.Equal(ROTATION, item.Rotation);
        Assert.Equal(scale, item.Scale);
        Assert.Equal(tint, item.Tint);
        Assert.Equal(LAYER_DEPTH, item.LayerDepth);
    }

    [Fact]
    public void Constructor_WithDefaults_UsesDefaultValues()
    {
        // Arrange
        var texture = new StubTexture { Id = new ResourceId(1), Width = 64, Height = 64 };
        var position = new Vector2(100, 200);
        var region = SpriteRegion.FromBounds(0, 0, 64, 64);

        // Act
        var item = new SpriteBatchItem(texture, position, region);

        // Assert
        Assert.Equal(Vector2.One, item.Scale);
        Assert.Equal(Color.White, item.Tint);
        Assert.Equal(0f, item.LayerDepth);
    }

    [Fact]
    public void Constructor_ClampsLayerDepth()
    {
        // Arrange
        var texture = new StubTexture { Id = new ResourceId(1), Width = 64, Height = 64 };
        var region = SpriteRegion.FromBounds(0, 0, 64, 64);

        // Act
        var itemBelow = new SpriteBatchItem(texture, Vector2.Zero, region, layerDepth: -0.5f);
        var itemAbove = new SpriteBatchItem(texture, Vector2.Zero, region, layerDepth: 1.5f);

        // Assert
        Assert.Equal(0f, itemBelow.LayerDepth);
        Assert.Equal(1f, itemAbove.LayerDepth);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var texture = new StubTexture { Id = new ResourceId(1), Width = 64, Height = 64 };
        var position = new Vector2(100, 200);
        var region = SpriteRegion.FromBounds(0, 0, 32, 32);

        var item1 = new SpriteBatchItem(texture, position, region);
        var item2 = new SpriteBatchItem(texture, position, region);

        // Act & Assert
        Assert.Equal(item1, item2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var texture = new StubTexture { Id = new ResourceId(1), Width = 64, Height = 64 };
        var region = SpriteRegion.FromBounds(0, 0, 64, 64);

        var item1 = new SpriteBatchItem(texture, new Vector2(100, 200), region);
        var item2 = new SpriteBatchItem(texture, new Vector2(200, 100), region);

        // Act & Assert
        Assert.NotEqual(item1, item2);
    }

    [Fact]
    public void GetHashCode_IsConsistent()
    {
        // Arrange
        var texture = new StubTexture { Id = new ResourceId(1), Width = 64, Height = 64 };
        var region = SpriteRegion.FromBounds(0, 0, 64, 64);
        var item = new SpriteBatchItem(texture, new Vector2(100, 200), region);

        // Act
        var hash1 = item.GetHashCode();
        var hash2 = item.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }
}
