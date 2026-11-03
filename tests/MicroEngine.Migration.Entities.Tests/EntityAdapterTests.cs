using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.Testing;
using MicroEngine.World;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace MicroEngine.Migration.Entities.Tests;

/// <summary>
/// The transitional <see cref="Entity"/>/<see cref="EntityList{T}"/> adapter over <see cref="IWorld"/>: the legacy
/// behaviour the demos rely on, and the documented changes of the mapping (Removed runs when the world releases the
/// entity, in destroy order; Clear destroys, so Removed runs).
/// </summary>
[TestClass]
public sealed class EntityAdapterTests
{
    private sealed class Probe(float y = 0) : Entity
    {
        public override float Depth => y;
        public int Updates;
        public string Name = "";
        public List<string>? Log;
        public Action? OnUpdate;
        public override void Update(float dt) { Updates++; OnUpdate?.Invoke(); }
        public override void Removed() => Log?.Add($"removed {Name}");
    }

    private sealed class DrawLog(List<string> log, string name, float depth) : Entity
    {
        public override float Depth => depth;
        public override void Draw(IRenderer renderer) => log.Add(name);
    }

    [TestMethod]
    public void An_entity_keeps_its_position_hitbox_and_bounds_before_and_after_joining_a_list()
    {
        var list = new EntityList<Entity>();
        var e = new Probe { Position = new Vector2(3, 4), Hitbox = new RectF(1, 1, 8, 8) };
        Assert.IsTrue(e.Alive);
        Assert.AreEqual(EntityId.None, e.Id, "No entity id before it joins a list.");

        list.Add(e);

        Assert.IsFalse(e.Id.IsNone);
        Assert.AreEqual(new Vector2(3, 4), e.Position, "The position moves into the world's transform.");
        Assert.AreEqual(new Vector2(3, 4), list.World.Local(e.Id).Position2D);
        e.Position += new Vector2(1, 0);
        Assert.AreEqual(new Vector2(4, 4), list.World.Local(e.Id).Position2D);
        Assert.AreEqual(new RectF(5, 5, 8, 8), e.Bounds);
        Assert.AreEqual(new Vector2(9, 9), e.Center);
        Assert.AreSame(e, list.World.Get<Entity>(e.Id), "Attached under the Entity type.");
    }

    [TestMethod]
    public void Entities_added_during_update_are_updated_from_the_next_update()
    {
        var list = new EntityList<Entity>();
        var child = new Probe();
        var parent = list.Add(new Probe());
        parent.OnUpdate = () => { if (parent.Updates == 1) list.Add(child); };
        list.Update(0.016f);
        Assert.AreEqual(0, child.Updates, "New entities are not updated in the update they spawn in.");
        Assert.AreEqual(2, list.Count);
        list.Update(0.016f);
        Assert.AreEqual(1, child.Updates);
    }

    [TestMethod]
    public void An_entity_added_outside_an_update_is_visible_at_once()
    {
        var list = new EntityList<Entity>();
        var e = list.Add(new Probe());
        CollectionAssert.AreEqual(new Entity[] { e }, list.ToList());
    }

    [TestMethod]
    public void Walking_querying_and_drawing_allocates_nothing_and_keeps_depth_order_stable()
    {
        var list = new EntityList<Entity>();
        var drawn = new List<string>(64);
        list.Add(new DrawLog(drawn, "b1", 2));
        list.Add(new DrawLog(drawn, "a", 1));
        list.Add(new DrawLog(drawn, "b2", 2)).Destroy();
        list.Add(new DrawLog(drawn, "b3", 2));
        list.Add(new DrawLog(drawn, "c", 0));
        var renderer = new MicroEngine.Core.HeadlessRenderer(320, 180);
        list.Draw(renderer);
        CollectionAssert.AreEqual(new[] { "c", "a", "b1", "b3" }, drawn, "by depth, insertion order for equal depth, living only");

        int alive = 0;
        foreach (var e in list) alive++;
        Assert.AreEqual(4, alive);
        Assert.AreEqual(4, list.OfType<DrawLog>().Count(), "OfType skips the dead");
        Assert.AreEqual(4, list.Overlapping(new RectF(-1, -1, 18, 18)).Count(), "every living entity overlaps its own corner");
        Assert.AreEqual(0, list.Overlapping(new RectF(100, 100, 4, 4)).Count());

        drawn.Clear();
        AllocationAssert.None(() =>
        {
            foreach (var e in list) alive += e.Alive ? 0 : 1;
            foreach (var e in list.OfType<DrawLog>()) alive += e.Alive ? 0 : 1;
            foreach (var e in list.Overlapping(new RectF(0, 0, 8, 8))) alive += e.Alive ? 0 : 1;
            list.Draw(renderer);
            drawn.Clear();
        }, calls: 20, because: "every frame reuses what it needs");
        Assert.AreEqual(4, alive, "the walks saw living entities only");
    }

