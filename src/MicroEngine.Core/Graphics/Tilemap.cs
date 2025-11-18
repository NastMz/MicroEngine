using MicroEngine.Core.Math;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Represents a 2D grid-based tilemap for efficient tile-based rendering.
/// Tilemaps store tile IDs in a grid structure where 0 represents an empty tile.
/// </summary>
public class Tilemap
{
    private readonly int[,] _tiles;

    /// <summary>
    /// Gets the width of the tilemap in tiles.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the tilemap in tiles.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the width of each tile in pixels.
    /// </summary>
    public int TileWidth { get; }

    /// <summary>
    /// Gets the height of each tile in pixels.
    /// </summary>
    public int TileHeight { get; }

    /// <summary>
    /// Gets the total number of tiles in the tilemap (Width × Height).
    /// </summary>
    public int TotalTileCount => Width * Height;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tilemap"/> class.
    /// </summary>
    /// <param name="width">Width of the tilemap in tiles.</param>
    /// <param name="height">Height of the tilemap in tiles.</param>
    /// <param name="tileWidth">Width of each tile in pixels.</param>
    /// <param name="tileHeight">Height of each tile in pixels.</param>
    /// <exception cref="ArgumentException">Thrown when any dimension is zero or negative.</exception>
    public Tilemap(int width, int height, int tileWidth, int tileHeight)
    {
        if (width <= 0)
        {
            throw new ArgumentException("Width must be greater than zero.", nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentException("Height must be greater than zero.", nameof(height));
        }

        if (tileWidth <= 0)
        {
            throw new ArgumentException("Tile width must be greater than zero.", nameof(tileWidth));
        }

        if (tileHeight <= 0)
        {
            throw new ArgumentException("Tile height must be greater than zero.", nameof(tileHeight));
        }

        Width = width;
        Height = height;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        _tiles = new int[width, height];
    }

    /// <summary>
    /// Sets the tile ID at the specified grid coordinates.
    /// </summary>
    /// <param name="x">X coordinate in the grid.</param>
    /// <param name="y">Y coordinate in the grid.</param>
    /// <param name="tileId">Tile ID to set (0 for empty).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are out of bounds.</exception>
    public void SetTile(int x, int y, int tileId)
    {
        ValidateCoordinates(x, y);
        _tiles[x, y] = tileId;
    }

    /// <summary>
    /// Gets the tile ID at the specified grid coordinates.
    /// </summary>
    /// <param name="x">X coordinate in the grid.</param>
    /// <param name="y">Y coordinate in the grid.</param>
    /// <returns>The tile ID at the specified position (0 for empty).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are out of bounds.</exception>
    public int GetTile(int x, int y)
    {
        ValidateCoordinates(x, y);
        return _tiles[x, y];
    }

    /// <summary>
    /// Clears all tiles in the tilemap, setting them to 0 (empty).
    /// </summary>
    public void Clear()
    {
        Array.Clear(_tiles, 0, _tiles.Length);
    }

    /// <summary>
    /// Converts world position to tile coordinates.
    /// </summary>
    /// <param name="worldPosition">Position in world space.</param>
    /// <returns>Tile coordinates (x, y).</returns>
    public (int x, int y) WorldToTile(Vector2 worldPosition)
    {
        int x = (int)(worldPosition.X / TileWidth);
        int y = (int)(worldPosition.Y / TileHeight);
        return (x, y);
    }

    /// <summary>
    /// Converts tile coordinates to world position (top-left corner of the tile).
    /// </summary>
    /// <param name="x">X coordinate in the grid.</param>
    /// <param name="y">Y coordinate in the grid.</param>
    /// <returns>World position of the tile's top-left corner.</returns>
    public Vector2 TileToWorld(int x, int y)
    {
        return new Vector2(x * TileWidth, y * TileHeight);
    }

    /// <summary>
    /// Fills a rectangular area with the specified tile ID.
    /// </summary>
    /// <param name="startX">Starting X coordinate.</param>
    /// <param name="startY">Starting Y coordinate.</param>
    /// <param name="width">Width of the area in tiles.</param>
    /// <param name="height">Height of the area in tiles.</param>
    /// <param name="tileId">Tile ID to fill with.</param>
    public void Fill(int startX, int startY, int width, int height, int tileId)
    {
        int endX = System.Math.Min(startX + width, Width);
        int endY = System.Math.Min(startY + height, Height);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    _tiles[x, y] = tileId;
                }
            }
        }
    }

    /// <summary>
    /// Checks if the tile at the specified coordinates is empty (tile ID = 0).
    /// </summary>
    /// <param name="x">X coordinate in the grid.</param>
    /// <param name="y">Y coordinate in the grid.</param>
    /// <returns>True if the tile is empty; otherwise, false.</returns>
    public bool IsEmpty(int x, int y)
    {
        ValidateCoordinates(x, y);
        return _tiles[x, y] == 0;
    }

    private void ValidateCoordinates(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"X must be between 0 and {Width - 1}.");
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), $"Y must be between 0 and {Height - 1}.");
        }
    }
}
