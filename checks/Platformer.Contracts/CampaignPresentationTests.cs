using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platformer.Model;
using Platformer.Checks;
namespace Platformer.Contracts;

[TestClass]
public sealed class CampaignPresentationTests
{
    private sealed class Image : IImageResource
    {
        public int Width => 902; public int Height => 902;
        public int Releases;
        public void Dispose() => Releases++;
    }
    private sealed class Canvas : IImageRegionCanvas, ILabelCanvas
    {
        public List<Image> Images { get; } = [];
        public List<Rect> Regions { get; } = [];
        public List<(Rect Source,Rect Destination)> Draws { get; } = [];
        public List<Rect> WholeImages { get; } = [];
        public List<string> Labels { get; } = [];
        public void Clear(Rgba color) { Regions.Clear(); Labels.Clear(); Draws.Clear(); WholeImages.Clear(); }
        public void Fill(Rect rectangle, Rgba color) { }
        public IImageResource CreateImage(PixelImage image) { var value=new Image(); Images.Add(value); return value; }
        public void DrawImage(IImageResource image, Rect destination) => WholeImages.Add(destination);
        public void DrawImageRegion(IImageResource image, Rect source, Rect destination) { Regions.Add(source); Draws.Add((source,destination)); }
        public void Label(string text, int x,int y,int pixelSize,Rgba color) => Labels.Add(text);
    }
    private static PlatformerPresentation Art() => new(Path.Combine(AppContext.BaseDirectory,"Assets"),
        _ => new PixelImage(902,902,new Rgba[902*902]));

    [TestMethod]
    public void CampaignHudAndSevenLeasesSurviveContinueAndFinalReplay()
    {
        var art=Art(); var canvas=new Canvas();var campaign=new CampaignGame();
        art.DrawCampaign(canvas,campaign);
        Assert.IsTrue(canvas.Labels.Any(text=>text.Contains("Level 1/2")));
        Course.Run(campaign.Current);art.DrawCampaign(canvas,campaign);
        Assert.IsTrue(canvas.Labels.Any(text=>text.Contains("Space to continue")));
        campaign.Update(Course.Step,new([], ["jump"]));art.AdvanceCampaign(Course.Step,campaign);art.DrawCampaign(canvas,campaign);
        Assert.IsTrue(canvas.Labels.Any(text=>text.Contains("Level 2/2")));
        Course.Run(campaign.Current);art.DrawCampaign(canvas,campaign);
        Assert.IsTrue(canvas.Labels.Any(text=>text.Contains("Meadow Run complete! Space to replay")));
        campaign.Update(Course.Step,new([], ["jump"]));art.DrawCampaign(canvas,campaign);
        Assert.AreEqual(7,canvas.Images.Count);
        art.Dispose();art.Dispose();foreach(var image in canvas.Images)Assert.AreEqual(1,image.Releases);
    }
    [TestMethod]
    public void NewLevelInstanceResetsAnimationEvenWithSameRunNumber()
    {
        using var art=Art();var canvas=new Canvas();var first=new LevelGame();
        first.Update(Course.Step,new(["right"], []));art.Advance(TimeSpan.Zero,first);art.Draw(canvas,first);
        var initial=canvas.Regions[^1];art.Advance(TimeSpan.FromMilliseconds(120),first);art.Draw(canvas,first);
        Assert.AreNotEqual(initial,canvas.Regions[^1]);
        var second=new LevelGame(2);second.Update(Course.Step,new(["right"], []));
        art.Advance(TimeSpan.Zero,second);art.Draw(canvas,second);
        Assert.AreEqual(initial,canvas.Regions[^1]);
        Assert.AreEqual(first.RunNumber,second.RunNumber);
    }
    [TestMethod]
    public void RendererDoesNotBridgePhysicalGapAndUsesElevatedCoinContent()
    {
        using var art=Art();var canvas=new Canvas();var second=new LevelGame(2);
        art.Draw(canvas,second);
        Assert.IsFalse(canvas.Draws.Any(draw=>draw.Destination.Y==432 && draw.Destination.X>=480 && draw.Destination.X<576));
        Assert.IsTrue(canvas.WholeImages.Contains(new Rect(864,304,24,24)));
        Assert.IsTrue(canvas.Draws.Any(draw=>draw.Destination.X==816 && draw.Destination.Y==336));
        Assert.IsFalse(second.Completed);Assert.AreEqual(0,second.CollectedCount);
    }
}