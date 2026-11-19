using System.Numerics;
using Arch.Core;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Ecs.Tests;

// Inside a namespace under MicroEngine, World alone names the engine's MicroEngine.World namespace.
using World = Arch.Core.World;

public struct Place : IStateHashed
{
    public Vector2 Value;
    public readonly void HashState(StateHasher hash) => hash.Add(Value);
}

public struct Speed : IStateHashed
{
    public Vector2 Value;
    public readonly void HashState(StateHasher hash) => hash.Add(Value);
}

/// <summary>A component the hash covers through a function, as for a type the game cannot change.</summary>
public struct Health { public int Value; }
/// <summary>Not simulation state: known to the store, left out of the hash.</summary>
public struct Note { public int Value; }
public struct Marked;
/// <summary>A component nobody added to the store's components.</summary>
public struct Stray { public int Value; }

/// <summary>
/// The store in a real scene of a real game, stepped by the engine: its lifetime, the order of its systems, deferred
/// changes, links to the scene's world, interpolation, the state hash and registration.
/// </summary>
[TestClass]
public sealed class EcsStoreTests
{
    internal sealed class TestGame(Scene first, Action<Game>? setup = null) : Game
    {
        public override GameSettings Settings { get; } = new("ecs");
        protected override void Load()
        {
            setup?.Invoke(this);
            Scenes.Push(first);
        }
    }

    internal sealed class Host(bool overlay = false) : Scene
    {
        public Action<Host>? OnEnter, OnUpdate, OnDraw;
        public override bool IsOverlay => overlay;
        public override void Enter() => OnEnter?.Invoke(this);
        public override void Update(float dt) => OnUpdate?.Invoke(this);
        public override void Draw(IRenderer renderer) => OnDraw?.Invoke(this);
    }

    private sealed class Logged(string name, List<string> log) : EcsSystem
    {
        public override string Name => name;
        protected override void Start() => log.Add($"start {name}");
        protected override void Update(float dt) => log.Add(name);
    }

    internal static EcsComponents Components() => new EcsComponents()
        .AddState<Place>().AddState<Speed>().AddState<Health>(static (in Health h, StateHasher hash) => hash.Add(h.Value))
        .Add<Note>().AddTag<Marked>()
        .Interpolate<Place>(static (in Place from, in Place to, float blend) => new Place { Value = Vector2.Lerp(from.Value, to.Value, blend) })
        .SyncTransform<Place>(
            static (in Place place, ref Transform local) => local.Position2D = place.Value,
            static (in Transform local, ref Place place) => place.Value = local.Position2D);

    /// <summary>A game whose first scene has a store, started and ready to step.</summary>
    private static (TestGame Game, Host Scene, EcsStore Ecs) Start(Action<Host>? onUpdate = null)
    {
        var scene = new Host { OnUpdate = onUpdate };
        var ecs = scene.AddEcs(Components());
        var game = new TestGame(scene);
        game.InitializeForTest(new HeadlessPlatform());
        return (game, scene, ecs);
    }

    private static readonly QueryDescription Placed = new QueryDescription().WithAll<Place>();
    private static readonly QueryDescription Moving = new QueryDescription().WithAll<Place, Speed>();

    private static void Near(Vector2 expected, Vector2 actual, string message = "") =>
        Assert.IsTrue(Vector2.Distance(expected, actual) < 1e-3f, $"{message} expected {expected}, was {actual}");

    private static void Move(EcsStore ecs, float dt) =>
        ecs.World.Query(in Moving, (ref Place place, ref Speed speed) => place.Value += speed.Value * dt);

    // ------------------------------------------------------------------ lifetime

