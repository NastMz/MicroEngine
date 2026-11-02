using System.Numerics;

namespace MicroEngine;

/// <summary>Small numeric helpers that games repeatedly need for feel and motion.</summary>
public static class MathX
{
    /// <summary>Moves <paramref name="value"/> toward <paramref name="target"/> by at most <paramref name="step"/>.</summary>
    public static float Approach(float value, float target, float step)
        => value < target ? MathF.Min(value + step, target) : MathF.Max(value - step, target);

    /// <summary>Moves <paramref name="value"/> toward <paramref name="target"/> by at most <paramref name="step"/> along the straight line.</summary>
    public static Vector2 Approach(Vector2 value, Vector2 target, float step)
    {
        var delta = target - value;
        float length = delta.Length();
        return length <= step || length == 0 ? target : value + delta / length * step;
    }

    /// <summary>Linear interpolation: <paramref name="a"/> at <paramref name="t"/> = 0, <paramref name="b"/> at 1. <paramref name="t"/> is not clamped.</summary>
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>Frame-rate independent exponential smoothing; <paramref name="sharpness"/> is per second.</summary>
    public static float Damp(float current, float target, float sharpness, float dt)
        => Lerp(current, target, 1f - MathF.Exp(-sharpness * dt));

    /// <summary>Frame-rate independent exponential smoothing of a point; <paramref name="sharpness"/> is per second.</summary>
    public static Vector2 Damp(Vector2 current, Vector2 target, float sharpness, float dt)
        => Vector2.Lerp(current, target, 1f - MathF.Exp(-sharpness * dt));

    /// <summary>Returns 0..1..0 over a <paramref name="period"/>, useful for pulses and bobbing.</summary>
    public static float PingPong(float time, float period)
    {
        float t = time / period % 1f;
        return t < 0.5f ? t * 2f : 2f - t * 2f;
    }

    /// <summary>A sine wave: <c>sin(time × frequency × 2π) × amplitude</c>, with <paramref name="frequency"/> in cycles per second.</summary>
    public static float Wave(float time, float frequency, float amplitude = 1f)
        => MathF.Sin(time * frequency * MathF.Tau) * amplitude;

    /// <summary>The index of the cell of width <paramref name="size"/> that contains <paramref name="value"/>, rounding toward negative infinity (so -1 is in cell -1).</summary>
    public static int FloorDiv(float value, int size) => (int)MathF.Floor(value / size);

    /// <summary>A vector of <paramref name="length"/> pointing at <paramref name="radians"/> (0 = right, π/2 = down on screen).</summary>
    public static Vector2 FromAngle(float radians, float length = 1f)
        => new(MathF.Cos(radians) * length, MathF.Sin(radians) * length);
}

/// <summary>Standard easing curves mapping 0..1 to 0..1 (some overshoot).</summary>
public static class Ease
{
    /// <summary>No easing.</summary>
    public static float Linear(float t) => t;
    /// <summary>Starts slow, ends fast (quadratic).</summary>
    public static float InQuad(float t) => t * t;
    /// <summary>Starts fast, ends slow (quadratic); the default of <c>Tweener.Add</c>.</summary>
    public static float OutQuad(float t) => 1 - (1 - t) * (1 - t);
    /// <summary>Slow at both ends (quadratic).</summary>
    public static float InOutQuad(float t) => t < 0.5f ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2;
    /// <summary>Starts fast, ends slow (cubic).</summary>
    public static float OutCubic(float t) => 1 - MathF.Pow(1 - t, 3);
    /// <summary>Starts slow, ends fast (cubic).</summary>
    public static float InCubic(float t) => t * t * t;
    /// <summary>Slow at both ends (sine).</summary>
    public static float InOutSine(float t) => -(MathF.Cos(MathF.PI * t) - 1) / 2;
    /// <summary>Overshoots past 1 and settles back.</summary>
    public static float OutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1;
        return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
    }
    /// <summary>Springs past 1 and oscillates to rest.</summary>
    public static float OutElastic(float t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        return MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * (2 * MathF.PI / 3)) + 1;
    }
    /// <summary>Bounces on 1 like a dropped ball.</summary>
    public static float OutBounce(float t)
    {
        const float n = 7.5625f, d = 2.75f;
        if (t < 1 / d) return n * t * t;
        if (t < 2 / d) return n * (t -= 1.5f / d) * t + 0.75f;
        if (t < 2.5 / d) return n * (t -= 2.25f / d) * t + 0.9375f;
        return n * (t -= 2.625f / d) * t + 0.984375f;
    }
}
