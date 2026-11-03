using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Worlds;

/// <summary>
/// The conformance suite of <see cref="IWorld"/>: every rule of the contract (ids, destroy and creation timing, attach
/// order, attachments, hierarchy, interpolation and teleport, per-world step tracking, clocks, storage, state hash),
/// checked on <see cref="SceneWorld"/> through its public interface.
/// </summary>
[TestClass]
public sealed class WorldRulesTests
{
    private const float Dt = 1 / 60f;

    private sealed class Tag(string name) { public string Name { get; } = name; public override string ToString() => Name; }
    private class Base { }
    private sealed class Derived : Base { }

    private sealed class Notified(List<string> log, string name) : IDestroyedCallback
    {
        public IWorld? World { get; set; }
        public Action<EntityId>? Then { get; set; }
        public void OnDestroyed(EntityId entity)
        {
            log.Add($"{name}:{World!.Local(entity).Position2D.X}");
            Then?.Invoke(entity);
        }
    }

    private sealed class Health(int value) : IStateHashed
    {
        public int Value { get; set; } = value;
        public void HashState(StateHasher hash) => hash.Add(Value);
    }

    private static List<string> Names(IWorld world)
    {
        var names = new List<string>();
        foreach (var tag in world.All<Tag>()) names.Add(tag.Name);
        return names;
    }

    private static EntityId Tagged(IWorld world, string name)
    {
        var id = world.Create();
        world.Attach(id, new Tag(name));
        return id;
    }

    private static ulong Hash(SceneWorld world)
    {
        var hash = new StateHasher();
        world.HashState(hash);
        return hash.Value;
    }

    // ------------------------------------------------------------------ 1. ids

    [TestMethod]
    public void A_new_entity_is_live_at_identity()
    {
        IWorld world = new SceneWorld();
        var a = world.Create();
        var b = world.Create();

        Assert.IsFalse(a.IsNone);
        Assert.AreNotEqual(a, b);
        Assert.IsTrue(world.IsAlive(a));
        Assert.AreEqual(2, world.Count);
        Assert.AreEqual(Vector3.Zero, world.Local(a).Position);
        Assert.AreEqual(Quaternion.Identity, world.Local(a).Rotation);
        Assert.AreEqual(Vector3.One, world.Local(a).Scale);
        Assert.AreEqual(EntityId.None, world.ParentOf(a));
        Assert.IsFalse(world.IsAlive(EntityId.None));
    }

