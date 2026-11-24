using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// The seed-spitting heart-trap: a hand-drawn monster from the Ninja Adventure pack (CC0, see CREDITS.md)
/// in <c>Content/monsters</c>, repainted at load with the game's own ramps and ink outline. Which cells of its
/// sheet are the three frames, and their anchor at the feet, is in <c>Content/pack.sheet</c>. Every other enemy and
/// the Moss Warden are procedural (<see cref="CreatureArt"/>).
/// </summary>
public static class MonsterArt
{
    public static void AddAll(SpriteAtlasBuilder b, SpriteSheet pack)
    {
        foreach (var (name, img, origin) in Frames(pack))
        {
            b.Add(name, img, origin);
            b.Add(name + "!", img.Silhouette(Pal.White), origin);
        }
    }

    /// <summary>Every heart-trap frame, repainted, with its pivot (hit-flash twins are added by <see cref="AddAll"/>).</summary>
    /// <param name="pack">The pack pieces (<c>Content/pack.sheet</c>): the frames "spitter0" to "spitter2".</param>
    public static IEnumerable<(string Name, PixelImage Image, Vector2 Origin)> Frames(SpriteSheet pack)
    {
        // Spitter: a snapping heart-trap; mouth shut, rearing up as the tell, then wide open as it fires.
        foreach (var frame in pack.Frames.Where(f => f.Name.StartsWith("spitter", StringComparison.Ordinal)))
            yield return (frame.Name, Repaint(frame.Image,
                ("543c52", Pal.Roof[0]), ("8f3e56", Pal.Roof[1]), ("e0394c", Pal.Roof[2]), ("e46d3a", Pal.Roof[3]), ("c8966b", Pal.Sand[2]), ("fce2ca", Pal.Cream)), frame.Origin);
    }

    /// <summary>Swaps the pack's colours for the game's: listed colours by hex, then the pack's outline for <see cref="Pal.Ink"/>.</summary>
    private static PixelImage Repaint(PixelImage frame, params (string From, Color To)[] swaps)
    {
        foreach (var (from, to) in swaps) frame.Recolor(Color.Hex(from), to);
        return frame.Recolor(Color.Hex("141b1b"), Pal.Ink);
    }
}
