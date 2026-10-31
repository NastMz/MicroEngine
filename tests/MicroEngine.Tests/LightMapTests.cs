using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

[TestClass]
public sealed class LightMapTests
{
    [TestMethod]
    public void Brightness_falls_off_from_the_light_to_ambient()
    {
        var map = new LightMap(null) { Darkness = 0.8f };
        map.Add(new Vector2(100, 100), radius: 40, brightness: 1f);
        Assert.AreEqual(1f, map.BrightnessAt(new Vector2(100, 100)), 0.001f);
        float mid = map.BrightnessAt(new Vector2(120, 100));
        Assert.IsTrue(mid is > 0.2f and < 1f, $"Half way should be partly lit, was {mid}.");
        Assert.AreEqual(0.2f, map.BrightnessAt(new Vector2(200, 100)), 0.001f, "Outside every light only ambient remains.");
    }

    [TestMethod]
    public void Glow_only_lights_do_not_lift_darkness()
    {
        var map = new LightMap(null) { Darkness = 1f };
        map.Add(Vector2.Zero, 30, brightness: 0, glow: Color.White);
        Assert.AreEqual(1, map.LightCount);
        Assert.AreEqual(0f, map.BrightnessAt(Vector2.Zero), 0.001f);
        map.Add(Vector2.Zero, 30, brightness: 0);
        Assert.AreEqual(1, map.LightCount, "A light that neither brightens nor glows is ignored.");
    }

    [TestMethod]
    public void Uniform_darkness_draws_one_merged_strip_per_cell_row()
    {
        var renderer = new HeadlessRenderer(320, 180);
        var map = new LightMap(null) { Darkness = 1f, CellSize = 4 };
        map.Draw(renderer, Vector2.Zero, new RectF(0, 0, 320, 180));
        Assert.AreEqual(180 / 4, renderer.DrawCalls);
    }

    [TestMethod]
    public void Glows_are_drawn_once_per_glowing_light_and_respect_intensity()
    {
        var renderer = new HeadlessRenderer(320, 180);
        using var map = new LightMap(new HeadlessGraphicsDevice());
        map.Add(new Vector2(50, 50), 20, 0, Color.Hex("ffaa00"));
        map.Add(new Vector2(80, 50), 20, 1);
        map.Draw(renderer, Vector2.Zero, new RectF(0, 0, 320, 180));
        Assert.AreEqual(1, renderer.DrawCalls, "Darkness 0 skips the grid; only the glowing light draws.");
        renderer.Reset();
        map.GlowIntensity = 0;
        map.Draw(renderer, Vector2.Zero, new RectF(0, 0, 320, 180));
        Assert.AreEqual(0, renderer.DrawCalls);
    }

    [TestMethod]
    public void Glow_image_is_bright_at_the_centre_and_clear_at_the_edge()
    {
        var image = LightMap.CreateGlowImage(32);
        Assert.IsTrue(image[16, 16].A > 240);
        Assert.AreEqual(0, image[0, 0].A);
        Assert.IsTrue(image[16, 16].A > image[24, 16].A);
    }
}
