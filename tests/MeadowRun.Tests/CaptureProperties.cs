using MicroEngine.Graphics;

namespace MeadowRun.Tests;

/// <summary>
/// Property checks for scripted captures: instead of comparing a whole frame with a recording, a test states what the
/// scenario's known state implies (a sprite at a position, a HUD region that changes with a value) and checks only that.
/// Each check names the property it evaluates and is exact: an opaque sprite drawn at whole pixels with nearest sampling
/// and scale 1 copies its texels exactly on every renderer, by the written rendering rules. Self-contained (it depends
/// only on <see cref="PixelImage"/>) so it can move to a shared testing assembly.
/// </summary>
internal static class CaptureProperties
{
    /// <summary>A rectangle of whole pixels.</summary>
    public readonly record struct Region(int X, int Y, int Width, int Height)
    {
        public override string ToString() => $"({X},{Y} {Width}x{Height})";
    }

    /// <summary>
    /// Position, exact: whether every opaque texel (alpha 255) of <paramref name="sprite"/>, mirrored when
    /// <paramref name="flipX"/>, appears with its exact colour at (<paramref name="x"/>, <paramref name="y"/>) plus the
    /// texel's offset. Translucent texels are skipped: they mix with whatever lies behind.
    /// </summary>
    public static bool SpriteAt(PixelImage capture, PixelImage sprite, int x, int y, bool flipX = false)
    {
        bool any = false;
        for (int ty = 0; ty < sprite.Height; ty++)
            for (int tx = 0; tx < sprite.Width; tx++)
            {
                var texel = sprite[flipX ? sprite.Width - 1 - tx : tx, ty];
                if (texel.A != 255) continue;
                any = true;
                if (!capture.InBounds(x + tx, y + ty) || capture[x + tx, y + ty] != texel) return false;
            }
        return any;
    }

    /// <summary>
    /// Every top-left position within <paramref name="radius"/> pixels of <paramref name="expectedX"/>,
    /// <paramref name="expectedY"/> where one of <paramref name="frames"/> is found (<see cref="SpriteAt"/>), nearest first.
    /// </summary>
    public static List<(int X, int Y, int Frame)> Locate(PixelImage capture, IReadOnlyList<PixelImage> frames, int expectedX, int expectedY, int radius, bool flipX = false)
    {
        var found = new List<(int X, int Y, int Frame)>();
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
                for (int f = 0; f < frames.Count; f++)
                    if (SpriteAt(capture, frames[f], expectedX + dx, expectedY + dy, flipX)) found.Add((expectedX + dx, expectedY + dy, f));
        return [.. found.OrderBy(p => Math.Abs(p.X - expectedX) + Math.Abs(p.Y - expectedY))];
    }

    /// <summary>
    /// Position, exact (±0): one of <paramref name="frames"/> (the animation frames the state allows) is drawn with its
    /// top-left exactly at the expected pixel. When it is not, the message says where the sprite was found nearby.
    /// </summary>
    public static void AssertSpriteAt(PixelImage capture, IReadOnlyList<PixelImage> frames, int x, int y, string what, bool flipX = false, int searchRadius = 16)
    {
        if (frames.Any(f => SpriteAt(capture, f, x, y, flipX))) return;
        var near = Locate(capture, frames, x, y, searchRadius, flipX);
        string where = near.Count == 0 ? $"not found within {searchRadius} px" : $"found at ({near[0].X},{near[0].Y}), moved by ({near[0].X - x},{near[0].Y - y})";
        Assert.Fail($"{what}: expected at ({x},{y}) exactly, {where}");
    }

    /// <summary>The number of pixels of <paramref name="region"/> whose colour differs between two captures.</summary>
    public static int Changed(PixelImage before, PixelImage after, Region region)
    {
        int changed = 0;
        for (int y = region.Y; y < region.Y + region.Height; y++)
            for (int x = region.X; x < region.X + region.Width; x++)
                if (before[x, y] != after[x, y]) changed++;
        return changed;
    }

    /// <summary>State: the region shows something else in <paramref name="after"/> (its value changed).</summary>
    public static void AssertRegionChanged(PixelImage before, PixelImage after, Region region, string what) =>
        Assert.IsTrue(Changed(before, after, region) > 0, $"{what}: region {region} should differ between the captures");

    /// <summary>State: the region is pixel for pixel the same in both captures (its value held).</summary>
    public static void AssertRegionUnchanged(PixelImage before, PixelImage after, Region region, string what)
    {
        int changed = Changed(before, after, region);
        Assert.AreEqual(0, changed, $"{what}: region {region} should be identical, {changed} pixels differ");
    }
}
