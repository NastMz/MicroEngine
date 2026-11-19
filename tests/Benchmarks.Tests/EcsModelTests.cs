using System.Numerics;
using EcsModel;

namespace Benchmarks.Tests;

/// <summary>
/// ENTITY-1 reopened: the slice written on the scene world and on an Entity Component System can be compared only if
/// both are the same game. At each of the three steps of the comparison the two versions must show the same things,
/// in the same places, with the same health and effects, after every step of one script; and each rule is checked on
/// both, so they cannot agree by being wrong together.
/// </summary>
[TestClass]
public class EcsModelTests
{
    private static ISlice Make(string model, int step) => model == "simple" ? Slices.Simple(step) : Slices.Ecs(step);

    private static void Run(ISlice slice, int steps, params Command[] script)
    {
        for (int i = 0; i < steps; i++) slice.Step([.. script.Where(c => c.Step == i)]);
    }

    private static Seen Thing(ISlice slice, int tag) => slice.Things().Single(t => t.Tag == tag);

    [TestMethod]
    [DataRow(1), DataRow(2), DataRow(3)]
    public void Both_models_show_the_same_slice_at_every_step_of_the_script(int step)
    {
        var simple = Script.Trace(Slices.Simple(step), step);
        var ecs = Script.Trace(Slices.Ecs(step), step);
        for (int i = 0; i < simple.Count; i++) Assert.AreEqual(simple[i], ecs[i], $"step {step}, update {i}");
    }

    [TestMethod]
    public void The_scripts_reach_every_rule()
    {
        var first = Script.Trace(Slices.Simple(1), 1);
        StringAssert.Contains(first[^1], "kills ");
        Assert.IsFalse(first[^1].Contains("kills 0 "), "projectiles kill enemies in step 1");

        var second = Script.Trace(Slices.Simple(2), 2);
        Assert.IsTrue(second.Any(l => l.Contains(" B")) && second.Any(l => l.Contains(" F")) && second.Any(l => l.Contains(" S")), "burning, frozen and shielded all appear");
        Assert.IsTrue(second.Any(l => l.Contains(" B F") || l.Contains(" F S") || l.Contains(" B S")), "and combine on one enemy");
        Assert.AreNotEqual(first[^1], second[^1], "the effects change the outcome");

        var third = Script.Trace(Slices.Simple(3), 3);
        Assert.IsTrue(third.Any(l => l.Contains(" c3")), "crates are there");
        Assert.IsFalse(third[^1].Contains("crates 0 "), "and break");
        Assert.IsTrue(third.Any(l => !l.EndsWith("near ", StringComparison.Ordinal)), "the late question has answers");
        Assert.IsTrue(third.Any(l => l.Split(" | ").Any(t => t.StartsWith('c') && t.EndsWith(" B", StringComparison.Ordinal))), "a crate burns");
    }

