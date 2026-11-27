using System.Numerics;
using NeonRift.Gameplay;
using static NeonRift.Tests.Harness;

namespace NeonRift.Tests;

/// <summary>
/// Drawing between updates: actors and the scroll are entities of the stage's world, so what is drawn follows the
/// world's blend (from where each stood when the update started to where it stands now); the rules never do.
/// </summary>
[TestClass]
public sealed class DrawBlendTests
{
    private const float Tolerance = 1e-3f;

    private static void AreClose(Vector2 expected, Vector2 actual, string message)
    {
        Assert.AreEqual(expected.X, actual.X, Tolerance, message);
        Assert.AreEqual(expected.Y, actual.Y, Tolerance, message);
    }

    [TestMethod]
    public void Running_player_and_scroll_are_drawn_between_the_previous_and_current_update()
    {
        var stage = Settled();
        Run(stage, 100, In(right: true));
        var before = stage.Player.Position;
        var cameraBefore = stage.Camera.Position;
        Run(stage, 1, In(right: true));
        var after = stage.Player.Position;
        Assert.IsTrue(after.X > before.X, "The player moved during the update.");
        Assert.IsTrue(stage.Camera.Position.X > cameraBefore.X, "The screen scrolled during the update.");

        Draw(stage, 0.25f);
        AreClose(Vector2.Lerp(before, after, 0.25f), stage.Player.DrawPosition, "the player");
        AreClose(Vector2.Lerp(cameraBefore, stage.Camera.Position, 0.25f), stage.World.DrawPosition2D(stage.CameraEntity), "the scroll");

        Draw(stage, 1);
        Assert.AreEqual(after, stage.Player.DrawPosition, "Blend 1 draws exactly the current update.");
        Assert.AreEqual(stage.Camera.Position, stage.World.DrawPosition2D(stage.CameraEntity), "Blend 1 draws exactly the current scroll.");
    }

    [TestMethod]
    public void A_moving_shot_is_drawn_where_it_spawned_until_its_first_update()
    {
        var stage = Settled();
        var shot = stage.Spawn(new EnemyShot(new Vector2(300, 200), new Vector2(-120, 0)));
        Draw(stage, 0.5f);
        Assert.AreEqual(shot.Position, shot.DrawPosition, "New: no previous position yet, no streak from the origin.");

        Run(stage, 1, PlayerInput.None);
        Draw(stage, 0.5f);
        Assert.IsTrue(shot.Alive);
        AreClose(Vector2.Lerp(new Vector2(300, 200), shot.Position, 0.5f), shot.DrawPosition, "the shot");
    }

    [TestMethod]
    public void Respawning_teleports_the_player_instead_of_drawing_a_streak()
    {
        var stage = Settled();
        Run(stage, 10, In(right: true));
        stage.Player.Respawn(stage.Camera.View.Left + 200, WeaponKind.Pulse);
        Draw(stage, 0);
        Assert.AreEqual(stage.Player.Position, stage.Player.DrawPosition);
    }

    [TestMethod]
    public void The_blend_never_changes_the_rules()
    {
        var plain = Settled();
        var blended = Settled();
        for (int i = 0; i < 120; i++)
        {
            var input = In(right: i < 90, fire: true, jump: i == 50, jumpHeld: i is >= 50 and < 60);
            Step(plain, input);
            Draw(blended, (i % 7) / 7f);
            _ = blended.Player.DrawPosition;
            Step(blended, input);
        }
        Assert.AreEqual(plain.Player.Position, blended.Player.Position);
        Assert.AreEqual(plain.Camera.Position, blended.Camera.Position);
        Assert.AreEqual(Actor.Count(plain.PlayerShots), Actor.Count(blended.PlayerShots));
    }

    [TestMethod]
    public void An_effect_plays_its_clip_once_whether_or_not_it_is_drawn()
    {
        var stage = Settled();
        var blast = stage.Spawn(new Effect("explosion", new Vector2(200, 200)));   // the sheet: 8 frames at 18 a second, 0.44 s, 26.7 updates
        Run(stage, 26, PlayerInput.None);
        Assert.IsTrue(blast.Alive, "still playing its eighth frame after 26 updates");
        Run(stage, 1, PlayerInput.None);
        Assert.IsFalse(blast.Alive, "gone once its eight frames have played");
    }
}
