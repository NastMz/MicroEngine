using System.Numerics;
using NeonRift.Content;
using NeonRift.Gameplay;
using static NeonRift.Tests.Harness;

namespace NeonRift.Tests;

[TestClass]
public sealed class PlayerTests
{
    [TestMethod]
    public void Player_spawns_standing_on_the_ground()
    {
        var stage = Settled();
        Assert.IsTrue(stage.Player.Grounded);
        Assert.AreEqual(StageBuilder.Surface(3), Feet(stage).Y, 0.01f);
    }

    [TestMethod]
    public void Aims_in_eight_directions_from_the_ground_and_air()
    {
        var stage = Settled();
        Run(stage, 2, In(up: true));
        Assert.AreEqual(new Vector2(0, -1), stage.Player.Aim, "Up alone aims straight up.");
        Run(stage, 2, In(up: true, right: true));
        Assert.AreEqual(Vector2.Normalize(new Vector2(1, -1)), stage.Player.Aim);
        Run(stage, 2, In(down: true, right: true));
        Assert.AreEqual(Vector2.Normalize(new Vector2(1, 1)), stage.Player.Aim, "Down while walking aims low.");
        Run(stage, 2, In(down: true));
        Assert.IsTrue(stage.Player.Crouching, "Down alone on the ground kneels.");
        Assert.AreEqual(new Vector2(1, 0), stage.Player.Aim, "Kneeling shoots forward.");
        Run(stage, 1, In(jump: true, jumpHeld: true));
        Run(stage, 6, In(down: true, jumpHeld: true));
        Assert.IsFalse(stage.Player.Grounded);
        Assert.AreEqual(new Vector2(0, 1), stage.Player.Aim, "Down in the air aims straight down.");
    }

    [TestMethod]
    public void Lock_plants_the_feet_while_aiming_diagonally()
    {
        var stage = Settled();
        float x = Feet(stage).X;
        Run(stage, 20, In(right: true, up: true, @lock: true));
        Assert.AreEqual(x, Feet(stage).X, 0.01f);
        Assert.AreEqual(Vector2.Normalize(new Vector2(1, -1)), stage.Player.Aim);
    }

    [TestMethod]
    public void Full_jump_clears_four_tiles_and_a_tap_is_lower()
    {
        float Peak(int hold)
        {
            var stage = Settled();
            float start = Feet(stage).Y, best = start;
            for (int i = 0; i < 70; i++)
            {
                stage.Update(Dt, In(jump: i == 0, jumpHeld: i < hold));
                best = MathF.Min(best, Feet(stage).Y);
            }
            return start - best;
        }
        float full = Peak(60), tap = Peak(2);
        Assert.IsGreaterThan(64f, full, $"Full jump {full} px.");
        Assert.IsLessThan(full * 0.6f, tap, $"Tap {tap} px vs full {full} px.");
    }

    [TestMethod]
    public void Down_and_jump_drops_through_a_platform()
    {
        var def = Flat(extra: b => b.PlatformAt(2, 6, 6));
        var stage = Stage(def);
        Run(stage, 40, PlayerInput.None);
        Assert.AreEqual(StageBuilder.Surface(6), Feet(stage).Y, 0.01f, "Spawned on the platform.");
        Run(stage, 1, In(down: true, jump: true, jumpHeld: true));
        Run(stage, 40, PlayerInput.None);
        Assert.AreEqual(StageBuilder.Surface(3), Feet(stage).Y, 0.01f);
    }

    [TestMethod]
    public void Spread_fires_five_pellets_and_respects_the_cooldown()
    {
        var stage = Settled();
        stage.Player.Equip(WeaponKind.Spread);
        Run(stage, 1, In(fire: true));
        Assert.AreEqual(5, stage.PlayerShots.Count);
        Run(stage, 5, In(fire: true));
        Assert.AreEqual(5, stage.PlayerShots.Count, "Still cooling down.");
        Run(stage, 20, In(fire: true));
        Assert.IsGreaterThan(5, stage.PlayerShots.Count);
    }

    [TestMethod]
    public void Shots_leave_from_the_aimed_direction()
    {
        var stage = Settled();
        Run(stage, 1, In(up: true, fire: true));
        var shot = stage.PlayerShots.Single();
        Assert.AreEqual(0, shot.Velocity.X, 0.01f);
        Assert.IsLessThan(0f, shot.Velocity.Y);
    }

    [TestMethod]
    public void Camera_only_scrolls_forward()
    {
        var stage = Settled();
        Run(stage, 180, In(right: true));
        float x = stage.Camera.Position.X;
        Assert.IsGreaterThan(0f, x);
        Run(stage, 120, In(left: true));
        Assert.AreEqual(x, stage.Camera.Position.X, 0.01f);
        Assert.IsGreaterThanOrEqualTo(stage.Camera.View.Left + 8 - 0.01f, Feet(stage).X, "The screen edge holds the player back.");
    }
}
