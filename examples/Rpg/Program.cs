using Rpg;
using Rpg.Model;
using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;

if (!MicroEngine.Examples.LaunchOptions.TryParse(args, out var launch))
{
    Console.Error.WriteLine("Usage: Rpg [--smoke N(1..600)] [--fail-update]");
    return 2;
}
int limit = launch.FrameLimit; bool fail = launch.FailUpdate;
try
{
    string assets=Path.Combine(AppContext.BaseDirectory,"Assets");
    var sources=RpgPresentation.AssetNames.ToDictionary(name=>name,name=>DesktopImageDecoder.Decode(File.ReadAllBytes(Path.Combine(assets,name+".png"))));
    using var art=new RpgPresentation(sources);var game=new RpgSession();int frames=0,updates=0;
    Console.WriteLine("Trail Duel: explore Camp/Meadow. Space at the red sentinel marker starts combat; up/down selects Attack/Guard/Retreat, Space confirms. Escape exits.");
    var report=FrameLoop.Run(()=>new DesktopPlatform(new(672,480,"MicroEngine - Trail Duel",2048,32)),
        new LoopOptions(TimeSpan.FromSeconds(1d/60),5,TimeSpan.FromMilliseconds(250)),
        frame=>
        {
            if(fail)throw new ApplicationException("Requested update failure.");updates++;
            var mode=game.Mode;var room=game.Room;var turn=game.Battle.Turn;
            game.Update(frame.Delta,frame.Input);art.Advance(frame.Delta,game);
            if(room!=game.Room)Console.WriteLine($"room:{game.Room}");
            if(mode!=game.Mode)Console.WriteLine($"mode:{game.Mode}");
            if(turn!=game.Battle.Turn)Console.WriteLine($"turn:{game.Battle.Turn};hp:{game.Battle.PlayerHp}/{game.Battle.OpponentHp}");
            if(mode!=RpgMode.Result&&game.Mode==RpgMode.Result)Console.WriteLine($"encounter:{game.Battle.Outcome}");
        },
        (canvas,frame)=>{art.Draw(canvas,game);frames++;if(limit>0&&frames>=limit)frame.Control.RequestStop();});
    Console.WriteLine($"outcome:{report.Outcome};frames:{frames};updates:{updates}");
    if(report.PrimaryError is not null)Console.Error.WriteLine($"primary:{report.PrimaryError.GetType().Name}:{report.PrimaryError.Message}");
    foreach(var error in report.CleanupErrors)Console.Error.WriteLine($"cleanup:{error.GetType().Name}:{error.Message}");
    return report.Outcome==ExecutionOutcome.Failed?1:0;
}
catch(Exception error){Console.Error.WriteLine($"startup:{error.GetType().Name}:{error.Message}");return 1;}
