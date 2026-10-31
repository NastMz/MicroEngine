using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.Rules;

namespace TrailDuel.Ui;

/// <summary>
/// Everything a player needs to judge a move, in the game's cream panel: name, type, category, power,
/// accuracy, PP, how it fares against the current foe, and what it does (<see cref="MoveText"/>).
/// </summary>
internal static class MoveCard
{
    /// <summary>The foe a matchup line is shown for (battle only).</summary>
    public readonly record struct Foe(string Name, ElementType Type);

    /// <summary>Height the card needs at <paramref name="width"/>.</summary>
    public static float Height(BitmapFont font, MoveData move, float width, Foe? foe = null)
        => 33 + (Matchup(move, foe) is null ? 0 : 11) + Lines(font, move, width).Count * font.LineHeight + 2;

    /// <summary>Draws the card with its top-left corner at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public static void Draw(IRenderer r, BitmapFont font, MoveData move, int? pp, float x, float y, float width, Foe? foe = null)
    {
        var rect = new RectF(x, y, width, Height(font, move, width, foe));
        UiKit.Box(r, rect);
        float left = rect.X + 8, right = rect.Right - 8, row = rect.Y + 6;

        UiKit.Text(r, font, move.Name, left, row);
        UiKit.TypeBadge(r, font, move.Type, left + font.MeasureWidth(move.Name) + 6, row - 1);
        UiKit.TextRight(r, font, MoveText.Category(move), right, row, UiKit.Muted);

        row += 12;
        float cx = left;
        void Stat(string label, string value)
        {
            UiKit.Text(r, font, label, cx, row, UiKit.Muted);
            cx += font.MeasureWidth(label) + 3;
            UiKit.Text(r, font, value, cx, row);
            cx += font.MeasureWidth(value) + 10;
        }
        Stat("POW", MoveText.Power(move));
        Stat("ACC", MoveText.Accuracy(move));
        Stat("PP", pp is { } current ? $"{current}/{move.Pp}" : move.Pp.ToString());

        row += 11;
        if (Matchup(move, foe) is { } matchup)
        {
            UiKit.Text(r, font, matchup.Text, left, row, matchup.Color);
            row += 11;
        }
        r.FillRect(new RectF(left, row - 1, rect.Width - 16, 1), UiKit.FillShade);
        row += 2;
        foreach (var line in Lines(font, move, width))
        {
            UiKit.Text(r, font, line, left, row);
            row += font.LineHeight;
        }
    }

    private static IReadOnlyList<string> Lines(BitmapFont font, MoveData move, float width) => font.Wrap(MoveText.Describe(move), (int)width - 16);

    private static (string Text, Color Color)? Matchup(MoveData move, Foe? foe)
    {
        if (foe is not { } f || MoveText.Matchup(move, f.Type) is not { } label) return null;
        var color = label switch
        {
            "Super effective" => Color.Hex("3f9a4a"),
            "No effect" => UiKit.Muted,
            _ => Color.Hex("c8504a"),
        };
        return ($"{label} against {f.Name}", color);
    }
}
