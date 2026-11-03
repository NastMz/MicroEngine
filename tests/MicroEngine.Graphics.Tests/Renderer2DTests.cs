using System.Numerics;
using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics.Tests;

/// <summary>The 2D batcher over the device: batching, sort, state and transform stacks, clears and its exact counters.</summary>
[TestClass]
public sealed class Renderer2DTests
{
    private const uint Black = 0x000000FF;
    private ReferenceGraphicsDevice _device = null!;
    private Renderer2D _batcher = null!;

    [TestInitialize]
    public void Open()
    {
        _device = new ReferenceGraphicsDevice();
        _batcher = new Renderer2D(_device);
    }

    [TestCleanup]
    public void Close()
    {
        _batcher.Dispose();
        _device.Dispose();
    }

    private Texture Solid(uint rgba) => _device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(rgba));
    private RenderTarget Target(int w, int h) => _device.CreateRenderTarget(new RenderTargetDescription(w, h));
    private static ColorF Opaque(byte r, byte g, byte b) => PremultipliedColor.FromBytes(r, g, b);

    private Image Read(RenderTarget target)
    {
        var request = _device.ReadPixels(target, target.Bounds);
        request.Wait();
        return new Image(target.Width, target.Height, request.Pixels.ToArray());
    }

    private void Pixel(Texture texture, float x, float y, float depth = 0) => _batcher.Draw(texture, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(x, y), ColorF.White, depth);

    [TestMethod]
    public void ConsecutiveDrawsOfOneTextureAndState_ShareOneDrawCall_InSubmissionOrder()
    {
        Texture a = Solid(0xFF0000FF), b = Solid(0x00FF00FF);
        var target = Target(8, 8);
        var before = _device.Counters;
        _batcher.Begin(new BatchSettings(target, Clear: Opaque(0, 0, 0)));
        Pixel(a, 0, 0); Pixel(a, 1, 0); Pixel(b, 2, 0); Pixel(a, 3, 0);
        _batcher.End();
        Assert.AreEqual(new Renderer2DStatistics(Sprites: 4, DrawCalls: 3, Flushes: 1), _batcher.Statistics);
        Assert.AreEqual(3, (_device.Counters - before).DrawCalls, "the device received exactly the batcher's draws");
        var image = Read(target);
        Assert.AreEqual(0xFF0000FFu, image[1, 0]);
        Assert.AreEqual(0x00FF00FFu, image[2, 0], "submission order kept");
        Assert.AreEqual(0xFF0000FFu, image[3, 0]);
    }

    [TestMethod]
    public void TextureSort_GroupsByTexture_AndDepthSortsOrderByDepth()
    {
        Texture a = Solid(0xFF0000FF), b = Solid(0x00FF00FF);
        var target = Target(8, 8);
        _batcher.Begin(new BatchSettings(target, Sort: SortMode.Texture));
        Pixel(a, 0, 0); Pixel(b, 1, 0); Pixel(a, 2, 0);
        _batcher.End();
        Assert.AreEqual(2, _batcher.Statistics.DrawCalls, "A, B, A sorted by texture is two draws");

        _batcher.Begin(new BatchSettings(target, Sort: SortMode.BackToFront, Clear: Opaque(0, 0, 0)));
        Pixel(a, 4, 4, depth: 0.1f); // front, recorded first
        Pixel(b, 4, 4, depth: 0.9f); // back
        _batcher.End();
        Assert.AreEqual(0xFF0000FFu, Read(target)[4, 4], "back to front: the nearer (smaller depth) draw ends on top");

        _batcher.Begin(new BatchSettings(target, Sort: SortMode.FrontToBack, Clear: Opaque(0, 0, 0)));
        Pixel(a, 4, 4, depth: 0.1f);
        Pixel(b, 4, 4, depth: 0.9f);
        _batcher.End();
        Assert.AreEqual(0x00FF00FFu, Read(target)[4, 4], "front to back: the farther draw is drawn last");
    }

    [TestMethod]
    public void StateStack_ChangesBlendSamplerAndScissor_UntilPopped()
    {
        var white = Solid(0xFFFFFFFF);
        var target = Target(8, 4);
        _batcher.Begin(new BatchSettings(target, Clear: Opaque(0, 0, 0)));
        _batcher.PushState(_batcher.State with { Scissor = new RectI(2, 0, 2, 4) });
        _batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(8, 4), ColorF.White);
        _batcher.PopState();
        _batcher.PushState(_batcher.State with { Blend = BlendState.Additive });
        _batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(6, 0), new ColorF(0.2f, 0, 0, 0.2f));
        _batcher.PopState();
        Assert.AreEqual(RenderState.Default, _batcher.State, "every push popped");
        _batcher.End();
        var image = Read(target);
        Assert.AreEqual(new RectI(2, 0, 2, 4), image.BoundsOf(0xFFFFFFFFu), "the scissored draw");
        Assert.AreEqual(51, image.Channel(6, 0, 0), "the additive draw added 0.2 of red");
        Assert.AreEqual(2, _batcher.Statistics.DrawCalls, "a state change starts a new draw");
        Assert.ThrowsExactly<InvalidOperationException>(() => _batcher.PopState(), "pop without push");
    }

    [TestMethod]
    public void TransformStack_AndView_PlaceDraws()
    {
        var white = Solid(0xFFFFFFFF);
        var target = Target(16, 16);
        _batcher.Begin(new BatchSettings(target, View: Matrix3x2.CreateTranslation(4, 0), Clear: Opaque(0, 0, 0)));
        _batcher.PushTransform(Matrix3x2.CreateTranslation(0, 3));
        Pixel(white, 1, 1);
        _batcher.PopTransform();
        _batcher.End();
        Assert.AreEqual(new RectI(5, 4, 1, 1), Read(target).BoundsOfNot(Black), "world, then pushed transform, then view");
    }

    [TestMethod]
    public void Clear_InTheMiddleOfABatch_ReplacesWhatWasDrawnBefore()
    {
        Texture a = Solid(0xFF0000FF), b = Solid(0x00FF00FF);
        var target = Target(8, 8);
        _batcher.Begin(new BatchSettings(target));
        Pixel(a, 0, 0);
        _batcher.Clear(Opaque(0, 0, 255));
        Pixel(b, 1, 0);
        _batcher.End();
        var image = Read(target);
        Assert.AreEqual(0x0000FFFFu, image[0, 0], "the clear replaced the earlier draw");
        Assert.AreEqual(0x00FF00FFu, image[1, 0]);
        Assert.AreEqual(2, _batcher.Statistics.Flushes);
    }

    [TestMethod]
    public void DefaultBlend_IsPremultipliedOver()
    {
        var white = Solid(0xFFFFFFFF);
        var target = Target(4, 4);
        _batcher.Begin(new BatchSettings(target, Clear: Opaque(0, 0, 255)));
        _batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(4), PremultipliedColor.FromStraight(new Color(255, 0, 0, 128)));
        _batcher.End();
        var p = Read(target);
        Assert.AreEqual(128, p.Channel(1, 1, 0), "red 0.502");
        Assert.AreEqual(127, p.Channel(1, 1, 2), "blue 1 × (1 − 0.502)");
    }

    [TestMethod]
    public void Geometry_WithoutATexture_UsesTheVertexColours()
    {
        var target = Target(8, 8);
        var red = new ColorF(1, 0, 0, 1);
        _batcher.Begin(new BatchSettings(target, Clear: Opaque(0, 0, 0)));
        _batcher.DrawGeometry(null, [new(new(1, 1), default, red), new(new(5, 1), default, red), new(new(5, 5), default, red), new(new(1, 5), default, red)], [0, 1, 2, 0, 2, 3]);
        _batcher.End();
        Assert.AreEqual(new RectI(1, 1, 4, 4), Read(target).BoundsOf(0xFF0000FFu));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _batcher.Begin(new BatchSettings(target));
            try { _batcher.DrawGeometry(null, [new(default, default, red)], [0, 1, 2]); }
            finally { _batcher.End(); }
        }, "an index outside the vertices");
    }

    [TestMethod]
    public void ManyVertices_SplitIntoSeveralDraws_With16BitIndices()
    {
        var white = Solid(0xFFFFFFFF);
        var target = Target(4, 4);
        _batcher.Begin(new BatchSettings(target));
        for (int i = 0; i < 20000; i++) Pixel(white, 0, 0); // 80,000 vertices, one texture, one state
        _batcher.End();
        Assert.AreEqual(2, _batcher.Statistics.DrawCalls, "16,383 quads fit in 65,535 vertices: two draws");
    }

    [TestMethod]
    public void Counters_AreIdenticalOnEveryRun()
    {
        Renderer2DStatistics Run()
        {
            using var device = new ReferenceGraphicsDevice();
            using var batcher = new Renderer2D(device);
            var t = device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFFFFFFFF));
            var u = device.CreateTexture(new TextureDescription(1, 1), Texels.Bytes(0xFF0000FF));
            batcher.Begin(new BatchSettings(device.CreateRenderTarget(new RenderTargetDescription(8, 8))));
            for (int i = 0; i < 50; i++) batcher.Draw(i % 3 == 0 ? u : t, new RectF(0, 0, 1, 1), Matrix3x2.CreateTranslation(i % 8, i / 8), ColorF.White);
            batcher.End();
            return batcher.Statistics;
        }
        Assert.AreEqual(Run(), Run());
    }

    [TestMethod]
    public void Misuse_IsRejected()
    {
        var target = Target(4, 4);
        Assert.ThrowsExactly<InvalidOperationException>(() => _batcher.End(), "End without Begin");
        Assert.ThrowsExactly<InvalidOperationException>(() => Pixel(Solid(0xFFFFFFFF), 0, 0), "Draw without Begin");
        _batcher.Begin(new BatchSettings(target));
        Assert.ThrowsExactly<InvalidOperationException>(() => _batcher.Begin(new BatchSettings(target)), "Begin twice");
        _batcher.PushTransform(Matrix3x2.Identity);
        Assert.ThrowsExactly<InvalidOperationException>(() => _batcher.End(), "a pushed transform not popped");
        _batcher.PopTransform();
        _batcher.End();
    }
}

