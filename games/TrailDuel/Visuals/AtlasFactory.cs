using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.Rules;
using TrailDuel.World;

namespace TrailDuel.Visuals;

/// <summary>Builds the single sprite atlas holding every image in the game.</summary>
internal static class AtlasFactory
{
    /// <param name="device">Creates the atlas texture.</param>
    /// <param name="image">Reads a content image by path (<c>Game.Assets.Image</c>), decoded fresh so it can be recoloured.</param>
    public static GameArt Build(IGraphicsDevice device, Func<string, PixelImage> image)
    {
        var b = new SpriteAtlasBuilder();
        var heights = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in SpeciesDex.All)
        {
            var front = CreatureArt.Front(image, s.Id);
            var back = CreatureArt.Back(front);
            var feet = CreatureArt.Feet(front);
            heights[s.Id] = front.OpaqueBounds()?.Height ?? CreatureArt.Size;
            var backFeet = new Vector2(front.Width - feet.X, feet.Y);
            b.Add("front:" + s.Id, front, feet);
            b.Add("back:" + s.Id, back, backFeet);
            b.Add("white:" + s.Id, front.Silhouette(Pal.White), feet);
            b.Add("whiteback:" + s.Id, back.Silhouette(Pal.White), backFeet);
            b.Add("front2:" + s.Id, Breathe(front), feet);
            b.Add("back2:" + s.Id, Breathe(back), backFeet);
            var shadow = ContactShadow(front);
            b.Add("cshadow:" + s.Id, shadow, new Vector2(shadow.Width / 2f, shadow.Height / 2f));
            var mini = Mini(front);
            b.Add("mini:" + s.Id, mini, new Vector2(12, 12));
            b.Add("mini2:" + s.Id, Hop(mini), new Vector2(12, 12));
        }
        var village = new Village(image);
        TileArt.AddAll(b, village);
        CharacterArt.AddAll(b, image);
        UiArt.AddAll(b);
        BattleStageArt.AddAll(b);
        FxArt.AddAll(b, image);
        foreach (var map in Maps.Every)
        {
            b.Add($"ground:{map.Id}", GroundArt.Build(map));
            for (int i = 0; i < map.Buildings.Count; i++)
            {
                b.Add($"bld:{map.Id}:{i}", BuildingArt.Build(village, map.Buildings[i]));
            }
        }
        return new GameArt(b.Build(device), heights);
    }

    /// <summary>
    /// The second idle frame: everything above the legs rises one pixel (a row is repeated just above
    /// the feet), so creatures breathe on the spot instead of hopping off their platform.
    /// </summary>
    private static PixelImage Breathe(PixelImage img)
    {
        int bottom = LowestOpaqueRow(img);
        int split = Math.Max(2, bottom - 7);
        var result = new PixelImage(img.Width, img.Height);
        for (int y = 0; y < img.Height; y++)
        for (int x = 0; x < img.Width; x++)
            result[x, y] = y < split - 1 ? img[x, y + 1] : img[x, y];
        return result;
    }

    /// <summary>An oval ground shadow as wide as the creature's footing.</summary>
    private static PixelImage ContactShadow(PixelImage img)
    {
        int bottom = LowestOpaqueRow(img), minX = img.Width, maxX = 0;
        for (int y = Math.Max(0, bottom - 4); y <= bottom; y++)
        for (int x = 0; x < img.Width; x++)
            if (img[x, y].A > 0) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); }
        float rx = Math.Max(6, (maxX - minX) / 2f + 3), ry = 3.2f;
        var shadow = new PixelImage((int)(rx * 2) + 2, 8);
        shadow.FillEllipse(shadow.Width / 2f, 4, rx, ry, Pal.Ink.WithAlpha(0.3f));
        return shadow;
    }

    private static int LowestOpaqueRow(PixelImage img)
    {
        for (int y = img.Height - 1; y >= 0; y--)
            for (int x = 0; x < img.Width; x++)
                if (img[x, y].A > 0) return y;
        return img.Height - 1;
    }

    /// <summary>
    /// A 24x24 party icon: the sprite's opaque area box-sampled down to fit 22 pixels, keeping the most
    /// common colour of each block (outline pixels lose ties), then re-outlined so it stays crisp.
    /// </summary>
    private static PixelImage Mini(PixelImage front)
    {
        var (bx, by, bw, bh) = front.OpaqueBounds() ?? (0, 0, front.Width, front.Height);
        float scale = 22f / Math.Max(bw, bh);
        int w = Math.Max(1, (int)MathF.Round(bw * scale)), h = Math.Max(1, (int)MathF.Round(bh * scale));
        int ox = (24 - w) / 2, oy = 23 - h;
        var mini = new PixelImage(24, 24);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int x0 = bx + (int)(x / scale), x1 = Math.Max(x0 + 1, bx + (int)((x + 1) / scale));
            int y0 = by + (int)(y / scale), y1 = Math.Max(y0 + 1, by + (int)((y + 1) / scale));
            var counts = new Dictionary<Color, int>();
            int opaque = 0, total = 0;
            for (int sy = y0; sy < y1; sy++)
            for (int sx = x0; sx < x1; sx++)
            {
                total++;
                var c = front[sx, sy];
                if (c.A == 0) continue;
                opaque++;
                // Dark outline pixels count for less so interior colours survive the reduction.
                int weight = c.R + c.G + c.B < 120 ? 1 : 2;
                counts[c] = counts.GetValueOrDefault(c) + weight;
            }
            if (opaque * 2 < total) continue;
            mini[ox + x, oy + y] = counts.MaxBy(kv => kv.Value).Key;
        }
        return mini.Outline(Pal.Ink);
    }

    /// <summary>The second idle frame of a party icon: shifted up a pixel for a little hop.</summary>
    private static PixelImage Hop(PixelImage mini)
    {
        var img = new PixelImage(24, 24);
        img.Blit(mini, 0, 1, 24, 23, 0, 0);
        return img;
    }
}
