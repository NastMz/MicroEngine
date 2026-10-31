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
