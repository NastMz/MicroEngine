using System.Globalization;
using System.Numerics;
using System.Text;
using MicroEngine;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Testing;
using MicroEngine.World;
using NeonRift.Content;
using NeonRift.Gameplay;
using static NeonRift.Tests.Harness;

namespace NeonRift.Tests;

/// <summary>
/// The engine's <see cref="CharacterController2D"/> driven beside Vela's own mover (<see cref="Player.Step"/> over
/// <see cref="Body"/>) with the same input: over the three stages with the autopilot (mortal and not), over the
/// game's scripts, and over one small stage per rule of her movement. Position, velocity and the grounded flag are
/// compared exactly after every step. Set NEON_DIFFERENTIAL_REPORT to a file path to get the report.
/// </summary>
/// <remarks>
/// The result keeps Vela on her own mover: every rule is identical, and over the stages the two are apart in 113 of
/// 25,849 steps, each time by under a ten-thousandth of a pixel in height and in nothing else. Vela lands when ground
/// is within a pixel under her feet and keeps her speed until the floor stops her on the next step, which puts her on
/// it exactly; when such a landing ends within the tile world's contact distance of the floor (0.00009 px above it)
/// and she does not run, the controller takes her as resting there and never makes that last move. The test holds
/// that measure: it fails if the two come apart in any other way, or come together entirely (then she can move over).
/// </remarks>
[TestClass]
public sealed class ControllerDifferentialTests
{
    public TestContext TestContext { get; set; } = null!;

    private void Report(StringBuilder text)
    {
        TestContext.WriteLine(text.ToString());
        if (Environment.GetEnvironmentVariable("NEON_DIFFERENTIAL_REPORT") is { Length: > 0 } path) File.AppendAllText(path, text.ToString());
    }

    /// <summary>The inputs of a script of the game, one per update: holds and presses of the play actions.</summary>
    private static PlayerInput[] ScriptInputs(string name)
    {
        var lines = File.ReadAllLines(Path.Combine(StateTraceRuns.RepositoryRoot(), "games", "NeonRift", "scripts", name + ".script"));
        var held = new Dictionary<string, List<(int From, int To)>>();
        int end = 0;
        foreach (var line in lines)
        {
            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2 || !int.TryParse(words[0], out int at)) continue;
            end = Math.Max(end, at);
            if (words[1] is not ("hold" or "press")) continue;
            int length = words[1] == "hold" ? int.Parse(words[3], CultureInfo.InvariantCulture) : 1;
            if (!held.TryGetValue(words[2], out var spans)) held[words[2]] = spans = [];
            spans.Add((at, at + length));
        }
        bool Down(string action, int update) => held.TryGetValue(action, out var spans) && spans.Any(s => update >= s.From && update < s.To);
        var inputs = new PlayerInput[end];
        for (int u = 0; u < end; u++)
            inputs[u] = new PlayerInput(Down("left", u), Down("right", u), Down("up", u), Down("down", u),
                Down("jump", u) && !Down("jump", u - 1), Down("jump", u), Down("fire", u), Down("lock", u));
        return inputs;
    }

    [TestMethod]
    public void TheControllerShadowsVelaOverTheStagesAndTheScripts()
    {
        var text = new StringBuilder();
        int steps = 0, apart = 0, other = 0;
        void Add(string name, VelaShadow shadow)
        {
            steps += shadow.Steps;
            apart += shadow.Divergences.Count;
            other += shadow.Divergences.Count(d => d.Differs != "position" || d.Apart.X != 0 || MathF.Abs(d.Apart.Y) >= 1e-4f);
            text.AppendLine(CultureInfo.InvariantCulture, $"{name}: {shadow.Steps} steps compared, {shadow.Divergences.Count} apart");
            foreach (var group in shadow.Divergences.GroupBy(d => d.Differs).OrderByDescending(g => g.Count()))
                text.AppendLine(CultureInfo.InvariantCulture, $"  {group.Count(),5} x differs in {group.Key}; first: {group.First()}");
        }
        for (int index = 0; index < Stages.Count; index++)
        foreach (bool god in new[] { true, false })
        {
            var stage = new Stage(Stages.All[index], new Session(), new MicroEngine.Scenes.SceneWorld(), Sprites, seed: 11 + index);
            stage.Player.God = god;
            var shadow = new VelaShadow(stage);
            var bot = new Autopilot();
            for (int i = 0; i < 60 * 60 * 6 && !stage.Finished && stage.Phase != StagePhase.GameOver; i++) shadow.Step(bot.Next(stage, Dt));
            Add($"stage {index + 1}, autopilot{(god ? "" : ", mortal")}", shadow);
        }
        foreach (string script in new[] { "stage1", "poses", "tour" })
        {
            var stage = new Stage(Stages.All[0], new Session(), new MicroEngine.Scenes.SceneWorld(), Sprites, seed: 11);
            stage.Player.God = script != "stage1";
            var shadow = new VelaShadow(stage);
            foreach (var input in ScriptInputs(script)) shadow.Step(input);
            Add($"script {script}", shadow);
        }
        text.AppendLine(CultureInfo.InvariantCulture, $"total: {steps} steps compared, {apart} apart");
        Report(text);
        Assert.IsTrue(steps > 20000, $"only {steps} steps were compared");
        Assert.AreEqual(0, other, $"steps apart in something other than a height under 0.0001 px:{Environment.NewLine}{text}");
        Assert.AreNotEqual(0, apart, "the controller never left Vela: she can move with it");
    }

    /// <summary>A small stage for one rule of the movement and the input of each of its steps.</summary>
    private sealed record Rule(string Name, Action<StageBuilder> Build, Func<int, PlayerInput> Input, int Steps = 240);

    private static readonly Rule[] Rules =
    [
        new("run and stop", _ => { }, i => In(right: i is > 40 and < 120, left: i is > 150 and < 200)),
        new("a full jump", _ => { }, i => In(right: true, jump: i == 50, jumpHeld: i is >= 50 and < 110)),
        new("a hop: jump released early", _ => { }, i => In(right: true, jump: i == 50, jumpHeld: i is >= 50 and < 56)),
        new("a tap: jump pressed for one update", _ => { }, i => In(jump: i is 50 or 120, jumpHeld: i is 50 or 120)),
        new("jumps under a low ceiling", b => b.BlockAt(6, 6, 12, 1), i => In(right: i > 20, jump: i % 40 == 35, jumpHeld: true), 300),
        new("into a wall, then a jump onto it", b => b.BlockAt(12, 3, 3, 2), i => In(right: true, jump: i == 130, jumpHeld: i >= 130), 300),
        new("walking off a ledge", b => b.BlockAt(0, 3, 9, 3), i => In(right: i > 30)),
        new("onto a platform and down through it", b => b.PlatformAt(8, 6, 6), i => In(right: i < 70 || i > 200, jump: i is 40 or 140, jumpHeld: i is >= 40 and < 80 or 140, down: i is >= 120 and < 160), 320),
        new("kneeling and planted feet", _ => { }, i => In(right: i is > 40 and < 160, down: i is > 80 and < 100, @lock: i is > 110 and < 140)),
        new("into a pit", b => b.Pit(4).Floor(40, 3), i => In(right: true), 200),
    ];

    [TestMethod]
    public void TheControllerFollowsEachRuleOfVelasMovement()
    {
        var text = new StringBuilder();
        int apart = 0;
        foreach (var rule in Rules)
        {
            var stage = Stage(Flat(rule.Name == "into a pit" ? 14 : 60, rule.Build));
            var shadow = new VelaShadow(stage);
            for (int i = 0; i < rule.Steps; i++) shadow.Step(rule.Input(i));
            apart += shadow.Divergences.Count;
            text.AppendLine(CultureInfo.InvariantCulture, $"{rule.Name}: {shadow.Steps} steps, {shadow.Divergences.Count} apart{(shadow.Divergences.Count > 0 ? "; first: " + shadow.Divergences[0] : "")}");
        }
        Report(text);
        Assert.AreEqual(0, apart, $"rules in which the controller left Vela:{Environment.NewLine}{text}");
    }
}

