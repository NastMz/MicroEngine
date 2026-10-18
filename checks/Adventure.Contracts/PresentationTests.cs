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
        public int Width=>902;public int Height=>902;public int Releases;
        public void Dispose()=>Releases++;
    }
    private class Canvas : IImageRegionCanvas
    {
        public List<Lease> Leases=[];public List<Rect> Regions=[];
        public void Clear(Rgba c) { } public void Fill(Rect r,Rgba c) { }
        public IImageResource CreateImage(PixelImage p) { var lease=new Lease();Leases.Add(lease);return lease; }
        public void DrawImage(IImageResource image,Rect destination) { }
        public void DrawImageRegion(IImageResource image,Rect source,Rect destination)=>Regions.Add(source);
    }
    private sealed class Labels : Canvas,ILabelCanvas
    {
        public List<string> Text=[];
        public void Label(string text,int x,int y,int pixelSize,Rgba color)=>Text.Add(text);
    }
    private static Dictionary<string,PixelImage> Sources()
    {
        var image=new PixelImage(902,902,new Rgba[902*902]);
        return AdventurePresentation.AssetNames.ToDictionary(key=>key,_=>image);
    }
    [TestMethod] public void RenderingIsPureCachesAcrossAreasAndUsesOptionalLabels()
    {
        var art=new AdventurePresentation(Sources());var game=new AdventureModel();var canvas=new Labels();
        var state=(game.Body,game.Area,game.LeverOn,game.HasKey,game.Completed);
        art.Draw(canvas,game);art.Draw(canvas,game);Assert.AreEqual(3,canvas.Leases.Count);
        Assert.AreEqual(state,(game.Body,game.Area,game.LeverOn,game.HasKey,game.Completed));
        Assert.AreEqual(new Rect(645,0,128,128),canvas.Regions[^1]);Assert.IsTrue(canvas.Text.Any(text=>text.Contains("Courtyard")));
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
        Assert.AreEqual(new Rect(129,129,128,128),canvas.Regions[^1]);
        for(int i=0;i<150;i++)game.Update(Course.Step,new(["right"],[]));Assert.IsFalse(game.Moving);
        art.Advance(Course.Step,game);art.Draw(canvas,game);Assert.AreEqual(new Rect(645,0,128,128),canvas.Regions[^1]);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>art.Advance(TimeSpan.FromTicks(-1),game));
    }
}
