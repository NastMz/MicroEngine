using MicroEngine.Loop;
using MicroEngine.TwoD;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platformer.Model;
using Platformer.Checks;
namespace Platformer.Contracts;

[TestClass]
public sealed class DistinctLevelTests
{
    [TestMethod]
    public void SecondCourseHasPhysicalGapAndRaisedCoinInsteadOfOffsets()
    {
        var game=new LevelGame(2);
        Assert.IsFalse(game.Terrain.Any(solid=>StaticWorld.Overlaps(solid,new(480,432,96,48))));
        Assert.AreEqual(new Rect(864,304,24,24),game.CoinPositions[1]);
        Assert.AreEqual(new Rect(816,336,96,96),game.Terrain[^1]);
        Assert.IsTrue(LevelGame.Solids.Any(solid=>StaticWorld.Overlaps(solid,new(480,432,96,48))));
    }
    [TestMethod]
    public void HorizontalWalkDiesInGapNotAtDistantSpikes()
    {
        var game=new LevelGame(2);
        bool fellBetweenFloors=false;
        for(int i=0;i<500&&!game.Dead;i++)
        {
            game.Update(Course.Step,new(["right"],[]));
            if(game.Body.Y>392 && game.Body.X<576 && game.Body.X+game.Body.Width>480)fellBetweenFloors=true;
        }
        Assert.IsTrue(fellBetweenFloors);Assert.IsTrue(game.Dead);Assert.IsTrue(game.Body.Y>528);Assert.IsTrue(game.Body.X<720);
        Assert.IsFalse(StaticWorld.Overlaps(game.Body,game.Danger));Assert.AreEqual(1,game.CollectedCount);
    }
    [TestMethod]
    public void DistinctRouteRequiresGapInputAndRetainsImmutableAuthoredContent()
    {
        var unsupported=new LevelGame(2);
        Assert.ThrowsExactly<InvalidOperationException>(()=>Course.Run(unsupported,anticipateGaps:false));
        Assert.IsTrue(unsupported.Dead);Assert.IsTrue(unsupported.Body.Y>528);
        Assert.AreEqual(1,unsupported.CollectedCount);
        var supported=new LevelGame(2);Assert.IsTrue(Course.Run(supported)<=2000);
        Assert.IsTrue(supported.Completed);Assert.AreEqual(supported.CoinCount,supported.CollectedCount);
        Assert.AreSame(LevelContent.First.Terrain,LevelGame.Solids);
        Assert.AreSame(LevelContent.Second.Coins,supported.CoinPositions);
        Assert.ThrowsExactly<NotSupportedException>(()=>((IList<Rect>)LevelContent.Second.Coins)[0]=default);
    }
}