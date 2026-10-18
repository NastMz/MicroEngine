using Platformer.Model;
using MicroEngine.Loop;
namespace Platformer.Checks;
public static class Course
{
    public static readonly TimeSpan Step = TimeSpan.FromSeconds(1d / 60);
    public static int Run(LevelGame game, Action<TimeSpan, LevelGame>? afterStep = null)
    {
        float previousX = float.NaN;
        for (int i = 1; i <= 2000; i++)
        {
            int next = Enumerable.Range(0, 3).FirstOrDefault(index => !game.IsCollected(index), -1);
            float target = next >= 0 ? LevelGame.Coins[next].X + 12 : LevelGame.Flag.X + 24;
            bool right = game.Body.X + 14 < target;
            bool obstacle = right && game.Body.X == previousX;
            bool hazard = right && game.Body.X + 28 >= LevelGame.Hazard.X - 60 && game.Body.X + 28 <= LevelGame.Hazard.X;
            bool jump = game.Grounded && (obstacle || hazard);
            previousX = game.Body.X;
            game.Update(Step, new(right ? ["right"] : [], jump ? ["jump"] : []));
            afterStep?.Invoke(Step, game);
            if (game.Dead) throw new InvalidOperationException($"Course died at {game.Body}.");
            if (game.Completed) return i;
        }
        throw new InvalidOperationException($"Course bound exceeded at {game.Body}; coins:{game.CollectedCount}.");
    }
}
