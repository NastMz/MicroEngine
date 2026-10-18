using MicroEngine.Loop;
using MicroEngine.TwoD;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace TwoD.Contracts;
[TestClass]
public class UtilityTests
{
    [TestMethod] public void SweepStopsAtNearestWallAndFloorRegardlessOfSolidOrder()
    {
        Rect[] boxes = [new(100, 0, 20, 200), new(0, 100, 100, 20), new(200, 0, 20, 200)];
        foreach (var source in new[] { boxes, boxes.Reverse().ToArray() })
        {
            var move = new StaticWorld(source).Move(new(0, 0, 10, 10), 1000, 1000);
            Assert.AreEqual(new Rect(90, 90, 10, 10), move.Bounds);
            Assert.IsTrue(move.BlockedX); Assert.IsTrue(move.BlockedY);
        }
    }
    [TestMethod] public void EdgesCanMoveAwayAndPerpendicularTouchDoesNotBlock()
    {
        var world = new StaticWorld([new(20, 20, 20, 20)]);
        Assert.AreEqual(new Rect(30, 0, 10, 20), world.Move(new(0, 0, 10, 20), 30, 0).Bounds);
        Assert.AreEqual(new Rect(0, 20, 10, 10), world.Move(new(10, 20, 10, 10), -10, 0).Bounds);
        Assert.IsFalse(StaticWorld.Overlaps(new(0, 0, 10, 10), new(10, 0, 10, 10)));
        Assert.IsTrue(StaticWorld.Overlaps(new(0, 0, 10, 10), new(9, 9, 10, 10)));
    }
    [TestMethod] public void WorldCopiesAndRejectsInvalidOrInitiallyOverlappingBodies()
    {
        Rect[] solids = [new(20, 0, 10, 10)]; var world = new StaticWorld(solids); solids[0] = new(100, 0, 10, 10);
        Assert.AreEqual(10, world.Move(new(0, 0, 10, 10), 100, 0).Bounds.X);
        Assert.ThrowsExactly<ArgumentException>(() => world.Move(new(21, 0, 10, 10), 0, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.Move(new(0, 0, 10, 10), float.NaN, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StaticWorld([new(0, 0, -1, 2)]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StaticWorld([]).Move(new(0, 0, 10, 10), float.MaxValue, 0));
    }
    [TestMethod] public void WorldRejectsMutuallyOverlappingSolidsButAcceptsTouching()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new StaticWorld([new(0,0,10,10),new(5,0,10,10)]));
        var touching = new StaticWorld([new(0,0,10,10),new(10,0,10,10)]);
        Assert.AreEqual(new Rect(20,0,5,5),touching.Move(new(20,0,5,5),0,0).Bounds);
    }
    [TestMethod] public void NegativeSweepAndCeilingContactRetainBodySize()
    {
        var world = new StaticWorld([new(0, 0, 10, 100), new(10, 0, 100, 10)]);
        var result = world.Move(new(30, 30, 15, 12), -100, -100);
        Assert.AreEqual(new Rect(10, 10, 15, 12), result.Bounds); Assert.IsTrue(result.BlockedX && result.BlockedY);
    }
    [TestMethod] public void AnimationCopiesFramesSupportsBoundariesHugeDeltasAndReset()
    {
        Rect[] frames = [new(0, 0, 128, 128), new(129, 0, 128, 128)];
        var animation = new FrameAnimation(frames, TimeSpan.FromTicks(3)); frames[0] = default;
        Assert.AreEqual(128, animation.Current.Width); animation.Advance(TimeSpan.FromTicks(2)); Assert.AreEqual(0, animation.Current.X);
        animation.Advance(TimeSpan.FromTicks(1)); Assert.AreEqual(129, animation.Current.X);
        animation.Advance(TimeSpan.MaxValue); Assert.AreEqual(129, animation.Current.X);
        animation.Reset(); Assert.AreEqual(0, animation.Current.X);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => animation.Advance(TimeSpan.FromTicks(-1))); Assert.AreEqual(0, animation.Current.X);
    }
    [TestMethod] public void AnimationRejectsEmptyNonIntegerAndInvalidDuration()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new FrameAnimation([], TimeSpan.FromTicks(1)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FrameAnimation([new(0.5f, 0, 1, 1)], TimeSpan.FromTicks(1)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FrameAnimation([new(0, 0, 1, 1)], TimeSpan.Zero));
    }
    private sealed class Lease : IImageResource
    {
        public int Width => 1; public int Height => 1; public int Calls; public bool Fail;
        public void Dispose() { Calls++; if (Fail) throw new InvalidOperationException(); }
    }
    private sealed class Canvas : IImageCanvas
    {
        public List<Lease> Leases = []; public bool Fail; public int Creates;
        public void Clear(Rgba c) { } public void Fill(Rect r, Rgba c) { }
        public IImageResource CreateImage(PixelImage image) { Creates++; if (Fail) throw new InvalidOperationException(); var lease = new Lease(); Leases.Add(lease); return lease; }
        public void DrawImage(IImageResource image, Rect destination) { }
    }
    private static Dictionary<string, PixelImage> Sources() => new() { ["a"] = new(1, 1, [new(1, 2, 3)]), ["b"] = new(1, 1, [new(1, 2, 3)]) };
    [TestMethod] public void CacheCreatesOnceCopiesSourcesAndRejectsProviderWithoutEffects()
    {
        var sources = Sources(); using var images = new ImageSet(sources); sources.Clear(); var canvas = new Canvas();
        var a = images.Get(canvas, "a"); Assert.AreSame(a, images.Get(canvas, "a")); Assert.AreEqual(1, canvas.Creates);
        var other = new Canvas(); Assert.ThrowsExactly<InvalidOperationException>(() => images.Get(other, "b")); Assert.AreEqual(0, other.Creates);
        Assert.ThrowsExactly<KeyNotFoundException>(() => images.Get(canvas, "missing")); Assert.AreEqual(1, canvas.Creates);
    }
    [TestMethod] public void FailedFirstAcquisitionDoesNotBindAndLaterFailureRetainsLeases()
    {
        using var images = new ImageSet(Sources()); var bad = new Canvas { Fail = true };
        Assert.ThrowsExactly<InvalidOperationException>(() => images.Get(bad, "a")); var good = new Canvas(); var a = images.Get(good, "a");
        good.Fail = true; Assert.ThrowsExactly<InvalidOperationException>(() => images.Get(good, "b")); Assert.AreSame(a, images.Get(good, "a"));
    }
    [TestMethod] public void DisposalRetainsFailuresAndRetriesWithoutReleasingSuccessfulLeaseTwice()
    {
        var images = new ImageSet(Sources()); var canvas = new Canvas(); images.Get(canvas, "a"); images.Get(canvas, "b");
        canvas.Leases[1].Fail = true; Assert.ThrowsExactly<AggregateException>(() => images.Dispose());
        Assert.AreEqual(1, canvas.Leases[0].Calls); Assert.ThrowsExactly<ObjectDisposedException>(() => images.Get(canvas, "a"));
        canvas.Leases[1].Fail = false; images.Dispose(); images.Dispose();
        Assert.AreEqual(1, canvas.Leases[0].Calls); Assert.AreEqual(2, canvas.Leases[1].Calls);
    }
}
