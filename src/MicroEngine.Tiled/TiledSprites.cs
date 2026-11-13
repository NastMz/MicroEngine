using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace MicroEngine.Tiled;

/// <summary>
/// The sprites of a <see cref="TiledMap"/>'s tiles: each tileset image loaded as a texture through the game's assets
/// (shared and released with them), and a draw call for tile layers.
/// </summary>
public sealed class TiledSprites
{
    private readonly Sprite?[] _byGid;
    private readonly int _margin;

    /// <summary>Loads every tileset image of <paramref name="map"/> through <paramref name="assets"/>.</summary>
    /// <param name="map">The map.</param>
    /// <param name="assets">The game's assets (<c>Game.Assets</c>) or the scope the level's textures belong to.</param>
    /// <param name="filter">The textures' sampling; null for the assets' default (the game's presentation).</param>
    /// <exception cref="AssetLoadException">A tileset image is missing or cannot be decoded.</exception>
    public TiledSprites(TiledMap map, AssetCache assets, TextureFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(assets);
        Map = map;
        int last = map.Tilesets.Count == 0 ? 0 : map.Tilesets.SelectMany(t => t.Tiles).Select(t => t.Gid + 1).DefaultIfEmpty(0).Max();
        _byGid = new Sprite?[last];
        foreach (var tileset in map.Tilesets)
            foreach (var tile in tileset.Tiles)
            {
                if (tile.Image.Length == 0 || tile.Source.Width <= 0 || tile.Source.Height <= 0) continue;
                var texture = assets.Texture(tile.Image, filter);
                // Tiled draws a tile from the bottom-left of its cell, so a tile taller than the grid goes up.
                var origin = new Vector2(0, tile.Source.Height - map.TileHeight) - tileset.TileOffset;
                _byGid[tile.Gid] = new Sprite(texture, tile.Source, origin);
                _margin = Math.Max(_margin, (int)MathF.Ceiling(Math.Max(tile.Source.Width, tile.Source.Height)));
            }
    }

    /// <summary>The map the sprites are for.</summary>
    public TiledMap Map { get; }

    /// <summary>The sprite of the tile with global id <paramref name="gid"/>, or null (0, unknown ids and tiles without an image).</summary>
    public Sprite? this[int gid] => gid > 0 && gid < _byGid.Length ? _byGid[gid] : null;

    /// <summary>The sprite of <paramref name="gid"/> <paramref name="seconds"/> into its animation (its own sprite when it does not animate).</summary>
    public Sprite? At(int gid, double seconds) => Map.Tile(gid) is { } tile ? this[tile.FrameAt(seconds)] : this[gid];

