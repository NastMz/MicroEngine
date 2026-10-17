using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;

namespace Loop.Contracts;

[TestClass]
public sealed class MarkerInteractionTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromTicks(1);
    private static InputSnapshot Input(string[]? held = null, string[]? pressed = null) => new(held ?? [], pressed ?? []);
    private static (Room, float, float, bool, bool) State(Explorer g)
        => (g.Room, g.X, g.Y, g.MarkerActivated, g.InteractionOccurred);
    private static Explorer AtMarker()
    {
        var g = new Explorer(); g.Update(TimeSpan.FromSeconds(1.5), Input(["right"])); return g;
    }

    [TestMethod]
    public void EdgesToggleOnceAndOccurrenceLastsOnlyUntilNextValidUpdate()
    {
        var g = AtMarker(); Assert.IsFalse(g.MarkerActivated); Assert.IsFalse(g.InteractionOccurred);
        g.Update(Tick, Input(pressed: ["jump", "jump"]));
        Assert.IsTrue(g.MarkerActivated); Assert.IsTrue(g.InteractionOccurred);
        g.Update(Tick, Input(["jump"])); Assert.IsTrue(g.MarkerActivated); Assert.IsFalse(g.InteractionOccurred);
        g.Update(Tick, Input(pressed: ["Jump", "unknown"])); Assert.IsTrue(g.MarkerActivated); Assert.IsFalse(g.InteractionOccurred);
        g.Update(Tick, Input(pressed: ["jump"])); Assert.IsFalse(g.MarkerActivated); Assert.IsTrue(g.InteractionOccurred);
        var other = AtMarker(); Assert.IsFalse(other.MarkerActivated); Assert.IsFalse(other.InteractionOccurred);
    }

    [TestMethod]
    public void MovementResolvesBeforeInteractionAndOutsideEdgesNeverBuffer()
    {
        var g = new Explorer(); g.Update(Tick, Input(pressed: ["jump"]));
        g.Update(TimeSpan.FromSeconds(1.5), Input(["right"])); Assert.IsFalse(g.MarkerActivated);
        g.Update(Tick, Input(pressed: ["jump"])); Assert.IsTrue(g.MarkerActivated);
        var moved = new Explorer(); moved.Update(TimeSpan.FromSeconds(1.5), Input(["right"], ["jump"]));
        Assert.IsTrue(moved.MarkerActivated); Assert.IsTrue(moved.InteractionOccurred);
        var crossed = new Explorer(); crossed.Update(TimeSpan.FromSeconds(2.5), Input(["right"], ["jump"]));
        Assert.IsFalse(crossed.MarkerActivated); Assert.IsFalse(crossed.InteractionOccurred);
    }

    [TestMethod]
    public void RoomTransitionsDoNotInteractAndKeepOnlyInstanceState()
    {
        var g = AtMarker(); g.Update(Tick, Input(pressed: ["jump"]));
        g.Update(TimeSpan.MaxValue, Input(["right"], ["jump"]));
        Assert.AreEqual(Room.Hall, g.Room); Assert.IsTrue(g.MarkerActivated); Assert.IsFalse(g.InteractionOccurred);
        g.Update(TimeSpan.MaxValue, Input(["left"], ["jump"]));
        Assert.AreEqual(Room.Courtyard, g.Room); Assert.IsTrue(g.MarkerActivated); Assert.IsFalse(g.InteractionOccurred);
        g.Update(TimeSpan.FromSeconds(1.65), Input(["left"])); g.Update(Tick, Input(pressed: ["jump"]));
        Assert.IsFalse(g.MarkerActivated); Assert.IsTrue(g.InteractionOccurred);
        var wrongRoom = new Explorer(); wrongRoom.Update(TimeSpan.MaxValue, Input(["right"]));
        wrongRoom.Update(TimeSpan.FromSeconds(1.7), Input(["right"])); wrongRoom.Update(Tick, Input(pressed: ["jump"]));
        Assert.AreEqual(Room.Hall, wrongRoom.Room); Assert.IsFalse(wrongRoom.MarkerActivated); Assert.IsFalse(wrongRoom.InteractionOccurred);
    }

    private static Explorer At(float x, float y)
    {
        foreach (var originX in new[] { 0f, 616f })
        foreach (var originY in new[] { 0f, 328f })
        {
            var tx = TimeSpan.FromSeconds(Math.Abs(x - originX) / 180d).Ticks;
            var ty = TimeSpan.FromSeconds(Math.Abs(y - originY) / 180d).Ticks;
            for (var i = -8; i <= 8; i++)
            for (var j = -8; j <= 8; j++)
            {
                var g = new Explorer(); g.Update(TimeSpan.MaxValue, Input(["up"]));
                g.Update(TimeSpan.MaxValue, Input([originX == 0 ? "left" : "right"]));
                if (originY == 328) g.Update(TimeSpan.MaxValue, Input(["down"]));
                g.Update(TimeSpan.FromTicks(tx + i), Input([x > originX ? "right" : "left"]));
                g.Update(TimeSpan.FromTicks(ty + j), Input([y > originY ? "down" : "up"]));
                if (g.X == x && g.Y == y) return g;
            }
        }
        throw new AssertFailedException("No exact public geometry fixture found.");
    }

    [TestMethod]
    [DataRow(276f, 164f, false)]
    [DataRow(277f, 164f, true)]
    [DataRow(340f, 164f, false)]
    [DataRow(339f, 164f, true)]
    [DataRow(310f, 118f, false)]
    [DataRow(310f, 119f, true)]
    [DataRow(310f, 210f, false)]
    [DataRow(310f, 209f, true)]
    [DataRow(276f, 118f, false)]
    public void OnlyStrictInteriorOverlapInteracts(float x, float y, bool expected)
    {
        var g = At(x, y); g.Update(Tick, Input(pressed: ["jump"]));
        Assert.AreEqual(expected, g.MarkerActivated); Assert.AreEqual(expected, g.InteractionOccurred);
    }

    [TestMethod]
    public void RejectedUpdatesPreservePriorOccurrenceAndActivation()
    {
        var g = AtMarker(); g.Update(Tick, Input(pressed: ["jump"])); var before = State(g);
        Assert.AreEqual("input", Assert.ThrowsExactly<ArgumentNullException>(() => g.Update(TimeSpan.Zero, null!)).ParamName);
        Assert.AreEqual("delta", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => g.Update(TimeSpan.Zero, Input())).ParamName);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => g.Update(-Tick, Input())); Assert.AreEqual(before, State(g));
        g.Update(Tick, Input()); Assert.IsFalse(g.InteractionOccurred); Assert.IsTrue(g.MarkerActivated);
    }
}
