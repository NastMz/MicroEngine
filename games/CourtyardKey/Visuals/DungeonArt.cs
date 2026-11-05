using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// The Sunken Courtyard's stonework, lit from the upper left on the eight-step <see cref="Pal.Flagstone"/> ramp.
/// Irregular flagstones repeat only every 8x8 tiles (<c>flagstone0</c>..<c>flagstone63</c>, picked by world
/// position); cracks, plates, statues and braziers are transparent overlays on them. Walls are ashlar faces under a
/// lit cap, and throw a shadow onto the floor beside them (<c>shade_n</c>, <c>shade_w</c>).
/// </summary>
public static class DungeonArt
{
    public const int S = 16, Period = 8, Macro = S * Period;

    public static void AddAll(SpriteAtlasBuilder b)
    {
        var floor = MacroFloor();
        for (int ty = 0; ty < Period; ty++)
            for (int tx = 0; tx < Period; tx++)
                b.Add($"flagstone{tx + ty * Period}", floor.Crop(tx * S, ty * S, S, S));
        b.Add("cracked", CrackOverlay());
        b.Add("statue", StatueOverlay());
        b.Add("brazier", BrazierOverlay());
        b.Add("plate_up", PlateOverlay(false));
        b.Add("plate_down", PlateOverlay(true));
        foreach (var (name, img) in Walls()) b.Add(name, img);
        foreach (var (name, img) in DoorFrames(floor.Crop(0, 0, S, S))) b.Add(name, img);
    }

    private static int Wrap(int v) => ((v % Macro) + Macro) % Macro;

    /// <summary>Irregular flagstones by recursive splitting; each stone bevelled from the upper-left light, moss in some joints.</summary>
    public static PixelImage MacroFloor()
    {
        var img = new PixelImage(Macro, Macro);
        var st = Pal.Flagstone;
        Color[] body = [st[2], st[3], st[4]];
        var rects = new List<(int X, int Y, int W, int H)>();
        void Split(int x, int y, int w, int h, int depth)
        {
            float r = Art.Hash(x * 7 + w, y * 13 + h, 601 + depth);
            bool canW = w > 30, canH = h > 24;
            if ((!canW && !canH) || (depth > 3 && r < 0.18f && w < 34 && h < 30)) { rects.Add((x, y, w, h)); return; }
            bool vertical = canW && (!canH || w * 0.9f > h || r > 0.6f);
            if (vertical)
            {
                int cut = 13 + (int)(Art.Hash(x, y, 602 + depth) * (w - 26));
                Split(x, y, cut, h, depth + 1); Split(x + cut, y, w - cut, h, depth + 1);
            }
            else
            {
                int cut = 11 + (int)(Art.Hash(x, y, 603 + depth) * (h - 22));
                Split(x, y, w, cut, depth + 1); Split(x, y + cut, w, h - cut, depth + 1);
            }
        }
        Split(0, 0, Macro, Macro, 0);
        int id = 0;
        foreach (var (rx, ry, rw, rh) in rects)
        {
            id++;
            float tone = 0.34f + Art.Hash(id, 0, 611) * 0.22f;
            bool worn = Art.Hash(id, 1, 611) > 0.8f;
            for (int y = ry; y < ry + rh; y++)
                for (int x = rx; x < rx + rw; x++)
                {
                    int lx = x - rx, ly = y - ry;
                    Color c;
                    if (lx == rw - 1 || ly == rh - 1) c = st[1];
                    else if (lx == 0 || ly == 0) c = lx == 0 && ly == 0 ? st[5] : st[4];
                    else if (lx == rw - 2 || ly == rh - 2) c = st[2];
                    else
                    {
                        float n = ArtKit.PeriodicNoise(x, y, 8, Macro, 621) * 0.5f + Art.Hash(x, y, 622) * 0.25f + tone - 0.12f;
                        if (worn) n += 0.06f;
                        c = ArtKit.Band(body, Math.Clamp(n, 0, 1), x, y, 0.3f);
                    }
                    img[x, y] = c;
                }
            // Occasional crack running across the stone.
            if (Art.Hash(id, 2, 611) > 0.86f && rw > 10 && rh > 8)
            {
                int cx = rx + 3 + (int)(Art.Hash(id, 3, 611) * (rw - 8)), cy = ry + 2;
                for (int k = 0; k < rh - 5; k++)
                {
                    img[cx, cy + k] = st[1];
                    if (img.InBounds(cx - 1, cy + k) && k % 2 == 0) img[cx - 1, cy + k] = st[4];
                    if (Art.Hash(id, k, 612) > 0.6f) cx += Art.Hash(id, k, 613) > 0.5f ? 1 : -1;
                    cx = Math.Clamp(cx, rx + 2, rx + rw - 3);
                }
            }
        }
        // Moss creeping along some mortar joints.
        for (int y = 0; y < Macro; y++)
            for (int x = 0; x < Macro; x++)
                if (img[x, y] == st[1] && ArtKit.PeriodicNoise(x, y, 16, Macro, 631) > 0.68f && Art.Hash(x, y, 632) > 0.35f)
                {
                    img[x, y] = Pal.Lichen[Art.Hash(x, y, 633) > 0.5f ? 1 : 0];
                    if (Art.Hash(x, y, 634) > 0.7f) img[Wrap(x + 1), y] = Pal.Lichen[2];
                }
        return img;
    }

