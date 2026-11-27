using System.Globalization;
using System.Text;
using MeadowRun.Content;
using MeadowRun.Gameplay;

namespace MeadowRun.Tests;

/// <summary>
/// Proves that the engine's character controller moves Pip as his own mover does: both are driven with the same input
/// over the game's levels and over one small stage per movement rule, and every step is compared exactly (position,
/// velocity, ground). The tests fail on the first step that differs. Set MEADOW_DIFFERENTIAL_REPORT to a file path to
/// get the full report.
/// </summary>
[TestClass]
public sealed class ControllerDifferentialTests
{
    public TestContext TestContext { get; set; } = null!;

    private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private void Report(string name, StringBuilder text)
    {
        TestContext.WriteLine(text.ToString());
        if (Environment.GetEnvironmentVariable("MEADOW_DIFFERENTIAL_REPORT") is { Length: > 0 } path)
            File.AppendAllText(path, $"## {name}{Environment.NewLine}{text}{Environment.NewLine}");
    }

    /// <summary>
    /// The autopilot plays each level as the level tests do (five lives, checkpoints), Pip's mover decides what happens,
    /// and the controller shadows it step by step; whenever it leaves Pip it is put back on him.
    /// </summary>
    [TestMethod]
    public void TheControllerShadowsPipOverEveryLevel()
    {
        var text = new StringBuilder();
        int steps = 0, diverged = 0, pristine = 0, attempts = 0, apart = 0;
        // Level 1 under the full rules, then every level with enemy contact ignored, which lets the autopilot cross it.
        foreach (var (index, ignoreEnemies) in new[] { (0, false), (0, true), (1, true), (2, true), (3, true), (4, true) })
        {
            var session = new Session();
            int checkpoint = 0;
            for (int attempt = 0; attempt < Session.StartingLives; attempt++)
            {
                var stage = StageHarness.New(new Level(Levels.All[index]), session, checkpoint);
                var shadow = new ControllerShadow(stage, closedLoop: true);
                var bot = new Autopilot(attempt);
                for (int f = 0; f < 60 * 200 && stage.Phase != StagePhase.Done; f++)
                {
                    stage.IgnoreEnemiesForTesting = ignoreEnemies;
                    shadow.Step(bot.Next(stage));
                }
                steps += shadow.Steps;
                diverged += shadow.Divergences.Count;
                pristine += shadow.Divergences.Count(d => d.Pristine);
                attempts++;
                if (shadow.Divergences.Count > 0) apart++;

                text.AppendLine(CultureInfo.InvariantCulture, $"level {index + 1}{(ignoreEnemies ? ", enemies ignored" : "")}, attempt {attempt + 1} ({stage.Outcome}): {shadow.Steps} steps compared, {shadow.Divergences.Count} diverged ({shadow.Divergences.Count(d => d.Pristine)} with the controller on its own for the half second before), {shadow.Interventions} changed by the stage");
                if (shadow.Divergences.Count > 0)
                {
                    var first = shadow.Divergences[0];
                    text.AppendLine(CultureInfo.InvariantCulture, $"  first at step {first.Step}: {first.Situation}; differs in {first.Differs}; position by ({F(first.Position.X)}, {F(first.Position.Y)}) px, velocity by ({F(first.Velocity.X)}, {F(first.Velocity.Y)}) px/s");
                    text.AppendLine(CultureInfo.InvariantCulture, $"  {first.Detail}");
                }
                foreach (var group in shadow.Divergences.Where(d => d.Pristine).GroupBy(d => d.Situation + " -> " + d.Differs).OrderByDescending(g => g.Count()))
                {
                    var worst = group.MaxBy(d => d.Position.Length() + d.Velocity.Length() / 60)!;
                    text.AppendLine(CultureInfo.InvariantCulture, $"  {group.Count(),5} x {group.Key}; largest: position ({F(worst.Position.X)}, {F(worst.Position.Y)}) px, velocity ({F(worst.Velocity.X)}, {F(worst.Velocity.Y)}) px/s at step {worst.Step}");
                }
                if (stage.Outcome == StageOutcome.Cleared) break;
                checkpoint = stage.CheckpointReached;
            }
        }
        text.AppendLine(CultureInfo.InvariantCulture, $"total: {steps} steps compared in {attempts} attempts, {diverged} steps diverged ({pristine} with the controller on its own for the half second before), {apart} attempts with a divergence");
        Report("Levels, closed loop", text);
        Assert.IsTrue(steps > 90000, $"only {steps} steps were compared");
        Assert.AreEqual(0, diverged, $"steps in which the controller left Pip:{Environment.NewLine}{text}");
    }

