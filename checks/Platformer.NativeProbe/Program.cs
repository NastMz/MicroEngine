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
        var campaign=new CampaignGame();
        (byte R,byte G,byte B) CampaignFrame(string name)
        {
            art.AdvanceCampaign(TimeSpan.Zero,campaign);
            for(int i=0;i<2;i++){window.Begin();art.DrawCampaign(window.Canvas,campaign);window.End();}
            var capture=Raylib.LoadImageFromScreen();
            try
            {
                if(!Raylib.ExportImage(capture,Path.Combine(Path.GetTempPath(),name)))throw new InvalidOperationException("Campaign capture failed.");
                var color=Raylib.GetImageColor(capture,500,450);return(color.R,color.G,color.B);
            }
            finally{Raylib.UnloadImage(capture);}
        }
        var levelOnePixel=CampaignFrame("microengine-meadow2-level1.png");
        int campaignFirst=Course.Run(campaign.Current,(delta,_)=>art.AdvanceCampaign(delta,campaign));
        if(campaign.Completed)throw new InvalidOperationException("First flag completed campaign.");
        CampaignFrame("microengine-meadow2-continue.png");
        campaign.Update(Course.Step,new(["right"],["jump"]));
        if(campaign.LevelNumber!=2 || campaign.Current.Body!=new Rect(48,392,28,40))throw new InvalidOperationException("Level continuation consumed-input mismatch.");
        var levelTwoPixel=CampaignFrame("microengine-meadow2-level2.png");
        if(levelOnePixel==levelTwoPixel)throw new InvalidOperationException("Distinct second-level terrain pixels missing.");
        float previous=float.NaN;
        for(int i=0;i<2000&&!campaign.Current.Dead;i++)
        {
            var current=campaign.Current;
            bool jump=current.Grounded&&current.Body.X==previous&&current.Body.X<1450;previous=current.Body.X;
            campaign.Update(Course.Step,new(["right"],jump?["jump"]:[]));
        }
        if(!campaign.Current.Dead || campaign.Current.Body.Y<=528 || campaign.Current.Body.X>=720)throw new InvalidOperationException("Second-level gap death not reached.");
        CampaignFrame("microengine-meadow2-retry.png");
        campaign.Update(Course.Step,new(["right"],["jump"]));
        if(campaign.LevelNumber!=2||campaign.Current.Dead||campaign.Current.CollectedCount!=0)throw new InvalidOperationException("Current-level retry failed.");
                bool raisedCaptured=false;
        int campaignSecond=Course.Run(campaign.Current,(delta,state)=>
        {
            art.AdvanceCampaign(delta,campaign);
            if(!raisedCaptured&&state.CollectedCount==2)
            {
                if(state.Body.Y>=392)throw new InvalidOperationException("Elevated coin collected from floor.");
                CampaignFrame("microengine-meadow3-raised.png");raisedCaptured=true;
            }
        });
        if(!raisedCaptured)throw new InvalidOperationException("Raised route not rendered.");
        if(!campaign.Completed)throw new InvalidOperationException("Second flag did not complete campaign.");
        CampaignFrame("microengine-meadow2-final.png");
        campaign.Update(Course.Step,new(["right"],["jump"]));
        if(campaign.LevelNumber!=1||campaign.Completed||campaign.Current.Body!=new Rect(48,392,28,40))throw new InvalidOperationException("Final campaign restart failed.");
        CampaignFrame("microengine-meadow2-restart.png");
        if(campaignFirst!=Course.Run(campaign.Current))throw new InvalidOperationException("Campaign first-level replay diverged.");
        campaign.Update(Course.Step,new([],["jump"]));
        if(campaignSecond!=Course.Run(campaign.Current)||!campaign.Completed)throw new InvalidOperationException("Campaign second-level replay diverged.");
        Console.WriteLine("platformer:campaign:two-levels:verified");
        Console.WriteLine("platformer:distinct-gap-raised-route:verified");
    }
    Console.WriteLine("platformer:closed");return 0;
}
catch(Exception error) { Console.Error.WriteLine($"platformer:failed:{error}");return 1; }
