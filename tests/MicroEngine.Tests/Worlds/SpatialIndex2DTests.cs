using System.Numerics;
using MicroEngine.Scenes;
using MicroEngine.Testing;

namespace MicroEngine.Tests.Worlds;

/// <summary>
/// <see cref="SpatialIndex2D{T}"/>: every query gives exactly what a walk over every item gives, whatever the cell
/// size, in an order that depends only on the calls made, without allocating.
/// </summary>
[TestClass]
public sealed class SpatialIndex2DTests
{
    private static List<int> Rect(SpatialIndex2D<int> index, RectF area)
    {
        var found = new List<int>();
        index.Query(area, found);
        return found;
    }

    private static List<int> Circle(SpatialIndex2D<int> index, Vector2 center, float radius)
    {
        var found = new List<int>();
        index.QueryCircle(center, radius, found);
        return found;
    }

    private static void AssertSame(IEnumerable<int> expected, List<int> found, string what)
    {
        CollectionAssert.AreEqual(expected.Order().ToList(), found.Order().ToList(), what);
        Assert.AreEqual(found.Count, found.Distinct().Count(), $"{what}: an item is reported once");
    }

    [TestMethod]
    public void A_rectangle_query_finds_the_items_that_overlap_or_touch_the_area_and_no_other()
    {
        var index = new SpatialIndex2D<int>(32);
        index.Add(1, new Vector2(10, 10));                 // a point inside
        index.Add(2, new Vector2(50, 20));                 // a point on the right edge
        index.Add(3, new Vector2(50.01f, 20));             // just outside
        index.Add(4, new RectF(-40, -40, 40, 40));         // a rectangle touching the corner (0, 0)
        index.Add(5, new RectF(60, 0, 10, 10));            // a rectangle outside
        index.Add(6, new RectF(-100, 5, 400, 2));          // a long rectangle crossing the area
        index.Add(7, new Vector2(10, 50));                 // a point on the bottom edge
        index.Add(8, new Vector2(10, -0.5f));              // just above

        AssertSame([1, 2, 4, 6, 7], Rect(index, new RectF(0, 0, 50, 50)), "edges included");
        AssertSame([1], Rect(index, new RectF(10, 10, 0, 0)), "an area of size zero is a point");
        Assert.AreEqual(8, index.Count);
    }

    [TestMethod]
    public void A_circle_query_finds_the_items_with_a_point_within_the_radius()
    {
        var index = new SpatialIndex2D<int>(16);
        index.Add(1, new Vector2(100, 100));               // the centre itself
        index.Add(2, new Vector2(130, 140));               // exactly 50 away (30, 40)
        index.Add(3, new Vector2(131, 140));               // farther than 50
        index.Add(4, new Vector2(140, 140));               // inside the circle's square, outside the circle
        index.Add(5, new RectF(150, 90, 30, 20));          // a rectangle whose left edge is exactly 50 away
        index.Add(6, new RectF(151, 90, 30, 20));          // one unit farther
        index.Add(7, new RectF(0, 0, 300, 300));           // a rectangle that contains the circle
        index.Add(8, new RectF(136, 136, 50, 50));         // its corner is 36 * sqrt(2) = 50.9 away

        AssertSame([1, 2, 5, 7], Circle(index, new Vector2(100, 100), 50), "the nearest point of each item against the radius");
        AssertSame([1, 7], Circle(index, new Vector2(100, 100), 0), "a radius of zero is a point");
    }

