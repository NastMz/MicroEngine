using System.Numerics;

namespace MicroEngine.Navigation.Tests;

/// <summary>Reachability: which cells are joined, the nearest cell that can be reached, and whether a wall would cut a way.</summary>
[TestClass]
public sealed class NavRegionsTests
{
    private static readonly string[] Rooms =
    [
        "###########",
        "#A..#....B#",
        "#...#.....#",
        "#...D.....#",
        "#...#.....#",
        "#####.....#",
        "#C..#.....#",
        "#...#.....#",
        "###########",
    ];

    [TestMethod]
    public void Regions_tell_which_cells_are_joined_and_follow_the_map()
    {
        var grid = Maps.Grid(Rooms);
        int a = Maps.Node(grid, Rooms, 'A'), b = Maps.Node(grid, Rooms, 'B'), c = Maps.Node(grid, Rooms, 'C'), door = Maps.Node(grid, Rooms, 'D');
        var regions = new NavRegions(grid);

        Assert.AreEqual(2, regions.Count, "the two rooms joined by the door, and the closet");
        Assert.AreEqual(0, regions.RegionOf(a), "regions are numbered from the first cell of the grid");
        Assert.AreEqual(1, regions.RegionOf(c));
        Assert.AreEqual(-1, regions.RegionOf(grid.NodeOf(0, 0)), "a wall is in no region");
        Assert.IsTrue(regions.Connected(a, b));
        Assert.IsFalse(regions.Connected(a, c));
        Assert.IsFalse(regions.Connected(a, grid.NodeOf(0, 0)));
        Assert.AreEqual(6, regions.SizeOf(1));
        Assert.AreEqual(12 + 1 + 35, regions.SizeOf(0));

        grid.SetWalkable(4, 3, false);
        Assert.AreEqual(3, regions.Count, "the door closed: no call was needed to know");
        Assert.IsFalse(regions.Connected(a, b));
        Assert.AreEqual(1, regions.RegionOf(door), "somebody caught in the closed door walks out into the first room beside it");
        Assert.AreEqual(1, regions.RegionOf(b));

        grid.SetWalkable(2, 5, true);
        grid.SetWalkable(4, 3, true);
        Assert.AreEqual(1, regions.Count);
        Assert.IsTrue(regions.Connected(a, c));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => regions.RegionOf(grid.NodeCount));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => regions.SizeOf(1));
    }

    [TestMethod]
    public void A_gap_joins_two_rooms_only_for_agents_that_fit_through_it()
    {
        var grid = Maps.Grid(
            "###########",
            "#....#....#",
            "#....#....#",
            "#.........#",
            "#....#....#",
            "#....#....#",
            "###########");
        int left = grid.NodeOf(2, 3), right = grid.NodeOf(7, 3);
        Assert.IsTrue(new NavRegions(grid).Connected(left, right));
        var wide = new NavRegions(grid, radius: 1.5f);
        Assert.AreEqual(1.5f, wide.Radius);
        Assert.IsFalse(wide.Connected(left, right), "three cells across do not pass a gap of one");
        Assert.AreEqual(2, wide.Count);
        Assert.AreEqual(wide.RegionOf(left), wide.RegionOf(grid.NodeOf(1, 1)), "pressed into a corner, the body still belongs to the room it can walk into");
    }

    [TestMethod]
    public void The_nearest_reachable_cell_is_the_closest_in_a_straight_line_and_can_be_walked_to()
    {
        var grid = Maps.Grid(Rooms);
        int a = Maps.Node(grid, Rooms, 'A'), b = Maps.Node(grid, Rooms, 'B'), c = Maps.Node(grid, Rooms, 'C');
        var regions = new NavRegions(grid);
        var finder = new PathFinder();

        Assert.AreEqual(b, regions.NearestReachable(a, b), "what can be reached is its own answer");
        int near = regions.NearestReachable(a, c);
        Assert.AreEqual(grid.NodeOf(1, 4), near, "the cell of the first room just above the closet's corner");
        Assert.AreEqual(PathStatus.Found, finder.Find(grid, a, near));
        Assert.AreEqual(grid.NodeOf(1, 4), regions.NearestReachable(a, grid.NodeOf(0, 5)), "a wall is asked for: the walkable cell beside it");
        Assert.AreEqual(-1, regions.NearestReachable(grid.NodeOf(0, 0), b), "from inside a wall with no way out nothing is reached");

        // Against every cell of the region, on random maps.
        for (int seed = 0; seed < 30; seed++)
        {
            var map = Maps.Random(seed, 20, 14, 0.3, costs: false);
            var random = new Random(seed);
            var parts = new NavRegions(map);
            for (int i = 0; i < 20; i++)
            {
                int from = random.Next(map.NodeCount), to = random.Next(map.NodeCount);
                int region = parts.RegionOf(from);
                int best = -1;
                float bestDistance = float.PositiveInfinity;
                for (int node = 0; node < map.NodeCount && region >= 0; node++)
                {
                    if (!map.IsWalkable(map.CellOf(node).X, map.CellOf(node).Y) || parts.RegionOf(node) != region) continue;
                    float distance = Vector2.DistanceSquared(map.CenterOf(node), map.CenterOf(to));
                    if (distance < bestDistance) { bestDistance = distance; best = node; }
                }
                Assert.AreEqual(best, parts.NearestReachable(from, to), $"seed {seed}, {from} to {to}");
            }
        }
    }

    [TestMethod]
    public void A_wall_is_refused_when_it_would_cut_the_way_and_asking_changes_nothing()
    {
        var grid = Maps.Grid(
            "#########",
            "#S......#",
            "#.#####.#",
            "#.#...#.#",
            "#...#..E#",
            "#########");
        int spawn = grid.NodeOf(1, 1), exit = grid.NodeOf(7, 4);
        var regions = new NavRegions(grid);
        int version = grid.Version;

        Assert.IsTrue(regions.StaysConnected(new RectF(4, 1, 1, 1), spawn, exit), "the top way closes: the bottom one remains");
        Assert.IsTrue(regions.StaysConnected(new RectF(3, 3, 1, 1), spawn, exit), "the bottom way closes: the top one remains");
        Assert.IsFalse(regions.StaysConnected(new RectF(1, 1, 1, 1), spawn, exit), "the spawn's own cell cannot be built on");
        Assert.IsFalse(regions.StaysConnected(new RectF(3, 1, 1, 3), spawn, exit), "a wall across both ways");
        Assert.IsFalse(regions.StaysConnected(new RectF(7, 4, 1, 1), spawn, exit), "the exit itself");
        Assert.IsTrue(regions.StaysConnected(new RectF(-5, -5, 2, 2), spawn, exit), "an area outside the grid closes nothing");
        Assert.AreEqual(version, grid.Version, "asking builds nothing");
        Assert.IsTrue(grid.IsWalkable(3, 1));

        Assert.IsFalse(regions.StaysConnected(new RectF(4, 1, 1, 1), [spawn, grid.NodeOf(0, 0)]), "a wall was never joined to anything");
        Assert.IsTrue(regions.StaysConnected(new RectF(4, 1, 1, 1), [spawn]), "one place alone is joined to itself");

        // Three places must stay joined: the two ends and a creep in a dead end.
        var pocket = Maps.Grid(
            "#######",
            "#S...E#",
            "###.###",
            "###C###",
            "#######");
        var parts = new NavRegions(pocket);
        int from = pocket.NodeOf(1, 1), to = pocket.NodeOf(5, 1), creep = pocket.NodeOf(3, 3);
        Assert.IsTrue(parts.StaysConnected(new RectF(3, 2, 1, 1), from, to), "the two ends do not need the dead end");
        Assert.IsFalse(parts.StaysConnected(new RectF(3, 2, 1, 1), [from, to, creep]), "but the creep would be shut in");
        Assert.IsTrue(parts.StaysConnected(new RectF(1, 2, 1, 1), [from, to, creep]), "a wall where there was one already");
    }

    [TestMethod]
    public void Asking_whether_a_wall_cuts_the_way_agrees_with_building_it()
    {
        var finder = new PathFinder();
        int cuts = 0, keeps = 0;
        for (int seed = 0; seed < 60; seed++)
        {
            float radius = seed % 3 == 0 ? 1.5f : 0;
            var grid = Maps.Random(seed, 16, 12, radius > 0 ? 0.04 : 0.25, costs: false);
            var random = new Random(seed);
            var regions = new NavRegions(grid, radius);
            for (int i = 0; i < 15; i++)
            {
                int a = random.Next(grid.NodeCount), b = random.Next(grid.NodeCount);
                var (ax, ay) = (grid.CellOf(a).X, grid.CellOf(a).Y);
                var (bx, by) = (grid.CellOf(b).X, grid.CellOf(b).Y);
                if (!grid.Fits(ax, ay, radius) || !grid.Fits(bx, by, radius)) continue;
                var area = new RectF(random.Next(16), random.Next(12), 1 + random.Next(3), 1 + random.Next(3));
                bool asked = regions.StaysConnected(area, a, b);

                var built = new NavGrid(16, 12);
                for (int node = 0; node < grid.NodeCount; node++) built.SetWalkable(node % 16, node / 16, grid.IsWalkable(node % 16, node / 16));
                built.SetBlocked(area, true);
                bool joined = built.Fits(ax, ay, radius) && built.Fits(bx, by, radius) && finder.Find(built.ForAgent(radius), a, b) == PathStatus.Found;
                Assert.AreEqual(joined, asked, $"seed {seed}: {area} between {a} and {b}, radius {radius}");
                if (joined) keeps++; else cuts++;
            }
        }
        Assert.IsGreaterThan(50, cuts);
        Assert.IsGreaterThan(50, keeps);
    }
}
