using System.Numerics;
using NeonRift.Gameplay;
using static NeonRift.Tests.Harness;

namespace NeonRift.Tests;

/// <summary>Drawing between updates: what is drawn follows <see cref="Stage.Blend"/>, the rules never do.</summary>
[TestClass]
public sealed class DrawBlendTests
{
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

        stage.Blend = 0.25f;
        Assert.AreEqual(Vector2.Lerp(before, after, 0.25f), stage.Player.DrawPosition);
        Assert.AreEqual(cameraBefore, stage.PreviousCameraPosition);
        Assert.AreEqual(Vector2.Lerp(cameraBefore, stage.Camera.Position, 0.25f), stage.Between(stage.PreviousCameraPosition, stage.Camera.Position));

        stage.Blend = 1;
        Assert.AreEqual(after, stage.Player.DrawPosition, "Blend 1 draws exactly the current update.");
    }

    [TestMethod]
    public void A_moving_shot_is_drawn_where_it_spawned_until_its_first_update()
    {
        var stage = Settled();
        var shot = stage.Spawn(new EnemyShot(new Vector2(300, 200), new Vector2(-120, 0)));
        stage.Blend = 0.5f;
        Assert.AreEqual(shot.Position, shot.DrawPosition, "No previous position yet: no streak from the origin.");

        stage.Blend = 1;
        Run(stage, 1, PlayerInput.None);
        stage.Blend = 0.5f;
        Assert.IsTrue(shot.Alive);
        Assert.AreEqual(Vector2.Lerp(new Vector2(300, 200), shot.Position, 0.5f), shot.DrawPosition);
    }

    [TestMethod]
    public void Respawning_snaps_the_player_instead_of_drawing_a_streak()
    {
        var stage = Settled();
        Run(stage, 10, In(right: true));
        stage.Player.Respawn(stage.Camera.View.Left + 200, WeaponKind.Pulse);
        stage.Blend = 0;
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
            plain.Update(Dt, input);
            blended.Blend = (i % 7) / 7f;
            blended.Update(Dt, input);
        }
        Assert.AreEqual(plain.Player.Position, blended.Player.Position);
        Assert.AreEqual(plain.Camera.Position, blended.Camera.Position);
        Assert.AreEqual(plain.PlayerShots.Count, blended.PlayerShots.Count);
    }
}
