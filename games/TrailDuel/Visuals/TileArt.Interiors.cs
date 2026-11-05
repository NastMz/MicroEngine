using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;

namespace TrailDuel.Visuals;

/// <summary>Interior tiles and furniture.</summary>
internal static partial class TileArt
{
    private static PixelImage Floor(int variant)
    {
        var img = New();
        var f = Pal.Floor;
        for (int y = 0; y < T; y++)
        {
            int plank = y / 4;
            int seam = (plank * 7 + variant * 5) % 16;
            for (int x = 0; x < T; x++)
            {
                float grain = Art.ValueNoise(x * 0.4f, y + plank * 9 + variant * 30, 3, 9);
                var c = Art.Ramp([f[1], f[2], f[3]], 0.35f + grain * 0.5f, x, y);
                if (y % 4 == 3) c = f[0];
                else if (x == seam) c = f[1];
                img[x, y] = c;
            }
        }
        return img;
    }

    private static PixelImage StoneFloor()
    {
        var img = New();
        for (int y = 0; y < T; y++)
        for (int x = 0; x < T; x++)
        {
            bool checker = (x < 8) == (y < 8);
            var ramp = checker ? new[] { Color.Hex("b8a690"), Color.Hex("cfbea6"), Color.Hex("e2d4bc") } : new[] { Color.Hex("a8947c"), Color.Hex("bca88e"), Color.Hex("d0bea2") };
            img[x, y] = Art.Ramp(ramp, 0.55f + Art.ValueNoise(x, y, 4, 13) * 0.3f, x, y);
            if (x % 8 == 7 || y % 8 == 7) img[x, y] = Color.Hex("8a7a68");
            else if (x % 8 == 0 || y % 8 == 0) img[x, y] = Color.Hex("ece0c8");
        }
        return img;
    }

    private static PixelImage Wall(bool top, bool window)
    {
        var img = New();
        var w = Pal.Plaster;
        for (int y = 0; y < T; y++)
        for (int x = 0; x < T; x++)
            img[x, y] = Art.Ramp([w[0], w[1], w[2]], 0.55f + Art.ValueNoise(x, y, 4, 2) * 0.35f - (top ? 0.2f : 0), x, y);
        if (top)
        {
            img.FillRect(0, 0, T, 3, Pal.Wood[1]);
            img.FillRect(0, 0, T, 1, Pal.Wood[2]);
            img.FillRect(0, 3, T, 1, Pal.Wood[0]);
        }
        else
        {
            img.FillRect(0, 10, T, 6, Pal.Wood[2]);
            img.FillRect(0, 10, T, 1, Pal.Wood[3]);
            img.FillRect(0, 15, T, 1, Pal.Wood[0]);
            for (int x = 3; x < T; x += 5) img.FillRect(x, 11, 1, 4, Pal.Wood[1]);
        }
        if (window)
        {
            img.FillRect(3, 1, 10, 8, Pal.Wood[1]);
            for (int y = 2; y < 8; y++)
            for (int x = 4; x < 12; x++)
                img[x, y] = Color.Lerp(Pal.Sky, Pal.White, (y - 2) / 8f + (x + y) % 5 * 0.03f);
            img.FillRect(7, 2, 1, 6, Pal.Wood[2]);
            img.FillRect(4, 5, 8, 1, Pal.Wood[2]);
            img[5, 3] = Pal.White; img[9, 3] = Pal.White;
        }
        return img;
    }

    private static PixelImage Mat()
    {
        var img = Floor(0);
        img.FillRect(1, 4, 14, 10, Pal.Coral);
        img.DrawRect(1, 4, 14, 10, Pal.CoralDark);
        for (int x = 3; x < 13; x += 2) img.FillRect(x, 6, 1, 6, Pal.Coral.Shade(1.15f));
        return img;
    }

