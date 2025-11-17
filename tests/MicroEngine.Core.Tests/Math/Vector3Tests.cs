using MicroEngine.Core.Math;

namespace MicroEngine.Core.Tests.Math;

/// <summary>
/// Unit tests for the <see cref="Vector3"/> struct.
/// </summary>
public class Vector3Tests
{
    [Fact]
    public void Constructor_WithThreeParameters_SetsComponentsCorrectly()
    {
        var vector = new Vector3(1f, 2f, 3f);

        Assert.Equal(1f, vector.X);
        Assert.Equal(2f, vector.Y);
        Assert.Equal(3f, vector.Z);
    }

    [Fact]
    public void Constructor_WithVector2AndZ_SetsComponentsCorrectly()
    {
        var xy = new Vector2(1f, 2f);
        var vector = new Vector3(xy, 3f);

        Assert.Equal(1f, vector.X);
        Assert.Equal(2f, vector.Y);
        Assert.Equal(3f, vector.Z);
    }

    [Fact]
    public void Zero_ReturnsVectorWithAllComponentsZero()
    {
        var zero = Vector3.Zero;

        Assert.Equal(0f, zero.X);
        Assert.Equal(0f, zero.Y);
        Assert.Equal(0f, zero.Z);
    }

    [Fact]
    public void DirectionVectors_ReturnCorrectValues()
    {
        Assert.Equal(new Vector3(0f, 0f, 1f), Vector3.Forward);
        Assert.Equal(new Vector3(0f, 0f, -1f), Vector3.Backward);
        Assert.Equal(new Vector3(0f, 1f, 0f), Vector3.Up);
        Assert.Equal(new Vector3(0f, -1f, 0f), Vector3.Down);
        Assert.Equal(new Vector3(1f, 0f, 0f), Vector3.Right);
        Assert.Equal(new Vector3(-1f, 0f, 0f), Vector3.Left);
    }

    [Fact]
    public void Magnitude_CalculatesCorrectly()
    {
        var vector = new Vector3(2f, 3f, 6f);
        float expected = MathF.Sqrt(4f + 9f + 36f);

        Assert.Equal(expected, vector.Magnitude, 5);
    }

    [Fact]
    public void Normalized_ReturnsUnitVector()
    {
        var vector = new Vector3(2f, 3f, 6f);
        var normalized = vector.Normalized;

        Assert.Equal(1f, normalized.Magnitude, 5);
    }

    [Fact]
    public void Addition_AddsComponentsCorrectly()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(4f, 5f, 6f);

        var result = a + b;

        Assert.Equal(5f, result.X);
        Assert.Equal(7f, result.Y);
        Assert.Equal(9f, result.Z);
    }

    [Fact]
    public void Cross_CalculatesCrossProductCorrectly()
    {
        var a = new Vector3(1f, 0f, 0f);
        var b = new Vector3(0f, 1f, 0f);

        var result = Vector3.Cross(a, b);

        Assert.Equal(0f, result.X);
        Assert.Equal(0f, result.Y);
        Assert.Equal(1f, result.Z);
    }

    [Fact]
    public void Dot_CalculatesDotProductCorrectly()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(4f, 5f, 6f);

        float dot = Vector3.Dot(a, b);

        Assert.Equal(32f, dot);
    }

    [Fact]
    public void Distance_CalculatesDistanceCorrectly()
    {
        var a = new Vector3(0f, 0f, 0f);
        var b = new Vector3(3f, 4f, 0f);

        float distance = Vector3.Distance(a, b);

        Assert.Equal(5f, distance, 5);
    }

    [Fact]
    public void Lerp_InterpolatesCorrectly()
    {
        var a = new Vector3(0f, 0f, 0f);
        var b = new Vector3(10f, 20f, 30f);

        var result = Vector3.Lerp(a, b, 0.5f);

        Assert.Equal(5f, result.X);
        Assert.Equal(10f, result.Y);
        Assert.Equal(15f, result.Z);
    }

    [Fact]
    public void Equals_ReturnsTrueForEqualVectors()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(1f, 2f, 3f);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }
}
