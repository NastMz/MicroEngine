using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>Treasures, pickups, weapons, effects and HUD icons.</summary>
public static class ItemArt
{
    private static string H(Color c) => ArtKit.Hex(c);

    public static void AddAll(SpriteAtlasBuilder b)
    {
        var center = (PixelImage img) => new Vector2(img.Width / 2f, img.Height / 2f);
        void Add(string name, PixelImage img) => b.Add(name, img, center(img));

        Add("gem_green", Gem(Pal.Gem));
        Add("gem_blue", Gem(Pal.GemBlue));
        Add("gem_red", Gem(Pal.GemRed));
        Add("heart", Heart(full: true, half: false));
        Add("heart_half", Heart(full: false, half: true));
        Add("heart_empty", Heart(full: false, half: false));
        Add("key_small", SmallKey());
        Add("key_boss", BossKey());
        Add("heart_container", HeartContainer());
        Add("boomerang", Boomerang());
        Add("sword", Sword());
        Add("courtyard_key", CourtyardKey());
        Add("tonic", Tonic());
        Add("pie", Pie());
        b.Add("blade", Blade(), new Vector2(2.5f, 14));
        b.Add("swoosh", Swoosh(), new Vector2(12, 12));
        for (int f = 0; f < 4; f++) Add($"puff{f}", Puff(f));
        for (int f = 0; f < 3; f++) Add($"spark{f}", Spark(f));
        Add("seed", Seed(Pal.Roof));
        Add("orb", Orb());
        Add("glow_s", ArtKit.Glow(12, Pal.White));
        Add("glow_m", ArtKit.Glow(24, Pal.White));
        Add("glow_l", ArtKit.Glow(40, Pal.White, 1.3f));
        Add("emblem", Emblem());
    }

    public static PixelImage Gem(Color[] ramp)
    {
        string pal = $"o={H(Pal.Ink)} a={H(ramp[0])} b={H(ramp[1])} c={H(ramp[2])} d={H(ramp[3])}";
        return ArtKit.Map(pal,
            "..oo..",
            ".odco.",
            "obdcbo",
            "obccbo",
            "obcbbo",
            "oabbao",
            ".oaao.",
            "..oo..");
    }

