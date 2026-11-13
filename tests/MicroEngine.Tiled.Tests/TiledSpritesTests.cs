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

    private static PixelImage Render(string path, Action<HeadlessRenderer, TiledSprites> draw, int width = 64, int height = 48)
    {
        var device = new HeadlessGraphicsDevice { KeepsPixels = true };
        var assets = new AssetCache(device, Maps);
        var sprites = new TiledSprites(assets.TiledMap(path), assets, TextureFilter.Nearest);
        var renderer = new HeadlessRenderer(width, height, device);
        renderer.Reset();
        renderer.Clear(Bg);
        draw(renderer, sprites);
        return renderer.Frame!;
    }

    private static PixelImage DrawGround(string path, double seconds) => Render(path, (r, s) =>
    {
        foreach (var layer in s.Map.TileLayers) s.Draw(r, layer, null, seconds);
    });

    [TestMethod]
    [DataRow("maps/level.tmx")]
    [DataRow("maps/level.tmj")]
    public void A_tile_object_stands_on_its_position_stretched_to_its_size_and_mirrored(string path)
    {
        // Object 7: the 16 × 32 chest, mirrored, at (48, 48) in a layer offset by (8, 4): it covers x 56–71, y 20–51.
        var f = Render(path, (r, s) => s.Draw(r, s.Map.ObjectLayer("spawns")), 80, 60);
        Assert.AreEqual(Brown, f[56, 20]);
        Assert.AreEqual(Brown, f[71, 51]);
        Assert.AreEqual(Bg, f[55, 30]);
        Assert.AreEqual(Bg, f[60, 52]);
        Assert.AreEqual(Bg, f[60, 19]);
        // The template's block is not a tile object, and the other shapes are data: nothing else is drawn.
        Assert.AreEqual(Bg, f[34, 30]);
        var everything = Render(path, (r, s) => s.DrawAll(r), 80, 60);
        Assert.AreEqual(Brown, everything[60, 50], "DrawAll draws object layers too, above the tiles");
    }

    [TestMethod]
    public void A_stretched_and_turned_tile_object_covers_what_Tiled_shows()
    {
        var f = Render("maps/level.tmj", (r, s) =>
        {
            var chest = s.Map.Object(7)! with { Position = new Vector2(40, 10), Size = new Vector2(32, 16), Rotation = MathF.PI / 2, Orientation = default };
            s.Draw(r, chest);
        }, 80, 60);
        // Unturned, the 32 × 16 box would rise from its bottom-left (40, 10) to x 72, y -6. A quarter turn clockwise
        // about that corner swings it down: x 40 to 56, y 10 to 42.
        Assert.AreEqual(Brown, f[41, 11]);
        Assert.AreEqual(Brown, f[55, 41]);
        Assert.AreEqual(Bg, f[39, 20]);
        Assert.AreEqual(Bg, f[57, 20]);
        Assert.AreEqual(Bg, f[45, 9]);
        Assert.AreEqual(Bg, f[45, 43]);
    }

    [TestMethod]
    public void The_turned_bounds_hold_what_a_turned_object_covers()
    {
        var map = new AssetCache(new HeadlessGraphicsDevice(), Maps).TiledMap("maps/level.tmx");
        var chest = map.Object(7)! with { Position = new Vector2(40, 10), Size = new Vector2(32, 16), Rotation = MathF.PI / 2 };
        Assert.AreEqual(new RectF(40, 10, 16, 32), chest.TurnedBounds, "the box the drawing test covers");
        var door = map.Object(1)!;   // 16 × 32 at (24, 20), a quarter turn about its top-left
        Assert.AreEqual(new RectF(-8, 20, 32, 16), door.TurnedBounds);
        Assert.AreEqual(map.Object(2)!.Bounds, map.Object(2)!.TurnedBounds, "an unturned object");
    }

    [TestMethod]
    [DataRow("maps/level.tmx")]
    [DataRow("maps/level.tmj")]
    public void Parallax_shifts_a_layer_by_how_far_the_view_is_from_the_parallax_origin(string path)
    {
        var map = new AssetCache(new HeadlessGraphicsDevice(), Maps).TiledMap(path);
        Assert.AreEqual(new Vector2(32, 24), map.ParallaxOrigin);
        var sky = map.Layer<TiledImageLayer>("sky");
        Assert.AreEqual(new Vector2(0.5f, 0.25f), sky.Parallax);
        Assert.AreEqual(new Vector2(16, 18), map.ParallaxShift(sky, new Vector2(64, 48)));
        Assert.AreEqual(Vector2.Zero, map.ParallaxShift(map.TileLayer("ground"), new Vector2(64, 48)), "a layer that moves with the map");
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
