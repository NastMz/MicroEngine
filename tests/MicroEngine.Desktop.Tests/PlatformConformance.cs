using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The platform contract (<see cref="IPlatformHost"/>) as a conformance suite: every host runs the same tests by
/// subclassing this class, saying how it opens and how a test drives the events it can drive (a resize, the focus,
/// a trip to the background, a quit request). An event a host cannot be made to send here is reported as skipped with
/// the reason, never passed. The suite only uses <see cref="IPlatformHost"/> and <see cref="PlatformRequest"/>, so it
/// can move to a testing assembly that third-party backends reference.
/// </summary>
public abstract class PlatformConformance
{
    /// <summary>Opens the host for <paramref name="request"/>, or returns null after marking the test skipped when it cannot open here.</summary>
    protected abstract IPlatform? Open(PlatformRequest request);

    /// <summary>Makes the host's main surface change size (the event is then delivered by a pump); false when this host cannot be made to here.</summary>
    protected virtual bool Resize(IPlatform host, int width, int height) => false;
    /// <summary>Makes the host gain or lose the input focus; false when this host cannot be made to here.</summary>
    protected virtual bool Focus(IPlatform host, bool focused) => false;
    /// <summary>The window's size as the windowing toolkit itself reports it, independent of the host's surface (evidence for a resize the environment did not apply).</summary>
    protected virtual (int Width, int Height) ToolkitWindowSize(IPlatform host) => host.MainSurface?.PixelSize ?? (0, 0);
    /// <summary>What the loop does between two pumps on this host that lets it learn of window changes (nothing by default; a host that reads the window system when a frame ends presents one).</summary>
    protected virtual void BetweenPumps(IPlatform host) { }
    /// <summary>Sends the host to the background (true) or brings it back (false); false when this host cannot be made to here.</summary>
    protected virtual bool Background(IPlatform host, bool background) => false;
    /// <summary>Asks the host to quit as the system would (a window's close button); false when this host cannot be made to here.</summary>
    protected virtual bool RequestQuit(IPlatform host) => false;

    private static readonly GameSettings Settings = new("platform conformance", 64, 36, 2);

    /// <summary>The request every test opens with: a hidden window, no display pacing, no sound.</summary>
    protected static PlatformRequest Request => new(Settings) { Hidden = true, WaitForDisplay = false, Mute = true };

    private IPlatform OpenOrSkip() => Open(Request) ?? throw new AssertInconclusiveException("the host cannot open here");

    private static void Skip(bool driven, string what)
    {
        if (!driven) Assert.Inconclusive($"this host cannot be made to {what} here");
    }

    /// <summary>The events a pump delivered, as text, in order.</summary>
    private sealed class Sink : IEngineEventSink
    {
        public List<string> Events { get; } = [];
        public void Resized(SurfaceResized size) => Events.Add($"resized {size.Width}x{size.Height}");
        public void FocusChanged(bool focused) => Events.Add(focused ? "focus" : "blur");
        public void Suspending() => Events.Add("suspending");
        public void Resumed() => Events.Add("resumed");
        public void QuitRequested() => Events.Add("quit");
    }

    /// <summary>Pumps once and returns what was delivered and whether a frame may run.</summary>
    private static (List<string> Events, bool Run) Pump(IPlatform host)
    {
        var sink = new Sink();
        bool run = host.PumpEvents(sink);
        return (sink.Events, run);
    }

    /// <summary>Pumps until nothing is pending (events of the host's own start-up), at most a few times.</summary>
    private static void Drain(IPlatform host)
    {
        for (int i = 0; i < 8 && Pump(host).Events.Count > 0; i++) { }
    }

    // ---- Clock --------------------------------------------------------------------------------------------------------