    private static PixelImage Heart(bool full, bool half)
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Heart[0])} b={H(Pal.Heart[1])} c={H(Pal.Heart[2])} d={H(Pal.Heart[3])} e={H(Pal.Night)} f={H(Pal.Stone[2])}";
        string[] rows =
        [
            ".oo.oo.",
            "obcobbo",
            "obdbbbo",
            "obbbbao",
            ".obbao.",
            "..oao..",
            "...o...",
        ];
        var img = ArtKit.Map(pal, rows);
        if (!full)
        {
            int fromX = half ? 4 : 0;
            img.Map((x, y, c) => x >= fromX && c.A > 0 && c != Pal.Ink ? (y < 2 ? Pal.Stone[2] : Pal.Night) : c);
        }
        return img;
    }

    private static PixelImage SmallKey()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Gold[0])} b={H(Pal.Gold[1])} c={H(Pal.Gold[2])} d={H(Pal.Gold[3])}";
        return ArtKit.Map(pal,
            ".ooo.",
            "ocdco",
            "oc.bo",
            "obbbo",
            ".obo.",
            ".obo.",
            ".obco",
            ".obo.",
            ".obco",
            ".obbo",
            "..oo.");
    }

    private static PixelImage BossKey()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Gold[0])} b={H(Pal.Gold[1])} c={H(Pal.Gold[2])} d={H(Pal.Gold[3])} r={H(Pal.GemRed[1])} R={H(Pal.GemRed[2])}";
        return ArtKit.Map(pal,
            "...oooo.....",
            "..occddo....",
            ".ocboobco...",
            ".obo..obo...",
            ".ocboobco...",
            "..obrRbo....",
            "...obco.....",
            "...obcooo...",
            "...obccco...",
            "...obcooo...",
            "...obcoo....",
            "...obccco...",
            "....oooo....");
    }

    private static PixelImage HeartContainer()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Heart[0])} b={H(Pal.Heart[1])} c={H(Pal.Heart[2])} d={H(Pal.Heart[3])} y={H(Pal.Gold[2])} Y={H(Pal.Gold[1])}";
        return ArtKit.Map(pal,
            "..ooo...ooo...",
            ".oyyyo.oyyyo..",
            "oyccbyoybbbyo.",
            "oycddbybbbbyo.",
            "oycdbbbbbbbYo.",
            "oybbbbbbbbbYo.",
            ".oYbbbbbbbaYo.",
            "..oYbbbbbaYo..",
            "...oYbbbaYo...",
            "....oYbaYo....",
            ".....oYYo.....",
            "......oo......");
    }

    private static PixelImage Boomerang()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Spirit[0])} b={H(Pal.Spirit[1])} c={H(Pal.Spirit[2])} d={H(Pal.Spirit[3])} y={H(Pal.Gold[2])}";
        return ArtKit.Map(pal,
            "..oooo....",
            ".occccoo..",
            "obddcbbbo.",
            "obcooobbbo",
            "obco..oyyo",
            "obco...oo.",
            "obbo......",
            "oyyo......",
            ".oo.......");
    }

    private static PixelImage Sword()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Iron[1])} b={H(Pal.Iron[2])} c={H(Pal.Iron[3])} d={H(Pal.White)} y={H(Pal.Gold[2])} Y={H(Pal.Gold[1])} w={H(Pal.Wood[1])}";
        return ArtKit.Map(pal,
            "...o...",
            "..odo..",
            "..ocbo.",
            "..ocbo.",
            "..ocbo.",
            "..ocbo.",
            "..ocao.",
            "..ocao.",
            "ooocaooo",
            "oyYyyYyo",
            ".ooywoo.",
            "...owo..",
            "...owo..",
            "...oyo..",
            "....o...");
    }

    /// <summary>The swung blade, pointing up, pivot at the hilt.</summary>
    private static PixelImage Blade()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Iron[1])} b={H(Pal.Iron[2])} c={H(Pal.Iron[3])} d={H(Pal.White)} y={H(Pal.Gold[2])} w={H(Pal.Wood[1])}";
        return ArtKit.Map(pal,
            ".o.",
            "odo",
            "oco",
            "oco",
            "obo",
            "oco",
            "obo",
            "oco",
            "obo",
            "obo",
            "oao",
            "yyy",
            "owo",
            "owo",
            "oyo",
            ".o.");
    }

    /// <summary>A quarter-circle crescent drawn behind the blade during a swing.</summary>
    private static PixelImage Swoosh()
    {
        var img = new PixelImage(24, 24);
        for (int y = 0; y < 24; y++)
            for (int x = 0; x < 24; x++)
            {
                float dx = x + 0.5f - 12, dy = y + 0.5f - 12;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d < 7 || d > 12 || dy > 0) continue;
                float a = (d - 7) / 5f;
                img[x, y] = (a > 0.75f ? Pal.White : Pal.Spirit[3]).WithAlpha(0.25f + a * 0.6f);
            }
        return img;
    }

    private static PixelImage CourtyardKey()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Gold[0])} b={H(Pal.Gold[1])} c={H(Pal.Gold[2])} d={H(Pal.Gold[3])} e={H(Pal.Spirit[1])} f={H(Pal.Spirit[2])} g={H(Pal.Spirit[3])}";
        return ArtKit.Map(pal,
            "....oooooo....",
            "...occddcco...",
            "..ocbooooboo..",
            ".ocbo.ef.obco.",
            ".obo.efgf.obo.",
            ".obo.effe.obo.",
            ".ocbo.ee.obco.",
            "..ocbooooboo..",
            "...obbccbbo...",
            ".....obco.....",
            ".....obco.....",
            ".....obccoo...",
            ".....obcddo...",
            ".....obcoo....",
            ".....obccco...",
            ".....obcddo...",
            ".....obcoo....",
            "......oo......");
    }

    private static PixelImage Tonic()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Heart[0])} b={H(Pal.Heart[1])} c={H(Pal.Heart[2])} w={H(Pal.White)} g={H(Pal.Rock[3])} k={H(Pal.Wood[2])}";
        return ArtKit.Map(pal,
            "...oo...",
            "..okko..",
            "...gg...",
            "..ogwo..",
            ".obbwbo.",
            "obcbbbbo",
            "obbbbbao",
            "obbbbaao",
            ".oaaaao.",
            "..oooo..");
    }

    private static PixelImage Pie()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Sand[1])} b={H(Pal.Sand[2])} c={H(Pal.Sand[3])} r={H(Pal.GemRed[1])} R={H(Pal.GemRed[2])}";
        return ArtKit.Map(pal,
            "...oooooo...",
            ".oocbcbcboo.",
            "ocbrRbrRbcbo",
            "obcbbcbbcbbo",
            "oaaaaaaaaaao",
            ".obababababo",
            "..oooooooo..");
    }

    private static PixelImage Puff(int frame)
    {
        var img = new PixelImage(16, 16);
        float r = 3 + frame * 1.6f;
        float fade = 1 - frame * 0.2f;
        foreach (var (dx, dy) in new[] { (-3, -2), (3, -1), (0, 3), (-2, 2), (2, -3) })
        {
            float rr = r * (0.55f + Art.Hash(dx, dy, frame) * 0.3f);
            img.FillCircle(8 + dx * (0.6f + frame * 0.3f), 8 + dy * (0.6f + frame * 0.3f), rr, Pal.Cream.WithAlpha(fade * 0.95f));
        }
        img.FillCircle(7, 7, r * 0.4f, Pal.White.WithAlpha(fade));
        if (frame < 3) img.Outline(Pal.Rock[3].WithAlpha(fade));
        return img;
    }

    private static PixelImage Spark(int frame)
    {
        var img = new PixelImage(9, 9);
        int len = 4 - frame;
        for (int i = -len; i <= len; i++)
        {
            img[4 + i, 4] = Pal.White;
            img[4, 4 + i] = Pal.White;
        }
        if (frame == 0) { img[3, 3] = Pal.Gold[3]; img[5, 5] = Pal.Gold[3]; img[3, 5] = Pal.Gold[3]; img[5, 3] = Pal.Gold[3]; }
        return img;
    }

    private static PixelImage Seed(Color[] ramp)
    {
        var img = new PixelImage(7, 7);
        img.FillCircle(3.5f, 3.5f, 3f, ramp[1]);
        img.FillCircle(3f, 3f, 1.6f, ramp[3]);
        img[2, 2] = Pal.White;
        img.Outline(Pal.Ink);
        return img;
    }

    private static PixelImage Orb()
    {
        var img = new PixelImage(10, 10);
        img.FillCircle(5, 5, 4.2f, Pal.Ember[1]);
        img.FillCircle(4.5f, 4.5f, 2.8f, Pal.Ember[2]);
        img.FillCircle(4, 4, 1.4f, Pal.Ember[3]);
        img.Outline(Pal.Ember[0]);
        return img;
    }

    /// <summary>The title emblem: the Courtyard Key inside a gilded ring.</summary>
    private static PixelImage Emblem()
    {
        var img = new PixelImage(48, 48);
        for (int y = 0; y < 48; y++)
            for (int x = 0; x < 48; x++)
            {
                float dx = x + 0.5f - 24, dy = y + 0.5f - 24, d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > 22 || d < 17) continue;
                float lit = (-dx - dy) / 44f + 0.5f;
                img[x, y] = d > 21 || d < 18 ? Pal.Gold[0] : Art.Ramp([Pal.Gold[1], Pal.Gold[2], Pal.Gold[3]], lit, x, y);
            }
        img.FillCircle(24, 24, 16.5f, Pal.Night.WithAlpha((byte)220));
        var key = CourtyardKey();
        img.Blit(key, 17, 14);
        foreach (var a in new[] { 0f, 1.57f, 3.14f, 4.71f })
        {
            int x = (int)(24 + MathF.Cos(a) * 19.5f), y = (int)(24 + MathF.Sin(a) * 19.5f);
            img.FillRect(x - 1, y - 1, 3, 3, Pal.Spirit[2]);
            img[x, y] = Pal.Spirit[3];
        }
        return img;
    }
}
