using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Core.Tests.Scenes;

public class FadeTransitionTests
{
    private class MockRenderBackend : IRenderBackend
    {
        public int DrawRectangleCallCount { get; private set; }
        public Color LastDrawColor { get; private set; }

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
        public void SetTargetFPS(int fps) { }
        public float GetDeltaTime() => 0.016f;
        public int GetFPS() => 60;

        public void DrawRectangle(Vector2 position, Vector2 size, Color color)
        {
            DrawRectangleCallCount++;
            LastDrawColor = color;
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

        var transition = new FadeTransition(renderBackend);

        Assert.NotNull(transition);
        Assert.True(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Fact]
    public void Constructor_ThrowsWhenRenderBackendIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FadeTransition(null!));
    }

    [Fact]
    public void Constructor_ThrowsWhenDurationIsNonPositive()
    {
        var renderBackend = new MockRenderBackend();

        Assert.Throws<ArgumentOutOfRangeException>(() => new FadeTransition(renderBackend, duration: 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FadeTransition(renderBackend, duration: -0.5f));
    }

    [Fact]
    public void Start_ResetsStateAndBeginsTransition()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);

        Assert.False(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Fact]
    public void Update_ProgressesTransition()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f); // 50% progress

        Assert.False(transition.IsComplete);
        Assert.Equal(0.5f, transition.Progress);
    }

    [Fact]
    public void Update_CompletesTransitionWhenDurationReached()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(1.0f); // 100% progress

        Assert.True(transition.IsComplete);
        Assert.Equal(1.0f, transition.Progress);
    }

    [Fact]
    public void Update_ClampsProgressToOne()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(2.0f); // 200% would exceed

        Assert.True(transition.IsComplete);
        Assert.Equal(1.0f, transition.Progress);
    }

    [Fact]
    public void Update_DoesNothingWhenComplete()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(1.0f); // Complete
        float progressBefore = transition.Progress;

        transition.Update(0.5f); // Should not change

        Assert.Equal(progressBefore, transition.Progress);
        Assert.True(transition.IsComplete);
    }

    [Fact]
    public void Render_DoesNotDrawWhenProgressIsZero()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Render();

        Assert.Equal(0, renderBackend.DrawRectangleCallCount);
    }

    [Fact]
    public void Render_DrawsWithCorrectAlphaForFadeOut()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f); // 50% progress
        transition.Render();

        Assert.Equal(1, renderBackend.DrawRectangleCallCount);
        // Fade out: alpha should be 50% of 255 = 127.5 ≈ 127
        Assert.InRange(renderBackend.LastDrawColor.A, 125, 130);
    }

    [Fact]
    public void Render_DrawsWithCorrectAlphaForFadeIn()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: false);
        transition.Update(0.5f); // 50% progress
        transition.Render();

        Assert.Equal(1, renderBackend.DrawRectangleCallCount);
        // Fade in: alpha should be 50% of 255 (inverted) = 127.5 ≈ 127
        Assert.InRange(renderBackend.LastDrawColor.A, 125, 130);
    }

    [Fact]
    public void Reset_ResetsTransitionToInitialState()
    {
        var renderBackend = new MockRenderBackend();
        var transition = new FadeTransition(renderBackend, duration: 1.0f);

        transition.Start(fadeOut: true);
        transition.Update(0.5f);
        transition.Reset();

        Assert.True(transition.IsComplete);
        Assert.Equal(0f, transition.Progress);
    }

    [Fact]
    public void CustomFadeColor_UsedWhenRendering()
    {
        var renderBackend = new MockRenderBackend();
        var customColor = new Color(255, 0, 0, 255); // Red
        var transition = new FadeTransition(renderBackend, duration: 1.0f, fadeColor: customColor);

        transition.Start(fadeOut: true);
        transition.Update(1.0f); // Full fade
        transition.Render();

        Assert.Equal(1, renderBackend.DrawRectangleCallCount);
        Assert.Equal(255, renderBackend.LastDrawColor.R);
        Assert.Equal(0, renderBackend.LastDrawColor.G);
        Assert.Equal(0, renderBackend.LastDrawColor.B);
        Assert.Equal(255, renderBackend.LastDrawColor.A);
    }
}
