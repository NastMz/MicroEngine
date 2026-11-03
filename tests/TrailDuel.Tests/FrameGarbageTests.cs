using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.Testing;
using TrailDuel.Scenes;
using TrailDuel.Ui;

namespace TrailDuel.Tests;

/// <summary>Drawing a screen that does not change makes no garbage: keys, numbers and lines are built once and kept.</summary>
[TestClass]
public sealed class FrameGarbageTests
{
    private sealed class NullRenderer : IRenderer
    {
        public int Width => 320;
        public int Height => 180;
        public void Clear(Color color) { }
        public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default) { }
        public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) { }
        public void FillRect(RectF rect, Color color) { }
        public void FillCircle(Vector2 center, float radius, Color color) { }
        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) { }
        public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) { }
        public void PushCamera(Camera2D camera) { }
        public void PopCamera() { }
        public void SetBlendMode(BlendMode mode) { }
        public void SetClip(RectF? screenRect) { }
    }

    /// <summary>
    /// Fails if drawing <paramref name="scene"/> makes garbage after warm-up draws. Garbage made every frame shows in every
    /// round of draws; a one-off (a cache filled late, a background collection's count) shows in one only, as on macOS
    /// where one batch of the route once allocated about 3 KB in all.
    /// </summary>
    private static void AssertNoGarbage(Scene scene, string what)
    {
        var renderer = new NullRenderer();
        AllocationAssert.None(() => scene.Draw(renderer), warmup: 60, calls: 60, because: $"draws of {what}");
    }

    [TestMethod]
    [DataRow("town", typeof(OverworldScene))]
    [DataRow("route", typeof(OverworldScene))]
    [DataRow("lab", typeof(OverworldScene))]
    [DataRow("party", typeof(PartyScene))]
    [DataRow("shop", typeof(ShopScene))]
    [DataRow("summary", typeof(SummaryScene))]
    public void DrawingAStillScreenMakesNoGarbage(string start, Type scene)
    {
        using var h = new Harness(start);
        for (int i = 0; i < 400 && (h.Game.Scenes.Current?.GetType() != scene || h.Game.Scenes.InTransition); i++) h.Step();
        h.Step(30);
        Assert.IsInstanceOfType(h.Game.Scenes.Current, scene);
        AssertNoGarbage(h.Game.Scenes.Current!, start);
    }

    [TestMethod]
    public void DrawingTheBattleMenuMakesNoGarbage()
    {
        using var h = new Harness("battle");
        var battle = (BattleScene)h.Game.Scenes.Current!;
        for (int i = 0; i < 2000 && !battle.AwaitingChoice; i++) h.Step();
        Assert.IsTrue(battle.AwaitingChoice, "the action menu");
        h.Step(10);
        AssertNoGarbage(battle, "the battle menu");
    }

    [TestMethod]
    public void KeptNumbersReadExactlyAsPlainFormatting()
    {
        Assert.AreEqual("Lv12", NumberTexts.Level(12));
        Assert.AreEqual("Lv500", NumberTexts.Level(500));
        Assert.AreEqual("17/40", NumberTexts.Ratio(17, 40));
        Assert.AreEqual("$1250", NumberTexts.Money(1250));
        Assert.AreEqual("×3", NumberTexts.Count(3));
        Assert.AreEqual("7", NumberTexts.Of(7));
        Assert.AreEqual("12345", NumberTexts.Of(12345));
        Assert.AreSame(NumberTexts.Ratio(17, 40), NumberTexts.Ratio(17, 40), "built once, then kept");
    }
}
