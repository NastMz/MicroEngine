using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>Layered backdrop for the title and ending: dusk sky, rolling hills, the ruined courtyard.</summary>
public static class TitleArt
{
    public static void AddAll(SpriteAtlasBuilder b)
    {
        b.Add("hills_far", Hills(320, 70, 0.55f, [Color.Hex("2a3a5a"), Color.Hex("34506a"), Color.Hex("3f6a72")], 601));
        b.Add("hills_near", Hills(320, 60, 0.8f, [Pal.Leaf[0], Pal.Leaf[1], Pal.Leaf[2]], 602));
        b.Add("ruins", Ruins());
        for (int i = 0; i < 3; i++) b.Add($"cloud{i}", Cloud(i));
    }

    private static PixelImage Hills(int w, int h, float roughness, Color[] ramp, int seed)
    {
        var img = new PixelImage(w, h);
        for (int x = 0; x < w; x++)
        {
            float n = Art.ValueNoise(x, 0, 60, seed) * 0.6f + Art.ValueNoise(x, 0, 22, seed + 1) * 0.3f * roughness + Art.ValueNoise(x, 0, 7, seed + 2) * 0.1f;
            int top = (int)(h * (0.15f + (1 - n) * 0.45f));
            for (int y = top; y < h; y++)
            {
                float t = (y - top) / (float)(h - top + 1);
                img[x, y] = y == top ? ramp[2] : Art.Ramp(ramp, 0.9f - t * 0.9f, x, y);
            }
        }
        // Tiny tree silhouettes on the near ridge
        if (roughness > 0.7f)
            for (int k = 0; k < 18; k++)
            {
                int x = (int)(Art.Hash(k, 0, seed) * w);
                int top = 0;
                while (top < h && img[x, top].A == 0) top++;
                img.FillCircle(x, top - 2, 3 + Art.Hash(k, 1, seed) * 2, ramp[0]);
            }
        return img;
    }

    private static PixelImage Ruins()
    {
        // A broken colonnade around a sunken arch, in cool silhouette with a few lit edges.
        var img = new PixelImage(160, 70);
        Color dark = Color.Hex("2a2440"), mid = Color.Hex("3c3558"), edge = Color.Hex("6a5f8c");
        img.FillRect(0, 58, 160, 12, dark);
        img.FillRect(0, 58, 160, 1, edge);
        int[] heights = [38, 52, 30, 48, 44, 26, 50, 36];
        for (int i = 0; i < heights.Length; i++)
        {
            int x = 6 + i * 19, top = 58 - heights[i];
            img.FillRect(x, top, 9, heights[i], mid);
            img.FillRect(x, top, 2, heights[i], edge);
            img.FillRect(x - 2, top, 13, 3, mid);
            img.FillRect(x - 2, 55, 13, 3, mid);
            // Broken capitals
            for (int c = 0; c < 9; c++) if (Art.Hash(i, c, 611) > 0.6f) img[x + c, top] = Color.Transparent;
        }
        // Central arch over the stairway
        img.FillRect(62, 16, 36, 42, mid);
        img.FillEllipse(80, 34, 12, 14, Color.Hex("120e24"));
        img.FillRect(68, 34, 24, 24, Color.Hex("120e24"));
        img.FillRect(62, 16, 36, 2, edge);
        img.FillRect(78, 12, 4, 4, Pal.Spirit[2]);
        return img;
    }

    private static PixelImage Cloud(int variant)
    {
        var img = new PixelImage(48, 16);
        Color body = Color.Hex("c9a6c8"), light = Color.Hex("f2d6d0"), shade = Color.Hex("8a7aa8");
        for (int k = 0; k < 5; k++)
        {
            float x = 8 + k * 8 + Art.Hash(k, variant, 621) * 4, r = 4 + Art.Hash(variant, k, 622) * 4;
            img.FillCircle(x, 10 - r * 0.4f, r, body);
        }
        img.Map((x, y, c) => c.A == 0 ? c : y > 11 ? shade : y < 6 ? light : c);
        return img;
    }
}
