using FirstPlayable.Model;
using MicroEngine.Loop;

namespace FirstPlayable;

internal static class GoalRenderer
{
    internal static void Draw(ICanvas canvas, GameModel game)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(game);
        if (!game.GoalEnabled) return;
        canvas.Fill(GameModel.Goal, game.Completed ? new(80, 200, 120) : new(70, 200, 210));
        if (canvas is ILabelCanvas labels)
            labels.Label(game.Completed ? "Goal reached - Space to restart" : "Reach the cyan goal", 20, 20, 20, new(240, 240, 245));
    }
}
