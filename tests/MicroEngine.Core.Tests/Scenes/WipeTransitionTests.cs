using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Core.Tests.Scenes;

public class WipeTransitionTests
{
    private class MockRenderBackend : IRenderBackend2D
    {
        public int DrawRectangleCallCount { get; private set; }

        public string WindowTitle { get; set; } = "Test";
        public int WindowWidth => 800;
        public int WindowHeight => 600;
        public bool ShouldClose => false;
        public AntiAliasingMode AntiAliasing { get; set; } = AntiAliasingMode.None;

        public void Initialize(int width, int height, string title) { }
        public void Shutdown() { }
        public void BeginFrame() { }
        public void EndFrame() { }
        public void Clear(Color color) { }

        public void DrawRectangle(Vector2 position, Vector2 size, Color color)
        {
            DrawRectangleCallCount++;
        }

        public void DrawRectangleLines(Vector2 position, Vector2 size, Color color, float thickness = 1f) { }
        public void DrawCircle(Vector2 center, float radius, Color color) { }
        public void DrawCircleLines(Vector2 center, float radius, Color color, float thickness = 1f) { }
        public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f) { }
        public void DrawTexture(Core.Resources.ITexture texture, Vector2 position, Color tint) { }
        public void DrawTexturePro(Core.Resources.ITexture texture, Rectangle sourceRect, Rectangle destRect, Vector2 origin, float rotation, Color tint) { }
        public void DrawText(string text, Vector2 position, int fontSize, Color color) { }
        public void DrawTextEx(Core.Resources.IFont font, string text, Vector2 position, float fontSize, float spacing, Color color) { }
        public void BeginCamera2D(Camera2D camera) { }
        public void EndCamera2D() { }
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var renderBackend = new MockRenderBackend();

        var transition = new WipeTransition(renderBackend);

        Assert.NotNull(transition);
        Assert.True(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Fact]
    public void Constructor_ThrowsWhenRenderBackendIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new WipeTransition(null!));
    }

    [Fact]
    public void Constructor_ThrowsWhenDurationIsNonPositive()
    {
        var renderBackend = new MockRenderBackend();

        Assert.Throws<ArgumentOutOfRangeException>(() => new WipeTransition(renderBackend, duration: 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WipeTransition(renderBackend, duration: -0.5f));
    }

    [Fact]
    public void Start_ResetsStateAndBeginsTransition()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new WipeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);

        Assert.False(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Fact]
    public void Update_ProgressesTransition()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new WipeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f);

        Assert.False(transition.IsComplete);
        Assert.Equal(0.5f, transition.Progress);
    }

    [Fact]
    public void Update_CompletesTransitionWhenDurationReached()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new WipeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(1.0f);

        Assert.True(transition.IsComplete);
        Assert.Equal(1.0f, transition.Progress);
    }

    [Fact]
    public void Render_DrawsWipeOverlayDuringTransition()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new WipeTransition(renderBackend, WipeDirection.LeftToRight, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f);
        transition.Render();

        Assert.True(renderBackend.DrawRectangleCallCount > 0);
    }

    [Fact]
    public void Reset_ResetsTransitionState()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new WipeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f);
        transition.Reset();

        Assert.True(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Theory]
    [InlineData(WipeDirection.LeftToRight)]
    [InlineData(WipeDirection.RightToLeft)]
    [InlineData(WipeDirection.TopToBottom)]
    [InlineData(WipeDirection.BottomToTop)]
    [InlineData(WipeDirection.CenterOut)]
    [InlineData(WipeDirection.EdgeIn)]
    public void Constructor_AcceptsAllWipeDirections(WipeDirection direction)
    {
        var renderBackend = new MockRenderBackend();

        var transition = new WipeTransition(renderBackend, direction);

        Assert.NotNull(transition);
    }

    [Fact]
    public void Render_EdgeInMode_DrawsFourRectangles()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new WipeTransition(renderBackend, WipeDirection.EdgeIn, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f);
        transition.Render();

        // EdgeIn draws 4 rectangles (left, right, top, bottom)
        Assert.Equal(4, renderBackend.DrawRectangleCallCount);
    }
}
