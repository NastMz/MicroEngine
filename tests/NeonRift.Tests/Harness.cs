using System.Numerics;
using MicroEngine.Scenes;
using NeonRift.Content;
using NeonRift.Gameplay;

namespace NeonRift.Tests;

/// <summary>
/// Builds small stages with <see cref="StageBuilder"/> and steps them with synthetic input at 60 Hz, each in a world of
/// its own stepped the way the play scene's engine steps its objects (start the world's step, update, end it).
/// </summary>
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

    public static Stage Stage(StageDef? def = null, Session? session = null) => new(def ?? Flat(), session ?? new Session(), new SceneWorld());

    /// <summary>The stage's world, as the engine drives it.</summary>
    public static SceneWorld World(Stage stage) => (SceneWorld)stage.World;

    /// <summary>One update: starts the world's step, updates the stage, ends the step (releasing what was destroyed).</summary>
    public static void Step(Stage stage, PlayerInput input)
    {
        var world = World(stage);
        float dt = world.BeginStep(world.SteppedAt + 1, Dt);
        stage.Update(dt, input);
        world.EndStep();
    }

    /// <summary>Prepares drawing at <paramref name="blend"/> of the way from the previous update to the latest one.</summary>
    public static void Draw(Stage stage, float blend) => World(stage).BeginDraw(World(stage).SteppedAt, blend);

    public static PlayerInput In(bool left = false, bool right = false, bool up = false, bool down = false,
        bool jump = false, bool jumpHeld = false, bool fire = false, bool @lock = false) => new(left, right, up, down, jump, jumpHeld, fire, @lock);

    public static void Run(Stage stage, int steps, PlayerInput input)
    {
        for (int i = 0; i < steps; i++) Step(stage, input);
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
