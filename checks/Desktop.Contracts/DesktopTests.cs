using MicroEngine.Desktop;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Desktop.Contracts;

[TestClass]
public sealed class DesktopTests
{
    private static DesktopWindowOptions Options() => new(640, 360, "test");
    private sealed class Api : IDesktopApi
    {
        public List<string> Trace = [];
        public bool Ready { get; set; } = true;
        public Exception? InitError, CloseError, EndError;
        public NativeFrame Frame = new(TimeSpan.FromMilliseconds(10), true, false, ["right"], ["jump"]);
        public void Init(DesktopWindowOptions o) { Trace.Add("init"); if (InitError is not null) throw InitError; }
        public NativeFrame Read() { Trace.Add("read"); return Frame; }
        public void Begin() => Trace.Add("begin");
        public void End() { Trace.Add("end"); if (EndError is not null) throw EndError; }
        public void Clear(Rgba color) => Trace.Add("clear");
        public void Fill(Rect rect, Rgba color) => Trace.Add("fill");
        public void Close() { Trace.Add("close"); if (CloseError is not null) throw CloseError; }
    }

    [TestMethod]
    public void WindowLeaseConflictsBeforeNativeCallsAndReleasesAfterDispose()
    {
        var lease = new WindowReservation(); var a = new Api(); var b = new Api();
        var first = new DesktopPlatform(Options(), a, lease);
        Assert.ThrowsExactly<FrameApiException>(() => new DesktopPlatform(Options(), b, lease));
        Assert.HasCount(0, b.Trace);
        first.Dispose(); first.Dispose();
        using var second = new DesktopPlatform(Options(), b, lease);
        CollectionAssert.AreEqual(new[] { "init", "close" }, a.Trace);
    }

    [TestMethod]
    [DataRow("init")]
    [DataRow("ready")]
    public void PartialInitializationClosesAndReleasesReservation(string failure)
    {
        var lease = new WindowReservation(); var api = new Api { Ready = failure != "ready" };
        if (failure == "init") api.InitError = new ApplicationException("init");
        Assert.Throws<Exception>(() => new DesktopPlatform(Options(), api, lease));
        CollectionAssert.AreEqual(new[] { "init", "close" }, api.Trace);
        using var retry = new DesktopPlatform(Options(), new Api(), lease);
    }

    [TestMethod]
    public void UncertainInitializationCleanupPoisonsLeaseAndRetainsBothErrors()
    {
        var lease = new WindowReservation(); var init = new Exception("init"); var close = new Exception("close");
        var api = new Api { InitError = init, CloseError = close };
        var error = Assert.ThrowsExactly<AggregateException>(() => new DesktopPlatform(Options(), api, lease));
        CollectionAssert.AreEqual(new[] { init, close }, error.InnerExceptions.ToArray());
        var next = new Api(); Assert.ThrowsExactly<FrameApiException>(() => new DesktopPlatform(Options(), next, lease));
        Assert.HasCount(0, next.Trace);
    }

    [TestMethod]
    public void CanvasLifetimeAndGeometryAreCheckedBeforeNativeCalls()
    {
        var api = new Api(); using var window = new DesktopPlatform(Options(), api, new WindowReservation());
        Assert.ThrowsExactly<FrameApiException>(() => _ = window.Canvas);
        window.Begin(); var canvas = window.Canvas;
        Assert.ThrowsExactly<FrameApiException>(() => canvas.Fill(new Rect(float.NaN, 0, 1, 1), new()));
        Assert.ThrowsExactly<FrameApiException>(() => canvas.Fill(new Rect(0, 0, -1, 1), new()));
        canvas.Clear(new()); canvas.Fill(new Rect(0, 0, 1, 1), new()); window.End();
        Assert.ThrowsExactly<FrameApiException>(() => canvas.Clear(new()));
        CollectionAssert.AreEqual(new[] { "init", "begin", "clear", "fill", "end" }, api.Trace);
    }

    [TestMethod]
    public void WrongThreadCallsHaveNoNativeSideEffects()
    {
        var api = new Api(); var window = new DesktopPlatform(Options(), api, new WindowReservation());
        var errors = new List<Exception>();
        var thread = new Thread(() => { foreach (var action in new Action[] { () => window.Poll(), window.Begin, window.End, window.Dispose })
            try { action(); } catch (Exception e) { errors.Add(e); } });
        thread.Start(); thread.Join(); Assert.HasCount(4, errors); Assert.IsTrue(errors.All(e => e is FrameApiException));
        CollectionAssert.AreEqual(new[] { "init" }, api.Trace); window.Dispose();
    }

    [TestMethod]
    public void FocusTransitionsRebaseTimeAndRemoveUnfocusedInput()
    {
        var api = new Api(); using var window = new DesktopPlatform(Options(), api, new WindowReservation());
        Assert.AreEqual(TimeSpan.FromMilliseconds(10), window.Poll().Elapsed);
        api.Frame = api.Frame with { Active = false, Elapsed = TimeSpan.FromSeconds(5) };
        var inactive = window.Poll(); Assert.AreEqual(TimeSpan.Zero, inactive.Elapsed);
        Assert.HasCount(0, inactive.Input.Held); Assert.HasCount(0, inactive.Input.Pressed);
        api.Frame = api.Frame with { Active = true }; Assert.AreEqual(TimeSpan.Zero, window.Poll().Elapsed);
        api.Frame = api.Frame with { Elapsed = TimeSpan.FromMilliseconds(10) };
        Assert.AreEqual(TimeSpan.FromMilliseconds(10), window.Poll().Elapsed);
    }

    [TestMethod]
    public void DisposeActiveDrawingAttemptsEndThenCloseExactlyOnceAndPoisonsOnCloseFailure()
    {
        var lease = new WindowReservation(); var end = new Exception("end"); var close = new Exception("close");
        var api = new Api { EndError = end, CloseError = close }; var window = new DesktopPlatform(Options(), api, lease);
        window.Begin(); var error = Assert.ThrowsExactly<AggregateException>(window.Dispose); window.Dispose();
        CollectionAssert.AreEqual(new[] { end, close }, error.InnerExceptions.ToArray());
        CollectionAssert.AreEqual(new[] { "init", "begin", "end", "close" }, api.Trace);
        Assert.ThrowsExactly<FrameApiException>(() => new DesktopPlatform(Options(), new Api(), lease));
    }

    [TestMethod]
    public void InvalidWindowConfigurationFailsBeforeNativeFactory()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DesktopWindowOptions(0, 1, "test"));
        Assert.ThrowsExactly<ArgumentException>(() => new DesktopWindowOptions(1, 1, " "));
    }
}
