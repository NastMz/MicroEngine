using MicroEngine.Desktop;
using MicroEngine.Loop;
using Platformer;
using Platformer.Model;
using Platformer.Checks;
using Raylib_cs;
try
{
    string assets = Path.Combine(AppContext.BaseDirectory,"Assets");
    var source = DesktopImageDecoder.Decode(File.ReadAllBytes(Path.Combine(assets,"spritesheet-characters-default.png")));
    if(source.Width != 902 || source.Height != 902 || !source.Pixels.Any(pixel=>pixel.A==0)) throw new InvalidOperationException("Real atlas/alpha not decoded.");
    byte[] corrupt = File.ReadAllBytes(Path.Combine(assets,"coin_gold.png"))[..40];
    try { DesktopImageDecoder.Decode(corrupt); throw new InvalidOperationException("Truncated PNG accepted."); } catch(InvalidDataException) { }
    using (var limited = new DesktopPlatform(new(768,480,"Default image budget")))
    {
        limited.Begin();
        try { ((IImageCanvas)limited.Canvas).CreateImage(source); throw new InvalidOperationException("Default budget was bypassed."); }
        catch(NotSupportedException) { }
        limited.End();
    }
    using var art = new PlatformerPresentation(assets,DesktopImageDecoder.Decode);
    using(var window = new DesktopPlatform(new(768,480,"Meadow Run verification",2048,32)))
    {
        var game = new LevelGame(); int rendered=0;
        void Draw(bool spawn)
        {
            for(int frame=0;frame<2;frame++) { window.Begin(); art.Draw(window.Canvas,game); window.End(); }
            var image=Raylib.LoadImageFromScreen();
            try
            {
                var colors=new HashSet<(byte,byte,byte)>();
                for(int y=100;y<480;y+=8) for(int x=0;x<768;x+=8) { var c=Raylib.GetImageColor(image,x,y); colors.Add((c.R,c.G,c.B)); }
                if(colors.Count<30) throw new InvalidOperationException("Real artwork pixel diversity missing.");
                if(spawn)
                {
                    var expected=source.Pixels[80*902+645+64]; var actual=Raylib.GetImageColor(image,62,408);
                    if(expected.A!=255 || actual.R!=expected.R || actual.G!=expected.G || actual.B!=expected.B)
                        throw new InvalidOperationException($"Atlas torso mismatch: expected{expected}, got{actual.R}/{actual.G}/{actual.B}.");
                    if (!Raylib.ExportImage(image,Path.Combine(Path.GetTempPath(),"microengine-kpf1-start.png"))) throw new InvalidOperationException("Capture export failed.");
                }
            }
            finally { Raylib.UnloadImage(image); }
        }
        Draw(true);
        int first=Course.Run(game,(delta,state)=>{ art.Advance(delta,state); if(++rendered%120==0) Draw(false); });
        Draw(false);
        game.Update(Course.Step,new(["right"],["jump"])); art.Advance(Course.Step,game);
        if(game.Body != new Rect(48,392,28,40) || game.CollectedCount!=0) throw new InvalidOperationException("Replay reset mismatch.");
        Draw(true);
        int second=Course.Run(game,(delta,state)=>art.Advance(delta,state));
        if(first!=second || !game.Completed || game.CollectedCount!=3) throw new InvalidOperationException("Public replay diverged.");
        Console.WriteLine($"platformer:course:{first}:verified"); Console.WriteLine("platformer:reset:verified");
    }
    Console.WriteLine("platformer:closed");return 0;
}
catch(Exception error) { Console.Error.WriteLine($"platformer:failed:{error}");return 1; }
