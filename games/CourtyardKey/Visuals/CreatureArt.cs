using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// Enemies and the boss, sculpted procedurally: soft creatures from lit blobs, armoured ones from char maps.
/// The seed-spitter comes from a crafted sheet instead (<see cref="MonsterArt"/>).
/// </summary>
public static class CreatureArt
{
    private static string H(Color c) => ArtKit.Hex(c);

    public static void AddAll(SpriteAtlasBuilder b)
    {
        // Slime: idle, squash (about to hop), stretch (airborne), each with a white flash twin.
        var feet = new Vector2(8, 14);
        AddFlash(b, "slime0", Slime(6.5f, 5.5f), feet);
        AddFlash(b, "slime1", Slime(7.5f, 4.2f), feet);
        AddFlash(b, "slime2", Slime(5.2f, 6.8f), feet);
        var batOrigin = new Vector2(8, 6);
        for (int f = 0; f < 3; f++) AddFlash(b, $"bat{f}", Bat(f), batOrigin);
        foreach (var (name, img) in Knight()) AddFlash(b, name, img, new Vector2(8, 15));
        foreach (var (name, img) in Boss()) AddFlash(b, name, img, new Vector2(WardenW / 2f, WardenFeet));
    }

    private static void AddFlash(SpriteAtlasBuilder b, string name, PixelImage img, Vector2 origin)
    {
        b.Add(name, img, origin);
        b.Add(name + "!", img.Silhouette(Pal.White), origin);
    }

    private static PixelImage Slime(float rx, float ry)
    {
        var img = new PixelImage(16, 16);
        float cy = 14.5f - ry;
        ArtKit.Blob(img, 8, cy, rx, ry, Pal.Slime, 201, 0.1f);
        // Flat belly: the bottom rows of a dome are cut so it sits on the ground.
        for (int x = 0; x < 16; x++) for (int y = 15; y < 16; y++) img[x, y] = Color.Transparent;
        img.FillEllipse(8 - rx * 0.35f, cy - ry * 0.45f, rx * 0.28f, ry * 0.22f, Pal.Slime[3]);
        int eyeY = (int)(cy + ry * 0.05f);
        img[(int)(8 - rx * 0.35f), eyeY] = Pal.Ink;
        img[(int)(8 - rx * 0.35f), eyeY + 1] = Pal.Ink;
        img[(int)(8 + rx * 0.35f), eyeY] = Pal.Ink;
        img[(int)(8 + rx * 0.35f), eyeY + 1] = Pal.Ink;
        img.Outline(Pal.Slime[0]);
        return img;
    }

