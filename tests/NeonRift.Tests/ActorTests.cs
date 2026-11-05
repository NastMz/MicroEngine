using System.Numerics;
using NeonRift.Content;
using NeonRift.Gameplay;
using static NeonRift.Tests.Harness;

namespace NeonRift.Tests;

/// <summary>Actors as entities of the stage's world: spawn order, destruction and what stays readable afterwards.</summary>
[TestClass]
public sealed class ActorTests
{
    [TestMethod]
    public void Spawned_actors_are_walked_in_spawn_order_and_destroyed_ones_are_skipped_at_once()
    {
        var stage = Settled();
        var feet = Feet(stage);
        var a = stage.Spawn(new Gunner(feet + new Vector2(60, 0)));
        var b = stage.Spawn(new Gunner(feet + new Vector2(100, 0)));
        var c = stage.Spawn(new Gunner(feet + new Vector2(140, 0)));
        CollectionAssert.AreEqual(new Enemy[] { a, b, c }, stage.Enemies.ToList());
        b.Destroy();
        Assert.IsFalse(b.Alive);
        CollectionAssert.AreEqual(new Enemy[] { a, c }, stage.Enemies.ToList(), "skipped before the step ends");
        Run(stage, 1, PlayerInput.None);
        CollectionAssert.AreEqual(new Enemy[] { a, c }, stage.Enemies.ToList(), "and gone after it");
    }

    [TestMethod]
    public void A_released_actor_keeps_its_last_position_and_stays_dead()
    {
        var stage = Settled();
        var boss = stage.Spawn(Boss.Create(BossKind.Gunship, stage.Player.Position + new Vector2(200, -100)));
        Run(stage, 1, PlayerInput.None);
        var at = boss.Position;
        boss.Destroy();
        Run(stage, 1, PlayerInput.None);
        Assert.IsFalse(boss.Alive);
        Assert.AreEqual(at, boss.Position, "the clear sequence still explodes around a defeated boss");
        Assert.IsTrue(boss.Id.IsNone);
    }

    [TestMethod]
    public void An_actor_joins_one_stage_only()
    {
        var stage = Settled();
        var shot = stage.Spawn(new EnemyShot(new Vector2(300, 200), Vector2.Zero));
        Assert.ThrowsExactly<InvalidOperationException>(() => stage.Spawn(shot));
        Assert.ThrowsExactly<InvalidOperationException>(() => Settled().Spawn(shot));
    }

    [TestMethod]
    public void An_actor_destroyed_before_it_spawns_never_appears()
    {
        var stage = Settled();
        var shot = new EnemyShot(new Vector2(300, 200), Vector2.Zero);
        shot.Destroy();
        stage.Spawn(shot);
        Assert.IsFalse(shot.Alive);
        Assert.AreEqual(0, Actor.Count(stage.EnemyShots));
    }
}