    /// <summary>
    /// Draws the cells of <paramref name="layer"/> the camera shows, each tile turned as the map says and animated at
    /// <paramref name="seconds"/>, at the layer's offset, with its opacity and tint. A hidden layer draws nothing;
    /// parallax is not applied.
    /// </summary>
    /// <param name="renderer">The renderer; push <paramref name="camera"/> on it first when there is one.</param>
    /// <param name="layer">A tile layer of <see cref="Map"/>.</param>
    /// <param name="camera">The camera, used to skip what it does not show; null draws over the screen area.</param>
    /// <param name="seconds">The time animations are at (for example the scene's elapsed time).</param>
    /// <param name="shift">Pixels added to the layer's offset, for example its parallax (<see cref="TiledMap.ParallaxShift"/>).</param>
    public void Draw(IRenderer renderer, TiledTileLayer layer, Camera2D? camera = null, double seconds = 0, Vector2 shift = default)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(layer);
        if (!layer.Visible || layer.Opacity <= 0) return;
        if (Map.TileWidth != Map.TileHeight) throw new NotSupportedException($"{Map.Path}: drawing needs square cells, not {Map.TileWidth} × {Map.TileHeight}.");
        TileRenderer.Draw(renderer, layer.Tiles, Map.TileWidth, camera, (gid, _, _) => At(gid, seconds), layer.OrientationAt,
            layer.Offset + shift, TintOf(layer), _margin);
    }

    /// <summary>
    /// Draws the visible tile objects of <paramref name="layer"/> that the camera shows, in the layer's order, with
    /// the layer's opacity and tint. Other objects (rectangles, points, text...) are data and draw nothing.
    /// </summary>
    /// <param name="renderer">The renderer; push <paramref name="camera"/> on it first when there is one.</param>
    /// <param name="layer">An object layer of <see cref="Map"/>.</param>
    /// <param name="camera">The camera, used to skip what it does not show; null draws every object.</param>
    /// <param name="seconds">The time animations are at.</param>
    /// <param name="shift">Pixels added to every object's position, for example the layer's parallax.</param>
    public void Draw(IRenderer renderer, TiledObjectLayer layer, Camera2D? camera = null, double seconds = 0, Vector2 shift = default)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(layer);
        if (!layer.Visible || layer.Opacity <= 0) return;
        var tint = TintOf(layer);
        foreach (var o in layer.Objects)
        {
            if (!o.Visible || o.Shape != TiledShape.Tile) continue;
            if (camera is not null)
            {
                // A turned object stays inside the square its longest side spins in.
                var b = o.Bounds;
                float reach = o.Rotation == 0 ? 0 : Math.Max(b.Width, b.Height);
                if (!camera.IsVisible(new RectF(b.X + shift.X - reach, b.Y + shift.Y - reach, b.Width + 2 * reach, b.Height + 2 * reach))) continue;
            }
            Draw(renderer, o, seconds, shift, tint);
        }
    }

    /// <summary>
    /// Draws a tile object as Tiled shows it: its tile stretched to its size, standing on its position (the
    /// bottom-left corner), mirrored as it says and turned about that corner. Draws nothing for other objects.
    /// </summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="tileObject">A tile object of <see cref="Map"/>.</param>
    /// <param name="seconds">The time animations are at.</param>
    /// <param name="shift">Pixels added to its position.</param>
    /// <param name="tint">Colour it is multiplied by; null for white.</param>
    public void Draw(IRenderer renderer, TiledObject tileObject, double seconds = 0, Vector2 shift = default, Color? tint = null)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(tileObject);
        if (tileObject.Shape != TiledShape.Tile || At(tileObject.Gid, seconds) is not { } sprite) return;
        var source = sprite.Source;
        var size = tileObject.Size == Vector2.Zero ? new Vector2(source.Width, source.Height) : tileObject.Size;
        var scale = size / new Vector2(source.Width, source.Height);
        var origin = new Vector2(0, source.Height);
        var offset = Map.Tile(tileObject.Gid)?.Tileset.TileOffset ?? Vector2.Zero;
        var at = tileObject.Position + shift + offset - origin * scale;
        renderer.Draw(sprite.Texture, source, at, tint ?? Color.White, new DrawOptions(
            FlipX: (tileObject.Orientation & TileOrientation.FlipX) != 0, FlipY: (tileObject.Orientation & TileOrientation.FlipY) != 0,
            Rotation: tileObject.Rotation, Origin: origin, Scale: scale));
    }

    /// <summary>
    /// Draws every visible layer of the map that has something to draw, bottom to top: tile layers and the tile
    /// objects of object layers, each shifted by its parallax when a camera is given.
    /// </summary>
    public void DrawAll(IRenderer renderer, Camera2D? camera = null, double seconds = 0)
    {
        foreach (var layer in Map.AllLayers)
        {
            var shift = camera is null ? Vector2.Zero : Map.ParallaxShift(layer, camera.Center);
            if (layer is TiledTileLayer tiles) Draw(renderer, tiles, camera, seconds, shift);
            else if (layer is TiledObjectLayer objects) Draw(renderer, objects, camera, seconds, shift);
        }
    }

    private static Color TintOf(TiledLayer layer) => layer.Tint with { A = (byte)MathF.Round(layer.Tint.A * Math.Clamp(layer.Opacity, 0, 1)) };
}
