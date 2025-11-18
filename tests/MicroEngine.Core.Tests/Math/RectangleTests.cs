using MicroEngine.Core.Math;

namespace MicroEngine.Core.Tests.Math;

public class RectangleTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var rect = new Rectangle(10f, 20f, 100f, 200f);

        Assert.Equal(10f, rect.X);
        Assert.Equal(20f, rect.Y);
        Assert.Equal(100f, rect.Width);
        Assert.Equal(200f, rect.Height);
    }

    [Fact]
    public void Position_ReturnsTopLeft()
    {
        var rect = new Rectangle(10f, 20f, 100f, 200f);

        var position = rect.Position;

        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
    }

    [Fact]
    public void Center_ReturnsMiddlePoint()
    {
        var rect = new Rectangle(0f, 0f, 100f, 200f);

        var center = rect.Center;

        Assert.Equal(50f, center.X);
        Assert.Equal(100f, center.Y);
    }

    [Fact]
    public void Size_ReturnsWidthHeight()
    {
        var rect = new Rectangle(10f, 20f, 100f, 200f);

        var size = rect.Size;

        Assert.Equal(100f, size.X);
        Assert.Equal(200f, size.Y);
    }

    [Fact]
    public void Contains_PointInside_ReturnsTrue()
    {
        var rect = new Rectangle(0f, 0f, 100f, 100f);

        Assert.True(rect.Contains(new Vector2(50f, 50f)));
        Assert.True(rect.Contains(new Vector2(0f, 0f)));
        Assert.True(rect.Contains(new Vector2(100f, 100f)));
    }

    [Fact]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var rect = new Rectangle(0f, 0f, 100f, 100f);

        Assert.False(rect.Contains(new Vector2(-1f, 50f)));
        Assert.False(rect.Contains(new Vector2(101f, 50f)));
        Assert.False(rect.Contains(new Vector2(50f, -1f)));
        Assert.False(rect.Contains(new Vector2(50f, 101f)));
    }

    [Fact]
    public void Intersects_OverlappingRectangles_ReturnsTrue()
    {
        var rect1 = new Rectangle(0f, 0f, 100f, 100f);
        var rect2 = new Rectangle(50f, 50f, 100f, 100f);

        Assert.True(rect1.Intersects(rect2));
        Assert.True(rect2.Intersects(rect1));
    }

    [Fact]
    public void Intersects_AdjacentRectangles_ReturnsFalse()
    {
        var rect1 = new Rectangle(0f, 0f, 100f, 100f);
        var rect2 = new Rectangle(100f, 0f, 100f, 100f);

        Assert.False(rect1.Intersects(rect2));
        Assert.False(rect2.Intersects(rect1));
    }

    [Fact]
    public void Intersects_SeparateRectangles_ReturnsFalse()
    {
        var rect1 = new Rectangle(0f, 0f, 100f, 100f);
        var rect2 = new Rectangle(200f, 200f, 100f, 100f);

        Assert.False(rect1.Intersects(rect2));
        Assert.False(rect2.Intersects(rect1));
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var rect1 = new Rectangle(10f, 20f, 100f, 200f);
        var rect2 = new Rectangle(10f, 20f, 100f, 200f);

        Assert.True(rect1.Equals(rect2));
        Assert.True(rect1 == rect2);
        Assert.False(rect1 != rect2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var rect1 = new Rectangle(10f, 20f, 100f, 200f);
        var rect2 = new Rectangle(10f, 20f, 100f, 201f);

        Assert.False(rect1.Equals(rect2));
        Assert.True(rect1 != rect2);
        Assert.False(rect1 == rect2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHashCode()
    {
        var rect1 = new Rectangle(10f, 20f, 100f, 200f);
        var rect2 = new Rectangle(10f, 20f, 100f, 200f);

        Assert.Equal(rect1.GetHashCode(), rect2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var rect = new Rectangle(10.5f, 20.5f, 100.5f, 200.5f);

        var str = rect.ToString();

        Assert.Contains("10", str);
        Assert.Contains("20", str);
        Assert.Contains("100", str);
        Assert.Contains("200", str);
        Assert.Contains("Rectangle", str);
    }
}
