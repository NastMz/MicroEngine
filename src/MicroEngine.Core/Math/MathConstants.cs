namespace MicroEngine.Core.Math;

/// <summary>
/// Mathematical constants used throughout the engine.
/// </summary>
public static class MathConstants
{
    /// <summary>
    /// The mathematical constant pi (π).
    /// </summary>
    public const float PI = MathF.PI;

    /// <summary>
    /// Two times pi (2π).
    /// </summary>
    public const float TWO_PI = PI * 2f;

    /// <summary>
    /// Half of pi (π/2).
    /// </summary>
    public const float HALF_PI = PI / 2f;

    /// <summary>
    /// The mathematical constant e (Euler's number).
    /// </summary>
    public const float E = MathF.E;

    /// <summary>
    /// Conversion factor from degrees to radians (π/180).
    /// </summary>
    public const float DEG_TO_RAD = PI / 180f;

    /// <summary>
    /// Conversion factor from radians to degrees (180/π).
    /// </summary>
    public const float RAD_TO_DEG = 180f / PI;

    /// <summary>
    /// Small value used for floating-point comparisons.
    /// </summary>
    public const float EPSILON = 1e-6f;

    /// <summary>
    /// The golden ratio (φ).
    /// </summary>
    public const float GOLDEN_RATIO = 1.618033988749895f;
}
