using System.Globalization;
using System.Text;
using MeadowRun.Art;
using MeadowRun.Content;
using MeadowRun.Gameplay;

namespace MeadowRun.Tests;

/// <summary>
/// Proves that the engine's character controller moves Pip as his own mover does: both are driven with the same input
/// over the game's levels and over one small stage per movement rule, and every step is compared exactly (position,
/// velocity, ground, the jump, the timers, the events and the block bumped). On the autopilot's runs, the game's scripts
/// and the rules the controller is identical; under random input it is not (see the test). Set MEADOW_DIFFERENTIAL_REPORT to a file path to
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

    /// <summary>What the shadows of several attempts compared, added up.</summary>
    private sealed class Sum
    {
        private readonly HashSet<(int Level, (System.Numerics.Vector2, System.Numerics.Vector2, bool, bool, PlayerInput) Situation)> _situations = [];
        private readonly SortedDictionary<string, int> _kinds = [];
        private readonly StringBuilder _divergences = new();
        public int Steps, Attempts, Diverged, Restores, Launches, Placements, Interventions, AirTimeApart, LandImpactApart;
        public int Situations => _situations.Count;
        public int Kind(string name) => _kinds.GetValueOrDefault(name);

        public void Add(int level, string what, ControllerShadow shadow)
        {
            Steps += shadow.Steps;
            Attempts++;
            Diverged += shadow.Divergences.Count;
            Restores += shadow.Restores;
            Launches += shadow.Launches;
            Placements += shadow.Placements;
            Interventions += shadow.Interventions;
            AirTimeApart += shadow.AirApart.AirTime;
            LandImpactApart += shadow.AirApart.LandImpact;
            foreach (var situation in shadow.Situations) _situations.Add((level, situation));
            foreach (var (kind, count) in shadow.Kinds) _kinds[kind] = _kinds.GetValueOrDefault(kind) + count;
            if (shadow.Divergences.Count == 0) return;
            var first = shadow.Divergences[0];
            _divergences.AppendLine(CultureInfo.InvariantCulture, $"{what}: {shadow.Divergences.Count} of {shadow.Steps} steps diverged; first at step {first.Step}: {first.Situation}; differs in {first.Differs}; position by ({F(first.Position.X)}, {F(first.Position.Y)}) px, velocity by ({F(first.Velocity.X)}, {F(first.Velocity.Y)}) px/s");
            foreach (var d in shadow.Divergences)
            {
                _divergences.AppendLine(CultureInfo.InvariantCulture, $"  step {d.Step}: {d.Situation}; differs in {d.Differs}; position by ({F(d.Position.X)}, {F(d.Position.Y)}) px, velocity by ({F(d.Velocity.X)}, {F(d.Velocity.Y)}) px/s; {(d.Healed < 0 ? "still apart after " + ControllerShadow.HealLimit : "the same again after " + d.Healed)} steps, at most {F(d.Farthest)} px apart");
                _divergences.AppendLine(CultureInfo.InvariantCulture, $"    {d.Detail}");
            }
        }

        public StringBuilder Text()
        {
            var text = new StringBuilder();
            text.Append(_divergences);
            text.AppendLine(CultureInfo.InvariantCulture, $"total: {Steps} steps compared in {Attempts} attempts, {Situations} different situations (a level, Pip's position, velocity, ground and jump state, and the input); {Diverged} steps diverged; the stage changed Pip {Interventions} times: thrown {Launches} times, placed {Placements} times; the controller's state was copied from Pip {Restores} times");
            text.AppendLine(CultureInfo.InvariantCulture, $"the controller's own air time differed from Pip's after {AirTimeApart} steps, and the landing impact computed from it in {LandImpactApart} landings");
            text.AppendLine("steps by kind: " + string.Join(", ", _kinds.Select(k => $"{k.Key} {k.Value}")));
            return text;
        }

        public void AssertFollowed()
        {
            Assert.AreEqual(0, Restores, $"the controller was never given Pip's state:{Environment.NewLine}{Text()}");
            Assert.AreEqual(0, Diverged, $"steps in which the controller left Pip:{Environment.NewLine}{Text()}");
        }
    }

    /// <summary>
    /// The autopilot plays each level as the level tests do (five lives, checkpoints), Pip's mover decides what happens,
    /// and the controller shadows it step by step, on its own from the start of each attempt to its end: while Pip
    /// plays, on his walk off the goal and in the victory pose. The autopilot repeats itself (attempts that die where
    /// the one before did, long waits on levels 4 and 5 until the timer runs out), so the number of steps overstates
    /// what is compared: the test also counts the different situations.
    /// </summary>
    [TestMethod]
    public void TheControllerShadowsPipOverEveryLevel()
    {
        var sum = new Sum();
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
                    StageHarness.Step(stage, bot.Next(stage));
                }
                sum.Add(index, $"level {index + 1}{(ignoreEnemies ? ", enemies ignored" : "")}, attempt {attempt + 1} ({stage.Outcome})", shadow);
                if (stage.Outcome == StageOutcome.Cleared) break;
                checkpoint = stage.CheckpointReached;
            }
        }
        Report("Levels, autopilot", sum.Text());
        Assert.IsTrue(sum.Steps > 90000, $"only {sum.Steps} steps were compared");
        Assert.IsTrue(sum.Situations > 6000, $"only {sum.Situations} different situations were compared");
        Assert.IsTrue(sum.Launches > 0, "the stage's bounces were written as the game writes them");
        Assert.IsTrue(sum.Kind("walk off the goal") > 0, "the walk off the goal was compared");
        sum.AssertFollowed();
    }

    private static readonly int Seeds = int.TryParse(Environment.GetEnvironmentVariable("MEADOW_DIFFERENTIAL_SEEDS"), out int n) ? n : 6;

    /// <summary>Input nobody planned: each control held and released for random lengths, mostly to the right.</summary>
    private sealed class Noise(int seed)
    {
        private readonly Random _random = new(seed);
        private int _axis = 1, _move, _jumpFor, _runFor, _downFor;
        private bool _jump, _run, _down, _jumpWas, _runWas;

        public PlayerInput Next()
        {
            if (_move-- <= 0)
            {
                double r = _random.NextDouble();
                _axis = r < 0.62 ? 1 : r < 0.82 ? -1 : 0;
                _move = _random.Next(2, 70);
            }
            if (_jumpFor-- <= 0) { _jump = _random.NextDouble() < 0.45; _jumpFor = _random.Next(1, _jump ? 34 : 40); }
            if (_runFor-- <= 0) { _run = _random.NextDouble() < 0.6; _runFor = _random.Next(3, 120); }
            if (_downFor-- <= 0) { _down = _random.NextDouble() < 0.25; _downFor = _random.Next(3, 50); }
            var input = new PlayerInput(_axis < 0, _axis > 0, _down, _jump && !_jumpWas, _jump, _run, _run && !_runWas);
            _jumpWas = _jump;
            _runWas = _run;
            return input;
        }
    }

    /// <summary>
    /// What the autopilot avoids: every level, from its start and from each checkpoint, under the full rules (enemies
    /// that are stomped and that hurt, springs, spikes, fire bars, the Mole King) and with enemy contact ignored, as
    /// each of Pip's forms, with seeded random input, and with the autopilot interrupted by bursts of it (which takes
    /// Pip far into a level and then does what no plan would: turns back in the air, drops through planks, jumps
    /// under ceilings, leaves a moving platform half way).
    /// </summary>
    [TestMethod]
    public void TheControllerShadowsPipUnderSeededRandomInput()
    {
        var sum = new Sum();
        for (int index = 0; index < Levels.All.Count; index++)
        {
            int checkpoints = new Level(Levels.All[index]).Spawns.Count(s => s.Kind == 'K');
            for (int checkpoint = 0; checkpoint <= checkpoints; checkpoint++)
            for (int seed = 0; seed < Seeds; seed++)
            for (int mode = 0; mode < 3; mode++)
            {
                // Mode 0: random input alone. Modes 1 and 2: the autopilot with bursts of it, under the full rules
                // and with enemy contact ignored.
                var session = new Session { Form = (PipForm)((seed + mode) % 3) };
                var stage = StageHarness.New(new Level(Levels.All[index]), session, checkpoint);
                var shadow = new ControllerShadow(stage, closedLoop: true);
                var random = new Random(7919 * index + 211 * checkpoint + 17 * seed + mode);
                var noise = new Noise(index * 1000 + checkpoint * 100 + seed * 10 + mode);
                var bot = new Autopilot(seed);
                int burst = 0;
                for (int f = 0; f < 60 * 75 && stage.Phase != StagePhase.Done; f++)
                {
                    stage.IgnoreEnemiesForTesting = mode == 2;
                    PlayerInput input;
                    if (mode == 0) input = noise.Next();
                    else
                    {
                        if (burst <= 0 && random.Next(100) == 0) burst = random.Next(8, 70);
                        var planned = stage.Phase == StagePhase.Playing ? bot.Next(stage) : default;
                        input = burst-- > 0 ? noise.Next() : planned;
                    }
                    StageHarness.Step(stage, input);
                }
                sum.Add(index, $"level {index + 1}, checkpoint {checkpoint}, seed {seed}, mode {mode} ({stage.Outcome} {stage.DeathCause})", shadow);
            }
        }
        Report("Levels, seeded random input", sum.Text());
        Assert.IsTrue(sum.Situations > 60000, $"only {sum.Situations} different situations were compared");
        foreach (string kind in new[] { "landing on a platform", "riding a platform", "jump off a platform", "head bump", "skid", "rising after a launch", "jump cut", "boss fight", "wall stop" })
            Assert.IsTrue(sum.Kind(kind) > 0, $"no step of the kind \"{kind}\" was compared:{Environment.NewLine}{sum.Text()}");
        Assert.IsTrue(sum.Launches > 20, $"the stage threw Pip only {sum.Launches} times");
        // Not identical: the tile physics world counts a surface within 0.01 px as touched and lets a box 0.001 px
        // into one, where TileMotion stops a box exactly at a surface it would cross and nowhere else. The record
        // (odd/tasks/demo-modern-1/meadow-run.md, "Second pass") has the numbers; Pip keeps his mover for that.
        Assert.IsTrue(sum.Diverged * 10000L <= sum.Steps, $"more than one step in 10,000 diverged:{Environment.NewLine}{sum.Text()}");
    }

    /// <summary>
    /// The game's own scripts, run as the game runs them (the title, the map, the level card, the play scene with its
    /// input bindings and on-screen controls): every stage a play scene creates is shadowed from its first step.
    /// </summary>
    [TestMethod]
    public void TheControllerShadowsPipInEveryScriptOfTheGame()
    {
        var sum = new Sum();
        foreach (object[] scenario in StateTraceTests.Scenarios)
        {
            string script = (string)scenario[0], variables = (string)scenario[1];
            var values = variables.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);
            var launch = LaunchOptions.From(new MicroEngine.Hosting.HostEnvironment(MicroEngine.Hosting.HostKind.Test) { Variables = name => values.GetValueOrDefault(name) });
            var shadows = new List<(int Level, ControllerShadow Shadow)>();
            var game = new MeadowGame(launch);
            game.StageCreatedForTesting = stage => shadows.Add((stage.Level.Def.Number - 1, new ControllerShadow(stage, closedLoop: true)));
            MicroEngine.Testing.StateTraceRuns.Run(game, MicroEngine.Testing.StateTraceRuns.Script("games/MeadowRun/scripts/" + script));
            int steps = 0;
            foreach (var (level, shadow) in shadows)
            {
                sum.Add(level, $"{script}, level {level + 1}", shadow);
                steps += shadow.Steps;
            }
            TestContext.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{script}: {shadows.Count} stages, {steps} steps compared"));
        }
        Report("The game's scripts", sum.Text());
        Assert.IsTrue(sum.Steps > 3500, $"only {sum.Steps} steps were compared");
        sum.AssertFollowed();
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
            for (int i = 0; i < rule.Steps && stage.Phase == StagePhase.Playing; i++) StageHarness.Step(stage, rule.Input(i, stage));
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
