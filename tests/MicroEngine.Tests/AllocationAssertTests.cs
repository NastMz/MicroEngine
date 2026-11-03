using MicroEngine.Testing;

namespace MicroEngine.Tests;

/// <summary>The shared allocation check: garbage made on every call fails it; one-off work in a single round does not.</summary>
[TestClass]
public sealed class AllocationAssertTests
{
    [TestMethod]
    public void An_action_that_allocates_nothing_passes()
    {
        int calls = 0;
        AllocationAssert.None(() => calls++, warmup: 3, calls: 10, rounds: 4);
        Assert.AreEqual(3 + 10 * 4, calls, "warm-up calls, then every round's calls");
    }

    [TestMethod]
    public void Garbage_made_on_every_call_fails_and_names_each_round()
    {
        object? kept = null;
        var failure = Assert.ThrowsExactly<AssertFailedException>(
            () => AllocationAssert.None(() => kept = new object(), calls: 10, rounds: 3, because: "new objects"));
        StringAssert.Contains(failure.Message, "new objects");
        StringAssert.Contains(failure.Message, "240, 240, 240", "24 bytes per object, 10 calls per round, on a 64-bit runtime");
        Assert.IsNotNull(kept);
    }

    [TestMethod]
    public void Garbage_made_on_some_calls_only_still_fails_when_every_round_sees_it()
    {
        int call = 0;
        object? kept = null;
        Assert.ThrowsExactly<AssertFailedException>(
            () => AllocationAssert.None(() => { if (call++ % 7 == 0) kept = new object(); }, calls: 10, rounds: 3));
        Assert.IsNotNull(kept);
    }

    [TestMethod]
    public void A_one_off_allocation_in_a_single_round_is_not_garbage()
    {
        // A background collection can add the rest of the measuring thread's allocation buffer to its count in any one
        // window; it does not repeat in every round.
        int call = 0;
        object? kept = null;
        AllocationAssert.None(() => { if (call++ == 15) kept = new byte[4096]; }, warmup: 5, calls: 10, rounds: 3);
        Assert.IsNotNull(kept, "the one-off allocation happened inside the second round");
    }

    [TestMethod]
    public void Another_threads_garbage_does_not_fail_the_check()
    {
        // The checks run while the rest of the suite runs in parallel: only the calling thread's bytes are counted.
        using var stop = new ManualResetEventSlim();
        object? theirs = null;
        var other = new Thread(() => { while (!stop.IsSet) theirs = new byte[1024]; }) { IsBackground = true };
        other.Start();
        try
        {
            AllocationAssert.None(() => Thread.SpinWait(1000), calls: 100);
        }
        finally
        {
            stop.Set();
            other.Join();
        }
        Assert.IsNotNull(theirs);
    }

    [TestMethod]
    public void Bytes_per_round_are_counted_for_each_round_after_the_warm_up()
    {
        int call = 0;
        object? kept = null;
        var rounds = AllocationAssert.BytesPerRound(() => { if (call++ < 2) kept = new byte[100]; else kept = new object(); },
            warmup: 2, calls: 5, rounds: 2);
        CollectionAssert.AreEqual(new long[] { 5 * 24, 5 * 24 }, rounds, "the warm-up's arrays are not counted");
        Assert.IsNotNull(kept);
    }
}
