using MicroEngine.Loop;
using MicroEngine.TwoD;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platformer.Model;
using Platformer.Checks;
namespace Platformer.Contracts;

[TestClass]
public sealed class ThirdLevelTests
{
    [TestMethod]
    public void ThirdCourseHasThreeGapsStaggeredLandingsAndNoPatrol()
    {
        var game=new LevelGame(3);
        foreach(float start in new[]{384f,768f,1200f})
            Assert.IsFalse(game.Terrain.Any(solid=>solid.Y==432&&StaticWorld.Overlaps(solid,new(start,432,96,48))));
        CollectionAssert.IsSubsetOf(
            new[]{new Rect(432,384,96,48),new Rect(768,336,96,96),new Rect(1248,384,96,48)},
            game.Terrain.ToArray());
        CollectionAssert.AreEqual(
            new[]{new Rect(456,352,24,24),new Rect(792,304,24,24),new Rect(1272,352,24,24)},
            game.CoinPositions.ToArray());
        Assert.IsNull(game.EnemyBody);
        Assert.AreEqual(new Rect(1536,412,48,20),game.Danger);
        Assert.AreEqual(new Rect(1824,336,48,96),game.Goal);
    }

    [TestMethod]
    public void WalkingWithoutJumpFallsIntoTheFirstGap()
    {
        var game=new LevelGame(3);bool crossedUnsupportedFloor=false;
        for(int i=0;i<500&&!game.Dead;i++)
        {
            game.Update(Course.Step,new(["right"],[]));
            if(game.Body.Y>392&&game.Body.X<480&&game.Body.X+game.Body.Width>384)crossedUnsupportedFloor=true;
        }
        Assert.IsTrue(crossedUnsupportedFloor);Assert.IsTrue(game.Dead);
        Assert.IsTrue(game.Body.Y>528);Assert.IsTrue(game.Body.X<576);
        Assert.IsFalse(StaticWorld.Overlaps(game.Body,game.Danger));
    }

    [TestMethod]
    public void PublicInputRouteCrossesEveryGapCollectsCoinsAndCompletes()
    {
        var game=new LevelGame(3);int updates=Course.Run(game);
        Assert.IsTrue(updates<=2000);Assert.IsTrue(game.Completed);
        Assert.AreEqual(3,game.CollectedCount);Assert.AreEqual(game.CoinCount,game.CollectedCount);
    }
}
