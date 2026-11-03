using System.Numerics;
using MicroEngine.World;

namespace MicroEngine.Tests;

[TestClass]
public sealed class WorldTests
{
    private static readonly Dictionary<char, int> Legend = new() { ['#'] = 1, ['='] = 2, ['^'] = 3 };
    private static readonly Dictionary<int, TileFlags> Flags = new() { [1] = TileFlags.Solid, [2] = TileFlags.OneWay, [3] = TileFlags.Hazard };

    private static TileGrid Grid(params string[] rows) => new(TileLayer.Parse(rows, Legend), 16, Flags);

    [TestMethod]
    public void Falling_body_lands_flush_on_solid_floor()
    {
        var grid = Grid("....", "....", "####");
        var result = TileMotion.Move(new RectF(16, 0, 12, 14), new Vector2(0, 40), grid);
        Assert.IsTrue(result.HitFloor);
        Assert.AreEqual(32 - 14, result.Bounds.Y, 0.001f);
    }

    [TestMethod]
    public void Fast_body_does_not_tunnel_through_a_thin_wall()
    {
        var grid = Grid("..#.....", "..#.....");
        var result = TileMotion.Move(new RectF(0, 0, 10, 10), new Vector2(200, 0), grid);
        Assert.IsTrue(result.HitRight);
        Assert.AreEqual(32 - 10, result.Bounds.X, 0.001f);
    }

    [TestMethod]
    public void One_way_platform_supports_from_above_but_not_from_below()
    {
        var grid = Grid("....", "....", "====", "....", "....");
        grid.OutsideFlags = TileFlags.None;
        var landing = TileMotion.Move(new RectF(0, 10, 12, 14), new Vector2(0, 20), grid);
        Assert.IsTrue(landing.HitFloor, "Falling onto the platform must land.");
        Assert.AreEqual(32 - 14, landing.Bounds.Y, 0.001f);

        var jumping = TileMotion.Move(new RectF(0, 50, 12, 14), new Vector2(0, -30), grid);
        Assert.IsFalse(jumping.HitCeiling, "Jumping up through the platform must pass.");
        Assert.AreEqual(20, jumping.Bounds.Y, 0.001f);
    }

    [TestMethod]
    public void One_way_platform_can_be_ignored_to_drop_through()
    {
        var grid = Grid("....", "====", "....");
        grid.OutsideFlags = TileFlags.None;
        var box = new RectF(0, 2, 12, 14);
        Assert.IsTrue(TileMotion.IsOnGround(box, grid));
        Assert.IsFalse(TileMotion.IsOnGround(box, grid, ignoreOneWay: true));
    }

    [TestMethod]
    public void Hazard_tiles_are_reported_but_do_not_block()
    {
        var grid = Grid("....", ".^..", "####");
        var result = TileMotion.Move(new RectF(0, 18, 12, 14), new Vector2(10, 0), grid);
        Assert.IsFalse(result.HitWall);
        Assert.IsTrue(result.Touched.HasFlag(TileFlags.Hazard));
    }

    [TestMethod]
    public void Extra_solids_block_like_tiles()
    {
        var grid = Grid("......", "......");
        grid.OutsideFlags = TileFlags.None;
        var crate = new RectF(40, 0, 16, 16);
        var result = TileMotion.Move(new RectF(0, 0, 12, 12), new Vector2(60, 0), grid, [crate]);
        Assert.IsTrue(result.HitRight);
        Assert.AreEqual(28, result.Bounds.X, 0.001f);
    }

    [TestMethod]
    public void Outside_rule_can_open_a_bottomless_pit()
    {
        var grid = Grid("..", "..");
        grid.OutsideRule = (_, y) => y >= 2 ? TileFlags.None : null;
        var result = TileMotion.Move(new RectF(0, 10, 10, 10), new Vector2(0, 30), grid);
        Assert.IsFalse(result.HitFloor);
        Assert.AreEqual(TileFlags.Solid, grid.FlagsAt(-1, 0), "Side walls stay closed.");
    }

    [TestMethod]
    public void Autotile_mask_reflects_matching_neighbours()
    {
        var layer = TileLayer.Parse(["...", ".##", ".#."], Legend);
        int mask = Autotile.Mask4(layer, 1, 1, id => id == 1, outsideMatches: false);
        Assert.AreEqual(2 | 4, mask, "Right and down neighbours match.");
    }

