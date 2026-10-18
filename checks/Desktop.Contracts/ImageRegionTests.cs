using MicroEngine.Desktop;
using MicroEngine.Loop;
using MicroEngine.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Desktop.Contracts;

[TestClass]
public sealed class ImageRegionTests
{
    private sealed class Api : IDesktopApi
    {
        public readonly List<string> Trace = [];
        public readonly object Handle = new();
        public (Rect Source, Rect Destination) Last;
        public Exception? Error;
        public bool Ready => true;
        public void Init(DesktopWindowOptions options) { }
        public NativeFrame Read() => new(TimeSpan.Zero, true, false, [], []);
        public void Begin() { }
        public void End() => Trace.Add("end");
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) { }
        public void Label(string text, int x, int y, int size, Rgba color) { }
        public object CreateImage(PixelImage image) => Handle;
        public void DrawImage(object image, Rect destination) => Trace.Add("whole");
        public void DrawImageRegion(object image, Rect source, Rect destination)
        {
            Assert.AreSame(Handle, image); Last = (source, destination); Trace.Add("region");
            if (Error is not null) throw Error;
        }
        public void ReleaseImage(object image) => Trace.Add("release");
        public void Close() => Trace.Add("close");
    }
    private static DesktopPlatform Window(Api api) => new(new(100, 100, "regions"), api, new());
    private static PixelImage Pixels() => new(4, 2, new Rgba[8]);

    [TestMethod]
    public void ValidRegionForwardsExactBoundsAndZeroDestinationIsValidatedNoOp()
    {
        var api = new Api(); using var window = Window(api); window.Begin(); var canvas = (IImageRegionCanvas)window.Canvas;
        var image = canvas.CreateImage(Pixels());
        canvas.DrawImageRegion(image, new(2, 0, 2, 2), new(-10, 3, 20, 40));
        Assert.AreEqual((new Rect(2, 0, 2, 2), new Rect(-10, 3, 20, 40)), api.Last);
        canvas.DrawImageRegion(image, new(3, 1, 1, 1), new(0, 0, 0, 10));
        canvas.DrawImage(image, new(0, 0, 1, 1)); window.End();
        CollectionAssert.AreEqual(new[] { "region", "whole", "end" }, api.Trace);
    }

    [TestMethod]
    public void InvalidSourceOrDestinationHasNoNativeEffectsEvenForZeroDraw()
    {
        var api = new Api(); using var window = Window(api); window.Begin(); var canvas = (IImageRegionCanvas)window.Canvas;
        var image = canvas.CreateImage(Pixels());
        foreach (var source in new[] { new Rect(-1, 0, 1, 1), new(0, -1, 1, 1), new(0, 0, 0, 1), new(0, 0, 1, -1),
            new(0.5f, 0, 1, 1), new(0, 0, 1.5f, 1), new(0, 0, 1, float.NaN), new(float.PositiveInfinity, 0, 1, 1),
            new(4, 0, 1, 1), new(3, 0, 2, 1), new(0, 1, 1, 2), new(float.MaxValue, 0, float.MaxValue, 1) })
            Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImageRegion(image, source, new()));
        foreach (var destination in new[] { new Rect(float.NaN, 0, 1, 1), new(0, float.PositiveInfinity, 1, 1), new(0, 0, -1, 1), new(0, 0, 0, float.NaN) })
            Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImageRegion(image, new(0, 0, 1, 1), destination));
        Assert.AreEqual(0, api.Trace.Count); window.End();
    }

    [TestMethod]
    public void ResourceAndContextGuardsRunBeforeSourceAndDestination()
    {
        var api = new Api(); var window = Window(api); window.Begin(); var canvas = (IImageRegionCanvas)window.Canvas;
        var image = canvas.CreateImage(Pixels());
        Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImageRegion(null!, new(), new()));
        using var other = Window(new Api()); other.Begin();
        Assert.ThrowsExactly<FrameApiException>(() => ((IImageRegionCanvas)other.Canvas).DrawImageRegion(image, new(), new())); other.End();
        Exception? error = null; var thread = new Thread(() => { try { canvas.DrawImageRegion(image, new(), new()); } catch (Exception caught) { error = caught; } });
        thread.Start(); thread.Join(); Assert.IsInstanceOfType<FrameApiException>(error); StringAssert.Contains(error!.Message, "creating thread");
        window.End(); var ended = Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImageRegion(image, new(), new())); StringAssert.Contains(ended.Message, "committed drawing");
        image.Dispose(); window.Begin(); Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImageRegion(image, new(0, 0, 1, 1), new())); window.Dispose();
        Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImageRegion(image, new(), new()));
        CollectionAssert.AreEqual(new[] { "end", "release", "end", "close" }, api.Trace);
    }

    [TestMethod]
    public void FrameLoopPreservesMisuseClassificationAndNativeFailureCleanup()
    {
        var options = new LoopOptions(TimeSpan.FromTicks(1), 1, TimeSpan.FromTicks(10));
        var bad = new Api();
        Assert.ThrowsExactly<FrameApiException>(() => FrameLoop.Run(() => Window(bad), options, _ => { }, (canvas, _) =>
        {
            var regions = (IImageRegionCanvas)canvas; regions.DrawImageRegion(regions.CreateImage(Pixels()), new(), new());
        }));
        CollectionAssert.AreEqual(new[] { "end", "release", "close" }, bad.Trace);
        var failure = new ApplicationException("region"); var api = new Api { Error = failure };
        var report = FrameLoop.Run(() => Window(api), options, _ => { }, (canvas, _) =>
        {
            var regions = (IImageRegionCanvas)canvas; regions.DrawImageRegion(regions.CreateImage(Pixels()), new(0, 0, 1, 1), new(0, 0, 1, 1));
        });
        Assert.AreEqual(ExecutionOutcome.Failed, report.Outcome); Assert.AreSame(failure, report.PrimaryError);
        CollectionAssert.AreEqual(new[] { "region", "end", "release", "close" }, api.Trace);
    }
}
