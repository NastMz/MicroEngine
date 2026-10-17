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
            canvas.Fill(new Rect(20 + 20 * i, 20, 12, 12), session.EncounterCompleted ? new Rgba(80, 200, 120)
                : session.TurnPhase == i ? new Rgba(70, 200, 210) : gray);
        if (session.EncounterCompleted && canvas is ILabelCanvas completedLabels)
            completedLabels.Label("Completed", 20, 42, 20, new(80, 200, 120));
        if (!session.MenuOpen) return;
        canvas.Fill(new Rect(420, 40, 200, 40), session.SelectedOption == MenuOption.Advance ? gold : gray);
        canvas.Fill(new Rect(420, 88, 200, 40), session.SelectedOption == MenuOption.Return ? gold : gray);
        var dark = new Rgba(24, 28, 40);
        canvas.Fill(new Rect(432, 54, 12, 12), dark);
        canvas.Fill(new Rect(432, 102, 12, 12), dark);
        canvas.Fill(new Rect(452, 102, 12, 12), dark);
        if (canvas is ILabelCanvas labels)
        {
            var light = new Rgba(240, 240, 245);
            labels.Label("Advance", 480, 50, 20, session.SelectedOption == MenuOption.Advance ? dark : light);
            labels.Label("Return", 480, 98, 20, session.SelectedOption == MenuOption.Return ? dark : light);
        }
    }
}
