using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Testing;

namespace MicroEngine.Tests;

/// <summary>
/// The parts of the frame loop that used to make garbage every frame, each checked on its own so a regression names its
/// source: the counting renderer of headless runs, the input system's per-update work and the asset cache's frame update.
/// The whole loop is checked by <see cref="PerformanceTests"/>.
/// </summary>
[TestClass]
public sealed class FrameGarbageTests
{
    [TestMethod]
    public void A_counting_renderer_counts_draws_without_garbage()
    {
        var device = new HeadlessGraphicsDevice();
        var texture = device.CreateTexture(new PixelImage(4, 2));
        var renderer = new HeadlessRenderer(64, 32);
        var camera = new Camera2D(64, 32) { Position = new Vector2(3, 2) };
        void Frame()
        {
            renderer.Reset();
            renderer.Clear(new Color(10, 20, 30));
            renderer.PushCamera(camera);
            renderer.FillRect(new RectF(1, 1, 2.5f, 3), new Color(255, 0, 0));
            renderer.Draw(texture, texture.Bounds, new Vector2(4, 4), Color.White, new DrawOptions(FlipX: true, Rotation: 0.5f));
            renderer.DrawStretched(texture, texture.Bounds, new RectF(0, 0, 8, 8), Color.White);
            renderer.PopCamera();
            renderer.FillCircle(new Vector2(8, 8), 1.5f, Color.White);
            renderer.FillTriangle(Vector2.Zero, Vector2.UnitX, Vector2.UnitY, Color.Black);
            renderer.DrawLine(Vector2.Zero, Vector2.One, 1, Color.White);
            renderer.SetBlendMode(BlendMode.Additive);
            renderer.SetClip(new RectF(0, 0, 8, 8));
        }
        Frame();
        Assert.AreEqual(7, renderer.DrawCalls, "clear, rect, two sprites, circle, triangle and line are counted");
        Assert.IsEmpty(renderer.Commands, "a counting renderer records no text");
        Assert.AreEqual(0, renderer.CameraDepth);
        AllocationAssert.None(Frame, because: "headless runs draw every frame through the counting renderer");
    }

    [TestMethod]
    public void Sampling_and_ending_an_update_allocate_nothing_for_every_player()
    {
        var input = new InputSystem(InputMap.CreateDefault());
        input.Players.Add(InputMap.CreateDefault());
        AllocationAssert.None(() =>
        {
            input.BeginFrame();
            input.Sample();
            input.EndUpdate();
        }, because: "the loop samples input before and ends it after every fixed update");
    }

    [TestMethod]
    public void The_frame_update_of_an_asset_cache_with_scopes_allocates_nothing()
    {
        var assets = new AssetCache(new HeadlessGraphicsDevice(), AppContext.BaseDirectory);
        var level = assets.CreateScope("level");
        level.CreateScope("room");
        assets.CreateScope("menu");
        AllocationAssert.None(assets.Update, because: "the engine updates the asset cache and its scopes every frame");
    }

    [TestMethod]
    public void The_frame_update_finishes_the_uploads_of_child_scopes_in_order()
    {
        var device = new HeadlessGraphicsDevice();
        var assets = new AssetCache(device, Path.Combine(AppContext.BaseDirectory, "Gallery")) { BackgroundDecoding = false };
        var first = assets.CreateScope("first");
        var second = first.CreateScope("second");
        var a = first.Preload("items/blueGem.png");
        var b = second.Preload("backgrounds/set1_background.png");
        Assert.AreEqual(0, device.TexturesCreated, "nothing reaches the device before the engine's update");
        assets.Update();
        Assert.IsTrue(a.IsDone && b.IsDone, "the root's update reaches every scope");
        Assert.AreEqual(2, device.TexturesCreated);
        second.Dispose();
        assets.Update();
        Assert.AreEqual(1, device.LiveTextures, "a disposed scope is no longer updated and released its texture");
    }
}
