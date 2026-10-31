using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.World;

namespace TrailDuel.Visuals;

/// <summary>
/// Painted battle stages: a 320x128 backdrop per area and the two platforms the creatures stand on.
/// Every stage is built in depth layers (sky, far range, mid ground, near ground) with light coming
/// from the upper left, the same direction as the overworld shadows, so battles read as a closer
/// look at the place the encounter started.
/// </summary>
internal static class BattleStageArt
{
    public const int Width = 320, Height = 128;

    /// <summary>Where the far ground meets the sky or wall; the enemy platform sits just below it.</summary>
    private const int Horizon = 74;

    public static void AddAll(SpriteAtlasBuilder b)
    {
        foreach (var bd in Enum.GetValues<Backdrop>())
            b.Add("backdrop:" + bd, Build(bd));
        foreach (var (suffix, kind) in new[] { ("", PlatformKind.Grass), ("forest", PlatformKind.Moss), ("hall", PlatformKind.Marble), ("indoor", PlatformKind.Rug) })
        {
            b.Add("platform:enemy" + suffix, Platform(104, 22, kind, false), new System.Numerics.Vector2(52, 11));
            b.Add("platform:player" + suffix, Platform(128, 26, kind, true), new System.Numerics.Vector2(64, 13));
        }
        b.Add("backdrop:title", Panorama(180, 128, DaySky, battle: false));
        b.Add("backdrop:sunset", Panorama(180, 126, SunsetSky, battle: false));
        b.Add("cloud", Cloud(44, 18, 3));
        b.Add("cloud2", Cloud(30, 13, 8));
        b.Add("mote", Mote(), new System.Numerics.Vector2(1, 1));
    }

    public static PixelImage Build(Backdrop kind) => kind switch
    {
        World.Backdrop.Forest => Forest(),
        World.Backdrop.Hall => Hall(),
        World.Backdrop.Indoor => Lab(),
        _ => Meadow(),
    };

    // ------------------------------------------------------------------ meadow

    private static readonly Color[] DaySky = [C("6fb4e6"), C("8cc8ee"), C("acdaf2"), C("cbe9f4"), C("e6f4f0")];
    private static readonly Color[] SunsetSky = [C("e8845a"), C("f0a060"), C("f6bc76"), C("fad496"), C("fce6b8")];

    private static PixelImage Meadow() => Panorama(Height, Horizon, DaySky, battle: true);

    /// <summary>
    /// The open-country scene shared by meadow battles, the title screen and the ending: a cloud bank on
    /// two mountain ranges, a treeline and a mown field. <paramref name="horizon"/> is where the hills
    /// meet the field; everything is laid out relative to it.
    /// </summary>
    public static PixelImage Panorama(int height, int horizon, Color[] sky, bool battle)
    {
        var img = new PixelImage(Width, height);
        Gradient(img, 0, horizon, sky);
        bool sunset = sky == SunsetSky;
        // A low bank of cumulus resting on the far range; drifting clouds are drawn on top at runtime.
        for (int i = 0; i < 7; i++)
        {
            var cloud = Cloud(40 + (int)(Art.Hash(i, 3) * 30), 14 + (int)(Art.Hash(i, 4) * 6), 20 + i);
            if (sunset) cloud.Map((_, _, c) => c.A == 0 ? c : Color.Lerp(c, C("f8b888"), 0.45f));
            img.Blit(cloud, -10 + i * 50 + (int)(Art.Hash(i, 5) * 16), horizon - 40 + (int)(Art.Hash(i, 6) * 8));
        }
        Color[] far = sunset ? [C("a07a9a"), C("b890a8"), C("d0a8b4")] : [C("8aa6cc"), C("a4bcdc"), C("c2d4e8")];
        Color[] near = sunset ? [C("8a8a8a"), C("a09a8e"), C("b8ac94")] : [C("7fa6a8"), C("93bab2"), C("a8ccbc")];
        Range(img, horizon - 14, 16, far, 11, snow: sunset ? C("fce0c8") : C("eef4fa"));
        Range(img, horizon - 6, 10, near, 4, snow: null);
        RollingHills(img, horizon, 6, [C("5f9a62"), C("74b06a"), C("8cc474"), C("a6d684")], 17);
        for (int x = -6; x < Width; x += 9 + (int)(Art.Hash(x, 7) * 9))
        {
            if (battle && x > 170 && x < 300 && Art.Hash(x, 8) < 0.55f) continue; // keep the enemy's backdrop calm
            float s = 0.55f + Art.Hash(x, 9) * 0.35f;
            if (Art.Hash(x, 10) < 0.3f) Pine(img, x, horizon + 3, s, Far);
            else RoundTree(img, x, horizon + 3, s, Far);
        }
        Field(img, horizon + 2, Pal.Grass, stripes: true, seed: 3);
        Flowers(img, horizon + 6, 70 * (height - horizon) / 54, 5);
        Tufts(img, horizon + 4, 110 * (height - horizon) / 54, Pal.Grass, 8);
        if (battle)
        {
            // Two big trees framing the left edge, just behind the player's platform.
            RoundTree(img, -14, 112, 1.5f, Near);
            RoundTree(img, 18, 104, 1.05f, Near);
            Bush(img, 300, 98, 1.2f);
            Bush(img, 42, 120, 1.0f);
        }
        else
        {
            RoundTree(img, -16, height - 6, 1.6f, Near);
            RoundTree(img, 284, height - 2, 1.45f, Near);
            Bush(img, 60, height - 8, 1.1f);
        }
        // Evening light warms everything below the sky.
        if (sunset) img.Map((_, y, c) => y < horizon - 24 || c.A == 0 ? c : Color.Lerp(c, C("f0a070"), 0.22f));
        return img;
    }

