using FirstPlayable.Model;
using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;

var limit = 0;
var failUpdate = false;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--smoke" && i + 1 < args.Length && limit == 0
        && int.TryParse(args[++i], out var parsed) && parsed is >= 1 and <= 600)
        limit = parsed;
    else if (args[i] == "--fail-update" && !failUpdate) failUpdate = true;
    else
    {
        Console.Error.WriteLine("Usage: FirstPlayable [--smoke N(1..600)] [--fail-update]");
        return 2;
    }
}

var game = new GameModel();
var frames = 0;
var updates = 0;
var options = new LoopOptions(TimeSpan.FromSeconds(1d / 60), 5, TimeSpan.FromMilliseconds(250));
Console.WriteLine("Controls: Left/A, Right/D, Space jump; Escape/window close exits.");
var report = FrameLoop.Run(() => new DesktopPlatform(new DesktopWindowOptions(640, 360, "MicroEngine H1.0 — first tracer")), options,
    frame =>
    {
        if (failUpdate) throw new ApplicationException("Requested update failure.");
        updates++;
        game.Update(frame.Delta, frame.Input);
    },
    (canvas, frame) =>
    {
        // Game geometry and colors belong to this external consumer, not the engine.
        canvas.Clear(new Rgba(24, 28, 40));
        canvas.Fill(new Rect(0, 300, 640, 60), new Rgba(70, 100, 75));
        canvas.Fill(new Rect(game.X, game.Y, 24, 32), new Rgba(240, 180, 65));
        frames++;
        if (limit > 0 && frames >= limit) frame.Control.RequestStop();
    });
Console.WriteLine($"outcome:{report.Outcome};frames:{frames};updates:{updates}");
if (report.PrimaryError is not null) Console.Error.WriteLine($"primary:{report.PrimaryError.GetType().Name}:{report.PrimaryError.Message}");
foreach (var error in report.CleanupErrors) Console.Error.WriteLine($"cleanup:{error.GetType().Name}:{error.Message}");
return report.Outcome == ExecutionOutcome.Failed ? 1 : 0;