    [TestMethod]
    public void The_store_is_created_when_its_scene_starts_and_destroyed_when_it_leaves()
    {
        var scene = new Host();
        var ecs = scene.AddEcs(Components());
        Assert.IsFalse(ecs.IsRunning);
        var before = Assert.ThrowsExactly<InvalidOperationException>(() => _ = ecs.World);
        StringAssert.Contains(before.Message, "scene");

        World? seenInEnter = null;
        scene.OnEnter = s => seenInEnter = s.Ecs().World;
        var game = new TestGame(scene);
        game.InitializeForTest(new HeadlessPlatform());
        Assert.IsTrue(ecs.IsRunning);
        var world = ecs.World;
        Assert.AreSame(world, seenInEnter, "the store is there when the scene enters");
        Assert.AreSame(ecs, scene.Ecs());
        Assert.AreSame(world, World.Worlds[world.Id], "the library lists the store while the scene runs");

        game.Scenes.Pop();
        game.StepForTest();
        Assert.IsFalse(ecs.IsRunning);
        Assert.AreNotSame(world, World.Worlds[world.Id], "the library no longer lists the store of a scene that left");
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = ecs.World);
    }

    [TestMethod]
    public void One_line_gives_every_scene_a_store_and_a_scene_without_one_says_so()
    {
        var first = new Host();
        var game = new TestGame(first, g => g.Scenes.AddEcs(Components(), scene => !scene.IsOverlay));
        game.InitializeForTest(new HeadlessPlatform());
        Assert.IsTrue(first.Ecs().IsRunning);

        var second = new Host();
        var menu = new Host(overlay: true);
        game.Scenes.Push(second);
        game.StepForTest();
        game.Scenes.Push(menu);
        game.StepForTest();
        Assert.AreNotSame(first.Ecs().World, second.Ecs().World, "each scene has its own");
        var missing = Assert.ThrowsExactly<InvalidOperationException>(() => menu.Ecs());
        StringAssert.Contains(missing.Message, "AddEcs");
    }

    // ------------------------------------------------------------------ systems

    [TestMethod]
    public void Systems_run_in_the_order_they_were_added_around_the_scenes_update()
    {
        var log = new List<string>();
        var (game, _, ecs) = Start(_ => log.Add("scene"));
        ecs.Systems.Add(new Logged("move", log));
        ecs.Systems.Add("hit", (_, _) => log.Add("hit"));
        ecs.LateSystems.Add(new Logged("deaths", log));
        ecs.LateSystems.Add("camera", (_, _) => log.Add("camera"));
        CollectionAssert.AreEqual(new[] { "start move", "start deaths" }, log, "a system added to a running store starts at once");
        log.Clear();

        game.StepForTest();
        CollectionAssert.AreEqual(new[] { "move", "hit", "scene", "deaths", "camera" }, log);
        Assert.AreEqual(2, ecs.Systems.Count);
        Assert.AreSame(ecs, ecs.Systems.Get<Logged>().Store);
        Assert.AreEqual("hit", ecs.Systems["hit"].Name);
    }

    [TestMethod]
    public void Systems_added_before_the_scene_starts_start_with_the_store_and_receive_the_scenes_step()
    {
        var log = new List<string>();
        var scene = new Host();
        var ecs = scene.AddEcs(Components());
        ecs.Systems.Add(new Logged("early", log));
        float seen = -1;
        ecs.Systems.Add("dt", (_, dt) => seen = dt);
        Assert.AreEqual(0, log.Count);

        var game = new TestGame(scene);
        game.InitializeForTest(new HeadlessPlatform());
        CollectionAssert.AreEqual(new[] { "start early" }, log);
        game.StepForTest();
        Assert.AreEqual(1f / 60f, seen, 1e-6f);
    }

    [TestMethod]
    public void A_disabled_system_is_skipped_until_it_is_enabled_again_and_a_removed_one_is_gone()
    {
        var log = new List<string>();
        var (game, _, ecs) = Start();
        var a = ecs.Systems.Add(new Logged("a", log));
        var b = ecs.Systems.Add("b", (_, _) => log.Add("b"));
        log.Clear();

        a.Enabled = false;
        game.StepForTest();
        a.Enabled = true;
        game.StepForTest();
        Assert.IsTrue(ecs.Systems.Remove(b));
        Assert.IsFalse(ecs.Systems.Remove(b));
        game.StepForTest();
        CollectionAssert.AreEqual(new[] { "b", "a", "b", "a" }, log);
    }

    [TestMethod]
    public void A_system_cannot_be_in_two_lists_and_a_name_is_used_once()
    {
        var (_, _, ecs) = Start();
        var system = ecs.Systems.Add(new Logged("a", []));
        Assert.ThrowsExactly<InvalidOperationException>(() => ecs.LateSystems.Add(system));
        Assert.ThrowsExactly<InvalidOperationException>(() => ecs.Systems.Add("a", (_, _) => { }));
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = ecs.Systems["missing"]);
        Assert.ThrowsExactly<InvalidOperationException>(() => ecs.Systems.Get<Logged>().Store.LateSystems.Get<Logged>());
    }

    [TestMethod]
    public void A_scene_frozen_under_a_blocking_overlay_does_not_step_its_store()
    {
        int steps = 0;
        var (game, _, ecs) = Start();
        ecs.Systems.Add("count", (_, _) => steps++);
        var row = ecs.World.Create(new Place(), new Speed { Value = new Vector2(60, 0) });
        ecs.Systems.Add("move", Move);
        game.StepForTest();
        Assert.AreEqual(1, steps);

        game.Scenes.Push(new Host(overlay: true));
        game.StepForTest();
        game.StepForTest();
        int frozenAt = steps;
        game.StepForTest();
        Assert.AreEqual(frozenAt, steps, "the overlay blocks the scene below, and its store with it");
        var place = ecs.World.Get<Place>(row).Value;

        game.Scenes.Pop();
        game.StepForTest();
        game.StepForTest();
        Assert.IsTrue(steps > frozenAt);
        Assert.IsTrue(ecs.World.Get<Place>(row).Value.X > place.X, "and it goes on when the overlay leaves");
    }

    // ------------------------------------------------------------------ deferred changes

    [TestMethod]
    public void Changes_asked_of_the_commands_wait_for_the_end_of_the_system_that_asked()
    {
        var (game, _, ecs) = Start();
        var doomed = ecs.World.Create(new Place());
        var kept = ecs.World.Create(new Place());
        var seen = new List<string>();
        ecs.Systems.Add("ask", (store, _) =>
        {
            store.World.Query(in Placed, (Entity row, ref Place _) =>
            {
                if (row == doomed) store.Commands.Destroy(row);
                else store.Commands.Add(row, new Speed { Value = Vector2.One });
            });
            seen.Add($"during: doomed alive {store.World.IsAlive(doomed)}, kept has speed {store.World.Has<Speed>(kept)}");
        });
        ecs.Systems.Add("next", (store, _) => seen.Add($"next: doomed alive {store.World.IsAlive(doomed)}, kept has speed {store.World.Has<Speed>(kept)}"));
        game.StepForTest();
        CollectionAssert.AreEqual(new[] { "during: doomed alive True, kept has speed False", "next: doomed alive False, kept has speed True" }, seen);
    }

    [TestMethod]
    public void The_scenes_own_commands_are_applied_before_the_late_systems_and_theirs_before_the_step_ends()
    {
        var seen = new List<string>();
        Entity fromScene = default, fromLate = default;
        var (game, _, ecs) = Start(scene => scene.Ecs().Commands.Add(fromScene, new Marked()));
        fromScene = ecs.World.Create(new Place());
        fromLate = ecs.World.Create(new Place());
        ecs.LateSystems.Add("late", (store, _) =>
        {
            seen.Add($"late sees the scene's change: {store.World.Has<Marked>(fromScene)}");
            store.Commands.Destroy(fromLate);
        });
        game.StepForTest();
        CollectionAssert.AreEqual(new[] { "late sees the scene's change: True" }, seen);
        Assert.IsFalse(ecs.World.IsAlive(fromLate));

        // Between two walks of one system, a system applies what it asked for itself.
        var third = ecs.World.Create(new Place());
        ecs.Commands.Destroy(third);
        Assert.IsTrue(ecs.World.IsAlive(third));
        ecs.Flush();
        Assert.IsFalse(ecs.World.IsAlive(third));
        ecs.Flush();   // nothing pending: nothing happens
    }

    // ------------------------------------------------------------------ links

    [TestMethod]
    public void A_linked_row_and_entity_find_each_other_and_an_unlinked_row_needs_no_entity()
    {
        var (game, scene, ecs) = Start();
        var alone = ecs.World.Create(new Place());
        var row = ecs.World.Create(new Place());
        var entity = scene.Objects.Create();
        ecs.Link(row, entity);

        Assert.AreEqual(1, ecs.LinkCount);
        Assert.IsTrue(ecs.TryGetEntity(row, out var found) && found == entity);
        Assert.IsTrue(ecs.TryGetRow(entity, out var back) && back == row);
        Assert.AreEqual(entity, ecs.World.Get<SceneLink>(row).Entity, "the link is a component, so a query can ask for linked rows");
        Assert.AreSame(ecs, scene.Objects.Get<EcsRow>(entity).Store);
        Assert.AreEqual(row, scene.Objects.Get<EcsRow>(entity).Row);
        Assert.IsFalse(ecs.TryGetEntity(alone, out _));
        Assert.IsFalse(ecs.TryGetRow(scene.Objects.Create(), out _));
        game.StepForTest();
        Assert.IsTrue(ecs.World.IsAlive(alone), "a row without an entity is a row like any other");

        Assert.IsTrue(ecs.Unlink(row));
        Assert.IsFalse(ecs.Unlink(row));
        Assert.AreEqual(0, ecs.LinkCount);
        Assert.IsFalse(ecs.World.Has<SceneLink>(row));
        Assert.IsFalse(scene.Objects.TryGet<EcsRow>(entity, out _));
        game.StepForTest();
        Assert.IsTrue(ecs.World.IsAlive(row) && scene.Objects.IsAlive(entity), "unlinking destroys neither");
    }

    [TestMethod]
    public void Destroying_the_entity_removes_its_row_and_destroying_the_row_removes_its_entity()
    {
        var (game, scene, ecs) = Start();
        var (rowA, rowB, rowC) = (ecs.World.Create(new Place()), ecs.World.Create(new Place()), ecs.World.Create(new Place()));
        var (a, b, c) = (scene.Objects.Create(), scene.Objects.Create(), scene.Objects.Create());
        var child = scene.Objects.Create(b);
        ecs.Link(rowA, a);
        ecs.Link(rowB, b);
        ecs.Link(rowC, c);

        scene.Objects.Destroy(a);          // from the world's side
        ecs.World.Destroy(rowB);           // from the store's side, with the library's own call
        ecs.Commands.Destroy(rowC);        // and through the commands
        game.StepForTest();

        Assert.IsFalse(ecs.World.IsAlive(rowA), "the row went with its entity");
        Assert.IsFalse(scene.Objects.IsAlive(b), "the entity went with its row");
        Assert.IsFalse(scene.Objects.IsAlive(child), "and its children with it");
        Assert.IsFalse(scene.Objects.IsAlive(c));
        Assert.AreEqual(0, ecs.LinkCount);
        Assert.AreEqual(0, ecs.World.CountEntities(in Placed));
        Assert.AreEqual(0, scene.Objects.Count);
    }

    [TestMethod]
    public void A_stale_handle_reaches_neither_the_row_nor_the_entity_that_took_its_place()
    {
        var (game, scene, ecs) = Start();
        var row = ecs.World.Create(new Place());
        var entity = scene.Objects.Create();
        ecs.Link(row, entity);

        ecs.World.Destroy(row);
        var newcomer = ecs.World.Create(new Place { Value = new Vector2(1, 2) });
        Assert.AreEqual(row.Id, newcomer.Id, "the library reuses the place");
        Assert.IsFalse(ecs.TryGetEntity(row, out _), "the old handle is stale at once");
        Assert.IsFalse(ecs.TryGetRow(entity, out _));
        game.StepForTest();

        Assert.IsFalse(scene.Objects.IsAlive(entity));
        Assert.IsTrue(ecs.World.IsAlive(newcomer), "releasing the entity did not reach the row that reused the place");
        Assert.AreEqual(new Vector2(1, 2), ecs.World.Get<Place>(newcomer).Value);
        Assert.IsFalse(ecs.TryGetEntity(newcomer, out _));

        var reused = scene.Objects.Create();
        Assert.AreEqual(entity.Index, reused.Index, "the world reuses the slot");
        Assert.IsFalse(ecs.TryGetRow(entity, out _));
        Assert.IsFalse(ecs.TryGetRow(reused, out _));
    }

    [TestMethod]
    public void Linking_refuses_what_is_dead_or_already_linked()
    {
        var (_, scene, ecs) = Start();
        var row = ecs.World.Create(new Place());
        var other = ecs.World.Create(new Place());
        var entity = scene.Objects.Create();
        var gone = scene.Objects.Create();
        scene.Objects.Destroy(gone);
        var dead = ecs.World.Create(new Place());
        ecs.World.Destroy(dead);

        Assert.ThrowsExactly<InvalidOperationException>(() => ecs.Link(dead, entity));
        Assert.ThrowsExactly<InvalidOperationException>(() => ecs.Link(row, gone));
        ecs.Link(row, entity);
        Assert.ThrowsExactly<InvalidOperationException>(() => ecs.Link(row, scene.Objects.Create()));
        Assert.ThrowsExactly<InvalidOperationException>(() => ecs.Link(other, entity));
    }

    // ------------------------------------------------------------------ transforms

    [TestMethod]
    public void A_linked_row_moves_its_entity_and_the_world_carries_the_children()
    {
        var (game, scene, ecs) = Start();
        ecs.Systems.Add("move", Move);
        var row = ecs.World.Create(new Place { Value = new Vector2(10, 20) }, new Speed { Value = new Vector2(60, 0) });
        var entity = scene.Objects.Create();
        var child = scene.Objects.Create(entity);
        scene.Objects.Local(child).Position2D = new Vector2(0, -7);
        ecs.Link(row, entity);

        game.StepForTest();
        game.StepForTest();
        var place = ecs.World.Get<Place>(row).Value;
        Assert.AreEqual(12, place.X, 1e-3f);
        Assert.AreEqual(place, scene.Objects.Local(entity).Position2D, "the row's place is the entity's");
        Assert.AreEqual(place + new Vector2(0, -7), scene.Objects.DrawPosition2D(child), "the child follows through the hierarchy");
    }

    [TestMethod]
    public void An_entity_that_something_else_moves_can_move_its_row_instead()
    {
        var (game, scene, ecs) = Start();
        var seen = new List<Vector2>();
        var row = ecs.World.Create(new Place());
        var unsynced = ecs.World.Create(new Place { Value = new Vector2(5, 5) });
        var (entity, still) = (scene.Objects.Create(), scene.Objects.Create());
        ecs.Link(row, entity, EcsLink.EntityMovesRow);
        ecs.Link(unsynced, still, EcsLink.Unsynced);
        ecs.Systems.Add("read", (store, _) => seen.Add(store.World.Get<Place>(row).Value));

        scene.Objects.Local(entity).Position2D = new Vector2(3, 4);   // a physics body, a tween, the scene itself
        scene.Objects.Local(still).Position2D = new Vector2(9, 9);
        game.StepForTest();

        CollectionAssert.AreEqual(new[] { new Vector2(3, 4) }, seen, "the systems of the step already see where the entity is");
        Assert.AreEqual(new Vector2(3, 4), scene.Objects.Local(entity).Position2D, "and the row did not write it back");
        Assert.AreEqual(new Vector2(5, 5), ecs.World.Get<Place>(unsynced).Value, "an unsynced link copies nothing");
        Assert.AreEqual(new Vector2(9, 9), scene.Objects.Local(still).Position2D);
    }

    // ------------------------------------------------------------------ interpolation

    [TestMethod]
    public void Drawing_reads_a_component_between_the_start_of_the_step_and_its_end()
    {
        var (game, _, ecs) = Start();
        ecs.Systems.Add("move", Move);
        var row = ecs.World.Create(new Place { Value = new Vector2(100, 50) }, new Speed { Value = new Vector2(600, -300) });
        game.StepForTest();
        var was = ecs.World.Get<Place>(row);
        game.StepForTest();
        var places = ecs.Interpolated<Place>();
        var now = ecs.World.Get<Place>(row);
        Near(new Vector2(110, 45), was.Value);
        Near(new Vector2(120, 40), now.Value);

        ecs.PrepareDraw(0);
        Assert.AreEqual(was.Value, places.Blended(row, in now).Value, "blend 0: where it was when the step started");
        ecs.PrepareDraw(0.5f);
        Assert.AreEqual(0.5f, ecs.Blend);
        Near(new Vector2(115, 42.5f), places.Blended(row, in now).Value);
        Near(new Vector2(115, 42.5f), places.Blended(row).Value, "the row's current value is read when it is not given");
        Near(new Vector2(115, 42.5f), ecs.Blended<Place>(row).Value);
        ecs.PrepareDraw(1);
        Assert.AreEqual(now.Value, places.Blended(row, in now).Value, "blend 1: exactly the simulated value");
    }

    [TestMethod]
    public void A_row_that_is_new_this_step_is_drawn_where_it_is()
    {
        var (game, _, ecs) = Start();
        ecs.Systems.Add("move", Move);
        var old = ecs.World.Create(new Place(), new Speed { Value = new Vector2(60, 0) });
        game.StepForTest();
        // Destroyed after the step started and its snapshot was taken, and its place taken by a new row in the same step.
        ecs.Systems.Add("kill", (store, _) =>
        {
            if (store.World.IsAlive(old)) store.World.Destroy(old);
        });
        Entity born = default;
        var once = ecs.LateSystems.Add("spawn", (store, _) => born = store.World.Create(new Place { Value = new Vector2(40, 40) }, new Speed { Value = new Vector2(60, 0) }));
        game.StepForTest();
        once.Enabled = false;
        Assert.AreEqual(old.Id, born.Id);

        ecs.PrepareDraw(0.25f);
        Assert.AreEqual(new Vector2(40, 40), ecs.Blended<Place>(born).Value, "no blend from a snapshot that was another row's");
        game.StepForTest();
        ecs.PrepareDraw(0.5f);
        Near(new Vector2(40.5f, 40), ecs.Blended<Place>(born).Value, "from the next step on it blends");

        // A component that came back after steps without it does not blend from the value it had before.
        ecs.World.Remove<Place>(born);
        game.StepForTest();
        ecs.World.Add(born, new Place { Value = new Vector2(200, 0) });
        ecs.PrepareDraw(0.5f);
        Assert.AreEqual(new Vector2(200, 0), ecs.Blended<Place>(born).Value);
    }

    [TestMethod]
    public void The_engine_prepares_each_frame_and_a_frozen_scene_is_drawn_as_its_last_step()
    {
        // Through the real loop: a scripted run draws one frame per step, each at blend 1.
        var blends = new List<float>();
        var drawn = new List<Vector2>();
        Entity row = default;
        var scene = new Host
        {
            OnEnter = s =>
            {
                s.Ecs().Systems.Add("move", Move);
                row = s.Ecs().World.Create(new Place(), new Speed { Value = new Vector2(60, 0) });
            },
            OnDraw = s =>
            {
                blends.Add(s.Ecs().Blend);
                drawn.Add(s.Ecs().Blended<Place>(row).Value);
            },
        };
        var ecs = scene.AddEcs(Components());
        var game = new TestGame(scene);
        var result = GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { Script = new InputScript().QuitAfter(5) });
        Assert.IsNull(result.Error, result.Error?.ToString());
        Assert.IsTrue(ecs.FramesPrepared >= 4, $"{ecs.FramesPrepared} frames");
        Assert.IsTrue(blends.All(b => b == 1f));
        Assert.IsTrue(drawn[^1].X > drawn[0].X);

        // A scene that is not stepped is prepared with blend 1 by the engine, whatever the frame's blend: at blend 1
        // a row is drawn exactly where the last step left it.
        var (frozenGame, _, frozen) = Start();
        frozen.Systems.Add("move", Move);
        var still = frozen.World.Create(new Place(), new Speed { Value = new Vector2(60, 0) });
        frozenGame.StepForTest();
        frozenGame.Scenes.Push(new Host(overlay: true));
        frozenGame.StepForTest();
        frozenGame.StepForTest();
        frozen.PrepareDraw(1);
        Assert.AreEqual(frozen.World.Get<Place>(still).Value, frozen.Blended<Place>(still).Value);
    }

    [TestMethod]
    public void Only_a_component_marked_for_it_can_be_read_blended()
    {
        var (_, _, ecs) = Start();
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => ecs.Interpolated<Speed>());
        StringAssert.Contains(error.Message, "Interpolate<Speed>");
        Assert.ThrowsExactly<InvalidOperationException>(() => new EcsComponents().Interpolate<Place>(static (in Place a, in Place b, float t) => b));
    }

    // ------------------------------------------------------------------ the state hash

    private static (TestGame Game, Host Scene, EcsStore Ecs, Entity Row) Populated()
    {
        var (game, scene, ecs) = Start();
        ecs.Systems.Add("move", Move);
        Entity row = default;
        for (int i = 0; i < 20; i++)
        {
            var created = ecs.World.Create(new Place { Value = new Vector2(i, 2 * i) }, new Speed { Value = new Vector2(i % 3, 1) }, new Health { Value = 5 }, new Note());
            if (i == 7) row = created;
        }
        ecs.World.Create(new Place());
        for (int i = 0; i < 10; i++) game.StepForTest();
        return (game, scene, ecs, row);
    }

    [TestMethod]
    public void Equal_stores_hash_the_same_and_any_change_of_simulation_state_changes_the_hash()
    {
        var (first, _, _, _) = Populated();
        var (second, scene, ecs, row) = Populated();
        ulong hash = first.ComputeStateHash().Hash;
        Assert.AreEqual(hash, second.ComputeStateHash().Hash, "the same steps end in the same hash");

        ecs.World.Get<Place>(row).Value.X += 0.25f;
        Assert.AreNotEqual(hash, second.ComputeStateHash().Hash, "a state component's value");
        ecs.World.Get<Place>(row).Value.X -= 0.25f;
        Assert.AreEqual(hash, second.ComputeStateHash().Hash);

        ecs.World.Get<Health>(row).Value--;
        Assert.AreNotEqual(hash, second.ComputeStateHash().Hash, "a component hashed by a function");
        ecs.World.Get<Health>(row).Value++;

        ecs.World.Add<Marked>(row);
        ulong marked = second.ComputeStateHash().Hash;
        Assert.AreNotEqual(hash, marked, "a tag");
        ecs.World.Remove<Marked>(row);
        Assert.AreEqual(hash, second.ComputeStateHash().Hash);

        var entity = scene.Objects.Create();
        ulong withEntity = second.ComputeStateHash().Hash;
        ecs.Link(row, entity);
        Assert.AreNotEqual(withEntity, second.ComputeStateHash().Hash, "a link");
        ecs.Unlink(row);
        Assert.AreEqual(withEntity, second.ComputeStateHash().Hash);

        ecs.Systems["move"].Enabled = false;
        Assert.AreNotEqual(withEntity, second.ComputeStateHash().Hash, "a system switched off");
    }

    [TestMethod]
    public void The_hash_leaves_out_what_is_not_simulation_state_and_drawing_does_not_change_it()
    {
        var (game, _, ecs, row) = Populated();
        ulong hash = game.ComputeStateHash().Hash;

        ecs.World.Get<Note>(row).Value = 99;
        Assert.AreEqual(hash, game.ComputeStateHash().Hash, "a component added without AddState is not hashed");

        ecs.PrepareDraw(0.3f);
        _ = ecs.Blended<Place>(row);
        ecs.PrepareDraw(0.9f);
        Assert.AreEqual(hash, game.ComputeStateHash().Hash, "preparing and reading a frame changes nothing the hash covers");
    }

    [TestMethod]
    public void A_scene_without_rows_or_systems_and_a_scene_without_a_store_hash_differently_but_steadily()
    {
        var (withStore, _, _) = Start();
        var bare = new TestGame(new Host());
        bare.InitializeForTest(new HeadlessPlatform());
        Assert.AreNotEqual(bare.ComputeStateHash().Hash, withStore.ComputeStateHash().Hash, "the store is one of the scene's systems");
        var (again, _, _) = Start();
        Assert.AreEqual(again.ComputeStateHash().Hash, withStore.ComputeStateHash().Hash);
    }

    // ------------------------------------------------------------------ registration

    [TestMethod]
    public void A_component_missing_from_the_stores_components_fails_the_step_and_names_itself()
    {
        var (game, _, ecs) = Start();
        ecs.World.Create(new Place(), new Stray());
        var error = Assert.ThrowsExactly<InvalidOperationException>(game.StepForTest);
        StringAssert.Contains(error.Message, "Stray");
        StringAssert.Contains(error.Message, "EcsComponents");
        StringAssert.Contains(error.Message, "Add<Stray>");

        // Added later to a live row, through the commands: the same.
        var (second, _, other) = Start();
        var row = other.World.Create(new Place());
        second.StepForTest();
        other.Commands.Add(row, new Stray());
        Assert.ThrowsExactly<InvalidOperationException>(second.StepForTest);
    }

    [TestMethod]
    public void A_component_is_added_once_and_the_components_say_what_they_hold()
    {
        var components = Components();
        Assert.AreEqual(5, components.Count);
        Assert.IsTrue(components.Contains(typeof(Place)) && components.Contains(typeof(Marked)));
        Assert.IsFalse(components.Contains(typeof(Stray)));
        var twice = Assert.ThrowsExactly<InvalidOperationException>(() => components.Add<Place>());
        StringAssert.Contains(twice.Message, "Place");
        Assert.ThrowsExactly<InvalidOperationException>(() => components.SyncTransform<Stray>(static (in Stray _, ref Transform _) => { }));
    }

    // ------------------------------------------------------------------ cost

    [TestMethod]
    public void Stepping_a_store_with_systems_links_and_rows_that_come_and_go_allocates_nothing_once_warm()
    {
        var (game, scene, ecs) = Start();
        ecs.Systems.Add(new Mover());
        ecs.LateSystems.Add(new Recycler());
        for (int i = 0; i < 2000; i++) ecs.World.Create(new Place { Value = new Vector2(i, 0) }, new Speed { Value = new Vector2(1, 1) }, new Health { Value = 30 + i % 50 });
        for (int i = 0; i < 20; i++)
        {
            var row = ecs.World.Create(new Place(), new Speed { Value = new Vector2(2, 0) });
            ecs.Link(row, scene.Objects.Create());
        }
        for (int i = 0; i < 300; i++) game.StepForTest();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            game.StepForTest();
            ecs.PrepareDraw(0.5f);
        }
        long perStep = (GC.GetAllocatedBytesForCurrentThread() - before) / 100;
        Assert.AreEqual(0, perStep, $"{perStep} bytes a step");
    }

    private sealed class Mover : EcsSystem
    {
        private static readonly QueryDescription Moving = new QueryDescription().WithAll<Place, Speed>();
        protected override void Update(float dt)
        {
            foreach (ref var chunk in World.Query(in Moving))
            {
                var places = chunk.GetSpan<Place>();
                var speeds = chunk.GetSpan<Speed>();
                for (int i = 0; i < chunk.Count; i++) places[i].Value += speeds[i].Value * dt;
            }
        }
    }

    /// <summary>
    /// Rows whose health runs out are destroyed and replaced. The system keeps them in a list and changes the rows
    /// after its walk: the library's command buffer allocates when it is played back, a list does not.
    /// </summary>
    private sealed class Recycler : EcsSystem
    {
        private static readonly QueryDescription Mortal = new QueryDescription().WithAll<Health>();
        private readonly List<Entity> _spent = [];
        protected override void Update(float dt)
        {
            foreach (ref var chunk in World.Query(in Mortal))
            {
                var health = chunk.GetSpan<Health>();
                for (int i = 0; i < chunk.Count; i++)
                    if (--health[i].Value <= 0) _spent.Add(chunk.Entity(i));
            }
            foreach (var row in _spent)
            {
                World.Destroy(row);
                World.Create(new Place(), new Speed { Value = new Vector2(1, 1) }, new Health { Value = 40 });
            }
            _spent.Clear();
        }
    }
}
