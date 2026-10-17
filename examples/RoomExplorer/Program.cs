using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;
using RoomExplorer.Model;

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
        Console.Error.WriteLine("Usage: RoomExplorer [--smoke N(1..600)] [--fail-update]");
        return 2;
    }
}

var game = new Explorer();
var frames = 0;
var updates = 0;
Console.WriteLine("Controls: Arrows/WASD move; cyan edge passage changes room; Space interacts with marker; Escape/window close exits.");
var report = FrameLoop.Run(
    () => new DesktopPlatform(new DesktopWindowOptions(640, 360, "MicroEngine H2.0 — room explorer")),
    new LoopOptions(TimeSpan.FromSeconds(1d / 60), 5, TimeSpan.FromMilliseconds(250)),
    frame =>
    {
        if (failUpdate) throw new ApplicationException("Requested update failure.");
        var previous = game.Room;
        game.Update(frame.Delta, frame.Input);
        updates++;
        if (game.Room != previous) Console.WriteLine($"room:{game.Room}");
        if (game.InteractionOccurred)
            Console.WriteLine($"interaction:{Explorer.MarkerId};active:{(game.MarkerActivated ? "true" : "false")}");
    },
    (canvas, frame) =>
    {
        var courtyard = game.Room == Room.Courtyard;
        canvas.Clear(courtyard ? new Rgba(24, 40, 32) : new Rgba(32, 24, 48));
        canvas.Fill(new Rect(courtyard ? 632 : 0, 120, 8, 120), new Rgba(70, 200, 210));
        if (game.Room == Explorer.MarkerRoom)
            canvas.Fill(new Rect(Explorer.MarkerX, Explorer.MarkerY, Explorer.MarkerWidth, Explorer.MarkerHeight),
                game.MarkerActivated ? new Rgba(80, 200, 120) : new Rgba(180, 100, 70));
        canvas.Fill(new Rect(game.X, game.Y, 24, 32), new Rgba(240, 180, 65));
        frames++;
        if (limit > 0 && frames >= limit) frame.Control.RequestStop();
    });
Console.WriteLine($"outcome:{report.Outcome};frames:{frames};updates:{updates}");
if (report.PrimaryError is not null) Console.Error.WriteLine($"primary:{report.PrimaryError.GetType().Name}:{report.PrimaryError.Message}");
foreach (var error in report.CleanupErrors) Console.Error.WriteLine($"cleanup:{error.GetType().Name}:{error.Message}");
return report.Outcome == ExecutionOutcome.Failed ? 1 : 0;
