using MapExplorer;
using MapExplorer.Model;
using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;

int limit = 0;
bool failUpdate = false;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--smoke" && i + 1 < args.Length && limit == 0
        && int.TryParse(args[i + 1], out int count) && count is >= 1 and <= 600)
    {
        limit = count; i++;
    }
    else if (args[i] == "--fail-update" && !failUpdate) failUpdate = true;
    else
    {
        Console.Error.WriteLine("Usage: MapExplorer [--smoke N(1..600)] [--fail-update]");
        return 2;
    }
}

var game = new MapWalker();
int frames = 0, updates = 0;
Console.WriteLine("Controls: Arrows/WASD move; Space at blue Meadow switch opens its gray gate; barriers may also be detoured; cyan edge passage changes map; Escape/window close exits.");
var report = FrameLoop.Run(
    () => new DesktopPlatform(new(640, 360, "MicroEngine — map and viewport explorer")),
    new(TimeSpan.FromSeconds(1d / 60), 5, TimeSpan.FromMilliseconds(250)),
    frame =>
    {
        if (failUpdate) throw new ApplicationException("Requested update failure.");
        var previous = game.Zone;
        var wasOpen = game.GateOpen;
        game.Update(frame.Delta, frame.Input);
        updates++;
        if (game.Zone != previous) Console.WriteLine($"zone:{game.Zone}");
        if (game.GateOpen && !wasOpen) Console.WriteLine("gate:open");
    },
    (canvas, frame) =>
    {
        MapRenderer.Draw(canvas, game);
        frames++;
        if (limit > 0 && frames >= limit) frame.Control.RequestStop();
    });
Console.WriteLine($"outcome:{report.Outcome};frames:{frames};updates:{updates}");
if (report.PrimaryError is not null) Console.Error.WriteLine($"primary:{report.PrimaryError.GetType().Name}:{report.PrimaryError.Message}");
foreach (var error in report.CleanupErrors) Console.Error.WriteLine($"cleanup:{error.GetType().Name}:{error.Message}");
return report.Outcome == ExecutionOutcome.Failed ? 1 : 0;
