using Adventure;
using Adventure.Model;
using Adventure.Checks;
using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;
try
{
    string assets=Path.Combine(AppContext.BaseDirectory,"Assets");
    var sources=AdventurePresentation.AssetNames.ToDictionary(name=>name,name=>DesktopImageDecoder.Decode(File.ReadAllBytes(Path.Combine(assets,name+".png"))));
    using var art=new AdventurePresentation(sources);
    using(var window=new DesktopPlatform(new(640,360,"Courtyard Key verification",2048,32)))
    {
        var game=new AdventureModel();var canvas=new CountingCanvas(window);
        void Draw(string? capture=null)
        {
            for(int i=0;i<2;i++){window.Begin();art.Draw(canvas,game);window.End();}
            var image=Raylib.LoadImageFromScreen();
            try
            {
                var colors=new HashSet<(byte,byte,byte)>();int glyphs=0;
                for(int y=64;y<360;y+=4)for(int x=0;x<640;x+=4){var c=Raylib.GetImageColor(image,x,y);colors.Add((c.R,c.G,c.B));}
                if(colors.Count<25)throw new InvalidOperationException("Adventure artwork not rendered.");
                for(int y=8;y<58;y++)for(int x=12;x<620;x++) {var c=Raylib.GetImageColor(image,x,y);if(c.R==240&&c.G==240&&c.B==245)glyphs++;}
                if(glyphs<30)throw new InvalidOperationException("Adventure HUD missing.");
                if(game.Area==Area.Courtyard)
                {
                    var actual=Raylib.GetImageColor(image,324,180);var expected=game.LeverOn?new Rgba(76,126,85):new Rgba(210,145,55);
                    if(actual.R!=expected.R||actual.G!=expected.G||actual.B!=expected.B)throw new InvalidOperationException("Gate rendering/topology mismatch.");
                }
                if(capture is not null&&!Raylib.ExportImage(image,Path.Combine(Path.GetTempPath(),capture)))throw new InvalidOperationException("Adventure capture export failed.");
            }
            finally {Raylib.UnloadImage(image);}
        }
        Draw("microengine-adventure-start.png");
        var driver=new Course.Driver(game,(delta,state)=>art.Advance(delta,state));
        driver.MoveTo(212,168);driver.Interact();Draw();driver.EnterShrine();Draw("microengine-adventure-shrine.png");
        driver.GetKey();driver.ReachAltar();driver.Interact();Draw();
        if(!game.Completed||!game.HasKey||!game.LeverOn)throw new InvalidOperationException("Adventure route failed.");
        int first=driver.Steps;game.Update(Course.Step,new(["right"],["jump"]));art.Advance(Course.Step,game);
        if(game.Body!=new Rect(64,168,24,24)||game.Area!=Area.Courtyard||game.HasKey||game.LeverOn)throw new InvalidOperationException("Reset input leaked.");
        Draw();int second=Course.Run(game,(delta,state)=>art.Advance(delta,state));Draw();
        if(first!=second||canvas.Creates!=6)throw new InvalidOperationException($"Adventure replay/cache mismatch {first}/{second}/{canvas.Creates}.");
        Console.WriteLine($"adventure:course:{first}:verified");Console.WriteLine("adventure:reuse:6:verified");Console.WriteLine("adventure:reset:verified");
    }
    Console.WriteLine("adventure:closed");return 0;
}
catch(Exception error){Console.Error.WriteLine($"adventure:failed:{error}");return 1;}

sealed class CountingCanvas(DesktopPlatform window):IImageRegionCanvas,ILabelCanvas
{
    public int Creates {get;private set;}
    private IImageRegionCanvas Inner=>(IImageRegionCanvas)window.Canvas;
    public void Clear(Rgba color)=>Inner.Clear(color);
    public void Fill(Rect rectangle,Rgba color)=>Inner.Fill(rectangle,color);
    public IImageResource CreateImage(PixelImage image){Creates++;return Inner.CreateImage(image);}
    public void DrawImage(IImageResource image,Rect destination)=>Inner.DrawImage(image,destination);
    public void DrawImageRegion(IImageResource image,Rect source,Rect destination)=>Inner.DrawImageRegion(image,source,destination);
    public void Label(string text,int x,int y,int pixelSize,Rgba color)=>((ILabelCanvas)window.Canvas).Label(text,x,y,pixelSize,color);
}
