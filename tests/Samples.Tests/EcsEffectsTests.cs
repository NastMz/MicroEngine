using System.Numerics;
using Arch.Core;
using EcsEffects;
using MicroEngine;
using MicroEngine.Core;

namespace Samples.Tests;

/// <summary>
/// The modelling sample, played headlessly: effects are components that come and go on live rows, a kind of thing is
/// a choice of components, and behaviours are systems the scene lists in order.
/// </summary>
[TestClass]
public sealed class EcsEffectsTests
{
    private static readonly QueryDescription Everything = new();
    private static readonly QueryDescription Bolts = new QueryDescription().WithAll<Shot>();

    /// <summary>The game with an empty arena and no walkers arriving, for a test that places its own things.</summary>
    private static (EffectsGame Game, ArenaScene Scene, World World) Empty()
    {
        var game = new EffectsGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        var scene = (ArenaScene)game.Scenes.Current!;
        scene.Timers.Clear();
        scene.Ecs.World.Destroy(in Everything);
        return (game, scene, scene.Ecs.World);
    }

    private static void Step(EffectsGame game, int steps)
    {
        for (int i = 0; i < steps; i++) game.StepForTest();
    }

    private static readonly Vector2 Up = new(0, -ArenaScene.BoltSpeed);

    [TestMethod]
    public void The_scene_lists_its_systems_in_order_and_the_arena_starts_with_walkers_and_crates()
    {
        var game = new EffectsGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        var scene = (ArenaScene)game.Scenes.Current!;
        Assert.AreEqual(3, scene.Ecs.Systems.Count);
        Assert.AreEqual(1, scene.Ecs.LateSystems.Count);
        Assert.AreEqual(6, scene.Ecs.World.CountEntities(new QueryDescription().WithAll<Crate>()));
        Assert.AreEqual(8, scene.Ecs.World.CountEntities(new QueryDescription().WithAll<Freezes, Shieldable>()));
        Assert.AreEqual(3, scene.Ecs.World.CountEntities(new QueryDescription().WithAll<Shielded>()));
        Assert.AreEqual(0, scene.Ecs.LinkCount, "nothing here needs an entity of the scene's world");
    }

    [TestMethod]
    public void A_shield_takes_one_hit_then_fire_catches_and_hurts_over_time()
    {
        var (game, scene, world) = Empty();
        var walker = scene.AddWalker(new Vector2(100, 60), Vector2.Zero, shielded: true);
        scene.Shoot(new Vector2(100, 80), Up, 2, Element.Fire);
        Step(game, 12);
        Assert.IsFalse(world.Has<Shielded>(walker), "the shield is spent");
        Assert.IsFalse(world.Has<Burning>(walker));
        Assert.AreEqual(ArenaScene.WalkerHealth, world.Get<Health>(walker).Value, "and took the whole hit");
        Assert.AreEqual(0, world.CountEntities(in Bolts), "a bolt ends at what it hits");

        scene.Shoot(new Vector2(100, 80), Up, 2, Element.Fire);
        Step(game, 12);
        Assert.IsTrue(world.Has<Burning>(walker));
        Assert.AreEqual(ArenaScene.WalkerHealth - 2, world.Get<Health>(walker).Value);
        Step(game, Rules.BurnTick);
        Assert.AreEqual(ArenaScene.WalkerHealth - 3, world.Get<Health>(walker).Value, "fire hurts at each tick");
        Step(game, Rules.BurnSteps);
        Assert.IsFalse(world.IsAlive(walker), "and it died of it");
        Assert.AreEqual(1, scene.Ecs.LateSystems.Get<Deaths>().Kills);
    }

    [TestMethod]
    public void Ice_stops_a_walker_a_plain_bolt_shatters_it_and_fire_only_thaws_it()
    {
        var (game, scene, world) = Empty();
        var walker = scene.AddWalker(new Vector2(100, 60), new Vector2(0, 0.5f));
        scene.Shoot(new Vector2(100, 80), Up, 2, Element.Ice);
        Step(game, 12);
        Assert.IsTrue(world.Has<Frozen>(walker));
        var at = world.Get<Position>(walker).Value;
        Step(game, 10);
        Assert.AreEqual(at, world.Get<Position>(walker).Value, "frozen: the movement system leaves it out");

        scene.Shoot(at + new Vector2(0, 20), Up, 2, Element.Plain);
        Step(game, 12);
        Assert.IsFalse(world.Has<Frozen>(walker));
        Assert.AreEqual(ArenaScene.WalkerHealth - 2 - 4, world.Get<Health>(walker).Value, "shattered: twice the damage");
        Assert.IsTrue(world.Get<Position>(walker).Value.Y > at.Y, "and it moves again");

        var other = scene.AddWalker(new Vector2(200, 60), Vector2.Zero);
        scene.Shoot(new Vector2(200, 80), Up, 2, Element.Ice);
        Step(game, 12);
        scene.Shoot(new Vector2(200, 80), Up, 2, Element.Fire);
        Step(game, 12);
        Assert.IsFalse(world.Has<Frozen>(other) || world.Has<Burning>(other), "fire took the ice and did not catch");
        Assert.AreEqual(ArenaScene.WalkerHealth - 2 - 2, world.Get<Health>(other).Value);
    }

