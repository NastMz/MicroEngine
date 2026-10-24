using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platformer.Model;
using Platformer.Checks;
namespace Platformer.Contracts;

[TestClass]
public sealed class SessionTests
{
    private static void Tick(MeadowSession session, string[]? edges=null, string[]? held=null) => session.Update(Course.Step,new(held??[],edges??[]));
    private static MeadowSession SecondLevel()
    {
        var session=new MeadowSession();Tick(session,["jump"]);Course.Run(session.Campaign.Current);Tick(session,["jump"]);
        Assert.AreEqual(2,session.Campaign.LevelNumber);return session;
    }
    [TestMethod]
    public void TitleStartsOnlyOnFreshEdgeAndConsumesMovementAndJump()
    {
        var session=new MeadowSession();Tick(session,["pause"],["jump","right"]);
        Assert.AreEqual(SessionPhase.Title,session.Phase);Assert.IsFalse(session.AdvancedThisUpdate);
        Tick(session,["jump"],["right"]);Assert.AreEqual(SessionPhase.Running,session.Phase);
        Assert.AreEqual(new Rect(48,392,28,40),session.Campaign.Current.Body);Assert.IsFalse(session.AdvancedThisUpdate);
        Tick(session,held:["right"]);Assert.IsTrue(session.AdvancedThisUpdate);Assert.IsTrue(session.Campaign.Current.Body.X>48);
    }
    [TestMethod]
    public void PauseFreezesSimulationAndResumeConsumesAllInput()
    {
        var session=SecondLevel();Tick(session,["pause","jump"],["right"]);
        var before=(session.Campaign.Current.Body,session.Campaign.Current.EnemyBody,session.Campaign.Current.CollectedCount,session.Campaign.LevelNumber);
        for(int i=0;i<100;i++){Tick(session,["Jump","Pause"],["right","jump"]);Assert.IsFalse(session.AdvancedThisUpdate);}
        Assert.AreEqual(before,(session.Campaign.Current.Body,session.Campaign.Current.EnemyBody,session.Campaign.Current.CollectedCount,session.Campaign.LevelNumber));
        Tick(session,["pause","jump"],["right"]);Assert.AreEqual(SessionPhase.Running,session.Phase);Assert.IsFalse(session.AdvancedThisUpdate);
        Assert.AreEqual(before.Item1,session.Campaign.Current.Body);
        Tick(session);Assert.IsTrue(session.AdvancedThisUpdate);Assert.IsTrue(session.Campaign.Current.Grounded);
    }
    [TestMethod]
    public void PausedMenuTogglesOnceAndFirstConfirmationConsumesRemainingEdges()
    {
        var session=SecondLevel();var previous=session.Campaign;Tick(session,["pause"]);
        Tick(session,["up","down","jump","jump"]);
        Assert.AreEqual(SessionPhase.Running,session.Phase);Assert.AreNotSame(previous,session.Campaign);
        Assert.AreEqual(1,session.Campaign.LevelNumber);Assert.AreEqual(0,session.Selection);Assert.IsFalse(session.AdvancedThisUpdate);
        previous=session.Campaign;Tick(session,["pause"]);Tick(session,["jump","down"]);
        Assert.AreSame(previous,session.Campaign);Assert.AreEqual(0,session.Selection);Assert.AreEqual(SessionPhase.Running,session.Phase);
    }
    [TestMethod]
    public void RestartHasPriorityFromTitlePauseDeathAndFinalCompletion()
    {
        var session=new MeadowSession();Tick(session,["restart","jump"]);Assert.AreEqual(SessionPhase.Running,session.Phase);
        Course.Run(session.Campaign.Current);Tick(session,["jump"]);
        Assert.ThrowsExactly<InvalidOperationException>(()=>Course.Run(session.Campaign.Current,anticipateGaps:false));
        var dead=session.Campaign;Tick(session,["pause","jump","restart"]);
        Assert.AreNotSame(dead,session.Campaign);Assert.AreEqual(1,session.Campaign.LevelNumber);Assert.IsFalse(session.AdvancedThisUpdate);
        Course.Run(session.Campaign.Current);Tick(session,["jump"]);Course.Run(session.Campaign.Current);
        Assert.IsFalse(session.Campaign.Completed);Tick(session,["jump"]);Course.Run(session.Campaign.Current);
        Assert.IsTrue(session.Campaign.Completed);Tick(session,["pause"]);var complete=session.Campaign;
        Tick(session,["restart"]);Assert.AreNotSame(complete,session.Campaign);Assert.IsFalse(session.Campaign.Completed);
        Assert.AreEqual(new Rect(48,392,28,40),session.Campaign.Current.Body);
    }
    [TestMethod]
    public void InvalidUpdatesAreAtomicAndLegacyCampaignIgnoresNewActions()
    {
        var session=new MeadowSession();Tick(session,["jump"]);Tick(session);var old=(session.Phase,session.Selection,session.AdvancedThisUpdate,session.Campaign);
        Assert.ThrowsExactly<ArgumentNullException>(()=>session.Update(TimeSpan.Zero,null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>session.Update(TimeSpan.FromMilliseconds(101),new([],["restart"])));
        Assert.AreEqual(old,(session.Phase,session.Selection,session.AdvancedThisUpdate,session.Campaign));
        var legacy=new CampaignGame();var body=legacy.Current.Body;legacy.Update(Course.Step,new([],["pause","restart"]));Assert.AreEqual(body,legacy.Current.Body);
    }
    [TestMethod]
    public void PauseClearsBufferedLandingIntentRatherThanReplayingItOnResume()
    {
        var session=new MeadowSession();Tick(session,["jump"]);Tick(session,["jump"]);var game=session.Campaign.Current;float previous=game.Body.Y;
        for(int i=0;i<100;i++){Tick(session);if(game.Body.Y>365&&game.Body.Y>previous)break;previous=game.Body.Y;}
        Tick(session,["jump"]);float low=game.Body.Y;Tick(session,["pause"]);Tick(session,["pause"]);
        for(int i=0;i<20;i++){Tick(session);Assert.IsTrue(game.Body.Y>=low);}
        Assert.IsTrue(game.Grounded);
    }
    private sealed class InactivePlatform : IFramePlatform
    {
        public int Begins,Ends,Closes;
        public PlatformFrame Poll()=>new(Course.Step,new([],[]),Active:false);
        public ICanvas Canvas=>throw new NotSupportedException();
        public void Begin()=>Begins++;public void End()=>Ends++;public void Dispose()=>Closes++;
    }
    [TestMethod]
    public void SessionWrapperClearsTheReplacementCampaignBufferWithoutAutoPause()
    {
        var session=new MeadowSession();Tick(session,["restart"]);var inner=new InactivePlatform();
        var wrapper=GamePlatform.ForSession(inner,session);Tick(session,["restart"]);Tick(session,["jump"]);
        var game=session.Campaign.Current;float previous=game.Body.Y;
        for(int i=0;i<100;i++){Tick(session);if(game.Body.Y>365&&game.Body.Y>previous)break;previous=game.Body.Y;}
        Tick(session,["jump"]);float low=game.Body.Y;Assert.IsFalse(wrapper.Poll().Active);
        for(int i=0;i<20;i++){Tick(session);Assert.IsTrue(game.Body.Y>=low);}
        Assert.AreEqual(SessionPhase.Running,session.Phase);Assert.IsTrue(game.Grounded);
        wrapper.Begin();wrapper.End();wrapper.Dispose();
        Assert.AreEqual((1,1,1),(inner.Begins,inner.Ends,inner.Closes));
        Assert.ThrowsExactly<ArgumentNullException>(()=>GamePlatform.ForSession(null!,session));
        Assert.ThrowsExactly<ArgumentNullException>(()=>GamePlatform.ForSession(inner,null!));
    }
}
