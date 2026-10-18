using FirstPlayable;
using FirstPlayable.Model;
using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;

try
{
    using var sprites = new AvatarSprites();
    using (var window = new DesktopPlatform(new(640, 360, "MicroEngine platformer goal verification")))
    {
        var game = new GameModel(new(TimeSpan.FromMilliseconds(100)), [new(180, 260, 64, 40)], true);
        var animation = new AvatarAnimation(); var step = TimeSpan.FromSeconds(1d / 60);
        void Draw(bool completed)
        {
            for (int frame = 0; frame < 2; frame++)
            {
                window.Begin(); var canvas = window.Canvas; canvas.Clear(new(24, 28, 40));
                canvas.Fill(new(0, 300, 640, 60), new(70, 100, 75));
                foreach (var solid in game.Obstacles) canvas.Fill(new(solid.X, solid.Y, solid.Width, solid.Height), new(95, 130, 165));
                GoalRenderer.Draw(canvas, game); sprites.Draw(canvas, game.X, game.Y, animation); window.End();
            }
            var image = Raylib.LoadImageFromScreen();
            try
            {
                var goal = Raylib.GetImageColor(image, 615, 270); var expected = completed ? new Rgba(80, 200, 120) : new Rgba(70, 200, 210);
                if (goal.R != expected.R || goal.G != expected.G || goal.B != expected.B) throw new InvalidOperationException("Goal color mismatch.");
                if (!completed)
                {
                    var head = Raylib.GetImageColor(image, 50, 274);
                    if (head.R != 240 || head.G != 180 || head.B != 65) throw new InvalidOperationException("Spawn avatar mismatch.");
                }
                int glyphs = 0;
                for (int y = 20; y < 43; y++) for (int x = 20; x < 380; x++) if (Raylib.GetImageColor(image, x, y).G > 100) glyphs++;
                if (glyphs <= 10) throw new InvalidOperationException("Goal HUD missing.");
            }
            finally { Raylib.UnloadImage(image); }
        }
        int RunCourse()
        {
            for (int i = 1; i <= 600; i++)
            {
                float oldX = game.X;
                game.Update(step, new(["right"], game.Grounded && game.X < 260 ? ["jump"] : []));
                animation.Update(step, game.Completed ? 0 : game.X - oldX, game.Grounded);
                if (game.Completed)
                {
                    if (game.X <= 556 || game.Y != 268 || !game.Grounded || game.VerticalVelocity != 0)
                        throw new InvalidOperationException("Completed course state mismatch.");
                    return i;
                }
            }
            throw new InvalidOperationException("Course input bound exceeded.");
        }
        Draw(false); int first = RunCourse(); Draw(true); float finishX = game.X;
        game.Update(TimeSpan.MaxValue, new(["right", "jump"], []));
        if (!game.Completed || game.X != finishX) throw new InvalidOperationException("Held input altered completed run.");
        game.Update(step, new(["right"], ["jump", "jump"])); animation = new();
        game.Update(step, new([], []));
        if (game.Completed || game.X != 40 || game.Y != 268 || !game.Grounded || game.VerticalVelocity != 0)
            throw new InvalidOperationException("Reset input leaked into next update.");
        Draw(false); int replay = RunCourse();
        if (first != replay) throw new InvalidOperationException("Reset replay diverged.");
        Console.WriteLine("goal:course:verified"); Console.WriteLine("goal:reset:verified");
    }
    Console.WriteLine("goal:closed"); return 0;
}
catch (Exception error) { Console.Error.WriteLine($"goal:failed:{error.GetType().Name}:{error.Message}"); return 1; }
