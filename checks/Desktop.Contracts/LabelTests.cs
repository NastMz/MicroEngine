using MicroEngine.Desktop;
using MicroEngine.Hosting;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Desktop.Contracts;

[TestClass]
public sealed class LabelTests
{
    private sealed class Api : IDesktopApi
    {
        public List<string> Trace = [];
        public List<(string, int, int, int, Rgba)> Labels = [];
        public Exception? LabelError;
        public bool Ready => true;
        public void Init(DesktopWindowOptions options) => Trace.Add("init");
        public NativeFrame Read() => new(TimeSpan.Zero, true, false, [], []);
        public void Begin() => Trace.Add("begin");
        public void End() => Trace.Add("end");
        public void Clear(Rgba color) => Trace.Add("clear");
        public void Fill(Rect rectangle, Rgba color) => Trace.Add("fill");
        public void Label(string text, int x, int y, int size, Rgba color)
        {
            Trace.Add("label");
            if (LabelError is not null) throw LabelError;
            Labels.Add((text, x, y, size, color));
        }
        public void Close() => Trace.Add("close");
        public object CreateImage(PixelImage image) => throw new NotImplementedException();
        public void DrawImage(object image, Rect destination) => throw new NotImplementedException();
        public void ReleaseImage(object image) => throw new NotImplementedException();
    }
    private static DesktopPlatform Window(Api api) => new(new(640, 360, "labels"), api, new WindowReservation());
    private static ILabelCanvas Labels(DesktopPlatform window)
    {
        Assert.IsInstanceOfType<ILabelCanvas>(window.Canvas);
        return (ILabelCanvas)window.Canvas;
    }

    [TestMethod]
    public void OptionalProfileForwardsExactValuesAndRetainsPrimitiveBehavior()
    {
        var api = new Api(); using var window = Window(api); window.Begin();
        var canvas = Labels(window); var color = new Rgba(1, 2, 3, 4);
        canvas.Label("Advance", -5, 10, 20, color);
        canvas.Label(new string('~', 80), 0, -10, 64, color);
        canvas.Label(" ", 0, 0, 10, color);
        canvas.Label("", 0, 0, 20, color);
        Assert.AreEqual(3, api.Labels.Count);
        Assert.AreEqual(("Advance", -5, 10, 20, color), api.Labels[0]);
        Assert.AreEqual((new string('~', 80), 0, -10, 64, color), api.Labels[1]);
        canvas.Clear(color); canvas.Fill(new(0, 0, 1, 1), color); window.End();
        CollectionAssert.AreEqual(new[] { "init", "begin", "label", "label", "label", "clear", "fill", "end" }, api.Trace);
    }

    [TestMethod]
    public void InvalidLabelsAndSizesHaveNoNativeEffects()
    {
        var api = new Api(); using var window = Window(api); window.Begin(); var canvas = Labels(window);
        foreach (var text in new[] { null!, "\n", "\t", "\0", "ñ", new string('x', 81) })
            Assert.ThrowsExactly<FrameApiException>(() => canvas.Label(text, 0, 0, 20, new()));
        foreach (var size in new[] { 0, 9, 65, int.MaxValue })
            Assert.ThrowsExactly<FrameApiException>(() => canvas.Label("", 0, 0, size, new()));
        Assert.AreEqual(0, api.Labels.Count);
        CollectionAssert.AreEqual(new[] { "init", "begin" }, api.Trace);
        window.End();
    }

    [TestMethod]
    public void RetainedCanvasChecksLifetimeAndThreadBeforeArguments()
    {
        var api = new Api(); var window = Window(api); window.Begin(); var canvas = Labels(window); window.End();
        var ended = Assert.ThrowsExactly<FrameApiException>(() => canvas.Label(null!, 0, 0, 0, new()));
        StringAssert.Contains(ended.Message, "committed drawing");
        window.Begin(); Exception? failure = null;
        var thread = new Thread(() => { try { canvas.Label(null!, 0, 0, 0, new()); } catch (Exception error) { failure = error; } });
        thread.Start(); thread.Join(); Assert.IsInstanceOfType<FrameApiException>(failure);
        StringAssert.Contains(failure!.Message, "creating thread");
        window.End(); window.Dispose();
        var disposed = Assert.ThrowsExactly<FrameApiException>(() => canvas.Label("", 0, 0, 20, new()));
        StringAssert.Contains(disposed.Message, "disposed"); Assert.AreEqual(0, api.Labels.Count);
    }

    [TestMethod]
    public void MisuseAndNativeFailureKeepClassificationAndCleanup()
    {
        var bad = new Api();
        Assert.ThrowsExactly<FrameApiException>(() => FrameLoop.Run(() => Window(bad),
            new(TimeSpan.FromTicks(1), 1, TimeSpan.FromTicks(10)), _ => { },
            (canvas, _) => ((ILabelCanvas)canvas).Label("\n", 0, 0, 20, new())));
        CollectionAssert.AreEqual(new[] { "init", "begin", "end", "close" }, bad.Trace);
        var nativeError = new ApplicationException("native label"); var failing = new Api { LabelError = nativeError };
        var report = FrameLoop.Run(() => Window(failing), new(TimeSpan.FromTicks(1), 1, TimeSpan.FromTicks(10)), _ => { },
            (canvas, _) => ((ILabelCanvas)canvas).Label("Advance", 0, 0, 20, new()));
        Assert.AreEqual(ExecutionOutcome.Failed, report.Outcome); Assert.AreSame(nativeError, report.PrimaryError);
        CollectionAssert.AreEqual(new[] { "init", "begin", "label", "end", "close" }, failing.Trace);
    }
}
