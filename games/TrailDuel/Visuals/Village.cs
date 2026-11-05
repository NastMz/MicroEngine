using MicroEngine;
using MicroEngine.Graphics;

namespace TrailDuel.Visuals;

/// <summary>
/// The overworld's drawn pieces from LimeZu's Serene Village revamped (CC-BY 4.0, see CREDITS.md): houses, trees,
/// flowers, rocks, signs, the path edges and the pond, cut from <c>Content/serene-village</c> at load. Pieces are cut
/// whole, along their own silhouette; the code only recolours, places and composes them.
/// </summary>
internal sealed class Village
{
    public const string SheetPath = "serene-village/serene-village-16x16.png";
    public const string WavesPath = "serene-village/water-waves-16x16.png";

    /// <summary>Frames of the pond's wave loop in the waves sheet.</summary>
    public const int WaveFrames = 14;

    public PixelImage Sheet { get; }
    public PixelImage Waves { get; }

    /// <param name="image">Reads a content image by path (<c>Game.Assets.Image</c>).</param>
    public Village(Func<string, PixelImage> image)
    {
        Sheet = image(SheetPath);
        Waves = image(WavesPath);
    }

    /// <summary>A rectangle of the sheet.</summary>
    public PixelImage Cut(int x, int y, int w, int h) => Sheet.Crop(x, y, w, h);

    /// <summary>One 16x16 cell of the sheet.</summary>
    public PixelImage Cell(int col, int row) => Sheet.Crop(col * 16, row * 16, 16, 16);

    /// <summary>
    /// A piece cut along its silhouette: the opaque pixels joined (8-neighbour) to the one at (<paramref name="seedX"/>,
    /// <paramref name="seedY"/>) inside the rectangle, so a neighbour's grass tuft reaching into the rectangle stays out.
    /// </summary>
    public PixelImage Piece(int x, int y, int w, int h, int seedX, int seedY)
    {
        var src = Cut(x, y, w, h);
        // Start from the opaque pixel nearest the seed (the seed is a hint, not an exact pixel).
        int best = int.MaxValue, sx = seedX, sy = seedY;
        for (int py = 0; py < h; py++)
        for (int px = 0; px < w; px++)
        {
            int d = (px - seedX) * (px - seedX) + (py - seedY) * (py - seedY);
            if (src[px, py].A > 0 && d < best) (best, sx, sy) = (d, px, py);
        }
        var keep = new bool[w * h];
        var stack = new Stack<(int X, int Y)>();
        if (src[sx, sy].A > 0) { keep[sy * w + sx] = true; stack.Push((sx, sy)); }
        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Pop();
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!src.InBounds(nx, ny) || keep[ny * w + nx] || src[nx, ny].A == 0) continue;
                keep[ny * w + nx] = true;
                stack.Push((nx, ny));
            }
        }
        return src.Map((px, py, c) => keep[py * w + px] ? c : Color.Transparent);
    }

    /// <summary>
    /// The colour each pixel of <paramref name="from"/> takes in <paramref name="to"/>, two drawings of the same piece
    /// in different colours (the pack's red, green and blue houses, its four trees): a recolouring learned from the
    /// pack itself. Ties go to the most frequent pairing.
    /// </summary>
    public static Dictionary<Color, Color> Recolouring(PixelImage from, PixelImage to)
    {
        var votes = new Dictionary<(Color, Color), int>();
        for (int y = 0; y < Math.Min(from.Height, to.Height); y++)
        for (int x = 0; x < Math.Min(from.Width, to.Width); x++)
        {
            var a = from[x, y];
            var b = to[x, y];
            if (a.A == 0 || b.A == 0 || a == b) continue;
            votes[(a, b)] = votes.GetValueOrDefault((a, b)) + 1;
        }
        var map = new Dictionary<Color, Color>();
        foreach (var group in votes.GroupBy(v => v.Key.Item1))
            map[group.Key] = group.MaxBy(v => v.Value).Key.Item2;
        return map;
    }

    public static PixelImage Recolour(PixelImage img, IReadOnlyDictionary<Color, Color> map)
        => img.Clone().Map((_, _, c) => c.A > 0 && map.TryGetValue(c, out var to) ? to : c);

    /// <summary>The colour with its hue turned to <paramref name="hue"/> (0..6, red 0, green 2, blue 4), keeping saturation and value.</summary>
    public static Color WithHue(Color c, float hue, float saturation = 1)
    {
        var (_, max, min) = Pal.Hue(c);
        float d = (max - min) * saturation, m = max - d;
        float x = d * (1 - MathF.Abs(hue % 2 - 1));
        var (r, g, b) = hue switch
        {
            < 1 => (d, x, 0f), < 2 => (x, d, 0f), < 3 => (0f, d, x), < 4 => (0f, x, d), < 5 => (x, 0f, d), _ => (d, 0f, x),
        };
        static byte B(float v) => (byte)Math.Clamp(MathF.Round(v * 255), 0, 255);
        return new Color(B(r + m), B(g + m), B(b + m), c.A);
    }
}

/// <summary>The pack's own colours, used by the parts of the overworld still drawn in code so they match it.</summary>
internal static class VillagePal
{
    private static Color H(string hex) => Color.Hex(hex);

    /// <summary>The pack's outline and its softer second outline.</summary>
    public static readonly Color Outline = H("3a3a50"), OutlineSoft = H("46465e"), OutlineGrey = H("565972");
    /// <summary>Meadow grass: the two dither tones, the darker rim at its edges and the deep blades.</summary>
    public static readonly Color GrassA = H("7bcb69"), GrassB = H("76c564"), GrassRim = H("69ad59"), GrassDeep = H("55b14e");
    /// <summary>Foliage of the bushes and green trees, dark to light.</summary>
    public static readonly Color[] Leaf = [H("455c5b"), H("4e6e61"), H("568d61"), H("9bc246"), H("b8d040")];
    /// <summary>Dirt: dither tones, the soft rim and the earth face under raised grass, dark to light.</summary>
    public static readonly Color DirtA = H("d29f51"), DirtB = H("cb9a4e"), DirtRim = H("cb8a46"), Earth = H("bb6d33"), EarthDeep = H("8e5429"), Pebble = H("cbbd97");
    /// <summary>Foundation stone, dark to light.</summary>
    public static readonly Color[] Stone = [H("565972"), H("9c786b"), H("b99e86"), H("d0be9c")];
}