    private static PixelImage Carpet(int mask)
    {
        var img = Floor(1);
        var red = Color.Hex("b8404a");
        bool up = (mask & 1) != 0, right = (mask & 2) != 0, down = (mask & 4) != 0, left = (mask & 8) != 0;
        int x0 = left ? 0 : 2, x1 = right ? 15 : 13, y0 = up ? 0 : 2, y1 = down ? 15 : 13;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            bool border = (!left && x == x0) || (!right && x == x1) || (!up && y == y0) || (!down && y == y1);
            bool trim = (!left && x == x0 + 1) || (!right && x == x1 - 1);
            img[x, y] = border ? red.Shade(0.6f) : trim ? Pal.Gold : Art.Dither(x, y, 0.2f) ? red.Shade(1.1f) : red;
        }
        return img;
    }

    private static PixelImage Rug()
    {
        var img = Floor(0);
        img.FillEllipse(8, 8, 7.5f, 6.5f, Color.Hex("6a8ac8"));
        img.FillEllipse(8, 8, 5.5f, 4.5f, Color.Hex("f2e2c4"));
        img.FillEllipse(8, 8, 3, 2.2f, Color.Hex("e8735a"));
        return img;
    }

    private static PixelImage Bookshelf()
    {
        var img = new PixelImage(16, 24);
        var w = Pal.Wood;
        img.FillRect(0, 0, 16, 24, w[1]);
        img.FillRect(1, 1, 14, 22, w[0]);
        Color[] books = [Color.Hex("c84a4a"), Color.Hex("4a7ac8"), Color.Hex("f2c25c"), Color.Hex("5aa04f"), Color.Hex("9a6ac8"), Color.Hex("e8e0d0")];
        for (int shelf = 0; shelf < 3; shelf++)
        {
            int y = 2 + shelf * 7;
            int x = 2;
            int i = shelf * 3;
            while (x < 14)
            {
                int bw = 1 + (int)(Art.Hash(x, shelf, 4) * 2);
                int bh = 4 + (int)(Art.Hash(shelf, x, 5) * 2);
                var col = books[i++ % books.Length];
                img.FillRect(x, y + 6 - bh, bw, bh, col);
                img.FillRect(x, y + 6 - bh, 1, bh, col.Shade(1.2f));
                x += bw + 1;
            }
            img.FillRect(1, y + 6, 14, 1, w[2]);
        }
        img.FillRect(0, 0, 16, 1, w[3]);
        return img.Outline(Pal.Ink);
    }

    private static PixelImage BedTop()
    {
        var img = New();
        img.FillRect(1, 2, 14, 14, Pal.Wood[2]);
        img.FillRect(1, 2, 14, 2, Pal.Wood[3]);
        img.FillRect(3, 5, 10, 6, Pal.White);
        img.FillRect(3, 10, 10, 1, Pal.CreamShade);
        img.FillRect(2, 12, 12, 4, Color.Hex("5b82c8"));
        img.FillRect(2, 12, 12, 1, Color.Hex("86aee2"));
        return img.Outline(Pal.Ink);
    }

    private static PixelImage BedBottom()
    {
        var img = New();
        var q = Color.Hex("5b82c8");
        img.FillRect(2, 0, 12, 13, q);
        for (int y = 2; y < 12; y += 4)
        for (int x = 3; x < 13; x += 4) img.FillRect(x, y, 2, 2, Color.Hex("86aee2"));
        img.FillRect(1, 12, 14, 3, Pal.Wood[1]);
        img.FillRect(1, 12, 14, 1, Pal.Wood[3]);
        return img.Outline(Pal.Ink);
    }

    /// <summary>Tables join horizontally: bit 1 = table continues left, bit 2 = right.</summary>
    private static PixelImage Table(int mask)
    {
        var img = New();
        bool left = (mask & 1) != 0, right = (mask & 2) != 0;
        int x0 = left ? 0 : 1, x1 = right ? 15 : 14;
        var w = Pal.Wood;
        img.FillRect(x0, 3, x1 - x0 + 1, 8, w[3]);
        img.FillRect(x0, 3, x1 - x0 + 1, 1, Pal.White.WithAlpha(0.5f));
        img.FillRect(x0, 10, x1 - x0 + 1, 2, w[1]);
        if (!left) img.FillRect(2, 12, 2, 4, w[1]);
        if (!right) img.FillRect(12, 12, 2, 4, w[1]);
        if (!left) img[x0, 3] = Color.Transparent;
        if (!right) img[x1, 3] = Color.Transparent;
        return img.Outline(Pal.Ink);
    }

    private static PixelImage Plant()
    {
        var c = new PropCanvas(22);
        var pot = Material.Of("7a3a2a", "b0583a", "d8804a", "f0a868");
        var leaf = new Material(Pal.Canopy[1..5], Pal.Canopy[0]);
        c.Ellipse(5, 9, 4, 6, leaf).Ellipse(11, 9, 4, 6, leaf).Ellipse(8, 6, 4, 6, leaf, contour: false);
        c.Ellipse(8, 17, 5, 4, pot);
        var src = c.Render();
        var img = new PixelImage(16, 22);
        img.Blit(src, 0, 0, 16, 22, 0, 0);
        return img;
    }

    private static PixelImage Counter(int mask)
    {
        var img = New();
        bool up = (mask & 1) != 0, right = (mask & 2) != 0, down = (mask & 4) != 0, left = (mask & 8) != 0;
        var top = Pal.Cream;
        var front = Color.Hex("8a6ac8");
        img.FillRect(0, 0, 16, 16, top);
        if (!down) { img.FillRect(0, 9, 16, 7, front); img.FillRect(0, 9, 16, 1, front.Shade(1.25f)); img.FillRect(0, 15, 16, 1, front.Shade(0.6f)); }
        if (!up) img.FillRect(0, 0, 16, 1, Pal.Ink);
        if (!left) img.FillRect(0, 0, 1, 16, Pal.Ink);
        if (!right) img.FillRect(15, 0, 1, 16, Pal.Ink);
        if (!down) img.FillRect(0, 8, 16, 1, Pal.CreamShade);
        return img;
    }

    private static PixelImage Tv()
    {
        var img = new PixelImage(16, 20);
        img.FillRect(1, 2, 14, 10, Pal.InkSoft);
        img.FillRect(3, 4, 10, 6, Color.Hex("6ac8c0"));
        img.FillRect(3, 4, 4, 2, Color.Hex("c8f4ee"));
        img.FillRect(4, 12, 8, 2, Pal.InkSoft);
        img.FillRect(2, 14, 12, 6, Pal.Wood[2]);
        img.FillRect(2, 14, 12, 1, Pal.Wood[3]);
        img[5, 1] = Pal.Ink; img[4, 0] = Pal.Ink; img[10, 1] = Pal.Ink; img[11, 0] = Pal.Ink;
        return img.Outline(Pal.Ink);
    }

    private static PixelImage Kitchen()
    {
        var img = new PixelImage(16, 22);
        img.FillRect(0, 6, 16, 16, Pal.Cream);
        img.FillRect(0, 6, 16, 3, Pal.CreamShade);
        img.FillRect(2, 11, 12, 9, Color.Hex("9aa6b8"));
        img.FillRect(3, 12, 10, 3, Color.Hex("c8d2e0"));
        img.FillRect(3, 2, 4, 4, Pal.Coral);
        img.FillRect(9, 3, 4, 3, Color.Hex("5b82c8"));
        return img.Outline(Pal.Ink);
    }

    private static PixelImage Altar()
    {
        var c = new PropCanvas(26);
        var stone = Material.Of("8a7aa8", "b4a6cc", "dcd2ec", "fbf6ff");
        var glow = Material.Of("c8608a", "f08aa8", "fbb6c8", "fff0f4");
        c.Ellipse(8, 22, 7.5f, 3.5f, stone, bulge: 0.6f);
        c.Capsule(8, 21, 8, 13, 3.5f, 3, stone);
        c.Circle(8, 9, 4.5f, glow);
        c.Dot(6.5f, 7.5f, 1.2f, Pal.White);
        var src = c.Render();
        var img = new PixelImage(16, 26);
        img.Blit(src, 0, 0, 16, 26, 0, 0);
        return img;
    }

    private static PixelImage Pedestal()
    {
        var img = Table(0);
        return img;
    }

    private static PixelImage ShopShelf()
    {
        var img = new PixelImage(16, 24);
        img.FillRect(0, 0, 16, 24, Pal.Plaster[1]);
        img.FillRect(1, 1, 14, 22, Pal.Plaster[0]);
        Color[] goods = [Pal.Coral, Color.Hex("5b82c8"), Pal.Gold, Color.Hex("5aa04f")];
        for (int s = 0; s < 3; s++)
        {
            int y = 2 + s * 7;
            for (int i = 0; i < 3; i++)
            {
                var col = goods[(s + i) % goods.Length];
                img.FillRect(2 + i * 4, y + 2, 3, 4, col);
                img.FillRect(2 + i * 4, y + 2, 3, 1, col.Shade(1.3f));
            }
            img.FillRect(1, y + 6, 14, 1, Pal.Wood[2]);
        }
        return img.Outline(Pal.Ink);
    }

    private static PixelImage Machine()
    {
        var img = new PixelImage(16, 24);
        img.FillRect(1, 2, 14, 22, Color.Hex("9aa6b8"));
        img.FillRect(1, 2, 14, 2, Color.Hex("c8d2e0"));
        img.FillRect(3, 6, 10, 6, Color.Hex("2b3a4a"));
        for (int x = 4; x < 12; x++) img[x, 9 + (int)MathF.Round(MathF.Sin(x) * 1.5f)] = Color.Hex("6ae0a0");
        img.FillRect(3, 15, 2, 2, Pal.Coral);
        img.FillRect(7, 15, 2, 2, Pal.Gold);
        img.FillRect(11, 15, 2, 2, Color.Hex("6ae0a0"));
        img.FillRect(3, 19, 10, 2, Color.Hex("6a7688"));
        return img.Outline(Pal.Ink);
    }

    private static PixelImage Statue()
    {
        var c = new PropCanvas(28);
        var stone = Material.Of("6a6070", "9a90a6", "c8c0d2", "f0ecf6");
        c.Ellipse(8, 25, 7, 2.5f, stone, bulge: 0.5f);
        c.Capsule(8, 24, 8, 17, 4, 4, stone);
        c.Circle(8, 11, 5.5f, stone);
        c.Triangle(4, 8, 3, 1, 8, 6, stone);
        c.Triangle(12, 8, 13, 1, 8, 6, stone);
        c.Dot(8, 20, 1.4f, Pal.Gold);
        var src = c.Render();
        var img = new PixelImage(16, 28);
        img.Blit(src, 0, 0, 16, 28, 0, 0);
        return img;
    }
}
