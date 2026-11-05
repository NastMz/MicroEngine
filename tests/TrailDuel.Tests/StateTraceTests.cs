using MicroEngine.Kernel;
using MicroEngine.Testing;

namespace TrailDuel.Tests;

/// <summary>
/// Simulation determinism: every Trail Duel script runs twice headless with a state trace (one hash per fixed step, in
/// sections), and both runs must record the same trace; drawing must never change the hash; and the game's own state
/// (the adventure, the overworld, battles) must take part in it.
/// </summary>
[TestClass]
public sealed class StateTraceTests
{
    /// <summary>
    /// Every script of <c>games/TrailDuel/scripts</c> with its starting point (<c>TRAIL_DEBUG</c>): the scenarios of
    /// <c>web/tests/scenarios.json</c> (title, newgame, master-battle with <c>trainer</c>), then <c>lab-starter</c> (lab),
    /// <c>route-trainer</c> (route), <c>brook-shrine</c> (brook), <c>catch</c> (battle: the wild Mossback of the Hollow
    /// Wood; at update 550 the script opens the bag, throws a Capture Orb at 620, and the creature breaks free) and
    /// <c>touch</c> (town, as its header and the phone checks say; its touches go through the same hit testing headless).
    /// An empty debug start means the title screen. <see cref="EveryScriptHasAStartingPoint"/> keeps the list complete.
    /// </summary>
    public static IEnumerable<object[]> Scenarios =>
    [
        ["title", ""],
        ["newgame", ""],
        ["master-battle", "trainer"],
        ["lab-starter", "lab"],
        ["route-trainer", "route"],
        ["brook-shrine", "brook"],
        ["catch", "battle"],
        ["touch", "town"],
    ];

    [TestMethod]
    public void EveryScriptHasAStartingPoint()
    {
        var scripts = Directory.GetFiles(Path.Combine(StateTraceRuns.RepositoryRoot(), "games/TrailDuel/scripts"), "*.script")
            .Select(Path.GetFileNameWithoutExtension).Order().ToArray();
        CollectionAssert.AreEqual(scripts, Scenarios.Select(s => (string)s[0]).Order().ToArray());
    }

    private static TracedRun RunOnce(string script, string debugStart, out long? quit)
    {
        var input = StateTraceRuns.Script($"games/TrailDuel/scripts/{script}.script");
        quit = input.QuitUpdate;
        string saves = Path.Combine(Path.GetTempPath(), "trailduel-state-trace-" + Guid.NewGuid().ToString("N"));
        try
        {
            return StateTraceRuns.Run(new TrailDuelGame(saves, debugStart), input);
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void TwoRunsOfAScriptRecordTheSameTraceAndDrawingChangesNothing(string script, string debugStart)
    {
        var first = RunOnce(script, debugStart, out long? quit);
        var second = RunOnce(script, debugStart, out _);
        StateTraceRuns.AssertIdentical(first.Trace, second.Trace, script);
        StateTraceRuns.AssertDrawingChangesNothing(first, script);
        Assert.IsNotNull(quit, $"{script}: the script has no quit line.");
        Assert.AreEqual(quit.Value, first.Result.Updates, $"{script}: the run did not reach the script's quit update.");
    }

    /// <summary>
    /// The catch script's starting point is the one it was written for: from the wild battle it opens the bag and throws
    /// one of the six Capture Orbs at the wild creature, which breaks free.
    /// </summary>
    [TestMethod]
    public void TheCatchScriptThrowsAnOrbInTheWildBattle()
    {
        var input = StateTraceRuns.Script("games/TrailDuel/scripts/catch.script");
        string saves = Path.Combine(Path.GetTempPath(), "trailduel-state-trace-" + Guid.NewGuid().ToString("N"));
        try
        {
            var game = new TrailDuelGame(saves, "battle");
            StateTraceRuns.Run(game, input);
            Assert.AreEqual(5, game.State.Bag.Count("orb"), "one orb thrown");
            Assert.AreEqual(0, game.State.Caught.Count(id => id == "mossnail"), "it broke free");
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }

    /// <summary>
    /// The game's own state is live in the hash: walking up Route 1 (route-trainer holds up from update 30) changes the
    /// overworld's player section between consecutive steps, and the adventure (section <c>game</c>) changes too.
    /// </summary>
    [TestMethod]
    public void WalkingChangesThePlayerAndGameSectionsBetweenSteps()
    {
        var run = RunOnce("route-trainer", "route", out _);
        var steps = run.Trace.Steps;
        Assert.IsTrue(Changes(steps, "overworld.player"), "the player's position never changed the hash");
        Assert.IsTrue(Changes(steps, "game"), "the adventure's state never changed the hash");
    }

    /// <summary>The battle's rules (HP, turns, the battle's generator) and its screen take part: master-battle fights turns.</summary>
    [TestMethod]
    public void FightingChangesTheBattleSectionsBetweenSteps()
    {
        var steps = RunOnce("master-battle", "trainer", out _).Trace.Steps;
        Assert.IsTrue(Changes(steps, "battle.rules"), "the battle's rules never changed the hash");
        Assert.IsTrue(Changes(steps, "battle.screen"), "the battle screen never changed the hash");
    }

    private static bool Changes(IReadOnlyList<StepHash> steps, string section)
    {
        for (int i = 1; i < steps.Count; i++)
        {
            ulong? before = Find(steps[i - 1], section), after = Find(steps[i], section);
            if (before is not null && after is not null && before != after) return true;
        }
        return false;
    }

    private static ulong? Find(StepHash step, string section)
    {
        foreach (var s in step.Sections) if (s.Name == section) return s.Hash;
        return null;
    }
}
