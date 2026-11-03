using AssetGallery;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Testing;

namespace Samples.Tests;

/// <summary>The gallery's tour is deterministic: two runs record the same per-step state hash, and drawing never changes it.</summary>
[TestClass]
public sealed class GalleryStateTraceTests
{
    private static InputScript Tour() => InputScript.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "tour.script")));

    #region trace
    /// <summary>Plays the tour headless (one fixed step per frame) and records the state hash after every step.</summary>
    private static StateTrace Record(string captures)
    {
        var game = new GalleryGame();
        var trace = new StateTrace { Sections = true };
        var result = GameRunner.Run(game, HeadlessPlatform.For(game.Settings),
            new RunOptions { Script = Tour(), CaptureDirectory = captures, StateTrace = trace });
        Assert.IsNull(result.Error);
        return trace;
    }

    [TestMethod]
    public void TwoRunsOfTheTourRecordTheSameTrace()
    {
        var captures = Directory.CreateTempSubdirectory("gallery-trace-").FullName;
        var first = Record(captures);
        var second = Record(captures);
        Assert.AreEqual(112, first.Steps.Count, "one hash per update of the tour");
        var divergence = StateTrace.FirstDivergence(first, second);
        Assert.IsNull(divergence, divergence?.ToString()); // e.g. "state diverges at update 41, section 'scene[0]:GalleryScene'"
        Directory.Delete(captures, true);
    }
    #endregion

    [TestMethod]
    public void DrawingTheTourNeverChangesTheHash()
    {
        var run = StateTraceRuns.Run(new GalleryGame(), Tour());
        Assert.AreEqual(112L, run.Result.Updates, "the tour ran to its end");
        StateTraceRuns.AssertIdentical(run.Trace, StateTraceRuns.Run(new GalleryGame(), Tour()).Trace, "gallery tour");
        StateTraceRuns.AssertDrawingChangesNothing(run, "gallery tour");
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
