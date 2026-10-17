using MapExplorer;
using MapExplorer.Model;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class MapNavigationTests
{
    private static InputSnapshot Input(params string[] actions) => new(actions, []);
    private static (Zone, float, float, float, float) State(MapWalker game) => (game.Zone, game.X, game.Y, game.CameraX, game.CameraY);
    private static void NoOverlap(MapWalker game)
    {
        foreach (var wall in MapLayout.For(game.Zone).Solids)
            Assert.IsFalse(game.X < wall.X + wall.Width && game.X + 24 > wall.X && game.Y < wall.Y + wall.Height && game.Y + 32 > wall.Y);
    }

    [TestMethod]
    public void ExplicitSolidsAreImmutableAndCreateRejectsOverlapButAllowsContact()
    {
        foreach (var (zone, wall) in new[] { (Zone.Meadow, new Rect(720, 200, 32, 280)), (Zone.Ruins, new Rect(400, 200, 32, 200)) })
        {
            var map = MapLayout.For(zone); Assert.AreEqual(1, map.Solids.Count); Assert.AreEqual(wall, map.Solids[0]);
            Assert.ThrowsExactly<NotSupportedException>(() => ((IList<Rect>)map.Solids)[0] = default);
            Assert.ThrowsExactly<ArgumentException>(() => MapWalker.Create(zone, wall.X, wall.Y));
            Assert.ThrowsExactly<ArgumentException>(() => MapWalker.Create(zone, wall.X - 23, wall.Y - 31));
            NoOverlap(MapWalker.Create(zone, wall.X - 24, wall.Y)); NoOverlap(MapWalker.Create(zone, wall.X + wall.Width, wall.Y));
            NoOverlap(MapWalker.Create(zone, wall.X, wall.Y - 32)); NoOverlap(MapWalker.Create(zone, wall.X, wall.Y + wall.Height));
        }
        Assert.AreEqual(3, MapLayout.For(Zone.Meadow).Landmarks.Count);
    }

    [TestMethod]
    public void FourFacesBlockSweptDisplacementsWithoutTunneling()
    {
        foreach (var duration in new[] { TimeSpan.FromSeconds(1), TimeSpan.MaxValue })
        foreach (var (x, y, action, expectedX, expectedY) in new[]
        {
            (600f, 250f, "right", 696f, 250f), (800f, 250f, "left", 752f, 250f),
            (725f, 100f, "down", 725f, 168f), (725f, 500f, "up", 725f, 480f)
        })
        {
            var game = MapWalker.Create(Zone.Meadow, x, y); game.Update(duration, Input(action));
            Assert.AreEqual(expectedX, game.X); Assert.AreEqual(expectedY, game.Y); NoOverlap(game);
        }
    }

    [TestMethod]
    public void WallSlidingAndCornerResolutionUseTheSpecifiedYThenXPath()
    {
        var slide = MapWalker.Create(Zone.Meadow, 696, 250); slide.Update(TimeSpan.FromSeconds(1), Input("right", "down"));
        Assert.AreEqual(696f, slide.X); Assert.AreEqual(250 + 180 / MathF.Sqrt(2), slide.Y, .001f); NoOverlap(slide);
        var corner = MapWalker.Create(Zone.Meadow, 700, 150); corner.Update(TimeSpan.FromSeconds(1), Input("right", "down"));
        Assert.AreEqual(168f, corner.Y); Assert.AreEqual(700 + 180 / MathF.Sqrt(2), corner.X, .001f); NoOverlap(corner);
        var edge = MapWalker.Create(Zone.Meadow, 696, 168); edge.Update(TimeSpan.FromSeconds(1), Input("right"));
        Assert.AreEqual(876f, edge.X); Assert.AreEqual(168f, edge.Y); NoOverlap(edge);
        var away = MapWalker.Create(Zone.Meadow, 696, 304); away.Update(TimeSpan.FromSeconds(1), Input("left")); Assert.AreEqual(516f, away.X);
    }

    [TestMethod]
    public void DetourReachesPortalAndReturnPreservesCameraAndSpawn()
    {
        var game = new MapWalker(); game.Update(TimeSpan.FromSeconds(10), Input("right"));
        Assert.AreEqual((Zone.Meadow, 696f, 304f, 388f, 140f), State(game)); NoOverlap(game);
        game.Update(TimeSpan.FromSeconds(1), Input("up")); Assert.AreEqual(124f, game.Y);
        game.Update(TimeSpan.FromSeconds(1), Input("right")); Assert.AreEqual(876f, game.X); NoOverlap(game);
        game.Update(TimeSpan.FromSeconds(1), Input("down")); Assert.AreEqual(304f, game.Y);
        game.Update(TimeSpan.FromSeconds(10), Input("right")); Assert.AreEqual((Zone.Ruins, 8f, 304f, 0f, 140f), State(game)); NoOverlap(game);
        game.Update(TimeSpan.FromSeconds(1), Input("left")); Assert.AreEqual((Zone.Meadow, 1248f, 304f, 640f, 140f), State(game)); NoOverlap(game);
    }

    [TestMethod]
    public void RuinsBarrierAndFreeDecorativeLandmarksHaveDifferentSemantics()
    {
        var game = MapWalker.Create(Zone.Ruins, 300, 250); game.Update(TimeSpan.MaxValue, Input("right"));
        Assert.AreEqual(376f, game.X); Assert.AreEqual(Zone.Ruins, game.Zone); NoOverlap(game);
        var decoration = MapWalker.Create(Zone.Meadow, 350, 250); decoration.Update(TimeSpan.FromSeconds(1), Input("right"));
        Assert.AreEqual(530f, decoration.X); NoOverlap(decoration);
    }

    [TestMethod]
    public void InvalidUpdatesStayAtomicAndReplayNeverPenetrates()
    {
        var first = new MapWalker(); var second = new MapWalker(); var untouched = new MapWalker();
        var before = State(first); Assert.ThrowsExactly<ArgumentNullException>(() => first.Update(TimeSpan.FromSeconds(1), null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => first.Update(TimeSpan.Zero, Input("right"))); Assert.AreEqual(before, State(first));
        for (int i = 0; i < 1000; i++)
        {
            var input = Input((i / 80) % 2 == 0 ? "right" : "left", (i / 120) % 2 == 0 ? "down" : "up");
            first.Update(TimeSpan.FromMilliseconds(50), input); second.Update(TimeSpan.FromMilliseconds(50), input);
            Assert.AreEqual(State(first), State(second)); NoOverlap(first);
        }
        Assert.AreEqual(before, State(untouched));
    }

    private sealed class Canvas : ICanvas
    {
        public readonly List<(Rect, Rgba)> Fills = [];
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) => Fills.Add((rectangle, color));
    }

    [TestMethod]
    public void SolidRenderingSharesCameraClippingAndPrecedesPlayerWithoutMutation()
    {
        var game = MapWalker.Create(Zone.Meadow, 508, 688); var before = State(game); var canvas = new Canvas();
        MapRenderer.Draw(canvas, game);
        CollectionAssert.AreEqual(new (Rect, Rgba)[] { (new(520, 0, 32, 120), new(110, 120, 140)), (new(308, 328, 24, 32), new(240, 180, 65)) }, canvas.Fills);
        Assert.AreEqual(before, State(game));
    }
}
