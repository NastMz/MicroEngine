using MapExplorer;
using MapExplorer.Model;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class MapInteractionTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromTicks(1);
    private static InputSnapshot Jump() => new([], ["jump", "jump"]);
    private static (Zone, float, float, bool, float, float) State(MapWalker game) => (game.Zone, game.X, game.Y, game.GateOpen, game.CameraX, game.CameraY);

    [TestMethod]
    public void FixedSwitchAndOldBlockingViewsRemainImmutableAcrossOpeningAndZones()
    {
        var layout = MapLayout.For(Zone.Meadow); Assert.AreEqual(new Rect(620, 280, 40, 60), layout.Switch); Assert.IsNull(MapLayout.For(Zone.Ruins).Switch);
        var game = MapWalker.Create(Zone.Meadow, 650, 304); var oldView = game.BlockingSolids;
        Assert.IsFalse(game.GateOpen); Assert.AreEqual(1, oldView.Count);
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<Rect>)oldView)[0] = default);
        game.Update(Tick, Jump()); Assert.IsTrue(game.GateOpen); Assert.AreEqual(0, game.BlockingSolids.Count);
        Assert.AreEqual(1, oldView.Count); Assert.AreEqual(1, layout.Solids.Count);
        game.Update(TimeSpan.FromSeconds(10), new(["right"], [])); Assert.AreEqual(Zone.Ruins, game.Zone); Assert.AreEqual(1, game.BlockingSolids.Count);
        game.Update(TimeSpan.FromSeconds(1), new(["left"], [])); Assert.AreEqual(Zone.Meadow, game.Zone); Assert.AreEqual(0, game.BlockingSolids.Count);
        Assert.AreEqual(new Rect(720, 200, 32, 280), oldView[0]); Assert.IsFalse(new MapWalker().GateOpen);
    }

    [TestMethod]
    public void StrictContactHeldUnknownAndWrongZoneCannotActivate()
    {
        foreach (var (x, y) in new[] { (596f, 304f), (660f, 304f), (620f, 248f), (620f, 340f), (40f, 304f) })
        {
            var game = MapWalker.Create(Zone.Meadow, x, y); game.Update(Tick, Jump()); Assert.IsFalse(game.GateOpen);
        }
        var ruins = MapWalker.Create(Zone.Ruins, 620, 304); ruins.Update(Tick, Jump()); Assert.IsFalse(ruins.GateOpen);
        var inside = MapWalker.Create(Zone.Meadow, 650, 304);
        inside.Update(Tick, new(["jump"], ["Jump", "unknown"])); Assert.IsFalse(inside.GateOpen);
        inside.Update(Tick, Jump()); Assert.IsTrue(inside.GateOpen); inside.Update(Tick, Jump()); Assert.IsTrue(inside.GateOpen);
    }

    [TestMethod]
    public void InteractionUsesResolvedEndPositionAndDoesNotReplayMovement()
    {
        var entering = MapWalker.Create(Zone.Meadow, 580, 304); entering.Update(TimeSpan.FromSeconds(.2), new(["right"], ["jump"]));
        Assert.AreEqual(616f, entering.X); Assert.IsTrue(entering.GateOpen);
        var leaving = MapWalker.Create(Zone.Meadow, 650, 304); leaving.Update(TimeSpan.FromSeconds(1), new(["left"], ["jump"])); Assert.IsFalse(leaving.GateOpen);
        var blocked = MapWalker.Create(Zone.Meadow, 650, 304); blocked.Update(TimeSpan.FromSeconds(1), new(["right"], ["jump"]));
        Assert.AreEqual(696f, blocked.X); Assert.IsFalse(blocked.GateOpen);
        var opening = MapWalker.Create(Zone.Meadow, 650, 304); opening.Update(Tick, Jump()); Assert.AreEqual(650f, opening.X);
        opening.Update(TimeSpan.FromSeconds(.4), new(["right"], [])); Assert.AreEqual(722f, opening.X); Assert.AreEqual(0, opening.BlockingSolids.Count);
        Assert.ThrowsExactly<ArgumentException>(() => MapWalker.Create(Zone.Meadow, 722, 304)); // Factory still creates closed sessions.
    }

    [TestMethod]
    public void OpenShortcutPersistsAcrossPortalButNeverDisablesRuinsWall()
    {
        var game = MapWalker.Create(Zone.Meadow, 650, 304); game.Update(Tick, Jump());
        game.Update(TimeSpan.FromSeconds(1), new(["right"], [])); Assert.AreEqual(830f, game.X);
        game.Update(TimeSpan.FromSeconds(10), new(["right"], [])); Assert.AreEqual((Zone.Ruins, 8f, 304f, true, 0f, 140f), State(game));
        game.Update(TimeSpan.MaxValue, new(["right"], [])); Assert.AreEqual(376f, game.X); Assert.AreEqual(Zone.Ruins, game.Zone);
        game.Update(TimeSpan.MaxValue, new(["left"], ["jump"])); Assert.AreEqual((Zone.Meadow, 1248f, 304f, true, 640f, 140f), State(game));
        game.Update(TimeSpan.FromSeconds(4), new(["left"], [])); Assert.AreEqual(528f, game.X); Assert.IsTrue(game.GateOpen);
    }

    [TestMethod]
    public void InvalidUpdatesAreAtomicAndReplayIsInstanceLocal()
    {
        var first = MapWalker.Create(Zone.Meadow, 650, 304); var second = MapWalker.Create(Zone.Meadow, 650, 304);
        first.Update(Tick, Jump()); second.Update(Tick, Jump()); var before = State(first);
        Assert.ThrowsExactly<ArgumentNullException>(() => first.Update(TimeSpan.Zero, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => first.Update(TimeSpan.Zero, Jump())); Assert.AreEqual(before, State(first));
        for (int i = 0; i < 600; i++)
        {
            var input = new InputSnapshot([i < 300 ? "right" : "left"], i % 7 == 0 ? ["jump"] : []);
            first.Update(TimeSpan.FromMilliseconds(25), input); second.Update(TimeSpan.FromMilliseconds(25), input); Assert.AreEqual(State(first), State(second));
            foreach (var wall in first.BlockingSolids) Assert.IsFalse(first.X < wall.X + wall.Width && first.X + 24 > wall.X && first.Y < wall.Y + wall.Height && first.Y + 32 > wall.Y);
        }
        Assert.IsFalse(new MapWalker().GateOpen); Assert.AreEqual(1, MapLayout.For(Zone.Meadow).Solids.Count);
    }

    private class Canvas : ICanvas
    {
        public readonly List<(Rect, Rgba)> Fills = [];
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) => Fills.Add((rectangle, color));
    }
    private sealed class Labels : Canvas, ILabelCanvas
    {
        public readonly List<(string, int, int, int, Rgba)> Calls = [];
        public void Label(string text, int x, int y, int size, Rgba color) => Calls.Add((text, x, y, size, color));
    }

    [TestMethod]
    public void RenderingUsesCurrentBlockingViewClippedSwitchAndOptionalPureHud()
    {
        var game = MapWalker.Create(Zone.Meadow, 650, 304); var closed = new Labels(); var before = State(game); MapRenderer.Draw(closed, game);
        CollectionAssert.AreEqual(new (Rect, Rgba)[] { (new(58, 100, 100, 80), new(180, 100, 70)), (new(378, 60, 32, 280), new(110, 120, 140)), (new(278, 140, 40, 60), new(90, 140, 220)), (new(308, 164, 24, 32), new(240, 180, 65)) }, closed.Fills);
        CollectionAssert.AreEqual(new[] { ("Gate closed - Space at blue switch", 20, 20, 20, new Rgba(240, 240, 245)) }, closed.Calls); Assert.AreEqual(before, State(game));
        game.Update(Tick, Jump()); before = State(game); var open = new Labels(); MapRenderer.Draw(open, game);
        CollectionAssert.AreEqual(new (Rect, Rgba)[] { closed.Fills[0], (new(278, 140, 40, 60), new(80, 200, 120)), closed.Fills[3] }, open.Fills);
        CollectionAssert.AreEqual(new[] { ("Gate open", 20, 20, 20, new Rgba(240, 240, 245)) }, open.Calls);
        var primitive = new Canvas(); MapRenderer.Draw(primitive, game); CollectionAssert.AreEqual(open.Fills, primitive.Fills); Assert.AreEqual(before, State(game));
        var clipped = new Canvas(); MapRenderer.Draw(clipped, MapWalker.Create(Zone.Meadow, 948, 304)); Assert.IsTrue(clipped.Fills.Contains((new Rect(0, 140, 20, 60), new Rgba(90, 140, 220))));
        var ruins = new Labels(); MapRenderer.Draw(ruins, MapWalker.Create(Zone.Ruins, 8, 304)); Assert.AreEqual(0, ruins.Calls.Count);
    }
}
