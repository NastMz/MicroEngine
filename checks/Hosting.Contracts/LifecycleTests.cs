using MicroEngine.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hosting.Contracts;

[TestClass]
public sealed class LifecycleTests
{
    private sealed class Resource(Action release) : IDisposable
    {
        public void Dispose() => release();
    }

    [TestMethod]
    public void NormalRunAcquiresWorksAndReleasesInReverseOrder()
    {
        var trace = new List<string>();
        var config = new EngineConfiguration(
            () => { trace.Add("A"); return new Resource(() => trace.Add("release A")); },
            () => { trace.Add("B"); return new Resource(() => trace.Add("release B")); });
        var result = config.CreateSession().Run(_ => trace.Add("work"));
        CollectionAssert.AreEqual(new[] { "A", "B", "work", "release B", "release A" }, trace);
        Assert.AreEqual(ExecutionOutcome.Succeeded, result.Outcome);
        Assert.IsNull(result.PrimaryError);
        Assert.HasCount(0, result.CleanupErrors);
    }

    [TestMethod]
    [DataRow("init")]
    [DataRow("work")]
    [DataRow("cleanup")]
    [DataRow("multiple")]
    public void FailureAlwaysCleansOwnedResourcesAndPreservesErrorOrder(string scenario)
    {
        var trace = new List<string>();
        var original = new Exception("primary");
        var cleanupB = new Exception("B release");
        var cleanupA = new Exception("A release");
        var config = new EngineConfiguration(
            () => new Resource(() => { trace.Add("release A"); if (scenario == "multiple") throw cleanupA; }),
            () =>
            {
                if (scenario == "init") { trace.Add("partial B cleanup"); throw original; }
                return new Resource(() => { trace.Add("release B"); if (scenario is "cleanup" or "multiple") throw cleanupB; });
            });
        var result = config.CreateSession().Run(_ =>
        {
            trace.Add("work");
            if (scenario is "work" or "multiple") throw original;
        });
        Assert.AreEqual(ExecutionOutcome.Failed, result.Outcome);
        Assert.AreSame(scenario == "cleanup" ? cleanupB : original, result.PrimaryError);
        CollectionAssert.AreEqual(scenario == "init"
            ? new[] { "partial B cleanup", "release A" }
            : new[] { "work", "release B", "release A" }, trace);
        CollectionAssert.AreEqual(scenario == "multiple" ? new[] { cleanupB, cleanupA } : Array.Empty<Exception>(), result.CleanupErrors.ToArray());
    }

