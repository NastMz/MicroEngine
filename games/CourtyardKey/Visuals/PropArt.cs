using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// Y-sorted scenery and interactive objects drawn in code: pillars, chests, pots, signs, doors, switches and the actor
/// shadows. The houses and trees come from a pack (<see cref="PackArt"/>).
/// </summary>
public static class PropArt
{
    private static string H(Color c) => ArtKit.Hex(c);

    public static void AddAll(SpriteAtlasBuilder b)
    {
        b.Add("ruin_pillar", RuinPillar(), new Vector2(0, 32));
        b.Add("pillar", DungeonPillar(), new Vector2(0, 32));
        b.Add("chest", ArtKit.ColorOutline(Chest(false), Pal.Ink), new Vector2(8, 15));
        b.Add("chest_open", ArtKit.ColorOutline(Chest(true), Pal.Ink), new Vector2(8, 15));
        b.Add("pot", ArtKit.ColorOutline(Pot(), Pal.Ink), new Vector2(8, 15));
        b.Add("sign", ArtKit.ColorOutline(Sign(), Pal.Ink), new Vector2(8, 15));
        b.Add("block", Block(), new Vector2(0, 0));
        b.Add("crystal_blue", Crystal(Pal.Spirit), new Vector2(8, 15));
        b.Add("crystal_gold", Crystal(Pal.Gold), new Vector2(8, 15));
        b.Add("pedestal", Pedestal(), new Vector2(8, 15));
        for (int f = 0; f < 4; f++) b.Add($"flame{f}", Flame(f), new Vector2(4, 11));
        foreach (var (name, img) in Doors()) b.Add(name, img);
        b.Add("shadow_s", ShadowImage(10, 4), new Vector2(5, 2));
        b.Add("shadow_m", ShadowImage(14, 5), new Vector2(7, 2.5f));
        b.Add("shadow_l", ShadowImage(30, 8), new Vector2(15, 4));
    }

    private static PixelImage ShadowImage(int w, int h)
    {
        var img = new PixelImage(w, h);
        // The same cool dark every cast shadow in the vale uses, denser at the core.
        img.FillEllipse(w / 2f, h / 2f, w / 2f, h / 2f, Pal.CastShadow.WithAlpha((byte)85));
        img.FillEllipse(w / 2f, h / 2f, w / 2f - 1.5f, h / 2f - 1f, Pal.CastShadow.WithAlpha((byte)110));
        return img;
    }

    private static PixelImage RuinPillar()
    {
        var img = new PixelImage(16, 32);
        ArtKit.CastShadow(img, 9.5f, 29.5f, 7, 2.5f, 0.55f);
        Color[] stone = [Pal.Boulder[2], Pal.Boulder[3], Pal.Boulder[4], Pal.Boulder[5]];
        for (int y = 6; y < 30; y++)
            for (int x = 3; x < 13; x++)
            {
                float light = x < 6 ? 0.85f : x < 10 ? 0.55f : 0.25f;
                img[x, y] = Art.Ramp(stone, light + (Art.Hash(x, y, 511) - 0.5f) * 0.3f, x, y);
            }
        for (int x = 2; x < 14; x++) { img[x, 27] = stone[1]; img[x, 28] = stone[0]; img[x, 29] = stone[0]; }
        // Broken top
        for (int x = 3; x < 13; x++)
        {
            int top = 6 + (int)(Art.Hash(x, 0, 512) * 4) + (x > 8 ? 2 : 0);
            for (int y = 6; y < top; y++) img[x, y] = Color.Transparent;
            img[x, top] = stone[3];
        }
        foreach (var (x, y) in new[] { (4, 16), (5, 17), (10, 22), (11, 21), (6, 25) }) img[x, y] = Pal.Lichen[2];
        img.Line(8, 12, 9, 19, stone[0]);
        img.Outline(Pal.Ink);
        return ArtKit.ColorOutline(img, Pal.Ink);
    }

