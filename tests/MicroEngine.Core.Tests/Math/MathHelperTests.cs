using MicroEngine.Core.Math;

namespace MicroEngine.Core.Tests.Math;

/// <summary>
/// Unit tests for the <see cref="MathHelper"/> class.
/// </summary>
public class MathHelperTests
{
    [Fact]
    public void ToRadians_ConvertsDegreesCorrectly()
    {
        float degrees = 180f;
        float radians = MathHelper.ToRadians(degrees);

        Assert.Equal(MathF.PI, radians, 5);
    }

    [Fact]
    public void ToDegrees_ConvertsRadiansCorrectly()
    {
        float radians = MathF.PI;
        float degrees = MathHelper.ToDegrees(radians);

        Assert.Equal(180f, degrees, 5);
    }

    [Fact]
    public void Lerp_InterpolatesCorrectly()
    {
        float result = MathHelper.Lerp(0f, 10f, 0.5f);

        Assert.Equal(5f, result);
    }

    [Fact]
    public void Lerp_ClampsValue()
    {
        float resultBelow = MathHelper.Lerp(0f, 10f, -0.5f);
        float resultAbove = MathHelper.Lerp(0f, 10f, 1.5f);

        Assert.Equal(0f, resultBelow);
        Assert.Equal(10f, resultAbove);
    }

    [Fact]
    public void ApproximatelyEqual_ReturnsTrueForSimilarValues()
    {
        Assert.True(MathHelper.ApproximatelyEqual(1f, 1.0000001f));
    }

    [Fact]
    public void ApproximatelyEqual_ReturnsFalseForDifferentValues()
    {
        Assert.False(MathHelper.ApproximatelyEqual(1f, 2f));
    }

    [Fact]
    public void Wrap_WrapsValueCorrectly()
    {
        float result = MathHelper.Wrap(15f, 0f, 10f);

        Assert.Equal(5f, result, 5);
    }

    [Fact]
    public void NormalizeAngle_NormalizesCorrectly()
    {
        float angle = MathHelper.NormalizeAngle(7f);

        Assert.True(angle >= -MathF.PI && angle <= MathF.PI);
    }

    [Fact]
    public void SmoothStep_InterpolatesSmoothly()
    {
        float start = MathHelper.SmoothStep(0f, 10f, 0f);
        float middle = MathHelper.SmoothStep(0f, 10f, 0.5f);
        float end = MathHelper.SmoothStep(0f, 10f, 1f);

        Assert.Equal(0f, start);
        Assert.Equal(5f, middle);
        Assert.Equal(10f, end);
    }

    [Fact]
    public void Sign_ReturnsCorrectSign()
    {
        Assert.Equal(1, MathHelper.Sign(5f));
        Assert.Equal(-1, MathHelper.Sign(-5f));
        Assert.Equal(0, MathHelper.Sign(0f));
    }
}
