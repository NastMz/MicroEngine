using Adventure;
using Adventure.Model;
using Adventure.Checks;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Adventure.Contracts;
[TestClass] public class PresentationTests
{
    private sealed class Lease : IImageResource
    {
        public int Width {get;init;} public int Height {get;init;} public int Releases;
        public void Dispose()=>Releases++;
    }
    private class Canvas : IImageRegionCanvas
    {
        public List<Lease> Leases=[];public List<Rect> Regions=[];public List<Rect> Destinations=[];
        public void Clear(Rgba c) { } public void Fill(Rect r,Rgba c) { }
        public IImageResource CreateImage(PixelImage p) { var lease=new Lease { Width=p.Width,Height=p.Height };Leases.Add(lease);return lease; }
        public void DrawImage(IImageResource image,Rect destination) { }
        public void DrawImageRegion(IImageResource image,Rect source,Rect destination)
        {
            Assert.IsTrue(source.X>=0&&source.Y>=0&&source.X+source.Width<=image.Width&&source.Y+source.Height<=image.Height);
            Assert.IsTrue(source.Width>0&&source.Height>0&&source.Width==(int)source.Width&&source.Height==(int)source.Height);
            Assert.AreEqual(source.Width*2,destination.Width);Assert.AreEqual(source.Height*2,destination.Height);
            Regions.Add(source);Destinations.Add(destination);
        }
    }
    private sealed class Labels : Canvas,ILabelCanvas
    {
        public List<string> Text=[];
        public void Label(string text,int x,int y,int pixelSize,Rgba color)=>Text.Add(text);
    }
    private static Dictionary<string,PixelImage> Sources()
    {
        return AdventurePresentation.AssetNames.ToDictionary(key=>key,key=>
        {
            var (w,h)=key switch { "ArMM1998/character"=>(272,256),"ArMM1998/Overworld"=>(640,576),"ArMM1998/Inner"=>(640,400),_=>(128,128) };
            return new PixelImage(w,h,new Rgba[w*h]);
        });
    }
    [TestMethod] public void RenderingIsPureCachesAcrossAreasAndUsesOptionalLabels()
    {
        var art=new AdventurePresentation(Sources());var game=new AdventureModel();var canvas=new Labels();
        var state=(game.Body,game.Area,game.LeverOn,game.HasKey,game.Completed);
        art.Draw(canvas,game);art.Draw(canvas,game);Assert.AreEqual(4,canvas.Leases.Count);
        Assert.AreEqual(new Rect(60,128,32,64),canvas.Destinations[^1]);
        Assert.AreEqual(state,(game.Body,game.Area,game.LeverOn,game.HasKey,game.Completed));
        Assert.AreEqual(new Rect(0,0,16,32),canvas.Regions[^1]);Assert.IsTrue(canvas.Text.Any(text=>text.Contains("Courtyard")));
        var driver=new Course.Driver(game,(delta,model)=>art.Advance(delta,model));driver.OpenAndEnter();art.Draw(canvas,game);
        Assert.IsTrue(canvas.Text.Any(text=>text.Contains("Shrine")));int acquired=canvas.Leases.Count;
        driver.GetKey();driver.ReachAltar();driver.Interact();art.Draw(canvas,game);Assert.AreEqual(acquired,canvas.Leases.Count);
        Assert.IsTrue(canvas.Text.Contains("Shrine restored! Space to replay"));
        art.Dispose();art.Dispose();foreach(var lease in canvas.Leases)Assert.AreEqual(1,lease.Releases);
        Assert.ThrowsExactly<ObjectDisposedException>(()=>art.Draw(canvas,game));
    }
    [TestMethod] public void EnginePlaybackUsesActualMotionAndResetsWithoutLabels()
    {
        using var art=new AdventurePresentation(Sources());var game=new AdventureModel();var canvas=new Canvas();
        game.Update(Course.Step,new(["right"],[]));art.Advance(TimeSpan.FromMilliseconds(120),game);art.Draw(canvas,game);
        Assert.AreEqual(new Rect(16,32,16,32),canvas.Regions[^1]);
        for(int i=0;i<150;i++)game.Update(Course.Step,new(["right"],[]));Assert.IsFalse(game.Moving);
        art.Advance(Course.Step,game);art.Draw(canvas,game);Assert.AreEqual(new Rect(0,32,16,32),canvas.Regions[^1]);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>art.Advance(TimeSpan.FromTicks(-1),game));
    }
    [TestMethod] public void AllDirectionsUseOriginalSheetRowsAndReplayResetsFacing()
    {
        using var art=new AdventurePresentation(Sources());var game=new AdventureModel();var canvas=new Canvas();
        foreach(var (action,row) in new[]{("right",32),("up",64),("left",96),("down",0)})
        {
            game.Update(Course.Step,new([action],[]));art.Advance(TimeSpan.FromMilliseconds(120),game);art.Draw(canvas,game);
            Assert.AreEqual(row,canvas.Regions[^1].Y);Assert.AreEqual(16f,canvas.Regions[^1].Width);
            game.Update(Course.Step,new([],[]));art.Advance(Course.Step,game);art.Draw(canvas,game);
            Assert.AreEqual(new Rect(0,row,16,32),canvas.Regions[^1]);
        }
        Course.Run(game,(delta,model)=>art.Advance(delta,model));
        game.Update(Course.Step,new(["left"],["jump"]));art.Advance(Course.Step,game);art.Draw(canvas,game);
        Assert.AreEqual(new Rect(0,0,16,32),canvas.Regions[^1]);
    }
    [TestMethod] public void RoomsHaveDistinctGroundRoutesAndLayeredArchitecturalLandmarks()
    {
        using var art=new AdventurePresentation(Sources());var game=new AdventureModel();var canvas=new Canvas();
        var initial=(game.Body,game.Area,game.LeverOn,game.HasKey,game.Completed);
        art.Draw(canvas,game);
        Assert.IsTrue(canvas.Regions.Distinct().Count()>=8,"Courtyard needs varied ground, route and boundary content, not a single repeated tile.");
        Assert.IsTrue(canvas.Regions.Where(r=>r.Width==16&&r.Height==16).Distinct().Count()>=4,"Clipped edge sizes do not count as terrain variety.");
        Assert.AreEqual(initial,(game.Body,game.Area,game.LeverOn,game.HasKey,game.Completed));
        var driver=new Course.Driver(game);driver.OpenAndEnter();canvas.Regions.Clear();canvas.Destinations.Clear();
        art.Draw(canvas,game);
        Assert.IsTrue(canvas.Regions.Distinct().Count()>=8,"Shrine needs its own floor, inlays and architectural treatment.");
        Assert.IsTrue(canvas.Regions.Where(r=>r.Width==16&&r.Height==16).Distinct().Count()>=4,"Shrine floor variety must use distinct complete tiles.");
        Assert.IsTrue(canvas.Destinations.Any(r=>r.X==288&&r.Y==128&&r.Width==32&&r.Height==128),"Pillar artwork must align with the existing solid, not add a new obstacle.");
        Assert.AreEqual(new Rect(game.Body.X-4,game.Body.Y-40,32,64),canvas.Destinations[^1],"Player remains above scenery.");
    }
}