    private static PixelImage DungeonPillar()
    {
        var img = new PixelImage(16, 32);
        var st = Pal.Flagstone;
        for (int x = 2; x < 16; x++)
            for (int y = 27; y < 32; y++)
                if (y < 30 || Art.Dither(x, y, 0.5f)) ArtKit.Soft(img, x, y, Pal.DeepShadow, 0.55f);
        for (int y = 4; y < 30; y++)
            for (int x = 3; x < 13; x++)
            {
                float light = x < 6 ? 0.85f : x < 10 ? 0.55f : 0.22f;
                img[x, y] = Art.Ramp(st[1..], light + (Art.Hash(x, y, 521) - 0.5f) * 0.2f, x, y);
            }
        img.FillRect(1, 2, 14, 4, st[4]);
        img.FillRect(1, 2, 14, 1, st[6]);
        img.FillRect(13, 2, 2, 4, st[3]);
        img.FillRect(1, 26, 14, 4, st[3]);
        img.FillRect(1, 26, 14, 1, st[5]);
        img.FillRect(13, 26, 2, 4, st[2]);
        img.FillRect(7, 12, 2, 6, Pal.Spirit[1]);
        img[7, 13] = Pal.Spirit[3];
        img.Outline(Pal.Ink);
        return ArtKit.ColorOutline(img, Pal.Ink);
    }

