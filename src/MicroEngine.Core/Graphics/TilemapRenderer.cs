using MicroEngine.Core.Math;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Renders a tilemap using a sprite atlas for tile textures.
/// Supports culling to only render visible tiles based on camera view.
/// </summary>
public class TilemapRenderer
{
    /// <summary>
    /// Gets the tilemap being rendered.
    /// </summary>
    public Tilemap Tilemap { get; }

    /// <summary>
    /// Gets the sprite atlas used for tile textures.
    /// </summary>
    public SpriteAtlas Atlas { get; }

    /// <summary>
    /// Gets or sets the pixel offset for rendering tiles.
    /// Useful for parallax scrolling or fine-tuning tile positions.
    /// </summary>
    public Vector2 TileOffset { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TilemapRenderer"/> class.
    /// </summary>
    /// <param name="tilemap">The tilemap to render.</param>
    /// <param name="atlas">The sprite atlas containing tile textures.</param>
    /// <exception cref="ArgumentNullException">Thrown when tilemap or atlas is null.</exception>
    public TilemapRenderer(Tilemap tilemap, SpriteAtlas atlas)
    {
        Tilemap = tilemap ?? throw new ArgumentNullException(nameof(tilemap));
        Atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        TileOffset = Vector2.Zero;
    }

    /// <summary>
    /// Sets the pixel offset for rendering tiles.
    /// </summary>
    /// <param name="offsetX">X offset in pixels.</param>
    /// <param name="offsetY">Y offset in pixels.</param>
    public void SetTileOffset(float offsetX, float offsetY)
    {
        TileOffset = new Vector2(offsetX, offsetY);
    }

    /// <summary>
    /// Calculates the visible tile bounds based on camera position and screen size.
    /// This is used for culling to only render visible tiles.
    /// </summary>
    /// <param name="camera">The camera viewing the tilemap.</param>
    /// <param name="screenWidth">Screen width in pixels.</param>
    /// <param name="screenHeight">Screen height in pixels.</param>
    /// <returns>Tile coordinates (startX, startY, endX, endY) for visible bounds.</returns>
    public (int startX, int startY, int endX, int endY) GetVisibleBounds(
        Camera2D camera,
        int screenWidth,
        int screenHeight)
    {
        // Calculate world bounds visible by camera
        var bounds = camera.GetVisibleBounds(screenWidth, screenHeight);

        // Convert world bounds to tile coordinates
        (int startX, int startY) = Tilemap.WorldToTile(new Vector2(bounds.X, bounds.Y));
        (int endX, int endY) = Tilemap.WorldToTile(
            new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height));

        // Clamp to tilemap bounds
        startX = System.Math.Max(0, startX);
        startY = System.Math.Max(0, startY);
        endX = System.Math.Min(Tilemap.Width, endX + 1);
        endY = System.Math.Min(Tilemap.Height, endY + 1);

        return (startX, startY, endX, endY);
    }

    /// <summary>
    /// Renders the tilemap using the provided sprite batch.
    /// Only renders tiles visible to the camera for performance.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to use for rendering.</param>
    /// <param name="camera">The camera viewing the tilemap.</param>
    /// <param name="screenWidth">Screen width in pixels.</param>
    /// <param name="screenHeight">Screen height in pixels.</param>
    public void Render(
        ISpriteBatch spriteBatch,
        Camera2D camera,
        int screenWidth,
        int screenHeight)
    {
        (int startX, int startY, int endX, int endY) = GetVisibleBounds(camera, screenWidth, screenHeight);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int tileId = Tilemap.GetTile(x, y);

                // Skip empty tiles (tile ID = 0)
                if (tileId == 0)
                {
                    continue;
                }

                // Calculate world position for this tile
                Vector2 worldPos = Tilemap.TileToWorld(x, y);
                worldPos = new Vector2(worldPos.X + TileOffset.X, worldPos.Y + TileOffset.Y);

                // Get sprite region from atlas by tile ID name
                string regionName = $"tile_{tileId}";

                if (Atlas.TryGetRegion(regionName, out SpriteRegion region))
                {
                    spriteBatch.DrawRegion(
                        Atlas.Texture,
                        worldPos,
                        region,
                        Color.White,
                        0f,
                        Vector2.One);
                }
            }
        }
    }
}