    [TestMethod]
    [DataRow(4f)]
    [DataRow(37.5f)]
    [DataRow(1000f)]
    public void Queries_give_what_a_walk_over_every_item_gives_whatever_the_cell_size(float cell)
    {
        var random = new Random(7);
        var index = new SpatialIndex2D<int>(cell);
        var bounds = new Dictionary<int, RectF>();
        var handles = new Dictionary<int, SpatialHandle>();
        float Coordinate() => random.NextSingle() * 1200 - 600;   // both signs
        RectF Place() => random.Next(4) switch
        {
            0 => new RectF(Coordinate(), Coordinate(), random.NextSingle() * 60, random.NextSingle() * 60),
            1 => new RectF(Coordinate(), Coordinate(), random.NextSingle() * 900, random.NextSingle() * 900),   // covers many cells
            _ => new RectF(Coordinate(), Coordinate(), 0, 0),
        };
        static bool Touches(RectF b, RectF a) => b.Left <= a.Right && b.Right >= a.Left && b.Top <= a.Bottom && b.Bottom >= a.Top;
        static bool Within(RectF b, Vector2 c, float r)
        {
            float dx = MathF.Max(MathF.Max(b.Left - c.X, c.X - b.Right), 0), dy = MathF.Max(MathF.Max(b.Top - c.Y, c.Y - b.Bottom), 0);
            return dx * dx + dy * dy <= r * r;
        }

        int next = 0;
        for (int round = 0; round < 400; round++)
        {
            // A history of additions, moves and removals, so cells are entered, left and reused.
            int action = random.Next(10);
            if (action < 5 || bounds.Count < 20)
            {
                var place = Place();
                handles[next] = place.Width == 0 && place.Height == 0 ? index.Add(next, place.Position) : index.Add(next, place);
                bounds[next++] = place;
            }
            else if (action < 8)
            {
                int item = bounds.Keys.ElementAt(random.Next(bounds.Count));
                var place = Place();
                if (place.Width == 0 && place.Height == 0) index.Move(handles[item], place.Position); else index.Move(handles[item], place);
                bounds[item] = place;
            }
            else
            {
                int item = bounds.Keys.ElementAt(random.Next(bounds.Count));
                Assert.IsTrue(index.Remove(handles[item]));
                bounds.Remove(item);
                handles.Remove(item);
            }

            var area = new RectF(Coordinate(), Coordinate(), random.NextSingle() * 300, random.NextSingle() * 300);
            AssertSame(bounds.Where(b => Touches(b.Value, area)).Select(b => b.Key), Rect(index, area), $"round {round}, {area}");
            var center = new Vector2(Coordinate(), Coordinate());
            float radius = random.NextSingle() * 200;
            AssertSame(bounds.Where(b => Within(b.Value, center, radius)).Select(b => b.Key), Circle(index, center, radius), $"round {round}, circle {center} {radius}");
            Assert.AreEqual(bounds.Count, index.Count);
        }
        AssertSame(bounds.Keys, Rect(index, new RectF(-1e9f, -1e9f, 2e9f, 2e9f)), "an area over everything finds everything");
        foreach (var (item, handle) in handles)
        {
            Assert.AreEqual(item, index.ItemOf(handle));
            Assert.AreEqual(bounds[item], index.BoundsOf(handle));
        }
    }

    [TestMethod]
    public void A_moved_item_is_found_where_it_is_now_and_not_where_it_was()
    {
        var index = new SpatialIndex2D<string>(32);
        var a = index.Add("a", new Vector2(10, 10));
        var found = new List<string>();

        index.Move(a, new Vector2(12, 12));                // within its cell
        Assert.AreEqual(1, index.Query(new RectF(11, 11, 2, 2), found));
        Assert.AreEqual(0, index.Query(new RectF(9, 9, 1.5f, 1.5f), found), "not at the old point");
        Assert.AreEqual(new RectF(12, 12, 0, 0), index.BoundsOf(a));

        index.Move(a, new Vector2(500, -300));             // to another cell
        Assert.AreEqual(0, index.Query(new RectF(0, 0, 32, 32), found));
        Assert.AreEqual(1, index.QueryCircle(new Vector2(500, -300), 1, found));

        index.Move(a, new RectF(0, 0, 100, 100));          // a point becomes a rectangle over several cells
        Assert.AreEqual(1, index.Query(new RectF(90, 90, 5, 5), found));
        Assert.AreEqual(0, index.QueryCircle(new Vector2(500, -300), 1, found));
        Assert.AreEqual(1, index.Count);
        CollectionAssert.AreEqual(new[] { "a", "a", "a" }, found, "a list is added to, never cleared");
    }

