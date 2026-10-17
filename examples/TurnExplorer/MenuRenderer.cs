using MicroEngine.Loop;
using TurnExplorer.Model;

namespace TurnExplorer;

internal static class MenuRenderer
{
    internal static void Draw(ICanvas canvas, TurnSession session)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(session);
        var gray = new Rgba(70, 75, 85); var gold = new Rgba(240, 180, 65);
        for (var i = 0; i < 4; i++)
            canvas.Fill(new Rect(20 + 20 * i, 20, 12, 12), session.TurnPhase == i ? new Rgba(70, 200, 210) : gray);
        if (!session.MenuOpen) return;
        canvas.Fill(new Rect(420, 40, 200, 40), session.SelectedOption == MenuOption.Advance ? gold : gray);
        canvas.Fill(new Rect(420, 88, 200, 40), session.SelectedOption == MenuOption.Return ? gold : gray);
        var dark = new Rgba(24, 28, 40);
        canvas.Fill(new Rect(432, 54, 12, 12), dark);
        canvas.Fill(new Rect(432, 102, 12, 12), dark);
        canvas.Fill(new Rect(452, 102, 12, 12), dark);
    }
}
