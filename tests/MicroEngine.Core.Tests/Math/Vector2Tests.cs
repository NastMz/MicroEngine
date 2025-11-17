using MicroEngine.Core.Math;

namespace MicroEngine.Core.Tests.Math;

/// <summary>
/// Unit tests for the <see cref="Vector2"/> struct.
/// </summary>
public class Vector2Tests
{
    [Fact]
    public void Constructor_WithTwoParameters_SetsComponentsCorrectly()
    {
        var vector = new Vector2(3f, 4f);

        Assert.Equal(3f, vector.X);
        Assert.Equal(4f, vector.Y);
    }

    [Fact]
    public void Constructor_WithSingleParameter_SetsAllComponentsToSameValue()
    {
        var vector = new Vector2(5f);

        Assert.Equal(5f, vector.X);
        Assert.Equal(5f, vector.Y);
    }

    [Fact]
    public void Zero_ReturnsVectorWithAllComponentsZero()
    {
        var zero = Vector2.Zero;

        Assert.Equal(0f, zero.X);
        Assert.Equal(0f, zero.Y);
    }

    [Fact]
    public void One_ReturnsVectorWithAllComponentsOne()
    {
        var one = Vector2.One;

        Assert.Equal(1f, one.X);
        Assert.Equal(1f, one.Y);
    }

    [Fact]
    public void UnitX_ReturnsCorrectUnitVector()
    {
        var unitX = Vector2.UnitX;

        Assert.Equal(1f, unitX.X);
        Assert.Equal(0f, unitX.Y);
    }

    [Fact]
    public void UnitY_ReturnsCorrectUnitVector()
    {
        var unitY = Vector2.UnitY;

        Assert.Equal(0f, unitY.X);
        Assert.Equal(1f, unitY.Y);
    }

    [Fact]
    public void Magnitude_CalculatesCorrectly()
    {
        var vector = new Vector2(3f, 4f);

        Assert.Equal(5f, vector.Magnitude, 5);
    }

    [Fact]
    public void SqrMagnitude_CalculatesCorrectly()
    {
        var vector = new Vector2(3f, 4f);

        Assert.Equal(25f, vector.SqrMagnitude);
    }

    [Fact]
    public void Normalized_ReturnsUnitVector()
    {
        var vector = new Vector2(3f, 4f);
        var normalized = vector.Normalized;

        Assert.Equal(1f, normalized.Magnitude, 5);
    }

    [Fact]
    public void Addition_AddsComponentsCorrectly()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(3f, 4f);

        var result = a + b;

        Assert.Equal(4f, result.X);
        Assert.Equal(6f, result.Y);
    }

    [Fact]
    public void Subtraction_SubtractsComponentsCorrectly()
    {
        var a = new Vector2(5f, 7f);
        var b = new Vector2(2f, 3f);

        var result = a - b;

        Assert.Equal(3f, result.X);
        Assert.Equal(4f, result.Y);
    }

    [Fact]
    public void Negation_NegatesAllComponents()
    {
        var vector = new Vector2(3f, -4f);

        var result = -vector;

        Assert.Equal(-3f, result.X);
        Assert.Equal(4f, result.Y);
    }

    [Fact]
    public void ScalarMultiplication_MultipliesCorrectly()
    {
        var vector = new Vector2(2f, 3f);

        var result = vector * 2f;

        Assert.Equal(4f, result.X);
        Assert.Equal(6f, result.Y);
    }

    [Fact]
    public void ScalarDivision_DividesCorrectly()
    {
        var vector = new Vector2(6f, 8f);

        var result = vector / 2f;

        Assert.Equal(3f, result.X);
        Assert.Equal(4f, result.Y);
    }

    [Fact]
    public void Dot_CalculatesDotProductCorrectly()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(3f, 4f);

        float dot = Vector2.Dot(a, b);

        Assert.Equal(11f, dot);
    }

    [Fact]
    public void Distance_CalculatesDistanceCorrectly()
    {
        var a = new Vector2(0f, 0f);
        var b = new Vector2(3f, 4f);

        float distance = Vector2.Distance(a, b);

        Assert.Equal(5f, distance, 5);
    }

    [Fact]
    public void Lerp_InterpolatesCorrectly()
    {
        var a = new Vector2(0f, 0f);
        var b = new Vector2(10f, 10f);

        var result = Vector2.Lerp(a, b, 0.5f);

        Assert.Equal(5f, result.X);
        Assert.Equal(5f, result.Y);
    }

    [Fact]
    public void Lerp_ClampsToZeroAndOne()
    {
        var a = new Vector2(0f, 0f);
        var b = new Vector2(10f, 10f);

        var resultBelow = Vector2.Lerp(a, b, -0.5f);
        var resultAbove = Vector2.Lerp(a, b, 1.5f);

        Assert.Equal(a, resultBelow);
        Assert.Equal(b, resultAbove);
    }

    [Fact]
    public void Equals_ReturnsTrueForEqualVectors()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(1f, 2f);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentVectors()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(3f, 4f);

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var vector = new Vector2(1.234f, 5.678f);

        string result = vector.ToString();

        Assert.Contains("1", result);
        Assert.Contains("5", result);
        Assert.Contains("(", result);
        Assert.Contains(")", result);
    }
}
