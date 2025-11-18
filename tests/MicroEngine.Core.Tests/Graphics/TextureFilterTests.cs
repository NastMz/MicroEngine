using MicroEngine.Core.Graphics;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Graphics;

public sealed class TextureFilterTests
{
    private sealed class TestTexture : ITexture
    {
        private bool _hasMipmaps;
        private int _mipmapCount = 1;

        public ResourceId Id { get; init; }
        public string Path { get; init; } = string.Empty;
        public bool IsLoaded { get; init; } = true;
        public long SizeInBytes { get; init; }
        public ResourceMetadata? Metadata { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public string Format { get; init; } = "RGBA32";
        public TextureFilter Filter { get; set; }
        public bool HasMipmaps => _hasMipmaps;
        public int MipmapCount => _mipmapCount;
        
        public void GenerateMipmaps() 
        { 
            if (_hasMipmaps)
            {
                return; // Don't regenerate if already has mipmaps
            }

            _hasMipmaps = true;
            _mipmapCount = CalculateMipmapLevels(Width, Height);
        }

        public void SetPreloadedMipmaps(int count)
        {
            _mipmapCount = count;
            _hasMipmaps = count > 1;
        }

        private static int CalculateMipmapLevels(int width, int height)
        {
            int maxDim = System.Math.Max(width, height);
            return (int)System.Math.Floor(System.Math.Log2(maxDim)) + 1;
        }

        public void Dispose() { }
    }

    [Fact]
    public void TextureFilter_DefaultValue_ShouldBePoint()
    {
        // Arrange
        var texture = new TestTexture();

        // Act
        var filter = texture.Filter;

        // Assert
        Assert.Equal(TextureFilter.Point, filter);
    }

    [Fact]
    public void TextureFilter_CanBeSetToBilinear()
    {
        // Arrange
        var texture = new TestTexture();

        // Act
        texture.Filter = TextureFilter.Bilinear;

        // Assert
        Assert.Equal(TextureFilter.Bilinear, texture.Filter);
    }

    [Fact]
    public void TextureFilter_CanBeSetToTrilinear()
    {
        // Arrange
        var texture = new TestTexture();

        // Act
        texture.Filter = TextureFilter.Trilinear;

        // Assert
        Assert.Equal(TextureFilter.Trilinear, texture.Filter);
    }

    [Fact]
    public void TextureFilter_CanBeSetToAnisotropic()
    {
        // Arrange
        var texture = new TestTexture();

        // Act
        texture.Filter = TextureFilter.Anisotropic16X;

        // Assert
        Assert.Equal(TextureFilter.Anisotropic16X, texture.Filter);
    }

    [Fact]
    public void TextureFilter_CanBeChangedMultipleTimes()
    {
        // Arrange
        var texture = new TestTexture();

        // Act & Assert
        texture.Filter = TextureFilter.Point;
        Assert.Equal(TextureFilter.Point, texture.Filter);

        texture.Filter = TextureFilter.Bilinear;
        Assert.Equal(TextureFilter.Bilinear, texture.Filter);

        texture.Filter = TextureFilter.Trilinear;
        Assert.Equal(TextureFilter.Trilinear, texture.Filter);

        texture.Filter = TextureFilter.Anisotropic16X;
        Assert.Equal(TextureFilter.Anisotropic16X, texture.Filter);

        texture.Filter = TextureFilter.Point;
        Assert.Equal(TextureFilter.Point, texture.Filter);
    }

    [Theory]
    [InlineData(TextureFilter.Point)]
    [InlineData(TextureFilter.Bilinear)]
    [InlineData(TextureFilter.Trilinear)]
    [InlineData(TextureFilter.Anisotropic16X)]
    public void TextureFilter_AllValidValues_CanBeSet(TextureFilter filter)
    {
        // Arrange
        var texture = new TestTexture();

        // Act
        texture.Filter = filter;

        // Assert
        Assert.Equal(filter, texture.Filter);
    }

    [Fact]
    public void TextureFilter_EnumValues_ShouldBeSequential()
    {
        // Assert - Verify enum values are as expected
        Assert.Equal(0, (int)TextureFilter.Point);
        Assert.Equal(1, (int)TextureFilter.Bilinear);
        Assert.Equal(2, (int)TextureFilter.Trilinear);
        Assert.Equal(3, (int)TextureFilter.Anisotropic4X);
        Assert.Equal(4, (int)TextureFilter.Anisotropic8X);
        Assert.Equal(5, (int)TextureFilter.Anisotropic16X);
    }

    [Fact]
    public void TextureFilter_ToString_ShouldReturnFilterName()
    {
        // Act & Assert
        Assert.Equal("Point", TextureFilter.Point.ToString());
        Assert.Equal("Bilinear", TextureFilter.Bilinear.ToString());
        Assert.Equal("Trilinear", TextureFilter.Trilinear.ToString());
        Assert.Equal("Anisotropic4X", TextureFilter.Anisotropic4X.ToString());
        Assert.Equal("Anisotropic8X", TextureFilter.Anisotropic8X.ToString());
        Assert.Equal("Anisotropic16X", TextureFilter.Anisotropic16X.ToString());
    }

    [Fact]
    public void Texture_WithoutMipmaps_HasMipmapsShouldBeFalse()
    {
        // Arrange
        var texture = new TestTexture { Width = 256, Height = 256 };

        // Assert
        Assert.False(texture.HasMipmaps);
        Assert.Equal(1, texture.MipmapCount);
    }

    [Fact]
    public void Texture_GenerateMipmaps_ShouldSetHasMipmapsTrue()
    {
        // Arrange
        var texture = new TestTexture { Width = 256, Height = 256 };

        // Act
        texture.GenerateMipmaps();

        // Assert
        Assert.True(texture.HasMipmaps);
        Assert.Equal(9, texture.MipmapCount); // 256 -> log2(256) + 1 = 9 levels
    }

    [Fact]
    public void Texture_WithPreloadedMipmaps_ShouldDetectThem()
    {
        // Arrange
        var texture = new TestTexture { Width = 512, Height = 512 };
        
        // Act - Simulate texture loaded with mipmaps from file
        texture.SetPreloadedMipmaps(10);

        // Assert
        Assert.True(texture.HasMipmaps);
        Assert.Equal(10, texture.MipmapCount);
    }

    [Fact]
    public void Texture_GenerateMipmaps_ShouldNotOverwriteExisting()
    {
        // Arrange
        var texture = new TestTexture { Width = 256, Height = 256 };
        texture.SetPreloadedMipmaps(5); // Simulate preloaded mipmaps

        // Act
        texture.GenerateMipmaps(); // Should not regenerate

        // Assert
        Assert.True(texture.HasMipmaps);
        Assert.Equal(5, texture.MipmapCount); // Should preserve preloaded count
    }

    [Theory]
    [InlineData(64, 64, 7)]    // 64 -> log2(64) + 1 = 7
    [InlineData(128, 128, 8)]  // 128 -> log2(128) + 1 = 8
    [InlineData(256, 256, 9)]  // 256 -> log2(256) + 1 = 9
    [InlineData(512, 512, 10)] // 512 -> log2(512) + 1 = 10
    [InlineData(1024, 1024, 11)] // 1024 -> log2(1024) + 1 = 11
    public void Texture_MipmapCount_ShouldMatchTextureSize(int width, int height, int expectedLevels)
    {
        // Arrange
        var texture = new TestTexture { Width = width, Height = height };

        // Act
        texture.GenerateMipmaps();

        // Assert
        Assert.Equal(expectedLevels, texture.MipmapCount);
    }
}