    [TestMethod]
    public void TheClockIsMonotonicAndFollowsRealTimeNotFrames()
    {
        using var host = OpenOrSkip();
        double previous = host.Clock.Now;
        for (int i = 0; i < 200; i++)
        {
            host.PumpEvents(new Sink());
            double now = host.Clock.Now;
            Assert.IsGreaterThanOrEqualTo(previous, now, $"the clock went back at pump {i}: {now} < {previous}");
            previous = now;
        }
        double before = host.Clock.Now;
        Thread.Sleep(50);
        double slept = host.Clock.Now - before;
        Assert.IsGreaterThanOrEqualTo(0.04, slept, $"50 ms of real time advanced the clock by {slept:0.000} s");
        Assert.IsLessThan(2.0, slept, "the clock follows real time");
    }

    // ---- Events ------------------------------------------------------------------------------------------------------

    [TestMethod]
    public void AnIdleHostDeliversNothingAndLetsFramesRun()
    {
        using var host = OpenOrSkip();
        Drain(host);
        var (events, run) = Pump(host);
        Assert.AreEqual("", string.Join(", ", events), "nothing happened, so nothing is delivered");
        Assert.IsTrue(run, "a host that is not suspended lets the frame run");
    }

    [TestMethod]
    public void AResizeIsDeliveredOnceWithTheNewPixelSizeAndRaisesTheSurfaceEvent()
    {
        using var host = OpenOrSkip();
        var surface = host.MainSurface;
        Assert.IsNotNull(surface, "a host with a window has a main surface");
        Drain(host);
        int raised = 0;
        surface.Resized += () => raised++;
        var before = surface.PixelSize;
        var toolkitBefore = ToolkitWindowSize(host);
        Skip(Resize(host, 200, 120), "resize its surface");
        // The window system may apply a size asynchronously (X11 sends it back as an event): pump, as the loop does once
        // per frame, until something is delivered or a bounded deadline passes.
        var delivered = new List<string>();
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (delivered.Count == 0 && deadline.Elapsed < TimeSpan.FromSeconds(2))
        {
            BetweenPumps(host);
            delivered.AddRange(Pump(host).Events);
            if (delivered.Count == 0) Thread.Sleep(10);
        }
        var after = surface.PixelSize;
        var toolkitAfter = ToolkitWindowSize(host);
        // Evidence that the environment cannot resize: the toolkit's own window size (not the host's surface) did not
        // change either. X11 without a window manager (Xvfb) may keep an unmapped window's size. Reported, never passed;
        // a toolkit size that changed while the surface did not is a host defect and fails below.
        if (before == after && delivered.Count == 0 && toolkitBefore == toolkitAfter)
            Assert.Inconclusive($"{GetType().Name}: the window system kept the hidden window at {toolkitAfter.Width}x{toolkitAfter.Height} " +
                $"(asked for 200x120) for {deadline.Elapsed.TotalSeconds:0.0} s of pumping");
        Assert.AreNotEqual(before, after, $"{GetType().Name}: the surface's size follows the window (toolkit {toolkitBefore} -> {toolkitAfter})");
        CollectionAssert.AreEqual(new[] { $"resized {after.Width}x{after.Height}" }, delivered, "one Resized with the surface's new pixel size");
        Assert.AreEqual(1, raised, "ISurface.Resized is raised in the same pump");
        Assert.AreEqual("", string.Join(", ", Pump(host).Events), "a size that did not change again is not delivered again");
    }

    [TestMethod]
    public void FocusChangesAreDeliveredOnlyWhenTheFocusChanges()
    {
        using var host = OpenOrSkip();
        Skip(Focus(host, false), "lose the focus");
        Drain(host);
        Skip(Focus(host, true), "gain the focus");
        CollectionAssert.AreEqual(new[] { "focus" }, Pump(host).Events);
        Focus(host, true);
        Assert.AreEqual("", string.Join(", ", Pump(host).Events), "the focus did not change");
        Focus(host, false);
        CollectionAssert.AreEqual(new[] { "blur" }, Pump(host).Events);
    }

