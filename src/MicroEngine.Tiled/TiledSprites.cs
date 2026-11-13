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
        int last = map.Tilesets.Count == 0 ? 0 : map.Tilesets.Max(t => t.FirstGid + t.Tiles.Count);
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
    public void Draw(IRenderer renderer, TiledTileLayer layer, Camera2D? camera = null, double seconds = 0)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(layer);
        if (!layer.Visible || layer.Opacity <= 0) return;
        if (Map.TileWidth != Map.TileHeight) throw new NotSupportedException($"{Map.Path}: drawing needs square cells, not {Map.TileWidth} × {Map.TileHeight}.");
        var tint = layer.Tint with { A = (byte)MathF.Round(layer.Tint.A * Math.Clamp(layer.Opacity, 0, 1)) };
        TileRenderer.Draw(renderer, layer.Tiles, Map.TileWidth, camera, (gid, _, _) => At(gid, seconds), layer.OrientationAt,
            layer.Offset, tint, _margin);
    }

    /// <summary>Draws every visible tile layer of the map, bottom to top (see <see cref="Draw(IRenderer, TiledTileLayer, Camera2D?, double)"/>).</summary>
    public void DrawAll(IRenderer renderer, Camera2D? camera = null, double seconds = 0)
    {
        foreach (var layer in Map.TileLayers) Draw(renderer, layer, camera, seconds);
    }
}
