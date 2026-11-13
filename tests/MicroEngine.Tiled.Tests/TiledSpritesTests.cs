using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tiled.Tests;

[TestClass]
public sealed class TiledSpritesTests
{
    private static readonly string Maps = Path.Combine(AppContext.BaseDirectory, "Maps");
    private static readonly Color Bg = new(10, 20, 30);
    private static readonly Color White = Color.White, Red = new(255, 0, 0), Green = new(0, 255, 0), Blue = new(0, 0, 255), Yellow = new(255, 255, 0), Brown = new(120, 70, 20);

    private static PixelImage DrawGround(string path, double seconds)
    {
        var device = new HeadlessGraphicsDevice { KeepsPixels = true };
        var assets = new AssetCache(device, Maps);
        var map = assets.TiledMap(path);
        var sprites = new TiledSprites(map, assets, TextureFilter.Nearest);
        var renderer = new HeadlessRenderer(64, 48, device);
        renderer.Reset();
        renderer.Clear(Bg);
        sprites.DrawAll(renderer, null, seconds);
        return renderer.Frame!;
    }

    [TestMethod]
    [DataRow("maps/level.tmx")]
    [DataRow("maps/level.tmj")]
    public void Tiles_are_drawn_from_their_tileset_cells_turned_as_the_map_says(string path)
    {
        var f = DrawGround(path, 0);
        // Row 2: gid 1 (red, white top-left), gid 1 mirrored, gid 2 (green), gid 2 turned half a turn.
        Assert.AreEqual(White, f[0, 32]);
        Assert.AreEqual(Red, f[15, 47]);
        Assert.AreEqual(White, f[31, 32], "mirrored: the white pixel moves to the top-right");
        Assert.AreEqual(Red, f[16, 32]);
        Assert.AreEqual(White, f[32, 32]);
        Assert.AreEqual(Green, f[47, 47]);
        Assert.AreEqual(White, f[63, 47], "half a turn: the white pixel moves to the bottom-right");
        Assert.AreEqual(Green, f[48, 32]);
        // Cell (0, 0): gid 3 (blue) mirrored on the diagonal keeps its corner; (0, 1) gid 4 (yellow).
        Assert.AreEqual(White, f[0, 0]);
        Assert.AreEqual(Blue, f[15, 15]);
        Assert.AreEqual(Yellow, f[8, 24]);
        // Cell (3, 1): the 16 × 32 chest stands on its cell's bottom, covering the cell above too.
        Assert.AreEqual(Brown, f[50, 2]);
        Assert.AreEqual(Brown, f[50, 31]);
        Assert.AreEqual(Bg, f[20, 4], "empty cells stay empty");
        Assert.AreEqual(Bg, f[36, 4], "the hidden layer's tile is not drawn");
    }

    [TestMethod]
    public void Animated_tiles_show_the_frame_of_the_time_given()
    {
        Assert.AreEqual(Blue, DrawGround("maps/level.tmj", 0.05)[8, 8]);
        Assert.AreEqual(Yellow, DrawGround("maps/level.tmj", 0.2)[8, 8]);
        Assert.AreEqual(Blue, DrawGround("maps/level.tmj", 0.45)[8, 8]);
    }

    [TestMethod]
    public void Each_tile_has_a_sprite_on_its_tileset_texture()
    {
        var device = new HeadlessGraphicsDevice();
        var assets = new AssetCache(device, Maps);
        var map = assets.TiledMap("maps/level.tmx");
        var sprites = new TiledSprites(map, assets);
        Assert.AreEqual(new RectF(16, 0, 16, 16), sprites[2]!.Source);
        Assert.AreSame(sprites[1]!.Texture, sprites[4]!.Texture, "one texture per tileset image");
        Assert.AreSame(assets.Texture("tilesets/terrain.png"), sprites[1]!.Texture, "loaded through the game's assets");
        Assert.AreEqual(new Vector2(0, 16), sprites[5]!.Origin, "a tall tile is drawn upward from its cell");
        Assert.IsNull(sprites[0]);
        Assert.IsNull(sprites[99]);
        Assert.AreSame(sprites[4], sprites.At(3, 0.2));
    }
}
