using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using TrailDuel.Rules;

namespace TrailDuel.Visuals;

/// <summary>
/// Which drawn trainer an overworld character uses: a sheet in <c>Content/characters</c>, the slot on it
/// (0-7, four per band of rows) and an optional hue turn for characters that share a drawing.
/// </summary>
public sealed record CharacterLook(string Key, int Sheet, int Slot, float HueTurn = 0)
{
    public static readonly CharacterLook Player = new("player", 2, 0);
    public static readonly CharacterLook Rival = new("rival", 1, 1);
    public static readonly CharacterLook Mom = new("mom", 1, 6);
    public static readonly CharacterLook Mentor = new("mentor", 1, 3);
    public static readonly CharacterLook Keeper = new("keeper", 2, 2);
    public static readonly CharacterLook Clerk = new("clerk", 1, 2);
    public static readonly CharacterLook Youngster = new("youngster", 2, 3);
    public static readonly CharacterLook Lass = new("lass", 2, 1);
    public static readonly CharacterLook Hiker = new("hiker", 2, 5);
    public static readonly CharacterLook Birder = new("birder", 1, 5);
    public static readonly CharacterLook Elder = new("elder", 1, 7);
    public static readonly CharacterLook Girl = new("girl", 2, 7);
    public static readonly CharacterLook Boy = new("boy", 2, 6);
    public static readonly CharacterLook Apprentice = new("apprentice", 1, 0);
    public static readonly CharacterLook Master = new("master", 2, 4);
    // The fisher shares the hiker's drawing, turned from forest green to lake blue.
    public static readonly CharacterLook Fisher = new("fisher", 2, 5, HueTurn: 0.38f);

    public static readonly CharacterLook[] All = [Player, Rival, Mom, Mentor, Keeper, Clerk, Youngster, Lass, Hiker, Birder, Elder, Girl, Boy, Apprentice, Master, Fisher];
}

/// <summary>
/// Overworld characters: hand-drawn 18x26 trainers from the Openmon collection (CC0, see CREDITS.md),
/// three walk frames in four directions. Each sheet holds eight characters in two bands; a band has
/// four rows (down, left, right, up) of three frames (step, stand, step).
/// </summary>
internal static class CharacterArt
{
    public const int W = 18, H = 26;
    /// <summary>Pixel row of the feet; battle and menus anchor characters here.</summary>
    public const int FeetY = 25;

    public static void AddAll(SpriteAtlasBuilder b, Func<string, PixelImage> image)
    {
        var sheets = new[] { image("characters/trainers1.png"), image("characters/trainers2.png") };
        foreach (var look in CharacterLook.All)
        {
            foreach (var dir in new[] { Direction.Down, Direction.Up, Direction.Left, Direction.Right })
                for (int f = 0; f < 3; f++)
                    b.Add(Key(look.Key, dir, f), Frame(sheets, look, dir, f), new Vector2(1, FeetY - 15));
            b.Add(PortraitKey(look.Key, Direction.Down), Portrait(Frame(sheets, look, Direction.Down, 0)), new Vector2(W, FeetY * 2 + 1));
            b.Add(PortraitKey(look.Key, Direction.Up), Portrait(Frame(sheets, look, Direction.Up, 0)), new Vector2(W, FeetY * 2 + 1));
        }
    }

    public static string Key(string look, Direction dir, int frame) => $"chr:{look}:{(int)dir}{frame}";

    public static string PortraitKey(string look, Direction dir) => $"portrait:{look}:{(int)dir}";

    private static PixelImage Frame(PixelImage[] sheets, CharacterLook look, Direction dir, int frame)
    {
        int row = dir switch { Direction.Down => 0, Direction.Left => 1, Direction.Right => 2, _ => 3 };
        // Frame 0 stands (the middle column); 1 and 2 are the two steps.
        int col = frame switch { 0 => 1, 1 => 0, _ => 2 };
        int x = (look.Slot % 4 * 3 + col) * W, y = (look.Slot / 4 * 4 + row) * H;
        var img = sheets[look.Sheet - 1].Crop(x, y, W, H);
        return look.HueTurn == 0 ? img : img.Map((_, _, c) => TurnHue(c, look.HueTurn));
    }

    /// <summary>
    /// The battle-size figure (36x52): the frame doubled with Scale2x, then the doubled outline thinned
    /// back to one pixel so it keeps the creatures' line weight.
    /// </summary>
    private static PixelImage Portrait(PixelImage frame)
    {
        var big = frame.Scale2x();
        var src = big.Clone();
        static bool Dark(Color c) => c.A > 0 && c.R + c.G + c.B < 150;
        bool Clear(int x, int y) => !src.InBounds(x, y) || src[x, y].A == 0;
        bool Rim(int x, int y) => Dark(src[x, y]) && (Clear(x - 1, y) || Clear(x + 1, y) || Clear(x, y - 1) || Clear(x, y + 1));
        for (int y = 0; y < src.Height; y++)
        for (int x = 0; x < src.Width; x++)
        {
            if (!Dark(src[x, y]) || Rim(x, y)) continue;
            // The inner half of a doubled outline borders the rim; it takes a lighter neighbour's colour.
            bool nearRim = false;
            Color? fill = null;
            foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
            {
                int nx = x + dx, ny = y + dy;
                if (!src.InBounds(nx, ny)) continue;
                if (Rim(nx, ny)) nearRim = true;
                else if (!Dark(src[nx, ny]) && src[nx, ny].A > 0) fill = src[nx, ny];
            }
            if (nearRim && fill is { } f) big[x, y] = f;
        }
        return big;
    }

    /// <summary>Rotates green hues only, leaving outlines, greys, whites, skin, hair and reds alone.</summary>
    private static Color TurnHue(Color c, float turn)
    {
        if (c.A == 0) return c;
        var (h, max, min) = Pal.Hue(c);
        float d = max - min;
        if (d < 0.12f || max <= 0) return c;
        if (h is < 1.2f or > 3.4f) return c; // only greens turn: skin, hair and reds stay put
        h = (h / 6 + turn) % 1 * 6;
        float x = d * (1 - MathF.Abs(h % 2 - 1));
        var (R, G, B) = h switch
        {
            < 1 => (d, x, 0f), < 2 => (x, d, 0f), < 3 => (0f, d, x), < 4 => (0f, x, d), < 5 => (x, 0f, d), _ => (d, 0f, x),
        };
        return new Color((byte)((R + min) * 255), (byte)((G + min) * 255), (byte)((B + min) * 255), c.A);
    }
}