    /// <summary>A small stage and an input for each step, given the step's number and the stage as Pip's mover left it.</summary>
    private sealed record Rule(string Name, string[] Rows, Func<int, Stage, PlayerInput> Input, int Steps = 240);

    private static PlayerInput In(bool left = false, bool right = false, bool down = false, bool jump = false, bool hold = false, bool run = false) =>
        new(left, right, down, jump, jump || hold, run, false);

    private static readonly string[] Flat =
    [
        "S.......................................",
        "########################################",
        "########################################",
    ];

    private static readonly string[] Ledge =
    [
        "S.......................................",
        "##########..............................",
        "##########..............................",
        "##########..............................",
        "########################################",
    ];

    private static readonly string[] LowCeiling =
    [
        "..==========............................",
        "........................................",
        "........................................",
        "S.......................................",
        "########################################",
    ];

    private static readonly string[] Block =
    [
        "....=...................................",
        "........................................",
        "........................................",
        "..S.....................................",
        "########################################",
    ];

    private static readonly string[] Hidden =
    [
        "..H.....................................",
        "........................................",
        "........................................",
        "..S.....................................",
        "########################################",
    ];

    private static readonly string[] OneWay =
    [
        "..S.....................................",
        "-------.................................",
        "........................................",
        "........................................",
        "########################################",
    ];

    private static string[] Platform(char kind) =>
    [
        "......S.......",
        "......" + kind + ".......",
        "..............",
        "####......####",
    ];

    /// <summary>True while Pip falls with his feet at most <paramref name="pixels"/> above the floor of <paramref name="row"/> rows from the bottom.</summary>
    private static bool AboutToLand(Stage stage, float pixels, int row = 2)
    {
        float floor = (stage.Level.Height - row) * Level.TileSize;
        return !stage.Player.Grounded && stage.Player.Velocity.Y > 0 && floor - stage.Player.Feet.Y <= pixels;
    }

    private static IEnumerable<Rule> Rules()
    {
        yield return new("stand still", Flat, (_, _) => default, 60);
        yield return new("walk", Flat, (i, _) => In(right: i >= 5));
        yield return new("run", Flat, (i, _) => In(right: i >= 5, run: true));
        yield return new("run, then walk on (braking above the cap)", Flat, (i, _) => In(right: i >= 5, run: i < 90));
        yield return new("run, then let go (ground friction)", Flat, (i, _) => In(right: i is >= 5 and < 90, run: true));
        yield return new("run, then turn (skid)", Flat, (i, _) => In(right: i is >= 5 and < 90, left: i >= 90, run: true));
        yield return new("standing jump, held", Flat, (i, _) => In(jump: i == 5, hold: i is > 5 and < 40), 120);
        yield return new("standing jump, tapped", Flat, (i, _) => In(jump: i == 5, hold: i is > 5 and < 8), 120);
        yield return new("walking jump, held", Flat, (i, _) => In(right: true, jump: i == 60, hold: i is > 60 and < 100));
        yield return new("running jump, held", Flat, (i, _) => In(right: true, run: true, jump: i == 60, hold: i is > 60 and < 100));
        yield return new("running jump, run released in the air (air momentum)", Flat, (i, _) => In(right: true, run: i < 70, jump: i == 60, hold: i is > 60 and < 100));
        yield return new("running jump, direction released in the air (air drag)", Flat, (i, _) => In(right: i < 70, run: true, jump: i == 60, hold: i is > 60 and < 100));
        yield return new("running jump, turned in the air (air control)", Flat, (i, _) => In(right: i < 70, left: i >= 70, run: true, jump: i == 60, hold: i is > 60 and < 100));
        yield return new("standing jump, then steered in the air (air control from rest)", Flat, (i, _) => In(right: i > 10, jump: i == 5, hold: i is > 5 and < 40), 120);
        yield return new("jump pressed again just before landing (buffer)", Flat, (i, s) => In(jump: i == 5 || (i > 20 && AboutToLand(s, 8)), hold: i is > 5 and < 12), 200);
        yield return new("walk off a ledge and fall", Ledge, (i, _) => In(right: i >= 5));
        yield return new("run off a ledge, jump three steps later (coyote)", Ledge, (i, s) => In(right: i >= 5, run: true, jump: s.Player is { Grounded: false, AirTime: > 2.5f / 60 and < 3.5f / 60 }, hold: !s.Player.Grounded));
        yield return new("jump under a low ceiling (head bump)", LowCeiling, (i, _) => In(right: i >= 5, jump: i % 40 == 30, hold: i % 40 > 30));
        for (int walk = 8; walk <= 40; walk++)
        {
            int until = walk;
            yield return new($"walk {walk} steps, stop, jump past a block's corner", Block, (i, _) => In(right: i >= 5 && i < 5 + until, jump: i == 80, hold: i is > 80 and < 120), 160);
        }
        yield return new("jump into a hidden block", Hidden, (i, _) => In(jump: i == 5, hold: i is > 5 and < 40), 120);
        yield return new("jump beside a hidden block's column", Hidden, (i, _) => In(left: i is >= 5 and < 14, jump: i == 40, hold: i is > 40 and < 80), 160);
        yield return new("drop through a one-way floor (down and jump)", OneWay, (i, _) => In(down: i >= 10, jump: i == 12), 120);
        yield return new("jump onto a one-way floor from below", OneWay, (i, _) => In(down: i is >= 10 and < 20, jump: i == 12 || i == 70, hold: i is > 70 and < 110), 200);
        yield return new("drop through with jump pressed just before landing (buffered drop)", OneWay, (i, s) => In(down: i > 20, jump: i == 5 || (i > 20 && AboutToLand(s, 8, row: 4)), hold: i is > 5 and < 12), 200);
        yield return new("stand on a platform that moves sideways", Platform('_'), (_, _) => default, 400);
        yield return new("walk on a platform that moves sideways", Platform('_'), (i, _) => In(right: i % 120 is > 20 and < 40, left: i % 120 is > 80 and < 100), 400);
        yield return new("jump on a platform that moves sideways", Platform('_'), (i, _) => In(jump: i % 90 == 30, hold: i % 90 is > 30 and < 50), 400);
        yield return new("stand on a platform that moves up and down", Platform('I'), (_, _) => default, 400);
        yield return new("jump on a platform that moves up and down", Platform('I'), (i, _) => In(jump: i % 90 == 30, hold: i % 90 is > 30 and < 50), 400);
        yield return new("stand on a platform that falls", Platform('v'), (_, _) => default, 120);
    }

