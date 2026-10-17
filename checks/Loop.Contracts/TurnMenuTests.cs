using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;
using TurnExplorer.Model;
using TurnExplorer;

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

[TestClass]
public sealed class TurnMenuPresentationTests
{
    private sealed class Canvas : ICanvas
    {
        public List<(Rect, Rgba)> Calls { get; } = [];
        public void Clear(Rgba color) => Assert.Fail("Menu renderer must not clear the world.");
        public void Fill(Rect rectangle, Rgba color) => Calls.Add((rectangle, color));
    }

    private static (Room, float, float, bool, MenuOption, int, bool) State(TurnSession g)
        => (g.Room, g.X, g.Y, g.MenuOpen, g.SelectedOption, g.TurnPhase, g.TurnResolved);

    [TestMethod]
    public void OverlayAppearsOnlyWhileOpenAndDrawsExactPureContent()
    {
        var g = new TurnSession(); var canvas = new Canvas();
        MenuRenderer.Draw(canvas, g); Assert.AreEqual(4, canvas.Calls.Count);
        var cyan = new Rgba(70, 200, 210); var gray = new Rgba(70, 75, 85);
        var gold = new Rgba(240, 180, 65); var dark = new Rgba(24, 28, 40);
        for (var i = 0; i < 4; i++) Assert.AreEqual((new Rect(20 + 20 * i, 20, 12, 12), i == 0 ? cyan : gray), canvas.Calls[i]);
        g.Update(TimeSpan.FromSeconds(1.5), new InputSnapshot(["right"], ["jump"]));
        canvas.Calls.Clear(); var before = State(g); MenuRenderer.Draw(canvas, g);
        Assert.AreEqual(before, State(g)); Assert.AreEqual(9, canvas.Calls.Count);
        var expected = new[] { (new Rect(420,40,200,40),gold), (new Rect(420,88,200,40),gray),
            (new Rect(432,54,12,12),dark), (new Rect(432,102,12,12),dark), (new Rect(452,102,12,12),dark) };
        for (var i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], canvas.Calls[4 + i]);
        var tick = TimeSpan.FromTicks(1);
        g.Update(tick, new InputSnapshot([], ["jump"])); before = State(g);
        canvas.Calls.Clear(); MenuRenderer.Draw(canvas, g); MenuRenderer.Draw(canvas, g);
        Assert.AreEqual(before, State(g)); Assert.AreEqual(18, canvas.Calls.Count);
        Assert.AreEqual(gray, canvas.Calls[0].Item2); Assert.AreEqual(cyan, canvas.Calls[1].Item2);
        for (var i = 0; i < 9; i++) Assert.AreEqual(canvas.Calls[i], canvas.Calls[9 + i]);
        g.Update(tick, new InputSnapshot([], ["down"])); canvas.Calls.Clear(); MenuRenderer.Draw(canvas, g);
        Assert.AreEqual(gray, canvas.Calls[4].Item2); Assert.AreEqual(gold, canvas.Calls[5].Item2);
        g.Update(tick, new InputSnapshot([], ["jump"])); canvas.Calls.Clear(); MenuRenderer.Draw(canvas, g);
        Assert.AreEqual(4, canvas.Calls.Count); Assert.IsTrue(canvas.Calls.All(call => call.Item1.Y == 20));
    }

    [TestMethod]
    public void NullValidationPrecedesAllDrawing()
    {
        var canvas = new Canvas();
        Assert.AreEqual("canvas", Assert.ThrowsExactly<ArgumentNullException>(() => MenuRenderer.Draw(null!, null!)).ParamName);
        Assert.AreEqual("session", Assert.ThrowsExactly<ArgumentNullException>(() => MenuRenderer.Draw(canvas, null!)).ParamName);
        Assert.AreEqual(0, canvas.Calls.Count);
    }
}
