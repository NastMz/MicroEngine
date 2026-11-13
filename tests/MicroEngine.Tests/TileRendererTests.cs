using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.World;

namespace MicroEngine.Tests;

[TestClass]
public sealed class TileRendererTests
{
    private static readonly Color Bg = new(10, 20, 30);

    private static (HeadlessRenderer Renderer, HeadlessGraphicsDevice Device) Reference(int width, int height)
    {
        var device = new HeadlessGraphicsDevice { KeepsPixels = true };
        var renderer = new HeadlessRenderer(width, height, device);
        renderer.Reset();
        renderer.Clear(Bg);
        return (renderer, device);
    }

    private static TileLayer Filled(int width, int height, int id)
    {
        var layer = new TileLayer(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) layer[x, y] = id;
        return layer;
    }

    [TestMethod]
    public void A_zoomed_out_camera_draws_every_tile_it_shows()
    {
        var (r, _) = Reference(32, 16);
        // At zoom 0.5 the 32 × 16 view shows 64 × 32 world pixels: the whole 16 × 8 layer of 4-pixel tiles.
        var camera = new Camera2D(32, 16) { Zoom = 0.5f, MaxShake = 0 };
        camera.CenterOn(new Vector2(32, 16));
        var asked = new HashSet<(int, int)>();
        TileRenderer.Draw(r, Filled(16, 8, 1), 4, camera, (_, x, y) => { asked.Add((x, y)); return null; });
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 16; x++)
                Assert.Contains((x, y), asked, $"tile ({x},{y}) is on screen");
    }

    [TestMethod]
    public void A_camera_at_zoom_one_still_skips_the_tiles_out_of_view()
    {
        var (r, g) = Reference(8, 8);
        var sprite = new Sprite(g.CreateTexture(new PixelImage(1, 1), TextureFilter.Nearest), new RectF(0, 0, 1, 1), Vector2.Zero);
        var asked = new HashSet<(int, int)>();
        var camera = new Camera2D(8, 8) { MaxShake = 0 };
        TileRenderer.Draw(r, Filled(100, 100, 1), 1, camera, (_, x, y) => { asked.Add((x, y)); return sprite; });
        Assert.IsTrue(asked.All(c => c.Item1 <= 9 && c.Item2 <= 9), "only the view plus a one-tile margin");
        Assert.IsTrue(asked.Contains((7, 7)));
    }

    [TestMethod]
    public void A_layer_is_drawn_at_its_offset_with_its_tint()
    {
        var (r, g) = Reference(8, 4);
        var white = new PixelImage(1, 1);
        white[0, 0] = Color.White;
        var sprite = new Sprite(g.CreateTexture(white, TextureFilter.Nearest), new RectF(0, 0, 1, 1), Vector2.Zero);
        var layer = new TileLayer(2, 1);
        layer[0, 0] = 1;
        TileRenderer.Draw(r, layer, 1, null, (_, _, _) => sprite, null, new Vector2(5, 2), new Color(255, 0, 0));
        Assert.AreEqual(new Color(255, 0, 0), r.Frame![5, 2], "tile (0, 0) at the offset, tinted");
        Assert.HasCount(1, Enumerable.Range(0, 32).Where(i => r.Frame![i % 8, i / 8] != Bg));
    }

    /// <summary>The Tiled rule: the diagonal flip (x and y swapped) first, then the horizontal, then the vertical.</summary>
    private static Color Expected(Color[,] source, int x, int y, TileOrientation o)
    {
        int n = source.GetLength(0);
        for (int sy = 0; sy < n; sy++)
            for (int sx = 0; sx < n; sx++)
            {
                int px = sx, py = sy;
                if ((o & TileOrientation.Diagonal) != 0) (px, py) = (py, px);
                if ((o & TileOrientation.FlipX) != 0) px = n - 1 - px;
                if ((o & TileOrientation.FlipY) != 0) py = n - 1 - py;
                if (px == x && py == y) return source[sx, sy];
            }
        throw new InvalidOperationException();
    }

    [TestMethod]
    public void Every_orientation_draws_the_tile_flipped_and_turned_inside_its_cell()
    {
        const int n = 3;
        var colors = new Color[n, n];
        var image = new PixelImage(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++) image[x, y] = colors[x, y] = new Color((byte)(40 + x * 80), (byte)(40 + y * 80), 200);
        var (r, g) = Reference(8 * n, n);
        var sprite = new Sprite(g.CreateTexture(image, TextureFilter.Nearest), new RectF(0, 0, n, n), Vector2.Zero);
        var layer = Filled(8, 1, 1);
        TileRenderer.Draw(r, layer, n, null, (_, _, _) => sprite, (x, _) => (TileOrientation)x);
        var frame = r.Frame!;
        for (int cell = 0; cell < 8; cell++)
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    Assert.AreEqual(Expected(colors, x, y, (TileOrientation)cell), frame[cell * n + x, y], $"{(TileOrientation)cell} at ({x},{y})");
    }
}
