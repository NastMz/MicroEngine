using System.Runtime.CompilerServices;
using MicroEngine.Hosting;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class ProviderGuardTests
{
    private static LoopOptions Options() => new(TimeSpan.FromMilliseconds(10), 2, TimeSpan.FromMilliseconds(100));

    // This assembly has no Loop friendship or Desktop reference: only the supported public seam.
    private sealed class Provider : IFramePlatform, ICanvas
    {
        private int _polls;
        public List<string> Trace { get; } = [];
        public Action? DuringBegin { get; init; }
        public Exception? EndFailure { get; init; }
        public Exception? DisposeFailure { get; init; }
        public PlatformFrame Poll()
        {
            Trace.Add("poll");
            return new(TimeSpan.FromMilliseconds(10), new InputSnapshot([], []), CloseRequested: _polls++ > 0);
        }
        public void Begin() { Trace.Add("begin"); DuringBegin?.Invoke(); }
        public ICanvas Canvas => this;
        public void End() { Trace.Add("end"); if (EndFailure is not null) throw EndFailure; }
        public void Dispose() { Trace.Add("dispose"); if (DisposeFailure is not null) throw DisposeFailure; }
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) { }
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(" ")]
    [DataRow("valid provider state")]
    public void TrueConditionReturnsWithoutException(string? message) => FrameApi.Require(true, message);

    [TestMethod]
    [DataRow(null, "Invalid frame API use.")]
    [DataRow(" \t", "Invalid frame API use.")]
    [DataRow("  invalid backend sequence  ", "  invalid backend sequence  ")]
    public void FalseConditionThrowsExactEngineTypeAndMessage(string? input, string expected)
    {
        var error = Assert.ThrowsExactly<FrameApiException>(() => FrameApi.Require(false, input));
        Assert.AreEqual(expected, error.Message);
        Assert.IsNull(error.InnerException);
    }

    [TestMethod]
    public void CoreHasNoConcreteProviderFriendAndMarkerConstructorStaysInternal()
    {
        var friends = typeof(FrameApiException).Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false)
            .Cast<InternalsVisibleToAttribute>().Select(x => x.AssemblyName).ToArray();
        Assert.IsFalse(friends.Any(name => name.Split(',')[0] == "Desktop"));
        Assert.HasCount(0, typeof(FrameApiException).GetConstructors());
        Assert.IsTrue(typeof(FrameApiException).IsSealed);
    }

    [TestMethod]
    public void PublicNonfriendProviderReportsGuardOnlyAfterOwnedCleanup()
    {
        var provider = new Provider { DuringBegin = () => FrameApi.Require(false, "provider lifecycle violation") };
        var error = Assert.ThrowsExactly<FrameApiException>(() => FrameLoop.Run(() => provider, Options(), _ => { }, (_, _) => Assert.Fail("Draw must not run.")));
        Assert.AreEqual("provider lifecycle violation", error.Message);
        CollectionAssert.AreEqual(new[] { "poll", "begin", "dispose" }, provider.Trace);
    }

    [TestMethod]
    public void PublicGuardPreservesNestedDrawEndAndDisposeOrder()
    {
        var end = new Exception("end"); var cleanup = new Exception("dispose");
        var provider = new Provider { EndFailure = end, DisposeFailure = cleanup };
        var all = Assert.ThrowsExactly<AggregateException>(() => FrameLoop.Run(() => provider, Options(), _ => { },
            (_, _) => FrameApi.Require(false, "invalid drawing use")));
        Assert.HasCount(2, all.InnerExceptions);
        var drawing = (AggregateException)all.InnerExceptions[0];
        Assert.HasCount(2, drawing.InnerExceptions);
        Assert.IsInstanceOfType<FrameApiException>(drawing.InnerExceptions[0]);
        Assert.AreEqual("invalid drawing use", drawing.InnerExceptions[0].Message);
        Assert.AreSame(end, drawing.InnerExceptions[1]);
        Assert.AreSame(cleanup, all.InnerExceptions[1]);
        CollectionAssert.AreEqual(new[] { "poll", "begin", "end", "dispose" }, provider.Trace);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void OrdinaryFailuresAreNotReclassifiedByArbitraryInnerExceptions(bool wrapMarker)
    {
        FrameApiException? marker = null;
        try { FrameApi.Require(false); } catch (FrameApiException error) { marker = error; }
        if (wrapMarker) Assert.IsNotNull(marker);
        Exception ordinary = wrapMarker ? new ApplicationException("consumer failure", marker) : new InvalidOperationException("consumer failure");
        var provider = new Provider();
        var report = FrameLoop.Run(() => provider, Options(), _ => throw ordinary, (_, _) => Assert.Fail("Draw must not run."));
        Assert.AreEqual(ExecutionOutcome.Failed, report.Outcome);
        Assert.AreSame(ordinary, report.PrimaryError);
        Assert.HasCount(0, report.CleanupErrors);
        CollectionAssert.AreEqual(new[] { "poll", "dispose" }, provider.Trace);
    }

    [TestMethod]
    public void GuardHasNoCreatingThreadAffinityOrSharedState()
    {
        Exception? workerError = null;
        var worker = new Thread(() =>
        {
            try { FrameApi.Require(true); FrameApi.Require(false, "worker guard"); }
            catch (Exception error) { workerError = error; }
        });
        worker.Start(); worker.Join();
        Assert.IsInstanceOfType<FrameApiException>(workerError);
        Assert.AreEqual("worker guard", workerError.Message);
        FrameApi.Require(true);
        var mainError = Assert.ThrowsExactly<FrameApiException>(() => FrameApi.Require(false, "main guard"));
        Assert.AreNotSame(workerError, mainError);
    }
}
