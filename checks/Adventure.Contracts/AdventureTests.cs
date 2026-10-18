using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroEngine.Loop;
using MicroEngine.TwoD;
using Adventure.Model;
using Adventure.Checks;
namespace Adventure.Contracts;
[TestClass] public class AdventureTests
{
    [TestMethod] public void FullRouteCompletesAndFreshReplayConsumesMovement()
    {
        var game=new AdventureModel(); int first=Course.Run(game);
        Assert.AreEqual(Interaction.Altar,game.LastInteraction); var end=game.Body;
        game.Update(Course.Step,new(["left","jump"],[])); Assert.AreEqual(end,game.Body); Assert.IsTrue(game.Completed);
        game.Update(Course.Step,new(["right"],["jump","jump"]));
        Assert.AreEqual(new Rect(64,168,24,24),game.Body); Assert.AreEqual(Area.Courtyard,game.Area);
        Assert.IsFalse(game.Completed||game.HasKey||game.LeverOn||game.Moving); Assert.AreEqual(1,game.RunNumber);
        Assert.AreEqual(first,Course.Run(game));
    }
    [TestMethod] public void GateBlocksUntilExplicitLeverEdgeAndOldSolidViewRemainsImmutable()
    {
        var game=new AdventureModel(); var closed=game.ActiveSolids;
        for(int i=0;i<150;i++)game.Update(Course.Step,new(["right"],[]));
        Assert.AreEqual(296f,game.Body.X); Assert.IsFalse(game.Moving||game.LeverOn);
        var driver=new Course.Driver(game);driver.MoveTo(212,168); driver.Tick([],["jump","jump"]);
        Assert.IsTrue(game.LeverOn); Assert.AreEqual(Interaction.Lever,game.LastInteraction);
        Assert.IsTrue(closed.Contains(AdventureModel.Gate)); Assert.IsFalse(game.ActiveSolids.Contains(AdventureModel.Gate));
        Assert.ThrowsExactly<NotSupportedException>(()=>((IList<Rect>)closed)[0]=default);
        driver.Interact(); Assert.IsFalse(game.LeverOn); Assert.IsTrue(game.ActiveSolids.Contains(AdventureModel.Gate));
        Assert.IsFalse(StaticWorld.Overlaps(game.Body,AdventureModel.Gate));
    }
    [TestMethod] public void CardinalDiagonalAndOpposingMovementKeepGameSelectedSpeed()
    {
        var right=new AdventureModel();var diagonal=new AdventureModel();var opposing=new AdventureModel();
        right.Update(TimeSpan.FromMilliseconds(100),new(["right"],[]));
        diagonal.Update(TimeSpan.FromMilliseconds(100),new(["right","down"],[]));
        opposing.Update(TimeSpan.FromMilliseconds(100),new(["right","left","up","down"],[]));
        Assert.AreEqual(15f,right.Body.X-64); float dx=diagonal.Body.X-64,dy=diagonal.Body.Y-168;
        Assert.AreEqual(15f,MathF.Sqrt(dx*dx+dy*dy),0.001f); Assert.AreEqual(new Rect(64,168,24,24),opposing.Body);
        Assert.IsFalse(opposing.Moving);
    }
    [TestMethod] public void StrictEdgeHeldWrongCaseAndUnrelatedActionsDoNotInteract()
    {
        var game=new AdventureModel();
        for(int i=0;i<8;i++)game.Update(TimeSpan.FromMilliseconds(100),new(["right"],[]));
        Assert.AreEqual(184f,game.Body.X); game.Update(Course.Step,new([],["jump"])); Assert.IsFalse(game.LeverOn);
        game.Update(Course.Step,new(["right"],[]));
        game.Update(Course.Step,new(["jump"],["Jump","attack"])); Assert.IsFalse(game.LeverOn);
        game.Update(Course.Step,new([],["jump","jump"]));Assert.IsTrue(game.LeverOn);
    }
    [TestMethod] public void MissingKeyCannotCompleteAndRoomTransitionCannotInteractOrBounce()
    {
        var game=new AdventureModel();var driver=new Course.Driver(game);driver.OpenAndEnter();
        var spawn=game.Body;driver.Interact();Assert.AreEqual(Area.Shrine,game.Area);Assert.AreEqual(spawn,game.Body);Assert.IsFalse(game.HasKey);
        driver.ReachAltar(); driver.Interact();Assert.IsFalse(game.Completed);Assert.AreEqual(Interaction.None,game.LastInteraction);
    }
    [TestMethod] public void KeyAndLeverPersistAcrossVisitsAndSafeClosingReopening()
    {
        var game=new AdventureModel();var driver=new Course.Driver(game);driver.OpenAndEnter();driver.GetKey();
        driver.MoveTo(116,168);driver.ReturnCourtyard(); Assert.IsTrue(game.HasKey&&game.LeverOn);
        driver.MoveTo(212,168);driver.Interact();Assert.IsTrue(game.HasKey);Assert.IsFalse(game.LeverOn);
        driver.Interact();driver.EnterShrine();driver.ReachAltar();driver.Interact();Assert.IsTrue(game.Completed);
    }
    [TestMethod] public void InvalidCallsAreAtomicIncludingLastInteraction()
    {
        var game=new AdventureModel();var driver=new Course.Driver(game);driver.MoveTo(212,168);driver.Interact();
        var state=(game.Body,game.Area,game.LeverOn,game.HasKey,game.Completed,game.Moving,game.RunNumber,game.LastInteraction);
        Assert.ThrowsExactly<ArgumentNullException>(()=>game.Update(Course.Step,null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>game.Update(TimeSpan.Zero,new([],[])));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>game.Update(TimeSpan.FromSeconds(1),new([],[])));
        Assert.AreEqual(state,(game.Body,game.Area,game.LeverOn,game.HasKey,game.Completed,game.Moving,game.RunNumber,game.LastInteraction));
    }
    [TestMethod] public void FullRouteNeverEmbedsBodyAndInstancesAreIsolated()
    {
        var game=new AdventureModel();var other=new AdventureModel();
        Course.Run(game,(_,state)=>{ Assert.IsTrue(state.Body.X>=16&&state.Body.X<=600&&state.Body.Y>=64&&state.Body.Y<=320);foreach(var solid in state.ActiveSolids)Assert.IsFalse(StaticWorld.Overlaps(state.Body,solid)); });
        Assert.AreEqual(new Rect(64,168,24,24),other.Body);Assert.IsFalse(other.LeverOn||other.HasKey||other.Completed);
    }
    [TestMethod] public void PortalCornerClampPreventsEscapingOrFalseTransition()
    {
        var game=new AdventureModel();var driver=new Course.Driver(game);driver.MoveTo(212,168);driver.Interact();driver.MoveTo(590,120);
        for(int i=0;i<100;i++)driver.Tick(["right","up"],[]);
        Assert.AreEqual(Area.Courtyard,game.Area);Assert.IsTrue(game.Body.X<=600&&game.Body.Y>=64);
    }
}
