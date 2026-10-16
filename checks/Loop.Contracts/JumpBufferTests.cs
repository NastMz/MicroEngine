using FirstPlayable;
using FirstPlayable.Model;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class JumpBufferTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(1d / 60);
    private static readonly InputSnapshot Empty = new([], []);
    private static readonly InputSnapshot Jump = new([], ["jump"]);
    private static readonly InputSnapshot Held = new(["jump"], []);
    private static GameModel Buffered(TimeSpan? window = null) => new(new PlatformerOptions(window ?? TimeSpan.FromMilliseconds(100)));

    private static int LandingTick()
    {
        var baseline = new GameModel();
        baseline.Update(Step, Jump);
        for (var tick = 2; tick <= 200; tick++)
        {
            baseline.Update(Step, Empty);
            if (baseline.Grounded) return tick;
        }
        throw new AssertFailedException("Baseline did not land within the bounded fixture.");
    }

    private static void BeforeLanding(GameModel game, int updatesBeforeContact = 1)
    {
        game.Update(Step, Jump);
        for (var tick = 2; tick <= LandingTick() - updatesBeforeContact; tick++) game.Update(Step, Empty);
        Assert.IsFalse(game.Grounded, "Fixture must still be airborne.");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DisabledControllerPreservesBaselineLandingEdgeDrop(bool explicitZero)
    {
        var game = explicitZero ? Buffered(TimeSpan.Zero) : new GameModel();
        BeforeLanding(game);
        game.Update(Step, Jump); // The original grounded check precedes contact.
        Assert.IsTrue(game.Grounded); Assert.AreEqual(268f, game.Y); Assert.AreEqual(0f, game.VerticalVelocity);
        game.Update(Step, Empty); Assert.IsTrue(game.Grounded);
    }

    [TestMethod]
    public void BufferedLandingEdgeLaunchesOnceWithoutSecondIntegration()
    {
        var game = Buffered(); BeforeLanding(game);
        game.Update(Step, new InputSnapshot([], ["jump", "jump"]));
        Assert.IsFalse(game.Grounded); Assert.AreEqual(268f, game.Y); Assert.AreEqual(-300f, game.VerticalVelocity);
        game.Update(Step, Held);
        Assert.AreEqual(-285f, game.VerticalVelocity, .001f); Assert.AreEqual(263.25f, game.Y, .001f);
        for (var i = 0; i < 100; i++) game.Update(Step, Held);
        Assert.IsTrue(game.Grounded); Assert.AreEqual(0f, game.VerticalVelocity);
    }

    [TestMethod]
    [DataRow(0L, true)]
    [DataRow(-1L, false)]
    [DataRow(1000000L, true)]
    public void PriorTickIntentUsesInclusiveWindowAndExpiresAfterIt(long extraTicks, bool accepted)
    {
        var game = Buffered(Step + TimeSpan.FromTicks(extraTicks)); BeforeLanding(game, 2);
        game.Update(Step, Jump); // One update before contact.
        Assert.IsFalse(game.Grounded);
        game.Update(Step, Empty); // Contact update.
        Assert.AreEqual(!accepted, game.Grounded);
        Assert.AreEqual(accepted ? -300f : 0f, game.VerticalVelocity);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void NewPressRefreshesOnePendingIntent(bool refresh)
    {
        var game = Buffered(Step); BeforeLanding(game, 3);
        game.Update(Step, Jump); // Two updates before contact.
        game.Update(Step, refresh ? Jump : Empty); // One update before contact.
        game.Update(Step, Empty); // Contact update.
        Assert.AreEqual(!refresh, game.Grounded);
    }

    [TestMethod]
    public void HeldWithoutNewEdgeNeverQueuesOrRepeatsJump()
    {
        var game = Buffered(); BeforeLanding(game);
        for (var i = 0; i < 30; i++) game.Update(Step, Held);
        Assert.IsTrue(game.Grounded); Assert.AreEqual(268f, game.Y);
    }

    [TestMethod]
    public void AirbornePressDoesNotGrantAirJumpAndExpiredIntentDoesNotLaunchLater()
    {
        var game = Buffered(); game.Update(Step, Jump);
        for (var i = 0; i < 8; i++) game.Update(Step, Empty);
        var prior = game.VerticalVelocity;
        game.Update(Step, Jump);
        Assert.AreEqual(prior + 900 * (float)Step.TotalSeconds, game.VerticalVelocity, .001f);
        for (var i = 0; i < 40; i++) game.Update(Step, Empty);
        Assert.IsTrue(game.Grounded); Assert.AreEqual(0f, game.VerticalVelocity);
    }

    [TestMethod]
    public void ClearCancelsIntentWithoutChangingVisibleMovementState()
    {
        var game = Buffered(); BeforeLanding(game, 2); game.Update(Step, Jump);
        var state = (game.X, game.Y, game.VerticalVelocity, game.Grounded);
        game.ClearJumpBuffer(); game.ClearJumpBuffer();
        Assert.AreEqual(state, (game.X, game.Y, game.VerticalVelocity, game.Grounded));
        game.Update(Step, Empty); Assert.IsTrue(game.Grounded);
    }

    [TestMethod]
    public void ControllerOptionsAreIndependentAndDoNotChangeOtherMovement()
    {
        var buffered = Buffered(); var disabled = new GameModel();
        BeforeLanding(buffered); BeforeLanding(disabled);
        buffered.Update(Step, Jump); disabled.Update(Step, Jump);
        Assert.IsFalse(buffered.Grounded); Assert.IsTrue(disabled.Grounded);
        Assert.AreEqual(disabled.X, buffered.X);
    }

    [TestMethod]
    public void OptionsRejectNegativeAndNullButAcceptZeroAndLargestDuration()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PlatformerOptions(TimeSpan.FromTicks(-1)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new GameModel(null!));
        Assert.AreEqual(TimeSpan.Zero, new PlatformerOptions(TimeSpan.Zero).JumpBufferWindow);
        var game = Buffered(TimeSpan.MaxValue); BeforeLanding(game, 2); game.Update(Step, Jump); game.Update(Step, Empty);
        Assert.IsFalse(game.Grounded); Assert.AreEqual(-300f, game.VerticalVelocity);
    }

    [TestMethod]
    public void LargeDeltaExpiresIntentWithoutBufferArithmeticOverflow()
    {
        var game = Buffered(); BeforeLanding(game, 2); game.Update(Step, Jump);
        game.Update(TimeSpan.MaxValue, Empty);
        Assert.IsTrue(game.Grounded); Assert.AreEqual(0f, game.VerticalVelocity);
    }

    private sealed class Platform : IFramePlatform, ICanvas
    {
        public PlatformFrame Frame = new(TimeSpan.Zero, Empty);
        public List<string> Trace { get; } = [];
        public PlatformFrame Poll() { Trace.Add("poll"); return Frame; }
        public void Begin() => Trace.Add("begin");
        public ICanvas Canvas => this;
        public void End() => Trace.Add("end");
        public void Dispose() => Trace.Add("dispose");
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) { }
    }

    [TestMethod]
    [DataRow("inactive", true)]
    [DataRow("close", true)]
    [DataRow("active", false)]
    public void ConsumerPollClearsOnlyOnInactiveOrCloseAndForwardsExactly(string phase, bool cleared)
    {
        var game = Buffered(); BeforeLanding(game, 2); game.Update(Step, Jump);
        var inner = new Platform { Frame = new(TimeSpan.Zero, Empty, Active: phase != "inactive", CloseRequested: phase == "close") };
        IFramePlatform wrapper = new ControllerPlatform(inner, game);
        Assert.AreEqual(inner.Frame, wrapper.Poll());
        wrapper.Begin(); Assert.AreSame(inner, wrapper.Canvas); wrapper.End(); wrapper.Dispose();
        CollectionAssert.AreEqual(new[] { "poll", "begin", "end", "dispose" }, inner.Trace);
        game.Update(Step, Empty);
        Assert.AreEqual(cleared, game.Grounded);
    }
}
