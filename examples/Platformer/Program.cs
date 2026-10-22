using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;
using Platformer;
using Platformer.Model;
if (!MicroEngine.Examples.LaunchOptions.TryParse(args, out var launch))
{
    Console.Error.WriteLine("Usage: Platformer [--smoke N(1..600)] [--fail-update]");
    return 2;
}
int limit = launch.FrameLimit; bool fail = launch.FailUpdate;
try
{
    var game = new MeadowSession();
    using var art = new PlatformerPresentation(Path.Combine(AppContext.BaseDirectory,"Assets"), DesktopImageDecoder.Decode);
    int frames=0, updates=0;
    Console.WriteLine("Meadow Run: Space starts. P pauses/resumes, R restarts campaign, Escape exits. Paused: Up/Down and Space select. Collect three coins per level, avoid spiked slime and reach the flag.");
    var report = FrameLoop.Run(() => GamePlatform.ForSession(new DesktopPlatform(new DesktopWindowOptions(768,480,"MicroEngine - Meadow Run",2048,32)),game),
        new LoopOptions(TimeSpan.FromSeconds(1d/60),5,TimeSpan.FromMilliseconds(250)),
        frame => { if(fail) throw new ApplicationException("Requested update failure."); updates++; game.Update(frame.Delta,frame.Input); art.AdvanceSession(frame.Delta,game); },
        (canvas,frame) => { art.DrawSession(canvas,game); frames++; if(limit>0 && frames>=limit) frame.Control.RequestStop(); });
    Console.WriteLine($"outcome:{report.Outcome};frames:{frames};updates:{updates}");
    if(report.PrimaryError is not null) Console.Error.WriteLine($"primary:{report.PrimaryError.GetType().Name}:{report.PrimaryError.Message}");
    foreach(var error in report.CleanupErrors) Console.Error.WriteLine($"cleanup:{error.GetType().Name}:{error.Message}");
    return report.Outcome == ExecutionOutcome.Failed ? 1 : 0;
}
catch(Exception error) { Console.Error.WriteLine($"startup:{error.GetType().Name}:{error.Message}"); return 1; }