    [TestMethod]
    public void Find_locates_spawn_markers()
    {
        var spawns = TileLayer.Find(["..P.", "E..E"], 'E').ToList();
        CollectionAssert.AreEqual(new[] { (0, 1), (3, 1) }, spawns);
    }

    private static TileGrid Open(params string[] rows)
    {
        var grid = Grid(rows);
        grid.OutsideFlags = TileFlags.None;
        return grid;
    }

    [TestMethod]
    public void D6_a_fast_small_body_does_not_tunnel_through_a_thin_extra_solid_E1()
    {
        // A3 WO-2 E1: a 2×2 body moving 15.9 px down through a 4 px solid at y 100..104 used to end at y 105.9.
        var thin = new RectF(0, 100, 40, 4);
        var result = TileMotion.Move(new RectF(20, 90, 2, 2), new Vector2(0, 15.9f), Open("...."), [thin]);
        Assert.IsTrue(result.HitFloor, result.ToString());
        Assert.AreEqual(98, result.Bounds.Y);
        Assert.IsTrue(TileMotion.IsOnGround(result.Bounds, Open("...."), [thin]));
    }

    [TestMethod]
    public void D6_a_fast_body_does_not_tunnel_sideways_through_a_thin_extra_solid()
    {
        // Sub-steps of 7.5 px used to jump a 2 px box over a 1 px solid.
        var thin = new RectF(60, 0, 1, 40);
        var result = TileMotion.Move(new RectF(40, 10, 2, 2), new Vector2(30, 0), Open("........"), [thin]);
        Assert.IsTrue(result.HitRight, result.ToString());
        Assert.AreEqual(58, result.Bounds.X);
    }

    [TestMethod]
    public void D6_a_solid_that_rose_into_a_resting_body_lifts_it_and_it_stays_on_ground_E2()
    {
        // A3 WO-2 E2: a 10×10 body resting at bottom 90, platform raised to top 88: after a 2 px fall the body used to
        // end embedded (bottom 92) with HitFloor and IsOnGround false.
        var grid = Open("....");
        var platform = new RectF(0, 88, 40, 8);
        var result = TileMotion.Move(new RectF(10, 80, 10, 10), new Vector2(0, 2), grid, [platform]);
        Assert.AreEqual(88, result.Bounds.Bottom, result.ToString());
        Assert.IsTrue(result.HitFloor);
        Assert.IsTrue(TileMotion.IsOnGround(result.Bounds, grid, [platform]));
    }

    [TestMethod]
    public void A_solid_the_box_overlaps_from_the_side_still_does_not_trap_it()
    {
        // Unchanged: a rectangle the box already overlaps does not block it (here it overlaps from the side, so it is not
        // a platform that rose into it and the box keeps moving).
        var crate = new RectF(20, 0, 16, 16);
        var right = TileMotion.Move(new RectF(10, 2, 12, 12), new Vector2(5, 0), Open("....", "...."), [crate]);
        Assert.AreEqual(15, right.Bounds.X);
        Assert.IsFalse(right.HitWall);
        var down = TileMotion.Move(new RectF(10, 2, 12, 12), new Vector2(0, 3), Open("....", "...."), [crate]);
        Assert.AreEqual(5, down.Bounds.Y, "its top edge is not in the box's lower half: no lift");
    }

    [TestMethod]
    public void Sub_stepped_free_movement_keeps_the_exact_positions_of_earlier_versions()
    {
        // Unchanged: a move longer than half a tile advances in equal sub-steps, so positions match bit for bit.
        var start = new RectF(0.3f, 0.7f, 5, 5);
        var delta = new Vector2(37.3f, 23.9f);
        var result = TileMotion.Move(start, delta, Open("......", "......", "......", "......"));
        float x = start.X, y = start.Y;
        for (int i = 0; i < 5; i++) x += delta.X / 5;
        for (int i = 0; i < 3; i++) y += delta.Y / 3;
        Assert.AreEqual(x, result.Bounds.X);
        Assert.AreEqual(y, result.Bounds.Y);
    }

