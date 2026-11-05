using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.World;

namespace TrailDuel.Visuals;

/// <summary>Icons, orbs, bubbles and the satchel.</summary>
internal static class UiArt
{
        public static void AddAll(SpriteAtlasBuilder b)
    {
        b.Add("orb", Orb(12, false), new Vector2(6, 6));
        b.Add("orbsuper", Orb(12, true), new Vector2(6, 6));
        b.Add("orbsmall", Orb(8, false), new Vector2(4, 4));
        b.Add("orbopen", OrbOpen(), new Vector2(7, 8));
        b.Add("itemball", ItemBall(), new Vector2(0, 0));
        b.Add("icon:potion", Bottle(Color.Hex("b86ae0")));
        b.Add("icon:superpotion", Bottle(Color.Hex("e8504a")));
        b.Add("icon:remedy", Bottle(Color.Hex("58c060")));
        b.Add("icon:orb", Orb(12, false));
        b.Add("icon:superorb", Orb(12, true));
        b.Add("bubble", Bubble(), new Vector2(0, 0));
        b.Add("sparkle", Sparkle(), new Vector2(3, 3));
        b.Add("satchel", Satchel(), new Vector2(32, 32));
    }

    // Capture Orb glass (teal) and Super Orb glass (violet), dark to light.
    private static readonly Color[] TealGlass = [Color.Hex("1f5458"), Color.Hex("2f7d78"), Color.Hex("47a596"), Color.Hex("74ccb4"), Color.Hex("d8f4ea")];
    private static readonly Color[] VioletGlass = [Color.Hex("3e2670"), Color.Hex("5c3e9e"), Color.Hex("8462c8"), Color.Hex("b294ea"), Color.Hex("efe4ff")];

    /// <summary>
    /// A capture orb: a glass globe with a glowing core under a metal cap (bronze, or gold for the Super
    /// Orb). The trail's own design, deliberately unlike a two-tone ball with a band and a centre button.
    /// </summary>
    internal static PixelImage Orb(int size, bool super, bool capped = true, bool glowing = false)
    {
        var img = new PixelImage(size, size);
        var glass = super ? VioletGlass : TealGlass;
        float r = size / 2f - 1, cx = size / 2f, cy = size - 1 - r;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            if (d > r) continue;
            // Clean bands lit from the upper left, darker toward the rim; the core glows just below the middle.
            float light = 0.5f - (dx + dy) / (r * 2.6f) - (d / r) * 0.3f;
            float cdy = dy - r * 0.2f;
            if (dx * dx + cdy * cdy <= r * r * 0.16f) light = MathF.Max(light, 0.62f);
            if (glowing) light += 0.4f;
            img[x, y] = glass[Math.Clamp((int)(light * glass.Length), 0, glass.Length - 2 + (glowing ? 1 : 0))];
        }
        // Glint on the glass.
        int gx = (int)(cx - r * 0.5f), gy = (int)(cy - r * 0.45f);
        img[gx, gy] = Pal.White;
        if (size >= 12) img[gx, gy + 1] = Color.Lerp(glass[4], Pal.White, 0.5f);
        if (capped) DrawCap(img, size, cy - r, super);
        return img.Outline(Pal.Ink);
    }

    /// <summary>The metal cap sitting on the globe's crown, with a ring on top when there is room.</summary>
    private static void DrawCap(PixelImage img, int size, float crown, bool super)
    {
        Color dark = super ? Pal.GoldDark : Pal.Wood[1], mid = super ? Pal.Gold : Pal.Wood[2], light = super ? Color.Hex("fbe8a8") : Pal.Wood[3];
        int w = size >= 10 ? 4 : 2, left = (size - w) / 2;
        int top = Math.Max(size >= 10 ? 1 : 0, (int)crown - 1);
        img.FillRect(left, top, w, 2, mid);
        img.FillRect(left, top + 1, w, 1, dark);
        img[left, top] = light;
        if (size >= 10) { img[size / 2 - 1, top - 1] = dark; img[size / 2, top - 1] = dark; }
    }

    /// <summary>The orb bursting open: the cap knocked up and off, the glass flaring.</summary>
    private static PixelImage OrbOpen()
    {
        var img = new PixelImage(14, 16);
        img.Blit(Orb(12, false, capped: false, glowing: true), 1, 4);
        var cap = new PixelImage(12, 6);
        DrawCap(cap, 12, 3, false);
        img.Blit(cap.Outline(Pal.Ink), 2, -1);
        return img;
    }

    private static PixelImage ItemBall()
    {
        var img = new PixelImage(16, 16);
        img.FillEllipse(8, 13, 5, 1.8f, Pal.Ink.WithAlpha(0.3f));
        img.Blit(Orb(10, false), 3, 3);
        return img;
    }

    private static PixelImage Bottle(Color liquid)
    {
        var img = new PixelImage(12, 12);
        img.FillRect(4, 0, 4, 2, Pal.Wood[2]);
        img.FillRect(5, 2, 2, 2, Color.Hex("d8eef8"));
        img.FillEllipse(6, 7.5f, 4.5f, 4, Color.Hex("d8eef8"));
        img.FillEllipse(6, 8.2f, 3.6f, 3, liquid);
        img[4, 6] = Pal.White; img[4, 7] = Pal.White;
        return img.Outline(Pal.Ink);
    }

    private static PixelImage Bubble()
    {
        var img = new PixelImage(12, 14);
        img.FillRect(1, 0, 10, 11, Pal.White);
        img.FillRect(0, 1, 12, 9, Pal.White);
        img.FillRect(5, 11, 3, 2, Pal.White);
        img.FillRect(5, 2, 2, 5, Pal.Coral);
        img.FillRect(5, 8, 2, 2, Pal.Coral);
        return img.Outline(Pal.Ink);
    }




    private static PixelImage Satchel()
    {
        var c = new PropCanvas(64);
        var leather = Material.Of("5e3424", "8a5236", "b47448", "d89a62");
        var flap = Material.Of("7a3a2a", "a8543a", "cc7a4c", "e8a26a");
        var brass = Material.Of("8a5a20", "c88a30", "f2c25c", "fff0a8");
        c.Capsule(14, 22, 32, 4, 2.5f, 2.5f, leather);
        c.Capsule(50, 22, 32, 4, 2.5f, 2.5f, leather);
        c.Ellipse(32, 40, 25, 19, leather, bulge: 0.6f);
        c.Ellipse(32, 30, 24, 12, flap, bulge: 0.5f);
        c.Ellipse(32, 40, 5, 4.5f, brass);
        c.Dot(31, 39, 1.5f, Pal.White);
        c.Stroke(12, 31, 52, 31, flap.Line);
        for (int x = 14; x < 52; x += 4) c.Pixel(x, 33, Pal.Cream.WithAlpha(0.6f));
        return c.Render();
    }

    private static PixelImage Sparkle()
    {
        var img = new PixelImage(7, 7);
        img[3, 0] = Pal.White; img[3, 6] = Pal.White; img[0, 3] = Pal.White; img[6, 3] = Pal.White;
        img.FillRect(3, 1, 1, 5, Pal.White);
        img.FillRect(1, 3, 5, 1, Pal.White);
        img[3, 3] = Pal.Gold;
        return img;
    }
}