    // ------------------------------------------------------------------ forest

    private static PixelImage Forest()
    {
        var img = new PixelImage(Width, Height);
        Gradient(img, 0, Horizon + 8, [C("2d5a52"), C("3e7062"), C("52866e"), C("6c9c78"), C("8db486")]);
        // Three ranks of trunks fading into the green haze, each with its own canopy mass above.
        Trunks(img, seed: 1, count: 22, minW: 3, maxW: 5, baseY: Horizon + 4, color: [C("6a9a7e"), C("78a888"), C("86b490")]);
        Canopy(img, 0, 22, [C("4d806a"), C("5a8e72"), C("6a9c7a")], 5);
        Trunks(img, seed: 2, count: 12, minW: 6, maxW: 9, baseY: Horizon + 8, color: [C("3e5e50"), C("4e6e58"), C("5e7e60")]);
        Canopy(img, -4, 16, [C("2f5e4a"), C("3c6e52"), C("4a7e5a")], 6);
        LightShafts(img, C("fff4c0"), 0.28f);
        Field(img, Horizon + 6, [C("2f5a3a"), C("3f7044"), C("52864c"), C("6c9c56")], stripes: false, seed: 9);
        LeafLitter(img, Horizon + 8, 160);
        Tufts(img, Horizon + 6, 70, [C("2a4e34"), C("3a6a40"), C("4f8a4c"), C("74ac5c")], 21);
        // Near trunks frame the stage: one on each side, rooted below the enemy and the player.
        NearTrunk(img, 4, 18, Height + 4);
        NearTrunk(img, 296, 14, Horizon + 26);
        Fern(img, 30, 118, 1.2f);
        Fern(img, 286, 104, 1f);
        Mushrooms(img, 150, 96);
        Mushrooms(img, 58, 112);
        return img;
    }

    // ------------------------------------------------------------------ hall

    private static PixelImage Hall()
    {
        var img = new PixelImage(Width, Height);
        int floorTop = Horizon + 4;
        // Back wall: large pale ashlar blocks, softly lit from the windows, under a deep plum frieze.
        for (int y = 0; y < floorTop; y++)
        for (int x = 0; x < Width; x++)
        {
            int row = (y - 13) / 12, shift = (row & 1) * 18;
            bool joint = (y - 13) % 12 == 0 || (x + shift) % 36 == 0;
            float n = Art.Hash((x + shift) / 36, row, 4) * 0.18f + Art.ValueNoise(x, y, 24, 3) * 0.14f;
            var c = Art.Ramp([C("c8a8a0"), C("d8bcb0"), C("e6d0c2"), C("f2e2d2")], 0.4f + n + y / (float)floorTop * 0.15f, x, y);
            img[x, y] = joint ? C("b8988e") : c;
        }
        img.FillRect(0, 0, Width, 10, C("4a2a44"));
        img.FillRect(0, 10, Width, 2, Pal.GoldDark);
        img.FillRect(0, 12, Width, 1, Pal.Gold);
        for (int x = 4; x < Width; x += 10) img.FillRect(x, 3, 4, 4, C("6a3a5c"));
        // Tall arched windows pour light onto the floor; columns stand between them.
        int[] windows = [52, 160, 268];
        foreach (int wx in windows) ArchWindow(img, wx, 18, 26, floorTop - 26);
        foreach (int cx in new[] { 0, 106, 214, 320 }) Column(img, cx, 14, floorTop);
        foreach (int bx in new[] { 84, 236 }) Banner(img, bx, 18);
        // Polished floor in perspective: tiles converge on the centre of the wall.
        MarbleFloor(img, floorTop);
        foreach (int wx in windows) FloorLight(img, wx, floorTop);
        // Reflections of the column bases on the polish.
        foreach (int cx in new[] { 106, 214 })
            for (int y = floorTop; y < floorTop + 14; y++)
            for (int x = cx - 8; x < cx + 8; x++)
                if (img.InBounds(x, y) && Art.Dither(x, y, 0.5f - (y - floorTop) / 28f)) img.Blend(x, y, C("fff0d8").WithAlpha(0.2f));
        // A strip of red carpet runs from the player toward the master.
        Carpet(img, floorTop);
        return img;
    }

    // ------------------------------------------------------------------ lab (indoor battles)

    private static PixelImage Lab()
    {
        var img = new PixelImage(Width, Height);
        int floorTop = Horizon + 2;
        for (int y = 0; y < floorTop; y++)
        for (int x = 0; x < Width; x++)
            img[x, y] = Art.Ramp(Pal.Wall, 0.85f - y / (float)floorTop * 0.45f + Art.ValueNoise(x, y, 14, 2) * 0.1f, x, y);
        img.FillRect(0, floorTop - 10, Width, 10, Pal.Wood[1]);
        img.FillRect(0, floorTop - 10, Width, 1, Pal.Wood[3]);
        img.FillRect(0, floorTop - 1, Width, 1, Pal.Wood[0]);
        // Bookshelves, a specimen machine with a glowing screen and two sunny windows.
        Shelf(img, 8, 14);
        Shelf(img, 262, 14);
        Machine(img, 142, 12);
        LabWindow(img, 70, 14);
        LabWindow(img, 206, 14);
        // Wooden boards in perspective.
        for (int y = floorTop; y < Height; y++)
        {
            float depth = (y - floorTop) / (float)(Height - floorTop);
            for (int x = 0; x < Width; x++)
            {
                float u = (x - 160) / (0.55f + depth * 0.9f) + 160;
                int board = (int)MathF.Floor(u / 22f);
                bool seam = ((int)u % 22 + 22) % 22 == 0;
                float n = Art.Hash(board, 3, 7) * 0.3f + Art.ValueNoise(u, y, 9, board) * 0.15f;
                img[x, y] = seam ? Pal.Wood[0] : Art.Ramp(Pal.Floor, 0.3f + n + depth * 0.25f, x, y);
            }
        }
        for (int x = 0; x < Width; x++) img[x, floorTop] = Pal.Wood[0];
        return img;
    }