    [TestMethod]
    public void A_removed_item_is_not_found_and_its_handle_is_stale()
    {
        var index = new SpatialIndex2D<int>(32);
        var one = index.Add(1, new Vector2(5, 5));
        var two = index.Add(2, new RectF(0, 0, 10, 10));

        Assert.IsTrue(index.Contains(one));
        Assert.IsTrue(index.Remove(one));
        Assert.IsFalse(index.Contains(one));
        Assert.IsFalse(index.Remove(one), "removing twice does nothing");
        Assert.IsFalse(index.Remove(SpatialHandle.None));
        Assert.IsFalse(index.Contains(SpatialHandle.None));
        AssertSame([2], Rect(index, new RectF(0, 0, 10, 10)), "only the other item is left");
        Assert.AreEqual(1, index.Count);

        var three = index.Add(3, new Vector2(5, 5));        // takes the freed slot with a new generation
        Assert.AreEqual(one.Index, three.Index);
        Assert.AreNotEqual(one, three);
        Assert.IsFalse(index.Contains(one), "the old handle does not reach the new item");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => index.Move(one, new Vector2(1, 1))).Message, one.ToString());
        Assert.ThrowsExactly<InvalidOperationException>(() => index.Move(one, new RectF(0, 0, 1, 1)));
        Assert.ThrowsExactly<InvalidOperationException>(() => index.ItemOf(one));
        Assert.ThrowsExactly<InvalidOperationException>(() => index.BoundsOf(SpatialHandle.None));
        Assert.AreEqual(3, index.ItemOf(three));
        Assert.AreEqual(2, index.ItemOf(two));
        Assert.AreEqual("spatial:none", SpatialHandle.None.ToString());
        Assert.IsTrue(SpatialHandle.None.IsNone && !three.IsNone);
    }

    [TestMethod]
    public void Clear_empties_the_index_and_makes_every_handle_stale()
    {
        var index = new SpatialIndex2D<int>(32);
        var small = index.Add(1, new Vector2(5, 5));
        var large = index.Add(2, new RectF(-5000, -5000, 10000, 10000));

        index.Clear();

        Assert.AreEqual(0, index.Count);
        Assert.IsFalse(index.Contains(small) || index.Contains(large));
        Assert.AreEqual(0, Rect(index, new RectF(-10, -10, 20, 20)).Count);
        var again = index.Add(3, new Vector2(5, 5));
        Assert.IsFalse(index.Contains(small), "a handle from before the clear does not reach an item added after it");
        AssertSame([3], Rect(index, new RectF(0, 0, 10, 10)), "the index works after a clear");
        Assert.IsTrue(index.Contains(again));
    }

    [TestMethod]
    public void A_span_takes_the_first_results_and_the_query_returns_how_many_there_are()
    {
        var index = new SpatialIndex2D<int>(32);
        for (int i = 0; i < 10; i++) index.Add(i, new Vector2(i, i));
        var all = Rect(index, new RectF(0, 0, 10, 10));

        Span<int> three = stackalloc int[3];
        Assert.AreEqual(10, index.Query(new RectF(0, 0, 10, 10), three));
        CollectionAssert.AreEqual(all.Take(3).ToList(), three.ToArray(), "the first ones, in the order of the full result");
        Assert.AreEqual(10, index.QueryCircle(new Vector2(5, 5), 100, three));
        Assert.AreEqual(10, index.Query(new RectF(0, 0, 10, 10), Span<int>.Empty), "a count needs no room");
        Span<int> room = stackalloc int[16];
        Assert.AreEqual(10, index.QueryCircle(new Vector2(5, 5), 100, room));
        CollectionAssert.AreEqual(Circle(index, new Vector2(5, 5), 100), room[..10].ToArray());
    }

    [TestMethod]
    public void The_order_of_the_results_depends_only_on_the_calls_made()
    {
        static List<int> History()
        {
            var random = new Random(3);
            var index = new SpatialIndex2D<int>(24);
            var handles = new List<SpatialHandle>();
            for (int i = 0; i < 600; i++) handles.Add(index.Add(i, new Vector2(random.Next(400), random.Next(400))));
            for (int i = 0; i < 600; i += 3) index.Move(handles[i], new Vector2(random.Next(400), random.Next(400)));
            for (int i = 0; i < 600; i += 7) index.Remove(handles[i]);
            for (int i = 0; i < 40; i++) index.Add(1000 + i, new RectF(random.Next(400), random.Next(400), random.Next(90), random.Next(90)));
            var found = new List<int>();
            index.Query(new RectF(50, 50, 200, 200), found);
            index.QueryCircle(new Vector2(300, 300), 80, found);
            index.Query(new RectF(-1e6f, -1e6f, 2e6f, 2e6f), found);   // large enough to walk the items, not the cells
            return found;
        }

        var first = History();
        Assert.IsTrue(first.Count > 600);
        CollectionAssert.AreEqual(first, History(), "the same history gives the same results in the same order");
    }

    [TestMethod]
    public void Queries_and_moves_allocate_nothing()
    {
        var random = new Random(5);
        var index = new SpatialIndex2D<int>(32);
        var handles = new SpatialHandle[2000];
        for (int i = 0; i < handles.Length; i++) handles[i] = index.Add(i, new Vector2(random.Next(1000), random.Next(1000)));
        index.Add(-1, new RectF(0, 0, 1000, 1000));
        var results = new int[256];
        var list = new List<int>(4096);
        int step = 0;

        AllocationAssert.None(() =>
        {
            step++;
            for (int i = 0; i < handles.Length; i++)
                index.Move(handles[i], new Vector2((i * 37 + step * 13) % 1000, (i * 91 + step * 7) % 1000));   // every item changes cell
            index.Move(handles[0], new RectF(step % 50, 10, 300, 300));                                          // and one changes shape
            index.Query(new RectF(step % 800, 100, 200, 200), results);
            index.QueryCircle(new Vector2(500, step % 900), 120, results);
            list.Clear();
            index.Query(new RectF(0, 0, 1000, 1000), list);
        }, because: "a step of moves and queries on a warm index");
    }

    [TestMethod]
    public void Places_far_from_the_origin_and_areas_larger_than_the_world_are_answered()
    {
        var index = new SpatialIndex2D<int>(1);
        index.Add(1, new Vector2(3e9f, -3e9f));            // beyond what a cell number holds
        index.Add(2, new Vector2(-2e7f, 2e7f));
        index.Add(3, new RectF(-1e30f, -1e30f, 2e30f, 2e30f));

        AssertSame([1, 3], Circle(index, new Vector2(3e9f, -3e9f), 10), "a far point is found where it is");
        AssertSame([2, 3], Rect(index, new RectF(-2e7f - 1, 2e7f - 1, 2, 2)), "and only there");
        AssertSame([1, 2, 3], Rect(index, new RectF(-1e35f, -1e35f, 2e35f, 2e35f)), "an area over everything");
        AssertSame([3], Rect(index, new RectF(0, 0, 100, 100)), "an item over everything is found anywhere");
    }

    [TestMethod]
    public void Places_that_are_not_numbers_and_negative_sizes_are_refused()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SpatialIndex2D<int>(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SpatialIndex2D<int>(-4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SpatialIndex2D<int>(float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SpatialIndex2D<int>(float.PositiveInfinity));
        var index = new SpatialIndex2D<int>(32);
        Assert.AreEqual(32f, index.CellSize);
        var item = index.Add(1, new Vector2(1, 1));
        var results = new List<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.Add(2, new Vector2(float.NaN, 0)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.Add(2, new RectF(0, 0, -1, 5)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.Add(2, new RectF(0, float.PositiveInfinity, 1, 5)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.Move(item, new Vector2(0, float.NegativeInfinity)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.Move(item, new RectF(0, 0, 5, float.NaN)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.Query(new RectF(0, 0, -5, 5), results));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.Query(new RectF(float.NaN, 0, 5, 5), new int[4]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.QueryCircle(new Vector2(0, 0), -1, results));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.QueryCircle(new Vector2(float.NaN, 0), 1, new int[4]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index.QueryCircle(new Vector2(0, 0), float.NaN, results));

        Assert.AreEqual(1, index.Count, "a refused call changes nothing");
        Assert.AreEqual(new RectF(1, 1, 0, 0), index.BoundsOf(item));
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void An_index_of_entities_finds_them_without_a_physics_body()
    {
        var world = new SceneWorld();
        var index = new SpatialIndex2D<EntityId>(64);
        var near = world.Create();
        var far = world.Create();
        world.Local(near).Position2D = new Vector2(30, 40);
        world.Local(far).Position2D = new Vector2(900, 40);
        var nearHandle = index.Add(near, world.Local(near).Position2D);
        index.Add(far, world.Local(far).Position2D);

        Span<EntityId> found = stackalloc EntityId[4];
        Assert.AreEqual(1, index.QueryCircle(Vector2.Zero, 50, found));
        Assert.AreEqual(near, found[0]);

        world.Local(near).Position2D = new Vector2(880, 40);   // the game moves the entity and tells the index
        index.Move(nearHandle, world.Local(near).Position2D);
        Assert.AreEqual(0, index.QueryCircle(Vector2.Zero, 50, found));
        Assert.AreEqual(2, index.QueryCircle(new Vector2(890, 40), 50, found));
    }
}
