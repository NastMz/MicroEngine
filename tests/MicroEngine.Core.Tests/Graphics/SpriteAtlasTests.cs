using MicroEngine.Core.Graphics;
using MicroEngine.Core.Resources;
using Xunit;

namespace MicroEngine.Core.Tests.Graphics;

public sealed class SpriteAtlasTests
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
        public TextureFilter Filter { get; set; }
        public bool HasMipmaps => false;
        public int MipmapCount => 1;
        public void GenerateMipmaps() { }
        public void Dispose() { }
    }

    private readonly ITexture _stubTexture;

    public SpriteAtlasTests()
    {
        _stubTexture = new StubTexture
        {
            Id = new ResourceId(1),
            Width = 256,
            Height = 256
        };
    }

    [Fact]
    public void Constructor_WithNullTexture_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SpriteAtlas(null!));
    }

    [Fact]
    public void Constructor_SetsTexture()
    {
        // Act
        var atlas = new SpriteAtlas(_stubTexture);

        // Assert
        Assert.Same(_stubTexture, atlas.Texture);
        Assert.Equal(0, atlas.RegionCount);
    }

    [Fact]
    public void AddRegion_AddsRegionSuccessfully()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);
        var region = SpriteRegion.FromBounds(0, 0, 64, 64, "sprite1");

        // Act
        atlas.AddRegion("sprite1", region);

        // Assert
        Assert.Equal(1, atlas.RegionCount);
        Assert.True(atlas.HasRegion("sprite1"));
    }

    [Fact]
    public void AddRegion_WithNullOrEmptyName_ThrowsException()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);
        var region = SpriteRegion.FromBounds(0, 0, 64, 64);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => atlas.AddRegion("", region));
        Assert.Throws<ArgumentException>(() => atlas.AddRegion(null!, region));
    }

    [Fact]
    public void AddRegion_WithBounds_CreatesRegion()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);

        // Act
        atlas.AddRegion("sprite1", 10f, 20f, 64f, 64f);

        // Assert
        var region = atlas.GetRegion("sprite1");
        Assert.Equal(10f, region.Source.X);
        Assert.Equal(20f, region.Source.Y);
        Assert.Equal(64f, region.Source.Width);
        Assert.Equal(64f, region.Source.Height);
    }

    [Fact]
    public void GetRegion_WithExistingName_ReturnsRegion()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);
        var region = SpriteRegion.FromBounds(0, 0, 64, 64, "sprite1");
        atlas.AddRegion("sprite1", region);

        // Act
        var retrieved = atlas.GetRegion("sprite1");

        // Assert
        Assert.Equal(region, retrieved);
    }

    [Fact]
    public void GetRegion_WithNonExistingName_ThrowsException()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => atlas.GetRegion("nonexistent"));
    }

    [Fact]
    public void TryGetRegion_WithExistingName_ReturnsTrue()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);
        var region = SpriteRegion.FromBounds(0, 0, 64, 64, "sprite1");
        atlas.AddRegion("sprite1", region);

        // Act
        var result = atlas.TryGetRegion("sprite1", out var retrieved);

        // Assert
        Assert.True(result);
        Assert.Equal(region, retrieved);
    }

    [Fact]
    public void TryGetRegion_WithNonExistingName_ReturnsFalse()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);

        // Act
        var result = atlas.TryGetRegion("nonexistent", out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveRegion_RemovesRegion()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);
        atlas.AddRegion("sprite1", 0, 0, 64, 64);

        // Act
        var removed = atlas.RemoveRegion("sprite1");

        // Assert
        Assert.True(removed);
        Assert.False(atlas.HasRegion("sprite1"));
        Assert.Equal(0, atlas.RegionCount);
    }

    [Fact]
    public void Clear_RemovesAllRegions()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);
        atlas.AddRegion("sprite1", 0, 0, 64, 64);
        atlas.AddRegion("sprite2", 64, 0, 64, 64);

        // Act
        atlas.Clear();

        // Assert
        Assert.Equal(0, atlas.RegionCount);
    }

    [Fact]
    public void GetRegionNames_ReturnsAllNames()
    {
        // Arrange
        var atlas = new SpriteAtlas(_stubTexture);
        atlas.AddRegion("sprite1", 0, 0, 64, 64);
        atlas.AddRegion("sprite2", 64, 0, 64, 64);

        // Act
        var names = atlas.GetRegionNames().ToList();

        // Assert
        Assert.Equal(2, names.Count);
        Assert.Contains("sprite1", names);
        Assert.Contains("sprite2", names);
    }

    [Fact]
    public void CreateGrid_CreatesCorrectNumberOfFrames()
    {
        // Arrange
        const int FRAME_WIDTH = 32;
        const int FRAME_HEIGHT = 32;
        const int EXPECTED_COLUMNS = 256 / 32;
        const int EXPECTED_ROWS = 256 / 32;
        const int EXPECTED_FRAMES = EXPECTED_COLUMNS * EXPECTED_ROWS;

        // Act
        var atlas = SpriteAtlas.CreateGrid(_stubTexture, FRAME_WIDTH, FRAME_HEIGHT);

        // Assert
        Assert.Equal(EXPECTED_FRAMES, atlas.RegionCount);
    }

    [Fact]
    public void CreateGrid_WithSpacing_CreatesCorrectFrames()
    {
        // Arrange
        const int FRAME_WIDTH = 30;
        const int FRAME_HEIGHT = 30;
        const int SPACING = 2;

        // 256 / (30 + 2) = 8 frames per row/column
        const int EXPECTED_FRAMES = 8 * 8;

        // Act
        var atlas = SpriteAtlas.CreateGrid(
            _stubTexture,
            FRAME_WIDTH,
            FRAME_HEIGHT,
            spacing: SPACING);

        // Assert
        Assert.Equal(EXPECTED_FRAMES, atlas.RegionCount);
    }

    [Fact]
    public void CreateGrid_WithMargin_CreatesCorrectFrames()
    {
        // Arrange
        const int FRAME_WIDTH = 32;
        const int FRAME_HEIGHT = 32;
        const int MARGIN = 16;

        // (256 - 16*2) / 32 = 7 frames per row/column
        const int EXPECTED_FRAMES = 7 * 7;

        // Act
        var atlas = SpriteAtlas.CreateGrid(
            _stubTexture,
            FRAME_WIDTH,
            FRAME_HEIGHT,
            margin: MARGIN);

        // Assert
        Assert.Equal(EXPECTED_FRAMES, atlas.RegionCount);
    }

    [Fact]
    public void CreateGrid_GeneratesSequentialFrameNames()
    {
        // Arrange & Act
        var atlas = SpriteAtlas.CreateGrid(_stubTexture, 32, 32, namePrefix: "anim");

        // Assert
        Assert.True(atlas.HasRegion("anim_0"));
        Assert.True(atlas.HasRegion("anim_1"));
        Assert.True(atlas.HasRegion("anim_63")); // Last frame (8x8 grid)
    }
}
