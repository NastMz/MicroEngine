using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;
using Rpg.Checks;
using Rpg.Model;
namespace Rpg.Contracts;

[TestClass] public class SessionTests
{
    private static object State(RpgSession s)=>(s.Room,s.X,s.Y,s.Mode,s.SelectedAction,s.Battle,s.EncounterNumber,s.EncounterCleared);
    [TestMethod] public void ExistingExplorationAndPortalsAreReusedWithoutAutomaticEncounter()
    {
        var s=new RpgSession();Assert.AreEqual(new Rect(40,164,24,32),s.Body);
        Course.VisitMeadowAndReturn(s);Assert.AreEqual(RpgMode.Exploration,s.Mode);Assert.AreEqual(0,s.EncounterNumber);
        var room=s.Room;Course.Tick(s);Assert.AreEqual(room,s.Room);
    }
    [TestMethod] public void FinalPositionInteractionOpensOnceWithoutAttacking()
    {
        var s=new RpgSession();for(int i=0;i<78;i++)Course.Tick(s,"right");
        Course.Tick(s,edge:"jump");Assert.AreEqual(RpgMode.Exploration,s.Mode);
        s.Update(Course.Step,new(["right"],["jump","jump"]));Assert.AreEqual(RpgMode.Battle,s.Mode);
        Assert.AreEqual(new BattleSnapshot(12,10,0,BattleOutcome.None),s.Battle);Assert.AreEqual(1,s.EncounterNumber);
    }
    [TestMethod] public void NavigationWrapOrderCaseHeldAndFirstConfirmationAreExplicit()
    {
        var s=new RpgSession();Course.Open(s);Course.Tick(s,edge:"up");Assert.AreEqual(BattleAction.Retreat,s.SelectedAction);
        Course.Tick(s,edge:"down");Assert.AreEqual(BattleAction.Attack,s.SelectedAction);
        s.Update(Course.Step,new(["jump","down"],["Jump","DOWN"]));Assert.AreEqual(0,s.Battle.Turn);
        s.Update(Course.Step,new([],["down","down","up","jump","jump","down"]));
        Assert.AreEqual(BattleAction.Guard,s.SelectedAction);Assert.AreEqual(new BattleSnapshot(11,10,1,BattleOutcome.None),s.Battle);
    }
    [TestMethod] public void ThreeAttacksWinWithoutTerminalRetaliation()
    {
        var s=new RpgSession();Course.Open(s);Course.Finish(s,BattleOutcome.Victory);
        Assert.AreEqual(new BattleSnapshot(2,0,3,BattleOutcome.Victory),s.Battle);Assert.IsTrue(s.EncounterCleared);
    }
    [TestMethod] public void HeldOrWrongCaseInteractionDoesNotOpenAtTrainer()
    {
        var s=new RpgSession();for(int i=0;i<87;i++)Course.Tick(s,"right");
        s.Update(Course.Step,new(["jump"],["Jump"]));Assert.AreEqual(RpgMode.Exploration,s.Mode);
        Course.Tick(s,edge:"jump");Assert.AreEqual(RpgMode.Battle,s.Mode);Assert.AreEqual(1,s.EncounterNumber);
    }
    [TestMethod] public void GuardOnlyReducesIncomingDamageAndDefeatIsReachable()
    {
        var s=new RpgSession();Course.Open(s);Course.Finish(s,BattleOutcome.Defeat);
        Assert.AreEqual(new BattleSnapshot(0,2,4,BattleOutcome.Defeat),s.Battle);Assert.IsFalse(s.EncounterCleared);
    }
    [TestMethod] public void RetreatHasNoDamageTurnOrRetaliation()
    {
        var s=new RpgSession();Course.Open(s);Course.Finish(s,BattleOutcome.Retreated);
        Assert.AreEqual(new BattleSnapshot(12,10,0,BattleOutcome.Retreated),s.Battle);
    }
    [TestMethod] public void BattleFreezesExplorationAndHeldConfirmDoesNothing()
    {
        var s=new RpgSession();Course.Open(s);var position=(s.Room,s.Body);var battle=s.Battle;
        for(int i=0;i<30;i++)s.Update(Course.Step,new(["right","up","jump"],[]));
        Assert.AreEqual(position,(s.Room,s.Body));Assert.AreEqual(battle,s.Battle);
    }
    [TestMethod] public void ResultFreezesUntilFreshAcknowledgementWhichConsumesMovement()
    {
        var s=new RpgSession();Course.Open(s);Course.Finish(s,BattleOutcome.Retreated);var position=(s.Room,s.Body);var result=s.Battle;
        s.Update(Course.Step,new(["right","jump"],["down","Jump"]));Assert.AreEqual(RpgMode.Result,s.Mode);Assert.AreEqual(result,s.Battle);
        s.Update(Course.Step,new(["right"],["jump","jump"]));Assert.AreEqual(RpgMode.Exploration,s.Mode);Assert.AreEqual(position,(s.Room,s.Body));
        Course.Tick(s,"right");Assert.IsTrue(s.X>position.Body.X);
    }
    [TestMethod][DataRow(BattleOutcome.Defeat)][DataRow(BattleOutcome.Retreated)]
    public void NonVictoryAllowsFreshFullHealthRetry(BattleOutcome outcome)
    {
        var s=new RpgSession();Course.Open(s);Course.Finish(s,outcome);var old=s.Battle;
        Course.Tick(s,edge:"jump");Assert.AreEqual(RpgMode.Exploration,s.Mode);
        Course.Tick(s);Course.Tick(s,edge:"jump");Assert.AreEqual(RpgMode.Battle,s.Mode);Assert.AreEqual(2,s.EncounterNumber);
        Assert.AreEqual(new BattleSnapshot(12,10,0,BattleOutcome.None),s.Battle);Assert.AreEqual(outcome,old.Outcome);
    }
    [TestMethod] public void VictoryCannotRetriggerButExplorationAndPortalsContinue()
    {
        var s=new RpgSession();Course.Open(s);Course.Finish(s,BattleOutcome.Victory);Course.Tick(s,edge:"jump");
        Course.Tick(s,edge:"jump");Assert.AreEqual(RpgMode.Exploration,s.Mode);Assert.AreEqual(1,s.EncounterNumber);
        Course.VisitMeadowAndReturn(s);Assert.AreEqual(RpgMode.Exploration,s.Mode);
    }
    [TestMethod] public void InvalidInputIsAtomicAndNullIsValidatedFirst()
    {
        var s=new RpgSession();Course.Open(s);Course.Finish(s,BattleOutcome.Defeat);var before=State(s);
        Assert.ThrowsExactly<ArgumentNullException>(()=>s.Update(TimeSpan.Zero,null!));
        foreach(var delta in new[]{TimeSpan.Zero,TimeSpan.FromTicks(-1),TimeSpan.FromMilliseconds(101)})
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>s.Update(delta,new([], ["jump"])));
        Assert.AreEqual(before,State(s));
    }
    [TestMethod] public void SessionsAreIndependentAndGuardAttackOrderIsDeterministic()
    {
        var s=new RpgSession();var other=new RpgSession();var before=State(other);Course.Open(s);Course.Command(s,BattleAction.Guard);
        for(int i=0;i<3;i++)Course.Command(s,BattleAction.Attack);
        Assert.AreEqual(new BattleSnapshot(1,0,4,BattleOutcome.Victory),s.Battle);Assert.AreEqual(before,State(other));
    }
}
