using Platformer.Model;
using MicroEngine.Loop;
namespace Platformer.Checks;
public static class Course
{
    public static readonly TimeSpan Step = TimeSpan.FromSeconds(1d / 60);
    public static int Run(LevelGame game, Action<TimeSpan, LevelGame>? afterStep = null, bool anticipateGaps = true, bool anticipateEnemies = true, Func<LevelGame,bool>? stopWhen = null, Action<TimeSpan,InputSnapshot>? update = null)
    {
        float previousX = float.NaN;
        for (int i = 1; i <= 2000; i++)
        {
            int next = Enumerable.Range(0, game.CoinCount).FirstOrDefault(index => !game.IsCollected(index), -1);
            float target = next >= 0 ? game.CoinPositions[next].X + 12 : game.Goal.X + 24;
            bool right = game.Body.X + 14 < target;
            bool obstacle = right && game.Body.X == previousX;
            bool hazard = right && game.Body.X + 28 >= game.Danger.X - 60 && game.Body.X + 28 <= game.Danger.X;
            // Verification driver only: anticipate a real unsupported floor edge, never auto-jump in the game.
            bool gap = anticipateGaps && right && game.Terrain.Any(solid => solid.Y == 432 && solid.X + solid.Width < 1920
                && game.Body.X + 28 >= solid.X + solid.Width - 60 && game.Body.X + 28 <= solid.X + solid.Width
                && !game.Terrain.Any(nextSolid => nextSolid.Y == 432 && nextSolid.X == solid.X + solid.Width));
            bool enemy = anticipateEnemies && right && game.EnemyBody is { } patrol && patrol.X - (game.Body.X + 28) is >= 0 and <= 70;
            bool jump = game.Grounded && (obstacle || hazard || gap || enemy);
            previousX = game.Body.X;
            var input=new InputSnapshot(right ? ["right"] : [], jump ? ["jump"] : []);
            if(update is null)game.Update(Step,input);else update(Step,input);
            afterStep?.Invoke(Step, game);
            if (game.Dead) throw new InvalidOperationException($"Course died at {game.Body}.");
            if (game.Completed || stopWhen?.Invoke(game) == true) return i;
        }
        throw new InvalidOperationException($"Course bound exceeded at {game.Body}; coins:{game.CollectedCount}.");
    }
}