    [TestMethod]
    public void None_is_the_default_id_and_refers_to_nothing()
    {
        IWorld world = new SceneWorld();
        Assert.AreEqual(default, EntityId.None);
        Assert.IsTrue(EntityId.None.IsNone);
        Assert.AreEqual("entity:none", EntityId.None.ToString());
        var e = world.Create();
        Assert.AreEqual($"entity:{e.Index}#{e.Generation}", e.ToString());
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => world.Local(EntityId.None));
        StringAssert.Contains(error.Message, "entity:none");
        Assert.IsFalse(world.TryGet<Tag>(EntityId.None, out _));
    }

    [TestMethod]
    public void A_reused_slot_gets_a_new_generation_and_the_old_id_reaches_nothing()
    {
        var world = new SceneWorld();
        var a = Tagged(world, "a");
        world.Destroy(a);
        world.EndStep();

        var b = Tagged(world, "b");

        Assert.AreEqual(a.Index, b.Index, "The slot is reused.");
        Assert.AreNotEqual(a.Generation, b.Generation);
        Assert.IsFalse(world.IsAlive(a));
        Assert.IsFalse(world.TryGet<Tag>(a, out _), "The old id does not reach the new entity's attachment.");
        Assert.AreEqual("b", world.Get<Tag>(b).Name);
    }

    [TestMethod]
    public void A_stale_id_throws_naming_the_id_and_why()
    {
        var world = new SceneWorld();
        var a = world.Create();
        world.Destroy(a);
        world.EndStep();

        foreach (var call in new Action[] { () => world.Local(a), () => world.Get<Tag>(a), () => world.ParentOf(a) })
        {
            var error = Assert.ThrowsExactly<InvalidOperationException>(call);
            StringAssert.Contains(error.Message, a.ToString());
            StringAssert.Contains(error.Message, "stale");
        }
        var foreign = Assert.ThrowsExactly<InvalidOperationException>(() => world.Local(new EntityId(500, 1)));
        StringAssert.Contains(foreign.Message, "not issued by this world");
    }

    // ------------------------------------------------------------------ 2. destroy timing

    [TestMethod]
    public void Destroy_is_immediate_for_queries_and_data_stays_readable_until_the_step_ends()
    {
        var world = new SceneWorld();
        world.BeginStep(1, Dt);
        var a = Tagged(world, "a");
        world.Local(a).Position2D = new Vector2(7, 3);

        world.Destroy(a);

        Assert.IsFalse(world.IsAlive(a), "Dead at once.");
        Assert.AreEqual(0, world.Count);
        CollectionAssert.AreEqual(Array.Empty<string>(), Names(world), "Iterations skip it at once.");
        Assert.AreEqual(new Vector2(7, 3), world.Local(a).Position2D, "Its transform is readable until the step ends.");
        Assert.IsTrue(world.TryGet<Tag>(a, out var tag) && tag.Name == "a", "Its attachments are readable until the step ends.");

        world.EndStep();

        Assert.IsFalse(world.TryGet<Tag>(a, out _), "Released: the id is stale.");
        Assert.ThrowsExactly<InvalidOperationException>(() => world.Local(a));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.Get<Tag>(a));
    }

    [TestMethod]
    public void Destroying_a_destroyed_stale_or_none_id_does_nothing()
    {
        var world = new SceneWorld();
        var a = world.Create();
        var b = world.Create();
        world.Destroy(a);
        world.Destroy(a);
        world.EndStep();
        world.Destroy(a);
        world.Destroy(EntityId.None);

        Assert.IsTrue(world.IsAlive(b));
        Assert.AreEqual(1, world.Count);
    }

    [TestMethod]
    public void A_destroyed_entity_cannot_take_attachments_or_a_parent_but_can_be_read()
    {
        var world = new SceneWorld();
        var a = world.Create();
        var b = world.Create();
        world.Destroy(a);

        var attach = Assert.ThrowsExactly<InvalidOperationException>(() => world.Attach(a, new Tag("late")));
        StringAssert.Contains(attach.Message, "destroyed");
        Assert.ThrowsExactly<InvalidOperationException>(() => world.SetParent(b, a));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.Create(a));
        Assert.AreEqual(EntityId.None, world.ParentOf(a), "Readable until the step ends.");
    }

    [TestMethod]
    public void Destroying_a_parent_destroys_its_subtree()
    {
        var world = new SceneWorld();
        var root = world.Create();
        var child = world.Create(root);
        var grandchild = world.Create(child);
        var other = world.Create();

        world.Destroy(root);

        Assert.IsFalse(world.IsAlive(child));
        Assert.IsFalse(world.IsAlive(grandchild));
        Assert.IsTrue(world.IsAlive(other));
        Assert.AreEqual(1, world.Count);
        world.EndStep();
        Assert.IsFalse(world.IsAlive(grandchild));
        Assert.AreEqual(1, world.Count);
    }

    [TestMethod]
    public void Destroyed_callbacks_run_when_the_step_ends_in_destroy_order_with_data_readable()
    {
        var world = new SceneWorld();
        var log = new List<string>();
        var ids = new EntityId[3];
        for (int i = 0; i < 3; i++)
        {
            ids[i] = world.Create();
            world.Local(ids[i]).Position2D = new Vector2(i, 0);
            world.Attach(ids[i], new Notified(log, $"n{i}") { World = world });
        }

        world.Destroy(ids[2]);
        world.Destroy(ids[0]);
        Assert.AreEqual(0, log.Count, "Nothing is told before the step ends.");
        world.EndStep();

        CollectionAssert.AreEqual(new[] { "n2:2", "n0:0" }, log);
        world.EndStep();
        Assert.AreEqual(2, log.Count, "Told once.");
    }

    [TestMethod]
    public void A_callback_may_destroy_more_and_those_are_released_in_the_same_step_end()
    {
        var world = new SceneWorld();
        var log = new List<string>();
        var first = world.Create();
        var second = world.Create();
        world.Local(second).Position2D = new Vector2(2, 0);
        world.Attach(first, new Notified(log, "first") { World = world, Then = _ => world.Destroy(second) });
        world.Attach(second, new Notified(log, "second") { World = world });

        world.Destroy(first);
        world.EndStep();

        CollectionAssert.AreEqual(new[] { "first:0", "second:2" }, log);
        Assert.AreEqual(0, world.Count);
        Assert.ThrowsExactly<InvalidOperationException>(() => world.Local(second), "Released too.");
    }

    [TestMethod]
    public void An_entity_created_during_a_callback_never_reuses_a_slot_being_released()
    {
        var world = new SceneWorld();
        var log = new List<string>();
        var created = EntityId.None;
        var doomed = world.Create();
        world.Attach(doomed, new Notified(log, "doomed") { World = world, Then = _ => created = world.Create() });

        world.Destroy(doomed);
        world.EndStep();

        Assert.AreNotEqual(doomed.Index, created.Index);
        Assert.IsTrue(world.IsAlive(created));
        Assert.AreEqual(1, world.Count);
    }

    // ------------------------------------------------------------------ 3. creation

    [TestMethod]
    public void An_iteration_skips_what_is_destroyed_during_it_and_visits_what_is_created_only_next_time()
    {
        var world = new SceneWorld();
        Tagged(world, "a");
        Tagged(world, "b");
        var c = Tagged(world, "c");
        var seen = new List<string>();

        foreach (var tag in world.All<Tag>())
        {
            seen.Add(tag.Name);
            if (tag.Name == "a")
            {
                world.Destroy(c);
                var created = Tagged(world, "new");
                world.Local(created).Position2D = new Vector2(4, 4);   // valid and writable at once
            }
        }

        CollectionAssert.AreEqual(new[] { "a", "b" }, seen);
        CollectionAssert.AreEqual(new[] { "a", "b", "new" }, Names(world));
    }

    [TestMethod]
    public void The_enumerator_reports_the_entity_of_each_attachment()
    {
        var world = new SceneWorld();
        var a = Tagged(world, "a");
        var b = Tagged(world, "b");
        var entities = new List<EntityId>();
        for (var e = world.All<Tag>().GetEnumerator(); e.MoveNext();) entities.Add(e.Entity);
        CollectionAssert.AreEqual(new[] { a, b }, entities);
    }

    // ------------------------------------------------------------------ 4. ordering and iteration

    [TestMethod]
    public void Iteration_follows_attach_order_across_destruction_and_slot_reuse()
    {
        var world = new SceneWorld();
        var ids = new[] { "a", "b", "c", "d", "e" }.Select(n => Tagged(world, n)).ToArray();
        world.Destroy(ids[1]);
        world.Destroy(ids[3]);
        world.EndStep();
        var f = Tagged(world, "f");
        Tagged(world, "g");

        Assert.AreEqual(ids[3].Index, f.Index, "A released slot was reused...");
        CollectionAssert.AreEqual(new[] { "a", "c", "e", "f", "g" }, Names(world), "...and the order is still the attach order.");
    }

    [TestMethod]
    public void Compaction_keeps_the_order_and_never_fills_a_hole_with_the_last_element()
    {
        var world = new SceneWorld();
        var ids = new[] { "a", "b", "c", "d", "e" }.Select(n => Tagged(world, n)).ToArray();
        world.Destroy(ids[1]);
        Assert.IsTrue(world.Detach<Tag>(ids[2]));
        world.BeginStep(1, Dt);   // compacts what the last step left
        world.EndStep();

        CollectionAssert.AreEqual(new[] { "a", "d", "e" }, Names(world), "A swap-remove store would give a, e, d.");
    }

    [TestMethod]
    public void Attach_order_is_per_type_and_reattaching_moves_an_attachment_to_the_end()
    {
        var world = new SceneWorld();
        var a = Tagged(world, "a");
        Tagged(world, "b");
        world.Detach<Tag>(a);
        world.Attach(a, new Tag("a2"));

        CollectionAssert.AreEqual(new[] { "b", "a2" }, Names(world));
    }

    [TestMethod]
    public void Attachments_are_keyed_by_the_type_argument_not_the_runtime_type()
    {
        var world = new SceneWorld();
        var e = world.Create();
        var attached = world.Attach<Base>(e, new Derived());

        Assert.IsTrue(world.TryGet<Base>(e, out var asBase));
        Assert.AreSame(attached, asBase);
        Assert.IsFalse(world.TryGet<Derived>(e, out _), "No polymorphic lookup.");
        Assert.AreEqual(1, world.All<Base>().Count());
        Assert.AreEqual(0, world.All<Derived>().Count());
    }

    [TestMethod]
    public void Attach_replaces_in_place_and_detach_removes()
    {
        var world = new SceneWorld();
        var e = world.Create();
        var other = Tagged(world, "other");
        world.Attach(e, new Tag("first"));
        world.Attach(other, new Tag("other2"));
        world.Attach(e, new Tag("second"));

        Assert.AreEqual("second", world.Get<Tag>(e).Name);
        CollectionAssert.AreEqual(new[] { "other2", "second" }, Names(world), "Replacing keeps the attachment's place.");

        Assert.IsTrue(world.Detach<Tag>(e));
        Assert.IsFalse(world.Detach<Tag>(e));
        Assert.IsFalse(world.TryGet<Tag>(e, out _));
        CollectionAssert.AreEqual(new[] { "other2" }, Names(world));
        Assert.IsTrue(world.IsAlive(e), "Detaching leaves the entity.");
        var missing = Assert.ThrowsExactly<InvalidOperationException>(() => world.Get<Tag>(e));
        StringAssert.Contains(missing.Message, "Tag");
        Assert.ThrowsExactly<ArgumentNullException>(() => world.Attach<Tag>(e, null!));
    }

    [TestMethod]
    public void Iterating_allocates_nothing()
    {
        var world = new SceneWorld();
        var parent = world.Create();
        for (int i = 0; i < 50; i++)
        {
            var e = world.Create(parent);
            world.Attach(e, new Tag("t"));
        }
        int count = 0;
        foreach (var _ in world.All<Tag>()) count++;
        foreach (var _ in world.Children(parent)) count++;

        long before = GC.GetAllocatedBytesForCurrentThread();
        foreach (var _ in world.All<Tag>()) count++;
        foreach (var _ in world.Children(parent)) count++;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(200, count);
        Assert.AreEqual(0, allocated);
    }

    // ------------------------------------------------------------------ hierarchy

    [TestMethod]
    public void A_child_is_placed_by_its_parent()
    {
        var world = new SceneWorld();
        var parent = world.Create();
        var child = world.Create(parent);
        world.Local(parent).Position2D = new Vector2(100, 50);
        world.Local(parent).Rotation2D = MathF.PI / 2;
        world.Local(child).Position2D = new Vector2(10, 0);

        var p = world.DrawPosition2D(child);

        Assert.AreEqual(100, p.X, 1e-4);
        Assert.AreEqual(60, p.Y, 1e-4, "Rotated a quarter turn by the parent.");
        Assert.AreEqual(parent, world.ParentOf(child));
        var m = world.WorldMatrix(child);
        Assert.AreEqual(p, new Vector2(m.Translation.X, m.Translation.Y));

        world.SetParent(child, EntityId.None);
        Assert.AreEqual(new Vector2(10, 0), world.DrawPosition2D(child), "A root keeps its local transform.");
        Assert.AreEqual(EntityId.None, world.ParentOf(child));
    }

    [TestMethod]
    public void A_translation_only_parent_places_its_children_exactly_as_the_matrix_path()
    {
        var world = new SceneWorld();
        var room = world.Create();
        var actor = world.Create(room);
        var hand = world.Create(actor);
        world.Local(room).Position2D = new Vector2(320.25f, -180.5f);
        world.Local(actor).Position2D = new Vector2(17.125f, 3.75f);
        world.Local(hand).Position2D = new Vector2(-2.5f, 1);
        world.Local(hand).Rotation2D = 1.2f;   // a child's own rotation does not move its origin

        var fast = world.DrawPosition2D(hand);
        var matrix = world.WorldMatrix(hand).Translation;

        Assert.AreEqual(new Vector2(320.25f + 17.125f - 2.5f, -180.5f + 3.75f + 1), fast);
        Assert.AreEqual(new Vector2(matrix.X, matrix.Y), fast);
    }

    [TestMethod]
    public void Children_walks_the_live_children_in_parenting_order()
    {
        var world = new SceneWorld();
        var parent = world.Create();
        var a = world.Create(parent);
        var b = world.Create(parent);
        var c = world.Create();
        world.SetParent(c, parent);
        var grandchild = world.Create(a);
        world.Destroy(b);

        CollectionAssert.AreEqual(new[] { a, c }, world.Children(parent).ToList(), "Direct children only; destroyed ones skipped at once.");
        CollectionAssert.AreEqual(new[] { grandchild }, world.Children(a).ToList());
        Assert.AreEqual(0, world.Children(c).Count());

        world.SetParent(a, EntityId.None);
        CollectionAssert.AreEqual(new[] { c }, world.Children(parent).ToList());
    }

    [TestMethod]
    public void A_child_created_during_a_children_walk_is_visited_only_next_time_and_reparenting_during_it_throws()
    {
        var world = new SceneWorld();
        var parent = world.Create();
        var a = world.Create(parent);
        var seen = new List<EntityId>();
        EntityId late = default;
        foreach (var child in world.Children(parent))
        {
            seen.Add(child);
            late = world.Create(parent);
        }
        CollectionAssert.AreEqual(new[] { a }, seen);
        CollectionAssert.AreEqual(new[] { a, late }, world.Children(parent).ToList());

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var child in world.Children(parent)) world.SetParent(child, EntityId.None);
        });
    }

    [TestMethod]
    public void SetParent_refuses_a_cycle()
    {
        var world = new SceneWorld();
        var a = world.Create();
        var b = world.Create(a);

        Assert.ThrowsExactly<InvalidOperationException>(() => world.SetParent(a, b));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.SetParent(a, a));
        Assert.AreEqual(a, world.ParentOf(b));
    }

    // ------------------------------------------------------------------ 5-7. interpolation, teleport, step tracking

    [TestMethod]
    public void Drawing_blends_from_the_step_start_at_the_worlds_blend_and_is_exact_at_one()
    {
        var world = new SceneWorld();
        var e = world.Create();
        world.Local(e).Position2D = new Vector2(10, 10);
        world.BeginStep(1, Dt);
        world.Local(e).Position2D = new Vector2(10.3f, 20);
        world.EndStep();

        world.BeginDraw(1, 0.5f);
        Assert.AreEqual(0.5f, world.Blend);
        Assert.AreEqual(Vector2.Lerp(new(10, 10), new(10.3f, 20), 0.5f), world.DrawPosition2D(e));
        world.BeginDraw(1, 0);
        Assert.AreEqual(new Vector2(10, 10), world.DrawPosition2D(e));
        world.BeginDraw(1, 1);
        Assert.AreEqual(new Vector2(10.3f, 20), world.DrawPosition2D(e), "Exactly the simulated position at 1.");
        Assert.AreEqual(world.Local(e).ToMatrix(), world.WorldMatrix(e));
    }

    [TestMethod]
    public void What_did_not_move_is_drawn_exactly_where_it_is_at_any_blend()
    {
        var world = new SceneWorld();
        var e = world.Create();
        world.Local(e).Position2D = new Vector2(0.1f, 0.7f);
        world.Local(e).Rotation2D = 0.3f;
        world.BeginStep(1, Dt);
        world.EndStep();
        world.BeginDraw(1, 0.37f);

        Assert.AreEqual(new Vector2(0.1f, 0.7f), world.DrawPosition2D(e));
        Assert.AreEqual(world.Local(e).ToMatrix(), world.WorldMatrix(e));
    }

    [TestMethod]
    public void A_childs_drawn_matrix_is_its_interpolated_local_matrix_times_its_parents()
    {
        var world = new SceneWorld();
        var parent = world.Create();
        var child = world.Create(parent);
        world.Local(child).Position2D = new Vector2(4, 0);
        world.BeginStep(1, Dt);
        world.Local(parent).Position2D = new Vector2(10, 0);
        world.Local(parent).Rotation2D = MathF.PI / 2;
        world.Local(child).Position2D = new Vector2(8, 0);
        world.EndStep();
        world.BeginDraw(1, 0.5f);

        var start = Transform.Identity;
        var parentNow = world.Local(parent);
        var childStart = Transform.Identity;
        childStart.Position2D = new Vector2(4, 0);
        var expected = Transform.Interpolate(childStart, world.Local(child), 0.5f).ToMatrix() * Transform.Interpolate(start, parentNow, 0.5f).ToMatrix();

        Assert.AreEqual(expected, world.WorldMatrix(child));
        var drawn = world.DrawPosition2D(child);
        Assert.AreEqual(expected.Translation.X, drawn.X, 1e-5);
        Assert.AreEqual(expected.Translation.Y, drawn.Y, 1e-5);
    }

    [TestMethod]
    public void A_new_or_teleported_entity_is_drawn_where_it_is_until_the_next_step()
    {
        var world = new SceneWorld();
        var walker = world.Create();
        world.BeginStep(1, Dt);
        var spawned = world.Create();
        world.Local(spawned).Position2D = new Vector2(50, 0);
        world.Local(walker).Position2D = new Vector2(500, 0);
        world.Teleport(walker);
        world.Local(walker).Position2D = new Vector2(501, 0);
        world.EndStep();
        world.BeginDraw(1, 0);

        Assert.AreEqual(new Vector2(50, 0), world.DrawPosition2D(spawned), "New: no previous position.");
        Assert.AreEqual(new Vector2(501, 0), world.DrawPosition2D(walker), "Teleported: drawn where it is, even after later moves this step.");

        world.BeginStep(2, Dt);
        world.Local(walker).Position2D = new Vector2(503, 0);
        world.EndStep();
        world.BeginDraw(2, 0.5f);
        Assert.AreEqual(new Vector2(502, 0), world.DrawPosition2D(walker), "Blends again from the next step.");
    }

    [TestMethod]
    public void Teleporting_a_parent_carries_its_children()
    {
        var world = new SceneWorld();
        var parent = world.Create();
        var child = world.Create(parent);
        world.Local(child).Position2D = new Vector2(1, 0);
        world.BeginStep(1, Dt);
        world.Local(parent).Position2D = new Vector2(1000, 0);
        world.Teleport(parent);
        world.EndStep();
        world.BeginDraw(1, 0.25f);

        Assert.AreEqual(new Vector2(1001, 0), world.DrawPosition2D(child));
        Assert.AreEqual(1001, world.WorldMatrix(child).Translation.X);
    }

    [TestMethod]
    public void Reparenting_draws_the_entity_where_it_is_until_the_next_step()
    {
        var world = new SceneWorld();
        var a = world.Create();
        var b = world.Create();
        var e = world.Create(a);
        world.Local(b).Position2D = new Vector2(100, 0);
        world.BeginStep(1, Dt);
        world.Local(e).Position2D = new Vector2(2, 0);
        world.SetParent(e, b);
        world.EndStep();
        world.BeginDraw(1, 0.5f);

        Assert.AreEqual(new Vector2(102, 0), world.DrawPosition2D(e), "Its step-start transform was in another parent's space.");
    }

    [TestMethod]
    public void A_world_that_did_not_step_in_the_latest_engine_step_is_drawn_at_blend_one()
    {
        var world = new SceneWorld();
        var e = world.Create();
        Assert.AreEqual(1f, world.Blend, "Before any frame.");
        Assert.AreEqual(-1, world.SteppedAt);
        world.BeginStep(5, Dt);
        world.Local(e).Position2D = new Vector2(10, 0);
        world.EndStep();
        world.BeginDraw(latestEngineStep: 5, engineBlend: 0.3f);
        Assert.AreEqual(0.3f, world.Blend, "It stepped: the engine's blend.");
        Assert.AreEqual(new Vector2(3, 0), world.DrawPosition2D(e));

        // An overlay froze the scene: the engine stepped (6) but this world did not.
        world.BeginDraw(latestEngineStep: 6, engineBlend: 0.3f);
        Assert.AreEqual(1f, world.Blend, "Frozen: drawn as its last step, not rocking between its last two.");
        Assert.AreEqual(new Vector2(10, 0), world.DrawPosition2D(e));
        Assert.AreEqual(5, world.SteppedAt);
    }

    // ------------------------------------------------------------------ 8. clocks

    [TestMethod]
    public void The_clock_advances_with_the_worlds_step_exactly_at_scale_one()
    {
        var world = new SceneWorld();
        Assert.AreEqual(Dt, world.BeginStep(1, Dt), "Bit for bit at scale 1.");
        world.EndStep();
        Assert.AreEqual(Dt, world.Clock.Delta);
        Assert.AreEqual((double)Dt, world.Clock.Time);

        world.Clock.Scale = 0.5f;
        Assert.AreEqual(Dt * 0.5f, world.BeginStep(2, Dt), "Slow motion is the scale.");
        Assert.AreEqual(Dt * 0.5f, world.Clock.Delta);
        Assert.AreEqual(Dt + (double)(Dt * 0.5f), world.Clock.Time, 1e-12);
        world.Clock.Scale = 0;
        Assert.AreEqual(0f, world.BeginStep(3, Dt));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.Clock.Scale = -1);
        Assert.AreEqual(0f, world.Clock.Scale);
    }

    [TestMethod]
    public void A_freeze_keeps_the_longer_one_and_stops_whole_steps()
    {
        var world = new SceneWorld();
        var clock = world.Clock;
        clock.Freeze(0.05f);
        clock.Freeze(0.02f);
        Assert.AreEqual(0.05f, clock.FreezeRemaining, "The longer freeze wins.");
        Assert.IsTrue(clock.Frozen);

        int frozen = 0;
        long step = 0;
        while (world.BeginStep(++step, Dt) == 0) frozen++;

        Assert.AreEqual(3, frozen, "0.05 s at 60 Hz freezes three whole steps.");
        Assert.IsFalse(clock.Frozen);
        Assert.IsTrue(clock.FreezeRemaining <= 0, "The remainder may end slightly below zero.");
        Assert.AreEqual((double)Dt, clock.Time, "Frozen steps add no time.");

        clock.Freeze(1);
        clock.Unfreeze();
        Assert.IsFalse(clock.Frozen);
        Assert.AreEqual(0f, clock.FreezeRemaining);
    }

    [TestMethod]
    public void A_world_that_does_not_step_consumes_no_time_and_no_freeze()
    {
        var world = new SceneWorld();
        world.Clock.Freeze(0.1f);
        world.BeginDraw(10, 0.5f);   // drawn under an overlay for many frames, never stepped
        world.BeginDraw(11, 0.5f);

        Assert.AreEqual(0.1f, world.Clock.FreezeRemaining);
        Assert.AreEqual(0d, world.Clock.Time);
    }

    // ------------------------------------------------------------------ transforms

    [TestMethod]
    public void The_2D_helpers_are_precise_where_promised()
    {
        var t = Transform.Identity;
        var p = new Vector2(123.456f, -0.1f);
        t.Position.Z = 5;
        t.Position2D = p;
        Assert.AreEqual(p, t.Position2D, "Positions read back bit for bit.");
        Assert.AreEqual(5f, t.Position.Z, "Writing X/Y keeps Z.");
        t.Rotation2D = 0.3f;
        Assert.AreEqual(0.3f, t.Rotation2D, 1e-6, "Angles read back within a few ulps.");
        t.Scale2D = new Vector2(2, 3);
        Assert.AreEqual(new Vector3(2, 3, 1), t.Scale);
        var to = t;
        to.Position2D = new Vector2(1, 1);
        Assert.AreEqual(to.Position, Transform.Interpolate(t, to, 1).Position, "Exactly the target at 1.");
        Assert.AreEqual(to.Position, Transform.Interpolate(t, to, 2).Position);
        Assert.AreEqual(Vector3.Lerp(t.Position, to.Position, 0.25f), Transform.Interpolate(t, to, 0.25f).Position);
    }

    [TestMethod]
    public void A_transforms_matrix_scales_then_rotates_then_translates()
    {
        var t = Transform.Identity;
        t.Scale2D = new Vector2(2, 2);
        t.Rotation2D = MathF.PI / 2;
        t.Position2D = new Vector2(10, 0);

        var p = Vector2.Transform(new Vector2(1, 0), t.ToMatrix());

        Assert.AreEqual(10, p.X, 1e-5);
        Assert.AreEqual(2, p.Y, 1e-5);
        Assert.AreEqual(Matrix4x4.Identity, Transform.Identity.ToMatrix());
    }

    // ------------------------------------------------------------------ state hash

    [TestMethod]
    public void The_state_hash_covers_ids_transforms_parents_attachments_and_the_clock()
    {
        SceneWorld Build(Action<SceneWorld>? change = null)
        {
            var world = new SceneWorld();
            var a = world.Create();
            var b = world.Create(a);
            world.Local(b).Position2D = new Vector2(3, 4);
            world.Attach(a, new Health(10));
            world.Attach(b, new Tag("b"));
            change?.Invoke(world);
            return world;
        }
        ulong reference = Hash(Build());

        Assert.AreEqual(reference, Hash(Build()), "Same state, same hash.");
        Assert.AreNotEqual(reference, Hash(Build(w => w.Local(new EntityId(1, 1)).Position2D = new Vector2(3, 5))), "A transform.");
        Assert.AreNotEqual(reference, Hash(Build(w => w.Get<Health>(new EntityId(0, 1)).Value = 9)), "An attachment's own state.");
        Assert.AreNotEqual(reference, Hash(Build(w => w.Detach<Tag>(new EntityId(1, 1)))), "Which attachments there are.");
        Assert.AreNotEqual(reference, Hash(Build(w => w.SetParent(new EntityId(1, 1), EntityId.None))), "The hierarchy.");
        Assert.AreNotEqual(reference, Hash(Build(w => w.Clock.Freeze(0.1f))), "The clock.");
        Assert.AreNotEqual(reference, Hash(Build(w => w.Create())), "The entities.");
    }

    [TestMethod]
    public void Drawing_never_changes_the_state_hash()
    {
        var world = new SceneWorld();
        var a = world.Create();
        var b = world.Create(a);
        world.BeginStep(1, Dt);
        world.Local(a).Position2D = new Vector2(5, 5);
        world.Local(b).Rotation2D = 1;
        world.Teleport(b);
        world.EndStep();
        ulong before = Hash(world);

        world.BeginDraw(1, 0.4f);
        _ = world.DrawPosition2D(b);
        _ = world.WorldMatrix(b);
        world.BeginDraw(2, 0.9f);
        _ = world.DrawPosition2D(a);

        Assert.AreEqual(before, Hash(world));
    }
}
