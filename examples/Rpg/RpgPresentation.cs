using MicroEngine.Loop;
using MicroEngine.TwoD;
using Rpg.Model;
namespace Rpg;

public sealed class RpgPresentation : IDisposable
{
    public static IReadOnlyList<string> AssetNames {get;}=Array.AsReadOnly<string>([
        "ArMM1998/character","ArMM1998/Overworld","ArMM1998/Inner","gem_blue"]);
    private readonly ImageSet _images;
    private readonly FrameAnimation _walk=new([new(0,0,16,32),new(16,0,16,32),new(32,0,16,32),new(48,0,16,32)],TimeSpan.FromMilliseconds(120));
    private Rect _previous=new(40,164,24,32);
    private RoomExplorer.Model.Room _room;
    private RpgMode _mode;
    private int _encounter,_row;
    private bool _moving,_disposed;
    public RpgPresentation(IReadOnlyDictionary<string,PixelImage> sources)=>_images=new(sources);
    public void Advance(TimeSpan elapsed,RpgSession game)
    {
        ArgumentNullException.ThrowIfNull(game);ObjectDisposedException.ThrowIf(_disposed,this);
        if(elapsed<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(elapsed));
        bool changed=_room!=game.Room||_mode!=game.Mode||_encounter!=game.EncounterNumber;
        _moving=!changed&&game.Mode==RpgMode.Exploration&&game.Body!=_previous;
        if(changed) { _row=0;_walk.Reset(); }
        if(_moving)
        {
            float dx=game.X-_previous.X,dy=game.Y-_previous.Y;
            int row=dx>0?32:dx<0?96:dy<0?64:0;
            if(row!=_row)_walk.Reset();_row=row;_walk.Advance(elapsed);
        }
        else _walk.Reset();
        _previous=game.Body;_room=game.Room;_mode=game.Mode;_encounter=game.EncounterNumber;
    }
    public void Draw(ICanvas canvas,RpgSession game)
    {
        ArgumentNullException.ThrowIfNull(canvas);ArgumentNullException.ThrowIfNull(game);ObjectDisposedException.ThrowIf(_disposed,this);
        if(canvas is not IImageRegionCanvas images||canvas is not ILabelCanvas labels)
            throw new NotSupportedException("Trail Duel requires image regions and labels.");
        canvas.Clear(new(24,32,48));
        if(game.Mode==RpgMode.Exploration)Explore(images,labels,game);else Encounter(images,labels,game);
    }
    private void Explore(IImageRegionCanvas canvas,ILabelCanvas labels,RpgSession game)
    {
        bool camp=game.Room==RoomExplorer.Model.Room.Courtyard;
        canvas.Fill(new(8,88,656,376),new(110,120,130));
        canvas.Fill(new(16,96,640,360),camp?new(66,118,80):new(91,107,119));
        Region(canvas,camp?"ArMM1998/Overworld":"ArMM1998/Inner",camp?new(0,48,48,48):new(0,112,48,48),new(464,320,96,96));
        canvas.Fill(camp?new(648,216,8,120):new(16,216,8,120),new(70,200,220));
        labels.Label(camp?"Meadow >":"< Camp",camp?548:24,196,18,new(240,240,245));
        if(camp)
        {
            canvas.Fill(new(316,246,40,60),game.EncounterCleared?new(70,180,110):new(200,160,60));
            canvas.DrawImage(_images.Get(canvas,"gem_blue"),new(320,250,32,32));
        }
        var pose=_moving?_walk.Current:new Rect(0,0,16,32);
        Region(canvas,"ArMM1998/character",pose with { Y=_row },new(12+game.X,64+game.Y,32,64));
        labels.Label($"{(camp?"Camp":"Meadow")} | Trainer {(game.EncounterCleared?"cleared":"ready")}",20,12,22,new(240,240,245));
        labels.Label(game.EncounterCleared?"Training complete - both rooms remain open":"Move: WASD/arrows | Space: challenge | Esc: exit",20,44,18,new(240,240,245));
    }
    private void Encounter(IImageRegionCanvas canvas,ILabelCanvas labels,RpgSession game)
    {
        canvas.Fill(new(16,80,640,224),new(44,60,75));
        Region(canvas,"ArMM1998/character",new(0,64,16,32),new(96,156,64,128));
        canvas.DrawImage(_images.Get(canvas,"gem_blue"),new(448,160,96,96));
        labels.Label($"Training Sentinel | Turn {game.Battle.Turn}",20,12,22,new(240,240,245));
        labels.Label($"Player HP {game.Battle.PlayerHp}/12",32,96,18,new(240,240,245));
        labels.Label($"Sentinel HP {game.Battle.OpponentHp}/10",400,96,18,new(240,240,245));
        Bar(canvas,32,game.Battle.PlayerHp,12);Bar(canvas,400,game.Battle.OpponentHp,10);
        if(game.Mode==RpgMode.Result)
        {
            labels.Label(game.Battle.Outcome.ToString(),220,324,28,new(240,240,245));
            labels.Label("Space: return to exploration",128,380,22,new(240,240,245));
        }
        else
        {
            for(int i=0;i<3;i++)
            {
                int y=328+i*40;
                canvas.Fill(new(32,y,608,32),(int)game.SelectedAction==i?new(40,92,116):new(24,32,48));
                labels.Label(((BattleAction)i).ToString(),48,y+5,20,new(240,240,245));
            }
            labels.Label("Up/down: select | Space: confirm | Esc: exit",32,454,16,new(240,240,245));
        }
    }
    private static void Bar(ICanvas canvas,int x,int hp,int maximum)
    {
        canvas.Fill(new(x,120,160,12),new(70,75,85));
        if(hp>0)canvas.Fill(new(x,120,160f*hp/maximum,12),new(70,190,110));
    }
    private void Region(IImageRegionCanvas canvas,string key,Rect source,Rect destination)
        =>canvas.DrawImageRegion(_images.Get(canvas,key),source,destination);
    public void Dispose() { _disposed=true;_images.Dispose(); }
}