    private static PixelImage CrackOverlay()
    {
        var img = new PixelImage(S, S);
        var st = Pal.Flagstone;
        (int, int)[] path = [(1, 3), (3, 4), (5, 6), (7, 7), (8, 9), (8, 11), (7, 13), (6, 15)];
        for (int i = 0; i + 1 < path.Length; i++)
        {
            var (ax, ay) = path[i];
            var (bx, by) = path[i + 1];
            img.Line(ax, ay, bx, by, st[0]);
            img.Line(ax + 1, ay, bx + 1, by, st[1]);
        }
        img.Line(7, 7, 11, 8, st[0]); img.Line(11, 8, 14, 10, st[0]); img.Line(11, 9, 14, 11, st[5]);
        img.Line(1, 2, 7, 6, st[6]);
        // Loose chips and grit.
        foreach (var (x, y) in new[] { (10, 12), (4, 9), (12, 4) }) { img[x, y] = st[5]; img[x + 1, y + 1] = st[1]; }
        return img;
    }

    private static PixelImage StatueOverlay()
    {
        var img = new PixelImage(S, S);
        ArtKit.CastShadow(img, 10f, 14f, 7f, 2.6f, 0.6f);
        var s = Pal.Flagstone;
        string H(Color c) => ArtKit.Hex(c);
        var fig = ArtKit.Map($"o={H(Pal.Ink)} a={H(s[2])} b={H(s[3])} c={H(s[4])} d={H(s[5])} e={H(s[6])} f={H(s[7])} m={H(Pal.Lichen[2])} M={H(Pal.Lichen[1])} r=#ff8a40",
            "..oo........oo..",
            ".oeco......ocdo.",
            ".oedco....occbo.",
            "..odccooooccbo..",
            "...oeddddccbo...",
            "..ofeeddccccbo..",
            "..oedrcccrcbbo..",
            "..oedccccccbbo..",
            "...oedcmMcbbo...",
            "..oeddccccbbbo..",
            ".oeddcccccmbbbo.",
            ".oedcccmMcccbbo.",
            ".ofeeeddddccbao.",
            ".obbbbbbbbbbbao.",
            ".oaaaaaaaaaaaao.",
            "..oooooooooooo..");
        ArtKit.ColorOutline(fig, Pal.Ink);
        ArtKit.Over(img, fig);
        return img;
    }

