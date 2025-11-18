using MicroEngine.Core.ECS;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Graphics;

public class TilemapRendererTests
{
    private sealed class FakeTexture : ITexture
    {
        public int Width { get; }
        public int Height { get; }
        public string Name { get; }
        public string Path { get; }
        public ResourceId Id { get; }
        public bool IsLoaded => true;
        public long SizeInBytes => Width * Height * 4;
        public ResourceMetadata? Metadata => null;
        public string Format => "RGBA";

        public FakeTexture(int width, int height)
        {
            Width = width;
            Height = height;
            Name = "test-texture";
            Path = "test.png";
            Id = new ResourceId(1);
        }

        public void Dispose() { }
    }

    private static SpriteAtlas CreateTestAtlas()
    {
        var texture = new FakeTexture(512, 512);
        return new SpriteAtlas(texture);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesTilemapRenderer()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);
        var atlas = CreateTestAtlas();

        // Act
        var renderer = new TilemapRenderer(tilemap, atlas);

        // Assert
        Assert.NotNull(renderer);
        Assert.Equal(tilemap, renderer.Tilemap);
        Assert.Equal(atlas, renderer.Atlas);
    }

    [Fact]
    public void Constructor_WithNullTilemap_ThrowsArgumentNullException()
    {
        // Arrange
        var atlas = CreateTestAtlas();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TilemapRenderer(null!, atlas));
    }

    [Fact]
    public void Constructor_WithNullAtlas_ThrowsArgumentNullException()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TilemapRenderer(tilemap, null!));
    }

    [Fact]
    public void SetTileOffset_UpdatesOffset()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);
        var atlas = CreateTestAtlas();
        var renderer = new TilemapRenderer(tilemap, atlas);

        // Act
        renderer.SetTileOffset(10, 5);

        // Assert
        Assert.Equal(new Vector2(10, 5), renderer.TileOffset);
    }

    [Fact]
    public void GetVisibleBounds_ReturnsCorrectBounds()
    {
        // Arrange
        var tilemap = new Tilemap(20, 15, 32, 32);
        var atlas = CreateTestAtlas();
        var renderer = new TilemapRenderer(tilemap, atlas);
        var camera = new Camera2D(new Vector2(320, 240));

        // Act
        (int startX, int startY, int endX, int endY) = renderer.GetVisibleBounds(camera, 800, 600);

        // Assert
        Assert.InRange(startX, 0, tilemap.Width);
        Assert.InRange(startY, 0, tilemap.Height);
        Assert.InRange(endX, startX, tilemap.Width);
        Assert.InRange(endY, startY, tilemap.Height);
    }
}
