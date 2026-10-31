using System.Numerics;
using MeadowRun.Content;
using MeadowRun.Gameplay;

namespace MeadowRun.Tests;

/// <summary>Builds small stages from ASCII and steps them with synthetic input at 60 Hz.</summary>
internal static class StageHarness
{
    public const float Dt = 1f / 60f;

    /// <summary>Pads the given rows to the standard height by adding sky rows above.</summary>
    public static LevelDef Def(params string[] rows)
    {
        var all = Enumerable.Repeat(new string('.', rows[0].Length), Math.Max(0, Levels.Rows - rows.Length)).Concat(rows).ToArray();
        return new LevelDef(99, "Test", "", Biome.Meadow, 300, "meadow", all);
    }

    public static Stage Stage(params string[] rows) => new(new Level(Def(rows)), new Session());

    public static PlayerInput Input(bool left = false, bool right = false, bool down = false, bool jumpPressed = false, bool jumpHeld = false, bool run = false, bool runPressed = false)
        => new(left, right, down, jumpPressed, jumpHeld, run, runPressed);

    public static void Run(Stage stage, int steps, PlayerInput input)
    {
        for (int i = 0; i < steps; i++) stage.Update(Dt, input);
    }

    /// <summary>Presses jump and holds it for <paramref name="holdSteps"/>, recording the highest point (smallest feet Y).</summary>
    public static float JumpPeak(Stage stage, int holdSteps, int totalSteps = 90, bool right = false)
    {
        float startFeet = stage.Player.Feet.Y, best = startFeet;
        for (int i = 0; i < totalSteps; i++)
        {
            stage.Update(Dt, Input(right: right, jumpPressed: i == 0, jumpHeld: i < holdSteps));
            best = MathF.Min(best, stage.Player.Feet.Y);
        }
        return startFeet - best;
    }

    public static Vector2 Feet(Stage stage) => stage.Player.Feet;
}
