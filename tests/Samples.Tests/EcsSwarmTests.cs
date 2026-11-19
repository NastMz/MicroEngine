using System.Numerics;
using Arch.Core;
using EcsSwarm;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Ecs;
using MicroEngine.Scenes;

namespace Samples.Tests;

/// <summary>
/// The ECS sample, played headlessly: a component store (the optional package) runs beside the scene's world as one
/// of the scene's systems, and the two stay in step.
/// </summary>
[TestClass]
public sealed class EcsSwarmTests
{
    private static readonly QueryDescription Members = new QueryDescription().WithAll<Chase>();
    private static readonly QueryDescription Sparks = new QueryDescription().WithAll<Spark>();
    private static readonly QueryDescription Twins = new QueryDescription().WithAll<Position, SceneLink>();
    private static readonly QueryDescription MemberPlaces = new QueryDescription().WithAll<Position, Chase>();

    private static (SwarmGame Game, SwarmScene Scene) Start(int seed = 1)
    {
        var game = new SwarmGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings), seed);
        return (game, (SwarmScene)game.Scenes.Current!);
    }

    private static void Step(SwarmGame game, int steps)
    {
        for (int i = 0; i < steps; i++) game.StepForTest();
    }

    [TestMethod]
    public void The_swarm_lives_in_the_store_and_the_world_holds_only_what_needs_a_hierarchy()
    {
        var (_, scene) = Start();

        Assert.AreEqual(SwarmScene.Members + SwarmScene.Carriers, scene.Rows.Size, "one row per member and per carrier");
        Assert.AreEqual(2 + 2 * SwarmScene.Carriers, scene.Objects.Count, "the beacon and its halo, each carrier and its flag");
        Assert.AreEqual(scene.Beacon, scene.Objects.ParentOf(scene.Halo));
        foreach (var twin in scene.CarrierTwins)
            Assert.AreEqual(1, scene.Objects.Children(twin).Count(), "a carrier's flag is its child in the world");
    }

    [TestMethod]
    public void The_swarm_chases_the_beacon_and_what_it_catches_becomes_a_spark_and_a_new_member()
    {
        var (game, scene) = Start();
        var beacon = scene.Objects.Local(scene.Beacon).Position2D;
        float before = MeanDistance(scene, beacon);

        Step(game, 45);
        Assert.IsTrue(MeanDistance(scene, beacon) < before - 5, $"the swarm closes in: {before} then {MeanDistance(scene, beacon)}");
        Step(game, 75);

        Assert.IsTrue(scene.Caught > 0, "members reached the beacon");
        Assert.AreEqual(SwarmScene.Members, scene.Rows.CountEntities(in Members), "every caught member is replaced");
        Assert.IsTrue(scene.Rows.CountEntities(in Sparks) > 0, "and leaves a spark");

        // A spark lasts its steps: the ones alive are exactly those of the members caught in the last SparkSteps steps.
        int caught = scene.Caught;
        Step(game, SwarmScene.SparkSteps);
        Assert.AreEqual(scene.Caught - caught, scene.Rows.CountEntities(in Sparks));
        Assert.AreEqual(SwarmScene.Members, scene.Rows.CountEntities(in Members));
    }

    private static float MeanDistance(SwarmScene scene, Vector2 point)
    {
        double sum = 0;
        int count = 0;
        foreach (ref var chunk in scene.Rows.Query(in MemberPlaces))
            foreach (ref var p in chunk.GetSpan<Position>()[..chunk.Count])
            {
                sum += Vector2.Distance(p.Value, point);
                count++;
            }
        return (float)(sum / count);
    }

    [TestMethod]
    public void A_carrier_row_moves_its_world_entity_and_the_world_carries_the_flag()
    {
        var (game, scene) = Start();
        Step(game, 30);

        int carriers = 0;
        foreach (ref var chunk in scene.Rows.Query(in Twins))
        {
            var positions = chunk.GetSpan<Position>()[..chunk.Count];
            var twins = chunk.GetSpan<SceneLink>()[..chunk.Count];
            for (int i = 0; i < positions.Length; i++)
            {
                Assert.AreEqual(positions[i].Value, scene.Objects.Local(twins[i].Entity).Position2D, "the row's place is the entity's");
                var flag = scene.Objects.Children(twins[i].Entity).First();
                Assert.AreEqual(positions[i].Value + new Vector2(0, -7), scene.Objects.DrawPosition2D(flag), "the child follows through the hierarchy");
                carriers++;
            }
        }
        Assert.AreEqual(SwarmScene.Carriers, carriers);
    }

    [TestMethod]
    public void Destroying_a_carriers_entity_removes_its_row_when_the_step_ends()
    {
        var (game, scene) = Start();
        var twin = scene.CarrierTwins[0];
        var row = scene.Objects.Get<EcsRow>(twin).Row;
        Assert.IsTrue(scene.Rows.IsAlive(row));

        scene.Objects.Destroy(twin);
        Assert.IsTrue(scene.Rows.IsAlive(row), "the world releases a destroyed entity when the step ends, and tells its attachments then");
        Step(game, 1);

        Assert.IsFalse(scene.Rows.IsAlive(row), "the row went with the entity");
        Assert.AreEqual(SwarmScene.Carriers - 1, scene.Rows.CountEntities(in Twins));
        Assert.AreEqual(SwarmScene.Carriers - 1, scene.CarrierTwins.Count);
        Assert.AreEqual(2 + 2 * (SwarmScene.Carriers - 1), scene.Objects.Count, "the flag went with its parent");
    }

    [TestMethod]
    public void Removing_a_carriers_row_destroys_its_entity_and_a_later_row_in_the_same_place_is_untouched()
    {
        var (game, scene) = Start();
        var twin = scene.CarrierTwins[2];
        var row = scene.Objects.Get<EcsRow>(twin).Row;

        scene.Rows.Destroy(row);   // the library's own call: the store finds the link broken when the step ends
        Assert.IsFalse(scene.Rows.IsAlive(row));
        Assert.IsFalse(scene.Ecs.TryGetRow(twin, out _), "the entity has no row any more, for every question");
        // A new row takes the removed row's place in the store before the step ends.
        var newcomer = scene.Rows.Create(new Position { Value = new Vector2(1, 2) });
        Assert.AreEqual(row.Id, newcomer.Id, "the store reuses the place");
        Step(game, 1);

        Assert.IsFalse(scene.Objects.IsAlive(twin), "the entity went with its row");
        Assert.IsTrue(scene.Rows.IsAlive(newcomer), "the stale handle did not reach the row that reused its place");
        Assert.AreEqual(new Vector2(1, 2), scene.Rows.Get<Position>(newcomer).Value);
        Assert.AreEqual(SwarmScene.Carriers - 1, scene.Rows.CountEntities(in Twins));
        Assert.AreEqual(2 + 2 * (SwarmScene.Carriers - 1), scene.Objects.Count);
        Assert.IsFalse(scene.Ecs.Unlink(row), "the old handle names no row");
        Step(game, 1);
        Assert.IsTrue(scene.Rows.IsAlive(newcomer));
        Assert.AreEqual(SwarmScene.Carriers - 1, scene.Rows.CountEntities(in Twins));
    }

    [TestMethod]
    public void The_confirm_action_drops_a_carrier_from_the_worlds_side()
    {
        var (game, scene) = Start();
        for (int press = 0; press < 2; press++)
        {
            game.Input.SetForced("confirm", true);
            game.StepForTest();
            game.Input.SetForced("confirm", false);
            game.StepForTest();
        }
        Assert.AreEqual(SwarmScene.Carriers - 2, scene.CarrierTwins.Count);
        Assert.AreEqual(SwarmScene.Carriers - 2, scene.Rows.CountEntities(in Twins));
        Assert.AreEqual(2 + 2 * (SwarmScene.Carriers - 2), scene.Objects.Count);
    }

    [TestMethod]
    public void The_stores_state_is_part_of_the_state_hash_and_two_runs_agree()
    {
        var (first, firstScene) = Start();
        var (second, _) = Start();
        Step(first, 90);
        Step(second, 90);
        Assert.AreEqual(first.ComputeStateHash().Hash, second.ComputeStateHash().Hash, "the same seed and steps end in the same state");

        var (other, _) = Start(seed: 2);
        Step(other, 90);
        Assert.AreNotEqual(first.ComputeStateHash().Hash, other.ComputeStateHash().Hash);

        // A change only the store knows about changes the hash: the store is one of the scene's systems.
        ulong before = first.ComputeStateHash().Hash;
        foreach (ref var chunk in firstScene.Rows.Query(in MemberPlaces))
        {
            chunk.GetSpan<Position>()[0].Value += new Vector2(0.25f, 0);
            break;
        }
        Assert.AreNotEqual(before, first.ComputeStateHash().Hash);
    }

    [TestMethod]
    public void A_whole_run_draws_every_member_and_ends_without_error()
    {
        var game = new SwarmGame();
        var platform = HeadlessPlatform.For(game.Settings);
        var result = GameRunner.Run(game, platform, new RunOptions { Script = new InputScript().QuitAfter(10) });
        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.IsTrue(platform.Renderer.DrawCalls >= SwarmScene.Members, $"{platform.Renderer.DrawCalls} draws");
    }

    [TestMethod]
    public void Playing_allocates_nothing_once_the_stores_buffers_have_grown()
    {
        var (game, _) = Start();
        Step(game, 240);
        long before = GC.GetAllocatedBytesForCurrentThread();
        Step(game, 60);
        long perStep = (GC.GetAllocatedBytesForCurrentThread() - before) / 60;
        Assert.AreEqual(0, perStep, $"{perStep} bytes a step");
    }

    [TestMethod]
    public void Leaving_the_scene_releases_the_store()
    {
        var (game, scene) = Start();
        Step(game, 5);
        var rows = scene.Rows;
        Assert.AreSame(rows, World.Worlds[rows.Id], "the library lists the store while the scene runs");

        game.Scenes.Pop();
        Step(game, 1);

        Assert.AreNotSame(rows, World.Worlds[rows.Id], "the library no longer lists the store the scene left");
    }
}
