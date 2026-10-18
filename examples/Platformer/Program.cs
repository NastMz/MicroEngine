using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;
using Platformer;
using Platformer.Model;
int limit = 0; bool fail = false;
for (int i=0;i<args.Length;i++)
{
    if (args[i] == "--smoke" && limit == 0 && i+1 < args.Length && int.TryParse(args[++i],out int n) && n is >=1 and <=600) limit=n;
    else if (args[i] == "--fail-update" && !fail) fail=true;
    else { Console.Error.WriteLine("Usage: Platformer [--smoke N(1..600)] [--fail-update]"); return 2; }
}
try
{
    var game = new LevelGame();
    using var art = new PlatformerPresentation(Path.Combine(AppContext.BaseDirectory,"Assets"), DesktopImageDecoder.Decode);
    int frames=0, updates=0;
    Console.WriteLine("Collect three coins, jump platforms and spikes, reach green flag. Space retries/replays; Escape exits.");
    var report = FrameLoop.Run(() => new GamePlatform(new DesktopPlatform(new DesktopWindowOptions(768,480,"MicroEngine - Meadow Run",2048,32)),game),
        new LoopOptions(TimeSpan.FromSeconds(1d/60),5,TimeSpan.FromMilliseconds(250)),
        frame => { if(fail) throw new ApplicationException("Requested update failure."); updates++; game.Update(frame.Delta,frame.Input); art.Advance(frame.Delta,game); },
        (canvas,frame) => { art.Draw(canvas,game); frames++; if(limit>0 && frames>=limit) frame.Control.RequestStop(); });
    Console.WriteLine($"outcome:{report.Outcome};frames:{frames};updates:{updates}");
    if(report.PrimaryError is not null) Console.Error.WriteLine($"primary:{report.PrimaryError.GetType().Name}:{report.PrimaryError.Message}");
    foreach(var error in report.CleanupErrors) Console.Error.WriteLine($"cleanup:{error.GetType().Name}:{error.Message}");
    return report.Outcome == ExecutionOutcome.Failed ? 1 : 0;
}
catch(Exception error) { Console.Error.WriteLine($"startup:{error.GetType().Name}:{error.Message}"); return 1; }
