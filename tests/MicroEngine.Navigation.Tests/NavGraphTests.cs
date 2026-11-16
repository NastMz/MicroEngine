using System.Numerics;

namespace MicroEngine.Navigation.Tests;

[TestClass]
public sealed class NavGraphTests
{
    [TestMethod]
    public void Waypoints_joined_by_hand_are_crossed_by_the_shortest_way_at_the_distance_between_them()
    {
        var graph = new NavGraph();
        int a = graph.AddNode(new Vector2(0, 0)), b = graph.AddNode(new Vector2(10, 0)), c = graph.AddNode(new Vector2(10, 10)), d = graph.AddNode(new Vector2(0, 12));
        graph.Connect(a, b);
        graph.Connect(b, c);
        graph.Connect(a, d);
        graph.Connect(d, c);
        Assert.AreEqual(4, graph.NodeCount);
        Assert.AreEqual(new Vector2(0, 12), graph.PositionOf(d));
        var finder = new PathFinder();
        Assert.AreEqual(PathStatus.Found, finder.Find(graph, a, c));
        CollectionAssert.AreEqual(new[] { a, b, c }, finder.Path.ToArray());
        Assert.AreEqual(20, finder.Cost, 1e-4f);
        Assert.AreEqual(PathStatus.Found, finder.Find(graph, c, a), "connections go both ways unless told otherwise");
        Assert.AreEqual(20, finder.Cost, 1e-4f);
    }

    [TestMethod]
    public void Platforms_are_joined_by_one_way_links_whose_tag_tells_the_game_how_to_cross_them()
    {
        const int Walk = 0, Jump = 1, Drop = 2;
        var graph = new NavGraph();
        // The ground from x 0 to 100 and a ledge above its right half.
        int ground0 = graph.AddNode(new Vector2(0, 100)), ground1 = graph.AddNode(new Vector2(50, 100)), ground2 = graph.AddNode(new Vector2(100, 100));
        int ledge0 = graph.AddNode(new Vector2(50, 60)), ledge1 = graph.AddNode(new Vector2(100, 60));
        graph.Connect(ground0, ground1, tag: Walk);
        graph.Connect(ground1, ground2, tag: Walk);
        graph.Connect(ledge0, ledge1, tag: Walk);
        graph.Connect(ground1, ledge0, cost: 60, tag: Jump, bothWays: false);   // the jump reaches the ledge only from below its left end
        graph.Connect(ledge0, ground1, cost: 10, tag: Drop, bothWays: false);
        graph.Connect(ledge1, ground2, cost: 10, tag: Drop, bothWays: false);   // stepping off the right end: down only

        var finder = new PathFinder();
        Assert.AreEqual(PathStatus.Found, finder.Find(graph, ground2, ledge1));
        CollectionAssert.AreEqual(new[] { ground2, ground1, ledge0, ledge1 }, finder.Path.ToArray(), "back to the jump: the drop cannot be climbed");
        Assert.IsTrue(graph.TryGetEdge(ground1, ledge0, out var link));
        Assert.AreEqual((ledge0, 60f, Jump), (link.To, link.Cost, link.Tag));
        Assert.IsFalse(graph.TryGetEdge(ground2, ledge1, out _));

        Assert.AreEqual(PathStatus.Found, finder.Find(graph, ledge1, ground2));
        CollectionAssert.AreEqual(new[] { ledge1, ground2 }, finder.Path.ToArray());
        Assert.IsTrue(graph.TryGetEdge(ledge1, ground2, out link));
        Assert.AreEqual(Drop, link.Tag);
    }

