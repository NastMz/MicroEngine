using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Testing;

namespace CourtyardKey.Tests;

/// <summary>
/// Simulation determinism: every demo script runs twice from the same start and must record the same per-step state
/// hash (engine sections plus the game's own state), and drawing must never change that hash.
/// </summary>
[TestClass]
public sealed class StateTraceTests
{
    /// <summary>
    /// Each script with the start it runs from (the <c>COURTYARD_START</c> value of <c>web/tests/scenarios.json</c>, or
    /// the start its header names). <c>touch.script</c> is left out: it drives on-screen touches on a phone layout.
    /// </summary>
    public static IEnumerable<object?[]> Scenarios =>
    [
        // web/tests/scenarios.json
        ["title.script", null],
        ["opening.script", null],
        ["fight.script", "road"],
        ["boss.script", "boss"],
        ["ending.script", "ending"],
        // The other scripts, by the start their header names.
        ["gallery.script", "channel"],
        ["showcase.script", "blocks"],
        ["swing.script", "dungeon"],
        ["shop.script", "town"],
        ["gameover.script", "ruins"],
        // No header: the browser checks run it from the title (no start).
        ["look.script", null],
    ];

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void TwoRunsOfAScriptRecordTheSameStateTrace(string script, string? start)
    {
        string path = $"games/CourtyardKey/scripts/{script}";
        long quit = QuitUpdate(path);

        var first = RunOnce(path, start);
        var second = RunOnce(path, start);

        string what = $"{script} (start {start ?? "title"})";
        StateTraceRuns.AssertIdentical(first.Trace, second.Trace, what);
        StateTraceRuns.AssertDrawingChangesNothing(first, what);
        Assert.AreEqual(quit, first.Result.Updates, $"{what}: the run stopped before the script's quit");
    }

    /// <summary>
    /// The game's own contribution is live: changing only gameplay state (the hero's position, the progress, the room's
    /// generator, an enemy's health) between two hashes of the same step changes the play scene's section and nothing
    /// in the engine's sections, only the play scene's section that holds it. Without the game's overrides the play
    /// scene hashes only engine state (its age, coroutines and tweens).
    /// </summary>
    [TestMethod]
    public void GameplayStateChangesThePlaySceneSection()
    {
        using var h = new Harness("road");
        h.Step(30);
        var changes = new (string What, string Section, Action Change)[]
        {
            ("hero position", "play.hero", () => h.Play.Hero.Position += new Vector2(1, 0)),
            ("hero health", "play.hero", () => h.Play.Hero.Health.TryDamage(1, 0)),
            ("progress gems", "play.progress", () => h.Play.Progress.AddGems(1)),
            ("progress flag", "play.progress", () => h.Play.Progress.Set("test:flag")),
            ("room generator", "play.progress", () => h.Play.Rng.Next()),
            ("cut bush", "play.room", () => h.Play.Room.SetTile(0, 0, h.Play.Room.TileAt(0, 0) == Content.Tile.Path ? Content.Tile.Grass : Content.Tile.Path)),
            ("enemy health", "play.actors", () => h.Play.Actors.OfType<Entities.Enemy>().First().Health.TryDamage(1, 0)),
        };
        foreach (var (what, section, change) in changes)
        {
            var before = h.Game.ComputeStateHash(sections: true);
            change();
            var after = h.Game.ComputeStateHash(sections: true);
            var differing = before.Sections.Zip(after.Sections).Where(p => p.First.Hash != p.Second.Hash).Select(p => p.First.Name).ToArray();
            CollectionAssert.AreEqual(new[] { section }, differing, $"{what}: sections that changed: {string.Join(", ", differing)}");
        }
    }

    private static TracedRun RunOnce(string script, string? start)
    {
        string saves = Path.Combine(Path.GetTempPath(), "courtyard-state-trace", Guid.NewGuid().ToString("N"));
        try
        {
            return StateTraceRuns.Run(new CourtyardKeyGame(saves, start), StateTraceRuns.Script(script));
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }

    /// <summary>The update of the script's <c>quit</c> command.</summary>
    private static long QuitUpdate(string script)
    {
        string text = File.ReadAllText(Path.Combine(StateTraceRuns.RepositoryRoot(), script));
        var quit = text.Split('\n').Select(l => l.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Single(parts => parts.Length == 2 && parts[1] == "quit");
        return long.Parse(quit[0], System.Globalization.CultureInfo.InvariantCulture);
    }
}
