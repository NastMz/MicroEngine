using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platformer.Model;
using Platformer.Checks;
namespace Platformer.Contracts;

[TestClass]
public sealed class CampaignTests
{
    private static readonly InputSnapshot Next = new(["right"], ["jump"]);
    [TestMethod]
    public void ThreeLevelsCompleteAndRestartWithConsumedTransitions()
    {
        var campaign = new CampaignGame();
        int first = Course.Run(campaign.Current);
        Assert.IsFalse(campaign.Completed);
        var old = campaign.Current;
        campaign.Update(Course.Step, new(["jump"], ["Jump", "unknown"]));
        Assert.AreSame(old, campaign.Current);
        campaign.Update(Course.Step, Next);
        Assert.AreNotSame(old, campaign.Current);
        Assert.AreEqual(2, campaign.LevelNumber);
        Assert.AreEqual(new Rect(48,392,28,40), campaign.Current.Body);
        Assert.IsFalse(campaign.Completed);
        int second = Course.Run(campaign.Current);
        Assert.IsFalse(campaign.Completed);
        campaign.Update(Course.Step, Next);
        Assert.AreEqual(3, campaign.LevelNumber);
        int third = Course.Run(campaign.Current);
        Assert.IsTrue(campaign.Completed);
        var final = campaign.Current;
        campaign.Update(Course.Step, new(["jump"], []));
        Assert.AreSame(final, campaign.Current);
        campaign.Update(Course.Step, Next);
        Assert.AreEqual(1, campaign.LevelNumber);
        Assert.IsFalse(campaign.Completed);
        Assert.AreEqual(0, campaign.Current.CollectedCount);
        Assert.AreEqual(first, Course.Run(campaign.Current));
        campaign.Update(Course.Step, Next);
        Assert.AreEqual(second, Course.Run(campaign.Current));
        campaign.Update(Course.Step, Next);
        Assert.AreEqual(third, Course.Run(campaign.Current));
        Assert.IsTrue(campaign.Completed);
    }
    [TestMethod]
    public void InvalidTerminalUpdatesAreAtomicAndNullTakesPrecedence()
    {
        var campaign = new CampaignGame(); Course.Run(campaign.Current);
        var old = campaign.Current;
        Assert.ThrowsExactly<ArgumentNullException>(() => campaign.Update(TimeSpan.Zero, null!));
        foreach (var delta in new[] { TimeSpan.Zero, TimeSpan.FromTicks(-1), TimeSpan.FromMilliseconds(101) })
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => campaign.Update(delta, Next));
        Assert.AreSame(old, campaign.Current);
        Assert.AreEqual(1, campaign.LevelNumber);
        Assert.IsTrue(old.Completed);
    }
    [TestMethod]
    public void DeathRetriesOnlyTheCurrentLevel()
    {
        var campaign = new CampaignGame(); Course.Run(campaign.Current); campaign.Update(Course.Step, Next);
        var current = campaign.Current;
        float previous = float.NaN;
        for (int i=0; i<2000 && !current.Dead; i++)
        {
            bool jump = current.Grounded && current.Body.X == previous && current.Body.X < 1450;
            previous = current.Body.X;
            campaign.Update(Course.Step, new(["right"], jump ? ["jump"] : []));
        }
        Assert.IsTrue(current.Dead); Assert.IsFalse(campaign.Completed);
        campaign.Update(Course.Step, Next);
        Assert.AreSame(current, campaign.Current); Assert.AreEqual(2, campaign.LevelNumber);
        Assert.IsFalse(current.Dead); Assert.AreEqual(0, current.CollectedCount);
        Assert.AreEqual(new Rect(48,392,28,40), current.Body);
        Course.Run(current); Assert.IsFalse(campaign.Completed);
        campaign.Update(Course.Step, Next); Assert.AreEqual(3,campaign.LevelNumber);
        Course.Run(campaign.Current); Assert.IsTrue(campaign.Completed);
    }
    [TestMethod]
    public void LevelContentIsImmutableAndDefaultLevelRemainsCompatible()
    {
        var first=new LevelGame(); var second=new LevelGame(2);
        Assert.AreEqual(1,first.LevelNumber);
        Assert.AreSame(LevelGame.Solids,first.Terrain); Assert.AreSame(LevelGame.Coins,first.CoinPositions);
        Assert.AreEqual(LevelGame.Hazard,first.Danger); Assert.AreEqual(LevelGame.Flag,first.Goal);
        Assert.AreEqual(new Rect(576,432,1344,48),second.Terrain[1]);
        Assert.AreEqual(new Rect(240,400,24,24),second.CoinPositions[0]);
        Assert.AreEqual(new Rect(1536,412,48,20),second.Danger);
        Assert.ThrowsExactly<NotSupportedException>(()=>((IList<Rect>)second.Terrain)[0]=default);
        var before=(first.Body,first.CollectedCount,first.RunNumber);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>new LevelGame(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>new LevelGame(4));
        Assert.AreEqual(before,(first.Body,first.CollectedCount,first.RunNumber));
        // The original null call remains source-compatible, not an ambiguous overload.
        Assert.ThrowsExactly<ArgumentNullException>(()=>new GamePlatform(null!,null!));
        Assert.ThrowsExactly<ArgumentNullException>(()=>GamePlatform.ForCampaign(null!,new CampaignGame()));
    }
}