    [TestMethod]
    public void SuspendingAndResumedAlternateAndNoFrameRunsWhileSuspended()
    {
        using var host = OpenOrSkip();
        Drain(host);
        Skip(Background(host, true), "go to the background");
        var (events, run) = Pump(host);
        CollectionAssert.AreEqual(new[] { "suspending" }, events);
        Assert.IsFalse(run, "no frame runs while suspended");
        (events, run) = Pump(host);
        Assert.AreEqual("", string.Join(", ", events), "still in the background: no second Suspending");
        Assert.IsFalse(run);
        Background(host, false);
        (events, run) = Pump(host);
        CollectionAssert.AreEqual(new[] { "resumed" }, events);
        Assert.IsTrue(run, "frames run again after Resumed");
    }

    [TestMethod]
    public void ATripToTheBackgroundBetweenTwoPumpsIsDeliveredAsBothEventsInOrder()
    {
        using var host = OpenOrSkip();
        Drain(host);
        Skip(Background(host, true), "go to the background");
        Background(host, false);
        var (events, run) = Pump(host);
        CollectionAssert.AreEqual(new[] { "suspending", "resumed" }, events);
        Assert.IsTrue(run, "the host came back before the pump");
    }

    [TestMethod]
    public void AQuitRequestIsDeliveredOncePerRequest()
    {
        using var host = OpenOrSkip();
        Drain(host);
        Skip(RequestQuit(host), "be asked to quit");
        CollectionAssert.AreEqual(new[] { "quit" }, Pump(host).Events);
        Assert.AreEqual("", string.Join(", ", Pump(host).Events), "one request, one delivery");
        RequestQuit(host);
        CollectionAssert.AreEqual(new[] { "quit" }, Pump(host).Events, "a later request is delivered again");
    }

    // ---- Surface -----------------------------------------------------------------------------------------------------

    [TestMethod]
    public void TheMainSurfaceHasAPixelSizeAndADensity()
    {
        using var host = OpenOrSkip();
        var surface = host.MainSurface;
        Assert.IsNotNull(surface);
        Assert.IsGreaterThan(0, surface.PixelSize.Width);
        Assert.IsGreaterThan(0, surface.PixelSize.Height);
        Assert.IsGreaterThan(0f, surface.PixelDensity);
        Assert.IsFalse(surface.IsMinimized, "a surface with a size is not minimised");
    }

    // ---- Capabilities ------------------------------------------------------------------------------------------------

    private interface IUndeclared;

    [TestMethod]
    public void AnUndeclaredCapabilityIsAbsentAndADeclaredOneIsStable()
    {
        using var host = OpenOrSkip();
        Assert.IsFalse(host.TryGetCapability<IUndeclared>(out var none));
        Assert.IsNull(none);
        if (host.TryGetCapability<IDisplayControl>(out var display))
        {
            Assert.IsTrue(host.TryGetCapability<IDisplayControl>(out var again));
            Assert.AreSame(display, again, "a capability is the same object on every query");
        }
    }

    [TestMethod]
    public void OnlyAHostThatCanBlockOffersAPreciseDelayAndItBlocks()
    {
        using var host = OpenOrSkip();
        if (!host.TryGetCapability<IPreciseDelay>(out var delay))
        {
            Assert.IsFalse(host.TryGetCapability<IPreciseDelay>(out _));
            return;
        }
        Assert.IsTrue(host.CanBlock, "a host that can never block offers no way to wait");
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        delay.Delay(0.02);
        double waited = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds;
        Assert.IsGreaterThanOrEqualTo(0.015, waited, $"Delay(0.02) returned after {waited * 1000:0.0} ms");
    }

    [TestMethod]
    public void AHostAskedNotToWaitForTheDisplayReportsNoDisplayPacing()
    {
        using var host = OpenOrSkip();
        double rate = host.TryGetCapability<IDisplayPacing>(out var pacing) ? pacing.DisplayRate : 0;
        Assert.AreEqual(0.0, rate, "WaitForDisplay = false: frames are not paced to a display");
    }
}
