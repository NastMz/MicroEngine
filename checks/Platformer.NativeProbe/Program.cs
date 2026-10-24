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
        if(campaign.Completed)throw new InvalidOperationException("Second flag completed the three-level campaign.");
        campaign.Update(Course.Step,new(["right"],["jump"]));
        if(campaign.LevelNumber!=3||campaign.Current.Body!=new Rect(48,392,28,40))throw new InvalidOperationException("Third-level continuation mismatch.");
        CampaignFrame("microengine-meadow7-level3.png");
        var verifiedGaps=new bool[3];
        var expectedCoinColors=DesktopImageDecoder.Decode(File.ReadAllBytes(Path.Combine(assets,"coin_gold.png"))).Pixels
            .Where(pixel=>pixel.A==255).Select(pixel=>(pixel.R,pixel.G,pixel.B)).ToHashSet();
        var gaps=new[]{(Start:384f,CoinX:456f,CoinY:352,PlatformY:400),(Start:768f,CoinX:792f,CoinY:304,PlatformY:352),(Start:1200f,CoinX:1272f,CoinY:352,PlatformY:400)};
        void VerifyVisibleThirdGap(int index)
        {
            float camera=campaign.Current.CameraX;
            int gapX=(int)(gaps[index].Start+48-camera),floorX=(int)(gaps[index].Start-24-camera),platformX=(int)(gaps[index].CoinX-camera),coinX=(int)(gaps[index].CoinX+12-camera);
            if(gapX<1||gapX>=767||floorX<1||floorX>=767||platformX<1||platformX>=767||coinX<1||coinX>=767)return;
            if(campaign.Current.IsCollected(index))throw new InvalidOperationException($"Third-course coin {index+1} was collected before native pixel proof.");
            CampaignFrame($"microengine-meadow7-gap{index+1}.png");
            var image=Raylib.LoadImageFromScreen();
            try
            {
                var gap=Raylib.GetImageColor(image,gapX,450);var floor=Raylib.GetImageColor(image,floorX,450);
                var platform=Raylib.GetImageColor(image,platformX,gaps[index].PlatformY);
                if((gap.R,gap.G,gap.B)==(floor.R,floor.G,floor.B)||(gap.R,gap.G,gap.B)==(platform.R,platform.G,platform.B))
                    throw new InvalidOperationException($"Third-course gap/platform pixels missing for gap {index+1}.");
                int coinPixels=0;
                for(int y=gaps[index].CoinY;y<gaps[index].CoinY+24;y++)for(int x=coinX-12;x<coinX+12;x++)
                {
                    var coin=Raylib.GetImageColor(image,x,y);
                    if(expectedCoinColors.Contains((coin.R,coin.G,coin.B)))coinPixels++;
                }
                if(coinPixels<20)
                    throw new InvalidOperationException($"Third-course original coin pixel mismatch for coin {index+1}.");
                verifiedGaps[index]=true;
            }
            finally{Raylib.UnloadImage(image);}
        }
        int campaignThird=Course.Run(campaign.Current,(delta,state)=>
        {
            art.AdvanceCampaign(delta,campaign);
            for(int index=0;index<verifiedGaps.Length;index++)if(!verifiedGaps[index])VerifyVisibleThirdGap(index);
        });
        if(verifiedGaps.Any(value=>!value))throw new InvalidOperationException("All three native gap views were not captured.");
        if(!campaign.Completed)throw new InvalidOperationException("Third flag did not complete campaign.");
        CampaignFrame("microengine-meadow7-final.png");
        campaign.Update(Course.Step,new(["right"],["jump"]));
        if(campaign.LevelNumber!=1||campaign.Completed||campaign.Current.Body!=new Rect(48,392,28,40))throw new InvalidOperationException("Final campaign restart failed.");
        CampaignFrame("microengine-meadow2-restart.png");
        if(campaignFirst!=Course.Run(campaign.Current))throw new InvalidOperationException("Campaign first-level replay diverged.");
        campaign.Update(Course.Step,new([],["jump"]));
        if(campaignSecond!=Course.Run(campaign.Current)||campaign.Completed)throw new InvalidOperationException("Campaign second-level replay diverged.");
        campaign.Update(Course.Step,new([],["jump"]));
        if(campaignThird!=Course.Run(campaign.Current)||!campaign.Completed)throw new InvalidOperationException("Campaign third-level replay diverged.");
        Console.WriteLine("platformer:campaign:three-levels:verified");
        Console.WriteLine("platformer:distinct-gap-raised-route:verified");
        Console.WriteLine("platformer:third-course:three-gaps-staggered:verified");
        var enemyCampaign=new CampaignGame();Course.Run(enemyCampaign.Current);
        enemyCampaign.Update(Course.Step,new([],["jump"]));
        Course.Run(enemyCampaign.Current,anticipateEnemies:false,stopWhen:state=>state.Body.X>=960);
        for(int i=0;i<100&&!enemyCampaign.Current.Grounded;i++)enemyCampaign.Update(Course.Step,new([],[]));
        art.AdvanceCampaign(TimeSpan.Zero,enemyCampaign);
        void VerifyPatrolFrame(string key,string name)
        {
            for(int frame=0;frame<2;frame++){window.Begin();art.DrawCampaign(window.Canvas,enemyCampaign);window.End();}
            var expected=DesktopImageDecoder.Decode(File.ReadAllBytes(Path.Combine(assets,key+".png")));
            if(expected.Width!=64||expected.Height!=64)throw new InvalidOperationException("Enemy original dimensions changed.");
            var enemy=enemyCampaign.Current.EnemyBody??throw new InvalidOperationException("Missing authored enemy.");
            int left=(int)MathF.Ceiling(enemy.X-12-enemyCampaign.Current.CameraX);
            int top=(int)(enemy.Y+enemy.Height-64);
            var image=Raylib.LoadImageFromScreen();
            try
            {
                int checkedPixels=0;
                for(int y=1;y<63;y++)for(int x=1;x<63;x++)
                {
                    var pixel=expected.Pixels[y*64+x];
                    if(pixel.A!=255||expected.Pixels[y*64+x-1]!=pixel||expected.Pixels[y*64+x+1]!=pixel||expected.Pixels[(y-1)*64+x]!=pixel||expected.Pixels[(y+1)*64+x]!=pixel)continue;
                    var actual=Raylib.GetImageColor(image,left+x,top+y);
                    if(actual.R!=pixel.R||actual.G!=pixel.G||actual.B!=pixel.B)throw new InvalidOperationException("Patrol original framebuffer pixel mismatch.");
                    checkedPixels++;
                }
                if(checkedPixels<25)throw new InvalidOperationException("Insufficient enemy pixel proof.");
                if(!Raylib.ExportImage(image,Path.Combine(Path.GetTempPath(),name)))throw new InvalidOperationException("Enemy capture export failed.");
            }
            finally{Raylib.UnloadImage(image);}
        }
        VerifyPatrolFrame("slime_spike_walk_a","microengine-meadow4-patrol-a.png");
        art.AdvanceCampaign(TimeSpan.FromMilliseconds(120),enemyCampaign);
        VerifyPatrolFrame("slime_spike_walk_b","microengine-meadow4-patrol-b.png");
        for(int i=0;i<100&&!enemyCampaign.Current.Dead;i++)
        {
            enemyCampaign.Update(TimeSpan.FromMilliseconds(100),new(["right"],[]));
            art.AdvanceCampaign(TimeSpan.FromMilliseconds(100),enemyCampaign);
        }
        var hit=enemyCampaign.Current;
        if(!hit.Dead||hit.EnemyBody is not Rect contact||!MicroEngine.TwoD.StaticWorld.Overlaps(hit.Body,contact)||hit.CollectedCount!=2)
            throw new InvalidOperationException("Public maximum-delta enemy contact not proved.");
        var stoppedEnemy=hit.EnemyBody;
        enemyCampaign.Update(Course.Step,new(["right"],[]));
        if(hit.EnemyBody!=stoppedEnemy)throw new InvalidOperationException("Enemy did not freeze after contact.");
        enemyCampaign.Update(Course.Step,new(["right"],["jump"]));
        if(hit.EnemyBody!=new Rect(1152,392,40,40)||hit.Dead||hit.CollectedCount!=0)throw new InvalidOperationException("Patrol retry did not reset.");
        Course.Run(hit,(delta,_)=>art.AdvanceCampaign(delta,enemyCampaign));
        if(enemyCampaign.Completed)throw new InvalidOperationException("Level-two avoidance completed the three-level campaign.");
        enemyCampaign.Update(Course.Step,new([],["jump"]));
        if(enemyCampaign.LevelNumber!=3)throw new InvalidOperationException("Avoidance route did not continue to level three.");
        Course.Run(enemyCampaign.Current,(delta,_)=>art.AdvanceCampaign(delta,enemyCampaign));
        if(!enemyCampaign.Completed)throw new InvalidOperationException("Avoidance campaign route did not complete.");
        Console.WriteLine("platformer:patrol:poses-contact-retry-avoidance:verified");
        var session=new MeadowSession();
        int SessionFrame(string filename,bool panel,bool arrow=false)
        {
            for(int frame=0;frame<2;frame++){window.Begin();art.DrawSession(window.Canvas,session);window.End();}
            var image=Raylib.LoadImageFromScreen();
            try
            {
                var corner=Raylib.GetImageColor(image,180,124);
                if(panel?(corner.R>80||corner.G>90):(corner.R<180||corner.G<180))throw new InvalidOperationException("Session panel visibility mismatch.");
                int pixels=0;
                for(int y=198;y<220;y++)for(int x=208;x<222;x++)
                {var color=Raylib.GetImageColor(image,x,y);if(color.R>180&&color.G>180&&color.B>170)pixels++;}
                if(arrow&&pixels==0)throw new InvalidOperationException("Selected resume arrow missing.");
                if(!Raylib.ExportImage(image,Path.Combine(Path.GetTempPath(),filename)))throw new InvalidOperationException("Session capture failed.");
                return pixels;
            }
            finally{Raylib.UnloadImage(image);}
        }
        SessionFrame("microengine-meadow5-title.png",true);
        session.Update(Course.Step,new(["right"],["jump"]));art.AdvanceSession(Course.Step,session);
        if(session.Campaign.Current.Body!=new Rect(48,392,28,40)||session.AdvancedThisUpdate)throw new InvalidOperationException("Start edge leaked into gameplay.");
        Course.Run(session.Campaign.Current);session.Update(Course.Step,new([],["jump"]));
        session.Update(Course.Step,new([],["pause"]));
        var pausedState=(session.Campaign.Current.Body,session.Campaign.Current.EnemyBody,session.Campaign.Current.CollectedCount);
        for(int i=0;i<100;i++){session.Update(Course.Step,new(["right","jump"],["unknown"]));art.AdvanceSession(Course.Step,session);}
        if(pausedState!=(session.Campaign.Current.Body,session.Campaign.Current.EnemyBody,session.Campaign.Current.CollectedCount))throw new InvalidOperationException("Paused simulation advanced.");
        int selected=SessionFrame("microengine-meadow5-paused.png",true,true);
        session.Update(Course.Step,new([],["down"]));int unselected=SessionFrame("microengine-meadow5-selection.png",true);
        if(selected<=unselected)throw new InvalidOperationException("Native selection arrow did not change.");
        var oldCampaign=session.Campaign;session.Update(Course.Step,new(["right"],["jump"]));art.AdvanceSession(Course.Step,session);
        if(ReferenceEquals(oldCampaign,session.Campaign)||session.Campaign.LevelNumber!=1||session.Campaign.Current.Body!=new Rect(48,392,28,40))throw new InvalidOperationException("Menu restart did not consume input.");
        session.Update(Course.Step,new([],["pause"]));session.Update(Course.Step,new(["right"],["pause","jump"]));
        if(session.Phase!=SessionPhase.Running||session.AdvancedThisUpdate||session.Campaign.Current.Body.X!=48)throw new InvalidOperationException("Pause resume edge leaked.");
        SessionFrame("microengine-meadow5-resumed.png",false);
        Console.WriteLine("platformer:session:title-pause-selection-resume-restart:verified");
        var soundSession=new MeadowSession();var recording=new RecordingAudio();var feedback=new MeadowSoundFeedback();
        feedback.Publish(soundSession,recording);
        soundSession.Update(Course.Step,new([],["jump"]));feedback.Publish(soundSession,recording);
        if(recording.Cues.Count!=0)throw new InvalidOperationException("Title confirmation played stale gameplay audio.");
        Course.Run(soundSession.Campaign.Current,(_,_)=>feedback.Publish(soundSession,recording),update:soundSession.Update);
        if(recording.Cues.Count(c=>c=="coin")!=3||recording.Cues.Count(c=>c=="level-complete")!=1||!recording.Cues.Contains("jump"))
            throw new InvalidOperationException("Public session audio cue mapping failed.");
        soundSession.Update(Course.Step,new([],["jump"]));feedback.Publish(soundSession,recording);
        try{Course.Run(soundSession.Campaign.Current,(_,_)=>feedback.Publish(soundSession,recording),anticipateGaps:false,update:soundSession.Update);throw new InvalidOperationException("Expected public fall missing.");}
        catch(InvalidOperationException)when(soundSession.Campaign.Current.Dead){}
        if(recording.Cues.Count(c=>c=="defeat")!=1)throw new InvalidOperationException("Defeat cue was absent or repeated.");
        soundSession.Update(Course.Step,new([],["jump"]));feedback.Publish(soundSession,recording);
        soundSession.Update(Course.Step,new([],["pause"]));feedback.Publish(soundSession,recording);
        int cuesBefore=recording.Cues.Count;
        for(int i=0;i<50;i++){soundSession.Update(Course.Step,new(["jump"],[]));feedback.Publish(soundSession,recording);}
        if(recording.Cues.Count!=cuesBefore||recording.Stops<2)throw new InvalidOperationException("Paused or restarted audio leaked.");
        Console.WriteLine("platformer:audio:four-cues-pause-reset:verified");
    }
    Console.WriteLine("platformer:closed");return 0;
}
catch(Exception error) { Console.Error.WriteLine($"platformer:failed:{error}");return 1; }

sealed class RecordingAudio : MicroEngine.Audio.IOneShotAudio
{
    public List<string> Cues {get;}=[];
    public int Stops;
    public void Play(string cue)=>Cues.Add(cue);
    public void StopAll()=>Stops++;
    public void Dispose(){}
}