/// <summary>The 2D camera's transform: zoom and rotation around the view's centre, optional pixel snap.</summary>
[TestClass]
public sealed class Camera2DTests
{
    [TestMethod]
    public void WithoutZoomOrRotation_TheTransformIsTheRenderOffset()
    {
        var camera = new Camera2D(320, 180) { Position = new Vector2(10.4f, 20.6f) };
        Assert.AreEqual(new Vector2(-10, -21), camera.RenderOffset, "snapped by default");
        Assert.AreEqual(Matrix3x2.CreateTranslation(camera.RenderOffset), camera.WorldToView);
        Assert.AreEqual(camera.View, camera.VisibleBounds);
    }

    [TestMethod]
    public void Zoom_ScalesAroundTheViewCentre()
    {
        var camera = new Camera2D(100, 100) { Position = new Vector2(0, 0), Zoom = 2, PixelSnap = false };
        Assert.AreEqual(new Vector2(50, 50), Vector2.Transform(new Vector2(50, 50), camera.WorldToView), "the centre stays");
        Assert.AreEqual(new Vector2(70, 50), Vector2.Transform(new Vector2(60, 50), camera.WorldToView), "10 world pixels are 20 canvas pixels");
        Assert.AreEqual(new RectF(25, 25, 50, 50), camera.VisibleBounds, "half as much of the world");
        Assert.AreEqual(new Vector2(60, 50), camera.ScreenToWorld(new Vector2(70, 50)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => camera.Zoom = 0);
    }

    [TestMethod]
    public void Rotation_TurnsTheWorldClockwiseAroundTheViewCentre()
    {
        var camera = new Camera2D(100, 100) { Rotation = MathF.PI / 2, PixelSnap = false };
        var p = Vector2.Transform(new Vector2(60, 50), camera.WorldToView);
        Assert.AreEqual(50f, p.X, 1e-4f);
        Assert.AreEqual(60f, p.Y, 1e-4f, "a point right of the centre ends below it (y down)");
        Assert.IsTrue(camera.IsVisible(new RectF(45, 45, 10, 10)));
    }
}

/// <summary>The camera's pixel snap comes from the presentation preset, and each camera can change it.</summary>
[TestClass]
public sealed class CameraPresentationTests
{
    [TestMethod]
    public void PixelArt_SnapsByDefault_HighResolution2D_DoesNot_AndEitherCanBeOverridden()
    {
        Assert.IsTrue(Presentation.PixelArt.CameraPixelSnap);
        Assert.IsFalse(Presentation.HighResolution2D.CameraPixelSnap);
        var pixel = new Camera2D(320, 180, Presentation.PixelArt) { Position = new Vector2(10.4f, 0) };
        var smooth = new Camera2D(1280, 720, Presentation.HighResolution2D) { Position = new Vector2(10.4f, 0) };
        Assert.IsTrue(pixel.PixelSnap);
        Assert.AreEqual(-10f, pixel.RenderOffset.X, "snapped to whole pixels");
        Assert.IsFalse(smooth.PixelSnap);
        Assert.AreEqual(-10.4f, smooth.RenderOffset.X, 1e-5f, "smooth motion");
        smooth.PixelSnap = true;
        Assert.AreEqual(-10f, smooth.RenderOffset.X, "overridden per camera");
        Assert.IsTrue(new Camera2D(320, 180).PixelSnap, "the two-argument constructor keeps the pixel-art default");
        Assert.IsFalse(new Camera2D(320, 180, Presentation.PixelArt with { CameraPixelSnap = false }).PixelSnap, "a presentation can choose it");
        Assert.ThrowsExactly<ArgumentNullException>(() => new Camera2D(1, 1, null!));
    }
}
