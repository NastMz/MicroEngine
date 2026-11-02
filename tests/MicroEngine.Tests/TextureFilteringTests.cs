using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>Filtering, canvas scaling and window sizes: pixel art is a supported style, not the only behaviour.</summary>
[TestClass]
public sealed class TextureFilteringTests
{
    [TestMethod]
    public void The_default_texture_filter_follows_the_canvas_scaling_unless_the_game_chooses_one()
    {
        Assert.AreEqual(TextureFilter.Nearest, new GameSettings("pixel").DefaultTextureFilter);
        Assert.AreEqual(TextureFilter.Linear, new GameSettings("hd", 1280, 720) { Scaling = CanvasScaling.Smooth }.DefaultTextureFilter);
        Assert.AreEqual(TextureFilter.Nearest, new GameSettings("hd", 1280, 720) { Scaling = CanvasScaling.Smooth, TextureFilter = TextureFilter.Nearest }.DefaultTextureFilter);
        Assert.AreEqual(TextureFilter.Linear, new GameSettings("pixel") { TextureFilter = TextureFilter.Linear }.DefaultTextureFilter);
    }

    private sealed class HdGame : Game
    {
        public override GameSettings Settings { get; } = new("hd", 1280, 720) { Scaling = CanvasScaling.Smooth };
        public Texture? Image;
        protected override void Load() { Image = Graphics.CreateTexture(new PixelImage(4, 4)); Scenes.Push(new Empty()); }
        private sealed class Empty : Scene { public override void Update(float dt) { } public override void Draw(IRenderer r) { } }
    }

    [TestMethod]
    public void A_smooth_game_gets_linear_textures_and_keeps_the_built_in_pixel_font_nearest()
    {
        var game = new HdGame();
        var platform = HeadlessPlatform.For(game.Settings);
        game.InitializeForTest(platform);
        Assert.AreEqual(TextureFilter.Linear, platform.Graphics.DefaultFilter);
        Assert.AreEqual(TextureFilter.Linear, game.Image!.Filter);
        var device = (HeadlessGraphicsDevice)platform.Graphics;
        var explicitNearest = device.CreateTexture(new PixelImage(2, 2), TextureFilter.Nearest);
        Assert.AreEqual(TextureFilter.Nearest, explicitNearest.Filter);
    }

    [TestMethod]
    public void A_texture_filter_can_change_at_run_time_and_a_disposed_texture_refuses_changes()
    {
        var device = new HeadlessGraphicsDevice();
        var texture = device.CreateTexture(new PixelImage(2, 2), TextureFilter.Nearest);
        texture.Filter = TextureFilter.Linear;
        Assert.AreEqual(TextureFilter.Linear, texture.Filter);
        texture.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => texture.Filter = TextureFilter.Nearest);
        Assert.AreEqual(0, device.LiveTextures);
    }

    [TestMethod]
    public void Texture_updates_check_their_rectangle_and_send_exactly_its_pixels()
    {
        var device = new HeadlessGraphicsDevice();
        var texture = device.CreateTexture(new PixelImage(8, 4));
        long afterCreate = device.PixelsUploaded;
        texture.Update(2, 1, 3, 2, new Color[6]);
        Assert.AreEqual(1, device.Updates);
        Assert.AreEqual(afterCreate + 6, device.PixelsUploaded);
        texture.Update(new PixelImage(8, 4));
        Assert.AreEqual(afterCreate + 6 + 32, device.PixelsUploaded);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => texture.Update(6, 0, 3, 1, new Color[3]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => texture.Update(-1, 0, 1, 1, new Color[1]));
        Assert.ThrowsExactly<ArgumentException>(() => texture.Update(0, 0, 2, 2, new Color[3]));
        Assert.AreEqual(32 * 4, texture.SizeInBytes);
    }

    [TestMethod]
    public void Pixel_scaling_prefers_whole_factors_and_smooth_scaling_fills_the_window()
    {
        var window = new Vector2(1366, 768);
        var safe = new RectF(0, 0, 1366, 768);
        // 320×180 on 1366×768: 4.27 fits; 4 is within 80 %, so pixel scaling takes it.
        Assert.AreEqual(new Vector2(1280, 720), FrameLayout.Fit(window, safe, 320, 180).Size);
        var smooth = FrameLayout.Fit(window, safe, 320, 180, CanvasScaling.Smooth);
        Assert.AreEqual(1365.33f, smooth.Width, 0.01f);
        Assert.AreEqual(768, smooth.Height, 0.01f);
        // An HD canvas shrinks into a smaller window in both modes.
        var hd = FrameLayout.Fit(window, safe, 1920, 1080, CanvasScaling.Smooth);
        Assert.AreEqual(1365.33f, hd.Width, 0.01f);
        Assert.AreEqual(768, hd.Height, 0.01f);
    }

    [TestMethod]
    public void Touch_controls_keep_whole_factors_only_for_pixel_scaling()
    {
        var controls = new VirtualControls(TouchLayout.CreateDefault());
        var window = new Vector2(1366, 768);
        var safe = new RectF(0, 0, 1366, 768);
        Assert.AreEqual(FrameLayout.Fit(window, safe, 320, 180, CanvasScaling.Smooth), controls.Frame(window, safe, 320, 180, CanvasScaling.Smooth));
        Assert.AreEqual(FrameLayout.Fit(window, safe, 320, 180), controls.Frame(window, safe, 320, 180));
    }

    [TestMethod]
    public void Windows_open_at_the_requested_scale_when_it_fits_and_shrink_to_the_display_otherwise()
    {
        var pixel = new GameSettings("pixel");
        Assert.AreEqual((1280, 720), WindowSizing.Initial(pixel, 4, 1920, 1040));
        // 4× would be 1280×720 on a 1366×728 usable area: the largest whole factor inside 90 % is 3.
        Assert.AreEqual((960, 540), WindowSizing.Initial(pixel, 4, 1366, 728));
        Assert.AreEqual((1280, 720), WindowSizing.Initial(pixel, 4, 0, 0), "an unknown display keeps the request");
        var hd = new GameSettings("hd", 1920, 1080, 1) { Scaling = CanvasScaling.Smooth };
        Assert.AreEqual((1920, 1080), WindowSizing.Initial(hd, 1, 3840, 2100));
        var (w, h) = WindowSizing.Initial(hd, 1, 1366, 728);
        Assert.AreEqual(1164, w);
        Assert.AreEqual(655, h);
        // A pixel game whose canvas is larger than the display still opens, at the canvas's shape.
        Assert.AreEqual((1164, 655), WindowSizing.Initial(new GameSettings("big", 1920, 1080, 1), 1, 1366, 728));
    }

    [TestMethod]
    public void Hd_canvases_allow_windows_smaller_than_themselves()
    {
        Assert.AreEqual((320, 180), WindowSizing.Minimum(new GameSettings("pixel")));
        Assert.AreEqual((640, 360), WindowSizing.Minimum(new GameSettings("pixel", 640, 360)));
        Assert.AreEqual((480, 270), WindowSizing.Minimum(new GameSettings("hd", 1920, 1080)));
    }
}
