using MicroEngine.Desktop;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Desktop.Contracts;

[TestClass]
public sealed class ImageResourceTests
{
    private sealed class Api : IDesktopApi
    {
        public readonly List<string> Trace = [];
        public readonly Dictionary<object, int> Handles = [];
        public readonly Dictionary<int, Exception> ReleaseErrors = [];
        public Exception? EndError, CloseError, CreateError, DrawError;
        public bool NullImage;
        public bool Ready => true;
        public void Init(DesktopWindowOptions options) { }
        public NativeFrame Read() => new(TimeSpan.Zero, true, false, [], []);
        public void Begin() { }
        public void End() { Trace.Add("end"); if (EndError is not null) throw EndError; }
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) { }
        public void Label(string text, int x, int y, int size, Rgba color) { }
        public object CreateImage(PixelImage image)
        {
            Trace.Add("create");
            if (CreateError is not null) throw CreateError;
            if (NullImage) return null!;
            var handle = new object(); Handles.Add(handle, Handles.Count + 1); return handle;
        }
        public void DrawImage(object image, Rect destination)
        {
            Trace.Add($"draw:{Handles[image]}");
            if (DrawError is not null) throw DrawError;
        }
        public void ReleaseImage(object image)
        {
            int id = Handles[image]; Trace.Add($"release:{id}");
            if (ReleaseErrors.TryGetValue(id, out var error)) throw error;
        }
        public void Close() { Trace.Add("close"); if (CloseError is not null) throw CloseError; }
        public void DrawImageRegion(object image, Rect source, Rect destination) => throw new NotImplementedException();
    }
    private static PixelImage Pixel() => new(1, 1, new Rgba[] { new(1, 2, 3, 4) });
    private static DesktopPlatform Window(Api api, WindowReservation? reservation = null)
        => new(new(100, 100, "images"), api, reservation ?? new());
    private static IImageCanvas Canvas(DesktopPlatform window) => (IImageCanvas)window.Canvas;

    [TestMethod]
    public void CpuPixelsAreCopiedReadOnlyAndValidateBeforeAllocation()
    {
        Rgba[] source = [new(1, 2, 3, 4), new(5, 6, 7, 8)];
        var image = new PixelImage(2, 1, source); source[0] = default;
        Assert.AreEqual(new Rgba(1, 2, 3, 4), image.Pixels[0]);
        Assert.AreEqual(new Rgba(5, 6, 7, 8), image.Pixels[1]);
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<Rgba>)image.Pixels)[0] = default);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PixelImage(0, 1, []));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PixelImage(1, -1, []));
        Assert.ThrowsExactly<ArgumentException>(() => new PixelImage(int.MaxValue, int.MaxValue, []));
        Assert.ThrowsExactly<ArgumentException>(() => new PixelImage(2, 1, [new()]));
    }

    [TestMethod]
    public void ValidationPrecedesEffectsAndDisposalIsSingleAttempt()
    {
        var api = new Api(); using var window = Window(api); window.Begin(); var canvas = Canvas(window);
        Assert.ThrowsExactly<FrameApiException>(() => canvas.CreateImage(null!));
        var image = canvas.CreateImage(Pixel());
        Assert.ThrowsExactly<FrameApiException>(() => image.Dispose());
        foreach (var rect in new[] { new Rect(float.NaN, 0, 1, 1), new(0, float.PositiveInfinity, 1, 1), new(0, 0, -1, 1), new(0, 0, 1, float.NaN) })
            Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImage(image, rect));
        Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImage(null!, new(0, 0, 0, 0)));
        canvas.DrawImage(image, new(0, 0, 0, 1)); canvas.DrawImage(image, new(-1, 2, 3, 4));
        window.End();
        Assert.ThrowsExactly<FrameApiException>(() => canvas.CreateImage(Pixel()));
        image.Dispose(); image.Dispose();
        Assert.AreEqual(1, image.Width); Assert.AreEqual(1, image.Height);
        window.Begin(); Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImage(image, new(0, 0, 0, 0))); window.End();
        CollectionAssert.AreEqual(new[] { "create", "draw:1", "end", "release:1", "end" }, api.Trace);
    }

    [TestMethod]
    public void ForeignLeasesAndWrongThreadsCannotTouchNativeState()
    {
        var api = new Api(); var window = Window(api); window.Begin(); var canvas = Canvas(window); var image = canvas.CreateImage(Pixel());
        var otherApi = new Api(); using var other = Window(otherApi); other.Begin();
        Assert.ThrowsExactly<FrameApiException>(() => Canvas(other).DrawImage(image, new())); other.End();
        Exception? failure = null;
        void WrongThread(Action action)
        {
            failure = null; var thread = new Thread(() => { try { action(); } catch (Exception error) { failure = error; } });
            thread.Start(); thread.Join(); Assert.IsInstanceOfType<FrameApiException>(failure);
        }
        WrongThread(() => canvas.CreateImage(Pixel())); WrongThread(() => canvas.DrawImage(image, new())); WrongThread(image.Dispose);
        window.Dispose(); image.Dispose(); WrongThread(image.Dispose);
        Assert.ThrowsExactly<FrameApiException>(() => canvas.DrawImage(image, new()));
        CollectionAssert.AreEqual(new[] { "create", "end", "release:1", "close" }, api.Trace);
    }

    [TestMethod]
    public void CleanupAttemptsEveryResourceInReverseOrderAndRetainsErrors()
    {
        var end = new ApplicationException("end"); var first = new ApplicationException("first");
        var second = new ApplicationException("second"); var close = new ApplicationException("close");
        var api = new Api { EndError = end, CloseError = close }; api.ReleaseErrors[1] = first; api.ReleaseErrors[2] = second;
        var reservation = new WindowReservation(); var window = Window(api, reservation); window.Begin(); var canvas = Canvas(window);
        var one = canvas.CreateImage(Pixel()); var two = canvas.CreateImage(Pixel());
        var errors = Assert.ThrowsExactly<AggregateException>(window.Dispose);
        CollectionAssert.AreEqual(new Exception[] { end, second, first, close }, errors.InnerExceptions.ToArray());
        one.Dispose(); two.Dispose(); window.Dispose();
        CollectionAssert.AreEqual(new[] { "create", "create", "end", "release:2", "release:1", "close" }, api.Trace);
        Assert.ThrowsExactly<FrameApiException>(() => Window(new Api(), reservation));
    }

    [TestMethod]
    public void FailedReleaseIsNeverRetriedAndSuccessfulCloseFreesReservation()
    {
        var error = new ApplicationException("release"); var api = new Api(); api.ReleaseErrors[1] = error;
        var reservation = new WindowReservation(); var window = Window(api, reservation); window.Begin(); var image = Canvas(window).CreateImage(Pixel()); window.End();
        Assert.AreSame(error, Assert.ThrowsExactly<ApplicationException>(image.Dispose)); image.Dispose(); window.Dispose();
        using var replacement = Window(new Api(), reservation);
        Assert.AreEqual(1, api.Trace.Count(item => item == "release:1"));
        var fallback = new Api(); fallback.ReleaseErrors[1] = error;
        var fallbackWindow = Window(fallback); fallbackWindow.Begin(); Canvas(fallbackWindow).CreateImage(Pixel()); fallbackWindow.End();
        Assert.AreSame(error, Assert.ThrowsExactly<ApplicationException>(fallbackWindow.Dispose));
        Assert.AreEqual("close", fallback.Trace[^1]);
    }

    [TestMethod]
    public void AdapterBudgetIsAnExecutionLimitAndReleasedCapacityIsReusable()
    {
        var api = new Api(); using var window = Window(api); window.Begin(); var canvas = Canvas(window);
        Assert.ThrowsExactly<NotSupportedException>(() => canvas.CreateImage(new(257, 1, new Rgba[257])));
        var images = Enumerable.Range(0, 32).Select(_ => canvas.CreateImage(Pixel())).ToArray();
        Assert.ThrowsExactly<NotSupportedException>(() => canvas.CreateImage(Pixel())); Assert.AreEqual(32, api.Trace.Count);
        window.End(); images[0].Dispose(); window.Begin(); canvas.CreateImage(Pixel()); window.End();
    }

    [TestMethod]
    public void NativeAndNullFailuresAreNotApiMisuse()
    {
        var error = new ApplicationException("upload"); var api = new Api { CreateError = error };
        using var window = Window(api); window.Begin(); var canvas = Canvas(window);
        Assert.AreSame(error, Assert.ThrowsExactly<ApplicationException>(() => canvas.CreateImage(Pixel())));
        api.CreateError = null; api.NullImage = true;
        Assert.ThrowsExactly<InvalidOperationException>(() => canvas.CreateImage(Pixel()));
        api.NullImage = false; var image = canvas.CreateImage(Pixel()); api.DrawError = error;
        Assert.AreSame(error, Assert.ThrowsExactly<ApplicationException>(() => canvas.DrawImage(image, new(0, 0, 1, 1)))); window.End();
    }

    [TestMethod]
    public void FrameLoopSeparatesMisuseFromExecutionFailureAndCleansUp()
    {
        var options = new LoopOptions(TimeSpan.FromTicks(1), 1, TimeSpan.FromTicks(10));
        var invalid = new Api();
        Assert.ThrowsExactly<FrameApiException>(() => FrameLoop.Run(() => Window(invalid), options, _ => { },
            (canvas, _) => ((IImageCanvas)canvas).CreateImage(null!)));
        CollectionAssert.AreEqual(new[] { "end", "close" }, invalid.Trace);
        var error = new ApplicationException("upload"); var failing = new Api { CreateError = error };
        var report = FrameLoop.Run(() => Window(failing), options, _ => { },
            (canvas, _) => ((IImageCanvas)canvas).CreateImage(Pixel()));
        Assert.AreEqual(MicroEngine.Hosting.ExecutionOutcome.Failed, report.Outcome); Assert.AreSame(error, report.PrimaryError);
        CollectionAssert.AreEqual(new[] { "create", "end", "close" }, failing.Trace);
    }
}
