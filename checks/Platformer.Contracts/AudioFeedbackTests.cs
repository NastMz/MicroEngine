using MicroEngine.Audio;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platformer.Model;
using Platformer.Checks;
namespace Platformer.Contracts;

[TestClass]
public sealed class AudioFeedbackTests
{
    private sealed class Audio(List<string> trace) : IOneShotAudio
    {
        public bool FailDispose;
        public void Play(string cue)=>trace.Add(cue);public void StopAll()=>trace.Add("stop");
        public void Dispose(){trace.Add("audio-close");if(FailDispose)throw new IOException("audio-close");}
    }
    private sealed class Window(List<string> trace) : IFramePlatform
    {
        public bool FailDispose;public PlatformFrame Poll()=>new(Course.Step,new([],[]));
        public ICanvas Canvas=>throw new NotSupportedException();public void Begin()=>trace.Add("begin");public void End()=>trace.Add("end");
        public void Dispose(){trace.Add("window-close");if(FailDispose)throw new IOException("window-close");}
    }
    [TestMethod] public void EventsAreTransientOrderedAndInvalidCallsAreAtomic()
    {
        var game=new LevelGame();game.Update(Course.Step,new([],["jump"]));
        CollectionAssert.AreEqual(new[]{LevelEvent.Jumped},game.EventsThisUpdate.ToArray());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>game.Update(TimeSpan.Zero,new([],[])));
        CollectionAssert.AreEqual(new[]{LevelEvent.Jumped},game.EventsThisUpdate.ToArray());
        game.Update(Course.Step,new(["jump"],[]));Assert.AreEqual(0,game.EventsThisUpdate.Count);
        game=new LevelGame();var all=new List<LevelEvent>();Course.Run(game,(_,g)=>all.AddRange(g.EventsThisUpdate));
        Assert.AreEqual(3,all.Count(e=>e==LevelEvent.CoinCollected));Assert.AreEqual(1,all.Count(e=>e==LevelEvent.LevelCompleted));
        Assert.AreEqual(LevelEvent.LevelCompleted,all[^1]);game.Update(Course.Step,new([],[]));Assert.AreEqual(0,game.EventsThisUpdate.Count);
    }
    [TestMethod] public void DefeatEmitsOnceAndRetryDoesNotReplayHistory()
    {
        var game=new LevelGame(2);for(int i=0;i<500&&!game.Dead;i++)game.Update(Course.Step,new(["right"],[]));
        CollectionAssert.AreEqual(new[]{LevelEvent.Defeated},game.EventsThisUpdate.ToArray());
        game.Update(Course.Step,new(["right"],[]));Assert.AreEqual(0,game.EventsThisUpdate.Count);
        game.Update(Course.Step,new(["right"],["jump"]));Assert.AreEqual(0,game.EventsThisUpdate.Count);Assert.IsFalse(game.Dead);
    }
    [TestMethod] public void FeedbackFollowsSessionAdvanceAndStopsOnPauseAndReplacement()
    {
        var game=new MeadowSession();var trace=new List<string>();var audio=new Audio(trace);var feedback=new MeadowSoundFeedback();
        feedback.Publish(game,audio);game.Update(Course.Step,new([],["jump"]));feedback.Publish(game,audio);Assert.AreEqual(0,trace.Count);
        game.Update(Course.Step,new([],["jump"]));feedback.Publish(game,audio);CollectionAssert.AreEqual(new[]{"jump"},trace);
        game.Update(Course.Step,new([],["pause"]));feedback.Publish(game,audio);Assert.AreEqual("stop",trace[^1]);
        int count=trace.Count;game.Update(Course.Step,new(["jump"],[]));feedback.Publish(game,audio);Assert.AreEqual(count,trace.Count);
        game.Update(Course.Step,new([],["restart"]));feedback.Publish(game,audio);Assert.AreEqual("stop",trace[^1]);
        feedback.Publish(new MeadowSession(),null);
    }
    [TestMethod] public void SoundPlatformPreservesAcquisitionAndBothCleanupErrors()
    {
        var trace=new List<string>();var audio=new Audio(trace){FailDispose=true};
        var error=Assert.ThrowsExactly<AggregateException>(()=>SoundPlatform.Create(()=>throw new IOException("window-init"),()=>audio));
        Assert.AreEqual(2,error.InnerExceptions.Count);CollectionAssert.AreEqual(new[]{"audio-close"},trace);
        trace.Clear();audio=new Audio(trace){FailDispose=true};var window=new Window(trace){FailDispose=true};
        var platform=SoundPlatform.Create(()=>{trace.Add("window-acquire");return window;},()=>{trace.Add("audio-acquire");return audio;});
        Assert.AreEqual(Course.Step,platform.Poll().Elapsed);platform.Begin();platform.End();
        error=Assert.ThrowsExactly<AggregateException>(()=>platform.Dispose());Assert.AreEqual(2,error.InnerExceptions.Count);platform.Dispose();
        CollectionAssert.AreEqual(new[]{"audio-acquire","window-acquire","begin","end","window-close","audio-close"},trace);
    }
    [TestMethod] public void AllFourCuesFollowPublicSessionEventsWithoutStateInference()
    {
        var session=new MeadowSession();var trace=new List<string>();var audio=new Audio(trace);var feedback=new MeadowSoundFeedback();feedback.Publish(session,audio);
        session.Update(Course.Step,new([],["jump"]));feedback.Publish(session,audio);
        Course.Run(session.Campaign.Current,(_,_)=>feedback.Publish(session,audio),update:session.Update);
        Assert.AreEqual(3,trace.Count(c=>c=="coin"));Assert.AreEqual(1,trace.Count(c=>c=="level-complete"));Assert.IsTrue(trace.Contains("jump"));
        session.Update(Course.Step,new([],["jump"]));feedback.Publish(session,audio);Assert.AreEqual("stop",trace[^1]);
        Assert.ThrowsExactly<InvalidOperationException>(()=>Course.Run(session.Campaign.Current,(_,_)=>feedback.Publish(session,audio),anticipateGaps:false,update:session.Update));
        Assert.AreEqual("defeat",trace[^1]);Assert.AreEqual(1,trace.Count(c=>c=="defeat"));
        session.Update(Course.Step,new([],["jump"]));feedback.Publish(session,audio);Assert.AreEqual("stop",trace[^1]);
    }
    [TestMethod] public void SoundPlatformFailureIsReportedAsLoopCleanupAndSilentProviderIsValid()
    {
        var trace=new List<string>();var audio=new Audio(trace){FailDispose=true};var window=new Window(trace){FailDispose=true};
        var report=FrameLoop.Run(()=>SoundPlatform.Create(()=>window,()=>audio),new(Course.Step,5,TimeSpan.FromMilliseconds(250)),
            _=>throw new ApplicationException("update"),(_,_)=>{});
        Assert.IsInstanceOfType<ApplicationException>(report.PrimaryError);
        Assert.AreEqual(1,report.CleanupErrors.Count);Assert.IsInstanceOfType<AggregateException>(report.CleanupErrors[0]);
        Assert.AreEqual(2,((AggregateException)report.CleanupErrors[0]).InnerExceptions.Count);
        using var silent=SoundPlatform.Create(()=>new Window(trace),()=>null);Assert.AreEqual(Course.Step,silent.Poll().Elapsed);
    }
    [TestMethod] public void WrongThreadWrapperDisposalDoesNotPoisonOwnerCleanup()
    {
        var trace=new List<string>();var platform=SoundPlatform.Create(()=>new Window(trace),()=>new Audio(trace));
        Task.Run(()=>Assert.ThrowsExactly<InvalidOperationException>(()=>platform.Dispose())).GetAwaiter().GetResult();
        Assert.AreEqual(0,trace.Count);platform.Dispose();CollectionAssert.AreEqual(new[]{"window-close","audio-close"},trace);
    }
}