    private static PixelImage Chest(bool open)
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Wood[0])} b={H(Pal.Wood[1])} c={H(Pal.Wood[2])} d={H(Pal.Wood[3])} y={H(Pal.Gold[2])} Y={H(Pal.Gold[1])} w={H(Pal.Gold[3])} k={H(Pal.Night)}";
        string[] closed =
        [
            "................",
            "................",
            "..oooooooooooo..",
            ".occcccccccccco.",
            ".odddddddddddco.",
            ".oYyyyyyyyyyyYo.",
            ".occccoyyocccco.",
            ".oaaaaowYoaaaao.",
            ".oYyyyoyyoyyyYo.",
            ".obbbbboobbbbbo.",
            ".occcccccccccco.",
            ".obbbbbbbbbbbbo.",
            ".oYyyyyyyyyyyYo.",
            ".oaaaaaaaaaaaao.",
            "..oooooooooooo..",
            "................",
        ];
        string[] opened =
        [
            "..oooooooooooo..",
            ".oaaaaaaaaaaaao.",
            ".obbbbbbbbbbbbo.",
            ".oYyyyyyyyyyyYo.",
            ".okkkkkkkkkkkko.",
            ".okkkkkkkkkkkko.",
            ".oYyyyyyyyyyyYo.",
            ".oaaaaoyyoaaaao.",
            ".oYyyyoyyoyyyYo.",
            ".obbbbboobbbbbo.",
            ".occcccccccccco.",
            ".obbbbbbbbbbbbo.",
            ".oYyyyyyyyyyyYo.",
            ".oaaaaaaaaaaaao.",
            "..oooooooooooo..",
            "................",
        ];
        var img = ArtKit.Map(pal, open ? opened : closed);
        var shadowed = new PixelImage(16, 16);
        ArtKit.Shadow(shadowed, 8, 14.5f, 7.5f, 1.5f, 110);
        shadowed.Blit(img, 0, 0);
        return shadowed;
    }

    private static PixelImage Pot()
    {
        var img = new PixelImage(16, 16);
        ArtKit.Shadow(img, 8, 14.5f, 6, 1.6f, 110);
        Color[] clay = [Color.Hex("5a2e22"), Color.Hex("8c4a2e"), Color.Hex("b8703e"), Color.Hex("e0a060")];
        var pot = new PixelImage(16, 16);
        ArtKit.Blob(pot, 8, 9.5f, 6, 5, clay, 531, 0.1f);
        pot.FillRect(5, 2, 6, 3, clay[1]);
        pot.FillRect(4, 2, 8, 1, clay[3]);
        pot.FillRect(6, 3, 4, 1, Pal.Ink);
        pot.Line(3, 9, 12, 9, clay[0]);
        pot.Line(4, 10, 11, 10, clay[3]);
        pot.Outline(Pal.Ink);
        img.Blit(pot, 0, 0);
        return img;
    }

    private static PixelImage Sign()
    {
        var img = new PixelImage(16, 16);
        ArtKit.Shadow(img, 8, 14.5f, 5, 1.5f, 110);
        img.FillRect(7, 9, 2, 6, Pal.Wood[1]);
        img.FillRect(1, 2, 14, 9, Pal.Wood[2]);
        img.FillRect(1, 2, 14, 1, Pal.Wood[3]);
        img.FillRect(1, 10, 14, 1, Pal.Wood[0]);
        for (int y = 4; y < 9; y += 2) img.FillRect(3, y, 10, 1, Pal.Wood[0]);
        img.Outline(Pal.Ink);
        return img;
    }

    private static PixelImage Block()
    {
        // A dressed block, a step lighter than the flagstones so the puzzle reads: lit top, shaded front.
        var st = Pal.Flagstone;
        var img = new PixelImage(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                img[x, y] = Art.Ramp([st[4], st[5], st[6]], 0.3f + Art.ValueNoise(x, y, 4, 541) * 0.5f, x, y);
        img.FillRect(0, 0, 16, 2, st[7]);
        img.FillRect(14, 0, 2, 12, st[4]);
        img.FillRect(0, 12, 16, 4, st[3]);
        img.FillRect(0, 12, 16, 1, st[4]);
        img.DrawRect(3, 4, 10, 6, st[4]);
        img[4, 5] = st[7];
        img.DrawRect(0, 0, 16, 16, Pal.Ink);
        return ArtKit.ColorOutline(img, Pal.Ink);
    }

    private static PixelImage Crystal(Color[] ramp)
    {
        var img = new PixelImage(16, 16);
        ArtKit.Shadow(img, 8, 14.5f, 6, 1.6f, 120);
        img.FillRect(4, 12, 8, 3, Pal.Stone[2]);
        img.FillRect(4, 12, 8, 1, Pal.Stone[4]);
        string pal = $"o={H(Pal.Ink)} a={H(ramp[0])} b={H(ramp[1])} c={H(ramp[2])} d={H(ramp[3])}";
        var gem = ArtKit.Map(pal,
            "......oo......",
            ".....obco.....",
            "....obcdco....",
            "...obbcddco...",
            "..obbbcdcbco..",
            "..obbbcccbbo..",
            "..oabbcccbao..",
            "...oabbcbao...",
            "....oabbao....",
            ".....oaao.....",
            "......oo......");
        img.Blit(gem, 1, 1);
        return img;
    }

    private static PixelImage Pedestal()
    {
        var img = new PixelImage(16, 16);
        ArtKit.Shadow(img, 8, 14.5f, 7, 1.8f, 120);
        img.FillRect(2, 5, 12, 3, Pal.Stone[4]);
        img.FillRect(2, 5, 12, 1, Pal.Stone[5]);
        img.FillRect(4, 8, 8, 5, Pal.Stone[3]);
        img.FillRect(4, 8, 2, 5, Pal.Stone[4]);
        img.FillRect(3, 12, 10, 3, Pal.Stone[2]);
        img.FillRect(7, 9, 2, 2, Pal.Gold[2]);
        img.Outline(Pal.Ink);
        return img;
    }

    private static PixelImage Flame(int frame)
    {
        var img = new PixelImage(8, 12);
        float sway = MathF.Sin(frame * MathF.PI / 2) * 0.8f;
        img.FillEllipse(4 + sway * 0.5f, 7.5f, 3.2f, 4f, Pal.Ember[1]);
        img.FillEllipse(4 + sway, 6f - frame % 2, 2.2f, 3.5f, Pal.Ember[2]);
        img.FillEllipse(4 + sway, 7.5f, 1.2f, 2f, Pal.Ember[3]);
        img[(int)(4 + sway), 1 + frame % 2] = Pal.Ember[2];
        return img;
    }

    /// <summary>Door panels for a two-tile opening, authored for the north wall and rotated for the others.</summary>
    private static IEnumerable<(string, PixelImage)> Doors()
    {
        var kinds = new Dictionary<string, PixelImage>
        {
            ["locked"] = LockedDoor(),
            ["shutter"] = Shutter(),
            ["bigdoor"] = BigDoor(),
        };
        foreach (var (kind, north) in kinds)
        {
            yield return ($"door_{kind}_n", north);
            yield return ($"door_{kind}_e", north.Rotate90());
            yield return ($"door_{kind}_s", north.Rotate90().Rotate90());
            yield return ($"door_{kind}_w", north.Rotate90().Rotate90().Rotate90());
        }
    }

    private static PixelImage LockedDoor()
    {
        var img = new PixelImage(32, 16);
        img.FillRect(0, 0, 32, 16, Pal.Wood[1]);
        for (int x = 0; x < 32; x += 4) { img.FillRect(x, 0, 1, 16, Pal.Wood[0]); img.FillRect(x + 1, 0, 1, 16, Pal.Wood[2]); }
        img.FillRect(0, 3, 32, 2, Pal.Iron[1]);
        img.FillRect(0, 11, 32, 2, Pal.Iron[1]);
        img.FillRect(0, 3, 32, 1, Pal.Iron[2]);
        img.FillRect(0, 11, 32, 1, Pal.Iron[2]);
        // Padlock
        img.FillRect(12, 5, 8, 7, Pal.Gold[1]);
        img.FillRect(12, 5, 8, 1, Pal.Gold[3]);
        img.DrawRect(13, 2, 6, 5, Pal.Gold[0]);
        img.FillRect(15, 7, 2, 3, Pal.Ink);
        img.DrawRect(12, 5, 8, 7, Pal.Gold[0]);
        img.DrawRect(0, 0, 32, 16, Pal.Ink);
        return img;
    }

    private static PixelImage Shutter()
    {
        var img = new PixelImage(32, 16);
        img.FillRect(0, 0, 32, 16, Pal.Ink);
        for (int x = 2; x < 32; x += 5)
        {
            img.FillRect(x, 0, 3, 16, Pal.Iron[1]);
            img.FillRect(x, 0, 1, 16, Pal.Iron[3]);
            img.FillRect(x + 2, 0, 1, 16, Pal.Iron[0]);
            img.FillRect(x, 13, 3, 3, Pal.Iron[2]);
            img[x + 1, 15] = Pal.Iron[3];
        }
        img.FillRect(0, 5, 32, 2, Pal.Iron[2]);
        img.FillRect(0, 5, 32, 1, Pal.Iron[3]);
        return img;
    }

    private static PixelImage BigDoor()
    {
        var img = new PixelImage(32, 16);
        img.FillRect(0, 0, 32, 16, Pal.Carpet[0]);
        for (int x = 0; x < 32; x += 3) img.FillRect(x, 0, 1, 16, Pal.Carpet[1]);
        img.DrawRect(0, 0, 32, 16, Pal.Gold[1]);
        img.DrawRect(1, 1, 30, 14, Pal.Gold[2]);
        img.FillRect(15, 1, 2, 14, Pal.Gold[1]);
        // Ornate lock plate with the courtyard emblem.
        img.FillEllipse(16, 8, 6, 6, Pal.Gold[1]);
        img.FillEllipse(16, 8, 4.5f, 4.5f, Pal.Gold[2]);
        img.FillEllipse(15.5f, 7.5f, 2, 2, Pal.Gold[3]);
        img.FillRect(15, 7, 2, 5, Pal.Ink);
        img.FillRect(14, 6, 4, 2, Pal.Ink);
        return img;
    }
}
