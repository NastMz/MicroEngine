using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;

namespace MicroEngine.Core.Tests.Graphics;

public class TilemapTests
{
    [Fact]
    public void Constructor_CreatesEmptyTilemap()
    {
        // Arrange & Act
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Assert
        Assert.Equal(10, tilemap.Width);
        Assert.Equal(8, tilemap.Height);
        Assert.Equal(32, tilemap.TileWidth);
        Assert.Equal(32, tilemap.TileHeight);
    }

    [Fact]
    public void Constructor_WithInvalidDimensions_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new Tilemap(0, 8, 32, 32));
        Assert.Throws<ArgumentException>(() => new Tilemap(10, 0, 32, 32));
        Assert.Throws<ArgumentException>(() => new Tilemap(10, 8, 0, 32));
        Assert.Throws<ArgumentException>(() => new Tilemap(10, 8, 32, 0));
    }

    [Fact]
    public void SetTile_WithValidCoordinates_SetsTile()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act
        tilemap.SetTile(5, 4, 42);

        // Assert
        Assert.Equal(42, tilemap.GetTile(5, 4));
    }

    [Fact]
    public void SetTile_WithInvalidCoordinates_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => tilemap.SetTile(-1, 4, 42));
        Assert.Throws<ArgumentOutOfRangeException>(() => tilemap.SetTile(5, -1, 42));
        Assert.Throws<ArgumentOutOfRangeException>(() => tilemap.SetTile(10, 4, 42));
        Assert.Throws<ArgumentOutOfRangeException>(() => tilemap.SetTile(5, 8, 42));
    }

    [Fact]
    public void GetTile_WithInvalidCoordinates_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => tilemap.GetTile(-1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => tilemap.GetTile(5, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tilemap.GetTile(10, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => tilemap.GetTile(5, 8));
    }

    [Fact]
    public void GetTile_WithEmptyTile_ReturnsZero()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act
        int tileId = tilemap.GetTile(5, 4);

        // Assert
        Assert.Equal(0, tileId);
    }

    [Fact]
    public void Clear_RemovesAllTiles()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);
        tilemap.SetTile(5, 4, 42);
        tilemap.SetTile(2, 3, 13);

        // Act
        tilemap.Clear();

        // Assert
        Assert.Equal(0, tilemap.GetTile(5, 4));
        Assert.Equal(0, tilemap.GetTile(2, 3));
    }

    [Fact]
    public void WorldToTile_ConvertsWorldPositionToTileCoordinates()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act
        var (x, y) = tilemap.WorldToTile(new Vector2(96, 64));

        // Assert
        Assert.Equal(3, x);
        Assert.Equal(2, y);
    }

    [Fact]
    public void TileToWorld_ConvertsTileCoordinatesToWorldPosition()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act
        Vector2 position = tilemap.TileToWorld(3, 2);

        // Assert
        Assert.Equal(96, position.X);
        Assert.Equal(64, position.Y);
    }

    [Fact]
    public void Fill_FillsRectangleWithTileId()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act
        tilemap.Fill(2, 3, 4, 2, 99);

        // Assert
        for (int y = 3; y < 5; y++)
        {
            for (int x = 2; x < 6; x++)
            {
                Assert.Equal(99, tilemap.GetTile(x, y));
            }
        }
    }

    [Fact]
    public void IsEmpty_ReturnsTrueForEmptyTile()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act & Assert
        Assert.True(tilemap.IsEmpty(5, 4));
    }

    [Fact]
    public void IsEmpty_ReturnsFalseForNonEmptyTile()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);
        tilemap.SetTile(5, 4, 42);

        // Act & Assert
        Assert.False(tilemap.IsEmpty(5, 4));
    }

    [Fact]
    public void TotalTileCount_ReturnsCorrectCount()
    {
        // Arrange
        var tilemap = new Tilemap(10, 8, 32, 32);

        // Act & Assert
        Assert.Equal(80, tilemap.TotalTileCount);
    }
}
