using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroEngine.Testing;

/// <summary>
/// Checks that code makes no garbage, exactly, while the rest of the suite runs in parallel. Shared by the engine's
/// and the games' test projects (linked source).
/// </summary>
/// <remarks>
/// <para>Bytes are counted with <see cref="GC.GetAllocatedBytesForCurrentThread"/>: what the calling thread allocates,
/// not what other tests allocate. The code under test must run on the calling thread.</para>
/// <para>That count is not exact while other threads cause collections. When a background (concurrent) collection
/// runs while other threads allocate, the calling thread's count can grow by up to one allocation quantum (about
/// 8 KB) although it allocated nothing: the unused rest of its allocation buffer is counted when the collector takes
/// the buffer back. A thread that allocates nothing gets no new buffer, so this happens once and not again until the
/// thread allocates. With concurrent collection off it does not happen. A whole test project run in parallel makes such
/// collections, so a single measured window fails now and then.</para>
/// <para>The action is therefore warmed up first, so first-call work (compilation, static initialisation, caches the
/// code fills once) is outside the measurement, and then measured in several rounds of several calls each. Garbage made
/// on every call, or on any regular fraction of calls, shows in every round, so the check fails on it; the collector's
/// one-off count shows in one round at most and does not. A round needs enough calls for regular garbage to show in
/// it.</para>
/// </remarks>
internal static class AllocationAssert
{
    /// <summary>Fails unless <paramref name="action"/> allocates nothing in at least one round.</summary>
    /// <param name="action">The code under test; it runs on the calling thread.</param>
    /// <param name="warmup">Calls made before measuring.</param>
    /// <param name="calls">Calls per round.</param>
    /// <param name="rounds">Measured rounds; at least 2.</param>
    /// <param name="because">What the check protects, for the failure message.</param>
    public static void None(Action action, int warmup = 10, int calls = 100, int rounds = 5, string? because = null)
    {
        var bytes = BytesPerRound(action, warmup, calls, rounds);
        if (bytes.Min() != 0)
            Assert.Fail($"{(because is null ? "" : because + ": ")}allocated in every round of {calls} calls (bytes per round: {string.Join(", ", bytes)})");
    }

    /// <summary>
    /// Bytes the calling thread allocated in each of <paramref name="rounds"/> rounds of <paramref name="calls"/> calls
    /// of <paramref name="action"/>, after <paramref name="warmup"/> calls that are not measured.
    /// </summary>
    public static long[] BytesPerRound(Action action, int warmup = 10, int calls = 100, int rounds = 5)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegative(warmup);
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rounds, 2);
        for (int i = 0; i < warmup; i++) action();
        var bytes = new long[rounds];
        for (int round = 0; round < rounds; round++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < calls; i++) action();
            bytes[round] = GC.GetAllocatedBytesForCurrentThread() - before;
        }
        return bytes;
    }
}
