using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rpg.Model;
using Rpg.Checks;
namespace Rpg.Contracts;

[TestClass] public class PresentationTests
{
    private sealed class Lease(int width,int height):IImageResource
    { public int Width=>width;public int Height=>height;public int Releases;public void Dispose()=>Releases++; }
    private sealed class Canvas:IImageRegionCanvas,ILabelCanvas
    {
        public List<Lease> Leases=[];public List<Rect> Regions=[];public List<Rect> Destinations=[];public List<string> Labels=[];
        public void Clear(Rgba c) { } public void Fill(Rect r,Rgba c) { }
        public IImageResource CreateImage(PixelImage image) { var lease=new Lease(image.Width,image.Height);Leases.Add(lease);return lease; }
        public void DrawImage(IImageResource image,Rect destination) { }
        public void DrawImageRegion(IImageResource image,Rect source,Rect destination)
        {
            Assert.IsTrue(source.X>=0&&source.Y>=0&&source.Width>0&&source.Height>0&&source.X+source.Width<=image.Width&&source.Y+source.Height<=image.Height);
            Assert.IsTrue(source.X==(int)source.X&&source.Y==(int)source.Y&&source.Width==(int)source.Width&&source.Height==(int)source.Height);
            Regions.Add(source);Destinations.Add(destination);
        }
        public void Label(string text,int x,int y,int size,Rgba c)=>Labels.Add(text);
    }
    private sealed class Primitive:ICanvas { public void Clear(Rgba c) { } public void Fill(Rect r,Rgba c) { } }
    private static Dictionary<string,PixelImage> Sources()=>RpgPresentation.AssetNames.ToDictionary(name=>name,name=>
    {
        var (w,h)=name switch { "ArMM1998/character" or "Hero/character"=>(272,256),"Town/atlas" or "Dungeon/atlas"=>(192,176),"ArMM1998/Overworld"=>(640,576),"ArMM1998/Inner"=>(640,400),_=>(64,64) };
        return new PixelImage(w,h,new Rgba[w*h]);
    });
    [TestMethod] public void ModesArePureAndReuseCompleteSelectedBundleAcrossAreasAndRetry()
    {
        var art=new RpgPresentation(Sources());var game=new RpgSession();var canvas=new Canvas();
        void Advance()=>art.Advance(Course.Step,game);
        art.Draw(canvas,game);Course.VisitMeadowAndReturn(game,Advance);art.Draw(canvas,game);
        // Draw Meadow explicitly through a separate real public journey, not state injection.
        for(int i=0;game.Room==RoomExplorer.Model.Room.Courtyard&&i<700;i++)Course.Tick(game,"right",after:Advance);
        art.Draw(canvas,game);
        for(int i=0;game.Room==RoomExplorer.Model.Room.Hall&&i<700;i++)Course.Tick(game,"left",after:Advance);
        Course.Open(game,Advance);var state=(game.Body,game.Mode,game.Battle,game.SelectedAction);
        art.Draw(canvas,game);Assert.AreEqual(state,(game.Body,game.Mode,game.Battle,game.SelectedAction));
        Assert.IsTrue(canvas.Labels.Contains("Player HP 12/12"));Assert.IsTrue(canvas.Labels.Contains("Attack"));
        // RPG-ART-1 selects three original sheets, not the previous four provisional images.
        Assert.AreEqual(RpgPresentation.AssetNames.Count,canvas.Leases.Count);Course.Finish(game,BattleOutcome.Retreated,Advance);art.Draw(canvas,game);
        Course.Tick(game,edge:"jump",after:Advance);Course.Open(game,Advance);art.Draw(canvas,game);Assert.AreEqual(RpgPresentation.AssetNames.Count,canvas.Leases.Count);
        art.Dispose();art.Dispose();foreach(var lease in canvas.Leases)Assert.AreEqual(1,lease.Releases);
        Assert.ThrowsExactly<ObjectDisposedException>(()=>art.Draw(canvas,game));
    }
    [TestMethod] public void DirectionalClockAndBattleTransitionArePresentationOnly()
    {
        using var art=new RpgPresentation(Sources());var game=new RpgSession();var canvas=new Canvas();
        Course.Tick(game,"right");art.Advance(TimeSpan.FromMilliseconds(120),game);art.Draw(canvas,game);
        Assert.AreEqual(new Rect(16,32,16,32),canvas.Regions[^1]);Course.Tick(game);art.Advance(Course.Step,game);art.Draw(canvas,game);
        Assert.AreEqual(new Rect(0,32,16,32),canvas.Regions[^1]);
        Course.Open(game,()=>art.Advance(Course.Step,game));art.Draw(canvas,game);Assert.IsTrue(canvas.Labels.Contains("Training Sentinel | Turn 0"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>art.Advance(TimeSpan.FromTicks(-1),game));
    }
    [TestMethod][DataRow(BattleOutcome.Victory)][DataRow(BattleOutcome.Defeat)][DataRow(BattleOutcome.Retreated)]
    public void ResultsDisplayActualHpOutcomeAndReturnPrompt(BattleOutcome outcome)
    {
        using var art=new RpgPresentation(Sources());var game=new RpgSession();var canvas=new Canvas();Course.Open(game);Course.Finish(game,outcome);
        art.Draw(canvas,game);Assert.IsTrue(canvas.Labels.Contains(outcome.ToString()));
        Assert.IsTrue(canvas.Labels.Contains($"Player HP {game.Battle.PlayerHp}/12"));
        Assert.IsTrue(canvas.Labels.Contains($"Sentinel HP {game.Battle.OpponentHp}/10"));Assert.IsTrue(canvas.Labels.Contains("Space: return to exploration"));
    }
    [TestMethod] public void RequiredProfilesAndNullDisposedContractsAreExplicit()
    {
        using var art=new RpgPresentation(Sources());var game=new RpgSession();
        Assert.ThrowsExactly<NotSupportedException>(()=>art.Draw(new Primitive(),game));
        Assert.ThrowsExactly<ArgumentNullException>(()=>art.Draw(null!,game));Assert.ThrowsExactly<ArgumentNullException>(()=>art.Advance(Course.Step,null!));
    }
    [TestMethod] public void PackedTerrainUsesDistinctCornersEdgesAndInteriorAndCreatureChangesContext()
    {
        using var art=new RpgPresentation(Sources());var game=new RpgSession();var canvas=new Canvas();
        art.Draw(canvas,game);
        foreach(var cell in new Rect[]{new(0,16,16,16),new(16,16,16,16),new(0,32,16,16),new(16,32,16,16),new(32,48,16,16)})
            Assert.IsTrue(canvas.Regions.Contains(cell),$"Missing authored packed terrain role {cell}.");
        Assert.IsTrue(canvas.Regions.Contains(new(32,144,16,16)),"World marker must use the selected real creature.");
        Course.Open(game);canvas.Regions.Clear();canvas.Destinations.Clear();art.Draw(canvas,game);
        Assert.AreEqual(new Rect(32,144,16,16),canvas.Regions[^1]);
        Assert.AreEqual(new Rect(448,160,96,96),canvas.Destinations[^1]);
        Assert.IsTrue(canvas.Regions.Contains(new(16,32,16,16)),"Battle background must use the interior, not repeat its edge.");
        Assert.AreEqual(3,RpgPresentation.AssetNames.Count,"Approved art bundle is exactly three original sheets.");
        Assert.AreEqual(RpgPresentation.AssetNames.Count,canvas.Leases.Count,"Every selected sheet is acquired once across both contexts.");
    }
}
