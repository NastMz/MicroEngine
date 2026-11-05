using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.World;

namespace TrailDuel.Visuals;

/// <summary>
/// One baked image per map, drawn over the ground tiles and under every object. Outdoors it adds
/// soft tone patches, clover, pebbles and wild flowers that ignore the tile grid; everywhere it adds
/// the shadows that buildings, trees, rocks, signs and furniture cast toward the bottom-right.
/// Shadows are a single union mask, so overlapping shadows never stack darker.
/// </summary>
internal static class GroundArt
{
    private static readonly Color ShadowColor = Pal.Ink.WithAlpha(0.24f), HardColor = Pal.Ink.WithAlpha(0.34f), SoftColor = Pal.Ink.WithAlpha(0.17f);

    /// <summary>Maps in open daylight, where shadows get depth: every outdoor map but the shaded Hollow Wood.</summary>
    private static bool Lit(MapDef map) => !map.Indoor && map.Backdrop != Backdrop.Forest;

    public static PixelImage Build(MapDef map)
    {
        int w = map.Width * 16, h = map.Height * 16;
        var img = new PixelImage(w, h);
        var shadow = new bool[w * h];
        var shade = new bool[w * h];
        if (!map.Indoor)
        {
            Patches(img, map);
            Specks(img, map);
            TallGrassFringe(img, map);
        }
        bool lit = Lit(map);
        CastShadows(map, shadow, shade, w, h);
        // Towns and routes get deeper contact shadows and a second, softer level (crowns overhead, long cast shadows);
        // interiors and the Hollow Wood keep the single level they had.
        float hardShade = lit ? 0.64f : 0.74f;
        var hardColor = lit ? HardColor : ShadowColor;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var c = img[x, y];
            if (shadow[y * w + x]) img[x, y] = c.A == 255 ? c.Shade(hardShade) : hardColor;
            else if (shade[y * w + x]) img[x, y] = c.A == 255 ? c.Shade(0.82f) : SoftColor;
        }
        return img;
    }

    private static bool IsGrass(MapDef map, int tx, int ty) => map.KindAt(tx, ty) == TileKind.Grass;

    /// <summary>Large patches of shadier and sunnier grass from world-space noise, as translucent tone over the tile texture.</summary>
    private static void Patches(PixelImage img, MapDef map)
    {
        var dark = VillagePal.GrassDeep;
        var sun = VillagePal.Leaf[4];
        int seed = map.Id.Length * 7 + map.Id[0];
        for (int y = 0; y < img.Height; y++)
        for (int x = 0; x < img.Width; x++)
        {
            if (!IsGrass(map, x / 16, y / 16)) continue;
            float n = Art.ValueNoise(x, y, 30, seed) * 0.8f + Art.ValueNoise(x, y, 8, seed + 1) * 0.2f;
            if (n > 0.7f) img[x, y] = dark.WithAlpha(0.14f);
            else if (n < 0.24f) img[x, y] = sun.WithAlpha(0.08f);
        }
    }

    /// <summary>Clover sprigs, pebbles and tiny flowers scattered at sub-tile offsets.</summary>
    private static void Specks(PixelImage img, MapDef map)
    {
        // The pack's greens: rim, deep blades and the bright leaf of its bushes.
        Color[] g = [VillagePal.GrassDeep, VillagePal.GrassRim, VillagePal.GrassB, VillagePal.Leaf[3]];
        for (int ty = 0; ty < map.Height; ty++)
        for (int tx = 0; tx < map.Width; tx++)
        {
            if (!IsGrass(map, tx, ty)) continue;
            float roll = Art.Hash(tx, ty, 91);
            int x = tx * 16 + 3 + (int)(Art.Hash(tx, ty, 92) * 10), y = ty * 16 + 4 + (int)(Art.Hash(tx, ty, 93) * 9);
            if (roll < 0.18f)
            {
                // Clover: three dark leaves around a bright centre.
                img[x, y] = g[3]; img[x - 1, y] = g[0]; img[x + 1, y] = g[0]; img[x, y - 1] = g[0]; img[x, y + 1] = g[1];
            }
            else if (roll < 0.28f)
            {
                img[x, y] = VillagePal.Stone[2]; img[x + 1, y] = VillagePal.Stone[1]; img[x, y + 1] = VillagePal.OutlineSoft; img[x - 1, y] = VillagePal.Stone[3];
            }
            else if (roll < 0.36f)
            {
                var petal = Pal.FlowerColors[(int)(Art.Hash(tx, ty, 94) * 4)];
                img[x, y] = petal; img[x, y + 1] = g[0];
            }
            else if (roll < 0.5f)
            {
                // A taller tuft of blades.
                img[x, y] = g[0]; img[x, y - 1] = g[1]; img[x, y - 2] = g[3]; img[x + 1, y] = g[0]; img[x + 1, y - 1] = g[2]; img[x - 1, y - 1] = g[3];
            }
        }
    }

    /// <summary>Short blades spilling out of tall-grass patches onto the plain grass beside them, so patches have no ruler edges.</summary>
    private static void TallGrassFringe(PixelImage img, MapDef map)
    {
        Color[] c = [VillagePal.Outline, VillagePal.Leaf[2], Color.Hex("62b04e"), VillagePal.Leaf[3]];
        (int Dx, int Dy)[] sides = [(0, -1), (1, 0), (0, 1), (-1, 0)];
        for (int ty = 0; ty < map.Height; ty++)
        for (int tx = 0; tx < map.Width; tx++)
        {
            if (!IsGrass(map, tx, ty)) continue;
            foreach (var (dx, dy) in sides)
            {
                if (map.KindAt(tx + dx, ty + dy) != TileKind.TallGrass) continue;
                for (int i = 1; i < 15; i += 2)
                {
                    float roll = Art.Hash(tx * 16 + i, ty * 16 + dx * 3 + dy * 7, 120);
                    if (roll > 0.6f) continue;
                    int len = 1 + (int)(roll * 5);
                    for (int k = 0; k < len; k++)
                    {
                        // Distance from the shared edge into this tile.
                        int depth = k;
                        int x = dx switch { 1 => tx * 16 + 15 - depth, -1 => tx * 16 + depth, _ => tx * 16 + i };
                        int y = dy switch { 1 => ty * 16 + 15 - depth, -1 => ty * 16 + depth, _ => ty * 16 + i };
                        if (dy == 0) y += k % 2;
                        img[x, y] = k == len - 1 ? c[3] : k == 0 ? c[1] : c[2];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Shadows fall to the bottom-right (light from the upper left): <paramref name="mask"/> holds the contact and cast
    /// shadows, <paramref name="soft"/> the lighter shade of crowns overhead and of a building's long cast shadow.
    /// </summary>
    private static void CastShadows(MapDef map, bool[] mask, bool[] soft, int w, int h)
    {
        bool lit = Lit(map);
        void Soft(float cx, float cy, float rx, float ry)
        {
            if (!lit) return;
            for (int y = (int)(cy - ry); y <= (int)(cy + ry) + 1; y++)
            for (int x = (int)(cx - rx); x <= (int)(cx + rx) + 1; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1) soft[y * w + x] = true;
            }
        }
        void Ellipse(float cx, float cy, float rx, float ry)
        {
            for (int y = (int)(cy - ry); y <= (int)(cy + ry) + 1; y++)
            for (int x = (int)(cx - rx); x <= (int)(cx + rx) + 1; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1) mask[y * w + x] = true;
            }
        }
        void Rect(int x0, int y0, int rw, int rh)
        {
            for (int y = Math.Max(0, y0); y < Math.Min(h, y0 + rh); y++)
            for (int x = Math.Max(0, x0); x < Math.Min(w, x0 + rw); x++)
                mask[y * w + x] = true;
        }

        for (int ty = 0; ty < map.Height; ty++)
        for (int tx = 0; tx < map.Width; tx++)
        {
            float px = tx * 16, py = ty * 16;
            switch (map.KindAt(tx, ty))
            {
                case TileKind.Tree or TileKind.Pine:
                    // The floor of a wood lies in the crowns' shade.
                    if (TreeLayout.InWood(map, tx, ty)) Rect(tx * 16, ty * 16, 16, 16);
                    if (TreeLayout.At(map, tx, ty) is { } tree)
                    {
                        // The trunk's contact shadow, and the crown's shade cast down and to the right of it.
                        if (tree.Low)
                        {
                            Ellipse(px + 10, py + 16, 7, 2.4f);
                            Soft(px + 13, py + 13, 10, 5.5f);
                        }
                        else
                        {
                            Ellipse(px + 10 + tree.Shift * 8, py + 15, 9, 3);
                            Soft(px + 15 + tree.Shift * 8, py + 11, 15, 8);
                        }
                    }
                    break;
                case TileKind.Rock: Ellipse(px + 11, py + 14, 6.5f, 2.6f); Soft(px + 13, py + 14, 7, 3.4f); break;
                case TileKind.Bush: Ellipse(px + 11, py + 14, 6.5f, 2.6f); Soft(px + 13, py + 14, 7, 3.4f); break;
                case TileKind.Fence: Rect(tx * 16 + 2, ty * 16 + 13, 16, 2); if (lit) Rect(tx * 16 + 3, ty * 16 + 13, 16, 3); break;
                case TileKind.Ledge: Rect(tx * 16, ty * 16 + 14, 16, 2); break;
                case TileKind.Statue: Ellipse(px + 10, py + 14.5f, 7, 2.6f); break;
                case TileKind.Bookshelf or TileKind.Shelf or TileKind.Machine or TileKind.Kitchen:
                    Rect(tx * 16 + 2, ty * 16 + 15, 16, 2);
                    if (map.KindAt(tx + 1, ty) is TileKind.Floor or TileKind.Mat or TileKind.Rug or TileKind.Carpet) Rect(tx * 16 + 16, ty * 16 - 2, 3, 18);
                    break;
                case TileKind.Bed:
                    if (map.KindAt(tx, ty + 1) != TileKind.Bed) Rect(tx * 16 + 2, ty * 16 + 15, 15, 2);
                    break;
                case TileKind.Table or TileKind.Pedestal or TileKind.Counter or TileKind.Altar or TileKind.Tv:
                    if (map.KindAt(tx, ty + 1) is not (TileKind.Table or TileKind.Pedestal or TileKind.Counter)) Rect(tx * 16 + 1, ty * 16 + 15, 16, 2);
                    break;
                case TileKind.Plant: Ellipse(px + 9.5f, py + 14.5f, 5.5f, 2.2f); break;
                case TileKind.Wall:
                    // Walls darken the floor strip right below them.
                    if (map.KindAt(tx, ty + 1) is not (TileKind.Wall or TileKind.Void)) Rect(tx * 16, ty * 16 + 16, 16, 3);
                    break;
            }
        }
        foreach (var s in map.Signs)
        {
            Ellipse(s.X * 16 + 10, s.Y * 16 + 14.5f, 5, 2);
            // The board's shadow lies on the grass beside the post.
            Soft(s.X * 16 + 14, s.Y * 16 + 13.5f, 7, 3);
        }
        foreach (var b in map.Buildings)
        {
            var (left, artTop, artRight) = BuildingArt.Bounds(b);
            int x0 = b.X * 16 + left, right = b.X * 16 + artRight, top = b.Y * 16 + artTop, bottom = (b.Y + b.H) * 16;
            // Cast shadow off the right wall: a band that starts below the eave and slants down.
            for (int y = top + 2; y < bottom + 3; y++)
            {
                int reach = Math.Min(5, (y - top - 2) / 3 + 2);
                for (int x = right - 2; x < right + reach; x++)
                    if (x >= 0 && x < w && y >= 0 && y < h) mask[y * w + x] = true;
            }
            // Contact shadow along the base, shifted right.
            Rect(x0 + 4, bottom, right - x0 - 2, 3);
            if (!lit) continue;
            // In daylight the wall casts a longer, softer shadow down and to the right, and the base a wider one.
            for (int y = top + 4; y < bottom + 7; y++)
            {
                int reach = Math.Min(12, (y - top - 4) / 2 + 3);
                for (int x = right - 2; x < right + reach; x++)
                    if (x >= 0 && x < w && y >= 0 && y < h) soft[y * w + x] = true;
            }
            for (int y = bottom; y < Math.Min(h, bottom + 6); y++)
            for (int x = Math.Max(0, x0 + 6); x < Math.Min(w, right + 4); x++)
                soft[y * w + x] = true;
        }
    }
}