    [TestMethod]
    [DataRow("simple", 1), DataRow("ecs", 1), DataRow("simple", 3), DataRow("ecs", 3)]
    public void An_enemy_moves_bounces_and_dies_of_projectiles(string model, int step)
    {
        var slice = Make(model, step);
        Run(slice, 60,
            new SpawnEnemy(0, 1, new Vector2(300, 50), new Vector2(60, 0), 5),
            new Shoot(30, new Vector2(100, 100), new Vector2(0, -100), 1));
        var enemy = Thing(slice, 1);
        Assert.IsTrue(enemy.At.X is > 270 and < 320, $"it turned at the edge: {enemy.At}");
        Assert.AreEqual(5, enemy.Health);
        Assert.AreEqual(1, slice.Shots, "a projectile that hits nothing flies on");

        slice = Make(model, step);
        Run(slice, 40,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 5),
            new SpawnEnemy(0, 2, new Vector2(100, 59), Vector2.Zero, 5),
            new Shoot(1, new Vector2(100, 80), new Vector2(0, -60), 3),
            new Shoot(2, new Vector2(100, 80), new Vector2(0, -60), 3));
        Assert.AreEqual(0, slice.Shots, "a projectile ends at what it hits");
        Assert.AreEqual(1, slice.Kills);
        Assert.AreEqual(1, slice.Things().Count, "the nearer enemy took both and died");
        Assert.AreEqual(5, Thing(slice, 1).Health);
    }

    [TestMethod]
    [DataRow("simple", 2), DataRow("ecs", 2), DataRow("simple", 3), DataRow("ecs", 3)]
    public void A_shield_absorbs_the_next_hit_and_only_that_one(string model, int step)
    {
        var slice = Make(model, step);
        Run(slice, 30,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 10),
            new Shield(1, 1),
            new Shoot(2, new Vector2(100, 70), new Vector2(0, -120), 4, Element.Fire));
        Assert.AreEqual(new Seen(1, 'e', new Vector2(100, 50), 10, false, false, false), Thing(slice, 1), "no damage, no fire, and the shield is spent");
        Run(slice, 30, new Shoot(0, new Vector2(100, 70), new Vector2(0, -120), 4));
        Assert.AreEqual(6, Thing(slice, 1).Health);
    }

    [TestMethod]
    [DataRow("simple", 2), DataRow("ecs", 2), DataRow("simple", 3), DataRow("ecs", 3)]
    public void Two_projectiles_that_arrive_together_hit_in_the_order_they_were_fired(string model, int step)
    {
        var slice = Make(model, step);
        Run(slice, 30,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 10),
            new Shield(1, 1),
            new Shoot(2, new Vector2(100, 70), new Vector2(0, -120), 1),
            new Shoot(2, new Vector2(100, 70), new Vector2(0, -120), 5));
        Assert.AreEqual(5, Thing(slice, 1).Health, "the shield took the first one fired, the second one landed");
    }

    [TestMethod]
    [DataRow("simple", 2), DataRow("ecs", 2), DataRow("simple", 3), DataRow("ecs", 3)]
    public void A_frozen_enemy_stands_still_shatters_for_double_and_thaws_in_fire(string model, int step)
    {
        var slice = Make(model, step);
        Run(slice, 30,
            new SpawnEnemy(0, 1, new Vector2(100, 50), new Vector2(30, 0), 20),
            new Shoot(1, new Vector2(100.5f, 60), new Vector2(0, -120), 2, Element.Ice));
        var frozen = Thing(slice, 1);
        Assert.IsTrue(frozen.Frozen);
        Assert.AreEqual(18, frozen.Health);
        Run(slice, 10);
        Assert.AreEqual(frozen.At, Thing(slice, 1).At, "frozen: it does not move");

        // A plain hit shatters the ice: twice the damage, and it moves again.
        Run(slice, 20, new Shoot(0, frozen.At + new Vector2(0, 10), new Vector2(0, -120), 3));
        Assert.AreEqual(new Seen(1, 'e', Thing(slice, 1).At, 12, false, false, false), Thing(slice, 1));
        Assert.IsTrue(Thing(slice, 1).At.X > frozen.At.X);

        // Fire on a frozen enemy removes the ice instead: plain damage, and it does not start burning.
        slice = Make(model, step);
        Run(slice, 40,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 20),
            new Shoot(1, new Vector2(100, 60), new Vector2(0, -120), 2, Element.Ice),
            new Shoot(20, new Vector2(100, 60), new Vector2(0, -120), 3, Element.Fire));
        Assert.AreEqual(new Seen(1, 'e', new Vector2(100, 50), 15, false, false, false), Thing(slice, 1));

        // Left alone, the ice ends by itself.
        slice = Make(model, step);
        Run(slice, 10 + Rules.FrozenSteps,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 20),
            new Shoot(1, new Vector2(100, 60), new Vector2(0, -120), 2, Element.Ice));
        Assert.IsFalse(Thing(slice, 1).Frozen);
    }

    [TestMethod]
    [DataRow("simple", 2), DataRow("ecs", 2), DataRow("simple", 3), DataRow("ecs", 3)]
    public void Burning_hurts_over_time_spreads_to_neighbours_thaws_the_frozen_and_ends(string model, int step)
    {
        var slice = Make(model, step);
        Run(slice, 8 + Rules.BurnTick,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 20),
            new SpawnEnemy(0, 2, new Vector2(100 + Rules.SpreadRadius - 1, 50), Vector2.Zero, 20),   // near enough
            new SpawnEnemy(0, 3, new Vector2(100 - Rules.SpreadRadius - 1, 50), Vector2.Zero, 20),   // too far
            new SpawnEnemy(0, 4, new Vector2(100, 50 + Rules.SpreadRadius - 1), Vector2.Zero, 20),   // near, and frozen
            new Shoot(1, new Vector2(100, 50 + Rules.SpreadRadius + 9), new Vector2(0, -120), 1, Element.Ice),
            new Shoot(1, new Vector2(100, 60), new Vector2(0, -120), 2, Element.Fire));
        Assert.AreEqual(new Seen(1, 'e', new Vector2(100, 50), 17, true, false, false), Thing(slice, 1), "the hit, then one point of fire");
        Assert.IsTrue(Thing(slice, 2).Burning, "fire reached the neighbour");
        Assert.AreEqual(20, Thing(slice, 2).Health, "which starts to hurt a tick later");
        Assert.IsFalse(Thing(slice, 3).Burning);
        Assert.AreEqual(new Seen(4, 'e', new Vector2(100, 50 + Rules.SpreadRadius - 1), 19, false, false, false), Thing(slice, 4), "fire took the ice and did not catch");

        // Alone, a fire ends after its time (among neighbours they keep lighting each other).
        slice = Make(model, step);
        Run(slice, 8 + Rules.BurnSteps,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 20),
            new Shoot(1, new Vector2(100, 60), new Vector2(0, -120), 2, Element.Fire));
        Assert.IsFalse(Thing(slice, 1).Burning, "the fire ends");
        Assert.AreEqual(20 - 2 - Rules.BurnSteps / Rules.BurnTick, Thing(slice, 1).Health);

        // Ice puts a fire out, and does not freeze what it put out.
        slice = Make(model, step);
        Run(slice, 40,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 20),
            new Shoot(1, new Vector2(100, 60), new Vector2(0, -120), 2, Element.Fire),
            new Shoot(15, new Vector2(100, 60), new Vector2(0, -120), 2, Element.Ice));
        Assert.AreEqual(new Seen(1, 'e', new Vector2(100, 50), 16, false, false, false), Thing(slice, 1));
    }

    [TestMethod]
    [DataRow("simple"), DataRow("ecs")]
    public void A_crate_burns_and_breaks_but_never_moves_freezes_or_takes_a_shield(string model)
    {
        var slice = Make(model, 3);
        Run(slice, 40,
            new SpawnCrate(0, 7, new Vector2(100, 50), 6),
            new Shield(1, 7),
            new Shoot(2, new Vector2(100, 60), new Vector2(0, -120), 1, Element.Ice),
            new Shoot(20, new Vector2(100, 60), new Vector2(0, -120), 1, Element.Fire));
        Assert.AreEqual(new Seen(7, 'c', new Vector2(100, 50), 4, true, false, false), Thing(slice, 7), "both hits landed: no shield, no ice, and it burns");
        CollectionAssert.AreEqual(new[] { 7 }, slice.BurningUnshieldedNear(new Vector2(110, 50), 15));
        Assert.AreEqual(0, slice.BurningUnshieldedNear(new Vector2(130, 50), 15).Count);

        Run(slice, Rules.BurnSteps);
        Assert.AreEqual(0, slice.Things().Count, "the fire finished it");
        Assert.AreEqual(1, slice.CratesBroken);
        Assert.AreEqual(0, slice.Kills, "a crate is not a kill");
    }

    [TestMethod]
    [DataRow("simple"), DataRow("ecs")]
    public void The_late_question_leaves_out_the_shielded_and_the_far(string model)
    {
        var slice = Make(model, 3);
        Run(slice, 12 + Rules.BurnTick,
            new SpawnEnemy(0, 1, new Vector2(100, 50), Vector2.Zero, 20),
            new SpawnEnemy(0, 2, new Vector2(110, 50), Vector2.Zero, 20),
            new SpawnCrate(0, 3, new Vector2(100, 60), 20),
            new SpawnEnemy(0, 4, new Vector2(100, 150), Vector2.Zero, 20),
            new Shoot(1, new Vector2(100, 40), new Vector2(0, 120), 1, Element.Fire),
            new Shoot(1, new Vector2(100, 160), new Vector2(0, -120), 1, Element.Fire),
            new Shield(10 + Rules.BurnTick, 2));
        // Fire spread from 1 to 2 and to the crate; 2 was shielded afterwards, while burning; 4 burns far away.
        Assert.IsTrue(Thing(slice, 2) is { Burning: true, Shielded: true });
        CollectionAssert.AreEqual(new[] { 1, 3 }, slice.BurningUnshieldedNear(new Vector2(100, 50), 30));
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, slice.BurningUnshieldedNear(new Vector2(100, 100), 500));
    }

    /// <summary>
    /// Why the component-store version settles hits in the order the projectiles were fired instead of the order of
    /// its walk: the library (pinned to the version observed) walks a chunk's rows from the last to the first with a
    /// delegate, and fills the place of a removed row with the last one, so no walk promises the order of creation.
    /// The scene world walks attachments in attach order, whatever was removed.
    /// </summary>
    [TestMethod]
    public void The_library_walks_rows_in_an_order_that_is_not_the_order_of_creation()
    {
        var world = Arch.Core.World.Create();
        try
        {
            var tagged = new Arch.Core.QueryDescription().WithAll<EcsModel.Ecs.Step1.Tag>();
            var first = world.Create(new EcsModel.Ecs.Step1.Tag { Value = 1 });
            world.Create(new EcsModel.Ecs.Step1.Tag { Value = 2 });
            world.Create(new EcsModel.Ecs.Step1.Tag { Value = 3 });
            var order = new List<int>();
            world.Query(in tagged, (ref EcsModel.Ecs.Step1.Tag tag) => order.Add(tag.Value));
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, order, "a walk with a delegate");

            world.Destroy(first);
            order.Clear();
            foreach (ref var chunk in world.Query(in tagged))
                foreach (ref var tag in chunk.GetSpan<EcsModel.Ecs.Step1.Tag>()[..chunk.Count]) order.Add(tag.Value);
            CollectionAssert.AreEqual(new[] { 3, 2 }, order, "a walk over a chunk's rows, after the first was removed");
        }
        finally { Arch.Core.World.Destroy(world); }
    }
}
