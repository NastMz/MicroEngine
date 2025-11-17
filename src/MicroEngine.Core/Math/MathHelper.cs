namespace MicroEngine.Core.Math;

/// <summary>
/// Utility methods for common mathematical operations.
/// </summary>
public static class MathHelper
{
    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>Angle in radians.</returns>
    public static float ToRadians(float degrees) =>
        degrees * MathConstants.DEG_TO_RAD;

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>Angle in degrees.</returns>
    public static float ToDegrees(float radians) =>
        radians * MathConstants.RAD_TO_DEG;

    /// <summary>
    /// Linearly interpolates between two values.
    /// </summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor (0 to 1).</param>
    /// <returns>Interpolated value.</returns>
    public static float Lerp(float a, float b, float t)
    {
        t = System.Math.Clamp(t, 0f, 1f);
        return a + (b - a) * t;
    }

    /// <summary>
    /// Determines if two floating-point values are approximately equal.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <param name="epsilon">Tolerance for comparison.</param>
    /// <returns>True if values are approximately equal.</returns>
    public static bool ApproximatelyEqual(float a, float b, float epsilon = MathConstants.EPSILON) =>
        MathF.Abs(a - b) < epsilon;

    /// <summary>
    /// Wraps a value to the range [min, max].
    /// </summary>
    /// <param name="value">Value to wrap.</param>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <returns>Wrapped value.</returns>
    public static float Wrap(float value, float min, float max)
    {
        float range = max - min;
        return value - range * MathF.Floor((value - min) / range);
    }

    /// <summary>
    /// Normalizes an angle to the range [-π, π].
    /// </summary>
    /// <param name="angle">Angle in radians.</param>
    /// <returns>Normalized angle.</returns>
    public static float NormalizeAngle(float angle) =>
        Wrap(angle, -MathConstants.PI, MathConstants.PI);

    /// <summary>
    /// Smoothly interpolates between two values using Hermite interpolation.
    /// </summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor (0 to 1).</param>
    /// <returns>Smoothly interpolated value.</returns>
    public static float SmoothStep(float a, float b, float t)
    {
        t = System.Math.Clamp(t, 0f, 1f);
        t = t * t * (3f - 2f * t);
        return a + (b - a) * t;
    }

    /// <summary>
    /// Returns the sign of a number (-1, 0, or 1).
    /// </summary>
    /// <param name="value">Value to check.</param>
    /// <returns>-1 if negative, 0 if zero, 1 if positive.</returns>
    public static int Sign(float value)
    {
        if (value > MathConstants.EPSILON)
        {
            return 1;
        }

        if (value < -MathConstants.EPSILON)
        {
            return -1;
        }

        return 0;
    }
}
