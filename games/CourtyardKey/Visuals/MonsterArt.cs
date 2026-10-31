using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// The seed-spitting heart-trap: a hand-drawn monster from the Ninja Adventure pack (CC0, see CREDITS.md)
/// in <c>Content/monsters</c>, repainted at load with the game's own ramps and ink outline. The sheet is
/// 16x16 cells: columns face down, up, left, right; rows are animation frames. Every other enemy and the
/// Moss Warden are procedural (<see cref="CreatureArt"/>).
/// </summary>
public static class MonsterArt
{
    private const int Cell = 16;
    private const int Down = 0;

    public static void AddAll(SpriteAtlasBuilder b, ContentFolder content)
    {
        foreach (var (name, img, origin) in Frames(content))
        {
            b.Add(name, img, origin);
            b.Add(name + "!", img.Silhouette(Pal.White), origin);
        }
    }

    /// <summary>Every heart-trap frame, repainted, with its pivot (hit-flash twins are added by <see cref="AddAll"/>).</summary>
    public static IEnumerable<(string Name, PixelImage Image, Vector2 Origin)> Frames(ContentFolder content)
    {
        var feet = new Vector2(8, 14);

        // Spitter: a snapping heart-trap; mouth shut, rearing up as the tell, then wide open as it fires.
        var trap = Repaint(content.Image("monsters/spitter.png"),
            ("543c52", Pal.Roof[0]), ("8f3e56", Pal.Roof[1]), ("e0394c", Pal.Roof[2]), ("e46d3a", Pal.Roof[3]), ("c8966b", Pal.Sand[2]), ("fce2ca", Pal.Cream));
        yield return ("spitter0", Cut(trap, Down, 0), feet);
        yield return ("spitter1", Cut(trap, Down, 1), feet);
        yield return ("spitter2", Cut(trap, Down, 2), feet);
    }

    /// <summary>Swaps the pack's colours for the game's: listed colours by hex, then the pack's outline for <see cref="Pal.Ink"/>.</summary>
    private static PixelImage Repaint(PixelImage sheet, params (string From, Color To)[] swaps)
    {
        foreach (var (from, to) in swaps) sheet.Recolor(Color.Hex(from), to);
        return sheet.Recolor(Color.Hex("141b1b"), Pal.Ink);
    }

    private static PixelImage Cut(PixelImage sheet, int column, int row) => sheet.Crop(column * Cell, row * Cell, Cell, Cell);
}