    [TestMethod]
    public void Drawing_with_a_camera_skips_entities_outside_its_view()
    {
        var list = new EntityList<Entity>();
        var drawn = new List<string>();
        list.Add(new DrawLog(drawn, "near", 0) { Position = new Vector2(10, 10) });
        list.Add(new DrawLog(drawn, "far", 0) { Position = new Vector2(5000, 10) });
        list.Draw(new MicroEngine.Core.HeadlessRenderer(320, 180), new Camera2D(320, 180));
        CollectionAssert.AreEqual(new[] { "near" }, drawn);
    }

    [TestMethod]
    public void An_entity_can_destroy_others_during_update_and_each_is_removed_once_in_destroy_order()
    {
        var list = new EntityList<Entity>();
        var log = new List<string>();
        var killer = list.Add(new Probe { Name = "killer", Log = log });
        var earlier = list.Add(new Probe { Name = "earlier", Log = log });
        var later = list.Add(new Probe { Name = "later", Log = log });
        killer.OnUpdate = () => { later.Destroy(); earlier.Destroy(); killer.Destroy(); };

        list.Update(0.016f);

        Assert.AreEqual(0, earlier.Updates + later.Updates, "destroyed entities stop updating at once");
        Assert.AreEqual(0, list.Count);
        CollectionAssert.AreEqual(new[] { "removed later", "removed earlier", "removed killer" }, log, "at the end of the update, in destroy order");
        Assert.IsFalse(killer.Alive);
        Assert.AreEqual(Vector2.Zero, killer.Position, "a removed entity keeps its last position");
    }

    [TestMethod]
    public void Destroyed_entities_are_removed_and_notified_once_and_count_until_flushed()
    {
        var list = new EntityList<Entity>();
        var log = new List<string>();
        var e = list.Add(new Probe { Log = log });
        e.Destroy();
        Assert.IsFalse(e.Alive);
        Assert.AreEqual(1, list.Count, "counted until the list is flushed, as before");
        list.Flush();
        list.Update(0.016f);
        Assert.AreEqual(0, list.Count);
        CollectionAssert.AreEqual(new[] { "removed " }, log);
    }

    [TestMethod]
    public void An_entity_destroyed_before_joining_never_updates_and_is_removed_at_the_flush()
    {
        var list = new EntityList<Entity>();
        var log = new List<string>();
        var e = new Probe { Log = log };
        e.Destroy();
        list.Add(e);
        list.Update(0.016f);
        Assert.AreEqual(0, e.Updates);
        CollectionAssert.AreEqual(new[] { "removed " }, log);
    }

    [TestMethod]
    public void Clear_destroys_every_entity_skipping_those_not_yet_updated_and_now_calls_Removed()
    {
        var list = new EntityList<Entity>();
        var log = new List<string>();
        var before = list.Add(new Probe { Name = "before", Log = log });
        var clearer = list.Add(new Probe { Name = "clearer", Log = log });
        var after = list.Add(new Probe { Name = "after", Log = log });
        var spawnedAfter = new Probe { Name = "spawned" };
        clearer.OnUpdate = () =>
        {
            list.Clear();
            Assert.AreEqual(0, list.Count, "cleared at once, as before");
            Assert.IsFalse(list.Any(), "nothing left to walk");
            list.Add(spawnedAfter);
        };

        list.Update(0.016f);
        Assert.AreEqual(1, list.Count, "the cleared entities released at the flush are not counted twice");

        Assert.AreEqual(1, before.Updates);
        Assert.AreEqual(0, after.Updates, "entities not yet updated when the list was cleared are skipped");
        CollectionAssert.AreEqual(new[] { spawnedAfter }, list.ToList(), "an entity added after the clear stays");
        CollectionAssert.AreEqual(new[] { "removed before", "removed clearer", "removed after" }, log, "the change: Removed runs");
        list.Update(0.016f);
        Assert.AreEqual(1, spawnedAfter.Updates);
    }

    [TestMethod]
    public void Two_lists_keep_their_own_entities()
    {
        var a = new EntityList<Entity>();
        var b = new EntityList<Entity>();
        var e = a.Add(new Probe());
        b.Add(new Probe());
        Assert.AreEqual(1, a.Count());
        Assert.AreEqual(1, b.Count());
        Assert.ThrowsExactly<InvalidOperationException>(() => b.Add(e), "an entity belongs to one list");
    }

    [TestMethod]
    public void The_adapter_is_obsolete_with_its_own_diagnostic()
    {
        foreach (var type in new[] { typeof(Entity), typeof(EntityList<>) })
        {
            var obsolete = (ObsoleteAttribute?)Attribute.GetCustomAttribute(type, typeof(ObsoleteAttribute));
            Assert.IsNotNull(obsolete, type.Name);
            Assert.AreEqual("MEMIG001", obsolete.DiagnosticId);
            StringAssert.Contains(obsolete.Message, "IWorld");
        }
        Assert.AreEqual("MicroEngine.Migration.Entities", typeof(Entity).Assembly.GetName().Name);
    }
}
