using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>
/// Draw order that does not depend on call order (<see cref="SortedRenderer"/>): draws recorded between Begin and End
/// reach the renderer by layer, then by depth, then in the order they were made, each with the camera, blending, clip
/// and state it was recorded with.
/// </summary>
[TestClass]
public sealed class SortedRendererTests
{
    private static readonly Color Red = new(255, 0, 0), Green = new(0, 255, 0), Blue = new(0, 0, 255);

    private static HeadlessRenderer Canvas(int width = 16, int height = 8) => new(width, height, new HeadlessGraphicsDevice { KeepsPixels = true });

    [TestMethod]
    public void Draws_reach_the_renderer_by_layer_then_depth_then_call_order()
    {
        var canvas = Canvas();
        var sorted = new SortedRenderer();
        var r = sorted.Begin(canvas);
        r.Depth = 30; r.FillRect(new RectF(0, 0, 4, 4), Red);          // in front: drawn last of its layer
        r.Depth = 10; r.FillRect(new RectF(0, 0, 4, 4), Green);
        r.Depth = 10; r.FillRect(new RectF(0, 0, 4, 4), Blue);         // same depth: after the green one, as called
        r.At(layer: -1, depth: 99).FillRect(new RectF(0, 0, 8, 8), Color.White);   // a lower layer: under everything
        Assert.AreEqual(4, sorted.Count);
        Assert.AreEqual(0, canvas.DrawCalls, "nothing reaches the renderer before End");
        sorted.End();

        CollectionAssert.AreEqual(new[] { "#FFFFFFFF", "#00FF00FF", "#0000FFFF", "#FF0000FF" },
            canvas.Commands.Where(c => c.StartsWith("rect", StringComparison.Ordinal)).Select(c => c[^9..]).ToArray());
        Assert.AreEqual(Red, canvas.Frame![1, 1], "the largest depth of the top layer covers the rest");
        Assert.AreEqual(Color.White, canvas.Frame![6, 6]);
        Assert.AreEqual(0, sorted.Count, "End empties the queue");
    }

