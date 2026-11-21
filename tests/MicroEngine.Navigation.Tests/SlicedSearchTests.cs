namespace MicroEngine.Navigation.Tests;

/// <summary>A search spread over several steps gives what one call gives.</summary>
[TestClass]
public sealed class SlicedSearchTests
{
    [TestMethod]
    public void A_search_continued_a_few_nodes_at_a_time_gives_the_same_path_as_one_call()
    {
        var whole = new PathFinder();
        var sliced = new PathFinder();
        int unreachable = 0;
        for (int seed = 0; seed < 30; seed++)
        {
            var grid = Maps.Random(seed, 30, 20, 0.25, costs: seed % 2 == 0);
            var random = new Random(seed);
            for (int i = 0; i < 6; i++)
            {
                int start = random.Next(grid.NodeCount), goal = random.Next(grid.NodeCount);
                var expected = whole.Find(grid, start, goal);
                if (expected == PathStatus.Unreachable) unreachable++;

                sliced.Begin(grid, start, goal);
                Assert.IsTrue(sliced.IsSearching);
                Assert.IsEmpty(sliced.Path, "nothing is known before the search ends");
                int calls = 1;
                PathStatus status;
                while ((status = sliced.Continue(7)) == PathStatus.Searching)
                {
                    Assert.IsTrue(sliced.IsSearching);
                    Assert.IsEmpty(sliced.Path);
                    Assert.AreEqual(calls * 7, sliced.Expanded, "each call looks at as many nodes as it was given");
                    calls++;
                }
                Assert.IsFalse(sliced.IsSearching);
                Assert.AreEqual(expected, status, $"seed {seed}, {start} to {goal}");
                CollectionAssert.AreEqual(whole.Path.ToArray(), sliced.Path.ToArray(), $"seed {seed}, {start} to {goal}");
                Assert.AreEqual(whole.Cost, sliced.Cost);
                Assert.AreEqual(whole.Expanded, sliced.Expanded);
                Assert.AreEqual(Math.Max(1, (whole.Expanded + 6) / 7), calls, 1);
            }
        }
        Assert.IsGreaterThan(5, unreachable, "searches that fail are sliced too");
    }

    [TestMethod]
    public void The_limit_of_a_search_counts_all_its_slices()
    {
        var grid = Maps.Grid(
            "..........",
            ".########.",
            ".#......#.",
            ".#..G...#.",
            ".########.",
            "S.........");
        int start = grid.NodeOf(0, 5), goal = grid.NodeOf(4, 3);
        var finder = new PathFinder { MaxExpansions = 12 };
        finder.Begin(grid, start, goal);
        Assert.AreEqual(PathStatus.Searching, finder.Continue(5));
        Assert.AreEqual(PathStatus.Searching, finder.Continue(5));
        Assert.AreEqual(PathStatus.Incomplete, finder.Continue(5));
        Assert.AreEqual(12, finder.Expanded);
        Assert.IsGreaterThan(0, finder.Path.Count, "the path leads to the nearest node found so far");
        Assert.AreEqual(start, finder.Path[0]);
    }

    [TestMethod]
    public void A_search_is_begun_before_it_is_continued_and_a_new_one_replaces_it()
    {
        var grid = Maps.Grid("....", "....", "....");
        var finder = new PathFinder();
        Assert.IsFalse(finder.IsSearching);
        Assert.ThrowsExactly<InvalidOperationException>(() => finder.Continue(10));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => finder.Begin(grid, 0, 99));

        finder.Begin(grid, 0, 11);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => finder.Continue(0));
        Assert.AreEqual(PathStatus.Searching, finder.Continue(1));
        Assert.AreEqual(PathStatus.Found, finder.Find(grid, 3, 8), "a whole search takes the finder");
        Assert.IsFalse(finder.IsSearching, "and the one in progress is gone");
        Assert.AreEqual(3, finder.Path[0]);
        Assert.ThrowsExactly<InvalidOperationException>(() => finder.Continue(10));

        finder.Begin(grid, 0, 0);
        Assert.AreEqual(PathStatus.Found, finder.Continue(1), "a search that needs no node ends in its first slice");
        CollectionAssert.AreEqual(new[] { 0 }, finder.Path.ToArray());
    }
}
