extern alias original;

using Arch1.Scene;
using CourtyardKey.Entities;
using CourtyardKey.Scenes;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Testing;

namespace CourtyardKey.Tests;

/// <summary>
/// P7: the port must simulate exactly what the original simulates. Every script of the state trace check runs once on
/// the original game and once on the port; their per-step state hashes (every section, the play scene's included)
/// must be identical, for each attachment storage that keeps attach order.
/// </summary>
[TestClass]
public sealed class PortEquivalenceTests
{
    public static IEnumerable<object?[]> Runs =>
        from s in StateTraceTests.Scenarios
        from storage in new[] { AttachmentStorage.DenseStable, AttachmentStorage.PerEntityDictionary }
        select new object?[] { s[0], s[1], storage };

    [TestMethod]
    [DynamicData(nameof(Runs))]
    public void ThePortRecordsTheOriginalsStateTrace(string script, string? start, AttachmentStorage storage)
    {
        var original = RunOriginal(script, start);
        var port = RunPort(script, start, storage);

        string what = $"{script} (start {start ?? "title"}, {storage})";
        Assert.AreEqual(original.Trace.Steps.Count, port.Trace.Steps.Count, $"{what}: step count");
        Assert.IsNull(StateTrace.FirstDivergence(original.Trace, port.Trace), $"{what}: {StateTrace.FirstDivergence(original.Trace, port.Trace)}");
        StateTraceRuns.AssertDrawingChangesNothing(port, what);
    }

    /// <summary>
    /// Iteration order is behaviour: with the swap-remove store (order lost on removal) some script must diverge, which is
    /// why the contract freezes attach order. Of the eleven scripts only gameover.script catches it (P7 probe: it diverges
    /// at update 248 in play.actors); the unit tests pin the order directly.
    /// </summary>
    [TestMethod]
    public void SwapRemoveStorageChangesTheSimulation()
    {
        var original = RunOriginal("gameover.script", "ruins");
        var port = RunPort("gameover.script", "ruins", AttachmentStorage.DenseSwapRemove);
        var divergence = StateTrace.FirstDivergence(original.Trace, port.Trace);
        Assert.IsNotNull(divergence, "Losing attach order changed nothing in this script.");
        Console.WriteLine($"swap-remove: {divergence}");
    }

    /// <summary>A scroll leaves the old room's actors destroyed in the world, not only out of the lists.</summary>
    [TestMethod]
    public void AScrollReleasesThePreviousRoomsEntities()
    {
        using var h = new Harness("road");
        h.Step(40);
        int before = h.Play.Objects.Count;
        h.WalkToTile(10, 4.8f);
        h.Platform.Keys.Set(MicroEngine.Input.Key.Left, true);
        h.WaitUntil(() => h.Play.Mode == PlayMode.Scroll, 400, "scroll start");
        Assert.IsTrue(h.Play.LeavingActors.Count > 0, "The old room's actors are drawn while scrolling.");
        h.WaitUntil(() => h.Play.Mode == PlayMode.Play, 120, "scroll end");
        h.Platform.Keys.Set(MicroEngine.Input.Key.Left, false);
        h.Step();

        int actors = 0;
        foreach (var _ in h.Play.Objects.All<Actor>()) actors++;
        Assert.AreEqual(h.Play.Actors.Count + 1, actors, "Only the new room's actors and the hero are left in the world.");
        Assert.AreEqual(actors + 1, h.Play.Objects.Count, "Plus the camera entity.");
        Assert.IsTrue(before > 0);
    }

    internal static TracedRun RunOriginal(string script, string? start) =>
        Run(saves => new original::CourtyardKey.CourtyardKeyGame(saves, start), script);

    internal static TracedRun RunPort(string script, string? start, AttachmentStorage storage)
    {
        StorageChoice.Override = storage;
        try { return Run(saves => new CourtyardKeyGame(saves, start), script); }
        finally { StorageChoice.Override = null; }
    }

    private static TracedRun Run(Func<string, Game> create, string script)
    {
        string saves = Path.Combine(Path.GetTempPath(), "courtyard-p7", Guid.NewGuid().ToString("N"));
        try { return StateTraceRuns.Run(create(saves), StateTraceRuns.Script($"games/CourtyardKey/scripts/{script}")); }
        finally { if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true); }
    }
}
