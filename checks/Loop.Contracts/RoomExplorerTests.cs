using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;

namespace Loop.Contracts;

[TestClass]
public sealed class RoomExplorerTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);
    private static InputSnapshot Held(params string[] actions) => new(actions, []);
    private static (Room, float, float) State(Explorer g) => (g.Room, g.X, g.Y);

    [TestMethod]
    public void CardinalDiagonalCancellationAndIgnoredInput()
    {
        var g = new Explorer(); Assert.AreEqual((Room.Courtyard, 40f, 164f), State(g));
        g.Update(Second, Held("right")); Assert.AreEqual((Room.Courtyard, 220f, 164f), State(g));
        var d = new Explorer(); d.Update(Second, Held("right", "up"));
        Assert.AreEqual(180f, MathF.Sqrt(MathF.Pow(d.X - 40, 2) + MathF.Pow(d.Y - 164, 2)), .001f);
        var before = State(g); g.Update(Second, Held("left", "right", "up", "down", "jump"));
        Assert.AreEqual(before, State(g)); g.Update(Second, new InputSnapshot([], ["right", "jump"]));
        Assert.AreEqual(before, State(g)); g.Update(Second, Held("RIGHT", "unknown")); Assert.AreEqual(before, State(g));
    }

    [TestMethod]
    public void BoundsAndInvalidUpdatesAreAtomic()
    {
        var g = new Explorer(); g.Update(TimeSpan.MaxValue, Held("up", "left"));
        Assert.AreEqual((Room.Courtyard, 0f, 0f), State(g));
        g.Update(TimeSpan.MaxValue, Held("down", "right")); Assert.AreEqual((Room.Courtyard, 616f, 328f), State(g));
        var before = State(g);
        Assert.AreEqual("input", Assert.ThrowsExactly<ArgumentNullException>(() => g.Update(TimeSpan.Zero, null!)).ParamName);
        Assert.AreEqual("delta", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => g.Update(TimeSpan.Zero, Held("left"))).ParamName);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => g.Update(-Second, Held("up")));
        Assert.AreEqual(before, State(g));
    }

    [TestMethod]
    public void TransitionsDiscardOvershootAndDoNotRetriggerAtRest()
    {
        var g = new Explorer(); g.Update(TimeSpan.MaxValue, Held("right"));
        Assert.AreEqual((Room.Hall, 8f, 164f), State(g)); g.Update(Second, Held());
        Assert.AreEqual((Room.Hall, 8f, 164f), State(g));
        g.Update(TimeSpan.MaxValue, Held("left")); Assert.AreEqual((Room.Courtyard, 608f, 164f), State(g));
        g.Update(Second, Held()); Assert.AreEqual((Room.Courtyard, 608f, 164f), State(g));
        g.Update(TimeSpan.FromSeconds(.1), Held("right", "down"));
        Assert.AreEqual((Room.Hall, 8f, 164f), State(g));
    }

    private static Explorer AtY(float y, bool hall)
    {
        foreach (var origin in new[] { 164f, 0f, 328f })
        {
            var ticks = TimeSpan.FromSeconds(Math.Abs(y - origin) / 180d).Ticks;
            for (var offset = -8; offset <= 8; offset++)
            {
                var g = new Explorer();
                if (hall) g.Update(TimeSpan.MaxValue, Held("right"));
                if (origin != 164) g.Update(TimeSpan.MaxValue, Held(origin == 0 ? "up" : "down"));
                g.Update(TimeSpan.FromTicks(ticks + offset), Held(y < origin ? "up" : "down"));
                if (g.Y == y) return g;
            }
        }
        throw new AssertFailedException("No exact public boundary fixture found.");
    }

    [TestMethod]
    [DataRow(120f, true)]
    [DataRow(208f, true)]
    [DataRow(119f, false)]
    [DataRow(209f, false)]
    public void PassageRequiresWholeBodyInsideInclusiveOpening(float y, bool allowed)
    {
        foreach (var hall in new[] { false, true })
        {
            var g = AtY(y, hall); g.Update(TimeSpan.FromSeconds(4), Held(hall ? "left" : "right"));
            var original = hall ? Room.Hall : Room.Courtyard;
            var destination = hall ? Room.Courtyard : Room.Hall;
            Assert.AreEqual(allowed ? destination : original, g.Room);
            if (!allowed) { Assert.AreEqual(hall ? 0f : 616f, g.X); Assert.AreEqual(y, g.Y); }
        }
    }

    [TestMethod]
    public void ExactEdgeNeedsOutwardAttemptAndReplayIsIndependent()
    {
        var g = new Explorer(); g.Update(TimeSpan.MaxValue, Held("right"));
        g.Update(Second, Held("up")); g.Update(Second, Held("left"));
        Assert.AreEqual(Room.Hall, g.Room); Assert.AreEqual(0f, g.X);
        g.Update(Second, Held("down")); Assert.AreEqual(Room.Hall, g.Room); Assert.AreEqual(180f, g.Y);
        g.Update(Second, Held("left")); Assert.AreEqual(Room.Courtyard, g.Room);
        var a = new Explorer(); var b = new Explorer(); var step = TimeSpan.FromSeconds(1d / 60);
        for (var i = 0; i < 1000; i++)
        {
            var input = i % 3 == 0 ? Held("right") : i % 3 == 1 ? Held("up", "left") : Held("down");
            a.Update(step, input); b.Update(step, input); Assert.AreEqual(State(a), State(b));
        }
        a.Update(Second, Held("up")); Assert.AreNotEqual(State(a), State(b));
    }
}