    [TestMethod]
    public void A_connection_removed_or_a_node_disabled_changes_the_route_and_the_version()
    {
        var graph = new NavGraph();
        int a = graph.AddNode(new Vector2(0, 0)), b = graph.AddNode(new Vector2(10, 0)), c = graph.AddNode(new Vector2(20, 0)), d = graph.AddNode(new Vector2(10, 30));
        graph.Connect(a, b);
        graph.Connect(b, c);
        graph.Connect(a, d);
        graph.Connect(d, c);
        var finder = new PathFinder();
        finder.Find(graph, a, c);
        CollectionAssert.AreEqual(new[] { a, b, c }, finder.Path.ToArray());

        int version = graph.Version;
        graph.SetEnabled(b, false);
        Assert.IsFalse(graph.IsEnabled(b));
        Assert.AreNotEqual(version, graph.Version);
        Assert.AreEqual(PathStatus.Found, finder.Find(graph, a, c));
        CollectionAssert.AreEqual(new[] { a, d, c }, finder.Path.ToArray());
        graph.SetEnabled(b, true);

        version = graph.Version;
        Assert.IsTrue(graph.Disconnect(b, c));
        Assert.IsFalse(graph.Disconnect(b, c), "already gone");
        Assert.AreNotEqual(version, graph.Version);
        finder.Find(graph, a, c);
        CollectionAssert.AreEqual(new[] { a, d, c }, finder.Path.ToArray());
        graph.Disconnect(d, c);
        Assert.AreEqual(PathStatus.Unreachable, finder.Find(graph, a, c));
        Assert.AreEqual(b, finder.Path[^1], "the reachable node nearest the goal");
    }

    [TestMethod]
    public void A_link_cheaper_than_its_length_is_still_part_of_the_cheapest_path()
    {
        var finder = new PathFinder();
        for (int seed = 0; seed < 30; seed++)
        {
            var random = new Random(seed);
            var graph = new NavGraph();
            for (int i = 0; i < 30; i++) graph.AddNode(new Vector2(random.Next(200), random.Next(200)));
            for (int i = 0; i < 60; i++)
            {
                int a = random.Next(30), b = random.Next(30);
                if (a == b) continue;
                // One connection in five is a teleporter (free) or a cheap road; the rest cost their length.
                float? cost = random.Next(5) == 0 ? random.Next(3) * 0.5f * Vector2.Distance(graph.PositionOf(a), graph.PositionOf(b)) : null;
                graph.Connect(a, b, cost, bothWays: random.Next(4) != 0);
            }
            int start = random.Next(30), goal = random.Next(30);
            float expected = Maps.Reference(graph, start, goal);
            Assert.AreEqual(float.IsFinite(expected), finder.Find(graph, start, goal) == PathStatus.Found, $"seed {seed}");
            if (float.IsFinite(expected)) Assert.AreEqual(expected, finder.Cost, expected * 1e-4f + 1e-3f, $"seed {seed}");
        }
    }

    [TestMethod]
    public void The_nearest_node_to_a_point_is_an_enabled_one()
    {
        var graph = new NavGraph();
        Assert.AreEqual(-1, graph.Nearest(Vector2.Zero));
        int a = graph.AddNode(new Vector2(0, 0)), b = graph.AddNode(new Vector2(10, 0));
        Assert.AreEqual(b, graph.Nearest(new Vector2(7, 1)));
        graph.SetEnabled(b, false);
        Assert.AreEqual(a, graph.Nearest(new Vector2(7, 1)));
    }

    [TestMethod]
    public void Connecting_twice_replaces_the_connection_and_bad_connections_are_refused()
    {
        var graph = new NavGraph();
        int a = graph.AddNode(new Vector2(0, 0)), b = graph.AddNode(new Vector2(3, 4));
        graph.Connect(a, b);
        Assert.IsTrue(graph.TryGetEdge(a, b, out var edge));
        Assert.AreEqual(5f, edge.Cost);
        graph.Connect(a, b, cost: 2, tag: 7, bothWays: false);
        var edges = new List<NavEdge>();
        graph.GetEdges(a, edges);
        CollectionAssert.AreEqual(new[] { new NavEdge(b, 2, 7) }, edges);
        Assert.IsTrue(graph.TryGetEdge(b, a, out edge));
        Assert.AreEqual(5f, edge.Cost, "the other direction keeps its own cost");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => graph.Connect(a, 2));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => graph.Connect(a, b, cost: -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => graph.Connect(a, b, cost: float.NaN));
        Assert.ThrowsExactly<ArgumentException>(() => graph.Connect(a, a));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => graph.PositionOf(5));
    }
}
