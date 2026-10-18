using Rpg;
using Rpg.Model;
using Rpg.Checks;
using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;
try
{
    string assets=Path.Combine(AppContext.BaseDirectory,"Assets");
    var sources=RpgPresentation.AssetNames.ToDictionary(name=>name,name=>DesktopImageDecoder.Decode(File.ReadAllBytes(Path.Combine(assets,name+".png"))));
    using(var window=new DesktopPlatform(new(672,480,"Trail Duel verification",2048,32)))
    using(var art=new RpgPresentation(sources))
    {
        var game=new RpgSession();var canvas=new CountingCanvas(window);
        void Draw(string? capture=null)
        {
            for(int i=0;i<2;i++){canvas.Labels.Clear();window.Begin();art.Draw(canvas,game);window.End();}
            var image=Raylib.LoadImageFromScreen();
            try
            {
                void Pixel(int x,int y,Rgba expected)
                {
                    var actual=Raylib.GetImageColor(image,x,y);
                    if(actual.R!=expected.R||actual.G!=expected.G||actual.B!=expected.B)throw new InvalidOperationException($"UI pixel mismatch {x},{y}.");
                }
                void Glyphs(int x,int y,int width,int height)
                {
                    int count=0;for(int j=y;j<y+height;j++)for(int i=x;i<x+width;i++)
                    {var c=Raylib.GetImageColor(image,i,j);if(c.R==240&&c.G==240&&c.B==245)count++;}
                    if(count<20)throw new InvalidOperationException("Required RPG text missing from framebuffer.");
                }
                Glyphs(20,12,600,25);
                if(game.Mode==RpgMode.Exploration)
                {
                    bool camp=game.Room==RoomExplorer.Model.Room.Courtyard;
                    Pixel(camp?652:20,220,new(70,200,220));
                    if(!canvas.Labels.Any(text=>text.StartsWith(camp?"Camp |":"Meadow |")))throw new InvalidOperationException("Room UI mismatch.");
                    if(game.Body==new Rect(40,164,24,32))
                    {
                        var sheet=sources["ArMM1998/character"];int opaque=0;
                        for(int y=0;y<32;y++)for(int x=0;x<16;x++)
                        {
                            var c=sheet.Pixels[y*sheet.Width+x];if(c.A!=255)continue;
                            for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)Pixel(52+x*2+dx,228+y*2+dy,c);
                            opaque++;
                        }
                        if(opaque<25)throw new InvalidOperationException("Original avatar absent.");
                    }
                }
                else
                {
                    Pixel(34,122,game.Battle.PlayerHp>0?new(70,190,110):new(70,75,85));
                    Pixel(402,122,game.Battle.OpponentHp>0?new(70,190,110):new(70,75,85));
                    Glyphs(32,96,240,22);Glyphs(400,96,250,22);
                    if(!canvas.Labels.Contains($"Player HP {game.Battle.PlayerHp}/12")||!canvas.Labels.Contains($"Sentinel HP {game.Battle.OpponentHp}/10"))throw new InvalidOperationException("Numeric HP mismatch.");
                    if(game.Mode==RpgMode.Battle)Pixel(35,332+(int)game.SelectedAction*40,new(40,92,116));
                    else
                    {
                        Glyphs(220,324,260,32);Glyphs(128,380,420,24);
                        if(!canvas.Labels.Contains(game.Battle.Outcome.ToString())||!canvas.Labels.Contains("Space: return to exploration"))throw new InvalidOperationException("Result UI mismatch.");
                    }
                }
                if(capture is not null&&!Raylib.ExportImage(image,Path.Combine(Path.GetTempPath(),capture)))throw new InvalidOperationException("Capture failed.");
            }
            finally {Raylib.UnloadImage(image);}
        }
        foreach(var outcome in new[]{BattleOutcome.Victory,BattleOutcome.Defeat,BattleOutcome.Retreated})
        {
            game=new();art.Advance(TimeSpan.Zero,game);Draw(outcome==BattleOutcome.Victory?"microengine-rpg-camp.png":null);
            void Advance()=>art.Advance(Course.Step,game);
            if(outcome==BattleOutcome.Victory)
            {
                for(int i=0;game.Room==RoomExplorer.Model.Room.Courtyard&&i<700;i++)Course.Tick(game,"right",after:Advance);
                Draw("microengine-rpg-meadow.png");
                for(int i=0;game.Room==RoomExplorer.Model.Room.Hall&&i<700;i++)Course.Tick(game,"left",after:Advance);
            }
            Course.Open(game,Advance);var position=(game.Room,game.Body);Draw("microengine-rpg-battle.png");
            Course.Tick(game,edge:"down",after:Advance);Draw();Course.Tick(game,edge:"up",after:Advance);
            Course.Finish(game,outcome,Advance);Draw($"microengine-rpg-{outcome}.png");
            var expected=outcome switch { BattleOutcome.Victory=>new BattleSnapshot(2,0,3,outcome),BattleOutcome.Defeat=>new(0,2,4,outcome),_=>new(12,10,0,outcome) };
            if(game.Battle!=expected)throw new InvalidOperationException("Combat arithmetic differs.");
            game.Update(Course.Step,new(["right"],["jump","jump"]));Advance();Draw();
            if(game.Mode!=RpgMode.Exploration||(game.Room,game.Body)!=position)throw new InvalidOperationException("Return leaked movement or changed position.");
            if(outcome==BattleOutcome.Victory)
            { Course.Tick(game,edge:"jump",after:Advance);if(game.Mode!=RpgMode.Exploration||!game.EncounterCleared)throw new InvalidOperationException("Victory retriggered."); }
            else
            { Course.Open(game,Advance);if(game.Battle!=new BattleSnapshot(12,10,0,BattleOutcome.None)||game.EncounterNumber!=2)throw new InvalidOperationException("Retry did not reset battle.");Draw(); }
            Console.WriteLine($"rpg:{outcome}:verified");
        }
        if(canvas.Creates!=4)throw new InvalidOperationException($"RPG cache acquired {canvas.Creates}, expected four.");
        Console.WriteLine("rpg:four-images:verified");
    }
    Console.WriteLine("rpg:closed");return 0;
}
catch(Exception error){Console.Error.WriteLine($"rpg:failed:{error}");return 1;}

sealed class CountingCanvas(DesktopPlatform window):IImageRegionCanvas,ILabelCanvas
{
    private IImageRegionCanvas Inner=>(IImageRegionCanvas)window.Canvas;
    public int Creates {get;private set;}public List<string> Labels=[];
    public void Clear(Rgba c)=>Inner.Clear(c);public void Fill(Rect r,Rgba c)=>Inner.Fill(r,c);
    public IImageResource CreateImage(PixelImage image){Creates++;return Inner.CreateImage(image);}
    public void DrawImage(IImageResource image,Rect destination)=>Inner.DrawImage(image,destination);
    public void DrawImageRegion(IImageResource image,Rect source,Rect destination)=>Inner.DrawImageRegion(image,source,destination);
    public void Label(string text,int x,int y,int size,Rgba c){Labels.Add(text);((ILabelCanvas)window.Canvas).Label(text,x,y,size,c);}
}
