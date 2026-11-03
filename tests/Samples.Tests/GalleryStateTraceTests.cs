using AssetGallery;
using MicroEngine.Core;
using MicroEngine.Testing;

namespace Samples.Tests;

/// <summary>The gallery's tour is deterministic: two runs record the same per-step state hash, and drawing never changes it.</summary>
[TestClass]
public sealed class GalleryStateTraceTests
{
    private static InputScript Tour() => InputScript.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "tour.script")));

    [TestMethod]
    public void TwoRunsOfTheTourRecordTheSameTraceAndDrawingChangesNothing()
    {
        var first = StateTraceRuns.Run(new GalleryGame(), Tour());
        var second = StateTraceRuns.Run(new GalleryGame(), Tour());
        Assert.AreEqual(112L, first.Result.Updates, "the tour ran to its end");
        StateTraceRuns.AssertIdentical(first.Trace, second.Trace, "gallery tour");
        StateTraceRuns.AssertDrawingChangesNothing(first, "gallery tour");
    }

    [TestMethod]
    public void TheSamplingSwitchAndTheLevelScopeArePartOfTheGallerysState()
    {
        var tour = StateTraceRuns.Run(new GalleryGame(), Tour());
        var idle = StateTraceRuns.Run(new GalleryGame(), new InputScript().QuitAfter(112));
        // At update 70 the tour shows nearest sampling and the level scope loaded; the idle run shows neither. The scene's
        // age is the same in both, so only the gallery's own state can tell them apart.
        static ulong Scene(TracedRun run, long update) =>
            run.Trace.Steps.Single(s => s.Update == update).Sections.Single(s => s.Name == "scene[0]:GalleryScene").Hash;
        Assert.AreNotEqual(Scene(idle, 70), Scene(tour, 70));
        Assert.AreEqual(Scene(idle, 30), Scene(tour, 30), "before the first switch the states are the same");
    }
}
