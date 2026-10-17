using MicroEngine.Hosting;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomExplorer.Model;
using TurnExplorer;
using TurnExplorer.Model;

namespace Loop.Contracts;

[TestClass]
public sealed class LabelCanvasTests
{
    private class PrimitiveCanvas(Action? guard = null) : ICanvas
    {
        public List<(Rect, Rgba)> Fills = [];
        public List<string> Trace = [];
        protected void Check() => guard?.Invoke();
        public void Clear(Rgba color) { Check(); Trace.Add("clear"); }
        public void Fill(Rect rectangle, Rgba color) { Check(); Fills.Add((rectangle, color)); Trace.Add("fill"); }
    }
    private sealed class TextCanvas(Action? guard = null) : PrimitiveCanvas(guard), ILabelCanvas
    {
        public List<(string, int, int, int, Rgba)> Labels = [];
        public void Label(string text, int x, int y, int size, Rgba color)
        {
            Check();
            FrameApi.Require(text is not null && text.Length <= 80 && text.All(c => c is >= ' ' and <= '~') && size is >= 10 and <= 64, "label");
            if (string.IsNullOrEmpty(text)) return;
            Labels.Add((text, x, y, size, color)); Trace.Add("label");
        }
    }
    private sealed class PublicProvider : IFramePlatform
    {
        private bool _drawing, _disposed;
        public int Closes;
        public TextCanvas Text { get; }
        public PublicProvider() => Text = new(() => FrameApi.Require(_drawing && !_disposed, "drawing"));
        public PlatformFrame Poll() => new(TimeSpan.Zero, new([], []));
        public void Begin() => _drawing = true;
        public ICanvas Canvas { get { FrameApi.Require(_drawing && !_disposed, "drawing"); return Text; } }
        public void End() => _drawing = false;
        public void Dispose() { _disposed = true; Closes++; }
    }
    private static TurnSession Open(MenuOption selected = MenuOption.Advance)
        => TurnSession.Restore(new(1, Room.Courtyard, 310, 164, true, true, selected, 2));

    [TestMethod]
    public void OptionalLabelsPreserveFallbackGeometryAndAllState()
    {
        foreach (var open in new[] { false, true })
        foreach (var selected in Enum.GetValues<MenuOption>())
        {
            var game = TurnSession.Restore(Open(selected).Capture() with { MenuOpen = open });
            if (open && selected == MenuOption.Advance) game.Update(TimeSpan.FromTicks(1), new([], ["jump"]));
            var before = game.Capture(); var resolved = game.TurnResolved;
            var primitive = new PrimitiveCanvas(); var text = new TextCanvas();
            MenuRenderer.Draw(primitive, game); MenuRenderer.Draw(text, game);
            Assert.AreEqual(open ? 9 : 4, primitive.Fills.Count);
            CollectionAssert.AreEqual(primitive.Fills, text.Fills);
            Assert.AreEqual(open ? 2 : 0, text.Labels.Count);
            CollectionAssert.AreEqual(primitive.Trace, text.Trace.Take(primitive.Trace.Count).ToArray());
            if (open)
            {
                var dark = new Rgba(24, 28, 40); var light = new Rgba(240, 240, 245);
                Assert.AreEqual(("Advance", 480, 50, 20, selected == MenuOption.Advance ? dark : light), text.Labels[0]);
                Assert.AreEqual(("Return", 480, 98, 20, selected == MenuOption.Return ? dark : light), text.Labels[1]);
                CollectionAssert.AreEqual(new[] { "label", "label" }, text.Trace.Skip(9).ToArray());
            }
            Assert.AreEqual(before, game.Capture()); Assert.AreEqual(resolved, game.TurnResolved);
        }
    }

    [TestMethod]
    public void PublicOnlyProviderUsesProfileWithoutNativeOrFriendshipDependency()
    {
        var provider = new PublicProvider(); var game = Open();
        var report = FrameLoop.Run(() => provider, new(TimeSpan.FromTicks(1), 1, TimeSpan.FromTicks(10)), _ => { },
            (canvas, frame) => { canvas.Clear(new()); MenuRenderer.Draw(canvas, game); frame.Control.RequestStop(); });
        Assert.AreEqual(ExecutionOutcome.Stopped, report.Outcome);
        Assert.AreEqual(2, provider.Text.Labels.Count); Assert.AreEqual(1, provider.Closes);
        CollectionAssert.AreEquivalent(new[] { "Clear", "Fill" }, typeof(ICanvas).GetMethods().Select(m => m.Name).ToArray());
        Assert.IsFalse(typeof(ILabelCanvas).Assembly.GetReferencedAssemblies().Any(a => a.Name is "Raylib-cs" or "Desktop"));
    }
}
