using System.Text.RegularExpressions;
using MeadowRun.Art;
using MeadowRun.Scenes;
using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Kernel;
using MicroEngine.Testing;

namespace MeadowRun.Tests;

/// <summary>
/// Simulation determinism: every script of the game runs twice headless with a per-step state trace; both runs must
/// record the same state at every fixed step, drawing must never change it, and the game's own state must take part.
/// </summary>
[TestClass]
public sealed partial class StateTraceTests
{
    /// <summary>Every script of the game with its starting point: the developer variables its header declares.</summary>
    public static IEnumerable<object[]> Scenarios =>
    [
        // The browser and desktop scenarios (web/tests/scenarios.json).
        ["menus.script", ""],
        ["smoke.script", ""],
        ["showcase.script", "MEADOW_START=level MEADOW_FORM=bloom"],
        ["end.script", "MEADOW_START=credits"],
        // The other scripts, started as their headers say.
        ["level1.script", "MEADOW_START=level"],
        ["sheet.script", "MEADOW_START=sheet"],
        ["boss.script", "MEADOW_START=level MEADOW_LEVEL=5 MEADOW_FORM=ember"],
        ["tour.script", "MEADOW_START=level MEADOW_LEVEL=2"],
        ["touch.script", "MEADOW_START=level"],
    ];

    /// <summary>The launch options of <paramref name="variables"/> (NAME=value pairs), read as a host reads them.</summary>
    private static LaunchOptions Options(string variables)
    {
        var values = variables.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);
        return LaunchOptions.From(new HostEnvironment(HostKind.Test) { Variables = name => values.GetValueOrDefault(name) });
    }

    private static TracedRun RunOnce(string script, string variables)
    {
        string save = Path.Combine(Path.GetTempPath(), "meadowrun-state-trace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(save);
        try
        {
            return StateTraceRuns.Run(new MeadowGame(save, Options(variables)), StateTraceRuns.Script("games/MeadowRun/scripts/" + script));
        }
        finally
        {
            Directory.Delete(save, recursive: true);
        }
    }

    [GeneratedRegex(@"^\s*(\d+)\s+quit\b", RegexOptions.Multiline)]
    private static partial Regex QuitLine();

    private static long QuitUpdate(string script)
    {
        var text = File.ReadAllText(Path.Combine(StateTraceRuns.RepositoryRoot(), "games/MeadowRun/scripts", script));
        return long.Parse(QuitLine().Match(text).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void TwoRunsOfAScriptRecordTheSameStateAtEveryStep(string script, string variables)
    {
        var first = RunOnce(script, variables);
        var second = RunOnce(script, variables);
        StateTraceRuns.AssertIdentical(first.Trace, second.Trace, script);
        StateTraceRuns.AssertDrawingChangesNothing(first, script);
        Assert.AreEqual(QuitUpdate(script), first.Result.Updates, $"{script}: the run reaches the script's quit update");
    }

    /// <summary>Every script in the game's folder has a declared starting point here, so none is left out of the check.</summary>
    [TestMethod]
    public void EveryScriptHasAStartingPoint()
    {
        var scripts = Directory.GetFiles(Path.Combine(StateTraceRuns.RepositoryRoot(), "games/MeadowRun/scripts"), "*.script").Select(Path.GetFileName).Order().ToArray();
        var covered = Scenarios.Select(row => (string)row[0]).Order().ToArray();
        CollectionAssert.AreEqual(scripts, covered);
    }

    /// <summary>
    /// The game's own state is live in the trace: in the showcase (a Bloom Pip running through level 1 and collecting
    /// coins) the <c>game</c> section changes between steps, which only the session's score and coins can do.
    /// </summary>
    [TestMethod]
    public void TheGameSectionFollowsTheSession()
    {
        var run = RunOnce("showcase.script", "MEADOW_START=level MEADOW_FORM=bloom");
        int changes = 0;
        for (int i = 1; i < run.Trace.Steps.Count; i++)
            if (Section(run.Trace.Steps[i], "game") != Section(run.Trace.Steps[i - 1], "game")) changes++;
        Assert.IsTrue(changes > 0, "the game section never changed while Pip collected coins");
    }

    /// <summary>
    /// The play scene's own contribution follows Pip: holding right moves him, and its hash (without the engine's part of
    /// the scene, such as its age) changes from one step to the next.
    /// </summary>
    [TestMethod]
    public void ThePlaySceneHashFollowsPip()
    {
        string save = Path.Combine(Path.GetTempPath(), "meadowrun-state-trace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(save);
        try
        {
            var game = new MeadowGame(save, new LaunchOptions("level"));
            game.InitializeForTest(new HeadlessPlatform());
            for (int i = 0; i < 5; i++) game.StepForTest();
            var play = (PlayScene)game.Scenes.Current!;
            game.Input.SetForced("right", true);
            var hash = new StateHasher();
            play.HashState(hash);
            ulong before = hash.Value;
            float x = play.Stage.Player.Position.X;
            for (int i = 0; i < 10; i++) game.StepForTest();
            hash.Reset();
            play.HashState(hash);
            Assert.AreNotEqual(x, play.Stage.Player.Position.X, "Pip moved");
            Assert.AreNotEqual(before, hash.Value, "the play scene's hash follows Pip");
        }
        finally
        {
            Directory.Delete(save, recursive: true);
        }
    }

    private static ulong Section(StepHash step, string name) => step.Sections.Single(s => s.Name == name).Hash;
}