    [TestMethod]
    public void A_crate_shares_fire_and_damage_with_a_walker_but_not_ice_shields_or_movement()
    {
        var (game, scene, world) = Empty();
        var crate = scene.AddCrate(new Vector2(100, 60));
        var near = scene.AddWalker(new Vector2(100 + Rules.SpreadRadius - 2, 60), Vector2.Zero);
        var far = scene.AddWalker(new Vector2(100 + Rules.SpreadRadius * 3, 60), Vector2.Zero);
        scene.Shoot(new Vector2(100, 80), Up, 1, Element.Ice);
        Step(game, 12);
        Assert.IsFalse(world.Has<Frozen>(crate), "a crate does not freeze");
        Assert.AreEqual(ArenaScene.CrateHealth - 1, world.Get<Health>(crate).Value, "but the bolt hurt it");

        scene.Shoot(new Vector2(100, 80), Up, 1, Element.Fire);
        Step(game, 12 + Rules.BurnTick);
        Assert.IsTrue(world.Has<Burning>(crate), "it burns like anything else");
        Assert.AreEqual(new Vector2(100, 60), world.Get<Position>(crate).Value);
        Assert.IsTrue(world.Has<Burning>(near), "and its fire reaches the walker beside it");
        Assert.IsFalse(world.Has<Burning>(far));

        Step(game, Rules.BurnSteps * 2);   // the walker beside it lights it again when its first fire ends
        Assert.IsFalse(world.IsAlive(crate));
        var deaths = scene.Ecs.LateSystems.Get<Deaths>();
        Assert.AreEqual(1, deaths.CratesBroken, "a crate is counted as broken");
    }

    [TestMethod]
    public void The_question_finds_burning_things_near_a_point_that_have_no_shield()
    {
        var (game, scene, world) = Empty();
        var crate = scene.AddCrate(new Vector2(100, 60));
        var open = scene.AddWalker(new Vector2(110, 60), Vector2.Zero);
        var shielded = scene.AddWalker(new Vector2(90, 60), Vector2.Zero);
        var away = scene.AddWalker(new Vector2(250, 60), Vector2.Zero);
        foreach (var row in (Entity[])[crate, open, shielded, away]) Effects.Ignite(world, row);
        world.Add<Shielded>(shielded);   // shielded after it caught fire: burning, and left out

        var found = new List<Entity>();
        Assert.AreEqual(2, scene.BurningNear(new Vector2(100, 60), 30, found));
        CollectionAssert.AreEquivalent(new[] { crate, open }, found);
        Assert.AreEqual(3, scene.BurningNear(new Vector2(100, 60), 500));
        Step(game, 1);
        Assert.AreEqual(2, scene.BurningNear(new Vector2(100, 60), 30));
    }

    [TestMethod]
    public void A_system_switched_off_stops_its_behaviour_and_nothing_else()
    {
        var (game, scene, world) = Empty();
        var walker = scene.AddWalker(new Vector2(100, 60), new Vector2(30, 0));
        Effects.Ignite(world, walker);
        scene.Ecs.Systems.Get<Effects>().Enabled = false;
        Step(game, Rules.BurnTick * 3);
        Assert.AreEqual(ArenaScene.WalkerHealth, world.Get<Health>(walker).Value, "no effect passes its time");
        Assert.IsTrue(world.Get<Position>(walker).Value.X > 100, "movement goes on");

        scene.Ecs.Systems.Get<Effects>().Enabled = true;
        Step(game, Rules.BurnTick);
        Assert.AreEqual(ArenaScene.WalkerHealth - 1, world.Get<Health>(walker).Value);
    }

    [TestMethod]
    public void The_player_moves_the_caster_switches_what_is_loaded_and_shoots()
    {
        var (game, scene, world) = Empty();
        var start = scene.Caster;
        game.Input.SetForced("left", true);
        Step(game, 30);
        game.Input.SetForced("left", false);
        Assert.IsTrue(scene.Caster.X < start.X - 30);

        Assert.AreEqual(Element.Fire, scene.Loaded);
        game.Input.SetForced("cancel", true);
        Step(game, 1);
        game.Input.SetForced("cancel", false);
        Assert.AreEqual(Element.Ice, scene.Loaded);
        game.Input.SetForced("confirm", true);
        Step(game, 1);
        game.Input.SetForced("confirm", false);
        Step(game, 1);
        Assert.AreEqual(1, world.CountEntities(in Bolts));
        Step(game, Rules.ShotLife + 2);
        Assert.AreEqual(0, world.CountEntities(in Bolts), "a bolt that hits nothing ends");
    }

    [TestMethod]
    public void Two_runs_of_the_tour_end_in_the_same_state_and_a_different_seed_does_not()
    {
        static ulong Play(int seed)
        {
            var game = new EffectsGame();
            game.InitializeForTest(HeadlessPlatform.For(game.Settings), seed);
            var scene = (ArenaScene)game.Scenes.Current!;
            for (int i = 0; i < 600; i++)
            {
                if (i % 40 == 5) scene.Shoot(scene.Caster, new Vector2(0, -ArenaScene.BoltSpeed), 2, (Element)(i / 40 % 3));
                game.StepForTest();
            }
            return game.ComputeStateHash().Hash;
        }
        Assert.AreEqual(Play(1), Play(1));
        Assert.AreNotEqual(Play(1), Play(2));
    }

    [TestMethod]
    public void A_whole_run_draws_and_ends_without_error()
    {
        var game = new EffectsGame();
        var platform = HeadlessPlatform.For(game.Settings);
        var script = new InputScript().Press(5, "confirm").Press(40, "cancel").Press(50, "confirm").QuitAfter(200);
        var result = GameRunner.Run(game, platform, new RunOptions { Script = script });
        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.IsTrue(platform.Renderer.DrawCalls > 0);
    }
}
