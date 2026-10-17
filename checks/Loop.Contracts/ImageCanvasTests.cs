using FirstPlayable;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class ImageCanvasTests
{
    private sealed class PrimitiveCanvas : ICanvas
    {
        public int Calls;
        public void Clear(Rgba color) => Calls++;
        public void Fill(Rect rectangle, Rgba color) => Calls++;
    }
    private sealed class Resource : IImageResource
    {
        public int Width => 2;
        public int Height => 2;
        public int Releases;
        public void Dispose() => Releases++;
    }
    private sealed class Images : IImageCanvas
    {
        public readonly Resource Resource = new();
        public readonly List<Rect> Draws = [];
        public PixelImage? Source;
        public int Creates;
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) { }
        public IImageResource CreateImage(PixelImage image) { Source = image; Creates++; return Resource; }
        public void DrawImage(IImageResource image, Rect destination) { Assert.AreSame(Resource, image); Draws.Add(destination); }
    }

    [TestMethod]
    public void SupplementUsesPublicProviderCachesOnceAndPreservesExactPixels()
    {
        var canvas = new Images(); var avatar = new AvatarImage();
        avatar.Draw(canvas, 10, 20); avatar.Draw(canvas, -1, -2);
        Assert.AreEqual(1, canvas.Creates); Assert.AreEqual(2, canvas.Source!.Width); Assert.AreEqual(2, canvas.Source.Height);
        CollectionAssert.AreEqual(new Rgba[] { new(70, 200, 210), new(255, 255, 255), new(0, 0, 0, 0), new(24, 28, 40) }, canvas.Source.Pixels.ToArray());
        CollectionAssert.AreEqual(new Rect[] { new(18, 34, 8, 8), new(7, 12, 8, 8) }, canvas.Draws);
        avatar.Dispose(); avatar.Dispose(); Assert.AreEqual(1, canvas.Resource.Releases);
        Assert.ThrowsExactly<ObjectDisposedException>(() => avatar.Draw(canvas, 0, 0));
    }

    [TestMethod]
    public void PrimitiveOnlyFallbackHasNoEffectsAndValidatesInput()
    {
        var canvas = new PrimitiveCanvas(); using var avatar = new AvatarImage(); avatar.Draw(canvas, 0, 0);
        Assert.AreEqual(0, canvas.Calls);
        Assert.ThrowsExactly<ArgumentNullException>(() => avatar.Draw(null!, 0, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => avatar.Draw(canvas, float.NaN, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => avatar.Draw(canvas, 0, float.PositiveInfinity));
        Assert.AreEqual(0, canvas.Calls);
        CollectionAssert.AreEquivalent(new[] { "Clear", "Fill" }, typeof(ICanvas).GetMethods().Select(method => method.Name).ToArray());
        Assert.IsFalse(typeof(PixelImage).Assembly.GetReferencedAssemblies().Any(name => name.Name!.Contains("Raylib") || name.Name.Contains("Desktop")));
    }
}