    /// <summary>
    /// One small stage per rule of Pip's movement, the controller left alone from the first step: where it first leaves
    /// Pip and how far it gets from him.
    /// </summary>
    [TestMethod]
    public void TheControllerFollowsEachRuleOfPipsMovement()
    {
        var text = new StringBuilder();
        int rules = 0, identical = 0, cornerRuns = 0, cornerSame = 0;
        (int Game, int Controller) corners = default;
        foreach (var rule in Rules())
        {
            var stage = StageHarness.Stage(rule.Rows);
            var shadow = new ControllerShadow(stage, closedLoop: false);
            for (int i = 0; i < rule.Steps && stage.Phase == StagePhase.Playing; i++) shadow.Step(rule.Input(i, stage));
            rules++;
            corners = (corners.Game + shadow.Corners.Game, corners.Controller + shadow.Corners.Controller);
            if (rule.Name.Contains("corner", StringComparison.Ordinal))
            {
                // One line for the whole sweep of corner approaches.
                cornerRuns++;
                if (shadow.FirstApart < 0) { identical++; cornerSame++; }
                else text.AppendLine(CultureInfo.InvariantCulture, $"DIFFERS    {rule.Name}: first at step {shadow.FirstApart}, farthest {F(shadow.Farthest.Pixels)} px, at the end ({F(shadow.Apart.X)}, {F(shadow.Apart.Y)}) px{Environment.NewLine}           {shadow.FirstApartDetail}");
                continue;
            }
            if (shadow.FirstApart < 0)
            {
                identical++;
                text.AppendLine(CultureInfo.InvariantCulture, $"identical  {rule.Name} ({shadow.Steps} steps)");
            }
            else
                text.AppendLine(CultureInfo.InvariantCulture, $"DIFFERS    {rule.Name}: first at step {shadow.FirstApart} of {shadow.Steps}, apart in {shadow.StepsApart} steps, farthest {F(shadow.Farthest.Pixels)} px at step {shadow.Farthest.Step}, at the end ({F(shadow.Apart.X)}, {F(shadow.Apart.Y)}) px{Environment.NewLine}           {shadow.FirstApartDetail}");
        }
        text.AppendLine(CultureInfo.InvariantCulture, $"corner approaches: {cornerSame} of {cornerRuns} identical; the game nudged Pip in {corners.Game} steps, the controller in {corners.Controller}");
        text.AppendLine(CultureInfo.InvariantCulture, $"{identical} of {rules} identical");
        Report("Rules, open loop", text);
        Assert.IsTrue(rules > 60, $"only {rules} rules were run");
        Assert.AreEqual(rules, identical, $"rules in which the controller left Pip:{Environment.NewLine}{text}");
    }
}