    private static PixelImage Bat(int frame)
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Bat[0])} b={H(Pal.Bat[1])} c={H(Pal.Bat[2])} d={H(Pal.Bat[3])} r={H(Pal.Heart[2])} w={H(Pal.White)}";
        string[][] frames =
        [
            [
                "oo............oo",
                "obo..........obo",
                "obbo..o..o..obbo",
                "obcbo.oooo.obcbo",
                ".obcboobcbobcbo.",
                ".obbbbcrrcbbbbo.",
                "..oobbbcccbboo..",
                "....oobwwboo....",
                "......oooo......",
                "................",
                "................",
                "................",
            ],
            [
                "................",
                "................",
                "......o..o......",
                "......oooo......",
                ".oooooobcbooooo.",
                "obbbbbcrrcbbbbbo",
                "obcccbbcccbbccbo",
                ".oobbooooooobbo.",
                "...oo.owwo..oo..",
                "......oooo......",
                "................",
                "................",
            ],
            [
                "................",
                "................",
                "......o..o......",
                "......oooo......",
                "......obcbo.....",
                "...oobcrrcboo...",
                "..obbbbcccbbbo..",
                ".obcbboooobbcbo.",
                ".obbo.owwo.obbo.",
                "obbo..oooo..obbo",
                "obo..........obo",
                "oo............oo",
            ],
        ];
        return ArtKit.Map(pal, frames[frame]);
    }


    private static IEnumerable<(string, PixelImage)> Knight()
    {
        string pal = $"o={H(Pal.Ink)} a={H(Pal.Iron[0])} b={H(Pal.Iron[1])} c={H(Pal.Iron[2])} d={H(Pal.Iron[3])} " +
                     $"r={H(Pal.Scarf[1])} R={H(Pal.Scarf[0])} y={H(Pal.Gold[2])} Y={H(Pal.Gold[1])} e={H(Pal.Spirit[2])} w={H(Pal.Wood[1])}";
        string[] down =
        [
            "......rr........",
            ".....oRro.......",
            "....oooooo......",
            "...obccccbo.....",
            "...ocddddco.....",
            "...oaeaaeao.....",
            "...obbbbbbo.....",
            "..obccccccbo....",
            ".oYYYYYYYbcbo...",
            ".oyyyyyyyYcco...",
            ".oyYrrrryYbbo...",
            ".oyYrRRryYbo....",
            ".oyYrRRryYo.....",
            "..oyyyyyYo......",
            "...oYYYYo.......",
            "....oooo........",
        ];
        string[] up =
        [
            "........rr......",
            ".......orRo.....",
            "......oooooo....",
            ".....obccccbo...",
            ".....obccccbo...",
            ".....obbbbbbo...",
            ".....oaaaaaao...",
            "....obccccccbo..",
            "...obcccccccYo..",
            "...obcbbbbcbYo..",
            "...obcbccbcbYo..",
            "...obbbccbbbo...",
            "....obbbbbbo....",
            "....obbo.obbo...",
            "....oaao.oaao...",
            "....oooo.oooo...",
        ];
        string[] side =
        [
            ".......rr.......",
            "......oRro......",
            ".....ooooooo....",
            "....obcccccbo...",
            "....obcdddddo...",
            "....obaaaaeao...",
            "....obbbbbbbo...",
            "...obccccccoyo..",
            "...obccccccoyYo.",
            "...obbbbbbboyYo.",
            "...owcccccboyYo.",
            "...owbbbbbboyYo.",
            "....obbbbbboyo..",
            "....obbo.obboo..",
            "....oaao.oaao...",
            "....oooo.oooo...",
        ];
        foreach (var (name, rows) in new[] { ("down", down), ("up", up), ("side", side) })
        {
            var a = ArtKit.Map(pal, rows);
            yield return ($"knight_{name}0", a);
            // Second walk frame: alternate feet by shifting the leg rows one pixel.
            var b = new PixelImage(16, 16);
            b.Blit(a, 0, 0, 16, 13, 0, 0);
            b.Blit(a, 0, 13, 8, 3, 0, 12);
            b.Blit(a, 8, 13, 8, 3, 8, 13);
            yield return ($"knight_{name}1", b);
        }
    }

    /// <summary>The Moss Warden's canvas and where its feet sit on it.</summary>
    public const int WardenW = 64, WardenH = 60, WardenFeet = 56;

    /// <summary>
    /// The Moss Warden: an ancient stone guardian that has grown into the shrine. Boulder shoulders,
    /// long knuckle-dragging arms, a small head sunk between them, a sapling rooted in its crown and a
    /// rune core in the chest. Poses: idle (two breaths), tell (fists raised), dazed (slumped, core
    /// split open) and rage (embers in every crack).
    /// </summary>
    private static IEnumerable<(string, PixelImage)> Boss()
    {
        yield return ("boss_idle0", Warden(core: Pal.Spirit, eyes: Pal.Spirit[3], arms: 0, open: false));
        yield return ("boss_idle1", Warden(core: Pal.Spirit, eyes: Pal.Spirit[3], arms: 1, open: false));
        yield return ("boss_tell", Warden(core: Pal.Ember, eyes: Pal.Ember[3], arms: 2, open: false));
        yield return ("boss_dazed", Warden(core: Pal.Spirit, eyes: Pal.Rock[2], arms: 3, open: true));
        yield return ("boss_rage0", Warden(core: Pal.Ember, eyes: Pal.Ember[3], arms: 0, open: false));
        yield return ("boss_rage1", Warden(core: Pal.Ember, eyes: Pal.Ember[3], arms: 1, open: false));
    }

    private static PixelImage Warden(Color[] core, Color eyes, int arms, bool open)
    {
        const int W = WardenW, H = WardenH, F = WardenFeet;
        bool rage = core == Pal.Ember && arms < 2;
        bool dazed = arms == 3;
        float breathe = arms == 1 ? 1 : 0;
        // Slumped forward when dazed: everything above the legs drops and the head hangs.
        float sag = dazed ? 4 : 0;
        Color[] stone = [Pal.Rock[0], Pal.Rock[1], Pal.Rock[2], Pal.Rock[3], Pal.Rock[4]];
        Color[] moss = [Pal.Moss[0], Pal.Moss[1], Pal.Moss[2], Pal.Leaf[3]];

        var img = new PixelImage(W, H);
        ArtKit.Shadow(img, W / 2f, F, 24, 4f, 130);

        PixelImage Part(Action<PixelImage> paint)
        {
            var p = new PixelImage(W, H);
            paint(p);
            return p.Outline(Pal.Ink);
        }

        // Legs: short, thick pillars with mossy knees.
        foreach (int lx in new[] { 23, 41 })
            img.Blit(Part(p =>
            {
                ArtKit.Blob(p, lx, F - 5, 6, 6, stone, 401 + lx, 0.18f);
                ArtKit.Blob(p, lx - 1, F - 9, 4, 2.5f, moss, 402 + lx, 0.25f);
            }), 0, 0);

        // Arms. Idle: hanging to the ground with fists planted. Tell: raised overhead. Dazed: limp.
        foreach (int side in new[] { -1, 1 })
        {
            float sx = W / 2f + side * 19, sy = 24 + sag + breathe;
            (float X, float Y) fist = arms switch
            {
                2 => (W / 2f + side * 21, 7),
                3 => (W / 2f + side * 23, F - 3),
                _ => (W / 2f + side * 24, F - 6 + breathe * 0.5f),
            };
            img.Blit(Part(p =>
            {
                // Upper and fore arm as a chain of stones between shoulder and fist.
                for (int k = 1; k <= 3; k++)
                {
                    float t = k / 4f;
                    ArtKit.Blob(p, sx + (fist.X - sx) * t, sy + (fist.Y - sy) * t, 5.2f - k * 0.3f, 5f - k * 0.3f, stone, 410 + k + side, 0.2f);
                }
                ArtKit.Blob(p, fist.X, fist.Y, 7, 6, stone, 415 + side, 0.18f);
                // Knuckle ridge.
                for (int k = -2; k <= 2; k++) p[(int)(fist.X + k * 2), (int)(fist.Y + (arms == 2 ? -4 : 3))] = Pal.Rock[0];
                // Vines trailing from the wrist.
                for (int k = 0; k < 7; k++)
                {
                    int vx = (int)(fist.X - side * 3 + MathF.Sin(k * 0.9f) * 1.5f), vy = (int)(fist.Y + (arms == 2 ? 5 : -6) + k * (arms == 2 ? 1.4f : -0.6f));
                    if (p.InBounds(vx, vy)) p[vx, vy] = k % 3 == 2 ? Pal.Leaf[3] : Pal.Leaf[2];
                }
            }), 0, 0);
        }

        // Torso: a wide boulder, broader at the shoulders, wrapped in a moss mantle.
        img.Blit(Part(p =>
        {
            ArtKit.Blob(p, W / 2f, 36 + sag, 15, 13, stone, 421, 0.2f);
            foreach (int side in new[] { -1, 1 })
                ArtKit.Blob(p, W / 2f + side * 15, 25 + sag + breathe, 9.5f, 8.5f, stone, 422 + side, 0.18f);
            // Moss pours over the shoulders and down the back.
            for (int x = 6; x < W - 6; x++)
            {
                int depth = 3 + (int)(Art.ValueNoise(x, 0, 4, 423) * 6);
                for (int y = 0; y < H; y++)
                {
                    if (p[x, y].A == 0) continue;
                    int top = y;
                    for (int yy = top; yy < Math.Min(H, top + depth); yy++)
                        if (p[x, yy].A > 0) p[x, yy] = Art.Ramp(moss, 0.25f + Art.Hash(x, yy, 424) * 0.7f, x, yy);
                    break;
                }
            }
            // Hanging moss strands with a few blossoms.
            for (int k = 0; k < 9; k++)
            {
                int hx = 12 + k * 5 + (int)(Art.Hash(k, 1, 425) * 3);
                int topY = -1;
                for (int y = 0; y < H; y++) if (p[hx, y].A > 0) { topY = y; break; }
                if (topY < 0) continue;
                int len = 5 + (int)(Art.Hash(k, 2, 425) * 6);
                for (int y = topY; y < topY + len && y < H; y++) if (p[hx, y].A > 0) p[hx, y] = y % 2 == 0 ? Pal.Moss[1] : Pal.Moss[2];
                if (k % 3 == 1) p[hx, topY + len - 1] = Pal.Heart[3];
            }
            // Rune cracks across the chest, glowing with embers in a rage.
            var crack = rage ? Pal.Ember[2] : Pal.Rock[0];
            p.Line(W / 2 - 11, (int)(38 + sag), W / 2 - 6, (int)(44 + sag), crack);
            p.Line(W / 2 + 10, (int)(37 + sag), W / 2 + 6, (int)(45 + sag), crack);
            p.Line(W / 2 - 3, (int)(28 + sag), W / 2 - 1, (int)(32 + sag), crack);
            // The heart-core: a carved ring with the light inside; split wide open when dazed.
            float cx = W / 2f, cy = 37 + sag;
            p.FillEllipse(cx, cy, 6.5f, 6, Pal.Rock[1]);
            p.FillEllipse(cx, cy, 5, 4.6f, Pal.Ink);
            if (open)
            {
                p.FillEllipse(cx, cy, 4.2f, 3.8f, core[2]);
                p.FillEllipse(cx - 0.8f, cy - 0.8f, 2.2f, 2, core[3]);
                p.Line((int)cx, (int)cy - 6, (int)cx - 2, (int)cy - 10, Pal.Ink);
                p.Line((int)cx, (int)cy + 6, (int)cx + 2, (int)cy + 9, Pal.Ink);
            }
            else
            {
                p.FillEllipse(cx, cy, 3, 2.8f, core[1]);
                p.FillEllipse(cx - 0.5f, cy - 0.5f, 1.6f, 1.4f, core[2]);
                p[(int)cx - 1, (int)cy - 1] = core[3];
            }
        }), 0, 0);

        // Head: small and low between the shoulders, under a heavy mossy brow, with a sapling crown.
        float hy = 18 + sag * 1.6f + breathe;
        img.Blit(Part(p =>
        {
            ArtKit.Blob(p, W / 2f, hy, 7.5f, 6.5f, stone, 431, 0.15f);
            ArtKit.Blob(p, W / 2f, hy - 4, 8.5f, 3, moss, 432, 0.3f);
            // Deep eye slot and the glowing eyes.
            p.FillRect(W / 2 - 5, (int)hy - 1, 10, 3, Pal.Ink);
            p.FillRect(W / 2 - 4, (int)hy, 2, 1, eyes);
            p.FillRect(W / 2 + 2, (int)hy, 2, 1, eyes);
            if (!dazed) { p[W / 2 - 4, (int)hy - 1] = eyes; p[W / 2 + 3, (int)hy - 1] = eyes; }
            // Jaw line.
            p.Line(W / 2 - 3, (int)hy + 4, W / 2 + 3, (int)hy + 4, Pal.Rock[0]);
        }), 0, 0);
        // The sapling: a crooked trunk and a little leafy canopy, leaning with the head.
        img.Blit(Part(p =>
        {
            int tx = W / 2 + 2, ty = (int)hy - 6;
            p.Line(tx, ty, tx + 1, ty - 4, Pal.Wood[2]);
            p.Line(tx + 1, ty - 4, tx - 1, ty - 7, Pal.Wood[2]);
            p.Line(tx + 1, ty - 4, tx + 4, ty - 6, Pal.Wood[1]);
            ArtKit.Blob(p, tx - 1, ty - 9, 4.5f, 3.5f, Pal.Leaf, 441, 0.3f);
            ArtKit.Blob(p, tx + 4, ty - 7, 3.2f, 2.6f, Pal.Leaf, 442, 0.3f);
            if (rage) { p[tx - 2, ty - 10] = Pal.Ember[3]; p[tx + 4, ty - 8] = Pal.Ember[2]; }
            else { p[tx - 2, ty - 10] = Pal.Heart[3]; }
        }), 0, 0);
        // Embers rising off the shoulders in a rage.
        if (rage)
            foreach (var (ex, ey) in new[] { (14, 15), (50, 14), (18, 9), (47, 8) })
                img[ex, ey + (int)breathe] = Pal.Ember[ex % 2 == 0 ? 2 : 3];
        return img;
    }
}