/// <summary>
/// A <see cref="CharacterController2D"/> beside Vela, in a tile physics world of its own holding the stage's grid. The
/// mapping is what the game would write on the controller: one <see cref="CharacterMotion"/> with her numbers, her
/// intent passed as it is, and the game's own rules around the step (when a jump is a drop, the two pixels a drop
/// starts with, the camera's clamp, a hit's knock, the fall of a death, a respawn).
/// </summary>
internal sealed class VelaShadow
{
    private static readonly Vector2 Corner = new(-6, -40), Size = new(12, 40);

    /// <summary>Vela's feel, from the game's numbers: a run with no inertia, one gravity, a jump cut to 45 % on release.</summary>
    public static readonly CharacterMotion Feel = new()
    {
        MoveSpeed = Player.RunSpeed,
        Gravity = Player.Gravity, MaxFallSpeed = Player.MaxFall,
        JumpSpeed = Player.JumpSpeed, JumpReleaseSpeedScale = 0.45f, JumpReleaseGravityScale = 1,
        CoyoteTime = 0, JumpBuffer = 0,
        LandingReach = 1,            // ground a pixel under the feet is ground (TileMotion.IsOnGround)
        DropThroughTime = 0.25f,
    };
    /// <summary>The fall of a death: nothing steers it, nothing takes its speed away (the game stops it on the ground) and no released jump cuts it.</summary>
    public static readonly CharacterMotion Dying = Feel with { AirDeceleration = 0, Deceleration = 0, JumpReleaseSpeedScale = 1 };

    private readonly Stage _stage;
    private readonly TilePhysicsWorld2D _world = new();
    private bool _crouching;

    public VelaShadow(Stage stage)
    {
        _stage = stage;
        var level = _world.CreateBody(default, BodyDescription.Static());
        _world.AddTiles(level, stage.Grid);
        Hero = CharacterController2D.Create(_world, default, stage.Player.Position + Corner, Size, Feel, offset: Size / 2);
    }

