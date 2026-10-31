using System.Numerics;
using NeonRift.Content;
using NeonRift.Gameplay;

namespace NeonRift.Tests;

/// <summary>Builds small stages with <see cref="StageBuilder"/> and steps them with synthetic input at 60 Hz.</summary>
internal static class Harness
{
    public const float Dt = 1f / 60f;

    /// <summary>A flat stage of ground three tiles thick, long enough to scroll, with an arena at the end.</summary>
    public static StageDef Flat(int columns = 120, Action<StageBuilder>? extra = null)
    {
        var b = new StageBuilder();
        b.Floor(columns, 3);
        extra?.Invoke(b);
        return b.Build("TEST", "", "city", "city", BossKind.Gunship);
    }

    public static Stage Stage(StageDef? def = null, Session? session = null) => new(def ?? Flat(), session ?? new Session());

    public static PlayerInput In(bool left = false, bool right = false, bool up = false, bool down = false,
        bool jump = false, bool jumpHeld = false, bool fire = false, bool @lock = false) => new(left, right, up, down, jump, jumpHeld, fire, @lock);

    public static void Run(Stage stage, int steps, PlayerInput input)
    {
        for (int i = 0; i < steps; i++) stage.Update(Dt, input);
    }

    /// <summary>Lets the player settle on the ground after spawning.</summary>
    public static Stage Settled(StageDef? def = null, Session? session = null)
    {
        var stage = Stage(def, session);
        Run(stage, 30, PlayerInput.None);
        return stage;
    }

    public static Vector2 Feet(Stage stage) => stage.Player.Position;
}
