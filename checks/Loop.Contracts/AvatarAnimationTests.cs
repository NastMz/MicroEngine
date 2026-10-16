using FirstPlayable;
using FirstPlayable.Model;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class AvatarAnimationTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromTicks(1);
    private static readonly TimeSpan Beat = TimeSpan.FromMilliseconds(120);
    private static (AvatarPose, bool, int) State(AvatarAnimation a) => (a.Pose, a.FacingLeft, a.WalkFrame);
    private static AvatarAnimation Walk(bool left = false)
    {
        var a = new AvatarAnimation(); a.Update(Tick, left ? -1 : 1, true); return a;
    }

    [TestMethod]
    public void PoseFacingTransitionsUseActualMotionAndAirPrecedence()
    {
        var a = new AvatarAnimation(); Assert.AreEqual((AvatarPose.Idle, false, 0), State(a));
        a.Update(Tick, -1, true); Assert.AreEqual((AvatarPose.Walk, true, 0), State(a));
        a.Update(Beat, -1, true); Assert.AreEqual(1, a.WalkFrame);
        a.Update(Tick, 1, true); Assert.IsFalse(a.FacingLeft); Assert.AreEqual(1, a.WalkFrame);
        a.Update(Tick, -1, false); Assert.AreEqual((AvatarPose.Air, true, 0), State(a));
        a.Update(Beat, -0f, false); Assert.AreEqual((AvatarPose.Air, true, 0), State(a));
        a.Update(Tick, 0, true); Assert.AreEqual((AvatarPose.Idle, true, 0), State(a));
        a.Update(Beat, 1, true); Assert.AreEqual((AvatarPose.Walk, false, 0), State(a));
    }

    [TestMethod]
    public void GaitBoundariesPartitionsAndLargestDeltaAreDeterministic()
    {
        var a = Walk(); a.Update(Beat - Tick, 1, true); Assert.AreEqual(0, a.WalkFrame);
        a.Update(Tick, 1, true); Assert.AreEqual(1, a.WalkFrame);
        a.Update(Beat, 1, true); Assert.AreEqual(0, a.WalkFrame);
        var b = Walk(); b.Update(Beat + Beat, 1, true); Assert.AreEqual(State(a), State(b));
        a.Update(TimeSpan.MaxValue, 1, true);
        Assert.AreEqual((int)((TimeSpan.MaxValue.Ticks % (Beat.Ticks * 2)) / Beat.Ticks), a.WalkFrame);
        var separate = Walk(); Assert.AreEqual(0, separate.WalkFrame);
    }

    [TestMethod]
    public void InvalidUpdatesPreservePhaseAndPublicState()
    {
        var a = Walk(); a.Update(Beat - Tick, 1, true); var before = State(a);
        Assert.AreEqual("delta", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.Update(TimeSpan.Zero, float.NaN, false)).ParamName);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.Update(-Tick, 1, true));
        foreach (var value in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity })
            Assert.AreEqual("displacementX", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.Update(Tick, value, false)).ParamName);
        Assert.AreEqual(before, State(a)); a.Update(Tick, 1, true); Assert.AreEqual(1, a.WalkFrame);
        a.Update(Tick, 0, true); a.Update(Beat, 1, true); Assert.AreEqual(0, a.WalkFrame);
    }

    [TestMethod]
    public void BlockedMotionDoesNotTurnOrWalk()
    {
        var game = new GameModel(new PlatformerOptions(TimeSpan.Zero), [new StaticObstacle(64, 268, 10, 32)]);
        var a = Walk(true); var oldX = game.X;
        game.Update(TimeSpan.FromSeconds(1d / 60), new InputSnapshot(["right"], []));
        a.Update(Tick, game.X - oldX, game.Grounded);
        Assert.AreEqual((AvatarPose.Idle, true, 0), State(a));
    }

    private sealed class Canvas : ICanvas
    {
        public List<(Rect, Rgba)> Calls { get; } = [];
        public void Clear(Rgba color) => Assert.Fail("Avatar must not clear its canvas.");
        public void Fill(Rect rectangle, Rgba color) => Calls.Add((rectangle, color));
    }

    [TestMethod]
    [DataRow(AvatarPose.Idle, 0)]
    [DataRow(AvatarPose.Walk, 0)]
    [DataRow(AvatarPose.Walk, 1)]
    [DataRow(AvatarPose.Air, 0)]
    public void EveryFrameHasExactMirroredContentAndRenderingIsPure(AvatarPose pose, int frame)
    {
        var gold = new Rgba(240, 180, 65);
        var feet = pose switch
        {
            AvatarPose.Idle => new[] { new Rect(5,24,5,8), new Rect(14,24,5,8) },
            AvatarPose.Air => [new Rect(3,24,7,5), new Rect(14,24,7,5)],
            _ when frame == 0 => [new Rect(2,24,8,8), new Rect(15,24,6,5)],
            _ => [new Rect(3,24,6,5), new Rect(14,24,8,8)]
        };
        var expected = new[] { (new Rect(4,2,16,10),gold), (new Rect(6,12,12,12),new Rgba(210,140,45)),
            (new Rect(16,5,2,3),new Rgba(24,28,40)), (feet[0],gold), (feet[1],gold) };
        foreach (var left in new[] { false, true })
        {
            var a = new AvatarAnimation(); a.Update(Tick, left ? -1 : 1, pose != AvatarPose.Air);
            if (frame == 1) a.Update(Beat, left ? -1 : 1, true);
            if (pose == AvatarPose.Idle) a.Update(Tick, 0, true);
            var before = State(a); var canvas = new Canvas();
            AvatarRenderer.Draw(canvas, 40, 228, a); AvatarRenderer.Draw(canvas, 40, 228, a);
            Assert.AreEqual(before, State(a)); Assert.AreEqual(10, canvas.Calls.Count);
            for (var i = 0; i < 10; i++)
            {
                var (rect, color) = expected[i % 5];
                Assert.IsTrue(rect.X >= 0 && rect.Y >= 0 && rect.X + rect.Width <= 24 && rect.Y + rect.Height <= 32);
                var mirroredX = left ? 24 - rect.X - rect.Width : rect.X;
                Assert.AreEqual((new Rect(40 + mirroredX, 228 + rect.Y, rect.Width, rect.Height), color), canvas.Calls[i]);
            }
            if (pose == AvatarPose.Walk) { a.Update(Beat, left ? -1 : 1, true); Assert.AreEqual(1 - frame, a.WalkFrame); }
        }
    }

    [TestMethod]
    public void InvalidDrawDoesNotPartiallyRender()
    {
        var canvas = new Canvas(); var a = new AvatarAnimation();
        Assert.ThrowsExactly<ArgumentNullException>(() => AvatarRenderer.Draw(null!, 0, 0, a));
        Assert.ThrowsExactly<ArgumentNullException>(() => AvatarRenderer.Draw(canvas, 0, 0, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AvatarRenderer.Draw(canvas, float.NaN, 0, a));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AvatarRenderer.Draw(canvas, 0, float.PositiveInfinity, a));
        Assert.AreEqual(0, canvas.Calls.Count);
    }
}
