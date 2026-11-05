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
    /// the start its header names). Every script of the game is here (<see cref="EveryScriptHasAStartingPoint"/>).
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
        // The phone checks' on-screen controls (its header names the start): touches go through the touch layout's own
        // hit testing, which the headless platform has too.
        ["touch.script", "road"],
    ];

    /// <summary>Every script of the game is traced: each one declares the start it runs from in <see cref="Scenarios"/>.</summary>
    [TestMethod]
    public void EveryScriptHasAStartingPoint()
    {
        var scripts = Directory.GetFiles(Path.Combine(StateTraceRuns.RepositoryRoot(), "games/CourtyardKey/scripts"), "*.script")
            .Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray();
        var traced = Scenarios.Select(s => (string)s[0]!).Order(StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(scripts, traced, $"scripts: {string.Join(", ", scripts)}; traced: {string.Join(", ", traced)}");
    }

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
    /// generator, an enemy's health) between two hashes of the same step changes the play scene's section that holds it
    /// and no engine section. The hero and the room's actors are also entities of the scene's world, which the engine
    /// hashes in the scene's own section (<c>scene[0]:PlayScene</c>: transforms and the actors' state), so a change to
    /// them changes that section too; progress and room state are the game's alone.
    /// </summary>
    [TestMethod]
    public void GameplayStateChangesThePlaySceneSection()
    {
        using var h = new Harness("road");
        h.Step(30);
        const string World = "scene[0]:PlayScene";
        var changes = new (string What, string[] Sections, Action Change)[]
        {
            ("hero position", [World, "play.hero"], () => h.Play.Hero.Position += new Vector2(1, 0)),
            ("hero health", [World, "play.hero"], () => h.Play.Hero.Health.TryDamage(1, 0)),
            ("progress gems", ["play.progress"], () => h.Play.Progress.AddGems(1)),
            ("progress flag", ["play.progress"], () => h.Play.Progress.Set("test:flag")),
            ("room generator", ["play.progress"], () => h.Play.Rng.Next()),
            ("cut bush", ["play.room"], () => h.Play.Room.SetTile(0, 0, h.Play.Room.TileAt(0, 0) == Content.Tile.Path ? Content.Tile.Grass : Content.Tile.Path)),
            ("enemy health", [World, "play.actors"], () => h.Play.Actors.OfType<Entities.Enemy>().First().Health.TryDamage(1, 0)),
        };
        foreach (var (what, sections, change) in changes)
        {
            var before = h.Game.ComputeStateHash(sections: true);
            change();
            var after = h.Game.ComputeStateHash(sections: true);
            var differing = before.Sections.Zip(after.Sections).Where(p => p.First.Hash != p.Second.Hash).Select(p => p.First.Name).ToArray();
            CollectionAssert.AreEqual(sections, differing, $"{what}: sections that changed: {string.Join(", ", differing)}");
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
