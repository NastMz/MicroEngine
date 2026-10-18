using Adventure;
using Adventure.Model;
using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;
int limit=0;bool fail=false;
for(int i=0;i<args.Length;i++)
{
    if(args[i]=="--smoke"&&limit==0&&i+1<args.Length&&int.TryParse(args[++i],out int n)&&n is >=1 and <=600)limit=n;
    else if(args[i]=="--fail-update"&&!fail)fail=true;
    else { Console.Error.WriteLine("Usage: Adventure [--smoke N(1..600)] [--fail-update]");return 2; }
}
try
{
    string assets=Path.Combine(AppContext.BaseDirectory,"Assets");
    var sources=AdventurePresentation.AssetNames.ToDictionary(name=>name,name=>DesktopImageDecoder.Decode(File.ReadAllBytes(Path.Combine(assets,name+".png"))));
    using var art=new AdventurePresentation(sources);var game=new AdventureModel();int frames=0,updates=0;
    Console.WriteLine("Courtyard Key: use the lever, explore the shrine, take the key and restore the altar. Space interacts/replays; Escape exits.");
    var report=FrameLoop.Run(()=>new DesktopPlatform(new DesktopWindowOptions(640,360,"MicroEngine - Courtyard Key",2048,32)),
        new LoopOptions(TimeSpan.FromSeconds(1d/60),5,TimeSpan.FromMilliseconds(250)),
        frame=>
        {
            if(fail)throw new ApplicationException("Requested update failure.");updates++;
            var oldArea=game.Area;bool oldComplete=game.Completed;int oldRun=game.RunNumber;
            game.Update(frame.Delta,frame.Input);art.Advance(frame.Delta,game);
            if(oldArea!=game.Area)Console.WriteLine($"area:{game.Area}");
            if(game.LastInteraction!=Interaction.None)Console.WriteLine($"interaction:{game.LastInteraction}");
            if(!oldComplete&&game.Completed)Console.WriteLine("adventure:completed");
            if(oldRun!=game.RunNumber)Console.WriteLine("adventure:reset");
        },
        (canvas,frame)=>{ art.Draw(canvas,game);frames++;if(limit>0&&frames>=limit)frame.Control.RequestStop(); });
    Console.WriteLine($"outcome:{report.Outcome};frames:{frames};updates:{updates}");
    if(report.PrimaryError is not null)Console.Error.WriteLine($"primary:{report.PrimaryError.GetType().Name}:{report.PrimaryError.Message}");
    foreach(var error in report.CleanupErrors)Console.Error.WriteLine($"cleanup:{error.GetType().Name}:{error.Message}");
    return report.Outcome==ExecutionOutcome.Failed?1:0;
}
catch(Exception error){Console.Error.WriteLine($"startup:{error.GetType().Name}:{error.Message}");return 1;}
