using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;
using RoomExplorer.Model;
using TurnExplorer;
using TurnExplorer.Model;
using TurnExplorer.Storage;

if (!LaunchOptions.TryParse(args, out var options) || options is null)
{
    Console.Error.WriteLine("Usage: TurnExplorer [--smoke N(1..600)] [--fail-update] [--load PATH] [--save-new PATH]");
    return 2;
}
var limit = options.Limit;
var failUpdate = options.FailUpdate;

var game = new TurnSession();
if (options.LoadPath is not null)
{
    try
    {
        game = TurnSession.Restore(CheckpointFile.Read(options.LoadPath));
        Console.WriteLine("checkpoint:loaded");
    }
    catch (Exception error)
    {
        Console.Error.WriteLine($"checkpoint:load-failed:{error.GetType().Name}:{error.Message}");
        return 1;
    }
}
var frames = 0;
var updates = 0;
Console.WriteLine("Controls: Arrows/WASD explore; Space at marker opens menu; Up/Down select, Space confirms; four Advances complete once; top=Advance, bottom=Return; Escape/window close exits.");
var report = FrameLoop.Run(
    () => new DesktopPlatform(new DesktopWindowOptions(640, 360, "MicroEngine H3.0 — turn explorer")),
    new LoopOptions(TimeSpan.FromSeconds(1d / 60), 5, TimeSpan.FromMilliseconds(250)),
    frame =>
    {
        if (failUpdate) throw new ApplicationException("Requested update failure.");
        var room = game.Room; var wasOpen = game.MenuOpen; var selected = game.SelectedOption;
        var wasCompleted = game.EncounterCompleted;
        game.Update(frame.Delta, frame.Input);
        updates++;
        if (game.Room != room) Console.WriteLine($"room:{game.Room}");
        if (game.MenuOpen && !wasOpen) Console.WriteLine("menu:open;selected:Advance");
        else if (!game.MenuOpen && wasOpen) Console.WriteLine("menu:closed");
        else if (game.MenuOpen && game.SelectedOption != selected) Console.WriteLine($"menu:selected:{game.SelectedOption}");
        if (game.TurnResolved) Console.WriteLine($"turn:phase:{game.TurnPhase}");
        if (game.EncounterCompleted && !wasCompleted) Console.WriteLine("encounter:completed");
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
        MenuRenderer.Draw(canvas, game);
        frames++;
        if (limit > 0 && frames >= limit) frame.Control.RequestStop();
    });
Console.WriteLine($"outcome:{report.Outcome};frames:{frames};updates:{updates}");
if (report.PrimaryError is not null) Console.Error.WriteLine($"primary:{report.PrimaryError.GetType().Name}:{report.PrimaryError.Message}");
foreach (var error in report.CleanupErrors) Console.Error.WriteLine($"cleanup:{error.GetType().Name}:{error.Message}");
if (report.Outcome == ExecutionOutcome.Failed) return 1;
if (options.SaveNewPath is not null)
{
    try
    {
        CheckpointFile.CreateNew(options.SaveNewPath, game.Capture());
        Console.WriteLine("checkpoint:saved");
    }
    catch (Exception error)
    {
        Console.Error.WriteLine($"checkpoint:save-failed:{error.GetType().Name}:{error.Message}");
        return 1;
    }
}
return 0;
