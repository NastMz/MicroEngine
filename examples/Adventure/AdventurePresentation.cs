using MicroEngine.Loop;
using MicroEngine.TwoD;
using Adventure.Model;
namespace Adventure;

/// <summary>Game-specific art/layout composed through public reusable engine capabilities.</summary>
public sealed class AdventurePresentation : IDisposable
{
    public static IReadOnlyList<string> AssetNames { get; } = Array.AsReadOnly<string>([
        "spritesheet-characters-default","terrain_grass_block_center","lever_left","lever_right","key_yellow","gem_blue"]);
    private static readonly Rect Idle = new(645,0,128,128);
    private readonly ImageSet _images;
    private readonly FrameAnimation _walk = new([new(0,129,128,128),new(129,129,128,128)],TimeSpan.FromMilliseconds(120));
    private int _run;
    private bool _disposed;
    public AdventurePresentation(IReadOnlyDictionary<string,PixelImage> sources) => _images=new(sources);
    public void Advance(TimeSpan elapsed,AdventureModel game)
    {
        ArgumentNullException.ThrowIfNull(game); ObjectDisposedException.ThrowIf(_disposed,this);
        if(elapsed<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(elapsed));
        if(_run!=game.RunNumber) { _walk.Reset();_run=game.RunNumber; }
        if(game.Moving&&!game.Completed)_walk.Advance(elapsed);else _walk.Reset();
    }
    public void Draw(ICanvas canvas,AdventureModel game)
    {
        ArgumentNullException.ThrowIfNull(canvas);ArgumentNullException.ThrowIfNull(game);ObjectDisposedException.ThrowIf(_disposed,this);
        if(canvas is not IImageRegionCanvas images)throw new NotSupportedException("This adventure requires image-region rendering.");
        var floor=game.Area==Area.Courtyard?new Rgba(76,126,85):new Rgba(67,90,113);
        canvas.Clear(floor);
        canvas.Fill(game.Area==Area.Courtyard?new Rect(600,144,24,72):new Rect(16,144,16,72),new(60,180,210,160));
        var wall=_images.Get(images,"terrain_grass_block_center");
        foreach(var solid in game.ActiveSolids)
        for(float y=solid.Y;y<solid.Y+solid.Height;y+=32)
        for(float x=solid.X;x<solid.X+solid.Width;x+=32)
        {
            float width=Math.Min(32,solid.X+solid.Width-x),height=Math.Min(32,solid.Y+solid.Height-y);
            images.DrawImageRegion(wall,new(0,0,width*2,height*2),new(x,y,width,height));
        }
        if(game.Area==Area.Courtyard)
        {
            if(!game.LeverOn)canvas.Fill(AdventureModel.Gate,new(210,145,55));
            Whole(images,game.LeverOn?"lever_right":"lever_left",AdventureModel.Lever);
        }
        else
        {
            if(!game.HasKey)Whole(images,"key_yellow",AdventureModel.Key);
            canvas.Fill(AdventureModel.Altar,new(105,90,130));Whole(images,"gem_blue",AdventureModel.Altar);
        }
        images.DrawImageRegion(_images.Get(images,"spritesheet-characters-default"),game.Moving&&!game.Completed?_walk.Current:Idle,
            new(game.Body.X-12,game.Body.Y-24,48,48));
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
    private void Whole(IImageCanvas canvas,string key,Rect destination)=>canvas.DrawImage(_images.Get(canvas,key),destination);
    public void Dispose() { _disposed=true;_images.Dispose(); }
}