    private sealed class Probe(float y) : Entity
    {
        public override float Depth => y;
        public int Updates;
        public List<string>? Log;
        public Action? OnUpdate;
        public override void Update(float dt) { Updates++; OnUpdate?.Invoke(); }
        public override void Removed() => Log?.Add("removed");
    }

    [TestMethod]
    public void Entities_spawned_during_update_join_after_the_update()
    {
        var list = new EntityList<Entity>();
        var child = new Probe(0);
        var parent = list.Add(new Probe(0));
        parent.OnUpdate = () => { if (parent.Updates == 1) list.Add(child); };
        list.Update(0.016f);
        Assert.AreEqual(0, child.Updates, "New entities are not updated in the frame they spawn.");
        Assert.AreEqual(2, list.Count);
        list.Update(0.016f);
        Assert.AreEqual(1, child.Updates);
    }

    private sealed class DrawLog(List<string> log, string name, float depth) : Entity
    {
        public override float Depth => depth;
        public override void Draw(MicroEngine.Graphics.IRenderer renderer) => log.Add(name);
    }

    [TestMethod]
    public void Walking_querying_and_drawing_an_entity_list_allocates_nothing_and_keeps_depth_order_stable()
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
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < 100; frame++)
        {
            foreach (var e in list) alive += e.Alive ? 0 : 1;
            foreach (var e in list.OfType<DrawLog>()) alive += e.Alive ? 0 : 1;
            foreach (var e in list.Overlapping(new RectF(0, 0, 8, 8))) alive += e.Alive ? 0 : 1;
            list.Draw(renderer);
            drawn.Clear();
        }
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "every frame reuses what it needs");
    }

    [TestMethod]
    public void An_entity_can_clear_its_list_during_update()
    {
        var list = new EntityList<Entity>();
        var log = new List<string>();
        var before = list.Add(new Probe(0) { Log = log });
        var clearer = list.Add(new Probe(0) { Log = log });
        var after = list.Add(new Probe(0) { Log = log });
        var spawnedBefore = new Probe(0);
        var spawnedAfter = new Probe(0);
        clearer.OnUpdate = () =>
        {
            list.Add(spawnedBefore);
            list.Clear();
            Assert.AreEqual(0, list.Count, "cleared at once");
            Assert.IsFalse(list.Any(), "nothing left to walk");
            list.Add(spawnedAfter);
        };
        list.Update(0.016f);
        Assert.AreEqual(1, before.Updates);
        Assert.AreEqual(1, clearer.Updates);
        Assert.AreEqual(0, after.Updates, "entities not yet updated when the list was cleared are skipped");
        CollectionAssert.AreEqual(new[] { spawnedAfter }, list.ToList(), "an entity added after the clear joins at the end of the update");
        Assert.AreEqual(0, log.Count, "clearing does not call Removed");
        list.Update(0.016f);
        Assert.AreEqual(1, spawnedAfter.Updates);
        Assert.AreEqual(0, spawnedBefore.Updates);
    }

    [TestMethod]
    public void An_entity_can_destroy_others_during_update()
    {
        var list = new EntityList<Entity>();
        var log = new List<string>();
        var killer = list.Add(new Probe(0));
        var earlier = new Probe(0) { Log = log };
        var later = new Probe(0) { Log = log };
        list.Add(earlier);
        list.Add(later);
        killer.OnUpdate = () => { earlier.Destroy(); later.Destroy(); killer.Destroy(); };
        list.Update(0.016f);
        Assert.AreEqual(0, earlier.Updates + later.Updates, "destroyed entities stop updating at once");
        Assert.AreEqual(0, list.Count);
        CollectionAssert.AreEqual(new[] { "removed", "removed" }, log, "each removed once, at the end of the update");
    }

    [TestMethod]
    public void Destroyed_entities_are_removed_and_notified_once()
    {
        var list = new EntityList<Entity>();
        var log = new List<string>();
        var e = list.Add(new Probe(0) { Log = log });
        e.Destroy();
        list.Update(0.016f);
        list.Update(0.016f);
        Assert.AreEqual(0, list.Count);
        CollectionAssert.AreEqual(new[] { "removed" }, log);
    }
}
