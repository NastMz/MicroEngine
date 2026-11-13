using MicroEngine.Graphics;

namespace MicroEngine.World;

/// <summary>Draws the visible part of a layer, asking the game which sprite each tile uses.</summary>
public static class TileRenderer
{
    /// <summary>Draws the non-zero tiles that can be seen, each at its tile position minus its sprite's origin.</summary>
    /// <param name="renderer">The renderer; push <paramref name="camera"/> on it first when there is one.</param>
    /// <param name="layer">The tile ids; 0 is never drawn.</param>
    /// <param name="tileSize">Tile size in pixels.</param>
    /// <param name="camera">The camera, used only to skip tiles outside its view; null draws the tiles over the screen area.</param>
    /// <param name="spriteFor">Returns the sprite for a tile id at tile (x, y), or null to draw nothing.</param>
    public static void Draw(IRenderer renderer, TileLayer layer, int tileSize, Camera2D? camera, Func<int, int, int, Sprite?> spriteFor) =>
        Draw(renderer, layer, tileSize, camera, spriteFor, null);

    /// <summary>
    /// Draws the non-zero tiles that can be seen, each at its tile position minus its sprite's origin, mirrored or
    /// turned as <paramref name="orientationAt"/> says.
    /// </summary>
    /// <param name="renderer">The renderer; push <paramref name="camera"/> on it first when there is one.</param>
    /// <param name="layer">The tile ids; 0 is never drawn.</param>
    /// <param name="tileSize">Tile size in pixels.</param>
    /// <param name="camera">The camera, used only to skip tiles outside what it shows (zoom and rotation included); null draws the tiles over the screen area.</param>
    /// <param name="spriteFor">Returns the sprite for a tile id at tile (x, y), or null to draw nothing.</param>
    /// <param name="orientationAt">The orientation of the tile at (x, y); null draws every tile as it is. A turned tile turns about its sprite's centre.</param>
    /// <param name="margin">Extra pixels around the view whose tiles are drawn too, for sprites larger than a cell.</param>
    public static void Draw(IRenderer renderer, TileLayer layer, int tileSize, Camera2D? camera, Func<int, int, int, Sprite?> spriteFor,
        Func<int, int, TileOrientation>? orientationAt, float margin = 0)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(spriteFor);
        var view = camera is null
            ? new RectF(0, 0, renderer.Width, renderer.Height).Inflate(tileSize + margin)
            : camera.VisibleBounds.Inflate(tileSize + margin + camera.MaxShake);
        int x0 = Math.Max(0, (int)MathF.Floor(view.X / tileSize)), y0 = Math.Max(0, (int)MathF.Floor(view.Y / tileSize));
        int x1 = Math.Min(layer.Width - 1, (int)MathF.Ceiling(view.Right / tileSize)), y1 = Math.Min(layer.Height - 1, (int)MathF.Ceiling(view.Bottom / tileSize));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            int id = layer[x, y];
            if (id == 0) continue;
            if (spriteFor(id, x, y) is not { } sprite) continue;
            var at = new System.Numerics.Vector2(x * tileSize, y * tileSize) - sprite.Origin;
            var orientation = orientationAt?.Invoke(x, y) ?? TileOrientation.None;
            if (orientation == TileOrientation.None) renderer.Draw(sprite.Texture, sprite.Source, at, Color.White);
            else renderer.Draw(sprite.Texture, sprite.Source, at, Color.White, orientation.ToDrawOptions(new System.Numerics.Vector2(sprite.Source.Width, sprite.Source.Height)));
        }
    }
}
