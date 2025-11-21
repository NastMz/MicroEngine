using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Core.Tests.Scenes;

public class ZoomTransitionTests
{
    private class MockRenderBackend : IRenderer2D, IWindow
    {
        public int DrawRectangleCallCount { get; private set; }

        public string Title { get; set; } = "Test";
        public int Width => 800;
        public int Height => 600;
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

        var transition = new ZoomTransition(renderBackend, renderBackend);

        Assert.NotNull(transition);
        Assert.True(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Fact]
    public void Constructor_ThrowsWhenRenderBackendIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ZoomTransition(null!, new MockRenderBackend()));
        Assert.Throws<ArgumentNullException>(() => new ZoomTransition(new MockRenderBackend(), null!));
    }

    [Fact]
    public void Constructor_ThrowsWhenDurationIsNonPositive()
    {
        var renderBackend = new MockRenderBackend();

        Assert.Throws<ArgumentOutOfRangeException>(() => new ZoomTransition(renderBackend, renderBackend, duration: 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZoomTransition(renderBackend, renderBackend, duration: -0.5f));
    }

    [Fact]
    public void Start_ResetsStateAndBeginsTransition()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new ZoomTransition(renderBackend, renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);

        Assert.False(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Fact]
    public void Update_ProgressesTransition()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new ZoomTransition(renderBackend, renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f);

        Assert.False(transition.IsComplete);
        Assert.Equal(0.5f, transition.Progress);
    }

    [Fact]
    public void Update_CompletesTransitionWhenDurationReached()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new ZoomTransition(renderBackend, renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(1.0f);

        Assert.True(transition.IsComplete);
        Assert.Equal(1.0f, transition.Progress);
    }

    [Fact]
    public void Render_DrawsZoomOverlayDuringTransition()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new ZoomTransition(renderBackend, renderBackend, ZoomMode.ZoomOut, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f);
        transition.Render();

        // Zoom draws fullscreen overlay + 4 border rectangles
        Assert.Equal(5, renderBackend.DrawRectangleCallCount);
    }

    [Fact]
    public void Reset_ResetsTransitionState()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new ZoomTransition(renderBackend, renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f);
        transition.Reset();

        Assert.True(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Theory]
    [InlineData(ZoomMode.ZoomIn)]
    [InlineData(ZoomMode.ZoomOut)]
    public void Constructor_AcceptsAllZoomModes(ZoomMode zoomMode)
    {
        var renderBackend = new MockRenderBackend();

        var transition = new ZoomTransition(renderBackend, renderBackend, zoomMode);

        Assert.NotNull(transition);
    }
}