    [TestMethod]
    public void Sprites_sorted_by_their_feet_cover_each_other_like_objects_standing_on_the_ground()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var canvas = new HeadlessRenderer(16, 16, graphics);
        var near = graphics.CreateTexture(new PixelImage(4, 8).Clear(Red));
        var far = graphics.CreateTexture(new PixelImage(4, 8).Clear(Blue));
        var sorted = new SortedRenderer();
        var r = sorted.Begin(canvas);
        // Called nearest first, the wrong way round for a painter.
        r.Depth = 12; r.Draw(near, near.Bounds, new Vector2(2, 4), Color.White);    // feet at y = 12
        r.Depth = 9; r.Draw(far, far.Bounds, new Vector2(4, 1), Color.White);       // feet at y = 9: behind
        sorted.End();
        Assert.AreEqual(Red, canvas.Frame![4, 6], "where they overlap the nearer sprite shows");
        Assert.AreEqual(Blue, canvas.Frame![7, 6]);
    }

    [TestMethod]
    public void Every_draw_keeps_the_camera_blending_clip_and_state_it_was_recorded_with()
    {
        var canvas = Canvas(16, 8);
        canvas.Clear(Color.Black);
        var camera = new Camera2D(16, 8) { PixelSnap = true };
        camera.Position = new Vector2(4, 0);   // the world moves 4 pixels left on screen
        var sorted = new SortedRenderer();
        var r = sorted.Begin(canvas);

        r.Depth = 3;
        r.PushCamera(camera);
        r.FillRect(new RectF(4, 0, 2, 2), Red);                 // world (4, 0) is screen (0, 0)
        r.PopCamera();

        r.Depth = 1;
        r.SetClip(new RectF(8, 0, 2, 8));
        r.FillRect(new RectF(0, 4, 16, 2), Green);              // only columns 8 and 9
        r.SetClip(null);

        r.Depth = 2;
        r.PushState(DrawState.Additive);
        r.FillRect(new RectF(8, 4, 1, 1), Red);                 // added to the green under it: yellow
        r.PopState();

        r.Depth = 0;
        r.FillRect(new RectF(12, 0, 2, 2), Blue);               // no camera, no clip, normal blending
        sorted.End();

        var frame = canvas.Frame!;
        Assert.AreEqual(Red, frame[0, 0], "drawn through the camera it was recorded under");
        Assert.AreEqual(Color.Black, frame[4, 0], "and not where it would be without it");
        Assert.AreEqual(Green, frame[9, 4]);
        Assert.AreEqual(Color.Black, frame[2, 4], "the clip of that draw alone");
        Assert.AreEqual(new Color(255, 255, 0), frame[8, 4], "the additive state of that draw alone");
        Assert.AreEqual(Blue, frame[12, 0]);
        Assert.AreEqual(0, canvas.CameraDepth, "nothing is left pushed");
        Assert.AreEqual(0, canvas.StateDepth);
        // The renderer is as a frame leaves it: the next draw is not clipped and blends normally.
        canvas.FillRect(new RectF(0, 6, 1, 1), new Color(255, 255, 255, 255));
        Assert.AreEqual(Color.White, canvas.Frame![0, 6]);
    }

    [TestMethod]
    public void The_same_draws_in_any_call_order_give_the_same_picture()
    {
        (int Layer, float Depth, RectF Rect, Color Color)[] draws =
        [
            (0, 5, new RectF(0, 0, 6, 6), Red), (0, 2, new RectF(2, 2, 6, 6), Green), (1, 0, new RectF(4, 0, 3, 8), Blue),
            (0, 9, new RectF(1, 4, 9, 2), Color.White), (-2, 1, new RectF(0, 0, 16, 8), new Color(40, 40, 40)),
        ];
        PixelImage Picture(IEnumerable<int> order)
        {
            var canvas = Canvas();
            var sorted = new SortedRenderer();
            var r = sorted.Begin(canvas);
            foreach (int i in order) r.At(draws[i].Layer, draws[i].Depth).FillRect(draws[i].Rect, draws[i].Color);
            sorted.End();
            return canvas.Frame!;
        }
        var expected = Picture([0, 1, 2, 3, 4]);
        foreach (var order in new[] { new[] { 4, 3, 2, 1, 0 }, new[] { 2, 0, 4, 1, 3 } })
            CollectionAssert.AreEqual(expected.Pixels.ToArray(), Picture(order).Pixels.ToArray());
    }

    [TestMethod]
    public void What_cannot_be_ordered_is_refused_and_begin_and_end_must_pair()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var canvas = new HeadlessRenderer(8, 8, graphics);
        var sorted = new SortedRenderer();
        Assert.ThrowsExactly<InvalidOperationException>(() => sorted.End(), "End without Begin");
        Assert.ThrowsExactly<InvalidOperationException>(() => sorted.FillRect(new RectF(0, 0, 1, 1), Red), "a draw without Begin");
        var r = sorted.Begin(canvas);
        Assert.AreEqual((8, 8), (r.Width, r.Height));
        Assert.ThrowsExactly<InvalidOperationException>(() => sorted.Begin(canvas), "Begin twice");
        Assert.ThrowsExactly<InvalidOperationException>(() => r.Clear(Color.Black), "a clear has no place in an order");
        using var target = graphics.CreateRenderTarget(4, 4);
        Assert.ThrowsExactly<InvalidOperationException>(() => r.PushTarget(target), "a target is drawn into with the renderer itself");
        Assert.ThrowsExactly<InvalidOperationException>(() => r.PopCamera(), "PopCamera without PushCamera");
        Assert.ThrowsExactly<InvalidOperationException>(() => r.PopState(), "PopState without PushState");
        r.PushState(DrawState.Additive);
        Assert.ThrowsExactly<InvalidOperationException>(() => sorted.End(), "a state left pushed");
        Assert.AreEqual(0, canvas.DrawCalls, "a refused End draws nothing");
        // The refused End closed the order: the next Begin starts clean.
        sorted.Begin(canvas).FillRect(new RectF(0, 0, 1, 1), Red);
        sorted.End();
        Assert.AreEqual(Red, canvas.Frame![0, 0]);
    }
}
