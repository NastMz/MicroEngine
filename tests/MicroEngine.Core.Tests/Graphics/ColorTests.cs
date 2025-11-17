using MicroEngine.Core.Graphics;

namespace MicroEngine.Core.Tests.Graphics;

/// <summary>
/// Tests for Color struct to verify behavior and equality.
/// </summary>
public class ColorTests
{
    [Fact]
    public void Color_Constructor_SetsValuesCorrectly()
    {
        var color = new Color(100, 150, 200, 250);
        
        Assert.Equal(100, color.R);
        Assert.Equal(150, color.G);
        Assert.Equal(200, color.B);
        Assert.Equal(250, color.A);
    }

    [Fact]
    public void Color_Constructor_DefaultAlphaIs255()
    {
        var color = new Color(100, 150, 200);
        
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void Color_CommonColors_HaveCorrectValues()
    {
        Assert.Equal(new Color(255, 255, 255, 255), Color.White);
        Assert.Equal(new Color(0, 0, 0, 255), Color.Black);
        Assert.Equal(new Color(255, 0, 0, 255), Color.Red);
        Assert.Equal(new Color(0, 255, 0, 255), Color.Green);
        Assert.Equal(new Color(0, 0, 255, 255), Color.Blue);
    }

    [Fact]
    public void Color_Transparent_HasZeroAlpha()
    {
        Assert.Equal(0, Color.Transparent.A);
        Assert.Equal(0, Color.Transparent.R);
        Assert.Equal(0, Color.Transparent.G);
        Assert.Equal(0, Color.Transparent.B);
    }

    [Fact]
    public void Color_Equals_ReturnsTrueForSameValues()
    {
        var color1 = new Color(100, 150, 200, 250);
        var color2 = new Color(100, 150, 200, 250);
        
        Assert.True(color1.Equals(color2));
        Assert.True(color1 == color2);
    }

    [Fact]
    public void Color_Equals_ReturnsFalseForDifferentValues()
    {
        var color1 = new Color(100, 150, 200, 250);
        var color2 = new Color(100, 150, 200, 255);
        
        Assert.False(color1.Equals(color2));
        Assert.True(color1 != color2);
    }

    [Fact]
    public void Color_GetHashCode_IsSameForEqualColors()
    {
        var color1 = new Color(100, 150, 200, 250);
        var color2 = new Color(100, 150, 200, 250);
        
        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
    }

    [Fact]
    public void Color_ToString_ReturnsExpectedFormat()
    {
        var color = new Color(100, 150, 200, 250);
        var result = color.ToString();
        
        Assert.Equal("Color(100, 150, 200, 250)", result);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255)]
    [InlineData(128, 64, 192, 32)]
    public void Color_RoundTrip_PreservesValues(byte r, byte g, byte b, byte a)
    {
        var color = new Color(r, g, b, a);
        
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
        Assert.Equal(a, color.A);
    }
}
