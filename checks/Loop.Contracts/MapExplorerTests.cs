using MapExplorer;
using MapExplorer.Model;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class MapExplorerTests
{
    private static InputSnapshot Input(params string[] actions) => new(actions, []);
    private static (Zone, float, float, float, float) State(MapWalker game) => (game.Zone, game.X, game.Y, game.CameraX, game.CameraY);
    private sealed class Canvas : ICanvas
    {
        public Rgba Background;
        public readonly List<(Rect, Rgba)> Fills = [];
        public void Clear(Rgba color) => Background = color;
        public void Fill(Rect rect, Rgba color) => Fills.Add((rect, color));
    }

    [TestMethod]
    public void FixedMapsAreImmutableAndInitialCameraUsesPlayerCenter()
    {
        var meadow = MapLayout.For(Zone.Meadow); var ruins = MapLayout.For(Zone.Ruins);
        Assert.AreEqual((1280, 720, 3), (meadow.Width, meadow.Height, meadow.Landmarks.Count));
        Assert.AreEqual((960, 540, 2), (ruins.Width, ruins.Height, ruins.Landmarks.Count));
        Assert.AreEqual(new Landmark(new(400, 240, 100, 80), new(180, 100, 70)), meadow.Landmarks[1]);
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<Landmark>)meadow.Landmarks)[0] = default);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MapLayout.For((Zone)99));
        Assert.AreEqual((Zone.Meadow, 40f, 304f, 0f, 140f), State(new()));
    }

    [TestMethod]
    public void MovementNormalizesCancelsAndIgnoresPressedActions()
    {
        var game = MapWalker.Create(Zone.Meadow, 500, 300); game.Update(TimeSpan.FromSeconds(1), Input("right")); Assert.AreEqual(680f, game.X);
        game = MapWalker.Create(Zone.Meadow, 500, 300); game.Update(TimeSpan.FromSeconds(1), Input("right", "down"));
        var dx = game.X - 500; var dy = game.Y - 300; Assert.AreEqual(180f, MathF.Sqrt(dx * dx + dy * dy), .001f);
        var before = State(game); game.Update(TimeSpan.FromSeconds(1), Input("left", "right", "up", "down")); Assert.AreEqual(before, State(game));
        game.Update(TimeSpan.FromSeconds(1), new([], ["right", "down", "jump"])); Assert.AreEqual(before, State(game));
    }

    [TestMethod]
    public void BoundsAndCameraClampInBothDifferentSizedMaps()
    {
        foreach (var zone in new[] { Zone.Meadow, Zone.Ruins })
        {
            var map = MapLayout.For(zone);
            var top = MapWalker.Create(zone, 0, 0); top.Update(TimeSpan.MaxValue, Input("left", "up"));
            Assert.AreEqual((zone, 0f, 0f, 0f, 0f), State(top));
            var bottom = MapWalker.Create(zone, map.Width - 24, map.Height - 32); bottom.Update(TimeSpan.MaxValue, Input("right", "down"));
            Assert.AreEqual((zone, (float)map.Width - 24, (float)map.Height - 32, (float)map.Width - 640, (float)map.Height - 360), State(bottom));
            var middle = MapWalker.Create(zone, 508, 304); Assert.AreEqual(200f, middle.CameraX); Assert.AreEqual(140f, middle.CameraY);
        }
    }

    [TestMethod]
    public void PortalUsesStrictCrossingInclusiveBodyOpeningAndImmediateCameraHandoff()
    {
        foreach (var y in new[] { 240f, 368f })
        {
            var east = MapWalker.Create(Zone.Meadow, 1256, y); east.Update(TimeSpan.FromSeconds(1), Input()); Assert.AreEqual(Zone.Meadow, east.Zone);
            east.Update(TimeSpan.MaxValue, Input("right")); Assert.AreEqual((Zone.Ruins, 8f, 304f, 0f, 140f), State(east));
            east.Update(TimeSpan.FromSeconds(1), Input()); Assert.AreEqual(Zone.Ruins, east.Zone);
            var west = MapWalker.Create(Zone.Ruins, 0, y); west.Update(TimeSpan.MaxValue, Input("left")); Assert.AreEqual((Zone.Meadow, 1248f, 304f, 640f, 140f), State(west));
        }
        foreach (var y in new[] { 239f, 369f })
        {
            var east = MapWalker.Create(Zone.Meadow, 1256, y); east.Update(TimeSpan.MaxValue, Input("right")); Assert.AreEqual((Zone.Meadow, 1256f, y, 640f, y - 164), State(east));
            var west = MapWalker.Create(Zone.Ruins, 0, y); west.Update(TimeSpan.MaxValue, Input("left")); Assert.AreEqual(Zone.Ruins, west.Zone); Assert.AreEqual(0f, west.X);
        }
    }

    [TestMethod]
    public void InvalidCallsAreAtomicAndAllCreationBoundsAreChecked()
    {
        var game = new MapWalker(); var before = State(game);
        Assert.ThrowsExactly<ArgumentNullException>(() => game.Update(TimeSpan.FromSeconds(1), null!));
        foreach (var time in new[] { TimeSpan.Zero, TimeSpan.FromTicks(-1) }) Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => game.Update(time, Input("right")));
        Assert.AreEqual(before, State(game));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MapWalker.Create((Zone)99, 0, 0));
        foreach (var zone in new[] { Zone.Meadow, Zone.Ruins })
        {
            var map = MapLayout.For(zone);
            foreach (var x in new[] { float.NaN, float.PositiveInfinity, -1, (float)map.Width - 23 }) Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MapWalker.Create(zone, x, 0));
            foreach (var y in new[] { float.NaN, float.NegativeInfinity, -1, (float)map.Height - 31 }) Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MapWalker.Create(zone, 0, y));
        }
    }

    [TestMethod]
    public void ReplayAndIndependentInstancesRemainDeterministic()
    {
        var first = new MapWalker(); var second = new MapWalker(); var untouched = new MapWalker();
        for (int i = 0; i < 900; i++)
        {
            var input = Input(i < 450 ? "right" : "left"); first.Update(TimeSpan.FromSeconds(1d / 60), input); second.Update(TimeSpan.FromSeconds(1d / 60), input);
            Assert.AreEqual(State(first), State(second));
        }
        Assert.AreEqual((Zone.Meadow, 40f, 304f, 0f, 140f), State(untouched));
    }

    [TestMethod]
    public void RendererTranslatesClipsCullsAndDoesNotMutateModel()
    {
        var game = MapWalker.Create(Zone.Meadow, 508, 304); var before = State(game); var canvas = new Canvas();
        MapRenderer.Draw(canvas, game); Assert.AreEqual(new Rgba(24, 40, 32), canvas.Background);
        CollectionAssert.AreEqual(new (Rect, Rgba)[] { (new(200, 100, 100, 80), new(180, 100, 70)), (new(520, 60, 32, 280), new(110, 120, 140)), (new(308, 164, 24, 32), new(240, 180, 65)) }, canvas.Fills);
        var repeated = new Canvas(); MapRenderer.Draw(repeated, game); CollectionAssert.AreEqual(canvas.Fills, repeated.Fills); Assert.AreEqual(before, State(game));
        canvas = new(); MapRenderer.Draw(canvas, MapWalker.Create(Zone.Meadow, 258, 40));
        Assert.AreEqual((new Rect(200, 80, 100, 60), new Rgba(95, 130, 165)), canvas.Fills[0]);
        var clipped = new Canvas(); MapRenderer.Draw(clipped, MapWalker.Create(Zone.Meadow, 558, 40));
        Assert.AreEqual((new Rect(0, 80, 50, 60), new Rgba(95, 130, 165)), clipped.Fills[0]);
    }

    [TestMethod]
    public void RuinsRendererDrawsLeftPortalAndChecksArguments()
    {
        var canvas = new Canvas(); var game = MapWalker.Create(Zone.Ruins, 8, 304); MapRenderer.Draw(canvas, game);
        Assert.AreEqual(new Rgba(32, 24, 48), canvas.Background);
        CollectionAssert.AreEqual(new (Rect, Rgba)[] { (new(160, 0, 100, 60), new(95, 130, 165)), (new(400, 60, 32, 200), new(110, 120, 140)), (new(0, 100, 8, 160), new(70, 200, 210)), (new(8, 164, 24, 32), new(240, 180, 65)) }, canvas.Fills);
        Assert.ThrowsExactly<ArgumentNullException>(() => MapRenderer.Draw(null!, game)); Assert.ThrowsExactly<ArgumentNullException>(() => MapRenderer.Draw(canvas, null!));
    }
}