    private static PixelImage BrazierOverlay()
    {
        var img = new PixelImage(S, S);
        ArtKit.CastShadow(img, 9.5f, 14.5f, 6f, 2f, 0.6f);
        string H(Color c) => ArtKit.Hex(c);
        var b = ArtKit.Map($"o={H(Pal.Ink)} a={H(Pal.Iron[0])} b={H(Pal.Iron[1])} c={H(Pal.Iron[2])} d={H(Pal.Iron[3])} e={H(Pal.Ember[0])} f={H(Pal.Ember[1])} g={H(Pal.Ember[2])}",
            "................",
            "................",
            "................",
            "................",
            "................",
            ".oooooooooooooo.",
            ".odddcccccccbbo.",
            ".obgfegfefgfeao.",
            "..odcccccccbao..",
            "...oacbbbbbao...",
            ".....odcbbo.....",
            "......odbo......",
            "......odco......",
            ".....odcbbo.....",
            "....odccbbbo....",
            "....oooooooo....");
        ArtKit.ColorOutline(b, Pal.Ink);
        ArtKit.Over(img, b);
        return img;
    }

    private static PixelImage PlateOverlay(bool pressed)
    {
        var img = new PixelImage(S, S);
        var st = Pal.Flagstone;
        img.FillRect(1, 1, 14, 14, st[0]);
        if (pressed)
        {
            img.FillRect(2, 2, 12, 12, st[2]);
            img.FillRect(2, 2, 12, 1, st[0]);
            img.FillRect(5, 5, 6, 6, Pal.Spirit[1]);
            img.FillRect(6, 6, 4, 4, Pal.Spirit[2]);
        }
        else
        {
            img.FillRect(2, 1, 12, 12, st[5]);
            img.FillRect(2, 1, 12, 1, st[7]);
            img.FillRect(2, 1, 1, 12, st[6]);
            img.FillRect(13, 1, 1, 12, st[4]);
            img.FillRect(2, 13, 12, 2, st[3]);
            img.FillRect(5, 4, 6, 6, st[4]);
            img.DrawRect(5, 4, 6, 6, st[3]);
            img.FillRect(6, 5, 4, 1, st[6]);
        }
        return img;
    }

