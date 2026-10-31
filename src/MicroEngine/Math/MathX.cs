using System.Numerics;

namespace MicroEngine;

/// <summary>Small numeric helpers that games repeatedly need for feel and motion.</summary>
public static class MathX
{
    /// <summary>Moves <paramref name="value"/> toward <paramref name="target"/> by at most <paramref name="step"/>.</summary>
    public static float Approach(float value, float target, float step)
        => value < target ? MathF.Min(value + step, target) : MathF.Max(value - step, target);

    public static Vector2 Approach(Vector2 value, Vector2 target, float step)
    {
        var delta = target - value;
        float length = delta.Length();
        return length <= step || length == 0 ? target : value + delta / length * step;
    }

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>Frame-rate independent exponential smoothing; <paramref name="sharpness"/> is per second.</summary>
    public static float Damp(float current, float target, float sharpness, float dt)
        => Lerp(current, target, 1f - MathF.Exp(-sharpness * dt));

    public static Vector2 Damp(Vector2 current, Vector2 target, float sharpness, float dt)
        => Vector2.Lerp(current, target, 1f - MathF.Exp(-sharpness * dt));

    /// <summary>Returns 0..1..0 over a <paramref name="period"/>, useful for pulses and bobbing.</summary>
    public static float PingPong(float time, float period)
    {
        float t = time / period % 1f;
        return t < 0.5f ? t * 2f : 2f - t * 2f;
    }

    public static float Wave(float time, float frequency, float amplitude = 1f)
        => MathF.Sin(time * frequency * MathF.Tau) * amplitude;

    public static int FloorDiv(float value, int size) => (int)MathF.Floor(value / size);

    public static Vector2 FromAngle(float radians, float length = 1f)
        => new(MathF.Cos(radians) * length, MathF.Sin(radians) * length);
}

/// <summary>Standard easing curves mapping 0..1 to 0..1 (some overshoot).</summary>
public static class Ease
{
    public static float Linear(float t) => t;
    public static float InQuad(float t) => t * t;
    public static float OutQuad(float t) => 1 - (1 - t) * (1 - t);
    public static float InOutQuad(float t) => t < 0.5f ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2;
    public static float OutCubic(float t) => 1 - MathF.Pow(1 - t, 3);
    public static float InCubic(float t) => t * t * t;
    public static float InOutSine(float t) => -(MathF.Cos(MathF.PI * t) - 1) / 2;
    public static float OutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1;
        return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
    }
    public static float OutElastic(float t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        return MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * (2 * MathF.PI / 3)) + 1;
    }
    public static float OutBounce(float t)
    {
        const float n = 7.5625f, d = 2.75f;
        if (t < 1 / d) return n * t * t;
        if (t < 2 / d) return n * (t -= 1.5f / d) * t + 0.75f;
        if (t < 2.5 / d) return n * (t -= 2.25f / d) * t + 0.9375f;
        return n * (t -= 2.625f / d) * t + 0.984375f;
    }
}