    // ------------------------------------------------------------------ platforms

    private enum PlatformKind { Grass, Moss, Marble, Rug }

    /// <summary>
    /// An oval mound seen from slightly above: a lit top surface, a darker rim, and a visible front
    /// face (soil, stone or fabric) so it reads as solid ground rather than a flat disc.
    /// </summary>
    private static PixelImage Platform(int w, int h, PlatformKind kind, bool player)
    {
        int face = kind == PlatformKind.Rug ? 2 : 4;
        var img = new PixelImage(w, h + face);
        float cx = w / 2f, cy = h / 2f, rx = w / 2f - 0.5f, ry = h / 2f - 0.5f;
        Color[] top = kind switch
        {
            PlatformKind.Moss => [C("2f5a3a"), C("3f7044"), C("52864c"), C("6c9c56"), C("8cb466")],
            PlatformKind.Marble => [C("8c8496"), C("aaa2b2"), C("c8c0cc"), C("e2dce4"), C("f6f2f4")],
            PlatformKind.Rug => [C("6a3048"), C("8a4058"), C("a8546a"), C("c46e7e"), C("dc8e94")],
            _ => [Pal.Grass[0], Pal.Grass[1], Pal.Grass[2], Pal.Grass[3], C("c4ea92")],
        };
        Color[] side = kind switch
        {
            PlatformKind.Marble => [Pal.GoldDark.Shade(0.7f), Pal.GoldDark, Pal.Gold],
            PlatformKind.Rug => [C("4a2034"), C("5a2a40"), C("6a3048")],
            PlatformKind.Moss => [C("3a2a24"), C("4e3a2e"), C("644a36")],
            _ => [C("5e3e2c"), C("7a5238"), C("966846")],
        };
        // Front face first, as an extruded copy of the lower half of the oval.
        for (int y = 0; y < h + face; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x + 0.5f - cx) / rx;
            if (dx * dx > 1) continue;
            float lower = cy + MathF.Sqrt(1 - dx * dx) * ry;
            if (y < cy || y > lower + face) continue;
            float t = (y - lower) / face;
            if (y < lower) continue;
            img[x, y] = Art.Ramp(side, 0.85f - t * 0.6f - MathF.Abs(dx) * 0.25f, x, y);
        }
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
            float d = dx * dx + dy * dy;
            if (d > 1) continue;
            // Light from the upper left; the rim darkens like a gentle dome.
            float light = 0.72f - dx * 0.18f - dy * 0.22f - d * 0.32f;
            light += kind switch
            {
                PlatformKind.Marble => (Art.ValueNoise(x * 0.6f, y * 2, 6, 4) - 0.5f) * 0.18f,
                PlatformKind.Rug => 0,
                _ => (Art.ValueNoise(x, y * 2, 5, 3) - 0.5f) * 0.3f,
            };
            var c = Art.Ramp(top, light, x, y);
            // A crisp lit lip along the back edge and a dark rim at the front separate the mound from the field.
            if (d > 0.9f) c = dy < 0 ? top[4] : top[0];
            else if (d > 0.82f && dy > 0) c = top[0];
            if (kind == PlatformKind.Marble && d > 0.62f && d < 0.74f) c = dy < 0 ? Pal.Gold : Pal.GoldDark;
            if (kind == PlatformKind.Rug && d > 0.7f && d < 0.78f) c = Pal.Gold;
            img[x, y] = c;
        }
        if (kind is PlatformKind.Grass or PlatformKind.Moss)
        {
            // Blades and clover tufts spill over the back and side rims; pebbles sit on the front face.
            var blade = kind == PlatformKind.Moss ? C("8cb466") : C("c4ea92");
            for (int i = 0; i < w / 3; i++)
            {
                float a = MathF.PI + Art.Hash(i, w, 2) * MathF.PI;
                int bx = (int)(cx + MathF.Cos(a) * rx * 0.98f), by = (int)(cy + MathF.Sin(a) * ry * 0.98f);
                int len = 1 + (int)(Art.Hash(i, 5, w) * 3);
                for (int k = 0; k < len; k++)
                    if (img.InBounds(bx, by - k)) img[bx, by - k] = k == len - 1 ? blade : top[2];
            }
            for (int i = 0; i < w / 10; i++)
            {
                int px = 6 + (int)(Art.Hash(i, 8, w) * (w - 12));
                float dx = (px + 0.5f - cx) / rx;
                int py = (int)(cy + MathF.Sqrt(MathF.Max(0, 1 - dx * dx)) * ry) + 2;
                if (img.InBounds(px, py) && img[px, py].A > 0) { img[px, py] = C("b0a090"); if (img.InBounds(px + 1, py)) img[px + 1, py] = C("8a7a6e"); }
            }
            if (kind == PlatformKind.Grass)
                for (int i = 0; i < 6; i++)
                {
                    int fx = (int)(cx + (Art.Hash(i, 11, w) - 0.5f) * rx * 1.4f), fy = (int)(cy + (Art.Hash(11, i, w) - 0.6f) * ry * 1.1f);
                    if (img.InBounds(fx, fy)) img[fx, fy] = Pal.FlowerColors[i % 4];
                }
        }
        if (kind == PlatformKind.Marble)
        {
            // An inlaid orb emblem in the centre of each dais.
            img.FillEllipse(cx, cy, rx * 0.18f, ry * 0.2f, Pal.GoldDark);
            img.FillEllipse(cx, cy - 0.5f, rx * 0.12f, ry * 0.12f, Pal.Gold);
        }
        _ = player;
        return img;
    }

    // ------------------------------------------------------------------ painting helpers

    private static Color C(string hex) => Color.Hex(hex);

    private static readonly Color[] Far = [C("4f8a64"), C("5e9a6a"), C("6faa72"), C("84bc7c")];
    private static readonly Color[] Near = [C("1f4a3c"), C("2e6b4a"), C("3f8a52"), C("5cad5c"), C("8fd06a")];

    /// <summary>Vertical gradient in flat bands joined by ordered dithering.</summary>
    private static void Gradient(PixelImage img, int top, int bottom, Color[] bands)
    {
        for (int y = top; y < bottom; y++)
        {
            float t = (y - top) / (float)(bottom - top) * (bands.Length - 1);
            int i = Math.Min((int)t, bands.Length - 2);
            for (int x = 0; x < img.Width; x++)
                img[x, y] = Art.Dither(x, y, t - i) ? bands[i + 1] : bands[i];
        }
    }

    /// <summary>A soft cumulus: overlapping puffs lit from above, flat-bottomed and shaded underneath.</summary>
    private static PixelImage Cloud(int w, int h, int seed)
    {
        var img = new PixelImage(w, h);
        var mask = new float[w * h];
        int puffs = 3 + w / 14;
        for (int p = 0; p < puffs; p++)
        {
            float px = 6 + (w - 12) * (p + 0.5f) / puffs + (Art.Hash(p, seed) - 0.5f) * 4;
            float r = h * (0.3f + Art.Hash(seed, p) * 0.25f) * (1 - MathF.Abs(p + 0.5f - puffs / 2f) / puffs * 0.8f);
            float py = h - 3 - r * 0.8f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - px, dy = y + 0.5f - py;
                float v = 1 - (dx * dx + dy * dy) / (r * r);
                if (v > mask[y * w + x]) mask[y * w + x] = v;
            }
        }
        Color[] ramp = [C("c4d6ea"), C("dce8f4"), C("f4f8fc"), C("fffffa")];
        for (int y = 0; y < h - 1; y++)
        for (int x = 0; x < w; x++)
        {
            float v = mask[y * w + x];
            if (v <= 0) continue;
            float light = 0.25f + v * 0.5f - (y / (float)h) * 0.45f + 0.35f;
            img[x, y] = Art.Ramp(ramp, light, x, y);
        }
        return img;
    }

    /// <summary>A distant mountain range with lit left-facing slopes and optional snow caps.</summary>
    private static void Range(PixelImage img, int baseY, int amplitude, Color[] ramp, int seed, Color? snow)
    {
        var heights = new float[img.Width];
        for (int x = 0; x < img.Width; x++)
        {
            float n = Art.ValueNoise(x, 0, 46, seed) * 0.65f + Art.ValueNoise(x, 0, 17, seed + 1) * 0.35f;
            heights[x] = baseY - n * amplitude * 1.6f + amplitude * 0.3f;
        }
        for (int x = 0; x < img.Width; x++)
        {
            int top = (int)heights[x];
            float slope = heights[Math.Min(img.Width - 1, x + 1)] - heights[Math.Max(0, x - 1)];
            // Slopes rising to the right face the light (it comes from the upper left).
            for (int y = Math.Max(0, top); y < img.Height; y++)
            {
                float lit = slope > 0.15f ? 0.85f : slope < -0.15f ? 0.2f : 0.5f;
                float fade = (y - top) / 12f;
                var c = Art.Ramp(ramp, lit - fade * 0.3f, x, y);
                if (snow is { } s && y - top < 2 && top < baseY - amplitude * 0.45f && slope >= -0.1f) c = s;
                img[x, y] = c;
            }
        }
    }

    /// <summary>Gentle hills with a sunlit crest and a darker body.</summary>
    private static void RollingHills(PixelImage img, int baseY, int amplitude, Color[] ramp, int seed)
    {
        for (int x = 0; x < img.Width; x++)
        {
            int top = baseY - (int)(Art.ValueNoise(x, 0, 34, seed) * amplitude * 2);
            for (int y = Math.Max(0, top); y < img.Height; y++)
            {
                float t = (y - top) / 6f;
                img[x, y] = y - top < 1 ? ramp[3] : Art.Ramp(ramp, 0.75f - t * 0.25f, x, y);
            }
        }
    }

    /// <summary>A round deciduous tree: clustered leaf masses shaded toward the lower right, outlined and on a short trunk.</summary>
    private static void RoundTree(PixelImage img, int x, int baseY, float scale, Color[] ramp)
    {
        float r = 7 * scale;
        int trunkH = (int)(5 * scale);
        int tw = Math.Max(2, (int)(3 * scale));
        img.FillRect(x + (int)r - tw / 2, baseY - trunkH, tw, trunkH, Pal.Bark[1]);
        img.FillRect(x + (int)r - tw / 2, baseY - trunkH, 1, trunkH, Pal.Bark[2]);
        var blobs = new (float X, float Y, float R)[]
        {
            (x + r, baseY - trunkH - r * 0.9f, r),
            (x + r * 0.45f, baseY - trunkH - r * 0.45f, r * 0.7f),
            (x + r * 1.55f, baseY - trunkH - r * 0.5f, r * 0.72f),
            (x + r * 0.9f, baseY - trunkH - r * 1.55f, r * 0.62f),
        };
        int minX = (int)(x - r), maxX = (int)(x + r * 3), minY = (int)(baseY - trunkH - r * 2.4f), maxY = baseY;
        var inside = new Dictionary<(int, int), float>();
        for (int py = minY; py <= maxY; py++)
        for (int px = minX; px <= maxX; px++)
        {
            float best = -1;
            foreach (var (bx, by, br) in blobs)
            {
                float dx = (px + 0.5f - bx) / br, dy = (py + 0.5f - by) / br;
                float d = dx * dx + dy * dy;
                if (d <= 1)
                {
                    // Per-blob normal: lit on its upper-left shoulder.
                    float shade = 0.75f - dx * 0.3f - dy * 0.35f - d * 0.25f;
                    best = MathF.Max(best, shade);
                }
            }
            if (best >= 0) inside[(px, py)] = best;
        }
        foreach (var ((px, py), shade) in inside)
        {
            if (!img.InBounds(px, py)) continue;
            bool edge = !inside.ContainsKey((px - 1, py)) || !inside.ContainsKey((px + 1, py)) || !inside.ContainsKey((px, py - 1)) || !inside.ContainsKey((px, py + 1));
            float n = (Art.Hash(px, py, 13) - 0.5f) * 0.18f;
            img[px, py] = edge ? ramp[0] : Art.Ramp(ramp, shade + n, px, py);
        }
    }

    private static void Pine(PixelImage img, int x, int baseY, float scale, Color[] ramp)
    {
        int h = (int)(22 * scale), w = (int)(8 * scale);
        img.FillRect(x + w - 1, baseY - 3, 2, 3, Pal.Bark[0]);
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)h;
            // Three stacked tiers that widen toward the bottom.
            float tier = (t * 3) % 1;
            int half = (int)((0.25f + t * 0.75f) * w * (0.6f + tier * 0.4f));
            for (int dx = -half; dx <= half; dx++)
            {
                int px = x + w + dx, py = baseY - 3 - h + y;
                if (!img.InBounds(px, py)) continue;
                bool edge = dx == -half || dx == half;
                img[px, py] = edge ? ramp[0] : Art.Ramp(ramp, 0.65f - dx / (float)Math.Max(1, half) * 0.3f - tier * 0.2f, px, py);
            }
        }
    }

    private static void Bush(PixelImage img, int x, int baseY, float scale)
    {
        float r = 6 * scale;
        for (int py = (int)(baseY - r * 1.6f); py <= baseY; py++)
        for (int px = (int)(x - r * 1.6f); px <= x + r * 1.6f; px++)
        {
            float best = -1;
            foreach (var (bx, by, br) in new[] { (x - r * 0.6f, baseY - r * 0.6f, r * 0.8f), (x + r * 0.6f, baseY - r * 0.6f, r * 0.8f), (x, baseY - r, r) })
            {
                float dx = (px + 0.5f - bx) / br, dy = (py + 0.5f - by) / br, d = dx * dx + dy * dy;
                if (d <= 1) best = MathF.Max(best, 0.8f - dx * 0.3f - dy * 0.35f - d * 0.3f);
            }
            if (best >= 0 && img.InBounds(px, py) && py < baseY) img[px, py] = Art.Ramp(Near, best, px, py);
        }
    }

    /// <summary>The field the platforms rest on: lighter and finer far away, with optional mown stripes.</summary>
    private static void Field(PixelImage img, int top, Color[] ramp, bool stripes, int seed)
    {
        for (int y = top; y < img.Height; y++)
        {
            float depth = (y - top) / (float)(img.Height - top);
            for (int x = 0; x < img.Width; x++)
            {
                if (img[x, y].A > 0 && y < top + 2) continue;
                float light = 0.78f - depth * 0.42f + (Art.ValueNoise(x, y * 3, 18, seed) - 0.5f) * 0.25f;
                if (stripes)
                {
                    // Mown bands that widen with depth, slanted toward the vanishing point.
                    float u = (x - 160) / (0.4f + depth * 1.6f);
                    if (((int)MathF.Floor(u / 24f) & 1) == 0) light += 0.08f;
                }
                img[x, y] = Art.Ramp(ramp, light, x, y);
            }
        }
    }

    private static void Flowers(PixelImage img, int top, int count, int seed)
    {
        for (int i = 0; i < count; i++)
        {
            int x = (int)(Art.Hash(i, seed) * img.Width);
            int y = top + (int)(MathF.Pow(Art.Hash(seed, i), 1.4f) * (img.Height - top - 2));
            var c = Pal.FlowerColors[(int)(Art.Hash(i, i, seed) * 4)];
            if (!img.InBounds(x, y)) continue;
            img[x, y] = c;
            if ((y - top) > 24 && img.InBounds(x + 1, y)) { img[x + 1, y] = c; img[x, y + 1] = Pal.Grass[0]; }
        }
    }

    /// <summary>Grass tufts that grow taller toward the viewer.</summary>
    private static void Tufts(PixelImage img, int top, int count, Color[] ramp, int seed)
    {
        for (int i = 0; i < count; i++)
        {
            int x = (int)(Art.Hash(i, seed, 1) * img.Width);
            float depth = Art.Hash(seed, i, 2);
            int y = top + (int)(depth * (img.Height - top));
            int len = 1 + (int)(depth * 4);
            for (int k = 0; k < len; k++)
            {
                var c = k == len - 1 ? ramp[3] : ramp[2];
                if (img.InBounds(x, y - k)) img[x, y - k] = c;
                if (len > 2 && img.InBounds(x - 2, y - k + 1) && k < len - 1) img[x - 2, y - k + 1] = ramp[2];
                if (len > 2 && img.InBounds(x + 2, y - k + 1) && k < len - 1) img[x + 2, y - k + 1] = ramp[1];
            }
            if (img.InBounds(x, y + 1)) img[x, y + 1] = ramp[0];
        }
    }

    private static void Trunks(PixelImage img, int seed, int count, int minW, int maxW, int baseY, Color[] color)
    {
        for (int i = 0; i < count; i++)
        {
            int x = (int)(Art.Hash(i, seed, 3) * (img.Width + 20)) - 10;
            int w = minW + (int)(Art.Hash(seed, i, 4) * (maxW - minW + 1));
            for (int y = 0; y < baseY; y++)
            for (int dx = 0; dx < w; dx++)
            {
                if (!img.InBounds(x + dx, y)) continue;
                float lit = dx < w / 3f ? 0.9f : dx > w * 0.7f ? 0.1f : 0.5f;
                img[x + dx, y] = Art.Ramp(color, lit + (Art.Hash(dx, y / 3, i) - 0.5f) * 0.2f, x + dx, y);
            }
            // Root flare at the base.
            img.FillRect(x - 1, baseY - 2, w + 2, 2, color[0]);
        }
    }

    private static void Canopy(PixelImage img, int top, int depth, Color[] ramp, int seed)
    {
        for (int x = 0; x < img.Width; x++)
        {
            int bottom = top + depth - (int)(Art.ValueNoise(x, 0, 12, seed) * depth * 0.7f);
            for (int y = 0; y < bottom; y++)
            {
                float edge = (bottom - y) / (float)depth;
                img[x, y] = y == bottom - 1 ? ramp[0] : Art.Ramp(ramp, 0.3f + Art.ValueNoise(x, y, 5, seed + 3) * 0.6f + edge * 0.1f, x, y);
            }
        }
    }

    private static void LightShafts(PixelImage img, Color light, float alpha)
    {
        foreach (var (sx, width) in new[] { (60, 14), (150, 9), (232, 16) })
            for (int y = 10; y < Horizon + 30; y++)
            for (int x = 0; x < width; x++)
            {
                int px = sx + x + (y - 10) / 2;
                float fade = 1 - MathF.Abs(x - width / 2f) / (width / 2f);
                if (img.InBounds(px, y) && Art.Dither(px, y, fade * 0.8f)) img.Blend(px, y, light.WithAlpha(alpha));
            }
    }

    private static void LeafLitter(PixelImage img, int top, int count)
    {
        Color[] leaves = [C("a8662c"), C("c88a3c"), C("8a7a3a"), C("6a8a3c")];
        for (int i = 0; i < count; i++)
        {
            int x = (int)(Art.Hash(i, 31) * img.Width), y = top + (int)(Art.Hash(31, i) * (img.Height - top));
            if (!img.InBounds(x, y)) continue;
            img[x, y] = leaves[i % 4];
            if (Art.Hash(i, 32) < 0.5f && img.InBounds(x + 1, y)) img[x + 1, y] = leaves[(i + 1) % 4].Shade(0.85f);
        }
    }

    private static void NearTrunk(PixelImage img, int x, int w, int baseY)
    {
        Color[] bark = [C("2a2024"), C("3e2e2c"), Pal.Bark[1], Pal.Bark[2]];
        for (int y = 0; y < Math.Min(img.Height, baseY); y++)
        for (int dx = 0; dx < w; dx++)
        {
            int px = x + dx;
            if (!img.InBounds(px, y)) continue;
            float u = dx / (float)w;
            float lit = 0.95f - u * 0.8f;
            // Vertical bark furrows.
            if (Art.ValueNoise(dx * 3, y * 0.3f, 3, x) > 0.68f) lit -= 0.35f;
            img[px, y] = dx == 0 || dx == w - 1 ? bark[0] : Art.Ramp(bark, lit, px, y);
        }
        // Moss on the shaded side and a root spreading into the ground.
        for (int y = 0; y < Math.Min(img.Height, baseY); y++)
            if (Art.ValueNoise(0, y, 6, x + 2) > 0.55f && img.InBounds(x + w - 3, y)) { img[x + w - 3, y] = C("5a8a4a"); img[x + w - 2, y] = C("4a7a40"); }
        if (baseY < img.Height)
        {
            img.FillEllipse(x + w / 2f, baseY - 1, w * 0.85f, 2.5f, bark[1]);
            img.FillEllipse(x + w / 2f - 1, baseY - 2, w * 0.6f, 1.5f, bark[2]);
        }
    }

    private static void Fern(PixelImage img, int x, int baseY, float scale)
    {
        Color[] ramp = [C("23503a"), C("336a44"), C("4a8a4c"), C("6aa858")];
        for (int f = 0; f < 5; f++)
        {
            float angle = MathF.PI * (1.15f + f * 0.17f);
            float len = (10 + Art.Hash(f, x) * 5) * scale;
            for (float t = 0; t < 1; t += 0.04f)
            {
                float droop = t * t * 6 * scale;
                int px = (int)(x + MathF.Cos(angle) * len * t), py = (int)(baseY + MathF.Sin(angle) * len * t + droop);
                if (!img.InBounds(px, py)) continue;
                img[px, py] = ramp[1];
                // Leaflets alternate along the frond.
                int side = ((int)(t * 25) & 1) == 0 ? -1 : 1;
                int lx = px + side, ly = py - 1;
                if (img.InBounds(lx, ly) && t < 0.9f) img[lx, ly] = t < 0.5f ? ramp[3] : ramp[2];
            }
        }
    }

    private static void Mushrooms(PixelImage img, int x, int y)
    {
        foreach (var (dx, s) in new[] { (0, 1f), (5, 0.7f), (-4, 0.6f) })
        {
            int px = x + dx;
            int stem = (int)(3 * s) + 1;
            img.FillRect(px, y - stem, 1, stem, C("efe3c8"));
            float r = 2.6f * s + 0.8f;
            img.FillEllipse(px + 0.5f, y - stem, r, r * 0.65f, C("c8504a"));
            img[px - (int)(r * 0.4f), y - stem - 1] = C("fff0e0");
            img.FillRect(px - 1, y, 3, 1, C("2a3e2e"));
        }
    }

    private static void ArchWindow(PixelImage img, int cx, int top, int w, int h)
    {
        int left = cx - w / 2;
        Color[] glass = [C("8ec8ea"), C("b2dcf2"), C("dceff8"), C("fffbe8")];
        for (int y = top; y < top + h; y++)
        for (int x = left; x < left + w; x++)
        {
            float dx = (x + 0.5f - cx) / (w / 2f);
            float archY = top + w / 2f - MathF.Sqrt(MathF.Max(0, 1 - dx * dx)) * (w / 2f);
            if (y < archY) continue;
            bool frame = MathF.Abs(dx) > 0.86f || y > top + h - 3 || y < archY + 2;
            bool mullion = MathF.Abs(x - cx) < 1 || (y - top) % 14 == 0;
            float light = 0.4f + (y - top) / (float)h * 0.5f - dx * 0.15f;
            img[x, y] = frame ? C("5a3a40") : mullion ? C("6e4a4c") : Art.Ramp(glass, light, x, y);
        }
        // Stone sill.
        img.FillRect(left - 2, top + h - 1, w + 4, 2, C("d8c4b0"));
        img.FillRect(left - 2, top + h + 1, w + 4, 1, C("7a5a58"));
    }

    private static void Column(PixelImage img, int cx, int top, int bottom)
    {
        int w = 16, left = cx - w / 2;
        Color[] stone = [C("b8a8a0"), C("d4c6bc"), C("ece2d6"), C("fbf5e6")];
        for (int y = top + 6; y < bottom - 5; y++)
        for (int x = left; x < left + w; x++)
        {
            if (!img.InBounds(x, y)) continue;
            float u = (x - left + 0.5f) / w;
            float light = 1 - MathF.Abs(u - 0.32f) * 1.6f;
            bool flute = (x - left) % 4 == 3;
            img[x, y] = x == left || x == left + w - 1 ? C("7a6260") : Art.Ramp(stone, light - (flute ? 0.25f : 0), x, y);
        }
        // Capital and base mouldings in gold-trimmed stone.
        foreach (var (y, h, extra) in new[] { (top, 6, 3), (bottom - 5, 5, 3) })
        {
            for (int yy = y; yy < y + h; yy++)
            for (int x = left - extra; x < left + w + extra; x++)
            {
                if (!img.InBounds(x, yy)) continue;
                float u = (x - left + extra + 0.5f) / (w + extra * 2);
                img[x, yy] = yy == y || yy == y + h - 1 ? Pal.GoldDark : Art.Ramp(stone, 1 - MathF.Abs(u - 0.32f) * 1.4f, x, yy);
            }
            img.FillRect(left - extra, y + h / 2, w + extra * 2, 1, Pal.Gold);
        }
    }

    private static void Banner(PixelImage img, int cx, int top)
    {
        int w = 22, h = 40, left = cx - w / 2;
        img.FillRect(left - 3, top - 2, w + 6, 2, Pal.GoldDark);
        for (int y = top; y < top + h; y++)
        for (int x = left; x < left + w; x++)
        {
            // Swallow-tailed hem.
            int notch = Math.Abs(x - cx);
            if (y > top + h - 7 + notch / 2 && notch < 6) continue;
            float light = 0.75f - (x - left) / (float)w * 0.5f + MathF.Sin(x * 0.7f) * 0.08f;
            img[x, y] = x == left || x == left + w - 1 ? C("4a1e38") : Art.Ramp([C("5a2444"), C("7a3458"), C("9a4470"), C("b85a84")], light, x, y);
        }
        for (int y = top; y < top + h - 8; y++) { img[left + 2, y] = Pal.Gold; img[left + w - 3, y] = Pal.Gold; }
        // The Trail League emblem: a capped capture orb in gold, its core glowing.
        float ey = top + 17;
        img.FillCircle(cx - 0.5f, ey, 6, Pal.GoldDark);
        img.FillCircle(cx - 0.5f, ey, 5, Pal.Gold);
        img.FillCircle(cx - 0.5f, ey + 1, 2.2f, C("fbe8a8"));
        img.FillRect(cx - 3, (int)ey - 8, 6, 2, Pal.GoldDark);
        img.FillRect(cx - 1, (int)ey - 9, 2, 1, Pal.GoldDark);
        img[cx - 3, (int)ey - 3] = Pal.White;
    }

    private static void MarbleFloor(PixelImage img, int top)
    {
        Color[] marble = [C("b8aab4"), C("cec2c8"), C("e0d6d8"), C("f0eae6")];
        // Tile rows grow taller toward the viewer (inverse depth); seams are faint so the polish reads, not the grid.
        int RowAt(int y) => (int)MathF.Floor(70f / (y - top + 7));
        float DepthAt(int y) => (y - top + 7) / (float)(img.Height - top + 7);
        int ColAt(int x, int y) => (int)MathF.Floor((x - 160) / DepthAt(y) / 70f);
        for (int y = top; y < img.Height; y++)
        {
            float depth = DepthAt(y);
            int rowIndex = RowAt(y);
            bool rowLine = y > top && RowAt(y - 1) != rowIndex;
            for (int x = 0; x < img.Width; x++)
            {
                int col = ColAt(x, y);
                bool colLine = x > 0 && ColAt(x - 1, y) != col;
                bool dark = ((col + rowIndex) & 1) == 0;
                float vein = (Art.ValueNoise((x - 160) / depth * 0.4f, y * 1.5f, 11, col * 7 + rowIndex) - 0.5f) * 0.25f;
                var c = Art.Ramp(marble, (dark ? 0.42f : 0.66f) + vein - depth * 0.1f, x, y);
                img[x, y] = rowLine || colLine ? Color.Lerp(c, C("8a7c86"), 0.45f) : c;
            }
        }
        for (int x = 0; x < img.Width; x++) { img[x, top] = C("7a6268"); img[x, top + 1] = C("a8949c"); }
    }

    private static void FloorLight(PixelImage img, int wx, int top)
    {
        for (int y = top + 2; y < img.Height; y++)
        {
            float depth = (y - top) / (float)(img.Height - top);
            float cx = wx + (wx - 160) * depth * 0.6f + depth * 30;
            float half = 9 + depth * 16;
            for (int x = (int)(cx - half); x < cx + half; x++)
                if (img.InBounds(x, y) && Art.Dither(x, y, 0.55f - depth * 0.35f)) img.Blend(x, y, C("fff4d8").WithAlpha(0.35f));
        }
    }

    /// <summary>A red runner down the middle of the hall, narrowing toward the far wall.</summary>
    private static void Carpet(PixelImage img, int top)
    {
        for (int y = top + 2; y < img.Height; y++)
        {
            float depth = (y - top) / (float)(img.Height - top);
            float half = 7 + depth * 26;
            for (int x = (int)(160 - half); x <= 160 + half; x++)
            {
                if (!img.InBounds(x, y)) continue;
                float u = (x - (160 - half)) / (half * 2);
                img[x, y] = u < 0.07f || u > 0.93f ? (u < 0.5f ? Pal.Gold : Pal.GoldDark)
                    : Art.Ramp([C("8a2a3a"), C("a83a46"), C("c24e52")], 0.65f - depth * 0.3f - u * 0.15f, x, y);
            }
        }
    }

    private static void Shelf(PixelImage img, int x, int top)
    {
        int w = 50, h = 58;
        img.FillRect(x, top, w, h, Pal.Wood[0]);
        img.FillRect(x + 2, top + 2, w - 4, h - 4, Pal.Wood[1].Shade(0.7f));
        for (int s = 0; s < 3; s++)
        {
            int sy = top + 4 + s * 18;
            for (int bx = x + 3; bx < x + w - 4;)
            {
                int bw = 2 + (int)(Art.Hash(bx, sy) * 3), bh = 9 + (int)(Art.Hash(sy, bx) * 5);
                var c = new[] { Pal.RoofRed[1], Pal.RoofBlue[1], Pal.RoofTeal[2], Pal.Gold, Pal.RoofPink[1], Pal.Cream }[(int)(Art.Hash(bx, sy, 3) * 6)];
                img.FillRect(bx, sy + 14 - bh, bw, bh, c);
                img.FillRect(bx, sy + 14 - bh, 1, bh, c.Shade(1.2f));
                bx += bw + (Art.Hash(bx, 9) < 0.15f ? 3 : 0);
            }
            img.FillRect(x + 2, sy + 14, w - 4, 2, Pal.Wood[2]);
        }
    }

    private static void Machine(PixelImage img, int x, int top)
    {
        int w = 36, h = 60;
        img.FillRect(x, top, w, h, C("6a7a8a"));
        img.FillRect(x + 1, top + 1, w - 2, h - 2, C("8a9aa8"));
        img.FillRect(x + 1, top + 1, 2, h - 2, C("b4c2cc"));
        img.FillRect(x + 5, top + 6, w - 10, 18, C("2b2140"));
        for (int y = 0; y < 16; y++)
        for (int xx = 0; xx < w - 12; xx++)
            img[x + 6 + xx, top + 7 + y] = Art.Dither(xx, y, 0.3f + y / 40f) ? C("6ae0b0") : C("2e8a6a");
        for (int i = 0; i < 4; i++) img.FillCircle(x + 8 + i * 7, top + 32, 2, new[] { Pal.Coral, Pal.Gold, C("6ae0b0"), Pal.RoofBlue[2] }[i]);
        img.FillRect(x + 6, top + 40, w - 12, 12, C("5a6a7a"));
        img.FillRect(x + 8, top + 42, w - 16, 3, Pal.Gold);
    }

    private static void LabWindow(PixelImage img, int x, int top)
    {
        int w = 44, h = 30;
        img.FillRect(x - 1, top - 1, w + 2, h + 2, Pal.Wood[0]);
        for (int y = 0; y < h; y++)
        for (int xx = 0; xx < w; xx++)
            img[x + xx, top + y] = Art.Ramp([C("7ec4ec"), C("a8dcf2"), C("e8f8ff")], 0.3f + y / (float)h * 0.6f, x + xx, top + y);
        img.FillRect(x, top + h - 8, w, 8, Pal.Grass[2]);
        img.FillRect(x, top + h - 8, w, 1, Pal.Grass[3]);
        img.FillRect(x + w / 2, top, 1, h, Pal.Wood[1]);
        img.FillRect(x, top + h / 2, w, 1, Pal.Wood[1]);
        img.FillRect(x - 3, top + h + 1, w + 6, 2, Pal.Wood[2]);
    }

    private static PixelImage Mote()
    {
        var img = new PixelImage(3, 3);
        img[1, 0] = img[0, 1] = img[2, 1] = img[1, 2] = Pal.Gold.WithAlpha(0.5f);
        img[1, 1] = Pal.White;
        return img;
    }
}
