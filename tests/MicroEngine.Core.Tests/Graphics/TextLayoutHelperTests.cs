using MicroEngine.Core.Graphics;
using MicroEngine.Core.Math;
using Moq;
using Xunit;

namespace MicroEngine.Core.Tests.Graphics;

/// <summary>
/// Unit tests for TextLayoutHelper class.
/// </summary>
public sealed class TextLayoutHelperTests
{
    private readonly Mock<IRenderer2D> _mockRenderBackend;

    public TextLayoutHelperTests()
    {
        _mockRenderBackend = new Mock<IRenderer2D>();
    }

    [Fact]
    public void Constructor_SetsInitialPosition()
    {
        // Arrange & Act
        var layout = new TextLayoutHelper(startX: 10, startY: 20);

        // Assert
        Assert.Equal(10, layout.CurrentX);
        Assert.Equal(20, layout.CurrentY);
    }

    [Fact(Skip = "TextLayoutHelper no longer takes renderer in constructor")]
    public void Constructor_ThrowsWhenRenderBackendIsNull()
    {
        // Test obsoleto - TextLayoutHelper es ref struct sin renderer
        Assert.True(true);
    }

    [Fact]
    public void DrawText_CallsRenderBackendWithCorrectParameters()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);
        var color = new Color(255, 255, 255, 255);

        // Act
        layout.DrawText(_mockRenderBackend.Object, "Test", 14, color);

        // Assert
        _mockRenderBackend.Verify(
            rb => rb.DrawText("Test", new Vector2(10, 20), 14, color),
            Times.Once);
    }

    [Fact]
    public void DrawText_AdvancesYPosition()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20, defaultLineHeight: 20);

        // Act
        layout.DrawText(_mockRenderBackend.Object, "Test", 14, Color.White);

        // Assert
        Assert.Equal(40, layout.CurrentY); // 20 + 20
    }

    [Fact]
    public void DrawText_UsesCustomLineHeight()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);

        // Act
        layout.DrawText(_mockRenderBackend.Object, "Test", 14, Color.White, customLineHeight: 30);

        // Assert
        Assert.Equal(50, layout.CurrentY); // 20 + 30
    }

    [Fact]
    public void DrawText_CalculatesLineHeightFromFontSizeWhenDefaultIsZero()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20, defaultLineHeight: 0);

        // Act
        layout.DrawText(_mockRenderBackend.Object, "Test", 14, Color.White);

        // Assert
        Assert.Equal(40, layout.CurrentY); // 20 + (14 + 6)
    }

    [Fact]
    public void DrawText_SupportsMethodChaining()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);

        // Act
        var result = layout.DrawText(_mockRenderBackend.Object, "Test1", 14, Color.White)
                           .DrawText(_mockRenderBackend.Object, "Test2", 14, Color.White);

        // Assert - ref struct returns itself by value, verify Y position advanced
        Assert.Equal(60, result.CurrentY); // 20 + 20 + 20 (two texts with default line height)
    }

    [Fact]
    public void AddSpacing_IncreasesYPosition()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);

        // Act
        layout.AddSpacing(15);

        // Assert
        Assert.Equal(35, layout.CurrentY);
    }

    [Fact]
    public void SetX_UpdatesCurrentX()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);

        // Act
        layout.SetX(50);

        // Assert
        Assert.Equal(50, layout.CurrentX);
    }

    [Fact]
    public void SetY_UpdatesCurrentY()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);

        // Act
        layout.SetY(100);

        // Assert
        Assert.Equal(100, layout.CurrentY);
    }

    [Fact]
    public void SetPosition_UpdatesBothXAndY()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);

        // Act
        layout.SetPosition(50, 100);

        // Assert
        Assert.Equal(50, layout.CurrentX);
        Assert.Equal(100, layout.CurrentY);
    }

    [Fact]
    public void ResetY_ResetsToStartingY()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);
        layout.SetY(100);

        // Act
        layout.ResetY();

        // Assert
        Assert.Equal(20, layout.CurrentY);
    }

    [Fact]
    public void Reset_ResetsBothXAndY()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);
        layout.SetPosition(100, 200);

        // Act
        layout.Reset();

        // Assert
        Assert.Equal(10, layout.CurrentX);
        Assert.Equal(20, layout.CurrentY);
    }

    [Fact]
    public void NewColumn_SetsXAndResetsY()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);
        layout.DrawText(_mockRenderBackend.Object, "Test", 14, Color.White); // Advances Y

        // Act
        layout.NewColumn(200);

        // Assert
        Assert.Equal(210, layout.CurrentX); // 10 + 200
        Assert.Equal(20, layout.CurrentY); // Reset to start
    }

    [Fact]
    public void DrawSection_DrawsTitleAndAddsSpacing()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20, defaultLineHeight: 20);
        var titleColor = new Color(100, 200, 255, 255);

        // Act
        layout.DrawSection(_mockRenderBackend.Object, "Section Title", 18, titleColor, spacingAfter: 10);

        // Assert
        _mockRenderBackend.Verify(
            rb => rb.DrawText("Section Title", new Vector2(10, 20), 18, titleColor),
            Times.Once);
        Assert.Equal(50, layout.CurrentY); // 20 + 20 (line height) + 10 (spacing)
    }

    [Fact]
    public void DrawKeyValue_DrawsBothKeyAndValue()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20);
        var keyColor = new Color(200, 200, 200, 255);
        var valueColor = new Color(100, 255, 100, 255);

        // Act
        layout.DrawKeyValue(_mockRenderBackend.Object, "Key", "Value", 12, keyColor, valueColor);

        // Assert
        _mockRenderBackend.Verify(
            rb => rb.DrawText("Key", new Vector2(10, 20), 12, keyColor),
            Times.Once);
        _mockRenderBackend.Verify(
            rb => rb.DrawText("Value", It.IsAny<Vector2>(), 12, valueColor),
            Times.Once);
    }

    [Fact]
    public void DefaultLineHeight_CanBeChanged()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20, defaultLineHeight: 20);

        // Act
        layout.DefaultLineHeight = 30;
        layout.DrawText(_mockRenderBackend.Object, "Test", 14, Color.White);

        // Assert
        Assert.Equal(50, layout.CurrentY); // 20 + 30
    }

    [Fact]
    public void FluentAPI_WorksCorrectly()
    {
        // Arrange
        var layout = new TextLayoutHelper(startX: 10, startY: 20, defaultLineHeight: 20);

        // Act
        layout.DrawText(_mockRenderBackend.Object, "Title", 20, Color.White)
              .AddSpacing(10)
              .DrawText(_mockRenderBackend.Object, "Line 1", 14, Color.White)
              .DrawText(_mockRenderBackend.Object, "Line 2", 14, Color.White)
              .SetX(50)
              .DrawText(_mockRenderBackend.Object, "Column 2", 14, Color.White);

        // Assert
        Assert.Equal(50, layout.CurrentX);
        // 20 (start) + 20 (title line height) + 10 (spacing) + 20 (line1) + 20 (line2) + 20 (column2) = 110
        Assert.Equal(110, layout.CurrentY);
    }
}

