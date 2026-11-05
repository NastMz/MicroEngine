using System.Text.RegularExpressions;
using MicroEngine.Kernel;
using MicroEngine.Testing;
using NeonRift.Gameplay;

namespace NeonRift.Tests;

/// <summary>
/// Simulation determinism: every script of the game, run twice from the same start, records the same per-step state
/// hash (engine sections plus the game's own <c>HashState</c>), drawing never changes it, and the game's contribution
/// follows its gameplay.
/// </summary>
[TestClass]
public sealed partial class StateTraceTests
{
    /// <summary>
    /// Every script of <c>games/NeonRift/scripts</c> with the start its environment stands for: the three scenarios of
    /// <c>web/tests/scenarios.json</c> first, then the capture scripts their headers (and the README) describe.
    /// <c>touch.script</c> starts in stage 1 (<c>RIFT_START=stage</c>, its header); its touches go through the on-screen
    /// controls' hit testing, which the headless platform has too.
    /// </summary>
    public static IEnumerable<object[]> Scenarios =>
    [
        ["menus", "title", 1, false, false],
        ["stage1", "stage", 1, false, false],
        ["ending", "ending", 1, false, false],
        ["poses", "stage", 1, true, false],
        ["tour", "stage", 1, true, false],
        ["autotour", "stage", 2, true, true],
        ["touch", "stage", 1, false, false],
    ];

    private static LaunchOptions Options(string start, int stage, bool god, bool auto) => new(start, stage, God: god, Auto: auto);

    private static string ScriptPath(string name) => $"games/NeonRift/scripts/{name}.script";

    /// <summary>Runs the script once with a fresh save folder, deleted afterwards.</summary>
    private static TracedRun RunOnce(string name, LaunchOptions options)
    {
        string saves = Path.Combine(Path.GetTempPath(), "neonrift-state-trace", Guid.NewGuid().ToString("N"));
        try
        {
            return StateTraceRuns.Run(new RiftGame(options, saveDirectory: saves), StateTraceRuns.Script(ScriptPath(name)));
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }

    [GeneratedRegex(@"^\s*(\d+)\s+quit\b", RegexOptions.Multiline)]
    private static partial Regex QuitLine();

    private static long QuitUpdate(string name) =>
        long.Parse(QuitLine().Match(File.ReadAllText(Path.Combine(StateTraceRuns.RepositoryRoot(), ScriptPath(name)))).Groups[1].Value);

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void TwoRunsOfAScriptRecordTheSameTrace(string name, string start, int stage, bool god, bool auto)
    {
        var options = Options(start, stage, god, auto);
        var first = RunOnce(name, options);
        var second = RunOnce(name, options);
        StateTraceRuns.AssertIdentical(first.Trace, second.Trace, name);
        StateTraceRuns.AssertDrawingChangesNothing(first, name);
        Assert.AreEqual(QuitUpdate(name), first.Result.Updates, $"{name}: the run did not reach the script's quit");
    }

    /// <summary>
    /// Stage 1's opening (the player runs right, firing at troopers): the session's score (section <c>game</c>) and the
    /// player's state (section <c>stage.player</c> of the play scene) change from one step to the next. Engine state
    /// alone (the scene's age, time, input) would leave both unchanged or absent.
    /// </summary>
    [TestMethod]
    public void TheGamesOwnStateFollowsGameplay()
    {
        var run = RunOnce("stage1", Options("stage", 1, false, false));
        var steps = run.Trace.Steps;

        static ulong? Section(StepHash step, string name) =>
            step.Sections.Where(s => s.Name == name).Select(s => (ulong?)s.Hash).FirstOrDefault();

        bool scoreChanged = false, playerChanged = false;
        for (int i = 1; i < steps.Count; i++)
        {
            scoreChanged |= Section(steps[i], "game") != Section(steps[i - 1], "game");
            playerChanged |= Section(steps[i], "stage.player") is { } now && Section(steps[i - 1], "stage.player") is { } before && now != before;
        }
        Assert.IsTrue(scoreChanged, "the game section never changed: the session (score, lives) is not hashed");
        Assert.IsTrue(playerChanged, "the stage.player section never changed: the player's state is not hashed");
    }

    /// <summary>
    /// The touch script's starting point really plays its touches: before FIRE is touched (update 135) the player's
    /// shots section stays as it started; while the stick and FIRE are held, it changes.
    /// </summary>
    [TestMethod]
    public void TheTouchScriptFiresThroughTheOnScreenControls()
    {
        var steps = RunOnce("touch", Options("stage", 1, false, false)).Trace.Steps;

        static ulong Shots(StepHash step) => step.Sections.Single(s => s.Name == "stage.shots").Hash;

        var first = Shots(steps[0]);
        Assert.IsTrue(steps.Where(s => s.Update < 135).All(s => Shots(s) == first), "a shot before any touch");
        Assert.IsTrue(steps.Where(s => s.Update is > 135 and < 180).Any(s => Shots(s) != first), "touching FIRE fired nothing");
    }
}
