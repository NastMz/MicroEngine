using System.Numerics;
using MeadowRun.Content;
using MeadowRun.Gameplay;
using MicroEngine.Scenes;

namespace MeadowRun.Tests;

/// <summary>
/// Builds small stages from ASCII and steps them with synthetic input at 60 Hz. Each stage gets a world of its own
/// that the harness steps around every stage update, as the play scene's engine does with its scene's world.
/// </summary>
internal static class StageHarness
{
    public const float Dt = 1f / 60f;

    /// <summary>Pads the given rows to the standard height by adding sky rows above.</summary>
    public static LevelDef Def(params string[] rows)
    {
        var all = Enumerable.Repeat(new string('.', rows[0].Length), Math.Max(0, Levels.Rows - rows.Length)).Concat(rows).ToArray();
        return new LevelDef(99, "Test", "", Biome.Meadow, 300, "meadow", all);
    }

    /// <summary>A stage of <paramref name="level"/> in a new world.</summary>
    public static Stage New(Level level, Session session, int checkpoint = 0) => new(level, session, new SceneWorld(), checkpoint);

    public static Stage Stage(params string[] rows) => New(new Level(Def(rows)), new Session());

    public static PlayerInput Input(bool left = false, bool right = false, bool down = false, bool jumpPressed = false, bool jumpHeld = false, bool run = false, bool runPressed = false)
        => new(left, right, down, jumpPressed, jumpHeld, run, runPressed);

    /// <summary>One stage update inside one step of its world (the world's clock advances, destroyed actors are released).</summary>
    public static void Step(Stage stage, PlayerInput input)
    {
        var world = (SceneWorld)stage.Objects;
        world.BeginStep(world.SteppedAt + 1, Dt);
        stage.Update(Dt, input);
        world.EndStep();
    }

    public static void Run(Stage stage, int steps, PlayerInput input)
    {
        for (int i = 0; i < steps; i++) Step(stage, input);
    }

    /// <summary>Prepares a frame drawn <paramref name="blend"/> of the way from the previous step to the latest one.</summary>
    public static void Draw(Stage stage, float blend)
    {
        var world = (SceneWorld)stage.Objects;
        world.BeginDraw(world.SteppedAt, blend);
    }

    /// <summary>Presses jump and holds it for <paramref name="holdSteps"/>, recording the highest point (smallest feet Y).</summary>
    public static float JumpPeak(Stage stage, int holdSteps, int totalSteps = 90, bool right = false)
    {
        float startFeet = stage.Player.Feet.Y, best = startFeet;
        for (int i = 0; i < totalSteps; i++)
        {
            Step(stage, Input(right: right, jumpPressed: i == 0, jumpHeld: i < holdSteps));
            best = MathF.Min(best, stage.Player.Feet.Y);
        }
        return startFeet - best;
    }

    public static Vector2 Feet(Stage stage) => stage.Player.Feet;
}
