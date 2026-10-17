using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;
using TurnExplorer.Model;

namespace Loop.Contracts;

[TestClass]
public sealed class TurnMenuTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromTicks(1);
    private static InputSnapshot Input(string[]? held = null, params string[] pressed) => new(held ?? [], pressed);
    private static (Room, float, float, bool, bool, MenuOption, int, bool) State(TurnSession g)
        => (g.Room, g.X, g.Y, g.MarkerActivated, g.MenuOpen, g.SelectedOption, g.TurnPhase, g.TurnResolved);
    private static TurnSession Open()
    {
        var g = new TurnSession(); g.Update(TimeSpan.FromSeconds(1.5), Input(["right"], "jump", "jump"));
        Assert.IsTrue(g.MenuOpen); Assert.IsFalse(g.TurnResolved); Assert.AreEqual(0, g.TurnPhase); return g;
    }

    [TestMethod]
    public void OpeningConsumesConfirmationAndPausesOwnedExploration()
    {
        var g = Open(); Assert.AreEqual(310f, g.X); Assert.IsTrue(g.MarkerActivated);
        g.Update(TimeSpan.MaxValue, Input(["right", "up", "jump"]));
        Assert.AreEqual(310f, g.X); Assert.AreEqual(164f, g.Y); Assert.AreEqual(Room.Courtyard, g.Room);
        Assert.IsTrue(g.MenuOpen); Assert.IsFalse(g.TurnResolved); Assert.AreEqual(0, g.TurnPhase);
        Assert.AreEqual(MenuOption.Advance, g.SelectedOption);
    }

    [TestMethod]
    public void OrderedNavigationAndFirstConfirmationDetermineExactlyOneOutcome()
    {
        var g = Open(); g.Update(Tick, Input(null, "jump", "down", "jump"));
        Assert.IsTrue(g.MenuOpen); Assert.AreEqual(1, g.TurnPhase); Assert.IsTrue(g.TurnResolved);
        Assert.AreEqual(MenuOption.Advance, g.SelectedOption);
        g.Update(Tick, Input(null, "up", "down", "down")); Assert.AreEqual(MenuOption.Return, g.SelectedOption);
        Assert.IsFalse(g.TurnResolved); g.Update(Tick, Input(null, "jump", "jump"));
        Assert.IsFalse(g.MenuOpen); Assert.AreEqual(1, g.TurnPhase); Assert.IsFalse(g.TurnResolved);
        g.Update(Tick, Input()); Assert.IsFalse(g.MenuOpen);
        g.Update(Tick, Input(null, "jump")); Assert.IsTrue(g.MenuOpen);
        Assert.AreEqual(MenuOption.Advance, g.SelectedOption); Assert.AreEqual(1, g.TurnPhase); Assert.IsFalse(g.TurnResolved);
        var back = Open(); back.Update(Tick, Input(null, "down", "jump"));
        Assert.IsFalse(back.MenuOpen); Assert.AreEqual(0, back.TurnPhase);
    }

    [TestMethod]
    public void ReturnConsumesMovementAndResumesOnlyNextUpdate()
    {
        var g = Open(); g.Update(TimeSpan.FromSeconds(1), Input(["right"], "down", "jump"));
        Assert.IsFalse(g.MenuOpen); Assert.AreEqual(310f, g.X);
        g.Update(TimeSpan.FromSeconds(1), Input(["right"])); Assert.AreEqual(490f, g.X);
        Assert.IsFalse(g.MenuOpen); Assert.AreEqual(0, g.TurnPhase);
    }

    [TestMethod]
    public void PhaseWrapsAndHeldCaseOrUnknownEdgesCannotResolveTurns()
    {
        var g = Open();
        for (var i = 1; i <= 5; i++)
        {
            g.Update(Tick, Input(null, "jump", "jump")); Assert.AreEqual(i % 4, g.TurnPhase); Assert.IsTrue(g.TurnResolved);
            g.Update(Tick, Input(["up", "down", "jump"], "Jump", "Up", "left", "unknown"));
            Assert.IsFalse(g.TurnResolved); Assert.AreEqual(i % 4, g.TurnPhase); Assert.AreEqual(MenuOption.Advance, g.SelectedOption);
        }
        var other = new TurnSession(); Assert.IsFalse(other.MenuOpen); Assert.IsFalse(other.MarkerActivated); Assert.AreEqual(0, other.TurnPhase);
    }

    [TestMethod]
    public void InvalidUpdatesPreserveAllStateIncludingResolvedTurn()
    {
        var g = Open(); g.Update(Tick, Input(null, "jump")); var before = State(g);
        Assert.AreEqual("input", Assert.ThrowsExactly<ArgumentNullException>(() => g.Update(TimeSpan.Zero, null!)).ParamName);
        Assert.AreEqual("delta", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => g.Update(TimeSpan.Zero, Input())).ParamName);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => g.Update(-Tick, Input())); Assert.AreEqual(before, State(g));
    }

    [TestMethod]
    public void ClosedExplorationMatchesPublicModelAndReplayIsIndependent()
    {
        var g = new TurnSession(); var explorer = new Explorer(); var step = TimeSpan.FromSeconds(1d / 60);
        for (var i = 0; i < 1000; i++)
        {
            var input = i % 2 == 0 ? Input(["right", "up"]) : Input(["down", "left"]);
            explorer.Update(step, input); g.Update(step, input);
            Assert.AreEqual((explorer.Room, explorer.X, explorer.Y), (g.Room, g.X, g.Y)); Assert.IsFalse(g.MenuOpen);
        }
        var a = Open(); var b = Open();
        foreach (var edge in new[] { "down", "up", "jump", "down", "jump", "jump", "up", "jump" })
        {
            a.Update(step, Input(null, edge)); b.Update(step, Input(null, edge)); Assert.AreEqual(State(a), State(b));
        }
    }
}
