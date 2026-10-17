using FirstPlayable;
using FirstPlayable.Model;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class SpriteRegionTests
{
    private sealed class Resource(int width, int height) : IImageResource
    {
        public int Width => width;
        public int Height => height;
        public int Releases;
        public void Dispose() => Releases++;
    }
    private class Images : IImageCanvas
    {
        public PixelImage? Source;
        public Resource? Resource;
        public int Creates;
        public readonly List<(Rect, Rgba)> Fills = [];
        public readonly List<Rect> Whole = [];
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) => Fills.Add((rectangle, color));
        public IImageResource CreateImage(PixelImage image) { Creates++; Source = image; return Resource = new(image.Width, image.Height); }
        public void DrawImage(IImageResource image, Rect destination) => Whole.Add(destination);
    }
    private sealed class Regions : Images, IImageRegionCanvas
    {
        public readonly List<(Rect Source, Rect Destination)> Draws = [];
        public void DrawImageRegion(IImageResource image, Rect source, Rect destination)
        {
            Assert.AreSame(Resource, image); Draws.Add((source, destination));
        }
    }
    private sealed class Primitive : ICanvas
    {
        public readonly List<(Rect, Rgba)> Fills = [];
        public void Clear(Rgba color) { }
        public void Fill(Rect rectangle, Rgba color) => Fills.Add((rectangle, color));
    }
    private static AvatarAnimation Animation(int cell)
    {
        var animation = new AvatarAnimation(); var direction = cell < 4 ? 1 : -1;
        switch (cell % 4)
        {
            case 0:
                animation.Update(TimeSpan.FromTicks(1), direction, true);
                animation.Update(TimeSpan.FromTicks(1), 0, true); break;
            case 1: animation.Update(TimeSpan.FromTicks(1), direction, true); break;
            case 2:
                animation.Update(TimeSpan.FromTicks(1), direction, true);
                animation.Update(TimeSpan.FromMilliseconds(120), direction, true); break;
            case 3: animation.Update(TimeSpan.FromTicks(1), direction, false); break;
        }
        return animation;
    }

    [TestMethod]
    public void AllEightCellsSelectExactRegionsWithoutMutatingAnimationOrReuploading()
    {
        var canvas = new Regions(); var sprites = new AvatarSprites();
        for (int cell = 0; cell < 8; cell++)
        {
            var animation = Animation(cell); var before = (animation.Pose, animation.FacingLeft, animation.WalkFrame);
            sprites.Draw(canvas, -2, 10, animation);
            Assert.AreEqual((new Rect(cell * 24, 0, 24, 32), new Rect(-2, 10, 24, 32)), canvas.Draws[^1]);
            Assert.AreEqual(before, (animation.Pose, animation.FacingLeft, animation.WalkFrame));
        }
        Assert.AreEqual(1, canvas.Creates); Assert.AreEqual(0, canvas.Whole.Count); Assert.AreEqual(0, canvas.Fills.Count);
        sprites.Dispose(); sprites.Dispose(); Assert.AreEqual(1, canvas.Resource!.Releases);
        Assert.ThrowsExactly<ObjectDisposedException>(() => sprites.Draw(canvas, 0, 0, new()));
    }

    [TestMethod]
    public void EverySourcePixelMatchesCurrentProceduralAvatarAndBadgeComposition()
    {
        var canvas = new Regions(); using var sprites = new AvatarSprites(); sprites.Draw(canvas, 0, 0, new());
        Assert.AreEqual(192, canvas.Source!.Width); Assert.AreEqual(32, canvas.Source.Height);
        for (int cell = 0; cell < 8; cell++)
        {
            var expected = new Rgba[24 * 32]; var primitive = new Primitive(); AvatarRenderer.Draw(primitive, 0, 0, Animation(cell));
            foreach (var (rect, color) in primitive.Fills)
                for (int y = (int)rect.Y; y < rect.Y + rect.Height; y++)
                for (int x = (int)rect.X; x < rect.X + rect.Width; x++) expected[y * 24 + x] = color;
            // Badge's transparent lower-left preserves underlying orange torso.
            for (int y = 14; y < 22; y++)
            for (int x = 8; x < 16; x++)
            {
                Rgba? color = (x < 12, y < 18) switch
                {
                    (true, true) => new(70, 200, 210), (false, true) => new(255, 255, 255),
                    (false, false) => new(24, 28, 40), _ => null
                };
                if (color is not null) expected[y * 24 + x] = color.Value;
            }
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 24; x++) Assert.AreEqual(expected[y * 24 + x], canvas.Source.Pixels[y * 192 + cell * 24 + x], $"cell{cell}:{x},{y}");
        }
    }

    [TestMethod]
    public void WholeImageAndPrimitiveFallbackRetainExactExistingRendering()
    {
        var animation = Animation(6); var expected = new Primitive(); AvatarRenderer.Draw(expected, 4, 8, animation);
        var primitive = new Primitive(); using (var sprites = new AvatarSprites()) sprites.Draw(primitive, 4, 8, animation);
        CollectionAssert.AreEqual(expected.Fills, primitive.Fills);
        var images = new Images(); using (var sprites = new AvatarSprites()) { sprites.Draw(images, 4, 8, animation); sprites.Draw(images, 4, 8, animation); }
        CollectionAssert.AreEqual(expected.Fills.Concat(expected.Fills).ToList(), images.Fills);
        Assert.AreEqual(1, images.Creates); Assert.AreEqual(2, images.Source!.Width); Assert.AreEqual(2, images.Source.Height);
        CollectionAssert.AreEqual(new Rect[] { new(12, 22, 8, 8), new(12, 22, 8, 8) }, images.Whole);
        Assert.AreEqual(1, images.Resource!.Releases);
    }

    [TestMethod]
    public void InvalidInputsHaveNoProviderEffectsAndBaseInterfaceRemainsUnchanged()
    {
        var canvas = new Regions(); using var sprites = new AvatarSprites();
        Assert.ThrowsExactly<ArgumentNullException>(() => sprites.Draw(null!, 0, 0, new()));
        Assert.ThrowsExactly<ArgumentNullException>(() => sprites.Draw(canvas, 0, 0, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sprites.Draw(canvas, float.NaN, 0, new()));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sprites.Draw(canvas, 0, float.PositiveInfinity, new()));
        Assert.AreEqual(0, canvas.Creates); Assert.AreEqual(0, canvas.Draws.Count);
        CollectionAssert.AreEquivalent(new[] { "CreateImage", "DrawImage" }, typeof(IImageCanvas).GetMethods().Select(method => method.Name).ToArray());
    }
}
