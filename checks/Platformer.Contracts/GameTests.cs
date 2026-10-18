using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroEngine.Loop;
using Platformer.Model;
using Platformer.Checks;
namespace Platformer.Contracts;
[TestClass] public class GameTests
{
    [TestMethod] public void PublicRouteCollectsAllCoinsFinishesAndReplaysIdentically()
    {
        var game = new LevelGame(); int first = Course.Run(game);
        Assert.AreEqual(3, game.CollectedCount); Assert.IsTrue(game.Completed); Assert.AreEqual(1152f, game.CameraX);
        var finish = game.Body; game.Update(Course.Step, new(["left", "jump"], [])); Assert.AreEqual(finish, game.Body);
        game.Update(Course.Step, new(["right"], ["jump"])); Assert.AreEqual(new Rect(48,392,28,40), game.Body); Assert.AreEqual(0, game.CollectedCount);
        game.Update(Course.Step, new([], [])); Assert.IsTrue(game.Grounded); Assert.AreEqual(392f, game.Body.Y);
        Assert.AreEqual(first, Course.Run(game));
    }
    [TestMethod] public void HeldJumpDoesNotLaunchAndInvalidCallsAreAtomic()
    {
        var game = new LevelGame(); var start = game.Body;
        game.Update(Course.Step, new(["jump"], [])); Assert.AreEqual(start, game.Body);
        Assert.ThrowsExactly<ArgumentNullException>(() => game.Update(Course.Step, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => game.Update(TimeSpan.Zero, new([], [])));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => game.Update(TimeSpan.FromSeconds(1), new([], [])));
        Assert.AreEqual(start, game.Body); Assert.AreEqual(0, game.CollectedCount);
        game.Update(Course.Step, new([], ["jump"])); Assert.IsFalse(game.Grounded); Assert.IsTrue(game.Body.Y < start.Y);
    }
    [TestMethod] public void HazardLatchesDeathAndFreshResetConsumesInput()
    {
        var game = new LevelGame(); float previous = float.NaN;
        for (int i = 0; i < 2000 && !game.Dead; i++)
        {
            bool jump = game.Grounded && game.Body.X == previous && game.Body.X < 1400; previous = game.Body.X;
            game.Update(Course.Step, new(["right"], jump ? ["jump"] : []));
        }
        Assert.IsTrue(game.Dead); Assert.IsFalse(game.Completed); var body = game.Body;
        game.Update(Course.Step, new(["right"], [])); Assert.AreEqual(body, game.Body);
        game.Update(Course.Step, new(["right"], ["jump"])); Assert.IsFalse(game.Dead); Assert.AreEqual(new Rect(48,392,28,40), game.Body);
    }

    [TestMethod] public void MissingCoinPreventsFlagCompletionWithoutStoppingMovement()
    {
        var game = new LevelGame(); float previous = float.NaN;
        for (int i=0;i<1500 && game.Body.X<1845;i++)
        {
            bool jump = game.Grounded && ((game.Body.X>=120 && game.Body.X<200) || game.Body.X==previous
                || (game.Body.X+28>=1380 && game.Body.X+28<=1440));
            previous=game.Body.X; game.Update(Course.Step,new(["right"],jump?["jump"]:[]));
        }
        Assert.IsFalse(game.Dead); Assert.IsTrue(game.Body.X>=1845); Assert.IsFalse(game.IsCollected(0));
        Assert.IsFalse(game.Completed); Assert.IsTrue(game.CollectedCount<3);
    }
    [TestMethod] public void BufferedJumpCanBeCancelledBeforeLanding()
    {
        var keep=new LevelGame(); var cancel=new LevelGame();
        keep.Update(Course.Step,new([], ["jump"])); cancel.Update(Course.Step,new([], ["jump"]));
        float previous=keep.Body.Y;
        for(int i=0;i<100;i++)
        {
            keep.Update(Course.Step,new([],[])); cancel.Update(Course.Step,new([],[]));
            if(keep.Body.Y>365 && keep.Body.Y>previous) break;
            previous=keep.Body.Y;
        }
        keep.Update(Course.Step,new([], ["jump"])); cancel.Update(Course.Step,new([], ["jump"])); cancel.ClearJumpBuffer();
        bool relaunched=false;
        for(int i=0;i<20;i++)
        {
            float y=keep.Body.Y; keep.Update(Course.Step,new([],[])); cancel.Update(Course.Step,new([],[]));
            if(keep.Body.Y<y) relaunched=true;
        }
        Assert.IsTrue(relaunched); Assert.IsTrue(cancel.Grounded); Assert.AreEqual(392f,cancel.Body.Y);
    }

    private sealed class FakePlatform(bool active, bool close) : IFramePlatform
    {
        public int Begins, Ends, Disposes;
        public PlatformFrame Poll() => new(TimeSpan.Zero,new([],[]),active,close);
        public ICanvas Canvas => throw new NotSupportedException();
        public void Begin() => Begins++; public void End() => Ends++; public void Dispose() => Disposes++;
    }
    [TestMethod] public void InactiveOrClosingPollCancelsPendingLandingIntentAndForwardsLifecycle()
    {
        foreach(var flags in new[] {(false,false),(true,true)})
        {
            var game=new LevelGame(); game.Update(Course.Step,new([], ["jump"])); float previous=game.Body.Y;
            for(int i=0;i<100;i++) { game.Update(Course.Step,new([],[])); if(game.Body.Y>365 && game.Body.Y>previous)break; previous=game.Body.Y; }
            game.Update(Course.Step,new([], ["jump"])); var inner=new FakePlatform(flags.Item1,flags.Item2);
            using(var wrapper=new Platformer.GamePlatform(inner,game)) { var frame=wrapper.Poll(); Assert.AreEqual(flags.Item1,frame.Active); wrapper.Begin();wrapper.End(); }
            for(int i=0;i<20;i++)game.Update(Course.Step,new([],[]));
            Assert.IsTrue(game.Grounded); Assert.AreEqual(1,inner.Begins); Assert.AreEqual(1,inner.Ends); Assert.AreEqual(1,inner.Disposes);
        }
    }
}
