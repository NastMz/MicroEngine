using MicroEngine.Loop;
using MicroEngine.TwoD;
using Adventure.Model;
namespace Adventure;

/// <summary>Game-specific art/layout composed through public reusable engine capabilities.</summary>
public sealed class AdventurePresentation : IDisposable
{
    public static IReadOnlyList<string> AssetNames { get; } = Array.AsReadOnly<string>([
        "ArMM1998/character","ArMM1998/Overworld","ArMM1998/Inner","lever_left","lever_right","key_yellow","gem_blue"]);
    private readonly ImageSet _images;
    private readonly FrameAnimation _walk = new([new(0,0,16,32),new(16,0,16,32),new(32,0,16,32),new(48,0,16,32)],TimeSpan.FromMilliseconds(120));
    private Rect _previous = new(64,168,24,24);
    private Area _area = Area.Courtyard;
    private int _row;
    private int _run;
    private bool _disposed;
    public AdventurePresentation(IReadOnlyDictionary<string,PixelImage> sources) => _images=new(sources);
    public void Advance(TimeSpan elapsed,AdventureModel game)
    {
        ArgumentNullException.ThrowIfNull(game); ObjectDisposedException.ThrowIf(_disposed,this);
        if(elapsed<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(elapsed));
        if(_run!=game.RunNumber||_area!=game.Area)
        {
            _walk.Reset();_run=game.RunNumber;_area=game.Area;_row=0;_previous=game.Body;
        }
        if(game.Moving)
        {
            float dx=game.Body.X-_previous.X,dy=game.Body.Y-_previous.Y;
            int row=dx>0?32:dx<0?96:dy<0?64:dy>0?0:_row;
            if(row!=_row)_walk.Reset();
            _row=row;
        }
        _previous=game.Body;
        if(game.Moving&&!game.Completed)_walk.Advance(elapsed);else _walk.Reset();
    }
    public void Draw(ICanvas canvas,AdventureModel game)
    {
        ArgumentNullException.ThrowIfNull(canvas);ArgumentNullException.ThrowIfNull(game);ObjectDisposedException.ThrowIf(_disposed,this);
        if(canvas is not IImageRegionCanvas images)throw new NotSupportedException("This adventure requires image-region rendering.");
        var floor=game.Area==Area.Courtyard?new Rgba(76,126,85):new Rgba(67,90,113);
        canvas.Clear(floor);
        Tile(images,game.Area==Area.Courtyard?"ArMM1998/Overworld":"ArMM1998/Inner",
            game.Area==Area.Courtyard?new(64,96,16,16):new(176,32,16,16),new(0,64,640,296));
        canvas.Fill(game.Area==Area.Courtyard?new Rect(600,144,24,72):new Rect(16,144,16,72),new(60,180,210,160));
        foreach(var solid in game.ActiveSolids)
            Tile(images,"ArMM1998/Overworld",new(352,16,16,16),solid);
        if(game.Area==Area.Courtyard)
        {
            if(!game.LeverOn)Tile(images,"ArMM1998/Inner",new(96,0,16,16),AdventureModel.Gate);
            Whole(images,game.LeverOn?"lever_right":"lever_left",AdventureModel.Lever);
        }
        else
        {
            if(!game.HasKey)Whole(images,"key_yellow",AdventureModel.Key);
            canvas.Fill(AdventureModel.Altar,new(105,90,130));Whole(images,"gem_blue",AdventureModel.Altar);
        }
        var pose=game.Moving&&!game.Completed?_walk.Current:new Rect(0,0,16,32);
        images.DrawImageRegion(_images.Get(images,"ArMM1998/character"),pose with { Y=_row },
            new(game.Body.X-4,game.Body.Y-40,32,64));
        canvas.Fill(new(0,0,640,64),new(24,28,40));
        if(canvas is ILabelCanvas labels)
        {
            labels.Label($"{game.Area} | Gate {(game.LeverOn?"open":"closed")} | Key {(game.HasKey?"acquired":"missing")}",12,10,20,new(240,240,245));
            string prompt="WASD/arrows: move | Space: interact | Esc: exit";
            if(game.Completed)prompt="Shrine restored! Space to replay";
            else if(game.Area==Area.Courtyard&&StaticWorld.Overlaps(game.Body,AdventureModel.Lever))prompt="Space: toggle the gate";
            else if(game.Area==Area.Courtyard&&!game.LeverOn&&game.Body.X>260)prompt="Gate closed: find and use the lever";
            else if(game.Area==Area.Shrine&&!game.HasKey&&StaticWorld.Overlaps(game.Body,AdventureModel.Key))prompt="Space: take the key";
            else if(game.Area==Area.Shrine&&StaticWorld.Overlaps(game.Body,AdventureModel.Altar))prompt=game.HasKey?"Space: restore the shrine":"The shrine needs the golden key";
            labels.Label(prompt,12,36,18,new(240,240,245));
        }
    }
    private void Tile(IImageRegionCanvas canvas,string key,Rect source,Rect destination)
    {
        var image=_images.Get(canvas,key);
        for(float y=destination.Y;y<destination.Y+destination.Height;y+=32)
        for(float x=destination.X;x<destination.X+destination.Width;x+=32)
        {
            float width=Math.Min(32,destination.X+destination.Width-x),height=Math.Min(32,destination.Y+destination.Height-y);
            canvas.DrawImageRegion(image,source with { Width=width/2,Height=height/2 },new(x,y,width,height));
        }
    }
    private void Whole(IImageCanvas canvas,string key,Rect destination)=>canvas.DrawImage(_images.Get(canvas,key),destination);
    public void Dispose() { _disposed=true;_images.Dispose(); }
}
