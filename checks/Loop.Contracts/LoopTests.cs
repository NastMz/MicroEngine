using FirstPlayable.Model;
using MicroEngine.Hosting;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class LoopTests
{
    private static InputSnapshot Input(string[]? held = null, string[]? pressed = null) => new(held ?? [], pressed ?? []);
    private static PlatformFrame Frame(int ms, string[]? held = null, string[]? pressed = null, bool active = true)
        => new(TimeSpan.FromMilliseconds(ms), Input(held, pressed), active);
    private static LoopOptions Options(int limit = 2, int maxElapsed = 100, int inputs = 256)
        => new(TimeSpan.FromMilliseconds(10), limit, TimeSpan.FromMilliseconds(maxElapsed), inputs);

    private sealed class Platform(params PlatformFrame[] frames) : IFramePlatform, ICanvas
    {
        private readonly Queue<PlatformFrame> _frames = new(frames);
        public List<string> Trace { get; } = [];
        public Exception? BeginError, EndError, DisposeError, CanvasError;
        public Action? OnBegin, OnCanvas;
        public bool NullCanvas;
        public ICanvas Canvas { get { OnCanvas?.Invoke(); return CanvasError is not null ? throw CanvasError : NullCanvas ? null! : this; } }
        public PlatformFrame Poll() { Trace.Add("poll"); return _frames.Count > 0 ? _frames.Dequeue() : new(TimeSpan.Zero, Input(), CloseRequested: true); }
        public void Begin() { Trace.Add("begin"); OnBegin?.Invoke(); if (BeginError is not null) throw BeginError; }
        public void End() { Trace.Add("end"); if (EndError is not null) throw EndError; }
        public void Dispose() { Trace.Add("dispose"); if (DisposeError is not null) throw DisposeError; }
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) { }
    }

    [TestMethod]
    public void ZeroTicksRetainEdgesAndCatchupConsumesOrderedEdgesOnce()
    {
        var p = new Platform(Frame(4, pressed: ["jump"]), Frame(21, ["right"], ["jump"]));
        var updates = new List<UpdateFrame>(); var draws = new List<PresentationFrame>();
        var report = FrameLoop.Run(() => p, Options(), updates.Add, (_, f) => draws.Add(f));
        Assert.AreEqual(ExecutionOutcome.Stopped, report.Outcome);
        Assert.HasCount(2, updates);
        Assert.IsTrue(updates.All(f => f.Delta == TimeSpan.FromMilliseconds(10) && f.Input.IsHeld("right")));
        CollectionAssert.AreEqual(new[] { "jump", "jump" }, updates[0].Input.Pressed.ToArray());
        Assert.HasCount(0, updates[1].Input.Pressed);
        Assert.AreEqual(.4, draws[0].Alpha, 1e-10); Assert.AreEqual(.5, draws[1].Alpha, 1e-10);
        Assert.AreEqual(1, p.Trace.Count(x => x == "dispose"));
    }

    [TestMethod]
    public void ClampAndDroppedBacklogAreReportedWithoutAlphaOverflow()
    {
        var p = new Platform(Frame(65)); PresentationFrame? draw = null; var count = 0;
        FrameLoop.Run(() => p, Options(maxElapsed: 45), _ => count++, (_, f) => draw = f);
        Assert.AreEqual(2, count); Assert.AreEqual(.5, draw!.Alpha, 1e-10);
        Assert.AreEqual(TimeSpan.FromMilliseconds(40), draw.DiscardedTime);
    }

    [TestMethod]
    public void InactiveFrameCancelsPendingEdgesAndAccumulator()
    {
        var p = new Platform(Frame(4, pressed: ["jump"]), Frame(90, ["right"], ["jump"], false), Frame(10));
        var updates = new List<UpdateFrame>(); var draws = new List<PresentationFrame>();
        FrameLoop.Run(() => p, Options(), updates.Add, (_, f) => draws.Add(f));
        Assert.HasCount(1, updates); Assert.HasCount(0, updates[0].Input.Pressed);
        Assert.IsFalse(updates[0].Input.IsHeld("right")); Assert.AreEqual(0, draws[1].Alpha);
    }

    [TestMethod]
    [DataRow("update")]
    [DataRow("draw")]
    public void StopSuppressesLaterCallbacksButEndsCommittedDraw(string phase)
    {
        var p = new Platform(Frame(25)); var ticks = 0; var draws = 0;
        var result = FrameLoop.Run(() => p, Options(), f => { ticks++; if (phase == "update") f.Control.RequestStop(); },
            (_, f) => { draws++; f.Control.RequestStop(); });
        Assert.AreEqual(ExecutionOutcome.Stopped, result.Outcome);
        Assert.AreEqual(phase == "update" ? 1 : 2, ticks); Assert.AreEqual(phase == "update" ? 0 : 1, draws);
        Assert.AreEqual(draws, p.Trace.Count(x => x == "end"));
    }

    [TestMethod]
    [DataRow("begin")]
    [DataRow("canvas")]
    public void StopInsidePlatformPresentationBoundarySuppressesConsumerDraw(string phase)
    {
        var p = new Platform(Frame(0), Frame(0)); FrameControl? control = null; var draws = 0;
        Action stop = () => control?.RequestStop();
        if (phase == "begin") p.OnBegin = stop; else p.OnCanvas = stop;
        var report = FrameLoop.Run(() => p, Options(), _ => { }, (_, frame) => { control = frame.Control; draws++; });
        Assert.AreEqual(ExecutionOutcome.Stopped, report.Outcome);
        Assert.AreEqual(1, draws);
        Assert.AreEqual(2, p.Trace.Count(x => x == "end"));
        Assert.AreEqual("dispose", p.Trace[^1]);
    }

    [TestMethod]
    [DataRow("begin")]
    [DataRow("canvas")]
    [DataRow("null-canvas")]
    [DataRow("draw")]
    [DataRow("end")]
    [DataRow("update")]
    public void FailuresCloseOnlyCommittedDrawingAndDispose(string phase)
    {
        var failure = new ApplicationException(phase); var p = new Platform(Frame(10));
        if (phase == "begin") p.BeginError = failure;
        if (phase == "end") p.EndError = failure;
        if (phase == "canvas") p.CanvasError = failure;
        if (phase == "null-canvas") p.NullCanvas = true;
        var report = FrameLoop.Run(() => p, Options(), _ => { if (phase == "update") throw failure; }, (_, _) => { if (phase == "draw") throw failure; });
        Assert.AreEqual(ExecutionOutcome.Failed, report.Outcome);
        Assert.AreEqual(phase is "begin" or "update" ? 0 : 1, p.Trace.Count(x => x == "end"));
        Assert.AreEqual("dispose", p.Trace[^1]);
        if (phase != "null-canvas") Assert.AreSame(failure, report.PrimaryError);
    }

    [TestMethod]
    public void DrawEndAndCleanupCausesRetainExactOrder()
    {
        var draw = new ApplicationException("draw"); var end = new Exception("end"); var cleanup = new Exception("cleanup");
        var p = new Platform(Frame(10)) { EndError = end, DisposeError = cleanup };
        var report = FrameLoop.Run(() => p, Options(), _ => { }, (_, _) => throw draw);
        var both = (AggregateException)report.PrimaryError!;
        CollectionAssert.AreEqual(new Exception[] { draw, end }, both.InnerExceptions.ToArray());
        CollectionAssert.AreEqual(new[] { cleanup }, report.CleanupErrors.ToArray());
    }

    [TestMethod]
    [DataRow("negative")]
    [DataRow("null-input")]
    [DataRow("overflow")]
    public void InvalidPlatformResultsFailAndDispose(string phase)
    {
        var frame = phase == "negative" ? Frame(-1) : phase == "null-input" ? default : Frame(0, pressed: ["a", "b"]);
        var p = new Platform(frame);
        var report = FrameLoop.Run(() => p, Options(inputs: 1), _ => Assert.Fail("no update"), (_, _) => Assert.Fail("no draw"));
        Assert.AreEqual(ExecutionOutcome.Failed, report.Outcome); Assert.AreEqual("dispose", p.Trace[^1]);
    }

    [TestMethod]
    public void CloseBeforeFirstUpdateDoesNotOpenDrawing()
    {
        var p = new Platform(); FrameLoop.Run(() => p, Options(), _ => Assert.Fail(), (_, _) => Assert.Fail());
        CollectionAssert.AreEqual(new[] { "poll", "dispose" }, p.Trace);
    }

    [TestMethod]
    public void ControlRejectsWrongThreadAndMisuseRethrowsAfterCleanup()
    {
        var p = new Platform(Frame(10));
        var error = Assert.ThrowsExactly<FrameApiException>(() => FrameLoop.Run(() => p, Options(), f =>
        {
            Exception? wrong = null; var thread = new Thread(() => { try { f.Control.RequestStop(); } catch (Exception e) { wrong = e; } });
            thread.Start(); thread.Join(); throw wrong!;
        }, (_, _) => Assert.Fail()));
        Assert.IsInstanceOfType<InvalidOperationException>(error); Assert.AreEqual("dispose", p.Trace[^1]);
        Assert.HasCount(0, typeof(FrameApiException).GetConstructors());
    }

    [TestMethod]
    public void ConsumerInvalidOperationIsExecutionFailureNotApiMisuse()
    {
        var e = new InvalidOperationException("consumer"); var p = new Platform(Frame(10));
        var report = FrameLoop.Run(() => p, Options(), _ => throw e, (_, _) => { });
        Assert.AreEqual(ExecutionOutcome.Failed, report.Outcome); Assert.AreSame(e, report.PrimaryError);
    }

    [TestMethod]
    public void NestedApiMisuseRetainsEndAndCleanupCauses()
    {
        var end = new Exception("end"); var cleanup = new Exception("cleanup");
        var p = new Platform(Frame(10)) { EndError = end, DisposeError = cleanup };
        var all = Assert.ThrowsExactly<AggregateException>(() => FrameLoop.Run(() => p, Options(), _ => { }, (_, f) =>
        {
            Exception? misuse = null;
            var thread = new Thread(() => { try { f.Control.RequestStop(); } catch (Exception error) { misuse = error; } });
            thread.Start(); thread.Join(); throw misuse!;
        }));
        Assert.HasCount(2, all.InnerExceptions);
        var drawing = (AggregateException)all.InnerExceptions[0];
        Assert.IsInstanceOfType<FrameApiException>(drawing.InnerExceptions[0]);
        Assert.AreSame(end, drawing.InnerExceptions[1]);
        Assert.AreSame(cleanup, all.InnerExceptions[1]);
        Assert.AreEqual("dispose", p.Trace[^1]);
    }

    [TestMethod]
    public void InputCopiesAreImmutableOrderedAndCaseSensitive()
    {
        string[] held = ["left"]; string[] pressed = ["jump", "jump"];
        var input = new InputSnapshot(held, pressed); held[0] = "right"; pressed[0] = "other";
        Assert.IsTrue(input.IsHeld("left")); Assert.IsFalse(input.IsHeld("Left")); Assert.AreEqual("jump", input.Pressed[0]);
        Assert.ThrowsExactly<ArgumentException>(() => input.IsHeld(" "));
        Assert.ThrowsExactly<ArgumentNullException>(() => new InputSnapshot(null!, []));
    }

    [TestMethod]
    public void PreflightRejectsInvalidOptionsAndCallbacksBeforeFactory()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LoopOptions(TimeSpan.Zero, 2, TimeSpan.FromSeconds(1)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LoopOptions(TimeSpan.FromTicks(1), 0, TimeSpan.FromSeconds(1)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LoopOptions(TimeSpan.FromTicks(1), 1, TimeSpan.Zero));
        var created = false;
        Assert.ThrowsExactly<ArgumentNullException>(() => FrameLoop.Run(() => { created = true; return new Platform(); }, Options(), null!, (_, _) => { }));
        Assert.IsFalse(created);
    }

    [TestMethod]
    public void GameOwnsMovementJumpAndLandingWithoutHeldOrAirborneRepeat()
    {
        var game = new GameModel(); var step = TimeSpan.FromSeconds(1d / 60);
        game.Update(step, Input(["right"], ["jump"]));
        Assert.IsTrue(game.X > 40 && game.Y < 268); Assert.IsFalse(game.Grounded);
        var velocity = game.VerticalVelocity;
        game.Update(step, Input(["jump"], ["jump"])); Assert.IsTrue(game.VerticalVelocity > velocity);
        for (var i = 0; i < 180; i++) game.Update(step, Input(["jump"]));
        Assert.IsTrue(game.Grounded); Assert.AreEqual(268f, game.Y); Assert.AreEqual(0f, game.VerticalVelocity);
        for (var i = 0; i < 600; i++) game.Update(step, Input(["right"])); Assert.AreEqual(616f, game.X);
    }
}
