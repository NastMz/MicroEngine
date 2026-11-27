using Examples;
using MicroEngine.Testing;

namespace Samples.Tests;

/// <summary>The effects example: a cast plays three one-shot clips from the caster's hand, and its HUD draws without garbage.</summary>
[TestClass]
public sealed class EffectsExampleTests
{
    [TestMethod]
    public void ACastPlaysThreeEffectsThatEndByThemselves()
    {
        var (game, platform) = SettingsExampleTests.Start(new EffectsExample());
        var scene = (EffectsExample)game.Scenes.Current!;
        Assert.AreEqual(0, scene.Effects.Count);

        SettingsExampleTests.Press(game, platform, "confirm");
        Assert.AreEqual(1, scene.Casts);
        Assert.AreEqual(125, scene.Score);
        Assert.AreEqual(3, scene.Effects.Count, "One at once, two after their delays.");
        for (int i = 0; i < 30; i++) SettingsExampleTests.Frame(game, platform);
        Assert.AreEqual(1, scene.Effects.Count, "The two at 18 frames a second are over; the slow one still plays.");
        for (int i = 0; i < 30; i++) SettingsExampleTests.Frame(game, platform);
        Assert.IsFalse(scene.Effects.Busy, "Every effect removed itself.");
    }

    [TestMethod]
    public void TheBodyBoxChangesSideWhenTheCasterTurns()
    {
        var (game, platform) = SettingsExampleTests.Start(new EffectsExample());
        var scene = (EffectsExample)game.Scenes.Current!;
        var right = scene.Body;
        Assert.AreEqual(new MicroEngine.RectF(144, 76, 32, 52), right, "The sheet's box (4, 3, 8, 13) from the anchor (8, 16), four times as large, at the feet.");
        SettingsExampleTests.Press(game, platform, "left");
        var left = scene.Body;
        Assert.AreEqual(right.Width, left.Width);
        Assert.AreEqual(160 - (right.X + right.Width - 160), left.X, "Mirrored around the feet.");
    }

    [TestMethod]
    public void DrawingTheHudAndTheEffectsEveryFrameMakesNoGarbage()
    {
        var (game, platform) = SettingsExampleTests.Start(new EffectsExample());
        var scene = (EffectsExample)game.Scenes.Current!;
        SettingsExampleTests.Press(game, platform, "confirm");
        var renderer = new MicroEngine.Core.HeadlessRenderer(320, 180);
        AllocationAssert.None(() =>
        {
            renderer.Reset();
            scene.Draw(renderer);
        }, warmup: 30, calls: 120, because: "a score, a clock, counts and a hint, drawn every frame");
    }
}
