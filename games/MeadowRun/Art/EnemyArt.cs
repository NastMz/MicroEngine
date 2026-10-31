using MicroEngine;
using MicroEngine.Graphics;

namespace MeadowRun.Art;

/// <summary>Enemy sprites. Every enemy faces left by default (they walk toward Pip).</summary>
internal static class EnemyArt
{
    private static readonly Color[] Shell = [Pal.Bark, Pal.Wine, Pal.Berry, Pal.Coral];
    private static readonly Color[] Feet = [Pal.Bark, Pal.Soil, Pal.Clay, Pal.Sand];

    /// <summary>Bumblet: a ladybird-like walker. Frames 0-1 walk, 2 squashed.</summary>
    public static PixelImage Bumblet(int frame)
    {
        var img = new PixelImage(16, 16);
        if (frame == 2)
        {
            Brush.Ellipse(img, 8, 13.5f, 7, 2.5f, Shell);
            img[5, 13] = Pal.Ink; img[10, 13] = Pal.Ink; img[8, 12] = Pal.Bark;
            Brush.SelOutline(img, Pal.Ink);
            return img;
        }
        int step = frame == 0 ? 0 : 1;
        Brush.Ellipse(img, 5 - step, 14.2f, 2, 1.3f, Feet);
        Brush.Ellipse(img, 11 + step, 14.2f, 2, 1.3f, Feet);
        Brush.Ellipse(img, 8.5f, 9 - step * 0.5f, 6.5f, 5.2f, Shell);
        // Spots and the shell seam.
        img.Line(9, 4, 10, 13, Pal.Bark);
        foreach (var (x, y) in new[] { (6, 7), (12, 8), (11, 11), (7, 11) }) { img[x, y] = Pal.Bark; img[x + 1, y] = Pal.Bark; }
        // Face on the left (front).
        Brush.Ellipse(img, 3.5f, 10.5f - step * 0.5f, 3.2f, 2.8f, Pal.StoneRamp, -0.2f);
        img[2, 9] = Pal.Cream; img[2, 10] = Pal.Ink; img[4, 9] = Pal.Cream; img[4, 10] = Pal.Ink;
        img[1, 9 - step] = Pal.Ink; img[0, 8 - step] = Pal.Ink;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Thornback: a spiny hedgehog; cannot be stomped. Frames 0-1 walk.</summary>
    public static PixelImage Thornback(int frame)
    {
        var img = new PixelImage(18, 16);
        int step = frame;
        Brush.Ellipse(img, 5 - step, 14.3f, 2, 1.3f, Feet);
        Brush.Ellipse(img, 12 + step, 14.3f, 2, 1.3f, Feet);
        // Spines first so the body overlaps their roots.
        for (int i = 0; i < 7; i++)
        {
            float a = MathF.PI * (1.05f + i * 0.14f);
            int bx = (int)(9.5f + MathF.Cos(a) * 5), by = (int)(10 + MathF.Sin(a) * 4.5f);
            int tx = (int)(9.5f + MathF.Cos(a) * 9), ty = (int)(10 + MathF.Sin(a) * 8.5f) - step;
            img.Line(bx, by, tx, ty, Pal.Slate);
            img[tx, ty] = Pal.Mist;
        }
        Brush.Ellipse(img, 10, 10, 6.5f, 4.6f, [Pal.Night, Pal.Plum, Pal.Violet, Pal.Lilac]);
        Brush.Ellipse(img, 4, 11, 3.2f, 2.6f, [Pal.Soil, Pal.Clay, Pal.Sand, Pal.Wheat]);
        img[3, 10] = Pal.Ink; img[3, 9] = Pal.Cream; img[1, 11] = Pal.Ink; img[0, 11] = Pal.Bark;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Hopkin: a frog that hops toward Pip. 0 sit, 1 crouch (telegraph), 2 airborne.</summary>
    public static PixelImage Hopkin(int frame)
    {
        var img = new PixelImage(16, 16);
        float squash = frame == 1 ? 1.2f : frame == 2 ? -1f : 0;
        var skin = new[] { Pal.Pine, Pal.Teal, Pal.Aqua, Pal.Cream };
        if (frame == 2)
        {
            img.Line(9, 11, 14, 15, Pal.Teal);
            img.Line(6, 11, 2, 15, Pal.Teal);
        }
        else
        {
            Brush.Ellipse(img, 4, 14.4f, 2.6f, 1.4f, skin);
            Brush.Ellipse(img, 12, 14.4f, 2.6f, 1.4f, skin);
        }
        Brush.Ellipse(img, 8, 10 + squash * 0.8f, 6.2f + squash * 0.6f, 4.6f - squash * 0.8f, skin);
        Brush.Ellipse(img, 5, 6 + squash, 2.2f, 2.2f, skin, 0.1f);
        Brush.Ellipse(img, 10, 6 + squash, 2.2f, 2.2f, skin, 0.1f);
        int ey = (int)(6 + squash);
        img[4, ey] = Pal.Ink; img[5, ey] = Pal.Ink; img[4, ey - 1] = Pal.Cream;
        img[9, ey] = Pal.Ink; img[10, ey] = Pal.Ink; img[9, ey - 1] = Pal.Cream;
        img.Line(4, (int)(11 + squash), 10, (int)(11 + squash), Pal.Pine);
        Brush.Flat(img, 8, 12.5f + squash * 0.6f, 3, 1.2f, Pal.Butter);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Mothwing: a fluffy moth that drifts on a sine path. Frames 0-1 flap.</summary>
    public static PixelImage Mothwing(int frame)
    {
        var img = new PixelImage(18, 16);
        var wing = new[] { Pal.Plum, Pal.Violet, Pal.Lilac, Pal.Blush };
        if (frame == 0)
        {
            Brush.Ellipse(img, 12, 5, 5, 4, wing);
            Brush.Ellipse(img, 5, 5, 4, 3.4f, wing, -0.15f);
        }
        else
        {
            Brush.Ellipse(img, 12, 11, 5, 3, wing);
            Brush.Ellipse(img, 5, 11, 4, 2.6f, wing, -0.15f);
        }
        img[12, frame == 0 ? 4 : 11] = Pal.Gold;
        Brush.Ellipse(img, 9, 9, 3, 4.2f, [Pal.Bark, Pal.Clay, Pal.Wheat, Pal.Cream]);
        img[7, 7] = Pal.Ink; img[7, 6] = Pal.Cream; img[9, 7] = Pal.Ink;
        img.Line(7, 4, 5, 1, Pal.Bark); img.Line(9, 4, 10, 1, Pal.Bark);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Spitpod: a potted plant that spits thorns. 0 idle, 1 swelling (telegraph), 2 spit.</summary>
    public static PixelImage Spitpod(int frame)
    {
        var img = new PixelImage(16, 20);
        Brush.Box(img, 3, 14, 10, 6, [Pal.Bark, Pal.Rust, Pal.Clay, Pal.Wheat]);
        img.Line(2, 14, 13, 14, Pal.Wheat);
        img.Line(8, 9, 8, 14, Pal.Fern);
        Brush.Ellipse(img, 4, 12, 2.6f, 1.2f, Pal.GrassRamp);
        float swell = frame == 1 ? 1.2f : 0;
        Brush.Ellipse(img, 8, 6, 5 + swell, 4.6f + swell * 0.6f, [Pal.Plum, Pal.Wine, Pal.Berry, Pal.Coral]);
        Brush.Flat(img, 3.2f - swell * 0.6f, 6, 2 + (frame == 2 ? 0.8f : 0), 1.6f + (frame == 2 ? 0.8f : 0), Pal.Ink);
        img[8, 3] = Pal.Blush; img[10, 4] = Pal.Blush; img[6, 3] = Pal.Ink; img[6, 4] = Pal.Cream;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>
    /// Grumbold, the Cinder Mole King. 48x40 frames: 0 idle, 1 idle bob, 2 telegraph (paws raised),
    /// 3-4 charge, 5 stunned, 6 hurt flash pose, 7 slam crouch.
    /// </summary>
    public static PixelImage Grumbold(int frame)
    {
        var img = new PixelImage(48, 40);
        var fur = new[] { Pal.Ink, Pal.Bark, Pal.Soil, Pal.Clay };
        var armor = new[] { Pal.Night, Pal.Slate, Pal.Dusk, Pal.Haze };
        float bob = frame == 1 ? 1 : frame == 7 ? 4 : 0;
        float lean = frame is 3 or 4 ? -3 : frame == 5 ? 2 : 0;
        int step = frame == 4 ? 2 : 0;
        // Feet / claws.
        Brush.Ellipse(img, 14 - step + lean * 0.5f, 37, 6, 2.6f, fur);
        Brush.Ellipse(img, 33 + step, 37, 6, 2.6f, fur);
        // Body.
        Brush.Ellipse(img, 24 + lean, 25 + bob, 16, 12 - bob * 0.4f, fur);
        Brush.Flat(img, 21 + lean, 29 + bob, 8, 6, Pal.Clay);
        Brush.Flat(img, 21 + lean, 30 + bob, 6, 4, Pal.Sand);
        // Armored helmet with a crown.
        float hx = 15 + lean * 1.4f, hy = 15 + bob;
        Brush.Ellipse(img, hx, hy, 10, 8, armor);
        for (int i = 0; i < 3; i++)
        {
            int px = (int)(hx - 6 + i * 6), py = (int)(hy - 8);
            img.FillRect(px, py - 3, 3, 4, Pal.Gold);
            img[px + 1, py - 4] = Pal.Butter;
            img[px + 1, py - 1] = Pal.Berry;
        }
        img.Line((int)(hx - 9), (int)(hy - 6), (int)(hx + 9), (int)(hy - 6), Pal.Amber);
        // Snout and eyes peeking under the helmet.
        Brush.Ellipse(img, hx - 8, hy + 4, 4, 3, [Pal.Wine, Pal.Berry, Pal.Coral, Pal.Blush]);
        int ex = (int)(hx - 4), ey = (int)(hy + 1);
        if (frame == 5)
        {
            img[ex, ey] = Pal.Ink; img[ex + 1, ey + 1] = Pal.Ink; img[ex + 1, ey] = Pal.Ink; img[ex, ey + 1] = Pal.Ink;
            img[ex + 4, ey] = Pal.Ink; img[ex + 5, ey + 1] = Pal.Ink;
        }
        else
        {
            img.FillRect(ex, ey, 2, 2, frame == 2 ? Pal.Flame : Pal.Gold);
            img.FillRect(ex + 4, ey, 2, 2, frame == 2 ? Pal.Flame : Pal.Gold);
            img[ex, ey] = Pal.Cream;
        }
        // Digging claws.
        float cy = frame == 2 ? 14 + bob : 27 + bob;
        Brush.Ellipse(img, hx - 4 + lean, cy, 4, 3, [Pal.Rust, Pal.Sand, Pal.Wheat, Pal.Cream]);
        Brush.Ellipse(img, 36 + lean * 0.5f, cy + 2, 4, 3, [Pal.Rust, Pal.Sand, Pal.Wheat, Pal.Cream]);
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    /// <summary>Dirt mound that betrays the burrowing boss.</summary>
    public static PixelImage Mound(int frame)
    {
        var img = new PixelImage(32, 12);
        Brush.Ellipse(img, 16, 12, 14, 8 + frame, Pal.SoilRamp);
        for (int i = 0; i < 6; i++) img[(int)(6 + i * 4 + frame), (int)(6 - frame + (i % 2))] = Pal.Sand;
        Brush.SelOutline(img, Pal.Ink);
        return img;
    }
}