    public CharacterController2D Hero { get; }
    public int Steps { get; private set; }
    public List<Divergence> Divergences { get; } = [];

    public sealed record Divergence(int Step, string Differs, Vector2 Apart, string Detail)
    {
        public override string ToString() => $"step {Step}: {Detail}";
    }

    private Vector2 Feet => Hero.Position - Corner;

    /// <summary>One update: the controller moves as the game would drive it, then the stage runs with Vela's own mover, and the two are compared.</summary>
    public void Step(PlayerInput input)
    {
        var player = _stage.Player;
        var before = player.State;
        int health = player.Health;
        bool cleared = _stage.Phase is StagePhase.Cleared or StagePhase.GameOver;
        if (before == PlayerState.Alive) Live(cleared ? PlayerInput.None : input);
        else if (before == PlayerState.Dying) Fall();

        Harness.Step(_stage, input);

        // What the stage did to Vela after her step, written as the game would write it on the controller.
        int facing = player.Facing;
        if (before == PlayerState.Alive && player.State != PlayerState.Alive)
        {
            Hero.Motion = Dying;
            Hero.Velocity = new Vector2(-facing * 70, -260);
        }
        else if (before == PlayerState.Alive && player.Health < health)
            Hero.Velocity = new Vector2(-facing * 60, Hero.IsGrounded ? -140 : Hero.Velocity.Y);
        else if (before == PlayerState.Waiting && player.State == PlayerState.Alive)
        {
            Hero.Motion = Feel;
            Hero.Position = player.Position + Corner;
            Hero.Velocity = Vector2.Zero;
        }
        if (before == PlayerState.Waiting || player.State == PlayerState.Waiting) return;

        Steps++;
        string differs = string.Join("+", new[]
        {
            Feet != player.Position ? "position" : null,
            Hero.Velocity != player.Velocity ? "velocity" : null,
            Hero.IsGrounded != player.Grounded ? "ground" : null,
        }.Where(s => s is not null));
        if (differs.Length == 0) return;
        Divergences.Add(new Divergence(Steps, differs, Feet - player.Position, string.Create(CultureInfo.InvariantCulture,
            $"{before}, input {input}; Vela at {player.Position} moving {player.Velocity}, grounded {player.Grounded}; the controller at {Feet} moving {Hero.Velocity}, grounded {Hero.IsGrounded}; apart by {Feet - player.Position} px and {Hero.Velocity - player.Velocity} px/s")));
        // Together again for the next step.
        Hero.Restore(Hero.Snapshot() with { Position = player.Position + Corner, Velocity = player.Velocity, IsGrounded = player.Grounded, IsJumping = player.Somersault && player.Velocity.Y < 0 });
    }

    private void Live(PlayerInput input)
    {
        int move = (input.Right ? 1 : 0) - (input.Left ? 1 : 0);
        bool grounded = Hero.IsGrounded, planted = input.Lock && grounded;
        _crouching = grounded && input.Down && move == 0;
        // The game's rule: down and jump on a platform (and no solid under any foot) is a drop, which starts two pixels down.
        bool drop = input.Jump && grounded && input.Down && StandingOnPlatform();
        if (drop) Hero.Position += new Vector2(0, 2);
        Hero.Step(Dt, new CharacterInput(_crouching || planted ? 0 : move, input.Jump && grounded && !drop, input.JumpHeld, Drop: drop));
        KeepFalling();
        var view = _stage.Camera.View;
        Hero.Position = new Vector2(Math.Clamp(Feet.X, view.Left + 8, view.Right - 8), Feet.Y) + Corner;
    }

    private void Fall()
    {
        Hero.Step(Dt, new CharacterInput(0));
        KeepFalling();
        if (Hero.IsGrounded) Hero.Velocity = new Vector2(0, Hero.Velocity.Y);
        // The game keeps the feet and derives the box from them every step.
        Hero.Position = Feet + Corner;
    }

    /// <summary>A landing the floor did not stop (ground within a pixel under the feet) keeps its speed until the floor stops it.</summary>
    private void KeepFalling()
    {
        if ((Hero.Events & CharacterEvents.Landed) != 0 && !Hero.LastMove.StoppedGoing(Vector2.UnitY)) Hero.Velocity = new Vector2(Hero.Velocity.X, Hero.LandingSpeed);
    }

    private bool StandingOnPlatform()
    {
        int ts = _stage.Grid.TileSize;
        var feet = Feet + new Vector2(0, 1);
        int y = (int)MathF.Floor(feet.Y / ts);
        bool any = false;
        foreach (float dx in (ReadOnlySpan<float>)[-5f, 0, 5f])
        {
            var flags = _stage.Grid.FlagsAt((int)MathF.Floor((feet.X + dx) / ts), y);
            if ((flags & TileFlags.Solid) != 0) return false;
            any |= (flags & TileFlags.OneWay) != 0;
        }
        return any;
    }
}