    [TestMethod]
    [DataRow("before")]
    [DataRow("factory")]
    [DataRow("work")]
    [DataRow("dispose")]
    public void TerminationIsCooperativeSingleUseAndNeverRepeatsCleanup(string boundary)
    {
        var trace = new List<string>();
        ExecutionSession? session = null;
        var resourceAlive = false;
        var config = new EngineConfiguration(
            () => { resourceAlive = true; trace.Add("A"); return new Resource(() => { resourceAlive = false; trace.Add("release A"); }); },
            () => { trace.Add("B"); if (boundary == "factory") session!.Stop(); return new Resource(() => trace.Add("release B")); },
            () => { trace.Add("C"); return new Resource(() => trace.Add("release C")); });
        session = config.CreateSession();
        if (boundary == "before") session.Stop();
        var result = session.Run(active =>
        {
            trace.Add("work");
            if (boundary == "dispose") active.Dispose(); else active.Stop();
            Assert.IsTrue(resourceAlive, "Cleanup must wait for the active callback.");
        });
        Assert.AreEqual(ExecutionOutcome.Stopped, result.Outcome);
        CollectionAssert.AreEqual(boundary switch
        {
            "before" => Array.Empty<string>(),
            "factory" => new[] { "A", "B", "release B", "release A" },
            _ => new[] { "A", "B", "C", "work", "release C", "release B", "release A" }
        }, trace);
        var snapshot = trace.ToArray();
        session.Stop(); session.Stop(); session.Dispose(); session.Dispose();
        Assert.Throws<InvalidOperationException>(() => session.Run(_ => trace.Add("unexpected")));
        CollectionAssert.AreEqual(snapshot, trace);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void EscapedApiMisuseThrowsAfterCleanupInsteadOfReturningFailure(bool cleanupFails)
    {
        var trace = new List<string>();
        var cleanup = new Exception("release failed");
        var session = new EngineConfiguration(() => new Resource(() =>
        {
            trace.Add("release");
            if (cleanupFails) throw cleanup;
        })).CreateSession();
        var error = Assert.Throws<InvalidOperationException>(() => session.Run(active => active.Run(_ => trace.Add("nested"))));
        CollectionAssert.AreEqual(new[] { "release" }, trace);
        if (cleanupFails)
        {
            var diagnostics = error.InnerException as AggregateException;
            Assert.IsNotNull(diagnostics);
            Assert.HasCount(2, diagnostics.InnerExceptions);
            Assert.IsInstanceOfType<InvalidOperationException>(diagnostics.InnerExceptions[0]);
            Assert.AreSame(cleanup, diagnostics.InnerExceptions[1]);
        }
        session.Dispose();
        CollectionAssert.AreEqual(new[] { "release" }, trace);
    }

    [TestMethod]
    [DataRow("run")]
    [DataRow("stop")]
    [DataRow("dispose")]
    public void ForeignThreadRejectsBeforeMutatingTheOwnerSession(string operation)
    {
        var workCount = 0;
        var releaseCount = 0;
        var session = new EngineConfiguration(() => new Resource(() => releaseCount++)).CreateSession();
        Exception? rejection = null;
        var other = new Thread(() =>
        {
            try
            {
                if (operation == "run") session.Run(_ => workCount++);
                else if (operation == "stop") session.Stop();
                else session.Dispose();
            }
            catch (Exception error) { rejection = error; }
        });
        other.Start();
        Assert.IsTrue(other.Join(TimeSpan.FromSeconds(5)), "Foreign-thread call did not finish.");
        Assert.IsInstanceOfType<InvalidOperationException>(rejection);
        Assert.AreEqual(0, workCount);
        Assert.AreEqual(0, releaseCount);
        Assert.AreEqual(ExecutionOutcome.Succeeded, session.Run(_ => workCount++).Outcome);
        Assert.AreEqual(1, workCount);
        Assert.AreEqual(1, releaseCount);
    }

    [TestMethod]
    public void ConfigurationSnapshotsFactoriesAndCreatesFreshSessions()
    {
        var releases = 0;
        Func<IDisposable>[] factories = [() => new Resource(() => releases++)];
        var config = new EngineConfiguration(factories);
        factories[0] = () => throw new Exception("changed input");
        Assert.AreEqual(ExecutionOutcome.Succeeded, config.CreateSession().Run(_ => { }).Outcome);
        Assert.AreEqual(ExecutionOutcome.Succeeded, config.CreateSession().Run(_ => { }).Outcome);
        Assert.AreEqual(2, releases);
    }

    [TestMethod]
    public void InvalidArgumentsDoNotConsumeTheSessionAndDisposedRunDoesNotAcquire()
    {
        Assert.Throws<ArgumentNullException>(() => new EngineConfiguration(null!));
        Assert.Throws<ArgumentException>(() => new EngineConfiguration([null!]));
        var session = new EngineConfiguration().CreateSession();
        Assert.Throws<ArgumentNullException>(() => session.Run(null!));
        Assert.AreEqual(ExecutionOutcome.Succeeded, session.Run(_ => { }).Outcome);
        var acquisitions = 0;
        var disposed = new EngineConfiguration(() => { acquisitions++; return new Resource(() => { }); }).CreateSession();
        disposed.Dispose(); disposed.Dispose();
        Assert.Throws<InvalidOperationException>(() => disposed.Run(_ => { }));
        Assert.AreEqual(0, acquisitions);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InvalidFactoryResultFailsWithoutDoubleCleanup(bool duplicate)
    {
        var releases = 0;
        var calls = 0;
        var resource = new Resource(() => releases++);
        var config = duplicate
            ? new EngineConfiguration(() => resource, () => resource, () => { calls++; return resource; })
            : new EngineConfiguration(() => null!);
        var result = config.CreateSession().Run(_ => calls++);
        Assert.AreEqual(ExecutionOutcome.Failed, result.Outcome);
        Assert.IsInstanceOfType<InvalidOperationException>(result.PrimaryError);
        Assert.AreEqual(duplicate ? 1 : 0, releases);
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public void CleanupOnlyFailureUsesFirstErrorAndImmutableOrderedSecondaries()
    {
        var errorA = new Exception("A");
        var errorB = new Exception("B");
        var result = new EngineConfiguration(
            () => new Resource(() => throw errorA),
            () => new Resource(() => throw errorB)).CreateSession().Run(_ => { });
        Assert.AreEqual(ExecutionOutcome.Failed, result.Outcome);
        Assert.AreSame(errorB, result.PrimaryError);
        CollectionAssert.AreEqual(new[] { errorA }, result.CleanupErrors.ToArray());
        Assert.Throws<NotSupportedException>(() => ((IList<Exception>)result.CleanupErrors).Add(new Exception()));
    }

    [TestMethod]
    public void ConsumerInvalidOperationRemainsExecutionFailureAndStopDoesNotHideCleanupFailure()
    {
        var ordinary = new InvalidOperationException("consumer failure");
        var result = new EngineConfiguration().CreateSession().Run(_ => throw ordinary);
        Assert.AreEqual(ExecutionOutcome.Failed, result.Outcome);
        Assert.AreSame(ordinary, result.PrimaryError);
        var cleanup = new Exception("cleanup");
        result = new EngineConfiguration(() => new Resource(() => throw cleanup)).CreateSession().Run(active => active.Stop());
        Assert.AreEqual(ExecutionOutcome.Failed, result.Outcome);
        Assert.AreSame(cleanup, result.PrimaryError);
    }

    [TestMethod]
    public void DisposeDuringFactoryCommitsCurrentResourceAndSkipsLaterWork()
    {
        ExecutionSession? session = null;
        var trace = new List<string>();
        var config = new EngineConfiguration(
            () => { session!.Dispose(); trace.Add("factory returned"); return new Resource(() => trace.Add("release")); },
            () => { trace.Add("unexpected factory"); return new Resource(() => { }); });
        session = config.CreateSession();
        Assert.AreEqual(ExecutionOutcome.Stopped, session.Run(_ => trace.Add("unexpected work")).Outcome);
        CollectionAssert.AreEqual(new[] { "factory returned", "release" }, trace);
    }

    [TestMethod]
    public void MisuseEscapingCleanupDoesNotPreventOtherReleaseAttempts()
    {
        var trace = new List<string>();
        ExecutionSession? session = null;
        session = new EngineConfiguration(
            () => new Resource(() => trace.Add("A")),
            () => new Resource(() => session!.Run(_ => { }))).CreateSession();
        Assert.Throws<InvalidOperationException>(() => session.Run(_ => { }));
        CollectionAssert.AreEqual(new[] { "A" }, trace);
    }

    [TestMethod]
    [DataRow("nested", false)]
    [DataRow("nested", true)]
    [DataRow("configuration", false)]
    [DataRow("factories", false)]
    [DataRow("consumer", false)]
    public void ArgumentMisuseIsIdentifiedByEngineOriginAndStillCleans(string source, bool cleanupFails)
    {
        var trace = new List<string>();
        var cleanup = new Exception("cleanup");
        var ordinary = new ArgumentNullException("consumer");
        var session = new EngineConfiguration(() => new Resource(() =>
        {
            trace.Add("release");
            if (cleanupFails) throw cleanup;
        })).CreateSession();
        void Work(ExecutionSession active)
        {
            if (source == "nested") active.Run(null!);
            else if (source == "configuration") _ = new EngineConfiguration(null!);
            else if (source == "factories") _ = new EngineConfiguration([null!]);
            else throw ordinary;
        }
        if (source == "consumer")
        {
            var report = session.Run(Work);
            Assert.AreEqual(ExecutionOutcome.Failed, report.Outcome);
            Assert.AreSame(ordinary, report.PrimaryError);
        }
        else
        {
            var error = Assert.Throws<ArgumentException>(() => session.Run(Work));
            if (cleanupFails)
            {
                var causes = error.InnerException as AggregateException;
                Assert.IsNotNull(causes);
                Assert.HasCount(2, causes.InnerExceptions);
                Assert.IsInstanceOfType<ArgumentNullException>(causes.InnerExceptions[0]);
                Assert.AreSame(cleanup, causes.InnerExceptions[1]);
            }
            else if (source != "factories") Assert.IsInstanceOfType<ArgumentNullException>(error);
        }
        CollectionAssert.AreEqual(new[] { "release" }, trace);
        session.Dispose();
        CollectionAssert.AreEqual(new[] { "release" }, trace);
    }

    [TestMethod]
    [DataRow("success", 0)]
    [DataRow("callback-failure", 1)]
    [DataRow("stopped", 0)]
    [DataRow("unknown", 2)]
    public void ExternalConsumerExposesDocumentedOutputAndExitCode(string scenario, int expectedExit)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var dll = Path.Combine(repository, "examples", "LifecycleProbe", "bin", configuration, "net10.0", "LifecycleProbe.dll");
        var start = new System.Diagnostics.ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        start.ArgumentList.Add(dll); start.ArgumentList.Add("--scenario"); start.ArgumentList.Add(scenario);
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(10000)) { process.Kill(entireProcessTree: true); Assert.Fail("Consumer timeout."); }
        Assert.AreEqual(expectedExit, process.ExitCode);
        Assert.AreEqual(string.Empty, errors.GetAwaiter().GetResult());
        var expected = scenario switch
        {
            "success" => new[] { "acquire:A", "acquire:B", "work", "release:B", "release:A", "outcome:Succeeded" },
            "callback-failure" => new[] { "acquire:A", "acquire:B", "work", "release:B", "release:A", "outcome:Failed", "primary:ApplicationException" },
            "stopped" => new[] { "outcome:Stopped" },
            _ => new[] { "usage: LifecycleProbe --scenario <success|callback-failure|stopped>" }
        };
        CollectionAssert.AreEqual(expected, output.GetAwaiter().GetResult().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
    }
}