    /// <summary>Wall pieces and the cast-shadow overlays the room renderer lays on the floor beside them.</summary>
    private static IEnumerable<(string, PixelImage)> Walls()
    {
        var st = Pal.Flagstone;
        // North wall: a lit cap with a bright lip, then a face of big ashlar blocks fading into shadow.
        var n = new PixelImage(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color c;
                if (y < 4) c = Art.Ramp([st[2], st[3], st[3], st[4]], ArtKit.PeriodicNoise(x, y, 4, 16, 701), x, y);
                else if (y == 4) c = st[6];
                else
                {
                    int fy = y - 5;
                    int row = fy / 5, ly = fy % 5;
                    int bx = (x + (row % 2) * 6) % 12;
                    bool joint = ly == 4 || bx == 11;
                    float depth = fy / 11f;
                    if (joint) c = st[0];
                    else if (ly == 0 || bx == 0) c = st[Math.Max(2, 6 - (int)(depth * 3))];
                    else if (bx == 10 || ly == 3) c = st[Math.Max(1, 3 - (int)(depth * 2))];
                    else c = Art.Ramp([st[2], st[3], st[4]], Math.Clamp(0.75f - depth * 0.6f + (Art.Hash(x, y, 702) - 0.5f) * 0.25f, 0, 1), x, y);
                }
                n[x, y] = c;
            }
        n.FillRect(0, 15, S, 1, Pal.DeepShadow);
        yield return ("wall_n", n);
        var torch = n.Clone();
        torch.FillRect(6, 8, 4, 6, Pal.Iron[1]);
        torch.FillRect(6, 8, 4, 1, Pal.Iron[3]);
        torch.FillRect(6, 8, 1, 6, Pal.Iron[2]);
        torch.FillRect(7, 14, 2, 1, Pal.Iron[0]);
        torch.FillRect(5, 7, 6, 1, Pal.Iron[2]);
        yield return ("wall_torch", torch);
        // Caps seen from above: big slabs, with a bevelled lip toward the room.
        PixelImage Cap(int lipSide)
        {
            var c = new PixelImage(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int sx = (x + (y / 8) * 5) % 10;
                    bool joint = sx == 9 || y % 8 == 7;
                    c[x, y] = joint ? st[1] : Art.Ramp([st[2], st[3], st[3], st[4]], ArtKit.PeriodicNoise(x, y, 4, 16, 711) * 0.8f + 0.1f, x, y);
                    if (!joint && (sx == 0 || y % 8 == 0)) c[x, y] = st[4];
                }
            for (int i = 0; i < S; i++)
                switch (lipSide)
                {
                    case 1: c[S - 2, i] = st[2]; c[S - 1, i] = Pal.DeepShadow; break; // west wall: the inner edge faces away from the light
                    case 3: c[1, i] = st[6]; c[0, i] = st[5]; break; // east wall: the inner edge catches the light
                    case 2: c[i, 0] = st[6]; c[i, 1] = st[5]; break; // south wall: lip toward the room
                }
            return c;
        }
        yield return ("wall_w", Cap(1));
        yield return ("wall_e", Cap(3));
        yield return ("wall_s", Cap(2));
        yield return ("wall_corner", Cap(-1));
        var shadeN = new PixelImage(S, S);
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < S; x++)
                if (y < 3 || Art.Dither(x, y, 1 - (y - 2) / 5f)) shadeN[x, y] = Pal.DeepShadow.WithAlpha(0.62f - y * 0.06f);
        yield return ("shade_n", shadeN);
        var shadeW = new PixelImage(S, S);
        for (int x = 0; x < 5; x++)
            for (int y = 0; y < S; y++)
                if (x < 2 || Art.Dither(x, y, 1 - (x - 1) / 4f)) shadeW[x, y] = Pal.DeepShadow.WithAlpha(0.5f - x * 0.07f);
        yield return ("shade_w", shadeW);
    }

    /// <summary>
    /// Door frames: a stone jamb beside an opening that darkens toward the outside of the room; the door panel itself is
    /// drawn by the door entity. "a" is the first cell of the two-cell opening in reading order (left or top).
    /// </summary>
    private static IEnumerable<(string, PixelImage)> DoorFrames(PixelImage floor)
    {
        PixelImage Frame(bool pillarLeft)
        {
            var st = Pal.Flagstone;
            var img = floor.Clone();
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < S; x++) img[x, y] = Color.Lerp(img[x, y], Pal.DeepShadow, 1 - y / 13f);
            int px = pillarLeft ? 0 : S - 4;
            img.FillRect(px, 0, 4, S, st[4]);
            img.FillRect(px + (pillarLeft ? 0 : 3), 0, 1, S, pillarLeft ? st[6] : st[2]);
            img.FillRect(px + (pillarLeft ? 3 : 0), 0, 1, S, st[0]);
            for (int y = 3; y < S; y += 4) img.FillRect(px, y, 4, 1, st[2]);
            return img;
        }
        PixelImage a = Frame(pillarLeft: true), b = Frame(pillarLeft: false);
        yield return ("frame_n_a", a);
        yield return ("frame_n_b", b);
        yield return ("frame_e_a", a.Rotate90());
        yield return ("frame_e_b", b.Rotate90());
        yield return ("frame_s_a", b.Rotate90().Rotate90());
        yield return ("frame_s_b", a.Rotate90().Rotate90());
        yield return ("frame_w_a", b.Rotate90().Rotate90().Rotate90());
        yield return ("frame_w_b", a.Rotate90().Rotate90().Rotate90());
    }
}
